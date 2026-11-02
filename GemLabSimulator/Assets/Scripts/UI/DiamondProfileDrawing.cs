using UnityEngine;

// The side view of a diamond drawn from its proportions (crown, girdle, pavilion, table and culet, in % of the diameter), used by the
// proportion tab of the grading desk and by the certificate.
public static class DiamondProfileDrawing
{
    // scale is the number of pixels for 1 % of the diameter. centerX and top place the table; rulers adds the graduations used to read the
    // proportions. A texture can be given to be redrawn instead of creating a new one.
    public static Texture2D Draw(DiamondGrade grade, int width, int height, float scale, float centerX, float top, bool rulers, Texture2D reuse = null)
    {
        var pixels = new Color32[width * height];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(247, 248, 251, 255);

        var ink = new Color32(40, 50, 80, 255);
        var tableHalf = grade.TablePercent * 0.5f * scale;
        var girdleHalf = 50f * scale;
        var culetHalf = grade.CuletPercent * 0.5f * scale;
        var yCrown = top + grade.CrownPercent * scale;
        var yGirdle = yCrown + grade.GirdlePercent * scale;
        var yCulet = yGirdle + grade.PavilionPercent * scale;

        var fill = new Color32(205, 226, 244, 255);
        for (var y = Mathf.RoundToInt(top); y <= Mathf.RoundToInt(yCulet); y++)
        {
            float half;
            if (y <= yCrown) half = Mathf.Lerp(tableHalf, girdleHalf, (y - top) / Mathf.Max(yCrown - top, 1f));
            else if (y <= yGirdle) half = girdleHalf;
            else half = Mathf.Lerp(girdleHalf, culetHalf, (y - yGirdle) / Mathf.Max(yCulet - yGirdle, 1f));
            for (var x = Mathf.RoundToInt(centerX - half); x <= Mathf.RoundToInt(centerX + half); x++) SetPixel(pixels, width, height, x, y, fill);
        }

        var left = centerX - girdleHalf;
        var right = centerX + girdleHalf;
        Line(pixels, width, height, centerX - tableHalf, top, centerX + tableHalf, top, ink);
        Line(pixels, width, height, centerX - tableHalf, top, left, yCrown, ink);
        Line(pixels, width, height, centerX + tableHalf, top, right, yCrown, ink);
        Line(pixels, width, height, left, yCrown, right, yCrown, ink);
        Line(pixels, width, height, left, yGirdle, right, yGirdle, ink);
        Line(pixels, width, height, left, yCrown, left, yGirdle, ink);
        Line(pixels, width, height, right, yCrown, right, yGirdle, ink);
        Line(pixels, width, height, left, yGirdle, centerX - culetHalf, yCulet, ink);
        Line(pixels, width, height, right, yGirdle, centerX + culetHalf, yCulet, ink);
        Line(pixels, width, height, centerX - culetHalf, yCulet, centerX + culetHalf, yCulet, ink);

        // A few facet lines to read it as a brilliant.
        var faint = new Color32(120, 150, 190, 255);
        Line(pixels, width, height, centerX - tableHalf * 0.5f, top, left + 20f, yCrown, faint);
        Line(pixels, width, height, centerX + tableHalf * 0.5f, top, right - 20f, yCrown, faint);
        Line(pixels, width, height, left + (centerX - culetHalf - left) * 0.45f, yGirdle + (yCulet - yGirdle) * 0.45f,
            right - (right - centerX - culetHalf) * 0.45f, yGirdle + (yCulet - yGirdle) * 0.45f, faint);

        if (rulers)
        {
            // The vertical ruler gives the depths in % of the diameter, the horizontal one the widths.
            var ruler = new Color32(90, 96, 120, 255);
            Line(pixels, width, height, 34f, top, 34f, top + 70f * scale, ruler);
            for (var pct = 0; pct <= 70; pct++)
                Line(pixels, width, height, 34f, top + pct * scale, pct % 5 == 0 ? 26f : 30f, top + pct * scale, ruler);
            Line(pixels, width, height, left, 32f, right, 32f, ruler);
            for (var pct = 0; pct <= 100; pct += 5)
                Line(pixels, width, height, left + pct * scale, 32f, left + pct * scale, pct % 25 == 0 ? 24f : 28f, ruler);
        }

        var texture = reuse;
        if (texture == null || texture.width != width || texture.height != height)
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // Pixels are given with y counted downward from the top of the picture.
    private static void SetPixel(Color32[] pixels, int width, int height, int x, int yDown, Color32 color)
    {
        if (x < 0 || x >= width || yDown < 0 || yDown >= height) return;
        pixels[(height - 1 - yDown) * width + x] = color;
    }

    private static void Line(Color32[] pixels, int width, int height, float x0, float y0, float x1, float y1, Color32 color)
    {
        var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f) + 1;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            SetPixel(pixels, width, height, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), color);
        }
    }
}
