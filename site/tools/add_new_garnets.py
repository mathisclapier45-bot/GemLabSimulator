#!/usr/bin/env python3
"""Puts the six new garnets on the site: Mali, topazolite, chrome pyrope, color-change garnet (four varieties written in the game code),
and the pure grossular and the pure andradite (two poles of the garnet diagram, not stones of the game).

The six are in site/data/extra_species.json (python3 site/tools/build_extra_species.py), so this script works now. It files them under
"silicates" and "garnets" and adds their English texts. Run it again after any change to extra_species.json.

When the game has the four varieties (the stones are generated in Unity), the game version takes over on the site:
  1. In Unity: run the GemLab menu that rebuilds the gems (SceneBootstrap), so that Assets/Data/Gems/Grenat_Mali.asset... exist.
  2. python3 site/tools/sync_data.py --export          copies the game data into the site
  3. python3 site/tools/export_palettes.py             the colour chart of the new species
  4. python3 site/tools/build_species_extra.py         crystal system and formula (already written for the four species)
  5. python3 site/tools/add_new_garnets.py             this script: the classes/families files and the English texts
  6. python3 -m unittest discover -s site/tests        everything must pass

This script is safe to run twice. It stops with a clear message when the species are not in the site data yet."""
import json
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(SITE / "tools"))
import species_strings  # noqa: E402

NEW = ["Grenat_Mali", "Grenat_Topazolite", "Grenat_Pyrope_Chromifere", "Grenat_Change_Couleur", "Grenat_Grossulaire", "Grenat_Andradite"]
# The species of the site that are not garnets: id -> (class, family)
OTHERS = {"Saphir_Change_Couleur": ("oxydes", "corindons")}

# French text -> English text, for every text the four species can bring. Only the ones the site is missing are added.
EN = {
    "Grenat du Mali": "Mali Garnet",
    "Grenat Topazolite": "Topazolite Garnet",
    "Grenat Pyrope chromifère": "Chrome Pyrope Garnet",
    "Grenat à changement de couleur": "Color-Change Garnet",
    "Un grenat jaune-vert à vert olive, mélange de grossulaire et d'andradite, découvert au Mali en 1994. Son éclat et son feu rappellent le démantoïde.":
        "A yellow-green to olive-green garnet, a mixture of grossular and andradite, discovered in Mali in 1994. Its brilliance and fire recall demantoid.",
    "Un grenat andradite jaune à jaune-vert, à l'éclat très fort, que sa couleur fait ressembler à la topaze (d'où son nom). C'est la variété jaune du démantoïde.":
        "A yellow to yellow-green andradite garnet of very strong luster, whose color makes it look like topaz (hence its name). It is the yellow variety of demantoid.",
    "Un pyrope d'un rouge vif, parfois orangé, coloré par le chrome. On le trouve dans les kimberlites, les roches qui contiennent le diamant : les prospecteurs le cherchent comme indice.":
        "A bright red pyrope, sometimes orange, colored by chromium. It is found in kimberlites, the rocks that hold diamonds: prospectors look for it as a clue.",
    "Un grenat pyrope-spessartite qui change de couleur : bleu-vert à la lumière du jour, violet à rouge sous une lampe à incandescence, comme l'alexandrite. Découvert à Madagascar à la fin des années 1990.":
        "A pyrope-spessartine garnet that changes color: blue-green in daylight, violet to red under an incandescent lamp, like alexandrite. Discovered in Madagascar in the late 1990s.",
    "Jaune-vert à vert olive": "Yellow-green to olive green",
    "Jaune à jaune-vert": "Yellow to yellow-green",
    "Rouge vif (parfois orangé)": "Bright red (sometimes orange)",
    "Bleu-vert à violet (changement de couleur)": "Blue-green to violet (color change)",
    "Forte absorption dans le violet et le bleu (fer), parfois des bandes faibles du manganèse.":
        "Strong absorption in the violet and blue (iron), sometimes weak manganese bands.",
    "Forte absorption dans le violet et le bleu (fer ferrique).": "Strong absorption in the violet and blue (ferric iron).",
    "Spectre du chrome : large bande vers 570 nm et absorption du violet, avec de faibles bandes du fer.":
        "Chromium spectrum: a broad band near 570 nm and absorption of the violet, with weak iron bands.",
    "Bandes du vanadium et du chrome (large bande vers 570 nm) et fines bandes du manganèse dans le bleu.":
        "Vanadium and chromium bands (broad band near 570 nm) and fine manganese bands in the blue.",
    "Généralement assez propre : fines aiguilles et plumes de guérison, plus rarement des fibres rappelant le démantoïde.":
        "Generally fairly clean: fine needles and healing feathers, more rarely fibers recalling demantoid.",
    "Fines inclusions fibreuses possibles, fissures ; généralement propre.": "Fine fibrous inclusions possible, fissures; generally clean.",
    "Souvent propre ; parfois des aiguilles de rutile, de petits cristaux de chromite ou de diopside.":
        "Often clean; sometimes rutile needles, small crystals of chromite or diopside.",
    "Généralement propre ; fines aiguilles de rutile et petits cristaux.": "Generally clean; fine rutile needles and small crystals.",
    "Réaction rougeâtre faible possible (chrome et vanadium).": "Weak reddish reaction possible (chromium and vanadium).",
    "Mali (région de Kayes)": "Mali (Kayes region)",
    "Italie (Piémont)": "Italy (Piedmont)",
    "Afrique du Sud (kimberlites)": "South Africa (kimberlites)",
    "Madagascar (Bekily)": "Madagascar (Bekily)",
    "Tanzanie (Umba)": "Tanzania (Umba)",
    "Mélange de grossulaire et d'andradite": "Mixture of grossular and andradite",
    "Silicate de calcium et de fer (andradite jaune)": "Calcium iron silicate (yellow andradite)",
    "Pyrope coloré par le chrome": "Pyrope colored by chromium",
    "Mélange de pyrope et de spessartite, avec vanadium et chrome": "Mixture of pyrope and spessartine, with vanadium and chromium",
    # the two poles that are not stones of the game
    "Grenat Grossulaire": "Grossular Garnet",
    "Grenat Andradite": "Andradite Garnet",
    "Le grenat de calcium et d'aluminium pur : incolore à vert pâle, jaune ou rose, plus coloré avec des traces de fer, de chrome ou de vanadium. Ses variétés les plus connues sont l'hessonite et la tsavorite.":
        "The pure calcium aluminum garnet: colorless to pale green, yellow or pink, more strongly colored with traces of iron, chromium or vanadium. Its best-known varieties are hessonite and tsavorite.",
    "Le grenat de calcium et de fer pur : jaune, vert, brun ou noir. Ses variétés sont le démantoïde (vert, au chrome), la topazolite (jaune) et la mélanite (noire, opaque).":
        "The pure calcium iron garnet: yellow, green, brown or black. Its varieties are demantoid (green, with chromium), topazolite (yellow) and melanite (black, opaque).",
    "Incolore à vert pâle, jaune ou rose": "Colorless to pale green, yellow or pink",
    "Jaune à vert, brun ou noir": "Yellow to green, brown or black",
    "Pas de spectre net ; bandes faibles du fer ou du manganèse selon la teinte.": "No clear spectrum; weak iron or manganese bands depending on the hue.",
    "Généralement propre ; petits cristaux d'apatite, de zircon ou de diopside, parfois des grains arrondis.":
        "Generally clean; small crystals of apatite, zircon or diopside, sometimes rounded grains.",
    "Fines fibres, parfois en « queue de cheval » ; sinon assez propre.": "Fine fibers, sometimes in a \"horsetail\"; otherwise fairly clean.",
    "Canada (Québec)": "Canada (Quebec)",
    "Silicate de calcium et d'aluminium (grossulaire)": "Calcium aluminum silicate (grossular)",
    # colour names that the chart of the new species brings
    "Saphir à changement de couleur": "Color-Change Sapphire",
    "Un saphir naturel qui change de couleur : bleu-violet à la lumière du jour, pourpre à rouge-violet sous une lampe à incandescence. Il le doit à un mélange de vanadium et de chrome, et se rapproche de l'alexandrite par son effet.":
        "A natural sapphire that changes color: blue-violet in daylight, purple to red-violet under an incandescent lamp. It owes this to a mixture of vanadium and chromium, and resembles alexandrite in its effect.",
    "Bleu violacé à pourpre (changement de couleur)": "Violet-blue to purple (color change)",
    "Bleu-violet / Violet-rouge selon l'axe d'observation (modéré).": "Blue-violet / Violet-red depending on the viewing axis (moderate).",
    "Inerte à rouge faible sous UV longue onde.": "Inert to weak red under long-wave UV.",
    "Bandes du chrome et du vanadium : large bande vers 570 nm et raies fines du chrome dans le rouge (694 nm).": "Chromium and vanadium bands: a broad band near 570 nm and fine chromium lines in the red (694 nm).",
    "Fine soie de rutile, zonation de couleur, petits cristaux dispersés.": "Fine rutile silk, color zoning, scattered small crystals.",
    "Oxyde d'aluminium (corindon) coloré par le chrome et le vanadium": "Aluminum oxide (corundum) colored by chromium and vanadium",
    "Jaune brunâtre": "Brownish yellow",
    "Pourpre grisâtre": "Grayish purple",
}


def main():
    data = json.loads((SITE / "data" / "gemlab_data.json").read_text(encoding="utf-8"))
    have = {s["id"] for s in data["species"]}
    extra = SITE / "data" / "extra_species.json"
    if extra.exists():
        have |= {s["id"] for s in json.loads(extra.read_text(encoding="utf-8"))["species"]}
    missing = [i for i in NEW + list(OTHERS) if i not in have]
    if missing:
        sys.exit("Ces espèces ne sont pas dans les données du site : {}\n"
                 "Lance d'abord : python3 site/tools/build_extra_species.py".format(", ".join(missing)))

    # 1. the classes and the families
    targets = {i: ("silicates", "grenats") for i in NEW}
    targets.update(OTHERS)
    for name, which in (("classes", 0), ("families", 1)):
        path = SITE / "data" / (name + ".json")
        doc = json.loads(path.read_text(encoding="utf-8"))
        for group in sorted({t[which] for t in targets.values()}):
            item = next(x for x in doc[name] if x["id"] == group)
            added = [i for i, t in targets.items() if t[which] == group and i not in item["species"]]
            item["species"] += added
            print("{} : {} ajoutée(s) à « {} »".format(name, len(added), group))
        path.write_text(json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")

    # 2. the English texts the site is missing
    path = SITE / "data" / "en" / "species.json"
    en = json.loads(path.read_text(encoding="utf-8"))
    needed = [t for t in species_strings.collect() if t not in en]
    unknown = [t for t in needed if t not in EN]
    for t in needed:
        if t in EN:
            en[t] = EN[t]
    path.write_text(json.dumps(en, ensure_ascii=False, indent=0), encoding="utf-8")
    print("textes anglais ajoutés :", len(needed) - len(unknown))
    if unknown:
        print("\nTextes à traduire encore (ajoute-les à EN dans ce script) :")
        for t in unknown:
            print("  -", t)
        sys.exit(1)
    print("Tout est traduit. Lance les tests : python3 -m unittest discover -s site/tests")


if __name__ == "__main__":
    main()
