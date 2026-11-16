using UnityEngine;

// Software renderer that draws a gem mesh as pixel art, from any orientation. Flat-shaded facets, a light at the
// upper left, quantized tones, a dark rim: the same look as the 2D icons, but with real volume when it turns.
// Each instance owns a small texture that can be redrawn every frame and shown in a UI Image.
public class GemView3D
{
    // The stone's largest half-size covers this fraction of the texture, so a rotated stone never gets clipped.
    public const float FitFraction = 0.84f;

    public Sprite Sprite { get; private set; }

    // The picture as drawn, for effects that compose with it (the inclusions inside the stone).
    public Color32[] Pixels => pixels;
    public int Size => size;

    private readonly int size;
    private readonly Texture2D texture;
    private readonly Color32[] pixels;
    private readonly float[] depth;
    private readonly int[] ids;
    private readonly float[] tones;
    private Vector3[] rotated = new Vector3[0];

    private static readonly Vector3 HalfVector = (GemPixelArt.Light + Vector3.forward).normalized;

    // A realistic view draws the faceted stones with the ray-tracing shader (see RealGemRenderer); domes and rough stones keep the pixel art.
    public bool Realistic;

    // Whether the inclusions that show without a loupe are painted in the stone (a view that draws the inclusions itself turns this off).
    public bool PaintInclusions = true;

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
        if (!gem.IsRough) return GemMesh.Get(gem.ShapeName, gem.CutStyleName, gem.HasOpenCulet);

        // A rough crystal has the mesh of its habit; a lump of rock the one of its species.
        var habit = gem.Habit;
        var mesh = habit != null ? GemMesh.GetHabit(habit.Id) : null;
        return mesh ?? GemMesh.GetRough((int)StoneOptics.StableHash(gem.Definition.name));
    }

    // Half sizes of the stone in mm. Length and width are fitted to the proportions of the shape, so a stone drawn as
    // round never shows up oval in the loupe (the footprint area is kept).
    public static Vector3 HalfExtents(GemInstance gem)
    {
        var length = Mathf.Max(gem.LengthMm, 0.1f);
        var width = Mathf.Max(gem.WidthMm, 0.1f);

        // A crystal habit fits in the unit sphere: its largest dimension is the largest of the three.
        if (gem.IsRough && gem.Habit != null && GemMesh.GetHabit(gem.Habit.Id) != null)
        {
            var biggest = Mathf.Max(length, width, Mathf.Max(gem.DepthMm, 0.1f));
            return Vector3.one * biggest * 0.5f;
        }
        if (!gem.IsRough)
        {
            var ratio = GemMesh.AspectRatio(gem.ShapeName);
            length = Mathf.Sqrt(length * width / ratio);
            width = length * ratio;
        }
        var depth = Mathf.Max(gem.DepthMm, 0.1f);
        if (!gem.IsRough) depth *= gem.Quality.DepthScale;                  // proportions that are not ideal: a shallow pavilion, a deep one
        return new Vector3(length, width, depth) * 0.5f;
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
        if (gem.HasColor)
        {
            body = gem.BodyColor;
            facet = gem.FacetColor;
            return;
        }
        body = def.iconColor.a > 0f ? def.iconColor : new Color(0.75f, 0.75f, 0.8f);
        facet = def.iconColor.a > 0f ? def.iconFacetColor : new Color(0.92f, 0.92f, 0.96f);
    }

    private Vector3[] rotatedNormals = new Vector3[0];
    private float[] vertexTones = new float[0];

    public void Draw(GemMesh mesh, Vector3 halfExtentsMm, Quaternion orientation, Color body, Color facet, Diaphaneity? level = null, int starRays = 0, GemLook? look = null, bool catsEye = false, StoneQuality? quality = null)
    {
        var gemLook = look ?? GemLook.Default;
        if (Realistic && RealGemRenderer.TryRender(mesh, halfExtentsMm, orientation, body, gemLook, level, size, pixels, starRays, catsEye, quality, PaintInclusions))
        {
            texture.filterMode = FilterMode.Bilinear;
            // The shader has put the inclusions and the zoning inside the stone: only what belongs to the surface and to the haze is painted on.
            var volumetric = RealGemRenderer.LastVolumetric;
            if (quality.HasValue) StoneQualityFx.Apply(pixels, size, quality.Value, body, orientation, halfExtentsMm, PaintInclusions && !volumetric, volumetric);
            texture.SetPixels32(pixels);
            texture.Apply();
            return;
        }
        texture.filterMode = FilterMode.Point;
        var vertexCount = mesh.Vertices.Length;
        if (rotated.Length < vertexCount) rotated = new Vector3[vertexCount];
        smoothDome = mesh.Smooth && mesh.Normals != null;
        if (smoothDome)
        {
            if (rotatedNormals.Length < vertexCount) { rotatedNormals = new Vector3[vertexCount]; vertexTones = new float[vertexCount]; }
            var inverse = new Vector3(1f / Mathf.Max(halfExtentsMm.x, 0.001f), 1f / Mathf.Max(halfExtentsMm.y, 0.001f), 1f / Mathf.Max(halfExtentsMm.z, 0.001f));
            for (var i = 0; i < vertexCount; i++)
            {
                // The normal of a stretched surface: scaled by the inverse of the stretch, then turned with the stone.
                rotatedNormals[i] = (orientation * Vector3.Scale(mesh.Normals[i], inverse)).normalized;
                vertexTones[i] = DomeTone(rotatedNormals[i]);
            }
        }

        var maxHalf = Mathf.Max(halfExtentsMm.x, halfExtentsMm.y, halfExtentsMm.z);
        var scale = size * FitFraction * 0.5f / maxHalf;
        var center = size * 0.5f;

        for (var i = 0; i < vertexCount; i++)
            rotated[i] = orientation * Vector3.Scale(mesh.Vertices[i], halfExtentsMm) * scale;

        facetNormals.Clear();
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

            if (smoothDome)
            {
                // A dome: the light is worked out at each corner and spread across the triangle, so the surface is smooth.
                Raster(p0, p1, p2, center, vertexTones[tri[0]], vertexTones[tri[1]], vertexTones[tri[2]], 1);
                continue;
            }

            var diffuse = Vector3.Dot(normal, GemPixelArt.Light);
            var specular = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, HalfVector)), 28f);
            var matte = (diffuse + 0.2f) / 1.15f + specular * 0.7f;

            // A brilliant stone mirrors the light: each facet shows what it reflects (a bright window, a dark corner), so neighbours differ a lot. A matte one is just lit.
            var tone = Mathf.Lerp(matte, Mirror(normal), gemLook.Brilliance);

            // Every facet gets its own small brightness offset, so neighbouring facets stay readable, as on a real cut stone.
            var facetId = mesh.FacetIds[t];
            tone += ((facetId * 73) % 11 / 10f - 0.5f) * Mathf.Lerp(0.16f, 0.26f, gemLook.Brilliance);
            if (gemLook.Contrast > 1f) tone = (tone - 0.5f) * gemLook.Contrast + 0.5f;

            // The normals of the facets that show, for the flashes of a fiery stone.
            if (gemLook.Fire > 0f)
            {
                facetNormals.TryGetValue(facetId, out var sum);
                facetNormals[facetId] = sum + normal;
            }

            // A big flat face (the table, the side of a prism) is a calm area of the stone, not its brightest: the flashes come from the tilted facets around it.
            if (normal.z > 0.96f) tone = Mathf.Min(tone, 0.78f);
            if (gemLook.ToneCap > 0f) tone = Mathf.Min(tone, gemLook.ToneCap);

            Raster(p0, p1, p2, center, tone, tone, tone, facetId);
        }

        Finish(body, facet, level, gemLook);
        if (gemLook.Fire > 0f && !smoothDome) DrawFire(gemLook.Fire, gemLook.Seed);
        if (starRays > 0) DrawStar(starRays, orientation);
        if (catsEye) DrawCatsEye(orientation);
        if (quality.HasValue) StoneQualityFx.Apply(pixels, size, quality.Value, body, orientation, halfExtentsMm, PaintInclusions);
        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private static readonly Vector3 KeyLight = new Vector3(-0.45f, 0.55f, 0.70f).normalized;
    private static readonly Vector3 FillLight = new Vector3(0.62f, -0.40f, 0.68f).normalized;

    // What a facet of normal `normal` mirrors, looking at it from the front: the reflected direction meets a big window of light at the upper left, a smaller one at the lower right,
    // a soft glow in front, and a dark room elsewhere.
    private static float Mirror(Vector3 normal)
    {
        var reflected = new Vector3(2f * normal.z * normal.x, 2f * normal.z * normal.y, 2f * normal.z * normal.z - 1f);
        var key = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(reflected, KeyLight)), 5f) * 1.05f;
        var fill = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(reflected, FillLight)), 9f) * 0.55f;
        var glow = Mathf.Clamp01(reflected.z * 0.5f + 0.5f) * 0.34f;
        return 0.14f + glow + key + fill;
    }

    private bool smoothDome;

    // The light on a smooth dome: a soft diffuse light from the upper left, a small bright highlight, a little light bounced back at the lower right.
    private static float DomeTone(Vector3 n)
    {
        var diffuse = Mathf.Max(0f, Vector3.Dot(n, GemPixelArt.Light));
        var highlight = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, HalfVector)), 70f) * 0.55f + Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, HalfVector)), 9f) * 0.12f;
        var bounce = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, FillLight)), 5f) * 0.14f;
        return 0.14f + diffuse * 0.62f + highlight + bounce;
    }

    private void Raster(Vector3 a, Vector3 b, Vector3 c, float center, float tone0, float tone1, float tone2, int id)
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
                tones[index] = w0 * tone0 + w1 * tone1 + w2 * tone2;
            }
        }
    }

    // The star of a cabochon with dense silk: lines of light across the dome, turning with the stone and fading as it
    // tilts away from the viewer.
    private void DrawStar(int rays, Quaternion orientation)
    {
        var facing = (orientation * Vector3.forward).z;
        if (facing < 0.35f) return;

        var strength = Mathf.Clamp01((facing - 0.35f) / 0.5f);
        var axis = orientation * Vector3.right;
        var baseAngle = Mathf.Atan2(axis.y, axis.x);
        var center = size * 0.5f;
        var length = size * 0.42f;
        var lines = Mathf.Max(1, rays / 2);
        var halfWidth = Mathf.Max(0.7f, size * 0.012f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (ids[i] < 0) continue;

                var dx = x + 0.5f - center;
                var dy = y + 0.5f - center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > length) continue;

                var best = 0f;
                for (var k = 0; k < lines; k++)
                {
                    var angle = baseAngle + k * Mathf.PI / lines;
                    var d = Mathf.Abs(dx * Mathf.Sin(angle) - dy * Mathf.Cos(angle));
                    best = Mathf.Max(best, Mathf.Clamp01(1f - d / (halfWidth * 2f)));
                }

                var falloff = 1f - r / length;
                var intensity = (best * (0.3f + 0.7f * falloff) + Mathf.Clamp01(1f - r / (length * 0.2f)) * 0.45f) * strength;
                if (intensity > 0.02f) pixels[i] = Color32.Lerp(pixels[i], new Color32(255, 255, 255, 255), Mathf.Clamp01(intensity));
            }
        }
    }

    private static float PixelHash(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519u);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    private readonly System.Collections.Generic.Dictionary<int, Vector3> facetNormals = new System.Collections.Generic.Dictionary<int, Vector3>();
    private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>> fireFacets = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();

    // The fire of a diamond-like stone: flashes on the facets that run from facet to facet as the stone turns. Each facet has several tiny hidden lights of its own, at slightly
    // different directions; it flashes when it mirrors one of them toward the eye, and the flash slides across the facet as the angle changes. A flash is a white heart with
    // four rays, tinted by one colour of the rainbow that follows the angle (blue when it is just right, then green, yellow, orange, red as the facet drifts away).
    private void DrawFire(float fire, int seed)
    {
        // The pixels of each facet, away from the outline.
        foreach (var list in fireFacets.Values) list.Clear();
        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var i = y * size + x;
                if (ids[i] < 0 || ids[i - 1] < 0 || ids[i + 1] < 0 || ids[i - size] < 0 || ids[i + size] < 0) continue;
                if (!fireFacets.TryGetValue(ids[i], out var list)) fireFacets[ids[i]] = list = new System.Collections.Generic.List<int>();
                list.Add(i);
            }
        }

        var scale = Mathf.Max(1f, size / 32f);                 // the flashes grow with the picture
        var white = new Color32(255, 255, 255, 255);
        const int lights = 6;
        foreach (var pair in fireFacets)
        {
            var id = pair.Key;
            var list = pair.Value;
            if (list.Count < 2 || !facetNormals.TryGetValue(id, out var sum) || sum.sqrMagnitude < 1e-6f) continue;

            var n = sum.normalized;
            var reflected = new Vector3(2f * n.z * n.x, 2f * n.z * n.y, 2f * n.z * n.z - 1f);
            float cx = 0f, cy = 0f;
            foreach (var i in list) { cx += i % size; cy += i / size; }
            cx /= list.Count;
            cy /= list.Count;

            for (var k = 0; k < lights; k++)
            {
                // The hidden light of this facet: the key light, nudged in a direction of its own.
                var nudge = new Vector3(PixelHash(id, 21 + k, seed) - 0.5f, PixelHash(id, 31 + k, seed) - 0.5f, (PixelHash(id, 41 + k, seed) - 0.5f) * 0.5f) * 1.5f;
                var light = (KeyLight + nudge).normalized;
                var c = Vector3.Dot(reflected, light);
                if (c < 0.93f) continue;

                var strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.93f, 0.995f, c)) * fire;
                if (strength < 0.08f) continue;

                // The flash slides across the facet as the angle changes: it sits where the reflected light is off by a little.
                var shift = new Vector2(reflected.x - light.x, reflected.y - light.y) * size * 0.35f;
                var targetX = cx + shift.x + (PixelHash(id, 61 + k, seed) - 0.5f) * 3f * scale;
                var targetY = cy + shift.y + (PixelHash(id, 71 + k, seed) - 0.5f) * 3f * scale;
                var pick = list[0];
                var best = float.MaxValue;
                foreach (var i in list)
                {
                    var d = (i % size - targetX) * (i % size - targetX) + (i / size - targetY) * (i / size - targetY);
                    if (d < best) { best = d; pick = i; }
                }
                var px = pick % size;
                var py = pick / size;

                // Blue when well lined up, then green, yellow, orange, red: the colours of the rainbow as the facet drifts.
                var deviation = Mathf.InverseLerp(0.995f, 0.93f, c);
                var hue = Mathf.Lerp(0.60f, 0.0f, Mathf.Clamp01(deviation));
                var tint = (Color32)Color.HSVToRGB(hue, 1f, 1f);

                // A pinpoint: one white pixel, with a ray of one pixel on each side when the flash is strong (two at the very strongest), tinted toward the tips. Nothing wider.
                pixels[pick] = white;
                var reach = Mathf.Max(1, Mathf.RoundToInt((strength > 0.85f ? 2f : 1f) * Mathf.Sqrt(scale)));
                if (strength > 0.35f)
                {
                    foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                    {
                        for (var step = 1; step <= reach; step++)
                        {
                            var jx = px + d.x * step;
                            var jy = py + d.y * step;
                            if (jx < 0 || jx >= size || jy < 0 || jy >= size) break;
                            var j = jy * size + jx;
                            if (ids[j] < 0) break;
                            var ray = step == 1 ? Color32.Lerp(white, tint, 0.25f) : tint;
                            pixels[j] = Color32.Lerp(pixels[j], ray, step == 1 ? 0.85f : 0.65f);
                        }
                    }
                }
                else
                {
                    // A weak flash is just a coloured point next to the white one.
                    var jx = px + (PixelHash(id, 81 + k, seed) > 0.5f ? 1 : -1);
                    var j = py * size + jx;
                    if (jx >= 0 && jx < size && ids[j] >= 0) pixels[j] = Color32.Lerp(pixels[j], tint, 0.8f);
                }
            }
        }
    }

    // The cat's eye of a cabochon with parallel fibres: a thin bright line across the dome, perpendicular to the fibres, that rolls as the stone is tilted.
    private void DrawCatsEye(Quaternion orientation)
    {
        var facing = (orientation * Vector3.forward).z;
        if (facing < 0.30f) return;
        var strength = Mathf.Clamp01((facing - 0.30f) / 0.45f);

        // The fibres run along the length of the stone; the line goes across.
        var fibre = orientation * Vector3.right;
        var along = new Vector2(fibre.x, fibre.y);
        if (along.sqrMagnitude < 0.0001f) along = Vector2.right;
        along.Normalize();
        var across = new Vector2(-along.y, along.x);

        // Where the light is mirrored: the line sits on the side of the dome that faces the light, and moves when the stone turns.
        var light = new Vector2(HalfVector.x, HalfVector.y);
        var half = size * FitFraction * 0.5f;
        var offset = Mathf.Clamp(Vector2.Dot(light, along) * half * 1.4f, -half * 0.45f, half * 0.45f);
        var center = size * 0.5f;
        var lineHalfLength = half * 0.95f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (ids[i] < 0) continue;

                var p = new Vector2(x + 0.5f - center, y + 0.5f - center);
                var u = Vector2.Dot(p, along) - offset;           // distance to the line, across its width
                var v = Vector2.Dot(p, across);                   // position along the line
                if (Mathf.Abs(v) > lineHalfLength) continue;

                // Thin and sharp in the middle, a soft glow beside it, the line tapering toward its ends.
                var taper = Mathf.Sqrt(Mathf.Max(0f, 1f - (v * v) / (lineHalfLength * lineHalfLength)));
                var core = Mathf.Clamp01(1f - Mathf.Abs(u) / 0.95f);
                var glow = Mathf.Clamp01(1f - Mathf.Abs(u) / 3.2f) * 0.35f;
                var intensity = (core * 0.95f + glow) * taper * strength;
                if (intensity > 0.03f) pixels[i] = Color32.Lerp(pixels[i], new Color32(255, 250, 235, 255), Mathf.Clamp01(intensity));
            }
        }
    }

    // Colors, facet creases and the dark rim.
    private void Finish(Color body, Color facet, Diaphaneity? level, GemLook look)
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

                Color color = smoothDome ? GemPixelArt.PaletteSmooth(tone, body, facet)
                    : tone > 1.12f ? Color.white : GemPixelArt.Palette(Mathf.Clamp01(tone), body, facet);
                if (level.HasValue) color = GemDiaphaneity.Shade(color, tone, level.Value, body, x, y);
                if (look.Pattern != GemPattern.None) color = GemPatterns.Apply(color, x, y, size, tone, look, body);
                pixels[i] = color;
            }
        }
    }
}
