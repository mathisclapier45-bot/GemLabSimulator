using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TownMapUI : MonoBehaviour
{
    public static TownMapUI Instance { get; private set; }

    // Must match TownMinX/MaxX/MinY/MaxY in SceneBootstrap.cs.
    private const float TownMinX = -70f;
    private const float TownMaxX = 70f;
    private const float TownMinY = -60f;
    private const float TownMaxY = 45f;

    // Must match the building positions passed to CreateRealBuilding/CreateTunnelEntrance in SceneBootstrap.cs.
    private static readonly (string scene, float x, float y)[] SceneToBuildingPos =
    {
        ("Home", -55f, 28f),
        ("Laboratoire", -30f, 28f),
        ("Musee", 20f, 28f),
        ("Marche", -45f, -15f),
        ("MarcheBrutes", 15f, -15f),
        ("Manoir", 45f, -15f),
        ("Tunnel", 0f, -50f),
    };

    private static readonly Color TownMarkerColor = Color.yellow;
    private static readonly Color IndoorMarkerColor = Color.red;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform mapAreaRect;
    [SerializeField] private RectTransform playerMarker;
    private Image playerMarkerImage;

    private void Awake()
    {
        Instance = this;

        // Resolved by hierarchy name instead of relying solely on the editor-time SerializedObject wiring,
        // since a freshly-added field can briefly fail to bind if the scene was regenerated before Unity
        // finished recompiling.
        if (panel == null)
            panel = transform.Find("TownMapPanel")?.gameObject;

        if (panel != null)
        {
            var mapArea = panel.transform.Find("MapArea");
            if (mapAreaRect == null && mapArea != null) mapAreaRect = mapArea as RectTransform;
            if (playerMarker == null && mapArea != null) playerMarker = mapArea.Find("PlayerMarker") as RectTransform;

            panel.SetActive(false);
        }

        if (playerMarker != null) playerMarkerImage = playerMarker.GetComponent<Image>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && panel != null)
            panel.SetActive(!panel.activeSelf);

        if (panel != null && panel.activeSelf)
            UpdatePlayerMarker();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void UpdatePlayerMarker()
    {
        if (playerMarker == null || mapAreaRect == null) return;

        var sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Town")
        {
            if (PlayerController.Instance == null) return;
            var pos = PlayerController.Instance.transform.position;
            SetMarker(pos.x, pos.y, TownMarkerColor);
            return;
        }

        foreach (var b in SceneToBuildingPos)
        {
            if (b.scene != sceneName) continue;
            SetMarker(b.x, b.y, IndoorMarkerColor);
            return;
        }

        playerMarker.gameObject.SetActive(false);
    }

    private void SetMarker(float worldX, float worldY, Color color)
    {
        playerMarker.gameObject.SetActive(true);
        if (playerMarkerImage != null) playerMarkerImage.color = color;

        var mapWidth = mapAreaRect.sizeDelta.x;
        var mapHeight = mapAreaRect.sizeDelta.y;
        var x = Mathf.InverseLerp(TownMinX, TownMaxX, worldX) * mapWidth - mapWidth / 2f;
        var y = Mathf.InverseLerp(TownMinY, TownMaxY, worldY) * mapHeight - mapHeight / 2f;
        playerMarker.anchoredPosition = new Vector2(x, y);
    }
}
