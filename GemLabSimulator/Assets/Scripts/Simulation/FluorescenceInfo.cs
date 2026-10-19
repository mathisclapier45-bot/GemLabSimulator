using System.Collections.Generic;
using UnityEngine;

// Turns the free-text UV description of a species into something that can be drawn: does it glow, in which
// color, how strongly, and does it keep glowing after the lamp is switched off.
public struct FluorescenceInfo
{
    public bool Glows;
    public Color Color;
    public float Intensity;
    public bool Phosphorescent;

    private static readonly (string[] keywords, Color color)[] ColorTable =
    {
        (new[] { "bleu-blanc", "blanc-bleu", "blanchâtre" }, new Color(0.78f, 0.88f, 1f)),
        (new[] { "bleu" }, new Color(0.30f, 0.50f, 1f)),
        (new[] { "violet", "mauve" }, new Color(0.70f, 0.42f, 1f)),
        (new[] { "rouge" }, new Color(1f, 0.16f, 0.10f)),
        (new[] { "rose", "rosée" }, new Color(1f, 0.46f, 0.66f)),
        (new[] { "orang" }, new Color(1f, 0.55f, 0.12f)),
        (new[] { "jaun" }, new Color(1f, 0.90f, 0.25f)),
        (new[] { "vert", "verd" }, new Color(0.30f, 1f, 0.38f)),
    };

    private static readonly Color DefaultColor = new Color(0.78f, 0.88f, 1f);

    public static FluorescenceInfo ForGem(GemDefinition def, bool longWave)
    {
        var own = longWave ? def.fluorescenceLongWave : def.fluorescenceShortWave;
        var other = longWave ? def.fluorescenceShortWave : def.fluorescenceLongWave;

        var info = Parse(own, out var hasColor);
        if (!info.Glows) return info;

        // A bare "weak fluorescence" without a color borrows the color given for the other wavelength.
        if (!hasColor)
        {
            Parse(other, out var otherHasColor, out var otherColor);
            info.Color = otherHasColor ? otherColor : DefaultColor;
        }
        return info;
    }

    private static FluorescenceInfo Parse(string text, out bool hasColor)
    {
        return Parse(text, out hasColor, out _);
    }

    private static FluorescenceInfo Parse(string text, out bool hasColor, out Color color)
    {
        var info = new FluorescenceInfo { Glows = false, Color = DefaultColor, Intensity = 0f };
        hasColor = false;
        color = DefaultColor;
        if (string.IsNullOrEmpty(text)) return info;

        var t = text.ToLowerInvariant();
        info.Phosphorescent = t.Contains("phosphorescence");

        // Colors in order of appearance; a range like "rouge à rose" is averaged.
        var found = new List<(int index, Color color)>();
        foreach (var (keywords, c) in ColorTable)
        {
            var best = -1;
            foreach (var keyword in keywords)
            {
                var i = t.IndexOf(keyword, System.StringComparison.Ordinal);
                if (i >= 0 && (best < 0 || i < best)) best = i;
            }
            if (best >= 0) found.Add((best, c));
        }
        found.Sort((a, b) => a.index.CompareTo(b.index));
        if (found.Count > 0)
        {
            hasColor = true;
            color = found.Count == 1 ? found[0].color : Color.Lerp(found[0].color, found[1].color, 0.5f);
            info.Color = color;
        }

        var inert = t.Contains("inerte") || t.Contains("aucune fluorescence") || t.Contains("non applicable") || t.Contains("quasi nulle");
        var hedged = t.Contains("parfois") || t.Contains("possible");
        if (inert && !(hedged && hasColor)) return info;

        info.Glows = true;
        if (t.Contains("vif") || t.Contains("intense")) info.Intensity = 1f;
        else if (t.Contains("modér")) info.Intensity = t.Contains("faible") ? 0.5f : 0.65f;
        else if (inert) info.Intensity = 0.2f;
        else if (t.Contains("faible") || hedged || t.Contains("variable") || t.Contains("terne") || t.Contains("diffuse") || t.Contains("absente"))
            info.Intensity = 0.3f;
        else info.Intensity = 0.75f;
        return info;
    }
}
