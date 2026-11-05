using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Hydrostatic balance: drop a gem in the basket hanging in the beaker to read its weight in water.
// The density (air weight / (air weight - water weight)) only shows up once the stone has also been weighed in
// air on the carat balance.
public class HydrostaticBalanceUI : MonoBehaviour
{
    public static HydrostaticBalanceUI Instance { get; private set; }

    private const float PanelWidth = 524f;
    private const float PanelHeight = 480f;
    private const string WaterObservationKey = "WaterWeight";

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color LcdColor = new Color(0.74f, 0.82f, 0.30f, 1f);
    private static readonly Color Ink = new Color(0.12f, 0.18f, 0.05f, 1f);
    private static readonly Color Ghost = new Color(0.12f, 0.18f, 0.05f, 0.07f);

    private GameObject panel;
    private Image gemImage;
    private Text hintText;
    private Text airText;
    private Text waterText;
    private Text densityText;
    private Text noteText;
    private RectTransform basketRoot;
    private SevenSegmentDisplay display;

    private GemInstance stone;
    private Coroutine settleRoutine;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<HydrostaticBalanceUI>();
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

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        StopSettling();
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Physics ---------------------------------------------------------------------------------------------

    // Apparent weight in water: W_air * (1 - 1 / density).
    private static float WeightInWater(GemInstance gem)
    {
        var density = Mathf.Max(gem.Definition.density, 1.01f);
        return gem.CaratWeight * (1f - 1f / density);
    }

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

    // Density computed from the two weighings, with the small repeatable error of a real measurement.
    private static float MeasuredDensity(GemInstance gem)
    {
        var air = gem.CaratWeight;
        var water = WeightInWater(gem);
        var exact = air / Mathf.Max(air - water, 0.0001f);
        var noise = ((StableHash(gem.SpecimenId ?? "") & 0xFFFF) / 65535f - 0.5f) * 0.008f;
        return exact * (1f + noise);
    }

    private bool AirWeightKnown()
    {
        return stone != null && stone.HasObserved(ExaminationTool.MeasurementType.Weight.ToString());
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        if (gem != null && gem.IsSet)
        {
            ToolFeedbackUI.Instance?.ShowMessage(JewelCatalog.SetRefusal);
            return;
        }
        stone = gem;
        Refresh();
        StopSettling();
        settleRoutine = StartCoroutine(Settle());
    }

    private void ClearStone()
    {
        StopSettling();
        stone = null;
        Refresh();
    }

    private void StopSettling()
    {
        if (settleRoutine != null) StopCoroutine(settleRoutine);
        settleRoutine = null;
    }

    private IEnumerator Settle()
    {
        var target = WeightInWater(stone);
        const float duration = 1.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            var remaining = 1f - elapsed / duration;
            ShowWater(Mathf.Max(0f, target + Random.Range(-1f, 1f) * Mathf.Max(0.4f, target * 0.3f) * remaining));
            yield return new WaitForSeconds(0.07f);
            elapsed += 0.07f;
        }

        ShowWater(target);
        stone.RecordObservation(WaterObservationKey);
        settleRoutine = null;
        UpdateReadouts();

        if (AirWeightKnown())
        {
            var density = MeasuredDensity(stone);
            stone.RecordMeasurement(ExaminationTool.MeasurementType.Density.ToString(), density);
            ToolFeedbackUI.Instance?.ShowMessage($"Balance hydrostatique — Densité ≈ {density:F2}");
        }
        else
        {
            ToolFeedbackUI.Instance?.ShowMessage(
                $"Balance hydrostatique — Poids dans l'eau : {target:F3} ct. Pesez aussi la gemme dans l'air pour obtenir la densité.");
        }
    }

    private void ShowWater(float carats)
    {
        display.ShowNumber(Mathf.RoundToInt(carats * 1000f), 1);
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            StopSettling();
        }

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = "Déposez une gemme dans le panier";
            ShowWater(0f);
        }
        else
        {
            gemImage.enabled = true;
            gemImage.sprite = stone.DisplaySprite;
            hintText.text = "";
            if (settleRoutine == null) ShowWater(stone.HasObserved(WaterObservationKey) ? WeightInWater(stone) : 0f);
        }

        UpdateReadouts();
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

    private void UpdateReadouts()
    {
        const string none = "—";

        if (stone == null)
        {
            airText.text = none;
            waterText.text = none;
            densityText.text = none;
            noteText.text = "Pesez d'abord la gemme dans l'air avec la balance à carats, puis plongez-la ici.";
            return;
        }

        var airKnown = AirWeightKnown();
        var waterKnown = stone.HasObserved(WaterObservationKey) && settleRoutine == null;

        airText.text = airKnown ? $"{stone.CaratWeight:F2} ct" : "non pesée";
        waterText.text = waterKnown ? $"{WeightInWater(stone):F3} ct" : none;

        if (airKnown && waterKnown)
        {
            densityText.text = $"{MeasuredDensity(stone):F2}";
            noteText.text = "Densité = P air / (P air − P eau)";
        }
        else
        {
            densityText.text = none;
            noteText.text = airKnown
                ? "Plongez la gemme dans le panier pour mesurer son poids dans l'eau."
                : "Sans le poids dans l'air, seul le poids dans l'eau est affiché. Pesez la gemme sur la balance à carats.";
        }
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("HydrostaticPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-140f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Balance hydrostatique", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(340f, 28f));

        BuildCabinet();
        BuildBase();
        BuildSidePanel();
    }

    private void BuildCabinet()
    {
        // Glass draft shield around the beaker, with its top rail.
        RuntimeUIKit.MakeBox("Cabinet", panel.transform, new Vector2(20f, -44f), new Vector2(300f, 300f), new Color(0.80f, 0.82f, 0.86f, 1f));
        var glass = RuntimeUIKit.MakeBox("Glass", panel.transform, new Vector2(30f, -60f), new Vector2(280f, 274f), new Color(0.90f, 0.94f, 0.97f, 1f));
        glass.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        glass.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        // Beaker: outline, empty glass, water.
        var outline = RuntimeUIKit.MakeBox("BeakerOutline", panel.transform, new Vector2(102f, -112f), new Vector2(136f, 168f), new Color(0.58f, 0.66f, 0.73f, 1f));
        outline.raycastTarget = false;
        var inner = RuntimeUIKit.MakeBox("BeakerInner", panel.transform, new Vector2(106f, -112f), new Vector2(128f, 160f), new Color(0.88f, 0.93f, 0.97f, 1f));
        inner.raycastTarget = false;
        var water = RuntimeUIKit.MakeBox("Water", panel.transform, new Vector2(106f, -164f), new Vector2(128f, 108f), new Color(0.50f, 0.72f, 0.88f, 0.8f), false);
        water.raycastTarget = false;

        // Wire from the balance down to the basket.
        var wire = RuntimeUIKit.MakeBox("Wire", panel.transform, new Vector2(169f, -62f), new Vector2(2f, 134f), new Color(0.35f, 0.36f, 0.40f, 1f), false);
        wire.raycastTarget = false;
        var rail = RuntimeUIKit.MakeBox("Rail", panel.transform, new Vector2(150f, -60f), new Vector2(40f, 6f), new Color(0.55f, 0.57f, 0.62f, 1f));
        rail.raycastTarget = false;

        var basketGO = new GameObject("Basket", typeof(RectTransform));
        basketGO.transform.SetParent(panel.transform, false);
        basketRoot = basketGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(basketRoot, Vector2.zero, new Vector2(1f, 1f));

        var bowl = RuntimeUIKit.MakeBox("Bowl", panel.transform, new Vector2(142f, -194f), new Vector2(56f, 16f), new Color(0.72f, 0.74f, 0.80f, 1f));
        bowl.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(155f, -170f), new Vector2(30f, 30f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.25f, 0.30f, 0.38f),
            new Vector2(30f, -308f), new Vector2(280f, 18f));
    }

    private void BuildBase()
    {
        RuntimeUIKit.MakeBox("Base", panel.transform, new Vector2(12f, -344f), new Vector2(316f, 120f), new Color(0.93f, 0.93f, 0.95f, 1f));
        RuntimeUIKit.MakeBox("LcdFrame", panel.transform, new Vector2(30f, -360f), new Vector2(240f, 60f), new Color(0.30f, 0.32f, 0.30f, 1f));
        RuntimeUIKit.MakeBox("Lcd", panel.transform, new Vector2(34f, -364f), new Vector2(232f, 52f), LcdColor, false);

        display = new SevenSegmentDisplay(panel.transform, new Vector2(48f, -371f), 5, 26f, 38f, 9f, 4f, Ink, Ghost);
        display.ShowBlank();

        var unit = RuntimeUIKit.MakeText("Unit", panel.transform, "ct", 14, TextAnchor.UpperRight, Ink, new Vector2(234f, -368f), new Vector2(30f, 18f));
        unit.fontStyle = FontStyle.Bold;

        // Decorative keys, like the real instrument.
        RuntimeUIKit.MakeBox("KeyA", panel.transform, new Vector2(30f, -432f), new Vector2(120f, 14f), new Color(0.80f, 0.80f, 0.84f, 1f));
        RuntimeUIKit.MakeBox("KeyB", panel.transform, new Vector2(164f, -432f), new Vector2(50f, 14f), new Color(0.80f, 0.80f, 0.84f, 1f));
        RuntimeUIKit.MakeBox("KeyC", panel.transform, new Vector2(224f, -432f), new Vector2(50f, 14f), new Color(0.80f, 0.80f, 0.84f, 1f));
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("Header", panel.transform, "Mesures", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(344f, -44f), new Vector2(160f, 20f));
        RuntimeUIKit.MakeBox("Box", panel.transform, new Vector2(340f, -68f), new Vector2(172f, 216f), new Color(0.04f, 0.05f, 0.07f, 1f));

        var muted = new Color(0.60f, 0.62f, 0.68f);
        RuntimeUIKit.MakeText("AirLabel", panel.transform, "Poids dans l'air", 11, TextAnchor.UpperLeft, muted, new Vector2(352f, -76f), new Vector2(150f, 16f));
        airText = RuntimeUIKit.MakeText("Air", panel.transform, "", 16, TextAnchor.UpperLeft, Color.white, new Vector2(352f, -92f), new Vector2(150f, 24f));
        RuntimeUIKit.MakeText("WaterLabel", panel.transform, "Poids dans l'eau", 11, TextAnchor.UpperLeft, muted, new Vector2(352f, -122f), new Vector2(150f, 16f));
        waterText = RuntimeUIKit.MakeText("Water", panel.transform, "", 16, TextAnchor.UpperLeft, Color.white, new Vector2(352f, -138f), new Vector2(150f, 24f));
        RuntimeUIKit.MakeText("DensityLabel", panel.transform, "Densité", 11, TextAnchor.UpperLeft, muted, new Vector2(352f, -172f), new Vector2(150f, 16f));
        densityText = RuntimeUIKit.MakeText("Density", panel.transform, "", 24, TextAnchor.UpperLeft, new Color(0.55f, 0.95f, 0.65f), new Vector2(352f, -188f), new Vector2(150f, 32f));
        densityText.fontStyle = FontStyle.Bold;

        noteText = RuntimeUIKit.MakeText("Note", panel.transform, "", 11, TextAnchor.UpperLeft, muted, new Vector2(340f, -226f), new Vector2(172f, 70f));

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(344f, -372f), new Vector2(160f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(344f, -410f), new Vector2(160f, 28f));
        close.onClick.AddListener(Close);
    }
}
