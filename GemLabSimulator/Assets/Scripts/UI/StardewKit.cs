using UnityEngine;
using UnityEngine.UI;

// The look of the interface, after the reference of the Stardew Valley screens: warm orange frames with a dark outline and a lighter top edge, cream
// insets with a dark border for the figures, deep red-brown digits. The frames are 9-sliced pixel-art drawn once, shown at twice their size so the
// pixels stay square. Used by the clock and the purse of the screen, and by the windows to come.
public static class StardewKit
{
    public static readonly Color Ink = new Color(0.36f, 0.13f, 0.03f, 1f);          // dark brown of the outlines and the small texts
    public static readonly Color Digits = new Color(0.45f, 0.06f, 0.05f, 1f);       // the red-brown of the figures
    public static readonly Color Cream = new Color(0.99f, 0.88f, 0.58f, 1f);
    public static readonly Color Orange = new Color(0.86f, 0.46f, 0.12f, 1f);

    private static Sprite frame;
    private static Sprite inset;

    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    // The orange frame: outline, a light edge on the top and left (a dark one on the bottom and right), two bands of orange, a brown line, then the fill.
    public static Sprite Frame
    {
        get
        {
            if (frame == null) frame = BuildFrame();
            return frame;
        }
    }

    // The cream inset with its dark border, a little darker on its top and left like a hollow.
    public static Sprite Inset
    {
        get
        {
            if (inset == null) inset = BuildInset();
            return inset;
        }
    }

    private static Sprite BuildFrame()
    {
        const int n = 16;
        var t = NewTexture(n, n);
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                var lit = x + (n - 1 - y) < n - 1;      // top-left half (y = 0 is the bottom of the texture)
                Color c;
                if (d == 0) c = C(0.27f, 0.10f, 0.03f);
                else if (d == 1) c = lit ? C(1.00f, 0.76f, 0.40f) : C(0.70f, 0.33f, 0.08f);
                else if (d <= 3) c = C(0.90f, 0.50f, 0.14f);
                else if (d == 4) c = C(0.60f, 0.27f, 0.07f);
                else c = C(0.80f, 0.40f, 0.10f);
                t.SetPixel(x, y, c);
            }
        }
        // Round corners.
        var clear = new Color(0f, 0f, 0f, 0f);
        foreach (var (cx, cy) in new[] { (0, 0), (n - 1, 0), (0, n - 1), (n - 1, n - 1) }) t.SetPixel(cx, cy, clear);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(5, 5, 5, 5));
    }

    private static Sprite BuildInset()
    {
        const int n = 8;
        var t = NewTexture(n, n);
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                var shadow = x + (n - 1 - y) < n;      // top-left half
                Color c;
                if (d == 0) c = Ink;
                else if (d == 1) c = shadow ? C(0.74f, 0.45f, 0.18f) : C(1.00f, 0.95f, 0.72f);
                else c = Cream;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
    }

    private static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
    }

    public static Image MakeFrame(string name, Transform parent, Vector2 topLeft, Vector2 size)
    {
        var image = RuntimeUIKit.MakeBox(name, parent, topLeft, size, Color.white, false);
        image.sprite = Frame;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.5f;
        image.raycastTarget = false;
        return image;
    }

    public static Image MakeInset(string name, Transform parent, Vector2 topLeft, Vector2 size)
    {
        var image = RuntimeUIKit.MakeBox(name, parent, topLeft, size, Color.white, false);
        image.sprite = Inset;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.5f;
        image.raycastTarget = false;
        return image;
    }

    // A small icon from its pixels: '.' is clear, each other letter a colour of the palette.
    public static Sprite FromRows(string[] rows, System.Collections.Generic.Dictionary<char, Color> palette)
    {
        var h = rows.Length;
        var w = rows[0].Length;
        var t = NewTexture(w, h);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var ch = x < rows[y].Length ? rows[y][x] : '.';
                t.SetPixel(x, h - 1 - y, ch == '.' ? new Color(0f, 0f, 0f, 0f) : palette.TryGetValue(ch, out var c) ? c : Color.magenta);
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Texture2D BlankTexture(int w, int h) => NewTexture(w, h);
}
