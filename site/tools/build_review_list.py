#!/usr/bin/env python3
"""Writes Docs/RELECTURE_DONNEES.md: one list, by species, of what the gemmologist has to review (the species added or changed since the verification of 2026-11-19):
the values now shown on the site (index, birefringence, density, hardness as ranges; fluorescence, spectrum, Chelsea filter) and what is doubtful.

  python3 site/tools/build_review_list.py"""
import json
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(SITE / "tools"))
from new_species_spec import SPECIES, PATCHES  # noqa: E402

OUT = SITE.parent / "Docs" / "RELECTURE_DONNEES.md"

# what is specifically doubtful, species by species (everything else of the added species: ranges written from the literature as I know it, not from a book at hand)
DOUBTS = {
    "Zircon": "plage 1,925 – 1,984 et densité 4,6 – 4,7 du zircon haut seul (l'ancien tableau donnait tout le zircon : 1,78 – 1,99)",
    "Zircon_Bas": "indice 1,78 – 1,85, densité 3,9 – 4,1, dureté 6 – 6,5 ; spectre (raie à 653,5 nm affaiblie, bande vers 691 nm) ; fluorescence et filtre Chelsea repris du zircon vert du cours ; deux spectres « peu / très abîmé »",
    "Beryl_Bleu": "bande à 628 nm non confirmée par les sources (GIA : bandes entre 500 et 700 nm, large bande vers 690 nm) ; deux spectres (rayon ordinaire / extraordinaire)",
    "Beryl_Vert": "spectre du fer repris de l'aigue-marine ; gisements ; valeur de base",
    "Grenat_Mali": "indice 1,78 – 1,81 (mélange grossulaire – andradite)", "Grenat_Change_Couleur": "indice 1,73 – 1,78 (pyrope – spessartite avec vanadium et chrome)",
    "Saussurite": "roche variable : indice 1,56 – 1,70, densité 2,8 – 3,4 ; gisements", "Sepiolite": "densité apparente très variable (porosité) ; indice 1,506 – 1,529",
    "Bezoard": "très peu de données : indice, densité et dureté sont des valeurs de départ", "Nacre": "plage de l'aragonite (indice 1,52 – 1,68, biréfringence 0,155) ; fluorescence variable",
    "Perle_Conque": "plage de l'aragonite ; gisements", "Ebonite": "caoutchouc durci : indice 1,66, densité 1,15 – 1,35, dureté 2 – 3 et phrase sur l'aiguille chaude à relire", "Vulcanite": "minéral CuTe (telluride de cuivre) : densité 7,1, dureté 1 – 2, jaune bronze ; pas d'indice (opaque) ; un seul gisement",
    "Bakelite": "indice 1,61 – 1,66 et densité 1,25 – 1,50 (avec charges) validés ; fluorescence faible à confirmer", "Swiss_Lapis": "indice et densité : pas de source trouvée (jaspe 2,58 – 2,65)",
    "Chrysocolle": "indice 1,50 – 1,64 très variable (matière peu cristalline)", "Chrysoprase": "spectre (nickel) : aucun trait net retenu",
    "Odontolite": "plage 1,57 – 1,65 (sources 1,57 – 1,63 et 1,61 – 1,65) ; fluorescence non trouvée", "Scheelite": "dispersion 0,026 ; fluorescence UV courts à vérifier selon l'origine",
    "Rutile_Synthetique": "couleurs ajoutées (jaune, orange, bleu foncé à noir réduit) à relire ; dispersion 0,33 validée", "Anatase": "pléochroïsme (bleu foncé / jaune pâle) et répartition des gisements (Pakistan, Russie ajoutés) à relire", "Brookite": "biréfringence resserrée à 0,12 – 0,158 ; pléochroïsme et répartition des gisements (Maroc, Suisse, Norvège ajoutés) à relire",
    "Variscite": "biréfringence 0 – 0,04 (agrégat)", "Charoite": "biréfringence 0,003 – 0,011 (agrégat)", "Azurite": "indice 1,730 – 1,838 (la plage s'applique aux cristaux)",
    "Hiddenite": "spectre du chrome : raies dans le rouge à 646, 669 et 686 nm", "Enstatite": "bandes du fer et du chrome",
    "Diaspore_Zultanite": "deux grilles de couleur (jour / incandescence) : tables de teinte de l'outil, pas des mesures", "Scapolite": "couleurs rose à violet ajoutées (modes de couleur)",
}


def _span(r, digits):
    low, high = r
    fmt = "{:." + str(digits) + "f}"
    return fmt.format(low) if abs(high - low) < 0.0005 else fmt.format(low) + " – " + fmt.format(high)


def main():
    data = json.loads((SITE / "data" / "gemlab_data.json").read_text(encoding="utf-8"))
    extra = json.loads((SITE / "data" / "extra_species.json").read_text(encoding="utf-8"))["species"]
    species = {s["id"]: s for s in data["species"] + extra}
    ranges = {name: json.loads((SITE / "data" / (name + "_ranges.json")).read_text(encoding="utf-8")) for name in ("refractive", "birefringence", "density", "hardness")}
    spectra = {e["key"]: e for e in data["spectra"]["entries"]}
    chelsea = {e["key"]: e for e in data["chelsea"]["entries"]}
    ids = [s["id"] for s in SPECIES] + [i for i in PATCHES if i not in {s["id"] for s in SPECIES}]
    lines = ["# Relecture des données (espèces ajoutées ou modifiées)", "",
             "Établie par `python3 site/tools/build_review_list.py`. Pour chaque espèce : ce que le site montre aujourd'hui (plages en gras = à vérifier d'abord) et ce qui est douteux. "
             "Toutes les plages des espèces ajoutées viennent de la littérature **telle que je la connais**, pas d'un livre sous la main ; les sources trouvées sont citées dans les notes. "
             "Corrige directement dans `site/tools/build_refractive_ranges.py` (tableaux `MANUAL*`) et `site/tools/new_species_spec.py`, puis relance les outils (README, sections 15 à 27).", "",
             "| Espèce | Indice | Biréfringence | Densité | Dureté | Fluorescence UV longs / courts | Spectre | Chelsea | À vérifier |", "|---|---|---|---|---|---|---|---|---|"]
    for ident in ids:
        s = species.get(ident)
        if not s:
            continue
        spec = spectra.get(ident)
        ch = chelsea.get(ident)
        spec_text = (str(len(spec["lines"])) + " raie(s)") if spec and spec.get("lines") else "—"
        row = [s["displayName"] + (" (site seul)" if s in extra else ""),
               _span(ranges["refractive"][ident], 3), _span(ranges["birefringence"][ident], 3), _span(ranges["density"][ident], 2), _span(ranges["hardness"][ident], 1),
               "{} / {}".format((s.get("fluorescenceLongWave") or "—")[:40], (s.get("fluorescenceShortWave") or "—")[:40]), spec_text, (ch["reaction"][:40] if ch else (s.get("chelseaFilterReaction") or "—")[:40]),
               DOUBTS.get(ident, "plages et valeurs typiques, gisements")]
        lines.append("| " + " | ".join(c.replace("|", "/") for c in row) + " |")
    lines += ["", "## Ce qui vaut pour toutes les espèces ajoutées", "",
              "- Fluorescence : « aucune réaction » quand je n'avais pas de source (ce n'est pas la même chose que « mesurée inerte »).",
              "- Gisements et poids de rareté : des valeurs de jeu, pas des statistiques.",
              "- Valeur de base : une valeur de jeu, pas un prix.", ""]
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print("{} espèces dans {}".format(len(ids), OUT))


if __name__ == "__main__":
    main()
