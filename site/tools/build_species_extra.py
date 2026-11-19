#!/usr/bin/env python3
"""Writes site/data/species_extra.json: crystal system and chemical composition of each species.
These facts are not in the game export, so they are written here by hand (standard mineralogy).
Edit this table, then run:  python3 site/tools/build_species_extra.py
System codes: cubic, tetragonal, hexagonal, trigonal, orthorhombic, monoclinic, triclinic, amorphous, organic.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

OUT = Path(__file__).resolve().parent.parent / "data" / "species_extra.json"

BERYL = ("hexagonal", "Be₃Al₂Si₆O₁₈", "Silicate de béryllium et d'aluminium")
QUARTZ = ("trigonal", "SiO₂", "Dioxyde de silicium (quartz)")
CORUNDUM = ("trigonal", "Al₂O₃", "Oxyde d'aluminium (corindon)")
FLUORITE = ("cubic", "CaF₂", "Fluorure de calcium")
DIAMOND = ("cubic", "C", "Carbone pur")
SPINEL = ("cubic", "MgAl₂O₄", "Aluminate de magnésium")
GLASS = ("amorphous", "SiO₂ + Na₂O + CaO (variable)", "Verre : silice et fondants, sans structure cristalline")
IVORY = ("organic", "Ca₅(PO₄)₃(OH) + collagène", "Dentine : phosphate de calcium et protéine")
KERATIN = ("organic", "Kératine (protéine)", "Matière organique, comme les ongles et les cheveux")
ELBAITE = ("trigonal", "Na(Li,Al)₃Al₆(BO₃)₃Si₆O₁₈(OH)₄", "Borosilicate complexe (tourmaline, variété elbaïte)")
GARNET = lambda f, n: ("cubic", f, n)

T = {
 "Agate": ("trigonal", "SiO₂", "Calcédoine : quartz microcristallin en bandes, souvent avec un peu d'eau"),
 "Aigue_Marine": BERYL, "Beryl_Bleu": BERYL, "Beryl_Rouge": ("hexagonal", "Be₃Al₂Si₆O₁₈ (Mn)", "Béryl coloré par le manganèse"),
 "Ambre": ("amorphous", "≈ C₁₀H₁₆O", "Résine fossile durcie, sans structure cristalline"),
 "Amethyste": ("trigonal", "SiO₂ (Fe)", "Quartz coloré par le fer et l'irradiation"),
 "Ametrine": ("trigonal", "SiO₂ (Fe)", "Quartz bicolore : zones violettes et jaunes"),
 "Andalousite": ("orthorhombic", "Al₂SiO₅", "Silicate d'aluminium"),
 "Apatite": ("hexagonal", "Ca₅(PO₄)₃(F,Cl,OH)", "Phosphate de calcium"),
 "CZ": ("cubic", "ZrO₂ (+ Y₂O₃)", "Oxyde de zirconium synthétique, stabilisé à l'yttrium"),
 "Calcite": ("trigonal", "CaCO₃", "Carbonate de calcium"),
 "Celestine": ("orthorhombic", "SrSO₄", "Sulfate de strontium"),
 "Chrysoberyl": ("orthorhombic", "BeAl₂O₄", "Oxyde de béryllium et d'aluminium"),
 "Chrysoberyl_Alexandrite": ("orthorhombic", "BeAl₂O₄ (Cr)", "Chrysobéryl coloré par le chrome : change de couleur"),
 "Citrine": ("trigonal", "SiO₂ (Fe)", "Quartz jaune à orangé"),
 "Corail": ("organic", "CaCO₃ (calcite) + conchioline", "Squelette de polypes : carbonate de calcium et matière organique"),
 "Corindon_Rubis": ("trigonal", "Al₂O₃ (Cr)", "Corindon coloré par le chrome"),
 "Corindon_Synthetique_Rubis": ("trigonal", "Al₂O₃ (Cr)", "Corindon de synthèse coloré par le chrome"),
 "Corindon_Synthetique_Saphir": ("trigonal", "Al₂O₃ (Fe, Ti)", "Corindon de synthèse coloré par le fer et le titane"),
 "Cornaline": ("trigonal", "SiO₂ (Fe)", "Calcédoine rouge-orangé colorée par des oxydes de fer"),
 "Corne_Rhinoceros": KERATIN, "Ecaille_Tortue": KERATIN,
 "Defense_Narval": IVORY, "Ivoire_Elephant": IVORY, "Ivoire_Hippopotame": IVORY, "Ivoire_Mammouth": IVORY, "Ivoire_Morse": IVORY,
 "Ivoire_Vegetal": ("organic", "Cellulose", "Albumen de la graine du palmier tagua"),
 "Diamant": DIAMOND, "Diamant_Synthetique_CVD": DIAMOND, "Diamant_Synthetique_HPHT": DIAMOND,
 "Diopside_Chromifere": ("monoclinic", "CaMgSi₂O₆ (Cr)", "Silicate de calcium et de magnésium coloré par le chrome"),
 "Emeraude": ("hexagonal", "Be₃Al₂Si₆O₁₈ (Cr, V)", "Béryl coloré par le chrome et le vanadium"),
 "Fluorite": FLUORITE, "Fluorite_Bandee": FLUORITE, "Fluorite_Blue_John": FLUORITE,
 "GGG": ("cubic", "Gd₃Ga₅O₁₂", "Grenat synthétique de gadolinium et de gallium"),
 "Goshenite": BERYL, "Heliodore": ("hexagonal", "Be₃Al₂Si₆O₁₈ (Fe³⁺)", "Béryl jaune coloré par le fer"),
 "Morganite": ("hexagonal", "Be₃Al₂Si₆O₁₈ (Mn)", "Béryl rose coloré par le manganèse"),
 "Grenat_Almandin": GARNET("Fe₃Al₂(SiO₄)₃", "Silicate de fer et d'aluminium"),
 "Grenat_Demantoide": GARNET("Ca₃Fe₂(SiO₄)₃", "Silicate de calcium et de fer (andradite)"),
 "Grenat_Hessonite": GARNET("Ca₃Al₂(SiO₄)₃", "Silicate de calcium et d'aluminium (grossulaire)"),
 "Grenat_Hydrogrossulaire": GARNET("Ca₃Al₂(SiO₄)₃₋ₓ(OH)₄ₓ", "Grossulaire où des groupes OH remplacent des silicates"),
 "Grenat_Malaya": GARNET("(Mg,Mn,Ca)₃Al₂(SiO₄)₃", "Mélange de pyrope et de spessartite"),
 "Saphir_Change_Couleur": ("trigonal", "Al₂O₃ (Cr,V)", "Oxyde d'aluminium (corindon) coloré par le chrome et le vanadium"),
 "Grenat_Grossulaire": GARNET("Ca₃Al₂(SiO₄)₃", "Silicate de calcium et d'aluminium (grossulaire)"),
 "Grenat_Andradite": GARNET("Ca₃Fe₂(SiO₄)₃", "Silicate de calcium et de fer (andradite)"),
 "Grenat_Mali": GARNET("Ca₃(Al,Fe)₂(SiO₄)₃", "Mélange de grossulaire et d'andradite"),
 "Grenat_Topazolite": GARNET("Ca₃Fe₂(SiO₄)₃", "Silicate de calcium et de fer (andradite jaune)"),
 "Grenat_Pyrope_Chromifere": GARNET("Mg₃(Al,Cr)₂(SiO₄)₃", "Pyrope coloré par le chrome"),
 "Grenat_Change_Couleur": GARNET("(Mg,Mn)₃(Al,V,Cr)₂(SiO₄)₃", "Mélange de pyrope et de spessartite, avec vanadium et chrome"),
 "Grenat_Pyrope": GARNET("Mg₃Al₂(SiO₄)₃", "Silicate de magnésium et d'aluminium"),
 "Grenat_Rhodolite": GARNET("(Mg,Fe)₃Al₂(SiO₄)₃", "Mélange de pyrope et d'almandin"),
 "Grenat_Spessartite": GARNET("Mn₃Al₂(SiO₄)₃", "Silicate de manganèse et d'aluminium"),
 "Grenat_Tsavorite": GARNET("Ca₃Al₂(SiO₄)₃ (V, Cr)", "Grossulaire vert coloré par le vanadium et le chrome"),
 "Grenat_Uvarovite": GARNET("Ca₃Cr₂(SiO₄)₃", "Silicate de calcium et de chrome"),
 "Gypse": ("monoclinic", "CaSO₄·2H₂O", "Sulfate de calcium hydraté"), "Selenite": ("monoclinic", "CaSO₄·2H₂O", "Gypse transparent, sulfate de calcium hydraté"),
 "Hematite": ("trigonal", "Fe₂O₃", "Oxyde de fer"),
 "Iolite": ("orthorhombic", "Mg₂Al₄Si₅O₁₈", "Silicate de magnésium et d'aluminium (cordiérite)"),
 "Iolite_Bloodshot": ("orthorhombic", "Mg₂Al₄Si₅O₁₈ + Fe₂O₃", "Cordiérite avec des plaquettes d'hématite"),
 "Jade_Jadeite": ("monoclinic", "NaAlSi₂O₆", "Pyroxène sodique : agrégat de cristaux microscopiques"),
 "Jade_Nephrite": ("monoclinic", "Ca₂(Mg,Fe)₅Si₈O₂₂(OH)₂", "Amphibole : agrégat de fibres microscopiques"),
 "Jais": ("organic", "Carbone (lignite)", "Bois fossile compressé"),
 "Kunzite": ("monoclinic", "LiAlSi₂O₆ (Mn)", "Spodumène rose coloré par le manganèse"),
 "Kyanite": ("triclinic", "Al₂SiO₅", "Silicate d'aluminium"),
 "Labradorite": ("triclinic", "(Na,Ca)Al(Al,Si)Si₂O₈", "Feldspath plagioclase"),
 "Lapis_Lazuli": ("cubic", "(Na,Ca)₈(AlSiO₄)₆(S,SO₄,Cl)", "Roche : lazurite, avec calcite et pyrite"),
 "Malachite": ("monoclinic", "Cu₂CO₃(OH)₂", "Carbonate de cuivre"),
 "Moissanite_Synthetique": ("hexagonal", "SiC", "Carbure de silicium"),
 "Moldavite": ("amorphous", "SiO₂ (≈ 80 %) + Al₂O₃", "Verre naturel formé par un impact de météorite"),
 "Obsidienne": ("amorphous", "SiO₂ (≈ 70 %) + Al₂O₃, K₂O, Na₂O", "Verre volcanique"),
 "Obsidienne_Flocons": ("amorphous", "SiO₂ (≈ 70 %) + cristobalite", "Verre volcanique avec des flocons de cristobalite"),
 "Oeil_de_Tigre": ("trigonal", "SiO₂ + oxydes de fer", "Quartz fibreux remplaçant de la crocidolite"),
 "Opale_De_Feu": ("amorphous", "SiO₂·nH₂O", "Silice hydratée, sans structure cristalline"),
 "Opale_Noble": ("amorphous", "SiO₂·nH₂O", "Silice hydratée en sphères ordonnées : jeu de couleurs"),
 "Orthose": ("monoclinic", "KAlSi₃O₈", "Feldspath potassique"),
 "Pierre_De_Lune": ("monoclinic", "KAlSi₃O₈ + NaAlSi₃O₈", "Feldspath en lamelles alternées : reflet bleuté"),
 "Padparadscha": ("trigonal", "Al₂O₃ (Cr, Fe)", "Saphir rose-orangé"),
 "Painite": ("hexagonal", "CaZrAl₉O₁₅(BO₃)", "Borate de calcium, zirconium et aluminium"),
 "Perle": ("organic", "CaCO₃ (aragonite) + conchioline", "Couches de nacre sécrétées par un mollusque"),
 "Perle_Tahiti": ("organic", "CaCO₃ (aragonite) + conchioline", "Couches de nacre sécrétées par une huître à lèvres noires"),
 "Pezzottaite": ("trigonal", "Cs(Be₂Li)Al₂Si₆O₁₈", "Silicate de césium, béryllium et lithium"),
 "Prasiolite": QUARTZ, "Quartz": QUARTZ, "Quartz_Fume": ("trigonal", "SiO₂ (Al)", "Quartz fumé par irradiation naturelle"),
 "Quartz_Rose": ("trigonal", "SiO₂ (Ti, Fe, Mn)", "Quartz rose, souvent laiteux"),
 "Prehnite": ("orthorhombic", "Ca₂Al₂Si₃O₁₀(OH)₂", "Silicate hydraté de calcium et d'aluminium"),
 "Pyrite": ("cubic", "FeS₂", "Sulfure de fer"),
 "Rhodocrosite": ("trigonal", "MnCO₃", "Carbonate de manganèse"),
 "Rhodonite": ("triclinic", "(Mn,Fe,Ca)SiO₃", "Silicate de manganèse"),
 "Saphir": ("trigonal", "Al₂O₃ (Fe, Ti, Cr, V)", "Oxyde d'aluminium (corindon) coloré par le fer, le titane, le chrome ou le vanadium selon la couleur"),
 "Scapolite": ("tetragonal", "(Na,Ca)₄Al₃(Al,Si)₃Si₆O₂₄(Cl,CO₃,SO₄)", "Silicate complexe"),
 "Serpentine_Bowenite": ("monoclinic", "(Mg,Fe)₃Si₂O₅(OH)₄", "Silicate de magnésium hydraté (antigorite)"),
 "Sinhalite": ("orthorhombic", "MgAlBO₄", "Borate de magnésium et d'aluminium"),
 "Sodalite": ("cubic", "Na₈Al₆Si₆O₂₄Cl₂", "Silicate de sodium et d'aluminium avec du chlore"),
 "Sphalerite": ("cubic", "ZnS", "Sulfure de zinc"),
 "Sphene": ("monoclinic", "CaTiSiO₅", "Silicate de calcium et de titane (titanite)"),
 "Spinelle": SPINEL, "Spinelle_Bleu": ("cubic", "MgAl₂O₄ (Co, Fe)", "Spinelle coloré par le cobalt ou le fer"),
 "Steatite": ("monoclinic", "Mg₃Si₄O₁₀(OH)₂", "Talc : silicate de magnésium hydraté"),
 "Taaffeite": ("hexagonal", "BeMgAl₄O₈", "Oxyde de béryllium, magnésium et aluminium"),
 "Tanzanite": ("orthorhombic", "Ca₂Al₃(SiO₄)(Si₂O₇)O(OH) (V)", "Zoïsite bleue colorée par le vanadium"),
 "Zoisite": ("orthorhombic", "Ca₂Al₃(SiO₄)(Si₂O₇)O(OH)", "Silicate de calcium et d'aluminium"),
 "Titanate_Strontium": ("cubic", "SrTiO₃", "Oxyde synthétique de strontium et de titane"),
 "Topaze": ("orthorhombic", "Al₂SiO₄(F,OH)₂", "Silicate d'aluminium avec du fluor"),
 "Tourmaline_Indicolite": ELBAITE, "Tourmaline_Melon_Eau": ELBAITE, "Tourmaline_Paraiba": ("trigonal", "Na(Li,Al)₃Al₆(BO₃)₃Si₆O₁₈(OH)₄ (Cu, Mn)", "Elbaïte colorée par le cuivre"),
 "Tourmaline_Rose": ELBAITE,
 "Tourmaline_Schorl": ("trigonal", "NaFe₃Al₆(BO₃)₃Si₆O₁₈(OH)₄", "Tourmaline noire riche en fer"),
 "Turquoise": ("triclinic", "CuAl₆(PO₄)₄(OH)₈·4H₂O", "Phosphate hydraté de cuivre et d'aluminium"),
 "Turquoise_Stabilisee": ("triclinic", "CuAl₆(PO₄)₄(OH)₈·4H₂O + résine", "Turquoise imprégnée de résine"),
 "Verre": GLASS, "YAG": ("cubic", "Y₃Al₅O₁₂", "Grenat synthétique d'yttrium et d'aluminium"),
 "Peridot": ("orthorhombic", "(Mg,Fe)₂SiO₄", "Silicate de magnésium et de fer (olivine)"),
 "Zircon": ("tetragonal", "ZrSiO₄", "Silicate de zirconium"),
}

try:                                            # the species written for both the game and the site
    from new_species_spec import SPECIES as _NEW
    for _s in _NEW:
        T[_s["id"]] = (_s["system"], _s["formula"], _s["note"][0])
except ImportError:
    pass


def main():
    data = json.load(open(Path(__file__).resolve().parent.parent / "data" / "gemlab_data.json", encoding="utf-8"))
    ids = [s["id"] for s in data["species"]]
    extra_path = Path(__file__).resolve().parent.parent / "data" / "extra_species.json"
    if extra_path.exists():                       # species of the site that the game data does not have (yet)
        ids += [e["id"] for e in json.load(open(extra_path, encoding="utf-8"))["species"] if e["id"] not in ids]
    missing = [i for i in ids if i not in T]
    extra = [k for k in T if k not in ids]
    if missing:
        print("Espèces sans fiche chimique :", ", ".join(missing))
    if extra:
        print("Entrées inconnues :", ", ".join(extra))
    out = {k: {"system": v[0], "formula": v[1], "note": v[2]} for k, v in sorted(T.items()) if k in ids}
    OUT.write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
    print("{} espèces écrites dans {}".format(len(out), OUT))

if __name__ == "__main__":
    main()
