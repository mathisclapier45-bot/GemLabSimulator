"""Legal pages (legal notice, privacy policy), contact and about pages. The texts are in i18n/fr.json; the facts about the publisher
come from the GEMLAB_* environment variables (see config.LEGAL_FIELDS) and show as "to complete" until they are set."""
from . import config
from .views import esc, page
from .web import Router

router = Router()

LEGAL_UPDATED = (2026, 11, 19)   # date of the last change of the legal texts: change it when you edit them


def _fields(req):
    """field -> HTML: the value given in the environment, or a visible 'to complete' mark."""
    out = {}
    for field in config.LEGAL_FIELDS:
        value = config.legal(field)
        out[field] = esc(value) if value else '<mark class="todo">[{}]</mark>'.format(esc(req.t("legal.todo." + field)))
    return out


def _text(req, key, fields):
    """A text of the i18n file with {name}-style holes filled by `fields` (already HTML), paragraphs split on blank lines."""
    holes = {k: "@@{}@@".format(k) for k in fields}
    holes["site"] = esc(req.t("site.name"))
    raw = esc(req.t(key, **holes))
    for k, html in fields.items():
        raw = raw.replace("@@{}@@".format(k), html)
    parts = [p.strip() for p in raw.split("\n\n") if p.strip()]
    return "".join("<p>{}</p>".format(p.replace("\n", "<br>")) for p in parts)


def _sections(req, prefix, count, fields):
    out = []
    for i in range(1, count + 1):
        out.append("<section><h2>{}</h2>{}</section>".format(esc(req.t("{}.s{}.h".format(prefix, i))), _text(req, "{}.s{}.p".format(prefix, i), fields)))
    return "".join(out)


def _updated(req):
    y, m, d = LEGAL_UPDATED
    months = req.t("date.months").split(",")
    return '<p class="small">{}</p>'.format(esc(req.t("legal.updated", date="{} {} {}".format(d, months[m - 1], y))))


def _legal_page(req, prefix, count):
    fields = _fields(req)
    missing = any(not config.legal(f) for f in config.LEGAL_FIELDS)
    warn = '<div class="banner warn">{}</div>'.format(esc(req.t("legal.todo.banner"))) if missing and config.DEV else ""
    content = '<article class="legal"><h1>{}</h1>{}{}{}{}</article>'.format(
        esc(req.t(prefix + ".title")), warn, _text(req, prefix + ".intro", fields), _sections(req, prefix, count, fields), _updated(req))
    return page(req, req.t(prefix + ".title"), content, description=req.t(prefix + ".desc"))


@router.route("GET", "/mentions-legales")
def legal_notice(req):
    return _legal_page(req, "legal", 7)


@router.route("GET", "/confidentialite")
def privacy(req):
    return _legal_page(req, "privacy", 9)


@router.route("GET", "/contact")
def contact(req):
    t = req.t
    email = config.legal("email")
    mail = ('<p><a class="btn" href="mailto:{e}">{e}</a></p>'.format(e=esc(email)) if email
            else '<p><mark class="todo">[{}]</mark></p>'.format(esc(t("legal.todo.email"))))
    content = '<article class="legal"><h1>{h1}</h1><p class="lead">{lead}</p>{mail}{blocks}</article>'.format(
        h1=esc(t("contact.title")), lead=esc(t("contact.lead")), mail=mail,
        blocks="".join("<section><h2>{}</h2><p>{}</p></section>".format(esc(t("contact.s{}.h".format(i))), esc(t("contact.s{}.p".format(i)))) for i in (1, 2, 3)))
    return page(req, t("contact.title"), content, description=t("contact.desc"))


@router.route("GET", "/a-propos")
def about(req):
    t = req.t
    from . import catalog
    d = catalog.data()
    fields = {"species": str(len(d["species"])), "lessons": str(len(catalog.lessons()))}
    blocks = "".join("<section><h2>{}</h2>{}</section>".format(esc(t("about.s{}.h".format(i))), _text(req, "about.s{}.p".format(i), fields)) for i in (1, 2, 3, 4))
    content = '<article class="legal"><h1>{}</h1><p class="lead">{}</p>{}</article>'.format(esc(t("about.title")), esc(t("about.lead")), blocks)
    return page(req, t("about.title"), content, description=t("about.desc"))
