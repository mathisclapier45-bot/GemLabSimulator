using UnityEngine;
using UnityEngine.UI;

// FTIR spectrometer of the laboratory: drop a gem in the sample compartment, press Start, and the infrared absorption curve
// (4000 to 1000 cm-1, wavenumber decreasing to the right) is drawn in three seconds in an OMNIC-like window. Once the curve is
// complete, hover it to read a peak and use the mouse wheel to zoom (needed to tell the 3595 and 3585 peaks of quartz apart).
public class FtirUI : MonoBehaviour
{
    public static FtirUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 660f;
    private const int PlotW = 620;
    private const int PlotH = 270;
    private const float PlotX = 64f;
    private const float PlotY = -96f;
    private const float ScanSeconds = 3f;
    private const int MaxXLabels = 14;

    private static readonly Color WindowColor = new Color(0.86f, 0.87f, 0.88f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color Ink = new Color(0.1f, 0.1f, 0.12f, 1f);

    private GameObject panel;
    private RawImage plotImage;
    private RectTransform plotRect;
    private Text[] yLabels;
    private Text[] xLabels;
    private Text cursorText;
    private Text statusText;
    private Text peaksText;
    private Text analysisText;
    private Text legendText;
    private Image gemImage;
    private Image lampGlow;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;

    private Texture2D plotTexture;
    private Color32[] basePixels;
    private Color32[] livePixels;

    private GemInstance stone;
    private FtirResult result;
    private float viewHi = FtirResult.MaxCm;
    private float viewLo = FtirResult.MinCm;
    private float yMax = 1f;
    private float yStep = 0.5f;
    private bool scanning;
    private bool hasCurve;
    private float scanTime;
    private int hoverX = -1;
    private float headFraction;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<FtirUI>();
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
        ClearCurve();
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
        ClearCurve();
        Refresh();
    }

    private void RemoveStone()
    {
        stone = null;
        ClearCurve();
        Refresh();
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("FTIR : glissez d'abord une gemme dans le compartiment.");
            return;
        }

        result = FtirSpectrum.Measure(stone);
        viewHi = FtirResult.MaxCm;
        viewLo = FtirResult.MinCm;
        RebuildView();

        scanning = true;
        hasCurve = false;
        scanTime = 0f;
        headFraction = 0f;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        legendText.text = $"Spectre {stone.SpecimenId}";
        statusText.text = "Collecte...";
        peaksText.text = "Collecte en cours...";
        analysisText.text = "";
        SetStartBusy(true);
        Render();
    }

    private void ClearCurve()
    {
        scanning = false;
        hasCurve = false;
        result = null;
        hoverX = -1;
        viewHi = FtirResult.MaxCm;
        viewLo = FtirResult.MinCm;
        yMax = 1f;
        yStep = 0.5f;
        RebuildView();
        cursorText.gameObject.SetActive(false);
        legendText.text = "";
        statusText.text = "Prêt";
        peaksText.text = stone == null ? "Aucune gemme dans le compartiment." : "Appuyez sur Start pour lancer la collecte.";
        analysisText.text = "";
        SetStartBusy(false);
        Render();
    }

    private void FinishScan()
    {
        scanning = false;
        hasCurve = true;
        statusText.text = "Terminé";
        SetStartBusy(false);
        headFraction = 1f;
        Render();

        var lines = FtirSpectrum.PeakLines(result);
        var shown = Mathf.Min(lines.Count, 6);
        var text = string.Join("\n", lines.GetRange(0, shown));
        if (lines.Count > shown) text += "\n...";
        peaksText.text = text;

        var analysis = result.Analysis;
        var count = Mathf.Min(analysis.Count, 4);
        analysisText.text = string.Join("\n", analysis.GetRange(0, count));

        stone.RecordObservation(LabFindings.FtirKey);
        if (result.TreatmentConclusive)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"FTIR — Traitement détecté : {stone.TreatmentGrade} — {stone.TreatmentDescription}");
        }
        else
        {
            ToolFeedbackUI.Instance?.ShowMessage("FTIR — Aucun comblement détecté ; chauffage et irradiation ne se voient pas en infrarouge sur cette pierre.");
        }
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.4f + 0.15f * Mathf.Sin(Time.time * 12f) : 0f;
        lampGlow.color = new Color(1f, 0.45f, 0.3f, glow);

        if (scanning)
        {
            scanTime += Time.deltaTime;
            headFraction = Mathf.Clamp01(scanTime / ScanSeconds);
            Render();
            if (headFraction >= 1f) FinishScan();
            return;
        }

        if (hasCurve)
        {
            HandleZoom();
            HandleHover();
        }
    }

    private bool PointerInPlot(out Vector2 local)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(plotRect, Input.mousePosition, null, out local);
        return local.x >= 0f && local.x <= PlotW - 1 && local.y <= 0f && local.y >= -(PlotH - 1);
    }

    private void HandleZoom()
    {
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f) || !PointerInPlot(out var local)) return;

        var span = viewHi - viewLo;
        var under = viewHi - local.x / (PlotW - 1) * span;
        var factor = scroll > 0f ? 0.7f : 1.4f;
        var newSpan = Mathf.Clamp(span * factor, 16f, FtirResult.MaxCm - FtirResult.MinCm);

        // The wavenumber under the pointer stays under the pointer.
        var t = local.x / (PlotW - 1);
        var hi = under + t * newSpan;
        var lo = hi - newSpan;
        if (hi > FtirResult.MaxCm) { lo -= hi - FtirResult.MaxCm; hi = FtirResult.MaxCm; }
        if (lo < FtirResult.MinCm) { hi += FtirResult.MinCm - lo; lo = FtirResult.MinCm; }
        viewHi = Mathf.Min(hi, FtirResult.MaxCm);
        viewLo = lo;

        RebuildView();
        hoverX = -1;
        Render();
    }

    private void ResetZoom()
    {
        if (!hasCurve) return;
        viewHi = FtirResult.MaxCm;
        viewLo = FtirResult.MinCm;
        RebuildView();
        hoverX = -1;
        Render();
    }

    private void HandleHover()
    {
        var inside = PointerInPlot(out var local);
        var px = inside ? Mathf.RoundToInt(local.x) : -1;
        if (px == hoverX) return;
        hoverX = px;
        Render();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            ClearCurve();
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

    // ---- Plot ------------------------------------------------------------------------------------------------

    private float Span => viewHi - viewLo;

    private int XOf(float wavenumber)
    {
        return Mathf.RoundToInt((viewHi - wavenumber) / Span * (PlotW - 1));
    }

    private int YOf(float absorbance)
    {
        return Mathf.RoundToInt(absorbance / yMax * (PlotH - 1));
    }

    private static float NiceStep(float value)
    {
        var p = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(value, 1e-4f))));
        var n = value / p;
        return (n <= 1f ? 1f : n <= 2f ? 2f : n <= 5f ? 5f : 10f) * p;
    }

    // New view (or new stone): vertical scale, axis labels and the grid behind the curve.
    private void RebuildView()
    {
        if (result != null)
        {
            var raw = Mathf.Max(0.3f, FtirSpectrum.InformativeMax(result, viewLo, viewHi) * 1.2f);
            yStep = NiceStep(raw / 5f);
            yMax = Mathf.Min(Mathf.Ceil(raw / yStep) * yStep, 6f);
        }
        else
        {
            yMax = 1f;
            yStep = 0.2f;
        }

        BuildBase();
        LayoutAxisLabels();
    }

    private void BuildBase()
    {
        var white = new Color32(255, 255, 255, 255);
        for (var i = 0; i < basePixels.Length; i++) basePixels[i] = white;

        var grid = new Color32(226, 226, 230, 255);
        for (var v = yStep; v <= yMax + 0.0001f; v += yStep)
        {
            var y = YOf(v);
            for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, grid);
        }

        var xStep = NiceStep(Span / 6f);
        for (var nu = Mathf.Ceil(viewLo / xStep) * xStep; nu <= viewHi + 0.001f; nu += xStep)
        {
            var x = XOf(nu);
            for (var y = 0; y < PlotH; y++) SetPixel(basePixels, x, y, grid);
        }

        var black = new Color32(0, 0, 0, 255);
        for (var x = 0; x < PlotW; x++)
        {
            SetPixel(basePixels, x, 0, black);
            SetPixel(basePixels, x, PlotH - 1, black);
        }
        for (var y = 0; y < PlotH; y++)
        {
            SetPixel(basePixels, 0, y, black);
            SetPixel(basePixels, PlotW - 1, y, black);
        }
    }

    // Draws the curve (up to the scan head while collecting) and the cursor, and pushes the texture.
    private void Render()
    {
        System.Array.Copy(basePixels, livePixels, basePixels.Length);

        var headX = scanning ? Mathf.RoundToInt(headFraction * (PlotW - 1)) : PlotW;
        if (result != null) DrawCurve(headX);

        if (scanning) DrawCursorLine(Mathf.Clamp(headX, 0, PlotW - 1));
        else if (hasCurve && hoverX >= 0) DrawCursorLine(hoverX);

        UpdateCursorText();
        plotTexture.SetPixels32(livePixels);
        plotTexture.Apply(false);
    }

    private void DrawCurve(int headX)
    {
        var first = Mathf.Max(0, Mathf.FloorToInt(FtirResult.MaxCm - viewHi) - 1);
        var last = Mathf.Min(result.Absorbance.Length - 1, Mathf.CeilToInt(FtirResult.MaxCm - viewLo) + 1);
        var ink = new Color32(176, 24, 24, 255);

        var prevX = 0;
        var prevY = 0;
        var havePrev = false;
        for (var i = first; i <= last; i++)
        {
            var nu = FtirResult.MaxCm - i;
            var x = XOf(nu);
            var y = Mathf.Clamp(YOf(result.Absorbance[i]), 0, PlotH - 2);
            if (x > headX) break;

            if (havePrev)
            {
                if (x != prevX || y != prevY) DrawSegment(prevX, prevY, x, y, ink);
            }
            prevX = x;
            prevY = y;
            havePrev = true;
        }
    }

    private void DrawSegment(int x0, int y0, int x1, int y1, Color32 color)
    {
        var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (var s = 0; s <= steps; s++)
        {
            var t = s / (float)steps;
            var x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            var y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            SetPixel(livePixels, x, y, color);
            SetPixel(livePixels, x, y + 1, color);
        }
    }

    private void DrawCursorLine(int x)
    {
        var teal = new Color32(0, 120, 120, 255);
        for (var y = 1; y < PlotH - 1; y++) SetPixel(livePixels, x, y, teal);
    }

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= PlotW || y >= PlotH) return;
        pixels[y * PlotW + x] = color;
    }

    private void UpdateCursorText()
    {
        if (!hasCurve || hoverX < 0 || scanning)
        {
            cursorText.gameObject.SetActive(false);
            return;
        }

        var wavenumber = viewHi - hoverX / (float)(PlotW - 1) * Span;
        var index = Mathf.Clamp(Mathf.RoundToInt(FtirResult.MaxCm - wavenumber), 0, result.Absorbance.Length - 1);
        cursorText.text = $"{wavenumber:F1} cm-1   A = {result.Absorbance[index]:F2}";
        var x = Mathf.Clamp(PlotX + hoverX - 70f, PlotX + 2f, PlotX + PlotW - 150f);
        RuntimeUIKit.SetTopLeft(cursorText.rectTransform, new Vector2(x, PlotY - 3f), new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(true);
    }

    private void LayoutAxisLabels()
    {
        var count = 0;
        for (var v = 0f; v <= yMax + 0.0001f && count < yLabels.Length; v += yStep, count++)
        {
            var label = yLabels[count];
            label.gameObject.SetActive(true);
            label.text = v.ToString(yStep >= 1f ? "0" : yStep >= 0.1f ? "0.0" : "0.00");
            var y = PlotY - (PlotH - 1 - YOf(v)) + 7f;
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX - 46f, y), new Vector2(40f, 14f));
        }
        for (var i = count; i < yLabels.Length; i++) yLabels[i].gameObject.SetActive(false);

        var xStep = NiceStep(Span / 6f);
        count = 0;
        for (var nu = Mathf.Ceil(viewLo / xStep) * xStep; nu <= viewHi + 0.001f && count < xLabels.Length; nu += xStep, count++)
        {
            var label = xLabels[count];
            label.gameObject.SetActive(true);
            label.text = nu.ToString(xStep >= 1f ? "0" : "0.0");
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX + XOf(nu) - 24f, PlotY - PlotH - 3f), new Vector2(48f, 14f));
        }
        for (var i = count; i < xLabels.Length; i++) xLabels[i].gameObject.SetActive(false);
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("FtirPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-170f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), WindowColor, false);

        BuildSoftwareWindow();
        BuildHardware();
        BuildResults();

        plotTexture = new Texture2D(PlotW, PlotH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        basePixels = new Color32[PlotW * PlotH];
        livePixels = new Color32[PlotW * PlotH];
        plotImage.texture = plotTexture;
    }

    private void BuildSoftwareWindow()
    {
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.05f, 0.22f, 0.45f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "OMNIC — Nicolet iS10 FTIR", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(30f, 0f), new Vector2(520f, 26f));
        RuntimeUIKit.MakeBox("Logo", panel.transform, new Vector2(8f, -6f), new Vector2(14f, 14f), new Color(0.85f, 0.15f, 0.15f, 1f), false);

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Menu", panel.transform, "File     Edit     View     Collect     Process     Analyze     Report     Window     Help", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -28f), new Vector2(560f, 20f));

        RuntimeUIKit.MakeBox("Toolbar", panel.transform, new Vector2(0f, -50f), new Vector2(PanelWidth, 28f), new Color(0.80f, 0.81f, 0.84f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Scans : 32      Résolution : 4.000      Gain : 1      Détecteur : DTGS", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -50f), new Vector2(380f, 28f));
        var reset = RuntimeUIKit.MakeButton("ResetZoom", panel.transform, "Vue complète", 12, ButtonColor,
            new Vector2(PanelWidth - 290f, -53f), new Vector2(120f, 22f));
        reset.onClick.AddListener(ResetZoom);
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.1f, 0.3f, 0.1f),
            new Vector2(PanelWidth - 160f, -50f), new Vector2(150f, 28f));

        RuntimeUIKit.MakeText("YTitle", panel.transform, "Absorbance", 11, TextAnchor.MiddleLeft, Ink,
            new Vector2(8f, -80f), new Vector2(120f, 14f));

        var plot = new GameObject("Plot", typeof(RectTransform), typeof(RawImage));
        plot.transform.SetParent(panel.transform, false);
        plotRect = plot.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(plotRect, new Vector2(PlotX, PlotY), new Vector2(PlotW, PlotH));
        plotImage = plot.GetComponent<RawImage>();
        plotImage.raycastTarget = false;

        yLabels = new Text[10];
        for (var i = 0; i < yLabels.Length; i++)
            yLabels[i] = RuntimeUIKit.MakeText("YLabel" + i, panel.transform, "", 11, TextAnchor.MiddleRight, Ink, Vector2.zero, new Vector2(40f, 14f));
        xLabels = new Text[MaxXLabels];
        for (var i = 0; i < xLabels.Length; i++)
            xLabels[i] = RuntimeUIKit.MakeText("XLabel" + i, panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(48f, 14f));

        RuntimeUIKit.MakeText("XTitle", panel.transform, "Wavenumber (cm-1)  —  molette : zoom", 11, TextAnchor.MiddleCenter, Ink,
            new Vector2(PlotX, PlotY - PlotH - 18f), new Vector2(PlotW, 14f));

        RuntimeUIKit.MakeBox("LegendLine", panel.transform, new Vector2(PlotX + 14f, PlotY - 14f), new Vector2(26f, 3f), new Color(0.69f, 0.09f, 0.09f, 1f), false);
        legendText = RuntimeUIKit.MakeText("Legend", panel.transform, "", 11, TextAnchor.MiddleLeft, Ink,
            new Vector2(PlotX + 46f, PlotY - 8f), new Vector2(200f, 14f));

        cursorText = RuntimeUIKit.MakeText("Cursor", panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(false);
    }

    private void BuildHardware()
    {
        const float top = -408f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 124f), new Color(0.12f, 0.13f, 0.16f, 1f), false);

        // Nicolet iS10: pale body, open lid over the sample compartment, blue vents on the right.
        var body = RuntimeUIKit.MakeBox("Spectrometer", panel.transform, new Vector2(60f, top - 24f), new Vector2(410f, 92f), new Color(0.88f, 0.89f, 0.90f, 1f));
        body.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;

        var lid = RuntimeUIKit.MakeBox("Lid", panel.transform, new Vector2(150f, top - 10f), new Vector2(170f, 30f), new Color(0.78f, 0.80f, 0.83f, 1f));
        lid.raycastTarget = false;
        RuntimeUIKit.MakeBox("LidSeal", panel.transform, new Vector2(158f, top - 14f), new Vector2(154f, 18f), new Color(0.10f, 0.10f, 0.12f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("LidWindow", panel.transform, new Vector2(164f, top - 17f), new Vector2(142f, 12f), new Color(0.82f, 0.84f, 0.87f, 1f), false).raycastTarget = false;

        var well = RuntimeUIKit.MakeBox("Compartment", panel.transform, new Vector2(150f, top - 38f), new Vector2(170f, 62f), new Color(0.20f, 0.21f, 0.24f, 1f));
        well.raycastTarget = false;
        var inner = RuntimeUIKit.MakeBox("CompartmentInner", panel.transform, new Vector2(158f, top - 44f), new Vector2(154f, 50f), new Color(0.32f, 0.33f, 0.37f, 1f));
        inner.raycastTarget = false;
        lampGlow = RuntimeUIKit.MakeBox("LampGlow", panel.transform, new Vector2(158f, top - 44f), new Vector2(154f, 50f), new Color(1f, 0.45f, 0.3f, 0f));
        lampGlow.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(213f, top - 47f), new Vector2(44f, 44f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une gemme", 10, TextAnchor.MiddleCenter, new Color(0.75f, 0.77f, 0.85f),
            new Vector2(158f, top - 44f), new Vector2(154f, 50f));

        // Brand, keypad and vents.
        RuntimeUIKit.MakeText("Brand", panel.transform, "Thermo", 12, TextAnchor.MiddleLeft, new Color(0.8f, 0.12f, 0.12f),
            new Vector2(72f, top - 34f), new Vector2(70f, 16f));
        RuntimeUIKit.MakeText("Model", panel.transform, "Nicolet iS10", 10, TextAnchor.MiddleLeft, new Color(0.3f, 0.32f, 0.38f),
            new Vector2(72f, top - 50f), new Vector2(80f, 14f));
        for (var i = 0; i < 4; i++)
            RuntimeUIKit.MakeBox("Key" + i, panel.transform, new Vector2(334f + i * 14f, top - 34f), new Vector2(10f, 10f), new Color(0.6f, 0.62f, 0.66f, 1f), false).raycastTarget = false;
        for (var i = 0; i < 5; i++)
            RuntimeUIKit.MakeBox("Vent" + i, panel.transform, new Vector2(400f, top - 30f - i * 11f), new Vector2(60f, 8f), new Color(0.12f, 0.42f, 0.70f, 1f), false).raycastTarget = false;

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(520f, top - 22f), new Vector2(150f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor, new Vector2(520f, top - 76f), new Vector2(150f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }

    private void BuildResults()
    {
        const float top = -540f;
        RuntimeUIKit.MakeBox("Results", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 120f), new Color(0.07f, 0.08f, 0.10f, 1f), false);

        RuntimeUIKit.MakeText("PeaksHeader", panel.transform, "Pics détectés", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 6f), new Vector2(300f, 20f));
        peaksText = RuntimeUIKit.MakeText("Peaks", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 28f), new Vector2(340f, 90f));

        RuntimeUIKit.MakeText("AnalysisHeader", panel.transform, "Analyse", 14, TextAnchor.UpperLeft, Gold, new Vector2(366f, top - 6f), new Vector2(300f, 20f));
        analysisText = RuntimeUIKit.MakeText("Analysis", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(366f, top - 28f), new Vector2(320f, 90f));
    }
}
