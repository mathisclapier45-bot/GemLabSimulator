using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Which inclusions a species carries, how often, and how clean its stones usually are, according to the species and the
// deposit it comes from (after the Photo Atlas of Inclusions in Gemstones): emerald is almost always included, with
// three-phase cavities in Colombia and mica in Zambia; aquamarine is nearly always clean; Kashmir sapphire is velvety...
public class InclusionProfile
{
    // Weights of the clarity grades, in the order of GemGrading.ClarityGrades:
    // fortement inclus, inclus, eye-clean, loupe clean.
    public readonly float[] Clarity = { 25f, 30f, 30f, 15f };

    public readonly List<(FeatureKind kind, int variant, float weight)> Entries = new List<(FeatureKind, int, float)>();
    public float Total;

    public void Add(FeatureKind kind, int variant, float weight)
    {
        Entries.Add((kind, variant, weight));
        Total += weight;
    }

    public (FeatureKind kind, int variant) Pick(System.Random rng)
    {
        var roll = (float)rng.NextDouble() * Total;
        var cumulative = 0f;
        foreach (var entry in Entries)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return (entry.kind, entry.variant);
        }
        var last = Entries[Entries.Count - 1];
        return (last.kind, last.variant);
    }

    public float Share(FeatureKind kind, int variant)
    {
        if (Total <= 0f) return 0f;
        var sum = 0f;
        foreach (var entry in Entries)
            if (entry.kind == kind && entry.variant == variant) sum += entry.weight;
        return sum / Total;
    }

    public float Share(FeatureKind kind)
    {
        if (Total <= 0f) return 0f;
        var sum = 0f;
        foreach (var entry in Entries)
            if (entry.kind == kind) sum += entry.weight;
        return sum / Total;
    }
}

public static class InclusionCatalog
{
    private static readonly Dictionary<string, (FeatureKind kind, int variant)> Tokens = new Dictionary<string, (FeatureKind, int)>
    {
        { "crystal", (FeatureKind.Crystal, (int)CrystalVariant.Generic) },
        { "apatite", (FeatureKind.Crystal, (int)CrystalVariant.Apatite) },
        { "zircon", (FeatureKind.Crystal, (int)CrystalVariant.Zircon) },
        { "rutile", (FeatureKind.Crystal, (int)CrystalVariant.Rutile) },
        { "sulfide", (FeatureKind.Crystal, (int)CrystalVariant.Sulfide) },
        { "mica", (FeatureKind.Crystal, (int)CrystalVariant.Mica) },
        { "octa", (FeatureKind.Crystal, (int)CrystalVariant.Octahedron) },
        { "calcite", (FeatureKind.Crystal, (int)CrystalVariant.Calcite) },
        { "garnet", (FeatureKind.Crystal, (int)CrystalVariant.Garnet) },
        { "chromite", (FeatureKind.Crystal, (int)CrystalVariant.Chromite) },
        { "bubble", (FeatureKind.Bubble, 0) },
        { "comet", (FeatureKind.Bubble, 1) },
        { "bubbles", (FeatureKind.Bubble, 2) },
        { "liquid", (FeatureKind.Fluid, 0) },
        { "liquidj", (FeatureKind.Fluid, 1) },
        { "two", (FeatureKind.TwoPhase, 0) },
        { "twoj", (FeatureKind.TwoPhase, 1) },
        { "three", (FeatureKind.ThreePhase, 1) },
        { "threer", (FeatureKind.ThreePhase, 0) },
        { "negative", (FeatureKind.NegativeCrystal, 0) },
        { "negoct", (FeatureKind.NegativeCrystal, 1) },
        { "silk", (FeatureKind.Needle, 0) },
        { "fibers", (FeatureKind.Needle, 1) },
        { "tubes", (FeatureKind.Needle, 2) },
        { "tremolite", (FeatureKind.Needle, 3) },
        { "horsetail", (FeatureKind.HorseTail, 0) },
        { "fracture", (FeatureKind.Fracture, 0) },
        { "cleavage", (FeatureKind.Cleavage, 0) },
        { "cleavage2", (FeatureKind.Cleavage, 1) },
        { "feather", (FeatureKind.Feather, 0) },
        { "finger", (FeatureKind.Healing, 0) },
        { "veil", (FeatureKind.Healing, 1) },
        { "flux", (FeatureKind.Healing, 2) },
        { "zoning", (FeatureKind.Zoning, 0) },
        { "hexzone", (FeatureKind.Zoning, 1) },
        { "sector", (FeatureKind.Zoning, 2) },
        { "growth", (FeatureKind.Growth, 0) },
        { "striae", (FeatureKind.Striae, 0) },
        { "twin", (FeatureKind.Twinning, 0) },
        { "cloud", (FeatureKind.Cloud, 0) },
    };

    // Species by (part of) their name. The first match wins, so the specific names come first.
    // Clarity: fortement inclus, inclus, eye-clean, loupe clean. Features: token=weight.
    private static readonly (string[] keys, string clarity, string features)[] Species =
    {
        (new[] { "émeraude" }, "45,40,13,2",
            "three=22,twoj=14,liquidj=8,feather=22,finger=6,mica=8,tremolite=5,fibers=4,fracture=8,negative=2,sulfide=3,growth=4,zoning=4"),
        (new[] { "béryl rouge" }, "35,42,20,3", "finger=18,feather=20,two=14,liquid=12,fracture=14,growth=8"),
        (new[] { "morganite" }, "10,35,40,15", "tubes=22,two=18,liquid=16,finger=12,feather=12,negative=8,growth=6"),
        (new[] { "héliodore", "goshénite" }, "6,22,44,28", "tubes=24,two=18,liquid=14,finger=10,feather=10,negative=8,growth=6,mica=4"),
        (new[] { "paraïba" }, "20,42,32,6", "tubes=20,feather=16,two=12,liquid=10,finger=8,mica=6,cloud=10,fracture=10,growth=6"),
        (new[] { "tourmaline" }, "35,42,20,3", "tubes=30,feather=16,finger=14,two=16,liquid=10,fracture=10,cloud=4,growth=4"),
        (new[] { "padparadscha" }, "8,32,42,18", "silk=22,finger=12,cloud=8,negative=8,zircon=6,twin=6,feather=8,hexzone=6"),
        (new[] { "saphir bleu" }, "12,38,38,12", "silk=26,hexzone=10,cloud=10,finger=10,negative=6,zircon=8,twin=8,feather=10,rutile=6,growth=6"),
        (new[] { "saphir" }, "10,36,40,14", "silk=22,hexzone=10,finger=10,negative=6,zircon=6,twin=6,feather=8,growth=8,cloud=6"),
        (new[] { "rubis" }, "15,40,35,10", "silk=28,rutile=6,finger=14,twin=10,feather=10,cleavage=3,hexzone=6,cloud=6,negative=4,calcite=4,zircon=6,octa=3"),
        (new[] { "spinelle" }, "8,30,42,20", "octa=26,negoct=18,finger=14,crystal=8,twin=12,cloud=4,zoning=6,silk=3"),
        (new[] { "démantoïde" }, "18,40,32,10", "horsetail=45,crystal=10,fracture=10,fibers=8,finger=6,liquid=6"),
        (new[] { "hessonite" }, "40,45,13,2", "garnet=30,apatite=16,liquid=12,cloud=16,zoning=10,finger=4"),
        (new[] { "hydrogrossulaire" }, "30,45,22,3", "cloud=30,chromite=22,zoning=10,fracture=8"),
        (new[] { "tsavorite" }, "10,35,40,15", "two=14,three=6,liquid=10,tubes=8,growth=6,finger=8,negative=8,sulfide=10"),
        (new[] { "spessartite" }, "12,38,36,14", "feather=14,finger=10,veil=10,two=14,liquid=14,crystal=6,fibers=4"),
        (new[] { "almandin", "rhodolite", "malaya" }, "20,45,30,5", "silk=28,apatite=10,zircon=10,finger=6,negative=5,crystal=6,cloud=6"),
        (new[] { "pyrope" }, "10,36,40,14", "garnet=14,zircon=8,silk=10,finger=8,crystal=10,negoct=4"),
        (new[] { "grenat" }, "15,42,34,9", "silk=14,crystal=12,apatite=8,finger=8,liquid=8,negative=4"),
        (new[] { "péridot" }, "18,45,30,7", "chromite=40,mica=8,feather=10,liquid=10,finger=6,negative=4,sulfide=4"),
        (new[] { "diamant synthétique (hpht)" }, "4,22,44,30", "sulfide=30,growth=25,cloud=10,crystal=6"),
        (new[] { "diamant synthétique" }, "4,22,44,30", "growth=40,crystal=15,cloud=8,sulfide=4"),
        (new[] { "diamant" }, "8,34,38,20", "crystal=16,octa=10,feather=22,cloud=14,growth=10,negoct=8,fracture=8,cleavage=8"),
        (new[] { "moissanite" }, "3,12,45,40", "tubes=45,growth=20,crystal=6"),
        (new[] { "cz" }, "3,8,30,59", "bubble=34,bubbles=8,crystal=18,cloud=8"),
        (new[] { "yag", "ggg" }, "3,8,34,55", "bubble=30,comet=12,flux=8,growth=10"),
        (new[] { "verre" }, "12,40,34,14", "bubble=38,comet=22,striae=28,bubbles=10"),
        (new[] { "améthyste" }, "8,30,42,20", "sector=30,zoning=14,twin=12,two=14,liquid=10,feather=10,finger=6,negative=4"),
        (new[] { "amétrine" }, "8,30,42,20", "sector=34,twin=14,two=14,liquid=10,feather=8,growth=6"),
        (new[] { "quartz rose" }, "40,45,13,2", "cloud=36,silk=24,fibers=14,fracture=10,two=6"),
        (new[] { "quartz", "citrine", "prasiolite" }, "5,22,45,28", "two=18,liquid=14,finger=10,crystal=8,rutile=6,fibers=6,growth=8,negative=14,feather=10,tubes=4"),
        (new[] { "topaze" }, "4,18,44,34", "two=28,liquid=18,feather=14,cleavage=12,finger=10,tubes=8,negative=6,three=4"),
        (new[] { "kunzite" }, "10,35,42,13", "tubes=24,two=16,liquid=14,finger=8,cleavage=18,feather=10"),
        (new[] { "iolite" }, "15,40,35,10", "sulfide=24,fibers=6,finger=8,feather=12,cleavage=6,liquid=8,zoning=6"),
        (new[] { "tanzanite", "zoïsite" }, "8,30,42,20", "liquid=18,two=18,feather=14,fibers=10,tubes=10,cleavage=10,sulfide=8,growth=10,zoning=12"),
        (new[] { "apatite" }, "18,42,32,8", "tubes=24,two=16,liquid=12,finger=10,crystal=8,cloud=10,cleavage=4"),
        (new[] { "andalousite" }, "22,45,28,5", "rutile=10,crystal=14,liquid=14,fracture=12,feather=10,cloud=10"),
        (new[] { "alexandrite" }, "14,40,36,10", "finger=16,feather=18,negative=12,silk=14,two=12,tubes=8,growth=6"),
        (new[] { "chrysobéryl" }, "10,38,38,14", "finger=22,feather=18,negative=12,silk=14,two=12,growth=6"),
        (new[] { "zircon" }, "15,40,35,10", "zoning=20,growth=14,feather=10,fracture=14,finger=8,crystal=6,cloud=6"),
        (new[] { "fluorite" }, "12,40,36,12", "two=20,three=14,negative=22,zoning=24,cleavage=22,fracture=14,feather=8"),
        (new[] { "pezzottaite" }, "12,42,34,12", "tubes=20,two=14,liquid=10,feather=14,finger=8,growth=6"),
        (new[] { "painite" }, "25,45,24,6", "zoning=14,crystal=14,fracture=12,finger=8,growth=6"),
        (new[] { "diopside" }, "15,40,35,10", "crystal=12,finger=14,two=14,cleavage=10,tubes=10,feather=8"),
        (new[] { "sinhalite" }, "12,40,36,12", "two=20,finger=14,feather=12,liquid=14,crystal=8"),
        (new[] { "scapolite" }, "10,38,38,14", "tubes=30,two=14,liquid=14,cleavage=10,fibers=6"),
        (new[] { "kyanite" }, "20,45,30,5", "cleavage=40,fracture=10,feather=14,tubes=12,growth=10,zoning=10"),
        (new[] { "jadéite" }, "18,42,32,8", "cloud=36,fibers=22,zoning=10,chromite=14"),
        (new[] { "néphrite" }, "18,42,32,8", "fibers=50,chromite=12,cloud=20,zoning=6"),
        (new[] { "pierre de lune" }, "20,45,30,5", "twin=30,cleavage2=24,feather=14,cloud=12"),
        (new[] { "orthose" }, "15,40,35,10", "cleavage2=22,twin=10,two=10,feather=14"),
        (new[] { "labradorite" }, "20,45,30,5", "twin=34,zoning=12,cloud=20,sulfide=10"),
        (new[] { "opale de feu" }, "15,40,35,10", "veil=14,fracture=22,liquid=12,cloud=16"),
        (new[] { "opale" }, "20,45,30,5", "veil=24,fracture=30,cloud=30"),
        (new[] { "obsidienne" }, "10,40,40,10", "bubbles=20,striae=28,cloud=20,crystal=10,comet=10"),
        (new[] { "rhodocrosite" }, "25,45,24,6", "zoning=34,cleavage2=26,fracture=10,two=10,finger=6"),
        (new[] { "rhodonite" }, "30,45,22,3", "fracture=20,zoning=10,cloud=30,chromite=10"),
        (new[] { "serpentine" }, "25,45,26,4", "fibers=30,cloud=30,chromite=8,zoning=10"),
        (new[] { "ivoire", "défense", "corne" }, "15,40,35,10", "twin=30,growth=30,cloud=10,fracture=8"),
        (new[] { "écaille" }, "15,40,35,10", "cloud=40,zoning=20,growth=10"),
    };

    // Deposits that change the picture. Clarity "" keeps the species' own; the features replace the species' ones.
    private static readonly (string species, string origin, string clarity, string features)[] OriginRules =
    {
        ("émeraude", "colombie", "40,42,16,2", "three=46,twoj=16,liquidj=8,feather=14,calcite=8,sulfide=6,finger=4,fracture=6,growth=3"),
        ("émeraude", "zambie", "30,45,22,3", "mica=30,tremolite=15,fibers=10,twoj=6,three=4,feather=15,finger=8,fracture=6,negative=2"),
        ("émeraude", "brésil", "35,45,17,3", "mica=15,tubes=10,feather=18,three=6,twoj=10,fracture=10,fibers=6,growth=6"),
        ("émeraude", "afghanistan", "40,42,15,3", "three=20,twoj=15,tubes=8,feather=20,finger=8,mica=6,fracture=8"),
        ("rubis", "birmanie", "8,32,42,18", "silk=35,calcite=12,octa=10,apatite=6,cloud=8,finger=6,rutile=6,negative=4"),
        ("rubis", "mozambique", "20,42,30,8", "silk=18,twin=10,finger=12,negative=8,rutile=8,feather=12,veil=6,zircon=4"),
        ("rubis", "thaïlande", "28,42,24,6", "feather=26,finger=22,twin=14,sulfide=6,zircon=8,bubble=6,flux=4,cleavage=5,silk=2"),
        ("rubis", "sri lanka", "14,38,36,12", "silk=30,zircon=14,finger=10,negative=8,rutile=8,twin=6,apatite=5,cloud=4"),
        ("saphir bleu", "cachemire", "8,25,40,27", "cloud=40,hexzone=12,zoning=10,silk=6,zircon=4,growth=6"),
        ("saphir bleu", "ceylan", "10,35,40,15", "silk=26,zircon=12,finger=10,negative=10,rutile=6,twin=6,hexzone=8,cloud=4"),
        ("saphir bleu", "madagascar", "18,40,34,8", "feather=14,finger=14,silk=14,negative=8,twin=10,hexzone=8,zircon=4"),
        ("saphir bleu", "australie", "15,45,32,8", "hexzone=22,zoning=14,growth=18,feather=10,finger=6,crystal=6,twin=8"),
        ("spinelle", "birmanie", "5,25,45,25", "octa=28,negoct=16,finger=10,twin=12,crystal=8,apatite=6"),
        ("spinelle", "tanzanie", "10,32,40,18", "octa=20,negoct=14,finger=14,twin=12,cloud=8,zoning=6,silk=6"),
        ("péridot", "pakistan", "10,38,40,12", "mica=22,chromite=24,feather=6,liquid=6,negative=4"),
        ("péridot", "arizona", "6,32,44,18", "chromite=18,negative=10,liquid=10,cloud=8"),
        ("péridot", "myanmar", "15,42,35,8", "chromite=30,feather=14,finger=8,liquid=8,mica=4"),
        ("démantoïde", "russie", "18,42,32,8", "horsetail=72,crystal=6,fracture=6"),
        ("démantoïde", "namibie", "22,42,28,8", "horsetail=18,crystal=14,feather=14,liquid=12,finger=10,fracture=6"),
        ("démantoïde", "madagascar", "22,42,28,8", "horsetail=15,crystal=14,fracture=10,fibers=8,finger=6"),
        ("améthyste", "brésil", "8,30,42,20", "sector=30,twin=14,two=12,liquid=10,feather=12,zoning=8"),
        ("améthyste", "uruguay", "10,32,40,18", "sector=22,sulfide=8,crystal=8,two=14,feather=10"),
        ("améthyste", "zambie", "12,35,38,15", "sector=22,veil=14,finger=8,two=10,zoning=10"),
        ("topaze", "brésil", "3,15,44,38", "two=30,liquid=16,feather=14,cleavage=14,tubes=8"),
        ("topaze", "russie", "5,20,46,29", "two=26,liquid=22,feather=10,cleavage=12,finger=8"),
        ("tourmaline", "paraíba", "20,42,32,6", "tubes=20,feather=16,mica=6,cloud=10,fracture=10"),
        ("diamant", "russie", "8,32,40,20", "crystal=18,octa=14,feather=20,cloud=14,growth=10,negoct=8"),
        ("diamant", "botswana", "8,32,40,20", "crystal=22,octa=10,feather=18,cloud=18,growth=8,negoct=6"),
        ("opale", "éthiopie", "25,45,26,4", "veil=30,fracture=40,cloud=14"),
        ("zircon", "cambodge", "10,38,38,14", "zoning=20,growth=16,feather=8,fracture=10,finger=8,crystal=6"),
    };

    private static readonly Dictionary<string, InclusionProfile> Cache = new Dictionary<string, InclusionProfile>();

    public static InclusionProfile ProfileFor(GemInstance gem)
    {
        return For(gem.Definition, gem.Origin, gem.IsSynthetic);
    }

    public static InclusionProfile For(GemDefinition def, GemOrigin origin, bool synthetic)
    {
        var name = (def.displayName ?? def.name ?? "").ToLowerInvariant();
        var originName = (origin != null && origin.name != null ? origin.name : "").ToLowerInvariant();
        var key = $"{name}|{originName}|{synthetic}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var profile = synthetic ? SyntheticProfile(name) : NaturalProfile(def, name, originName);
        Cache[key] = profile;
        return profile;
    }

    // One of the four clarity grades for a new stone, from its species and its deposit.
    public static GemGrading.Grade PickClarity(GemDefinition def, GemOrigin origin, bool synthetic)
    {
        var weights = For(def, origin, synthetic).Clarity;
        var total = 0f;
        foreach (var w in weights) total += w;

        var roll = Random.value * total;
        var cumulative = 0f;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return GemGrading.ClarityGrades[i];
        }
        return GemGrading.ClarityGrades[GemGrading.ClarityGrades.Length - 1];
    }

    private static InclusionProfile NaturalProfile(GemDefinition def, string name, string originName)
    {
        string clarity = null;
        string features = null;

        foreach (var (keys, c, f) in Species)
        {
            if (!MatchesAny(name, keys)) continue;
            clarity = c;
            features = f;
            break;
        }

        foreach (var (species, origin, c, f) in OriginRules)
        {
            if (!name.Contains(species) || !originName.Contains(origin)) continue;
            if (!string.IsNullOrEmpty(c)) clarity = c;
            features = f;
            break;
        }

        if (features == null) features = FeaturesFromText(def);
        if (clarity == null) clarity = ClarityFromText(def);
        return Make(clarity, features);
    }

    // Flame fusion, flux, hydrothermal... each synthetic shows its own growth marks.
    private static InclusionProfile SyntheticProfile(string name)
    {
        if (name.Contains("corindon") || name.Contains("rubis") || name.Contains("saphir") || name.Contains("padparadscha"))
            return Make("6,24,44,26", "striae=40,comet=32,bubbles=12,bubble=10,flux=6");

        // The synthetic species have their own profile in the table.
        var own = name.Contains("synthétique") || name == "cz" || name == "yag" || name == "ggg" || name == "verre";
        if (own) return NaturalProfile(null, name, "");

        if (name.Contains("diamant")) return Make("4,22,44,30", "sulfide=30,growth=25,cloud=10,crystal=6");

        return Make("8,26,42,24", "striae=26,comet=24,bubble=16,flux=24,growth=10");
    }

    private static bool MatchesAny(string name, string[] keys)
    {
        foreach (var key in keys)
            if (name.Contains(key)) return true;
        return false;
    }

    // Species not in the table: what the species' own description of its inclusions says.
    private static string FeaturesFromText(GemDefinition def)
    {
        var text = (def != null ? (def.inclusions ?? "") : "").ToLowerInvariant();
        var map = new (string word, string token)[]
        {
            ("clivage", "cleavage"), ("fracture", "fracture"), ("fissure", "fracture"), ("empreintes digitales", "finger"),
            ("plume", "feather"), ("voiles", "veil"), ("halo", "zircon"), ("aiguille", "silk"), ("soie", "silk"),
            ("fibre", "fibers"), ("tubulaire", "tubes"), ("cristaux", "crystal"), ("cristal", "crystal"), ("liquide", "two"),
            ("fluide", "liquid"), ("triphas", "three"), ("jardin", "three"), ("bulle", "bubble"), ("zonation", "zoning"),
            ("bandes", "zoning"), ("macl", "twin"),
        };

        var features = "";
        foreach (var (word, token) in map)
            if (text.Contains(word)) features += token + "=10,";
        return features.Length > 0 ? features : "liquid=10,crystal=8,finger=6";
    }

    private static string ClarityFromText(GemDefinition def)
    {
        var text = (def != null ? (def.inclusions ?? "") : "").ToLowerInvariant();
        if (text.Contains("très propre")) return "3,12,40,45";
        if (text.Contains("propre")) return "8,22,40,30";
        if (text.Contains("nombreuses")) return "45,40,13,2";
        return "25,30,30,15";
    }

    private static InclusionProfile Make(string clarity, string features)
    {
        var profile = new InclusionProfile();
        var weights = clarity.Split(',');
        for (var i = 0; i < profile.Clarity.Length && i < weights.Length; i++)
            profile.Clarity[i] = float.Parse(weights[i].Trim(), CultureInfo.InvariantCulture);

        foreach (var entry in features.Split(','))
        {
            var pair = entry.Split('=');
            var token = pair[0].Trim();
            if (token.Length == 0 || !Tokens.TryGetValue(token, out var shape)) continue;
            var weight = pair.Length > 1 ? float.Parse(pair[1].Trim(), CultureInfo.InvariantCulture) : 10f;
            profile.Add(shape.kind, shape.variant, weight);
        }

        if (profile.Entries.Count == 0)
        {
            profile.Add(FeatureKind.Fluid, 0, 10f);
            profile.Add(FeatureKind.Crystal, 0, 8f);
        }
        return profile;
    }
}
