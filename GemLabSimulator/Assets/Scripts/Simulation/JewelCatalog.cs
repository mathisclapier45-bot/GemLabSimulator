using UnityEngine;

public enum SetOutcome { Fine, Chipped, FillingDamaged }

public class JewelKindInfo
{
    public string Name;
    public string Detail;
    public float Grams;
    public int Labor;
    public int Days;
}

public class JewelMetalInfo
{
    public string Name;
    public string Detail;
    public float PricePerGram;
    public Color Color;
}

public class JewelSettingInfo
{
    public string Name;
    public string Detail;
    public string Description;
    public int Labor;
    public int Days;
    public float Visibility;         // how much of the stone, and of its traces, stays in view: 1 is all of it
    public float Light;              // how much light the stone keeps: what the value of the jewel makes of it
}

public struct SetQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public int MetalCost;
    public float Risk;               // 0..1, chance that the stone is damaged while it is set
    public string Hides;
    public string Advice;

    public string RiskLabel => Risk < 0.05f ? "très faible" : Risk < 0.12f ? "faible" : Risk < 0.25f ? "modéré" : "élevé";
}

public struct SetResult
{
    public SetOutcome Outcome;
    public float ValueFactor;
    public string Message;
}

// The jeweller: a cut stone is set in a ring, a pendant or a brooch, of silver, gold or platinum, with prongs, a bezel or a closed back. The jewel is
// worth more than the stone alone, and the setting hides a part of the stone and of what was done to it: a closed back covers the pavilion where the
// laser holes, the fillings and the coatings are, a bezel the girdle. The heat of the soldering can ruin a filling, and prongs can chip a soft stone.
// A set stone has to be taken out to be tested on the refractometer or the scales, or treated.
public static class JewelCatalog
{
    public const string SetRefusal = "Cette pierre est sertie dans un bijou : faites-la sortir de sa monture chez le bijoutier d'abord.";
    public const int UnsetFee = 30;
    public const float JewelerExpertise = 1.1f;
    public const float JewelerShare = 0.78f;        // the part of the value of a jewel the jeweller pays when he buys it back

    public static readonly JewelKindInfo[] Kinds =
    {
        new JewelKindInfo { Name = "Bague", Detail = "Un solitaire", Grams = 5f, Labor = 40, Days = 2 },
        new JewelKindInfo { Name = "Pendentif", Detail = "Sur une chaîne", Grams = 3f, Labor = 30, Days = 1 },
        new JewelKindInfo { Name = "Broche", Detail = "Une pièce de collection", Grams = 6f, Labor = 45, Days = 2 },
    };

    public static readonly JewelMetalInfo[] Metals =
    {
        new JewelMetalInfo { Name = "Argent", Detail = "925", PricePerGram = 3f, Color = new Color(0.80f, 0.82f, 0.86f) },
        new JewelMetalInfo { Name = "Or 18 carats", Detail = "750", PricePerGram = 55f, Color = new Color(0.95f, 0.78f, 0.28f) },
        new JewelMetalInfo { Name = "Platine", Detail = "950", PricePerGram = 75f, Color = new Color(0.86f, 0.88f, 0.92f) },
    };

    public static readonly JewelSettingInfo[] Settings =
    {
        new JewelSettingInfo { Name = "Griffes", Detail = "Quatre ou six griffes", Labor = 30, Days = 0, Visibility = 0.9f, Light = 1.0f,
            Description = "La pierre est tenue par des griffes : elle prend toute la lumière. Les griffes cachent peu de chose, mais elles peuvent ébrécher une pierre tendre." },
        new JewelSettingInfo { Name = "Clos", Detail = "Chaton fermé sur le rondiste", Labor = 55, Days = 1, Visibility = 0.65f, Light = 0.97f,
            Description = "Un chaton enserre le rondiste : il protège la pierre et cache les défauts du bord. Il laisse passer un peu moins de lumière." },
        new JewelSettingInfo { Name = "Fond fermé", Detail = "Serti fermé", Labor = 75, Days = 1, Visibility = 0.4f, Light = 0.93f,
            Description = "Le dessous est fermé par le métal : on ne voit plus la culasse, où se cachent les trous du laser, les remplissages et les revêtements. La pierre est plus sombre, et la soudure chauffe." },
    };

    public static bool CanSet(GemInstance gem) => gem != null && gem.IsIdentified && !gem.IsClientStone && !gem.IsDonation && !gem.IsRough && !gem.IsSet;

    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (gem.IsSet) return "Cette pierre est déjà sertie.";
        if (gem.IsRough) return "Une pierre brute ne se sertit pas : il faut la tailler d'abord.";
        if (gem.IsClientStone) return "C'est la pierre d'un client : elle lui retourne telle quelle, sans bijou.";
        if (gem.IsDonation) return "Un don se conserve : on ne le sertit pas.";
        if (!gem.IsIdentified) return "Le bijoutier veut savoir ce qu'il monte : faites identifier la pierre.";
        return null;
    }

    private static bool IsFilled(GemInstance gem) => gem.HasTreatment(TreatmentKind.Filled);

    private static float ChipRisk(GemInstance gem, int setting)
    {
        var hardness = gem.Definition != null ? gem.Definition.hardness : 7f;
        var soft = hardness < 6f ? 0.18f : hardness < 7f ? 0.10f : 0.03f;
        return setting == 0 ? soft : setting == 1 ? soft * 0.3f : soft * 0.2f;
    }

    private static float FillingRisk(GemInstance gem, int setting)
    {
        if (!IsFilled(gem)) return 0f;
        return setting == 0 ? 0.20f : setting == 1 ? 0.30f : 0.45f;
    }

    public static SetQuote GetQuote(int kind, int metal, int setting, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal != null) return new SetQuote { IsValid = false, Reason = refusal };

        kind = Mathf.Clamp(kind, 0, Kinds.Length - 1);
        metal = Mathf.Clamp(metal, 0, Metals.Length - 1);
        setting = Mathf.Clamp(setting, 0, Settings.Length - 1);
        var k = Kinds[kind];
        var m = Metals[metal];
        var s = Settings[setting];

        var metalCost = Mathf.RoundToInt(k.Grams * m.PricePerGram);
        var hides = setting == 2 ? "la culasse : trous du laser, remplissages, revêtements"
            : setting == 1 ? "le bord de la pierre : éclats, usure du rondiste"
            : "presque rien";
        var advice = IsFilled(gem)
            ? "Cette pierre est remplie : la chaleur de la soudure risque d'abîmer le remplissage."
            : gem.Definition != null && gem.Definition.hardness < 7f && setting == 0
                ? "Une pierre tendre dans des griffes risque de s'ébrécher : un chaton la protège mieux."
                : s.Description;
        return new SetQuote
        {
            IsValid = true,
            Days = k.Days + s.Days,
            MetalCost = metalCost,
            Fee = k.Labor + s.Labor + metalCost + Mathf.RoundToInt(gem.CaratWeight * 5f),
            Risk = Mathf.Clamp01(ChipRisk(gem, setting) + FillingRisk(gem, setting)),
            Hides = hides,
            Advice = advice,
        };
    }

    // What comes out of the workshop. The same stone with the same choices always gives the same result.
    public static SetResult Resolve(int kind, int metal, int setting, GemInstance gem)
    {
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|set|{kind}|{metal}|{setting}");
        var roll = (hash % 1000u) / 1000f;
        var filling = FillingRisk(gem, setting);
        var chip = ChipRisk(gem, setting);

        if (roll < filling)
            return new SetResult { Outcome = SetOutcome.FillingDamaged, ValueFactor = 0.6f, Message = "La chaleur de la soudure a ruiné le remplissage : les fissures sont revenues." };
        if (roll < filling + chip)
            return new SetResult { Outcome = SetOutcome.Chipped, ValueFactor = 0.75f, Message = "Une griffe a ébréché la pierre au sertissage : un éclat sur le rondiste." };
        return new SetResult { Outcome = SetOutcome.Fine, ValueFactor = 1f, Message = "Le bijou est terminé : la pierre est bien tenue." };
    }

    // The value of the jewel: the stone, a little less for a setting that lets less light in, and the metal.
    public static int JewelValue(GemInstance gem, int stoneValue)
    {
        var setting = SettingIndex(gem.JewelSetting);
        var light = setting >= 0 ? Settings[setting].Light : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(stoneValue * light + gem.JewelMetalValue * 0.8f));
    }

    public static int SettingIndex(string name)
    {
        for (var i = 0; i < Settings.Length; i++) if (Settings[i].Name == name) return i;
        return -1;
    }

    public static int KindIndex(string name)
    {
        for (var i = 0; i < Kinds.Length; i++) if (Kinds[i].Name == name) return i;
        return -1;
    }

    // How much of the stone the setting leaves in view.
    public static float VisibilityOf(GemInstance gem)
    {
        var i = SettingIndex(gem.JewelSetting);
        return i >= 0 ? Settings[i].Visibility : 1f;
    }

    public static string Describe(GemInstance gem)
    {
        return gem.IsSet ? $"{gem.JewelKind} en {gem.JewelMetal.ToLowerInvariant()}, serti {gem.JewelSetting.ToLowerInvariant()}" : "";
    }
}
