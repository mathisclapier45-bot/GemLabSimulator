"""Public pages: home, encyclopedia, lessons, robots.txt."""
import json
import re

from . import catalog, config, diagrams, home, i18n, markup, routes_learn, views
from .views import esc, fmt_num, gem_svg, page
from .web import HTTPError, Response, Router, redirect

router = Router()


@router.route("GET", "/")
def home_page(req):
    return home.render(req)


# ------------------------------------------------------------------ encyclopedia

def _select(name, label, options, current):
    opts = "".join('<option value="{}"{}>{}</option>'.format(
        esc(v), " selected" if v == current else "", esc(text)) for v, text in options)
    return '<label>{}<select name="{}">{}</select></label>'.format(esc(label), name, opts)


@router.route("GET", "/encyclopedie")
def encyclopedia(req):
    t = req.t
    q = req.query
    f = {k: q.get(k, "")[:80] for k in ("q", "transparency", "optic", "hardness", "extra", "synthetic")}
    results = catalog.search(**f)
    shown = results                      # every stone on one page: no pagination
    all_label = t("enc.filter.all")
    form = (
        '<form class="filters" method="get" action="/encyclopedie" role="search">'
        '<label class="grow">{ql}<input type="search" name="q" value="{qv}" placeholder="{qp}"></label>'
        '{tr}{op}{ha}{ex}{sy}'
        '<button type="submit" class="btn">{go}</button> <a href="/encyclopedie" class="linklike">{reset}</a>'
        '</form>').format(
        ql=esc(t("enc.search")), qv=esc(f["q"]), qp=esc(t("enc.search.placeholder")),
        tr=_select("transparency", t("enc.filter.transparency"), [
            ("", all_label), ("transparent", t("enc.transparency.transparent")),
            ("translucide", t("enc.transparency.translucent")), ("opaque", t("enc.transparency.opaque"))],
            f["transparency"]),
        op=_select("optic", t("enc.filter.optic"), [
            ("", all_label), ("Isotrope", t("enc.optic.isotropic")), ("Uniaxe", t("enc.optic.uniaxial")),
            ("Biaxe", t("enc.optic.biaxial")), ("Opaque", t("enc.optic.opaque"))], f["optic"]),
        ha=_select("hardness", t("enc.filter.hardness"), [
            ("", all_label), ("soft", t("enc.hardness.soft")), ("medium", t("enc.hardness.medium")),
            ("hard", t("enc.hardness.hard"))], f["hardness"]),
        ex=_select("extra", t("enc.filter.extra"), [
            ("", all_label), ("pleochroic", t("enc.extra.pleochroic")), ("spectrum", t("enc.extra.spectrum")),
            ("fluorescent", t("enc.extra.fluorescent")), ("habit", t("enc.extra.habit"))], f["extra"]),
        sy=_select("synthetic", t("enc.filter.synthetic"), [
            ("", all_label), ("no", t("enc.synthetic.no")), ("yes", t("enc.synthetic.yes"))], f["synthetic"]),
        go=esc(t("enc.filter.apply")), reset=esc(t("enc.filter.reset")))

    if shown:
        tiles = "".join(
            '<li><a class="tile" style="--gem:{gc}" href="/encyclopedie/{slug}">{gem}<span class="name">{name}</span>'
            '<span class="sub">{sub}</span></a></li>'.format(
                gc=esc(s["iconColor"]), slug=s["slug"], gem=gem_svg(s["iconColor"], s["iconFacetColor"], 44), name=esc(s["displayName"]),
                sub=esc(t("enc.tile.sub", hardness=fmt_num(s.get("hardness"), 1), ri=fmt_num(s.get("refractiveIndex"), 3))))
            for s in shown)
        body = '<ul class="tiles">{}</ul>'.format(tiles)
    else:
        body = '<p class="empty">{}</p>'.format(esc(t("enc.empty")))
    content = (
        '<h1>{h1}</h1><p class="lead">{lead}</p>{form}<p class="count">{count}</p>{body}'
        '<p class="side-link"><a href="/encyclopedie/formes">{habits}</a> · <a href="/comparer">{compare}</a> · <a href="/glossaire">{gloss}</a></p>').format(
        h1=esc(t("enc.title")), lead=esc(t("enc.lead")), form=form,
        count=esc(t("enc.count", n=len(results))), body=body,
        habits=esc(t("enc.habits.link")), compare=esc(t("cmp.title")), gloss=esc(t("gloss.title")))
    return page(req, t("enc.title"), content, section="encyclopedia")


def _row(label, value, gloss=None):
    if value in (None, "", False):
        return ""
    head = '<a class="gloss" href="/glossaire#{}">{}</a>'.format(gloss, esc(label)) if gloss else esc(label)
    return "<tr><th scope=\"row\">{}</th><td>{}</td></tr>".format(head, value)


def _wavelength_rgb(nm):
    """Rough visible-colour approximation for the spectrum strip."""
    if nm < 440:
        r, g, b = (440 - nm) / 60, 0, 1
    elif nm < 490:
        r, g, b = 0, (nm - 440) / 50, 1
    elif nm < 510:
        r, g, b = 0, 1, (510 - nm) / 20
    elif nm < 580:
        r, g, b = (nm - 510) / 70, 1, 0
    elif nm < 645:
        r, g, b = 1, (645 - nm) / 65, 0
    else:
        r, g, b = 1, 0, 0
    k = 0.45 + 0.55 * max(0, min(1, (nm - 400) / 40 if nm < 440 else (700 - nm) / 80 if nm > 620 else 1))
    return "rgb({},{},{})".format(int(255 * r * k), int(255 * g * k), int(255 * b * k))


def spectrum_svg(entry, t):
    w, h = 600, 64
    stops = "".join('<stop offset="{:.3f}" stop-color="{}"/>'.format((nm - 400) / 300, _wavelength_rgb(nm))
                    for nm in range(400, 701, 20))
    bands = []
    for ln in entry.get("lines", []):
        wid = max(ln["widthNm"], 1.2)
        x = (ln["wavelengthNm"] - wid / 2 - 400) / 300 * w
        bands.append('<rect x="{:.1f}" y="0" width="{:.1f}" height="{}" fill="#000" opacity="{:.2f}" filter="url(#soft)"/>'.format(
            x, wid / 300 * w, h, min(1, ln["depth"]) * 0.92))
    ticks = "".join(
        '<text x="{:.1f}" y="{}" font-size="10" text-anchor="middle" fill="currentColor">{}</text>'
        '<line x1="{:.1f}" x2="{:.1f}" y1="{}" y2="{}" stroke="currentColor"/>'.format(
            (nm - 400) / 300 * w, h + 20, nm, (nm - 400) / 300 * w, (nm - 400) / 300 * w, h, h + 5)
        for nm in range(400, 701, 50))
    return (
        '<svg class="spectrum" viewBox="-12 0 {vw} {vh}" role="img" aria-label="{label}">'
        '<defs><linearGradient id="rainbow" x1="0" x2="1" y1="0" y2="0">{stops}</linearGradient>'
        '<filter id="soft" x="-20%" y="0" width="140%" height="100%"><feGaussianBlur stdDeviation="1.2 0"/></filter></defs>'
        '<rect x="0" y="0" width="{w}" height="{h}" fill="url(#rainbow)"/>{bands}{ticks}</svg>').format(
        vw=w + 24, vh=h + 26, label=esc(t("sp.spectrum.label")), stops=stops, w=w, h=h, bands="".join(bands), ticks=ticks)


@router.route("GET", "/encyclopedie/formes")
def habits(req):
    t = req.t
    d = catalog.data()
    items = []
    for h in d["habits"]:
        species = d["habit_species"].get(h["id"], [])
        links = ", ".join('<a href="/encyclopedie/{}">{}</a>'.format(s["slug"], esc(s["displayName"])) for s in species)
        items.append('<article class="habit"><h2>{}</h2><p>{}</p>{}</article>'.format(
            esc(h["name"]), esc(h["description"]),
            '<p class="small">{} {}</p>'.format(esc(t("hab.species")), links) if links else ""))
    content = '<nav class="crumbs"><a href="/encyclopedie">{back}</a></nav><h1>{h1}</h1><p class="lead">{lead}</p>{items}'.format(
        back=esc(t("sp.back")), h1=esc(t("hab.title")), lead=esc(t("hab.lead")), items="".join(items))
    return page(req, t("hab.title"), content, section="encyclopedia")


def _dispersion_cell(t, s):
    """Dispersion (spread of the index between blue and red light), as in the game; '≈' when it is an estimate from the index."""
    value, exact = catalog.dispersion(s)
    text = fmt_num(value, 3)
    return esc(text) if exact else '{} <span class="small">{}</span>'.format(esc("\u2248 " + text), esc(t("sp.dispersion.est")))


def _colors_section(req, s):
    """The colour chart of the species: every colour it can have, click one and pick a shape."""
    t = req.t
    pal = s.get("palette")
    if not pal:
        return ""
    import colorsys

    def rgb(h, sat, bri):
        r, g, b = colorsys.hsv_to_rgb(h / 360.0, sat, bri)
        return "#{:02x}{:02x}{:02x}".format(int(r * 255 + .5), int(g * 255 + .5), int(b * 255 + .5))

    variants = []
    if pal.get("diamond"):
        cells = [[h, sa, br, 0, 0, i, t("col.letter", letter=letter)] for i, (h, sa, br, letter) in enumerate(pal["diamond"])]
        variants.append(("natural", cells, True))
    else:
        if pal.get("natural"):
            variants.append(("natural", pal["natural"], False))
        if pal.get("synthetic"):
            variants.append(("synthetic", pal["synthetic"], False))
    if not variants:
        return ""
    data = {}
    grids = []
    for name, cells, letters in variants:
        data[name] = [{"h": c[0], "s": c[1], "b": c[2], "share": c[3], "row": c[4], "name": c[6], "hex": rgb(c[0], c[1], c[2])} for c in cells]
        if letters:
            body = '<div class="sw-row">{}</div>'.format("".join(
                '<button type="button" class="sw" data-v="{v}" data-i="{i}" style="background:{hex}" title="{n}" aria-label="{n}"><span>{n2}</span></button>'.format(
                    v=name, i=i, hex=data[name][i]["hex"], n=esc(cells[i][6]), n2=esc(cells[i][6][-1])) for i in range(len(cells))))
        else:
            steps = sorted({c[5] for c in cells})
            by = {(c[4], c[5]): i for i, c in enumerate(cells)}
            rows = []
            for row in range(8):
                tds = []
                for st in steps:
                    i = by.get((row, st))
                    tds.append("<td></td>" if i is None else
                               '<td><button type="button" class="sw" data-v="{v}" data-i="{i}" style="background:{hex}" title="{n}" aria-label="{n}"></button></td>'.format(
                                   v=name, i=i, hex=data[name][i]["hex"], n=esc(cells[i][6])))
                rows.append("<tr><th scope=\"row\">{}</th>{}</tr>".format(esc(t("row.%d" % row)), "".join(tds)))
            body = '<table class="pgrid"><tbody>{}</tbody></table>'.format("".join(rows))
        grids.append('<div class="grid-wrap" data-variant="{}"{}>{}</div>'.format(name, "" if name == "natural" else " hidden", body))
    toggle = ""
    if len(variants) > 1:
        toggle = ('<div class="seg" role="group"><button type="button" class="on" data-variant="natural">{}</button>'
                  '<button type="button" data-variant="synthetic">{}</button></div>').format(esc(t("col.natural")), esc(t("col.synthetic")))
    shape_ids = ["Rond", "Ovale", "Coussin", "Carre", "Rectangle", "Emeraude", "Octogone", "Baguette", "Poire", "Marquise", "Shield", "Triangle", "Coeur"]
    shape_btns = "".join('<button type="button" data-shape="{k}">{l}</button>'.format(k=k, l=esc(t("shape." + k))) for k in shape_ids)
    cut_ids = ["brillant", "mixte", "a-degres", "ascher", "princesse", "ciseaux", "radiant", "cabochon"]
    cut_btns = "".join('<button type="button" data-cut="{k}">{l}</button>'.format(k=k, l=esc(t("cut." + k))) for k in cut_ids)
    shape_map = {"rectangle a pans coupes": "Emeraude", "carre": "Carre", "coeur": "Coeur"}
    cut_map = {"brillant": "brillant", "cabochon": "cabochon", "mixte": "mixte", "a degres": "a-degres",
               "emeraude": "a-degres", "emeraude (a degres)": "a-degres", "princesse": "princesse"}
    shape_key = catalog.fold(s.get("shape") or "ovale")
    shape = shape_map.get(shape_key, shape_key[:1].upper() + shape_key[1:])
    cut = cut_map.get(catalog.fold(s.get("cutStyle") or ""), "brillant")
    transp = catalog.fold(catalog.fr(s, "transparency") or "")
    milky = 1.0 if transp == "opaque" else 0.75 if transp in ("translucide a opaque", "opaque a translucide") else \
        0.42 if transp == "translucide" else 0.2 if transp == "transparent a translucide" else 0.0
    sid = s["id"]
    disp = catalog.dispersion(s)[0] / 2
    cab_only = sid != "Tourmaline_Schorl" and ("opaque" in transp or s.get("opticalCharacterClassName") == "Opaque"
                                               or sid.startswith(("Ivoire", "Corne", "Defense", "Ecaille")))
    phys = {"ior": s.get("refractiveIndex") or 1.6, "disp": round(disp, 4), "depth": 1.8 if sid in ("Spinelle", "Spinelle_Bleu") else 1.0,
            "milky": milky, "dims": [s.get("lengthMm") or 10, s.get("widthMm") or 8, s.get("depthMm") or 5], "cabonly": bool(cab_only)}
    n = sum(len(v[1]) for v in variants[:1])
    note = t("col.diamond") if pal.get("diamond") else (t("col.count", n=n) + (" " + t("col.any") if pal.get("any") else ""))
    labels = {"info": t("col.info"), "share": t("col.share"), "neutral": t("col.neutral"), "preview": t("col.preview"), "phys": phys}
    return (
        '<section class="colors" data-shape="{shape}" data-cut="{cut}" data-labels="{labels}"><h2>{title}</h2><p class="lead small">{lead}</p>'
        '<p class="small">{note}</p>{toggle}<div class="colors-body"><div class="cpreview"><div class="stone" role="img" aria-label="{pv}"></div>'
        '<p class="small drag">{drag}</p><p class="cinfo" aria-live="polite"></p></div><div class="cgrids">{grids}</div></div>'
        '<div class="pick"><div><strong>{ls}</strong><div class="seg wrap" data-kind="shape">{shapes}</div></div>'
        '<div><strong>{lc}</strong><div class="seg" data-kind="cut">{cuts}</div></div></div>'
        '<script type="application/json" id="pal-data">{data}</script>'
        '<script src="/static/js/gem3d.js" defer></script><script src="/static/js/palette.js" defer></script></section>').format(
        shape=esc(shape), cut=cut, labels=esc(json.dumps(labels, ensure_ascii=False)), title=esc(t("col.title")), lead=esc(t("col.lead")),
        note=esc(note), toggle=toggle, pv=esc(t("col.preview")), drag=esc(t("col.drag")), grids="".join(grids), ls=esc(t("col.shape")), shapes=shape_btns,
        lc=esc(t("col.cut")), cuts=cut_btns, data=json.dumps(data, ensure_ascii=False).replace("</", "<\\/"))


def _fig(svg, caption, extra_class=""):
    return '<figure class="fig {}">{}<figcaption>{}</figcaption></figure>'.format(extra_class, svg, caption)


@router.route("GET", "/encyclopedie/<slug>")
def species_page(req):
    t = req.t
    d = catalog.data()
    s = d["by_slug"].get(req.params["slug"])
    if not s:
        raise HTTPError(404)
    ex = s.get("extra") or {}
    system = ex.get("system", "")

    # ---- gallery of drawings and photo
    figs = []
    if s.get("photo"):
        ph = s["photo"]
        figs.append('<figure class="fig photo"><img src="/static/photos/{f}" alt="{alt}" loading="lazy">'
                    '<figcaption>{cap} <a href="{url}" rel="noopener noreferrer nofollow" target="_blank">{author}</a>, {lic}.</figcaption></figure>'.format(
                        f=esc(ph["file"]), alt=esc(s["displayName"]), cap=esc(t("sp.photo.credit")), url=esc(ph["url"]),
                        author=esc(ph["author"]), lic=esc(ph["license"])))
    if system in diagrams.CELLS:
        figs.append(_fig(diagrams.crystal_svg(system, t("sys." + system)), esc(t("sys." + system)) + "<br><span class=\"small\">" + esc(t("sys." + system + ".rule")) + "</span>"))
    elif system in ("hexagonal", "trigonal"):
        figs.append(_fig(diagrams.hexagonal_svg(t("sys." + system)), esc(t("sys." + system)) + "<br><span class=\"small\">" + esc(t("sys." + system + ".rule")) + "</span>"))
    elif system:
        figs.append('<figure class="fig nofig"><figcaption><strong>{}</strong><br>{}</figcaption></figure>'.format(
            esc(t("sys." + system)), esc(t("sys." + system + ".rule"))))
    optic = s.get("opticalCharacterClassName")
    poly = bool(s.get("isPolycrystalline"))
    if optic in ("Uniaxe", "Biaxe") and not poly:
        cano = ('<canvas class="conoscope" width="224" height="224" data-kind="{k}" data-bulls="{b}" role="img" aria-label="{l}"></canvas>'
                '<script src="/static/js/conoscope.js" defer></script>').format(
            k="bi" if optic == "Biaxe" else "uni", b="1" if s.get("isQuartzFamily") and optic == "Uniaxe" else "0",
            l=esc(t("sp.interference")))
        figs.append(_fig(cano, esc(t("sp.interference")) + "<br><span class=\"small\">" + esc(s.get("interferenceFigure", "")) + "</span>", "conofig"))
    else:
        reason = t("sp.na.opaque") if optic == "Opaque" else (t("sp.na.poly") if poly else t("sp.na.isotropic"))
        figs.append('<figure class="fig nofig"><figcaption><strong>{}</strong><br>{}<br><span class="small">{}</span></figcaption></figure>'.format(
            esc(t("sp.conoscope")), esc(t("sp.na")), esc(reason)))
    # dichroscope: windows with the colours seen in each direction, and how strong the dichroism is
    if optic in ("Uniaxe", "Biaxe") and not poly:
        if s.get("pleochroic"):
            colors = [c for _, c in diagrams.pleochroism_colors(catalog.fr(s, "pleochroicDescription") or "")]
            if not colors:
                base = diagrams.tonal_range(s["iconColor"])
                colors = [base[2][1], base[4][1]]
            strength = diagrams.pleo_strength(catalog.fr(s, "pleochroicDescription") or "")
            cap = '<strong>{}</strong><br>{}<br><span class="small">{}</span>'.format(
                esc(t("sp.dichroscope")), esc(t("sp.dichro.strength", level=t("pleo." + strength))), esc(s.get("pleochroicDescription", "")))
        else:
            colors = [s["iconColor"], s["iconColor"]]
            cap = '<strong>{}</strong><br>{}'.format(esc(t("sp.dichroscope")), esc(t("sp.dichro.none")))
        figs.append('<figure class="fig">{}<figcaption>{}</figcaption></figure>'.format(diagrams.dichroscope_svg(colors, t("sp.dichroscope")), cap))
    else:
        figs.append('<figure class="fig nofig"><figcaption><strong>{}</strong><br>{}<br><span class="small">{}</span></figcaption></figure>'.format(
            esc(t("sp.dichroscope")), esc(t("sp.na")), esc(t("sp.na.opaque") if optic == "Opaque" else (t("sp.na.poly") if poly else t("sp.na.isotropic")))))
    gallery = '<div class="gallery">{}</div>'.format("".join(figs))

    # ---- properties
    props = "".join([
        _row(t("sp.system"), esc(t("sys." + system)) if system else ""),
        _row(t("sp.formula"), '<span class="formula">{}</span> <span class="small">{}</span>'.format(esc(ex.get("formula", "")), esc(ex.get("note", ""))) if ex else ""),
        _row(t("sp.ri"), esc(fmt_num(s.get("refractiveIndex"), 3)), "indice-de-refraction"),
        _row(t("sp.birefringence"), esc(fmt_num(s.get("birefringence"), 3)) if s.get("birefringence") else "", "birefringence"),
        _row(t("sp.dispersion"), _dispersion_cell(t, s), "dispersion"),
        _row(t("sp.density"), esc(fmt_num(s.get("density"), 2)), "densite"),
        _row(t("sp.hardness"), esc(fmt_num(s.get("hardness"), 1)), "durete"),
        _row(t("sp.luster"), esc(s.get("luster", "")), "eclat"),
        _row(t("sp.color"), esc(s.get("color", ""))),
        _row(t("sp.transparency"), esc(s.get("transparency", "")), "transparence"),
        _row(t("sp.optic"), esc(s.get("opticalCharacter", ""))),
        _row(t("sp.pleochroism"), esc(s.get("pleochroicDescription") or "") if s.get("pleochroic") else esc(t("sp.none")), "pleochroisme"),
        _row(t("sp.interference"), esc(s.get("interferenceFigure", "")), "figure-d-interference"),
        _row(t("sp.magnetism"), esc(s.get("magnetism", ""))),
        _row(t("sp.thermal"), esc(t("sp.yes")) if s.get("isThermallyConductiveLikeDiamond") else "", "conductivite-thermique"),
        _row(t("sp.electric"), esc(t("sp.yes")) if s.get("isElectricallyConductive") else ""),
    ])
    sections = []
    if s.get("inclusions"):
        sections.append("<h2>{}</h2><p>{}</p>".format(esc(t("sp.inclusions")), markup.link_terms(esc(s["inclusions"]), set())))
    inst = []
    spec_text = s.get("absorptionSpectrum")
    entry = d["spectra"].get(s["id"])
    # always the whole spectrum: with dark bands when the stone has a typical absorption spectrum, plain when it has none
    text = (entry or {}).get("description") or spec_text or t("sp.spectrum.none")
    inst.append("<h3>{}</h3><p>{}</p>{}".format(esc(t("sp.spectrum")), esc(text), spectrum_svg(entry or {"lines": []}, t)))
    fl = d["fluorescence"].get(s["id"])
    uv_cards = []
    for uid, label, text, color, inten in (
            ("uvl", t("sp.uv.long"), (fl or {}).get("longText") or s.get("fluorescenceLongWave"),
             (fl or {}).get("longColor"), (fl or {}).get("longIntensity", 0)),
            ("uvs", t("sp.uv.short"), (fl or {}).get("shortText") or s.get("fluorescenceShortWave"),
             (fl or {}).get("shortColor"), (fl or {}).get("shortIntensity", 0))):
        if text:
            uv_cards.append('<figure class="fig uv">{}<figcaption><strong>{}</strong><br>{}</figcaption></figure>'.format(
                diagrams.uv_glow_svg(color, inten, uid, label), esc(label), esc(text)))
    if uv_cards:
        inst.append("<h3>{}</h3><div class=\"gallery\">{}</div>{}".format(
            esc(t("sp.uv")), "".join(uv_cards),
            '<p class="small">{}</p>'.format(esc(t("sp.phospho"))) if fl and fl.get("phosphorescent") else ""))
    ch = d["chelsea"].get(s["id"])
    if ch or s.get("chelseaFilterReaction"):
        inst.append("<h3>{}</h3><p>{}{}</p>".format(
            esc(t("sp.chelsea")),
            '<span class="glow" style="background:{}"></span>'.format(esc(ch["color"])) if ch and ch.get("color") else "",
            esc((ch or {}).get("reaction") or s["chelseaFilterReaction"])))
    if inst:
        sections.append("<h2>{}</h2>{}".format(esc(t("sp.instruments")), "".join(inst)))
    if s["habit_list"]:
        sections.append("<h2>{}</h2><ul>{}</ul><p class=\"small\"><a href=\"/encyclopedie/formes\">{}</a></p>".format(
            esc(t("sp.habits")),
            "".join("<li><strong>{}</strong> — {}</li>".format(esc(h["name"]), esc(h["description"])) for h in s["habit_list"]),
            esc(t("enc.habits.link"))))
    origins = [o["name"] for o in s.get("origins", []) if o.get("name")]
    if origins:
        sections.append("<h2>{}</h2><p>{}</p>".format(esc(t("sp.origins")), esc(", ".join(origins))))

    idx = d["species"].index(s)
    prev_s = d["species"][idx - 1] if idx > 0 else None
    next_s = d["species"][idx + 1] if idx + 1 < len(d["species"]) else None
    nav = '<nav class="prevnext">{}{}</nav>'.format(
        '<a href="/encyclopedie/{}">← {}</a>'.format(prev_s["slug"], esc(prev_s["displayName"])) if prev_s else "<span></span>",
        '<a href="/encyclopedie/{}">{} →</a>'.format(next_s["slug"], esc(next_s["displayName"])) if next_s else "<span></span>")
    nav += '<p class="side-link"><a href="/comparer?a={}">{}</a>{}</p>'.format(esc(s["slug"]), esc(t("sp.compare")), (
        ' \u00b7 <a href="/prix?s={}&amp;w=1">{}</a>'.format(esc(s["slug"]), esc(t("sp.price"))) if catalog.natural_species([s]) else ""))
    badge = '<span class="badge">{}</span>'.format(esc(t("sp.synthetic"))) if s["synthetic"] else ""
    seen_terms = set()
    content = (
        '<nav class="crumbs"><a href="/encyclopedie">{back}</a></nav>'
        '<header class="species-head" style="--gem:{gemc};--gem2:{gemf}">{gem}<div><h1>{name} {badge}</h1>'
        '<p class="formula big">{formula}</p><p class="lead">{desc}</p></div></header>'
        '{gallery}{colors}<h2>{ph}</h2><table class="props">{props}</table>{sections}{nav}').format(
        back=esc(t("sp.back")), gem=gem_svg(s["iconColor"], s["iconFacetColor"], 96), name=esc(s["displayName"]),
        gemc=esc(s["iconColor"]), gemf=esc(s["iconFacetColor"]), formula=esc(ex.get("formula", "")),
        badge=badge, desc=markup.link_terms(esc(s.get("description", "")), seen_terms), gallery=gallery, colors=_colors_section(req, s), ph=esc(t("sp.properties")), props=props,
        sections="".join(sections), nav=nav)
    return page(req, s["displayName"], content, section="encyclopedia", description=s.get("description", "")[:160])


# ------------------------------------------------------------------ search by data

@router.route("GET", "/identification")
def identification(req):
    t = req.t
    keys = ("ri", "ri_tol", "density", "density_tol", "birefringence", "birefringence_tol", "hardness_from", "hardness_to", "optic",
            "pleo", "color", "transparency", "luster", "uv", "chelsea", "spectrum", "thermal", "magnet", "synthetic")
    q = {k: req.query.get(k, "").strip()[:20] for k in keys}
    searched = any(q[k] for k in keys if not k.endswith("_tol"))
    any_label = t("idt.any")

    def sel(name, label, options):
        opts = "".join('<option value="{}"{}>{}</option>'.format(esc(v), " selected" if q[name] == v else "", esc(tx))
                       for v, tx in [("", any_label)] + options)
        return '<label>{}<select name="{}">{}</select></label>'.format(esc(label), name, opts)

    def num(name, label, step, tols):
        topts = "".join('<option value="{0}"{1}>±{0}</option>'.format(v, " selected" if q[name + "_tol"] == v else "") for v in tols)
        return ('<fieldset class="numrow"><legend>{l}</legend><input type="text" inputmode="decimal" name="{n}" value="{v}" placeholder="{ph}" aria-label="{l}">'
                '<span>{tol}</span><select name="{n}_tol" aria-label="{tol}">{o}</select></fieldset>').format(
            l=esc(label), n=name, v=esc(q[name]), ph=step, tol=esc(t("idt.tol")), o=topts)

    yes_no = [("yes", t("idt.yes")), ("no", t("idt.no"))]
    form = (
        '<form class="filters ident" method="get" action="/identification">'
        '<h2>{h1}</h2><div class="fgrid">{ri}{de}{bi}'
        '<fieldset class="numrow"><legend>{ha}</legend><span>{fr}</span><input type="text" inputmode="decimal" name="hardness_from" value="{hf}" placeholder="1" aria-label="{ha} {fr}">'
        '<span>{to}</span><input type="text" inputmode="decimal" name="hardness_to" value="{ht}" placeholder="10" aria-label="{ha} {to}"></fieldset></div>'
        '<h2>{h2}</h2><div class="fgrid">{optic}{pleo}{color}{transp}{luster}{uv}{chel}{spec}{therm}{magnet}{synth}</div>'
        '<p class="small">{hint}</p><button type="submit" class="btn">{go}</button> <a href="/identification" class="linklike">{reset}</a></form>').format(
        h1=esc(t("idt.measures")), h2=esc(t("idt.observations")),
        ri=num("ri", t("idt.ri"), "1,54", ["0.005", "0.01", "0.02", "0.05"]),
        de=num("density", t("idt.density"), "2,65", ["0.05", "0.1", "0.2", "0.5"]),
        bi=num("birefringence", t("idt.birefringence"), "0,009", ["0.002", "0.005", "0.01", "0.02"]),
        ha=esc(t("idt.hardness")), fr=esc(t("idt.from")), to=esc(t("idt.to")), hf=esc(q["hardness_from"]), ht=esc(q["hardness_to"]),
        optic=sel("optic", t("idt.optic"), [("Isotrope", t("enc.optic.isotropic")), ("Uniaxe", t("enc.optic.uniaxial")), ("Biaxe", t("enc.optic.biaxial")), ("Opaque", t("enc.optic.opaque"))]),
        pleo=sel("pleo", t("idt.pleo"), yes_no),
        color=sel("color", t("idt.color"), [(k, t("color." + k)) for k in catalog.COLOR_FAMILIES]),
        transp=sel("transparency", t("idt.transparency"), [("transparent", t("enc.transparency.transparent")), ("translucide", t("enc.transparency.translucent")), ("opaque", t("enc.transparency.opaque"))]),
        luster=sel("luster", t("idt.luster"), [(k, t("luster." + k)) for k in catalog.LUSTER_FAMILIES]),
        uv=sel("uv", t("idt.uv"), yes_no),
        chel=sel("chelsea", t("idt.chelsea"), [("red", t("idt.chelsea.red")), ("none", t("idt.chelsea.none"))]),
        spec=sel("spectrum", t("idt.spectrum"), yes_no), therm=sel("thermal", t("idt.thermal"), [("yes", t("idt.yes"))]),
        magnet=sel("magnet", t("idt.magnet"), [("yes", t("idt.magnet.yes")), ("no", t("idt.magnet.no"))]),
        synth=sel("synthetic", t("idt.origin"), [("no", t("enc.synthetic.no")), ("yes", t("enc.synthetic.yes"))]),
        hint=esc(t("idt.hint")), go=esc(t("enc.filter.apply")), reset=esc(t("idt.reset")))

    if not searched:
        body = '<p class="empty">{}</p>'.format(esc(t("idt.empty")))
    else:
        found = catalog.identify(q)
        if not found:
            body = '<p class="empty">{}</p>'.format(esc(t("idt.count.none")))
        else:
            tiles = []
            for sp, score, notes in found[:120]:
                tiles.append(
                    '<li><a class="tile" style="--gem:{gc}" href="/encyclopedie/{slug}">{gem}<span class="name">{name}</span><span class="sub">{sub}</span></a></li>'.format(
                        gc=esc(sp["iconColor"]), slug=sp["slug"], gem=gem_svg(sp["iconColor"], sp["iconFacetColor"], 44), name=esc(sp["displayName"]),
                        sub=esc(t("idt.result.sub", ri=fmt_num(sp.get("refractiveIndex"), 3), density=fmt_num(sp.get("density"), 2),
                                  hardness=fmt_num(sp.get("hardness"), 1)))))
            body = '<p class="count">{}</p><ul class="tiles">{}</ul>'.format(esc(t("idt.count", n=len(found))), "".join(tiles))
    content = '<h1>{}</h1><p class="lead">{}</p>{}{}'.format(esc(t("idt.title")), esc(t("idt.lead")), form, body)
    return page(req, t("idt.title"), content, section="identify")


# ------------------------------------------------------------------ lessons

@router.route("GET", "/lecons")
def lessons_list(req):
    t = req.t
    concepts, tools = [], []
    mine = routes_learn.results_of(req.db, req.user)
    for item in catalog.lessons():
        r = mine.get(item["id"])
        mark = ""
        if r:
            ok = routes_learn.passed(r["best"], r["total"])
            mark = ' <span class="chip {}">{}</span>'.format("ok" if ok else "mid", esc(t("quiz.chip", best=r["best"], total=r["total"])))
        card = '<li><a class="tile wide" href="/lecons/{id}"><span class="name">{title}</span><span class="sub">{n}{mark}</span></a></li>'.format(
            id=esc(item["id"]), title=esc(item["title"]), n=esc(t("les.steps", n=len(item["steps"]))), mark=mark)
        (tools if item["kind"] == "tool" else concepts).append(card)
    content = (
        '<h1>{h1}</h1><p class="lead">{lead}</p>'
        '<h2>{h_c}</h2><ul class="tiles">{c}</ul><h2>{h_t}</h2><ul class="tiles">{tl}</ul>'
        '<p class="side-link"><a href="/glossaire">{gloss}</a>{prog}</p>').format(
        h1=esc(t("les.title")), lead=esc(t("les.lead")), h_c=esc(t("les.concepts")), c="".join(concepts),
        h_t=esc(t("les.tools")), tl="".join(tools), gloss=esc(t("gloss.title")),
        prog=' · <a href="/progression">{}</a>'.format(esc(t("prog.title"))) if req.user else "")
    return page(req, t("les.title"), content, section="lessons")


@router.route("GET", "/lecons/<lesson_id>")
def lesson_page(req):
    t = req.t
    item = catalog.lesson(req.params["lesson_id"]) if catalog.SAFE_ID.match(req.params["lesson_id"]) else None
    if not item:
        raise HTTPError(404)
    toc = "".join('<li><a href="#e{}">{}</a></li>'.format(i + 1, esc(s["title"])) for i, s in enumerate(item["steps"]))
    if catalog.quiz(item["id"]):
        toc += '<li><a href="#quiz">{}</a></li>'.format(esc(t("quiz.title")))
    animated = item["id"] == "refractometre"
    anim_names = ["refraction", "reflection", "high_low", "critical", "edge", "liquid", "two"]
    labels = {k: t("les.anim." + k) for k in (
        "normal", "air", "glass", "stone", "incident", "refracted", "reflected", "total", "critical", "bright",
        "dark", "liquid", "with", "without", "iso", "aniso", "scale", "glass15")}
    labels.update({"play": t("les.anim.play"), "pause": t("les.anim.pause")})
    label_json = esc(json.dumps(labels, ensure_ascii=False))

    def anim_box(i):
        if not animated or i >= len(anim_names):
            return ""
        return ('<div class="anim" data-mode="{i}" data-labels="{lab}"><canvas width="640" height="360" role="img" '
                'aria-label="{cap}"></canvas><div class="bar"><button type="button">{pause}</button>'
                '<span class="cap">{cap}</span></div></div>').format(
            i=i, lab=label_json, cap=esc(t("les.anim.t." + anim_names[i])), pause=esc(t("les.anim.pause")))
    figures = [0]

    def figure_box(i, s):
        """The animated drawing of a step (port of the game's lesson figures), drawn on a canvas by lessonfig.js."""
        if not s.get("figure") or not re.fullmatch(r"[a-z_]+", s["figure"]):
            return ""
        figures[0] += 1
        desc = t("fig.desc." + s["figure"]) if i18n.has(req.lang, "fig.desc." + s["figure"]) else ""
        return ('<div class="lessonfig" data-fig="{fig}" data-focus="{focus}"><canvas role="img" aria-label="{label}"></canvas>'
                '<div class="bar"><button type="button" data-pause="{pause}" data-play="{play}">{pause}</button>'
                '<span class="cap">{cap}</span></div>{details}</div>').format(
            fig=esc(s["figure"]), focus=int(s.get("focus", 0)), label=esc(t("les.fig.label", n=i + 1, title=s["title"], desc=desc)),
            cap=esc(t("les.fig.caption", n=i + 1, title=s["title"])), pause=esc(t("les.anim.pause")), play=esc(t("les.anim.play")),
            details='<details class="figdesc"><summary>{}</summary><p>{}</p></details>'.format(esc(t("les.fig.describe")), esc(desc)) if desc else "")
    seen_terms = set()
    steps = "".join(
        '<section class="step" id="e{i}"><h2><span class="num">{i}</span> {title}</h2>{anim}{text}</section>'.format(
            i=i + 1, title=esc(s["title"]), anim=anim_box(i) or figure_box(i, s),
            text=markup.link_terms(markup.lesson_html(s["text"]), seen_terms))
        for i, s in enumerate(item["steps"]))
    steps += routes_learn.quiz_form(req, item["id"])
    steps += '<script src="/static/js/refraction.js" defer></script>' if animated else ""
    if figures[0]:
        steps += "".join('<script src="/static/js/{}" defer></script>'.format(f) for f in (
            "lessonfig.js", "lessonfig_figs.js", "lessonfig_figs2.js"))
    card = ""
    tool = item.get("tool") or {}
    if item["kind"] == "tool" and tool.get("name"):
        rec = "".join("<li>{}</li>".format(esc(r)) for r in tool.get("records", []))
        tips = "".join("<li>{}</li>".format(esc(r)) for r in tool.get("tips", []))
        card = (
            '<aside class="toolcard"><h2>{name}</h2><p><strong>{ml}</strong> {measures}</p>{rec}{tips}</aside>').format(
            name=esc(tool["name"]), ml=esc(t("les.measures")), measures=esc(tool.get("measures", "")),
            rec=('<h3>{}</h3><ul>{}</ul>'.format(esc(t("les.records")), rec) if rec else ""),
            tips=('<h3>{}</h3><ul>{}</ul>'.format(esc(t("les.tips")), tips) if tips else ""))
    content = (
        '<nav class="crumbs"><a href="/lecons">{back}</a></nav><h1>{title}</h1>{card}'
        '<nav class="toc" aria-label="{tl}"><strong>{tl}</strong><ol>{toc}</ol></nav>{steps}'
        '<p class="small note">{note}</p>').format(
        back=esc(t("les.back")), title=esc(item["title"]), card=card, tl=esc(t("les.toc")), toc=toc, steps=steps,
        note=esc(t("les.note")))
    return page(req, item["title"], content, section="lessons")


# ------------------------------------------------------------------ crawlers

@router.route("GET", "/robots.txt")
def robots(req):
    if not config.INDEXABLE:
        return Response("User-agent: *\nDisallow: /\n", content_type="text/plain; charset=utf-8")
    private = [p for p in views.PRIVATE_PREFIXES if p != "/dev"]
    lines = ["User-agent: *"] + ["Disallow: {}".format(p) for p in private] + ["Disallow: /en{}".format(p) for p in private] + [
        "", "Sitemap: {}/sitemap.xml".format(config.BASE_URL)]
    return Response("\n".join(lines) + "\n", content_type="text/plain; charset=utf-8")


PUBLIC_PATHS = ["/", "/encyclopedie", "/encyclopedie/formes", "/identification", "/comparer", "/glossaire", "/lecons", "/quiz", "/prix",
                "/a-propos", "/contact", "/mentions-legales", "/confidentialite"]


@router.route("GET", "/sitemap.xml")
def sitemap(req):
    """The list of public pages for search engines. Only served when the site is open to them."""
    if not config.INDEXABLE:
        raise HTTPError(404)
    paths = list(PUBLIC_PATHS)
    paths += ["/encyclopedie/" + s["slug"] for s in catalog.data()["species"]]
    paths += ["/lecons/" + it["id"] for it in catalog.lessons()]
    # every page in both languages, each one pointing to the other (hreflang)
    def entry(p):
        fr = config.BASE_URL + p
        en = config.BASE_URL + "/en" + (p if p != "/" else "")
        links = '<xhtml:link rel="alternate" hreflang="fr" href="{}"/><xhtml:link rel="alternate" hreflang="en" href="{}"/>'.format(esc(fr), esc(en))
        return "".join("<url><loc>{}</loc>{}</url>\n".format(esc(loc), links) for loc in (fr, en))
    body = ('<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">\n'
            + "".join(entry(p) for p in paths) + "</urlset>\n")
    return Response(body, content_type="application/xml; charset=utf-8")


@router.route("GET", "/favicon.ico")
def favicon(req):
    return redirect("/static/img/favicon.svg")
