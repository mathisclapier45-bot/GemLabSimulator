"""One small icon for each class of minerals: the shape of a typical crystal (or of the building block of the class), in flat tones.
Each icon is a 48 x 48 drawing made of a few facets, lit from the upper left, like the logo of the site."""
import colorsys


def _rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def _hex(rgb):
    return "#{:02x}{:02x}{:02x}".format(*[max(0, min(255, int(round(v * 255)))) for v in rgb])


def tones(base):
    """(lightest, light, mid, dark, darkest) tones of a color."""
    h, l, s = colorsys.rgb_to_hls(*_rgb(base))
    return [_hex(colorsys.hls_to_rgb(h, max(0.0, min(1.0, l * f)), s)) for f in (1.45, 1.2, 1.0, 0.78, 0.58)]


def _poly(points, fill):
    return '<polygon points="{}" fill="{}"/>'.format(" ".join("{},{}".format(x, y) for x, y in points), fill)


def _native():                                   # an atom: the class of single elements
    c = tones("#6f9bff")
    orbits = "".join('<ellipse cx="24" cy="24" rx="19" ry="7.5" transform="rotate({} 24 24)" fill="none" stroke="{}" stroke-width="2.4"/>'.format(a, col)
                     for a, col in ((0, c[1]), (60, c[2]), (120, c[3])))
    dots = "".join('<circle cx="{}" cy="{}" r="2.6" fill="{}"/>'.format(x, y, c[0]) for x, y in ((43, 24), (14.5, 40.4), (14.5, 7.6)))
    return orbits + '<circle cx="24" cy="24" r="5.5" fill="{}"/><circle cx="22.6" cy="22.6" r="2" fill="#ffffff" opacity=".7"/>'.format(c[1]) + dots


def _cube():                                     # sulfides: the cube of pyrite
    c = tones("#e0b13c")
    return (_poly([(24, 4), (42, 14), (24, 24), (6, 14)], c[0]) + _poly([(6, 14), (24, 24), (24, 44), (6, 34)], c[2]) +
            _poly([(42, 14), (24, 24), (24, 44), (42, 34)], c[4]))


def _octahedron():                               # halides: the octahedron of fluorite
    c = tones("#9a63ea")
    return (_poly([(24, 3), (5, 26), (24, 33)], c[1]) + _poly([(24, 3), (24, 33), (43, 26)], c[2]) +
            _poly([(5, 26), (24, 45), (24, 33)], c[3]) + _poly([(43, 26), (24, 45), (24, 33)], c[4]))


def _bipyramid():                                # oxides: the hexagonal barrel of corundum
    c = tones("#f0506a")
    p = [(7, 24), (16, 30), (32, 30), (41, 24)]
    top = (24, 3)
    bottom = (24, 45)
    out = ""
    for i, shade in enumerate((c[1], c[2], c[3])):
        out += _poly([top, p[i], p[i + 1]], shade)
    for i, shade in enumerate((c[3], c[4], c[4])):
        out += _poly([bottom, p[i], p[i + 1]], shade)
    return out


def _rhombohedron():                             # carbonates: the leaning cube of calcite
    c = tones("#3fbf8a")
    return (_poly([(14, 5), (42, 5), (36, 17), (8, 17)], c[0]) + _poly([(8, 17), (36, 17), (36, 44), (8, 44)], c[2]) +
            _poly([(36, 17), (42, 5), (42, 32), (36, 44)], c[4]))


def _borate():                                   # borates: the triangle of the BO3 group
    c = tones("#f08a4b")
    pts = [(24, 8), (41, 37), (7, 37)]
    lines = '<polygon points="{}" fill="none" stroke="{}" stroke-width="3" stroke-linejoin="round"/>'.format(
        " ".join("{},{}".format(x, y) for x, y in pts), c[2])
    bonds = "".join('<line x1="24" y1="27" x2="{}" y2="{}" stroke="{}" stroke-width="2.4"/>'.format(x, y, c[3]) for x, y in pts)
    return lines + bonds + "".join('<circle cx="{}" cy="{}" r="5.2" fill="{}"/>'.format(x, y, c[1]) for x, y in pts) + \
        '<circle cx="24" cy="27" r="4" fill="{}"/>'.format(c[0])


def _blades():                                   # sulfates: bladed crystals of celestine or gypsum
    c = tones("#62b6ff")
    out = ""
    for angle, shade in ((-28, c[3]), (0, c[2]), (28, c[1])):
        out += '<rect x="19" y="6" width="10" height="36" rx="2.2" transform="rotate({} 24 42)" fill="{}"/>'.format(angle, shade)
    return out + '<rect x="19" y="6" width="3.6" height="36" rx="1.6" transform="rotate(0 24 42)" fill="#ffffff" opacity=".35"/>'


def _prism():                                    # phosphates: the hexagonal prism of apatite
    c = tones("#2fc9bf")
    return (_poly([(8, 15), (24, 7), (40, 15), (32, 19), (16, 19)], c[0]) + _poly([(8, 15), (16, 19), (16, 43), (8, 39)], c[3]) +
            _poly([(16, 19), (32, 19), (32, 43), (16, 43)], c[2]) + _poly([(32, 19), (40, 15), (40, 39), (32, 43)], c[4]))


def _tetrahedron():                              # silicates: the SiO4 tetrahedron, silicon in the middle and an oxygen at each corner
    c = tones("#6f84ff")
    corners = [(24, 4), (5, 34), (43, 34), (26, 45)]
    out = (_poly([corners[0], corners[1], corners[3]], c[1]) + _poly([corners[0], corners[3], corners[2]], c[3]) +
           _poly([corners[1], corners[3], corners[2]], c[4]))
    out += "".join('<circle cx="{}" cy="{}" r="4.2" fill="{}"/><circle cx="{}" cy="{}" r="1.3" fill="#ffffff" opacity=".65"/>'.format(
        x, y, c[0], x - 1.2, y - 1.4) for x, y in corners)
    return out + '<circle cx="25" cy="30" r="3.4" fill="{}"/>'.format(c[4])


def _leaf():                                     # organic compounds: a leaf
    c = tones("#4fbf63")
    return ('<path d="M7 40 C6 19 20 6 42 6 C43 28 30 42 7 40 Z" fill="{}"/>'.format(c[2]) +
            '<path d="M7 40 C6 19 20 6 42 6 C26 14 14 26 7 40 Z" fill="{}"/>'.format(c[1]) +
            '<path d="M8 41 C18 28 28 18 41 8" fill="none" stroke="{}" stroke-width="2" stroke-linecap="round"/>'.format(c[4]) +
            '<path d="M18 30 L18 38 M24 24 L33 24 M28 19 L28 12" fill="none" stroke="{}" stroke-width="1.5" stroke-linecap="round" opacity=".8"/>'.format(c[4]))


def _flask():                                    # synthetics and imitations: a laboratory flask
    c = tones("#4dc3ff")
    glass = "M19 4 H29 V17 L41 38 C43 42 41 45 37 45 H11 C7 45 5 42 7 38 L19 17 Z"
    return ('<defs><clipPath id="flask-glass"><path d="{g}"/></clipPath></defs>'
            '<path d="{g}" fill="#ffffff" opacity=".18"/>'
            '<g clip-path="url(#flask-glass)"><rect x="0" y="29" width="48" height="20" fill="{a}"/><rect x="0" y="36" width="48" height="13" fill="{b}"/></g>'
            '<path d="{g}" fill="none" stroke="{s}" stroke-width="2.4" stroke-linejoin="round"/>'
            '<line x1="16.5" y1="4" x2="31.5" y2="4" stroke="{s}" stroke-width="2.6" stroke-linecap="round"/>'
            '<circle cx="19" cy="34" r="2" fill="#ffffff" opacity=".8"/><circle cx="27" cy="38.5" r="1.5" fill="#ffffff" opacity=".7"/>'
            '<path d="M24 22 l2.2 3.2 l-2.2 3.2 l-2.2 -3.2 Z" fill="{b}"/>').format(g=glass, a=c[1], b=c[3], s=c[0])


DRAW = {"elements-natifs": _native, "sulfures": _cube, "halogenures": _octahedron, "oxydes": _bipyramid, "carbonates": _rhombohedron,
        "borates": _borate, "sulfates": _blades, "phosphates": _prism, "silicates": _tetrahedron, "organiques": _leaf, "syntheses": _flask}


def icon(class_id, size=44):
    """The icon of a class as an inline SVG, or "" when the class has none."""
    draw = DRAW.get(class_id)
    if draw is None:
        return ""
    return '<svg class="clsicon" viewBox="0 0 48 48" width="{s}" height="{s}" aria-hidden="true" focusable="false">{body}</svg>'.format(s=size, body=draw())
