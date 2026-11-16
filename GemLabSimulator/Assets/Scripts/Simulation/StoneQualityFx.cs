using UnityEngine;

// Paints the quality of a stone (StoneQuality) into the picture of the stone: a veil and clouds of haze, the colour that is not even, the dull polish, the scratches,
// and the inclusions that show without a loupe. The inclusions have a place in the stone (they turn with it); only the pixels of the stone are touched.
public static class StoneQualityFx
{
    public static void Apply(Color32[] pixels, int size, StoneQuality quality, Color body, Quaternion orientation, Vector3 halfExtentsMm, bool inclusions = true, bool volumetric = false)
    {
        if (!quality.HasEffect || pixels == null || pixels.Length < size * size) return;

        var offsetA = (quality.Seed % 997) * 0.37f;
        var offsetB = ((quality.Seed / 997) % 991) * 0.41f;
        var angle = (quality.Seed % 360) * Mathf.Deg2Rad;
        var bandAxis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        var milky = new Color(Mathf.Lerp(body.r, 1f, 0.62f), Mathf.Lerp(body.g, 1f, 0.62f), Mathf.Lerp(body.b, 1f, 0.62f), 1f);
        var dullBody = body * 0.8f;
        var cap = 1f - quality.Dull * 0.30f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                var pixel = pixels[i];
                if (pixel.a < 8) continue;

                var u = (x + 0.5f) / size;
                var v = (y + 0.5f) / size;
                var c = (Color)pixel;
                var alpha = pixel.a / 255f;

                // Colour that is not even: bands across the stone and patches of a deeper or paler tone.
                if (quality.Zoning > 0.04f && !volumetric)
                {
                    var t = (u - 0.5f) * bandAxis.x + (v - 0.5f) * bandAxis.y;
                    var band = 0.6f * Mathf.PerlinNoise(t * 7f + offsetA, offsetB) + 0.4f * Mathf.PerlinNoise(u * 4f + offsetB, v * 4f + offsetA);
                    var lum = c.grayscale;
                    var saturation = 1f + quality.Zoning * (band - 0.5f) * 1.4f;
                    var value = 1f + quality.Zoning * (band - 0.5f) * 0.35f;
                    c = new Color((lum + (c.r - lum) * saturation) * value, (lum + (c.g - lum) * saturation) * value, (lum + (c.b - lum) * saturation) * value, 1f);
                }

                // Dull polish: less contrast, no brilliant highlights.
                if (quality.Dull > 0.02f)
                {
                    c = Color.Lerp(c, dullBody * (0.5f + 0.5f * c.grayscale), quality.Dull * 0.30f);
                    c = new Color(Mathf.Min(c.r, cap), Mathf.Min(c.g, cap), Mathf.Min(c.b, cap), 1f);
                }

                // The haze: a milky veil, thicker in patches (clouds), which also makes the stone less transparent.
                if (quality.Haze > 0.02f)
                {
                    var n = 0.6f * Mathf.PerlinNoise(u * 5f + offsetA, v * 5f + offsetB) + 0.4f * Mathf.PerlinNoise(u * 13f + offsetB, v * 13f + offsetA);
                    var cloud = Mathf.Clamp01((n - (0.62f - 0.30f * quality.Haze)) * 4.5f);
                    var veil = Mathf.Clamp01(quality.Haze * (0.18f + 0.62f * cloud)) * (volumetric ? 0.5f : 1f);
                    var target = milky * (0.70f + 0.30f * c.grayscale);
                    c = Color.Lerp(c, target, veil);
                    alpha = Mathf.Lerp(alpha, 1f, veil);
                }

                pixels[i] = new Color32(ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(alpha));
            }
        }

        if (inclusions) DrawInclusions(pixels, size, quality, body, orientation, halfExtentsMm);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
    }

    // The inclusions that can be seen without a loupe: none in a clean stone, a few in an included one, many in a heavily included one. Each has a place inside the stone.
    private static void DrawInclusions(Color32[] pixels, int size, StoneQuality quality, Color body, Quaternion orientation, Vector3 halfExtentsMm)
    {
        var count = quality.Tier >= 3 ? 14 : quality.Tier == 2 ? 6 : 0;
        if (quality.Tier == 1 && (quality.Seed % 100) < 25) count = 1;
        if (size < 64) count = Mathf.CeilToInt(count * 0.6f);
        if (count <= 0) return;

        var painter = new FeaturePainter(pixels, size, size, FeaturePainter.Mode.Composite);
        var rng = new System.Random(quality.Seed ^ 0x1C1D);
        float R() => (float)rng.NextDouble();

        var maxHalf = Mathf.Max(halfExtentsMm.x, Mathf.Max(halfExtentsMm.y, halfExtentsMm.z));
        if (maxHalf < 0.001f) return;
        var scale = size * GemView3D.FitFraction * 0.5f / maxHalf;
        var centre = new Vector2(size * 0.5f, size * 0.5f);
        var strength = quality.Tier >= 3 ? 1f : quality.Tier == 2 ? 0.75f : 0.4f;
        var pale = Color.Lerp(Color.white, body, 0.18f);
        var dark = Color.Lerp(body, Color.black, 0.78f);

        for (var k = 0; k < count; k++)
        {
            // A place in the stone (a point of the unit ball, in the frame of the stone), seen through its turn.
            var local = new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f);
            if (local.sqrMagnitude > 1f) local *= 0.7f;
            local *= 0.82f;
            var position = orientation * Vector3.Scale(local, halfExtentsMm);
            var screen = centre + new Vector2(position.x, position.y) * scale;
            var depth = Mathf.InverseLerp(-maxHalf, maxHalf, position.z);                 // 0 at the back, 1 at the front
            var alpha = Mathf.Lerp(0.40f, 1f, depth) * strength;

            var direction = orientation * new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f).normalized;
            var along = new Vector2(direction.x, direction.y);
            if (along.sqrMagnitude < 0.05f) along = new Vector2(1f, 0.3f);
            along.Normalize();

            var pick = R();
            var unit = size * 0.0065f;                                                     // the size of the smallest mark
            if ((pick -= quality.Needles) < 0f)
            {
                // Silk: a faint soft glint (no straight white line: it reads as a scratch).
                var radius = Mathf.Max(1.2f, unit * (2f + 2f * R()));
                painter.DrawDisc(screen, radius, pale, 0.22f * alpha, radius * 0.9f, false, false, 0f);
            }
            else if ((pick -= quality.Crystals) < 0f)
            {
                // A crystal: a small dark grain with a pale edge.
                var radius = Mathf.Max(0.7f, unit * (1.2f + 1.8f * R()));
                painter.DrawDisc(screen, radius * 1.5f, pale, 0.35f * alpha, 0.8f, false, false, 0f);
                painter.DrawDisc(screen, radius, dark, 0.85f * alpha, 0.6f, false, false, 0f);
            }
            else if ((pick -= quality.Clouds) < 0f)
            {
                // A cloud: a soft pale patch.
                var radius = Mathf.Max(2.5f, unit * (5f + 7f * R()));
                painter.DrawDisc(screen, radius, pale, 0.34f * alpha, radius * 0.9f, false, false, 0f);
            }
            else if ((pick -= quality.Feathers) < 0f)
            {
                // A feather: a soft, pale patch (no straight white line: it reads as a scratch).
                var radius = Mathf.Max(2f, unit * (4f + 5f * R()));
                painter.DrawDisc(screen, radius, pale, 0.20f * alpha, radius * 0.9f, false, false, 0f);
            }
            else
            {
                // A bubble: a small ring.
                var radius = Mathf.Max(0.9f, unit * (1.3f + 1.7f * R()));
                painter.DrawDisc(screen, radius, Color.white, 0.75f * alpha, 0.6f, false, true, Mathf.Max(0.8f, unit * 0.7f));
            }
        }
    }
}
