"""Page layout and small HTML helpers shared by all pages. No visible text here: see i18n/fr.json."""
import html
import re
import time

from . import catalog, config, gem_icons
from .web import Response

esc = html.escape

# Second menu shown under the header on the pages of the encyclopedia: (path, label key).
ENCYCLOPEDIA_MENU = [
    ("/encyclopedie", "sub.species"),
    ("/encyclopedie/familles", "sub.families"),
    ("/encyclopedie/formes", "sub.habits"),
    ("/comparer", "sub.compare"),
    ("/glossaire", "sub.glossary"),
]

NAV = [
    ("home", "/", "nav.home"),
    ("encyclopedia", "/encyclopedie", "nav.encyclopedia"),
    ("identify", "/identification", "nav.identify"),
    ("lessons", "/lecons", "nav.lessons"),
    ("quiz", "/quiz", "nav.quiz"),
    ("prices", "/prix", "nav.prices"),
    ("simulator", "/simulateur", "nav.simulator"),
    ("forum", "/forum", "nav.forum"),
]


# Pages that are never shown to search engines, even when the site is open to them: accounts, forum, simulator, results.
PRIVATE_PREFIXES = ("/forum", "/compte", "/connexion", "/inscription", "/deconnexion", "/admin", "/simulateur", "/progression",
                    "/mot-de-passe-oublie", "/reinitialisation", "/verification", "/recherche", "/quiz/jouer", "/prix/ajouter", "/dev")


def is_private(path):
    return path.startswith(PRIVATE_PREFIXES) or (path.startswith("/lecons/") and path.endswith("/quiz"))


def robots_value(path):
    """The robots directive of a page: closed while the site is private, open for public pages once INDEXABLE is on."""
    if config.INDEXABLE and not is_private(path):
        return "index, follow"
    return "noindex, nofollow"


def csrf_field(req):
    return '<input type="hidden" name="csrf" value="{}">'.format(esc(req.csrf))


def honeypot():
    # Hidden field that humans never fill in; bots usually do.
    return ('<div class="hp" aria-hidden="true"><label>Website<input type="text" name="website" '
            'tabindex="-1" autocomplete="off"></label></div>')


def fmt_date(req, ts, with_time=False):
    months = req.t("date.months").split(",")
    g = time.gmtime(ts)
    text = "{} {} {}".format(g.tm_mday, months[g.tm_mon - 1], g.tm_year)
    if with_time:
        text += " " + req.t("date.at") + " {:02d}:{:02d}".format(g.tm_hour, g.tm_min)
    return text


def fmt_num(value, digits=3):
    if value is None:
        return ""
    text = ("{:." + str(digits) + "f}").format(value).rstrip("0").rstrip(".")
    return text.replace(".", ",") if catalog.current_lang() == "fr" else text   # decimal comma in French, point in English


def fmt_ri(species):
    """The refractive index of a species as "lowest - highest" (the readings of the refractometer: both rays of a uniaxial stone, the lowest and the highest of a biaxial one,
    and the variation between specimens); one value when both are the same."""
    low, high = species.get("ri_min"), species.get("ri_max")
    if low is None or high is None:
        return fmt_num(species.get("refractiveIndex"), 3)
    if high <= 0:
        return "—"                                      # an opaque mineral has no index to read

    def fixed(v):                                       # always three decimals, so that both ends line up (1,567 - 1,590)
        text = "{:.3f}".format(v)
        return text.replace(".", ",") if catalog.current_lang() == "fr" else text
    if abs(high - low) < 0.0005:
        return fixed(low)
    return "{} – {}".format(fixed(low), fixed(high))


def fmt_bir(species):
    """The birefringence of a species as "lowest - highest" (three decimals); empty when it has none (isotropic, opaque)."""
    low, high = species.get("bir_min"), species.get("bir_max")
    if low is None or high is None:
        low = high = species.get("birefringence") or 0.0
    if high <= 0:
        return ""

    def fixed(v):
        text = "{:.3f}".format(v)
        return text.replace(".", ",") if catalog.current_lang() == "fr" else text
    return fixed(low) if abs(high - low) < 0.0005 else "{} – {}".format(fixed(low), fixed(high))


def _span(low, high, fixed_digits=None, strip_digits=1):
    """"lowest - highest" with a decimal comma in French; one value when both are the same."""
    def one(v):
        text = ("{:." + str(fixed_digits) + "f}").format(v) if fixed_digits is not None else ("{:." + str(strip_digits) + "f}").format(v).rstrip("0").rstrip(".")
        return text.replace(".", ",") if catalog.current_lang() == "fr" else text
    return one(low) if abs(high - low) < 0.0005 else "{} – {}".format(one(low), one(high))


def fmt_density(species):
    """The density of a species as "lowest - highest" (two decimals)."""
    low, high = species.get("density_min"), species.get("density_max")
    if low is None or high is None:
        return fmt_num(species.get("density"), 2)
    return _span(low, high, fixed_digits=2)


def fmt_hardness(species):
    """The Mohs hardness of a species as "lowest - highest"."""
    low, high = species.get("hardness_min"), species.get("hardness_max")
    if low is None or high is None:
        return fmt_num(species.get("hardness"), 1)
    return _span(low, high, strip_digits=1)


def transparency_levels(french_text):
    """The levels a species can have (see catalog.transparency_levels)."""
    return catalog.transparency_levels(french_text)


def transparency_cell(species, label):
    """Round dots for the transparency of a species: white for transparent, gray for semi-transparent, black for opaque, several when the species can be several."""
    levels = transparency_levels(catalog.fr(species, "transparency"))
    dots = "".join('<span class="tdot t{}"></span>'.format(i) for i in levels)
    return '<span class="tdots" aria-hidden="true">{}</span> {}'.format(dots, html.escape(label or ""))


def gem_svg(color, facet, size=48, style=None):
    """A small gem drawn from two colours, in the shape and the cut of the species when `style` (shape, cut, habit) is given (see gem_icons.py)."""
    return gem_icons.draw(color, facet, size, style)


BRAND_MARK = ('<svg class="mark" viewBox="0 0 48 48" width="28" height="28" aria-hidden="true"><defs><linearGradient id="bm" x1="0" y1="0" x2="1" y2="1">'
              '<stop offset="0" stop-color="#6ea8ff"/><stop offset="1" stop-color="#7b5cff"/></linearGradient></defs>'
              '<polygon points="12,6 36,6 45,18 24,43 3,18" fill="url(#bm)"/><polygon points="12,6 24,18 3,18" fill="#fff" opacity=".45"/>'
              '<polygon points="36,6 24,18 45,18" fill="#fff" opacity=".2"/><polygon points="12,6 36,6 24,18" fill="#fff" opacity=".7"/></svg>')


def motion_button(t):
    """The switch that pauses the animations (behaviour in static/js/motion.js)."""
    return '<button type="button" class="motion-toggle linklike" data-pause="{p}" data-resume="{r}">{p}</button>'.format(
        p=esc(t("motion.pause")), r=esc(t("motion.resume")))


def _footer(req):
    t = req.t
    user = req.user
    acct = ('<a href="/compte">{}</a>'.format(esc(t("nav.account"))) if user else
            '<a href="/connexion">{}</a><a href="/inscription">{}</a>'.format(esc(t("nav.login")), esc(t("nav.register"))))
    return (
        '<footer class="foot"><div class="wrap foot-grid"><div class="foot-brand"><a class="brand" href="/">{mark}<span>{site}</span></a>'
        '<p>{tag}</p></div><nav aria-label="{e}"><h3>{e}</h3><a href="/encyclopedie">{enc}</a><a href="/identification">{idt}</a><a href="/comparer">{cmp}</a><a href="/glossaire">{gl}</a><a href="/lecons">{les}</a><a href="/quiz">{qz}</a><a href="/prix">{pr}</a></nav>'
        '<nav aria-label="{m}"><h3>{m}</h3><a href="/simulateur">{sim}</a><a href="/forum">{forum}</a></nav>'
        '<nav aria-label="{a}"><h3>{a}</h3>{acct}</nav>'
        '<nav aria-label="{lg}"><h3>{lg}</h3><a href="/a-propos">{about}</a><a href="/contact">{contact}</a><a href="/mentions-legales">{legal}</a>'
        '<a href="/confidentialite">{privacy}</a></nav></div><div class="wrap foot-note">{note} {motion}</div></footer>').format(
        mark=BRAND_MARK, site=esc(t("site.name")), tag=esc(t("footer.tag")), e=esc(t("footer.explore")), enc=esc(t("nav.encyclopedia")),
        idt=esc(t("nav.identify")), cmp=esc(t("cmp.title")), gl=esc(t("gloss.title")), les=esc(t("nav.lessons")), qz=esc(t("nav.quiz")), pr=esc(t("nav.prices")), m=esc(t("footer.members")), sim=esc(t("nav.simulator")),
        forum=esc(t("nav.forum")), a=esc(t("footer.account")), acct=acct,
        lg=esc(t("footer.legal")), about=esc(t("footer.about")), contact=esc(t("footer.contact")), legal=esc(t("footer.legal.notice")),
        privacy=esc(t("footer.privacy")),
        note=esc(t("footer.copyright", year=time.gmtime().tm_year, name=config.legal("name") or t("site.name")) if config.INDEXABLE else t("footer.note")),
        motion=motion_button(t))


def page(req, title, content, section="", sim=False, status=200, description=""):
    """Wraps `content` in the site layout. Every page carries the noindex tag."""
    t = req.t
    user = req.user
    nav = []
    for key, href, label in NAV:
        cls = ' class="current"' if key == section else ""
        nav.append('<a href="{}"{}>{}</a>'.format(href, cls, esc(t(label))))
    if user:
        admin = '<a href="/admin">{}</a>'.format(esc(t("nav.admin"))) if user["role"] == "admin" else ""
        who = (
            '<span class="who"><a href="/compte">{name}</a>{admin}'
            '<form method="post" action="/deconnexion" class="inline">{csrf}'
            '<button type="submit" class="linklike">{logout}</button></form></span>'
        ).format(name=esc(user["username"]), admin=admin, csrf=csrf_field(req), logout=esc(t("nav.logout")))
    else:
        who = '<span class="who"><a href="/connexion">{}</a><a href="/inscription" class="btn small">{}</a></span>'.format(
            esc(t("nav.login")), esc(t("nav.register")))

    submenu = ""
    if section == "encyclopedia":
        here = req.path.rstrip("/") or "/"
        links = "".join('<a href="{}"{}>{}</a>'.format(
            href, ' class="current" aria-current="page"' if (here == href or (href == "/encyclopedie/familles" and here.startswith(("/encyclopedie/familles/", "/encyclopedie/classes/"))) or (href == "/encyclopedie" and here.startswith("/encyclopedie/") and here != "/encyclopedie/formes" and not here.startswith(("/encyclopedie/familles", "/encyclopedie/classes")))) else "",
            esc(t(label))) for href, label in ENCYCLOPEDIA_MENU)
        submenu = '<div class="wrap subnav"><nav aria-label="{}">{}</nav></div>'.format(esc(t("sub.label")), links)

    banners = []
    flash = req.pop_flash()
    if flash:
        banners.append('<div class="banner ok" role="status">{}</div>'.format(esc(t(flash))))
    if user and not user["email_verified_at"]:
        banners.append(
            '<div class="banner warn">{msg}<form method="post" action="/verification/renvoyer" class="inline">'
            '{csrf}<button type="submit" class="linklike">{btn}</button></form></div>'.format(
                msg=esc(t("banner.unverified")), csrf=csrf_field(req), btn=esc(t("banner.resend"))))

    description = description or t("meta.description")
    desc = '<meta name="description" content="{}">'.format(esc(description))
    base = config.BASE_URL if not config.DEV else req.base_url
    canonical_url = base + req.prefix + (req.path if req.path != "/" else ("" if req.prefix else "/"))
    page_no = req.query.get("page")
    if page_no and page_no.isdigit() and page_no != "1":
        canonical_url += "?page=" + page_no
    social = ('<meta property="og:site_name" content="{site}"><meta property="og:type" content="website">'
              '<meta property="og:title" content="{title}"><meta property="og:description" content="{d}">'
              '<meta property="og:url" content="{url}"><meta property="og:image" content="{img}">'
              '<meta property="og:locale" content="{loc}"><meta name="twitter:card" content="summary_large_image">').format(
        site=esc(t("site.name")), title=esc(title), d=esc(description), url=esc(canonical_url), img=esc(base + "/static/img/og-image.png"),
        loc=esc({"fr": "fr_FR", "en": "en_US"}.get(req.lang, req.lang)))
    if not is_private(req.path):
        # The same page in the other language, for search engines (the French version is the default one).
        fr_url = base + req.path
        en_url = base + "/en" + (req.path if req.path != "/" else "")
        social += ('<link rel="alternate" hreflang="fr" href="{fr}"><link rel="alternate" hreflang="en" href="{en}">'
                   '<link rel="alternate" hreflang="x-default" href="{fr}">').format(fr=esc(fr_url), en=esc(en_url))
        if config.INDEXABLE:
            social += '<link rel="canonical" href="{}">'.format(esc(canonical_url))
    other = "en" if req.lang == "fr" else "fr"
    qs = req.environ.get("QUERY_STRING", "")
    switch_path = ("/en" + (req.path if req.path != "/" else "")) if other == "en" else req.path
    switcher = '<a class="langswitch" href="{}" hreflang="{}" lang="{}" aria-label="{}" title="{}">{}</a>'.format(
        esc(switch_path + ("?" + qs if qs else "")), other, other, esc(t("lang.switch.label")), esc(t("lang.switch.label")), other.upper())
    body_class = "sim" if sim else ""
    doc = """<!doctype html>
<html lang="{lang}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="{robots}">
<title>{title} · {site}</title>
{desc}{social}
<link rel="icon" href="/static/img/favicon.svg" type="image/svg+xml">
<link rel="apple-touch-icon" href="/static/img/apple-touch-icon.png">
<script src="/static/js/motion.js" defer></script>
<link rel="stylesheet" href="/static/css/site.css">
<link rel="stylesheet" href="/static/css/theme.css">
<meta name="color-scheme" content="light dark">
</head>
<body class="{body_class}">
<a class="skip" href="#main">{skip}</a>
<header class="top">
  <div class="wrap bar">
    <a class="brand" href="/">{mark}<span>{site}</span></a>
    <nav aria-label="{navlabel}">{nav}</nav>
    <a class="searchlink" href="/recherche" aria-label="{searchlabel}" title="{searchlabel}"><svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg></a>
    @@LANGSWITCH@@
    {who}
  </div>
  {submenu}
</header>
<main id="main" class="wrap">
{banners}
{content}
</main>
{footer}
</body>
</html>""".format(
        lang=req.lang, title=esc(title), site=esc(t("site.name")), desc=desc, social=social, robots=robots_value(req.path), body_class=body_class,
        skip=esc(t("nav.skip")), navlabel=esc(t("nav.label")), searchlabel=esc(t("search.title")), nav="".join(nav), who=who,
        banners="".join(banners), submenu=submenu, content=content, footer=_footer(req), mark=BRAND_MARK)
    return Response(req.localize(doc).replace("@@LANGSWITCH@@", switcher), status=status)


def message_page(req, title, text, status=200, link=None):
    extra = ""
    if link:
        extra = '<p><a class="btn" href="{}">{}</a></p>'.format(esc(link[0]), esc(link[1]))
    content = '<section class="narrow"><h1>{}</h1><p>{}</p>{}</section>'.format(esc(title), esc(text), extra)
    return page(req, title, content, status=status)


def pagination(req, base, page_no, pages):
    if pages <= 1:
        return ""
    parts = []
    sep = "&" if "?" in base else "?"
    if page_no > 1:
        parts.append('<a href="{}{}page={}">{}</a>'.format(esc(base), sep, page_no - 1, esc(req.t("pager.prev"))))
    parts.append('<span>{}</span>'.format(esc(req.t("pager.of", n=page_no, total=pages))))
    if page_no < pages:
        parts.append('<a href="{}{}page={}">{}</a>'.format(esc(base), sep, page_no + 1, esc(req.t("pager.next"))))
    return '<nav class="pager">{}</nav>'.format(" ".join(parts))


def page_number(req, pages):
    try:
        n = int(req.query.get("page", "1"))
    except ValueError:
        n = 1
    return max(1, min(n, max(pages, 1)))


def safe_next(path):
    """Only allow redirects to a path inside this site."""
    if path and re.fullmatch(r"/[A-Za-z0-9_\-./?=&%]*", path) and not path.startswith("//"):
        return path
    return "/"
