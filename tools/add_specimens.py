#!/usr/bin/env python3
"""Puts specimens of the stones whose look was just changed into the armoire (donations cabinet) of a save slot, so they can be looked at in the game.

    python3 tools/add_specimens.py                      # the latest preset, save slot 1
    python3 tools/add_specimens.py --preset rubis       # another preset
    python3 tools/add_specimens.py --clean              # only removes the test specimens

Close the game (or stop Play mode) first: the game rewrites the save file when it saves and would erase the specimens.
A copy of the save is made before each change (savegame_N.json.avant-specimens). The specimens are recognised by their client name
"Spécimen test": running the tool again replaces them instead of piling them up.

Each specimen gets a specimen number chosen so that its look is the one asked for: the look of a stone (its place inside its clarity grade, its polish)
comes from a hash of its number (see StoneQuality.cs), and this tool works that hash out to find a number that gives the wanted place in the grade.
"""
import argparse
import json
import math
import os
import re
import shutil

SAVE_DIR = os.path.expanduser("~/Library/Application Support/DefaultCompany/GemLabSimulator")
GEM_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "GemLabSimulator", "Assets", "Data", "Gems")
CLIENT = "Spécimen test"
FIRST_ID = 9001

CLARITY = {
    3: ("Fortement inclus", "Nombreuses inclusions visibles à l'œil nu."),
    2: ("Inclus", "Inclusions visibles à la loupe, quelques-unes à l'œil nu."),
    1: ("Eye-clean", "Aucune inclusion visible à l'œil nu, quelques-unes à la loupe."),
    0: ("Loupe clean", "Aucune inclusion visible même à la loupe (10x)."),
}
# StoneEconomy: the clarity factor at the best and at the worst of each grade (tier 3 = heavily included ... 0 = loupe clean).
BEST = {3: 0.32, 2: 0.80, 1: 1.6, 0: 3.2}
WORST = {3: 0.04, 2: 0.30, 1: 0.9, 0: 1.9}


# ---- the hash and the random generator of the game (StoneOptics.StableHash, System.Random) -------------------------------------------

def stable_hash(text):
    h = 2166136261
    for c in text:
        h ^= ord(c)
        h = (h * 16777619) & 0xFFFFFFFF
    return h


class NetRandom:
    """System.Random with a seed (the subtractive generator of .NET and Mono)."""

    def __init__(self, seed):
        mbig = 2147483647
        subtraction = mbig if seed == -2147483648 else abs(seed)
        mj = 161803398 - subtraction
        arr = [0] * 56
        arr[55] = mj
        mk = 1
        for i in range(1, 55):
            ii = (21 * i) % 55
            arr[ii] = mk
            mk = mj - mk
            if mk < 0:
                mk += mbig
            mj = arr[ii]
        for _ in range(1, 5):
            for i in range(1, 56):
                arr[i] -= arr[1 + (i + 30) % 55]
                if arr[i] < 0:
                    arr[i] += mbig
        self.arr = arr
        self.inext = 0
        self.inextp = 21

    def next_double(self):
        mbig = 2147483647
        self.inext = 1 if self.inext + 1 >= 56 else self.inext + 1
        self.inextp = 1 if self.inextp + 1 >= 56 else self.inextp + 1
        ret = self.arr[self.inext] - self.arr[self.inextp]
        if ret == mbig:
            ret -= 1
        if ret < 0:
            ret += mbig
        self.arr[self.inext] = ret
        return ret / mbig


def look_of(specimen_id, tier):
    """What StoneQuality.For works out for a stone of this number: its place in the grade, its polish, its zoning, whether its proportions are poor."""
    rng = NetRandom(stable_hash(specimen_id + "|quality") & 0x7FFFFFFF)
    position = rng.next_double()
    dull = (rng.next_double() ** 2.2) * 0.6 + tier * 0.03
    if tier >= 2:
        rng.next_double()
    rng.next_double()
    zoning = (rng.next_double() ** 1.6) * 0.7
    poor = rng.next_double() < 0.18
    return position, dull, zoning, poor


# ---- the specimens -----------------------------------------------------------------------------------------------------------------------

BASE_VALUE = {"Corindon_Rubis": 150, "Emeraude": 180}      # baseValue of the species assets

BURMA = ("Birmanie (Mogok)", 3.0)
MOZAMBIQUE = ("Mozambique", 1.0)
THAILAND = ("Thaïlande (Chanthaburi)", 0.8)
SRI_LANKA = ("Sri Lanka", 1.3)

# species, origin, tier (0 loupe clean .. 3 heavily included), place in the grade (0 best .. 1 worst), colour (hue, saturation, brightness), colour factor of the price,
# trade name, shape, style, carats, minimum zoning (None = any), a star wanted
RUBIS = [
    ("Corindon_Rubis", BURMA, 0, 0.10, (358, 0.94, 0.79), 10.8, "Sang de pigeon", "Ovale", "Brillant", 2.0, None, False),
    ("Corindon_Rubis", BURMA, 1, 0.35, (356, 0.92, 0.80), 1.8, "", "Coussin", "Mixte", 1.6, None, False),
    ("Corindon_Rubis", BURMA, 2, 0.10, (350, 0.76, 0.95), 1.25, "", "Poire", "Brillant", 1.5, None, False),
    ("Corindon_Rubis", BURMA, 2, 0.90, (358, 0.94, 0.62), 1.0, "", "Rectangle à pans coupés", "Émeraude", 1.8, None, False),
    ("Corindon_Rubis", BURMA, 3, 0.12, (356, 0.90, 0.78), 1.8, "", "Ovale", "Brillant", 1.7, None, False),
    ("Corindon_Rubis", BURMA, 3, 0.50, (338, 0.88, 0.68), 1.0, "", "Coussin", "Mixte", 1.9, 0.40, False),
    ("Corindon_Rubis", BURMA, 3, 0.97, (0, 0.90, 0.40), 0.5, "", "Ovale", "Brillant", 1.6, None, False),
    ("Corindon_Rubis", BURMA, 3, 0.70, (355, 0.90, 0.70), 1.0, "", "Ovale", "Cabochon", 2.2, None, True),
    ("Corindon_Rubis", BURMA, 2, 0.50, (348, 0.42, 0.97), 0.6, "", "Ovale", "Brillant", 1.4, 0.40, False),
    ("Corindon_Rubis", BURMA, 1, 0.80, (354, 0.90, 0.72), 1.8, "", "Rond", "Brillant", 1.3, None, False),
    ("Corindon_Rubis", THAILAND, 3, 0.40, (2, 0.92, 0.45), 0.5, "", "Coussin", "Mixte", 1.8, None, False),
    ("Corindon_Rubis", THAILAND, 2, 0.60, (0, 0.90, 0.55), 1.0, "", "Rond", "Brillant", 1.4, None, False),
    ("Corindon_Rubis", MOZAMBIQUE, 2, 0.40, (354, 0.88, 0.80), 1.8, "", "Ovale", "Brillant", 1.6, None, False),
    ("Corindon_Rubis", MOZAMBIQUE, 3, 0.75, (356, 0.90, 0.62), 1.0, "", "Rectangle à pans coupés", "Émeraude", 1.9, None, False),
    ("Corindon_Rubis", SRI_LANKA, 2, 0.50, (346, 0.70, 0.92), 1.25, "", "Coussin", "Mixte", 1.5, None, False),
    ("Corindon_Rubis", SRI_LANKA, 3, 0.30, (350, 0.80, 0.78), 1.8, "", "Ovale", "Cabochon", 2.1, None, True),
]

# Cabochons of ruby: from the clear and pure (no star) to the milky ones with a strong star. star: True = a star wanted, False = none, None = any.
CABOCHONS = [
    ("Corindon_Rubis", BURMA, 0, 0.10, (358, 0.94, 0.79), 10.8, "Sang de pigeon", "Ovale", "Cabochon", 2.4, None, False),
    ("Corindon_Rubis", BURMA, 1, 0.30, (356, 0.92, 0.80), 1.8, "", "Ovale", "Cabochon", 2.0, None, False),
    ("Corindon_Rubis", BURMA, 1, 0.70, (350, 0.76, 0.95), 1.25, "", "Coussin", "Cabochon", 1.9, None, False),
    ("Corindon_Rubis", BURMA, 2, 0.08, (354, 0.90, 0.72), 1.8, "", "Ovale", "Cabochon", 2.2, None, False),
    ("Corindon_Rubis", BURMA, 2, 0.50, (352, 0.92, 0.68), 1.0, "", "Ovale", "Cabochon", 2.3, None, True),
    ("Corindon_Rubis", BURMA, 2, 0.90, (346, 0.85, 0.70), 1.0, "", "Rond", "Cabochon", 2.0, None, True),
    ("Corindon_Rubis", BURMA, 3, 0.15, (356, 0.90, 0.75), 1.8, "", "Ovale", "Cabochon", 2.5, 0.40, True),
    ("Corindon_Rubis", BURMA, 3, 0.50, (348, 0.88, 0.66), 1.0, "", "Ovale", "Cabochon", 2.6, 0.40, True),
    ("Corindon_Rubis", BURMA, 3, 0.95, (342, 0.82, 0.60), 0.8, "", "Coussin", "Cabochon", 2.2, None, True),
    ("Corindon_Rubis", SRI_LANKA, 2, 0.50, (346, 0.70, 0.92), 1.25, "", "Ovale", "Cabochon", 2.2, None, True),
    ("Corindon_Rubis", SRI_LANKA, 3, 0.50, (350, 0.80, 0.78), 1.8, "", "Ovale", "Cabochon", 2.4, None, True),
    ("Corindon_Rubis", THAILAND, 2, 0.40, (2, 0.90, 0.50), 1.0, "", "Ovale", "Cabochon", 2.0, None, False),
    ("Corindon_Rubis", MOZAMBIQUE, 2, 0.45, (354, 0.88, 0.70), 1.8, "", "Ovale", "Cabochon", 2.1, None, False),
    ("Corindon_Rubis", MOZAMBIQUE, 3, 0.60, (352, 0.90, 0.60), 1.0, "", "Rond", "Cabochon", 1.9, None, True),
]

COLOMBIA = ("Colombie (Muzo/Chivor)", 2.5)
ZAMBIA = ("Zambie", 1.2)
BRAZIL = ("Brésil", 0.9)
AFGHANISTAN = ("Afghanistan (Panjshir)", 1.5)
SIBERIA = ("Sibérie (Oural)", 1.3)
ZIMBABWE = ("Zimbabwe (Sandawana)", 1.6)

# Emeralds: always included, the clean ones very rare. Step cuts and two cabochons.
EMERAUDES = [
    ("Emeraude", COLOMBIA, 1, 0.30, (138, 0.92, 0.68), 5.4, "Vert Muzo", "Rectangle à pans coupés", "Émeraude", 1.6, None, None),
    ("Emeraude", COLOMBIA, 2, 0.20, (132, 0.90, 0.75), 1.8, "", "Rectangle à pans coupés", "Émeraude", 1.8, None, None),
    ("Emeraude", COLOMBIA, 2, 0.70, (126, 0.80, 0.80), 1.25, "", "Ovale", "Brillant", 1.5, None, None),
    ("Emeraude", COLOMBIA, 3, 0.25, (136, 0.88, 0.62), 1.0, "", "Rectangle à pans coupés", "Émeraude", 2.0, None, None),
    ("Emeraude", COLOMBIA, 3, 0.60, (131, 0.85, 0.55), 1.0, "", "Coussin", "Mixte", 1.7, None, None),
    ("Emeraude", COLOMBIA, 3, 0.95, (126, 0.75, 0.40), 0.5, "", "Ovale", "Cabochon", 2.4, None, None),
    ("Emeraude", COLOMBIA, 3, 0.45, (134, 0.90, 0.55), 1.0, "", "Ovale", "Cabochon", 2.2, None, None),
    ("Emeraude", ZAMBIA, 1, 0.40, (144, 0.90, 0.62), 1.8, "", "Ovale", "Brillant", 1.2, None, None),
    ("Emeraude", ZAMBIA, 2, 0.30, (146, 0.85, 0.60), 1.0, "", "Rectangle à pans coupés", "Émeraude", 1.4, None, None),
    ("Emeraude", ZAMBIA, 3, 0.50, (151, 0.80, 0.45), 0.8, "", "Rectangle à pans coupés", "Émeraude", 1.6, None, None),
    ("Emeraude", BRAZIL, 2, 0.40, (111, 0.65, 0.80), 1.25, "", "Coussin", "Mixte", 1.5, None, None),
    ("Emeraude", ZIMBABWE, 2, 0.50, (134, 0.95, 0.55), 1.0, "", "Rectangle à pans coupés", "Émeraude", 0.9, None, None),
    ("Emeraude", SIBERIA, 3, 0.40, (124, 0.80, 0.60), 1.0, "", "Rectangle à pans coupés", "Émeraude", 1.4, None, None),
    ("Emeraude", AFGHANISTAN, 3, 0.30, (136, 0.85, 0.70), 1.8, "", "Poire", "Brillant", 1.6, None, None),
]

PRESETS = {"rubis": RUBIS, "rubis10": RUBIS[:10], "cabochons": CABOCHONS, "emeraudes": EMERAUDES}      # rubis10: the ten faceted rubies of Burma, to go with the cabochons in the 24 places
DEFAULT_PRESET = "emeraudes"


def species_text(species, key):
    """A text field of the species asset (the yaml of Unity: a quoted string with \\xE9 style escapes)."""
    path = os.path.join(GEM_DIR, species + ".asset")
    if not os.path.exists(path):
        return ""
    for line in open(path, encoding="utf-8"):
        m = re.match(r"\s*" + key + r": (.*)$", line)
        if m:
            value = m.group(1).strip()
            if value.startswith('"') and value.endswith('"'):
                value = re.sub(r"\\x([0-9A-Fa-f]{2})", lambda x: chr(int(x.group(1), 16)), value[1:-1])
            return value
    return ""


def pick_id(used, tier, position, min_zoning, star):
    """The first free specimen number whose stone has the wanted place in its grade, a fair polish, ideal proportions and (if asked) the zoning and the star."""
    best = None
    for n in range(FIRST_ID, FIRST_ID + 40000):
        sid = "id%04d" % n
        if sid in used:
            continue
        pos, dull, zoning, poor = look_of(sid, tier)
        if abs(pos - position) > 0.04 or dull > 0.2 or poor:
            continue
        if min_zoning is not None and zoning < min_zoning:
            continue
        # The game gives a star when the hash of the number is under a share that grows with the density of the silk (at least a third when there is a star at all).
        if star is True and stable_hash(sid + "|star") % 100 >= 30:
            continue
        if star is False and stable_hash(sid + "|star") % 100 < 70:
            continue
        best = sid
        break
    return best


def make_entry(spec, sid, species_inclusions):
    species, (origin, origin_mult), tier, position, (hue, sat, bright), color_factor, trade, shape, style, carat, _zoning, _star = spec
    clarity_name, clarity_text = CLARITY[tier]
    factor = BEST[tier] + (WORST[tier] - BEST[tier]) * position
    value = max(1, round(BASE_VALUE[species] * origin_mult * color_factor * factor * (carat / 1.4) ** 1.5))
    ratio = {"Ovale": 1.25, "Poire": 1.5, "Coussin": 1.1, "Rond": 1.0, "Rectangle à pans coupés": 1.25}.get(shape, 1.0)
    length = round(math.sqrt(carat / 1.4) * 6.3 * math.sqrt(ratio), 3)
    width = round(length / ratio, 3)
    depth = round(math.sqrt(carat / 1.4) * (3.2 if style == "Cabochon" else 4.0), 3)
    return {
        "definitionName": species, "specimenId": sid, "clientName": CLIENT, "isDonation": True, "isRough": False, "needsCleaning": False,
        "isPersonalSpecimen": True, "isIdentified": True, "hasWindow": False, "cutShape": shape, "cutStyle": style,
        "originName": origin, "originValueMultiplier": origin_mult, "originInclusionOverride": "",
        "saturationGrade": "", "clarityGrade": clarity_name, "clarityDescription": clarity_text, "inclusionDescription": species_inclusions,
        "caratWeight": carat, "lengthMm": length, "widthMm": width, "depthMm": depth,
        "treatmentGrade": "Non traité", "treatmentDescription": "Aucune trace de traitement détectée.", "isSynthetic": False,
        "authenticityGrade": "Naturelle", "authenticityDescription": "Signature Raman cohérente avec une croissance naturelle.",
        "growthStructureDescription": "Zones de croissance irrégulières et courbes, typiques d'une cristallisation naturelle lente.",
        "finalValue": value, "colorGrade": "", "hasColor": True, "colorHue": float(hue), "colorSaturation": sat, "colorBrightness": bright,
        "isFancyDiamond": False, "tradeName": trade, "hasReport": False, "reportNumber": "", "reportDay": 0, "diamondClarity": "",
        "certificateNumber": "", "certificateIssuer": "", "certificateDay": 0, "roughForm": "", "roughYield": 0.0, "roughColor": 0,
        "roughClarity": 0, "cutQuality": 0, "purchasePrice": 0, "cuttingCost": 0, "heatStar": False, "laserName": "", "hphtName": "",
        "jewelKind": "", "jewelMetal": "", "jewelSetting": "", "jewelMetalValue": 0, "irradiationName": "", "irradiationFadeDay": 0,
        "irradiationFactor": 1.0, "treatmentValueFactor": 1.0, "treatmentFactorTracked": False, "treatmentDoneByPlayer": False,
        "irradiationHue": 0.0, "irradiationSaturation": 0.0, "irradiationBrightness": 0.0, "dyeName": "", "diffusionElement": "",
        "diffusionDepth": 0.0, "diffusionHue": 0.0, "diffusionSaturation": 0.0, "diffusionBrightness": 0.0, "diffusionStar": False,
        "fillerName": "", "coatingName": "", "coatingIridescence": 0.0, "measuredKeys": [], "measuredValues": [], "observedKeys": [],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--slot", type=int, default=1)
    parser.add_argument("--preset", default=DEFAULT_PRESET, help="a preset, or several separated by commas (%s)" % ", ".join(sorted(PRESETS)))
    parser.add_argument("--clean", action="store_true", help="only remove the test specimens")
    args = parser.parse_args()

    path = os.path.join(SAVE_DIR, "savegame_%d.json" % args.slot)
    if not os.path.exists(path):
        raise SystemExit("No save at " + path)
    with open(path, encoding="utf-8") as f:
        save = json.load(f)

    if not os.path.exists(path + ".avant-specimens"):
        shutil.copyfile(path, path + ".avant-specimens")      # the first copy is kept: it is the save as it was before any specimen
    cabinet = [g for g in save.get("cabinetGems", []) if g.get("clientName") != CLIENT]
    removed = len(save.get("cabinetGems", [])) - len(cabinet)

    added = []
    if not args.clean:
        used = set()
        for key in ("inventoryGems", "chestGems", "orderCabinetGems"):
            used.update(g["specimenId"] for g in save.get(key, []))
        used.update(g["specimenId"] for g in cabinet)                 # the old test specimens are gone: their numbers can be used again, so the same stone keeps its number
        specs = []
        for name in args.preset.split(","):
            if name not in PRESETS:
                raise SystemExit("Unknown preset " + name)
            specs += PRESETS[name]
        for spec in specs:
            if len(cabinet) + len(added) >= 24:
                print("The armoire holds 24 stones: the others are left out.")
                break
            sid = pick_id(used, spec[2], spec[3], spec[10], spec[11])
            if sid is None:
                print("No specimen number found for", spec[1][0], spec[2], spec[3])
                continue
            used.add(sid)
            added.append(make_entry(spec, sid, species_text(spec[0], "inclusions")))
    save["cabinetGems"] = cabinet + added

    with open(path, "w", encoding="utf-8") as f:
        json.dump(save, f, ensure_ascii=False, indent=4)
    print("Slot %d: %d old test specimen(s) removed, %d added to the armoire (copy of the save: %s)." % (args.slot, removed, len(added), path + ".avant-specimens"))
    for entry in added:
        print("  %s  %-24s %-14s %-9s %s" % (entry["specimenId"], entry["originName"], entry["clarityGrade"], entry["cutShape"], entry["cutStyle"]))


if __name__ == "__main__":
    main()
