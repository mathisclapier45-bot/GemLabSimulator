using UnityEngine;
using UnityEngine.UI;

// Magnet test: drop a gem on the table next to a strong magnet. If the stone reacts it slides toward the magnet,
// faster the stronger its attraction, and the closer it gets.
public class MagnetUI : MonoBehaviour
{
    public static MagnetUI Instance { get; private set; }

    private const float PanelWidth = 640f;
    private const float PanelHeight = 340f;
    private const float AreaX = 20f;
    private const float AreaY = -44f;
    private const float AreaWidth = 440f;
    private const float AreaHeight = 210f;
    private const float TableTop = -150f;         // y of the table surface, inside the panel
    private const float StoneSize = 44f;
    private const float MagnetFace = 322f;        // x (inside the area) of the magnet's left face
    private const float StartX = 130f;
    private const float PxPerMm = 4f;
    private const float ObservationTime = 3f;

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);

    private GameObject panel;
    private RectTransform areaRect;
    private RectTransform gemRect;
    private Image gemImage;
    private Text hintText;
    private Text distanceText;
    private Text resultText;
    private Text strengthText;

    private GemInstance stone;
    private float strength;
    private float stoneX;            // center, in area coordinates
    private float watched;
    private bool observed;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<MagnetUI>();
        }

        if (Instance == null) return false;
        Instance.Open();
        return true;
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        stone = null;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    private float MaxStoneX => MagnetFace - StoneSize / 2f;

    private void SetStone(GemInstance gem, Vector2 screenPosition)
    {
        stone = gem;
        // Each specimen is a little more or less magnetic than the species average.
        var variation = 0.9f + (StableHash(gem.SpecimenId ?? "") & 0xFF) / 255f * 0.2f;
        strength = Mathf.Clamp01(MagnetInfo.AttractionStrength(gem.Definition) * variation);

        // The stone is released where the pointer was, on the table.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(areaRect, screenPosition, null, out var local);
        stoneX = Mathf.Clamp(local.x, StoneSize / 2f + 4f, MaxStoneX);

        watched = 0f;
        observed = stone.HasObserved(ExaminationTool.MeasurementType.Magnetism.ToString());
        Refresh();
    }

    private void ResetPosition()
    {
        if (stone == null) return;
        stoneX = StartX;
        watched = 0f;
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || stone == null) return;

        var gap = MagnetFace - (stoneX + StoneSize / 2f);

        if (strength > 0f && gap > 0f)
        {
            // Pull grows quickly as the stone gets closer: 1 / distance.
            var speed = strength * (50f + 9000f / (gap + 40f));
            stoneX = Mathf.Min(stoneX + speed * Time.deltaTime, MaxStoneX);
            gap = MagnetFace - (stoneX + StoneSize / 2f);
        }

        watched += Time.deltaTime;
        if (!observed && watched >= ObservationTime) FinishObservation();

        ApplyPosition();
    }

    private void FinishObservation()
    {
        observed = true;
        stone.RecordObservation(ExaminationTool.MeasurementType.Magnetism.ToString());
        ToolFeedbackUI.Instance?.ShowMessage($"Aimant — {stone.Definition.magnetism}");
        UpdateTexts();
    }

    private void ApplyPosition()
    {
        RuntimeUIKit.SetTopLeft(gemRect, Vector2.zero, new Vector2(StoneSize, StoneSize));
        gemRect.anchorMin = gemRect.anchorMax = new Vector2(0f, 1f);
        gemRect.pivot = new Vector2(0.5f, 0f);
        gemRect.anchoredPosition = new Vector2(AreaX + stoneX, TableTop);

        var gapMm = Mathf.Max(0f, MagnetFace - (stoneX + StoneSize / 2f)) / PxPerMm;
        distanceText.text = stone == null ? "" : gapMm < 0.5f ? "Distance : contact" : $"Distance : {gapMm:F0} mm";
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = "Déposez une gemme sur la table, à côté de l'aimant";
            distanceText.text = "";
        }
        else
        {
            gemImage.enabled = true;
            gemImage.sprite = stone.DisplaySprite;
            hintText.text = "";
            ApplyPosition();
        }

        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if (stone == null)
        {
            resultText.text = "Aucune gemme sur la table.";
            return;
        }

        resultText.text = observed
            ? stone.Definition.magnetism
            : "Observez la gemme quelques secondes : si elle réagit, elle se rapproche de l'aimant.";
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("MagnetPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Aimant", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildScene();
        BuildSidePanel();
    }

    private void BuildScene()
    {
        // Bright background like a product photo, a table, and a silver cylinder magnet standing on it.
        var area = RuntimeUIKit.MakeBox("Area", panel.transform, new Vector2(AreaX, AreaY), new Vector2(AreaWidth, AreaHeight), new Color(0.92f, 0.93f, 0.95f, 1f));
        areaRect = area.rectTransform;
        area.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;

        var table = RuntimeUIKit.MakeBox("Table", panel.transform, new Vector2(AreaX, TableTop), new Vector2(AreaWidth, 44f), new Color(0.74f, 0.70f, 0.64f, 1f), false);
        table.raycastTarget = false;
        RuntimeUIKit.MakeBox("TableEdge", panel.transform, new Vector2(AreaX, TableTop), new Vector2(AreaWidth, 4f), new Color(0.88f, 0.85f, 0.80f, 1f), false).raycastTarget = false;

        // Magnet: body, bright highlight band, shadow band and the elliptical end cap facing the stone.
        var magnetLeft = AreaX + MagnetFace;
        RuntimeUIKit.MakeBox("MagnetBody", panel.transform, new Vector2(magnetLeft, TableTop + 96f), new Vector2(82f, 96f), new Color(0.78f, 0.80f, 0.85f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("MagnetLight", panel.transform, new Vector2(magnetLeft + 10f, TableTop + 92f), new Vector2(18f, 88f), new Color(0.97f, 0.98f, 1f, 0.85f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("MagnetShade", panel.transform, new Vector2(magnetLeft + 56f, TableTop + 92f), new Vector2(20f, 88f), new Color(0.52f, 0.54f, 0.60f, 0.8f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("MagnetCap", panel.transform, new Vector2(magnetLeft, TableTop + 96f), new Vector2(82f, 14f), new Color(0.92f, 0.94f, 0.97f, 1f)).raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        gemRect = gem.GetComponent<RectTransform>();
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.35f, 0.38f, 0.45f),
            new Vector2(AreaX + 10f, TableTop - 14f), new Vector2(AreaWidth - 120f, 18f));
        distanceText = RuntimeUIKit.MakeText("Distance", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.25f, 0.28f, 0.33f),
            new Vector2(AreaX + 10f, AreaY - 8f), new Vector2(200f, 16f));
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("Header", panel.transform, "Observation", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(484f, -44f), new Vector2(150f, 20f));
        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(480f, -68f), new Vector2(150f, 110f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(488f, -74f), new Vector2(134f, 98f));

        strengthText = RuntimeUIKit.MakeText("Magnet", panel.transform, "Aimant au néodyme (très puissant)", 11, TextAnchor.UpperLeft,
            new Color(0.6f, 0.62f, 0.68f), new Vector2(480f, -150f), new Vector2(150f, 30f));

        var reset = RuntimeUIKit.MakeButton("Reset", panel.transform, "Replacer la gemme", 13, ButtonColor,
            new Vector2(484f, -196f), new Vector2(146f, 28f));
        reset.onClick.AddListener(ResetPosition);
        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor,
            new Vector2(484f, -230f), new Vector2(146f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonColor,
            new Vector2(484f, -264f), new Vector2(146f, 28f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Tip", panel.transform, "Déposez la gemme à l'endroit voulu : plus elle est près de l'aimant, plus elle est attirée.", 11,
            TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f), new Vector2(AreaX, AreaY - AreaHeight - 8f), new Vector2(AreaWidth, 30f));
    }
}
