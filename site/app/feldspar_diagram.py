"""The diagram of the feldspar family: one triangle of mixtures between the three end members (orthoclase K, albite Na, anorthite Ca).

The side albite - anorthite is the plagioclase series (every proportion exists: oligoclase, andesine, labradorite, bytownite); the side orthoclase - albite is the alkali feldspars
(a continuous mixture only at high temperature, which is why moonstone shows two intergrown feldspars); the mixtures with the third pole are limited. The gem names sit on the
sides where they belong. Drawn for this site from the chemistry of the feldspars (the formulas and the series are facts of mineralogy); a table below says the same in words."""
from . import garnet_diagram as g
from .views import esc

TOP, LEFT, RIGHT = (230, 62), (78, 312), (382, 312)


def _svg(t):
    body = (g._line(TOP, LEFT, True) + g._line(TOP, RIGHT, False) + g._line(LEFT, RIGHT, True) +
            g._pill((154, 187), [t("feld.moonstone"), t("feld.amazonite")], ["#c9d6e8", "#35b9a3"], 130, ["Pierre_De_Lune", "Amazonite"]) +
            g._pill((306, 187), [t("feld.sunstone"), t("feld.labradorite")], ["#e8923c", "#4f6fa8"], 130, ["Pierre_Soleil", "Labradorite"]) +
            g._card(TOP[0], TOP[1], t("feld.albite"), "NaAlSi₃O₈", [], "#d9dde6", None) +
            g._card(LEFT[0], LEFT[1], t("feld.orthoclase"), "KAlSi₃O₈", [(t("feld.microcline"), "#35b9a3"), (t("feld.adularia"), "#c9d6e8")], "#e0b07a", g._species_href("Orthose")) +
            g._card(RIGHT[0], RIGHT[1], t("feld.anorthite"), "CaAl₂Si₂O₈", [], "#bfc7d6", None))
    return g._svg(t("feld.label"), body)


def section(t):
    """The block of the family page: explanation, the triangle, the legend and a table with the same content in words."""
    rows = [("albite", "NaAlSi₃O₈", "sodium", ""), ("orthoclase", "KAlSi₃O₈", "potassium", "microcline, adularia"), ("anorthite", "CaAl₂Si₂O₈", "calcium", "")]
    lines = "".join('<tr><th scope="row">{}</th><td>{}</td><td>{}</td><td>{}</td></tr>'.format(esc(t("feld." + k)), esc(f), esc(t("feld.metal." + m)), esc(extra)) for k, f, m, extra in rows)
    table = ('<table class="props gtable"><caption class="sr-only">{cap}</caption><thead><tr><th>{c1}</th><th>{c2}</th><th>{c3}</th><th>{c4}</th></tr></thead><tbody>{rows}</tbody></table>').format(
        cap=esc(t("feld.table.caption")), c1=esc(t("scap.col.pole")), c2=esc(t("garnet.col.formula")), c3=esc(t("scap.col.metal")), c4=esc(t("garnet.col.varieties")), rows=lines)
    legend = ('<ul class="glegend"><li><svg viewBox="0 0 40 8" width="40" height="8" aria-hidden="true"><line class="gd-line" x1="2" y1="4" x2="38" y2="4"/></svg> {a}</li>'
              '<li><svg viewBox="0 0 40 8" width="40" height="8" aria-hidden="true"><line class="gd-line limited" x1="2" y1="4" x2="38" y2="4"/></svg> {b}</li></ul>').format(
        a=esc(t("garnet.legend.continuous")), b=esc(t("garnet.legend.limited")))
    return ('<h2>{h}</h2><p>{lead}</p><div class="gfigure"><figure><figcaption>{cap}</figcaption>{svg}</figure></div>{legend}<p class="small">{read}</p>{table}').format(
        h=esc(t("feld.title")), lead=esc(t("feld.lead")), cap=esc(t("feld.caption")), svg=_svg(t), legend=legend, read=esc(t("feld.read")), table=table)
