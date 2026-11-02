using System.Collections.Generic;
using UnityEngine;

// One cut a cutter of the 5th floor offers: a shape and a style of cut, with what it costs in weight and in work.
public class DiamondCutOption
{
    public string Label;
    public string Shape;
    public string Style;
    public float YieldFactor = 1f;     // share of the weight kept, compared with a round brilliant
    public float PriceFactor = 1f;
    public float ValueFactor = 1f;     // fancy cuts are worth a little more per carat
    public int ExtraDays;
    public bool Melee;                 // several small round stones instead of a single one
}

// One of the four cutters of the cutting centre. They differ by the quality of their cut, by what they keep of the rough, by their prices
// and by the shapes they know.
public class DiamondCutter
{
    public string Name;
    public string Specialty;
    public string Pitch;
    public float WeightFactor;         // > 1: keeps more of the rough (and cuts less well); < 1: sacrifices weight to cut better
    public float BasePrice;
    public float PricePerCarat;
    public int BaseDays;
    public float MinRough;             // smallest rough this cutter agrees to work on
    public float[] QualityWeights;     // chance of an excellent, very good, good or fair cut
    public float ClarityGainChance;    // the cut removes an inclusion: one clarity grade better
    public float ClarityLossChance;    // the cut chips the stone: one clarity grade worse
    public DiamondCutOption[] Options;
}

public struct DiamondCutQuote
{
    public bool IsValid;
    public string Reason;
    public int Price;
    public int Days;
    public float WeightMin;
    public float WeightMax;
    public int StoneCount;
}

// Rules of the cutting centre: what a cutter makes of a rough diamond. The colour and the clarity of the rough are fixed; only the weight,
// the quality of the cut and, now and then, the clarity change.
public static class DiamondCutting
{
    private static readonly float[] QualityValue = { 1.08f, 1.00f, 0.92f, 0.82f };

    public static readonly DiamondCutter[] Cutters =
    {
        new DiamondCutter
        {
            Name = "Taillerie Jacobs",
            Specialty = "Tailles économiques et mêlées",
            Pitch = "Du travail rapide et bon marché. On garde un maximum de poids, mais les proportions et le poli ne sont pas soignés.",
            WeightFactor = 1.10f, BasePrice = 20f, PricePerCarat = 10f, BaseDays = 1, MinRough = 0f,
            QualityWeights = new[] { 2f, 18f, 45f, 35f }, ClarityGainChance = 0f, ClarityLossChance = 0.12f,
            Options = new[]
            {
                new DiamondCutOption { Label = "Rond brillant", Shape = "Rond", Style = "Brillant" },
                new DiamondCutOption { Label = "Ovale", Shape = "Ovale", Style = "Brillant", YieldFactor = 1.02f },
                new DiamondCutOption { Label = "Coussin", Shape = "Coussin", Style = "Brillant", YieldFactor = 1.03f },
                new DiamondCutOption { Label = "Mêlées (plusieurs petites pierres)", Shape = "Rond", Style = "Brillant", YieldFactor = 0.85f, PriceFactor = 1.4f, ExtraDays = 1, Melee = true },
            },
        },
        new DiamondCutter
        {
            Name = "Atelier Van Dael",
            Specialty = "Tailles standards",
            Pitch = "Des tailles courantes, bien exécutées : de bonnes proportions et un poli propre, pour un prix raisonnable.",
            WeightFactor = 1.00f, BasePrice = 60f, PricePerCarat = 35f, BaseDays = 2, MinRough = 0.3f,
            QualityWeights = new[] { 25f, 45f, 25f, 5f }, ClarityGainChance = 0.10f, ClarityLossChance = 0.04f,
            Options = new[]
            {
                new DiamondCutOption { Label = "Rond brillant", Shape = "Rond", Style = "Brillant" },
                new DiamondCutOption { Label = "Ovale", Shape = "Ovale", Style = "Brillant", YieldFactor = 1.06f },
                new DiamondCutOption { Label = "Coussin", Shape = "Coussin", Style = "Brillant", YieldFactor = 1.08f },
                new DiamondCutOption { Label = "Poire", Shape = "Poire", Style = "Brillant", YieldFactor = 1.04f, PriceFactor = 1.15f, ExtraDays = 1 },
                new DiamondCutOption { Label = "Marquise", Shape = "Marquise", Style = "Brillant", YieldFactor = 0.98f, PriceFactor = 1.15f, ExtraDays = 1 },
                new DiamondCutOption { Label = "Princesse", Shape = "Carré", Style = "Princesse", YieldFactor = 1.10f, PriceFactor = 1.1f },
                new DiamondCutOption { Label = "Émeraude", Shape = "Rectangle à pans coupés", Style = "À degrés", YieldFactor = 1.08f, PriceFactor = 1.1f },
            },
        },
        new DiamondCutter
        {
            Name = "Maison Dekeyser",
            Specialty = "Tailles d'exception",
            Pitch = "Une taille idéale, polie à la perfection. Elle sacrifie du poids et demande du temps, mais la pierre y gagne en éclat, et parfois en pureté.",
            WeightFactor = 0.92f, BasePrice = 250f, PricePerCarat = 150f, BaseDays = 4, MinRough = 1.0f,
            QualityWeights = new[] { 72f, 26f, 2f, 0f }, ClarityGainChance = 0.35f, ClarityLossChance = 0f,
            Options = new[]
            {
                new DiamondCutOption { Label = "Rond brillant idéal", Shape = "Rond", Style = "Brillant" },
                new DiamondCutOption { Label = "Ovale", Shape = "Ovale", Style = "Brillant", YieldFactor = 1.06f, PriceFactor = 1.1f },
                new DiamondCutOption { Label = "Coussin", Shape = "Coussin", Style = "Brillant", YieldFactor = 1.08f, PriceFactor = 1.1f },
                new DiamondCutOption { Label = "Princesse", Shape = "Carré", Style = "Princesse", YieldFactor = 1.10f, PriceFactor = 1.15f },
                new DiamondCutOption { Label = "Poire", Shape = "Poire", Style = "Brillant", YieldFactor = 1.04f, PriceFactor = 1.2f, ExtraDays = 1 },
            },
        },
        new DiamondCutter
        {
            Name = "Atelier Fantaisie Moreau",
            Specialty = "Tailles fantaisie",
            Pitch = "Cœurs, triangles, boucliers, baguettes... Des formes rares qui coûtent du poids et du temps, mais qui se vendent plus cher.",
            WeightFactor = 0.95f, BasePrice = 120f, PricePerCarat = 70f, BaseDays = 3, MinRough = 0.5f,
            QualityWeights = new[] { 20f, 45f, 30f, 5f }, ClarityGainChance = 0.15f, ClarityLossChance = 0.06f,
            Options = new[]
            {
                new DiamondCutOption { Label = "Cœur", Shape = "Cœur", Style = "Brillant", YieldFactor = 0.84f, PriceFactor = 1.6f, ValueFactor = 1.20f, ExtraDays = 1 },
                new DiamondCutOption { Label = "Triangle", Shape = "Triangle", Style = "Brillant", YieldFactor = 0.92f, PriceFactor = 1.3f, ValueFactor = 1.10f },
                new DiamondCutOption { Label = "Bouclier", Shape = "Shield", Style = "Brillant", YieldFactor = 0.95f, PriceFactor = 1.3f, ValueFactor = 1.12f },
                new DiamondCutOption { Label = "Baguette", Shape = "Baguette", Style = "À degrés", YieldFactor = 0.88f, PriceFactor = 1.2f, ValueFactor = 1.10f },
                new DiamondCutOption { Label = "Octogone Asscher", Shape = "Octogone", Style = "Ascher", YieldFactor = 1.00f, PriceFactor = 1.2f, ValueFactor = 1.10f },
                new DiamondCutOption { Label = "Radiant", Shape = "Rectangle", Style = "Radiant", YieldFactor = 1.08f, PriceFactor = 1.3f, ValueFactor = 1.10f },
            },
        },
    };

    // ---- The rough ---------------------------------------------------------------------------------------------------

    public static bool IsRoughDiamond(GemInstance gem)
    {
        return gem != null && gem.IsRough && LapidaryCatalog.IsDiamond(gem.Definition);
    }

    // The share of the weight that will remain once cut. A rough diamond from elsewhere than the tower gets an average one.
    public static float YieldOf(GemInstance gem)
    {
        return gem != null && gem.RoughYield > 0f ? gem.RoughYield : 0.40f;
    }

    // Colour and clarity of the rough as indexes in the diamond scales; the ones of a rough from elsewhere are drawn from the stone itself.
    public static void HiddenGrades(GemInstance gem, out int colorIndex, out int clarityIndex)
    {
        colorIndex = gem.RoughColorIndex >= 0
            ? gem.RoughColorIndex
            : Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(gem.ColorSaturation / 0.05f) * 12f), 0, 22);

        if (gem.RoughClarityIndex >= 0)
        {
            clarityIndex = gem.RoughClarityIndex;
            return;
        }
        var roll = StoneOptics.StableHash((gem.SpecimenId ?? "") + "|grade|clarity") % 100;
        switch (gem.ClarityGrade)
        {
            case "Loupe clean": clarityIndex = roll < 8 ? 0 : roll < 30 ? 1 : roll < 54 ? 2 : roll < 78 ? 3 : 4; break;
            case "Eye-clean": clarityIndex = roll < 50 ? 5 : 6; break;
            case "Inclus": clarityIndex = roll < 50 ? 7 : 8; break;
            default: clarityIndex = roll < 50 ? 9 : 10; break;
        }
    }

    public static string Refusal(GemInstance gem, DiamondCutter cutter)
    {
        if (gem == null) return "";
        if (!LapidaryCatalog.IsDiamond(gem.Definition)) return "Nous ne taillons que des diamants.";
        if (!gem.IsRough) return "Ce diamant est déjà taillé.";
        if (gem.CaratWeight < cutter.MinRough) return $"Ce cristal est trop petit pour nous : nous ne travaillons pas en dessous de {cutter.MinRough:0.0} ct.";
        return null;
    }

    // ---- Quote -------------------------------------------------------------------------------------------------------

    public static DiamondCutQuote GetQuote(DiamondCutter cutter, DiamondCutOption option, GemInstance gem)
    {
        var quote = new DiamondCutQuote();
        var refusal = Refusal(gem, cutter);
        if (!string.IsNullOrEmpty(refusal))
        {
            quote.Reason = refusal;
            return quote;
        }

        // The cutter's own estimate of the yield, as off-centre as the dealer's.
        DiamondDealerState.YieldEstimate(gem, out var low, out var high);
        var factor = cutter.WeightFactor * option.YieldFactor;
        quote.WeightMin = gem.CaratWeight * low / 100f * factor;
        quote.WeightMax = gem.CaratWeight * high / 100f * factor;
        var middle = (quote.WeightMin + quote.WeightMax) * 0.5f;

        if (option.Melee)
        {
            quote.StoneCount = MeleeCount(middle);
            if (middle < 0.06f)
            {
                quote.Reason = "Il n'y a pas assez de matière dans ce cristal pour en tirer des mêlées.";
                return quote;
            }
        }
        else
        {
            quote.StoneCount = 1;
        }

        quote.IsValid = true;
        quote.Price = Mathf.Max(10, Mathf.RoundToInt((cutter.BasePrice + cutter.PricePerCarat * gem.CaratWeight) * option.PriceFactor / 5f) * 5);
        quote.Days = cutter.BaseDays + option.ExtraDays;
        return quote;
    }

    private static int MeleeCount(float totalCarat)
    {
        return Mathf.Clamp(Mathf.RoundToInt(totalCarat / 0.1f), 3, 8);
    }

    // ---- Cutting -----------------------------------------------------------------------------------------------------

    // Rolls what the cutter really makes of the rough: the finished stones, not yet graded.
    public static List<GemInstance> Cut(DiamondCutter cutter, DiamondCutOption option, GemInstance rough, GemDefinition diamond)
    {
        HiddenGrades(rough, out var colorIndex, out var clarityIndex);

        var roll = Random.value;
        if (roll < cutter.ClarityGainChance) clarityIndex = Mathf.Max(1, clarityIndex - 1);
        else if (roll < cutter.ClarityGainChance + cutter.ClarityLossChance) clarityIndex = Mathf.Min(DiamondGrading.ClarityScale.Length - 1, clarityIndex + 1);

        var quality = PickQuality(cutter.QualityWeights);
        var total = rough.CaratWeight * YieldOf(rough) * cutter.WeightFactor * option.YieldFactor * Random.Range(0.97f, 1.03f);
        total = Mathf.Max(0.02f, total);

        var stones = new List<GemInstance>();
        var count = option.Melee ? MeleeCount(total) : 1;

        // The weight is shared between the stones, each a little different.
        var shares = new float[count];
        var sum = 0f;
        for (var i = 0; i < count; i++)
        {
            shares[i] = Random.Range(0.8f, 1.2f);
            sum += shares[i];
        }

        for (var i = 0; i < count; i++)
        {
            var carat = total * shares[i] / sum;
            carat = carat < 1f ? Mathf.Max(0.01f, Mathf.Round(carat * 100f) / 100f) : Mathf.Round(carat * 20f) / 20f;
            var size = DiamondDealerState.DimensionsFor(option.Shape, carat);
            var value = Mathf.Max(1, Mathf.RoundToInt(DiamondGrading.Value(carat, colorIndex, clarityIndex) * QualityValue[quality] * option.ValueFactor));
            stones.Add(GemInstance.CreateCutDiamond(diamond, carat, colorIndex, clarityIndex, option.Shape, option.Style, size.x, size.y, size.z, value, cutter.Name, quality));
        }
        return stones;
    }

    private static int PickQuality(float[] weights)
    {
        var total = 0f;
        foreach (var w in weights) total += w;
        var roll = Random.value * total;
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return i;
        }
        return weights.Length - 1;
    }
}
