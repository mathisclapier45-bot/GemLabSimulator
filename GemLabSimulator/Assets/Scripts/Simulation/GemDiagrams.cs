using System.Collections.Generic;
using UnityEngine;

// Line drawings of a stone's geometry for the laboratory report, from its real facet mesh: the top view (crown facets
// in dark lines, the pavilion seen through in light ones) and the profile view (the facets facing the viewer).
public static class GemDiagrams
{
    private const int Size = 256;

    private static readonly Color Ink = new Color(0.13f, 0.13f, 0.17f, 1f);
    private static readonly Color Faint = new Color(0.62f, 0.62f, 0.68f, 1f);

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite TopView(GemInstance gem) => Get(gem, true);
    public static Sprite ProfileView(GemInstance gem) => Get(gem, false);

    private static Sprite Get(GemInstance gem, bool top)
    {
        var half = GemView3D.HalfExtents(gem);
        var key = $"{gem.SpecimenId}|{gem.IsRough}|{gem.ShapeName}|{gem.CutStyleName}|{gem.HasOpenCulet}|{half.x:F2}|{half.y:F2}|{half.z:F2}|{top}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var sprite = Render(gem, half, top);
        Cache[key] = sprite;
        return sprite;
    }

    private static Sprite Render(GemInstance gem, Vector3 half, bool top)
    {
        var mesh = GemView3D.MeshFor(gem);
        var vertices = new Vector3[mesh.Vertices.Length];
        var zMin = float.MaxValue;
        var zMax = float.MinValue;
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Scale(mesh.Vertices[i], half);
            zMin = Mathf.Min(zMin, vertices[i].z);
            zMax = Mathf.Max(zMax, vertices[i].z);
        }

        // The same scale in both views, so the proportions between them are the real ones.
        var scale = Size * 0.44f / Mathf.Max(half.x, half.y, half.z);
        var zMid = (zMin + zMax) * 0.5f;
        var center = Size * 0.5f;

        Vector2 Project(Vector3 v) => top
            ? new Vector2(center + v.x * scale, center + v.y * scale)
            : new Vector2(center + v.x * scale, center + (v.z - zMid) * scale);

        var triangleCount = mesh.Triangles.Length;
        var normals = new Vector3[triangleCount];
        var edges = new Dictionary<long, Vector2Int>();
        for (var t = 0; t < triangleCount; t++)
        {
            var tri = mesh.Triangles[t];
            normals[t] = Vector3.Cross(vertices[tri[1]] - vertices[tri[0]], vertices[tri[2]] - vertices[tri[0]]).normalized;

            for (var e = 0; e < 3; e++)
            {
                var a = tri[e];
                var b = tri[(e + 1) % 3];
                var key = (long)Mathf.Min(a, b) * 100000L + Mathf.Max(a, b);
                edges[key] = edges.TryGetValue(key, out var pair) ? new Vector2Int(pair.x, t) : new Vector2Int(t, -1);
            }
        }

        // Seen from above the crown faces +z; seen from the front (profile) the stone faces -y.
        bool Facing(int t) => top ? normals[t].z > 0.001f : normals[t].y < -0.001f;

        var pixels = new Color32[Size * Size];
        foreach (var entry in edges)
        {
            var a = (int)(entry.Key / 100000L);
            var b = (int)(entry.Key % 100000L);
            var t1 = entry.Value.x;
            var t2 = entry.Value.y;

            Color? color = null;
            if (t2 < 0) color = Ink;                                        // open edge
            else if (Facing(t1) != Facing(t2)) color = Ink;                 // outline of the stone
            else if (mesh.FacetIds[t1] != mesh.FacetIds[t2])
            {
                if (Facing(t1)) color = Ink;                                // facet edge on the visible side
                else if (top) color = Faint;                                // pavilion seen through the crown
            }

            if (color.HasValue) Line(pixels, Project(vertices[a]), Project(vertices[b]), color.Value);
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Two pixels thick, so the lines survive being shown smaller than the texture.
    private static void Line(Color32[] pixels, Vector2 from, Vector2 to, Color color)
    {
        var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y)) * 1.5f) + 1;
        Color32 c = color;
        for (var i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(from, to, i / (float)steps);
            var x = Mathf.RoundToInt(p.x);
            var y = Mathf.RoundToInt(p.y);
            Plot(pixels, x, y, c);
            Plot(pixels, x + 1, y, c);
            Plot(pixels, x, y + 1, c);
        }
    }

    private static void Plot(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return;
        var index = y * Size + x;

        // A dark line is never overdrawn by a light one.
        if (pixels[index].a > 0 && color.r > pixels[index].r) return;
        pixels[index] = color;
    }
}
