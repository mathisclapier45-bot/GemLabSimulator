using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

// The crypt: a medieval, very old place, far from the town (its entrance is in the south-east corner), where the Ouroboros meet, a secret brotherhood of gemologists.
// Menu GemLab > Setup Crypt Scene builds the scene "Crypte"; GemLab > Setup Town Scene (once more) places its entrance in the town. Everything is drawn here, pixel by pixel
// (stone floor and walls, columns, round table, bookshelves of grimoires, alchemy table, torches...), so the scene needs no picture of its own. The lights of the torches,
// the candles and the cauldron are added when the scene runs (DayNightLighting, objects named "Torche", "Candelabre", "Bougies" and "Chaudron").
public static partial class SceneBootstrap
{
    private const int CryptMinX = -16;
    private const int CryptMaxX = 16;
    private const int CryptMinY = -11;
    private const int CryptMaxY = 11;

    // ---- Little helpers to draw ------------------------------------------------------------------------------------------------------

    private static Color Px(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    private static Color Shade(Color c, float k)
    {
        return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    // A sprite of any size drawn by a function (x to the right, y upwards from the bottom); it keeps the sprite of an earlier run, as the others do.
    private static Sprite CryptSprite(string name, int width, int height, Func<int, int, Color> pixel)
    {
        var path = $"Assets/Art/Sprites/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x, y));
        texture.Apply();

        Directory.CreateDirectory("Assets/Art/Sprites");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        return ImportSprite(path);
    }

    private static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        var dx = (x - cx) / rx;
        var dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    private static readonly Color CryptClear = new Color(0f, 0f, 0f, 0f);
    private static readonly Color StoneBase = new Color(0.20f, 0.21f, 0.27f, 1f);
    private static readonly Color SeamColor = new Color(0.06f, 0.06f, 0.09f, 1f);
    private static readonly Color WoodDark = new Color(0.20f, 0.12f, 0.08f, 1f);
    private static readonly Color WoodMid = new Color(0.34f, 0.21f, 0.12f, 1f);
    private static readonly Color Gold = new Color(0.86f, 0.70f, 0.26f, 1f);

    // ---- The tiles ---------------------------------------------------------------------------------------------------------------------

    // Flagstones, in staggered rows, with seams, noise, a crack here and some moss there.
    private static Color CryptFloorPixel(int x, int y, int variant)
    {
        var row = y / 16;
        var offset = row % 2 == 0 ? 0 : 8;
        var cx = (x + offset) % 16;
        if (cx == 0 || y % 16 == 0) return SeamColor;

        var stone = (x + offset) / 16 + row * 3 + variant * 5;
        var color = Shade(StoneBase, 0.85f + 0.3f * Hash01(stone, row, 7));
        color = Shade(color, 0.94f + 0.12f * Hash01(x, y, variant + 1));
        if (variant == 2 && Mathf.Abs(x - 10 - y / 3) <= 0 && y > 5 && y < 27) return Shade(SeamColor, 1.4f);
        if (variant == 1 && y < 7 && x < 9 && Hash01(x, y, 11) > 0.35f) return Color.Lerp(color, Px(48, 78, 44), 0.6f);
        return color;
    }

    private static Color CryptWallPixel(int x, int y, bool lit)
    {
        var row = y / 8;
        var offset = row % 2 == 0 ? 0 : 8;
        var cx = (x + offset) % 16;
        var baseColor = lit ? Px(82, 78, 92) : Px(46, 44, 56);
        if (cx == 0 || y % 8 == 0) return Shade(baseColor, 0.45f);
        var brick = (x + offset) / 16 + row * 5;
        var color = Shade(baseColor, 0.82f + 0.36f * Hash01(brick, row, 3));
        color = Shade(color, 0.94f + 0.12f * Hash01(x, y, 5));
        // The lit face of the north wall darkens towards the floor.
        if (lit) color = Shade(color, 0.7f + 0.3f * (y / 31f));
        return color;
    }

    private static Color CryptCarpetPixel(int x, int y, bool leftEdge)
    {
        var edge = leftEdge ? x : 31 - x;
        var cloth = Shade(Px(96, 24, 32), 0.9f + 0.2f * Hash01(x, y, 2));
        if (edge < 3) return Px(30, 12, 14);
        if (edge == 3 || edge == 5) return Shade(Gold, 0.8f);
        if (edge == 4) return Px(60, 18, 24);
        return cloth;
    }

    // ---- The props ---------------------------------------------------------------------------------------------------------------------

    private static Color ColumnPixel(int x, int y)
    {
        var stone = Px(112, 112, 128);
        float k;
        if (y < 8)
        {
            if (x < 4 || x > 27) return CryptClear;
            k = y == 7 ? 1.15f : 0.9f;
        }
        else if (y < 54)
        {
            if (x < 9 || x > 22) return CryptClear;
            k = 0.78f + 0.32f * (1f - (x - 9) / 13f);
            if ((x - 9) % 4 == 0) k *= 0.82f;
        }
        else
        {
            if (x < 5 || x > 26) return CryptClear;
            k = y == 54 ? 0.8f : 1.05f;
        }
        return Shade(stone, k * (0.95f + 0.1f * Hash01(x, y, 4)));
    }

    // A round table of dark wood seen from above, with a golden ring engraved on it.
    private static Color TablePixel(int x, int y)
    {
        if (InEllipse(x, y, 44f, 32f, 40f, 26f))
        {
            var grain = 0.9f + 0.2f * Hash01(x / 6, y, 9);
            var color = Shade(WoodMid, grain);
            if (!InEllipse(x, y, 44f, 32f, 36f, 22f)) color = Shade(WoodMid, 1.3f);
            var ring = Mathf.Abs(Mathf.Sqrt(Mathf.Pow((x - 44f) / 24f, 2f) + Mathf.Pow((y - 32f) / 14f, 2f)) - 1f);
            if (ring < 0.06f) color = Gold;
            return color;
        }
        if (InEllipse(x, y, 44f, 27f, 40f, 26f)) return WoodDark;
        return CryptClear;
    }

    private static Color ChairPixel(int x, int y)
    {
        if (y >= 14 && y <= 23 && x >= 4 && x <= 15) return x % 3 == 0 ? WoodDark : Shade(WoodMid, 0.9f);
        if (y >= 6 && y <= 13 && x >= 2 && x <= 17) return Shade(WoodMid, 1.1f);
        if (y <= 5 && (x == 3 || x == 16)) return WoodDark;
        return CryptClear;
    }

    // The ouroboros, a serpent that bites its tail, as a mosaic on the floor: a ring of scales, a head with a red eye, a little rune circle inside.
    private static Color OuroborosPixel(int x, int y)
    {
        const float cx = 64f;
        const float cy = 64f;
        var dx = x - cx;
        var dy = y - cy;
        var distance = Mathf.Sqrt(dx * dx + dy * dy);
        var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;                 // -180..180, 90 is up
        var fromHead = Mathf.Repeat(90f - angle, 360f) / 360f;            // 0 at the head, going clockwise
        var thickness = 9f - 6f * fromHead;
        const float ringRadius = 50f;

        // The head, at the top of the ring.
        var head = new Vector2(cx, cy + ringRadius);
        if (Vector2.Distance(new Vector2(x, y), head) <= 9f)
        {
            if (Vector2.Distance(new Vector2(x, y), head + new Vector2(3f, 2f)) <= 2f) return Px(200, 30, 30);
            return Shade(Gold, 0.95f);
        }

        if (Mathf.Abs(distance - ringRadius) <= thickness)
        {
            var scale = 0.5f + 0.5f * Mathf.Sin(fromHead * 360f * 0.5f);
            var body = Color.Lerp(Px(48, 110, 70), Gold, scale * 0.8f);
            return Shade(body, 0.9f + 0.2f * Hash01(x, y, 6));
        }

        // The rune circle inside, with small marks.
        if (Mathf.Abs(distance - 36f) <= 1f) return Shade(Gold, 0.8f);
        if (distance < 36f)
        {
            for (var i = 0; i < 8; i++)
            {
                var a = i * 45f * Mathf.Deg2Rad;
                var mark = new Vector2(cx + Mathf.Cos(a) * 28f, cy + Mathf.Sin(a) * 28f);
                if (Vector2.Distance(new Vector2(x, y), mark) <= 2f) return Shade(Gold, 0.9f);
            }
            return new Color(0f, 0f, 0f, 0.28f);
        }
        return CryptClear;
    }

    private static Color SarcophagusPixel(int x, int y)
    {
        if (x < 2 || x > 61 || y < 2 || y > 36) return CryptClear;
        var stone = Px(104, 102, 116);
        // The body, then the lid on it, lighter, with a carved cross.
        if (y <= 14) return Shade(stone, 0.65f + 0.1f * Hash01(x, y, 8));
        if (x < 6 || x > 57 || y > 33) return Shade(stone, 0.8f);
        var color = Shade(stone, 1.05f + 0.1f * Hash01(x, y, 9));
        if ((x >= 30 && x <= 33 && y >= 18 && y <= 32) || (y >= 26 && y <= 29 && x >= 22 && x <= 41)) return Shade(stone, 0.55f);
        return color;
    }

    private static readonly Color[] SpineColors =
    {
        new Color(0.45f, 0.10f, 0.14f), new Color(0.12f, 0.32f, 0.22f), new Color(0.14f, 0.20f, 0.45f),
        new Color(0.36f, 0.22f, 0.10f), new Color(0.58f, 0.44f, 0.16f), new Color(0.34f, 0.14f, 0.40f),
    };

    private static Color BookshelfPixel(int x, int y)
    {
        // The frame and three shelves.
        if (x < 3 || x > 92 || y > 61 || y < 2) return WoodDark;
        var shelfY = y % 20;
        if (shelfY < 2) return WoodMid;
        if (x < 5 || x > 90) return WoodDark;

        // The books of this shelf: spines of different widths and heights; some gaps.
        var shelf = y / 20;
        var cursor = 5;
        var spineIndex = 0;
        while (cursor <= 90)
        {
            var width = 3 + (int)(Hash01(spineIndex, shelf, 21) * 3f);
            var height = 11 + (int)(Hash01(spineIndex, shelf, 22) * 6f);
            var gap = Hash01(spineIndex, shelf, 23) > 0.93f;
            if (x >= cursor && x < cursor + width)
            {
                if (gap) return Shade(WoodDark, 0.6f);
                if (y - shelf * 20 - 2 < height)
                {
                    var spine = SpineColors[(int)(Hash01(spineIndex, shelf, 24) * SpineColors.Length) % SpineColors.Length];
                    if ((y - shelf * 20) % 6 == 3 && Hash01(spineIndex, shelf, 25) > 0.5f) return Gold;
                    return Shade(spine, 0.85f + 0.3f * Hash01(x, y, 26));
                }
                return Shade(WoodDark, 0.6f);
            }
            cursor += width;
            spineIndex++;
        }
        return Shade(WoodDark, 0.6f);
    }

    private static Color AlchemyTablePixel(int x, int y)
    {
        // The table: a top and two legs.
        if (y >= 10 && y <= 20 && x >= 2 && x <= 61) return y == 20 ? Shade(WoodMid, 1.3f) : Shade(WoodMid, 0.9f + 0.2f * Hash01(x / 5, y, 3));
        if (y < 10 && y >= 0 && ((x >= 4 && x <= 7) || (x >= 56 && x <= 59))) return WoodDark;

        // A round flask with green liquid.
        if (InEllipse(x, y, 14f, 28f, 7f, 7f)) return y < 28 ? Px(70, 200, 110) : Px(190, 230, 210, 140);
        if (x >= 13 && x <= 15 && y > 34 && y <= 42) return Px(190, 230, 210, 200);
        // A tall red tube.
        if (x >= 30 && x <= 35 && y >= 21 && y <= 42) return y < 32 ? Px(200, 50, 50) : Px(220, 200, 200, 150);
        // A blue flask.
        if (InEllipse(x, y, 46f, 29f, 6f, 6f)) return Px(70, 110, 220);
        if (x >= 45 && x <= 47 && y > 34 && y <= 40) return Px(190, 210, 240, 200);
        // The copper alembic.
        if (InEllipse(x, y, 55f, 31f, 5f, 6f)) return Px(190, 110, 60);
        if (y >= 38 && y <= 40 && x >= 52 && x <= 62) return Px(160, 90, 50);
        return CryptClear;
    }

    private static Color CauldronPixel(int x, int y)
    {
        // The flame under the pot.
        if (y < 7 && Mathf.Abs(x - 16) < 5 - y / 2) return y < 3 ? Px(255, 210, 90) : Px(240, 120, 40);
        if (y >= 7 && y <= 9 && ((x >= 7 && x <= 9) || (x >= 22 && x <= 24))) return Px(30, 30, 36);
        // The pot, black iron.
        if (InEllipse(x, y, 16f, 18f, 12f, 5f))
        {
            if (InEllipse(x, y, 16f, 18f, 10f, 3.6f)) return Shade(Px(80, 230, 120), 0.9f + 0.2f * Hash01(x, y, 12));
            return Px(54, 54, 62);
        }
        if (y >= 8 && y <= 18 && InEllipse(x, y, 16f, 18f, 12f, 10f)) return Shade(Px(36, 36, 44), 0.8f + 0.4f * (x / 31f));
        return CryptClear;
    }

    private static Color TorchPixel(int x, int y)
    {
        if (y >= 18 && y <= 21 && x >= 4 && x <= 11) return Px(56, 50, 46);
        if (y >= 4 && y < 18 && x >= 6 && x <= 9) return Px(48, 44, 40);
        if (y >= 22 && y <= 30)
        {
            var width = 4 - (y - 22) / 2;
            if (Mathf.Abs(x - 7.5f) <= width * 0.6f + 0.5f) return y < 25 ? Px(255, 228, 120) : Px(255, 150, 50);
        }
        return CryptClear;
    }

    private static Color CandelabraPixel(int x, int y)
    {
        if (y <= 3 && x >= 3 && x <= 12) return Px(150, 112, 52);
        if (x >= 7 && x <= 8 && y >= 3 && y <= 28) return Px(150, 112, 52);
        if (y >= 26 && y <= 28 && x >= 2 && x <= 13) return Px(150, 112, 52);
        foreach (var cx in new[] { 3, 7, 12 })
        {
            if (x >= cx && x <= cx + 1 && y >= 29 && y <= 36) return Px(236, 226, 196);
            if (x >= cx && x <= cx + 1 && y >= 37 && y <= 41) return y < 39 ? Px(255, 240, 150) : Px(255, 160, 60);
        }
        return CryptClear;
    }

    private static Color BannerPixel(int x, int y)
    {
        if (y >= 61 && x >= 2 && x <= 29) return Gold;
        if (x < 4 || x > 27 || y < 8 || y > 60) return CryptClear;
        // The swallowtail at the bottom.
        if (y < 18 && Mathf.Abs(x - 15.5f) < (18 - y) * 0.9f) return CryptClear;
        if (x < 6 || x > 25) return Shade(Gold, 0.85f);
        // The emblem: a ring with a head.
        var distance = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 38f) * (y - 38f));
        if (Mathf.Abs(distance - 8f) <= 1.5f) return Gold;
        if (distance < 2f) return Gold;
        return Shade(Px(98, 22, 30), 0.9f + 0.2f * Hash01(x, y, 14));
    }

    private static Color CobwebPixel(int x, int y)
    {
        // Threads from the top-left corner.
        var white = new Color(0.9f, 0.9f, 0.95f, 0.5f);
        var py = 23 - y;
        var radial = x == py || x * 2 == py || x == py * 2;
        var arc = (Mathf.Abs(Mathf.Sqrt(x * x + py * py) - 8f) < 0.7f) || (Mathf.Abs(Mathf.Sqrt(x * x + py * py) - 15f) < 0.7f);
        return radial || arc ? white : CryptClear;
    }

    private static Color TombstonePixel(int x, int y)
    {
        var stone = Px(120, 120, 128);
        var inside = (y < 18 && x >= 2 && x <= 13) || InEllipse(x, y, 7.5f, 18f, 5.5f, 5f);
        if (!inside) return CryptClear;
        if ((x >= 7 && x <= 8 && y >= 10 && y <= 19) || (y >= 14 && y <= 15 && x >= 5 && x <= 10)) return Shade(stone, 0.55f);
        if (y < 6 && Hash01(x, y, 17) > 0.5f) return Color.Lerp(Shade(stone, 0.9f), Px(50, 80, 46), 0.6f);
        return Shade(stone, 0.85f + 0.3f * Hash01(x, y, 16));
    }

    // The entrance of the crypt, seen from the town: a mausoleum front in old stone, a low pediment, a dark door with steps, a small golden ouroboros above it, and some ivy.
    private static Color CryptEntrancePixel(int x, int y)
    {
        var stone = Px(118, 116, 126);
        if (y >= 56)
        {
            // The pediment.
            var half = 27 - (y - 56) * 3;
            if (half <= 0 || Mathf.Abs(x - 31.5f) > half) return CryptClear;
            return Shade(stone, 1.1f - 0.1f * ((y - 56) / 8f));
        }
        if (y < 8)
        {
            // The steps.
            var inset = (8 - y) % 3 == 0 ? 0 : 0;
            if (x < 16 + inset || x > 47 - inset) return CryptClear;
            return Shade(stone, 0.7f + 0.12f * (y % 3));
        }
        if (x < 4 || x > 59) return CryptClear;

        // The dark door, with a round arch.
        var inDoor = (x >= 22 && x <= 41 && y >= 8 && y <= 30) || InEllipse(x, y, 31.5f, 30f, 10f, 10f);
        if (inDoor)
        {
            var depth = 0.12f + 0.06f * Hash01(x, y, 18);
            return new Color(depth, depth, depth + 0.03f, 1f);
        }
        // The ouroboros above the door.
        var ring = Mathf.Abs(Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 47f) * (y - 47f)) - 5f);
        if (ring <= 1.1f) return Gold;
        // The stone, in blocks, with ivy at the sides.
        var row = y / 8;
        var offset = row % 2 == 0 ? 0 : 6;
        if ((x + offset) % 12 == 0 || y % 8 == 0) return Shade(stone, 0.6f);
        var color = Shade(stone, 0.8f + 0.3f * Hash01((x + offset) / 12, row, 19));
        if ((x < 14 || x > 49) && Hash01(x, y, 20) > 0.62f - 0.5f * (1f - Mathf.Abs(x - 31.5f) / 31.5f)) return Shade(Px(52, 94, 50), 0.8f + 0.4f * Hash01(x, y, 21));
        return color;
    }

    // ---- Placing ---------------------------------------------------------------------------------------------------------------------

    private static GameObject CryptProp(string objectName, Sprite sprite, Vector3 position, int sortingOrder, Vector2? solidSize = null, Vector2? solidOffset = null)
    {
        var go = new GameObject(objectName);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        go.transform.position = position;
        if (solidSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = solidSize.Value;
            box.offset = solidOffset ?? Vector2.zero;
        }
        return go;
    }

    // The entrance of the crypt in the town: the front of a mausoleum, with a few tombstones.
    private static void CreateCryptEntrance(Vector3 position)
    {
        var sprite = CryptSprite("crypt_entrance", 64, 64, CryptEntrancePixel);
        var go = new GameObject("La Crypte");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        // The stone of the front cannot be crossed; the door at the bottom is the way in.
        var solid = go.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(1.7f, 0.7f);
        solid.offset = new Vector2(0f, 0.45f);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.5f, 0.4f);
        trigger.offset = new Vector2(0f, -0.6f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        so.FindProperty("sceneName").stringValue = "Crypte";
        so.FindProperty("implemented").boolValue = true;
        so.ApplyModifiedProperties();

        CreateBuildingLabel("La Crypte", position + new Vector3(0f, 2.4f, 0f));

        var tombstone = CryptSprite("crypt_tombstone", 16, 24, TombstonePixel);
        CryptProp("Pierre tombale", tombstone, position + new Vector3(-3.6f, -1.2f, 0f), 4, new Vector2(0.4f, 0.25f), new Vector2(0f, -0.2f));
        CryptProp("Pierre tombale", tombstone, position + new Vector3(3.4f, -1.5f, 0f), 4, new Vector2(0.4f, 0.25f), new Vector2(0f, -0.2f));
        CryptProp("Pierre tombale", tombstone, position + new Vector3(-5.2f, 0.3f, 0f), 4, new Vector2(0.4f, 0.25f), new Vector2(0f, -0.2f));
        CryptProp("Pierre tombale", tombstone, position + new Vector3(5.0f, 0.6f, 0f), 4, new Vector2(0.4f, 0.25f), new Vector2(0f, -0.2f));
    }

    [MenuItem("GemLab/Setup Crypt Scene")]
    public static void CreateCryptScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var pixelPerfect = cam.gameObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = PixelsPerUnit;
        pixelPerfect.refResolutionX = 384;
        pixelPerfect.refResolutionY = 216;
        pixelPerfect.upscaleRT = false;
        pixelPerfect.pixelSnapping = true;
        pixelPerfect.cropFrameX = false;
        pixelPerfect.cropFrameY = false;

        var gridGO = new GameObject("Grid", typeof(Grid));

        var groundGO = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
        groundGO.transform.SetParent(gridGO.transform);
        var groundTilemap = groundGO.GetComponent<Tilemap>();

        var wallsGO = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallsGO.transform.SetParent(gridGO.transform);
        var wallsTilemap = wallsGO.GetComponent<Tilemap>();

        // The tiles: three flagstones, the dark wall, the lit face of the north wall, the carpet.
        var floorTiles = new Tile[3];
        for (var v = 0; v < 3; v++)
        {
            var variant = v;
            floorTiles[v] = GetOrCreateTileFromSprite($"crypt_floor_{v}", CryptSprite($"crypt_floor_{v}", 32, 32, (x, y) => CryptFloorPixel(x, y, variant)));
        }
        var wallTile = GetOrCreateTileFromSprite("crypt_wall", CryptSprite("crypt_wall", 32, 32, (x, y) => CryptWallPixel(x, y, false)));
        var topWallTile = GetOrCreateTileFromSprite("crypt_wall_lit", CryptSprite("crypt_wall_lit", 32, 32, (x, y) => CryptWallPixel(x, y, true)));
        var carpetLeft = GetOrCreateTileFromSprite("crypt_carpet_l", CryptSprite("crypt_carpet_l", 32, 32, (x, y) => CryptCarpetPixel(x, y, true)));
        var carpetRight = GetOrCreateTileFromSprite("crypt_carpet_r", CryptSprite("crypt_carpet_r", 32, 32, (x, y) => CryptCarpetPixel(x, y, false)));

        for (var x = CryptMinX; x <= CryptMaxX; x++)
        {
            for (var y = CryptMinY; y <= CryptMaxY; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                var perimeter = x == CryptMinX || x == CryptMaxX || y == CryptMinY || y == CryptMaxY;
                if (!perimeter)
                {
                    groundTilemap.SetTile(cell, floorTiles[(int)(Hash01(x, y, 40) * 3f) % 3]);
                    continue;
                }
                wallsTilemap.SetTile(cell, y == CryptMaxY ? topWallTile : wallTile);
            }
        }

        // The carpet from the door to the round table.
        for (var y = CryptMinY + 1; y <= 0; y++)
        {
            groundTilemap.SetTile(new Vector3Int(-1, y, 0), carpetLeft);
            groundTilemap.SetTile(new Vector3Int(0, y, 0), carpetRight);
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0f, CryptMinY + 3f, 0f);

        var player = new GameObject("Player");
        player.tag = "Player";
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlayerSprite();
        sr.sortingOrder = 5;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        // The way out, at the foot of the stairs, in the south wall.
        CreateExitDoor(new Vector3(0f, CryptMinY + 1f, 0f), "SpawnPoint_Crypte");

        FurnishCrypt();

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Crypte.unity");
        AddSceneToBuildSettings("Assets/Scenes/Crypte.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Crypt scene created at Assets/Scenes/Crypte.unity");
    }

    private static void FurnishCrypt()
    {
        var column = CryptSprite("crypt_column", 32, 64, ColumnPixel);
        var table = CryptSprite("crypt_table", 88, 64, TablePixel);
        var chair = CryptSprite("crypt_chair", 20, 24, ChairPixel);
        var ouroboros = CryptSprite("crypt_ouroboros", 128, 128, OuroborosPixel);
        var sarcophagus = CryptSprite("crypt_sarcophagus", 64, 40, SarcophagusPixel);
        var bookshelf = CryptSprite("crypt_bookshelf", 96, 64, BookshelfPixel);
        var alchemy = CryptSprite("crypt_alchemy", 64, 48, AlchemyTablePixel);
        var cauldron = CryptSprite("crypt_cauldron", 32, 32, CauldronPixel);
        var torch = CryptSprite("crypt_torch", 16, 32, TorchPixel);
        var candelabra = CryptSprite("crypt_candelabra", 16, 48, CandelabraPixel);
        var banner = CryptSprite("crypt_banner", 32, 64, BannerPixel);
        var cobweb = CryptSprite("crypt_cobweb", 24, 24, CobwebPixel);

        // The round table of the Ouroboros, on its mosaic, with eight chairs and the candles of the meeting.
        var center = new Vector3(0f, 3f, 0f);
        var mosaic = CryptProp("Mosaique Ouroboros", ouroboros, center, 1);
        mosaic.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        CryptProp("Table ronde", table, center, 4, new Vector2(2.2f, 0.9f), new Vector2(0f, -0.2f));
        CryptProp("Bougies", CryptSprite("crypt_candles", 16, 16, (x, y) =>
        {
            foreach (var cx in new[] { 3, 8, 12 })
            {
                if (x >= cx && x <= cx + 1 && y >= 2 && y <= 8) return Px(236, 226, 196);
                if (x >= cx && x <= cx + 1 && y >= 9 && y <= 12) return y < 11 ? Px(255, 240, 150) : Px(255, 160, 60);
            }
            return CryptClear;
        }), center + new Vector3(0f, 0.3f, 0f), 5);
        for (var i = 0; i < 8; i++)
        {
            var angle = i * 45f * Mathf.Deg2Rad;
            CryptProp("Chaise", chair, center + new Vector3(Mathf.Cos(angle) * 2.6f, Mathf.Sin(angle) * 1.7f - 0.1f, 0f), 4);
        }

        // The columns of the nave.
        foreach (var p in new[] { new Vector2(-9f, 6f), new Vector2(9f, 6f), new Vector2(-9f, -2f), new Vector2(9f, -2f), new Vector2(-4.5f, -6.5f), new Vector2(4.5f, -6.5f) })
            CryptProp("Colonne", column, new Vector3(p.x, p.y + 0.5f, 0f), 4, new Vector2(0.8f, 0.35f), new Vector2(0f, -0.6f));

        // The sarcophagi, along the west and east walls.
        foreach (var p in new[] { new Vector2(-13.2f, 3f), new Vector2(-13.2f, -3.5f), new Vector2(13.2f, 3f), new Vector2(13.2f, -3.5f) })
        {
            var tomb = CryptProp("Sarcophage", sarcophagus, new Vector3(p.x, p.y, 0f), 4, new Vector2(1.9f, 0.9f), new Vector2(0f, -0.1f));
            tomb.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // The shelves of grimoires, against the north wall, and the banners of the brotherhood between them.
        foreach (var x in new[] { -11f, -6.5f, 6.5f, 11f })
            CryptProp("Bibliotheque de grimoires", bookshelf, new Vector3(x, CryptMaxY - 1.2f, 0f), 4, new Vector2(2.9f, 0.7f), new Vector2(0f, -0.9f));
        foreach (var x in new[] { -2.5f, 2.5f })
            CryptProp("Banniere Ouroboros", banner, new Vector3(x, CryptMaxY - 0.2f, 0f), 3);

        // The alchemy corner, in the north-west: a table with its flasks and a cauldron.
        CryptProp("Table d'alchimie", alchemy, new Vector3(-13f, 4.8f, 0f), 4, new Vector2(1.9f, 0.6f), new Vector2(0f, -0.4f));
        CryptProp("Chaudron", cauldron, new Vector3(-14.2f, 2.4f, 0f), 4, new Vector2(0.8f, 0.5f), new Vector2(0f, -0.2f));

        // The torches on the north wall and on the side walls, and the candelabras by the carpet.
        foreach (var x in new[] { -14f, -9f, -4f, 0f, 4f, 9f, 14f })
            CryptProp("Torche", torch, new Vector3(x, CryptMaxY + 0.3f, 0f), 3);
        foreach (var p in new[] { new Vector2(-1.8f, -4f), new Vector2(1.8f, -4f), new Vector2(-1.8f, 1f), new Vector2(1.8f, 1f) })
            CryptProp("Candelabre", candelabra, new Vector3(p.x, p.y + 0.7f, 0f), 4, new Vector2(0.4f, 0.25f), new Vector2(0f, -0.7f));

        // Cobwebs in the corners.
        var web = CryptProp("Toile", cobweb, new Vector3(CryptMinX + 1.2f, CryptMaxY - 1.0f, 0f), 6);
        var web2 = CryptProp("Toile", cobweb, new Vector3(CryptMaxX - 1.2f, CryptMaxY - 1.0f, 0f), 6);
        web2.transform.localScale = new Vector3(-1f, 1f, 1f);
        web.name = "Toile";
    }
}
