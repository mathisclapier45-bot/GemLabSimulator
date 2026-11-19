#!/usr/bin/env python3
"""Puts the four new garnets (Mali, topazolite, chrome pyrope, color-change garnet) on the site, once the game has them.

Order of work (the game must have the species first):
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

NEW = ["Grenat_Mali", "Grenat_Topazolite", "Grenat_Pyrope_Chromifere", "Grenat_Change_Couleur"]

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
}


def main():
    data = json.loads((SITE / "data" / "gemlab_data.json").read_text(encoding="utf-8"))
    have = {s["id"] for s in data["species"]}
    missing = [i for i in NEW if i not in have]
    if missing:
        sys.exit("Ces espèces ne sont pas encore dans les données du site : {}\n"
                 "Il faut d'abord les générer dans Unity (menu GemLab), puis lancer : python3 site/tools/sync_data.py --export".format(", ".join(missing)))

    # 1. the classes and the families
    for name, group in (("classes", "silicates"), ("families", "grenats")):
        path = SITE / "data" / (name + ".json")
        doc = json.loads(path.read_text(encoding="utf-8"))
        item = next(x for x in doc[name] if x["id"] == group)
        added = [i for i in NEW if i not in item["species"]]
        item["species"] += added
        path.write_text(json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")
        print("{} : {} ajoutée(s) à « {} »".format(name, len(added), group))

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
