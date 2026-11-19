"""Tiny web layer on top of WSGI (standard library only): requests, responses, routing."""
import hmac
import re
import secrets
from http.cookies import SimpleCookie
from urllib.parse import parse_qs, quote

from . import config, db, i18n, security


class HTTPError(Exception):
    def __init__(self, status, message_key=None):
        Exception.__init__(self, status)
        self.status = status
        self.message_key = message_key


class Response:
    def __init__(self, body=b"", status=200, content_type="text/html; charset=utf-8", headers=None):
        if isinstance(body, str):
            body = body.encode("utf-8")
        self.body = body
        self.status = status
        self.headers = [("Content-Type", content_type)]
        self.cookies = []
        if headers:
            self.headers.extend(headers)

    def set_cookie(self, name, value, max_age=None, http_only=True):
        parts = ["{}={}".format(name, value), "Path=/", "SameSite=Lax"]
        if http_only:
            parts.append("HttpOnly")
        if config.COOKIE_SECURE:
            parts.append("Secure")
        if max_age is not None:
            parts.append("Max-Age={}".format(max_age))
        self.cookies.append("; ".join(parts))

    def delete_cookie(self, name):
        self.set_cookie(name, "", max_age=0)


def redirect(location, status=303):
    return Response(b"", status=status, headers=[("Location", location)])


STATUS_TEXT = {200: "OK", 303: "See Other", 301: "Moved Permanently", 304: "Not Modified",
               400: "Bad Request", 403: "Forbidden", 404: "Not Found", 405: "Method Not Allowed",
               413: "Payload Too Large", 429: "Too Many Requests", 500: "Internal Server Error"}


_LINK = re.compile(r'(href|action)="/(?!static/|en/|en"|favicon\.ico|robots\.txt|sitemap\.xml|simulateur/fichiers/)([^"]*)"')


class Request:
    def __init__(self, environ):
        self.environ = environ
        self.method = environ.get("REQUEST_METHOD", "GET").upper()
        self.path = environ.get("PATH_INFO", "/") or "/"
        # Language of the page: the French pages have no prefix, the English ones start with /en (the path used by the routes has no prefix).
        self.prefix = ""
        self.lang = config.DEFAULT_LANG
        if self.path == "/en" or self.path.startswith("/en/"):
            self.prefix, self.lang = "/en", "en"
            self.path = self.path[3:] or "/"
        self.query = {k: v[0] for k, v in parse_qs(environ.get("QUERY_STRING", ""), keep_blank_values=True).items()}
        self.cookies = {}
        raw = environ.get("HTTP_COOKIE", "")
        if raw:
            try:
                jar = SimpleCookie()
                jar.load(raw)
                self.cookies = {k: m.value for k, m in jar.items()}
            except Exception:
                self.cookies = {}
        self._form = None
        self._conn = None
        self._user = False
        self.new_csrf = None
        self.extra_cookies = []   # (name, value, max_age, http_only) added by routes
        self.params = {}

    # -- client
    @property
    def ip(self):
        if config.TRUST_PROXY:
            fwd = self.environ.get("HTTP_X_FORWARDED_FOR", "")
            if fwd:
                return fwd.split(",")[-1].strip()
        return self.environ.get("REMOTE_ADDR", "unknown")

    @property
    def base_url(self):
        # Local mode: links in mails point to whatever address you are using. Production: the fixed BASE_URL.
        host = self.environ.get("HTTP_HOST")
        if config.DEV and host:
            return "http://" + host
        return config.BASE_URL

    # -- language of the links: the pages of a language keep their visitors in that language
    def localize(self, html):
        """Adds the language prefix to the internal links of a page (href="/x" becomes href="/en/x" on the English pages)."""
        if not self.prefix:
            return html
        return _LINK.sub(lambda m: '{}="{}{}"'.format(m.group(1), self.prefix, "/" + m.group(2) if m.group(2) else ""), html)

    def localize_location(self, location):
        if self.prefix and location.startswith("/") and not location.startswith(("/static/", self.prefix + "/")) and location != self.prefix:
            return self.prefix + (location if location != "/" else "")
        return location

    # -- text
    def t(self, key, **kw):
        return i18n.translate(self.lang, key, **kw)

    # -- database
    @property
    def db(self):
        if self._conn is None:
            self._conn = db.connect()
        return self._conn

    def finish_db(self, ok):
        if self._conn is not None:
            try:
                if ok:
                    self._conn.commit()
                else:
                    self._conn.rollback()
            finally:
                self._conn.close()
                self._conn = None

    # -- user
    @property
    def user(self):
        if self._user is False:
            self._user = security.user_for_session(self.db, self.cookies.get("gl_session"))
        return self._user

    # -- forms
    @property
    def form(self):
        if self._form is None:
            self._form = {}
            if self.method == "POST":
                try:
                    length = int(self.environ.get("CONTENT_LENGTH") or 0)
                except ValueError:
                    length = 0
                if length > config.MAX_BODY_BYTES:
                    raise HTTPError(413)
                ctype = self.environ.get("CONTENT_TYPE", "")
                if length and not ctype.startswith("application/x-www-form-urlencoded"):
                    raise HTTPError(400)
                raw = self.environ["wsgi.input"].read(length) if length else b""
                self._form = {k: v[0] for k, v in
                              parse_qs(raw.decode("utf-8", "replace"), keep_blank_values=True).items()}
        return self._form

    # -- CSRF (a secret value in a cookie that must come back inside the form)
    @property
    def csrf(self):
        token = self.cookies.get("gl_csrf")
        if not token or not re.fullmatch(r"[A-Za-z0-9_-]{20,64}", token):
            if self.new_csrf is None:
                self.new_csrf = secrets.token_urlsafe(24)
            return self.new_csrf
        return token

    def check_csrf(self):
        sent = self.form.get("csrf", "")
        cookie = self.cookies.get("gl_csrf", "")
        if not cookie or not hmac.compare_digest(sent.encode(), cookie.encode()):
            raise HTTPError(403, "error.csrf")

    # -- flash messages (a translation key in a short-lived cookie)
    def flash(self, key):
        self.extra_cookies.append(("gl_flash", key, 60, True))

    def pop_flash(self):
        key = self.cookies.get("gl_flash")
        if key and i18n.has(self.lang, key) and key.startswith("flash."):
            return key
        return None


class Router:
    def __init__(self):
        self.routes = []

    def add(self, method, pattern, handler):
        int_names = set(re.findall(r"<int:(\w+)>", pattern))
        regex = "^" + re.sub(r"<(int:)?(\w+)>",
                             lambda m: "(?P<{}>{})".format(m.group(2), r"\d+" if m.group(1) else r"[^/]+"),
                             pattern) + "$"
        self.routes.append((method, re.compile(regex), handler, int_names))

    def route(self, method, pattern):
        def deco(fn):
            self.add(method, pattern, fn)
            return fn
        return deco

    def match(self, method, path):
        path_known = False
        for m, rx, handler, int_names in self.routes:
            found = rx.match(path)
            if found:
                path_known = True
                if m == method or (method == "HEAD" and m == "GET"):
                    params = {k: (int(v) if k in int_names else v) for k, v in found.groupdict().items()}
                    return handler, params
        if path_known:
            raise HTTPError(405)
        raise HTTPError(404)


def login_url(path):
    return "/connexion?suite=" + quote(path, safe="/")
