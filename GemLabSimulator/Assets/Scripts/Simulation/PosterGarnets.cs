using System.Collections.Generic;
using UnityEngine;

// The poster of the garnet family: the diagram of Mackowsky (after Webster, Gems, 5th edition) redrawn in code, with its three groups (pyralspite, ugrandite),
// its six poles, the continuous and discontinuous series, the colour samples and the varieties. The layout follows the original figure, a little flattened to fit a landscape window.
public static class PosterGarnets
{
    public const int Width = 1200;
    public const int Height = 770;

    private static readonly Color32 Paper = new Color32(250, 247, 240, 255);
    private static readonly Color Ink = new Color(0.10f, 0.09f, 0.09f);
    private static readonly Color LineInk = new Color(0.12f, 0.11f, 0.11f);

    private const float RowHeight = 17f;
    private const float Pad = 5f;
    private const int TextSize = 13;
    private const float PoleRadius = 19f;
    private static readonly float[] DashDot = { 6f, 3f, 1.5f, 3f };

    private static Texture2D cachedTexture;
    private static List<PosterLabel> cachedLabels;
    private static Sprite cachedThumb;

    // A point of the original figure (about 1170 x 1300) turned into a point of the poster.
    private static Vector2 P(float x, float y) => new Vector2(40f + (x - 85f) * 1.12f, 12f + (y - 30f) * 0.6f);

    private struct Item
    {
        public string Text;
        public Color[] Swatch;
        public bool Bold;
        public Color Color;
        public int Size;

        public float Width => Swatch != null ? 46f : Text.Length * Size * (Bold ? 0.60f : 0.54f) + 3f;
    }

    private static Item T(string text, bool bold = false) => new Item { Text = text, Bold = bold, Color = Ink, Size = TextSize };
    private static Item S(params Color[] colors) => new Item { Swatch = colors };
    private static Color C(float r, float g, float b) => new Color(r, g, b);

    public static Texture2D Poster(out List<PosterLabel> labels)
    {
        if (cachedTexture == null)
        {
            var canvas = new PosterCanvas(Width, Height, Paper);
            var list = new List<PosterLabel>();
            Draw(canvas, list);
            cachedTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            cachedTexture.SetPixels32(canvas.Pixels);
            cachedTexture.Apply();
            cachedLabels = list;
        }
        labels = cachedLabels;
        return cachedTexture;
    }

    // The small picture of the lists: the poster reduced, on a square of paper.
    public static Sprite Thumbnail()
    {
        if (cachedThumb != null) return cachedThumb;
        var texture = Poster(out _);
        var source = texture.GetPixels32();
        const int size = 96;
        var pixels = new Color32[size * size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Paper;
        var scale = (float)Width / size;                                    // source pixels per thumbnail pixel
        var shownHeight = Mathf.RoundToInt(Height / scale);
        var top = (size - shownHeight) / 2;
        for (var y = 0; y < shownHeight; y++)
            for (var x = 0; x < size; x++)
            {
                float r = 0f, g = 0f, b = 0f; var count = 0;
                var x0 = Mathf.FloorToInt(x * scale); var x1 = Mathf.Min(Width, Mathf.CeilToInt((x + 1) * scale));
                var y0 = Mathf.FloorToInt(y * scale); var y1 = Mathf.Min(Height, Mathf.CeilToInt((y + 1) * scale));
                for (var sy = y0; sy < y1; sy++)
                    for (var sx = x0; sx < x1; sx++)
                    {
                        var c = source[(Height - 1 - sy) * Width + sx];
                        r += c.r; g += c.g; b += c.b; count++;
                    }
                if (count == 0) continue;
                pixels[(size - 1 - (top + y)) * size + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
            }
        var thumb = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        thumb.SetPixels32(pixels);
        thumb.Apply();
        cachedThumb = Sprite.Create(thumb, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedThumb;
    }

    private static void Draw(PosterCanvas canvas, List<PosterLabel> labels)
    {
        // Poles of the figure: centre and the text inside.
        var fe2 = P(578f, 155f);
        var mg = P(297f, 435f);
        var mn = P(843f, 435f);
        var fe3 = P(407f, 750f);
        var ca = P(740f, 750f);
        var cr = P(340f, 975f);

        // Brackets of the two groups.
        DrawBracket(canvas, P(975f, 63f), P(1080f, 63f), P(1080f, 482f), P(1038f, 482f));
        DrawBracket(canvas, P(135f, 482f), P(88f, 482f), P(88f, 172f), P(88f, 172f));
        DrawBracket(canvas, P(132f, 648f), P(88f, 648f), P(88f, 1030f), P(88f, 1030f));
        DrawBracket(canvas, P(1036f, 648f), P(1080f, 648f), P(1080f, 880f), P(1080f, 880f));
        AddText(labels, "Pyralspite", P(980f, 78f), 13, Ink, false, false);
        AddText(labels, "Ugrandite", P(93f, 1013f), 13, Ink, false, false);

        // Discontinuous series (dotted), then continuous ones (double arrows).
        Link(canvas, fe2, fe3, true, DashDot);
        Link(canvas, fe2, ca, true, DashDot);
        Link(canvas, mg, ca, true, DashDot);
        Link(canvas, mn, fe3, true, DashDot);
        Link(canvas, mn, ca, true, DashDot);
        Link(canvas, mg, fe3, true, DashDot);
        Link(canvas, fe3, cr, true, DashDot);

        Link(canvas, fe2, mg, true, null);
        Link(canvas, fe2, mn, true, null);
        Link(canvas, mg, mn, true, null);
        Link(canvas, fe3, ca, true, null);

        // The little bars crossing the continuous series.
        Tick(canvas, Vector2.Lerp(fe2, mg, 0.35f), mg - fe2, 20f);
        Tick(canvas, Vector2.Lerp(fe2, mg, 0.72f), mg - fe2, 20f);
        Tick(canvas, Vector2.Lerp(fe2, mn, 0.30f), mn - fe2, 20f);
        Tick(canvas, Vector2.Lerp(fe2, mn, 0.68f), mn - fe2, 20f);
        Tick(canvas, Vector2.Lerp(mg, mn, 0.36f), mn - mg, 24f);
        Tick(canvas, Vector2.Lerp(mg, mn, 0.64f), mn - mg, 24f);
        Tick(canvas, Vector2.Lerp(fe3, ca, 0.35f), ca - fe3, 20f);
        Tick(canvas, Vector2.Lerp(fe3, ca, 0.65f), ca - fe3, 20f);
        canvas.Disc(Vector2.Lerp(mg, mn, 0.36f) + new Vector2(0f, -14f), 2.2f, LineInk);

        // The poles.
        DrawPole(canvas, labels, fe2, "Fe2+", false);
        DrawPole(canvas, labels, mg, "Mg2+", false);
        DrawPole(canvas, labels, mn, "Mn2+", false);
        DrawPole(canvas, labels, fe3, "Fe3+", false);
        DrawPole(canvas, labels, ca, "Ca2+", false);
        DrawPole(canvas, labels, cr, "Cr3+", true);

        // The title box.
        var titleTop = P(88f, 45f);
        var titleRect = new Rect(titleTop.x, titleTop.y, 440f, 3f * RowHeight + 14f);
        canvas.RectFill(titleRect, Paper, 14f);
        canvas.RectOutline(titleRect, 1.2f, LineInk, 14f);
        AddText(labels, "Famille des grenats:", new Vector2(titleRect.x + 10f, titleRect.y + 8f + RowHeight * 0.5f), 15, Ink, true, false);
        AddText(labels, "X₃ Y₂(SiO₄)₃", new Vector2(titleRect.x + 188f, titleRect.y + 8f + RowHeight * 0.5f), 13, Ink, false, false);
        var blue = C(0.22f, 0.18f, 0.92f);
        var magenta = C(0.78f, 0.10f, 0.58f);
        canvas.Disc(new Vector2(titleRect.x + 14f, titleRect.y + 8f + RowHeight * 1.5f), 6.5f, C(0.62f, 0.58f, 0.98f), C(0.40f, 0.36f, 0.90f));
        AddText(labels, "X= Fe2+, Mg2+, Mn2+, Ca2+, ...", new Vector2(titleRect.x + 28f, titleRect.y + 8f + RowHeight * 1.5f), 13, blue, true, false);
        canvas.Disc(new Vector2(titleRect.x + 14f, titleRect.y + 8f + RowHeight * 2.5f), 6.5f, C(0.98f, 0.70f, 0.92f), C(0.80f, 0.40f, 0.70f));
        AddText(labels, "Y= Al3+, Fe3+, Cr3+, (V3+, Ti3+)..", new Vector2(titleRect.x + 28f, titleRect.y + 8f + RowHeight * 2.5f), 13, magenta, true, false);

        // The boxes of species, mixed series and varieties.
        var wine = new[] { C(0.30f, 0.04f, 0.12f), C(0.52f, 0.12f, 0.30f) };
        Box(canvas, labels, P(532f, 48f),
            new[] { T("Almandin"), S(wine) },
            new[] { T("Fe₃Al₂(SiO₄)₃") });
        Box(canvas, labels, P(193f, 243f),
            new[] { T("Pyrope-almandin"), S(C(0.62f, 0.04f, 0.10f), C(0.92f, 0.10f, 0.14f)) },
            new[] { S(C(0.86f, 0.12f, 0.56f), C(0.99f, 0.90f, 0.98f)), T("* Rhodolite") });
        Box(canvas, labels, P(705f, 258f),
            new[] { T("Almandin-spessartite"), S(C(0.48f, 0.05f, 0.14f), C(0.98f, 0.55f, 0.20f)) });
        Box(canvas, labels, P(880f, 396f),
            new[] { T("Spessartite"), S(C(1.00f, 0.68f, 0.26f), C(0.86f, 0.24f, 0.10f)) },
            new[] { T("Mn₃Al₂(SiO₄)₃") });
        Box(canvas, labels, P(138f, 470f),
            new[] { T("Pyrope"), S(C(0.82f, 0.10f, 0.08f), C(0.52f, 0.04f, 0.42f)) },
            new[] { T("Mg₃Al₂(SiO₄)₃") },
            new[] { S(C(0.60f, 0.12f, 0.10f), C(0.88f, 0.30f, 0.24f), C(0.74f, 0.46f, 0.42f)), T("* Pyrope chromifère") });
        Box(canvas, labels, P(483f, 466f),
            new[] { T("Pyrope-spessartite"), S(C(0.86f, 0.08f, 0.26f), C(1.00f, 0.78f, 0.46f)) },
            new[] { S(C(1.00f, 0.52f, 0.12f), C(1.00f, 0.72f, 0.36f)), T("* Malaia / Malaya") },
            new[] { S(C(0.34f, 0.12f, 0.56f), C(0.30f, 0.58f, 0.26f)), T("* Change couleur") });
        Box(canvas, labels, P(160f, 700f),
            new[] { T("Andradite"), S(C(0.96f, 0.86f, 0.28f), C(0.90f, 0.78f, 0.30f)) },
            new[] { T("Ca₃Fe₂(SiO₄)₃") },
            new[] { S(C(0.16f, 0.68f, 0.12f), C(0.52f, 0.88f, 0.42f)), T("* Démantoïde") },
            new[] { S(C(0.96f, 0.62f, 0.24f), C(1.00f, 0.96f, 0.20f)), T("* Topazolite") });
        Box(canvas, labels, P(773f, 690f),
            new[] { T("Grossulaire"), S(C(0.88f, 0.52f, 0.10f), C(0.64f, 0.64f, 0.20f), C(0.92f, 0.76f, 0.36f)) },
            new[] { T("Ca₃Al₂(SiO₄)₃") },
            new[] { S(C(0.86f, 0.46f, 0.10f), C(0.82f, 0.22f, 0.14f)), T("* Hessonite") },
            new[] { S(C(0.08f, 0.58f, 0.08f), C(0.30f, 0.86f, 0.30f)), T("* Tsavorite") },
            new[] { S(C(0.82f, 0.92f, 0.62f), C(0.78f, 0.74f, 0.44f), C(0.64f, 0.50f, 0.30f)), T("* Mali") });
        Box(canvas, labels, P(443f, 786f),
            new[] { T("Grossulaire-andradite"), S(C(0.40f, 0.60f, 0.22f), C(0.78f, 0.58f, 0.34f)) },
            new[] { S(C(0.84f, 0.94f, 0.64f), C(0.80f, 0.76f, 0.46f), C(0.66f, 0.50f, 0.30f)), T("* Mali") });
        Box(canvas, labels, P(378f, 940f),
            new[] { T("Uvarovite"), S(C(0.08f, 0.34f, 0.10f), C(0.46f, 0.72f, 0.30f)) },
            new[] { T("Ca₃Cr₂(SiO₄)₃") });

        DrawLegend(canvas, labels);

        AddText(labels, "(c) www.geminterest.com", P(130f, 1185f), 16, Ink, true, false);
        AddText(labels, "Schéma 1 :", P(85f, 1258f), 13, Ink, false, true);
        AddText(labels, "Adaptation du schéma de Mackowsky  (Gems, R. Webster 5th edition)", P(275f, 1258f), 13, Ink, false, true);
    }

    private static void DrawBracket(PosterCanvas canvas, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        canvas.Line(a, b, 1.4f, LineInk);
        canvas.Line(b, c, 1.4f, LineInk);
        if ((c - d).sqrMagnitude > 0.1f) canvas.Line(c, d, 1.4f, LineInk);
    }

    // A link between two poles, stopping at the edge of each circle.
    private static void Link(PosterCanvas canvas, Vector2 a, Vector2 b, bool arrows, float[] pattern)
    {
        var dir = (b - a).normalized;
        var from = a + dir * (PoleRadius + 2f);
        var to = b - dir * (PoleRadius + 2f);
        canvas.Arrow(from, to, 1.3f, LineInk, arrows, arrows, pattern, 10f, 4f);
    }

    private static void Tick(PosterCanvas canvas, Vector2 on, Vector2 direction, float length)
    {
        var d = direction.normalized;
        var normal = new Vector2(-d.y, d.x);
        canvas.Line(on - normal * length * 0.5f, on + normal * length * 0.5f, 1.4f, LineInk);
    }

    private static void DrawPole(PosterCanvas canvas, List<PosterLabel> labels, Vector2 center, string ion, bool pink)
    {
        var inner = pink ? C(0.98f, 0.82f, 0.98f) : C(0.88f, 0.88f, 1.00f);
        var rim = pink ? C(0.80f, 0.42f, 0.78f) : C(0.52f, 0.52f, 0.92f);
        canvas.Disc(center, PoleRadius, inner, rim);
        canvas.Ring(center, PoleRadius, 1f, new Color(0.45f, 0.40f, 0.55f));
        AddCentered(labels, "100%", center + new Vector2(0f, -5.5f), 9, Ink, true);
        AddCentered(labels, ion, center + new Vector2(0f, 5.5f), 9, Ink, true);
    }

    // A box of the figure: a rounded rectangle sized to its rows, which hold texts and colour samples from left to right.
    private static void Box(PosterCanvas canvas, List<PosterLabel> labels, Vector2 topLeft, params Item[][] rows)
    {
        var width = 0f;
        foreach (var row in rows)
        {
            var rowWidth = 0f;
            foreach (var item in row) rowWidth += item.Width + 6f;
            width = Mathf.Max(width, rowWidth - 6f);
        }
        var rect = new Rect(topLeft.x, topLeft.y, width + 18f, rows.Length * RowHeight + 2f * Pad);
        canvas.RectFill(rect, Paper, 3f);
        canvas.RectOutline(rect, 1.2f, LineInk, 3f);

        for (var r = 0; r < rows.Length; r++)
        {
            var y = rect.y + Pad + RowHeight * (r + 0.5f);
            var x = rect.x + 9f;
            foreach (var item in rows[r])
            {
                if (item.Swatch != null)
                    canvas.Swatch(new Rect(x, y - 5f, 40f, 10f), item.Swatch);
                else
                    AddText(labels, item.Text, new Vector2(x, y), item.Size, item.Color, item.Bold, false);
                x += item.Width + 6f;
            }
        }
    }

    private static void DrawLegend(PosterCanvas canvas, List<PosterLabel> labels)
    {
        var topLeft = P(658f, 870f);
        var bottomRight = P(1082f, 1150f);
        var rect = new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
        canvas.RectOutline(rect, 1.4f, LineInk, 26f);

        var poleY = P(0f, 930f).y;
        canvas.Disc(new Vector2(P(748f, 0f).x, poleY), 14f, Color.white, C(0.93f, 0.93f, 0.93f));
        canvas.Ring(new Vector2(P(748f, 0f).x, poleY), 14f, 1.2f, LineInk);
        AddText(labels, "Pôle pur", new Vector2(P(798f, 0f).x, poleY), 13, Ink, true, false);

        var y1 = P(0f, 973f).y;
        canvas.Arrow(new Vector2(P(688f, 0f).x, y1), new Vector2(P(790f, 0f).x, y1), 1.4f, LineInk, true, true, null, 10f, 4f);
        AddText(labels, "Série continue", new Vector2(P(798f, 0f).x, y1), 13, Ink, true, false);

        var y2 = P(0f, 1013f).y;
        canvas.Arrow(new Vector2(P(688f, 0f).x, y2), new Vector2(P(790f, 0f).x, y2), 1.4f, LineInk, true, true, DashDot, 10f, 4f);
        AddText(labels, "Série discontinue", new Vector2(P(798f, 0f).x, y2), 13, Ink, true, false);

        var y3 = P(0f, 1048f).y;
        canvas.RectOutline(new Rect(P(733f, 0f).x, y3 - 8f, 62f, 16f), 1.2f, LineInk, 2f);
        AddText(labels, "Echantillon de couleurs", new Vector2(P(798f, 0f).x, y3), 13, Ink, true, false);

        var y4 = P(0f, 1085f).y;
        AddCentered(labels, "*", new Vector2(P(780f, 0f).x, y4 + 3f), 14, Ink, true);
        AddText(labels, "Variété", new Vector2(P(800f, 0f).x, y4), 13, Ink, true, false);
    }

    private static void AddText(List<PosterLabel> labels, string text, Vector2 leftMiddle, int size, Color color, bool bold, bool italic)
    {
        labels.Add(new PosterLabel { Text = text, Position = leftMiddle, Size = size, Color = color, Width = text.Length * size * 0.6f + 8f, Bold = bold, Italic = italic, Left = true });
    }

    private static void AddCentered(List<PosterLabel> labels, string text, Vector2 center, int size, Color color, bool bold)
    {
        labels.Add(new PosterLabel { Text = text, Position = center, Size = size, Color = color, Width = text.Length * size * 0.6f + 8f, Bold = bold });
    }
}
