using System.Collections.Generic;
using UnityEngine;

// Colors seen along the three principal directions of a stone, derived from its free-text pleochroism
// description ("Vert / Orange / Rouge selon les trois axes...").
public struct PleochroismInfo
{
    public bool Pleochroic;
    public Color X;
    public Color Y;
    public Color Z;

    private static readonly (string[] keywords, Color color)[] ColorWords =
    {
        (new[] { "incolore" }, new Color(0.93f, 0.95f, 0.97f)),
        (new[] { "jaun" }, new Color(0.96f, 0.84f, 0.18f)),
        (new[] { "doré" }, new Color(1.00f, 0.76f, 0.12f)),
        (new[] { "vert", "verd" }, new Color(0.20f, 0.68f, 0.30f)),
        (new[] { "bleu" }, new Color(0.18f, 0.38f, 0.85f)),
        (new[] { "violet", "violac" }, new Color(0.52f, 0.28f, 0.78f)),
        (new[] { "pourpr" }, new Color(0.62f, 0.15f, 0.45f)),
        (new[] { "rose", "rosâtre" }, new Color(0.96f, 0.55f, 0.70f)),
        (new[] { "rouge" }, new Color(0.84f, 0.10f, 0.14f)),
        (new[] { "orang" }, new Color(0.96f, 0.52f, 0.10f)),
        (new[] { "brun" }, new Color(0.44f, 0.27f, 0.12f)),
    };

    public static PleochroismInfo ForGem(GemDefinition def)
    {
        var body = def.iconColor.a > 0f ? def.iconColor : new Color(0.72f, 0.72f, 0.76f);

        if (!def.pleochroic)
            return new PleochroismInfo { Pleochroic = false, X = body, Y = body, Z = body };

        var colors = ParseColors(def.pleochroicDescription);

        // No colors in the text ("Dichroïsme faible à modéré selon la teinte"): shades of the stone's own color.
        if (colors.Count == 0)
        {
            colors.Add(Color.Lerp(body, Color.white, 0.35f));
            colors.Add(Color.Lerp(body, Color.black, 0.30f));
        }
        else if (colors.Count == 1)
        {
            colors.Add(Darken(colors[0]));
        }

        var info = new PleochroismInfo { Pleochroic = true };
        if (colors.Count >= 3)
        {
            info.X = colors[0];
            info.Y = colors[1];
            info.Z = colors[2];
        }
        else
        {
            // Two colors: one unique direction (optic axis along Z) and two equivalent ones.
            info.X = colors[0];
            info.Y = colors[0];
            info.Z = colors[1];
        }
        return info;
    }

    private static List<Color> ParseColors(string text)
    {
        var result = new List<Color>();
        if (string.IsNullOrEmpty(text)) return result;

        var cut = text.IndexOf(" selon", System.StringComparison.OrdinalIgnoreCase);
        var head = cut >= 0 ? text.Substring(0, cut) : text;

        var parts = new List<string>(head.Split(new[] { " / " }, System.StringSplitOptions.RemoveEmptyEntries));
        if (parts.Count == 1)
        {
            parts.Clear();
            foreach (var piece in head.Split(new[] { ", ", " et " }, System.StringSplitOptions.RemoveEmptyEntries))
                parts.Add(piece);
        }

        foreach (var part in parts)
        {
            if (TryParsePhrase(part, out var color)) result.Add(color);
        }
        return result;
    }

    private static bool TryParsePhrase(string phrase, out Color color)
    {
        var t = phrase.ToLowerInvariant();
        var found = new List<(int index, Color color)>();

        foreach (var (keywords, c) in ColorWords)
        {
            var best = -1;
            foreach (var keyword in keywords)
            {
                var i = t.IndexOf(keyword, System.StringComparison.Ordinal);
                if (i >= 0 && (best < 0 || i < best)) best = i;
            }
            if (best >= 0) found.Add((best, c));
        }

        color = Color.white;
        if (found.Count == 0) return false;

        found.Sort((a, b) => a.index.CompareTo(b.index));
        color = found[0].color;
        // Compound shades ("vert jaunâtre", "bleu-violacé"): the first color dominates.
        if (found.Count > 1) color = Color.Lerp(found[0].color, found[1].color, 0.4f);

        if (t.Contains("pâle") || t.Contains("clair")) color = Color.Lerp(color, Color.white, 0.5f);
        if (t.Contains("foncé") || t.Contains("sombre") || t.Contains("soutenu") || t.Contains("intense")) color = Darken(color);
        return true;
    }

    private static Color Darken(Color c)
    {
        return new Color(c.r * 0.70f, c.g * 0.70f, c.b * 0.70f, 1f);
    }

    // Color seen through a polarizer whose vibration direction is `polarization`: each principal direction
    // contributes in proportion to the square of its projection on that direction.
    public Color Seen(Quaternion stoneToWorld, Quaternion opticFrame, Vector3 polarization)
    {
        var ex = stoneToWorld * (opticFrame * Vector3.right);
        var ey = stoneToWorld * (opticFrame * Vector3.up);
        var ez = stoneToWorld * (opticFrame * Vector3.forward);

        var wx = Mathf.Pow(Vector3.Dot(ex, polarization), 2f);
        var wy = Mathf.Pow(Vector3.Dot(ey, polarization), 2f);
        var wz = Mathf.Pow(Vector3.Dot(ez, polarization), 2f);

        var c = X * wx + Y * wy + Z * wz;
        c.a = 1f;
        return c;
    }
}
