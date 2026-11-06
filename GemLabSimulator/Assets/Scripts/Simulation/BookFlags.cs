using System;
using System.Collections.Generic;
using UnityEngine;

// The flags of the countries of the deposits, for the index of the Atlas. The twenty flags of the markets (Assets/Resources/Flags) are used when the country
// has one; the others are drawn here in pixels (24 x 16), in the same simple style. A deposit that is not a country (an ocean, an industrial process)
// has no flag.
public static class BookFlags
{
    private const int W = 24;
    private const int H = 16;

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    private static readonly Color White = Color.white;
    private static readonly Color Red = C(0.82f, 0.11f, 0.16f);
    private static readonly Color Blue = C(0.05f, 0.22f, 0.58f);
    private static readonly Color SkyBlue = C(0.42f, 0.70f, 0.90f);
    private static readonly Color Green = C(0.05f, 0.52f, 0.25f);
    private static readonly Color Yellow = C(1f, 0.80f, 0.05f);

    // The name of a deposit without the region in brackets, and with the country its name stands for.
    public static string CountryOf(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return "";
        var at = origin.IndexOf(" (", StringComparison.Ordinal);
        var country = at > 0 ? origin.Substring(0, at) : origin;
        switch (country)
        {
            case "Birmanie": return "Myanmar";
            case "Sibérie": return "Russie";
        }
        return country;
    }

    // The flag of the country of a deposit, or null.
    public static Sprite Get(string origin)
    {
        var country = CountryOf(origin);
        if (country.Length == 0) return null;
        if (Cache.TryGetValue(country, out var cached)) return cached;

        var sprite = MarketFlagLookup.GetFlag(country);
        if (sprite == null)
        {
            var pixels = Draw(country);
            if (pixels != null) sprite = ToSprite(pixels);
        }
        Cache[country] = sprite;
        return sprite;
    }

    private static Sprite ToSprite(Color[] pixels)
    {
        var texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
                texture.SetPixel(x, H - 1 - y, pixels[y * W + x]);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---- Drawing -----------------------------------------------------------------------------------------------------

    private static void Fill(Color[] p, Color color)
    {
        for (var i = 0; i < p.Length; i++) p[i] = color;
    }

    private static void Block(Color[] p, int x0, int y0, int x1, int y1, Color color)
    {
        for (var y = Mathf.Max(0, y0); y <= Mathf.Min(H - 1, y1); y++)
            for (var x = Mathf.Max(0, x0); x <= Mathf.Min(W - 1, x1); x++)
                p[y * W + x] = color;
    }

    // Horizontal bands from the top, each with its number of rows.
    private static void HBands(Color[] p, int[] rows, params Color[] colors)
    {
        var y = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            Block(p, 0, y, W - 1, y + rows[i] - 1, colors[i]);
            y += rows[i];
        }
    }

    // Vertical bands from the left, each with its number of columns.
    private static void VBands(Color[] p, int[] columns, params Color[] colors)
    {
        var x = 0;
        for (var i = 0; i < columns.Length; i++)
        {
            Block(p, x, 0, x + columns[i] - 1, H - 1, colors[i]);
            x += columns[i];
        }
    }

    private static void Disc(Color[] p, float cx, float cy, float radius, Color color)
    {
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius) p[y * W + x] = color;
            }
    }

    // A cross of the Nordic kind: the vertical bar at the given column.
    private static void NordicCross(Color[] p, int barX, int thickness, Color color)
    {
        Block(p, barX, 0, barX + thickness - 1, H - 1, color);
        Block(p, 0, 6, W - 1, 6 + thickness - 1, color);
    }

    private static Color[] Draw(string country)
    {
        var p = new Color[W * H];
        switch (country)
        {
            case "Argentine":
                HBands(p, new[] { 5, 6, 5 }, SkyBlue, White, SkyBlue);
                Disc(p, 11.5f, 7.5f, 2f, C(0.96f, 0.72f, 0.2f));
                return p;
            case "Autriche":
                HBands(p, new[] { 5, 6, 5 }, Red, White, Red);
                return p;
            case "Bolivie":
                HBands(p, new[] { 5, 6, 5 }, Red, Yellow, Green);
                return p;
            case "Botswana":
                Fill(p, SkyBlue);
                Block(p, 0, 6, W - 1, 10, White);
                Block(p, 0, 7, W - 1, 9, Color.black);
                return p;
            case "Cambodge":
                HBands(p, new[] { 4, 8, 4 }, Blue, Red, Blue);
                Block(p, 8, 6, 15, 9, White);
                Block(p, 10, 4, 13, 6, White);
                return p;
            case "Chili":
                HBands(p, new[] { 8, 8 }, White, Red);
                Block(p, 0, 0, 7, 7, Blue);
                foreach (var (sx, sy) in new[] { (3, 2), (2, 3), (3, 3), (4, 3), (3, 4) }) p[sy * W + sx] = White;
                return p;
            case "Espagne":
                HBands(p, new[] { 4, 8, 4 }, Red, Yellow, Red);
                return p;
            case "Finlande":
                Fill(p, White);
                NordicCross(p, 6, 4, Blue);
                return p;
            case "Guatemala":
                VBands(p, new[] { 8, 8, 8 }, SkyBlue, White, SkyBlue);
                return p;
            case "Iran":
                HBands(p, new[] { 5, 6, 5 }, Green, White, Red);
                return p;
            case "Islande":
                Fill(p, Blue);
                NordicCross(p, 5, 4, White);
                Block(p, 6, 0, 7, H - 1, Red);
                Block(p, 0, 7, W - 1, 8, Red);
                return p;
            case "Norvège":
                Fill(p, Red);
                NordicCross(p, 5, 4, White);
                Block(p, 6, 0, 7, H - 1, Blue);
                Block(p, 0, 7, W - 1, 8, Blue);
                return p;
            case "Suède":
                Fill(p, C(0f, 0.36f, 0.64f));
                NordicCross(p, 6, 4, Yellow);
                return p;
            case "Japon":
                Fill(p, White);
                Disc(p, 11.5f, 7.5f, 4f, Red);
                return p;
            case "Mexique":
                VBands(p, new[] { 8, 8, 8 }, C(0f, 0.41f, 0.26f), White, Red);
                Disc(p, 11.5f, 7.5f, 1.6f, C(0.55f, 0.40f, 0.20f));
                return p;
            case "Pérou":
                VBands(p, new[] { 8, 8, 8 }, Red, White, Red);
                return p;
            case "Nigeria":
                VBands(p, new[] { 8, 8, 8 }, Green, White, Green);
                return p;
            case "Namibie":
                for (var y = 0; y < H; y++)
                    for (var x = 0; x < W; x++)
                    {
                        var s = x / (float)(W - 1) + y / (float)(H - 1) - 1f;
                        var d = Mathf.Abs(s);
                        p[y * W + x] = d < 0.11f ? Red : d < 0.18f ? White : s < 0f ? Blue : Green;
                    }
                Disc(p, 4f, 4f, 1.8f, Yellow);
                return p;
            case "Nouvelle-Zélande":
                Fill(p, C(0f, 0.14f, 0.47f));
                // The Union Jack of the canton, simplified: two white diagonals, a white and a red cross.
                for (var i = 0; i < 12; i++)
                {
                    var y = Mathf.Min(7, i * 8 / 12);
                    p[y * W + i] = White;
                    p[(7 - y) * W + i] = White;
                }
                Block(p, 5, 0, 6, 7, White);
                Block(p, 0, 3, 11, 4, White);
                Block(p, 5, 0, 6, 7, Red);
                Block(p, 0, 3, 11, 4, Red);
                Block(p, 5, 3, 6, 4, Red);
                foreach (var (sx, sy) in new[] { (18, 3), (20, 7), (16, 8), (19, 12) }) Block(p, sx, sy, sx + 1, sy + 1, Red);
                return p;
            case "Polynésie française":
                HBands(p, new[] { 4, 8, 4 }, Red, White, Red);
                Disc(p, 11.5f, 7.5f, 3f, Blue);
                Disc(p, 11.5f, 7.5f, 1.2f, Yellow);
                return p;
            case "République Tchèque":
                HBands(p, new[] { 8, 8 }, White, Red);
                for (var y = 0; y < H; y++)
                    for (var x = 0; x < W; x++)
                        if (x < 12f * (1f - Mathf.Abs(y - 7.5f) / 8f)) p[y * W + x] = C(0.07f, 0.25f, 0.5f);
                return p;
            case "République dominicaine":
                Block(p, 0, 0, 11, 7, Blue);
                Block(p, 12, 0, 23, 7, Red);
                Block(p, 0, 8, 11, 15, Red);
                Block(p, 12, 8, 23, 15, Blue);
                Block(p, 10, 0, 13, H - 1, White);
                Block(p, 0, 6, W - 1, 9, White);
                return p;
            case "Suisse":
                Fill(p, Red);
                Block(p, 10, 3, 13, 12, White);
                Block(p, 6, 6, 17, 9, White);
                return p;
            case "Taïwan":
                Fill(p, Red);
                Block(p, 0, 0, 11, 7, Blue);
                Disc(p, 5.5f, 3.5f, 2.2f, White);
                return p;
            case "Ukraine":
                HBands(p, new[] { 8, 8 }, C(0f, 0.36f, 0.76f), C(1f, 0.84f, 0f));
                return p;
            case "Uruguay":
                HBands(p, new[] { 2, 2, 2, 2, 2, 2, 2, 2 }, White, C(0.2f, 0.4f, 0.75f), White, C(0.2f, 0.4f, 0.75f), White, C(0.2f, 0.4f, 0.75f), White, C(0.2f, 0.4f, 0.75f));
                Block(p, 0, 0, 7, 7, White);
                Disc(p, 3.5f, 3.5f, 2f, C(0.98f, 0.8f, 0.1f));
                return p;
            case "Vietnam":
                Fill(p, Red);
                var star = new[] { "...X...", "...X...", "XXXXXXX", ".XXXXX.", "..XXX..", ".XX.XX.", ".X...X." };
                for (var y = 0; y < star.Length; y++)
                    for (var x = 0; x < star[y].Length; x++)
                        if (star[y][x] == 'X') p[(y + 5) * W + x + 8] = Yellow;
                return p;
            case "Écosse":
                Fill(p, C(0f, 0.36f, 0.66f));
                for (var y = 0; y < H; y++)
                    for (var x = 0; x < W; x++)
                        if (Mathf.Abs(x * (H - 1) / (float)(W - 1) - y) < 1.3f || Mathf.Abs((W - 1 - x) * (H - 1) / (float)(W - 1) - y) < 1.3f) p[y * W + x] = White;
                return p;
            case "Éthiopie":
                HBands(p, new[] { 5, 6, 5 }, Green, C(0.98f, 0.85f, 0f), Red);
                Disc(p, 11.5f, 7.5f, 2.6f, C(0f, 0.3f, 0.7f));
                return p;
        }
        return null;
    }
}
