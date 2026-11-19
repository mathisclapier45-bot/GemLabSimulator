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

    // For the books: the colour, the name and the price factor of a row of the Bellerophon grids (0 = light .. 7 = dark).
    public static int RowCount => RowSaturation.Length;

    public static UnityEngine.Color RowColor(float hue, int row)
    {
        row = Mathf.Clamp(row, 0, RowSaturation.Length - 1);
        return Color.HSVToRGB(Mathf.Repeat(hue, 360f) / 360f, RowSaturation[row], RowBrightness[row]);
    }

    public static float RowSaturationOf(int row) => RowSaturation[Mathf.Clamp(row, 0, RowSaturation.Length - 1)];

    public static float RowBrightnessOf(int row) => RowBrightness[Mathf.Clamp(row, 0, RowBrightness.Length - 1)];

    public static float RowValueOf(int row) => RowValue[Mathf.Clamp(row, 0, RowValue.Length - 1)];

    public static string RowLabel(int row)
    {
        switch (row)
        {
            case 0: return "Clair";
            case 1: return "Pastel";
            case 2: return "Moyen";
            case 3: return "Intense";
            case 4: return "Vif";
            case 5: return "Vif foncé";
            case 6: return "Profond";
            default: return "Sombre";
        }
    }

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

        // Neutral: colorless, gray, black. A coloured diamond of the faintest grade is faintly tinted, not neutral.
        if (s < 0.10f && !(fancyDiamond && s >= 0.04f))
        {
            var neutral = b < 0.18f ? "Noir" : (b < 0.72f ? "Gris" : "Incolore");
            return new GemColorGrade { Name = neutral, Tone = neutral, Row = ToneRow(0f, b), ValueMultiplier = b < 0.18f ? 0.6f : 0.9f };
        }

        // A coloured diamond is named after the GIA chart: its hue among the 27, its grade among the nine.
        if (fancyDiamond)
        {
            var gia = GiaDiamondColors.Describe(color);
            return new GemColorGrade { Name = gia.Name, Tone = GiaDiamondColors.English(gia.Grade), Row = ToneRow(s, b), ValueMultiplier = gia.ValueMultiplier };
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
                if (InHue(h, 345f, 15f) && s >= 0.80f && b >= 0.70f) return "Sang de pigeon";
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
                if (h >= 128f && h <= 156f && s >= 0.88f && b >= 0.55f && b <= 0.82f) return "Vert Muzo";
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
            case "Sang de pigeon": return 6.0f;
            case "Bleu royal": return 3.5f;
            case "Cornflower (bleu bleuet)": return 2.5f;
            case "Padparadscha": return 3.0f;
            case "Paraïba (bleu néon)": return 5.0f;
            case "Jedi (rose néon)": return 3.0f;
            case "Rubellite": return 1.5f;
            case "Vert Muzo": return 3.0f;
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
        return GiaDiamondColors.Describe(c).Rarity;
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

    public static GemColorValue SampleFancyDiamond(Func<float> random)
    {
        return GiaDiamondColors.Sample(random);
    }

    public static GemColorValue Sample(GemDefinition definition, Func<float> random, bool synthetic = false)
    {
        return Pick(ProfileFor(definition, synthetic), random);
    }

    // The deposit pulls the colour of a natural stone: a Kashmir sapphire is a velvety cornflower blue, a Ceylon one lighter, an Australian or Thai one dark and inky.
    // dark / light multiply the weight of the dark modes (low brightness) and of the light ones (low saturation, bright); `favoured` multiplies the modes whose hue
    // falls in [hueMin, hueMax]. The first rule whose species (start of the name) and deposit (part of its name, lower case) match is used.
    private static readonly (string species, string origin, float dark, float light, float hueMin, float hueMax, float favoured)[] OriginColour =
    {
        ("Saphir_Bleu", "cachemire", 0.15f, 0.40f, 212f, 238f, 3.0f),
        ("Saphir_Bleu", "sri lanka", 0.30f, 1.80f, 0f, 0f, 1f),
        ("Saphir_Bleu", "australie", 3.50f, 0.25f, 180f, 205f, 2.5f),
        ("Saphir_Bleu", "madagascar", 1.00f, 1.00f, 0f, 0f, 1f),
        ("Saphir_", "australie", 2.50f, 0.40f, 0f, 0f, 1f),
        ("Saphir_", "thaïlande", 2.00f, 0.50f, 0f, 0f, 1f),
        ("Saphir_", "nigeria", 1.80f, 0.60f, 0f, 0f, 1f),
        ("Saphir_", "sri lanka", 0.40f, 1.60f, 0f, 0f, 1f),
    };

    public static GemColorValue Sample(GemDefinition definition, Func<float> random, bool synthetic, GemOrigin origin)
    {
        var modes = ProfileFor(definition, synthetic);
        if (synthetic || origin == null || string.IsNullOrEmpty(origin.name) || definition == null || definition.name == null) return Pick(modes, random);

        var place = origin.name.ToLowerInvariant();
        foreach (var rule in OriginColour)
        {
            if (!definition.name.StartsWith(rule.species) || !place.Contains(rule.origin)) continue;
            var pulled = new Mode[modes.Length];
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                var sat = (mode.SatMin + mode.SatMax) * 0.5f;
                var bright = (mode.BrightMin + mode.BrightMax) * 0.5f;
                var hue = Mathf.Repeat((mode.HueMin + mode.HueMax) * 0.5f, 360f);
                var dark = bright < 0.62f;
                var light = sat < 0.45f && bright >= 0.85f;
                var factor = dark ? rule.dark : light ? rule.light : 1f;
                if (!dark && !light && rule.hueMax > rule.hueMin && hue >= rule.hueMin && hue <= rule.hueMax) factor *= rule.favoured;
                mode.Weight *= factor;
                pulled[i] = mode;
            }
            return Pick(pulled, random);
        }
        return Pick(modes, random);
    }

    // The materials that are grown in every colour: the synthetic corundums and spinels, YAG, GGG and cubic zirconia.
    public static bool GrownInAnyColor(GemDefinition definition, bool synthetic)
    {
        if (definition == null) return false;
        var name = definition.name ?? "";
        if (name == "Corindon_Synthetique_Rubis") return false;               // a synthetic ruby keeps the colours of a ruby
        if (name == "YAG" || name == "GGG" || name == "CZ" || name == "Titanate_Strontium" || name.StartsWith("Corindon_Synthetique")) return true;
        if (!synthetic && !definition.forcedSynthetic) return false;
        return name.StartsWith("Saphir_") || name == "Padparadscha" || name.StartsWith("Spinelle");
    }

    // The palette of a synthetic stone grown in every colour: every hue of the circle, light to dark (and a few colourless ones).
    private static Mode[] anyColorProfile;

    private static Mode[] AnyColorProfile()
    {
        if (anyColorProfile != null) return anyColorProfile;
        var rows = RowSaturation.Length;
        var rowWeight = new[] { 0.8f, 1.0f, 1.2f, 1.2f, 1.2f, 1.0f, 0.8f, 0.5f };
        var modes = new List<Mode>();
        const int bands = 24;
        for (var band = 0; band < bands; band++)
        {
            var hue = band * 360f / bands;
            for (var row = 0; row < rows; row++)
                modes.Add(M(rowWeight[row], hue - 7.5f, hue + 7.5f, Mathf.Clamp01(RowSaturation[row] - 0.10f), Mathf.Clamp01(RowSaturation[row] + 0.07f),
                    Mathf.Clamp01(RowBrightness[row] - 0.10f), Mathf.Clamp01(RowBrightness[row] + 0.07f)));
        }
        // A share of colourless and nearly colourless stones.
        modes.Add(M(bands * 0.8f, 0f, 360f, 0f, 0.05f, 0.92f, 1f));
        anyColorProfile = modes.ToArray();
        return anyColorProfile;
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

    // ---- For the encyclopedia: every colour a species can have ----------------------------------------------------------------

    public struct PaletteCell
    {
        public GemColorValue Color;
        public float Share;           // how often a stone of the species has this colour, among all its colours (0..1)
        public int Row;               // the row of the grid of the laboratory, 0 (light) .. 7 (dark)
        public int Step;              // the column: the position in the window of hues of the species
    }

    // The colours the palette of a species can give, on a grid: its window of hues cut in steps, and the eight rows of the grid of the laboratory. The share is how
    // likely the colour is for that species (the characteristic and the cloud modes together).
    public static List<PaletteCell> PaletteCells(GemDefinition definition, int hueSteps = 14, bool synthetic = false)
    {
        var cells = new List<PaletteCell>();
        if (definition == null) return cells;
        var modes = ProfileFor(definition, synthetic);
        if (modes == null || modes.Length == 0) return cells;

        // The window of hues: around the circular mean, from the lowest to the highest edge of the modes.
        float sumX = 0f, sumY = 0f, total = 0f;
        foreach (var mode in modes)
        {
            var radians = (mode.HueMin + mode.HueMax) * 0.5f * Mathf.Deg2Rad;
            sumX += mode.Weight * Mathf.Cos(radians);
            sumY += mode.Weight * Mathf.Sin(radians);
            total += mode.Weight;
        }
        var mean = Mathf.Repeat(Mathf.Atan2(sumY, sumX) * Mathf.Rad2Deg, 360f);
        float lo = 0f, hi = 0f;
        foreach (var mode in modes)
        {
            var centre = Mathf.DeltaAngle(mean, (mode.HueMin + mode.HueMax) * 0.5f);
            var half = (mode.HueMax - mode.HueMin) * 0.5f;
            lo = Mathf.Min(lo, centre - half);
            hi = Mathf.Max(hi, centre + half);
        }
        if (hi - lo < 12f) { lo -= 6f; hi += 6f; }

        float weightSum = 0f;
        for (var step = 0; step < hueSteps; step++)
        {
            var offset = Mathf.Lerp(lo, hi, (step + 0.5f) / hueSteps);
            var hue = Mathf.Repeat(mean + offset, 360f);
            for (var row = 0; row < RowSaturation.Length; row++)
            {
                var saturation = RowSaturation[row];
                var brightness = RowBrightness[row];
                var weight = 0f;
                foreach (var mode in modes)
                {
                    var centre = Mathf.DeltaAngle(mean, (mode.HueMin + mode.HueMax) * 0.5f);
                    var half = (mode.HueMax - mode.HueMin) * 0.5f + (hi - lo) / hueSteps * 0.5f;
                    if (Mathf.Abs(offset - centre) > half) continue;
                    if (saturation < mode.SatMin - 0.06f || saturation > mode.SatMax + 0.06f) continue;
                    if (brightness < mode.BrightMin - 0.06f || brightness > mode.BrightMax + 0.06f) continue;
                    weight += mode.Weight;
                }
                if (weight <= 0f) continue;
                cells.Add(new PaletteCell { Color = new GemColorValue(hue, saturation, brightness), Share = weight, Row = row, Step = step });
                weightSum += weight;
            }
        }

        // A species with a narrow or neutral palette (a colourless stone) may have no cell on the grid: its own modes stand in.
        if (cells.Count == 0)
            foreach (var mode in modes)
            {
                cells.Add(new PaletteCell
                {
                    Color = new GemColorValue((mode.HueMin + mode.HueMax) * 0.5f, (mode.SatMin + mode.SatMax) * 0.5f, (mode.BrightMin + mode.BrightMax) * 0.5f),
                    Share = mode.Weight, Row = ToneRow((mode.SatMin + mode.SatMax) * 0.5f, (mode.BrightMin + mode.BrightMax) * 0.5f), Step = cells.Count,
                });
                weightSum += mode.Weight;
            }

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            cell.Share = weightSum > 0f ? cell.Share / weightSum : 0f;
            cells[i] = cell;
        }
        // From the hue window, then from the light rows to the dark ones.
        return cells;
    }

    // What a species can really look like: the window of hues (degrees, from the first to the second going clockwise), then the least and the most saturated and the
    // darkest and the lightest it reaches. Whatever the palette draws (the characteristic colours and the cloud around them), nothing falls outside this window.
    // { hue from, hue to, saturation min, saturation max, brightness min, brightness max }
    private static readonly Dictionary<string, float[]> Limits = new Dictionary<string, float[]>
    {
        // Corundum.
        { "Corindon_Rubis", new[] { 340f, 15f, 0.35f, 1.00f, 0.28f, 1.00f } },
        { "Saphir_Rose", new[] { 325f, 5f, 0.18f, 0.90f, 0.50f, 1.00f } },
        { "Padparadscha", new[] { 0f, 40f, 0.20f, 0.90f, 0.50f, 1.00f } },
        { "Saphir_Orange", new[] { 10f, 42f, 0.22f, 1.00f, 0.50f, 1.00f } },
        { "Saphir_Jaune", new[] { 38f, 75f, 0.20f, 1.00f, 0.50f, 1.00f } },
        { "Saphir_Vert", new[] { 70f, 180f, 0.25f, 1.00f, 0.22f, 0.95f } },
        { "Saphir_Bleu", new[] { 185f, 265f, 0.20f, 1.00f, 0.28f, 1.00f } },
        { "Saphir_Violet", new[] { 250f, 315f, 0.20f, 1.00f, 0.35f, 1.00f } },
        // Beryl.
        { "Emeraude", new[] { 100f, 175f, 0.25f, 1.00f, 0.30f, 0.95f } },
        { "Aigue_Marine", new[] { 170f, 230f, 0.10f, 0.80f, 0.55f, 1.00f } },
        { "Beryl_Bleu", new[] { 205f, 245f, 0.50f, 1.00f, 0.25f, 0.90f } },
        { "Heliodore", new[] { 42f, 78f, 0.15f, 0.95f, 0.70f, 1.00f } },
        { "Morganite", new[] { 335f, 30f, 0.12f, 0.70f, 0.85f, 1.00f } },
        { "Beryl_Rouge", new[] { 330f, 15f, 0.40f, 0.95f, 0.30f, 0.90f } },
        { "Pezzottaite", new[] { 325f, 20f, 0.25f, 0.90f, 0.40f, 1.00f } },
        // Garnets.
        { "Grenat_Hessonite", new[] { 18f, 48f, 0.40f, 0.95f, 0.40f, 0.90f } },
        { "Grenat_Almandin", new[] { 335f, 15f, 0.60f, 1.00f, 0.20f, 0.60f } },
        { "Grenat_Pyrope", new[] { 345f, 15f, 0.60f, 1.00f, 0.25f, 0.70f } },
        { "Grenat_Rhodolite", new[] { 320f, 355f, 0.40f, 0.90f, 0.35f, 0.80f } },
        { "Grenat_Spessartite", new[] { 12f, 40f, 0.60f, 1.00f, 0.50f, 1.00f } },
        { "Grenat_Malaya", new[] { 5f, 35f, 0.40f, 0.95f, 0.50f, 0.95f } },
        { "Grenat_Tsavorite", new[] { 110f, 150f, 0.50f, 1.00f, 0.30f, 0.85f } },
        { "Grenat_Demantoide", new[] { 70f, 140f, 0.45f, 1.00f, 0.40f, 0.95f } },
        { "Grenat_Mali", new[] { 55f, 95f, 0.45f, 0.95f, 0.40f, 0.85f } },
        { "Grenat_Topazolite", new[] { 42f, 70f, 0.55f, 1.00f, 0.60f, 1.00f } },
        { "Grenat_Pyrope_Chromifere", new[] { 350f, 18f, 0.70f, 1.00f, 0.40f, 0.90f } },
        { "Grenat_Change_Couleur", new[] { 165f, 205f, 0.30f, 0.80f, 0.30f, 0.75f } },
        { "Grenat_Hydrogrossulaire", new[] { 90f, 150f, 0.30f, 0.80f, 0.35f, 0.85f } },
        { "Grenat_Uvarovite", new[] { 125f, 160f, 0.60f, 1.00f, 0.25f, 0.70f } },
        // <new-species-2026-11-20>
        { "Amazonite", new[] { 150f, 190f, 0.25f, 0.7f, 0.45f, 0.85f } },
        { "Pierre_Soleil", new[] { 10f, 40f, 0.45f, 0.95f, 0.5f, 0.95f } },
        { "Hiddenite", new[] { 80f, 160f, 0.25f, 0.9f, 0.4f, 0.95f } },
        { "Tourmaline_Verdelite", new[] { 100f, 165f, 0.45f, 1f, 0.25f, 0.8f } },
        { "Tourmaline_Dravite", new[] { 15f, 45f, 0.5f, 1f, 0.25f, 0.7f } },
        { "Aventurine", new[] { 100f, 165f, 0.3f, 0.8f, 0.35f, 0.8f } },
        { "Jaspe", new[] { 0f, 55f, 0.5f, 1f, 0.25f, 0.85f } },
        { "Onyx", new[] { 0f, 360f, 0f, 0.12f, 0.05f, 0.35f } },
        { "Enstatite", new[] { 50f, 120f, 0.3f, 0.9f, 0.2f, 0.75f } },
        { "Larimar", new[] { 185f, 205f, 0.3f, 0.7f, 0.55f, 0.95f } },
        { "Charoite", new[] { 270f, 300f, 0.3f, 0.85f, 0.3f, 0.85f } },
        { "Sugilite", new[] { 280f, 310f, 0.45f, 0.95f, 0.25f, 0.75f } },
        { "Azurite", new[] { 215f, 240f, 0.8f, 1f, 0.25f, 0.65f } },
        { "Chrysocolle", new[] { 165f, 195f, 0.4f, 0.9f, 0.4f, 0.9f } },
        { "Variscite", new[] { 95f, 165f, 0.3f, 0.75f, 0.45f, 0.9f } },
        { "Lepidolite", new[] { 290f, 330f, 0.12f, 0.55f, 0.6f, 0.95f } },
        { "Benitoite", new[] { 215f, 270f, 0.5f, 1f, 0.3f, 0.9f } },
        { "Grandidierite", new[] { 170f, 200f, 0.35f, 0.9f, 0.35f, 0.85f } },
        { "Diaspore_Zultanite", new[] { 55f, 115f, 0.3f, 0.85f, 0.45f, 0.9f } },
        { "Euclase", new[] { 170f, 215f, 0.12f, 0.6f, 0.55f, 1f } },
        { "Danburite", new[] { 40f, 70f, 0f, 0.3f, 0.75f, 1f } },
        { "Kornerupine", new[] { 70f, 130f, 0.3f, 0.85f, 0.25f, 0.75f } },
        // </new-species-2026-11-20>
        // Quartz and other transparent stones.
        { "Amethyste", new[] { 255f, 310f, 0.20f, 0.90f, 0.30f, 0.95f } },
        { "Citrine", new[] { 28f, 62f, 0.25f, 1.00f, 0.50f, 1.00f } },
        { "Quartz_Rose", new[] { 320f, 355f, 0.08f, 0.40f, 0.85f, 1.00f } },
        { "Quartz_Fume", new[] { 20f, 45f, 0.20f, 0.80f, 0.20f, 0.60f } },
        { "Prasiolite", new[] { 80f, 140f, 0.10f, 0.50f, 0.70f, 1.00f } },
        { "Peridot", new[] { 60f, 110f, 0.40f, 1.00f, 0.40f, 0.95f } },
        { "Tanzanite", new[] { 235f, 290f, 0.30f, 1.00f, 0.25f, 0.85f } },
        { "Kunzite", new[] { 300f, 345f, 0.12f, 0.55f, 0.85f, 1.00f } },
        { "Iolite", new[] { 225f, 285f, 0.15f, 0.85f, 0.30f, 0.95f } },
        { "Tourmaline_Paraiba", new[] { 165f, 235f, 0.20f, 1.00f, 0.45f, 1.00f } },
        { "Chrysoberyl_Alexandrite", new[] { 120f, 185f, 0.25f, 0.80f, 0.25f, 0.75f } },
        { "Sphene", new[] { 40f, 95f, 0.35f, 1.00f, 0.45f, 1.00f } },
        { "Taaffeite", new[] { 270f, 325f, 0.10f, 0.60f, 0.60f, 1.00f } },
        { "Sinhalite", new[] { 25f, 55f, 0.35f, 0.85f, 0.35f, 0.75f } },
        { "Diopside_Chromifere", new[] { 115f, 160f, 0.50f, 1.00f, 0.20f, 0.55f } },
        { "Kyanite", new[] { 215f, 245f, 0.30f, 0.80f, 0.35f, 0.90f } },
        // Fluorite.
        { "Tourmaline_Indicolite", new[] { 190f, 245f, 0.30f, 0.95f, 0.30f, 0.90f } },
        { "Tourmaline_Melon_Eau", new[] { 320f, 360f, 0.25f, 0.85f, 0.50f, 1.00f } },
        { "Sphalerite", new[] { 350f, 60f, 0.45f, 1.00f, 0.15f, 0.90f } },
        { "Fluorite", new[] { 262f, 295f, 0.20f, 0.90f, 0.35f, 0.95f } },
        { "Fluorite_Verte", new[] { 100f, 160f, 0.20f, 0.90f, 0.40f, 1.00f } },
        { "Fluorite_Bleue", new[] { 205f, 245f, 0.30f, 0.95f, 0.35f, 1.00f } },
        { "Fluorite_Jaune", new[] { 40f, 65f, 0.20f, 0.90f, 0.55f, 1.00f } },
        // Opaque and translucent stones.
        { "Lapis_Lazuli", new[] { 215f, 245f, 0.60f, 1.00f, 0.25f, 0.65f } },
        { "Turquoise", new[] { 165f, 195f, 0.35f, 0.85f, 0.50f, 0.90f } },
        { "Jade_Jadeite", new[] { 90f, 160f, 0.15f, 0.80f, 0.30f, 0.80f } },
        { "Jade_Nephrite", new[] { 80f, 150f, 0.20f, 0.65f, 0.20f, 0.60f } },
        { "Malachite", new[] { 125f, 160f, 0.50f, 1.00f, 0.20f, 0.70f } },
        { "Sodalite", new[] { 220f, 245f, 0.55f, 1.00f, 0.25f, 0.70f } },
        { "Prehnite", new[] { 70f, 115f, 0.20f, 0.60f, 0.60f, 0.95f } },
        { "Cornaline", new[] { 0f, 22f, 0.70f, 1.00f, 0.45f, 0.90f } },
        { "Moldavite", new[] { 85f, 125f, 0.40f, 0.85f, 0.20f, 0.55f } },
        { "Rhodonite", new[] { 340f, 360f, 0.40f, 0.70f, 0.40f, 0.85f } },
        { "Rhodocrosite", new[] { 340f, 10f, 0.25f, 0.80f, 0.60f, 1.00f } },
    };

    // Cuts the modes of a palette to the window of the species: each mode keeps the part inside, with the weight it keeps; what falls entirely outside is dropped.
    private static Mode[] Restrict(Mode[] modes, string key)
    {
        if (modes == null || !Limits.TryGetValue(key, out var limit)) return modes;

        var width = Mathf.Repeat(limit[1] - limit[0], 360f);
        var centre = limit[0] + width * 0.5f;
        var result = new List<Mode>(modes.Length);
        foreach (var mode in modes)
        {
            var modeCentre = Mathf.DeltaAngle(centre, (mode.HueMin + mode.HueMax) * 0.5f);
            var modeHalf = Mathf.Max(0.5f, (mode.HueMax - mode.HueMin) * 0.5f);
            var lo = Mathf.Max(modeCentre - modeHalf, -width * 0.5f);
            var hi = Mathf.Min(modeCentre + modeHalf, width * 0.5f);
            if (hi <= lo) continue;

            var s0 = Mathf.Max(mode.SatMin, limit[2]);
            var s1 = Mathf.Min(mode.SatMax, limit[3]);
            var b0 = Mathf.Max(mode.BrightMin, limit[4]);
            var b1 = Mathf.Min(mode.BrightMax, limit[5]);
            if (s1 <= s0 || b1 <= b0) continue;

            var fraction = (hi - lo) / (2f * modeHalf)
                           * (s1 - s0) / Mathf.Max(0.001f, mode.SatMax - mode.SatMin)
                           * (b1 - b0) / Mathf.Max(0.001f, mode.BrightMax - mode.BrightMin);
            result.Add(M(mode.Weight * Mathf.Clamp01(fraction), centre + lo, centre + hi, s0, s1, b0, b1));
        }
        return result.Count > 0 ? result.ToArray() : modes;
    }

    private static Mode[] ProfileFor(GemDefinition definition, bool synthetic = false)
    {
        var key = definition.name ?? "";
        var grown = synthetic || definition.forcedSynthetic;

        // A synthetic ruby has the colours of a natural ruby, no other; a synthetic sapphire every colour but those of a ruby (user, 2026-11-17).
        if (key == "Corindon_Synthetique_Rubis" || (key == "Corindon_Rubis" && grown)) return NaturalProfile("Corindon_Rubis", definition);
        if (key == "Corindon_Synthetique_Saphir" || ((key.StartsWith("Saphir_") || key == "Padparadscha") && grown)) return AnyColorExceptRuby();

        // A stone grown in every colour (synthetic spinel, YAG, GGG...): the whole circle of hues, with no limit.
        if (GrownInAnyColor(definition, synthetic)) return AnyColorProfile();

        return NaturalProfile(key, definition);
    }

    // The colour range of a species as nature makes it (its own modes, or modes worked out from its description), within the limits of the species.
    private static Mode[] NaturalProfile(string key, GemDefinition definition)
    {
        if (Profiles.TryGetValue(key, out var cached)) return cached;
        var profile = Overrides(key) ?? Derived(definition);
        profile = Expand(profile);
        profile = Restrict(profile, key);
        Profiles[key] = profile;
        return profile;
    }

    // Every colour of the circle except the reds of a ruby: a strong red (or a strong purplish or orangy red) would be a ruby, not a sapphire. Pinks stay (pink sapphire).
    private static Mode[] anyColorExceptRuby;

    private static Mode[] AnyColorExceptRuby()
    {
        if (anyColorExceptRuby != null) return anyColorExceptRuby;
        var list = new List<Mode>();
        foreach (var mode in AnyColorProfile())
        {
            var hue = Mathf.Repeat((mode.HueMin + mode.HueMax) * 0.5f, 360f);
            var sat = (mode.SatMin + mode.SatMax) * 0.5f;
            var fromRed = Mathf.Abs(Mathf.DeltaAngle(hue, 352f));
            var ruby = mode.HueMax - mode.HueMin < 300f && ((fromRed <= 10f && sat >= 0.5f) || (fromRed <= 25f && sat >= 0.7f));
            if (!ruby) list.Add(mode);
        }
        anyColorExceptRuby = list.ToArray();
        return anyColorExceptRuby;
    }

    // Species whose color range matters: several characteristic color modes with their own rarity.
    private static Mode[] Overrides(string name)
    {
        switch (name)
        {
            case "Corindon_Rubis":
                return new[]
                {
                    M(0.04f, 352f, 366f, 0.88f, 1.00f, 0.70f, 0.88f),    // sang de pigeon core: a deep, pure red
                    M(0.20f, 350f, 368f, 0.88f, 1.00f, 0.72f, 0.88f),    // vivid red
                    M(0.18f, 344f, 364f, 0.64f, 0.86f, 0.88f, 1.00f),    // intense / pinkish red
                    M(0.20f, 350f, 372f, 0.90f, 1.00f, 0.50f, 0.68f),    // deep red
                    M(0.09f, 356f, 376f, 0.85f, 1.00f, 0.30f, 0.46f),    // dark red
                    M(0.09f, 340f, 358f, 0.38f, 0.60f, 0.92f, 1.00f),    // pink ruby
                    M(0.10f, 326f, 343f, 0.78f, 1.00f, 0.55f, 0.80f),    // purplish red
                    M(0.07f, 8f, 20f, 0.85f, 1.00f, 0.72f, 0.90f),       // orangy red
                    M(0.04f, 345f, 358f, 0.22f, 0.36f, 0.95f, 1.00f),    // very light pink
                    M(0.10f, 354f, 372f, 0.55f, 0.78f, 0.34f, 0.52f),    // dull brownish red, the colour of most of the cheap material
                    M(0.06f, 0f, 14f, 0.38f, 0.58f, 0.40f, 0.60f),       // greyish, muddy red
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
            // ---- Corundum: the other sapphires, on the hue ranges of the colour wheels of the laboratory ----
            case "Saphir_Rose":
                return new[]
                {
                    M(0.12f, 335f, 352f, 0.20f, 0.34f, 0.95f, 1.00f),    // light pink
                    M(0.20f, 335f, 354f, 0.34f, 0.50f, 0.92f, 1.00f),    // pastel pink
                    M(0.24f, 335f, 356f, 0.50f, 0.66f, 0.90f, 1.00f),    // pink
                    M(0.16f, 330f, 352f, 0.66f, 0.84f, 0.88f, 1.00f),    // intense pink
                    M(0.08f, 322f, 345f, 0.86f, 1.00f, 0.86f, 1.00f),    // vivid pink
                    M(0.07f, 310f, 332f, 0.40f, 0.70f, 0.78f, 0.96f),    // purplish pink
                    M(0.05f, 340f, 358f, 0.80f, 1.00f, 0.55f, 0.78f),    // deep pink
                    M(0.08f, 4f, 16f, 0.28f, 0.48f, 0.94f, 1.00f),       // orangy pink
                };
            case "Saphir_Orange":
                return new[]
                {
                    M(0.12f, 16f, 34f, 0.24f, 0.40f, 0.95f, 1.00f),      // light orange
                    M(0.18f, 14f, 36f, 0.40f, 0.58f, 0.90f, 1.00f),      // pastel / pinkish orange
                    M(0.24f, 20f, 38f, 0.60f, 0.78f, 0.88f, 1.00f),      // orange
                    M(0.16f, 22f, 38f, 0.80f, 0.92f, 0.88f, 1.00f),      // intense orange
                    M(0.08f, 24f, 38f, 0.92f, 1.00f, 0.90f, 1.00f),      // vivid orange
                    M(0.10f, 18f, 34f, 0.60f, 0.80f, 0.58f, 0.78f),      // deep / brownish orange
                    M(0.06f, 8f, 20f, 0.30f, 0.50f, 0.94f, 1.00f),       // orangy pink
                    M(0.06f, 34f, 46f, 0.60f, 0.85f, 0.85f, 1.00f),      // yellowish orange
                };
            case "Saphir_Jaune":
                return new[]
                {
                    M(0.14f, 46f, 64f, 0.20f, 0.36f, 0.94f, 1.00f),      // light yellow
                    M(0.18f, 44f, 64f, 0.36f, 0.54f, 0.92f, 1.00f),      // pastel yellow
                    M(0.22f, 44f, 62f, 0.54f, 0.74f, 0.90f, 1.00f),      // yellow
                    M(0.16f, 42f, 58f, 0.74f, 0.90f, 0.90f, 1.00f),      // intense yellow
                    M(0.08f, 44f, 58f, 0.90f, 1.00f, 0.92f, 1.00f),      // vivid yellow
                    M(0.08f, 38f, 50f, 0.70f, 0.92f, 0.82f, 0.96f),      // golden
                    M(0.08f, 62f, 76f, 0.40f, 0.72f, 0.82f, 0.98f),      // greenish yellow
                    M(0.06f, 36f, 52f, 0.55f, 0.80f, 0.55f, 0.75f),      // brownish yellow
                };
            case "Saphir_Vert":
                return new[]
                {
                    M(0.12f, 74f, 100f, 0.30f, 0.55f, 0.55f, 0.80f),     // grayish yellowish green
                    M(0.18f, 85f, 125f, 0.45f, 0.72f, 0.50f, 0.78f),     // yellowish green
                    M(0.20f, 110f, 150f, 0.45f, 0.75f, 0.42f, 0.72f),    // green
                    M(0.16f, 150f, 178f, 0.40f, 0.72f, 0.36f, 0.66f),    // bluish green (teal)
                    M(0.10f, 90f, 150f, 0.30f, 0.50f, 0.80f, 0.98f),     // pastel green
                    M(0.12f, 100f, 160f, 0.55f, 0.85f, 0.26f, 0.42f),    // deep / dark green
                    M(0.07f, 100f, 140f, 0.75f, 0.95f, 0.62f, 0.85f),    // intense green
                    M(0.05f, 84f, 110f, 0.20f, 0.34f, 0.92f, 1.00f),     // light green
                };
            case "Saphir_Violet":
                return new[]
                {
                    M(0.14f, 262f, 290f, 0.20f, 0.38f, 0.92f, 1.00f),    // light violet / lavender
                    M(0.18f, 258f, 292f, 0.38f, 0.56f, 0.86f, 0.98f),    // pastel violet
                    M(0.22f, 258f, 285f, 0.56f, 0.74f, 0.76f, 0.94f),    // violet
                    M(0.16f, 262f, 288f, 0.72f, 0.90f, 0.68f, 0.88f),    // intense violet
                    M(0.08f, 262f, 280f, 0.88f, 1.00f, 0.66f, 0.88f),    // vivid violet
                    M(0.08f, 285f, 312f, 0.40f, 0.72f, 0.70f, 0.92f),    // purple
                    M(0.10f, 262f, 285f, 0.82f, 1.00f, 0.38f, 0.58f),    // deep violet
                    M(0.04f, 250f, 262f, 0.55f, 0.85f, 0.68f, 0.90f),    // bluish violet
                };

            // ---- Beryl: aquamarine, maxixe, heliodor, morganite, red beryl and pezzottaite ----
            case "Aigue_Marine":
                return new[]
                {
                    M(0.22f, 185f, 202f, 0.14f, 0.32f, 0.94f, 1.00f),    // light greenish blue
                    M(0.22f, 192f, 214f, 0.30f, 0.48f, 0.90f, 1.00f),    // pastel blue
                    M(0.18f, 198f, 222f, 0.46f, 0.62f, 0.86f, 1.00f),    // blue
                    M(0.12f, 205f, 228f, 0.60f, 0.76f, 0.82f, 0.98f),    // intense blue (Santa Maria)
                    M(0.04f, 208f, 230f, 0.74f, 0.88f, 0.70f, 0.90f),    // vivid blue
                    M(0.04f, 212f, 232f, 0.64f, 0.82f, 0.50f, 0.68f),    // deep blue
                    M(0.10f, 175f, 192f, 0.22f, 0.50f, 0.88f, 1.00f),    // greenish blue
                    M(0.08f, 160f, 176f, 0.20f, 0.42f, 0.86f, 1.00f),    // bluish green
                };
            case "Beryl_Bleu":
                return new[]
                {
                    M(0.30f, 216f, 238f, 0.80f, 1.00f, 0.55f, 0.85f),    // maxixe deep blue
                    M(0.22f, 218f, 240f, 0.84f, 1.00f, 0.38f, 0.56f),    // deep blue
                    M(0.14f, 220f, 242f, 0.84f, 1.00f, 0.28f, 0.40f),    // dark blue
                    M(0.20f, 208f, 232f, 0.60f, 0.80f, 0.70f, 0.92f),    // intense blue
                    M(0.14f, 200f, 225f, 0.30f, 0.55f, 0.88f, 1.00f),    // faded blue
                };
            case "Heliodore":
                return new[]
                {
                    M(0.20f, 52f, 70f, 0.20f, 0.36f, 0.94f, 1.00f),      // light yellow
                    M(0.22f, 50f, 72f, 0.36f, 0.54f, 0.92f, 1.00f),      // pastel yellow
                    M(0.22f, 48f, 66f, 0.54f, 0.74f, 0.90f, 1.00f),      // yellow
                    M(0.10f, 44f, 62f, 0.74f, 0.92f, 0.90f, 1.00f),      // intense yellow
                    M(0.04f, 44f, 58f, 0.92f, 1.00f, 0.92f, 1.00f),      // vivid yellow
                    M(0.10f, 62f, 80f, 0.30f, 0.62f, 0.84f, 1.00f),      // greenish yellow
                    M(0.08f, 38f, 52f, 0.60f, 0.88f, 0.68f, 0.88f),      // golden / deep yellow
                    M(0.04f, 34f, 48f, 0.55f, 0.80f, 0.52f, 0.70f),      // brownish yellow
                };
            case "Morganite":
                return new[]
                {
                    M(0.24f, 335f, 354f, 0.18f, 0.32f, 0.96f, 1.00f),    // light pink
                    M(0.26f, 338f, 358f, 0.30f, 0.46f, 0.94f, 1.00f),    // pastel pink
                    M(0.16f, 340f, 360f, 0.44f, 0.60f, 0.92f, 1.00f),    // pink
                    M(0.10f, 340f, 358f, 0.60f, 0.74f, 0.88f, 1.00f),    // intense pink
                    M(0.12f, 4f, 22f, 0.22f, 0.44f, 0.94f, 1.00f),       // peach / orangy pink
                    M(0.06f, 14f, 30f, 0.34f, 0.52f, 0.90f, 1.00f),      // salmon / orange
                    M(0.06f, 318f, 338f, 0.20f, 0.40f, 0.92f, 1.00f),    // violetish pink
                };
            case "Beryl_Rouge":
                return new[]
                {
                    M(0.20f, 348f, 366f, 0.66f, 0.88f, 0.58f, 0.84f),    // red
                    M(0.20f, 332f, 350f, 0.60f, 0.86f, 0.55f, 0.82f),    // purplish red
                    M(0.14f, 340f, 358f, 0.40f, 0.62f, 0.80f, 0.98f),    // pinkish red
                    M(0.14f, 350f, 372f, 0.80f, 1.00f, 0.38f, 0.58f),    // deep red
                    M(0.08f, 352f, 374f, 0.80f, 1.00f, 0.26f, 0.40f),    // dark red
                    M(0.10f, 4f, 16f, 0.62f, 0.86f, 0.62f, 0.86f),       // orangy red
                    M(0.14f, 326f, 345f, 0.42f, 0.72f, 0.70f, 0.94f),    // raspberry pink
                };
            case "Pezzottaite":
                return new[]
                {
                    M(0.20f, 330f, 350f, 0.40f, 0.62f, 0.78f, 0.96f),    // raspberry pink
                    M(0.20f, 336f, 356f, 0.28f, 0.44f, 0.88f, 1.00f),    // pastel pink
                    M(0.18f, 330f, 352f, 0.60f, 0.82f, 0.62f, 0.86f),    // intense pink-red
                    M(0.12f, 340f, 362f, 0.66f, 0.88f, 0.48f, 0.68f),    // purplish red
                    M(0.10f, 4f, 18f, 0.32f, 0.56f, 0.86f, 1.00f),       // orangy pink
                    M(0.10f, 318f, 336f, 0.34f, 0.60f, 0.74f, 0.94f),    // violetish pink
                    M(0.10f, 340f, 360f, 0.70f, 0.90f, 0.38f, 0.56f),    // deep red-pink
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
                    M(0.07f, 134f, 148f, 0.88f, 1.00f, 0.58f, 0.80f),    // Muzo green
                    M(0.20f, 124f, 148f, 0.85f, 1.00f, 0.72f, 0.90f),    // vivid green
                    M(0.20f, 120f, 148f, 0.62f, 0.84f, 0.78f, 0.95f),    // intense / medium green
                    M(0.14f, 116f, 145f, 0.28f, 0.50f, 0.94f, 1.00f),    // light green
                    M(0.12f, 150f, 164f, 0.70f, 0.95f, 0.65f, 0.90f),    // bluish green
                    M(0.13f, 98f, 124f, 0.65f, 0.90f, 0.70f, 0.90f),    // yellowish green
                    M(0.08f, 128f, 152f, 0.88f, 1.00f, 0.38f, 0.58f),    // deep green
                    M(0.06f, 138f, 158f, 0.40f, 0.60f, 0.85f, 1.00f),    // pastel green
                    M(0.10f, 112f, 140f, 0.40f, 0.62f, 0.32f, 0.52f),    // dull, greyish dark green
                };
            case "Sphalerite":
                // Red to orange to brown, a honey yellow at the pale end, a near-black brown at the dark end.
                return new[]
                {
                    M(0.24f, 22f, 36f, 0.75f, 1.00f, 0.60f, 0.90f),      // orange
                    M(0.14f, 34f, 48f, 0.60f, 0.90f, 0.65f, 0.90f),      // honey
                    M(0.14f, 8f, 20f, 0.75f, 1.00f, 0.50f, 0.80f),       // red-orange
                    M(0.14f, 0f, 12f, 0.80f, 1.00f, 0.30f, 0.55f),       // deep red
                    M(0.22f, 24f, 38f, 0.60f, 0.90f, 0.25f, 0.50f),      // brown
                    M(0.12f, 5f, 25f, 0.55f, 0.85f, 0.15f, 0.30f),       // near-black red-brown
                };
            case "Ivoire_Vegetal":
                // Corozo: cream when fresh, honey, caramel and brown as it ages in the light or is dyed (the yellow-brown faceted stones of the shops).
                return new[]
                {
                    M(0.25f, 35f, 48f, 0.08f, 0.25f, 0.85f, 0.97f),
                    M(0.35f, 35f, 45f, 0.35f, 0.60f, 0.75f, 0.92f),
                    M(0.25f, 28f, 38f, 0.45f, 0.65f, 0.60f, 0.80f),
                    M(0.15f, 22f, 32f, 0.50f, 0.70f, 0.35f, 0.55f),
                };
            case "Agate":
                // The natural colours of agate: blue-gray chalcedony above all, gray, brown and beige, honey, the orange-red of iron, a faint lavender, nearly black.
                // The vivid blues, greens and pinks of the shops are dyed (see DyeingCatalog).
                return new[]
                {
                    M(0.30f, 205f, 230f, 0.12f, 0.35f, 0.55f, 0.80f),
                    M(0.14f, 200f, 240f, 0.00f, 0.06f, 0.40f, 0.75f),
                    M(0.20f, 25f, 40f, 0.25f, 0.55f, 0.45f, 0.75f),
                    M(0.10f, 35f, 45f, 0.30f, 0.55f, 0.70f, 0.90f),
                    M(0.12f, 15f, 30f, 0.55f, 0.85f, 0.55f, 0.85f),
                    M(0.07f, 300f, 340f, 0.10f, 0.25f, 0.65f, 0.85f),
                    M(0.07f, 200f, 240f, 0.00f, 0.08f, 0.10f, 0.30f),
                };
            case "Obsidienne_Flocons":
                // Black glass: the snowflakes are drawn by the pattern, the body stays black.
                return new[]
                {
                    M(1f, 20f, 40f, 0.00f, 0.05f, 0.02f, 0.07f),
                };
            case "Tourmaline_Schorl":
                // Black: no hue to speak of, only a trace of brown or gray, and very dark.
                return new[]
                {
                    M(0.70f, 20f, 40f, 0.00f, 0.06f, 0.03f, 0.10f),      // jet black
                    M(0.30f, 20f, 40f, 0.02f, 0.10f, 0.08f, 0.16f),      // black with a brownish or grayish glint
                };
            case "Iolite":
                // Violet-blue to blue-violet, from a grayish pale blue to a deep ink blue; what the cut shows depends on the axis.
                return new[]
                {
                    M(0.30f, 248f, 266f, 0.55f, 0.75f, 0.50f, 0.75f),    // violet-blue
                    M(0.20f, 240f, 256f, 0.60f, 0.80f, 0.45f, 0.65f),    // blue-violet
                    M(0.18f, 250f, 270f, 0.25f, 0.42f, 0.78f, 0.95f),    // light violet-blue
                    M(0.12f, 225f, 250f, 0.15f, 0.30f, 0.70f, 0.90f),    // grayish pale blue
                    M(0.12f, 250f, 270f, 0.60f, 0.85f, 0.30f, 0.45f),    // deep ink blue
                    M(0.08f, 265f, 285f, 0.20f, 0.38f, 0.75f, 0.92f),    // lavender
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

    // Widens a palette: besides the characteristic colours of the species, a cloud of colours around its mean hue, over a window of hues and over the rows of the grid
    // that surround its usual tones, from the paler and softer to the deeper ones, with muted (grayer, darker) variants of each. So no two stones of a species look
    // alike, while the stone keeps its identity: the cloud stays near the hue and the tones the species really has.
    private static Mode[] Expand(Mode[] curated)
    {
        if (curated == null || curated.Length == 0) return curated;

        var rows = RowSaturation.Length;
        float total = 0f, sumX = 0f, sumY = 0f, meanSaturation = 0f;
        foreach (var mode in curated)
        {
            var radians = (mode.HueMin + mode.HueMax) * 0.5f * Mathf.Deg2Rad;
            total += mode.Weight;
            sumX += mode.Weight * Mathf.Cos(radians);
            sumY += mode.Weight * Mathf.Sin(radians);
            meanSaturation += mode.Weight * (mode.SatMin + mode.SatMax) * 0.5f;
        }
        // A colourless, gray or black species keeps its subtle range.
        if (total <= 0f || meanSaturation / total < 0.12f) return curated;

        // A palette whose modes lie at far apart hues (a stone that is red or blue, blue or violet) has no single mean hue: it keeps its own modes.
        var concentration = Mathf.Sqrt(sumX * sumX + sumY * sumY) / total;
        if (concentration < 0.80f) return curated;

        var meanHue = Mathf.Repeat(Mathf.Atan2(sumY, sumX) * Mathf.Rad2Deg, 360f);
        var spread = 0f;
        var histogram = new float[rows];
        foreach (var mode in curated)
        {
            var centre = (mode.HueMin + mode.HueMax) * 0.5f;
            spread += mode.Weight * (Mathf.Abs(Mathf.DeltaAngle(centre, meanHue)) + (mode.HueMax - mode.HueMin) * 0.5f);

            var saturation = (mode.SatMin + mode.SatMax) * 0.5f;
            var brightness = (mode.BrightMin + mode.BrightMax) * 0.5f;
            var best = 0;
            var bestDistance = float.MaxValue;
            for (var row = 0; row < rows; row++)
            {
                var d = (saturation - RowSaturation[row]) * (saturation - RowSaturation[row]) + (brightness - RowBrightness[row]) * (brightness - RowBrightness[row]);
                if (d < bestDistance) { bestDistance = d; best = row; }
            }
            histogram[best] += mode.Weight / total;
        }
        spread = Mathf.Clamp(spread / total + 4f, 8f, 24f);

        // The rows around the usual tones of the species: two rows on each side, and the in-between ones.
        int low = rows, high = -1;
        for (var row = 0; row < rows; row++)
            if (histogram[row] > 0.02f) { low = Mathf.Min(low, row); high = Mathf.Max(high, row); }
        if (high < 0) return curated;
        low = Mathf.Max(0, low - 2);
        high = Mathf.Min(rows - 1, high + 2);

        var rowWeight = new float[rows];
        var rowTotal = 0f;
        for (var row = low; row <= high; row++)
        {
            var left = row > 0 ? histogram[row - 1] : 0f;
            var right = row < rows - 1 ? histogram[row + 1] : 0f;
            rowWeight[row] = histogram[row] + 0.5f * (left + right) + 0.04f;
            rowTotal += rowWeight[row];
        }

        const int bands = 13;
        var cloud = new List<Mode>();
        var cloudTotal = 0f;
        for (var band = 0; band < bands; band++)
        {
            var offset = (band - (bands - 1) * 0.5f) / ((bands - 1) * 0.5f);          // -1 .. 1 across the window
            var hue = meanHue + offset * spread;
            var half = spread / (bands - 1) * 1.6f;
            var bandWeight = Mathf.Exp(-(offset * offset) / 0.45f);
            for (var row = low; row <= high; row++)
            {
                var weight = bandWeight * rowWeight[row] / rowTotal;
                var s0 = Mathf.Clamp01(RowSaturation[row] - 0.10f);
                var s1 = Mathf.Clamp01(RowSaturation[row] + 0.07f);
                var b0 = Mathf.Clamp01(RowBrightness[row] - 0.10f);
                var b1 = Mathf.Clamp01(RowBrightness[row] + 0.07f);
                cloud.Add(M(weight, hue - half, hue + half, s0, s1, b0, b1));
                cloudTotal += weight;

                // The muted version: grayer and a little darker, like a stone with a brownish or grayish overtone.
                if (row <= 6 && band % 2 == 0)
                {
                    var muted = weight * 0.30f;
                    cloud.Add(M(muted, hue - half, hue + half, Mathf.Clamp01(RowSaturation[row] * 0.55f - 0.06f), Mathf.Clamp01(RowSaturation[row] * 0.55f + 0.08f),
                        Mathf.Clamp01(RowBrightness[row] * 0.80f - 0.06f), Mathf.Clamp01(RowBrightness[row] * 0.80f + 0.06f)));
                    cloudTotal += muted;
                }
            }
        }

        // The characteristic colours keep a little more than half of the draws, the cloud the rest.
        const float characteristic = 0.65f;
        var result = new List<Mode>(curated.Length + cloud.Count);
        foreach (var mode in curated)
        {
            var scaled = mode;
            scaled.Weight = mode.Weight / total * characteristic;
            result.Add(scaled);
        }
        foreach (var mode in cloud)
        {
            var scaled = mode;
            scaled.Weight = mode.Weight / cloudTotal * (1f - characteristic);
            result.Add(scaled);
        }
        return result.ToArray();
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
