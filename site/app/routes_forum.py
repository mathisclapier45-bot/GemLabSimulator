"""Forum, admin and simulator pages. Reserved for logged-in members."""
import mimetypes
import os

from . import config, forum, markup
from .forum import Denied
from .views import (csrf_field, esc, fmt_date, honeypot, message_page, page, page_number, pagination)
from .web import HTTPError, Response, Router, login_url, redirect

router = Router()


def members_only(req):
    """Returns a redirect to the login page for visitors who are not logged in, else None."""
    if not req.user:
        path = req.path + ("?" + req.environ["QUERY_STRING"] if req.environ.get("QUERY_STRING") else "")
        return redirect(login_url(path))
    return None


def _post_form(req, action, body="", button="", error=None, with_title=False, title=""):
    t = req.t
    err = '<div class="banner err" role="alert">{}</div>'.format(esc(t(error))) if error else ""
    title_field = ""
    if with_title:
        title_field = ('<label class="field">{}<input type="text" name="title" value="{}" required '
                       'minlength="{}" maxlength="{}"></label>').format(
            esc(t("forum.topic.title")), esc(title), config.TITLE_MIN, config.TITLE_MAX)
    return (
        '{err}<form method="post" action="{action}" class="form">{csrf}{hp}{title}'
        '<label class="field">{label}<textarea name="body" rows="8" required maxlength="{mx}">{body}</textarea></label>'
        '<p class="hint">{help}</p><button type="submit" class="btn">{btn}</button></form>').format(
        err=err, action=esc(action), csrf=csrf_field(req), hp=honeypot(), title=title_field,
        label=esc(t("forum.message")), mx=config.POST_MAX, body=esc(body), help=esc(t("forum.format.help")),
        btn=esc(button))


@router.route("GET", "/forum")
def index(req):
    r = members_only(req)
    if r:
        return r
    t = req.t
    rows = []
    for slug, stats in forum.categories_overview(req.db):
        last = fmt_date(req, stats["last"]) if stats and stats["last"] else t("forum.no_activity")
        n = stats["topics"] if stats else 0
        rows.append('<li><a class="tile wide" href="/forum/{slug}"><span class="name">{name}</span>'
                    '<span class="sub">{desc}</span><span class="meta">{topics} · {last}</span></a></li>'.format(
                        slug=slug, name=esc(t("forum.cat." + slug)), desc=esc(t("forum.cat." + slug + ".desc")),
                        topics=esc(t("forum.topics", n=n)), last=esc(last)))
    content = '<h1>{}</h1><p class="lead">{}</p><ul class="tiles">{}</ul><p class="small">{}</p>'.format(
        esc(t("forum.title")), esc(t("forum.lead")), "".join(rows), esc(t("forum.rules")))
    return page(req, t("forum.title"), content, section="forum")


@router.route("GET", "/forum/<cat>")
def category(req):
    r = members_only(req)
    if r:
        return r
    cat = req.params["cat"]
    if cat not in config.FORUM_CATEGORIES:
        raise HTTPError(404)
    t = req.t
    total = req.db.execute("SELECT COUNT(*) AS n FROM topics WHERE category = ? AND deleted = 0", (cat,)).fetchone()["n"]
    pages = max(1, -(-total // config.PAGE_SIZE))
    n = page_number(req, pages)
    topics, _ = forum.list_topics(req.db, cat, n)
    rows = []
    for tp in topics:
        marks = ""
        if tp["pinned"]:
            marks += '<span class="badge">{}</span> '.format(esc(t("forum.pinned")))
        if tp["locked"]:
            marks += '<span class="badge grey">{}</span> '.format(esc(t("forum.locked")))
        rows.append(
            '<li class="row"><a class="title" href="/forum/sujet/{id}">{marks}{title}</a>'
            '<span class="meta">{by} {user} · {replies} · {date}</span></li>'.format(
                id=tp["id"], marks=marks, title=esc(tp["title"]), by=esc(t("forum.by")), user=esc(tp["username"]),
                replies=esc(t("forum.replies", n=tp["post_count"] - 1)), date=esc(fmt_date(req, tp["last_post_at"]))))
    body = '<ul class="rows">{}</ul>'.format("".join(rows)) if rows else '<p class="empty">{}</p>'.format(esc(t("forum.empty")))
    content = (
        '<nav class="crumbs"><a href="/forum">{back}</a></nav><h1>{name}</h1><p class="lead">{desc}</p>'
        '<p><a class="btn" href="/forum/{cat}/nouveau">{new}</a></p>{body}{pager}').format(
        back=esc(t("forum.title")), name=esc(t("forum.cat." + cat)), desc=esc(t("forum.cat." + cat + ".desc")),
        cat=cat, new=esc(t("forum.new_topic")), body=body, pager=pagination(req, "/forum/" + cat, n, pages))
    return page(req, t("forum.cat." + cat), content, section="forum")


@router.route("GET", "/forum/<cat>/nouveau")
def new_topic_form(req):
    r = members_only(req)
    if r:
        return r
    cat = req.params["cat"]
    if cat not in config.FORUM_CATEGORIES:
        raise HTTPError(404)
    t = req.t
    content = '<nav class="crumbs"><a href="/forum/{cat}">{back}</a></nav><h1>{h1}</h1>{form}'.format(
        cat=cat, back=esc(t("forum.cat." + cat)), h1=esc(t("forum.new_topic")),
        form=_post_form(req, "/forum/{}/nouveau".format(cat), button=t("forum.publish"), with_title=True))
    return page(req, t("forum.new_topic"), content, section="forum")


@router.route("POST", "/forum/<cat>/nouveau")
def new_topic(req):
    if not req.user:
        return redirect(login_url("/forum"))
    f = req.form
    cat = req.params["cat"]
    if f.get("website"):
        raise HTTPError(400)
    try:
        topic_id = forum.create_topic(req.db, req.user, cat, f.get("title", ""), f.get("body", ""))
    except Denied as d:
        if d.status == 404:
            raise HTTPError(404)
        t = req.t
        content = '<nav class="crumbs"><a href="/forum/{cat}">{back}</a></nav><h1>{h1}</h1>{form}'.format(
            cat=esc(cat), back=esc(t("forum.cat." + cat)), h1=esc(t("forum.new_topic")),
            form=_post_form(req, "/forum/{}/nouveau".format(cat), f.get("body", ""), t("forum.publish"), d.key,
                            True, f.get("title", "")))
        return page(req, t("forum.new_topic"), content, section="forum", status=d.status)
    return redirect("/forum/sujet/{}".format(topic_id))


def _admin_buttons(req, tp):
    t = req.t
    def btn(path, label, danger=False):
        return ('<form method="post" action="{}" class="inline">{}<button type="submit" class="btn small{}">{}</button></form>'
                ).format(path, csrf_field(req), " danger" if danger else "", esc(label))
    base = "/forum/sujet/{}".format(tp["id"])
    return '<div class="modbar"><strong>{}</strong> {} {} {}</div>'.format(
        esc(t("forum.mod")),
        btn(base + "/epingler", t("forum.unpin") if tp["pinned"] else t("forum.pin")),
        btn(base + "/verrouiller", t("forum.unlock") if tp["locked"] else t("forum.lock")),
        btn(base + "/supprimer", t("forum.delete_topic"), True))


@router.route("GET", "/forum/sujet/<int:topic_id>")
def topic_page(req, error=None, draft="", status=200):
    r = members_only(req)
    if r:
        return r
    t = req.t
    tp = forum.get_topic(req.db, req.params["topic_id"])
    if not tp:
        raise HTTPError(404)
    pages = max(1, -(-tp["post_count"] // config.PAGE_SIZE))
    n = page_number(req, pages)
    posts, _ = forum.list_posts(req.db, tp["id"], n)
    admin = forum.is_admin(req.user)
    items = []
    for p in posts:
        if p["deleted"]:
            body = '<p class="deleted">{}</p>'.format(esc(t("forum.post_deleted")))
            actions = ""
        else:
            body = markup.forum_html(p["body"])
            actions = '<a class="small" href="/forum/message/{}/signaler">{}</a>'.format(p["id"], esc(t("forum.report")))
            if admin:
                actions += (' <form method="post" action="/forum/message/{}/supprimer" class="inline">{}'
                            '<button type="submit" class="linklike danger">{}</button></form>').format(
                    p["id"], csrf_field(req), esc(t("forum.delete_post")))
        role = ' <span class="badge">{}</span>'.format(esc(t("forum.admin"))) if p["role"] == "admin" else ""
        items.append(
            '<article class="post" id="m{id}"><header><strong>{user}</strong>{role} <span class="meta">{date}</span></header>'
            '<div class="content">{body}</div><footer>{actions}</footer></article>'.format(
                id=p["id"], user=esc(p["username"]), role=role, date=esc(fmt_date(req, p["created_at"], True)),
                body=body, actions=actions))
    if tp["locked"] and not admin:
        reply = '<p class="banner warn">{}</p>'.format(esc(t("forum.locked_note")))
    else:
        reply = '<h2>{}</h2>{}'.format(
            esc(t("forum.reply")),
            _post_form(req, "/forum/sujet/{}/repondre".format(tp["id"]), draft, t("forum.publish"), error))
    flags = ""
    if tp["pinned"]:
        flags += '<span class="badge">{}</span> '.format(esc(t("forum.pinned")))
    if tp["locked"]:
        flags += '<span class="badge grey">{}</span> '.format(esc(t("forum.locked")))
    content = (
        '<nav class="crumbs"><a href="/forum">{f}</a> › <a href="/forum/{cat}">{c}</a></nav>'
        '<h1>{flags}{title}</h1>{mod}{items}{pager}{reply}').format(
        f=esc(t("forum.title")), cat=tp["category"], c=esc(t("forum.cat." + tp["category"])), flags=flags,
        title=esc(tp["title"]), mod=_admin_buttons(req, tp) if admin else "", items="".join(items),
        pager=pagination(req, "/forum/sujet/{}".format(tp["id"]), n, pages), reply=reply)
    return page(req, tp["title"], content, section="forum", status=status)


@router.route("POST", "/forum/sujet/<int:topic_id>/repondre")
def reply(req):
    if not req.user:
        return redirect(login_url("/forum"))
    f = req.form
    if f.get("website"):
        raise HTTPError(400)
    tid = req.params["topic_id"]
    try:
        post_id = forum.reply(req.db, req.user, tid, f.get("body", ""))
    except Denied as d:
        if d.status == 404:
            raise HTTPError(404)
        req.query = {}
        return topic_page(req, error=d.key, draft=f.get("body", ""), status=d.status)
    # Jump to the last page, on the new message.
    total = req.db.execute("SELECT post_count FROM topics WHERE id = ?", (tid,)).fetchone()["post_count"]
    last_page = max(1, -(-total // config.PAGE_SIZE))
    return redirect("/forum/sujet/{}?page={}#m{}".format(tid, last_page, post_id))


@router.route("GET", "/forum/message/<int:post_id>/signaler")
def report_form(req):
    r = members_only(req)
    if r:
        return r
    t = req.t
    content = (
        '<section class="narrow"><h1>{h1}</h1><p>{lead}</p><form method="post" action="/forum/message/{id}/signaler" class="form">'
        '{csrf}<label class="field">{label}<textarea name="reason" rows="4" required maxlength="300"></textarea></label>'
        '<button type="submit" class="btn">{go}</button></form></section>').format(
        h1=esc(t("report.title")), lead=esc(t("report.lead")), id=req.params["post_id"], csrf=csrf_field(req),
        label=esc(t("report.reason")), go=esc(t("report.submit")))
    return page(req, t("report.title"), content, section="forum")


@router.route("POST", "/forum/message/<int:post_id>/signaler")
def report(req):
    if not req.user:
        return redirect(login_url("/forum"))
    try:
        forum.report_post(req.db, req.user, req.params["post_id"], req.form.get("reason", ""))
    except Denied as d:
        raise HTTPError(d.status, d.key)
    return message_page(req, req.t("report.done.title"), req.t("report.done.text"), link=("/forum", req.t("forum.title")))


def _moderate(req, fn):
    try:
        fn()
    except Denied as d:
        raise HTTPError(d.status, d.key)


@router.route("POST", "/forum/sujet/<int:topic_id>/epingler")
def pin(req):
    tp = forum.get_topic(req.db, req.params["topic_id"])
    if not tp:
        raise HTTPError(404)
    _moderate(req, lambda: forum.set_flag(req.db, req.user, tp["id"], "pinned", not tp["pinned"]))
    return redirect("/forum/sujet/{}".format(tp["id"]))


@router.route("POST", "/forum/sujet/<int:topic_id>/verrouiller")
def lock(req):
    tp = forum.get_topic(req.db, req.params["topic_id"])
    if not tp:
        raise HTTPError(404)
    _moderate(req, lambda: forum.set_flag(req.db, req.user, tp["id"], "locked", not tp["locked"]))
    return redirect("/forum/sujet/{}".format(tp["id"]))


@router.route("POST", "/forum/sujet/<int:topic_id>/supprimer")
def delete_topic(req):
    tp = forum.get_topic(req.db, req.params["topic_id"])
    if not tp:
        raise HTTPError(404)
    _moderate(req, lambda: forum.delete_topic(req.db, req.user, tp["id"]))
    return redirect("/forum/{}".format(tp["category"]))


@router.route("POST", "/forum/message/<int:post_id>/supprimer")
def delete_post(req):
    result = []
    _moderate(req, lambda: result.append(forum.delete_post(req.db, req.user, req.params["post_id"])))
    tp = forum.get_topic(req.db, result[0])
    return redirect("/forum/sujet/{}".format(tp["id"]) if tp else "/forum")


# ------------------------------------------------------------------ admin

@router.route("GET", "/admin")
def admin(req):
    r = members_only(req)
    if r:
        return r
    if not forum.is_admin(req.user):
        raise HTTPError(403, "err.admin_only")
    t = req.t
    rows = []
    for rep in forum.open_reports(req.db):
        ban = ""
        if not rep["banned_at"]:
            ban = ('<form method="post" action="/admin/membre/{}/bannir" class="inline">{}'
                   '<button type="submit" class="btn small danger">{}</button></form>').format(
                rep["author_id"], csrf_field(req), esc(t("admin.ban")))
        rows.append(
            '<article class="post"><header><strong>{author}</strong> <span class="meta">{topic} · {date}</span></header>'
            '<div class="content">{body}</div><p class="small"><strong>{rl}</strong> {reason} ({reporter})</p>'
            '<footer><a class="small" href="/forum/sujet/{tid}#m{pid}">{see}</a> '
            '<form method="post" action="/forum/message/{pid}/supprimer" class="inline">{csrf}'
            '<button type="submit" class="btn small danger">{dp}</button></form> '
            '<form method="post" action="/admin/signalement/{rid}/ignorer" class="inline">{csrf}'
            '<button type="submit" class="btn small">{dis}</button></form> {ban}</footer></article>'.format(
                author=esc(rep["author"]), topic=esc(rep["title"]), date=esc(fmt_date(req, rep["created_at"], True)),
                body=markup.forum_html(rep["body"]), rl=esc(t("admin.reason")), reason=esc(rep["reason"]),
                reporter=esc(rep["reporter"]), tid=rep["topic_id"], pid=rep["post_id"], see=esc(t("admin.see")),
                csrf=csrf_field(req), dp=esc(t("forum.delete_post")), rid=rep["id"], dis=esc(t("admin.dismiss")),
                ban=ban))
    members = req.db.execute("SELECT COUNT(*) AS n FROM users").fetchone()["n"]
    content = '<h1>{h1}</h1><p>{members}</p><h2>{h2}</h2>{body}'.format(
        h1=esc(t("admin.title")), members=esc(t("admin.members", n=members)), h2=esc(t("admin.reports")),
        body="".join(rows) or '<p class="empty">{}</p>'.format(esc(t("admin.no_reports"))))
    return page(req, t("admin.title"), content)


@router.route("POST", "/admin/signalement/<int:report_id>/ignorer")
def dismiss(req):
    _moderate(req, lambda: forum.dismiss_report(req.db, req.user, req.params["report_id"]))
    return redirect("/admin")


@router.route("POST", "/admin/membre/<int:user_id>/bannir")
def ban(req):
    _moderate(req, lambda: forum.ban_user(req.db, req.user, req.params["user_id"], True))
    return redirect("/admin")


# ------------------------------------------------------------------ simulator (members only)

@router.route("GET", "/simulateur")
def simulator(req):
    r = members_only(req)
    if r:
        return r
    t = req.t
    ready = all((config.SIM_DIR / name).exists() for name in config.SIM_FILES)
    if not ready:
        return message_page(req, t("sim.title"), t("sim.missing"), status=503)
    content = (
        '<h1>{h1}</h1><p class="lead">{lead}</p>'
        '<div class="xp-window" id="xp"><div class="xp-title"><span class="xp-icon"></span>'
        '<span class="xp-name">{name}</span><span class="xp-ctl"><button type="button" id="sim-full" class="xp-btn" '
        'title="{full}">&#9633;</button></span></div>'
        '<div class="xp-body"><div class="xp-stage"><canvas id="unity-canvas" tabindex="-1"></canvas>'
        '<div id="sim-loading" class="xp-loading"><p>{loading}</p><div class="xp-bar"><div id="sim-bar"></div></div></div>'
        '<p id="sim-error" class="xp-error" hidden>{error}</p></div></div>'
        '<div class="xp-status"><span>{hint}</span></div></div>'
        '<script src="/static/js/simulateur.js" defer></script>').format(
        h1=esc(t("sim.title")), lead=esc(t("sim.lead")), name=esc(t("sim.window")), full=esc(t("sim.fullscreen")),
        loading=esc(t("sim.loading")), error=esc(t("sim.error")), hint=esc(t("sim.hint")))
    resp = page(req, t("sim.title"), content, section="simulator", sim=True)
    return resp


@router.route("GET", "/simulateur/fichiers/<name>")
def simulator_file(req):
    # The files are not public: nothing is served without a valid login.
    if not req.user:
        raise HTTPError(403, "err.login_required")
    name = req.params["name"]
    ctype = config.SIM_FILES.get(name)
    if not ctype:
        raise HTTPError(404)
    path = config.SIM_DIR / name
    if not path.is_file():
        raise HTTPError(404)
    st = path.stat()
    etag = '"{:x}-{:x}"'.format(int(st.st_mtime), st.st_size)
    headers = [("ETag", etag), ("Cache-Control", "private, no-cache"), ("Content-Length", str(st.st_size))]
    if name.endswith(".gz"):
        headers.append(("Content-Encoding", "gzip"))
    if req.environ.get("HTTP_IF_NONE_MATCH") == etag:
        return Response(b"", status=304, headers=[("ETag", etag), ("Cache-Control", "private, no-cache")])
    resp = Response(b"", content_type=ctype, headers=headers)
    resp.file_path = str(path)
    return resp
