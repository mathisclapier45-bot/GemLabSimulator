#!/usr/bin/env python3
"""Writes site/data/refractive_ranges.json, birefringence_ranges.json, density_ranges.json and hardness_ranges.json: for each species, the lowest and the highest refractive index it can show (the readings of the refractometer: the two rays of a
uniaxial stone, the lowest and the highest of a biaxial one, and the variation between specimens). The species page shows "min - max" instead of one value (user, 2026-11-20); the birefringence too (the difference between the highest and the lowest index of a stone).

Where the ranges come from:
  * the 120 species checked on 2026-11-19: the table of Docs/VERIF_PROPRIETES.md (the widest plausible range of the literature);
  * the species added since: the table MANUAL below (typical ranges of the literature, to be reviewed by the gemmologist);
  * a few species whose range is narrower than the one of the old table (the high zircon, the blue beryl).
A test checks that every species has a range and that the index of the game lies inside it.

  python3 site/tools/build_refractive_ranges.py"""
import json
import re
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
DOC = SITE.parent / "Docs" / "VERIF_PROPRIETES.md"

# names of the old table that are now another species
ALIASES = {"Saphir Bleu": "Saphir", "Saphir Incolore": "Saphir", "Saphir Jaune": "Saphir", "Saphir Orange": "Saphir", "Saphir Rose": "Saphir", "Saphir Vert": "Saphir",
           "Saphir Violet": "Saphir", "Fluorite bleue": "Fluorite", "Fluorite jaune": "Fluorite", "Fluorite verte": "Fluorite", "Quartz": "Cristal de roche"}

# id -> (lowest, highest): ranges of the literature, for the species the old table does not have or has too wide
MANUAL = {
    "Zircon": (1.925, 1.984),                      # high zircon (the old table gave the whole zircon: 1.78 - 1.99)
    "Zircon_Bas": (1.78, 1.85),                    # low zircon, also called metamict
    "Beryl_Bleu": (1.583, 1.592),                  # maxixe (GIA, Gems & Gemology 2020)
    "Beryl_Vert": (1.567, 1.590),                  # beryl colored by iron, like aquamarine
    "Grenat_Mali": (1.78, 1.81), "Grenat_Topazolite": (1.855, 1.895), "Grenat_Pyrope_Chromifere": (1.714, 1.75), "Grenat_Change_Couleur": (1.73, 1.78),
    "Grenat_Grossulaire": (1.73, 1.76), "Grenat_Andradite": (1.855, 1.895), "Saphir_Change_Couleur": (1.759, 1.781),
    "Amazonite": (1.518, 1.542), "Pierre_Soleil": (1.525, 1.548), "Hiddenite": (1.655, 1.682), "Tourmaline_Verdelite": (1.61, 1.675), "Tourmaline_Dravite": (1.61, 1.675),
    "Aventurine": (1.544, 1.553), "Jaspe": (1.53, 1.545), "Onyx": (1.53, 1.54), "Enstatite": (1.65, 1.68), "Larimar": (1.59, 1.645), "Charoite": (1.55, 1.559),
    "Sugilite": (1.607, 1.61), "Azurite": (1.73, 1.838), "Chrysocolle": (1.5, 1.64), "Variscite": (1.55, 1.59), "Lepidolite": (1.525, 1.58), "Benitoite": (1.757, 1.804),
    "Grandidierite": (1.59, 1.639), "Diaspore_Zultanite": (1.682, 1.752), "Euclase": (1.652, 1.673), "Danburite": (1.627, 1.641), "Kornerupine": (1.665, 1.699),
    "Odontolite": (1.57, 1.65), "Bezoard": (1.5, 1.59), "Nacre": (1.52, 1.68), "Vulcanite": (0.0, 0.0), "Ebonite": (1.66, 1.66), "Perle_Conque": (1.5, 1.68), "Marbre": (1.486, 1.658),
    "Bakelite": (1.61, 1.66), "Swiss_Lapis": (1.53, 1.545), "Rutile_Synthetique": (2.616, 2.903), "Anatase": (2.488, 2.561), "Brookite": (2.583, 2.741),
    "Chrysoprase": (1.53, 1.54), "Albatre": (1.519, 1.53), "Sepiolite": (1.506, 1.529), "Saussurite": (1.56, 1.70), "Scheelite": (1.918, 1.937), "Spodumene": (1.648, 1.682),
}


# id -> (lowest, highest) birefringence, for the species the old table does not have or has too wide (ranges of the literature, to be reviewed by the gemmologist)
MANUAL_BIR = {
    "Zircon": (0.04, 0.059), "Zircon_Bas": (0.0, 0.008), "Beryl_Bleu": (0.007, 0.01), "Beryl_Vert": (0.004, 0.009), "Saphir_Change_Couleur": (0.008, 0.009),
    "Grenat_Mali": (0.0, 0.0), "Grenat_Topazolite": (0.0, 0.0), "Grenat_Pyrope_Chromifere": (0.0, 0.0), "Grenat_Change_Couleur": (0.0, 0.0), "Grenat_Grossulaire": (0.0, 0.0),
    "Grenat_Andradite": (0.0, 0.0), "Amazonite": (0.006, 0.01), "Pierre_Soleil": (0.006, 0.01), "Hiddenite": (0.014, 0.016), "Tourmaline_Verdelite": (0.014, 0.021),
    "Tourmaline_Dravite": (0.018, 0.03), "Aventurine": (0.009, 0.009), "Jaspe": (0.0, 0.0), "Onyx": (0.0, 0.0), "Enstatite": (0.009, 0.011), "Larimar": (0.036, 0.04),
    "Charoite": (0.003, 0.011), "Sugilite": (0.0, 0.003), "Azurite": (0.1, 0.11), "Chrysocolle": (0.0, 0.04), "Variscite": (0.0, 0.04), "Lepidolite": (0.018, 0.038),
    "Benitoite": (0.047, 0.047), "Grandidierite": (0.03, 0.04), "Diaspore_Zultanite": (0.04, 0.052), "Euclase": (0.019, 0.021), "Danburite": (0.006, 0.008),
    "Kornerupine": (0.012, 0.02), "Chrysoprase": (0.004, 0.009), "Albatre": (0.009, 0.01), "Sepiolite": (0.0, 0.0), "Saussurite": (0.0, 0.0), "Scheelite": (0.016, 0.019),
    "Spodumene": (0.014, 0.027),
    "Odontolite": (0.0, 0.04), "Bezoard": (0.0, 0.0), "Nacre": (0.155, 0.155), "Vulcanite": (0.0, 0.0), "Ebonite": (0.0, 0.0), "Perle_Conque": (0.155, 0.155), "Marbre": (0.172, 0.172),
    "Bakelite": (0.0, 0.0), "Swiss_Lapis": (0.0, 0.0), "Rutile_Synthetique": (0.287, 0.287), "Anatase": (0.073, 0.073), "Brookite": (0.12, 0.158),
}


# id -> (lowest, highest) density and hardness (Mohs), for the species the old table does not have (ranges of the literature, to be reviewed by the gemmologist)
MANUAL_DENS = {
    "Zircon": (4.6, 4.7), "Zircon_Bas": (3.9, 4.1), "Beryl_Bleu": (2.7, 2.8), "Beryl_Vert": (2.65, 2.8),
    "Grenat_Mali": (3.6, 3.8), "Grenat_Topazolite": (3.7, 4.1), "Grenat_Pyrope_Chromifere": (3.6, 3.8), "Grenat_Change_Couleur": (3.7, 4.0), "Grenat_Grossulaire": (3.57, 3.73),
    "Grenat_Andradite": (3.7, 4.1), "Saphir_Change_Couleur": (3.95, 4.1), "Amazonite": (2.54, 2.58), "Pierre_Soleil": (2.62, 2.67), "Hiddenite": (3.15, 3.2),
    "Tourmaline_Verdelite": (3.0, 3.25), "Tourmaline_Dravite": (3.0, 3.25), "Aventurine": (2.64, 2.69), "Jaspe": (2.58, 2.91), "Onyx": (2.58, 2.64), "Enstatite": (3.1, 3.3),
    "Larimar": (2.74, 2.9), "Charoite": (2.54, 2.78), "Sugilite": (2.74, 2.8), "Azurite": (3.77, 3.89), "Chrysocolle": (2.0, 2.4), "Variscite": (2.2, 2.6), "Lepidolite": (2.8, 2.9),
    "Benitoite": (3.64, 3.69), "Grandidierite": (2.85, 3.0), "Diaspore_Zultanite": (3.3, 3.5), "Euclase": (3.05, 3.12), "Danburite": (2.97, 3.02), "Kornerupine": (3.27, 3.45),
    "Chrysoprase": (2.58, 2.64), "Albatre": (2.3, 2.33), "Sepiolite": (1.0, 2.3), "Saussurite": (2.8, 3.4), "Scheelite": (5.9, 6.1), "Spodumene": (3.15, 3.2),
    "Odontolite": (3.0, 3.25), "Bezoard": (1.3, 1.7), "Nacre": (2.65, 2.78), "Vulcanite": (7.09, 7.1), "Ebonite": (1.15, 1.35), "Perle_Conque": (2.8, 2.9), "Marbre": (2.6, 2.8), "Bakelite": (1.25, 1.5),
    "Swiss_Lapis": (2.58, 2.65), "Rutile_Synthetique": (4.25, 4.3), "Anatase": (3.82, 3.95), "Brookite": (4.08, 4.18),
}
MANUAL_HARD = {
    "Zircon": (7.0, 7.5), "Zircon_Bas": (6.0, 6.5), "Beryl_Bleu": (7.5, 8.0), "Beryl_Vert": (7.5, 8.0),
    "Grenat_Mali": (6.5, 7.0), "Grenat_Topazolite": (6.5, 7.0), "Grenat_Pyrope_Chromifere": (7.0, 7.5), "Grenat_Change_Couleur": (7.0, 7.5), "Grenat_Grossulaire": (6.5, 7.5),
    "Grenat_Andradite": (6.5, 7.0), "Saphir_Change_Couleur": (9.0, 9.0), "Amazonite": (6.0, 6.5), "Pierre_Soleil": (6.0, 6.5), "Hiddenite": (6.5, 7.0),
    "Tourmaline_Verdelite": (7.0, 7.5), "Tourmaline_Dravite": (7.0, 7.5), "Aventurine": (6.5, 7.0), "Jaspe": (6.5, 7.0), "Onyx": (6.5, 7.0), "Enstatite": (5.0, 6.0),
    "Larimar": (4.5, 5.0), "Charoite": (5.0, 6.0), "Sugilite": (5.5, 6.5), "Azurite": (3.5, 4.0), "Chrysocolle": (2.0, 4.0), "Variscite": (3.5, 5.0), "Lepidolite": (2.5, 3.0),
    "Benitoite": (6.0, 6.5), "Grandidierite": (7.0, 7.5), "Diaspore_Zultanite": (6.5, 7.0), "Euclase": (7.0, 7.5), "Danburite": (7.0, 7.5), "Kornerupine": (6.0, 7.0),
    "Chrysoprase": (6.5, 7.0), "Albatre": (1.5, 2.0), "Sepiolite": (2.0, 2.5), "Saussurite": (6.5, 7.0), "Scheelite": (4.5, 5.0), "Spodumene": (6.5, 7.0),
    "Odontolite": (5.0, 5.0), "Bezoard": (2.0, 3.0), "Nacre": (2.5, 4.5), "Vulcanite": (1.0, 2.0), "Ebonite": (2.0, 3.0), "Perle_Conque": (3.5, 4.0), "Marbre": (3.0, 3.5), "Bakelite": (2.0, 3.0),
    "Swiss_Lapis": (6.5, 7.0), "Rutile_Synthetique": (6.0, 6.5), "Anatase": (5.5, 6.0), "Brookite": (5.5, 6.0),
}


def read_table():
    """{displayName of the old table: ((lowest, highest) index, birefringence, density, hardness)}."""
    num = r"([\d.]+)"
    cell = re.compile(r"^" + num + r" \(" + num + r"(?:\s*[–-]\s*" + num + r")?\)")
    out = {}
    for line in DOC.read_text(encoding="utf-8").split("\n"):
        if not line.startswith("| "):
            continue
        parts = [p.strip().replace("**", "") for p in line.strip().strip("|").split("|")]
        if len(parts) != 5:
            continue
        cells = [cell.match(p) for p in parts[1:]]
        if all(cells):
            def span(c):
                value, low, high = c.groups()
                return (float(low), float(high if high else low))
            out[parts[0]] = (span(cells[0]), span(cells[3]), span(cells[1]), span(cells[2]))
    return out


def main():
    data = json.loads((SITE / "data" / "gemlab_data.json").read_text(encoding="utf-8"))
    extra = json.loads((SITE / "data" / "extra_species.json").read_text(encoding="utf-8"))["species"]
    species = {s["id"]: s for s in data["species"] + extra}
    by_name = {s["displayName"]: s["id"] for s in species.values()}
    table = read_table()
    out, bir, dens, hard = {}, {}, {}, {}
    for name, ((low, high), (blow, bhigh), (dlow, dhigh), (hlow, hhigh)) in table.items():
        ident = by_name.get(ALIASES.get(name, name))
        if ident:
            old = out.get(ident)
            out[ident] = [min(old[0], low), max(old[1], high)] if old else [low, high]
            old = bir.get(ident)
            bir[ident] = [min(old[0], blow), max(old[1], bhigh)] if old else [blow, bhigh]
            for target, (lo, hi) in ((dens, (dlow, dhigh)), (hard, (hlow, hhigh))):
                old = target.get(ident)
                target[ident] = [min(old[0], lo), max(old[1], hi)] if old else [lo, hi]
    for ident, (low, high) in MANUAL_BIR.items():
        bir[ident] = [low, high]
    for ident, (low, high) in MANUAL_DENS.items():
        dens[ident] = [low, high]
    for ident, (low, high) in MANUAL_HARD.items():
        hard[ident] = [low, high]
    for ident, (low, high) in MANUAL.items():
        out[ident] = [low, high]
    missing = [i for i in species if i not in out]
    for ident in missing:                               # no range anywhere: the index of the game, widened by the birefringence
        s = species[ident]
        half = (s.get("birefringence") or 0) / 2.0
        out[ident] = [round(s["refractiveIndex"] - half, 3), round(s["refractiveIndex"] + half, 3)]
    outside = [i for i, (low, high) in out.items() if i in species and not (low - 0.0005 <= species[i]["refractiveIndex"] <= high + 0.0005)]
    path = SITE / "data" / "refractive_ranges.json"
    path.write_text(json.dumps({k: out[k] for k in sorted(out) if k in species}, indent=0), encoding="utf-8")
    for ident in species:                              # no range: the birefringence of the game alone
        bir.setdefault(ident, [species[ident].get("birefringence") or 0.0] * 2)
    bad = [i for i, (low, high) in bir.items() if i in species and not (low - 0.0005 <= (species[i].get("birefringence") or 0.0) <= high + 0.0005)]
    (SITE / "data" / "birefringence_ranges.json").write_text(json.dumps({k: bir[k] for k in sorted(bir) if k in species}, indent=0), encoding="utf-8")
    print("birefringence : {} dont la valeur du jeu est hors plage : {}".format(len(bad), ", ".join(bad)))
    for name, table_, field in (("density", dens, "density"), ("hardness", hard, "hardness")):
        for ident in species:
            table_.setdefault(ident, [species[ident].get(field) or 0.0] * 2)
        out_of = [i for i, (lo, hi) in table_.items() if i in species and not (lo - 0.0005 <= (species[i].get(field) or 0.0) <= hi + 0.0005)]
        (SITE / "data" / (name + "_ranges.json")).write_text(json.dumps({k: table_[k] for k in sorted(table_) if k in species}, indent=0), encoding="utf-8")
        print("{} : {} dont la valeur du jeu est hors plage : {}".format(name, len(out_of), ", ".join(out_of)))
    print("{} espèces, {} sans plage (calculées), {} dont l'indice du jeu est hors plage : {}".format(len(species), len(missing), len(outside), ", ".join(outside)))


if __name__ == "__main__":
    main()
