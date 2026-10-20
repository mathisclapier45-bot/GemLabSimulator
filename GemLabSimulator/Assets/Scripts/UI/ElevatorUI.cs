using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Floor selector of the Diamond Quarter elevator. Built at runtime, so no scene wiring is needed.
public class ElevatorUI : MonoBehaviour
{
    public static ElevatorUI Instance { get; private set; }

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color CurrentColor = new Color(0.30f, 0.55f, 0.35f, 1f);
    private static readonly Color LockedColor = new Color(0.20f, 0.20f, 0.22f, 1f);

    private GameObject panel;
    private Text statusText;
    private Button[] floorButtons;

    public static void TryToggle()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<ElevatorUI>();
        }

        if (Instance == null) return;
        if (Instance.panel.activeSelf) Instance.Close();
        else Instance.Open();
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        var current = SceneManager.GetActiveScene().name;
        var licensed = DiamondBuilding.FloorsUnlocked;
        for (var i = 0; i < floorButtons.Length; i++)
        {
            var scene = DiamondBuilding.Floors[i].scene;
            var isCurrent = scene == current;
            var locked = !licensed && i > 0;
            floorButtons[i].interactable = !isCurrent && !locked;
            floorButtons[i].GetComponent<Image>().color = isCurrent ? CurrentColor : locked ? LockedColor : ButtonColor;
        }

        statusText.text = licensed
            ? (DiamondBuilding.HasLicense ? "Licence de diamantaire : valide." : "Accès libre aux étages (mode exploration).")
            : "Les étages sont réservés aux titulaires de la licence de diamantaire (en vente à l'accueil).";
        statusText.color = licensed ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.95f, 0.55f, 0.45f);
    }

    private void GoTo(int index)
    {
        var floor = DiamondBuilding.Floors[index];
        if (index > 0 && !DiamondBuilding.FloorsUnlocked) return;

        Close();
        PlayerController.PendingSpawnPointName = "SpawnPoint_Elevator";
        SceneFader.FadeToScene(floor.scene);
    }

    private void BuildPanel()
    {
        var floors = DiamondBuilding.Floors;
        const float width = 400f;
        var height = 90f + floors.Length * 40f + 50f;

        panel = new GameObject("ElevatorPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.11f, 0.97f);

        RuntimeUIKit.MakeText("Title", panel.transform, "Ascenseur", 20, TextAnchor.UpperCenter, Gold,
            new Vector2(20f, -12f), new Vector2(width - 40f, 28f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 12, TextAnchor.UpperCenter, Color.white,
            new Vector2(20f, -44f), new Vector2(width - 40f, 34f));

        floorButtons = new Button[floors.Length];
        // The top floor is drawn first, like the buttons of a real elevator.
        for (var i = 0; i < floors.Length; i++)
        {
            var index = floors.Length - 1 - i;
            var captured = index;
            floorButtons[index] = RuntimeUIKit.MakeButton("Floor" + index, panel.transform, floors[index].label, 14, ButtonColor,
                new Vector2(30f, -90f - i * 40f), new Vector2(width - 60f, 34f));
            floorButtons[index].onClick.AddListener(() => GoTo(captured));
        }

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(width / 2f - 60f, -(90f + floors.Length * 40f + 6f)), new Vector2(120f, 30f));
        close.onClick.AddListener(Close);
    }
}
