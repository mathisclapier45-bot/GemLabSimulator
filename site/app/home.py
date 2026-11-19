"""The home page: hero with a search box, a ribbon of stones, the stone of the day, facts computed from the data,
a path for beginners, the instruments, a quick identification form and the lessons. All of it comes from the data files."""
import datetime
import random
import re

from . import catalog, routes_learn
from .views import esc, fmt_num, motion_button, page


ICONS = {
    "enc": '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3 4 9l8 12 8-12-8-6Z"/><path d="M4 9h16M9 9l3 12 3-12"/></svg>',
    "idt": '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4M8 11h6M11 8v6"/></svg>',
    "les": '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M4 5a2 2 0 0 1 2-2h12v16H6a2 2 0 0 0-2 2V5Z"/><path d="M8 7h6M8 11h8M18 19v2H6"/></svg>',
    "sim": '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4M9 10l2 2 4-4"/></svg>',
}


def natural_species(species):
    return catalog.natural_species(species)


def _chip(s, size=44):
    return '<span class="gemchip" style="--g:{};--gf:{};--s:{}px" aria-hidden="true"></span>'.format(esc(s["iconColor"]), esc(s["iconFacetColor"]), size)


def _link(s):
    return '<a href="/encyclopedie/{}">{}</a>'.format(esc(s["slug"]), esc(s["displayName"]))


def _lookalike(s, species):
    """The species that is the closest to this one by index, density and hardness: the one it is most often mistaken for."""
    best, best_gap = None, None
    for o in species:
        if o is s or not o.get("refractiveIndex") or not o.get("density"):
            continue
        gap = (abs(o["refractiveIndex"] - s["refractiveIndex"]) / 0.05 + abs(o["density"] - s["density"]) / 0.3
               + abs((o.get("hardness") or 0) - (s.get("hardness") or 0)) / 1.0)
        if o.get("opticalCharacterClassName") != s.get("opticalCharacterClassName"):
            gap += 1.0
        if best_gap is None or gap < best_gap:
            best, best_gap = o, gap
    return best


def _facts(req, species, today):
    """Short facts that are always true because they are read from the data; three of them, changing every day."""
    t = req.t
    natural = natural_species(species)
    clear = [s for s in natural if s["transparency_group"] == "transparent"]
    pool = []

    def top(key, field, biggest, value_fmt, among=None):
        have = [s for s in (among or natural) if s.get(field)]
        pick = max(have, key=lambda s: s[field]) if biggest else min(have, key=lambda s: s[field])
        pool.append(esc(t(key, name="@@", value=value_fmt(pick[field]))).replace("@@", _link(pick)))

    top("home.fact.hardest", "hardness", True, lambda v: fmt_num(v, 1))
    top("home.fact.softest", "hardness", False, lambda v: fmt_num(v, 1))
    top("home.fact.densest", "density", True, lambda v: fmt_num(v, 2))
    top("home.fact.lightest", "density", False, lambda v: fmt_num(v, 2))
    top("home.fact.ri", "refractiveIndex", True, lambda v: fmt_num(v, 3), clear)
    fiery = max(clear, key=lambda s: catalog.dispersion(s)[0])
    pool.append(esc(t("home.fact.fire", name="@@", value=fmt_num(catalog.dispersion(fiery)[0], 3))).replace("@@", _link(fiery)))
    pool.append(esc(t("home.fact.pleo", n=sum(1 for s in species if s.get("pleochroic")), total=len(species))))
    pool.append(esc(t("home.fact.spectrum", n=sum(1 for s in species if s["has_spectrum"]), total=len(species))))
    start = today.toordinal() % len(pool)
    return [pool[(start + i) % len(pool)] for i in range(3)]


def render(req):
    t = req.t
    d = catalog.data()
    species = d["species"]
    today = datetime.date.today()
    rnd = random.Random(today.toordinal())
    lessons = catalog.lessons()

    # ---- stats
    n_questions = sum(len(catalog.quiz(it["id"]) or []) for it in lessons)
    stats = [(len(species), t("home.stat.species")), (len(lessons), t("home.stat.lessons")), (n_questions, t("home.stat.questions")),
             (len(catalog.glossary()[0]), t("home.stat.terms"))]
    stat_html = "".join('<div class="stat"><strong>{}</strong><span>{}</span></div>'.format(esc(str(n)), esc(label)) for n, label in stats)

    # ---- ribbon of stones (two rows, going opposite ways)
    sample = rnd.sample(species, 36)
    rows = []
    for k, part in enumerate((sample[:18], sample[18:])):
        items = "".join('<a class="rib" href="/encyclopedie/{}">{}<span>{}</span></a>'.format(esc(s["slug"]), _chip(s, 26), esc(s["displayName"])) for s in part)
        rows.append('<div class="ribbon-row {}"><div class="ribbon-track">{}{}</div></div>'.format("rev" if k else "", items, items))
    ribbon = '<section class="ribbon" aria-label="{}">{}</section>'.format(esc(t("home.ribbon")), "".join(rows))

    # ---- stone of the day
    natural = natural_species(species)
    star = natural[(today.toordinal() * 7) % len(natural)]
    twin = _lookalike(star, species)
    formula = (star.get("extra") or {}).get("formula", "")
    desc = star.get("description", "")
    if len(desc) > 210:
        desc = desc[:207].rsplit(" ", 1)[0] + "…"
    figures = "".join('<div><strong>{}</strong><span>{}</span></div>'.format(esc(v), esc(label)) for label, v in (
        (t("sp.ri"), fmt_num(star.get("refractiveIndex"), 3)), (t("sp.density"), fmt_num(star.get("density"), 2)),
        (t("sp.hardness"), fmt_num(star.get("hardness"), 1)), (t("sp.dispersion"), fmt_num(catalog.dispersion(star)[0], 3))))
    twin_html = ""
    if twin:
        twin_html = '<p class="twin">{} <a href="/comparer?a={}&amp;b={}">{}</a></p>'.format(
            esc(t("home.star.twin")), esc(star["slug"]), esc(twin["slug"]), esc(t("home.star.twin.link", name=twin["displayName"])))
    star_html = (
        '<article class="star" style="--g:{g};--gf:{gf}"><span class="eyebrow">{eyebrow}</span><div class="star-head">{chip}<div><h3>{name}</h3>'
        '<p class="formula">{formula}</p></div></div><p>{desc}</p><div class="figures">{figures}</div>{twin}'
        '<a class="btn" href="/encyclopedie/{slug}">{open}</a></article>').format(
        g=esc(star["iconColor"]), gf=esc(star["iconFacetColor"]), eyebrow=esc(t("home.star")), chip=_chip(star, 72), name=esc(star["displayName"]),
        formula=esc(formula), desc=esc(desc), figures=figures, twin=twin_html, slug=esc(star["slug"]), open=esc(t("home.star.open")))

    facts = "".join("<li>{}</li>".format(f) for f in _facts(req, species, today))
    facts_html = '<aside class="facts"><span class="eyebrow">{}</span><h3>{}</h3><ul>{}</ul><a href="/glossaire">{}</a></aside>'.format(
        esc(t("home.facts.eyebrow")), esc(t("home.facts")), facts, esc(t("home.facts.more")))

    # ---- path for beginners
    steps = []
    for i, (href, key) in enumerate((("/lecons/gemmes", "1"), ("/lecons/loupe", "2"), ("/identification", "3"), ("/quiz", "4"))):
        steps.append('<li><a class="step-card" href="{}"><span class="n">{}</span><h3>{}</h3><p>{}</p></a></li>'.format(
            href, i + 1, esc(t("home.path." + key + ".title")), esc(t("home.path." + key + ".text"))))
    path_html = '<section class="path"><span class="eyebrow">{}</span><h2>{}</h2><ol>{}</ol></section>'.format(
        esc(t("home.path.eyebrow")), esc(t("home.path.title")), "".join(steps))

    # ---- the four doors of the site
    doors = [("enc", "/encyclopedie", "home.f1"), ("idt", "/identification", "home.f2"), ("les", "/lecons", "home.f3"), ("sim", "/simulateur", "home.f4")]
    feat_html = '<section class="features"><h2>{}</h2><div class="feature-grid">{}</div></section>'.format(esc(t("home.features")), "".join(
        '<a class="feature" href="{h}"><span class="ico">{i}</span><h3>{ti}</h3><p>{tx}</p><span class="more">{op} \u2192</span></a>'.format(
            h=h, i=ICONS[i], ti=esc(t(k + ".title")), tx=esc(t(k + ".text")), op=esc(t("home.open"))) for i, h, k in doors))

    # ---- instruments
    tools = [it for it in lessons if it["kind"] == "tool" and (it.get("tool") or {}).get("name")]
    cards = []
    for i, it in enumerate(tools):
        # the first clause only: what the instrument measures, without the details after ":" or "("
        measures = re.split(r"\s*[:(]", it["tool"].get("measures", ""))[0]
        if len(measures) > 72 and "," in measures[:72]:
            measures = measures[:72].rsplit(",", 1)[0]
        cards.append('<a class="inst c{}" href="/lecons/{}"><strong>{}</strong><span>{}</span></a>'.format(
            i % 6, esc(it["id"]), esc(it["tool"]["name"]), esc(t("home.inst.measures", what=measures))))
    inst_html = '<section class="instruments"><span class="eyebrow">{}</span><h2>{}</h2><div class="inst-grid">{}</div></section>'.format(
        esc(t("home.inst.eyebrow")), esc(t("home.inst.title")), "".join(cards))

    # ---- quick identification
    quick = (
        '<section class="quick"><div><span class="eyebrow">{eyebrow}</span><h2>{title}</h2><p>{text}</p></div>'
        '<form method="get" action="/identification" class="quickform"><label>{ri}<input type="text" inputmode="decimal" name="ri" placeholder="1,76"></label>'
        '<label>{de}<input type="text" inputmode="decimal" name="density" placeholder="4,0"></label>'
        '<button class="btn" type="submit">{go}</button></form></section>').format(
        eyebrow=esc(t("home.quick.eyebrow")), title=esc(t("home.quick.title")), text=esc(t("home.quick.text")),
        ri=esc(t("sp.ri")), de=esc(t("sp.density")), go=esc(t("home.quick.go")))

    # ---- lessons (the newest first) and the quiz
    mine = routes_learn.results_of(req.db, req.user)
    newest = list(reversed(lessons))[:6]
    tiles = []
    for it in newest:
        r = mine.get(it["id"])
        chip = ' <span class="chip {}">{}</span>'.format("ok" if r and routes_learn.passed(r["best"], r["total"]) else "mid", esc(t("quiz.chip", best=r["best"], total=r["total"]))) if r else ""
        tiles.append('<li><a class="tile wide" href="/lecons/{}"><span class="name">{}</span><span class="sub">{}{}</span></a></li>'.format(
            esc(it["id"]), esc(it["title"]), esc(t("les.steps", n=len(it["steps"]))), chip))
    lessons_html = ('<section class="lessons-home"><span class="eyebrow">{}</span><h2>{}</h2><ul class="tiles">{}</ul>'
                    '<p class="side-link"><a href="/lecons">{}</a> · <a href="/comparer">{}</a> · <a href="/glossaire">{}</a></p></section>').format(
        esc(t("home.lessons.eyebrow")), esc(t("home.lessons.title")), "".join(tiles), esc(t("home.lessons.all")), esc(t("cmp.title")), esc(t("gloss.title")))

    join = ""
    if not req.user:
        join = ('<section class="join"><div><h2>{}</h2><p>{}</p></div><a class="btn big light" href="/inscription">{}</a></section>').format(
            esc(t("home.join.title")), esc(t("home.join.text")), esc(t("home.cta.join")))

    hero = (
        '<section class="hero2"><div class="hero-text"><span class="pill">{badge}</span><h1>{h1}</h1><p class="lead">{lead}</p>'
        '<form class="herosearch" method="get" action="/recherche" role="search"><input type="search" name="q" placeholder="{ph}" aria-label="{ph}">'
        '<button class="btn" type="submit">{go}</button></form>'
        '<div class="cta"><a class="btn big" href="/lecons">{c1}</a><a class="btn big ghost" href="/simulateur">{c2}</a></div>'
        '<p class="motion-line">{motion}</p></div>'
        '<div class="hero-visual"><i class="spark s1"></i><i class="spark s2"></i><i class="spark s3"></i>'
        '<div id="hero-gem" role="img" aria-label="{cap}"></div><p class="small">{cap}</p></div></section>').format(
        badge=esc(t("home.badge")), h1=esc(t("home.title")), lead=esc(t("home.lead")), ph=esc(t("home.search")), go=esc(t("home.search.go")),
        c1=esc(t("home.cta.primary")), c2=esc(t("home.cta.secondary")), cap=esc(t("home.sapphire")), motion=motion_button(t))

    content = (hero + ribbon + '<section class="stats">' + stat_html + '</section>'
               + '<section class="today">' + star_html + facts_html + '</section>'
               + path_html + feat_html + inst_html + quick + lessons_html + join
               + '<script src="/static/js/gem3d.js" defer></script><script src="/static/js/home.js" defer></script>')
    return page(req, t("nav.home"), content, section="home")
