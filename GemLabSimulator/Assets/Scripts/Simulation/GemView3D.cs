using UnityEngine;

// Software renderer that draws a gem mesh as pixel art, from any orientation. Flat-shaded facets, a light at the
// upper left, quantized tones, a dark rim: the same look as the 2D icons, but with real volume when it turns.
// Each instance owns a small texture that can be redrawn every frame and shown in a UI Image.
public class GemView3D
{
    // The stone's largest half-size covers this fraction of the texture, so a rotated stone never gets clipped.
    public const float FitFraction = 0.84f;

    public Sprite Sprite { get; private set; }

    private readonly int size;
    private readonly Texture2D texture;
    private readonly Color32[] pixels;
    private readonly float[] depth;
    private readonly int[] ids;
    private readonly float[] tones;
    private Vector3[] rotated = new Vector3[0];

    private static readonly Vector3 HalfVector = (GemPixelArt.Light + Vector3.forward).normalized;

    public GemView3D(int size = 48)
    {
        this.size = size;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        pixels = new Color32[size * size];
        depth = new float[size * size];
        ids = new int[size * size];
        tones = new float[size * size];
        Sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static GemMesh MeshFor(GemInstance gem)
    {
        return gem.IsRough
            ? GemMesh.GetRough((int)StoneOptics.StableHash(gem.Definition.name))
            : GemMesh.Get(gem.ShapeName, gem.CutStyleName, gem.HasOpenCulet);
    }

    // Half sizes of the stone in mm. Length and width are fitted to the proportions of the shape, so a stone drawn as
    // round never shows up oval in the loupe (the footprint area is kept).
    public static Vector3 HalfExtents(GemInstance gem)
    {
        var length = Mathf.Max(gem.LengthMm, 0.1f);
        var width = Mathf.Max(gem.WidthMm, 0.1f);
        if (!gem.IsRough)
        {
            var ratio = GemMesh.AspectRatio(gem.ShapeName);
            length = Mathf.Sqrt(length * width / ratio);
            width = length * ratio;
        }
        return new Vector3(length, width, Mathf.Max(gem.DepthMm, 0.1f)) * 0.5f;
    }

    // Largest dimension of the stone in millimeters, which is what the image side is scaled from.
    public static float MaxDimensionMm(GemInstance gem)
    {
        var half = HalfExtents(gem);
        return Mathf.Max(half.x, half.y, half.z) * 2f;
    }

    public static void Colors(GemInstance gem, out Color body, out Color facet)
    {
        var def = gem.Definition;
        body = def.iconColor.a > 0f ? def.iconColor : new Color(0.75f, 0.75f, 0.8f);
        facet = def.iconColor.a > 0f ? def.iconFacetColor : new Color(0.92f, 0.92f, 0.96f);
    }

    public void Draw(GemMesh mesh, Vector3 halfExtentsMm, Quaternion orientation, Color body, Color facet)
    {
        var vertexCount = mesh.Vertices.Length;
        if (rotated.Length < vertexCount) rotated = new Vector3[vertexCount];

        var maxHalf = Mathf.Max(halfExtentsMm.x, halfExtentsMm.y, halfExtentsMm.z);
        var scale = size * FitFraction * 0.5f / maxHalf;
        var center = size * 0.5f;

        for (var i = 0; i < vertexCount; i++)
            rotated[i] = orientation * Vector3.Scale(mesh.Vertices[i], halfExtentsMm) * scale;

        var clear = new Color32(0, 0, 0, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
            depth[i] = float.MinValue;
            ids[i] = -1;
            tones[i] = 0f;
        }

        for (var t = 0; t < mesh.Triangles.Length; t++)
        {
            var tri = mesh.Triangles[t];
            var p0 = rotated[tri[0]];
            var p1 = rotated[tri[1]];
            var p2 = rotated[tri[2]];

            var normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (normal.z <= 0f) continue;                       // facing away from the viewer
            normal.Normalize();

            var diffuse = Vector3.Dot(normal, GemPixelArt.Light);
            var specular = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, HalfVector)), 28f);
            var tone = (diffuse + 0.2f) / 1.15f + specular * 0.7f;

            // Every facet gets its own small brightness offset, so neighbouring facets stay readable, as on a real cut stone.
            var facetId = mesh.FacetIds[t];
            tone += ((facetId * 73) % 11 / 10f - 0.5f) * 0.16f;

            Raster(p0, p1, p2, center, tone, facetId);
        }

        Finish(body, facet);
        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private void Raster(Vector3 a, Vector3 b, Vector3 c, float center, float tone, int id)
    {
        var ax = a.x + center;
        var ay = a.y + center;
        var bx = b.x + center;
        var by = b.y + center;
        var cx = c.x + center;
        var cy = c.y + center;

        var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx))));
        var maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx))));
        var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy))));
        var maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy))));

        var area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        if (Mathf.Abs(area) < 1e-5f) return;
        var inverse = 1f / area;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                var py = y + 0.5f;
                var w1 = ((px - ax) * (cy - ay) - (py - ay) * (cx - ax)) * inverse;
                var w2 = ((bx - ax) * (py - ay) - (by - ay) * (px - ax)) * inverse;
                var w0 = 1f - w1 - w2;
                if (w0 < -0.001f || w1 < -0.001f || w2 < -0.001f) continue;

                var z = w0 * a.z + w1 * b.z + w2 * c.z;
                var index = y * size + x;
                if (z <= depth[index]) continue;

                depth[index] = z;
                ids[index] = id;
                tones[index] = tone;
            }
        }
    }

    // Colors, facet creases and the dark rim.
    private void Finish(Color body, Color facet)
    {
        var half = size * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (ids[i] < 0) continue;

                var rim = x == 0 || y == 0 || x == size - 1 || y == size - 1
                          || ids[i - 1] < 0 || ids[i + 1] < 0 || ids[i - size] < 0 || ids[i + size] < 0;
                if (rim)
                {
                    pixels[i] = GemPixelArt.Dim(body, x < half && y > half ? 0.50f : 0.20f);
                    continue;
                }

                var tone = tones[i];

                // Neighboring facets with nearly the same tone would blur together: add a thin dark crease.
                var leftDiffers = ids[i - 1] != ids[i] && Mathf.Abs(tones[i - 1] - tone) < 0.08f;
                var belowDiffers = ids[i - size] != ids[i] && Mathf.Abs(tones[i - size] - tone) < 0.08f;
                if (leftDiffers || belowDiffers) tone -= 0.12f;

                pixels[i] = tone > 1.12f ? new Color32(255, 255, 255, 255) : (Color32)GemPixelArt.Palette(Mathf.Clamp01(tone), body, facet);
            }
        }
    }
}
