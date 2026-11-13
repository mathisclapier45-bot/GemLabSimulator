using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// The text of the books of the library. The Guide is made from the grimoire, the Atlas from the origins of the species; the Manual of the treatments and
// the Digest of the market follow the rules of the game (the figures come from the code where they are public, and from TreatmentDisclosure, MarketState
// and AuctionCatalog otherwise). The rich text is the one of the grimoire: the reader recolours it for the paper.
public static class BookContents
{
    private static readonly CultureInfo French = new CultureInfo("fr-FR");

    private static string Heading(string title) => "<color=#F2D9A6><b>" + title + "</b></color>";

    private static string Note(string text) => "<color=#9AA3B5>" + text + "</color>";

    private static string Bullets(params string[] lines) => string.Join("\n", lines.Where(l => l != null).Select(l => "· " + l));

    private static string Blocks(params string[] blocks) => string.Join("\n\n", blocks.Where(b => !string.IsNullOrEmpty(b)));

    private static string Section(string title, params string[] bullets) => Heading(title) + "\n" + Bullets(bullets);

    private static string Percent(float fraction) => Math.Round(fraction * 100f).ToString("0", French) + " %";

    private static string Euro(float value) => Math.Round(value).ToString("N0", French) + " €";

    private const string Turning =
        "<color=#F2D9A6><b>Tourner les pages</b></color>\n" +
        "· Les liens en bas des pages, ou les flèches gauche et droite du clavier.\n" +
        "· Le signet rouge, en haut à droite, ramène à l'index.\n" +
        "· La molette de la souris fait défiler une page plus longue que sa place.";

    // ---- Le Guide du gemmologue ---------------------------------------------------------------------------------------

    public static BookContent BuildGuide()
    {
        var book = new BookContent
        {
            IndexTitle = "Index des espèces",
            IntroHeading = "Comment lire ce livre",
            IntroText = Blocks(
                Turning,
                Heading("Ce que contient chaque espèce") + "\n" +
                "Sur la page de gauche, l'aspect, l'optique et la fluorescence : éclat, couleur, indice de réfraction, biréfringence, pléochroïsme, spectre d'absorption, filtre Chelsea...\n\n" +
                "Sur la page de droite, le physique (densité, dureté, magnétisme, conductivités), les inclusions, la valeur, les traitements que le laboratoire sait appliquer, puis ce que montrent les instruments sur une pierre naturelle de référence.\n\n" +
                "Le nom de chaque donnée est en gras.",
                Note("Une pierre réelle varie selon son gisement, sa couleur et ses traitements : le guide donne la référence, c'est à vous de comparer.")),
        };

        var database = GemDatabase.Instance;
        if (database == null) return book;
        foreach (var def in database.allGems)
        {
            if (def == null) continue;
            var species = def;
            book.Entries.Add(new BookEntry
            {
                Title = species.displayName,
                Subtitle = species.luster + "  ·  " + species.color,
                LeftHead = "Guide du gemmologue",
                RightHead = "Physique, traitements et instruments",
                Icon = species.GetIcon(),
                Spectrum = SpectrumLibrary.Lines(species),
                LeftStrip = HabitStrip(species),
                LeftBuilder = () => { SheetPages(species, out var left, out _); return left; },
                RightBuilder = () =>
                {
                    SheetPages(species, out _, out var right);
                    return Blocks(right, GrimoireUI.BuildTreatmentsText(species) + GrimoireUI.BuildInstrumentsText(species, true));
                },
            });
        }
        book.TitleNote = book.Entries.Count + " espèces";
        return book;
    }

    // The habits of a species as small pictures, one under the other of the sheet (null for a species that does not grow in crystals).
    private static BookStrip HabitStrip(GemDefinition def)
    {
        if (def == null || def.iconColor.a <= 0f) return null;
        var chances = CrystalHabits.For(def);
        if (chances.Count == 0) return null;

        var strip = new BookStrip();
        foreach (var chance in chances)
            strip.Icons.Add(new BookIcon { Sprite = GemIconFactory.GetHabit(def, chance.Habit.Id, def.iconColor, def.iconFacetColor), Label = chance.Habit.Short });
        strip.Title = chances.Count > 1 ? "Habitus cristallins" : "Habitus cristallin";
        strip.Labeled = true;
        return strip;
    }

    // The sheet of a species cut into themes: the look and the optics for the left page, the physics, the inclusions and the notes for the right one.
    private static void SheetPages(GemDefinition def, out string left, out string right)
    {
        var parts = GrimoireUI.BuildSheetText(def).Split(new[] { "\n\n" }, StringSplitOptions.None);
        var data = new Dictionary<string, string>();
        foreach (var line in parts[0].Split('\n'))
        {
            var at = line.IndexOf(" : ", StringComparison.Ordinal);
            if (at < 0) continue;
            data[line.Substring(0, at)] = line.Substring(at + 3).Replace("    Style de taille : ", "   ·   <b>Style de taille :</b> ");
        }

        string Group(string title, params string[] labels)
        {
            var lines = new List<string>();
            foreach (var label in labels)
                if (data.TryGetValue(label, out var value)) lines.Add("<b>" + label + " :</b> " + value);
            return lines.Count == 0 ? null : Heading(title) + "\n" + string.Join("\n", lines);
        }

        left = Blocks(
            Group("Aspect", "Éclat", "Couleur", "Forme", "Habitus cristallins", "Transparence"),
            Group("Optique", "Indice de réfraction", "Biréfringence", "Caractère optique", "Pléochroïsme", "Figure d'interférence", "Filtre Chelsea", "Spectre d'absorption"),
            Group("Fluorescence", "Fluorescence UV longue onde", "Fluorescence UV courte onde"));

        var notes = parts.Length > 1 && parts[1].StartsWith("Notes : ") ? Heading("Notes") + "\n" + parts[1].Substring(8) : null;
        right = Blocks(
            Group("Physique", "Densité", "Dureté", "Magnétisme", "Conductivité thermique", "Conductivité électrique"),
            Group("Inclusions et valeur", "Inclusions typiques", "Valeur de base"),
            notes);
    }

    // ---- Le Manuel des traitements --------------------------------------------------------------------------------------

    private class Treatment
    {
        public string Name;
        public string Materials;
        public string What;
        public string Diamond;           // what is different for a diamond, or null
        public string Resale;
        public string[] Reveals;
        public string[] Blind;           // what does not see it, or null
        public string[] Instruments;     // the instruments that reveal it, for the pictures
    }

    // The discount and the detection figures are the ones of TreatmentDisclosure (stigma and detectability of each treatment).
    private static readonly Treatment[] Treatments =
    {
        new Treatment
        {
            Instruments = new[] { "Loupe", "Microscope", "MEB", "Infrarouge", "Dichroscope", "Raman" },
            Name = "Chauffage", Materials = "Corindon, béryl, quartz, topaze, tourmaline, zircon, tanzanite, ambre",
            What = "La pierre passe au four : on règle la température, l'atmosphère (oxydante ou réductrice), le refroidissement et la durée. Selon la recette, la couleur s'éclaircit, se purifie ou vire : tanzanite bleue, rubis plus pur, topaze rose... C'est le traitement le plus accepté du marché.",
            Resale = "Décote déclarée : 2 %. Un acheteur le trouve rarement : environ 1 fois sur 4.",
            Reveals = new[]
            {
                "<b>Loupe et microscope :</b> zones de tension, halos autour des inclusions, fissures d'un refroidissement trop rapide.",
                "<b>MEB :</b> les aiguilles de rutile d'un corindon chauffé sont en pointillés.",
                "<b>Infrarouge :</b> peut conclure sur un corindon chauffé.",
                "<b>Dichroscope :</b> une tanzanite chauffée n'a plus que deux couleurs (bleu et violet) au lieu de trois ; une tourmaline chauffée a un dichroïsme affaibli.",
                "<b>Diamant :</b> les centres NV d'un recuit se lisent au Raman, au DiamondView et en photoluminescence.",
            },
            Blind = new[] { "La lampe UV, le spectroscope et le LA-ICP-MS : le chauffage ne laisse aucune trace chimique." },
        },
        new Treatment
        {
            Instruments = new[] { "Spectroscope", "UV-Vis-NIR", "Dichroscope", "Infrarouge", "Photoluminescence" },
            Name = "Irradiation", Materials = "Quartz, béryl, topaze, tourmaline, perle, corindon jaune, diamant",
            What = "Un rayonnement (gamma, électrons ou neutrons) crée des centres colorés : quartz fumé, améthyste, topaze bleue, béryl jaune, saphir jaune, perle sombre. La dose décide de la couleur. Les neutrons rendent la pierre radioactive : elle doit refroidir plusieurs jours. Certaines couleurs sont instables et pâlissent avec le temps ; un chauffage peut inverser l'irradiation.",
            Diamond = "Gamma : vert pâle. Électrons : bleu-vert. Neutrons : vert foncé. Le diamant devient un diamant de couleur fancy et perd son certificat.",
            Resale = "Décote déclarée : 8 % (22 % pour un diamant). Chance qu'un acheteur le trouve : environ 45 % (60 % pour un diamant).",
            Reveals = new[]
            {
                "<b>Spectroscope et UV-Vis-NIR :</b> une bande de centres colorés s'ajoute au spectre (le fumé vers 500 nm, le maxixe vers 695 et 615 nm...).",
                "<b>Dichroscope :</b> le bleu maxixe est très dichroïque, bleu et incolore ou rose pâle.",
                "<b>Infrarouge :</b> un diamant irradié puis recuit montre les raies H1a et H1b.",
                "<b>Photoluminescence :</b> la raie GR1 à 741 nm, la lacune neutre, plus forte après des neutrons.",
            },
            Blind = new[] { "La loupe, l'EDXRF et le LA-ICP-MS : l'irradiation ne laisse ni trace visible ni élément étranger." },
        },
        new Treatment
        {
            Instruments = new[] { "Loupe", "Lampe UV", "MEB" },
            Name = "Blanchiment", Materials = "Perle, corail, jadéite, œil-de-tigre",
            What = "Un bain (eau oxygénée, eau de Javel, acide oxalique, acide chlorhydrique ou bain mixte) éclaircit une matière organique ou poreuse. On blanchit d'abord, puis on peut teindre : la teinture est alors plus régulière. Une pierre teinte ne se blanchit plus, et un second bain l'achève.",
            Resale = "Décote déclarée : 12 %. Chance qu'un acheteur le trouve : environ 50 %.",
            Reveals = new[]
            {
                "<b>Loupe et MEB :</b> de petites piqûres, l'attaque de l'acide sur la surface.",
                "<b>Lampe UV :</b> une fluorescence blanc-bleuté, même sur la perle et le corail.",
            },
            Blind = new[] { "Le LA-ICP-MS : le bain ne laisse aucun élément étranger." },
        },
        new Treatment
        {
            Instruments = new[] { "Loupe", "Microscope", "Lampe UV", "Filtre Chelsea", "Spectroscope", "EDXRF", "MEB", "LA-ICP-MS" },
            Name = "Teinture", Materials = "Perle, corail, jade, agate, lapis, turquoise, opale, ambre : une matière poreuse ou fissurée",
            What = "Un bain de colorant (rouge, rose, bleu de Prusse, vert chrome, noir carbone, violet lavande) imprègne les pores et les fissures. Un colorant organique donne une couleur vive mais fragile, un colorant minéral est plus stable. Une pierre déjà teinte ne se reteint pas : le second bain ferait des taches.",
            Resale = "Décote déclarée : 20 %. Chance qu'un acheteur le trouve : environ 85 %, c'est l'un des traitements les plus visibles.",
            Reveals = new[]
            {
                "<b>Loupe :</b> la couleur s'accumule en veinules dans les fissures.",
                "<b>Immersion au microscope :</b> la couleur forme des plages concentrées dans les fissures.",
                "<b>Lampe UV :</b> les colorants organiques brillent (rouge orangé, rose, violet) ; les colorants minéraux et le noir carbone éteignent la pierre.",
                "<b>Filtre Chelsea :</b> le vert chrome, le rose et le rouge réagissent.",
                "<b>Spectroscope :</b> une bande large de colorant s'ajoute au spectre (le rouge vers 540 nm, le bleu de Prusse vers 700 nm, le noir carbone très large).",
                "<b>EDXRF et LA-ICP-MS :</b> le fer et le potassium du bleu de Prusse, le chrome du vert chrome.",
            },
            Blind = new[] { "L'infrarouge : une teinture ne s'y lit pas." },
        },
        new Treatment
        {
            Instruments = new[] { "Loupe", "Spectroscope", "Filtre Chelsea", "EDXRF", "MEB", "LA-ICP-MS" },
            Name = "Revêtement", Materials = "Topaze, quartz, zircone, béryl, tanzanite, kunzite, tourmaline, diamant ; avec une laque, l'opale et l'ambre",
            What = "Dans la chambre à vide, une couche de quelques atomes se dépose sur les facettes : l'or donne l'« aqua aura » d'un quartz, le titane la topaze « mystique » aux couleurs d'arc-en-ciel, le cobalt un bleu profond. On peut aussi poser une laque noire sous une opale translucide, une laque verte sous l'ambre, ou une surcouche d'émeraude de synthèse sur un béryl pâle. La couche s'use.",
            Diamond = "Un film mince de cobalt masque le jaune et améliore la couleur de 1 à 3 degrés. Tout autre film en fait un diamant fancy. Le certificat n'est plus valable.",
            Resale = "Décote déclarée : 15 % (28 % pour un diamant). Chance qu'un acheteur le trouve : environ 80 %.",
            Reveals = new[]
            {
                "<b>Loupe :</b> des éraflures et des plaques de couche usée sur les arêtes.",
                "<b>Spectroscope :</b> le spectre est celui de la couche (or, titane, cobalt, peinture), pas celui de la pierre.",
                "<b>EDXRF :</b> des métaux en surface que la pierre ne contient pas.",
                "<b>Filtre Chelsea :</b> la surcouche d'émeraude de synthèse devient rouge vif ; le cobalt, rose.",
                "<b>MEB et LA-ICP-MS :</b> le film lui-même, avec ses éléments.",
            },
            Blind = new[] { "L'infrarouge : la couche est trop mince pour s'y lire." },
        },
        new Treatment
        {
            Instruments = new[] { "Infrarouge", "Raman", "Lampe UV", "LA-ICP-MS", "Loupe", "Microscope" },
            Name = "Imprégnation et remplissage", Materials = "Émeraude, rubis, saphir, turquoise, jade, lapis, corail, opale, topaze, tourmaline, péridot, fluorite, feldspath, diamant",
            What = "Dans l'autoclave, un produit pénètre les fissures et les pores : huile de cèdre ou colorée, cire, résine époxy, résine de stabilisation, verre au plomb, borax. Les huiles et les cires sont temporaires, les résines et le verre durables. Le bain simple pénètre peu, le vide puis la pression vont partout.",
            Diamond = "Verre au plomb seulement, sur un diamant taillé qui a au moins une plume en surface et une pureté VS2 ou moins. La pureté monte de 1 à 3 degrés, jamais au-delà de VVS2. Le certificat n'est plus valable.",
            Resale = "Décote déclarée : 10 % (14 % pour un fondant, 18 % pour du verre au plomb, 30 % pour un diamant). Chance qu'un acheteur le trouve : environ 65 %, et 85 % pour du verre.",
            Reveals = new[]
            {
                "<b>Infrarouge et Raman :</b> ils identifient le produit (huile, cire, résine, verre).",
                "<b>Lampe UV :</b> une résine ou une cire brille en blanc-bleuté.",
                "<b>LA-ICP-MS :</b> du plomb irrégulier, à plusieurs milliers de ppm, dans un verre au plomb.",
                "<b>Loupe et microscope :</b> les fractures et fissures remplies.",
            },
            Blind = new[] { "Le spectroscope et le dichroscope : le remplissage ne change pas la couleur de la pierre." },
        },
        new Treatment
        {
            Instruments = new[] { "Microscope", "Loupe", "EDXRF", "MEB", "LA-ICP-MS" },
            Name = "Diffusion", Materials = "Corindon seulement : rubis et saphirs",
            What = "Dans le four tubulaire, un dopant entre dans la pierre à haute température : fer et titane pour le bleu, chrome pour le rouge, nickel pour le jaune, titane en excès pour l'astérisme, béryllium pour la diffusion à cœur. Plus la température monte, plus la couleur va profond, mais au-delà de 1 950 degrés la pierre risque d'être perdue. Une couche de surface disparaît quand on repolit.",
            Resale = "Décote déclarée : 18 % en surface, 8 % à cœur. Chance qu'un acheteur le trouve : environ 70 %, mais 30 % pour une diffusion peu profonde au béryllium et 15 % à cœur.",
            Reveals = new[]
            {
                "<b>Immersion au microscope :</b> en surface, la couleur longe les arêtes de facettes en toile d'araignée ; à cœur au béryllium, les zones suivent la forme de la pierre avec un pourtour orange.",
                "<b>Loupe :</b> de la couleur concentrée le long des arêtes.",
                "<b>EDXRF :</b> une teneur anormale en nickel, chrome ou titane en surface.",
                "<b>MEB :</b> de petites piqûres.",
                "<b>LA-ICP-MS :</b> le seul à lire le béryllium, avec son profil de profondeur.",
            },
            Blind = new[] { "L'infrarouge ne voit rien, et l'EDXRF ne voit pas le béryllium, un élément trop léger." },
        },
        new Treatment
        {
            Instruments = new[] { "Loupe", "Microscope", "MEB" },
            Name = "Laser", Materials = "Diamant seulement",
            What = "Le laser perce un canal fin jusqu'à une inclusion foncée, que l'on blanchit ensuite à l'acide : la pureté monte. Le laser classique laisse un canal droit, le laser violent « KM » des fissures en zigzag ou en spirale, plus difficiles à voir mais plus fragiles. Il faut une inclusion foncée accessible, et un diamant déjà traité ne se perce pas deux fois.",
            Diamond = "Le diamant traité perd son certificat, et sa pureté officielle est cachée.",
            Resale = "Décote déclarée : 5 % (12 % pour le laser KM). Chance qu'un acheteur le trouve : environ 50 %.",
            Reveals = new[]
            {
                "<b>Loupe et microscope :</b> un à trois canaux droits de la surface à l'inclusion, ou, pour le KM, des fissures en zigzag.",
                "<b>MEB :</b> le trou du laser ou les fissures du KM.",
            },
            Blind = new[] { "L'infrarouge : le laser ne laisse pas de trace infrarouge. Il faut chercher le canal." },
        },
        new Treatment
        {
            Instruments = new[] { "Lampe UV", "Photoluminescence", "Infrarouge", "Spectroscope", "Raman", "DiamondView" },
            Name = "HPHT", Materials = "Diamant seulement",
            What = "La presse chauffe le diamant à plus de 2 000 degrés sous haute pression. Un diamant de type IIa pauvre en azote devient incolore ; un diamant azoté devient d'un jaune intense, jaune-vert, avec une fluorescence verte. Si la pression est trop basse pour la température, le diamant se transforme en graphite.",
            Diamond = "Un diamant fancy ne se traite pas ici : on le perdrait.",
            Resale = "Décote déclarée : 10 %. Chance qu'un acheteur le trouve : environ 20 %, le plus discret des traitements du diamant.",
            Reveals = new[]
            {
                "<b>Lampe UV :</b> une fluorescence verte très forte, les centres d'azote.",
                "<b>Photoluminescence :</b> la raie H3 à 503 nm intense, et le NV0 plus fort que le NV-.",
                "<b>Infrarouge :</b> l'azote isolé (centre C) à 1344 et 1130.",
                "<b>Spectroscope :</b> la raie H3 s'ajoute au spectre.",
                "<b>Raman et DiamondView :</b> les centres NV d'un recuit.",
            },
            Blind = new[] { "Le LA-ICP-MS : le HPHT ne laisse aucun élément étranger." },
        },
    };

    private class Instrument
    {
        public string Name;
        public string Subtitle;
        public string Reads;
        public string[] Reveals;
        public string[] Blind;           // what it does not see, or null
    }

    private static readonly Instrument[] Instruments =
    {
        new Instrument
        {
            Name = "Loupe", Subtitle = "Grossissement 10× et 20×",
            Reads = "La surface et l'intérieur proche : arêtes, facettes, inclusions visibles, fissures, canaux. C'est le premier regard, avant tout autre instrument.",
            Reveals = new[]
            {
                "<b>Chauffage :</b> fissures de tension, inclusions lenticulaires, halos.",
                "<b>Laser :</b> un à trois canaux droits, ou les fissures en zigzag du laser KM.",
                "<b>Teinture :</b> la couleur en veinules dans les fissures.",
                "<b>Revêtement :</b> des éraflures et des plaques de couche usée sur les arêtes.",
                "<b>Blanchiment :</b> les piqûres de l'attaque acide.",
                "<b>Diffusion en surface :</b> la couleur concentrée le long des arêtes.",
            },
            Blind = new[] { "Irradiation et HPHT : aucune trace visible.", "Une diffusion à cœur au béryllium." },
        },
        new Instrument
        {
            Name = "Microscope", Subtitle = "Fort grossissement et immersion",
            Reads = "Les inclusions de près et, dans un liquide de même indice que la pierre, la répartition de la couleur : régulière, concentrée dans les fissures, ou le long des arêtes.",
            Reveals = new[]
            {
                "<b>Diffusion :</b> en surface, une toile d'araignée le long des arêtes ; à cœur au béryllium, des zones qui suivent la forme de la pierre avec un pourtour orange.",
                "<b>Teinture :</b> des plages de couleur dans les fissures.",
                "<b>Chauffage :</b> des halos de tension autour des inclusions.",
                "<b>Remplissage :</b> les fractures et fissures remplies.",
                "<b>Laser :</b> les canaux et les fissures du KM.",
            },
            Blind = new[] { "Irradiation et HPHT : aucune trace visible." },
        },
        new Instrument
        {
            Name = "Lampe UV", Subtitle = "Ondes longues et courtes",
            Reads = "La fluorescence de la pierre, qui change avec ce qu'on lui a fait : un colorant, une résine ou un recuit la modifient.",
            Reveals = new[]
            {
                "<b>Teinture :</b> les colorants organiques brillent (rouge orangé, rose, violet) ; les colorants minéraux et le noir carbone éteignent la pierre.",
                "<b>Remplissage :</b> une résine ou une cire brille en blanc-bleuté.",
                "<b>Blanchiment :</b> blanc-bleuté, même sur la perle et le corail.",
                "<b>HPHT :</b> une fluorescence verte très forte.",
            },
            Blind = new[] { "Chauffage, irradiation, diffusion, revêtement et laser." },
        },
        new Instrument
        {
            Name = "Filtre Chelsea", Subtitle = "Un filtre rouge de contrôle",
            Reads = "La couleur de la pierre vue à travers un filtre qui ne laisse passer que le rouge et le jaune-vert : le chrome, dans une pierre ou dans un colorant, vire au rouge.",
            Reveals = new[]
            {
                "<b>Revêtement :</b> la surcouche d'émeraude de synthèse devient rouge vif ; le cobalt, rose.",
                "<b>Teinture :</b> le vert chrome réagit en rouge, le rose et le rouge en rose.",
            },
            Blind = new[] { "Tous les autres traitements." },
        },
        new Instrument
        {
            Name = "Dichroscope", Subtitle = "Les couleurs selon la direction",
            Reads = "Les couleurs d'une pierre pléochroïque, vues côte à côte dans deux directions : une tanzanite en montre trois, un rubis deux.",
            Reveals = new[]
            {
                "<b>Chauffage :</b> une tanzanite chauffée n'a plus que deux couleurs, bleu et violet ; une tourmaline chauffée a un dichroïsme affaibli.",
                "<b>Irradiation :</b> le bleu maxixe d'un béryl est très dichroïque, bleu et incolore ou rose pâle.",
            },
            Blind = new[] { "Les pierres qui ne sont pas pléochroïques, et tous les autres traitements." },
        },
        new Instrument
        {
            Name = "Spectroscope", Subtitle = "Les bandes d'absorption",
            Reads = "Les raies et les bandes sombres du spectre de la pierre : celles de son espèce, et celles que lui ajoutent ses traitements.",
            Reveals = new[]
            {
                "<b>Teinture :</b> une bande large de colorant.",
                "<b>Irradiation :</b> une bande de centres colorés (le fumé vers 500 nm, le maxixe vers 695 et 615 nm).",
                "<b>Revêtement :</b> le spectre de la couche, pas celui de la pierre.",
                "<b>HPHT :</b> la raie H3 à 503 nm.",
            },
            Blind = new[] { "Chauffage, blanchiment, remplissage et laser." },
        },
        new Instrument
        {
            Name = "UV-Vis-NIR", Subtitle = "Le spectre en courbe",
            Reads = "Le même spectre que le spectroscope, mesuré en courbe, de l'ultraviolet à l'infrarouge proche : on y lit les bandes avec leur position et leur force.",
            Reveals = new[]
            {
                "<b>Teinture, irradiation, revêtement et HPHT :</b> les mêmes bandes que le spectroscope.",
                "<b>Recuit :</b> il lit aussi les effets d'un chauffage ou d'un HPHT sur le spectre.",
            },
            Blind = new[] { "Laser, blanchiment et remplissage." },
        },
        new Instrument
        {
            Name = "Infrarouge", Subtitle = "FTIR : l'empreinte chimique",
            Reads = "L'empreinte des liaisons chimiques de la matière : elle identifie les produits qui remplissent une pierre, et les défauts d'un diamant.",
            Reveals = new[]
            {
                "<b>Remplissage :</b> huile, cire, résine ou verre, identifié.",
                "<b>Chauffage :</b> peut conclure sur un corindon chauffé.",
                "<b>HPHT :</b> l'azote isolé (centre C) à 1344 et 1130.",
                "<b>Irradiation d'un diamant :</b> les raies H1a et H1b.",
            },
            Blind = new[] { "Teinture, revêtement, diffusion et laser : aucune trace infrarouge." },
        },
        new Instrument
        {
            Name = "Raman", Subtitle = "Identifier la matière",
            Reads = "La signature de vibration de la matière touchée par le laser : le minéral, une inclusion, le produit d'un remplissage.",
            Reveals = new[]
            {
                "<b>Remplissage :</b> le produit, huile de cèdre, paraffine, résine époxy, verre au plomb ou fondant.",
                "<b>Chauffage et HPHT d'un diamant :</b> les centres NV d'un recuit.",
            },
            Blind = null,
        },
        new Instrument
        {
            Name = "EDXRF", Subtitle = "Les éléments de la surface",
            Reads = "Les éléments chimiques présents en surface, sauf les plus légers.",
            Reveals = new[]
            {
                "<b>Revêtement :</b> des métaux en surface que la pierre ne contient pas.",
                "<b>Diffusion :</b> une teneur anormale en nickel, chrome ou titane.",
                "<b>Teinture :</b> le fer et le potassium du bleu de Prusse.",
            },
            Blind = new[] { "Le béryllium, trop léger : une diffusion à cœur au béryllium n'apparaît pas." },
        },
        new Instrument
        {
            Name = "MEB", Subtitle = "La surface à très fort grossissement",
            Reads = "La surface vue de très près, avec l'analyse des éléments : le film d'un revêtement, les piqûres, les fissures.",
            Reveals = new[]
            {
                "<b>Revêtement :</b> le film lui-même.",
                "<b>Blanchiment :</b> les piqûres de l'acide.",
                "<b>Diffusion :</b> de petites piqûres en surface.",
                "<b>Laser :</b> le trou du laser, ou les fissures du KM.",
                "<b>Teinture :</b> le colorant dans les fractures.",
                "<b>Chauffage :</b> les aiguilles de rutile en pointillés, dans un corindon.",
            },
            Blind = null,
        },
        new Instrument
        {
            Name = "LA-ICP-MS", Subtitle = "Les éléments, en profondeur",
            Reads = "Un laser prélève un peu de matière, analysée élément par élément, jusqu'aux traces et au profil de profondeur.",
            Reveals = new[]
            {
                "<b>Diffusion :</b> le béryllium et son profil de profondeur : c'est le seul à le lire.",
                "<b>Revêtement :</b> les couches.",
                "<b>Teinture :</b> les éléments du colorant.",
                "<b>Remplissage :</b> le plomb d'un verre au plomb, irrégulier, à plusieurs milliers de ppm.",
            },
            Blind = new[] { "Chauffage, irradiation, blanchiment, laser et HPHT ne laissent aucun élément étranger." },
        },
        new Instrument
        {
            Name = "Photoluminescence", Subtitle = "Les défauts du réseau, à 77 K",
            Reads = "La lumière que renvoie la pierre excitée par un laser à 488 nm et refroidie : les défauts du réseau donnent des raies fines. Cela sert surtout pour les diamants.",
            Reveals = new[]
            {
                "<b>HPHT :</b> la raie H3 à 503 nm intense, et le NV0 plus fort que le NV-.",
                "<b>Irradiation d'un diamant :</b> la raie GR1 à 741 nm, plus forte après des neutrons.",
                "<b>Diamant de synthèse :</b> le Si-V à 737 nm pour un CVD, le nickel à 883 nm pour un HPHT de synthèse.",
            },
            Blind = new[] { "Les rubis et les émeraudes donnent les raies du chrome, qui ne disent rien du traitement." },
        },
        new Instrument
        {
            Name = "DiamondView", Subtitle = "Les figures de croissance du diamant",
            Reads = "La fluorescence de surface d'un diamant sous ultraviolet profond, qui montre comment le cristal a grandi.",
            Reveals = new[]
            {
                "<b>Chauffage et HPHT :</b> un diamant naturel recuit se distingue d'un diamant non traité.",
                "<b>Synthèse :</b> les figures de croissance d'un CVD ou d'un HPHT de synthèse.",
            },
            Blind = new[] { "Les pierres qui ne sont pas des diamants." },
        },
    };

    // A strip of the pictures of instruments, with their names under them.
    private static BookStrip InstrumentStrip(string title, string[] names)
    {
        var strip = new BookStrip { Title = title, Labeled = true };
        foreach (var name in names)
            strip.Icons.Add(new BookIcon { Sprite = BookIcons.Instrument(name), Label = name == "Photoluminescence" ? "Photolum." : name });
        return strip;
    }

    public static BookContent BuildTreatments()
    {
        var book = new BookContent
        {
            IndexTitle = "Les traitements",
            IntroHeading = "Avant de lire",
            TitleNote = "neuf traitements et quatorze instruments",
            IntroText = Blocks(
                Turning,
                Heading("Un chapitre par traitement") + "\n" +
                "À gauche : ce que le traitement fait, pour quelles pierres, ce qu'il vaut à la revente. À droite : les instruments qui le trahissent, avec leur image, et ce qu'ils y voient.\n\n" +
                Heading("Un chapitre par instrument") + "\n" +
                "Après les traitements, chaque instrument du laboratoire a son chapitre : ce qu'il lit, les traitements qu'il révèle et ceux qu'il ne voit pas.",
                Note("Un traitement ne laisse pas toujours de trace : c'est en recoupant plusieurs instruments qu'on conclut.")),
        };

        foreach (var t in Treatments)
        {
            var treatment = t;
            book.Entries.Add(new BookEntry
            {
                Title = treatment.Name,
                Subtitle = treatment.Materials,
                LeftHead = "Manuel des traitements",
                RightHead = treatment.Name + " : le reconnaître",
                LeftBuilder = () => Blocks(
                    Heading("En bref") + "\n" + treatment.What,
                    treatment.Diamond != null ? Heading("Sur un diamant") + "\n" + treatment.Diamond : null,
                    Heading("À la revente") + "\n" + treatment.Resale),
                RightStrip = InstrumentStrip("Instruments qui le trahissent", treatment.Instruments),
                RightBuilder = () => Blocks(
                    Heading("Ce qui le trahit") + "\n" + Bullets(treatment.Reveals),
                    treatment.Blind != null ? Heading("Ce qui ne le voit pas") + "\n" + Bullets(treatment.Blind) : null),
            });
        }

        foreach (var i in Instruments)
        {
            var instrument = i;
            book.Entries.Add(new BookEntry
            {
                Title = instrument.Name,
                Subtitle = instrument.Subtitle,
                LeftHead = "Les instruments",
                RightHead = instrument.Name + " : ce qu'il révèle",
                Icon = BookIcons.Instrument(instrument.Name),
                LeftBuilder = () => Blocks(
                    Heading("Ce qu'il lit") + "\n" + instrument.Reads,
                    instrument.Blind != null ? Heading("Ce qu'il ne voit pas") + "\n" + Bullets(instrument.Blind) : null),
                RightBuilder = () => Heading("Les traitements qu'il révèle") + "\n" + Bullets(instrument.Reveals),
            });
        }
        return book;
    }

    // ---- L'Atlas des gisements ----------------------------------------------------------------------------------------

    private class OriginSpecies
    {
        public GemDefinition Def;
        public float Multiplier;
        public float Share;              // the weight of this origin among the origins of the species
        public string Inclusions;
    }

    private static string RarityOf(float share)
    {
        return share >= 0.4f ? "fréquent" : share >= 0.15f ? "courant" : "rare";
    }

    private static string Multiplier(float value)
    {
        var percent = Math.Round((value - 1f) * 100f);
        var sign = percent > 0 ? "+" : "";
        return "×" + value.ToString("0.00", French) + " (" + sign + percent.ToString("0", French) + " %)";
    }

    // The logo of a deposit: the pictures of the stones that are found there, the most valued first.
    private static BookStrip DepositStrip(List<OriginSpecies> found)
    {
        var strip = new BookStrip { Title = "Les pierres de ce gisement" };
        foreach (var species in found.Take(18)) strip.Icons.Add(new BookIcon { Sprite = species.Def.GetIcon() });
        return strip;
    }

    public static BookContent BuildAtlas()
    {
        var book = new BookContent
        {
            IndexTitle = "Les gisements",
            IntroHeading = "Comment lire cet atlas",
            IntroText = Blocks(
                Turning,
                Heading("Un chapitre par gisement") + "\n" +
                "À gauche : les pierres que l'on y trouve, avec leur fréquence dans ce gisement et ce qu'il fait à leur valeur. À droite : le meilleur et le moins bon, et les inclusions que ce gisement donne à ses pierres.",
                Heading("Lire les chiffres") + "\n" +
                "· <b>×1,25</b> veut dire une pierre qui vaut un quart de plus qu'une pierre de même qualité d'un gisement ordinaire.\n" +
                "· <b>Fréquent, courant, rare :</b> la part de ce gisement parmi tous ceux où l'on trouve l'espèce.",
                Note("Le prix d'une pierre dépend aussi de sa pureté, de sa taille, de sa couleur et de ses traitements.")),
        };

        var database = GemDatabase.Instance;
        if (database == null) return book;

        var byOrigin = new SortedDictionary<string, List<OriginSpecies>>(StringComparer.Create(French, true));
        foreach (var def in database.allGems)
        {
            if (def == null || def.origins == null || def.origins.Count == 0) continue;
            var total = 0f;
            foreach (var origin in def.origins) if (origin != null) total += Math.Max(0f, origin.rarityWeight);
            foreach (var origin in def.origins)
            {
                if (origin == null || string.IsNullOrWhiteSpace(origin.name)) continue;
                if (!byOrigin.TryGetValue(origin.name, out var list)) byOrigin[origin.name] = list = new List<OriginSpecies>();
                list.Add(new OriginSpecies
                {
                    Def = def,
                    Multiplier = origin.valueMultiplier <= 0f ? 1f : origin.valueMultiplier,
                    Share = total > 0f ? Math.Max(0f, origin.rarityWeight) / total : 1f,
                    Inclusions = origin.inclusionOverride,
                });
            }
        }

        foreach (var pair in byOrigin)
        {
            var name = pair.Key;
            var found = pair.Value.OrderByDescending(s => s.Multiplier).ThenBy(s => s.Def.displayName, StringComparer.Create(French, true)).ToList();
            var average = found.Average(s => s.Multiplier);
            book.Entries.Add(new BookEntry
            {
                Title = name,
                Subtitle = found.Count + (found.Count > 1 ? " espèces" : " espèce") + "  ·  valeur moyenne " + Multiplier(average),
                LeftHead = "Atlas des gisements",
                RightHead = name,
                Icon = found[0].Def.GetIcon(),
                IndexIcon = BookFlags.Get(name),
                MapBuilder = () => BookMaps.For(name),
                IndexIconIsFlag = true,
                LeftStrip = DepositStrip(found),
                LeftBuilder = () => Heading("Pierres de ce gisement") + "\n" + string.Join("\n",
                    found.Select(s => "· <b>" + s.Def.displayName + " :</b> " + Multiplier(s.Multiplier) + ", " + RarityOf(s.Share))),
                RightBuilder = () =>
                {
                    var best = found[0];
                    var worst = found[found.Count - 1];
                    var withInclusions = found.Where(s => !string.IsNullOrWhiteSpace(s.Inclusions)).ToList();
                    return Blocks(
                        Heading("Ce que le gisement ajoute") + "\n" + Bullets(
                            "<b>Valeur moyenne :</b> " + Multiplier(average),
                            "<b>Le plus recherché :</b> " + best.Def.displayName + ", " + Multiplier(best.Multiplier),
                            found.Count > 1 ? "<b>Le moins prisé :</b> " + worst.Def.displayName + ", " + Multiplier(worst.Multiplier) : null),
                        withInclusions.Count > 0
                            ? Heading("Inclusions de ce gisement") + "\n" + string.Join("\n", withInclusions.Select(s => "· <b>" + s.Def.displayName + " :</b> " + s.Inclusions))
                            : Heading("Inclusions de ce gisement") + "\n" + "Aucune inclusion propre à ce gisement : ses pierres montrent celles de leur espèce.");
                },
            });
        }
        return book;
    }

    // ---- Le Précis du marché ------------------------------------------------------------------------------------------

    private static string StigmaTable()
    {
        return Bullets(
            "<b>Chauffage :</b> 2 %",
            "<b>Laser :</b> 5 % (laser KM : 12 %)",
            "<b>Irradiation :</b> 8 % (diamant : 22 %)",
            "<b>Diffusion :</b> 8 % à cœur, 18 % en surface",
            "<b>HPHT :</b> 10 %",
            "<b>Blanchiment :</b> 12 %",
            "<b>Revêtement :</b> 15 % (diamant : 28 %)",
            "<b>Remplissage :</b> 10 % (fondant 14 %, verre 18 %, diamant 30 %)",
            "<b>Teinture :</b> 20 %");
    }

    public static BookContent BuildMarket()
    {
        var book = new BookContent
        {
            IndexTitle = "Les chapitres",
            IntroHeading = "À qui s'adresse ce précis",
            TitleNote = "les règles de la revente, avec leurs chiffres",
            IntroText = Blocks(
                Turning,
                Heading("Ce que vous y trouverez") + "\n" +
                "Comment les marchands achètent et vendent, ce qu'il en coûte de taire un traitement, le fonctionnement des enchères du samedi, le travail du bijoutier, la banque et la liste Rapaport.",
                Note("Les chiffres sont ceux du jeu : un traitement déclaré se paie moins cher, un traitement caché peut coûter une amende.")),
        };

        book.Entries.Add(new BookEntry
        {
            Title = "Les marchés de la ville", Subtitle = "Ce que les marchands paient et ce qu'ils demandent",
            LeftHead = "Précis du marché", RightHead = "Les marchés (suite)",
            LeftBuilder = () => Blocks(
                Heading("Acheter") + "\n" + "Un marchand vend ses pierres à " + MarketState.PriceMarkup.ToString("0.0", French) + " fois leur valeur. C'est le prix d'un client : on ne fait pas d'affaire en revendant sur place ce qu'on vient d'y acheter.",
                Heading("Vendre") + "\n" + Bullets(
                    "Un marchand rachète entre <b>" + Percent(MarketState.BuybackMin) + "</b> et <b>" + Percent(MarketState.BuybackMax) + "</b> de la valeur de la pierre, selon le marchand.",
                    "Une pierre de son propre pays se paie <b>×" + MarketState.HomeCountryBonus.ToString("0.0", French) + "</b>.",
                    "Une pierre brute se paie seulement <b>" + Percent(MarketState.RoughPenalty) + "</b> de ce qu'elle vaudrait taillée.",
                    "Chaque pierre a son petit écart personnel d'environ <b>± " + Percent(MarketState.StoneQuirkSpread) + "</b> : deux pierres de même qualité ne se paient pas tout à fait pareil.")),
            RightBuilder = () => Blocks(
                Heading("Conseils") + "\n" + Bullets(
                    "Allez voir le marchand du pays de la pierre : il la paie 30 % de plus.",
                    "Taillez les pierres brutes avant de les revendre.",
                    "Comparez les marchands avant de vendre : leurs prix ne sont pas les mêmes.",
                    "Les marchés ont des horaires : ils ouvrent à 9 h et ferment à 20 h."),
                Note("Pour une pierre traitée, voir le chapitre suivant : ce qu'on dit au marchand change le prix.")),
        });

        book.Entries.Add(new BookEntry
        {
            Title = "Déclarer ou taire un traitement", Subtitle = "La divulgation, le contrôle et l'amende",
            LeftHead = "Précis du marché", RightHead = "Divulgation (suite)",
            LeftBuilder = () => Blocks(
                Heading("Déclarer") + "\n" + "La pierre est payée à sa vraie valeur, moins la décote du traitement. C'est sûr : il n'y a aucun risque.",
                Heading("Se taire") + "\n" + "La pierre est payée comme une pierre non traitée, au prix fort. Mais l'acheteur peut la tester : plus le traitement est visible, plus le risque est grand. Un marchand de plus d'expérience le trouve plus souvent.",
                Heading("Si l'acheteur le trouve") + "\n" + Bullets(
                    "La vente est annulée, et la pierre reste à vous.",
                    "Si vous le saviez : une amende de <b>" + TreatmentDisclosure.FinePercent + " %</b> du prix demandé, et <b>" + TreatmentDisclosure.ReputationLoss + " points</b> de réputation en moins.",
                    "Si vous l'ignoriez : pas d'amende, mais une nouvelle offre au prix d'un traitement déclaré.")),
            RightBuilder = () => Blocks(
                Heading("Décote d'un traitement déclaré") + "\n" + StigmaTable(),
                Note("Chaque traitement de plus ajoute 2 points de décote. Le prix de la pierre a déjà baissé du fait du traitement lui-même : la décote s'y ajoute.")),
        });

        book.Entries.Add(new BookEntry
        {
            Title = "Les enchères du samedi", Subtitle = "Le bulletin, le catalogue et la salle",
            LeftHead = "Précis du marché", RightHead = "Les enchères (suite)",
            LeftBuilder = () => Blocks(
                Heading("Le rythme") + "\n" + Bullets(
                    "Une vente chaque samedi, à la banque, de 9 h à 20 h, sur un thème.",
                    "Le bulletin des ventes paraît le dimanche, dans l'onglet « Enchères » du sac : les lots, leurs images, leurs estimations.",
                    "Le calendrier annonce la veille le vendredi et la vente le samedi."),
                Heading("Acheter un lot") + "\n" + Bullets(
                    "Dans la salle, de deux à quatre acheteurs simulés enchérissent, chacun avec sa limite.",
                    "Vous payez le prix d'adjudication plus <b>" + Percent(AuctionCatalog.BuyerPremium) + "</b> de frais de la maison.",
                    "Le catalogue décrit le lot, mais pas toujours honnêtement : examinez les lots en salle.")),
            RightBuilder = () => Blocks(
                Heading("Ce que dit le catalogue") + "\n" + Bullets(
                    "Pour une pierre traitée : le traitement est <b>déclaré</b> une fois sur deux, <b>passé sous silence</b> trois fois sur dix, et <b>nié</b> deux fois sur dix.",
                    "Pour une pierre sans traitement : le catalogue le dit six fois sur dix, et se tait quatre fois sur dix.",
                    "La salle paie plus cher quand le catalogue ment : c'est à l'acheteur de vérifier."),
                Heading("Vendre") + "\n" + Bullets(
                    "Vous pouvez confier jusqu'à <b>" + AuctionState.MaxConsignments + " pierres</b> à la maison pour la prochaine vente, avec un prix de réserve.",
                    "La maison garde <b>" + Percent(AuctionCatalog.SellerCommission) + "</b> du prix d'adjudication.",
                    "Une pierre invendue vous est rendue. Les règles de la divulgation valent aussi ici : traitement caché et trouvé, amende et réputation.")),
        });

        book.Entries.Add(new BookEntry
        {
            Title = "Le bijoutier", Subtitle = "Sertir une pierre, et ce que le bijou cache",
            LeftHead = "Précis du marché", RightHead = "Le bijoutier (suite)",
            LeftBuilder = () => Blocks(
                Heading("Les bijoux") + "\n" + Bullets(JewelCatalog.Kinds.Select(k => "<b>" + k.Name + " :</b> " + k.Detail.ToLowerInvariant() + ", " + k.Grams.ToString("0", French) + " g de métal, " + k.Labor + " € de façon, " + k.Days + (k.Days > 1 ? " jours" : " jour")).ToArray()),
                Heading("Les métaux") + "\n" + Bullets(JewelCatalog.Metals.Select(m => "<b>" + m.Name + " (" + m.Detail + ") :</b> " + m.PricePerGram.ToString("0", French) + " € le gramme").ToArray())),
            RightBuilder = () => Blocks(
                Heading("Les montures") + "\n" + Bullets(JewelCatalog.Settings.Select(s => "<b>" + s.Name + " :</b> " + Percent(s.Visibility) + " de la pierre reste visible, " + Percent(s.Light) + " de la lumière").ToArray()),
                Heading("À savoir") + "\n" + Bullets(
                    "Une pierre sertie se cache en partie : le fond fermé couvre la culasse, où se trouvent les trous du laser, les remplissages et les revêtements.",
                    "La soudure chauffe : elle peut abîmer un remplissage. Les griffes peuvent ébrécher une pierre tendre.",
                    "Une pierre sertie ne se teste plus : il faut la faire sortir de sa monture, pour " + JewelCatalog.UnsetFee + " €.",
                    "Le bijoutier rachète ses bijoux à " + Percent(JewelCatalog.JewelerShare) + " de leur valeur, et il est assez expert pour trouver un traitement caché.")),
        });

        book.Entries.Add(new BookEntry
        {
            Title = "La banque", Subtitle = "Le compte, les taux et les intérêts",
            LeftHead = "Précis du marché", RightHead = "La banque (suite)",
            LeftBuilder = () => Blocks(
                Heading("Le compte") + "\n" + Bullets(
                    "Le solde du compte est plafonné à <b>" + Euro(BankState.Ceiling) + "</b>.",
                    "Le taux change chaque mois. Certains mois sont excellents : jusqu'à 10 % par mois."),
                Heading("Les intérêts") + "\n" + "Ils sont versés le premier du mois suivant, sur le <b>solde le plus bas</b> du mois : un dépôt ne rapporte qu'à partir du mois d'après, et retirer de l'argent en cours de mois diminue les intérêts."),
            RightBuilder = () => Blocks(
                Heading("Conseils") + "\n" + Bullets(
                    "Déposez tôt dans le mois et ne retirez pas.",
                    "Gardez de quoi payer les enchères : un retrait au dernier moment coûte le mois d'intérêts.",
                    "Le plafond oblige à dépenser ou à investir : une belle pierre rapporte plus qu'un solde qui dort.")),
        });

        book.Entries.Add(new BookEntry
        {
            Title = "Le Rapaport", Subtitle = "La liste des prix des diamants",
            LeftHead = "Précis du marché", RightHead = "Le Rapaport (suite)",
            LeftBuilder = () => Blocks(
                Heading("Ce que c'est") + "\n" + "Une liste de prix au carat des diamants ronds brillants, par poids, couleur et pureté. Elle paraît chaque jeudi. Les prix suivent la valeur du jeu, multipliée par un indice du marché qui change d'une semaine à l'autre.",
                Heading("L'abonnement") + "\n" + Bullets(
                    "<b>" + Euro(RapaportState.MonthlyPrice) + "</b> par mois, au bureau de Martin Rapaport, au sixième étage du quartier des diamantaires.",
                    "Tant qu'il dure, un onglet « Rapaport » apparaît dans le sac.")),
            RightBuilder = () => Blocks(
                Heading("S'en servir") + "\n" + Bullets(
                    "Comparez le prix de la liste au prix demandé par un diamantaire.",
                    "Un diamant dont la couleur ou la pureté est incertaine se lit entre deux cases : faites-le d'abord graver et noter.",
                    "L'indice change chaque semaine : vendez quand il est haut.")),
        });
        return book;
    }
}
