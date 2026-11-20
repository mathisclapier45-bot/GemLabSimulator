"""The shapes and the cut styles of the stone preview of a species page (two drop-down menus, not a wall of buttons).

A cut style is drawn from one of the meshes exported from the game (static/data/meshes): `mesh` is that cut, `height` scales the dome of a cabochon (low, medium, high),
`shapes` lists the shapes the style belongs to (none: every shape the mesh exists for). The square diamond cuts of the chart (table, French, quadrillion, Peruzzi, Brazilian,
Lisbon, antique cushion, crushed ice, Barion, Flanders...) are listed by name; their exact facet geometry is not modelled yet, so they are drawn with the nearest cut
(`near`) and the page says so."""

SHAPES = ["Rond", "Ovale", "Coussin", "Carre", "Rectangle", "Emeraude", "Octogone", "Baguette", "Poire", "Marquise", "Shield", "Triangle", "Coeur"]

# (key, group, mesh, height, shapes, near): near is the name of the cut it is drawn with, for the ones that are not modelled
STYLES = [
    ("brillant", "base", "brillant", 1, None, None), ("mixte", "base", "mixte", 1, None, None), ("a-degres", "base", "a-degres", 1, None, None),
    ("ascher", "base", "ascher", 1, None, None), ("princesse", "base", "princesse", 1, None, None), ("ciseaux", "base", "ciseaux", 1, None, None), ("radiant", "base", "radiant", 1, None, None),
    ("cabochon-bas", "cabochon", "cabochon", 0.55, None, None), ("cabochon-moyen", "cabochon", "cabochon", 1, None, None), ("cabochon-haut", "cabochon", "cabochon", 1.6, None, None),
    ("table", "square", "a-degres", 1, ["Carre", "Rectangle"], "a-degres"), ("french", "square", "a-degres", 1, ["Carre"], "a-degres"),
    ("quadrillion", "square", "brillant", 1, ["Carre", "Rectangle"], "brillant"), ("french-corner-princess", "square", "princesse", 1, ["Carre"], "princesse"),
    ("bezel-corner-princess", "square", "princesse", 1, ["Carre"], "princesse"), ("peruzzi", "square", "brillant", 1, ["Coussin"], "brillant"),
    ("brazilian", "square", "brillant", 1, ["Coussin"], "brillant"), ("lisbon", "square", "brillant", 1, ["Coussin"], "brillant"),
    ("antique-cushion", "square", "brillant", 1, ["Coussin"], "brillant"), ("crushed-ice", "square", "ciseaux", 1, ["Coussin"], "ciseaux"),
    ("carre-step", "square", "a-degres", 1, ["Carre"], "a-degres"), ("barion", "square", "brillant", 1, ["Carre", "Coussin"], "brillant"), ("flanders", "square", "ciseaux", 1, ["Coussin"], "ciseaux"),
]
GROUPS = ["base", "cabochon", "square"]


def options(t, selected):
    """The <option>s of the cut menu, grouped. Each carries what the script needs: the mesh, the height, the shapes, and the cut it is drawn with when it is not modelled."""
    out = ""
    for group in GROUPS:
        items = ""
        for key, g, mesh, height, shapes, near in STYLES:
            if g != group:
                continue
            attrs = ' data-mesh="{}" data-height="{}"'.format(mesh, height)
            if shapes:
                attrs += ' data-shapes="{}"'.format(",".join(shapes))
            if near:
                attrs += ' data-near="{}"'.format(t("cut." + near))
            items += '<option value="{}"{}{}>{}</option>'.format(key, attrs, " selected" if key == selected else "", t("cut." + key))
        out += '<optgroup label="{}">{}</optgroup>'.format(t("cut.group." + group), items)
    return out


def shape_options(t, selected):
    return "".join('<option value="{}"{}>{}</option>'.format(k, " selected" if k == selected else "", t("shape." + k)) for k in SHAPES)
