using System;
using System.Collections.Generic;
using UnityEngine;

// The colours of the coloured ("fancy") diamonds after the GIA colour reference charts: 27 hues around a circle, and nine grades that mix tone and saturation
// (Faint, Very Light, Light, Fancy Light, Fancy, Fancy Intense, Fancy Vivid, Fancy Deep, Fancy Dark). Browns and grays are weaker or darker colours of the warm
// and cool hues. The game draws its coloured diamonds from this table, names them with it, prices them with it, and the book "Nuancier des diamants de couleur" prints it.
//
// How common each colour is follows the trade's own words: most coloured diamonds are brown, yellow or gray, blue ones turn up now and then, and green, pink, violet,
// orange and above all red are rare to extremely rare. Within a hue, the pale grades are far more common than the vivid or the deep ones.
public enum GiaGrade { Faint, VeryLight, Light, FancyLight, Fancy, FancyIntense, FancyVivid, FancyDeep, FancyDark }

public struct GiaHue
{
    public string English;       // "Orangy Yellow"
    public string French;        // "Jaune orangé"
    public float Degrees;        // the hue in the HSV wheel of the game (red 0, yellow 60, green 120, blue 240)
    public float Weight;         // how often a coloured diamond has this hue (relative)
    public float Rarity;         // how much the hue alone multiplies the price
    public float SatCap;         // the strongest saturation the hue reaches (blue diamonds stay grayish)
    public bool Pale;            // the pale grades (Faint, Very Light, Light) exist for this hue; below Fancy Light a yellow diamond is only a D-Z stone

    public GiaHue(string english, string french, float degrees, float weight, float rarity, float satCap = 1f, bool pale = true)
    {
        English = english; French = french; Degrees = degrees; Weight = weight; Rarity = rarity; SatCap = satCap; Pale = pale;
    }
}

public struct GiaColor
{
    public string Name;          // "Jaune orangé Fancy Intense"
    public string Hue;           // "Jaune orangé", "Rose", "Brun jaunâtre", "Gris"
    public GiaGrade Grade;
    public float ValueMultiplier; // what the grade alone does to the price
    public float Rarity;         // what the hue alone does to the price
}

public static class GiaDiamondColors
{
    // The 27 hues around the circle, from red clockwise as the chart prints them.
    public static readonly GiaHue[] Hues =
    {
        new GiaHue("Red",             "Rouge",             355f, 0.002f, 160f, 1f, true),
        new GiaHue("Orangy Red",      "Rouge orangé",        8f, 0.003f, 110f, 1f, true),
        new GiaHue("Reddish Orange",  "Orange rougeâtre",   18f, 0.004f,  80f, 1f, true),
        new GiaHue("Orange",          "Orange",             28f, 0.006f,  60f, 1f, true),
        new GiaHue("Yellowish Orange","Orange jaunâtre",    37f, 0.010f,  30f, 1f, false),
        new GiaHue("Yellow-Orange",   "Jaune-orange",       44f, 0.018f,  12f, 1f, false),
        new GiaHue("Orange-Yellow",   "Orange-jaune",       49f, 0.030f,   8f, 1f, false),
        new GiaHue("Orangy Yellow",   "Jaune orangé",       54f, 0.050f,   6f, 1f, false),
        new GiaHue("Yellow",          "Jaune",              60f, 0.120f,   5f, 1f, false),
        new GiaHue("Greenish Yellow", "Jaune verdâtre",     68f, 0.040f,   6f, 1f, false),
        new GiaHue("Green-Yellow",    "Vert-jaune",         76f, 0.012f,  14f, 1f, false),
        new GiaHue("Yellow-Green",    "Jaune-vert",         88f, 0.010f,  20f, 0.95f, true),
        new GiaHue("Yellowish Green", "Vert jaunâtre",     105f, 0.008f,  28f, 0.90f, true),
        new GiaHue("Green",           "Vert",              135f, 0.006f,  38f, 0.80f, true),
        new GiaHue("Bluish Green",    "Vert bleuâtre",     160f, 0.004f,  42f, 0.75f, true),
        new GiaHue("Blue-Green",      "Bleu-vert",         175f, 0.003f,  45f, 0.75f, true),
        new GiaHue("Green-Blue",      "Vert-bleu",         187f, 0.003f,  45f, 0.75f, true),
        new GiaHue("Greenish Blue",   "Bleu verdâtre",     200f, 0.008f,  40f, 0.70f, true),
        new GiaHue("Blue",            "Bleu",              220f, 0.025f,  38f, 0.62f, true),
        new GiaHue("Violetish Blue",  "Bleu violacé",      240f, 0.006f,  50f, 0.62f, true),
        new GiaHue("Bluish Violet",   "Violet bleuâtre",   255f, 0.004f,  60f, 0.62f, true),
        new GiaHue("Violet",          "Violet",            268f, 0.004f,  70f, 0.65f, true),
        new GiaHue("Purple",          "Pourpre",           285f, 0.004f,  60f, 0.75f, true),
        new GiaHue("Reddish Purple",  "Pourpre rougeâtre", 305f, 0.004f,  55f, 0.85f, true),
        new GiaHue("Red-Purple",      "Rouge-pourpre",     320f, 0.003f,  70f, 0.90f, true),
        new GiaHue("Purple-Red",      "Pourpre-rouge",     332f, 0.004f,  75f, 0.95f, true),
        new GiaHue("Purplish Red",    "Rouge pourpré",     344f, 0.005f,  80f, 1f, true),
    };

    public const float BrownWeight = 0.360f;      // browns: the most common coloured diamonds
    public const float GrayWeight = 0.120f;
    public const float BrownRarity = 2.5f;
    public const float GrayRarity = 4f;

    public static readonly string[] GradeEnglish = { "Faint", "Very Light", "Light", "Fancy Light", "Fancy", "Fancy Intense", "Fancy Vivid", "Fancy Deep", "Fancy Dark" };

    // How often each grade comes up within a hue: the pale ones are far more common than the vivid and the deep ones.
    private static readonly float[] GradeWeight = { 0.08f, 0.13f, 0.20f, 0.20f, 0.16f, 0.11f, 0.05f, 0.05f, 0.02f };

    // What the grade alone does to the price.
    private static readonly float[] GradeValue = { 0.30f, 0.40f, 0.55f, 0.85f, 1.25f, 1.80f, 2.60f, 1.70f, 0.80f };

    // Saturation and brightness of each grade, as ranges (brightness first for the dark ones).
    private static readonly float[][] GradeRange =
    {
        new[] { 0.05f, 0.11f, 0.95f, 1.00f },
        new[] { 0.12f, 0.19f, 0.95f, 1.00f },
        new[] { 0.20f, 0.29f, 0.94f, 1.00f },
        new[] { 0.30f, 0.42f, 0.90f, 1.00f },
        new[] { 0.45f, 0.58f, 0.86f, 0.96f },
        new[] { 0.62f, 0.78f, 0.82f, 0.94f },
        new[] { 0.84f, 1.00f, 0.88f, 1.00f },
        new[] { 0.80f, 1.00f, 0.36f, 0.52f },       // deep: a rich, dark, saturated colour (a wine red, a deep green)
        new[] { 0.45f, 0.70f, 0.20f, 0.32f },       // dark: nearly black, with a hint of colour
    };

    public static string English(GiaGrade grade) => GradeEnglish[(int)grade];

    public static float GradeMultiplier(GiaGrade grade) => GradeValue[(int)grade];

    // ---- Sampling -----------------------------------------------------------------------------------------------------

    private static float TotalWeight()
    {
        var total = BrownWeight + GrayWeight;
        foreach (var hue in Hues) total += hue.Weight;
        return total;
    }

    // A colour for a new coloured diamond: the hue by its rarity, then the grade, then a value inside the ranges of that grade.
    public static GemColorValue Sample(Func<float> random)
    {
        var roll = random() * TotalWeight();
        if ((roll -= BrownWeight) <= 0f) return SampleBrown(random);
        if ((roll -= GrayWeight) <= 0f) return SampleGray(random);
        var chosen = Hues[Hues.Length - 1];
        foreach (var hue in Hues)
        {
            roll -= hue.Weight;
            if (roll <= 0f) { chosen = hue; break; }
        }
        return SampleHue(chosen, PickGrade(chosen, random), random);
    }

    private static GiaGrade PickGrade(GiaHue hue, Func<float> random)
    {
        var total = 0f;
        for (var g = 0; g < GradeWeight.Length; g++)
            if (hue.Pale || g >= (int)GiaGrade.FancyLight) total += GradeWeight[g];
        var roll = random() * total;
        for (var g = 0; g < GradeWeight.Length; g++)
        {
            if (!hue.Pale && g < (int)GiaGrade.FancyLight) continue;
            roll -= GradeWeight[g];
            if (roll <= 0f) return (GiaGrade)g;
        }
        return GiaGrade.Fancy;
    }

    public static GemColorValue SampleHue(GiaHue hue, GiaGrade grade, Func<float> random)
    {
        var range = GradeRange[(int)grade];
        var saturation = Mathf.Lerp(range[0], range[1], random()) * hue.SatCap;
        var brightness = Mathf.Lerp(range[2], range[3], random());
        var hueDegrees = hue.Degrees + (random() - 0.5f) * 4f;
        return new GemColorValue(hueDegrees, saturation, brightness);
    }

    // A synthetic diamond can be of any colour: every hue of the circle is as likely as another, in any grade (a few browns and grays too).
    public static GemColorValue SampleAnyHue(Func<float> random)
    {
        var roll = random();
        if (roll < 0.06f) return SampleBrown(random);
        if (roll < 0.10f) return SampleGray(random);
        var hue = Hues[Mathf.Min(Hues.Length - 1, (int)(random() * Hues.Length))];
        var total = 0f;
        for (var g = 0; g < GradeWeight.Length; g++) total += GradeWeight[g];
        var pick = random() * total;
        var grade = GiaGrade.Fancy;
        for (var g = 0; g < GradeWeight.Length; g++)
        {
            pick -= GradeWeight[g];
            if (pick <= 0f) { grade = (GiaGrade)g; break; }
        }
        return SampleHue(hue, grade, random);
    }

    private static GemColorValue SampleBrown(Func<float> random)
    {
        // Orange-brown and yellow-brown are the most common, pink-brown and greenish brown less so.
        var roll = random();
        var hue = roll < 0.40f ? 28f : roll < 0.75f ? 50f : roll < 0.90f ? 12f : 65f;
        return new GemColorValue(hue + (random() - 0.5f) * 8f, Mathf.Lerp(0.35f, 0.80f, random()), Mathf.Lerp(0.32f, 0.72f, random()));
    }

    private static GemColorValue SampleGray(Func<float> random)
    {
        var hue = Mathf.Lerp(190f, 270f, random());
        return new GemColorValue(hue, Mathf.Lerp(0.12f, 0.19f, random()), Mathf.Lerp(0.35f, 0.82f, random()));
    }

    // ---- Naming ---------------------------------------------------------------------------------------------------------

    public static int NearestHue(float degrees)
    {
        var best = 0;
        var bestDistance = 1000f;
        for (var i = 0; i < Hues.Length; i++)
        {
            var distance = Mathf.Abs(Mathf.DeltaAngle(degrees, Hues[i].Degrees));
            if (distance < bestDistance) { bestDistance = distance; best = i; }
        }
        return best;
    }

    private static bool IsBrown(GemColorValue c) => c.Hue >= 14f && c.Hue < 70f && c.Brightness < 0.75f && c.Saturation >= 0.30f;

    private static bool IsGray(GemColorValue c) => c.Saturation < 0.20f && c.Brightness < 0.85f;

    private static GiaGrade BrownGrade(float brightness)
    {
        if (brightness >= 0.62f) return GiaGrade.FancyLight;
        if (brightness >= 0.50f) return GiaGrade.Fancy;
        if (brightness >= 0.40f) return GiaGrade.FancyDeep;
        return GiaGrade.FancyDark;
    }

    private static GiaGrade GrayGrade(float brightness)
    {
        if (brightness >= 0.65f) return GiaGrade.FancyLight;
        if (brightness >= 0.45f) return GiaGrade.Fancy;
        return GiaGrade.FancyDark;
    }

    private static GiaGrade ChromaticGrade(float saturationOfHue, float brightness)
    {
        if (brightness < 0.66f)
        {
            if (brightness < 0.34f || saturationOfHue < 0.60f) return GiaGrade.FancyDark;
            return GiaGrade.FancyDeep;
        }
        if (saturationOfHue < 0.12f) return GiaGrade.Faint;
        if (saturationOfHue < 0.20f) return GiaGrade.VeryLight;
        if (saturationOfHue < 0.30f) return GiaGrade.Light;
        if (saturationOfHue < 0.44f) return GiaGrade.FancyLight;
        if (saturationOfHue < 0.60f) return GiaGrade.Fancy;
        if (saturationOfHue < 0.80f) return GiaGrade.FancyIntense;
        return GiaGrade.FancyVivid;
    }

    private static string BrownName(float hue)
    {
        if (hue < 20f) return "Brun rosâtre";
        if (hue < 38f) return "Brun orangé";
        if (hue < 55f) return "Brun jaunâtre";
        return "Brun verdâtre";
    }

    // The pale reds are pinks: the chart prints "Pink" over the red range at the lighter tones.
    private static string PinkName(int hueIndex)
    {
        switch (hueIndex)
        {
            case 1: return "Rose orangé";
            case 0: return "Rose";
            case 24: return "Rose pourpré";
            default: return "Rose violacé";      // purple-red and purplish red
        }
    }

    private static bool InRedRange(int hueIndex) => hueIndex == 0 || hueIndex == 1 || hueIndex == 24 || hueIndex == 25 || hueIndex == 26;

    public static GiaColor Describe(GemColorValue color)
    {
        if (IsGray(color))
        {
            var gray = GrayGrade(color.Brightness);
            return Make("Gris", gray, GrayRarity);
        }
        if (IsBrown(color))
        {
            var brown = BrownGrade(color.Brightness);
            return Make(BrownName(color.Hue), brown, BrownRarity);
        }

        var index = NearestHue(color.Hue);
        var hue = Hues[index];
        var normal = Mathf.Min(1f, color.Saturation / Mathf.Max(0.3f, hue.SatCap));
        var grade = ChromaticGrade(normal, color.Brightness);
        var name = hue.French;
        if (InRedRange(index) && normal < 0.80f && color.Brightness >= 0.80f) name = PinkName(index);
        return Make(name, grade, hue.Rarity);
    }

    private static GiaColor Make(string hue, GiaGrade grade, float rarity)
    {
        return new GiaColor
        {
            Hue = hue,
            Grade = grade,
            Name = hue + " " + GradeEnglish[(int)grade],
            ValueMultiplier = GradeValue[(int)grade],
            Rarity = rarity,
        };
    }

    // ---- For the book -----------------------------------------------------------------------------------------------------

    // How many coloured diamonds in a thousand have this hue.
    public static float PerThousand(int hueIndex)
    {
        return Hues[hueIndex].Weight / TotalWeight() * 1000f;
    }

    public static float BrownPerThousand => BrownWeight / TotalWeight() * 1000f;

    public static float GrayPerThousand => GrayWeight / TotalWeight() * 1000f;

    // The share of a grade within a hue that has it, in percent.
    public static float GradeShare(GiaHue hue, GiaGrade grade)
    {
        var total = 0f;
        for (var g = 0; g < GradeWeight.Length; g++)
            if (hue.Pale || g >= (int)GiaGrade.FancyLight) total += GradeWeight[g];
        if (!hue.Pale && (int)grade < (int)GiaGrade.FancyLight) return 0f;
        return GradeWeight[(int)grade] / total * 100f;
    }

    // A typical colour of a hue at a grade (the middle of the ranges), for the pictures of the book.
    public static Color Typical(GiaHue hue, GiaGrade grade)
    {
        var range = GradeRange[(int)grade];
        var saturation = (range[0] + range[1]) * 0.5f * hue.SatCap;
        var brightness = (range[2] + range[3]) * 0.5f;
        return Color.HSVToRGB(hue.Degrees / 360f, saturation, brightness);
    }

    public static Color TypicalBrown(GiaGrade grade)
    {
        var brightness = grade == GiaGrade.FancyLight ? 0.68f : grade == GiaGrade.Fancy ? 0.56f : grade == GiaGrade.FancyDeep ? 0.45f : 0.34f;
        return Color.HSVToRGB(32f / 360f, 0.62f, brightness);
    }

    public static Color TypicalGray(GiaGrade grade)
    {
        var brightness = grade == GiaGrade.FancyLight ? 0.76f : grade == GiaGrade.Fancy ? 0.55f : 0.36f;
        return Color.HSVToRGB(215f / 360f, 0.15f, brightness);
    }
}
