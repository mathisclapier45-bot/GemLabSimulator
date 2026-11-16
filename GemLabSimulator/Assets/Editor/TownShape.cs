using System.Collections.Generic;
using UnityEngine;

// The land of the town (editor only, used by SceneBootstrap.CreateTownScene): the old rectangle of streets is the core, and around it a dense forest with small lakes and marshes.
// The whole map is land except the lakes. Every array is indexed [x - minX, y - minY].
public static class TownShape
{
    // The corners of the core that hold nothing are cut: the forest comes closer there.
    private const int CornerCut = 16;

    public struct Lake
    {
        public float X;          // centre, in cells of the map (world coordinates)
        public float Y;
        public float RadiusX;
        public float RadiusY;
        public bool Marsh;       // a marsh: shallow, with lily pads and islets of grass
    }

    // ----- Noise (value noise, deterministic) -----

    private static float Hash(int ix, int iy, int seed)
    {
        unchecked
        {
            var n = (uint)(ix * 374761393 + iy * 668265263 + seed * 1442695041);
            n = (n ^ (n >> 13)) * 1274126177u;
            n ^= n >> 16;
            return (n & 0xffff) / 65535f;
        }
    }

    public static float ValueNoise(float x, float y, int seed)
    {
        var ix = Mathf.FloorToInt(x);
        var iy = Mathf.FloorToInt(y);
        var fx = x - ix;
        var fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        var a = Hash(ix, iy, seed);
        var b = Hash(ix + 1, iy, seed);
        var c = Hash(ix, iy + 1, seed);
        var d = Hash(ix + 1, iy + 1, seed);
        var top = a + (b - a) * fx;
        var bottom = c + (d - c) * fx;
        return top + (bottom - top) * fy;
    }

    public static float Fbm(float x, float y, int seed)
    {
        return ValueNoise(x, y, seed) * 0.65f + ValueNoise(x * 2.1f, y * 2.1f, seed + 7) * 0.35f;
    }

    // High where the ground rises: the hills (ledges) are put on these zones.
    public static float HillNoise(float x, float y)
    {
        return Fbm(x * 0.055f + 40f, y * 0.055f + 17f, 77);
    }

    // ----- The land -----

    // Distance of every cell to the part of the core that must stay clear (a two-pass chamfer transform); 0 inside it.
    public static float[,] DistanceToCore(int minX, int maxX, int minY, int maxY, int coreMinX, int coreMaxX, int coreMinY, int coreMaxY)
    {
        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        var dist = new float[w, h];
        for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
                dist[i, j] = Needed(i + minX, j + minY, coreMinX, coreMaxX, coreMinY, coreMaxY) ? 0f : 1e9f;
        const float Diagonal = 1.414f;
        for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
            {
                Relax(dist, w, h, i, j, i - 1, j, 1f);
                Relax(dist, w, h, i, j, i, j - 1, 1f);
                Relax(dist, w, h, i, j, i - 1, j - 1, Diagonal);
                Relax(dist, w, h, i, j, i + 1, j - 1, Diagonal);
            }
        for (var i = w - 1; i >= 0; i--)
            for (var j = h - 1; j >= 0; j--)
            {
                Relax(dist, w, h, i, j, i + 1, j, 1f);
                Relax(dist, w, h, i, j, i, j + 1, 1f);
                Relax(dist, w, h, i, j, i + 1, j + 1, Diagonal);
                Relax(dist, w, h, i, j, i - 1, j + 1, Diagonal);
            }
        return dist;
    }

    // The land mask: true everywhere except the lakes and marshes, which are scattered in the forest away from the core and from the edge of the map.
    public static bool[,] Build(int minX, int maxX, int minY, int maxY, float[,] distanceToCore, out List<Lake> lakes)
    {
        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        var land = new bool[w, h];
        for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
                land[i, j] = true;

        var candidates = new List<Vector2Int>();
        for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
            {
                var edge = Mathf.Min(Mathf.Min(i, w - 1 - i), Mathf.Min(j, h - 1 - j));
                if (distanceToCore[i, j] >= 5f && edge >= 9) candidates.Add(new Vector2Int(i, j));
            }
        var rng = new System.Random(5);
        for (var k = candidates.Count - 1; k > 0; k--)
        {
            var swap = rng.Next(k + 1);
            var temp = candidates[k]; candidates[k] = candidates[swap]; candidates[swap] = temp;
        }

        lakes = new List<Lake>();
        var centers = new List<Vector4>();
        foreach (var candidate in candidates)
        {
            var ok = true;
            foreach (var c in centers)
                if (Vector2.Distance(new Vector2(c.x, c.y), candidate) < 15f) { ok = false; break; }
            if (!ok) continue;

            var marsh = centers.Count % 3 == 2;
            var radiusX = marsh ? 3.5f + (float)rng.NextDouble() * 3f : 2.3f + (float)rng.NextDouble() * 2.5f;
            var radiusY = radiusX * (0.6f + (float)rng.NextDouble() * 0.35f);
            centers.Add(new Vector4(candidate.x, candidate.y, radiusX, radiusY));
            lakes.Add(new Lake { X = candidate.x + minX, Y = candidate.y + minY, RadiusX = radiusX, RadiusY = radiusY, Marsh = marsh });
            if (centers.Count >= 16) break;
        }

        foreach (var c in centers)
        {
            for (var i = Mathf.FloorToInt(c.x - c.z - 3f); i <= Mathf.CeilToInt(c.x + c.z + 3f); i++)
                for (var j = Mathf.FloorToInt(c.y - c.w - 3f); j <= Mathf.CeilToInt(c.y + c.w + 3f); j++)
                {
                    if (i < 0 || j < 0 || i >= w || j >= h || distanceToCore[i, j] <= 2.5f) continue;
                    var n = Fbm((i + minX) * 0.2f, (j + minY) * 0.2f, 31);
                    var v = Mathf.Pow((i - c.x) / c.z, 2f) + Mathf.Pow((j - c.y) / c.w, 2f);
                    if (v < 1f + 1.1f * (n - 0.5f)) land[i, j] = false;
                }
        }

        // Cells that would be a strip one tile wide, or a dead end, would need tiles that the set does not have: they become water.
        var changed = true;
        while (changed)
        {
            changed = false;
            for (var i = 0; i < w; i++)
                for (var j = 0; j < h; j++)
                {
                    if (!land[i, j] || distanceToCore[i, j] <= 2.5f) continue;
                    var n = !IsLand(land, w, h, i, j + 1);
                    var e = !IsLand(land, w, h, i + 1, j);
                    var s = !IsLand(land, w, h, i, j - 1);
                    var west = !IsLand(land, w, h, i - 1, j);
                    var count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (west ? 1 : 0);
                    if ((n && s) || (e && west) || count >= 3)
                    {
                        land[i, j] = false;
                        changed = true;
                    }
                }
        }
        return land;
    }

    private static bool Needed(int x, int y, int minX, int maxX, int minY, int maxY)
    {
        if (x < minX || x > maxX || y < minY || y > maxY) return false;
        if ((x - minX) + (maxY - y) < CornerCut) return false;
        if ((maxX - x) + (maxY - y) < CornerCut) return false;
        if ((x - minX) + (y - minY) < CornerCut) return false;
        if ((maxX - x) + (y - minY) < CornerCut) return false;
        return true;
    }

    private static void Relax(float[,] dist, int w, int h, int i, int j, int ni, int nj, float cost)
    {
        if (ni < 0 || nj < 0 || ni >= w || nj >= h) return;
        var candidate = dist[ni, nj] + cost;
        if (candidate < dist[i, j]) dist[i, j] = candidate;
    }

    // Outside the map counts as land: the edge of the map is a wall of forest, not water.
    private static bool IsLand(bool[,] land, int w, int h, int i, int j)
    {
        return i < 0 || j < 0 || i >= w || j >= h || land[i, j];
    }

    // The tile of the set Grass_Water_1 for a land cell (0 = plain grass): the edges 2 (water to the north), 4 (east), 6 (south, with its bank of earth), 8 (west),
    // the corners 1, 3, 5, 7 (north-west, north-east, south-west, south-east) and the notches of the inner corners 12 (north-west), 13 (north-east), 11 (south-west), 10 (south-east).
    public static int ShoreTile(bool[,] land, int minX, int minY, int x, int y)
    {
        var w = land.GetLength(0);
        var h = land.GetLength(1);
        var i = x - minX;
        var j = y - minY;
        bool Water(int a, int b) => !IsLand(land, w, h, a, b);

        var n = Water(i, j + 1);
        var e = Water(i + 1, j);
        var s = Water(i, j - 1);
        var west = Water(i - 1, j);
        var count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (west ? 1 : 0);

        if (count == 0)
        {
            if (Water(i - 1, j + 1)) return 12;
            if (Water(i + 1, j + 1)) return 13;
            if (Water(i + 1, j - 1)) return 10;
            if (Water(i - 1, j - 1)) return 11;
            return 0;
        }
        if (count == 1) return n ? 2 : e ? 4 : s ? 6 : 8;
        if (n && west) return 1;
        if (n && e) return 3;
        if (s && west) return 5;
        return 7;
    }
}
