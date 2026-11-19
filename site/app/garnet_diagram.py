"""The diagram of the garnet family: two triangles of mixtures, one for each group (pyralspites and ugrandites).

Each corner of a triangle is a pure garnet (a card with its name, its formula and its varieties); each side is a series of mixtures:
a full line when every proportion exists, a dashed line when the mixture is limited. Drawn for this site, from the chemistry of the garnets
(the formulas and the groups are facts of mineralogy). The texts come from the translation files; a table below the drawing says the same in words."""
from . import catalog
from .views import esc

CARD_W, CARD_H = 148, 92


def _card(cx, cy, title, formula, chips, color, href=None):
    """A card centered on (cx, cy): title, formula, then the varieties with a color dot."""
    x, y = cx - CARD_W / 2.0, cy - CARD_H / 2.0
    body = ('<rect class="gd-card" x="{x}" y="{y}" width="{w}" height="{h}" rx="12"/>'
            '<rect x="{x}" y="{y}" width="6" height="{h}" rx="3" fill="{color}"/>'
            '<text class="gd-title" x="{tx}" y="{ty}" text-anchor="middle">{title}</text>'
            '<text class="gd-formula" x="{tx}" y="{fy}" text-anchor="middle">{formula}</text>').format(
        x=x, y=y, w=CARD_W, h=CARD_H, color=color, tx=cx + 3, ty=y + 24, fy=y + 43, title=esc(title), formula=esc(formula))
    for i, (name, dot) in enumerate(chips):
        row = y + 62 + 16 * i
        body += ('<circle cx="{cx}" cy="{cy}" r="4.6" fill="{dot}"/><text class="gd-chip" x="{tx}" y="{ty}">{name}</text>').format(
            cx=x + 28, cy=row - 4, dot=dot, tx=x + 38, ty=row, name=esc(name))
    if href:
        return '<a href="{}">{}</a>'.format(esc(href), body)
    return body


def _species_href(species_id):
    """The page of a species of the encyclopedia, or None when the encyclopedia does not have it (yet)."""
    slug = species_id.lower().replace("_", "-")
    return "/encyclopedie/" + slug if slug in catalog.data()["by_slug"] else None


def _pill(center, lines, dots, width=104, species=()):
    """A small label on a side of a triangle: the names of the mixtures that sit there (each one is a link when the species exists)."""
    cx, cy = center
    h = 14 + 17 * len(lines)
    out = '<rect class="gd-pill" x="{x}" y="{y}" width="{w}" height="{h}" rx="9"/>'.format(x=cx - width / 2.0, y=cy - h / 2.0, w=width, h=h)
    for i, (text, dot) in enumerate(zip(lines, dots)):
        row = cy - h / 2.0 + 20 + 17 * i
        item = '<circle cx="{}" cy="{}" r="4.2" fill="{}"/><text class="gd-chip" x="{}" y="{}">{}</text>'.format(cx - width / 2.0 + 14, row - 4, dot, cx - width / 2.0 + 24, row, esc(text))
        href = _species_href(species[i]) if i < len(species) and species[i] else None
        out += '<a href="{}">{}</a>'.format(esc(href), item) if href else item
    return out


def _line(a, b, limited):
    return '<line class="gd-line{}" x1="{}" y1="{}" x2="{}" y2="{}"/>'.format(" limited" if limited else "", a[0], a[1], b[0], b[1])


def _svg(label, body):
    return ('<svg class="gdia" viewBox="0 0 460 380" role="img" aria-label="{}" focusable="false"><title>{}</title>{}</svg>').format(esc(label), esc(label), body)


def _slug(species_id):
    return "/encyclopedie/" + species_id.lower().replace("_", "-")


# Colors of the varieties (flat, from the stones themselves).
C = {"pyrope": "#b0203f", "chromifere": "#e0282f", "almandin": "#7a1236", "spessartite": "#f08a2a", "rhodolite": "#c4366f", "malaya": "#f27a55",
     "change": "#5d9fb5", "grossulaire": "#c9d47f", "hessonite": "#cf7d2c", "tsavorite": "#1fae4f", "mali": "#b9c866", "andradite": "#d8c84a",
     "demantoide": "#5cc83e", "topazolite": "#f3dc3a", "uvarovite": "#1d7d3d"}


def pyralspite_svg(t):
    top, left, right = (230, 62), (78, 312), (382, 312)
    body = (_line(top, left, False) + _line(top, right, False) + _line(left, right, False) +
            _pill((154, 187), [t("garnet.rhodolite")], [C["rhodolite"]], 104, ["Grenat_Rhodolite"]) +
            _pill((230, 312), [t("garnet.malaya"), t("garnet.change")], [C["malaya"], C["change"]], 140, ["Grenat_Malaya", "Grenat_Change_Couleur"]) +
            _card(top[0], top[1], t("garnet.almandin"), "Fe₃Al₂(SiO₄)₃", [], C["almandin"], _slug("Grenat_Almandin")) +
            _card(left[0], left[1], t("garnet.pyrope"), "Mg₃Al₂(SiO₄)₃", [(t("garnet.chromifere"), C["chromifere"])], C["pyrope"], _slug("Grenat_Pyrope")) +
            _card(right[0], right[1], t("garnet.spessartite"), "Mn₃Al₂(SiO₄)₃", [], C["spessartite"], _slug("Grenat_Spessartite")))
    return _svg(t("garnet.pyralspite.label"), body)


def ugrandite_svg(t):
    top, left, right = (230, 62), (78, 312), (382, 312)
    body = (_line(top, left, True) + _line(top, right, True) + _line(left, right, False) +
            _pill((230, 312), [t("garnet.mali")], [C["mali"]], 140, ["Grenat_Mali"]) +
            _card(top[0], top[1], t("garnet.uvarovite"), "Ca₃Cr₂(SiO₄)₃", [], C["uvarovite"], _slug("Grenat_Uvarovite")) +
            _card(left[0], left[1], t("garnet.grossular"), "Ca₃Al₂(SiO₄)₃", [(t("garnet.hessonite"), C["hessonite"]), (t("garnet.tsavorite"), C["tsavorite"])], C["grossulaire"], _species_href("Grenat_Grossulaire")) +
            _card(right[0], right[1], t("garnet.andradite"), "Ca₃Fe₂(SiO₄)₃", [(t("garnet.demantoid"), C["demantoide"]), (t("garnet.topazolite"), C["topazolite"])], C["andradite"], _species_href("Grenat_Andradite")))
    return _svg(t("garnet.ugrandite.label"), body)


def section(t):
    """The whole block of the family page: explanation, the two triangles, the legend, and a table with the same content in words."""
    variety_species = {"chromifere": "Grenat_Pyrope_Chromifere", "hessonite": "Grenat_Hessonite", "tsavorite": "Grenat_Tsavorite",
                       "demantoid": "Grenat_Demantoide", "topazolite": "Grenat_Topazolite"}

    def variety(v):
        href = _species_href(variety_species.get(v, ""))
        return '<a href="{}">{}</a>'.format(esc(href), esc(t("garnet." + v))) if href else esc(t("garnet." + v))

    rows = [("pyralspite", "pyrope", "Mg₃Al₂(SiO₄)₃", ["chromifere"], "Grenat_Pyrope"),
            ("pyralspite", "almandin", "Fe₃Al₂(SiO₄)₃", [], "Grenat_Almandin"),
            ("pyralspite", "spessartite", "Mn₃Al₂(SiO₄)₃", [], "Grenat_Spessartite"),
            ("ugrandite", "grossular", "Ca₃Al₂(SiO₄)₃", ["hessonite", "tsavorite"], "Grenat_Grossulaire"),
            ("ugrandite", "andradite", "Ca₃Fe₂(SiO₄)₃", ["demantoid", "topazolite"], "Grenat_Andradite"),
            ("ugrandite", "uvarovite", "Ca₃Cr₂(SiO₄)₃", [], "Grenat_Uvarovite")]
    lines = []
    for group, key, formula, varieties, species in rows:
        name = esc(t("garnet." + key))
        if species and _species_href(species):
            name = '<a href="{}">{}</a>'.format(esc(_species_href(species)), name)
        lines.append("<tr><th scope=\"row\">{}</th><td>{}</td><td>{}</td><td>{}</td></tr>".format(
            name, esc(formula), esc(t("garnet.group." + group)), esc(", ".join(t("garnet." + v) for v in varieties)) or "—"))
    table = ('<table class="props gtable"><caption class="sr-only">{cap}</caption><thead><tr><th>{c1}</th><th>{c2}</th><th>{c3}</th><th>{c4}</th></tr></thead>'
             '<tbody>{rows}</tbody></table>').format(cap=esc(t("garnet.table.caption")), c1=esc(t("garnet.col.pole")), c2=esc(t("garnet.col.formula")),
                                                      c3=esc(t("garnet.col.group")), c4=esc(t("garnet.col.varieties")), rows="".join(lines))
    legend = ('<ul class="glegend"><li><svg viewBox="0 0 40 8" width="40" height="8" aria-hidden="true"><line class="gd-line" x1="2" y1="4" x2="38" y2="4"/></svg> {a}</li>'
              '<li><svg viewBox="0 0 40 8" width="40" height="8" aria-hidden="true"><line class="gd-line limited" x1="2" y1="4" x2="38" y2="4"/></svg> {b}</li>'
              '<li><span class="gdot"></span> {c}</li></ul>').format(a=esc(t("garnet.legend.continuous")), b=esc(t("garnet.legend.limited")), c=esc(t("garnet.legend.variety")))
    return ('<h2>{h}</h2><p>{lead}</p><div class="gfigure"><figure><figcaption>{g1}</figcaption>{s1}</figure><figure><figcaption>{g2}</figcaption>{s2}</figure></div>'
            '{legend}<p class="small">{between}</p>{table}').format(
        h=esc(t("garnet.title")), lead=esc(t("garnet.lead")), g1=esc(t("garnet.pyralspite.caption")), g2=esc(t("garnet.ugrandite.caption")),
        s1=pyralspite_svg(t), s2=ugrandite_svg(t), legend=legend, between=esc(t("garnet.between")), table=table)
