"""Original SVG drawings made from the data: crystal axes, interference figures, pleochroism colours,
colour range, UV glow. Nothing here is a photo and nothing is taken from the game's images."""
import colorsys
import math
import re

from .views import esc

# ------------------------------------------------------------------ crystal system with axes

# (a, b, c, alpha, beta, gamma): lengths are only for the drawing.
CELLS = {
    "cubic": (1.0, 1.0, 1.0, 90, 90, 90),
    "tetragonal": (1.0, 1.0, 1.55, 90, 90, 90),
    "orthorhombic": (1.0, 1.45, 0.85, 90, 90, 90),
    "monoclinic": (1.25, 1.0, 0.95, 90, 108, 90),
    "triclinic": (1.0, 1.3, 0.95, 78, 96, 108),
}
SCALE = 70
AXIS_COLORS = {"a": "#c0392b", "b": "#2e8b57", "c": "#2f5fb5"}


def _project(p, yaw=-28.0, pitch=22.0):
    x, y, z = p
    ya, pi = math.radians(yaw), math.radians(pitch)
    xr = x * math.cos(ya) - y * math.sin(ya)
    yr = x * math.sin(ya) + y * math.cos(ya)
    return (xr, -(z * math.cos(pi) + yr * math.sin(pi)))


def _cell_vectors(a, b, c, al, be, ga):
    al, be, ga = (math.radians(v) for v in (al, be, ga))
    va = (a, 0.0, 0.0)
    vb = (b * math.cos(ga), b * math.sin(ga), 0.0)
    cx = c * math.cos(be)
    cy = c * (math.cos(al) - math.cos(be) * math.cos(ga)) / math.sin(ga)
    cz = math.sqrt(max(c * c - cx * cx - cy * cy, 0.0))
    return va, vb, (cx, cy, cz)


def _add(*vs):
    return tuple(sum(v[i] for v in vs) for i in range(3))


def _line(p, q, extra=""):
    return '<line x1="{:.1f}" y1="{:.1f}" x2="{:.1f}" y2="{:.1f}" {}/>'.format(p[0], p[1], q[0], q[1], extra)


def _arrow(origin, tip, color, label):
    k = 1.0 / SCALE   # sizes below are in screen pixels
    dx, dy = tip[0] - origin[0], tip[1] - origin[1]
    n = math.hypot(dx, dy) or 1
    ux, uy = dx / n, dy / n
    head = [(tip[0], tip[1]), (tip[0] - 9 * k * ux + 4 * k * uy, tip[1] - 9 * k * uy - 4 * k * ux),
            (tip[0] - 9 * k * ux - 4 * k * uy, tip[1] - 9 * k * uy + 4 * k * ux)]
    pts = " ".join("{:.1f},{:.1f}".format(*h) for h in head)
    return ('{line}<polygon points="{pts}" fill="{c}"/>'
            '<text x="{tx:.1f}" y="{ty:.1f}" fill="{c}" font-size="15" font-weight="700" font-style="italic" '
            'text-anchor="middle">{label}</text>').format(
        line=_line(origin, tip, 'stroke="{}" stroke-width="2.4"'.format(color)), pts=pts, c=color,
        tx=tip[0] + 13 * k * ux, ty=tip[1] + 13 * k * uy + 5 * k, label=label)


def crystal_svg(system, label):
    """Unit cell and crystallographic axes of a crystal system. Returns '' when the material has no crystal system."""
    if system in CELLS:
        a, b, c, al, be, ga = CELLS[system]
        va, vb, vc = _cell_vectors(a, b, c, al, be, ga)
        o = (0.0, 0.0, 0.0)
        corners = {
            "o": o, "a": va, "b": vb, "c": vc, "ab": _add(va, vb), "ac": _add(va, vc), "bc": _add(vb, vc),
            "abc": _add(va, vb, vc)}
        pr = {k: _project(v) for k, v in corners.items()}
        edges = [("o", "a"), ("o", "b"), ("o", "c"), ("a", "ab"), ("a", "ac"), ("b", "ab"), ("b", "bc"),
                 ("c", "ac"), ("c", "bc"), ("ab", "abc"), ("ac", "abc"), ("bc", "abc")]
        hidden = {("o", "b"), ("b", "ab"), ("b", "bc")}
        body = []
        for e in edges:
            style = 'stroke="#8a97a6" stroke-width="1.4"' + (' stroke-dasharray="4 3"' if e in hidden else "")
            body.append(_line(pr[e[0]], pr[e[1]], style))
        extra = [pr[k] for k in pr]
        arrows = []
        for key, vec, col in (("a", va, AXIS_COLORS["a"]), ("b", vb, AXIS_COLORS["b"]), ("c", vc, AXIS_COLORS["c"])):
            tip = _project(tuple(v * 1.45 for v in vec))
            arrows.append(_arrow(pr["o"], tip, col, key))
            extra.append(tip)
    else:
        return ""
    return _wrap(extra, "".join(body) + "".join(arrows), label)


def hexagonal_svg(label):
    r, h = 1.0, 1.5
    base = [(r * math.cos(math.radians(60 * i)), r * math.sin(math.radians(60 * i)), 0.0) for i in range(6)]
    top = [(x, y, h) for x, y, _ in base]
    pb = [_project(p, yaw=-20, pitch=26) for p in base]
    pt = [_project(p, yaw=-20, pitch=26) for p in top]
    body = []
    for i in range(6):
        j = (i + 1) % 6
        body.append(_line(pb[i], pb[j], 'stroke="#8a97a6" stroke-width="1.4"'))
        body.append(_line(pt[i], pt[j], 'stroke="#8a97a6" stroke-width="1.4"'))
        body.append(_line(pb[i], pt[i], 'stroke="#8a97a6" stroke-width="1.4"'))
    centre = _project((0, 0, h / 2), yaw=-20, pitch=26)
    arrows, extra = [], pb + pt
    for i, key in enumerate(("a₁", "a₂", "a₃")):
        ang = math.radians(120 * i)
        tip = _project((1.3 * math.cos(ang), 1.3 * math.sin(ang), h / 2), yaw=-20, pitch=26)
        arrows.append(_arrow(centre, tip, "#c0392b" if i == 0 else ("#2e8b57" if i == 1 else "#b8860b"), key))
        extra.append(tip)
    tip = _project((0, 0, h / 2 + 1.25), yaw=-20, pitch=26)
    arrows.append(_arrow(centre, tip, AXIS_COLORS["c"], "c"))
    extra.append(tip)
    return _wrap(extra, "".join(body) + "".join(arrows), label)


def _wrap(points, inner, label):
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    pad = 26
    scale = SCALE
    minx, maxx, miny, maxy = min(xs), max(xs), min(ys), max(ys)
    w = (maxx - minx) * scale + 2 * pad
    h = (maxy - miny) * scale + 2 * pad
    return ('<svg class="figure" viewBox="0 0 {w:.0f} {h:.0f}" role="img" aria-label="{label}">'
            '<g transform="translate({tx:.1f} {ty:.1f}) scale({s})" stroke-linecap="round">{inner}</g></svg>').format(
        w=w, h=h, label=esc(label), tx=pad - minx * scale, ty=pad - miny * scale, s=scale, inner=_scale_fix(inner, scale))


def _scale_fix(inner, scale):
    # Strokes and text are drawn in drawing units; divide their sizes by the scale so they stay constant on screen.
    def stroke(m):
        return 'stroke-width="{:.3f}"'.format(float(m.group(1)) / scale)

    def dash(m):
        return 'stroke-dasharray="{:.3f} {:.3f}"'.format(float(m.group(1)) / scale, float(m.group(2)) / scale)

    def font(m):
        return 'font-size="{:.3f}"'.format(float(m.group(1)) / scale)
    inner = re.sub(r'stroke-width="([\d.]+)"', stroke, inner)
    inner = re.sub(r'stroke-dasharray="([\d.]+) ([\d.]+)"', dash, inner)
    return re.sub(r'font-size="([\d.]+)"', font, inner)


# ------------------------------------------------------------------ interference figures

RING_COLORS = ["#e6c24a", "#d8572f", "#3b6fd1", "#49b36b", "#e6c24a", "#c44a8f", "#3b6fd1"]


def interference_svg(optic_class, label):
    """Conoscopic figure: uniaxial = black cross, biaxial = two curved bars, isotropic/opaque = none."""
    if optic_class == "Uniaxe":
        rings = "".join(
            '<circle cx="100" cy="100" r="{r}" fill="none" stroke="{c}" stroke-width="5" opacity="{o:.2f}"/>'.format(
                r=18 + 13 * i, c=RING_COLORS[i % len(RING_COLORS)], o=max(0.25, 0.95 - 0.1 * i)) for i in range(7))
        figure = (rings + '<g stroke="#000" stroke-linecap="butt" filter="url(#fb)">'
                  '<line x1="100" y1="-10" x2="100" y2="210" stroke-width="14"/>'
                  '<line x1="-10" y1="100" x2="210" y2="100" stroke-width="14"/></g>')
    elif optic_class == "Biaxe":
        rings = ""
        for cx in (70, 130):
            for i in range(4):
                rings += '<circle cx="{}" cy="100" r="{}" fill="none" stroke="{}" stroke-width="5" opacity="{:.2f}"/>'.format(
                    cx, 9 + 11 * i, RING_COLORS[i % len(RING_COLORS)], 0.95 - 0.12 * i)
        for i in range(3):
            rings += '<ellipse cx="100" cy="100" rx="{}" ry="{}" fill="none" stroke="{}" stroke-width="5" opacity="{:.2f}"/>'.format(
                72 + 12 * i, 46 + 15 * i, RING_COLORS[(i + 1) % len(RING_COLORS)], 0.7 - 0.12 * i)
        figure = (rings + '<g stroke="#000" fill="none" stroke-width="12" filter="url(#fb)">'
                  '<path d="M 20 20 Q 80 100 20 180"/><path d="M 180 20 Q 120 100 180 180"/></g>')
    else:
        return ""
    return ('<svg class="figure round" viewBox="0 0 200 200" role="img" aria-label="{label}">'
            '<defs><filter id="fb"><feGaussianBlur stdDeviation="2"/></filter><clipPath id="cp"><circle cx="100" cy="100" r="96"/></clipPath>'
            '<radialGradient id="vg" cx="50%" cy="50%" r="50%"><stop offset="55%" stop-color="#000" stop-opacity="0"/>'
            '<stop offset="100%" stop-color="#000" stop-opacity=".75"/></radialGradient></defs>'
            '<circle cx="100" cy="100" r="98" fill="#10121a"/><g clip-path="url(#cp)">{fig}'
            '<circle cx="100" cy="100" r="96" fill="url(#vg)"/></g></svg>').format(label=esc(label), fig=figure)


# ------------------------------------------------------------------ colours from text

COLOR_WORDS = [
    (r"incolore|blanc", (0.93, 0.95, 0.96)), (r"rouge|rougeatr", (0.77, 0.16, 0.12)),
    (r"orang", (0.91, 0.51, 0.16)), (r"jaun|dor[e\u00e9]", (0.91, 0.77, 0.23)), (r"vert", (0.25, 0.60, 0.32)),
    (r"bleu", (0.18, 0.37, 0.71)), (r"violet|violac", (0.48, 0.29, 0.69)), (r"mauve", (0.71, 0.55, 0.82)),
    (r"pourpr", (0.56, 0.23, 0.48)), (r"rose", (0.90, 0.54, 0.65)), (r"brun|marron", (0.54, 0.35, 0.20)),
    (r"gris", (0.54, 0.56, 0.59)), (r"noir", (0.12, 0.12, 0.13)),
]


def _mix(c1, c2, t):
    return tuple(a * (1 - t) + b * t for a, b in zip(c1, c2))


def _hex(c):
    return "#{:02x}{:02x}{:02x}".format(*(max(0, min(255, int(round(v * 255)))) for v in c))


def color_of_phrase(phrase):
    """'Orang\u00e9-rouge', 'Bleu tr\u00e8s p\u00e2le', 'Jaune plus soutenu' -> a hex colour, or None."""
    text = phrase.lower()
    found = []
    for m in re.finditer("|".join("(?P<w{}>{})".format(i, rx) for i, (rx, _) in enumerate(COLOR_WORDS)), text):
        idx = int(m.lastgroup[1:])
        found.append(COLOR_WORDS[idx][1])
    if not found:
        return None
    col = found[0] if len(found) == 1 else _mix(found[0], found[-1], 0.65)
    if re.search(r"tr[e\u00e8]s p[a\u00e2]le|tr[e\u00e8]s clair", text):
        col = _mix(col, (1, 1, 1), 0.78)
    elif re.search(r"p[a\u00e2]le|clair|faible", text):
        col = _mix(col, (1, 1, 1), 0.5)
    if re.search(r"fonc|sombre|profond|intense", text):
        col = _mix(col, (0, 0, 0), 0.3)
    elif re.search(r"soutenu|vif", text):
        col = _mix(col, (0, 0, 0), 0.16)
    return _hex(col)


def pleochroism_colors(description):
    """Splits 'Vert / Rouge / Jaune-brun selon les trois axes (...)' into the colours shown by each direction."""
    head = re.split(r"selon|\(", description, maxsplit=1)[0]
    head = re.sub(r"^[^:]*:", "", head)
    parts = [p.strip(" .,;") for p in re.split(r"/|,| et ", head) if p.strip(" .,;")]
    out = []
    for p in parts:
        c = color_of_phrase(p)
        if c:
            out.append((p[:1].upper() + p[1:], c))
    return out[:3] if len(out) >= 2 else []


# ------------------------------------------------------------------ colour range (original tonal scale)

TONES = [("very_light", 0.90, 0.45), ("light", 0.80, 0.65), ("soft", 0.68, 0.85), ("medium", None, 1.0),
         ("rich", 0.40, 1.0), ("deep", 0.28, 0.95), ("dark", 0.17, 0.85)]


def tonal_range(base_hex):
    """Seven tones of the species' own colour, from very light to dark."""
    r, g, b = (int(base_hex[i:i + 2], 16) / 255 for i in (1, 3, 5))
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    mid = min(max(l, 0.38), 0.58)
    out = []
    for key, lightness, sat in TONES:
        ll = mid if lightness is None else lightness
        rr, gg, bb = colorsys.hls_to_rgb(h, ll, min(1.0, s * sat))
        out.append((key, "#{:02x}{:02x}{:02x}".format(int(rr * 255), int(gg * 255), int(bb * 255))))
    return out


# ------------------------------------------------------------------ UV glow

def uv_glow_svg(color, intensity, uid, label):
    """A small dark box with the stone glowing in the colour it shows under that UV lamp."""
    if not color or intensity <= 0:
        glow = ('<polygon points="40,20 80,20 95,40 60,78 25,40" fill="#1b1830" stroke="#3a3560" stroke-width="1.2"/>')
    else:
        a = 0.35 + 0.65 * intensity
        glow = ('<circle cx="60" cy="48" r="44" fill="url(#{u})"/>'
                '<polygon points="40,20 80,20 95,40 60,78 25,40" fill="{c}" opacity="{a:.2f}" stroke="{c}" stroke-width="1.2"/>'
                '<polygon points="40,20 60,40 25,40" fill="#fff" opacity="{f:.2f}"/>').format(
            u=uid, c=esc(color), a=a, f=0.25 * intensity)
    return ('<svg class="uvbox" viewBox="0 0 120 96" role="img" aria-label="{label}">'
            '<defs><radialGradient id="{u}"><stop offset="0%" stop-color="{c}" stop-opacity="{ga:.2f}"/>'
            '<stop offset="100%" stop-color="{c}" stop-opacity="0"/></radialGradient></defs>'
            '<rect width="120" height="96" fill="#0c0a18"/>{glow}</svg>').format(
        label=esc(label), u=uid, c=esc(color or "#000"), ga=0.55 * intensity, glow=glow)


# ------------------------------------------------------------------ dichroscope

def pleo_strength(text):
    """Key of the strength of the pleochroism, read from the words of the description."""
    from .catalog import fold
    f = fold(text)
    if re.search(r"extremement|tres marque|tres fort|tres prononce", f):
        return "very_strong"
    if re.search(r"faible a (prononce|marque|fort)", f):
        return "light_strong"
    if re.search(r"faible a modere|faible a moyen", f):
        return "light_moderate"
    if re.search(r"tres faible", f):
        return "very_light"
    if re.search(r"modere a (marque|fort|prononce)", f):
        return "moderate_strong"
    if re.search(r"faible", f):
        return "light"
    if re.search(r"modere|moyen", f):
        return "moderate"
    if re.search(r"marque|prononce|fort|trichroisme|dichroisme", f):
        return "strong" if re.search(r"marque|prononce|fort", f) else "visible"
    return "visible"


def dichroscope_svg(colors, label):
    """The two (or three) small windows of the dichroscope, each showing the colour seen in one direction."""
    n = max(1, len(colors))
    gap = 8
    w = (200 - (n - 1) * gap) / n
    wins = []
    for i, c in enumerate(colors):
        x = 10 + i * (w + gap)
        wins.append('<rect x="{x:.1f}" y="24" width="{w:.1f}" height="58" rx="4" fill="{c}"/>'
                    '<rect x="{x:.1f}" y="24" width="{w:.1f}" height="58" rx="4" fill="url(#dg)"/>'.format(x=x, w=w, c=esc(c)))
    return ('<svg class="figure dichro" viewBox="0 0 220 106" role="img" aria-label="{label}"><defs><linearGradient id="dg" x1="0" y1="0" x2="0" y2="1">'
            '<stop offset="0" stop-color="#fff" stop-opacity=".30"/><stop offset=".5" stop-color="#fff" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".18"/></linearGradient></defs>'
            '<rect x="0" y="6" width="220" height="94" rx="12" fill="#1b1f2e" stroke="#3b4260"/><rect x="6" y="14" width="208" height="78" rx="8" fill="#0e101b"/>{wins}</svg>').format(
        label=esc(label), wins="".join(wins))
