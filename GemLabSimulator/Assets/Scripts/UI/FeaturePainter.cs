using UnityEngine;

// Paints the parts of an inclusion (InclusionShapes) into a pixel buffer, with soft edges.
// Opaque: the microscope's view. Composite: the picture of the stone itself, so the inclusions only show where there
// is stone (pixels with no stone stay untouched, and the stone's own opacity is kept). Overlay: a separate transparent layer.
public class FeaturePainter
{
    public enum Mode { Opaque, Composite, Overlay }

    private readonly Color32[] buffer;
    private readonly int width;
    private readonly int height;
    private readonly Mode mode;

    public FeaturePainter(Color32[] buffer, int width, int height, Mode mode)
    {
        this.buffer = buffer;
        this.width = width;
        this.height = height;
        this.mode = mode;
    }

    // `uAxis` and `vAxis` map the part's mm frame to pixels (they carry the scale and the foreshortening).
    // Reflected light: inclusions are lighter or darker than the host; dark field (`additive`): they glow.
    public void DrawPart(InclusionPart p, bool surface, Vector2 center, Vector2 uAxis, Vector2 vAxis,
        float alpha, float soft, bool additive, Color tint, float pxPerMm)
    {
        Vector2 Map(Vector2 local) => center + uAxis * local.x + vAxis * local.y;

        Color color;
        float strength;
        if (p.Dark)
        {
            color = new Color(0.04f, 0.04f, 0.05f);
            strength = additive ? 0.12f : 0.9f;
            if (additive) color = new Color(0.8f, 0.85f, 0.9f);
        }
        else if (additive)
        {
            color = tint;
            strength = surface ? 0.35f : 1f;
        }
        else
        {
            color = Color.Lerp(tint, Color.white, 0.55f);
            strength = surface ? 0.95f : 0.8f;
        }

        // An inclusion with a color of its own (a golden rutile, a dark gas rim) keeps it, lightly mixed with the host's.
        if (p.HasColor && !p.Dark)
            color = Color.Lerp(p.Tint, tint, additive ? 0.3f : 0.12f);

        strength *= p.Strength;
        var a = alpha * strength;

        switch (p.Type)
        {
            case InclusionPart.Shape.Line:
                DrawLine(Map(p.A), Map(p.B), Mathf.Max(1f, p.Width * pxPerMm), color, a, soft, additive);
                break;
            case InclusionPart.Shape.Dot:
                DrawDisc(Map(p.A), Mathf.Max(1f, p.Radius * pxPerMm), color, a, soft, additive, false, 0f);
                break;
            case InclusionPart.Shape.Ring:
                DrawDisc(Map(p.A), Mathf.Max(2f, p.Radius * pxPerMm), color, a * 0.9f, soft, additive, true, Mathf.Max(1f, p.Width * pxPerMm));
                break;
            case InclusionPart.Shape.Band:
                DrawLine(Map(p.A), Map(p.B), Mathf.Max(2f, p.Width * pxPerMm),
                    additive ? Color.Lerp(tint, Color.white, 0.2f) : Color.Lerp(tint, Color.black, 0.45f),
                    a * (additive ? 0.18f : 0.22f), soft + 3f, additive);
                break;
            case InclusionPart.Shape.Polygon:
            {
                var points = new Vector2[p.Points.Length];
                for (var i = 0; i < points.Length; i++) points[i] = Map(p.Points[i]);

                Color fillColor;
                if (p.Dark || p.HasColor) fillColor = additive ? color * 0.6f : color;
                else fillColor = Color.Lerp(tint, Color.black, additive ? 0.4f : 0.55f);
                var fillAlpha = a * (p.Fill >= 0f ? p.Fill : (additive ? 0.25f : 0.5f));
                if (fillAlpha > 0f) DrawPolygonFill(points, fillColor, fillAlpha, soft, additive, p.Iridescence, pxPerMm);

                // Outline (a purely iridescent film has none of its own: it is the edge of the sheet that shows).
                var outline = p.Iridescence > 0 ? 0.35f : 1f;
                for (var i = 0; i < points.Length; i++)
                    DrawLine(points[i], points[(i + 1) % points.Length], Mathf.Max(1f, 0.008f * pxPerMm), color, a * outline, soft, additive);
                break;
            }
        }
    }

    private void Blend(int x, int y, Color c, float a, bool additive)
    {
        if (a <= 0f || x < 0 || y < 0 || x >= width || y >= height) return;
        a = Mathf.Clamp01(a);

        var index = y * width + x;
        var existing = buffer[index];

        if (mode == Mode.Composite)
        {
            if (existing.a == 0) return;
            buffer[index] = new Color32(
                (byte)Mathf.Lerp(existing.r, c.r * 255f, a),
                (byte)Mathf.Lerp(existing.g, c.g * 255f, a),
                (byte)Mathf.Lerp(existing.b, c.b * 255f, a), existing.a);
            return;
        }

        if (mode == Mode.Overlay)
        {
            var existingAlpha = existing.a / 255f;
            var outAlpha = a + existingAlpha * (1f - a);
            if (outAlpha <= 0f) return;
            var keep = existingAlpha * (1f - a);
            buffer[index] = new Color32(
                (byte)(Mathf.Clamp01((c.r * a + existing.r / 255f * keep) / outAlpha) * 255f + 0.5f),
                (byte)(Mathf.Clamp01((c.g * a + existing.g / 255f * keep) / outAlpha) * 255f + 0.5f),
                (byte)(Mathf.Clamp01((c.b * a + existing.b / 255f * keep) / outAlpha) * 255f + 0.5f),
                (byte)(outAlpha * 255f + 0.5f));
            return;
        }

        if (additive)
        {
            buffer[index] = new Color32(
                (byte)Mathf.Min(255, existing.r + c.r * a * 255f),
                (byte)Mathf.Min(255, existing.g + c.g * a * 255f),
                (byte)Mathf.Min(255, existing.b + c.b * a * 255f), 255);
        }
        else
        {
            buffer[index] = new Color32(
                (byte)Mathf.Lerp(existing.r, c.r * 255f, a),
                (byte)Mathf.Lerp(existing.g, c.g * 255f, a),
                (byte)Mathf.Lerp(existing.b, c.b * 255f, a), 255);
        }
    }

    public void DrawLine(Vector2 a, Vector2 b, float lineWidth, Color color, float alpha, float soft, bool additive)
    {
        var reach = lineWidth * 0.5f + soft;
        var minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - reach);
        var maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + reach);
        var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - reach);
        var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + reach);
        if (maxX < 0 || maxY < 0 || minX >= width || minY >= height) return;

        var ab = b - a;
        var lengthSq = Mathf.Max(ab.sqrMagnitude, 1e-4f);
        var horizontal = Mathf.Abs(ab.x) >= Mathf.Abs(ab.y);
        var slope = horizontal
            ? (Mathf.Abs(ab.x) < 1e-4f ? 0f : ab.y / ab.x)
            : (Mathf.Abs(ab.y) < 1e-4f ? 0f : ab.x / ab.y);
        var spread = reach * Mathf.Sqrt(1f + slope * slope) + 1f;

        // Walk along the segment's main axis and only visit the pixels close to it.
        var from = horizontal ? Mathf.Max(0, minX) : Mathf.Max(0, minY);
        var to = horizontal ? Mathf.Min(width - 1, maxX) : Mathf.Min(height - 1, maxY);
        for (var step = from; step <= to; step++)
        {
            var along = step + 0.5f;
            float t;
            if (horizontal) t = Mathf.Abs(ab.x) < 1e-4f ? 0f : Mathf.Clamp01((along - a.x) / ab.x);
            else t = Mathf.Abs(ab.y) < 1e-4f ? 0f : Mathf.Clamp01((along - a.y) / ab.y);

            var other = horizontal ? a.y + ab.y * t : a.x + ab.x * t;
            var lo = Mathf.Max(0, Mathf.FloorToInt(other - spread));
            var hi = Mathf.Min(horizontal ? height - 1 : width - 1, Mathf.CeilToInt(other + spread));
            for (var k = lo; k <= hi; k++)
            {
                var x = horizontal ? step : k;
                var y = horizontal ? k : step;
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var distance = (p - (a + ab * u)).magnitude;
                var coverage = Mathf.Clamp01((lineWidth * 0.5f + soft - distance) / soft);
                if (coverage > 0f) Blend(x, y, color, alpha * coverage, additive);
            }
        }
    }

    public void DrawDisc(Vector2 center, float radius, Color color, float alpha, float soft, bool additive, bool ring, float thickness)
    {
        var reach = radius + thickness + soft;
        var minX = Mathf.FloorToInt(center.x - reach);
        var maxX = Mathf.CeilToInt(center.x + reach);
        var minY = Mathf.FloorToInt(center.y - reach);
        var maxY = Mathf.CeilToInt(center.y + reach);
        if (maxX < 0 || maxY < 0 || minX >= width || minY >= height) return;

        for (var y = Mathf.Max(0, minY); y <= Mathf.Min(height - 1, maxY); y++)
        {
            for (var x = Mathf.Max(0, minX); x <= Mathf.Min(width - 1, maxX); x++)
            {
                var distance = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude;
                float coverage;
                if (ring) coverage = Mathf.Clamp01((thickness * 0.5f + soft - Mathf.Abs(distance - radius)) / soft);
                else coverage = Mathf.Clamp01((radius + soft - distance) / soft);
                if (coverage > 0f) Blend(x, y, color, alpha * coverage, additive);
            }
        }
    }

    // `iridescence`: 1 colors the fill like a thin film (rainbow bands), 2 like the flash of a filled fracture (orange to blue).
    public void DrawPolygonFill(Vector2[] points, Color color, float alpha, float soft, bool additive, int iridescence, float pxPerMm)
    {
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        foreach (var p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        var period = Mathf.Max(4f, pxPerMm * 0.35f);
        for (var y = Mathf.Max(0, Mathf.FloorToInt(minY - soft)); y <= Mathf.Min(height - 1, Mathf.CeilToInt(maxY + soft)); y++)
        {
            for (var x = Mathf.Max(0, Mathf.FloorToInt(minX - soft)); x <= Mathf.Min(width - 1, Mathf.CeilToInt(maxX + soft)); x++)
            {
                if (!PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points)) continue;

                var fill = color;
                if (iridescence == 1)
                {
                    fill = Color.HSVToRGB(Mathf.Repeat((x * 0.8f + y * 0.5f) / period, 1f), 0.55f, 1f);
                }
                else if (iridescence == 2)
                {
                    var wave = 0.5f + 0.5f * Mathf.Sin((x * 0.9f + y * 0.4f) / period * Mathf.PI * 2f);
                    fill = Color.HSVToRGB(Mathf.Lerp(0.07f, 0.58f, wave), 0.7f, 1f);
                }
                Blend(x, y, fill, alpha, additive);
            }
        }
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];
            if ((pi.y > point.y) != (pj.y > point.y) && point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                inside = !inside;
        }
        return inside;
    }
}
