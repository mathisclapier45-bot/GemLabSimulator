"""The WSGI application: ties routes together and adds the security headers to every answer."""
import http.client
import logging
import mimetypes
import os
import re

from . import config, db
from . import routes_account, routes_forum, routes_site
from .views import esc, page
from .web import HTTPError, Request, Response, Router

log = logging.getLogger("gemlab")

router = Router()
for part in (routes_site, routes_account, routes_forum):
    router.routes.extend(part.router.routes)

CSP_DEFAULT = ("default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; "
               "frame-ancestors 'none'; base-uri 'self'; form-action 'self'")
# Unity (WebAssembly) needs a few more permissions, only on the simulator page.
CSP_SIMULATOR = ("default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; "
                 "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval' blob:; connect-src 'self' blob: data:; "
                 "media-src 'self' blob: data:; worker-src 'self' blob:; frame-ancestors 'none'; base-uri 'self'; "
                 "form-action 'self'")


def serve_static(req, rel):
    root = config.STATIC_DIR.resolve()
    target = (root / rel).resolve()
    if root not in target.parents or not target.is_file():
        raise HTTPError(404)
    ctype = mimetypes.guess_type(str(target))[0] or "application/octet-stream"
    if ctype.startswith("text/") or ctype in ("application/javascript", "image/svg+xml"):
        ctype += "; charset=utf-8"
    resp = Response(target.read_bytes(), content_type=ctype)
    resp.headers.append(("Cache-Control", "no-cache" if config.DEV else "public, max-age=3600"))
    return resp


def dev_mails(req):
    """Local test mailbox: shows the mails the site would have sent (never in production)."""
    rows = req.db.execute("SELECT * FROM outbox ORDER BY id DESC LIMIT 30").fetchall()
    items = "".join('<article class="post"><header><strong>{}</strong> <span class="meta">{}</span></header>'
                    '<pre class="mail">{}</pre></article>'.format(esc(r["subject"]), esc(r["to_addr"]),
                                                   re.sub(r"(https?://[^\s<]+)", r'<a href="\1">\1</a>', esc(r["body"])))
                    for r in rows)
    return page(req, req.t("dev.mails.title"),
                '<h1>{}</h1><p class="lead">{}</p>{}'.format(
                    esc(req.t("dev.mails.title")), esc(req.t("dev.mails.lead")),
                    items or "<p class=\"empty\">{}</p>".format(esc(req.t("dev.mails.empty")))))


def dev_photos(req):
    """Local review page for the candidate photos (never in production)."""
    import json
    path = config.DATA_DIR / "photo_candidates.json"
    if not path.exists():
        return page(req, req.t("dev.photos.title"), "<p>{}</p>".format(esc(req.t("dev.photos.run"))))
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    rows = []
    for key, item in data.items():
        cells = "".join(
            '<figure class="fig"><img src="{th}" alt="" loading="lazy"><figcaption><strong>{n}</strong> · {lic}<br>'
            '<span class="small"><a href="{pg}" target="_blank" rel="noopener noreferrer">{au}</a></span></figcaption></figure>'.format(
                th=esc(c["thumb"] or ""), n=i + 1, lic=esc(c["license"]), pg=esc(c["page"] or ""), au=esc(c["author"][:60]))
            for i, c in enumerate(item["candidates"]))
        rows.append("<h2>{} <span class=\"small\">({})</span></h2><div class=\"gallery\">{}</div>".format(
            esc(item["name"]), esc(key), cells or "<p class=\"empty\">{}</p>".format(esc(req.t("dev.photos.none")))))
    return page(req, req.t("dev.photos.title"), "<h1>{}</h1><p class=\"lead\">{}</p>{}".format(
        esc(req.t("dev.photos.title")), esc(req.t("dev.photos.lead")), "".join(rows)))


if config.DEV:
    router.add("GET", "/dev/courriels", dev_mails)
    router.add("GET", "/dev/photos", dev_photos)


def error_response(req, status, key=None):
    text_key = key or "error.{}".format(status)
    try:
        title = req.t("error.title.{}".format(status)) if status in (400, 403, 404, 405, 413, 429) else req.t("error.title.500")
        resp = page(req, title, '<section class="narrow"><h1>{}</h1><p>{}</p><p><a class="btn" href="/">{}</a></p></section>'.format(
            esc(title), esc(req.t(text_key)), esc(req.t("error.home"))), status=status)
    except Exception:
        resp = Response("Error {}".format(status), status=status, content_type="text/plain; charset=utf-8")
    return resp


def application(environ, start_response):
    req = Request(environ)
    ok = False
    try:
        try:
            if req.path.startswith("/static/"):
                resp = serve_static(req, req.path[len("/static/"):])
            else:
                handler, req.params = router.match(req.method, req.path)
                if req.method == "POST":
                    req.check_csrf()
                resp = handler(req)
            ok = True
        except HTTPError as e:
            resp = error_response(req, e.status, e.message_key)
            ok = e.status < 500
        except Exception:
            log.exception("Unhandled error on %s %s", req.method, req.path)
            resp = error_response(req, 500)
    finally:
        req.finish_db(ok)

    # Cookies set during the request
    if not req.path.startswith("/static/"):
        req.csrf  # makes sure every visitor has a CSRF cookie before any form is sent
    if req.new_csrf:
        resp.set_cookie("gl_csrf", req.new_csrf, max_age=365 * 86400)
    for name, value, max_age, http_only in req.extra_cookies:
        resp.set_cookie(name, value, max_age=max_age, http_only=http_only)
    if req.cookies.get("gl_flash") and not any(n == "gl_flash" for n, _, _, _ in req.extra_cookies):
        resp.delete_cookie("gl_flash")

    # Headers for every answer
    resp.headers += [
        ("X-Robots-Tag", "noindex, nofollow"),
        ("X-Content-Type-Options", "nosniff"),
        ("X-Frame-Options", "DENY"),
        ("Referrer-Policy", "same-origin"),
        ("Content-Security-Policy", CSP_SIMULATOR if req.path == "/simulateur" else (
            CSP_DEFAULT.replace("img-src 'self' data:", "img-src 'self' data: https://upload.wikimedia.org")
            if req.path == "/dev/photos" else CSP_DEFAULT)),
    ]
    if config.COOKIE_SECURE:
        resp.headers.append(("Strict-Transport-Security", "max-age=31536000"))
    if not req.path.startswith("/static/") and not any(k.lower() == "cache-control" for k, _ in resp.headers):
        resp.headers.append(("Cache-Control", "private, no-store"))

    status_line = "{} {}".format(resp.status, http.client.responses.get(resp.status, ""))
    headers = list(resp.headers) + [("Set-Cookie", c) for c in resp.cookies]
    file_path = getattr(resp, "file_path", None)
    if req.method == "HEAD":
        start_response(status_line, headers)
        return [b""]
    if file_path:
        start_response(status_line, headers)
        return _stream(file_path)
    body = resp.body
    headers.append(("Content-Length", str(len(body))))
    start_response(status_line, headers)
    return [body]


def _stream(path, chunk=256 * 1024):
    with open(path, "rb") as f:
        while True:
            data = f.read(chunk)
            if not data:
                break
            yield data


def init():
    db.init()
