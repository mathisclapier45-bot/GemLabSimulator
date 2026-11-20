#!/usr/bin/env python3
"""Writes site/data/extra_species.json: species that the encyclopedia of the site has but the game data does not (yet).

  - the four new garnets written in the game code (Assets/Editor/SceneBootstrap.cs: Grenat_Mali, Grenat_Topazolite, Grenat_Pyrope_Chromifere,
    Grenat_Change_Couleur) are read from there, so the site and the game say the same thing. As soon as the game data (gemlab_data.json) has
    one of them, the game version wins and this one is ignored;
  - two garnets that are not stones of the game, the pure grossular and the pure andradite, are written below by hand.

  python3 site/tools/build_extra_species.py
"""
import json
import re
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))
from new_species_spec import SPECTRA_META  # noqa: E402
REPO = SITE.parent
SRC = REPO / "GemLabSimulator" / "Assets" / "Editor" / "SceneBootstrap.cs"
OUT = SITE / "data" / "extra_species.json"

FROM_GAME_CODE = ["Grenat_Mali", "Grenat_Topazolite", "Grenat_Pyrope_Chromifere", "Grenat_Change_Couleur"]
GARNET_HABITS = [{"habit": "rhombic_dodecahedron", "weight": 90}, {"habit": "trapezohedron", "weight": 10}]
OPTIC = {"Isotropic": ("Isotrope", 0), "Uniaxial": ("Uniaxe", 1), "Biaxial": ("Biaxe", 2), "Opaque": ("Opaque", 3)}


def hexcolor(r, g, b):
    return "#{:02X}{:02X}{:02X}".format(*[int(round(float(v) * 255)) for v in (r, g, b)])


def csharp_string(text):
    return text.replace('\\"', '"').replace("\\\\", "\\")


def parse_block(text, species_id):
    start = text.index('BuildGem("{}", g =>'.format(species_id))
    end = text.index("}),", start)
    block = text[start:end]
    sp = {"id": species_id}
    for m in re.finditer(r'g\.(\w+) = ("(?:[^"\\]|\\.)*"|[^;]+);', block):
        key, raw = m.group(1), m.group(2).strip()
        if key in ("spectrumLines", "origins"):          # lists: read below
            continue
        if key in ("icon", "roughIcon"):
            if key == "icon":
                colors = re.findall(r"new Color\(([^)]*)\)", raw)
                c1, c2 = [[v.strip().rstrip("f") for v in c.split(",")[:3]] for c in colors[:2]]
                sp["iconColor"], sp["iconFacetColor"] = hexcolor(*c1), hexcolor(*c2)
            continue
        if raw.startswith('"'):
            sp[key] = csharp_string(raw[1:-1])
        elif raw in ("true", "false"):
            sp[key] = raw == "true"
        elif raw.startswith("GemDefinition.OpticalCharacterClass."):
            name, number = OPTIC[raw.rsplit(".", 1)[1]]
            sp["opticalCharacterClassName"], sp["opticalCharacterClass"] = name, number
        else:
            sp[key] = float(raw.rstrip("f"))
    sp["spectrumLines"] = [{"wavelengthNm": float(a), "widthNm": float(b), "depth": 1, "edgeNm": 0}
                           for a, b in re.findall(r"new SpectrumLine \{ wavelengthNm = ([\d.]+)f, widthNm = ([\d.]+)f", block)]
    origins = []
    for m in re.finditer(r'new GemOrigin \{(.*?)\},?\s*(?=new GemOrigin|\};)', block, re.S):
        body = m.group(1)
        o = {"name": csharp_string(re.search(r'name = "((?:[^"\\]|\\.)*)"', body).group(1)),
             "rarityWeight": float(re.search(r"rarityWeight = ([\d.]+)", body).group(1)),
             "valueMultiplier": float(re.search(r"valueMultiplier = ([\d.]+)f", body).group(1))}
        ov = re.search(r'inclusionOverride = "((?:[^"\\]|\\.)*)"', body)
        if ov:
            o["inclusionOverride"] = csharp_string(ov.group(1))
        origins.append(o)
    sp["origins"] = origins
    return finish(sp)


def finish(sp):
    """The fields every species of the game data has."""
    sp.setdefault("isQuartzFamily", False)
    sp.setdefault("isPolycrystalline", False)
    sp.setdefault("isThermallyConductiveLikeDiamond", False)
    sp.setdefault("isElectricallyConductive", False)
    sp.setdefault("forcedSynthetic", False)
    sp.setdefault("forcedTreatment", False)
    sp.setdefault("roughOnly", 0)
    sp.setdefault("isTunnelExclusive", False)
    sp.setdefault("tunnelMinPrice", 0)
    sp.setdefault("habits", GARNET_HABITS)
    sp.setdefault("birefringence", 0)
    sp["pleochroic"] = bool(sp.get("pleochroic"))
    for key in ("refractiveIndex", "density", "hardness", "lengthMm", "widthMm", "depthMm", "weightCarats", "baseValue"):
        value = sp[key]
        sp[key] = int(value) if float(value).is_integer() and key == "baseValue" else round(float(value), 4)
    return sp


# The two garnets that are not stones of the game: the pure grossular and the pure andradite (poles of the garnet diagram).
BY_HAND = [
    {"id": "Grenat_Grossulaire", "displayName": "Grenat Grossulaire",
     "description": "Le grenat de calcium et d'aluminium pur : incolore à vert pâle, jaune ou rose, plus coloré avec des traces de fer, de chrome ou de vanadium. Ses variétés les plus connues sont l'hessonite et la tsavorite.",
     "iconColor": "#CBD98C", "iconFacetColor": "#EAF2B8", "refractiveIndex": 1.74, "density": 3.6, "hardness": 7, "lengthMm": 6.5, "widthMm": 5.5, "depthMm": 3.6, "weightCarats": 1.2,
     "luster": "Vitreux", "color": "Incolore à vert pâle, jaune ou rose", "shape": "Rond", "cutStyle": "Brillant", "transparency": "Transparent",
     "pleochroic": False, "pleochroicDescription": "",
     "fluorescenceLongWave": "Généralement inerte sous UV longue onde.", "fluorescenceShortWave": "Généralement inerte sous UV courte onde.",
     "opticalCharacter": "Isotrope (monoréfringent, réseau cubique).", "opticalCharacterClassName": "Isotrope", "opticalCharacterClass": 0,
     "absorptionSpectrum": "Pas de spectre net ; bandes faibles du fer ou du manganèse selon la teinte.", "spectrumLines": [],
     "inclusions": "Généralement propre ; petits cristaux d'apatite, de zircon ou de diopside, parfois des grains arrondis.",
     "interferenceFigure": "Aucune figure d'interférence (matériau isotrope).", "chelseaFilterReaction": "Aucune réaction particulière.",
     "magnetism": "Aucune réaction notable à l'aimant (grenat pauvre en fer).", "baseValue": 60,
     "origins": [{"name": "Canada (Québec)", "rarityWeight": 50, "valueMultiplier": 1.0}, {"name": "Autre gisement", "rarityWeight": 50, "valueMultiplier": 1.0}]},
    {"id": "Grenat_Andradite", "displayName": "Grenat Andradite",
     "description": "Le grenat de calcium et de fer pur : jaune, vert, brun ou noir. Ses variétés sont le démantoïde (vert, au chrome), la topazolite (jaune) et la mélanite (noire, opaque).",
     "iconColor": "#80852A", "iconFacetColor": "#BFB859", "refractiveIndex": 1.887, "density": 3.85, "hardness": 6.75, "lengthMm": 5.5, "widthMm": 4.5, "depthMm": 3, "weightCarats": 0.8,
     "luster": "Vitreux à sub-adamantin", "color": "Jaune à vert, brun ou noir", "shape": "Rond", "cutStyle": "Brillant", "transparency": "Transparent",
     "pleochroic": False, "pleochroicDescription": "",
     "fluorescenceLongWave": "Généralement inerte sous UV.", "fluorescenceShortWave": "Généralement inerte sous UV.",
     "opticalCharacter": "Isotrope (monoréfringent, réseau cubique).", "opticalCharacterClassName": "Isotrope", "opticalCharacterClass": 0,
     "absorptionSpectrum": "Forte absorption dans le violet et le bleu (fer ferrique).", "spectrumLines": [{"wavelengthNm": 440, "widthNm": 45, "depth": 1, "edgeNm": 0}],
     "inclusions": "Fines fibres, parfois en « queue de cheval » ; sinon assez propre.",
     "interferenceFigure": "Aucune figure d'interférence (matériau isotrope).", "chelseaFilterReaction": "Aucune réaction particulière.",
     "magnetism": "Attiré par un aimant puissant (grenat riche en fer).", "baseValue": 80,
     "origins": [{"name": "Italie (Piémont)", "rarityWeight": 50, "valueMultiplier": 1.2}, {"name": "Autre gisement", "rarityWeight": 50, "valueMultiplier": 1.0}]},
]

# A natural sapphire that changes color (vanadium and chromium): blue-violet by day, purple to red-violet under an incandescent light.
# Its optics, habits and constants of corundum are copied from the blue sapphire of the game data.
COLOR_CHANGE_SAPPHIRE = {
    "id": "Saphir_Change_Couleur", "displayName": "Saphir à changement de couleur",
    "description": "Un saphir naturel qui change de couleur : bleu-violet à la lumière du jour, pourpre à rouge-violet sous une lampe à incandescence. Il le doit à un mélange de vanadium et de chrome, et se rapproche de l'alexandrite par son effet.",
    "iconColor": "#6661B3", "iconFacetColor": "#9E99E0", "refractiveIndex": 1.766, "density": 4.0, "hardness": 9, "lengthMm": 7.2, "widthMm": 5.6, "depthMm": 4, "weightCarats": 1.4,
    "luster": "Vitreux à sub-adamantin", "color": "Bleu violacé à pourpre (changement de couleur)", "shape": "Ovale", "cutStyle": "Mixte", "transparency": "Transparent",
    "pleochroic": True, "pleochroicDescription": "Bleu-violet / Violet-rouge selon l'axe d'observation (modéré).",
    "fluorescenceLongWave": "Inerte à rouge faible sous UV longue onde.", "fluorescenceShortWave": "Inerte sous UV courte onde.",
    "absorptionSpectrum": "Bandes du chrome et du vanadium : large bande vers 570 nm et raies fines du chrome dans le rouge (694 nm).",
    "spectrumLines": [{"wavelengthNm": 475, "widthNm": 10, "depth": 1, "edgeNm": 0}, {"wavelengthNm": 570, "widthNm": 80, "depth": 1, "edgeNm": 0}, {"wavelengthNm": 694, "widthNm": 4, "depth": 1, "edgeNm": 0}],
    "inclusions": "Fine soie de rutile, zonation de couleur, petits cristaux dispersés.",
    "chelseaFilterReaction": "Réaction rougeâtre faible possible (chrome et vanadium).", "baseValue": 160,
    "origins": [{"name": "Sri Lanka (Ceylan)", "rarityWeight": 40, "valueMultiplier": 1.2}, {"name": "Tanzanie (Umba)", "rarityWeight": 30, "valueMultiplier": 1.0},
                {"name": "Madagascar", "rarityWeight": 30, "valueMultiplier": 0.9}],
    "copyFrom": "Saphir", "copy": ["opticalCharacter", "opticalCharacterClassName", "opticalCharacterClass", "birefringence", "interferenceFigure", "magnetism", "habits"],
}

# Colour windows (hue from, hue to, saturation from, to, brightness from, to) of the species that are not in GemColorSystem.cs.
COLOR_LIMITS = {"Grenat_Grossulaire": [55, 130, 0.12, 0.70, 0.55, 1.0], "Grenat_Andradite": [40, 95, 0.40, 1.0, 0.25, 0.85],
                "Saphir_Change_Couleur": [235, 320, 0.25, 0.85, 0.25, 0.80]}


def hex_of(c):
    return "#{:02X}{:02X}{:02X}".format(*[int(round(v * 255)) for v in c])


def from_spec(sp):
    """A species of tools/new_species_spec.py as it appears in the game data."""
    optic_name, optic_number = OPTIC[sp["optic"]]
    out = {
        "id": sp["id"], "displayName": sp["name"][0], "description": sp["desc"][0], "iconColor": hex_of(sp["icon"][0]), "iconFacetColor": hex_of(sp["icon"][1]),
        "refractiveIndex": sp["ri"], "density": sp["sg"], "hardness": sp["h"], "lengthMm": sp["dims"][0], "widthMm": sp["dims"][1], "depthMm": sp["dims"][2], "weightCarats": sp["ct"],
        "luster": sp["luster"][0], "color": sp["color"][0], "shape": sp["shape"], "cutStyle": sp["cut"], "transparency": sp["transp"],
        "pleochroic": bool(sp.get("pleo")), "pleochroicDescription": sp["pleo"][0] if sp.get("pleo") else "",
        "fluorescenceLongWave": sp["fluL"][0], "fluorescenceShortWave": sp["fluS"][0], "opticalCharacter": sp["opt"][0],
        "opticalCharacterClassName": optic_name, "opticalCharacterClass": optic_number, "birefringence": sp["biref"], "isPolycrystalline": bool(sp.get("poly")),
        "absorptionSpectrum": sp["spec"][0], "spectrumLines": [{"wavelengthNm": a, "widthNm": b, "depth": 1, "edgeNm": 0} for a, b in sp["lines"]],
        "inclusions": sp["incl"][0], "interferenceFigure": sp["fig"][0], "chelseaFilterReaction": sp["chelsea"][0], "magnetism": sp["magn"][0], "baseValue": sp["value"],
        "origins": [dict({"name": o["fr"], "rarityWeight": o["weight"], "valueMultiplier": o["mult"]}, **({"inclusionOverride": o["over"][0]} if o.get("over") else {})) for o in sp["origins"]],
        "habits": [{"habit": h, "weight": w} for h, w in sp["habits"]], "colorLimits": list(sp["limits"]),
    }
    if sp.get("fluo_entry"):
        out["fluoEntry"] = sp["fluo_entry"]
    if sp.get("spectra_entry"):
        e = sp["spectra_entry"]
        out["spectraEntry"] = {"description": e["description"], "lines": [{"wavelengthNm": w, "widthNm": wd, "depth": dp, "edgeNm": 0.0} for w, wd, dp in e["lines"]]}
        if sp["id"] in SPECTRA_META:
            out["spectraEntry"]["label"] = SPECTRA_META[sp["id"]][1][0]
    if sp.get("spectra_extra"):                                   # the other spectra of the species (variants)
        out["spectraVariants"] = [{"key": e["key"], "label": SPECTRA_META[e["key"]][1][0], "description": e["description"], "lines": [{"wavelengthNm": w, "widthNm": wd, "depth": dp, "edgeNm": 0.0} for w, wd, dp in e["lines"]]}
                                  for e in sp["spectra_extra"]]
    if sp.get("chelsea_entry"):
        out["chelseaEntry"] = sp["chelsea_entry"]
    return finish(out)


def main():
    text = SRC.read_text(encoding="utf-8")
    game = {s["id"]: s for s in json.loads((SITE / "data" / "gemlab_data.json").read_text(encoding="utf-8"))["species"]}
    sapphire = dict(COLOR_CHANGE_SAPPHIRE)
    source = game[sapphire.pop("copyFrom")]
    for key in sapphire.pop("copy"):
        sapphire[key] = source[key]
    species = [parse_block(text, i) for i in FROM_GAME_CODE] + [finish(dict(s)) for s in BY_HAND] + [finish(sapphire)]
    for s in species:
        s["colorLimits"] = COLOR_LIMITS.get(s["id"])
    from new_species_spec import SPECIES            # the species written for both the game and the site
    species += [from_spec(s) for s in SPECIES]
    OUT.write_text(json.dumps({"note": "Espèces du site absentes des données du jeu. Fichier fabriqué par site/tools/build_extra_species.py : ne pas modifier à la main.",
                               "species": species}, ensure_ascii=False, indent=1), encoding="utf-8")
    print("{} espèces écrites dans {}".format(len(species), OUT))


if __name__ == "__main__":
    main()
