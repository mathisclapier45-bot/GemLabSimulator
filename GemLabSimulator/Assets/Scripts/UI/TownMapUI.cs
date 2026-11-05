using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The town map opened with C: the same map as the minimap in the corner (see MinimapUI), but showing the whole town at
// once, with a name under each building and a marker where the player is. The map is the one drawn the last time the
// town was loaded, so it also opens from inside a building. Built at runtime; the panel of older scenes is not used.
public class TownMapUI : MonoBehaviour
{
    public static TownMapUI Instance { get; private set; }

    // Where each indoor scene's building stands in the town: must match the building positions in SceneBootstrap.cs.
    private static readonly (string scene, float x, float y)[] SceneToBuildingPos =
    {
        ("Home", -55f, 28f),
        ("MaisonJoueur", -52f, -50f),
        ("GemStore", 35f, 7f),
        ("WatchStore", -31.5f, -15f),
        ("Animalerie", 8.5f, 7f),
        ("DiamantairesAccueil", 45f, 28f),
        ("Diamantaires1", 45f, 28f),
        ("Diamantaires2", 45f, 28f),
        ("Diamantaires3", 45f, 28f),
        ("Diamantaires4", 45f, 28f),
        ("Diamantaires5", 45f, 28f),
        ("Diamantaires6", 45f, 28f),
        ("DiamantairesToit", 45f, 28f),
        ("Laboratoire", -30f, 28f),
        ("AtelierLapidaire", -5f, 28f),
        ("LaboSynthese", -24f, -35f),
        ("LaboTraitements", -10f, -35f),
        ("Musee", 20f, 28f),
        ("Banque", 22f, 7f),
        ("Bijouterie", -22f, 7f),
        ("Marche", -45f, -15f),
        ("MarcheBrutes", 15f, -15f),
        ("Manoir", 45f, -15f),
        ("Tunnel", 0f, -50f),
    };

    private const float MapScale = 1.4f;              // screen pixels per texture pixel
    private const float IconSize = 24f;

    private static readonly Color Gold = new Color(0.86f, 0.70f, 0.36f, 1f);
    private static readonly Color GoldDark = new Color(0.45f, 0.32f, 0.14f, 1f);

    [SerializeField] private GameObject panel;        // panel of older scenes, no longer used

    private GameObject bigPanel;
    private RectTransform mapArea;
    private RectTransform marker;
    private RectTransform markerOutline;
    private Text notice;
    private float pulse;

    private void Awake()
    {
        Instance = this;

        if (panel == null) panel = transform.Find("TownMapPanel")?.gameObject;
        if (panel != null)
        {
            panel.SetActive(false);
            Destroy(panel);
            panel = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (bigPanel != null && bigPanel.activeSelf) Close();
            else Open();
        }
        else if (bigPanel != null && bigPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (bigPanel != null && bigPanel.activeSelf) UpdateMarker();
    }

    public void Close()
    {
        if (bigPanel != null) bigPanel.SetActive(false);
    }

    public void Open()
    {
        // Rebuilt each time: the doors that were locked or open may have changed since the last visit.
        Build();
        bigPanel.SetActive(true);
        bigPanel.transform.SetAsLastSibling();
        UpdateMarker();
    }

    // ---- Panel ---------------------------------------------------------------------------------------------

    private void Build()
    {
        if (bigPanel != null)
        {
            bigPanel.transform.SetParent(null, false);
            Destroy(bigPanel);
        }

        var texture = MinimapUI.SharedMap;
        var mapWidth = (MinimapUI.MapMaxX - MinimapUI.MapMinX) * 4f * MapScale;
        var mapHeight = (MinimapUI.MapMaxY - MinimapUI.MapMinY) * 4f * MapScale;

        bigPanel = new GameObject("TownMapBig", typeof(RectTransform), typeof(Image));
        bigPanel.transform.SetParent(transform, false);
        var rect = bigPanel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(mapWidth + 40f, mapHeight + 76f);
        rect.anchoredPosition = Vector2.zero;
        bigPanel.GetComponent<Image>().color = GoldDark;

        // Frame like the minimap's: dark outer line, gold ring, dark inner line.
        RuntimeUIKit.MakeBox("GoldRing", bigPanel.transform, new Vector2(4f, -4f), new Vector2(mapWidth + 32f, mapHeight + 68f), Gold, false);
        RuntimeUIKit.MakeBox("InnerRing", bigPanel.transform, new Vector2(10f, -10f), new Vector2(mapWidth + 20f, mapHeight + 56f), new Color(0.18f, 0.12f, 0.06f, 1f), false);

        var title = RuntimeUIKit.MakeText("Title", bigPanel.transform, "Carte de la ville", 20, TextAnchor.MiddleLeft, new Color(1f, 0.95f, 0.8f),
            new Vector2(24f, -14f), new Vector2(300f, 30f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Hint", bigPanel.transform, "C : fermer", 12, TextAnchor.MiddleRight, new Color(0.75f, 0.70f, 0.60f),
            new Vector2(mapWidth - 200f, -14f), new Vector2(160f, 30f));
        var close = RuntimeUIKit.MakeButton("Close", bigPanel.transform, "X", 14, new Color(0.30f, 0.22f, 0.20f, 1f),
            new Vector2(mapWidth - 30f, -15f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        // The map itself.
        var areaGO = new GameObject("MapArea", typeof(RectTransform), typeof(RawImage), typeof(RectMask2D));
        areaGO.transform.SetParent(bigPanel.transform, false);
        mapArea = areaGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(mapArea, new Vector2(20f, -46f), new Vector2(mapWidth, mapHeight));
        var image = areaGO.GetComponent<RawImage>();
        image.texture = texture;
        image.color = texture != null ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        image.raycastTarget = false;

        if (texture == null)
        {
            notice = RuntimeUIKit.MakeText("Notice", mapArea, "Carte indisponible : passez d'abord par la ville.", 16, TextAnchor.MiddleCenter,
                Color.white, Vector2.zero, new Vector2(mapWidth, mapHeight));
        }
        else
        {
            notice = null;
            BuildIcons(mapWidth, mapHeight);
        }

        BuildMarker();
    }

    private void BuildIcons(float mapWidth, float mapHeight)
    {
        foreach (var icon in MinimapUI.SharedIcons)
        {
            var sprite = Resources.Load<Sprite>($"MinimapIcons/{icon.Key}");
            if (sprite == null) continue;

            var position = ToMap(icon.World.x, icon.World.y, mapWidth, mapHeight);
            var badge = MinimapUI.CreateBadge("Icon_" + icon.Key, mapArea, sprite, icon.Accessible, IconSize);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 0f);
            badge.anchoredPosition = position;

            var label = RuntimeUIKit.MakeText("Name", mapArea, icon.Name, 11, TextAnchor.UpperCenter,
                icon.Accessible ? new Color(1f, 0.96f, 0.82f) : new Color(0.78f, 0.78f, 0.82f), Vector2.zero, new Vector2(120f, 16f));
            var labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = position + new Vector2(0f, -(IconSize / 2f + 6f));
            label.fontStyle = FontStyle.Bold;
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        }
    }

    private void BuildMarker()
    {
        var circle = RuntimeUIKit.MakeRoundedSprite(128, 64);

        markerOutline = MakeDot("MarkerOutline", circle, 18f, Color.white);
        marker = MakeDot("Marker", circle, 12f, new Color(0.95f, 0.25f, 0.20f, 1f));
    }

    private RectTransform MakeDot(string name, Sprite circle, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(mapArea, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        var image = go.GetComponent<Image>();
        image.sprite = circle;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Vector2 ToMap(float worldX, float worldY, float mapWidth, float mapHeight)
    {
        var x = Mathf.InverseLerp(MinimapUI.MapMinX, MinimapUI.MapMaxX, worldX) * mapWidth;
        var y = Mathf.InverseLerp(MinimapUI.MapMinY, MinimapUI.MapMaxY, worldY) * mapHeight;
        return new Vector2(x, y);
    }

    // In town the marker follows the player; inside a building it sits on that building.
    private void UpdateMarker()
    {
        if (marker == null || mapArea == null) return;

        var sceneName = SceneManager.GetActiveScene().name;
        Vector2? world = null;

        if (sceneName == "Town")
        {
            if (PlayerController.Instance != null) world = PlayerController.Instance.transform.position;
        }
        else
        {
            foreach (var building in SceneToBuildingPos)
            {
                if (building.scene != sceneName) continue;
                world = new Vector2(building.x, building.y);
                break;
            }
        }

        var visible = world.HasValue;
        marker.gameObject.SetActive(visible);
        markerOutline.gameObject.SetActive(visible);
        if (!visible) return;

        var position = ToMap(world.Value.x, world.Value.y, mapArea.sizeDelta.x, mapArea.sizeDelta.y);
        marker.anchoredPosition = position;
        markerOutline.anchoredPosition = position;

        // Small heartbeat so the player's dot is easy to spot.
        pulse += Time.unscaledDeltaTime * 3f;
        var scale = Vector3.one * (1f + 0.15f * Mathf.Sin(pulse));
        marker.localScale = scale;
        markerOutline.localScale = scale;
    }
}
