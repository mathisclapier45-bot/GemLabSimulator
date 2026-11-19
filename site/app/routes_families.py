"""The families of gems in the encyclopedia: the classes of minerals (and the synthetics), then the great families, each with its page."""
from . import catalog, class_icons, garnet_diagram, markup
from .views import esc, fmt_num, gem_svg, page
from .web import HTTPError, Router

router = Router()


def _range(values, digits):
    values = [v for v in values if v]
    if not values:
        return ""
    lo, hi = min(values), max(values)
    return fmt_num(lo, digits) if lo == hi else "{} – {}".format(fmt_num(lo, digits), fmt_num(hi, digits))


def _tile(t, s, note=""):
    return ('<li><a class="tile" style="--gem:{gc}" href="/encyclopedie/{slug}">{gem}<span class="name">{name}</span>'
            '<span class="sub">{sub}</span></a></li>').format(
        gc=esc(s["iconColor"]), slug=s["slug"], gem=gem_svg(s["iconColor"], s["iconFacetColor"], 44), name=esc(s["displayName"]),
        sub=esc(note or t("enc.tile.sub", hardness=fmt_num(s.get("hardness"), 1), ri=fmt_num(s.get("refractiveIndex"), 3))))


def _count(t, n):
    return t("fam.count.one") if n == 1 else t("fam.count", n=n)


def _picture(item, size, is_class):
    """The icon of a class of minerals, or the gem of the first species of a family."""
    if is_class and class_icons.icon(item["id"], size):
        return class_icons.icon(item["id"], size)
    first = item["species"][0]
    return gem_svg(first["iconColor"], first["iconFacetColor"], size)


def _card(t, base, item, is_class=False):
    first = item["species"][0]
    return ('<li><a class="tile wide" style="--gem:{gc}" href="{base}/{id}">{pic}<span class="name">{name}</span>'
            '<span class="sub">{count}</span></a></li>').format(
        gc=esc(first["iconColor"]), base=base, id=esc(item["id"]), pic=_picture(item, 44, is_class),
        name=esc(item["name"]), count=esc(_count(t, len(item["species"]))))


@router.route("GET", "/encyclopedie/familles")
def index(req):
    t = req.t
    classes = catalog.classes()["items"]
    families = catalog.families()["items"]
    content = (
        '<h1>{h1}</h1><p class="lead">{lead}</p>'
        '<h2>{ch}</h2><p class="small">{cl}</p><ul class="tiles">{ctiles}</ul>'
        '<h2>{fh}</h2><p class="small">{fl}</p><ul class="tiles">{ftiles}</ul><p class="small">{more}</p>').format(
        h1=esc(t("fam.title")), lead=esc(t("fam.lead")),
        ch=esc(t("fam.classes.title")), cl=esc(t("fam.classes.lead")), ctiles="".join(_card(t, "/encyclopedie/classes", c, True) for c in classes),
        fh=esc(t("fam.families.title")), fl=esc(t("fam.families.lead")), ftiles="".join(_card(t, "/encyclopedie/familles", f) for f in families),
        more=esc(t("fam.more")))
    return page(req, t("fam.title"), content, section="encyclopedia", description=t("fam.desc"))


# Figures that belong to one family: the diagram of the garnets sits under their introduction.
FIGURES = {"grenats": garnet_diagram.section}


def _group_page(req, item, items, base, back_label, is_class=False):
    """The page of a class or of a family: introduction, what its species have in common, key points, the species."""
    t = req.t
    members = item["species"]
    systems = []
    for s in members:
        key = (s.get("extra") or {}).get("system", "")
        if key and t("sys." + key) not in systems:
            systems.append(t("sys." + key))
    labels = {"Isotrope": t("enc.optic.isotropic"), "Uniaxe": t("enc.optic.uniaxial"), "Biaxe": t("enc.optic.biaxial"), "Opaque": t("enc.optic.opaque")}
    optics = []
    for s in members:
        label = labels.get(s.get("opticalCharacterClassName") or "")
        if label and label not in optics:
            optics.append(label)

    def row(label, value, gloss=None):
        if not value:
            return ""
        link = ' <a class="small" href="/glossaire#{}">?</a>'.format(esc(gloss)) if gloss else ""
        return '<tr><th scope="row">{}</th><td>{}{}</td></tr>'.format(esc(label), esc(value), link)

    props = "".join([
        row(t("sp.system"), ", ".join(systems)),
        row(t("sp.ri"), _range([s.get("refractiveIndex") for s in members], 3), "indice-de-refraction"),
        row(t("sp.density"), _range([s.get("density") for s in members], 2), "densite"),
        row(t("sp.hardness"), _range([s.get("hardness") for s in members], 1), "durete"),
        row(t("sp.optic"), ", ".join(optics)),
    ])
    seen = set()
    intro = markup.link_terms(esc(item["intro"]), seen)
    points = "".join("<li>{}</li>".format(markup.link_terms(esc(p), seen)) for p in item["points"])
    tiles = []
    for s in members:
        note = t("fam.synthetic.sub", sub=t("enc.tile.sub", hardness=fmt_num(s.get("hardness"), 1), ri=fmt_num(s.get("refractiveIndex"), 3))) if s["synthetic"] else ""
        tiles.append(_tile(t, s, note))
    idx = items.index(item)
    prev_i = items[idx - 1] if idx > 0 else None
    next_i = items[idx + 1] if idx + 1 < len(items) else None
    nav = '<nav class="prevnext">{}{}</nav>'.format(
        '<a href="{}/{}">← {}</a>'.format(base, prev_i["id"], esc(prev_i["name"])) if prev_i else "<span></span>",
        '<a href="{}/{}">{} →</a>'.format(base, next_i["id"], esc(next_i["name"])) if next_i else "<span></span>")
    first = members[0]
    content = (
        '<nav class="crumbs"><a href="/encyclopedie/familles">{back}</a></nav>'
        '<header class="species-head" style="--gem:{gc};--gem2:{gf}">{gem}<div><h1>{name}</h1><p class="formula big">{chem}</p>'
        '<p class="small">{count}</p></div></header><p class="lead">{intro}</p>'
        '{figure}<h2>{ph}</h2><table class="props">{props}</table><h2>{pt}</h2><ul class="famlist">{points}</ul>'
        '<h2>{mh}</h2><ul class="tiles">{tiles}</ul>{nav}').format(
        back=esc(back_label), gc=esc(first["iconColor"]), gf=esc(first["iconFacetColor"]), gem=_picture(item, 96, is_class),
        name=esc(item["name"]), chem=esc(item["chem"]), count=esc(_count(t, len(members))), intro=intro,
        figure=FIGURES[item["id"]](t) if not is_class and item["id"] in FIGURES else "",
        ph=esc(t("fam.props")), props=props, pt=esc(t("fam.points")), points=points, mh=esc(t("fam.members")), tiles="".join(tiles), nav=nav)
    return page(req, item["name"], content, section="encyclopedia", description=item["intro"][:160])


@router.route("GET", "/encyclopedie/familles/<fid>")
def family(req):
    items = catalog.families()["items"]
    item = next((x for x in items if x["id"] == req.params["fid"]), None)
    if item is None:
        raise HTTPError(404)
    return _group_page(req, item, items, "/encyclopedie/familles", req.t("fam.back"))


@router.route("GET", "/encyclopedie/classes/<cid>")
def mineral_class(req):
    items = catalog.classes()["items"]
    item = next((x for x in items if x["id"] == req.params["cid"]), None)
    if item is None:
        raise HTTPError(404)
    return _group_page(req, item, items, "/encyclopedie/classes", req.t("fam.back"), True)
