using System.Collections.Generic;
using UnityEngine;

// The symbols of a clarity plot, drawn in white on a small transparent texture so they can be tinted: red for what is inside the stone,
// green for what is on its surface.
public static class PlotSymbols
{
    private const int Size = 24;

    private static readonly Dictionary<PlotKind, Sprite> Cache = new Dictionary<PlotKind, Sprite>();
    private static Sprite ring;

    public static readonly Color Internal = new Color(0.86f, 0.14f, 0.14f, 1f);
    public static readonly Color External = new Color(0.12f, 0.62f, 0.24f, 1f);

    public static Color ColorOf(PlotKind kind) => DiamondGradeModel.IsInternal(kind) ? Internal : External;

    public static Sprite Get(PlotKind kind)
    {
        if (Cache.TryGetValue(kind, out var cached) && cached != null) return cached;

        var pixels = new Color32[Size * Size];
        switch (kind)
        {
            case PlotKind.Point:
                Disc(pixels, 11.5f, 11.5f, 2.6f);
                break;
            case PlotKind.Crystal:
                Circle(pixels, 11.5f, 11.5f, 6.5f, 1.4f);
                Disc(pixels, 11.5f, 11.5f, 1.2f);
                break;
            case PlotKind.Cloud:
                foreach (var p in new[] { new Vector2(-6, 2), new Vector2(-2, 6), new Vector2(3, 4), new Vector2(6, -1), new Vector2(0, -2), new Vector2(-4, -5), new Vector2(3, -6) })
                    Disc(pixels, 11.5f + p.x, 11.5f + p.y, 1.5f);
                break;
            case PlotKind.Feather:
                Line(pixels, 4, 4, 20, 20, 1.1f);
                foreach (var t in new[] { 0.3f, 0.5f, 0.7f })
                {
                    var cx = Mathf.Lerp(4f, 20f, t);
                    Line(pixels, cx, cx, cx + 5f, cx - 5f, 0.9f);
                }
                break;
            case PlotKind.Needle:
                Line(pixels, 3, 12, 21, 12, 0.9f);
                Disc(pixels, 3f, 12f, 1.4f);
                Disc(pixels, 21f, 12f, 1.4f);
                break;
            case PlotKind.Knot:
                Line(pixels, 5, 5, 18, 18, 1.2f);
                Line(pixels, 5, 18, 18, 5, 1.2f);
                Disc(pixels, 11.5f, 11.5f, 2f);
                break;
            case PlotKind.Natural:
                Line(pixels, 12, 20, 3, 5, 1.1f);
                Line(pixels, 3, 5, 21, 5, 1.1f);
                Line(pixels, 21, 5, 12, 20, 1.1f);
                break;
            case PlotKind.Chip:
                Line(pixels, 4, 19, 12, 6, 1.3f);
                Line(pixels, 12, 6, 20, 19, 1.3f);
                break;
            case PlotKind.Scratch:
                Line(pixels, 3, 8, 8, 16, 1.1f);
                Line(pixels, 8, 16, 13, 8, 1.1f);
                Line(pixels, 13, 8, 18, 16, 1.1f);
                Line(pixels, 18, 16, 21, 12, 1.1f);
                break;
            case PlotKind.Abrasion:
                Line(pixels, 3, 12, 7, 12, 1.3f);
                Line(pixels, 10, 12, 14, 12, 1.3f);
                Line(pixels, 17, 12, 21, 12, 1.3f);
                break;
            default:
                Line(pixels, 6, 6, 17, 6, 1.1f);
                Line(pixels, 17, 6, 17, 17, 1.1f);
                Line(pixels, 17, 17, 6, 17, 1.1f);
                Line(pixels, 6, 17, 6, 6, 1.1f);
                break;
        }

        var sprite = FromPixels(pixels);
        Cache[kind] = sprite;
        return sprite;
    }

    // The ring of the loupe that follows the pointer over the plot.
    public static Sprite Ring
    {
        get
        {
            if (ring != null) return ring;
            const int size = 96;
            var pixels = new Color32[size * size];
            Circle(pixels, size, size / 2f - 0.5f, size / 2f - 0.5f, size / 2f - 3f, 2.4f, new Color32(255, 255, 255, 255));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return ring;
        }
    }

    private static Sprite FromPixels(Color32[] pixels)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Plot(Color32[] pixels, int size, int x, int y, byte alpha)
    {
        if (x < 0 || y < 0 || x >= size || y >= size) return;
        var index = y * size + x;
        if (pixels[index].a < alpha) pixels[index] = new Color32(255, 255, 255, alpha);
    }

    private static void Disc(Color32[] pixels, float cx, float cy, float radius)
    {
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                var a = Mathf.Clamp01(radius + 0.5f - d);
                if (a > 0f) Plot(pixels, Size, x, y, (byte)(a * 255f));
            }
        }
    }

    private static void Circle(Color32[] pixels, float cx, float cy, float radius, float width)
    {
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - radius);
                var a = Mathf.Clamp01(width * 0.5f + 0.5f - d);
                if (a > 0f) Plot(pixels, Size, x, y, (byte)(a * 255f));
            }
        }
    }

    private static void Circle(Color32[] pixels, int size, float cx, float cy, float radius, float width, Color32 color)
    {
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - radius);
                var a = Mathf.Clamp01(width * 0.5f + 0.5f - d);
                if (a > 0f) pixels[y * size + x] = new Color32(color.r, color.g, color.b, (byte)(a * 255f));
            }
        }
    }

    private static void Line(Color32[] pixels, float x0, float y0, float x1, float y1, float width)
    {
        var a = new Vector2(x0, y0);
        var b = new Vector2(x1, y1);
        var ab = b - a;
        var lengthSquared = Mathf.Max(ab.sqrMagnitude, 0.0001f);
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var p = new Vector2(x, y);
                var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
                var d = Vector2.Distance(p, a + ab * t);
                var alpha = Mathf.Clamp01(width * 0.5f + 0.5f - d);
                if (alpha > 0f) Plot(pixels, Size, x, y, (byte)(alpha * 255f));
            }
        }
    }
}
