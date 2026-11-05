using System.Collections.Generic;
using UnityEngine;

// The pixel-art of the mailbox: one envelope per kind of letter, the gemologist's fold that holds a stone, and the logo of the mail. Drawn once.
public static class MailSprites
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private static readonly Color Edge = new Color(0.22f, 0.17f, 0.12f, 1f);

    public static Sprite Envelope(LetterKind kind) => Get("env" + kind, () => DrawEnvelope(MailLetters.BodyColor(kind), MailLetters.FlapColor(kind), MailLetters.SealColor(kind), false));

    // The logo of the mailbox: a cream envelope sealed with a blue gem.
    public static Sprite Logo() => Get("logo", () => DrawEnvelope(new Color(0.97f, 0.94f, 0.85f), new Color(0.84f, 0.78f, 0.64f), new Color(0.30f, 0.55f, 0.95f), true));

    // The fold of paper a gemologist wraps a stone in: three creases, and the stone showing at the middle.
    public static Sprite Fold() => Get("fold", DrawFold);

    private static Sprite Get(string key, System.Func<Sprite> make)
    {
        if (!Cache.TryGetValue(key, out var sprite) || sprite == null)
        {
            sprite = make();
            Cache[key] = sprite;
        }
        return sprite;
    }

    private static Texture2D NewTexture(int w, int h)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color(0f, 0f, 0f, 0f);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++) texture.SetPixel(x, y, clear);
        return texture;
    }

    // 56 x 40, y = 0 at the bottom.
    private static Sprite DrawEnvelope(Color body, Color flap, Color seal, bool gemSeal)
    {
        const int w = 56;
        const int h = 40;
        var t = NewTexture(w, h);
        var shade = Color.Lerp(body, Color.black, 0.12f);

        for (var y = 2; y < h - 2; y++)
            for (var x = 2; x < w - 2; x++)
            {
                var border = x == 2 || x == w - 3 || y == 2 || y == h - 3;
                t.SetPixel(x, y, border ? Edge : body);
            }

        // The flap: a triangle from the top corners down to the middle, a little darker, with an edge.
        var tipY = h / 2 - 2;
        for (var x = 3; x < w - 3; x++)
        {
            var distance = Mathf.Abs(x - w / 2f) / (w / 2f - 3f);
            var lineY = Mathf.RoundToInt(Mathf.Lerp(tipY, h - 3, distance));
            for (var y = lineY; y < h - 3; y++) t.SetPixel(x, y, flap);
            t.SetPixel(x, lineY, Edge);
            if (lineY - 1 > 3) t.SetPixel(x, lineY - 1, Edge);
        }

        // The folds of the bottom of the envelope.
        for (var i = 0; i < 17; i++)
        {
            t.SetPixel(3 + i, 3 + i / 2, shade);
            t.SetPixel(w - 4 - i, 3 + i / 2, shade);
        }

        // The seal on the tip of the flap.
        var cx = w / 2;
        var cy = tipY - 1;
        for (var y = -3; y <= 3; y++)
            for (var x = -3; x <= 3; x++)
            {
                var d = x * x + y * y;
                if (d > 10) continue;
                t.SetPixel(cx + x, cy + y, d > 7 ? Edge : seal);
            }
        if (gemSeal)
        {
            t.SetPixel(cx, cy + 1, Color.white);
            t.SetPixel(cx - 1, cy, Color.Lerp(seal, Color.white, 0.5f));
        }

        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // 44 x 44.
    private static Sprite DrawFold()
    {
        const int size = 44;
        var t = NewTexture(size, size);
        var paper = new Color(0.98f, 0.96f, 0.90f, 1f);
        var crease = new Color(0.80f, 0.76f, 0.66f, 1f);

        for (var y = 3; y < size - 3; y++)
            for (var x = 3; x < size - 3; x++)
            {
                var border = x == 3 || x == size - 4 || y == 3 || y == size - 4;
                t.SetPixel(x, y, border ? Edge : paper);
            }

        // Three horizontal creases and a vertical fold, like the parcels of the stone dealers.
        for (var x = 4; x < size - 4; x++)
        {
            t.SetPixel(x, 15, crease);
            t.SetPixel(x, 28, crease);
        }
        for (var y = 4; y < size - 4; y++) t.SetPixel(size / 2, y, crease);
        for (var i = 0; i < 9; i++)
        {
            t.SetPixel(4 + i, 28 + i, crease);
            t.SetPixel(size - 5 - i, 28 + i, crease);
        }

        // The stone, showing in the middle.
        var blue = new Color(0.40f, 0.65f, 0.98f, 1f);
        var light = new Color(0.82f, 0.92f, 1f, 1f);
        for (var y = -4; y <= 4; y++)
            for (var x = -5; x <= 5; x++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) * 1.3f > 5.5f) continue;
                t.SetPixel(size / 2 + x, 21 + y, Mathf.Abs(x) + Mathf.Abs(y) * 1.3f > 4.4f ? Edge : blue);
            }
        t.SetPixel(size / 2 - 1, 23, light);
        t.SetPixel(size / 2, 23, light);
        t.SetPixel(size / 2 - 2, 22, light);

        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
