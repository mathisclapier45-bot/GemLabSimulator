using System.Collections.Generic;
using UnityEngine;

public struct LapidaryQuote
{
    public bool IsValid;
    public string Reason;
    public int Price;
    public float LossMin;
    public float LossMax;
    public int Days;
    public float WeightMin;
    public float WeightMax;
}

// Prices, weight loss and turnaround of the lapidary workshop. Everything is derived from the
// chosen shape/style plus the species' fragility, so a delicate stone costs more and loses more.
public static class LapidaryCatalog
{
    public const float MinLoss = 0.38f;
    public const float MaxLoss = 0.70f;
    public const float LossSpread = 0.03f;
    public const int MaxJobs = 1;

    // Cutting grows the per-carat value of a finished stone compared with its rough form.
    private const float CutValuePremium = 3.2f;

    public static readonly string[] Shapes =
    {
        "Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Rectangle à pans coupés",
        "Octogone", "Baguette", "Poire", "Marquise", "Shield", "Triangle", "Cœur",
    };

    public static readonly string[] Styles =
    {
        "Brillant", "Mixte", "À degrés", "Ascher", "Princesse", "Ciseaux", "Radiant", "Cabochon",
    };

    private static readonly Dictionary<string, float> ShapeLoss = new Dictionary<string, float>
    {
        { "Rond", 0.52f }, { "Ovale", 0.46f }, { "Coussin", 0.44f }, { "Carré", 0.48f }, { "Rectangle", 0.45f },
        { "Rectangle à pans coupés", 0.47f }, { "Octogone", 0.49f }, { "Baguette", 0.56f }, { "Poire", 0.50f },
        { "Marquise", 0.54f }, { "Shield", 0.53f }, { "Triangle", 0.51f }, { "Cœur", 0.60f },
    };

    private static readonly Dictionary<string, float> StyleLoss = new Dictionary<string, float>
    {
        { "Brillant", 0.03f }, { "Mixte", 0f }, { "À degrés", -0.01f }, { "Ascher", 0.01f },
        { "Princesse", -0.02f }, { "Ciseaux", 0.04f }, { "Radiant", 0.02f }, { "Cabochon", -0.07f },
    };

    private static readonly Dictionary<string, float> ShapePrice = new Dictionary<string, float>
    {
        { "Rond", 1f }, { "Ovale", 1f }, { "Coussin", 1.1f }, { "Carré", 1.1f }, { "Rectangle", 1.1f },
        { "Rectangle à pans coupés", 1.3f }, { "Octogone", 1.2f }, { "Baguette", 1.3f }, { "Poire", 1.3f },
        { "Marquise", 1.3f }, { "Shield", 1.4f }, { "Triangle", 1.4f }, { "Cœur", 1.8f },
    };

    private static readonly Dictionary<string, float> StylePrice = new Dictionary<string, float>
    {
        { "Brillant", 1.3f }, { "Mixte", 1.2f }, { "À degrés", 1f }, { "Ascher", 1.3f },
        { "Princesse", 1.5f }, { "Ciseaux", 1.8f }, { "Radiant", 1.6f }, { "Cabochon", 0.6f },
    };

    private static readonly Dictionary<string, float> ShapeValue = new Dictionary<string, float>
    {
        { "Rond", 1f }, { "Ovale", 1.05f }, { "Coussin", 1.05f }, { "Carré", 1f }, { "Rectangle", 1f },
        { "Rectangle à pans coupés", 1.15f }, { "Octogone", 1.1f }, { "Baguette", 1.15f }, { "Poire", 1.1f },
        { "Marquise", 1.1f }, { "Shield", 1.15f }, { "Triangle", 1.1f }, { "Cœur", 1.25f },
    };

    private static readonly Dictionary<string, float> StyleValue = new Dictionary<string, float>
    {
        { "Brillant", 1.1f }, { "Mixte", 1.05f }, { "À degrés", 1f }, { "Ascher", 1.1f },
        { "Princesse", 1.15f }, { "Ciseaux", 1.15f }, { "Radiant", 1.15f }, { "Cabochon", 0.85f },
    };

    // Workshop time points: the sum decides between 1, 2 or 3 days.
    private static readonly Dictionary<string, int> ShapeTime = new Dictionary<string, int>
    {
        { "Rond", 0 }, { "Ovale", 0 }, { "Coussin", 0 }, { "Carré", 0 }, { "Rectangle", 0 },
        { "Rectangle à pans coupés", 1 }, { "Octogone", 1 }, { "Baguette", 1 }, { "Poire", 1 },
        { "Marquise", 1 }, { "Shield", 1 }, { "Triangle", 1 }, { "Cœur", 2 },
    };

    private static readonly Dictionary<string, int> StyleTime = new Dictionary<string, int>
    {
        { "Brillant", 1 }, { "Mixte", 1 }, { "À degrés", 0 }, { "Ascher", 1 },
        { "Princesse", 1 }, { "Ciseaux", 2 }, { "Radiant", 2 }, { "Cabochon", 0 },
    };

    // Brittleness / cleavage: multiplies both the price and the weight lost. Unlisted species fall back on hardness.
    private static readonly Dictionary<string, float> Fragility = new Dictionary<string, float>
    {
        { "Rhodocrosite", 1.8f }, { "Opale_Noble", 1.6f }, { "Opale_De_Feu", 1.6f }, { "Kyanite", 1.5f },
        { "Tanzanite", 1.4f }, { "Kunzite", 1.4f }, { "Fluorite", 1.4f }, { "Emeraude", 1.4f },
        { "Orthose", 1.4f }, { "Pierre_De_Lune", 1.4f }, { "Apatite", 1.3f }, { "Labradorite", 1.3f },
        { "Rhodonite", 1.3f }, { "Pezzottaite", 1.3f }, { "Zoisite", 1.3f }, { "Iolite", 1.2f },
        { "Turquoise", 1.2f }, { "Diopside_Chromifere", 1.2f }, { "Scapolite", 1.2f }, { "Painite", 1.2f },
    };

    private static readonly Dictionary<string, string[]> AllowedShapesByStyle = new Dictionary<string, string[]>
    {
        { "Brillant", new[] { "Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Octogone", "Poire", "Marquise", "Shield", "Triangle", "Cœur" } },
        { "À degrés", new[] { "Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Rectangle à pans coupés", "Octogone", "Baguette", "Poire", "Marquise", "Shield", "Triangle" } },
        { "Ascher", new[] { "Carré", "Octogone" } },
        { "Princesse", new[] { "Carré", "Rectangle" } },
        { "Ciseaux", new[] { "Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Octogone", "Shield" } },
        { "Radiant", new[] { "Carré", "Rectangle", "Rectangle à pans coupés", "Octogone" } },
    };

    private static readonly string[] OrganicPrefixes = { "Ivoire", "Corne", "Defense", "Ecaille" };

    public static bool IsDiamond(GemDefinition def)
    {
        return def != null && def.name.StartsWith("Diamant");
    }

    // Opaque and organic materials can only be polished into cabochons, never faceted.
    public static bool IsCabochonOnly(GemDefinition def)
    {
        if (def == null) return false;
        if (def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque) return true;
        if (!string.IsNullOrEmpty(def.transparency) && def.transparency.ToLowerInvariant().Contains("opaque")) return true;
        foreach (var prefix in OrganicPrefixes)
        {
            if (def.name.StartsWith(prefix)) return true;
        }
        return false;
    }

    public static float GetFragility(GemDefinition def)
    {
        if (Fragility.TryGetValue(def.name, out var value)) return value;
        if (def.hardness < 5f) return 1.3f;
        if (def.hardness < 6.5f) return 1.1f;
        return 1f;
    }

    public static bool IsStyleAllowed(GemDefinition def, string shape, string style)
    {
        if (style == "Cabochon") return true;
        if (IsCabochonOnly(def)) return false;
        if (style == "Mixte") return true;
        return AllowedShapesByStyle.TryGetValue(style, out var shapes) && System.Array.IndexOf(shapes, shape) >= 0;
    }

    public static string GetRefusalReason(GemInstance gem)
    {
        if (gem == null) return "";
        if (IsDiamond(gem.Definition)) return "Je ne taille pas les diamants. Adressez-vous aux diamantaires.";
        if (!gem.IsRough) return "Cette pierre est déjà taillée.";
        if (!gem.IsIdentified) return "Je ne taille pas une pierre dont j'ignore la nature. Identifiez-la d'abord.";
        return null;
    }

    public static LapidaryQuote GetQuote(GemInstance gem, string shape, string style)
    {
        var quote = new LapidaryQuote();
        var refusal = GetRefusalReason(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            quote.Reason = refusal;
            return quote;
        }

        if (!IsStyleAllowed(gem.Definition, shape, style))
        {
            quote.Reason = IsCabochonOnly(gem.Definition) && style != "Cabochon"
                ? "Cette matière est trop opaque pour être facettée : seul le cabochon est possible."
                : $"Le style « {style} » ne se taille pas en forme « {shape} ».";
            return quote;
        }

        var fragility = GetFragility(gem.Definition);
        var loss = ShapeLoss[shape] + StyleLoss[style] + (fragility - 1f) * 0.12f;
        loss = Mathf.Clamp(loss, MinLoss, MaxLoss);

        quote.IsValid = true;
        quote.LossMin = Mathf.Max(MinLoss, loss - LossSpread);
        quote.LossMax = Mathf.Min(MaxLoss, loss + LossSpread);
        quote.WeightMin = gem.CaratWeight * (1f - quote.LossMax);
        quote.WeightMax = gem.CaratWeight * (1f - quote.LossMin);

        var rawPrice = (10f + 8f * gem.CaratWeight) * ShapePrice[shape] * StylePrice[style] * fragility;
        quote.Price = Mathf.Max(10, Mathf.RoundToInt(rawPrice / 5f) * 5);

        var timePoints = ShapeTime[shape] + StyleTime[style] + (fragility >= 1.3f ? 1 : 0);
        quote.Days = timePoints <= 1 ? 1 : timePoints <= 3 ? 2 : 3;
        return quote;
    }

    // Rolls the actual loss inside the announced range and returns the resulting weight and market value.
    public static void RollResult(GemInstance gem, string shape, string style, LapidaryQuote quote,
        out float newWeight, out int newValue)
    {
        var loss = Random.Range(quote.LossMin, quote.LossMax);
        newWeight = Mathf.Max(0.01f, Mathf.Round(gem.CaratWeight * (1f - loss) * 100f) / 100f);
        var value = gem.FinalValue * Mathf.Pow(1f - loss, 1.5f) * CutValuePremium * ShapeValue[shape] * StyleValue[style];
        newValue = Mathf.Max(1, Mathf.RoundToInt(value));
    }
}
