using System;
using System.Collections.Generic;
using UnityEngine;

// Color of a stone as a gemological laboratory describes it: hue, saturation and brightness (HSB), graded in tone steps
// (clair, pastel, intense, vif, profond, sombre...) after the Bellerophon Gemlab colored-stone charts, with the GIA
// "Fancy" scale for colored diamonds and the rare trade names (sang de pigeon, bleu royal, padparadscha, Paraïba...).

public struct GemColorValue
{
    public float Hue;             // degrees, 0..360
    public float Saturation;      // 0..1
    public float Brightness;      // 0..1

    public GemColorValue(float hue, float saturation, float brightness)
    {
        Hue = Mathf.Repeat(hue, 360f);
        Saturation = Mathf.Clamp01(saturation);
        Brightness = Mathf.Clamp01(brightness);
    }

    public Color ToColor() => Color.HSVToRGB(Hue / 360f, Saturation, Brightness);
}

public struct GemColorGrade
{
    public string Name;           // e.g. "Rouge vif", "Rose pastel", "Jaune Fancy Vivid"
    public string Tone;           // the tone / saturation step alone: "Clair", "Vif", "Profond"...
    public int Row;               // 0 (light) .. 7 (dark), see ToneRow
    public float ValueMultiplier; // how much the color alone changes the price of the stone
}

public static class GemColorSystem
{
    // Rows of the Bellerophon grids: saturation 20 / 40 / 60 / 80 / 100 % at full brightness, then full saturation at 80 / 60 / 40 % brightness.
    private static readonly float[] RowSaturation = { 0.2f, 0.4f, 0.6f, 0.8f, 1f, 1f, 1f, 1f };
    private static readonly float[] RowBrightness = { 1f, 1f, 1f, 1f, 1f, 0.8f, 0.6f, 0.4f };
    private static readonly string[] RowWords = { "clair", "pastel", "", "intense", "vif", "vif", "profond", "sombre" };
    private static readonly string[] FancyWords = { "Fancy Light", "Fancy Light", "Fancy", "Fancy Intense", "Fancy Vivid", "Fancy Vivid", "Fancy Deep", "Fancy Dark" };
    private static readonly float[] RowValue = { 0.45f, 0.60f, 0.85f, 1.25f, 1.8f, 1.7f, 1.0f, 0.5f };

    // Hue names of the vivid hue wheel (degrees); red covers 340..15.
    private static readonly (float min, float max, string name)[] HueBands =
    {
        (15f, 20f, "rouge orangé"), (20f, 40f, "orange"), (40f, 65f, "jaune"), (65f, 80f, "vert jaunâtre"),
        (80f, 165f, "vert"), (165f, 175f, "vert bleuâtre"), (175f, 185f, "bleu verdâtre"), (185f, 255f, "bleu"),
        (255f, 265f, "bleu violacé"), (265f, 280f, "violet"), (280f, 330f, "pourpre"), (330f, 340f, "rouge pourpré"),
    };

    // ---- Grading ------------------------------------------------------------------------------------------------

    public static int ToneRow(float saturation, float brightness)
    {
        if (brightness >= 0.9f)
        {
            if (saturation < 0.3f) return 0;
            if (saturation < 0.5f) return 1;
            if (saturation < 0.7f) return 2;
            if (saturation < 0.9f) return 3;
            return 4;
        }
        if (brightness >= 0.7f) return saturation >= 0.85f ? 5 : (saturation >= 0.55f ? 3 : 2);
        if (brightness >= 0.5f) return saturation >= 0.65f ? 6 : 2;
        return 7;
    }

    private static string HueName(float hue)
    {
        foreach (var band in HueBands)
            if (hue >= band.min && hue < band.max) return band.name;
        return "rouge";
    }

    public static GemColorGrade Describe(GemColorValue color, bool fancyDiamond = false)
    {
        var h = color.Hue;
        var s = color.Saturation;
        var b = color.Brightness;

        // Neutral: colorless, gray, black.
        if (s < 0.10f)
        {
            var neutral = b < 0.18f ? "Noir" : (b < 0.72f ? "Gris" : "Incolore");
            return new GemColorGrade { Name = neutral, Tone = neutral, Row = ToneRow(0f, b), ValueMultiplier = b < 0.18f ? 0.6f : 0.9f };
        }

        var row = ToneRow(s, b);
        var noun = HueName(h);
        var word = RowWords[row];

        var neon = h >= 190f && h <= 212f && s >= 0.60f && b >= 0.85f;
        var pinkFamily = (h >= 295f && h < 335f && b >= 0.80f) || ((h >= 335f || h < 12f) && s < 0.70f && b >= 0.90f);
        var warm = h >= 18f && h < 66f;

        if (neon)
        {
            noun = "bleu néon";
            word = s >= 0.9f ? "vif" : (s >= 0.75f ? "intense" : "");
        }
        else if (pinkFamily)
        {
            noun = h >= 295f && h < 315f ? "rose violacé" : "rose";
        }
        else if (h >= 12f && h < 40f && s < 0.70f && b >= 0.90f)
        {
            noun = s < 0.55f ? "rose orangé" : "orange rosé";
        }
        else if (warm && b < 0.85f && s >= 0.35f)
        {
            // Warm colors turn brown as they darken or weaken.
            noun = b >= 0.7f ? noun + " brunâtre" : "brun";
        }
        else if (s < 0.5f && b < 0.85f && !(h >= 335f || h < 18f))
        {
            noun += " grisâtre";
        }

        var tone = string.IsNullOrEmpty(word) ? "Moyen" : char.ToUpperInvariant(word[0]) + word.Substring(1);
        string name;
        if (fancyDiamond) name = Capitalize(noun) + " " + FancyWords[row];
        else name = Capitalize(string.IsNullOrEmpty(word) ? noun : noun + " " + word);

        var multiplier = RowValue[row] * (b < 0.85f && s < 0.55f ? 0.75f : 1f);
        return new GemColorGrade { Name = name, Tone = tone, Row = row, ValueMultiplier = multiplier };
    }

    private static string Capitalize(string text)
    {
        return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    // ---- Facet colors ---------------------------------------------------------------------------------------------

    public static Color BodyColor(GemColorValue color) => color.ToColor();

    // The lighter tone of the stone, used for its highlights.
    public static Color FacetColor(GemColorValue color)
    {
        return Color.HSVToRGB(color.Hue / 360f, color.Saturation * 0.62f, Mathf.Min(1f, color.Brightness * 1.12f + 0.14f));
    }

    // ---- Trade names (rare, color-based) ----------------------------------------------------------------------------

    private static bool InHue(float hue, float from, float to)
    {
        hue = Mathf.Repeat(hue, 360f);
        from = Mathf.Repeat(from, 360f);
        to = Mathf.Repeat(to, 360f);
        return from <= to ? hue >= from && hue <= to : hue >= from || hue <= to;
    }

    // The trade name a stone's color qualifies for (null when none). The labs also require a clean, untreated stone:
    // the caller checks that.
    public static string TradeName(string speciesName, GemColorValue c)
    {
        var h = c.Hue;
        var s = c.Saturation;
        var b = c.Brightness;
        switch (speciesName)
        {
            case "Corindon_Rubis":
                if (InHue(h, 345f, 15f) && s >= 0.80f && b >= 0.80f) return "Sang de pigeon";
                break;
            case "Saphir_Bleu":
                if (h >= 220f && h <= 265f && s >= 0.80f && b >= 0.60f) return "Bleu royal";
                if (h >= 215f && h <= 235f && s >= 0.50f && s <= 0.75f && b >= 0.85f) return "Cornflower (bleu bleuet)";
                break;
            case "Padparadscha":
            case "Saphir_Rose":
            case "Saphir_Orange":
                if (InHue(h, 0f, 40f) && s >= 0.20f && s <= 0.60f && b >= 0.95f) return "Padparadscha";
                break;
            case "Spinelle":
                if (InHue(h, 338f, 355f) && s >= 0.88f && b >= 0.90f) return "Jedi (rose néon)";
                if (InHue(h, 0f, 40f) && s >= 0.20f && s <= 0.60f && b >= 0.95f) return "Padparadscha";
                break;
            case "Tourmaline_Paraiba":
                if (h >= 195f && h <= 215f && s >= 0.60f && b >= 0.85f) return "Paraïba (bleu néon)";
                break;
            case "Tourmaline_Rose":
                if (InHue(h, 335f, 360f) && s >= 0.80f && b >= 0.55f && b <= 0.90f) return "Rubellite";
                break;
            case "Emeraude":
                if (h >= 140f && h <= 165f && s >= 0.88f && b >= 0.55f && b <= 0.82f) return "Vert Muzo";
                break;
            case "Topaze":
                if (h >= 188f && h <= 208f && s >= 0.45f && s <= 0.60f && b >= 0.82f) return "Swiss blue";
                if (h >= 192f && h <= 212f && s >= 0.62f && s <= 0.85f && b >= 0.42f && b <= 0.68f) return "London blue";
                if (h >= 190f && h <= 215f && s >= 0.18f && s <= 0.40f && b >= 0.94f) return "Sky blue";
                if (h >= 12f && h <= 32f && s >= 0.55f && s <= 0.78f && b >= 0.85f) return "Topaze impériale";
                break;
        }
        return null;
    }

    public static float TradeMultiplier(string tradeName)
    {
        switch (tradeName)
        {
            case "Sang de pigeon": return 4.0f;
            case "Bleu royal": return 2.5f;
            case "Cornflower (bleu bleuet)": return 2.0f;
            case "Padparadscha": return 1.8f;
            case "Paraïba (bleu néon)": return 3.0f;
            case "Jedi (rose néon)": return 3.0f;
            case "Rubellite": return 1.5f;
            case "Vert Muzo": return 2.0f;
            case "Topaze impériale": return 3.0f;
            case "London blue": return 1.5f;
            case "Swiss blue": return 1.3f;
            case "Sky blue": return 1.1f;
            default: return 1f;
        }
    }

    // Rarity of colored diamonds by hue (a pink or blue diamond is worth far more than a yellow one).
    public static float FancyDiamondRarity(GemColorValue c)
    {
        var h = c.Hue;
        if (c.Saturation < 0.18f) return 3f;                    // gray
        if (c.Brightness < 0.72f && h >= 18f && h < 66f) return 2f;             // brown
        if (InHue(h, 350f, 8f)) return 60f;                      // red
        if (InHue(h, 300f, 350f)) return 25f;                    // pink, purple-pink
        if (h >= 195f && h <= 255f) return 40f;                  // blue
        if (h >= 100f && h <= 180f) return 22f;                  // green
        if (h >= 255f && h < 300f) return 28f;                   // violet, purple
        if (h >= 18f && h < 40f) return 9f;                      // orange
        return 5f;                                               // yellow, orange-yellow
    }

    // ---- Sampling -------------------------------------------------------------------------------------------------

    private struct Mode
    {
        public float Weight, HueMin, HueMax, SatMin, SatMax, BrightMin, BrightMax;
    }

    private static Mode M(float weight, float hue0, float hue1, float sat0, float sat1, float bright0, float bright1)
    {
        return new Mode { Weight = weight, HueMin = hue0, HueMax = hue1, SatMin = sat0, SatMax = sat1, BrightMin = bright0, BrightMax = bright1 };
    }

    private static readonly Dictionary<string, Mode[]> Profiles = new Dictionary<string, Mode[]>();

    private static readonly Mode[] FancyDiamondModes =
    {
        M(0.30f, 48f, 60f, 0.45f, 0.95f, 0.78f, 1.00f),     // yellow
        M(0.09f, 38f, 48f, 0.60f, 0.95f, 0.82f, 1.00f),     // orange-yellow
        M(0.18f, 25f, 45f, 0.45f, 0.80f, 0.35f, 0.62f),     // brown
        M(0.05f, 22f, 36f, 0.70f, 1.00f, 0.75f, 0.95f),     // orange
        M(0.08f, 335f, 356f, 0.25f, 0.65f, 0.88f, 1.00f),   // pink
        M(0.03f, 315f, 335f, 0.35f, 0.70f, 0.70f, 0.90f),   // purplish pink
        M(0.05f, 208f, 230f, 0.30f, 0.55f, 0.45f, 0.80f),   // blue
        M(0.05f, 130f, 165f, 0.30f, 0.60f, 0.55f, 0.85f),   // green
        M(0.03f, 70f, 95f, 0.50f, 0.80f, 0.70f, 0.90f),     // yellow-green
        M(0.05f, 200f, 260f, 0.12f, 0.20f, 0.40f, 0.70f),   // gray
        M(0.02f, 265f, 290f, 0.30f, 0.60f, 0.40f, 0.70f),   // violet
        M(0.01f, 352f, 364f, 0.70f, 0.95f, 0.40f, 0.65f),   // red
    };

    public static GemColorValue SampleFancyDiamond(Func<float> random)
    {
        return Pick(FancyDiamondModes, random);
    }

    public static GemColorValue Sample(GemDefinition definition, Func<float> random)
    {
        return Pick(ProfileFor(definition), random);
    }

    private static GemColorValue Pick(Mode[] modes, Func<float> random)
    {
        var total = 0f;
        foreach (var mode in modes) total += mode.Weight;

        var roll = random() * total;
        var chosen = modes[modes.Length - 1];
        foreach (var mode in modes)
        {
            roll -= mode.Weight;
            if (roll <= 0f)
            {
                chosen = mode;
                break;
            }
        }

        return new GemColorValue(
            Mathf.Lerp(chosen.HueMin, chosen.HueMax, random()),
            Mathf.Lerp(chosen.SatMin, chosen.SatMax, random()),
            Mathf.Lerp(chosen.BrightMin, chosen.BrightMax, random()));
    }

    private static Mode[] ProfileFor(GemDefinition definition)
    {
        var key = definition.name;
        if (Profiles.TryGetValue(key, out var cached)) return cached;

        var profile = Overrides(key) ?? Derived(definition);
        Profiles[key] = profile;
        return profile;
    }

    // Species whose color range matters: several characteristic color modes with their own rarity.
    private static Mode[] Overrides(string name)
    {
        switch (name)
        {
            case "Corindon_Rubis":
                return new[]
                {
                    M(0.03f, 352f, 366f, 0.92f, 1.00f, 0.88f, 1.00f),    // sang de pigeon core
                    M(0.20f, 350f, 368f, 0.88f, 1.00f, 0.72f, 0.88f),    // vivid red
                    M(0.18f, 344f, 364f, 0.64f, 0.86f, 0.88f, 1.00f),    // intense / pinkish red
                    M(0.20f, 350f, 372f, 0.90f, 1.00f, 0.50f, 0.68f),    // deep red
                    M(0.09f, 356f, 376f, 0.85f, 1.00f, 0.30f, 0.46f),    // dark red
                    M(0.09f, 340f, 358f, 0.38f, 0.60f, 0.92f, 1.00f),    // pink ruby
                    M(0.10f, 326f, 343f, 0.78f, 1.00f, 0.55f, 0.80f),    // purplish red
                    M(0.07f, 8f, 20f, 0.85f, 1.00f, 0.72f, 0.90f),       // orangy red
                    M(0.04f, 345f, 358f, 0.22f, 0.36f, 0.95f, 1.00f),    // very light pink
                };
            case "Saphir_Bleu":
                return new[]
                {
                    M(0.08f, 225f, 255f, 0.85f, 1.00f, 0.68f, 0.95f),    // royal blue
                    M(0.06f, 216f, 234f, 0.52f, 0.70f, 0.86f, 0.97f),    // cornflower
                    M(0.14f, 215f, 250f, 0.90f, 1.00f, 0.78f, 0.95f),    // vivid blue
                    M(0.17f, 205f, 240f, 0.70f, 0.86f, 0.80f, 1.00f),    // intense blue
                    M(0.15f, 205f, 235f, 0.50f, 0.68f, 0.88f, 1.00f),    // medium blue
                    M(0.09f, 195f, 225f, 0.22f, 0.40f, 0.95f, 1.00f),    // light blue
                    M(0.07f, 200f, 230f, 0.40f, 0.50f, 0.93f, 1.00f),    // pastel blue
                    M(0.10f, 225f, 262f, 0.90f, 1.00f, 0.50f, 0.68f),    // deep blue
                    M(0.08f, 225f, 262f, 0.88f, 1.00f, 0.30f, 0.46f),    // dark blue ("inky")
                    M(0.03f, 185f, 200f, 0.65f, 0.90f, 0.70f, 0.90f),    // greenish blue
                };
            case "Padparadscha":
                return new[]
                {
                    M(0.34f, 6f, 24f, 0.36f, 0.58f, 0.96f, 1.00f),       // padparadscha
                    M(0.14f, 6f, 24f, 0.20f, 0.34f, 0.96f, 1.00f),       // light orangy pink
                    M(0.14f, 0f, 14f, 0.40f, 0.58f, 0.92f, 1.00f),       // pinker
                    M(0.12f, 24f, 38f, 0.46f, 0.66f, 0.92f, 1.00f),      // pinkish orange
                    M(0.10f, 12f, 24f, 0.60f, 0.78f, 0.88f, 1.00f),      // intense orangy pink
                    M(0.10f, 12f, 26f, 0.70f, 0.90f, 0.78f, 0.90f),      // orangy red
                    M(0.06f, 14f, 28f, 0.62f, 0.80f, 0.55f, 0.70f),      // deep orangy
                };
            case "Spinelle":
                return new[]
                {
                    M(0.03f, 338f, 354f, 0.88f, 1.00f, 0.92f, 1.00f),    // jedi (neon pink-red)
                    M(0.02f, 8f, 22f, 0.38f, 0.58f, 0.97f, 1.00f),       // padparadscha spinel
                    M(0.18f, 352f, 370f, 0.85f, 1.00f, 0.72f, 0.90f),    // vivid red
                    M(0.16f, 345f, 365f, 0.62f, 0.84f, 0.88f, 1.00f),    // intense / pinkish red
                    M(0.14f, 350f, 372f, 0.90f, 1.00f, 0.50f, 0.68f),    // deep red
                    M(0.08f, 356f, 376f, 0.85f, 1.00f, 0.30f, 0.46f),    // dark red
                    M(0.15f, 325f, 345f, 0.45f, 0.70f, 0.90f, 1.00f),    // pink
                    M(0.08f, 330f, 350f, 0.20f, 0.40f, 0.95f, 1.00f),    // light pink
                    M(0.06f, 310f, 330f, 0.60f, 0.85f, 0.75f, 0.95f),    // purplish pink
                    M(0.10f, 5f, 18f, 0.75f, 0.95f, 0.75f, 0.90f),       // orangy red
                };
            case "Emeraude":
                return new[]
                {
                    M(0.07f, 146f, 160f, 0.88f, 1.00f, 0.58f, 0.80f),    // Muzo green
                    M(0.20f, 135f, 160f, 0.85f, 1.00f, 0.72f, 0.90f),    // vivid green
                    M(0.20f, 130f, 160f, 0.62f, 0.84f, 0.78f, 0.95f),    // intense / medium green
                    M(0.14f, 125f, 155f, 0.28f, 0.50f, 0.94f, 1.00f),    // light green
                    M(0.12f, 160f, 172f, 0.70f, 0.95f, 0.65f, 0.90f),    // bluish green
                    M(0.13f, 105f, 130f, 0.65f, 0.90f, 0.70f, 0.90f),    // yellowish green
                    M(0.08f, 140f, 165f, 0.88f, 1.00f, 0.38f, 0.58f),    // deep green
                    M(0.06f, 150f, 170f, 0.40f, 0.60f, 0.85f, 1.00f),    // pastel green
                };
            case "Tourmaline_Rose":
                return new[]
                {
                    M(0.10f, 344f, 358f, 0.80f, 1.00f, 0.58f, 0.84f),    // rubellite
                    M(0.18f, 318f, 344f, 0.86f, 1.00f, 0.86f, 1.00f),    // vivid pink
                    M(0.18f, 320f, 350f, 0.68f, 0.84f, 0.88f, 1.00f),    // intense pink
                    M(0.20f, 322f, 350f, 0.50f, 0.66f, 0.92f, 1.00f),    // pink
                    M(0.14f, 322f, 350f, 0.30f, 0.48f, 0.95f, 1.00f),    // pastel pink
                    M(0.08f, 325f, 350f, 0.18f, 0.28f, 0.97f, 1.00f),    // light pink
                    M(0.06f, 300f, 322f, 0.55f, 0.80f, 0.80f, 0.95f),    // purplish
                    M(0.06f, 350f, 372f, 0.70f, 0.90f, 0.40f, 0.60f),    // dark red-pink
                };
            case "Tourmaline_Paraiba":
                return new[]
                {
                    M(0.12f, 196f, 208f, 0.92f, 1.00f, 0.90f, 1.00f),    // vivid neon blue
                    M(0.20f, 196f, 210f, 0.78f, 0.90f, 0.88f, 1.00f),    // intense neon blue
                    M(0.22f, 194f, 212f, 0.60f, 0.76f, 0.88f, 1.00f),    // neon blue
                    M(0.14f, 176f, 194f, 0.60f, 0.90f, 0.80f, 1.00f),    // greenish blue
                    M(0.10f, 165f, 176f, 0.55f, 0.85f, 0.75f, 0.95f),    // bluish green
                    M(0.06f, 214f, 235f, 0.50f, 0.80f, 0.80f, 0.95f),    // blue
                    M(0.06f, 250f, 285f, 0.40f, 0.70f, 0.75f, 0.95f),    // violet / purple
                    M(0.10f, 185f, 200f, 0.20f, 0.40f, 0.95f, 1.00f),    // light greenish blue
                };
            case "Topaze":
                return new[]
                {
                    M(0.14f, 188f, 205f, 0.44f, 0.58f, 0.86f, 1.00f),    // Swiss blue
                    M(0.09f, 192f, 212f, 0.62f, 0.84f, 0.44f, 0.66f),    // London blue
                    M(0.16f, 190f, 214f, 0.18f, 0.38f, 0.95f, 1.00f),    // sky blue
                    M(0.05f, 12f, 32f, 0.56f, 0.78f, 0.86f, 1.00f),      // imperial
                    M(0.07f, 330f, 355f, 0.20f, 0.50f, 0.90f, 1.00f),    // pink
                    M(0.20f, 200f, 220f, 0.00f, 0.05f, 0.90f, 0.97f),    // colorless
                    M(0.10f, 38f, 50f, 0.40f, 0.70f, 0.78f, 0.95f),      // golden / sherry
                    M(0.10f, 20f, 40f, 0.35f, 0.60f, 0.65f, 0.82f),      // brownish orange
                    M(0.09f, 205f, 225f, 0.50f, 0.70f, 0.50f, 0.70f),    // deeper blue
                };
            default:
                return null;
        }
    }

    // Every other species: colors around the species' typical color, from light to dark, with a few percent of
    // each neighbouring tone / saturation step.
    private static Mode[] Derived(GemDefinition definition)
    {
        Color.RGBToHSV(definition.iconColor, out var h, out var s, out var v);
        var hue = h * 360f;

        // Colorless, white or nearly neutral materials: only subtle variation.
        if (s < 0.25f)
        {
            if (v < 0.5f)
                return new[] { M(1f, hue - 10f, hue + 10f, 0f, Mathf.Max(0.05f, s * 1.2f), Mathf.Max(0.04f, v * 0.8f), Mathf.Min(1f, v * 1.2f + 0.04f)) };
            return new[] { M(1f, hue - 8f, hue + 8f, s * 0.6f, s * 1.4f, Mathf.Max(0f, v - 0.07f), Mathf.Min(1f, v + 0.05f)) };
        }

        // Nearest tone/saturation row to the species' typical color.
        var center = 0;
        var best = float.MaxValue;
        for (var row = 0; row < RowSaturation.Length; row++)
        {
            var d = (s - RowSaturation[row]) * (s - RowSaturation[row]) + (v - RowBrightness[row]) * (v - RowBrightness[row]);
            if (d < best)
            {
                best = d;
                center = row;
            }
        }

        var offsets = new[] { -3, -2, -1, 0, 1, 2, 3 };
        var weights = new[] { 0.03f, 0.09f, 0.21f, 0.34f, 0.21f, 0.09f, 0.03f };
        var modes = new List<Mode>();
        for (var i = 0; i < offsets.Length; i++)
        {
            var row = center + offsets[i];
            if (row < 0 || row >= RowSaturation.Length) continue;
            modes.Add(M(weights[i], hue - 6f, hue + 6f,
                RowSaturation[row] - 0.10f, Mathf.Min(1f, RowSaturation[row] + 0.07f),
                RowBrightness[row] - 0.10f, Mathf.Min(1f, RowBrightness[row] + 0.07f)));
        }
        return modes.ToArray();
    }
}
