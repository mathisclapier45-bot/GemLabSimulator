using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum CardRarity { Commune, Rare, SuperRare, UltraRare, Gold, Secret }

public enum CardSeries { Element, MasterStone, Gem, Famous, Crystal, Synthesis, Tool, Secret }

public class CardDef
{
    public string Id;
    public CardSeries Series;
    public string Name;
    public string Description;
    public CardRarity Rarity;
    public int Order;               // position in the binder, from 1
    public string Badge;            // big text on the artwork (element symbol, grade letters); null when the artwork is a stone
    public string SubBadge;         // small text next to it (atomic number, category)
    public Color Tint;              // background of the artwork
    public Func<Sprite> Art;        // the stone drawn on the card; null for an element
}

// The collectible cards: the periodic table, the GIA master stones (colors D to Z and clarities FL to I3), the gems of the game,
// the famous stones of history, the crystal systems, the ways of making synthetic stones, and the tools of the gemologist. Every card has a rarity; a booster draws cards at random according to it. The order of the
// list is the order of the binder.
public static class CardCatalog
{
    private static List<CardDef> all;
    private static Dictionary<string, CardDef> byId;
    private static Dictionary<CardRarity, List<CardDef>> pools;

    public static IReadOnlyList<CardDef> All
    {
        get
        {
            Ensure();
            return all;
        }
    }

    public static int Count => All.Count;

    public static CardDef Find(string id)
    {
        Ensure();
        return id != null && byId.TryGetValue(id, out var card) ? card : null;
    }

    public static IReadOnlyList<CardDef> Pool(CardRarity rarity)
    {
        Ensure();
        return pools.TryGetValue(rarity, out var list) ? list : new List<CardDef>();
    }

    // How often each rarity comes out of a booster (secret cards are never drawn: they reward a completed series).
    public static float Weight(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Commune: return 60f;
            case CardRarity.Rare: return 24f;
            case CardRarity.SuperRare: return 10f;
            case CardRarity.UltraRare: return 4.5f;
            case CardRarity.Gold: return 1.5f;
            default: return 0f;
        }
    }

    public static string RarityName(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Commune: return "Commune";
            case CardRarity.Rare: return "Rare";
            case CardRarity.SuperRare: return "Super rare";
            case CardRarity.UltraRare: return "Ultra rare";
            case CardRarity.Gold: return "Gold";
            default: return "Secrète";
        }
    }

    public static Color RarityColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Commune: return new Color(0.62f, 0.64f, 0.70f);
            case CardRarity.Rare: return new Color(0.35f, 0.62f, 1f);
            case CardRarity.SuperRare: return new Color(0.70f, 0.40f, 0.95f);
            case CardRarity.UltraRare: return new Color(1f, 0.38f, 0.65f);
            case CardRarity.Gold: return new Color(0.98f, 0.80f, 0.25f);
            default: return new Color(0.45f, 0.95f, 0.90f);
        }
    }

    public static string SeriesName(CardSeries series)
    {
        switch (series)
        {
            case CardSeries.Element: return "Éléments";
            case CardSeries.MasterStone: return "Master stones";
            case CardSeries.Gem: return "Gemmes";
            case CardSeries.Famous: return "Pierres célèbres";
            case CardSeries.Crystal: return "Cristaux";
            case CardSeries.Synthesis: return "Synthèses";
            case CardSeries.Tool: return "Outils";
            default: return "Secrètes";
        }
    }

    // Draws `count` cards for a booster. At least one of them is a Rare or better.
    public static List<CardDef> DrawBooster(int count)
    {
        Ensure();
        var drawn = new List<CardDef>();
        var hasRare = false;
        for (var i = 0; i < count; i++)
        {
            var rarity = RollRarity();
            if (i == count - 1 && !hasRare) rarity = CardRarity.Rare;
            var pool = Pool(rarity);
            if (pool.Count == 0) pool = Pool(CardRarity.Commune);
            var card = pool[UnityEngine.Random.Range(0, pool.Count)];
            if (rarity != CardRarity.Commune) hasRare = true;
            drawn.Add(card);
        }
        return drawn;
    }

    private static CardRarity RollRarity()
    {
        var total = 0f;
        foreach (CardRarity rarity in Enum.GetValues(typeof(CardRarity))) total += Weight(rarity);
        var roll = UnityEngine.Random.value * total;
        foreach (CardRarity rarity in Enum.GetValues(typeof(CardRarity)))
        {
            roll -= Weight(rarity);
            if (roll <= 0f && Weight(rarity) > 0f) return rarity;
        }
        return CardRarity.Commune;
    }

    private static void Ensure()
    {
        if (all != null) return;

        all = new List<CardDef>();
        AddElements();
        AddMasterStones();
        AddGems();
        AddFamousStones();
        AddCrystalSystems();
        AddSyntheses();
        AddTools();

        byId = new Dictionary<string, CardDef>();
        pools = new Dictionary<CardRarity, List<CardDef>>();
        for (var i = 0; i < all.Count; i++)
        {
            all[i].Order = i + 1;
            byId[all[i].Id] = all[i];
            if (!pools.ContainsKey(all[i].Rarity)) pools[all[i].Rarity] = new List<CardDef>();
            pools[all[i].Rarity].Add(all[i]);
        }
    }

    private static Color Facet(Color body) => Color.Lerp(body, Color.white, 0.5f);

    // ---- Periodic table -------------------------------------------------------------------------------------------

    private static string CategoryOf(int z)
    {
        if (z == 1 || z == 6 || z == 7 || z == 8 || z == 15 || z == 16 || z == 34) return "nonmetal";
        if (z == 2 || z == 10 || z == 18 || z == 36 || z == 54 || z == 86 || z == 118) return "noble";
        if (z == 3 || z == 11 || z == 19 || z == 37 || z == 55 || z == 87) return "alkali";
        if (z == 4 || z == 12 || z == 20 || z == 38 || z == 56 || z == 88) return "alkaline";
        if (z == 9 || z == 17 || z == 35 || z == 53 || z == 85 || z == 117) return "halogen";
        if (z == 5 || z == 14 || z == 32 || z == 33 || z == 51 || z == 52) return "metalloid";
        if (z >= 57 && z <= 71) return "lanthanide";
        if (z >= 89 && z <= 103) return "actinide";
        if (z == 13 || z == 31 || z == 49 || z == 50 || (z >= 81 && z <= 84) || (z >= 113 && z <= 116)) return "post";
        return "transition";
    }

    private static string CategoryLabel(string category)
    {
        switch (category)
        {
            case "alkali": return "Métal alcalin";
            case "alkaline": return "Alcalino-terreux";
            case "post": return "Métal pauvre";
            case "metalloid": return "Métalloïde";
            case "nonmetal": return "Non-métal";
            case "halogen": return "Halogène";
            case "noble": return "Gaz noble";
            case "lanthanide": return "Lanthanide";
            case "actinide": return "Actinide";
            default: return "Métal de transition";
        }
    }

    private static Color CategoryColor(string category)
    {
        switch (category)
        {
            case "alkali": return new Color(0.78f, 0.34f, 0.28f);
            case "alkaline": return new Color(0.80f, 0.52f, 0.24f);
            case "post": return new Color(0.32f, 0.58f, 0.55f);
            case "metalloid": return new Color(0.42f, 0.68f, 0.34f);
            case "nonmetal": return new Color(0.30f, 0.66f, 0.42f);
            case "halogen": return new Color(0.74f, 0.68f, 0.22f);
            case "noble": return new Color(0.58f, 0.38f, 0.76f);
            case "lanthanide": return new Color(0.74f, 0.42f, 0.60f);
            case "actinide": return new Color(0.68f, 0.32f, 0.44f);
            default: return new Color(0.30f, 0.46f, 0.76f);
        }
    }

    // Atomic number | symbol | name | one-line description.
    private static readonly string[] Elements =
    {
        "1|H|Hydrogène|Le plus léger des éléments, présent dans l'eau et dans les étoiles.",
        "2|He|Hélium|Gaz noble plus léger que l'air, qui gonfle les ballons.",
        "3|Li|Lithium|Métal alcalin le plus léger, présent dans la tourmaline et dans les batteries.",
        "4|Be|Béryllium|Métal léger qui compose le béryl : émeraude, aigue-marine, morganite.",
        "5|B|Bore|Métalloïde de la tourmaline, qui colore en bleu le diamant.",
        "6|C|Carbone|Base de la vie et du diamant : le même élément que le graphite.",
        "7|N|Azote|Gaz de l'air dont les impuretés jaunissent les diamants de type I.",
        "8|O|Oxygène|Gaz vital, présent dans presque toutes les gemmes (oxydes et silicates).",
        "9|F|Fluor|Halogène très réactif, présent dans la fluorite et la topaze.",
        "10|Ne|Néon|Gaz noble des enseignes lumineuses.",
        "11|Na|Sodium|Métal alcalin tendre, présent dans la jadéite.",
        "12|Mg|Magnésium|Métal léger du spinelle, du péridot et du grenat pyrope.",
        "13|Al|Aluminium|Avec l'oxygène, il forme le corindon : rubis et saphir.",
        "14|Si|Silicium|Base du quartz et des silicates, la plus vaste famille de gemmes.",
        "15|P|Phosphore|Présent dans l'apatite et la turquoise.",
        "16|S|Soufre|Non-métal jaune, présent dans le lapis-lazuli.",
        "17|Cl|Chlore|Halogène du sel de table, gaz jaune-vert toxique.",
        "18|Ar|Argon|Gaz noble le plus abondant de l'air.",
        "19|K|Potassium|Métal alcalin du feldspath (orthose, pierre de lune).",
        "20|Ca|Calcium|Métal du calcaire, de la fluorite et des grenats grossulaires.",
        "21|Sc|Scandium|Métal rare, présent en traces dans certaines émeraudes.",
        "22|Ti|Titane|Avec le fer, il colore en bleu les saphirs ; il forme aussi le rutile.",
        "23|V|Vanadium|Colore la tsavorite, la tanzanite et certaines émeraudes.",
        "24|Cr|Chrome|Fait le rouge du rubis et le vert de l'émeraude.",
        "25|Mn|Manganèse|Colore la morganite en rose et la spessartite en orange.",
        "26|Fe|Fer|Colore le péridot, le grenat almandin et le saphir.",
        "27|Co|Cobalt|Donne leur bleu intense à certains spinelles.",
        "28|Ni|Nickel|Colore certaines gemmes en vert ; résidu des diamants HPHT.",
        "29|Cu|Cuivre|Fait le bleu néon de la tourmaline Paraïba et de la turquoise.",
        "30|Zn|Zinc|Présent dans les spinelles naturels.",
        "31|Ga|Gallium|Métal qui fond dans la main, en traces dans les saphirs de basalte.",
        "32|Ge|Germanium|Métalloïde utilisé en électronique et en optique infrarouge.",
        "33|As|Arsenic|Métalloïde toxique, présent dans certains minéraux.",
        "34|Se|Sélénium|Non-métal qui colore en rouge certains verres.",
        "35|Br|Brome|Seul halogène liquide à température ambiante.",
        "36|Kr|Krypton|Gaz noble rare utilisé en éclairage.",
        "37|Rb|Rubidium|Métal alcalin mou, présent en traces dans certaines émeraudes.",
        "38|Sr|Strontium|Métal alcalino-terreux dont les flammes sont rouge vif.",
        "39|Y|Yttrium|Métal du grenat synthétique YAG.",
        "40|Zr|Zirconium|Base du zircon et de la zircone cubique.",
        "41|Nb|Niobium|Métal qui prend des couleurs irisées par anodisation, utilisé en bijouterie.",
        "42|Mo|Molybdène|Métal des fondants utilisés pour synthétiser des émeraudes.",
        "43|Tc|Technétium|Premier élément synthétisé par l'homme, radioactif.",
        "44|Ru|Ruthénium|Métal dur et brillant du groupe du platine.",
        "45|Rh|Rhodium|Métal très rare et très cher, qui recouvre l'or blanc.",
        "46|Pd|Palladium|Métal du groupe du platine, utilisé en joaillerie.",
        "47|Ag|Argent|Métal précieux blanc, meilleur conducteur d'électricité.",
        "48|Cd|Cadmium|Métal toxique qui colore en jaune et en rouge les verres.",
        "49|In|Indium|Métal si mou qu'on peut le rayer à l'ongle.",
        "50|Sn|Étain|Métal du bronze et de la cassitérite.",
        "51|Sb|Antimoine|Métalloïde utilisé dans les alliages.",
        "52|Te|Tellure|Métalloïde rare, utilisé dans les panneaux solaires.",
        "53|I|Iode|Halogène qui se change en vapeur violette.",
        "54|Xe|Xénon|Gaz noble des lampes à fort éclat.",
        "55|Cs|Césium|Métal alcalin très mou, présent dans la pezzottaïte et certains béryls.",
        "56|Ba|Baryum|Métal alcalino-terreux de la barytine.",
        "57|La|Lanthane|Chef de file des lanthanides, utilisé dans les lentilles optiques.",
        "58|Ce|Cérium|Son oxyde sert à polir le verre et les gemmes.",
        "59|Pr|Praséodyme|Lanthanide qui colore en vert-jaune le verre et les céramiques.",
        "60|Nd|Néodyme|Donne des raies d'absorption fines à l'apatite jaune et au YAG.",
        "61|Pm|Prométhium|Lanthanide radioactif, extrêmement rare dans la nature.",
        "62|Sm|Samarium|Lanthanide utilisé pour fabriquer des aimants puissants.",
        "63|Eu|Europium|Responsable de la fluorescence bleue de la fluorite.",
        "64|Gd|Gadolinium|Lanthanide du grenat synthétique GGG, un simulant du diamant.",
        "65|Tb|Terbium|Lanthanide utilisé dans les luminophores verts.",
        "66|Dy|Dysprosium|Lanthanide aux propriétés magnétiques marquées.",
        "67|Ho|Holmium|Lanthanide au plus fort moment magnétique des éléments.",
        "68|Er|Erbium|Lanthanide qui donne du rose au verre et à certains cristaux.",
        "69|Tm|Thulium|Le moins abondant des lanthanides stables.",
        "70|Yb|Ytterbium|Lanthanide utilisé dans les lasers et les horloges atomiques.",
        "71|Lu|Lutécium|Dernier lanthanide, très dense.",
        "72|Hf|Hafnium|Métal toujours associé au zirconium, présent dans le zircon.",
        "73|Ta|Tantale|Métal très résistant à la corrosion.",
        "74|W|Tungstène|Le métal qui fond à la plus haute température : 3 422 °C.",
        "75|Re|Rhénium|Métal très dense, l'un des plus rares de la croûte terrestre.",
        "76|Os|Osmium|L'élément naturel le plus dense.",
        "77|Ir|Iridium|Métal du groupe du platine, signe de l'impact d'astéroïdes.",
        "78|Pt|Platine|Métal précieux blanc et dense, très utilisé en joaillerie.",
        "79|Au|Or|Métal précieux jaune inaltérable, symbole de richesse depuis toujours.",
        "80|Hg|Mercure|Seul métal liquide à température ambiante.",
        "81|Tl|Thallium|Métal mou et toxique.",
        "82|Pb|Plomb|Métal dense : le verre au plomb comble les fissures de certains rubis.",
        "83|Bi|Bismuth|Métal dont les cristaux forment des escaliers irisés.",
        "84|Po|Polonium|Élément radioactif découvert par Marie Curie.",
        "85|At|Astate|Halogène radioactif, l'élément le plus rare de la croûte terrestre.",
        "86|Rn|Radon|Gaz noble radioactif issu de l'uranium.",
        "87|Fr|Francium|Métal alcalin radioactif, extrêmement instable.",
        "88|Ra|Radium|Métal radioactif découvert par Marie Curie.",
        "89|Ac|Actinium|Premier actinide, radioactif.",
        "90|Th|Thorium|Actinide radioactif, plus abondant que l'uranium.",
        "91|Pa|Protactinium|Actinide très rare et radioactif.",
        "92|U|Uranium|Actinide radioactif : il explique la radioactivité de certains zircons.",
        "93|Np|Neptunium|Premier élément transuranien.",
        "94|Pu|Plutonium|Élément artificiel utilisé dans les réacteurs et les armes.",
        "95|Am|Américium|Élément artificiel présent dans les détecteurs de fumée.",
        "96|Cm|Curium|Élément artificiel nommé en l'honneur de Pierre et Marie Curie.",
        "97|Bk|Berkélium|Élément artificiel nommé d'après la ville de Berkeley.",
        "98|Cf|Californium|Élément artificiel utilisé comme source de neutrons.",
        "99|Es|Einsteinium|Élément artificiel nommé en l'honneur d'Einstein.",
        "100|Fm|Fermium|Élément artificiel nommé en l'honneur d'Enrico Fermi.",
        "101|Md|Mendélévium|Élément artificiel nommé en l'honneur de Mendeleïev.",
        "102|No|Nobélium|Élément artificiel nommé en l'honneur d'Alfred Nobel.",
        "103|Lr|Lawrencium|Dernier actinide, élément artificiel.",
        "104|Rf|Rutherfordium|Premier élément transactinide, éphémère.",
        "105|Db|Dubnium|Élément artificiel éphémère.",
        "106|Sg|Seaborgium|Élément artificiel nommé d'après Glenn Seaborg.",
        "107|Bh|Bohrium|Élément artificiel nommé d'après Niels Bohr.",
        "108|Hs|Hassium|Élément artificiel nommé d'après la Hesse.",
        "109|Mt|Meitnérium|Élément artificiel nommé d'après Lise Meitner.",
        "110|Ds|Darmstadtium|Élément artificiel nommé d'après la ville de Darmstadt.",
        "111|Rg|Roentgenium|Élément artificiel nommé d'après Röntgen.",
        "112|Cn|Copernicium|Élément artificiel nommé d'après Copernic.",
        "113|Nh|Nihonium|Élément artificiel nommé d'après le Japon (Nihon).",
        "114|Fl|Flérovium|Élément artificiel superlourd.",
        "115|Mc|Moscovium|Élément artificiel nommé d'après Moscou.",
        "116|Lv|Livermorium|Élément artificiel nommé d'après la ville de Livermore.",
        "117|Ts|Tennesse|Élément artificiel nommé d'après le Tennessee.",
        "118|Og|Oganesson|Dernier élément du tableau, un gaz noble artificiel.",
    };

    private static void AddElements()
    {
        foreach (var line in Elements)
        {
            var f = line.Split('|');
            var z = int.Parse(f[0], CultureInfo.InvariantCulture);
            var category = CategoryOf(z);
            all.Add(new CardDef
            {
                Id = $"el_{z}",
                Series = CardSeries.Element,
                Name = f[2],
                Description = f[3],
                Rarity = CardRarity.Commune,
                Badge = f[1],
                SubBadge = $"{z}  ·  {CategoryLabel(category)}",
                Tint = CategoryColor(category),
            });
        }
    }

    // ---- Master stones ----------------------------------------------------------------------------------------------

    private static CardRarity ColorRarity(char letter)
    {
        switch (letter)
        {
            case 'D': return CardRarity.Gold;
            case 'E':
            case 'F': return CardRarity.UltraRare;
            case 'G':
            case 'H': return CardRarity.SuperRare;
            case 'I':
            case 'J':
            case 'K': return CardRarity.Rare;
            default: return CardRarity.Commune;
        }
    }

    private static string ColorGroup(char letter)
    {
        if (letter <= 'F') return "incolore";
        if (letter <= 'J') return "presque incolore";
        if (letter <= 'M') return "légèrement teinté";
        if (letter <= 'R') return "très légèrement teinté";
        return "légèrement teinté de jaune";
    }

    private static readonly string[] Clarities = { "FL", "IF", "VVS1", "VVS2", "VS1", "VS2", "SI1", "SI2", "I1", "I2", "I3" };

    private static CardRarity ClarityRarity(string grade)
    {
        switch (grade)
        {
            case "FL": return CardRarity.Gold;
            case "IF": return CardRarity.UltraRare;
            case "VVS1":
            case "VVS2": return CardRarity.SuperRare;
            case "VS1":
            case "VS2": return CardRarity.Rare;
            default: return CardRarity.Commune;
        }
    }

    private static string ClarityDescription(string grade)
    {
        switch (grade)
        {
            case "FL": return "Flawless : aucun défaut, interne ou externe, visible à la loupe 10×.";
            case "IF": return "Internally Flawless : aucune inclusion interne à 10×, seulement de menus défauts de surface.";
            case "VVS1": return "Inclusions minuscules, très très difficiles à voir à 10×.";
            case "VVS2": return "Inclusions minuscules, très difficiles à voir à 10×.";
            case "VS1": return "Petites inclusions, difficiles à voir à 10×.";
            case "VS2": return "Petites inclusions, assez faciles à voir à 10×.";
            case "SI1": return "Inclusions visibles à 10×, rarement à l'œil nu.";
            case "SI2": return "Inclusions faciles à voir à 10×, parfois visibles à l'œil nu.";
            case "I1": return "Inclusions visibles à l'œil nu, qui altèrent un peu l'éclat.";
            case "I2": return "Inclusions bien visibles à l'œil nu, qui altèrent l'éclat.";
            default: return "Inclusions très visibles qui affectent la transparence et la solidité.";
        }
    }

    private static void AddMasterStones()
    {
        // Colors D to Z: the colorless-to-yellow scale, drawn like the diamonds of the dealers.
        for (var letter = 'D'; letter <= 'Z'; letter++)
        {
            var index = letter - 'D';
            var t = Mathf.Clamp01(index / 22f);
            var color = new GemColorValue(Mathf.Lerp(215f, 52f, Mathf.Clamp01(index / 5f)), Mathf.Lerp(0.02f, 0.62f, t * t), Mathf.Lerp(0.96f, 0.90f, t));
            var body = GemColorSystem.BodyColor(color);
            var facet = GemColorSystem.FacetColor(color);
            var current = letter;
            all.Add(new CardDef
            {
                Id = $"ms_{letter}",
                Series = CardSeries.MasterStone,
                Name = $"Master stone {letter}",
                Description = $"Pierre étalon de couleur {letter} : {ColorGroup(current)}. L'échelle va de D (incolore) à Z.",
                Rarity = ColorRarity(letter),
                Badge = letter.ToString(),
                SubBadge = "Couleur",
                Tint = new Color(0.20f, 0.24f, 0.34f),
                Art = () => GemIconFactory.GetSolid("Rond", "Brillant", body, facet),
            });
        }

        // Clarities FL to I3, on a colorless stone.
        var clear = new GemColorValue(215f, 0.02f, 0.96f);
        var clearBody = GemColorSystem.BodyColor(clear);
        var clearFacet = GemColorSystem.FacetColor(clear);
        foreach (var grade in Clarities)
        {
            var g = grade;
            all.Add(new CardDef
            {
                Id = $"mc_{grade}",
                Series = CardSeries.MasterStone,
                Name = $"Master stone {grade}",
                Description = ClarityDescription(grade),
                Rarity = ClarityRarity(grade),
                Badge = grade,
                SubBadge = "Pureté",
                Tint = new Color(0.22f, 0.20f, 0.34f),
                Art = () => GemIconFactory.GetSolid("Rond", "Brillant", clearBody, clearFacet),
            });
        }
    }

    // ---- Gems -------------------------------------------------------------------------------------------------------

    private static CardRarity GemRarity(GemDefinition def)
    {
        if (def.baseValue >= 900) return CardRarity.Gold;
        if (def.baseValue > 150) return CardRarity.UltraRare;
        if (def.baseValue > 65) return CardRarity.SuperRare;
        if (def.baseValue > 30) return CardRarity.Rare;
        return CardRarity.Commune;
    }

    private static string Fr(float value, string format)
    {
        return value.ToString(format, CultureInfo.GetCultureInfo("fr-FR"));
    }

    private static void AddGems()
    {
        var database = GemDatabase.Instance;
        if (database == null) return;

        var gems = new List<GemDefinition>();
        foreach (var def in database.allGems)
            if (def != null && !def.forcedSynthetic) gems.Add(def);
        gems.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.CurrentCultureIgnoreCase));

        foreach (var def in gems)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(def.color)) parts.Add(def.color + ".");
            var facts = new List<string>();
            if (def.hardness > 0f) facts.Add($"dureté {Fr(def.hardness, "0.#")}");
            if (def.refractiveIndex > 0f) facts.Add($"IR {Fr(def.refractiveIndex, "0.###")}");
            if (def.density > 0f) facts.Add($"densité {Fr(def.density, "0.##")}");
            if (facts.Count > 0) parts.Add(char.ToUpper(facts[0][0]) + facts[0].Substring(1) + (facts.Count > 1 ? ", " + string.Join(", ", facts.GetRange(1, facts.Count - 1)) : "") + ".");

            var captured = def;
            all.Add(new CardDef
            {
                Id = $"gem_{def.name}",
                Series = CardSeries.Gem,
                Name = def.displayName,
                Description = string.Join(" ", parts),
                Rarity = GemRarity(def),
                Tint = new Color(0.16f, 0.22f, 0.30f),
                Art = () => captured.GetIcon(),
            });
        }
    }

    // ---- Famous stones ----------------------------------------------------------------------------------------------

    private static void Famous(string slug, string name, string description, CardRarity rarity, string shape, string style, Color body)
    {
        all.Add(new CardDef
        {
            Id = $"fam_{slug}",
            Series = CardSeries.Famous,
            Name = name,
            Description = description,
            Rarity = rarity,
            Tint = new Color(0.26f, 0.20f, 0.14f),
            Art = () => GemIconFactory.GetSolid(shape, style, body, Facet(body)),
        });
    }

    private static void AddFamousStones()
    {
        var white = new Color(0.82f, 0.90f, 1f);
        var blue = new Color(0.20f, 0.34f, 0.85f);
        var pink = new Color(1f, 0.50f, 0.72f);
        var yellow = new Color(0.98f, 0.84f, 0.25f);
        var red = new Color(0.86f, 0.12f, 0.20f);
        var green = new Color(0.20f, 0.72f, 0.36f);
        var black = new Color(0.16f, 0.16f, 0.20f);

        Famous("cullinan", "Cullinan", "Plus gros diamant brut jamais trouvé (3 106 ct, 1905, Afrique du Sud). Taillé en neuf grandes pierres, dont l'Étoile d'Afrique de 530 ct.",
            CardRarity.Gold, "Poire", "Brillant", white);
        Famous("hope", "Hope", "Diamant bleu profond de 45,52 ct venu d'Inde, célèbre pour sa légende de malédiction. Exposé au Smithsonian.",
            CardRarity.Gold, "Coussin", "Brillant", blue);
        Famous("kohinoor", "Koh-i-Noor", "Diamant de 105,6 ct venu d'Inde. Son nom signifie « montagne de lumière ». Il orne une couronne britannique.",
            CardRarity.Gold, "Ovale", "Brillant", white);
        Famous("pinkstar", "Pink Star", "Diamant rose vif de 59,6 ct, vendu 71,2 millions de dollars en 2017 : record pour une gemme aux enchères.",
            CardRarity.Gold, "Ovale", "Brillant", pink);

        Famous("blackorlov", "Black Orlov", "Diamant noir de 67,5 ct extrait en Inde, entouré d'une légende de malédiction.",
            CardRarity.UltraRare, "Coussin", "Brillant", black);
        Famous("regent", "Régent", "Diamant incolore de 140,6 ct trouvé en Inde en 1698. Il est exposé au Louvre.",
            CardRarity.UltraRare, "Coussin", "Brillant", white);
        Famous("sancy", "Sancy", "Diamant jaune pâle de 55,2 ct en forme de poire, passé entre les mains de plusieurs rois.",
            CardRarity.UltraRare, "Poire", "Brillant", new Color(0.96f, 0.92f, 0.62f));
        Famous("dresden", "Dresden Green", "Le plus gros diamant vert naturel connu : 41 ct. Il est conservé à Dresde.",
            CardRarity.UltraRare, "Poire", "Brillant", new Color(0.42f, 0.82f, 0.60f));
        Famous("tiffany", "Tiffany Yellow", "Diamant jaune canari de 128,5 ct trouvé en Afrique du Sud en 1877 et acquis par Tiffany.",
            CardRarity.UltraRare, "Coussin", "Brillant", yellow);
        Famous("idols", "Idol's Eye", "Diamant bleu pâle de 70,2 ct en forme de poire, d'origine indienne, entouré de légendes.",
            CardRarity.UltraRare, "Poire", "Brillant", new Color(0.50f, 0.68f, 0.98f));
        Famous("eternity", "Heart of Eternity", "Diamant bleu vif de 27,6 ct taillé en cœur, joyau de la collection Millennium de De Beers.",
            CardRarity.UltraRare, "Cœur", "Brillant", blue);
        Famous("oppenheimer", "Oppenheimer Blue", "Diamant bleu de 14,6 ct vendu 57,5 millions de dollars en 2016 : record pour un diamant bleu.",
            CardRarity.UltraRare, "Émeraude", "À degrés", blue);
        Famous("wittelsbach", "Wittelsbach-Graff", "Diamant bleu intense de 31 ct, ancien joyau de couronne, retaillé en 2008.",
            CardRarity.UltraRare, "Coussin", "Brillant", blue);
        Famous("moussaieff", "Moussaieff Red", "Diamant rouge de 5,1 ct taillé en triangle, le plus gros diamant rouge connu.",
            CardRarity.UltraRare, "Triangle", "Brillant", red);
        Famous("sunrise", "Sunrise Ruby", "Rubis de Birmanie de 25,6 ct vendu 30 millions de dollars en 2015 : record pour un rubis.",
            CardRarity.UltraRare, "Coussin", "Mixte", red);
        Famous("starofadam", "Star of Adam", "Saphir étoilé bleu de 1 404 ct, découvert au Sri Lanka.",
            CardRarity.UltraRare, "Ovale", "Cabochon", new Color(0.24f, 0.40f, 0.86f));
        Famous("orlov", "Orlov", "Diamant de 189,6 ct monté sur le sceptre impérial de Russie, d'origine indienne.",
            CardRarity.UltraRare, "Ovale", "Mixte", white);

        Famous("starofindia", "Star of India", "Saphir étoilé gris-bleu de 563 ct, l'un des plus gros du monde, conservé à New York.",
            CardRarity.SuperRare, "Ovale", "Cabochon", new Color(0.52f, 0.60f, 0.80f));
        Famous("logan", "Logan Sapphire", "Saphir bleu de 423 ct, l'un des plus gros saphirs taillés connus.",
            CardRarity.SuperRare, "Coussin", "Mixte", new Color(0.26f, 0.38f, 0.88f));
        Famous("carmen", "Carmen Lucía", "Rubis de Birmanie de 23,1 ct exposé au Smithsonian.",
            CardRarity.SuperRare, "Coussin", "Mixte", red);
        Famous("patricia", "Patricia", "Cristal d'émeraude colombien de 632 ct aux faces hexagonales presque parfaites.",
            CardRarity.SuperRare, "Émeraude", "À degrés", green);
        Famous("mughal", "Mogul Mughal Emerald", "Émeraude de 217,8 ct gravée de prières en arabe, vendue en 2001.",
            CardRarity.SuperRare, "Rectangle", "À degrés", green);
        Famous("taylorburton", "Taylor-Burton", "Diamant en poire de 68 ct offert à Elizabeth Taylor par Richard Burton.",
            CardRarity.SuperRare, "Poire", "Brillant", white);
        Famous("millennium", "Millennium Star", "Diamant incolore de 203 ct, taillé en poire et sans défaut.",
            CardRarity.SuperRare, "Poire", "Brillant", white);
        Famous("lesedi", "Lesedi La Rona", "Brut de 1 109 ct trouvé au Botswana en 2015, le plus gros diamant gemme depuis le Cullinan.",
            CardRarity.SuperRare, "Octogone", "Mixte", new Color(0.78f, 0.84f, 0.92f));
        Famous("centenary", "Centenary", "Diamant de 273,9 ct taillé en cœur par De Beers dans un brut de 599 ct.",
            CardRarity.SuperRare, "Cœur", "Brillant", white);
    }

    // ---- Crystal systems --------------------------------------------------------------------------------------------

    private static void Crystal(string slug, string name, string description, CardRarity rarity, string drawing, Color color)
    {
        all.Add(new CardDef
        {
            Id = $"cr_{slug}",
            Series = CardSeries.Crystal,
            Name = name,
            Description = description,
            Rarity = rarity,
            Tint = new Color(0.14f, 0.22f, 0.28f),
            Art = () => CardArt.Get(drawing, color),
        });
    }

    private static void AddCrystalSystems()
    {
        Crystal("cubic", "Système cubique", "Trois axes égaux à 90°. Isotrope : une seule réfraction. Diamant, grenat, spinelle, fluorite.",
            CardRarity.Commune, "cubic", new Color(0.45f, 0.72f, 0.95f));
        Crystal("hexagonal", "Système hexagonal", "Trois axes égaux à 120° et un axe vertical. Uniaxe. Béryl (émeraude, aigue-marine), apatite.",
            CardRarity.Commune, "hexagonal", new Color(0.30f, 0.78f, 0.52f));
        Crystal("trigonal", "Système trigonal", "Symétrie d'ordre 3 autour d'un axe. Uniaxe. Quartz, corindon (rubis, saphir), tourmaline, calcite.",
            CardRarity.Commune, "trigonal", new Color(0.88f, 0.42f, 0.55f));
        Crystal("amorphous", "Amorphe", "Pas de réseau cristallin : les atomes sont désordonnés. Isotrope. Opale, verre, obsidienne, ambre.",
            CardRarity.Commune, "amorphous", new Color(0.70f, 0.78f, 0.90f));
        Crystal("tetragonal", "Système quadratique", "Deux axes égaux et un axe différent, tous à 90°. Uniaxe. Zircon, rutile, vésuvianite.",
            CardRarity.Rare, "tetragonal", new Color(0.95f, 0.72f, 0.30f));
        Crystal("orthorhombic", "Système orthorhombique", "Trois axes inégaux, tous à 90°. Biaxe. Topaze, péridot, chrysobéryl, tanzanite, iolite.",
            CardRarity.Rare, "orthorhombic", new Color(0.62f, 0.50f, 0.92f));
        Crystal("monoclinic", "Système monoclinique", "Trois axes inégaux, dont un incliné. Biaxe. Jadéite, orthose, kunzite (spodumène), malachite.",
            CardRarity.Rare, "monoclinic", new Color(0.35f, 0.80f, 0.78f));
        Crystal("triclinic", "Système triclinique", "Trois axes inégaux, aucun angle droit : la symétrie la plus basse. Biaxe. Turquoise, amazonite, rhodonite, labradorite.",
            CardRarity.SuperRare, "triclinic", new Color(0.92f, 0.55f, 0.30f));
    }

    // ---- Syntheses and simulants -------------------------------------------------------------------------------------------

    private static void Synthetic(string slug, string name, string description, CardRarity rarity, Func<Sprite> art)
    {
        all.Add(new CardDef
        {
            Id = $"syn_{slug}",
            Series = CardSeries.Synthesis,
            Name = name,
            Description = description,
            Rarity = rarity,
            Tint = new Color(0.24f, 0.17f, 0.28f),
            Art = art,
        });
    }

    private static Func<Sprite> Brilliant(Color body) => () => GemIconFactory.GetSolid("Rond", "Brillant", body, Facet(body));
    private static Func<Sprite> Cabochon(Color body) => () => GemIconFactory.GetSolid("Ovale", "Cabochon", body, Facet(body));
    private static Func<Sprite> Drawing(string key, Color body) => () => CardArt.Get(key, body);

    private static void AddSyntheses()
    {
        Synthetic("glass", "Verre", "Le plus ancien des simulants : un verre coloré aux bulles rondes et aux traces d'écoulement. Isotrope.",
            CardRarity.Commune, Cabochon(new Color(0.30f, 0.72f, 0.52f)));
        Synthetic("reconstituted", "Pierre reconstituée", "Poudre de pierre (turquoise, lapis) agglomérée avec de la résine, puis taillée : l'ensemble paraît pleine.",
            CardRarity.Commune, Cabochon(new Color(0.25f, 0.72f, 0.72f)));
        Synthetic("cz", "Zircone cubique (CZ)", "ZrO₂ stabilisé à l'yttrium, fondu en creuset froid (skull melting). Très dispersive et dense (5,7) : le simulant du diamant le plus courant.",
            CardRarity.Commune, Brilliant(new Color(0.92f, 0.95f, 1f)));
        Synthetic("doublet", "Doublet et triplet", "Plusieurs pièces collées : une table de vraie pierre sur un pavillon de verre, parfois un dôme de quartz au-dessus. Le collage se voit de côté.",
            CardRarity.Rare, Drawing("layers", new Color(0.30f, 0.62f, 0.45f)));
        Synthetic("verneuil", "Verneuil (fusion à la flamme)", "Inventée en 1902 : une poudre tombe dans une flamme d'oxhydrogène et se solidifie en boule. Rubis, saphir, spinelle. Bulles rondes et stries courbes.",
            CardRarity.Rare, Drawing("verneuil", new Color(0.85f, 0.12f, 0.22f)));
        Synthetic("moissanite", "Moissanite synthétique", "Carbure de silicium (SiC), plus dispersive que le diamant. Fortement biréfringente, elle conduit l'électricité : le testeur électrique la démasque.",
            CardRarity.Rare, Drawing("moissanite", new Color(0.62f, 0.78f, 0.66f)));
        Synthetic("yag", "YAG", "Grenat d'yttrium et d'aluminium (Y₃Al₅O₁₂), cubique, IR 1,83. Simulant du diamant des années 1970, aussi vendu en vert et en jaune.",
            CardRarity.Rare, Brilliant(new Color(0.55f, 0.85f, 0.40f)));
        Synthetic("ggg", "GGG", "Grenat de gadolinium et gallium, cubique, très dense (7,05) et assez tendre. Simulant du diamant abandonné après la zircone.",
            CardRarity.Rare, Brilliant(new Color(0.88f, 0.84f, 0.68f)));
        Synthetic("fabulite", "Fabulite", "Titanate de strontium (SrTiO₃), cubique, à fort feu. Simulant du diamant tendre et rayable, fabriqué par la méthode Verneuil.",
            CardRarity.Rare, Brilliant(new Color(0.92f, 0.94f, 0.98f)));
        Synthetic("rutile", "Rutile synthétique", "Oxyde de titane fabriqué par fusion à la flamme, à la dispersion exceptionnelle (0,33), jaunâtre. Biréfringence très forte, rayable.",
            CardRarity.Rare, Brilliant(new Color(0.92f, 0.82f, 0.45f)));
        Synthetic("czochralski", "Czochralski (tirage)", "Un germe tourne et remonte lentement hors d'un creuset de matière fondue : on tire un cylindre. Rubis, saphir, YAG, GGG, alexandrite.",
            CardRarity.SuperRare, Drawing("czochralski", new Color(0.85f, 0.70f, 0.30f)));
        Synthetic("hydrothermal", "Hydrothermale", "Cristallisation dans de l'eau surchauffée sous pression, dans un autoclave. Quartz (améthyste, citrine), émeraude, rubis. Voiles et « clous ».",
            CardRarity.SuperRare, Drawing("hydrothermal", new Color(0.62f, 0.38f, 0.80f)));
        Synthetic("gilson", "Opale synthétique Gilson", "Sphères de silice rangées en colonnes : le jeu de couleurs forme une mosaïque en « peau de lézard », avec des bords nets entre les plages.",
            CardRarity.SuperRare, Cabochon(new Color(0.72f, 0.82f, 0.92f)));
        Synthetic("flux", "Flux (fondant)", "La matière est dissoute dans un fondant en fusion, puis cristallise très lentement. Émeraude, rubis, saphir. Voiles de fondant et grains de platine.",
            CardRarity.UltraRare, Drawing("flux", new Color(0.18f, 0.72f, 0.45f)));
        Synthetic("hpht", "HPHT (haute pression)", "Le carbone cristallise sur un germe vers 5 GPa et 1 400 °C, dans une presse. Diamants jaunes ou bleus ; fluorescence en croix sous le DiamondView, inclusions métalliques.",
            CardRarity.UltraRare, Drawing("hpht", new Color(0.96f, 0.88f, 0.45f)));
        Synthetic("cvd", "CVD (dépôt en phase vapeur)", "Un plasma de méthane et d'hydrogène dépose le carbone couche par couche sur un germe. Diamants de type IIa, stries parallèles sous le DiamondView.",
            CardRarity.UltraRare, Drawing("cvd", new Color(0.85f, 0.95f, 0.92f)));
    }

    // ---- Tools of the gemologist ------------------------------------------------------------------------------------------

    private static void Tool(string slug, string name, string description, CardRarity rarity, string icon, string badge = null)
    {
        all.Add(new CardDef
        {
            Id = $"tool_{slug}",
            Series = CardSeries.Tool,
            Name = name,
            Description = description,
            Rarity = rarity,
            Tint = new Color(0.20f, 0.20f, 0.16f),
            Badge = icon == null ? badge : null,
            SubBadge = icon == null ? "Outil de laboratoire" : null,
            Art = icon == null ? (Func<Sprite>)null : () => Resources.Load<Sprite>(icon),
        });
    }

    private static void AddTools()
    {
        // The simple tools are common; the instruments of the laboratory are ultra rare to gold.
        Tool("cloth", "Chamoisine", "Un carré de feutre doux pour essuyer une gemme sans la rayer avant de l'observer.", CardRarity.Commune, "BagIcons/Chamoisine");
        Tool("loupe10", "Loupe 10×", "L'outil de base du gemmologue : un triplet qui grossit dix fois pour voir les inclusions et les défauts.", CardRarity.Commune, "BagIcons/Loupe10X");
        Tool("maglite", "Lampe torche", "Une lampe compacte pour éclairer une gemme : transparence, éclat, inclusions.", CardRarity.Commune, "BagIcons/Maglite");
        Tool("magnet", "Aimant de gemmologie", "Un aimant au néodyme : il attire les gemmes riches en fer, comme les grenats, pour un premier tri.", CardRarity.Commune, "ToolIcons/Aimant");
        Tool("scale", "Balance à carat", "Une balance de précision qui donne le poids d'une gemme en carats (1 ct = 0,2 g).", CardRarity.Commune, "ToolIcons/BalanceCarat");
        Tool("gauge", "Micromètre", "Un pied à coulisse électronique qui mesure longueur, largeur et profondeur d'une pierre au centième de millimètre.", CardRarity.Commune, "ToolIcons/Micrometre");

        Tool("loupe20", "Loupe 20×", "Un triplet qui grossit vingt fois : on voit les inclusions les plus fines, au prix d'un champ plus réduit.", CardRarity.Rare, "ToolIcons/Loupe");
        Tool("dichroscope", "Dichroscope", "Deux fenêtres côte à côte : on compare les deux couleurs d'un pléochroïsme en tournant la gemme.", CardRarity.Rare, "ToolIcons/Dichroscope");
        Tool("spectroscope", "Spectroscope", "Il décompose la lumière qui traverse la gemme et montre ses bandes d'absorption.", CardRarity.Rare, "ToolIcons/Spectroscope");
        Tool("chelsea", "Filtre Chelsea", "Un filtre qui ne laisse passer que le rouge profond et le vert-jaune : il révèle le chrome des émeraudes et des imitations.", CardRarity.Rare, "ToolIcons/FiltreChelsea");
        Tool("uvlamp", "Lampe UV longs", "Une lampe à 365 nm qui révèle la fluorescence : rouge du rubis, bleu du diamant.", CardRarity.Rare, "BagIcons/LampeUVLongs");
        Tool("thermal", "Testeur thermique", "Une sonde qui mesure la conduction de la chaleur : le diamant (et la moissanite) l'évacue mieux que ses imitations.", CardRarity.Rare, "ToolIcons/TesteurThermique");

        Tool("electric", "Testeur électrique", "Une sonde qui départage le diamant isolant de la moissanite, qui conduit l'électricité.", CardRarity.SuperRare, "ToolIcons/TesteurElectrique");
        Tool("polariscope", "Polariscope", "Deux filtres polarisants croisés : il montre si une gemme est simple ou double réfringente, et sa figure d'interférence.", CardRarity.SuperRare, "ToolIcons/Polariscope");
        Tool("refractometer", "Réfractomètre", "Un hémicylindre de verre dense : il donne l'indice de réfraction de la table d'une gemme et sa biréfringence.", CardRarity.SuperRare, "ToolIcons/Refractometre");
        Tool("hydrostatic", "Balance hydrostatique", "Elle pèse la gemme dans l'air puis dans l'eau pour calculer sa densité relative.", CardRarity.SuperRare, "ToolIcons/BalanceHydrostatique");
        Tool("microscope", "Microscope gemmologique", "Un microscope binoculaire à champ noir : il révèle les inclusions qui prouvent l'origine d'une pierre.", CardRarity.SuperRare, "ToolIcons/Microscope");
        Tool("uvcabin", "Cabine UV", "Une chambre noire à lampes longues et courtes : on y observe la fluorescence sans lumière parasite.", CardRarity.SuperRare, "ToolIcons/CabineUV");

        Tool("uvvisnir", "Spectrophotomètre UV-Vis-NIR", "Il mesure l'absorption d'une gemme de l'ultraviolet à l'infrarouge proche : un outil pour confirmer l'origine et la couleur.", CardRarity.UltraRare, "ToolIcons/UvVisNir");
        Tool("ftir", "Spectromètre FTIR", "Un spectromètre infrarouge à transformée de Fourier : il détecte les remplissages, les traitements et le type d'un diamant.", CardRarity.UltraRare, "ToolIcons/FTIR");
        Tool("diamondsure", "DiamondSure", "Un appareil de criblage par transmission UV : il écarte les diamants de type I et renvoie les autres au laboratoire.", CardRarity.UltraRare, "ToolIcons/DiamondSure");
        Tool("diamondview", "DiamondView", "Une caméra à fluorescence UV profonde : elle montre les stries de croissance qui trahissent un diamant CVD ou HPHT.", CardRarity.UltraRare, "ToolIcons/DiamondView");

        Tool("raman", "Spectromètre Raman", "Un laser qui fait vibrer les liaisons : il identifie une espèce minérale, une inclusion, un remplissage, sans toucher la pierre.", CardRarity.Gold, "ToolIcons/Raman");
        Tool("sem", "Microscope électronique à balayage", "Un faisceau d'électrons balaie la surface : images à très fort grossissement, cathodoluminescence et analyse chimique.", CardRarity.Gold, "ToolIcons/MEB");
        Tool("edxrf", "EDXRF", "La fluorescence X dispersive en énergie : elle donne les éléments chimiques majeurs et les traces d'une gemme.", CardRarity.Gold, "ToolIcons/EDXRF");
        Tool("laicpms", "LA-ICP-MS", "Un laser prélève une poussière de pierre, un plasma l'ionise et un spectromètre de masse compte les atomes : l'empreinte des traces d'une origine.", CardRarity.Gold, null, "LA-ICP-MS");
        Tool("photoluminescence", "Photoluminescence", "Un laser excite la pierre à froid et on enregistre la lumière qu'elle réémet : les défauts du réseau trahissent traitement et synthèse.", CardRarity.Gold, null, "PL");
    }
}
