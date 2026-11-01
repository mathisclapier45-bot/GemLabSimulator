using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Round minimap in the top-right corner of the town. It shows a wide, minimalist map of the whole town (grass, streets,
// river, paths, buildings as coloured blocks, trees as dots) drawn once from the scene's tiles and buildings, and scrolls
// to follow the player, who sits at the centre. Added to the Canvas automatically in the Town scene.
public class MinimapUI : MonoBehaviour
{
    private const float DiscSize = 168f;              // diameter of the map itself, in canvas units
    private const float ViewRadius = 40f;             // world units shown from the centre to the edge
    private const int PixelsPerUnit = 4;

    // Must match the town bounds in SceneBootstrap.cs (cells -70..70 and -60..45, inclusive).
    public const int MapMinX = -70;
    public const int MapMaxX = 71;
    public const int MapMinY = -60;
    public const int MapMaxY = 46;

    private static readonly Color Gold = new Color(0.86f, 0.70f, 0.36f, 1f);
    private static readonly Color GoldDark = new Color(0.45f, 0.32f, 0.14f, 1f);

    private static readonly Color32 GrassColor = new Color32(140, 184, 120, 255);
    private static readonly Color32 BorderColor = new Color32(70, 110, 70, 255);
    private static readonly Color32 RoadColor = new Color32(112, 116, 126, 255);
    private static readonly Color32 SidewalkColor = new Color32(176, 178, 186, 255);
    private static readonly Color32 DirtColor = new Color32(200, 174, 128, 255);
    private static readonly Color32 BridgeColor = new Color32(160, 118, 76, 255);
    private static readonly Color32 WaterColor = new Color32(84, 148, 206, 255);
    private static readonly Color32 RunwayColor = new Color32(70, 72, 82, 255);
    private static readonly Color32 TreeColor = new Color32(88, 142, 84, 255);
    private static readonly Color32 BuildingOpen = new Color32(238, 190, 88, 255);
    private static readonly Color32 BuildingLocked = new Color32(186, 156, 150, 255);
    private static readonly Color32 BuildingEdge = new Color32(92, 64, 40, 255);

    // Icon of each building on the map, by the name of the building's GameObject (sprites in Resources/MinimapIcons).
    private static readonly Dictionary<string, string> IconKeys = new Dictionary<string, string>
    {
        { "Maison & Bureau de Gemmologie", "maison" },
        { "Laboratoire de Gemmologie", "fiole" },
        { "Atelier Lapidaire", "atelier" },
        { "Musee", "musee" },
        { "Immeuble des Diamantaires", "diamant" },
        { "Marche aux Pierres", "rubis_dollar" },
        { "Marche aux Brutes", "brut_dollar" },
        { "Ecole", "ecole" },
        { "Le Manoir", "manoir" },
        { "Bijouterie", "bague" },
        { "Banque et Encheres", "banque" },
        { "Concessionnaire Automobile", "voiture" },
        { "Maison de Vincent Pardieu", "cabane" },
        { "Aeroport", "avion" },
        { "Bibliotheque", "livre" },
        { "GemStore", "sac" },
        { "Maison du personnage", "maison_perso" },
        { "Boutique de vêtements", "vetement" },
        { "Le Tunnel", "pioche" },
        { "WatchStore", "montre" },
        { "Animalerie", "patte" },
    };

    // Names of the buildings as shown on the big map (the town map opened with C).
    private static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { "Musee", "Musée" },
        { "Marche aux Pierres", "Marché aux Pierres" },
        { "Marche aux Brutes", "Marché aux Brutes" },
        { "Ecole", "École" },
        { "Aeroport", "Aéroport" },
        { "Bibliotheque", "Bibliothèque" },
        { "Banque et Encheres", "Banque et Enchères" },
        { "Concessionnaire Automobile", "Concessionnaire" },
        { "Maison & Bureau de Gemmologie", "Maison & Bureau" },
        { "Immeuble des Diamantaires", "Diamantaires" },
        { "Laboratoire de Gemmologie", "Laboratoire" },
        { "Maison de Vincent Pardieu", "Maison de V. Pardieu" },
    };

    // What the town looked like the last time it was loaded: the big map (C) draws from it, also from inside a building.
    public struct BuildingIcon
    {
        public Vector2 World;
        public string Key;
        public string Name;
        public bool Accessible;
    }

    public static Texture2D SharedMap { get; private set; }
    public static readonly List<BuildingIcon> SharedIcons = new List<BuildingIcon>();

    private const float IconSize = 20f;
    private readonly List<(Vector2 world, RectTransform rect)> buildingIcons = new List<(Vector2, RectTransform)>();

    private Texture2D mapTexture;
    private RawImage mapView;
    private Transform marker;
    private float pulse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (scene.name != "Town") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<MinimapUI>() == null) canvas.AddComponent<MinimapUI>();
    }

    private void Awake()
    {
        BuildMapTexture();
        SharedIcons.Clear();
        BuildFrame();
    }

    // The texture outlives the scene (the big map uses it everywhere): the next town load replaces it.
    private void OnDestroy()
    {
    }

    // ---- Map drawing ---------------------------------------------------------------------------------------

    private void BuildMapTexture()
    {
        var width = (MapMaxX - MapMinX) * PixelsPerUnit;
        var height = (MapMaxY - MapMinY) * PixelsPerUnit;
        mapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[width * height];
        var ground = FindTilemap("Ground");
        var water = FindTilemap("Water");
        var boundary = FindTilemap("Boundary");
        var tileColors = new Dictionary<TileBase, Color32>();

        for (var cy = MapMinY; cy < MapMaxY; cy++)
        {
            for (var cx = MapMinX; cx < MapMaxX; cx++)
            {
                var cell = new Vector3Int(cx, cy, 0);
                Color32 color = BorderColor;
                var waterTile = water != null ? water.GetTile(cell) : null;
                var groundTile = ground != null ? ground.GetTile(cell) : null;
                if (waterTile != null) color = WaterColor;
                else if (groundTile != null) color = ColorFor(groundTile, tileColors);
                else if (boundary != null && boundary.GetTile(cell) != null) color = BorderColor;

                var px0 = (cx - MapMinX) * PixelsPerUnit;
                var py0 = (cy - MapMinY) * PixelsPerUnit;
                for (var dy = 0; dy < PixelsPerUnit; dy++)
                    for (var dx = 0; dx < PixelsPerUnit; dx++)
                        pixels[(py0 + dy) * width + px0 + dx] = color;
            }
        }

        DrawTrees(pixels, width, height);
        DrawBuildings(pixels, width, height);

        mapTexture.SetPixels32(pixels);
        mapTexture.Apply();

        if (SharedMap != null) Destroy(SharedMap);
        SharedMap = mapTexture;
    }

    private static Tilemap FindTilemap(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<Tilemap>() : null;
    }

    private static Color32 ColorFor(TileBase tile, Dictionary<TileBase, Color32> cache)
    {
        if (cache.TryGetValue(tile, out var known)) return known;

        var name = tile.name;
        Color32 color = GrassColor;
        if (name.StartsWith("asphalt")) color = RoadColor;
        else if (name.StartsWith("sidewalk")) color = SidewalkColor;
        else if (name.StartsWith("path")) color = DirtColor;
        else if (name.StartsWith("bridge")) color = BridgeColor;
        else if (name.StartsWith("runway")) color = RunwayColor;
        else if (name.StartsWith("water")) color = WaterColor;

        cache[tile] = color;
        return color;
    }

    private static void DrawTrees(Color32[] pixels, int width, int height)
    {
        foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            var name = renderer.gameObject.name;
            if (!name.StartsWith("Arbre") && !name.StartsWith("Cyprès")) continue;

            var position = renderer.transform.position;
            var px = Mathf.RoundToInt((position.x - MapMinX) * PixelsPerUnit);
            var py = Mathf.RoundToInt((position.y + 0.6f - MapMinY) * PixelsPerUnit);
            FillRect(pixels, width, height, px - 3, py - 3, px + 3, py + 3, TreeColor);
        }
    }

    // Buildings (everything with a door): a coloured block over the footprint of the sprite, outlined.
    private static void DrawBuildings(Color32[] pixels, int width, int height)
    {
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
        {
            var renderer = door.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;

            var bounds = renderer.bounds;
            var accessible = door.IsAccessible;
            var x0 = Mathf.RoundToInt((bounds.min.x - MapMinX) * PixelsPerUnit);
            var x1 = Mathf.RoundToInt((bounds.max.x - MapMinX) * PixelsPerUnit);
            var y0 = Mathf.RoundToInt((bounds.min.y - MapMinY) * PixelsPerUnit);
            var y1 = Mathf.RoundToInt((bounds.max.y - MapMinY) * PixelsPerUnit);
            // Tall sprites show their roof: keep a compact block (at most 8 units tall) at the foot of the building.
            y1 = Mathf.Min(y1, y0 + 8 * PixelsPerUnit);

            FillRect(pixels, width, height, x0, y0, x1, y1, BuildingEdge);
            FillRect(pixels, width, height, x0 + 2, y0 + 2, x1 - 2, y1 - 2, accessible ? BuildingOpen : BuildingLocked);
        }
    }

    private static void FillRect(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
    {
        for (var y = Mathf.Max(0, y0); y <= Mathf.Min(height - 1, y1); y++)
            for (var x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++)
                pixels[y * width + x] = color;
    }

    // ---- Frame ---------------------------------------------------------------------------------------------

    private void BuildFrame()
    {
        var circle = RuntimeUIKit.MakeRoundedSprite(128, 64);

        var root = new GameObject("Minimap", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.sizeDelta = new Vector2(DiscSize + 24f, DiscSize + 24f);
        rootRect.anchoredPosition = new Vector2(-16f, -16f);

        // Frame: soft shadow, dark outer ring, gold ring, thin inner dark line.
        MakeDisc("Shadow", root.transform, circle, DiscSize + 24f, new Color(0f, 0f, 0f, 0.35f));
        MakeDisc("OuterRing", root.transform, circle, DiscSize + 18f, GoldDark);
        MakeDisc("GoldRing", root.transform, circle, DiscSize + 12f, Gold);
        MakeDisc("InnerRing", root.transform, circle, DiscSize + 4f, new Color(0.18f, 0.12f, 0.06f, 1f));

        // The map itself, clipped to a disc.
        var mapGO = MakeDisc("Map", root.transform, circle, DiscSize, Color.white);
        var mask = mapGO.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var viewGO = new GameObject("View", typeof(RectTransform), typeof(RawImage));
        viewGO.transform.SetParent(mapGO.transform, false);
        var viewRect = viewGO.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;
        mapView = viewGO.GetComponent<RawImage>();
        mapView.texture = mapTexture;
        mapView.raycastTarget = false;

        BuildIcons(mapGO.transform);

        // Player marker: a pulsing dot with a white outline at the centre.
        var outline = MakeDisc("MarkerOutline", root.transform, circle, 14f, Color.white);
        var dot = MakeDisc("Marker", root.transform, circle, 10f, new Color(0.95f, 0.25f, 0.20f, 1f));
        marker = dot.transform;
        outline.raycastTarget = false;
        dot.raycastTarget = false;

        // Cardinal point at the top, as on a compass.
        var north = RuntimeUIKit.MakeText("North", root.transform, "N", 13, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f),
            Vector2.zero, new Vector2(20f, 18f));
        var northRect = north.GetComponent<RectTransform>();
        northRect.anchorMin = northRect.anchorMax = new Vector2(0.5f, 1f);
        northRect.pivot = new Vector2(0.5f, 1f);
        northRect.anchoredPosition = new Vector2(0f, -14f);
        north.fontStyle = FontStyle.Bold;
        var shadow = north.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
    }

    // One small badge per building, placed over the map (clipped by the disc, so it slides in and out at the edge).
    private void BuildIcons(Transform parent)
    {
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
        {
            var renderer = door.GetComponent<SpriteRenderer>();
            if (renderer == null || !IconKeys.TryGetValue(door.gameObject.name, out var key)) continue;

            var sprite = Resources.Load<Sprite>($"MinimapIcons/{key}");
            if (sprite == null) continue;

            var bounds = renderer.bounds;
            var world = new Vector2(bounds.center.x, bounds.min.y + Mathf.Min(bounds.size.y, 8f) / 2f);
            var accessible = door.IsAccessible;

            var badge = CreateBadge("Icon_" + key, parent, sprite, accessible, IconSize);

            SharedIcons.Add(new BuildingIcon
            {
                World = world,
                Key = key,
                Name = DisplayNames.TryGetValue(door.gameObject.name, out var display) ? display : door.gameObject.name,
                Accessible = accessible,
            });
            buildingIcons.Add((world, badge));
        }
    }

    // Round badge with a ring and the glyph of a building, used by the minimap and by the big town map.
    public static RectTransform CreateBadge(string name, Transform parent, Sprite glyph, bool accessible, float iconSize)
    {
        var circle = RuntimeUIKit.MakeRoundedSprite(64, 32);
        var badge = MakeDisc(name, parent, circle, iconSize + 8f,
            accessible ? new Color(0.20f, 0.15f, 0.07f, 0.92f) : new Color(0.20f, 0.20f, 0.23f, 0.85f));
        var ring = MakeDisc("Ring", badge.transform, circle, iconSize + 8f, accessible ? Gold : new Color(0.55f, 0.55f, 0.60f, 1f));
        ring.rectTransform.sizeDelta = new Vector2(iconSize + 8f, iconSize + 8f);
        var inner = MakeDisc("Inner", badge.transform, circle, iconSize + 4f, accessible ? new Color(0.20f, 0.15f, 0.07f, 1f) : new Color(0.20f, 0.20f, 0.23f, 1f));

        var iconGO = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(badge.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize - 2f, iconSize - 2f);
        iconRect.anchoredPosition = Vector2.zero;
        var iconImage = iconGO.GetComponent<Image>();
        iconImage.sprite = glyph;
        iconImage.color = accessible ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        iconImage.raycastTarget = false;
        inner.raycastTarget = false;
        return badge.rectTransform;
    }

    private static Image MakeDisc(string name, Transform parent, Sprite circle, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = circle;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void LateUpdate()
    {
        var player = PlayerController.Instance;
        if (player != null && mapView != null)
        {
            var position = player.transform.position;
            var worldWidth = MapMaxX - MapMinX;
            var worldHeight = MapMaxY - MapMinY;
            mapView.uvRect = new Rect((position.x - ViewRadius - MapMinX) / worldWidth, (position.y - ViewRadius - MapMinY) / worldHeight,
                2f * ViewRadius / worldWidth, 2f * ViewRadius / worldHeight);
        }

        if (player != null)
        {
            var origin = (Vector2)player.transform.position;
            var scale = (DiscSize / 2f) / ViewRadius;
            foreach (var (world, rect) in buildingIcons)
            {
                var offset = (world - origin) * scale;
                var visible = offset.magnitude < DiscSize / 2f + 16f;
                if (rect.gameObject.activeSelf != visible) rect.gameObject.SetActive(visible);
                if (visible) rect.anchoredPosition = offset;
            }
        }

        // Small heartbeat so the player's dot is easy to spot.
        pulse += Time.unscaledDeltaTime * 3f;
        if (marker != null) marker.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(pulse));
    }
}
