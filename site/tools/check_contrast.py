#!/usr/bin/env python3
"""Checks the colour contrast (WCAG 2.1, level AA: 4.5 for normal text, 3 for large text) of the main pairs of the theme.

  python3 site/tools/check_contrast.py

Reads the colour tokens of static/css/theme.css (light and dark) and tests the pairs that the site really uses.
Exit code 1 when a pair is under its threshold."""
import re
import sys
from pathlib import Path

CSS = Path(__file__).resolve().parent.parent / "static" / "css" / "theme.css"


def parse_tokens(block):
    return {m.group(1): m.group(2) for m in re.finditer(r"--([a-z0-9-]+):\s*(#[0-9a-fA-F]{6})", block)}


def tokens():
    css = CSS.read_text(encoding="utf-8")
    start = css.index("@media (prefers-color-scheme: dark)")
    light = parse_tokens(css[css.index(":root"):start])
    dark = dict(light)
    dark.update(parse_tokens(css[start:css.index("}", css.index("}", start) + 1)]))
    # --btn is set further down (one value per theme): button colour
    light["btn"] = light["accent"]
    m = re.search(r"@media \(prefers-color-scheme: dark\) \{ :root \{ --btn: (#[0-9a-fA-F]{6})", css)
    dark["btn"] = m.group(1) if m else dark["accent"]
    return light, dark


def lum(hexcolor):
    rgb = [int(hexcolor[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    lin = [c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4 for c in rgb]
    return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]


def ratio(a, b):
    la, lb = sorted((lum(a), lum(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def mix(a, b, t):
    """a over b with opacity t."""
    pa, pb = [int(a[i:i + 2], 16) for i in (1, 3, 5)], [int(b[i:i + 2], 16) for i in (1, 3, 5)]
    return "#" + "".join("{:02x}".format(round(x * t + y * (1 - t))) for x, y in zip(pa, pb))


def pairs(tk, dark):
    """(name, text colour, background colour, minimum ratio)."""
    out = [
        ("texte sur fond", tk["text"], tk["bg"], 4.5),
        ("texte sur carte", tk["text"], tk["surface"], 4.5),
        ("texte discret (.small, .lead) sur fond", tk["muted"], tk["bg"], 4.5),
        ("texte discret sur carte", tk["muted"], tk["surface"], 4.5),
        ("texte discret sur carte grise", tk["muted"], tk["surface2"], 4.5),
        ("lien sur carte", tk["accent"] if not dark else tk["accent-dark"], tk["surface"], 4.5),
        ("lien sur fond", tk["accent"] if not dark else tk["accent-dark"], tk["bg"], 4.5),
        ("badge / pastille (accent foncé sur accent doux)", tk["accent-dark"], tk["accent-soft"], 4.5),
        ("bouton principal (blanc sur accent)", "#ffffff", tk["btn"], 4.5),
        ("bouton clair (texte violet sur blanc)", "#2b2680", "#ffffff", 4.5),
        ("bannière d'alerte", tk["text"], tk["warn-bg"], 4.5),
        ("bannière de confirmation", tk["text"], tk["ok-bg"], 4.5),
        ("bannière d'erreur", tk["text"], tk["err-bg"], 4.5),
        ("à compléter (surlignage jaune)", "#5b3d00", "#fde68a", 4.5),
        ("pastille « réussi »", "#17633b", "#d4f1df", 4.5),
        ("pastille « à refaire »", "#7a4d06", "#fbe7c6", 4.5),
        ("bouton danger", "#ffffff", "#d92d20", 4.5),
        ("texte blanc sur le bloc violet (clair du dégradé)", "#ffffff", "#9b4be3", 4.5),
        ("texte blanc sur le bloc violet (foncé du dégradé)", "#ffffff", "#3b2fc9", 4.5),
        ("« Juste » sur carte", tk["good"], tk["surface"], 4.5),
        ("« Pas tout à fait » sur carte", tk["bad"], tk["surface"], 4.5),
        ("« À refaire » sur carte", tk["mid"], tk["surface"], 4.5),
        ("barre de survol discrète (muted sur accent doux)", tk["muted"], tk["accent-soft"], 4.5),
    ]
    return out


def main():
    light, dark = tokens()
    failures = 0
    for mode, tk, is_dark in (("clair", light, False), ("sombre", dark, True)):
        print("== Thème {} ==".format(mode))
        for name, fg, bg, need in pairs(tk, is_dark):
            if mode == "sombre" and any(k in name for k in ("pastille", "à compléter", "bouton danger", "bloc violet", "bouton clair")):
                continue
            r = ratio(fg, bg)
            ok = r >= need
            failures += 0 if ok else 1
            print("{} {:5.2f}  (min {})  {}".format("ok " if ok else "NON", r, need, name))
    print("\n{}".format("Tous les contrastes sont suffisants." if not failures else "{} paire(s) à corriger.".format(failures)))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
