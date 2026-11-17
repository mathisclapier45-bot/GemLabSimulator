"""Accounts: sign up, log in, e-mail check, forgotten password."""
import re

from . import config, mailer, security
from .db import now
from .views import csrf_field, esc, honeypot, message_page, page, safe_next
from .web import HTTPError, Response, Router, redirect

router = Router()

EMAIL_RE = re.compile(r"^[^@\s]{1,64}@[^@\s]{1,255}\.[^@\s.]{2,}$")


def _field(label, name, typ="text", value="", extra="", hint=""):
    return ('<label class="field">{label}<input type="{typ}" name="{name}" value="{value}" {extra}></label>{hint}'
            ).format(label=esc(label), typ=typ, name=name, value=esc(value), extra=extra,
                     hint='<p class="hint">{}</p>'.format(esc(hint)) if hint else "")


def _errors(req, keys):
    if not keys:
        return ""
    return '<div class="banner err" role="alert"><ul>{}</ul></div>'.format(
        "".join("<li>{}</li>".format(esc(req.t(k))) for k in keys))


def _start_session(req, user_id, resp):
    raw = security.create_session(req.db, user_id)
    resp.set_cookie("gl_session", raw, max_age=config.SESSION_DAYS * 86400)


def _send_verification(req, user):
    token = security.create_token(req.db, user["id"], "verify", config.VERIFY_TOKEN_HOURS)
    link = "{}/verification?token={}".format(req.base_url, token)
    mailer.send(req.db, user["email"], req.t("mail.verify.subject"),
                req.t("mail.verify.body", name=user["username"], link=link, hours=config.VERIFY_TOKEN_HOURS))


def password_errors(pw):
    errs = []
    if len(pw) < config.PASSWORD_MIN:
        errs.append("err.password_short")
    if len(pw) > config.PASSWORD_MAX:
        errs.append("err.password_long")
    return errs


# ------------------------------------------------------------------ sign up

def _register_form(req, values=None, errors=None):
    v = values or {}
    t = req.t
    content = (
        '<section class="narrow"><h1>{h1}</h1>{errors}'
        '<form method="post" action="/inscription" class="form">{csrf}{hp}'
        '{username}{email}{pw}{pw2}<button type="submit" class="btn">{go}</button></form>'
        '<p>{have} <a href="/connexion">{login}</a></p></section>').format(
        h1=esc(t("reg.title")), errors=_errors(req, errors), csrf=csrf_field(req), hp=honeypot(),
        username=_field(t("reg.username"), "username", value=v.get("username", ""),
                        extra='required minlength="3" maxlength="24" autocomplete="username"', hint=t("reg.username.hint")),
        email=_field(t("reg.email"), "email", "email", v.get("email", ""), 'required autocomplete="email"'),
        pw=_field(t("reg.password"), "password", "password", "",
                  'required minlength="{}" autocomplete="new-password"'.format(config.PASSWORD_MIN),
                  t("reg.password.hint", n=config.PASSWORD_MIN)),
        pw2=_field(t("reg.password2"), "password2", "password", "", 'required autocomplete="new-password"'),
        go=esc(t("reg.submit")), have=esc(t("reg.have")), login=esc(t("nav.login")))
    return page(req, t("reg.title"), content, status=400 if errors else 200)


@router.route("GET", "/inscription")
def register_form(req):
    if req.user:
        return redirect("/compte")
    return _register_form(req)


@router.route("POST", "/inscription")
def register(req):
    f = req.form
    if f.get("website"):               # honeypot filled: a bot
        raise HTTPError(400)
    if security.over_limit(req.db, "register_ip", req.ip):
        raise HTTPError(429)
    username = f.get("username", "").strip()
    email = f.get("email", "").strip().lower()
    pw, pw2 = f.get("password", ""), f.get("password2", "")
    errs = []
    if not re.fullmatch(config.USERNAME_RE, username):
        errs.append("err.username")
    if not EMAIL_RE.match(email) or len(email) > 254:
        errs.append("err.email")
    errs += password_errors(pw)
    if pw != pw2:
        errs.append("err.password_mismatch")
    if errs:
        return _register_form(req, {"username": username, "email": email}, errs)

    existing = req.db.execute("SELECT * FROM users WHERE email = ?", (email,)).fetchone()
    if not existing and req.db.execute("SELECT 1 FROM users WHERE username = ?", (username,)).fetchone():
        return _register_form(req, {"username": username, "email": email}, ["err.username_taken"])
    security.record(req.db, "register_ip", req.ip)   # only real attempts count, not typos
    if existing:
        # Same answer as a real sign-up (same work, same text), so nobody can find out which e-mails have an account.
        security.hash_password(pw)
        link = req.base_url + "/mot-de-passe-oublie"
        mailer.send(req.db, email, req.t("mail.exists.subject"), req.t("mail.exists.body", link=link))
    else:
        cur = req.db.execute(
            "INSERT INTO users (email, username, pw_hash, created_at) VALUES (?,?,?,?)",
            (email, username, security.hash_password(pw), now()))
        user = req.db.execute("SELECT * FROM users WHERE id = ?", (cur.lastrowid,)).fetchone()
        _send_verification(req, user)
    return message_page(req, req.t("reg.done.title"), req.t("reg.done.text", email=email))


# ------------------------------------------------------------------ log in / out

def _login_form(req, next_path, email="", errors=None, status=200):
    t = req.t
    content = (
        '<section class="narrow"><h1>{h1}</h1>{errors}'
        '<form method="post" action="/connexion" class="form">{csrf}'
        '<input type="hidden" name="suite" value="{nxt}">{email}{pw}'
        '<button type="submit" class="btn">{go}</button></form>'
        '<p><a href="/mot-de-passe-oublie">{forgot}</a></p>'
        '<p>{no} <a href="/inscription">{reg}</a></p></section>').format(
        h1=esc(t("login.title")), errors=_errors(req, errors), csrf=csrf_field(req), nxt=esc(next_path),
        email=_field(t("reg.email"), "email", "email", email, 'required autocomplete="email"'),
        pw=_field(t("reg.password"), "password", "password", "", 'required autocomplete="current-password"'),
        go=esc(t("login.submit")), forgot=esc(t("login.forgot")), no=esc(t("login.no_account")),
        reg=esc(t("nav.register")))
    return page(req, t("login.title"), content, status=status)


@router.route("GET", "/connexion")
def login_form(req):
    nxt = safe_next(req.query.get("suite", "/"))
    if req.user:
        return redirect(nxt)
    return _login_form(req, nxt)


@router.route("POST", "/connexion")
def login(req):
    f = req.form
    nxt = safe_next(f.get("suite", "/"))
    email = f.get("email", "").strip().lower()[:254]
    if security.over_limit(req.db, "login_ip", req.ip) or security.over_limit(req.db, "login_email", email):
        raise HTTPError(429)
    user = req.db.execute("SELECT * FROM users WHERE email = ?", (email,)).fetchone()
    ok = False
    if user:
        ok = security.verify_password(f.get("password", ""), user["pw_hash"]) and not user["banned_at"]
    else:
        security.dummy_verify(f.get("password", ""))
    if not ok:
        security.record(req.db, "login_ip", req.ip)
        security.record(req.db, "login_email", email)
        req.db.commit()  # keep the failure counters even though the answer is an error
        return _login_form(req, nxt, email, ["err.login"], status=400)
    resp = redirect(nxt)
    _start_session(req, user["id"], resp)
    return resp


@router.route("POST", "/deconnexion")
def logout(req):
    security.delete_session(req.db, req.cookies.get("gl_session"))
    resp = redirect("/")
    resp.delete_cookie("gl_session")
    req.flash("flash.logged_out")
    return resp


# ------------------------------------------------------------------ e-mail check

@router.route("GET", "/verification")
def verify(req):
    user_id = security.consume_token(req.db, req.query.get("token", ""), "verify")
    if user_id is None:
        return message_page(req, req.t("verify.bad.title"), req.t("verify.bad.text"), status=400,
                            link=("/compte", req.t("nav.account")))
    req.db.execute("UPDATE users SET email_verified_at = ? WHERE id = ? AND email_verified_at IS NULL",
                   (now(), user_id))
    req.flash("flash.verified")
    return redirect("/compte" if req.user else "/connexion")


@router.route("POST", "/verification/renvoyer")
def resend_verification(req):
    user = req.user
    if not user:
        return redirect("/connexion")
    if user["email_verified_at"]:
        return redirect("/compte")
    if not security.hit(req.db, "mail_email", user["email"]):
        raise HTTPError(429)
    _send_verification(req, user)
    req.flash("flash.verify_sent")
    return redirect("/compte")


# ------------------------------------------------------------------ forgotten password

@router.route("GET", "/mot-de-passe-oublie")
def forgot_form(req):
    t = req.t
    content = (
        '<section class="narrow"><h1>{h1}</h1><p>{lead}</p>'
        '<form method="post" action="/mot-de-passe-oublie" class="form">{csrf}{email}'
        '<button type="submit" class="btn">{go}</button></form></section>').format(
        h1=esc(t("forgot.title")), lead=esc(t("forgot.lead")), csrf=csrf_field(req),
        email=_field(t("reg.email"), "email", "email", "", 'required autocomplete="email"'), go=esc(t("forgot.submit")))
    return page(req, t("forgot.title"), content)


@router.route("POST", "/mot-de-passe-oublie")
def forgot(req):
    email = req.form.get("email", "").strip().lower()[:254]
    if not security.hit(req.db, "mail_ip", req.ip):
        raise HTTPError(429)
    user = req.db.execute("SELECT * FROM users WHERE email = ? AND banned_at IS NULL", (email,)).fetchone()
    if user and security.hit(req.db, "mail_email", email):
        token = security.create_token(req.db, user["id"], "reset", config.RESET_TOKEN_HOURS)
        link = "{}/reinitialisation?token={}".format(req.base_url, token)
        mailer.send(req.db, email, req.t("mail.reset.subject"),
                    req.t("mail.reset.body", name=user["username"], link=link, hours=config.RESET_TOKEN_HOURS))
    # Always the same answer, whether or not the e-mail has an account.
    return message_page(req, req.t("forgot.done.title"), req.t("forgot.done.text"))


def _reset_form(req, token, errors=None, status=200):
    t = req.t
    content = (
        '<section class="narrow"><h1>{h1}</h1>{errors}'
        '<form method="post" action="/reinitialisation" class="form">{csrf}'
        '<input type="hidden" name="token" value="{token}">{pw}{pw2}'
        '<button type="submit" class="btn">{go}</button></form></section>').format(
        h1=esc(t("reset.title")), errors=_errors(req, errors), csrf=csrf_field(req), token=esc(token),
        pw=_field(t("reset.password"), "password", "password", "",
                  'required minlength="{}" autocomplete="new-password"'.format(config.PASSWORD_MIN),
                  t("reg.password.hint", n=config.PASSWORD_MIN)),
        pw2=_field(t("reg.password2"), "password2", "password", "", 'required autocomplete="new-password"'),
        go=esc(t("reset.submit")))
    return page(req, t("reset.title"), content, status=status)


@router.route("GET", "/reinitialisation")
def reset_form(req):
    token = req.query.get("token", "")
    if security.peek_token(req.db, token, "reset") is None:
        return message_page(req, req.t("reset.bad.title"), req.t("reset.bad.text"), status=400,
                            link=("/mot-de-passe-oublie", req.t("forgot.title")))
    return _reset_form(req, token)


@router.route("POST", "/reinitialisation")
def reset(req):
    f = req.form
    token = f.get("token", "")
    pw, pw2 = f.get("password", ""), f.get("password2", "")
    if security.peek_token(req.db, token, "reset") is None:
        return message_page(req, req.t("reset.bad.title"), req.t("reset.bad.text"), status=400,
                            link=("/mot-de-passe-oublie", req.t("forgot.title")))
    errs = password_errors(pw)
    if pw != pw2:
        errs.append("err.password_mismatch")
    if errs:
        return _reset_form(req, token, errs, status=400)
    user_id = security.consume_token(req.db, token, "reset")
    req.db.execute("UPDATE users SET pw_hash = ? WHERE id = ?", (security.hash_password(pw), user_id))
    # Receiving the mail proves the address is real, and every old session is closed.
    req.db.execute("UPDATE users SET email_verified_at = COALESCE(email_verified_at, ?) WHERE id = ?", (now(), user_id))
    security.delete_user_sessions(req.db, user_id)
    req.flash("flash.password_changed")
    return redirect("/connexion")


# ------------------------------------------------------------------ my account

@router.route("GET", "/compte")
def account(req):
    user = req.user
    if not user:
        return redirect("/connexion?suite=/compte")
    t = req.t
    status = t("acc.verified") if user["email_verified_at"] else t("acc.unverified")
    content = (
        '<section class="narrow"><h1>{h1}</h1><table class="props">'
        '<tr><th scope="row">{lu}</th><td>{u}</td></tr><tr><th scope="row">{le}</th><td>{e} — {st}</td></tr></table>'
        '<h2>{hp}</h2><form method="post" action="/compte/mot-de-passe" class="form">{csrf}{old}{pw}{pw2}'
        '<button type="submit" class="btn">{go}</button></form></section>').format(
        h1=esc(t("acc.title")), lu=esc(t("reg.username")), u=esc(user["username"]), le=esc(t("reg.email")),
        e=esc(user["email"]), st=esc(status), hp=esc(t("acc.password")), csrf=csrf_field(req),
        old=_field(t("acc.password.current"), "current", "password", "", 'required autocomplete="current-password"'),
        pw=_field(t("reset.password"), "password", "password", "",
                  'required minlength="{}" autocomplete="new-password"'.format(config.PASSWORD_MIN)),
        pw2=_field(t("reg.password2"), "password2", "password", "", 'required autocomplete="new-password"'),
        go=esc(t("acc.password.submit")))
    return page(req, t("acc.title"), content)


@router.route("POST", "/compte/mot-de-passe")
def change_password(req):
    user = req.user
    if not user:
        return redirect("/connexion")
    f = req.form
    pw, pw2 = f.get("password", ""), f.get("password2", "")
    errs = password_errors(pw)
    if pw != pw2:
        errs.append("err.password_mismatch")
    if not security.verify_password(f.get("current", ""), user["pw_hash"]):
        errs.append("err.password_current")
    if errs:
        t = req.t
        return page(req, t("acc.title"),
                    '<section class="narrow">{}<p><a href="/compte">{}</a></p></section>'.format(
                        _errors(req, errs), esc(t("acc.back"))), status=400)
    req.db.execute("UPDATE users SET pw_hash = ? WHERE id = ?", (security.hash_password(pw), user["id"]))
    security.delete_user_sessions(req.db, user["id"])
    resp = redirect("/connexion")
    resp.delete_cookie("gl_session")
    req.flash("flash.password_changed")
    return resp
