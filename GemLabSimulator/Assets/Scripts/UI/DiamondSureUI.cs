using UnityEngine;
using UnityEngine.UI;

// DiamondSure of the laboratory: drop a stone on the holder, press Start. In three seconds the unit measures how much of the
// 225 nm light goes through and lights Pass (type I diamond), Refer (further testing needed) or Error (not a diamond).
public class DiamondSureUI : MonoBehaviour
{
    public static DiamondSureUI Instance { get; private set; }

    private const float PanelWidth = 620f;
    private const float PanelHeight = 380f;
    private const float ScanSeconds = 3f;
    private const float BarWidth = 190f;

    private static readonly Color WindowColor = new Color(0.12f, 0.13f, 0.16f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color Ink = new Color(0.12f, 0.13f, 0.17f, 1f);
    private static readonly Color LedOff = new Color(0.22f, 0.23f, 0.26f, 1f);
    private static readonly Color PassColor = new Color(0.2f, 0.95f, 0.4f, 1f);
    private static readonly Color ReferColor = new Color(1f, 0.7f, 0.15f, 1f);
    private static readonly Color ErrorColor = new Color(1f, 0.25f, 0.2f, 1f);

    private GameObject panel;
    private Text lcdText;
    private Text percentText;
    private Text resultText;
    private RectTransform barFill;
    private Image barFillImage;
    private Image uvGlow;
    private Image gemImage;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;
    private Image[] leds;

    private GemInstance stone;
    private bool scanning;
    private bool done;
    private float scanTime;
    private int finalPercent;
    private DiamondSureVerdict verdict;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<DiamondSureUI>();
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

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
        stone = null;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        ResetDisplay();
        Refresh();
    }

    public void Close()
    {
        scanning = false;
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem, Vector2 screenPosition)
    {
        stone = gem;
        ResetDisplay();
        Refresh();
    }

    private void RemoveStone()
    {
        stone = null;
        ResetDisplay();
        Refresh();
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("DiamondSure : posez d'abord une pierre sur le support.");
            return;
        }

        verdict = DiamondSure.VerdictOf(stone);
        finalPercent = DiamondSure.TransmissionPercent(stone);

        scanning = true;
        done = false;
        scanTime = 0f;
        SetLeds(-1);
        lcdText.text = "ANALYSE";
        lcdText.color = new Color(0.4f, 1f, 0.55f);
        resultText.text = "Mesure de la transmission à 225 nm...";
        SetBar(0);
        SetStartBusy(true);
    }

    private void ResetDisplay()
    {
        scanning = false;
        done = false;
        SetLeds(-1);
        lcdText.text = "PRÊT";
        lcdText.color = new Color(0.4f, 1f, 0.55f);
        SetBar(0);
        percentText.text = "";
        resultText.text = stone == null
            ? "Aucune pierre sur le support."
            : "Appuyez sur Start : l'appareil mesure la transmission de l'UV court (225 nm) et classe la pierre.";
        SetStartBusy(false);
    }

    private void FinishScan()
    {
        scanning = false;
        done = true;
        SetStartBusy(false);
        SetBar(finalPercent);

        switch (verdict)
        {
            case DiamondSureVerdict.Pass:
                lcdText.text = "PASS";
                lcdText.color = PassColor;
                SetLeds(0);
                resultText.text = "Pass : l'UV est fortement absorbé. Cohérent avec un diamant naturel de type I courant.";
                break;
            case DiamondSureVerdict.Refer:
                lcdText.text = "REFER";
                lcdText.color = ReferColor;
                SetLeds(1);
                resultText.text = "Refer : l'UV passe largement. Diamant de type II, naturel rare ou synthétique : examen complémentaire nécessaire (DiamondView, Raman, FTIR).";
                break;
            default:
                lcdText.text = "ERREUR";
                lcdText.color = ErrorColor;
                SetLeds(2);
                resultText.text = "Erreur : la pierre est opaque à l'UV court, ce n'est pas un diamant.";
                break;
        }

        if (verdict == DiamondSureVerdict.NotDiamond)
        {
            ToolFeedbackUI.Instance?.ShowMessage("DiamondSure — Erreur : pas un diamant.");
            return;
        }

        stone.RecordObservation(ExaminationTool.MeasurementType.DiamondScreening.ToString());
        ToolFeedbackUI.Instance?.ShowMessage($"DiamondSure — {DiamondSure.Label(stone)}");
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.45f + 0.15f * Mathf.Sin(Time.time * 14f) : 0f;
        uvGlow.color = new Color(0.55f, 0.35f, 1f, glow);

        if (!scanning) return;

        scanTime += Time.deltaTime;
        var progress = Mathf.Clamp01(scanTime / ScanSeconds);

        // The reading creeps toward its final value with a little flutter, and the amber light blinks while measuring.
        var target = finalPercent * Mathf.SmoothStep(0f, 1f, progress);
        var flutter = (1f - progress) * 6f * Mathf.Sin(Time.time * 23f);
        SetBar(Mathf.Clamp(target + flutter, 0f, 100f));
        SetLeds(Mathf.FloorToInt(Time.time * 6f) % 2 == 0 ? 1 : -1);
        lcdText.text = "ANALYSE" + new string('.', 1 + Mathf.FloorToInt(Time.time * 3f) % 3);

        if (progress >= 1f) FinishScan();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            ResetDisplay();
        }

        gemImage.enabled = stone != null;
        if (stone != null) gemImage.sprite = stone.DisplaySprite;
        slotHint.gameObject.SetActive(stone == null);
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

    private void SetStartBusy(bool busy)
    {
        startImage.color = busy ? StartBusyColor : StartColor;
        startLabel.text = busy ? "..." : "Start";
    }

    // 0 = Pass, 1 = Refer, 2 = Error, -1 = all off.
    private void SetLeds(int lit)
    {
        var colors = new[] { PassColor, ReferColor, ErrorColor };
        for (var i = 0; i < leds.Length; i++) leds[i].color = i == lit ? colors[i] : LedOff;
    }

    private void SetBar(float percent)
    {
        var width = BarWidth * Mathf.Clamp01(percent / 100f);
        RuntimeUIKit.SetTopLeft(barFill, new Vector2(200f, -194f), new Vector2(Mathf.Max(width, 0.01f), 16f));
        barFillImage.color = percent > 40f ? new Color(0.5f, 0.55f, 1f, 1f) : new Color(0.25f, 0.75f, 0.9f, 1f);
        percentText.text = percent > 0.5f || done ? $"{Mathf.RoundToInt(percent)} %" : "";
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("DiamondSurePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-190f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), WindowColor, false);
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.05f, 0.26f, 0.30f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "DiamondSure — Dépistage des diamants", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(14f, 0f), new Vector2(420f, 26f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        BuildUnit();
        BuildControls();
    }

    private void BuildUnit()
    {
        // Pale unit with a stone holder on the left and the display and lights on the right.
        var body = RuntimeUIKit.MakeBox("Unit", panel.transform, new Vector2(16f, -40f), new Vector2(400f, 250f), new Color(0.88f, 0.89f, 0.91f, 1f));
        body.raycastTarget = false;
        RuntimeUIKit.MakeBox("UnitBase", panel.transform, new Vector2(16f, -246f), new Vector2(400f, 44f), new Color(0.74f, 0.76f, 0.80f, 1f)).raycastTarget = false;

        var holder = RuntimeUIKit.MakeBox("Holder", panel.transform, new Vector2(40f, -64f), new Vector2(140f, 150f), new Color(0.58f, 0.60f, 0.64f, 1f));
        holder.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;
        var well = RuntimeUIKit.MakeBox("HolderWell", panel.transform, new Vector2(54f, -78f), new Vector2(112f, 122f), new Color(0.16f, 0.17f, 0.21f, 1f));
        well.raycastTarget = false;
        uvGlow = RuntimeUIKit.MakeBox("UvGlow", panel.transform, new Vector2(54f, -78f), new Vector2(112f, 122f), new Color(0.55f, 0.35f, 1f, 0f));
        uvGlow.raycastTarget = false;
        RuntimeUIKit.MakeBox("Pedestal", panel.transform, new Vector2(84f, -146f), new Vector2(52f, 14f), new Color(0.30f, 0.31f, 0.36f, 1f), false).raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(90f, -112f), new Vector2(40f, 40f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;
        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une pierre", 11, TextAnchor.MiddleCenter, new Color(0.7f, 0.74f, 0.85f),
            new Vector2(54f, -78f), new Vector2(112f, 122f));

        RuntimeUIKit.MakeText("Logo", panel.transform, "DiamondSure", 20, TextAnchor.MiddleLeft, Ink, new Vector2(200f, -52f), new Vector2(200f, 26f));
        RuntimeUIKit.MakeText("Sub", panel.transform, "Dépistage par transmission UV (225 nm)", 10, TextAnchor.MiddleLeft, new Color(0.35f, 0.38f, 0.45f),
            new Vector2(200f, -76f), new Vector2(210f, 14f));

        // Display.
        RuntimeUIKit.MakeBox("Lcd", panel.transform, new Vector2(200f, -96f), new Vector2(190f, 70f), new Color(0.04f, 0.07f, 0.05f, 1f)).raycastTarget = false;
        lcdText = RuntimeUIKit.MakeText("LcdText", panel.transform, "PRÊT", 24, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.55f),
            new Vector2(200f, -96f), new Vector2(190f, 70f));

        // Transmission gauge.
        RuntimeUIKit.MakeText("BarLabel", panel.transform, "Transmission UV 225 nm", 11, TextAnchor.MiddleLeft, Ink, new Vector2(200f, -176f), new Vector2(190f, 14f));
        RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(200f, -194f), new Vector2(BarWidth, 16f), new Color(0.16f, 0.17f, 0.22f, 1f), false).raycastTarget = false;
        barFillImage = RuntimeUIKit.MakeBox("BarFill", panel.transform, new Vector2(200f, -194f), new Vector2(1f, 16f), new Color(0.25f, 0.75f, 0.9f, 1f), false);
        barFillImage.raycastTarget = false;
        barFill = barFillImage.rectTransform;
        percentText = RuntimeUIKit.MakeText("Percent", panel.transform, "", 11, TextAnchor.MiddleCenter, Color.white, new Vector2(200f, -194f), new Vector2(BarWidth, 16f));

        // Lights.
        var names = new[] { "Pass", "Refer", "Erreur" };
        leds = new Image[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var x = 200f + i * 66f;
            leds[i] = RuntimeUIKit.MakeBox("Led" + i, panel.transform, new Vector2(x, -222f), new Vector2(16f, 16f), LedOff);
            leds[i].raycastTarget = false;
            RuntimeUIKit.MakeText("LedLabel" + i, panel.transform, names[i], 11, TextAnchor.MiddleLeft, Ink, new Vector2(x + 20f, -222f), new Vector2(44f, 16f));
        }
    }

    private void BuildControls()
    {
        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(436f, -52f), new Vector2(168f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la pierre", 13, ButtonColor, new Vector2(436f, -106f), new Vector2(168f, 28f));
        remove.onClick.AddListener(RemoveStone);

        RuntimeUIKit.MakeText("Legend", panel.transform,
            "Pass : diamant naturel de type I courant.\nRefer : examen complémentaire (diamant de type II, naturel rare ou synthétique).\nErreur : pas un diamant.",
            11, TextAnchor.UpperLeft, new Color(0.65f, 0.69f, 0.76f), new Vector2(436f, -146f), new Vector2(170f, 130f));

        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(16f, -298f), new Vector2(588f, 66f), new Color(0.05f, 0.06f, 0.08f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(26f, -304f), new Vector2(568f, 54f));
    }
}
