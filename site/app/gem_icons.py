"""The logo of a species: its shape and its cut, drawn as a small SVG gem in the colours of the stone (original drawings, not game images).

The rules are the ones of GemIconStyle.cs in the game (user, 2026-11-20): the logo does not change the stones, only the picture of the species.
  beryls: emerald cut in the colour of the variety (the pezzottaite: cabochon) - corundums: oval - garnets: octagon - diamond, zircon, moissanite, YAG, GGG, CZ: round brilliant
  scapolites: trilliant - quartz: marquise (dutch cut), cabochon for the polycrystalline ones - fluorites: a cube - chrysoberyl and zoisite: cushion - opaque stones: cabochon
The other species keep the shape and the cut the game data gives them."""
import unicodedata
from html import escape as esc

BERYLS = {"Emeraude", "Aigue_Marine", "Beryl_Bleu", "Beryl_Vert", "Beryl_Rouge", "Goshenite", "Heliodore", "Morganite"}
QUARTZ = {"Quartz", "Amethyste", "Ametrine", "Citrine", "Prasiolite", "Quartz_Fume", "Quartz_Rose"}
ROUND = {"YAG", "GGG", "CZ", "Titanate_Strontium"}
POLY_QUARTZ = {"Agate", "Cornaline", "Oeil_de_Tigre", "Jaspe", "Onyx", "Aventurine"}


def plain(text):
    """A word without accents and in lower case: the shapes and cuts are compared in this form."""
    return "".join(ch for ch in unicodedata.normalize("NFKD", text or "") if not unicodedata.combining(ch)).lower()


def _opaque(transparency):
    t = plain(transparency)
    return t.startswith("opaque") and " a " not in t and "semi-opaque" not in t


def style_for(s):
    """(shape, cut, habit) of the logo of a species (a dict of the game data)."""
    name = s["id"]
    shape, cut = plain(s.get("shape", "")), plain(s.get("cutStyle", ""))
    cabochon = lambda sh: (sh or "ovale", "cabochon", None)
    if name.startswith("Fluorite"):
        return ("carre", "brillant", "cube")
    if name == "Pezzottaite":
        return cabochon("ovale")
    if name in BERYLS:
        return ("rectangle a pans coupes", "a degres", None)
    if name in QUARTZ and not s.get("isPolycrystalline"):
        return ("marquise", "brillant", None)
    if name.startswith("Scapolite"):
        return ("triangle", "brillant", None)
    if name.startswith("Chrysoberyl") or name in ("Zoisite", "Tanzanite"):
        return ("coussin", "mixte", None)
    if name.startswith("Corindon") or name.startswith("Saphir") or name == "Padparadscha":
        return ("ovale", "mixte", None)
    if name.startswith("Grenat"):
        return ("octogone", "mixte", None)
    if name.startswith("Diamant") or name.startswith("Moissanite") or name.startswith("Zircon") or name in ROUND:
        return ("rond", "brillant", None)
    if name in POLY_QUARTZ or _opaque(s.get("transparency")):
        return cabochon(shape if cut == "cabochon" else "ovale")
    return (shape, cut, None)


# ------------------------------------------------------------------ drawings (viewBox 48 x 48)

STROKE = 'stroke="rgba(0,0,0,.35)" stroke-width="1" stroke-linejoin="round"'


def _poly(points, fill, opacity=None, stroke=False):
    return '<polygon points="{}" fill="{}"{}{}/>'.format(
        " ".join("{},{}".format(x, y) for x, y in points), fill, ' opacity="{}"'.format(opacity) if opacity else "", " " + STROKE if stroke else "")


def _line(a, b, color, opacity):
    return '<line x1="{}" y1="{}" x2="{}" y2="{}" stroke="{}" stroke-opacity="{}" stroke-width="1"/>'.format(a[0], a[1], b[0], b[1], color, opacity)


def _outline_with_facets(outline_svg, table, corners, girdle, c, f):
    """A brilliant: the outline, a table, and the facets joining the corners of the table to points of the girdle."""
    out = outline_svg
    out += _poly(table, f, 0.55)
    for a, b in zip(corners, girdle):
        out += _line(a, b, f, 0.7)
    out += _poly(table[:3], "#fff", 0.18)
    out += '<polygon points="{}" fill="none" stroke="{}" stroke-opacity=".55" stroke-width="1"/>'.format(" ".join("{},{}".format(x, y) for x, y in table), f)
    return out


def _regular(cx, cy, r, n, start=-90):
    import math
    return [(round(cx + r * math.cos(math.radians(start + i * 360.0 / n)), 1), round(cy + r * math.sin(math.radians(start + i * 360.0 / n)), 1)) for i in range(n)]


def _round(c, f):
    outer = '<circle cx="24" cy="24" r="20" fill="{}" {}/>'.format(c, STROKE)
    table = _regular(24, 24, 9, 8, -22.5)
    girdle = _regular(24, 24, 20, 8, -22.5)
    return _outline_with_facets(outer, table, table, girdle, c, f)


def _oval(c, f):
    outer = '<ellipse cx="24" cy="24" rx="16" ry="21" fill="{}" {}/>'.format(c, STROKE)
    table = [(24, 13), (30, 18), (30, 30), (24, 35), (18, 30), (18, 18)]
    girdle = [(24, 3), (37, 14), (38, 34), (24, 45), (10, 34), (11, 14)]
    return _outline_with_facets(outer, table, table, girdle, c, f)


def _cabochon(c, f, shape):
    if shape == "rond":
        body = '<circle cx="24" cy="24" r="19" fill="{}" {}/><ellipse cx="18" cy="17" rx="6" ry="4" fill="#fff" opacity=".35" transform="rotate(-30 18 17)"/>'.format(c, STROKE)
        return body
    if shape in ("coussin", "carre", "rectangle"):
        body = '<rect x="5" y="7" width="38" height="34" rx="13" fill="{}" {}/>'.format(c, STROKE)
        return body + '<ellipse cx="17" cy="17" rx="7" ry="4" fill="#fff" opacity=".32" transform="rotate(-25 17 17)"/>'
    if shape == "baguette":
        body = '<rect x="13" y="4" width="22" height="40" rx="10" fill="{}" {}/>'.format(c, STROKE)
        return body + '<ellipse cx="20" cy="14" rx="3" ry="6" fill="#fff" opacity=".32"/>'
    body = '<ellipse cx="24" cy="24" rx="20" ry="15" fill="{}" {}/>'.format(c, STROKE)
    return body + '<ellipse cx="16" cy="17" rx="7" ry="3.6" fill="#fff" opacity=".35" transform="rotate(-18 16 17)"/>' + \
        '<ellipse cx="24" cy="24" rx="20" ry="15" fill="{}" opacity=".12" transform="translate(0 3) scale(1 .8)"/>'.format(f)


def _emerald(c, f):
    outer = _poly([(16, 4), (32, 4), (39, 11), (39, 37), (32, 44), (16, 44), (9, 37), (9, 11)], c, None, True)
    steps = ""
    for inset, op in ((4, ".55"), (8, ".5"), (12, ".6")):
        pts = [(16 + inset * 0.2, 4 + inset), (32 - inset * 0.2, 4 + inset), (39 - inset, 11 + inset * 0.4), (39 - inset, 37 - inset * 0.4),
               (32 - inset * 0.2, 44 - inset), (16 + inset * 0.2, 44 - inset), (9 + inset, 37 - inset * 0.4), (9 + inset, 11 + inset * 0.4)]
        steps += '<polygon points="{}" fill="none" stroke="{}" stroke-opacity="{}" stroke-width="1.2"/>'.format(" ".join("{:.1f},{:.1f}".format(x, y) for x, y in pts), f, op)
    table = _poly([(19, 16), (29, 16), (29, 32), (19, 32)], f, 0.5)
    corners = "".join(_line(a, b, f, 0.6) for a, b in (((16, 4), (21, 11)), ((32, 4), (27, 11)), ((16, 44), (21, 37)), ((32, 44), (27, 37))))
    return outer + steps + table + corners + _poly([(16, 4), (32, 4), (27, 11), (21, 11)], "#fff", 0.2)


def _cushion(c, f):
    outer = '<rect x="5" y="5" width="38" height="38" rx="12" fill="{}" {}/>'.format(c, STROKE)
    table = [(17, 14), (31, 14), (34, 17), (34, 31), (31, 34), (17, 34), (14, 31), (14, 17)]
    girdle = [(17, 6), (31, 6), (42, 17), (42, 31), (31, 42), (17, 42), (6, 31), (6, 17)]
    return _outline_with_facets(outer, table, table, girdle, c, f)


def _octagon(c, f):
    outer = _poly([(16, 4), (32, 4), (44, 16), (44, 32), (32, 44), (16, 44), (4, 32), (4, 16)], c, None, True)
    steps = ""
    for pts, op in ((((17, 9), (31, 9), (39, 17), (39, 31), (31, 39), (17, 39), (9, 31), (9, 17)), ".5"), (((19, 14), (29, 14), (34, 19), (34, 29), (29, 34), (19, 34), (14, 29), (14, 19)), ".6")):
        steps += '<polygon points="{}" fill="none" stroke="{}" stroke-opacity="{}" stroke-width="1.2"/>'.format(" ".join("{},{}".format(x, y) for x, y in pts), f, op)
    table = _poly([(21, 17), (27, 17), (31, 21), (31, 27), (27, 31), (21, 31), (17, 27), (17, 21)], f, 0.5)
    corners = "".join(_line(a, b, f, 0.6) for a, b in (((16, 4), (19, 14)), ((32, 4), (29, 14)), ((44, 16), (34, 19)), ((44, 32), (34, 29)), ((32, 44), (29, 34)), ((16, 44), (19, 34)), ((4, 32), (14, 29)), ((4, 16), (14, 19))))
    return outer + steps + table + corners


def _trilliant(c, f):
    outer = '<path d="M24 4 Q38 26 45 40 Q24 46 3 40 Q10 26 24 4 Z" fill="{}" {}/>'.format(c, STROKE)
    table = [(24, 17), (34, 35), (14, 35)]
    girdle = [(24, 4), (45, 40), (3, 40)]
    inner = _poly(table, f, 0.5)
    lines = "".join(_line(a, b, f, 0.7) for a, b in zip(table, girdle))
    mids = "".join(_line(a, b, f, 0.45) for a, b in (((24, 17), (38, 26)), ((24, 17), (12, 26)), ((34, 35), (24, 43)), ((14, 35), (24, 43)), ((34, 35), (45, 40)), ((14, 35), (3, 40))))
    return outer + inner + lines + mids + _poly([(24, 4), (31, 15), (17, 15)], "#fff", 0.2)


def _marquise(c, f):
    outer = '<path d="M24 2 C41 13 41 35 24 46 C7 35 7 13 24 2 Z" fill="{}" {}/>'.format(c, STROKE)
    table = [(24, 12), (31, 24), (24, 36), (17, 24)]
    inner = _poly(table, f, 0.5)
    lines = "".join(_line(a, b, f, 0.7) for a, b in (((24, 12), (24, 2)), ((31, 24), (36, 24)), ((24, 36), (24, 46)), ((17, 24), (12, 24)), ((24, 12), (31, 9)), ((24, 12), (17, 9)), ((24, 36), (31, 39)), ((24, 36), (17, 39))))
    return outer + inner + lines + _poly([(24, 2), (31, 10), (17, 10)], "#fff", 0.18)


def _cube(c, f):
    return (_poly([(24, 3), (43, 13), (24, 23), (5, 13)], f, 0.95, True) + _poly([(5, 13), (24, 23), (24, 45), (5, 35)], c, None, True) +
            _poly([(24, 23), (43, 13), (43, 35), (24, 45)], c, None, True) + _poly([(24, 23), (43, 13), (43, 35), (24, 45)], "#000", 0.22) +
            _poly([(5, 13), (24, 23), (24, 45), (5, 35)], "#000", 0.08) + _poly([(24, 3), (43, 13), (24, 23), (5, 13)], c, 0.35))


def _baguette(c, f):
    outer = _poly([(14, 4), (34, 4), (38, 8), (38, 40), (34, 44), (14, 44), (10, 40), (10, 8)], c, None, True)
    return outer + _poly([(16, 10), (32, 10), (32, 38), (16, 38)], f, 0.45) + _poly([(14, 4), (34, 4), (32, 10), (16, 10)], "#fff", 0.2)


def _default(c, f):
    return (_poly([(12, 6), (36, 6), (45, 18), (24, 43), (3, 18)], c, None, True) + _poly([(12, 6), (24, 18), (3, 18)], f, 0.55) +
            _poly([(36, 6), (24, 18), (45, 18)], f, 0.30) + _poly([(12, 6), (36, 6), (24, 18)], f, 0.80) + _poly([(3, 18), (24, 18), (24, 43)], "#000", 0.12))


def draw(color, facet, size, style):
    """The logo SVG for (shape, cut, habit)."""
    c = esc(color or "#888888")
    f = esc(facet or "#dddddd")
    shape, cut, habit = style if style else ("", "", None)
    shape, cut = plain(shape), plain(cut)
    if habit == "cube":
        body = _cube(c, f)
    elif cut == "cabochon":
        body = _cabochon(c, f, shape)
    elif shape == "rond":
        body = _round(c, f)
    elif shape == "ovale":
        body = _oval(c, f)
    elif shape == "rectangle a pans coupes":
        body = _emerald(c, f)
    elif shape == "coussin":
        body = _cushion(c, f)
    elif shape == "octogone":
        body = _octagon(c, f)
    elif shape == "triangle":
        body = _trilliant(c, f)
    elif shape == "marquise":
        body = _marquise(c, f)
    elif shape == "baguette":
        body = _baguette(c, f)
    elif shape in ("carre", "rectangle"):
        body = _cushion(c, f)
    else:
        body = _default(c, f)
    return '<svg class="gem" viewBox="0 0 48 48" width="{s}" height="{s}" aria-hidden="true" focusable="false">{b}</svg>'.format(s=size, b=body)
