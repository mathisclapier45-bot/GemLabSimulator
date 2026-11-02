using UnityEngine;
using UnityEngine.UI;

// Raman spectrometer of the laboratory (GemmoRaman 532): drop a gem in the sample chamber, choose what the laser is pointed at
// (host crystal, inclusion, fracture), press Start, and the spectrum (100 to 3500 cm-1) is drawn in three seconds. The software then
// proposes the closest library spectra; click one to lay it over the measure (green). Mouse wheel to zoom, hover to read a peak.
public class RamanUI : MonoBehaviour
{
    public static RamanUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 680f;
    private const int PlotW = 620;
    private const int PlotH = 250;
    private const float PlotX = 64f;
    private const float PlotY = -96f;
    private const float ScanSeconds = 3f;
    private const int MaxXLabels = 14;
    private const int MatchButtons = 4;

    private static readonly Color WindowColor = new Color(0.20f, 0.21f, 0.24f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color SelectedColor = new Color(0.16f, 0.46f, 0.30f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color LightText = new Color(0.86f, 0.88f, 0.92f, 1f);

    private GameObject panel;
    private RawImage plotImage;
    private RectTransform plotRect;
    private Text[] yLabels;
    private Text[] xLabels;
    private Text cursorText;
    private Text statusText;
    private Text legendText;
    private Text peaksText;
    private Text analysisText;
    private Text targetText;
    private Image gemImage;
    private Image laserGlow;
    private Image ledRing;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;
    private Image[] targetImages;
    private Image[] matchImages;
    private Button[] matchButtons;
    private Text[] matchTexts;

    private Texture2D plotTexture;
    private Color32[] basePixels;
    private Color32[] livePixels;

    private GemInstance stone;
    private RamanResult result;
    private RamanTarget target = RamanTarget.Host;
    private int selectedMatch = -1;
    private float viewLo = RamanResult.MinShift;
    private float viewHi = RamanResult.MaxShift;
    private float yMax = 1000f;
    private float yStep = 200f;
    private bool scanning;
    private bool hasCurve;
    private float scanTime;
    private float headFraction;
    private int hoverX = -1;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<RamanUI>();
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

    private void SetTarget(RamanTarget newTarget)
    {
        if (scanning) return;
        target = newTarget;
        for (var i = 0; i < targetImages.Length; i++) targetImages[i].color = i == (int)target ? SelectedColor : ButtonColor;
        targetText.text = target == RamanTarget.Host ? "Laser sur le cristal hôte"
            : target == RamanTarget.Inclusion ? "Laser sur une inclusion"
            : "Laser dans une fissure";
        if (stone != null) ClearCurve();
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Raman : glissez d'abord une gemme dans la chambre.");
            return;
        }

        result = RamanSpectrum.Measure(stone, target);
        selectedMatch = -1;
        viewLo = RamanResult.MinShift;
        viewHi = RamanResult.MaxShift;
        RebuildView();
        UpdateMatchButtons(false);

        scanning = true;
        hasCurve = false;
        scanTime = 0f;
        headFraction = 0f;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        legendText.text = $"{result.Description}  ({stone.SpecimenId})";
        statusText.text = "Acquisition...";
        peaksText.text = "Acquisition en cours...";
        analysisText.text = "";
        SetStartBusy(true);
        Render();
    }

    private void ClearCurve()
    {
        scanning = false;
        hasCurve = false;
        result = null;
        selectedMatch = -1;
        hoverX = -1;
        viewLo = RamanResult.MinShift;
        viewHi = RamanResult.MaxShift;
        yMax = 1000f;
        yStep = 200f;
        RebuildView();
        UpdateMatchButtons(false);
        cursorText.gameObject.SetActive(false);
        legendText.text = "";
        statusText.text = "Prêt";
        peaksText.text = stone == null ? "Aucune gemme dans la chambre." : "Choisissez la cible puis appuyez sur Start.";
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

        UpdateMatchButtons(true);
        peaksText.text = string.Join("\n", result.PeakLines);
        analysisText.text = string.Join("\n", result.Analysis);
        Render();

        stone.RecordObservation(LabFindings.RamanKey(target));
        if (result.RecordAuthenticity)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Authenticity.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"Raman — Authenticité : {stone.AuthenticityGrade} — {stone.AuthenticityDescription}");
        }
        if (result.RecordTreatment)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"Raman — Traitement détecté : {stone.TreatmentGrade} — {stone.TreatmentDescription}");
        }
        if (!result.RecordAuthenticity && !result.RecordTreatment)
            ToolFeedbackUI.Instance?.ShowMessage($"Raman — {result.Description} : {(result.Analysis.Count > 0 ? result.Analysis[0] : "spectre enregistré.")}");
    }

    private void SelectMatch(int index)
    {
        if (!hasCurve || result == null || index >= result.Matches.Count) return;
        selectedMatch = selectedMatch == index ? -1 : index;
        UpdateMatchButtons(true);
        Render();
    }

    private void UpdateMatchButtons(bool show)
    {
        for (var i = 0; i < MatchButtons; i++)
        {
            var visible = show && result != null && i < result.Matches.Count;
            matchButtons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            matchTexts[i].text = $"{result.Matches[i].Name} — {result.Matches[i].Score} %";
            matchImages[i].color = i == selectedMatch ? SelectedColor : ButtonColor;
        }
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.35f + 0.2f * Mathf.Sin(Time.time * 16f) : 0f;
        laserGlow.color = new Color(0.2f, 1f, 0.45f, glow);
        ledRing.color = scanning ? new Color(0.3f, 1f, 0.6f, 1f) : new Color(0.1f, 0.55f, 0.32f, 1f);

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
        var t = local.x / (PlotW - 1);
        var under = viewLo + t * span;
        var newSpan = Mathf.Clamp(span * (scroll > 0f ? 0.7f : 1.4f), 30f, RamanResult.MaxShift - RamanResult.MinShift);

        var lo = under - t * newSpan;
        var hi = lo + newSpan;
        if (lo < RamanResult.MinShift) { hi += RamanResult.MinShift - lo; lo = RamanResult.MinShift; }
        if (hi > RamanResult.MaxShift) { lo -= hi - RamanResult.MaxShift; hi = RamanResult.MaxShift; }
        viewLo = Mathf.Max(lo, RamanResult.MinShift);
        viewHi = hi;

        RebuildView();
        hoverX = -1;
        Render();
    }

    private void ResetZoom()
    {
        if (!hasCurve) return;
        viewLo = RamanResult.MinShift;
        viewHi = RamanResult.MaxShift;
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

    private int XOf(float shift)
    {
        return Mathf.RoundToInt((shift - viewLo) / Span * (PlotW - 1));
    }

    private int YOf(float intensity)
    {
        return Mathf.RoundToInt(intensity / yMax * (PlotH - 1));
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
            var lo = Mathf.Max(0, Mathf.FloorToInt(viewLo - RamanResult.MinShift));
            var hi = Mathf.Min(result.Intensity.Length - 1, Mathf.CeilToInt(viewHi - RamanResult.MinShift));
            var max = 0f;
            for (var i = lo; i <= hi; i++) max = Mathf.Max(max, result.Intensity[i]);

            var raw = Mathf.Max(400f, max * 1.12f);
            yStep = NiceStep(raw / 5f);
            yMax = Mathf.Ceil(raw / yStep) * yStep;
        }
        else
        {
            yMax = 1000f;
            yStep = 200f;
        }

        BuildBase();
        LayoutAxisLabels();
    }

    private void BuildBase()
    {
        var back = new Color32(10, 11, 16, 255);
        for (var i = 0; i < basePixels.Length; i++) basePixels[i] = back;

        var grid = new Color32(34, 36, 46, 255);
        for (var v = yStep; v <= yMax + 0.01f; v += yStep)
        {
            var y = YOf(v);
            for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, grid);
        }

        var xStep = NiceStep(Span / 6f);
        for (var s = Mathf.Ceil(viewLo / xStep) * xStep; s <= viewHi + 0.001f; s += xStep)
        {
            var x = XOf(s);
            for (var y = 0; y < PlotH; y++) SetPixel(basePixels, x, y, grid);
        }

        var edge = new Color32(110, 112, 124, 255);
        for (var x = 0; x < PlotW; x++)
        {
            SetPixel(basePixels, x, 0, edge);
            SetPixel(basePixels, x, PlotH - 1, edge);
        }
        for (var y = 0; y < PlotH; y++)
        {
            SetPixel(basePixels, 0, y, edge);
            SetPixel(basePixels, PlotW - 1, y, edge);
        }
    }

    private void Render()
    {
        System.Array.Copy(basePixels, livePixels, basePixels.Length);

        var headX = scanning ? Mathf.RoundToInt(headFraction * (PlotW - 1)) : PlotW;
        if (result != null)
        {
            // Library spectrum first (green, normalized to the height of the axis), then the measure on top.
            if (selectedMatch >= 0 && selectedMatch < result.Matches.Count)
                DrawTrace(result.Matches[selectedMatch].Reference, yMax * 0.82f, new Color32(80, 205, 110, 255), headX);
            DrawTrace(result.Intensity, 1f, new Color32(238, 238, 244, 255), headX);
        }

        if (scanning) DrawCursorLine(Mathf.Clamp(headX, 0, PlotW - 1));
        else if (hasCurve && hoverX >= 0) DrawCursorLine(hoverX);

        UpdateCursorText();
        plotTexture.SetPixels32(livePixels);
        plotTexture.Apply(false);
    }

    private void DrawTrace(float[] data, float scale, Color32 color, int headX)
    {
        var first = Mathf.Max(0, Mathf.FloorToInt(viewLo - RamanResult.MinShift) - 1);
        var last = Mathf.Min(data.Length - 1, Mathf.CeilToInt(viewHi - RamanResult.MinShift) + 1);

        var prevX = 0;
        var prevY = 0;
        var havePrev = false;
        for (var i = first; i <= last; i++)
        {
            var x = XOf(RamanResult.MinShift + i);
            var y = Mathf.Clamp(YOf(data[i] * scale), 0, PlotH - 2);
            if (x > headX) break;

            if (havePrev && (x != prevX || y != prevY)) DrawSegment(prevX, prevY, x, y, color);
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
        var teal = new Color32(0, 190, 190, 255);
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

        var shift = viewLo + hoverX / (float)(PlotW - 1) * Span;
        var index = Mathf.Clamp(Mathf.RoundToInt(shift - RamanResult.MinShift), 0, result.Intensity.Length - 1);
        cursorText.text = $"{shift:F1} cm-1   I = {result.Intensity[index]:F0}";
        var x = Mathf.Clamp(PlotX + hoverX - 70f, PlotX + 2f, PlotX + PlotW - 150f);
        RuntimeUIKit.SetTopLeft(cursorText.rectTransform, new Vector2(x, PlotY - 3f), new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(true);
    }

    private void LayoutAxisLabels()
    {
        var count = 0;
        for (var v = 0f; v <= yMax + 0.01f && count < yLabels.Length; v += yStep, count++)
        {
            var label = yLabels[count];
            label.gameObject.SetActive(true);
            label.text = v.ToString("0");
            var y = PlotY - (PlotH - 1 - YOf(v)) + 7f;
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX - 52f, y), new Vector2(48f, 14f));
        }
        for (var i = count; i < yLabels.Length; i++) yLabels[i].gameObject.SetActive(false);

        var xStep = NiceStep(Span / 6f);
        count = 0;
        for (var s = Mathf.Ceil(viewLo / xStep) * xStep; s <= viewHi + 0.001f && count < xLabels.Length; s += xStep, count++)
        {
            var label = xLabels[count];
            label.gameObject.SetActive(true);
            label.text = s.ToString(xStep >= 1f ? "0" : "0.0");
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX + XOf(s) - 24f, PlotY - PlotH - 3f), new Vector2(48f, 14f));
        }
        for (var i = count; i < xLabels.Length; i++) xLabels[i].gameObject.SetActive(false);
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("RamanPanel", typeof(RectTransform));
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
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.04f, 0.28f, 0.22f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "GemmoRaman — Analyse spectrale", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(30f, 0f), new Vector2(520f, 26f));
        RuntimeUIKit.MakeBox("Logo", panel.transform, new Vector2(8f, -6f), new Vector2(14f, 14f), new Color(0.2f, 0.85f, 0.5f, 1f), false);

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Menu", panel.transform, "Fichier     Acquisition     Traitement     Bibliothèque     Aide", 12,
            TextAnchor.MiddleLeft, LightText, new Vector2(10f, -28f), new Vector2(520f, 20f));

        RuntimeUIKit.MakeBox("Toolbar", panel.transform, new Vector2(0f, -50f), new Vector2(PanelWidth, 28f), new Color(0.15f, 0.16f, 0.19f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Laser : 532 nm      Temps d'intégration : 3 s      Résolution : 6 cm-1", 12,
            TextAnchor.MiddleLeft, LightText, new Vector2(10f, -50f), new Vector2(400f, 28f));
        var reset = RuntimeUIKit.MakeButton("ResetZoom", panel.transform, "Vue complète", 12, ButtonColor,
            new Vector2(PanelWidth - 290f, -53f), new Vector2(120f, 22f));
        reset.onClick.AddListener(ResetZoom);
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.5f, 1f, 0.6f),
            new Vector2(PanelWidth - 160f, -50f), new Vector2(150f, 28f));

        RuntimeUIKit.MakeText("YTitle", panel.transform, "Intensité (counts)", 11, TextAnchor.MiddleLeft, LightText,
            new Vector2(8f, -80f), new Vector2(160f, 14f));

        var plot = new GameObject("Plot", typeof(RectTransform), typeof(RawImage));
        plot.transform.SetParent(panel.transform, false);
        plotRect = plot.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(plotRect, new Vector2(PlotX, PlotY), new Vector2(PlotW, PlotH));
        plotImage = plot.GetComponent<RawImage>();
        plotImage.raycastTarget = false;

        yLabels = new Text[10];
        for (var i = 0; i < yLabels.Length; i++)
            yLabels[i] = RuntimeUIKit.MakeText("YLabel" + i, panel.transform, "", 11, TextAnchor.MiddleRight, LightText, Vector2.zero, new Vector2(48f, 14f));
        xLabels = new Text[MaxXLabels];
        for (var i = 0; i < xLabels.Length; i++)
            xLabels[i] = RuntimeUIKit.MakeText("XLabel" + i, panel.transform, "", 11, TextAnchor.MiddleCenter, LightText, Vector2.zero, new Vector2(48f, 14f));

        RuntimeUIKit.MakeText("XTitle", panel.transform, "Décalage Raman (cm-1)  —  molette : zoom", 11, TextAnchor.MiddleCenter, LightText,
            new Vector2(PlotX, PlotY - PlotH - 18f), new Vector2(PlotW, 14f));

        RuntimeUIKit.MakeBox("LegendLine", panel.transform, new Vector2(PlotX + 14f, PlotY - 14f), new Vector2(26f, 3f), new Color(0.93f, 0.93f, 0.96f, 1f), false);
        legendText = RuntimeUIKit.MakeText("Legend", panel.transform, "", 11, TextAnchor.MiddleLeft, LightText,
            new Vector2(PlotX + 46f, PlotY - 8f), new Vector2(260f, 14f));

        cursorText = RuntimeUIKit.MakeText("Cursor", panel.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 1f), Vector2.zero, new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(false);
    }

    private void BuildHardware()
    {
        const float top = -388f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 124f), new Color(0.12f, 0.13f, 0.16f, 1f), false);

        // GemmoRaman 532: black cube with the sample chamber on top, green ring and laser warning sticker.
        var body = RuntimeUIKit.MakeBox("Spectrometer", panel.transform, new Vector2(50f, top - 30f), new Vector2(270f, 84f), new Color(0.05f, 0.05f, 0.06f, 1f));
        body.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;
        RuntimeUIKit.MakeBox("BodySplit", panel.transform, new Vector2(50f, top - 70f), new Vector2(270f, 2f), new Color(0.16f, 0.16f, 0.18f, 1f), false).raycastTarget = false;

        var chamber = RuntimeUIKit.MakeBox("Chamber", panel.transform, new Vector2(84f, top - 8f), new Vector2(150f, 34f), new Color(0.09f, 0.09f, 0.11f, 1f));
        chamber.raycastTarget = false;
        var window = RuntimeUIKit.MakeBox("ChamberWindow", panel.transform, new Vector2(98f, top - 13f), new Vector2(122f, 24f), new Color(0.24f, 0.26f, 0.30f, 1f));
        window.raycastTarget = false;
        laserGlow = RuntimeUIKit.MakeBox("LaserGlow", panel.transform, new Vector2(98f, top - 13f), new Vector2(122f, 24f), new Color(0.2f, 1f, 0.45f, 0f));
        laserGlow.raycastTarget = false;
        RuntimeUIKit.MakeBox("Handle", panel.transform, new Vector2(152f, top - 4f), new Vector2(26f, 6f), new Color(0.2f, 0.2f, 0.23f, 1f), false).raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(145f, top - 12f), new Vector2(26f, 26f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une gemme", 10, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.8f),
            new Vector2(98f, top - 13f), new Vector2(122f, 24f));

        RuntimeUIKit.MakeBox("LedBack", panel.transform, new Vector2(64f, top - 54f), new Vector2(22f, 22f), new Color(0.02f, 0.02f, 0.03f, 1f)).raycastTarget = false;
        ledRing = RuntimeUIKit.MakeBox("LedRing", panel.transform, new Vector2(66f, top - 56f), new Vector2(18f, 18f), new Color(0.1f, 0.55f, 0.32f, 1f));
        ledRing.raycastTarget = false;
        RuntimeUIKit.MakeBox("LedCenter", panel.transform, new Vector2(70f, top - 60f), new Vector2(10f, 10f), new Color(0.02f, 0.02f, 0.03f, 1f)).raycastTarget = false;

        RuntimeUIKit.MakeBox("Sticker", panel.transform, new Vector2(272f, top - 48f), new Vector2(40f, 26f), new Color(0.95f, 0.78f, 0.12f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeText("StickerText", panel.transform, "LASER\nCLASS 1", 8, TextAnchor.MiddleCenter, new Color(0.15f, 0.12f, 0.02f),
            new Vector2(272f, top - 48f), new Vector2(40f, 26f));
        RuntimeUIKit.MakeText("Brand", panel.transform, "GemmoRaman", 14, TextAnchor.MiddleLeft, Color.white,
            new Vector2(100f, top - 56f), new Vector2(150f, 20f));
        RuntimeUIKit.MakeText("Model", panel.transform, "532", 14, TextAnchor.MiddleLeft, new Color(0.3f, 1f, 0.55f),
            new Vector2(100f, top - 76f), new Vector2(60f, 20f));

        // Where the laser is pointed.
        RuntimeUIKit.MakeText("TargetHeader", panel.transform, "Cible du laser", 12, TextAnchor.MiddleLeft, Gold, new Vector2(340f, top - 8f), new Vector2(160f, 18f));
        var names = new[] { "Hôte", "Inclusion", "Fissure" };
        targetImages = new Image[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Target" + i, panel.transform, names[i], 11, i == 0 ? SelectedColor : ButtonColor,
                new Vector2(340f + i * 56f, top - 30f), new Vector2(52f, 24f));
            targetImages[i] = button.GetComponent<Image>();
            button.onClick.AddListener(() => SetTarget((RamanTarget)index));
        }
        targetText = RuntimeUIKit.MakeText("TargetText", panel.transform, "Laser sur le cristal hôte", 11, TextAnchor.UpperLeft, new Color(0.7f, 0.74f, 0.8f),
            new Vector2(340f, top - 62f), new Vector2(170f, 32f));

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(530f, top - 22f), new Vector2(140f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor, new Vector2(530f, top - 76f), new Vector2(140f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }

    private void BuildResults()
    {
        const float top = -520f;
        RuntimeUIKit.MakeBox("Results", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 160f), new Color(0.07f, 0.08f, 0.10f, 1f), false);

        RuntimeUIKit.MakeText("PeaksHeader", panel.transform, "Pics détectés", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 6f), new Vector2(300f, 20f));
        peaksText = RuntimeUIKit.MakeText("Peaks", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 28f), new Vector2(340f, 46f));

        RuntimeUIKit.MakeText("AnalysisHeader", panel.transform, "Analyse", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 78f), new Vector2(300f, 20f));
        analysisText = RuntimeUIKit.MakeText("Analysis", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 100f), new Vector2(340f, 58f));

        RuntimeUIKit.MakeText("LibraryHeader", panel.transform, "Bibliothèque de spectres", 14, TextAnchor.UpperLeft, Gold, new Vector2(376f, top - 6f), new Vector2(300f, 20f));
        matchButtons = new Button[MatchButtons];
        matchImages = new Image[MatchButtons];
        matchTexts = new Text[MatchButtons];
        for (var i = 0; i < MatchButtons; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Match" + i, panel.transform, "", 12, ButtonColor,
                new Vector2(376f, top - 30f - i * 28f), new Vector2(300f, 24f));
            matchButtons[i] = button;
            matchImages[i] = button.GetComponent<Image>();
            matchTexts[i] = button.GetComponentInChildren<Text>();
            button.onClick.AddListener(() => SelectMatch(index));
            button.gameObject.SetActive(false);
        }
        RuntimeUIKit.MakeText("LibraryHint", panel.transform, "Cliquez une entrée pour superposer son spectre (vert).", 10, TextAnchor.UpperLeft,
            new Color(0.6f, 0.64f, 0.7f), new Vector2(376f, top - 144f), new Vector2(300f, 14f));
    }
}
