using UnityEngine;

public enum Diaphaneity { Transparent, SemiTransparent, SemiOpaque, Opaque }

// How much light a stone lets through, and how that looks on its sprite: glass-like and see-through when transparent,
// milky and soft when semi-transparent, dull and grainy when semi-opaque, flat and matte when opaque.
public static class GemDiaphaneity
{
    // The species data keeps the gemological words ("Translucide", "Opaque à translucide"...); they map onto the four levels.
    public static Diaphaneity Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return Diaphaneity.Transparent;

        var t = text.ToLowerInvariant();
        if (t.Contains("semi-opaque") || t.Contains(" à ")) return Diaphaneity.SemiOpaque;
        if (t.Contains("semi-transparent") || t.StartsWith("transluc")) return Diaphaneity.SemiTransparent;
        if (t.StartsWith("opaque")) return Diaphaneity.Opaque;
        return Diaphaneity.Transparent;
    }

    public static string Label(Diaphaneity level)
    {
        switch (level)
        {
            case Diaphaneity.SemiTransparent: return "Semi-transparent";
            case Diaphaneity.SemiOpaque: return "Semi-opaque";
            case Diaphaneity.Opaque: return "Opaque";
            default: return "Transparent";
        }
    }

    // Opacity of the stone's shaded-away side (the thickness drawn under and to the right of an icon).
    public static float EdgeAlpha(Diaphaneity level)
    {
        switch (level)
        {
            case Diaphaneity.Transparent: return 0.62f;
            case Diaphaneity.SemiTransparent: return 0.82f;
            default: return 1f;
        }
    }

    // Takes a lit facet color (`tone` 0..1 is how lit the facet is) and gives it the look of the stone's diaphaneity.
    // The pixel position drives the grain of semi-opaque stones.
    public static Color Shade(Color color, float tone, Diaphaneity level, Color body, int x, int y)
    {
        tone = Mathf.Clamp01(tone);
        Color result;
        switch (level)
        {
            case Diaphaneity.Transparent:
                // Glass: the shadows let the background through and keep the stone's tint, lit facets stay solid and bright.
                result = Color.Lerp(color, Color.Lerp(body, Color.white, 0.25f), (1f - tone) * 0.35f);
                result.a = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(tone * 1.3f));
                return result;

            case Diaphaneity.SemiTransparent:
                // Milky: soft contrast under a whitish haze, the light diffuses instead of sparkling.
                result = Color.Lerp(color, Color.Lerp(body, Color.white, 0.32f), 0.38f);
                result.a = Mathf.Lerp(0.78f, 1f, tone);
                return result;

            case Diaphaneity.SemiOpaque:
                // Dull and grainy: the colors lose their punch and a fine checkerboard grain breaks up the facets.
                result = Color.Lerp(color, Color.Lerp(body, new Color(0.45f, 0.45f, 0.45f), 0.4f), 0.32f);
                result = GemPixelArt.Dim(result, 0.9f);
                if (((x + y) & 1) == 0) result = GemPixelArt.Dim(result, 0.9f);
                return result;

            default:
                // Opaque: no light gets in, so no glints: flat, matte tones and the brightest ones are held back.
                result = Color.Lerp(color, body, 0.6f);
                return GemPixelArt.Dim(result, 0.78f + 0.12f * tone);
        }
    }
}
