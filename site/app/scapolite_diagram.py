"""The diagram of the scapolite family: one triangle of mixtures between three pure scapolites.

The corners are the three end members (marialite, meionite, silvialite, each with its formula); a point of the triangle is a mixture, and the closer it is to
a corner the more the stone looks like that end member. The three lines from the center to the middle of each side cut the triangle in three zones, the one
where each end member is the largest part. Drawn for this site from the chemistry of the series (the formulas are facts of mineralogy); the texts come from the
translation files, and a table below the drawing says the same in words."""
from .views import esc

W, H = 520, 500
TOP, LEFT, RIGHT = (260.0, 88.0), (60.0, 434.0), (460.0, 434.0)
CENTER = ((TOP[0] + LEFT[0] + RIGHT[0]) / 3.0, (TOP[1] + LEFT[1] + RIGHT[1]) / 3.0)

POLES = (("meionite", "Ca₄Al₆Si₆O₂₄(CO₃)"), ("marialite", "Na₄Al₃Si₉O₂₄Cl"), ("silvialite", "Ca₄Al₆Si₆O₂₄(SO₄)"))


def _mix(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def _fmt(p):
    return "{:.1f} {:.1f}".format(p[0], p[1])


def _edge(a, b, normal, steps, title, angle):
    """The graduations of one side: a tick and a number every 20 %, then the title of the scale, outside the triangle. `a` is the 0 %, `b` the 100 %."""
    out = ""
    for value in steps:
        p = _mix(a, b, value / 100.0)
        q = (p[0] + normal[0] * 8, p[1] + normal[1] * 8)
        n = (p[0] + normal[0] * 20, p[1] + normal[1] * 20 + (4 if abs(normal[1]) < 0.9 else 8))
        anchor = "end" if normal[0] < -0.1 else ("start" if normal[0] > 0.1 else "middle")
        out += ('<line class="gd-tick" x1="{}" y1="{}" x2="{}" y2="{}"/><text class="gd-num" x="{}" y="{}" text-anchor="{}">{}</text>').format(
            round(p[0], 1), round(p[1], 1), round(q[0], 1), round(q[1], 1), round(n[0], 1), round(n[1], 1), anchor, value)
    mid = _mix(a, b, 0.5)
    c = (mid[0] + normal[0] * 56, mid[1] + normal[1] * 56 + (4 if abs(normal[1]) < 0.9 else 0))
    out += '<text class="gd-axis" x="{}" y="{}" text-anchor="middle" transform="rotate({} {} {})">{}</text>'.format(
        round(c[0], 1), round(c[1], 1), angle, round(c[0], 1), round(c[1], 1), esc(title))
    return out


def _svg(t):
    sides = (
        '<path class="gd-line" d="M {} L {} L {} Z" fill="none"/>'.format(_fmt(TOP), _fmt(RIGHT), _fmt(LEFT)))
    zones = "".join('<line class="gd-zone" x1="{}" y1="{}" x2="{}" y2="{}"/>'.format(
        round(CENTER[0], 1), round(CENTER[1], 1), round(m[0], 1), round(m[1], 1)) for m in (_mix(LEFT, TOP, 0.5), _mix(TOP, RIGHT, 0.5), _mix(RIGHT, LEFT, 0.5)))
    # the graduations: meionite on the left side (0 at the marialite corner), silvialite on the right side (0 at the apex), marialite on the bottom (0 at the silvialite corner)
    ticks = (_edge(LEFT, TOP, (-0.866, -0.5), (20, 40, 60, 80), t("scap.axis.meionite"), -60) +
             _edge(TOP, RIGHT, (0.866, -0.5), (20, 40, 60, 80), t("scap.axis.silvialite"), 60) +
             _edge(RIGHT, LEFT, (0.0, 1.0), (20, 40, 60, 80), t("scap.axis.marialite"), 0))
    labels = ('<text class="gd-title" x="{tx}" y="26" text-anchor="middle">{m}</text><text class="gd-formula" x="{tx}" y="46" text-anchor="middle">{mf}</text>'
              '<text class="gd-title" x="8" y="484" text-anchor="start">{a}</text><text class="gd-formula" x="8" y="500" text-anchor="start" dy="-2">{af}</text>'
              '<text class="gd-title" x="512" y="484" text-anchor="end">{s}</text><text class="gd-formula" x="512" y="500" text-anchor="end" dy="-2">{sf}</text>').format(
        tx=TOP[0], m=esc(t("scap.meionite")), mf=esc(POLES[0][1]), a=esc(t("scap.marialite")), af=esc(POLES[1][1]), s=esc(t("scap.silvialite")), sf=esc(POLES[2][1]))
    label = t("scap.label")
    return ('<svg class="gdia sdia" viewBox="0 0 {w} {h}" role="img" aria-label="{l}" focusable="false"><title>{l}</title>{body}</svg>').format(
        w=W, h=H, l=esc(label), body=zones + sides + ticks + labels)


def section(t):
    """The block of the family page: explanation, the triangle, and a table with the same content in words."""
    rows = []
    for key, formula in POLES:
        rows.append('<tr><th scope="row">{}</th><td>{}</td><td>{}</td><td>{}</td></tr>'.format(
            esc(t("scap." + key)), esc(formula), esc(t("scap.metal." + key)), esc(t("scap.anion." + key))))
    table = ('<table class="props gtable"><caption class="sr-only">{cap}</caption><thead><tr><th>{c1}</th><th>{c2}</th><th>{c3}</th><th>{c4}</th></tr></thead>'
             '<tbody>{rows}</tbody></table>').format(cap=esc(t("scap.table.caption")), c1=esc(t("scap.col.pole")), c2=esc(t("garnet.col.formula")),
                                                      c3=esc(t("scap.col.metal")), c4=esc(t("scap.col.anion")), rows="".join(rows))
    return ('<h2>{h}</h2><p>{lead}</p><div class="gfigure"><figure><figcaption>{cap}</figcaption>{svg}</figure></div><p class="small">{read}</p>{table}').format(
        h=esc(t("scap.title")), lead=esc(t("scap.lead")), cap=esc(t("scap.caption")), svg=_svg(t), read=esc(t("scap.read")), table=table)
