using UnityEngine;

public enum HphtOutcome { Ruined, NoEffect, Weak, Good, Excellent }
public enum HphtKind { Decolor, Yellow }

public struct HphtQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public HphtKind Kind;
    public bool HasNitrogen;
    public float Reaction;           // 0..1, how close the settings are to what the stone needs
    public float Graphitization;     // 0..1, chance that the diamond turns to graphite
    public string Advice;

    public string ReactionLabel => Reaction < 0.2f ? "aucune" : Reaction < 0.45f ? "faible" : Reaction < 0.75f ? "probable" : "forte";
    public string RiskLabel => Graphitization < 0.08f ? "très faible" : Graphitization < 0.25f ? "faible" : Graphitization < 0.5f ? "élevé" : "très élevé";
}

public struct HphtResult
{
    public HphtOutcome Outcome;
    public HphtKind Kind;
    public int NewColorIndex;
    public float Hue, Saturation, Brightness;
    public float ValueFactor;
    public string Label;
    public string Description;
    public string Message;

    public bool IsRuined => Outcome == HphtOutcome.Ruined;
}

// The high-pressure high-temperature treatment of diamonds, as taught in the course: it changes the colour of a diamond and can be hard to detect
// without laboratory equipment. A brown diamond with little or no nitrogen in its atomic structure can become colourless. If there is nitrogen in
// the structure, an intense yellow to yellow-green colour is obtained, and the diamond can show a strong green fluorescence. The pressure keeps
// the diamond stable: with too little pressure for the temperature, it turns to graphite.
public static class HphtCatalog
{
    public const int MinTemperature = 1800;
    public const int MaxTemperature = 2600;
    public const int TemperatureStep = 50;
    public const int MinPressure = 30;          // in tenths of a GPa
    public const int MaxPressure = 80;
    public const int PressureStep = 5;
    public static readonly int[] DurationDays = { 1, 2, 3 };

    // Whether the diamond has nitrogen in its structure: most do. Stable for a given stone, and readable on the infrared spectrum of the station.
    public static bool HasNitrogen(GemInstance gem) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|nitrogen") % 100 >= 30;

    public static string TypeLabel(GemInstance gem) => HasNitrogen(gem) ? "azote en agrégats (type Ia)" : "très peu d'azote (type IIa)";

    public static int ColorIndexOf(GemInstance gem) => DiamondGradeModel.Official(gem).ColorIndex;

    public static bool IsFancy(GemInstance gem) => DiamondGradeModel.Official(gem).Fancy;

    public static bool CanTreat(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into the press, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (!DiamondGradeModel.CanGrade(gem)) return "Le HPHT ne traite ici que les diamants taillés.";
        if (gem.IsSynthetic || gem.Definition.name != "Diamant") return "Inutile de traiter un diamant synthétique.";
        if (gem.IsHphtTreated) return "Ce diamant a déjà été traité au HPHT : la presse n'a plus rien à lui apprendre.";
        if (IsFancy(gem)) return "Un diamant de couleur fancy ne se traite pas ici : on le perdrait.";
        if (!HasNitrogen(gem) && ColorIndexOf(gem) <= 3) return "Ce diamant est déjà presque incolore : il n'y a rien à éclaircir.";
        return null;
    }

    // The pressure a diamond needs not to turn to graphite at a temperature, in GPa.
    private static float StablePressure(int temperature) => 3f + (temperature - 1800f) / 200f;

    private static float WindowScore(HphtKind kind, int temperature)
    {
        var low = kind == HphtKind.Decolor ? 2000f : 2100f;
        var high = kind == HphtKind.Decolor ? 2400f : 2500f;
        if (temperature >= low && temperature <= high) return 1f;
        if (temperature < low) return Mathf.Clamp01((temperature - 1800f) / (low - 1800f)) * 0.7f;
        return 0.85f;
    }

    private static float DurationScore(int days) => days >= 3 ? 1f : days == 2 ? 0.95f : 0.75f;

    public static HphtQuote GetQuote(int temperature, int pressureTenths, int days, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal != null) return new HphtQuote { IsValid = false, Reason = refusal };

        var nitrogen = HasNitrogen(gem);
        var kind = nitrogen ? HphtKind.Yellow : HphtKind.Decolor;
        var pressure = pressureTenths / 10f;
        var needed = StablePressure(temperature);
        var graphitization = pressure >= needed ? 0.04f : Mathf.Clamp01(0.35f + (needed - pressure) * 0.3f);
        if (temperature > 2500) graphitization = Mathf.Clamp01(graphitization + 0.3f);

        var advice = nitrogen
            ? "Il y a de l'azote dans cette pierre : le HPHT en fera un jaune à jaune-vert intense, avec une forte fluorescence verte. À éviter pour un diamant incolore !"
            : "Peu d'azote : un diamant brun de ce type peut devenir incolore. Difficile à détecter sans appareils de laboratoire.";
        return new HphtQuote
        {
            IsValid = true,
            Days = days,
            Fee = 350 + Mathf.RoundToInt(temperature * 0.15f) + Mathf.RoundToInt(pressure * 30f) + days * 60 + Mathf.RoundToInt(gem.CaratWeight * 60f),
            Kind = kind,
            HasNitrogen = nitrogen,
            Reaction = WindowScore(kind, temperature) * DurationScore(days),
            Graphitization = graphitization,
            Advice = advice,
        };
    }

    // What comes out of the press. The same stone with the same settings always gives the same result.
    public static HphtResult Resolve(int temperature, int pressureTenths, int days, GemInstance gem)
    {
        var quote = GetQuote(temperature, pressureTenths, days, gem);
        if (!quote.IsValid) return new HphtResult { Outcome = HphtOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|hpht|{temperature}|{pressureTenths}|{days}");
        var roll = (hash % 1000u) / 1000f;

        if (roll < quote.Graphitization)
        {
            return new HphtResult
            {
                Outcome = HphtOutcome.Ruined,
                Kind = quote.Kind,
                ValueFactor = 0.25f,
                Label = "HPHT (graphitisé)",
                Description = "Traitement HPHT raté : la pression était insuffisante pour la température, la surface et des fissures internes sont graphitisées, noires.",
                Message = "Pas assez de pression pour cette température : le diamant a commencé à se transformer en graphite. Il est noirci et fissuré.",
            };
        }

        if (quote.Reaction < 0.35f)
        {
            return new HphtResult
            {
                Outcome = HphtOutcome.NoEffect,
                Kind = quote.Kind,
                ValueFactor = 1f,
                Description = "Passage à la presse sans effet visible.",
                Message = "Rien n'a changé : trop froid ou trop court, le diamant n'a pas réagi.",
            };
        }

        var score = quote.Reaction;
        var outcome = score >= 0.85f ? HphtOutcome.Excellent : score >= 0.6f ? HphtOutcome.Good : HphtOutcome.Weak;
        var oldIndex = ColorIndexOf(gem);
        var result = new HphtResult { Outcome = outcome, Kind = quote.Kind };

        if (quote.Kind == HphtKind.Decolor)
        {
            var target = outcome == HphtOutcome.Excellent ? 1 : outcome == HphtOutcome.Good ? 3 : Mathf.Max(2, oldIndex - 3);
            result.NewColorIndex = Mathf.Min(oldIndex, target);
            result.ValueFactor = Mathf.Clamp(DiamondGrading.ColorFactorOf(result.NewColorIndex) / Mathf.Max(0.01f, DiamondGrading.ColorFactorOf(oldIndex)) * 0.85f, 0.5f, 6f);
            result.Label = "HPHT (décoloré)";
            result.Description = "Traitement HPHT : diamant brun à peu d'azote devenu incolore ; difficile à détecter sans appareils de laboratoire (la photoluminescence et l'infrarouge le trahissent).";
            result.Message = outcome == HphtOutcome.Excellent ? "Remarquable : le diamant brun est devenu presque incolore."
                : outcome == HphtOutcome.Good ? "Beau résultat : la teinte brune a presque disparu."
                : "Un effet partiel : la teinte brune est atténuée, sans plus.";
        }
        else
        {
            result.Hue = outcome == HphtOutcome.Excellent ? 72f : outcome == HphtOutcome.Good ? 58f : 52f;
            result.Saturation = outcome == HphtOutcome.Excellent ? 0.95f : outcome == HphtOutcome.Good ? 0.85f : 0.45f;
            result.Brightness = outcome == HphtOutcome.Weak ? 0.92f : 0.86f;
            var factor = outcome == HphtOutcome.Excellent ? 3.0f : outcome == HphtOutcome.Good ? 2.4f : 1.4f;
            result.ValueFactor = Mathf.Clamp(factor / Mathf.Max(0.01f, DiamondGrading.ColorFactorOf(oldIndex)) * 0.85f, 0.3f, 6f);
            result.Label = "HPHT (jaune-vert)";
            result.Description = "Traitement HPHT : l'azote a produit une couleur jaune à jaune-vert intense, avec une forte fluorescence verte ; difficile à détecter sans appareils de laboratoire.";
            result.Message = outcome == HphtOutcome.Excellent ? "Spectaculaire : un jaune-vert intense, avec une forte fluorescence verte."
                : outcome == HphtOutcome.Good ? "Un beau jaune intense, avec une fluorescence verte marquée."
                : "Un jaune pâle : la couleur n'a pris qu'à moitié.";
        }
        return result;
    }
}
