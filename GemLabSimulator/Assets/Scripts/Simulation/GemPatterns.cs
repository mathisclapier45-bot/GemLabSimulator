using UnityEngine;

// What a species shows inside or on its sprite besides its colour: the bands of an agate, the gold flecks of lapis, the veins of turquoise, the silky band of a tiger's eye, the
// play of colour of an opal... Applied pixel by pixel to the finished colour of the sprite (see GemLook for which species have which).
public static class GemPatterns
{
    public static Color Apply(Color c, int px, int py, int size, float tone, GemLook look, Color body)
    {
        if (look.Pattern == GemPattern.None) return c;

        var u = (px + 0.5f) / size * 2f - 1f;                 // -1 .. 1, to the right
        var v = (py + 0.5f) / size * 2f - 1f;                 // -1 .. 1, up
        var s = look.Strength;
        var seed = look.Seed;
        var alpha = c.a;
        Color r = c;

        switch (look.Pattern)
        {
            case GemPattern.Banded:
            {
                // Rings that follow the outline, uneven, alternately lighter and darker.
                var warp = Noise(u * 2.4f + 3f, v * 2.4f + 1f, seed) * 0.55f;
                var t = Mathf.Repeat((Mathf.Sqrt(u * u * 0.85f + v * v * 1.15f) + warp * 0.4f) * 4.4f, 1f);
                r = t < 0.5f ? Darken(c, 1f - 0.20f * s) : Lighten(c, 0.20f * s);
                break;
            }
            case GemPattern.Flecks:
            {
                // Pale patches of calcite and a few grains of gold pyrite.
                var patch = Noise(u * 3.2f + 7f, v * 3.2f + 2f, seed);
                if (patch > 0.70f) r = Color.Lerp(c, new Color(0.78f, 0.82f, 0.92f), 0.42f * s);
                var grain = Hash(px, py, seed);
                if (grain > 0.93f) r = Color.Lerp(r, new Color(1f, 0.86f, 0.30f), 0.85f);
                else if (grain < 0.05f) r = Darken(r, 0.78f);
                break;
            }
            case GemPattern.Snowflakes:
            {
                // White and gray spherulites of cristobalite with feathery radial edges, in black glass.
                var gu = u * 4.4f;
                var gv = v * 4.4f;
                var cx0 = Mathf.FloorToInt(gu);
                var cy0 = Mathf.FloorToInt(gv);
                var best = 9f;
                var relX = 0f;
                var relY = 0f;
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var cx = cx0 + dx;
                        var cy = cy0 + dy;
                        var centreX = cx + 0.2f + 0.6f * Hash(cx, cy, seed);
                        var centreY = cy + 0.2f + 0.6f * Hash(cx + 57, cy + 13, seed);
                        var flakeSize = 0.24f + 0.20f * Hash(cx + 101, cy + 7, seed);
                        var present = Hash(cx + 211, cy + 31, seed) < 0.6f;       // only some of the cells hold a snowflake: most of the stone stays black
                        var score = present ? Mathf.Sqrt((gu - centreX) * (gu - centreX) + (gv - centreY) * (gv - centreY)) / flakeSize : 9f;
                        if (score < best) { best = score; relX = gu - centreX; relY = gv - centreY; }
                    }
                var feather = 0.5f + 0.5f * Mathf.Sin(Mathf.Atan2(relY, relX) * 9f + best * 6f);
                var edge = best + (feather - 0.5f) * 0.4f;
                var inside = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.95f, edge));
                var flake = Color.Lerp(new Color(0.52f, 0.54f, 0.60f), new Color(0.90f, 0.92f, 0.95f), feather);
                r = Color.Lerp(c, flake, inside * s);
                break;
            }
            case GemPattern.Veins:
            {
                // A network of thin dark veins.
                var line = Mathf.Abs(Mathf.Sin(u * 5.2f + 2.2f * Mathf.Sin(v * 4.1f + seed) + Noise(u * 3f, v * 3f, seed) * 2f));
                var line2 = Mathf.Abs(Mathf.Sin(v * 6.4f + 1.9f * Mathf.Sin(u * 3.7f + seed * 0.3f)));
                if (line < 0.11f || line2 < 0.08f) r = Color.Lerp(c, look.Accent, 0.78f * s);
                break;
            }
            case GemPattern.Mottled:
            {
                var blotch = Noise(u * 3.6f + 1f, v * 3.6f + 5f, seed) - 0.5f;
                r = blotch > 0f ? Lighten(c, blotch * 0.55f * s) : Darken(c, 1f + blotch * 0.55f * s);
                break;
            }
            case GemPattern.Chatoyant:
            {
                // Fine fibres, and a bright band across the middle that is the cat's eye.
                var fibre = Mathf.Sin(v * 26f + Noise(u * 2f, v * 2f, seed) * 4f) * 0.5f + 0.5f;
                r = Darken(c, 0.82f + 0.18f * fibre);
                var band = Mathf.Exp(-(u * u) / 0.045f);
                r = Color.Lerp(r, look.Accent, band * 0.55f * s * (0.6f + 0.4f * fibre));
                break;
            }
            case GemPattern.Adular:
            {
                // A blue-white sheen floating under the surface, toward the upper left.
                var glow = Mathf.Exp(-((u + 0.25f) * (u + 0.25f) + (v - 0.22f) * (v - 0.22f)) / 0.16f);
                r = Color.Lerp(c, look.Accent, glow * 0.62f * s);
                r = Color.Lerp(r, Color.white, glow * 0.25f * s);
                break;
            }
            case GemPattern.Opal:
            {
                // Patches of spectral colour on a milky ground.
                var cell = Noise(u * 4.2f, v * 4.2f, seed + 11);
                if (cell > 0.52f)
                {
                    var hue = Mathf.Repeat(Hash(Mathf.FloorToInt((u + 1f) * 3f), Mathf.FloorToInt((v + 1f) * 3f), seed) + cell, 1f);
                    r = Color.Lerp(c, Color.HSVToRGB(hue, 0.65f, 1f), 0.55f * s);
                }
                break;
            }
            case GemPattern.Labrador:
            {
                // A flash of blue-green and gold across a grey stone.
                var flash = Mathf.Exp(-((u - 0.1f) * (u - 0.1f) * 1.6f + (v + 0.12f) * (v + 0.12f) * 4.5f) / 0.22f);
                var n = Noise(u * 4f, v * 4f, seed);
                var tint = Color.Lerp(look.Accent, new Color(0.95f, 0.78f, 0.28f), Mathf.Clamp01((u + 0.2f) * 1.2f));
                r = Color.Lerp(c, tint, flash * (0.55f + 0.3f * n) * s);
                break;
            }
            case GemPattern.Pearl:
            {
                // Mother-of-pearl: a pink overtone on one side, a green one on the other, a round highlight.
                var side = Mathf.Clamp01(0.5f + (u - v) * 0.5f);
                r = Color.Lerp(c, Color.Lerp(look.Accent, new Color(0.80f, 0.95f, 0.90f), 1f - side), 0.30f * s);
                var d = (u + 0.30f) * (u + 0.30f) + (v - 0.32f) * (v - 0.32f);
                if (d < 0.020f) r = Color.Lerp(r, Color.white, 0.9f);
                else if (d < 0.07f) r = Lighten(r, 0.18f);
                break;
            }
            case GemPattern.MetalGray:
            {
                // Polished metal: a steep ramp from near black to bright steel, so the highlights are hard.
                var t = Mathf.Clamp01((tone - 0.25f) * 1.6f);
                t = t * t * (3f - 2f * t);
                r = Color.Lerp(new Color(0.05f, 0.05f, 0.07f), new Color(0.60f, 0.62f, 0.68f), t);
                break;
            }
            case GemPattern.MetalBrass:
            {
                var t = Mathf.Clamp01((tone - 0.25f) * 1.6f);
                t = t * t * (3f - 2f * t);
                r = Color.Lerp(new Color(0.30f, 0.20f, 0.03f), new Color(1f, 0.92f, 0.45f), t);
                break;
            }
            case GemPattern.Tortoise:
            {
                var blotch = Noise(u * 3.3f + 2f, v * 3.3f + 9f, seed);
                if (blotch > 0.58f) r = Color.Lerp(c, look.Accent, 0.85f * s);
                else if (blotch > 0.50f) r = Color.Lerp(c, look.Accent, 0.35f * s);
                break;
            }
            case GemPattern.Amber:
            {
                // The warm glow of a resin lit from inside, and a few dark specks.
                var glow = Mathf.Exp(-(u * u + v * v) / 0.35f);
                r = Color.Lerp(c, look.Accent, glow * 0.45f * s);
                if (Hash(px, py, seed) > 0.975f) r = Darken(r, 0.6f);
                break;
            }
            case GemPattern.Grain:
            {
                // Fine lines of the growth, crossing.
                var a = Mathf.Sin((u + v) * 22f + Noise(u * 2f, v * 2f, seed) * 3f);
                var b = Mathf.Sin((u - v) * 17f);
                r = a > 0.55f ? Darken(c, 1f - 0.10f * s) : Lighten(c, 0f);
                if (b > 0.85f) r = Darken(r, 1f - 0.06f * s);
                break;
            }
            case GemPattern.Streaks:
            {
                var streak = Mathf.Sin(u * 15f + Noise(u * 1.1f, v * 0.7f, seed) * 2.2f) * 0.5f + 0.5f;
                r = Color.Lerp(c, look.Accent, Mathf.Clamp01((streak - 0.62f) * 2.4f) * 0.55f * s);
                break;
            }
            case GemPattern.Bicolour:
            {
                // Two colours side by side, along a diagonal.
                var side = Mathf.Clamp01(((u - v * 0.5f) * 5f) * 0.5f + 0.5f);
                r = Color.Lerp(c, Retint(c, body, look.Accent), side * s);
                break;
            }
            case GemPattern.ColorChange:
            {
                var side = Mathf.Clamp01(((u + v * 0.3f) * 3.2f) * 0.5f + 0.5f);
                r = Color.Lerp(c, Retint(c, body, look.Accent), side * s);
                break;
            }
            case GemPattern.Gradient:
            {
                var side = Mathf.Clamp01(u * 0.8f + 0.5f);
                r = Color.Lerp(c, Retint(c, body, look.Accent), side * s);
                break;
            }
            case GemPattern.Cleavage:
            {
                // Pale streaks along the cleavage planes.
                var plane = Mathf.Abs(Mathf.Sin((u * 0.8f + v * 0.6f) * 9f + Noise(u * 2f, v * 2f, seed) * 2f));
                if (plane < 0.10f) r = Color.Lerp(c, Color.white, 0.40f * s);
                break;
            }
            case GemPattern.Bubbles:
            {
                // Round bubbles caught in the glass.
                for (var k = 0; k < 3; k++)
                {
                    var bx = Hash(k, 1, seed) * 1.4f - 0.7f;
                    var by = Hash(k, 2, seed) * 1.4f - 0.7f;
                    var d = (u - bx) * (u - bx) + (v - by) * (v - by);
                    if (d < 0.012f) r = Lighten(c, 0.45f);
                    else if (d < 0.020f) r = Darken(c, 0.82f);
                }
                break;
            }
        }

        r.a = alpha;
        return r;
    }

    private static Color Lighten(Color c, float amount) => new Color(Mathf.Clamp01(c.r + amount), Mathf.Clamp01(c.g + amount), Mathf.Clamp01(c.b + amount), c.a);

    private static Color Darken(Color c, float factor) => new Color(c.r * factor, c.g * factor, c.b * factor, c.a);

    // The same light and shade, in another colour: the pixel's brightness relative to the body, carried onto the accent.
    private static Color Retint(Color c, Color body, Color accent)
    {
        var lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        var bodyLum = Mathf.Max(0.2f, body.r * 0.3f + body.g * 0.59f + body.b * 0.11f);
        var k = Mathf.Clamp(lum / bodyLum, 0.15f, 1.7f);
        return new Color(Mathf.Clamp01(accent.r * k), Mathf.Clamp01(accent.g * k), Mathf.Clamp01(accent.b * k), c.a);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    // Smooth value noise in 0..1.
    private static float Noise(float x, float y, int seed)
    {
        var x0 = Mathf.FloorToInt(x);
        var y0 = Mathf.FloorToInt(y);
        var fx = x - x0;
        var fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        var a = Hash(x0, y0, seed);
        var b = Hash(x0 + 1, y0, seed);
        var c = Hash(x0, y0 + 1, seed);
        var d = Hash(x0 + 1, y0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }
}
