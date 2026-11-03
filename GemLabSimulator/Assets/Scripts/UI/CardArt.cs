using System.Collections.Generic;
using UnityEngine;

// The drawings of the crystal and synthesis cards: small solids (cubes, prisms, boules, octahedra...) drawn in code with a flat shading,
// seen from above and to the side like a crystal on a desk.
public static class CardArt
{
    private const int Size = 128;

    private struct Face
    {
        public Vector3[] Points;
        public Color Color;
        public Vector3 Center;      // centre of the solid the face belongs to, to find the outside of the face
    }

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    // ---- The pictures ----------------------------------------------------------------------------------------------

    public static Sprite Get(string key, Color color)
    {
        var id = key + "|" + ColorUtility.ToHtmlStringRGB(color);
        if (Cache.TryGetValue(id, out var cached) && cached != null) return cached;

        var faces = new List<Face>();
        switch (key)
        {
            case "cubic":
                Parallelepiped(faces, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f), color);
                break;
            case "tetragonal":
                Prism(faces, 4, 0.6f, -0.8f, 0.7f, 45f, 0.5f, color, Vector3.zero);
                break;
            case "hexagonal":
                Prism(faces, 6, 0.55f, -0.9f, 0.6f, 0f, 0.45f, color, Vector3.zero);
                break;
            case "trigonal":
                // A rhombohedron: three equal edges at the same angle, around a vertical axis.
                Parallelepiped(faces, new Vector3(0f, -0.5f, 0f), Edge(0f, 0.8f, 0.45f), Edge(120f, 0.8f, 0.45f), Edge(240f, 0.8f, 0.45f), color);
                break;
            case "orthorhombic":
                Parallelepiped(faces, new Vector3(-0.75f, -0.4f, -0.45f), new Vector3(1.5f, 0f, 0f), new Vector3(0f, 0.8f, 0f), new Vector3(0f, 0f, 0.9f), color);
                break;
            case "monoclinic":
                Parallelepiped(faces, new Vector3(-0.6f, -0.6f, -0.45f), new Vector3(1.1f, 0f, 0f), new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 0f, 0.9f), color);
                break;
            case "triclinic":
                Parallelepiped(faces, new Vector3(-0.6f, -0.55f, -0.45f), new Vector3(1.1f, 0.2f, 0.1f), new Vector3(0.25f, 1.1f, 0.3f), new Vector3(0.4f, 0.15f, 0.95f), color);
                break;
            case "amorphous":
                Lathe(faces, new[] { new Vector2(0f, -0.7f), new Vector2(0.45f, -0.55f), new Vector2(0.72f, -0.1f), new Vector2(0.66f, 0.35f), new Vector2(0.35f, 0.65f), new Vector2(0f, 0.72f) }, 10, color, 0.12f);
                break;
            case "verneuil":
                // The boule of the flame fusion: a thin neck, then the pear.
                Lathe(faces, new[]
                {
                    new Vector2(0f, -1f), new Vector2(0.14f, -0.95f), new Vector2(0.14f, -0.45f), new Vector2(0.26f, -0.25f), new Vector2(0.5f, 0.05f),
                    new Vector2(0.58f, 0.4f), new Vector2(0.5f, 0.75f), new Vector2(0.28f, 0.95f), new Vector2(0f, 1f),
                }, 14, color, 0f);
                break;
            case "czochralski":
                // The cylinder pulled from the crucible, with its seed rod on top.
                Lathe(faces, new[]
                {
                    new Vector2(0f, -1f), new Vector2(0.5f, -0.95f), new Vector2(0.54f, 0.1f), new Vector2(0.44f, 0.3f), new Vector2(0.16f, 0.45f),
                    new Vector2(0.14f, 1f), new Vector2(0f, 1f),
                }, 14, color, 0f);
                break;
            case "hydrothermal":
                Prism(faces, 6, 0.42f, -1f, 0.75f, 0f, 0.5f, color, Vector3.zero);
                break;
            case "flux":
                Prism(faces, 6, 0.3f, -0.6f, 0.3f, 10f, 0.25f, color, new Vector3(-0.45f, 0f, 0.1f));
                Prism(faces, 6, 0.34f, -0.6f, 0.65f, 0f, 0.3f, Color.Lerp(color, Color.white, 0.12f), new Vector3(0.1f, 0f, -0.15f));
                Prism(faces, 6, 0.26f, -0.6f, 0.05f, 20f, 0.2f, Color.Lerp(color, Color.black, 0.1f), new Vector3(0.5f, 0f, 0.25f));
                break;
            case "hpht":
                Octahedron(faces, 0.9f, 1f, color);
                break;
            case "cvd":
                Parallelepiped(faces, new Vector3(-0.85f, -0.15f, -0.85f), new Vector3(1.7f, 0f, 0f), new Vector3(0f, 0.3f, 0f), new Vector3(0f, 0f, 1.7f), color);
                break;
            case "moissanite":
                Prism(faces, 6, 0.5f, -0.5f, 0.5f, 0f, 0.5f, color, Vector3.zero, true);
                break;
            case "layers":
                // A doublet or a triplet: plates of different stones glued together.
                Parallelepiped(faces, new Vector3(-0.8f, -0.45f, -0.6f), new Vector3(1.6f, 0f, 0f), new Vector3(0f, 0.3f, 0f), new Vector3(0f, 0f, 1.2f), new Color(0.55f, 0.30f, 0.30f));
                Parallelepiped(faces, new Vector3(-0.8f, -0.15f, -0.6f), new Vector3(1.6f, 0f, 0f), new Vector3(0f, 0.22f, 0f), new Vector3(0f, 0f, 1.2f), new Color(0.88f, 0.90f, 0.95f));
                Parallelepiped(faces, new Vector3(-0.8f, 0.07f, -0.6f), new Vector3(1.6f, 0f, 0f), new Vector3(0f, 0.38f, 0f), new Vector3(0f, 0f, 1.2f), color);
                break;
            default:
                Octahedron(faces, 0.8f, 1f, color);
                break;
        }

        var sprite = Render(faces);
        Cache[id] = sprite;
        return sprite;
    }

    // ---- Solids --------------------------------------------------------------------------------------------------------

    private static Vector3 Edge(float degrees, float radius, float height)
    {
        var a = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
    }

    private static void AddSolid(List<Face> faces, List<Vector3[]> polygons, Color color, Vector3 center)
    {
        foreach (var polygon in polygons) faces.Add(new Face { Points = polygon, Color = color, Center = center });
    }

    private static void Parallelepiped(List<Face> faces, Vector3 origin, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        var p = new[]
        {
            origin, origin + a, origin + a + b, origin + b,
            origin + c, origin + a + c, origin + a + b + c, origin + b + c,
        };
        var polygons = new List<Vector3[]>
        {
            new[] { p[0], p[1], p[2], p[3] }, new[] { p[4], p[5], p[6], p[7] },
            new[] { p[0], p[1], p[5], p[4] }, new[] { p[3], p[2], p[6], p[7] },
            new[] { p[0], p[3], p[7], p[4] }, new[] { p[1], p[2], p[6], p[5] },
        };
        AddSolid(faces, polygons, color, origin + (a + b + c) * 0.5f);
    }

    // A prism with `sides` sides between two heights, closed by a flat top or a pyramid (cap > 0); `bipyramid` adds one under it too.
    private static void Prism(List<Face> faces, int sides, float radius, float bottom, float top, float rotation, float cap, Color color, Vector3 offset, bool bipyramid = false)
    {
        var ringBottom = new Vector3[sides];
        var ringTop = new Vector3[sides];
        for (var i = 0; i < sides; i++)
        {
            var angle = (rotation + i * 360f / sides) * Mathf.Deg2Rad;
            ringBottom[i] = offset + new Vector3(Mathf.Cos(angle) * radius, bottom, Mathf.Sin(angle) * radius);
            ringTop[i] = offset + new Vector3(Mathf.Cos(angle) * radius, top, Mathf.Sin(angle) * radius);
        }

        var polygons = new List<Vector3[]>();
        for (var i = 0; i < sides; i++)
        {
            var j = (i + 1) % sides;
            polygons.Add(new[] { ringBottom[i], ringBottom[j], ringTop[j], ringTop[i] });
        }

        if (cap > 0f)
        {
            var apex = offset + new Vector3(0f, top + cap, 0f);
            for (var i = 0; i < sides; i++) polygons.Add(new[] { ringTop[i], ringTop[(i + 1) % sides], apex });
        }
        else
        {
            polygons.Add((Vector3[])ringTop.Clone());
        }

        if (bipyramid)
        {
            var apex = offset + new Vector3(0f, bottom - cap, 0f);
            for (var i = 0; i < sides; i++) polygons.Add(new[] { ringBottom[i], ringBottom[(i + 1) % sides], apex });
        }
        else
        {
            polygons.Add((Vector3[])ringBottom.Clone());
        }

        AddSolid(faces, polygons, color, offset + new Vector3(0f, (bottom + top) * 0.5f, 0f));
    }

    private static void Octahedron(List<Face> faces, float radius, float height, Color color)
    {
        var top = new Vector3(0f, height, 0f);
        var bottom = new Vector3(0f, -height, 0f);
        var ring = new[] { new Vector3(radius, 0f, 0f), new Vector3(0f, 0f, radius), new Vector3(-radius, 0f, 0f), new Vector3(0f, 0f, -radius) };
        var polygons = new List<Vector3[]>();
        for (var i = 0; i < 4; i++)
        {
            polygons.Add(new[] { ring[i], ring[(i + 1) % 4], top });
            polygons.Add(new[] { ring[i], ring[(i + 1) % 4], bottom });
        }
        AddSolid(faces, polygons, color, Vector3.zero);
    }

    // A solid of revolution: a profile (radius, height) turned around the vertical axis. `wobble` makes the surface irregular.
    private static void Lathe(List<Face> faces, Vector2[] profile, int segments, Color color, float wobble)
    {
        var rnd = new System.Random(5);
        var rings = new Vector3[profile.Length][];
        for (var r = 0; r < profile.Length; r++)
        {
            rings[r] = new Vector3[segments];
            for (var s = 0; s < segments; s++)
            {
                var angle = s * 2f * Mathf.PI / segments;
                var radius = profile[r].x * (1f + (float)(rnd.NextDouble() - 0.5) * wobble * 2f);
                rings[r][s] = new Vector3(Mathf.Cos(angle) * radius, profile[r].y, Mathf.Sin(angle) * radius);
            }
        }

        var polygons = new List<Vector3[]>();
        for (var r = 0; r < profile.Length - 1; r++)
        {
            for (var s = 0; s < segments; s++)
            {
                var t = (s + 1) % segments;
                polygons.Add(new[] { rings[r][s], rings[r][t], rings[r + 1][t], rings[r + 1][s] });
            }
        }
        AddSolid(faces, polygons, color, Vector3.zero);
    }

    // ---- Rendering ----------------------------------------------------------------------------------------------------

    private static Sprite Render(List<Face> faces)
    {
        // Seen from the front, a little from above, and turned a third of a quarter.
        var view = Quaternion.Euler(26f, 0f, 0f) * Quaternion.Euler(0f, 32f, 0f);
        var light = new Vector3(-0.45f, 0.75f, 0.5f).normalized;

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var face in faces)
        {
            foreach (var point in face.Points)
            {
                var v = view * point;
                min = Vector2.Min(min, new Vector2(v.x, v.y));
                max = Vector2.Max(max, new Vector2(v.x, v.y));
            }
        }
        var extent = Mathf.Max(max.x - min.x, max.y - min.y, 0.01f);
        var scale = Size * 0.84f / extent;
        var middle = (min + max) * 0.5f;

        var visible = new List<(float depth, Vector2[] polygon, Color color)>();
        foreach (var face in faces)
        {
            var p = face.Points;
            var normal = Vector3.Cross(p[1] - p[0], p[2] - p[0]).normalized;
            var center = Vector3.zero;
            foreach (var point in p) center += point;
            center /= p.Length;
            // The normal points to the outside of the solid.
            if (Vector3.Dot(normal, center - face.Center) < 0f) normal = -normal;

            var viewNormal = view * normal;
            if (viewNormal.z <= 0.001f) continue;

            var polygon = new Vector2[p.Length];
            var depth = 0f;
            for (var i = 0; i < p.Length; i++)
            {
                var v = view * p[i];
                polygon[i] = new Vector2((v.x - middle.x) * scale + Size * 0.5f, (v.y - middle.y) * scale + Size * 0.5f);
                depth += v.z;
            }
            var intensity = 0.42f + 0.58f * Mathf.Max(0f, Vector3.Dot(viewNormal, light));
            var shaded = new Color(Mathf.Clamp01(face.Color.r * intensity), Mathf.Clamp01(face.Color.g * intensity), Mathf.Clamp01(face.Color.b * intensity), 1f);
            visible.Add((depth / p.Length, polygon, shaded));
        }
        // The far faces first.
        visible.Sort((a, b) => a.depth.CompareTo(b.depth));

        var pixels = new Color32[Size * Size];
        foreach (var (_, polygon, color) in visible)
        {
            Fill(pixels, polygon, color);
            var edge = Color.Lerp(color, Color.black, 0.5f);
            for (var i = 0; i < polygon.Length; i++) Line(pixels, polygon[i], polygon[(i + 1) % polygon.Length], edge);
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Fill(Color32[] pixels, Vector2[] polygon, Color color)
    {
        var fill = (Color32)color;
        // Triangle fan.
        for (var t = 1; t < polygon.Length - 1; t++)
        {
            var a = polygon[0];
            var b = polygon[t];
            var c = polygon[t + 1];
            var x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            var x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            var y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            var y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            var area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (Mathf.Abs(area) < 0.0001f) continue;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var w0 = ((b.x - p.x) * (c.y - p.y) - (b.y - p.y) * (c.x - p.x)) / area;
                    var w1 = ((c.x - p.x) * (a.y - p.y) - (c.y - p.y) * (a.x - p.x)) / area;
                    var w2 = 1f - w0 - w1;
                    if (w0 >= -0.01f && w1 >= -0.01f && w2 >= -0.01f) pixels[y * Size + x] = fill;
                }
            }
        }
    }

    private static void Line(Color32[] pixels, Vector2 from, Vector2 to, Color color)
    {
        var steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 2f) + 1;
        var line = (Color32)color;
        for (var i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(from, to, i / (float)steps);
            var x = Mathf.RoundToInt(p.x - 0.5f);
            var y = Mathf.RoundToInt(p.y - 0.5f);
            if (x >= 0 && y >= 0 && x < Size && y < Size) pixels[y * Size + x] = line;
        }
    }
}
