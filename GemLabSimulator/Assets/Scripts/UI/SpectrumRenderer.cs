using System.Collections.Generic;
using UnityEngine;

public static class SpectrumRenderer
{
    public const float MinNm = 400f;
    public const float MaxNm = 700f;
    private const int TextureWidth = 300;
    private const int TextureHeight = 40;

    public static Sprite BuildSpectrumSprite(List<SpectrumLine> lines)
    {
        return BuildSpectrumSprite(lines, TextureWidth, TextureHeight);
    }

    // The colour bands of the scale, as the spectroscope is graduated (nm).
    public static readonly (string name, float lo, float hi)[] Bands =
    {
        ("Violet", 400f, 450f), ("Bleu", 450f, 490f), ("Vert", 490f, 560f), ("Jaune", 560f, 590f), ("Orange", 590f, 610f), ("Rouge", 610f, 700f),
    };

    public static string BandAt(float nm)
    {
        foreach (var band in Bands)
            if (nm < band.hi) return band.name;
        return Bands[Bands.Length - 1].name;
    }

    public static Sprite BuildSpectrumSprite(List<SpectrumLine> lines, int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (var x = 0; x < width; x++)
        {
            var t = x / (float)(width - 1);
            var wavelength = Mathf.Lerp(MinNm, MaxNm, t);
            var finalColor = Color.Lerp(WavelengthToColor(wavelength), Color.black, Darkness(lines, wavelength));
            for (var y = 0; y < height; y++)
                tex.SetPixel(x, y, finalColor);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    // How much the lines swallow the light at a wavelength (0 to 1): a flat bottom with soft edges, a narrow line having sharp ones.
    public static float Darkness(List<SpectrumLine> lines, float wavelength)
    {
        var darkness = 0f;
        if (lines == null) return darkness;

        foreach (var line in lines)
        {
            var half = Mathf.Max(line.widthNm, 1f) * 0.5f;
            var dist = Mathf.Abs(wavelength - line.wavelengthNm);
            if (dist >= half) continue;
            var edge = line.edgeNm > 0f ? line.edgeNm : Mathf.Clamp(line.widthNm * 0.35f, 1.5f, 12f);
            var inside = Mathf.Clamp01((half - dist) / Mathf.Min(edge, half));
            darkness = Mathf.Max(darkness, inside * line.Depth);
        }
        return darkness;
    }

    // The colour of the spectrum at a wavelength: the bands of the scale (violet to 450 nm, blue to 490, green to 560, yellow to 590, orange to 610, red after),
    // dark at both ends where the eye sees little.
    private static readonly (float nm, Color color)[] Stops =
    {
        (400f, new Color(0.07f, 0.02f, 0.18f)), (415f, new Color(0.13f, 0.06f, 0.40f)), (432f, new Color(0.19f, 0.12f, 0.60f)), (450f, new Color(0.15f, 0.22f, 0.78f)),
        (462f, new Color(0.18f, 0.36f, 0.85f)), (472f, new Color(0.40f, 0.72f, 0.88f)), (490f, new Color(0.38f, 0.72f, 0.70f)), (510f, new Color(0.30f, 0.62f, 0.22f)),
        (535f, new Color(0.32f, 0.60f, 0.12f)), (560f, new Color(0.62f, 0.75f, 0.10f)), (575f, new Color(0.90f, 0.82f, 0.10f)), (590f, new Color(0.96f, 0.60f, 0.10f)),
        (610f, new Color(0.90f, 0.28f, 0.14f)), (630f, new Color(0.82f, 0.13f, 0.12f)), (660f, new Color(0.78f, 0.10f, 0.10f)), (685f, new Color(0.50f, 0.05f, 0.05f)),
        (700f, new Color(0.20f, 0.02f, 0.02f)),
    };

    public static Color WavelengthToColor(float wavelength)
    {
        if (wavelength <= Stops[0].nm) return Stops[0].color;
        for (var i = 1; i < Stops.Length; i++)
        {
            if (wavelength > Stops[i].nm) continue;
            var k = (wavelength - Stops[i - 1].nm) / (Stops[i].nm - Stops[i - 1].nm);
            return Color.Lerp(Stops[i - 1].color, Stops[i].color, k);
        }
        return Stops[Stops.Length - 1].color;
    }
}
