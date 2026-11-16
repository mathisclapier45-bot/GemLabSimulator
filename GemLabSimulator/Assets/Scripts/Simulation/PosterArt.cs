using System.Collections.Generic;
using UnityEngine;

// A text laid on a poster, in pixels from its top left corner; the viewer makes one Text for each.
public struct PosterLabel
{
    public string Text;
    public Vector2 Position;         // the centre of the text, from the top left corner of the poster
    public float Rotation;           // degrees, counter-clockwise
    public int Size;
    public Color Color;
    public float Width;
    public bool Bold;
    public bool Italic;
    public bool Left;                // Position is the left end of the text instead of its centre
}

// The pictures of the posters, drawn in code from the colour grid of the laboratory (GemColorSystem): the same 72 hues and eight rows as the book of the corundum.
// The left wheel carries the names the game gives to each zone of colour; the right one the angle of each hue.
public static class PosterArt
{
    public const int PosterWidth = 1200;
    public const int PosterHeight = 580;

    private const int Segments = 72;                 // a hue every 5 degrees
    private const float DegreesPerSegment = 360f / Segments;

    private static readonly Color32 Paper = new Color32(244, 238, 226, 255);
    private static readonly Color32 Ink = new Color32(34, 28, 26, 255);

    private static Texture2D cachedTexture;
    private static List<PosterLabel> cachedLabels;
    private static Sprite cachedThumb;

    // The two wheels of the poster of the colours: centre of each, from the top left corner, and radii.
    private static readonly Vector2 LeftCenter = new Vector2(300f, 292f);
    private static readonly Vector2 RightCenter = new Vector2(900f, 292f);
    private const float Outer = 248f;
    private const float Inner = 92f;

    // The size in pixels of the picture of a poster.
    public static Vector2Int SizeOf(string id)
    {
        return id == PosterCatalog.Garnets ? new Vector2Int(PosterGarnets.Width, PosterGarnets.Height) : new Vector2Int(PosterWidth, PosterHeight);
    }

    public static Texture2D Poster(string id, out List<PosterLabel> labels)
    {
        if (id == PosterCatalog.Garnets) return PosterGarnets.Poster(out labels);
        if (cachedTexture == null)
        {
            var pixels = new Color32[PosterWidth * PosterHeight];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Paper;

            var labelList = new List<PosterLabel>();
            DrawWheel(pixels, PosterWidth, PosterHeight, LeftCenter.x, PosterHeight - LeftCenter.y, Outer, Inner, true, labelList, LeftCenter);
            DrawWheel(pixels, PosterWidth, PosterHeight, RightCenter.x, PosterHeight - RightCenter.y, Outer, Inner, false, null, RightCenter);
            AddDegrees(labelList, LeftCenter);
            AddDegrees(labelList, RightCenter);

            cachedTexture = new Texture2D(PosterWidth, PosterHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            cachedTexture.SetPixels32(pixels);
            cachedTexture.Apply();
            cachedLabels = labelList;
        }
        labels = cachedLabels;
        return cachedTexture;
    }

    // A small picture of the poster for the lists: the wheel on its paper.
    public static Sprite Thumbnail(string id)
    {
        if (id == PosterCatalog.Garnets) return PosterGarnets.Thumbnail();
        if (cachedThumb != null) return cachedThumb;
        const int size = 96;
        var pixels = new Color32[size * size];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Paper;
        DrawWheel(pixels, size, size, size / 2f, size / 2f, 44f, 14f, false, null, Vector2.zero);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply();
        cachedThumb = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedThumb;
    }

    private static string[,] ZoneNames()
    {
        var rows = GemColorSystem.RowCount;
        var names = new string[rows, Segments];
        for (var row = 0; row < rows; row++)
            for (var seg = 0; seg < Segments; seg++)
            {
                var color = new GemColorValue(seg * DegreesPerSegment + DegreesPerSegment * 0.5f, GemColorSystem.RowSaturationOf(row), GemColorSystem.RowBrightnessOf(row));
                names[row, seg] = GemColorSystem.Describe(color).Name;
            }
        return names;
    }

    // One wheel: 72 hues around, eight rings from light (outside) to dark (inside). With `zones`, a dark line where the name of the colour changes, and its names as labels.
    private static void DrawWheel(Color32[] pixels, int width, int height, float cx, float cyTex, float outer, float inner, bool zones, List<PosterLabel> labels, Vector2 centerFromTop)
    {
        var rows = GemColorSystem.RowCount;
        var ringWidth = (outer - inner) / rows;
        var names = zones ? ZoneNames() : null;

        var minX = Mathf.Max(0, Mathf.FloorToInt(cx - outer - 2f));
        var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + outer + 2f));
        var minY = Mathf.Max(0, Mathf.FloorToInt(cyTex - outer - 2f));
        var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cyTex + outer + 2f));
        var cells = new Color32[rows, Segments];
        for (var row = 0; row < rows; row++)
            for (var seg = 0; seg < Segments; seg++)
                cells[row, seg] = GemColorSystem.RowColor(seg * DegreesPerSegment + DegreesPerSegment * 0.5f, row);

        for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x + 0.5f - cx;
                var dy = y + 0.5f - cyTex;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance < inner || distance >= outer) continue;

                var fromOuter = (outer - distance) / ringWidth;
                var row = Mathf.Min(rows - 1, (int)fromOuter);
                var radial = fromOuter - row;                                   // 0 at the outer edge of the ring, 1 at the inner edge
                var angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;               // clockwise from the top
                if (angle < 0f) angle += 360f;
                var seg = Mathf.Min(Segments - 1, (int)(angle / DegreesPerSegment));
                var around = angle / DegreesPerSegment - seg;                   // 0 at the start of the cell, 1 at its end
                var arcPixels = distance * DegreesPerSegment * Mathf.Deg2Rad;

                var edgeStart = around * arcPixels;
                var edgeEnd = (1f - around) * arcPixels;
                var edgeOuter = radial * ringWidth;
                var edgeInner = (1f - radial) * ringWidth;

                if (zones)
                {
                    var line = false;
                    var name = names[row, seg];
                    if (edgeStart < 1.4f && names[row, (seg + Segments - 1) % Segments] != name) line = true;
                    else if (edgeEnd < 1.4f && names[row, (seg + 1) % Segments] != name) line = true;
                    else if (edgeOuter < 1.4f && row > 0 && names[row - 1, seg] != name) line = true;
                    else if (edgeInner < 1.4f && row < rows - 1 && names[row + 1, seg] != name) line = true;
                    if (line) { pixels[y * width + x] = Ink; continue; }
                }

                // The white gap between two cells.
                if (edgeStart < 0.7f || edgeEnd < 0.7f || edgeOuter < 0.7f || edgeInner < 0.7f) continue;
                pixels[y * width + x] = cells[row, seg];
            }

        if (!zones || labels == null) return;

        // The names of the zones: a label at the middle of each run of cells of the same name that is wide enough to hold it.
        for (var row = 0; row < rows; row++)
        {
            var start = 0;
            while (start < Segments && names[row, start] == names[row, (start + Segments - 1) % Segments]) start++;
            if (start == Segments) continue;                                    // one name all around: no label
            var run = 0;
            for (var step = 0; step < Segments; step++)
            {
                var seg = (start + step) % Segments;
                var next = (start + step + 1) % Segments;
                run++;
                if (names[row, seg] == names[row, next] && step < Segments - 1) continue;

                var first = (start + step - run + 1 + Segments) % Segments;
                if (run >= 3) AddZoneLabel(labels, names[row, first], row, first, run, ringWidth, outer, centerFromTop);
                run = 0;
            }
        }
    }

    private static void AddZoneLabel(List<PosterLabel> labels, string name, int row, int firstSegment, int run, float ringWidth, float outer, Vector2 center)
    {
        var angle = (firstSegment + run * 0.5f) * DegreesPerSegment;            // clockwise from the top
        var radius = outer - (row + 0.5f) * ringWidth;
        var radians = angle * Mathf.Deg2Rad;
        var position = new Vector2(center.x + radius * Mathf.Sin(radians), center.y - radius * Mathf.Cos(radians));
        var rotation = -angle;
        if (angle > 90f && angle < 270f) rotation += 180f;                      // keep the text upright
        var cell = GemColorSystem.RowColor(angle, row);
        var light = cell.r * 0.3f + cell.g * 0.59f + cell.b * 0.11f;
        labels.Add(new PosterLabel
        {
            Text = name, Position = position, Rotation = rotation, Size = 8,
            Color = light > 0.55f ? new Color(0.12f, 0.10f, 0.09f) : new Color(0.97f, 0.95f, 0.92f),
            Width = radius * run * DegreesPerSegment * Mathf.Deg2Rad,
        });
    }

    // The angles around a wheel, every ten degrees.
    private static void AddDegrees(List<PosterLabel> labels, Vector2 center)
    {
        for (var angle = 0; angle < 360; angle += 10)
        {
            var radians = (angle + DegreesPerSegment * 0.5f) * Mathf.Deg2Rad;
            var radius = Outer + 13f;
            labels.Add(new PosterLabel
            {
                Text = angle.ToString(), Position = new Vector2(center.x + radius * Mathf.Sin(radians), center.y - radius * Mathf.Cos(radians)),
                Rotation = 0f, Size = 9, Color = new Color(0.45f, 0.40f, 0.36f), Width = 30f,
            });
        }
    }
}
