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

BASE_VALUE = {"Corindon_Rubis": 150, "Emeraude": 180, "Saphir_Bleu": 140, "Saphir_Jaune": 55, "Saphir_Rose": 110, "Padparadscha": 320, "Saphir_Vert": 40,
              "Saphir_Violet": 65, "Saphir_Orange": 90, "Spinelle": 45, "Spinelle_Bleu": 90, "Rhodocrosite": 28, "Agate": 30, "Cornaline": 8, "Malachite": 18, "Turquoise": 35, "Turquoise_Stabilisee": 12, "Ivoire_Vegetal": 6}      # baseValue of the species assets

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

KASHMIR = ("Cachemire (Kashmir)", 4.0)
CEYLON_BLUE = ("Sri Lanka (Ceylan)", 1.4)
MADAGASCAR_BLUE = ("Madagascar", 1.0)
AUSTRALIA_BLUE = ("Australie", 0.7)

# Sapphires: blue by deposit (Kashmir velvet, Ceylon light with long silk, Madagascar, dark Australian with hexagonal bands), star cabochons, and the other colours.
SAPHIRS = [
    ("Saphir_Bleu", KASHMIR, 0, 0.20, (222, 0.62, 0.92), 2.5, "Cornflower", "Coussin", "Mixte", 2.0, None, None),
    ("Saphir_Bleu", KASHMIR, 1, 0.40, (226, 0.70, 0.88), 2.5, "Cornflower", "Ovale", "Mixte", 1.8, 0.30, None),
    ("Saphir_Bleu", KASHMIR, 2, 0.50, (228, 0.75, 0.85), 1.25, "", "Coussin", "Mixte", 2.2, 0.40, None),
    ("Saphir_Bleu", CEYLON_BLUE, 0, 0.20, (215, 0.45, 0.96), 1.0, "", "Ovale", "Mixte", 2.5, None, None),
    ("Saphir_Bleu", CEYLON_BLUE, 1, 0.30, (232, 0.90, 0.82), 3.5, "Bleu royal", "Coussin", "Mixte", 2.0, None, None),
    ("Saphir_Bleu", CEYLON_BLUE, 2, 0.50, (218, 0.55, 0.94), 1.0, "", "Rond", "Brillant", 1.6, 0.40, None),
    ("Saphir_Bleu", CEYLON_BLUE, 2, 0.20, (220, 0.65, 0.90), 1.0, "", "Ovale", "Cabochon", 2.4, None, False),
    ("Saphir_Bleu", CEYLON_BLUE, 3, 0.40, (220, 0.60, 0.85), 1.0, "", "Ovale", "Cabochon", 3.0, None, True),
    ("Saphir_Bleu", CEYLON_BLUE, 3, 0.85, (215, 0.40, 0.88), 0.6, "", "Ovale", "Cabochon", 2.6, None, True),
    ("Saphir_Bleu", CEYLON_BLUE, 3, 0.97, (220, 0.40, 0.60), 0.3, "", "Ovale", "Cabochon", 2.4, None, None),
    ("Saphir_Bleu", MADAGASCAR_BLUE, 1, 0.50, (235, 0.90, 0.75), 1.8, "", "Ovale", "Mixte", 1.6, None, None),
    ("Saphir_Bleu", MADAGASCAR_BLUE, 2, 0.60, (228, 0.80, 0.80), 1.0, "", "Coussin", "Mixte", 1.5, None, None),
    ("Saphir_Bleu", MADAGASCAR_BLUE, 3, 0.50, (230, 0.85, 0.70), 0.6, "", "Ovale", "Mixte", 1.8, None, None),
    ("Saphir_Bleu", AUSTRALIA_BLUE, 2, 0.40, (240, 0.95, 0.40), 0.5, "", "Ovale", "Mixte", 1.8, 0.45, None),
    ("Saphir_Bleu", AUSTRALIA_BLUE, 3, 0.60, (235, 0.95, 0.32), 0.3, "", "Coussin", "Mixte", 2.0, 0.40, None),
    ("Saphir_Bleu", AUSTRALIA_BLUE, 2, 0.30, (192, 0.75, 0.70), 0.6, "", "Rond", "Brillant", 1.5, 0.40, None),
    ("Padparadscha", ("Sri Lanka", 2.2), 1, 0.30, (14, 0.48, 0.98), 3.0, "Padparadscha", "Ovale", "Mixte", 1.6, None, None),
    ("Saphir_Rose", ("Madagascar", 1.0), 2, 0.40, (340, 0.58, 0.95), 1.0, "", "Ovale", "Brillant", 1.4, None, None),
    ("Saphir_Rose", ("Sri Lanka", 1.2), 3, 0.50, (338, 0.50, 0.90), 1.0, "", "Ovale", "Cabochon", 2.4, None, True),
    ("Saphir_Jaune", ("Sri Lanka", 1.1), 1, 0.30, (52, 0.62, 0.96), 1.0, "", "Coussin", "Mixte", 2.5, None, None),
    ("Saphir_Jaune", ("Australie", 0.8), 2, 0.50, (44, 0.70, 0.70), 0.6, "", "Ovale", "Mixte", 2.0, 0.40, None),
    ("Saphir_Orange", ("Tanzanie", 0.9), 2, 0.40, (28, 0.70, 0.94), 1.0, "", "Ovale", "Brillant", 1.5, None, None),
    ("Saphir_Violet", ("Sri Lanka", 1.1), 1, 0.40, (272, 0.65, 0.85), 1.0, "", "Coussin", "Mixte", 1.6, None, None),
    ("Saphir_Vert", ("Australie", 0.8), 2, 0.50, (110, 0.55, 0.55), 0.6, "", "Ovale", "Mixte", 1.8, 0.40, None),
]

BURMA_SPINEL = ("Birmanie", 1.8)
TANZANIA_SPINEL = ("Tanzanie", 1.0)
CEYLON_SPINEL = ("Sri Lanka", 1.1)

# Spinels: very clear stones; red, pink and the neon "Jedi" of Mahenge, cobalt blue of Vietnam. Crystals, octahedral negative crystals, strings of tiny octahedra,
# iron-stained fissures, zircons with halos.
SPINELLES = [
    ("Spinelle", BURMA_SPINEL, 0, 0.20, (358, 0.92, 0.82), 1.8, "", "Ovale", "Brillant", 1.6, None, None),
    ("Spinelle", BURMA_SPINEL, 0, 0.50, (346, 0.94, 0.97), 3.0, "Jedi (rose néon)", "Coussin", "Mixte", 1.4, None, None),
    ("Spinelle", BURMA_SPINEL, 1, 0.30, (335, 0.58, 0.95), 1.0, "", "Coussin", "Mixte", 1.5, None, None),
    ("Spinelle", BURMA_SPINEL, 1, 0.70, (355, 0.90, 0.70), 1.25, "", "Rectangle à pans coupés", "Émeraude", 1.7, None, None),
    ("Spinelle", BURMA_SPINEL, 2, 0.30, (2, 0.88, 0.75), 1.0, "", "Ovale", "Brillant", 1.6, None, None),
    ("Spinelle", BURMA_SPINEL, 2, 0.60, (318, 0.70, 0.85), 1.0, "", "Coussin", "Mixte", 1.8, None, None),
    ("Spinelle", BURMA_SPINEL, 3, 0.40, (0, 0.92, 0.58), 0.6, "", "Ovale", "Mixte", 1.9, None, None),
    ("Spinelle", BURMA_SPINEL, 3, 0.50, (350, 0.85, 0.70), 0.6, "", "Ovale", "Cabochon", 2.2, None, None),
    ("Spinelle", TANZANIA_SPINEL, 0, 0.30, (348, 0.95, 0.98), 3.0, "Jedi (rose néon)", "Ovale", "Brillant", 1.5, None, None),
    ("Spinelle", TANZANIA_SPINEL, 1, 0.40, (338, 0.62, 0.95), 1.0, "", "Coussin", "Mixte", 1.6, None, None),
    ("Spinelle", TANZANIA_SPINEL, 2, 0.40, (12, 0.85, 0.85), 1.0, "", "Ovale", "Brillant", 1.5, None, None),
    ("Spinelle", TANZANIA_SPINEL, 2, 0.80, (352, 0.85, 0.72), 0.8, "", "Rond", "Brillant", 1.3, None, None),
    ("Spinelle", TANZANIA_SPINEL, 3, 0.50, (356, 0.85, 0.60), 0.5, "", "Coussin", "Mixte", 2.0, None, None),
    ("Spinelle", CEYLON_SPINEL, 1, 0.30, (338, 0.32, 0.97), 0.8, "", "Ovale", "Brillant", 1.6, None, None),
    ("Spinelle", CEYLON_SPINEL, 2, 0.50, (322, 0.65, 0.85), 1.0, "", "Coussin", "Mixte", 1.7, None, None),
    ("Spinelle", CEYLON_SPINEL, 1, 0.40, (14, 0.48, 0.98), 2.5, "Padparadscha", "Ovale", "Mixte", 1.4, None, None),
    ("Spinelle_Bleu", ("Vietnam", 1.3), 0, 0.30, (222, 0.85, 0.80), 2.0, "", "Coussin", "Mixte", 1.2, None, None),
    ("Spinelle_Bleu", ("Vietnam", 1.3), 1, 0.40, (225, 0.80, 0.70), 1.5, "", "Ovale", "Brillant", 1.3, None, None),
    ("Spinelle_Bleu", ("Vietnam", 1.3), 2, 0.50, (228, 0.75, 0.60), 1.0, "", "Rond", "Brillant", 1.1, None, None),
    ("Spinelle_Bleu", ("Sri Lanka", 1.2), 1, 0.30, (215, 0.40, 0.75), 0.8, "", "Ovale", "Mixte", 1.6, None, None),
    ("Spinelle_Bleu", ("Sri Lanka", 1.2), 2, 0.60, (232, 0.55, 0.70), 0.8, "", "Coussin", "Mixte", 1.8, None, None),
    ("Spinelle_Bleu", ("Tanzanie", 1.0), 2, 0.40, (240, 0.60, 0.65), 0.8, "", "Ovale", "Brillant", 1.5, None, None),
    ("Spinelle_Bleu", ("Tanzanie", 1.0), 3, 0.50, (235, 0.60, 0.50), 0.5, "", "Coussin", "Mixte", 1.9, None, None),
    ("Spinelle", TANZANIA_SPINEL, 3, 0.85, (354, 0.80, 0.55), 0.4, "", "Ovale", "Mixte", 2.1, None, None),
]

ARGENTINA = ("Argentine", 1.0)
PERU = ("P\u00e9rou", 1.1)
USA = ("\u00c9tats-Unis", 1.6)

# Rhodochrosite: banded cabochons of every kind (the crusts change from stone to stone: rings, stripes, oxide), and four clear faceted stones of one colour.
RHODOCHROSITES = [
    ("Rhodocrosite", ARGENTINA, 1, 0.30, (350, 0.62, 0.92), 1.0, "", "Ovale", "Cabochon", 6.0, None, None),
    ("Rhodocrosite", ARGENTINA, 1, 0.50, (346, 0.70, 0.88), 1.0, "", "Poire", "Cabochon", 5.0, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.40, (355, 0.55, 0.95), 1.0, "", "Ovale", "Cabochon", 8.0, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.60, (342, 0.75, 0.85), 1.0, "", "Coussin", "Cabochon", 4.5, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.20, (352, 0.48, 0.96), 1.0, "", "Poire", "Cabochon", 7.0, None, None),
    ("Rhodocrosite", ARGENTINA, 3, 0.40, (348, 0.66, 0.90), 1.0, "", "Ovale", "Cabochon", 9.0, None, None),
    ("Rhodocrosite", ARGENTINA, 1, 0.70, (358, 0.58, 0.93), 1.0, "", "Rond", "Cabochon", 4.0, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.80, (344, 0.80, 0.82), 1.0, "", "Ovale", "Cabochon", 6.5, None, None),
    ("Rhodocrosite", ARGENTINA, 3, 0.20, (350, 0.45, 0.97), 1.0, "", "Coussin", "Cabochon", 5.5, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.50, (2, 0.60, 0.92), 1.0, "", "Poire", "Cabochon", 6.0, None, None),
    ("Rhodocrosite", PERU, 1, 0.40, (347, 0.72, 0.88), 1.0, "", "Ovale", "Cabochon", 5.0, None, None),
    ("Rhodocrosite", PERU, 2, 0.30, (353, 0.52, 0.94), 1.0, "", "Ovale", "Cabochon", 7.5, None, None),
    ("Rhodocrosite", PERU, 2, 0.70, (340, 0.68, 0.84), 1.0, "", "Coussin", "Cabochon", 4.8, None, None),
    ("Rhodocrosite", PERU, 3, 0.60, (356, 0.62, 0.90), 1.0, "", "Poire", "Cabochon", 8.5, None, None),
    ("Rhodocrosite", PERU, 1, 0.20, (349, 0.78, 0.86), 1.0, "", "Rond", "Cabochon", 3.5, None, None),
    ("Rhodocrosite", ARGENTINA, 2, 0.45, (345, 0.66, 0.89), 1.0, "", "Ovale", "Cabochon", 10.0, None, None),
    ("Rhodocrosite", ARGENTINA, 3, 0.75, (351, 0.57, 0.91), 1.0, "", "Ovale", "Cabochon", 12.0, None, None),
    ("Rhodocrosite", PERU, 2, 0.55, (343, 0.74, 0.87), 1.0, "", "Poire", "Cabochon", 6.0, None, None),
    ("Rhodocrosite", ARGENTINA, 1, 0.60, (354, 0.50, 0.95), 1.0, "", "Coussin", "Cabochon", 5.2, None, None),
    ("Rhodocrosite", PERU, 3, 0.30, (348, 0.70, 0.88), 1.0, "", "Ovale", "Cabochon", 7.0, None, None),
    ("Rhodocrosite", USA, 0, 0.30, (352, 0.90, 0.80), 1.5, "", "Coussin", "Mixte", 2.5, None, None),
    ("Rhodocrosite", USA, 0, 0.50, (348, 0.88, 0.85), 1.5, "", "Ovale", "Brillant", 1.8, None, None),
    ("Rhodocrosite", USA, 1, 0.40, (355, 0.85, 0.75), 1.3, "", "Rectangle \u00e0 pans coup\u00e9s", "\u00c9meraude", 3.0, None, None),
    ("Rhodocrosite", PERU, 1, 0.50, (346, 0.75, 0.88), 1.2, "", "Rond", "Brillant", 1.5, None, None),
]

BRAZIL_AGATE = ("Br\u00e9sil", 1.0)
MADAGASCAR_AGATE = ("Madagascar", 1.0)
INDIA_AGATE = ("Inde", 0.9)

# Agate: cabochons of every natural colour; the pattern (the nodule, its bands, its middle) changes from stone to stone.
AGATES = [
    ("Agate", BRAZIL_AGATE, 0, 0.20, (215, 0.25, 0.72), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.30, (0, 0.03, 0.62), 1.0, "", "Ovale", "Cabochon", 5.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.40, (30, 0.42, 0.62), 1.0, "", "Poire", "Cabochon", 7.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.50, (22, 0.72, 0.75), 1.0, "", "Coussin", "Cabochon", 8.5, None, None),
    ("Agate", BRAZIL_AGATE, 0, 0.60, (40, 0.42, 0.82), 1.0, "", "Ovale", "Cabochon", 10.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.70, (320, 0.15, 0.78), 1.0, "", "Rond", "Cabochon", 11.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.80, (220, 0.30, 0.68), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.20, (0, 0.05, 0.25), 1.0, "", "Poire", "Cabochon", 5.5, None, None),
    ("Agate", BRAZIL_AGATE, 0, 0.30, (28, 0.50, 0.55), 1.0, "", "Ovale", "Cabochon", 7.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.40, (210, 0.20, 0.75), 1.0, "", "Ovale", "Cabochon", 8.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.50, (18, 0.75, 0.70), 1.0, "", "Poire", "Cabochon", 10.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.60, (35, 0.30, 0.70), 1.0, "", "Coussin", "Cabochon", 11.5, None, None),
    ("Agate", BRAZIL_AGATE, 0, 0.70, (225, 0.35, 0.60), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.80, (205, 0.15, 0.80), 1.0, "", "Rond", "Cabochon", 5.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.20, (32, 0.55, 0.50), 1.0, "", "Ovale", "Cabochon", 7.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.30, (25, 0.65, 0.80), 1.0, "", "Poire", "Cabochon", 8.5, None, None),
    ("Agate", BRAZIL_AGATE, 0, 0.40, (0, 0.02, 0.45), 1.0, "", "Ovale", "Cabochon", 10.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.50, (215, 0.22, 0.65), 1.0, "", "Ovale", "Cabochon", 11.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.60, (38, 0.40, 0.75), 1.0, "", "Poire", "Cabochon", 4.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.70, (330, 0.12, 0.72), 1.0, "", "Coussin", "Cabochon", 5.5, None, None),
    ("Agate", BRAZIL_AGATE, 0, 0.80, (20, 0.80, 0.62), 1.0, "", "Ovale", "Cabochon", 7.0, None, None),
    ("Agate", BRAZIL_AGATE, 1, 0.20, (212, 0.28, 0.78), 1.0, "", "Rond", "Cabochon", 8.5, None, None),
    ("Agate", MADAGASCAR_AGATE, 2, 0.30, (30, 0.30, 0.40), 1.0, "", "Ovale", "Cabochon", 10.0, None, None),
    ("Agate", INDIA_AGATE, 3, 0.40, (0, 0.04, 0.80), 1.0, "", "Poire", "Cabochon", 11.5, None, None),
]

INDIA_CARN = ("Inde", 1.0)
BRAZIL_CARN = ("Br\u00e9sil", 1.0)
URUGUAY_CARN = ("Uruguay", 1.0)
MADAGASCAR_CARN = ("Madagascar", 1.0)

# Carnelian: 18 cabochons (clouds, flames, white lines, eyes, cracks change from stone to stone) and 6 faceted stones, semi-opaque with a glassy polish.
CORNALINES = [
    ("Cornaline", INDIA_CARN, 0, 0.20, (10, 0.85, 0.85), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.30, (6, 0.90, 0.70), 1.0, "", "Coussin", "Cabochon", 5.5, None, None),
    ("Cornaline", URUGUAY_CARN, 2, 0.40, (12, 0.80, 0.92), 1.0, "", "Poire", "Cabochon", 7.0, None, None),
    ("Cornaline", MADAGASCAR_CARN, 3, 0.50, (3, 0.90, 0.55), 1.0, "", "Ovale", "Cabochon", 8.5, None, None),
    ("Cornaline", INDIA_CARN, 0, 0.60, (8, 0.88, 0.80), 1.0, "", "Rond", "Cabochon", 10.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.70, (14, 0.75, 0.90), 1.0, "", "Ovale", "Cabochon", 11.5, None, None),
    ("Cornaline", URUGUAY_CARN, 2, 0.80, (7, 0.92, 0.65), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Cornaline", MADAGASCAR_CARN, 3, 0.20, (11, 0.85, 0.75), 1.0, "", "Coussin", "Cabochon", 5.5, None, None),
    ("Cornaline", INDIA_CARN, 0, 0.30, (4, 0.85, 0.60), 1.0, "", "Poire", "Cabochon", 7.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.40, (15, 0.70, 0.95), 1.0, "", "Ovale", "Cabochon", 8.5, None, None),
    ("Cornaline", URUGUAY_CARN, 2, 0.50, (8, 0.90, 0.78), 1.0, "", "Rond", "Cabochon", 10.0, None, None),
    ("Cornaline", MADAGASCAR_CARN, 3, 0.60, (9, 0.80, 0.70), 1.0, "", "Ovale", "Cabochon", 11.5, None, None),
    ("Cornaline", INDIA_CARN, 0, 0.70, (3, 0.88, 0.50), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.80, (13, 0.82, 0.88), 1.0, "", "Coussin", "Cabochon", 5.5, None, None),
    ("Cornaline", URUGUAY_CARN, 2, 0.20, (6, 0.95, 0.72), 1.0, "", "Poire", "Cabochon", 7.0, None, None),
    ("Cornaline", MADAGASCAR_CARN, 3, 0.30, (10, 0.78, 0.82), 1.0, "", "Ovale", "Cabochon", 8.5, None, None),
    ("Cornaline", INDIA_CARN, 0, 0.40, (5, 0.90, 0.62), 1.0, "", "Rond", "Cabochon", 10.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.50, (12, 0.86, 0.86), 1.0, "", "Ovale", "Cabochon", 11.5, None, None),
    ("Cornaline", INDIA_CARN, 0, 0.40, (9, 0.88, 0.80), 1.0, "", "Ovale", "Brillant", 2.0, None, None),
    ("Cornaline", BRAZIL_CARN, 1, 0.40, (6, 0.92, 0.68), 1.0, "", "Coussin", "Mixte", 2.5, None, None),
    ("Cornaline", URUGUAY_CARN, 2, 0.40, (12, 0.82, 0.90), 1.0, "", "Rond", "Brillant", 3.0, None, None),
    ("Cornaline", MADAGASCAR_CARN, 0, 0.40, (7, 0.90, 0.75), 1.0, "", "Rectangle \u00e0 pans coup\u00e9s", "\u00c9meraude", 3.5, None, None),
    ("Cornaline", INDIA_CARN, 1, 0.40, (4, 0.90, 0.60), 1.0, "", "Poire", "Brillant", 4.0, None, None),
    ("Cornaline", BRAZIL_CARN, 2, 0.40, (10, 0.85, 0.85), 1.0, "", "Ovale", "Mixte", 4.5, None, None),
]

CONGO_MAL = ("Congo (RDC)", 1.0)
RUSSIA_MAL = ("Russie (Oural)", 1.8)
ZAMBIA_MAL = ("Zambie", 1.0)
AUSTRALIA_MAL = ("Australie", 1.1)

# Malachite: 20 cabochons (centres of growth, ribbons, seams change from stone to stone) and 4 faceted stones (opaque, a silky polish).
MALACHITES = [
    ("Malachite", CONGO_MAL, 0, 0.20, (145, 0.85, 0.55), 1.0, "", "Ovale", "Cabochon", 5.0, None, None),
    ("Malachite", CONGO_MAL, 1, 0.30, (140, 0.90, 0.45), 1.0, "", "Coussin", "Cabochon", 7.0, None, None),
    ("Malachite", RUSSIA_MAL, 2, 0.40, (150, 0.80, 0.60), 1.0, "", "Poire", "Cabochon", 9.0, None, None),
    ("Malachite", ZAMBIA_MAL, 3, 0.50, (135, 0.85, 0.50), 1.0, "", "Ovale", "Cabochon", 11.0, None, None),
    ("Malachite", AUSTRALIA_MAL, 0, 0.60, (155, 0.90, 0.40), 1.0, "", "Rond", "Cabochon", 13.0, None, None),
    ("Malachite", CONGO_MAL, 1, 0.70, (148, 0.75, 0.65), 1.0, "", "Ovale", "Cabochon", 15.0, None, None),
    ("Malachite", CONGO_MAL, 2, 0.80, (142, 0.88, 0.50), 1.0, "", "Ovale", "Cabochon", 5.0, None, None),
    ("Malachite", RUSSIA_MAL, 3, 0.20, (152, 0.82, 0.55), 1.0, "", "Coussin", "Cabochon", 7.0, None, None),
    ("Malachite", ZAMBIA_MAL, 0, 0.30, (138, 0.90, 0.45), 1.0, "", "Poire", "Cabochon", 9.0, None, None),
    ("Malachite", AUSTRALIA_MAL, 1, 0.40, (146, 0.80, 0.58), 1.0, "", "Ovale", "Cabochon", 11.0, None, None),
    ("Malachite", CONGO_MAL, 2, 0.50, (150, 0.86, 0.48), 1.0, "", "Rond", "Cabochon", 13.0, None, None),
    ("Malachite", CONGO_MAL, 3, 0.60, (144, 0.84, 0.52), 1.0, "", "Ovale", "Cabochon", 15.0, None, None),
    ("Malachite", RUSSIA_MAL, 0, 0.70, (147, 0.92, 0.42), 1.0, "", "Ovale", "Cabochon", 5.0, None, None),
    ("Malachite", ZAMBIA_MAL, 1, 0.80, (141, 0.78, 0.62), 1.0, "", "Coussin", "Cabochon", 7.0, None, None),
    ("Malachite", AUSTRALIA_MAL, 2, 0.20, (153, 0.85, 0.50), 1.0, "", "Poire", "Cabochon", 9.0, None, None),
    ("Malachite", CONGO_MAL, 3, 0.30, (139, 0.88, 0.56), 1.0, "", "Ovale", "Cabochon", 11.0, None, None),
    ("Malachite", CONGO_MAL, 0, 0.40, (149, 0.80, 0.46), 1.0, "", "Rond", "Cabochon", 13.0, None, None),
    ("Malachite", RUSSIA_MAL, 1, 0.50, (143, 0.86, 0.60), 1.0, "", "Ovale", "Cabochon", 15.0, None, None),
    ("Malachite", ZAMBIA_MAL, 2, 0.60, (151, 0.90, 0.52), 1.0, "", "Ovale", "Cabochon", 5.0, None, None),
    ("Malachite", AUSTRALIA_MAL, 3, 0.70, (145, 0.75, 0.55), 1.0, "", "Coussin", "Cabochon", 7.0, None, None),
    ("Malachite", CONGO_MAL, 0, 0.40, (146, 0.85, 0.55), 1.0, "", "Ovale", "Brillant", 3.0, None, None),
    ("Malachite", CONGO_MAL, 1, 0.40, (142, 0.88, 0.48), 1.0, "", "Coussin", "Mixte", 4.0, None, None),
    ("Malachite", RUSSIA_MAL, 2, 0.40, (150, 0.82, 0.58), 1.0, "", "Rectangle \u00e0 pans coup\u00e9s", "\u00c9meraude", 5.0, None, None),
    ("Malachite", ZAMBIA_MAL, 0, 0.40, (144, 0.86, 0.52), 1.0, "", "Rond", "Brillant", 6.0, None, None),
]

IRAN_TQ = ("Iran", 1.6)
USA_TQ = ("\u00c9tats-Unis", 1.2)
CHINA_TQ = ("Chine", 0.8)

# Turquoise: 18 natural cabochons (clean, spiderweb or patchy matrix, drawn from stone to stone) and 6 stabilized.
TURQUOISES = [
    ("Turquoise", IRAN_TQ, 0, 0.20, (188, 0.70, 0.85), 1.0, "", "Ovale", "Cabochon", 3.0, None, None),
    ("Turquoise", USA_TQ, 1, 0.30, (182, 0.60, 0.80), 1.0, "", "Poire", "Cabochon", 4.5, None, None),
    ("Turquoise", CHINA_TQ, 2, 0.40, (192, 0.75, 0.88), 1.0, "", "Coussin", "Cabochon", 6.0, None, None),
    ("Turquoise", IRAN_TQ, 3, 0.50, (178, 0.55, 0.75), 1.0, "", "Ovale", "Cabochon", 7.5, None, None),
    ("Turquoise", USA_TQ, 0, 0.60, (185, 0.65, 0.82), 1.0, "", "Rond", "Cabochon", 9.0, None, None),
    ("Turquoise", CHINA_TQ, 1, 0.70, (175, 0.50, 0.70), 1.0, "", "Ovale", "Cabochon", 10.5, None, None),
    ("Turquoise", IRAN_TQ, 2, 0.80, (190, 0.72, 0.90), 1.0, "", "Ovale", "Cabochon", 3.0, None, None),
    ("Turquoise", USA_TQ, 3, 0.20, (180, 0.60, 0.78), 1.0, "", "Poire", "Cabochon", 4.5, None, None),
    ("Turquoise", CHINA_TQ, 0, 0.30, (186, 0.68, 0.84), 1.0, "", "Coussin", "Cabochon", 6.0, None, None),
    ("Turquoise", IRAN_TQ, 1, 0.40, (172, 0.48, 0.72), 1.0, "", "Ovale", "Cabochon", 7.5, None, None),
    ("Turquoise", USA_TQ, 2, 0.50, (194, 0.70, 0.86), 1.0, "", "Rond", "Cabochon", 9.0, None, None),
    ("Turquoise", CHINA_TQ, 3, 0.60, (183, 0.62, 0.80), 1.0, "", "Ovale", "Cabochon", 10.5, None, None),
    ("Turquoise", IRAN_TQ, 0, 0.70, (189, 0.74, 0.87), 1.0, "", "Ovale", "Cabochon", 3.0, None, None),
    ("Turquoise", USA_TQ, 1, 0.80, (177, 0.52, 0.74), 1.0, "", "Poire", "Cabochon", 4.5, None, None),
    ("Turquoise", CHINA_TQ, 2, 0.20, (184, 0.66, 0.83), 1.0, "", "Coussin", "Cabochon", 6.0, None, None),
    ("Turquoise", IRAN_TQ, 3, 0.30, (170, 0.45, 0.70), 1.0, "", "Ovale", "Cabochon", 7.5, None, None),
    ("Turquoise", USA_TQ, 0, 0.40, (191, 0.70, 0.88), 1.0, "", "Rond", "Cabochon", 9.0, None, None),
    ("Turquoise", CHINA_TQ, 1, 0.50, (181, 0.58, 0.79), 1.0, "", "Ovale", "Cabochon", 10.5, None, None),
    ("Turquoise_Stabilisee", USA_TQ, 0, 0.40, (186, 0.75, 0.86), 1.0, "", "Ovale", "Cabochon", 4.0, None, None),
    ("Turquoise_Stabilisee", CHINA_TQ, 1, 0.40, (180, 0.65, 0.80), 1.0, "", "Poire", "Cabochon", 5.0, None, None),
    ("Turquoise_Stabilisee", USA_TQ, 2, 0.40, (190, 0.78, 0.88), 1.0, "", "Coussin", "Cabochon", 6.0, None, None),
    ("Turquoise_Stabilisee", CHINA_TQ, 0, 0.40, (176, 0.60, 0.76), 1.0, "", "Ovale", "Cabochon", 7.0, None, None),
    ("Turquoise_Stabilisee", USA_TQ, 1, 0.40, (184, 0.70, 0.84), 1.0, "", "Rond", "Cabochon", 8.0, None, None),
    ("Turquoise_Stabilisee", CHINA_TQ, 2, 0.40, (188, 0.72, 0.82), 1.0, "", "Ovale", "Cabochon", 9.0, None, None),
]

ECUADOR_TG = ("\u00c9quateur", 1.0)
COLOMBIA_TG = ("Colombie", 1.0)
PANAMA_TG = ("Panama", 0.9)

# Corozo (vegetable ivory): 18 cut stones, cream to caramel and brown, cabochons and faceted, and 6 whole nuts in their skin (rough).
COROZOS = [
    ("Ivoire_Vegetal", ECUADOR_TG, 0, 0.20, (40, 0.15, 0.94), 1.0, "", "Ovale", "Cabochon", 6.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 1, 0.30, (38, 0.45, 0.88), 1.0, "", "Poire", "Brillant", 8.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 2, 0.40, (32, 0.55, 0.70), 1.0, "", "Ovale", "Mixte", 10.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 3, 0.50, (36, 0.50, 0.85), 1.0, "", "Coussin", "Mixte", 12.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 0, 0.60, (42, 0.30, 0.92), 1.0, "", "Ovale", "Mixte", 14.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 1, 0.70, (30, 0.60, 0.75), 1.0, "", "Coussin", "Mixte", 6.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 2, 0.80, (26, 0.62, 0.48), 1.0, "", "Ovale", "Brillant", 8.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 3, 0.20, (40, 0.40, 0.86), 1.0, "", "Rond", "Cabochon", 10.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 0, 0.30, (34, 0.52, 0.80), 1.0, "", "Poire", "Mixte", 12.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 1, 0.40, (44, 0.20, 0.95), 1.0, "", "Ovale", "Cabochon", 14.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 2, 0.50, (28, 0.58, 0.62), 1.0, "", "Rectangle \u00e0 pans coup\u00e9s", "\u00c9meraude", 6.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 3, 0.60, (37, 0.48, 0.83), 1.0, "", "Ovale", "Mixte", 8.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 0, 0.70, (24, 0.65, 0.42), 1.0, "", "Coussin", "Mixte", 10.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 1, 0.80, (39, 0.35, 0.90), 1.0, "", "Ovale", "Cabochon", 12.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 2, 0.20, (33, 0.55, 0.72), 1.0, "", "Ovale", "Mixte", 14.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 3, 0.30, (41, 0.25, 0.93), 1.0, "", "Poire", "Cabochon", 6.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 0, 0.40, (35, 0.50, 0.78), 1.0, "", "Coussin", "Brillant", 8.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 1, 0.50, (29, 0.60, 0.55), 1.0, "", "Ovale", "Mixte", 10.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 60.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 75.0, None, None),
    ("Ivoire_Vegetal", PANAMA_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 55.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 90.0, None, None),
    ("Ivoire_Vegetal", COLOMBIA_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 68.0, None, None),
    ("Ivoire_Vegetal", ECUADOR_TG, 1, 0.40, (38, 0.15, 0.94), 1.0, "", "Ovale", "Brut", 80.0, None, None),
]

PRESETS = {"rubis": RUBIS, "rubis10": RUBIS[:10], "cabochons": CABOCHONS, "emeraudes": EMERAUDES, "saphirs": SAPHIRS, "spinelles": SPINELLES, "rhodochrosites": RHODOCHROSITES, "agates": AGATES, "cornalines": CORNALINES, "malachites": MALACHITES, "turquoises": TURQUOISES, "corozos": COROZOS}      # rubis10: the ten faceted rubies of Burma, to go with the cabochons in the 24 places
DEFAULT_PRESET = "corozos"


def game_species(species):
    """The sapphire is one species of the game, whatever its colour: the lines below keep the colour name ("Saphir_Rose") for the price and the name."""
    return "Saphir" if species.startswith("Saphir_") else species


def species_text(species, key):
    """A text field of the species asset (the yaml of Unity: a quoted string with \\xE9 style escapes)."""
    path = os.path.join(GEM_DIR, game_species(species) + ".asset")
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
        "definitionName": game_species(species), "specimenId": sid, "clientName": CLIENT, "isDonation": True, "isRough": style == "Brut", "needsCleaning": False,
        "isPersonalSpecimen": True, "isIdentified": True, "hasWindow": False, "cutShape": "" if style == "Brut" else shape, "cutStyle": "" if style == "Brut" else style,
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
