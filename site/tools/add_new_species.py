#!/usr/bin/env python3
"""Puts the 22 species of tools/new_species_spec.py on the site: classes, families, English texts.

Run after:  python3 site/tools/build_extra_species.py   (the species themselves, in data/extra_species.json)
            python3 site/tools/export_palettes.py       (their colour chart)
            python3 site/tools/build_species_extra.py   (crystal system and formula)
Then:       python3 -m unittest discover -s site/tests

Safe to run twice. Stops with a list of the texts that are still to translate (add them to EN below)."""
import json
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(SITE / "tools"))
import species_strings  # noqa: E402
from new_species_spec import SPECIES, ZIRCON_HIGH, PATCHES, SPECTRA_META, SPECTRA_NEW  # noqa: E402

# French text -> English text
EN = {
    "Amazonite": "Amazonite",
    "Un feldspath vert bleuté, coloré par le plomb, que l'on reconnaît à son aspect quadrillé. On le polit en cabochons et en perles.":
        "A blue-green feldspar, colored by lead, recognizable by its criss-cross look. It is polished into cabochons and beads.",
    "Vert bleuté (vert-de-gris)": "Blue-green (verdigris)",
    "Macle en grille (réseau quadrillé) du microcline et lamelles blanches de perthite.": "Grid twinning (a criss-cross network) of microcline and white lamellae of perthite.",
    "États-Unis (Colorado)": "United States (Colorado)",
    "Pierre de soleil": "Sunstone",
    "Un feldspath orangé qui scintille de milliers de petites paillettes d'hématite ou de goethite : c'est l'aventurescence. La pierre « brille comme le soleil ».":
        "An orange feldspar that glitters with thousands of tiny flakes of hematite or goethite: this is aventurescence. The stone \"shines like the sun\".",
    "Orange à rouge-brun, avec des reflets dorés": "Orange to reddish brown, with golden reflections",
    "Pas de spectre net ; absorption diffuse du fer.": "No clear spectrum; diffuse iron absorption.",
    "Plaquettes orientées de goethite ou d'hématite, qui donnent le scintillement doré (aventurescence) ; clivages.":
        "Oriented platelets of goethite or hematite, which give the golden glitter (aventurescence); cleavages.",
    "États-Unis (Oregon)": "United States (Oregon)",
    "Hiddénite": "Hiddenite",
    "Le spodumène vert, cousin de la kunzite, coloré par le chrome. Elle doit son nom à Hiddenite, en Caroline du Nord, où elle a été découverte en 1879.":
        "Green spodumene, a cousin of kunzite, colored by chromium. It is named after Hiddenite, North Carolina, where it was discovered in 1879.",
    "Vert pâle à vert émeraude": "Pale green to emerald green",
    "Vert bleuté / Vert jaunâtre selon l'axe (fort).": "Bluish green / Yellowish green depending on the axis (strong).",
    "Orange à jaune faible sous UV longue onde.": "Weak orange to yellow under long-wave UV.",
    "Faible ou inerte sous UV courte onde.": "Weak or inert under short-wave UV.",
    "Spectre du chrome : fines raies dans le rouge.": "Chromium spectrum: fine lines in the red.",
    "Clivage parfait dans deux directions, tubes et cavités à deux phases ; pierre fragile aux chocs.":
        "Perfect cleavage in two directions, tubes and two-phase cavities; the stone is fragile to knocks.",
    "Réaction rougeâtre faible possible (chrome).": "Weak reddish reaction possible (chromium).",
    "États-Unis (Caroline du Nord)": "United States (North Carolina)",
    "Tourmaline verdélite": "Verdelite Tourmaline",
    "La tourmaline verte : vert bouteille, émeraude ou olive, colorée par le fer, le manganèse ou le chrome. C'est la couleur de tourmaline la plus courante.":
        "Green tourmaline: bottle green, emerald or olive, colored by iron, manganese or chromium. It is the most common tourmaline color.",
    "Vert (bouteille, émeraude ou olive)": "Green (bottle, emerald or olive)",
    "Vert foncé / Vert jaunâtre selon l'axe (fort).": "Dark green / Yellowish green depending on the axis (strong).",
    "Forte absorption du rouge (fer et manganèse) ; faibles bandes dans le bleu et le bleu-vert.":
        "Strong absorption of the red (iron and manganese); weak bands in the blue and blue-green.",
    "Tubes de croissance parallèles à l'axe, inclusions liquides fragmentées, fissures.": "Growth tubes parallel to the axis, fragmented liquid inclusions, fissures.",
    "Vert terne, parfois teinté de rouge.": "Dull green, sometimes tinged with red.",
    "Légère réaction possible avec un aimant très puissant (fer).": "Slight reaction possible with a very strong magnet (iron).",
    "Tourmaline dravite": "Dravite Tourmaline",
    "La tourmaline brune, riche en magnésium, qui doit son nom à la Drave, une rivière de l'est des Alpes. Brun doré à brun foncé, elle est solide et peu chère.":
        "The brown tourmaline, rich in magnesium, named after the Drava, a river of the eastern Alps. Golden brown to dark brown, it is sturdy and inexpensive.",
    "Brun à brun jaunâtre, parfois vert foncé": "Brown to yellowish brown, sometimes dark green",
    "Brun clair / Brun foncé selon l'axe (fort).": "Light brown / Dark brown depending on the axis (strong).",
    "Bandes diffuses du fer ; absorption du bleu et du violet.": "Diffuse iron bands; absorption of the blue and violet.",
    "Tubes de croissance parallèles à l'axe, inclusions liquides, fissures ; cristaux souvent striés.":
        "Growth tubes parallel to the axis, liquid inclusions, fissures; crystals often striated.",
    "Slovénie (Drave)": "Slovenia (Drava)",
    "Aventurine": "Aventurine",
    "Un quartz massif plein de petites paillettes de mica vert (fuchsite) qui scintillent : c'est l'aventurescence. Elle est verte, parfois bleue, orange ou brune.":
        "A massive quartz full of tiny flakes of green mica (fuchsite) that glitter: this is aventurescence. It is green, sometimes blue, orange or brown.",
    "Vert (parfois bleu, orange ou brun)": "Green (sometimes blue, orange or brown)",
    "Pas de spectre net ; bandes faibles du chrome de la fuchsite.": "No clear spectrum; weak chromium bands from the fuchsite.",
    "Paillettes orientées de fuchsite (mica au chrome), qui donnent le scintillement ; cassure granuleuse.":
        "Oriented flakes of fuchsite (chromium mica), which give the glitter; granular fracture.",
    "Jaspe": "Jasper",
    "Un quartz microcristallin opaque, coloré par des oxydes de fer : rouge, jaune, vert ou brun, souvent tacheté ou rubané. Il est dur et se polit très bien.":
        "An opaque microcrystalline quartz, colored by iron oxides: red, yellow, green or brown, often spotted or banded. It is hard and polishes very well.",
    "Rouge, jaune, vert ou brun (souvent tacheté)": "Red, yellow, green or brown (often spotted)",
    "Grain microcristallin, taches et veines d'oxydes de fer, parfois fossiles ou dendrites.": "Microcrystalline grain, spots and veins of iron oxides, sometimes fossils or dendrites.",
    "Onyx": "Onyx",
    "Une calcédoine à bandes parallèles noires et blanches. La plupart des onyx du commerce sont des agates grises teintes en noir.":
        "A chalcedony with parallel black and white bands. Most onyx on the market is gray agate dyed black.",
    "Noir et blanc, en bandes parallèles": "Black and white, in parallel bands",
    "Pas de spectre net ; la teinture noire donne ses propres bandes.": "No clear spectrum; the black dye gives its own bands.",
    "Bandes rectilignes parallèles, noires et blanches ; grain microcristallin.": "Straight parallel black and white bands; microcrystalline grain.",
    "Enstatite": "Enstatite",
    "Un pyroxène de magnésium, d'un vert bouteille à brun. L'enstatite verte au chrome est très rare ; la variété riche en fer est l'hypersthène.":
        "A magnesium pyroxene, bottle green to brown. The green chromium enstatite is very rare; the iron-rich variety is hypersthene.",
    "Vert à brun (vert bouteille, jaune-vert)": "Green to brown (bottle green, yellow-green)",
    "Vert / Brun jaunâtre selon l'axe (faible à modéré).": "Green / Yellowish brown depending on the axis (weak to moderate).",
    "Bandes du fer dans le vert-bleu ; parfois une bande du chrome dans le jaune-vert.": "Iron bands in the blue-green; sometimes a chromium band in the yellow-green.",
    "Fines aiguilles parallèles, petits cristaux, fissures ; clivage net dans deux directions.": "Fine parallel needles, small crystals, fissures; clear cleavage in two directions.",
    "Faible réaction à l'aimant (fer).": "Weak reaction to the magnet (iron).",
    "Larimar": "Larimar",
    "Une variété bleue de pectolite, que l'on ne trouve qu'en République dominicaine. Son bleu ciel veiné de blanc rappelle la mer des Caraïbes.":
        "A blue variety of pectolite, found only in the Dominican Republic. Its sky blue veined with white recalls the Caribbean sea.",
    "Bleu ciel à bleu-vert, veiné de blanc": "Sky blue to blue-green, veined with white",
    "Inerte, parfois orangé faible.": "Inert, sometimes weak orange.",
    "Pas de spectre net ; le cuivre colore en bleu.": "No clear spectrum; copper colors it blue.",
    "Fibres rayonnantes de pectolite, volutes et veines blanches (motif en « vagues »).": "Radiating fibers of pectolite, swirls and white veins (a \"wave\" pattern).",
    "Charoïte": "Charoite",
    "Une roche violette aux tourbillons soyeux, trouvée seulement en Sibérie, près de la rivière Chara. Elle a été décrite en 1978.":
        "A violet rock with silky swirls, found only in Siberia, near the Chara River. It was described in 1978.",
    "Soyeux à nacré": "Silky to pearly",
    "Violet (lilas à pourpre) en tourbillons": "Violet (lilac to purple) in swirls",
    "Fibres violettes en tourbillons, avec des plages d'aegyrine noire et de feldspath blanc.": "Violet fibers in swirls, with patches of black aegirine and white feldspar.",
    "Russie (Yakoutie, massif du Murun)": "Russia (Yakutia, Murun massif)",
    "Sugilite": "Sugilite",
    "Une roche d'un violet intense, découverte au Japon en 1944 et exploitée en Afrique du Sud. Sa couleur vient du manganèse.":
        "A rock of an intense violet, discovered in Japan in 1944 and mined in South Africa. Its color comes from manganese.",
    "Violet intense à pourpre": "Intense violet to purple",
    "Anisotrope, uniaxial, lecture diffuse typique d'un agrégat.": "Anisotropic, uniaxial, a diffuse reading typical of an aggregate.",
    "Bandes du manganèse dans le violet et le vert.": "Manganese bands in the violet and green.",
    "Grain fin, plages plus claires et taches noires d'oxydes de manganèse.": "Fine grain, lighter patches and black spots of manganese oxides.",
    "Légère réaction possible avec un aimant très puissant (manganèse et fer).": "Slight reaction possible with a very strong magnet (manganese and iron).",
    "Afrique du Sud (mine Wessels)": "South Africa (Wessels mine)",
    "Azurite": "Azurite",
    "Un carbonate de cuivre d'un bleu profond, au nom qui vient de l'azur. Tendre et fragile, on le polit en cabochons ou on le garde en cristaux ; il se transforme lentement en malachite.":
        "A deep blue copper carbonate, whose name comes from azure. Soft and fragile, it is polished into cabochons or kept as crystals; it slowly turns into malachite.",
    "Bleu profond (bleu azur)": "Deep blue (azure blue)",
    "Cristaux et rosettes, passages à la malachite verte ; bulles de cavités botryoïdales.": "Crystals and rosettes, passages into green malachite; bubbles of botryoidal cavities.",
    "Chrysocolle": "Chrysocolla",
    "Un silicate de cuivre bleu-vert, tendre et poreux, le plus souvent mêlé de quartz ou d'autres minéraux de cuivre. Sa variété silicifiée, plus dure, est la « gem silica ».":
        "A blue-green copper silicate, soft and porous, most often mixed with quartz or other copper minerals. Its silicified variety, harder, is \"gem silica\".",
    "Vitreux à terne": "Vitreous to dull",
    "Bleu-vert à bleu turquoise": "Blue-green to turquoise blue",
    "Pas de spectre net ; le cuivre colore en bleu-vert.": "No clear spectrum; copper colors it blue-green.",
    "Masse cryptocristalline, bandes et plages vert foncé, parfois cristaux de quartz.": "Cryptocrystalline mass, bands and dark green patches, sometimes quartz crystals.",
    "République démocratique du Congo": "Democratic Republic of the Congo",
    "Variscite": "Variscite",
    "Un phosphate d'aluminium vert pomme à bleuté, souvent veiné de brun ou de noir. On la prend souvent pour de la turquoise.":
        "An apple-green to bluish aluminum phosphate, often veined with brown or black. It is often mistaken for turquoise.",
    "Vert pomme à vert bleuté": "Apple green to bluish green",
    "Pas de spectre net ; le chrome ou le vanadium colore en vert.": "No clear spectrum; chromium or vanadium colors it green.",
    "Veinules brunes ou noires d'oxydes de fer ou de manganèse (matrice), nodules en rognons.": "Brown or black veinlets of iron or manganese oxides (matrix), kidney-shaped nodules.",
    "Lépidolite": "Lepidolite",
    "Un mica de lithium rose à violet lilas, fait de minuscules paillettes. C'est un minerai de lithium, et une pierre d'ornement douce.":
        "A pink to lilac-violet lithium mica, made of tiny flakes. It is a lithium ore, and a soft ornamental stone.",
    "Lilas à rose-violet": "Lilac to pink-violet",
    "Paillettes de mica agglomérées, parfois cristaux de tourmaline rose ou de quartz.": "Agglomerated mica flakes, sometimes crystals of pink tourmaline or quartz.",
    "Bénitoïte": "Benitoite",
    "Un silicate de baryum et de titane d'un bleu saphir, trouvé seulement en Californie. Sa dispersion dépasse celle du diamant et elle brille en bleu vif sous les UV courts.":
        "A sapphire-blue barium titanium silicate, found only in California. Its dispersion exceeds that of diamond and it glows bright blue under short-wave UV.",
    "Bleu saphir à violet (parfois incolore)": "Sapphire blue to violet (sometimes colorless)",
    "Bleu intense / Incolore selon l'axe (très marqué).": "Intense blue / Colorless depending on the axis (very strong).",
    "Inerte ou très faible.": "Inert or very weak.",
    "Bleu-blanc vif, très intense.": "Bright blue-white, very intense.",
    "Souvent propre ; petits cristaux de natrolite, de neptunite ou de joaquinite.": "Often clean; small crystals of natrolite, neptunite or joaquinite.",
    "Grandidiérite": "Grandidierite",
    "Un borosilicate bleu-vert, l'une des gemmes les plus rares du monde, découvert à Madagascar en 1902. Son trichroïsme est très fort.":
        "A blue-green borosilicate, one of the rarest gems in the world, discovered in Madagascar in 1902. Its trichroism is very strong.",
    "Bleu-vert (turquoise)": "Blue-green (turquoise)",
    "Bleu-vert / Vert / Incolore à jaunâtre selon les trois axes (très marqué).": "Blue-green / Green / Colorless to yellowish depending on the three axes (very strong).",
    "Pas de raies nettes ; absorption du fer dans le rouge et le bleu.": "No sharp lines; iron absorption in the red and blue.",
    "Souvent propre ; petits cristaux, plumes de guérison, fines aiguilles.": "Often clean; small crystals, healing feathers, fine needles.",
    "Zultanite (diaspore)": "Zultanite (diaspore)",
    "Le diaspore gemme, qui change de couleur : vert jaunâtre à la lumière du jour, rose à orangé sous une lampe à incandescence. On ne l'exploite qu'en Turquie, dans les monts Ilbir (Anatolie).":
        "Gem diaspore, which changes color: yellowish green in daylight, pink to orange under an incandescent lamp. It is mined only in Turkey, in the Ilbir mountains (Anatolia).",
    "Vert jaunâtre à rose (changement de couleur)": "Yellowish green to pink (color change)",
    "Vert / Jaune-brun / Rose-violet selon les trois axes (fort).": "Green / Yellow-brown / Pink-violet depending on the three axes (strong).",
    "Absorption du bleu (fer) et faibles raies du chrome dans le rouge.": "Absorption of the blue (iron) and weak chromium lines in the red.",
    "Généralement propre ; fissures, cristaux, clivage parfait dans une direction (pierre fragile).": "Generally clean; fissures, crystals, perfect cleavage in one direction (fragile stone).",
    "Turquie (monts Ilbir)": "Turkey (Ilbir mountains)",
    "Euclase": "Euclase",
    "Un silicate de béryllium et d'aluminium d'un bleu-vert pâle, très dur mais fragile : son nom grec veut dire « qui se brise bien », à cause de son clivage parfait.":
        "A pale blue-green beryllium aluminum silicate, very hard but fragile: its Greek name means \"easily broken\", because of its perfect cleavage.",
    "Bleu à bleu-vert pâle (parfois incolore ou vert)": "Pale blue to blue-green (sometimes colorless or green)",
    "Bleu / Bleu-vert / Incolore selon les trois axes (modéré).": "Blue / Blue-green / Colorless depending on the three axes (moderate).",
    "Clivage parfait, fissures, inclusions liquides ; très fragile à la taille.": "Perfect cleavage, fissures, liquid inclusions; very fragile when cutting.",
    "Zimbabwe": "Zimbabwe",
    "Danburite": "Danburite",
    "Un borosilicate de calcium, incolore à jaune pâle, au bel éclat. Il est dur et clair, mais reste rare. Il tient son nom de Danbury, dans le Connecticut.":
        "A calcium borosilicate, colorless to pale yellow, with a fine luster. It is hard and clear, but remains rare. It is named after Danbury, Connecticut.",
    "Incolore à jaune pâle": "Colorless to pale yellow",
    "Bleu clair, variable selon la pierre.": "Light blue, varying from stone to stone.",
    "Bleu, plus faible qu'en onde longue.": "Blue, weaker than in long wave.",
    "Pas de spectre net ; parfois de fines raies des terres rares.": "No clear spectrum; sometimes fine rare-earth lines.",
    "Généralement propre ; cavités à liquide, fissures.": "Generally clean; liquid-filled cavities, fissures.",
    "Kornérupine": "Kornerupine",
    "Un silicate de magnésium et d'aluminium, vert à brun, au trichroïsme marqué. Rare en pierre taillée, elle donne parfois des œils-de-chat.":
        "A magnesium aluminum silicate, green to brown, with marked trichroism. Rare as a cut stone, it sometimes gives cat's-eyes.",
    "Vert à brun": "Green to brown",
    "Vert / Jaune-brun / Brun-rouge selon les trois axes (marqué).": "Green / Yellow-brown / Red-brown depending on the three axes (marked).",
    "Généralement inerte, parfois jaune orangé faible.": "Generally inert, sometimes weak orange-yellow.",
    "Absorption du bleu (fer), sans raie nette.": "Absorption of the blue (iron), without a sharp line.",
    "Aiguilles parallèles (parfois en œil-de-chat), fissures, cristaux.": "Parallel needles (sometimes in a cat's-eye), fissures, crystals.",
    "Orange grisâtre": "Grayish orange",
    "Vert jaunâtre grisâtre sombre": "Dark grayish yellowish green",
    "Vert grisâtre sombre": "Dark grayish green",
    "Jaune grisâtre sombre": "Dark grayish yellow",
    "Rouge orangé": "Orange-red",
    "Violet grisâtre": "Grayish violet",
    "Jaune grisâtre": "Grayish yellow",
    "Jaune brunâtre intense": "Intense brownish yellow",
    "Vert jaunâtre grisâtre": "Grayish yellowish green",
    "Vert jaunâtre brunâtre": "Brownish yellowish green",
    "Vert jaunâtre brunâtre intense": "Intense brownish yellowish green",
    "Feldspath potassique (microcline) coloré par le plomb": "Potassium feldspar (microcline) colored by lead",
    "Quartz à paillettes de mica vert (fuchsite)": "Quartz with flakes of green mica (fuchsite)",
    "Carbonate basique de cuivre": "Basic copper carbonate",
    "Silicate de baryum et de titane": "Barium titanium silicate",
    "Silicate complexe de potassium et de calcium (charoïte)": "Complex potassium calcium silicate (charoite)",
    "Silicate hydraté de cuivre, presque amorphe": "Hydrated copper silicate, almost amorphous",
    "Borosilicate de calcium": "Calcium borosilicate",
    "Hydroxyde d'aluminium (diaspore)": "Aluminum hydroxide (diaspore)",
    "Pyroxène de magnésium (enstatite)": "Magnesium pyroxene (enstatite)",
    "Borosilicate de magnésium, de fer et d'aluminium": "Magnesium iron aluminum borosilicate",
    "Spodumène vert coloré par le chrome": "Green spodumene colored by chromium",
    "Quartz microcristallin opaque coloré par des oxydes de fer": "Opaque microcrystalline quartz colored by iron oxides",
    "Silicate de magnésium et d'aluminium contenant du bore": "Magnesium aluminum silicate containing boron",
    "Pectolite (silicate de sodium et de calcium) bleue par le cuivre": "Pectolite (sodium calcium silicate) colored blue by copper",
    "Mica de lithium et d'aluminium": "Lithium aluminum mica",
    "Calcédoine : quartz microcristallin en bandes": "Chalcedony: banded microcrystalline quartz",
    "Feldspath plagioclase (oligoclase) à plaquettes d'oxyde de fer": "Plagioclase feldspar (oligoclase) with iron oxide platelets",
    "Silicate de potassium, de lithium et de manganèse (sugilite)": "Potassium lithium manganese silicate (sugilite)",
    "Tourmaline magnésienne (dravite)": "Magnesian tourmaline (dravite)",
    "Tourmaline verte (elbaïte) colorée par le fer, le manganèse ou le chrome": "Green tourmaline (elbaite) colored by iron, manganese or chromium",
    "Phosphate hydraté d'aluminium": "Hydrated aluminum phosphate",
}


def spec_pairs():
    """French -> English for every text written as a pair in new_species_spec.py (the species added after the first batch need no hand-written line here)."""
    pairs = {}

    def add(pair):
        if isinstance(pair, (tuple, list)) and len(pair) == 2 and all(isinstance(x, str) for x in pair):
            pairs[pair[0]] = pair[1]

    for sp in SPECIES + list(PATCHES.values()):
        for key in ("name", "desc", "luster", "color", "pleo", "fluL", "fluS", "opt", "fig", "spec", "incl", "chelsea", "magn", "note"):
            add(sp.get(key))
        if sp.get("spectra_entry") and sp["spectra_entry"].get("en"):
            pairs[sp["spectra_entry"]["description"]] = sp["spectra_entry"]["en"]
        for e in sp.get("spectra_extra", []):
            pairs[e["description"]] = e["en"]
        for o in sp.get("origins", []):
            pairs[o["fr"]] = o["en"]
            if o.get("over"):
                add(o["over"])
    for _, (fr, en) in SPECTRA_META.values():                      # the labels of the spectra
        pairs[fr] = en
    for n in SPECTRA_NEW:
        pairs[n["description"]] = n["en"]
    for p in PATCHES.values():
        for k, v in p.get("extra_en", {}).items():
            pairs[p["extra"][k]] = v
    return pairs


def main():
    EN.update({k: v for k, v in spec_pairs().items() if k not in EN})
    data = json.loads((SITE / "data" / "extra_species.json").read_text(encoding="utf-8"))
    have = {s["id"] for s in data["species"]}
    missing = [s["id"] for s in SPECIES if s["id"] not in have]
    if missing:
        sys.exit("Ces espèces ne sont pas dans extra_species.json : {}\nLance d'abord : python3 site/tools/build_extra_species.py".format(", ".join(missing)))

    # 1. classes and families
    for name, key in (("classes", "cls"), ("families", "fams")):
        path = SITE / "data" / (name + ".json")
        doc = json.loads(path.read_text(encoding="utf-8"))
        by_id = {x["id"]: x for x in doc[name]}
        for sp in SPECIES:
            groups = [sp[key]] if key == "cls" else sp[key]
            for g in groups:
                if sp["id"] not in by_id[g]["species"]:
                    by_id[g]["species"].append(sp["id"])
        path.write_text(json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")
    print("classes et familles à jour")

    # 2. English texts
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
    print("Tout est traduit.")


if __name__ == "__main__":
    main()
