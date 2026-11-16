using UnityEngine;

// A small drawing surface for the posters: lines (full, dashed, with arrow heads), circles, rectangles and colour gradients, anti-aliased, on a pixel array.
// Positions are in pixels from the top left corner of the picture, like the labels of the poster.
public class PosterCanvas
{
    public readonly int Width;
    public readonly int Height;
    public readonly Color32[] Pixels;

    public PosterCanvas(int width, int height, Color32 background)
    {
        Width = width;
        Height = height;
        Pixels = new Color32[width * height];
        for (var i = 0; i < Pixels.Length; i++) Pixels[i] = background;
    }

    private void Blend(int x, int y, Color color, float alpha)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        alpha *= color.a;
        if (alpha <= 0.002f) return;
        var index = (Height - 1 - y) * Width + x;
        var under = Pixels[index];
        Pixels[index] = new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(under.r, color.r * 255f, alpha)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(under.g, color.g * 255f, alpha)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(under.b, color.b * 255f, alpha)), 255);
    }

    // A line from a to b. `pattern` lists lengths: drawn, gap, drawn, gap... (repeated); null for a full line.
    public void Line(Vector2 a, Vector2 b, float width, Color color, float[] pattern = null)
    {
        var half = width * 0.5f;
        var delta = b - a;
        var length = delta.magnitude;
        if (length < 0.01f) return;
        var dir = delta / length;
        var patternLength = 0f;
        if (pattern != null) foreach (var part in pattern) patternLength += part;

        var minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - half - 1f);
        var maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + half + 1f);
        var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - half - 1f);
        var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + half + 1f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - a;
                var t = Mathf.Clamp(Vector2.Dot(p, dir), 0f, length);
                var distance = (p - dir * t).magnitude;
                var coverage = Mathf.Clamp01(half + 0.5f - distance);
                if (coverage <= 0f) continue;

                if (patternLength > 0f)
                {
                    var along = Mathf.Repeat(t, patternLength);
                    var drawn = false;
                    var cursor = 0f;
                    for (var i = 0; i < pattern.Length; i++)
                    {
                        if (along < cursor + pattern[i]) { drawn = i % 2 == 0; break; }
                        cursor += pattern[i];
                    }
                    if (!drawn) continue;
                }
                Blend(x, y, color, coverage);
            }
    }

    // A line with an arrow head at one or both ends.
    public void Arrow(Vector2 a, Vector2 b, float width, Color color, bool headAtStart, bool headAtEnd, float[] pattern = null, float headLength = 11f, float headWidth = 5f)
    {
        var dir = (b - a).normalized;
        var from = headAtStart ? a + dir * headLength * 0.7f : a;
        var to = headAtEnd ? b - dir * headLength * 0.7f : b;
        Line(from, to, width, color, pattern);
        if (headAtEnd) Triangle(b, b - dir * headLength + Perpendicular(dir) * headWidth, b - dir * headLength - Perpendicular(dir) * headWidth, color);
        if (headAtStart) Triangle(a, a + dir * headLength + Perpendicular(dir) * headWidth, a + dir * headLength - Perpendicular(dir) * headWidth, color);
    }

    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);

    public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        var minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - 1f);
        var maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + 1f);
        var minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - 1f);
        var maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + 1f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var d = Mathf.Min(EdgeSide(a, b, p, c), Mathf.Min(EdgeSide(b, c, p, a), EdgeSide(c, a, p, b)));
                Blend(x, y, color, Mathf.Clamp01(d + 0.5f));
            }
    }

    // Signed distance of p to the edge a-b, positive on the side of `inside`.
    private static float EdgeSide(Vector2 a, Vector2 b, Vector2 p, Vector2 inside)
    {
        var edge = b - a;
        var normal = Perpendicular(edge).normalized;
        if (Vector2.Dot(normal, inside - a) < 0f) normal = -normal;
        return Vector2.Dot(normal, p - a);
    }

    // A full disc; with `edge` set the colour goes from `color` at the centre (offset a little towards the top left, like a light) to `edge` at the rim.
    public void Disc(Vector2 center, float radius, Color color, Color? edge = null)
    {
        var minX = Mathf.FloorToInt(center.x - radius - 1f);
        var maxX = Mathf.CeilToInt(center.x + radius + 1f);
        var minY = Mathf.FloorToInt(center.y - radius - 1f);
        var maxY = Mathf.CeilToInt(center.y + radius + 1f);
        var light = center + new Vector2(-radius * 0.25f, -radius * 0.25f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var coverage = Mathf.Clamp01(radius + 0.5f - (p - center).magnitude);
                if (coverage <= 0f) continue;
                var tint = color;
                if (edge.HasValue) tint = Color.Lerp(color, edge.Value, Mathf.Clamp01((p - light).magnitude / (radius * 1.25f)));
                Blend(x, y, tint, coverage);
            }
    }

    public void Ring(Vector2 center, float radius, float width, Color color)
    {
        var half = width * 0.5f;
        var minX = Mathf.FloorToInt(center.x - radius - width - 1f);
        var maxX = Mathf.CeilToInt(center.x + radius + width + 1f);
        var minY = Mathf.FloorToInt(center.y - radius - width - 1f);
        var maxY = Mathf.CeilToInt(center.y + radius + width + 1f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var distance = Mathf.Abs((p - center).magnitude - radius);
                Blend(x, y, color, Mathf.Clamp01(half + 0.5f - distance));
            }
    }

    // Signed distance to a rounded rectangle (negative inside).
    private static float RoundedDistance(Vector2 p, Rect rect, float corner)
    {
        var half = rect.size * 0.5f;
        var q = new Vector2(Mathf.Abs(p.x - rect.center.x), Mathf.Abs(p.y - rect.center.y)) - half + new Vector2(corner, corner);
        var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner;
    }

    public void RectOutline(Rect rect, float width, Color color, float corner = 0f)
    {
        var half = width * 0.5f;
        var minX = Mathf.FloorToInt(rect.xMin - width - 1f);
        var maxX = Mathf.CeilToInt(rect.xMax + width + 1f);
        var minY = Mathf.FloorToInt(rect.yMin - width - 1f);
        var maxY = Mathf.CeilToInt(rect.yMax + width + 1f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var distance = Mathf.Abs(RoundedDistance(new Vector2(x + 0.5f, y + 0.5f), rect, corner));
                Blend(x, y, color, Mathf.Clamp01(half + 0.5f - distance));
            }
    }

    public void RectFill(Rect rect, Color color, float corner = 0f)
    {
        var minX = Mathf.FloorToInt(rect.xMin - 1f);
        var maxX = Mathf.CeilToInt(rect.xMax + 1f);
        var minY = Mathf.FloorToInt(rect.yMin - 1f);
        var maxY = Mathf.CeilToInt(rect.yMax + 1f);
        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
                Blend(x, y, color, Mathf.Clamp01(0.5f - RoundedDistance(new Vector2(x + 0.5f, y + 0.5f), rect, corner)));
    }

    // A colour sample of the poster: a rectangle with a horizontal gradient through the given colours, and a thin dark frame.
    public void Swatch(Rect rect, params Color[] colors)
    {
        var minX = Mathf.FloorToInt(rect.xMin);
        var maxX = Mathf.CeilToInt(rect.xMax);
        var minY = Mathf.FloorToInt(rect.yMin);
        var maxY = Mathf.CeilToInt(rect.yMax);
        for (var y = minY; y < maxY; y++)
            for (var x = minX; x < maxX; x++)
            {
                var t = Mathf.Clamp01((x + 0.5f - rect.xMin) / rect.width) * (colors.Length - 1);
                var i = Mathf.Min(colors.Length - 2, (int)t);
                var shade = 1f + 0.12f * (1f - Mathf.Abs((y + 0.5f - rect.yMin) / rect.height - 0.5f) * 2f);   // a slight light in the middle of the band
                var tint = Color.Lerp(colors[i], colors[i + 1], t - i);
                Blend(x, y, new Color(Mathf.Min(1f, tint.r * shade), Mathf.Min(1f, tint.g * shade), Mathf.Min(1f, tint.b * shade), 1f), 1f);
            }
        RectOutline(rect, 1f, new Color(0.18f, 0.15f, 0.14f));
    }
}
