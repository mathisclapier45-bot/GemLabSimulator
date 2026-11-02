using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// EDXRF spectrometer of the laboratory: drop a gem in the sample chamber, press Start, and the X-ray fluorescence spectrum (0 to 40 keV)
// is drawn in three seconds. Each peak is labelled with its element. Mouse wheel to zoom, hover to read a peak.
public class EdxrfUI : MonoBehaviour
{
    public static EdxrfUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 660f;
    private const int PlotW = 620;
    private const int PlotH = 270;
    private const float PlotX = 64f;
    private const float PlotY = -96f;
    private const float ScanSeconds = 3f;
    private const int MaxXLabels = 14;
    private const int MaxPeakLabels = 16;

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
    private Text[] peakLabels;
    private Text cursorText;
    private Text statusText;
    private Text elementsText;
    private Text analysisText;
    private Text legendText;
    private Image gemImage;
    private Image tubeGlow;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;

    private Texture2D plotTexture;
    private Color32[] basePixels;
    private Color32[] livePixels;

    private GemInstance stone;
    private EdxrfResult result;
    private float viewLo;
    private float viewHi = EdxrfResult.MaxKeV;
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
            canvas.AddComponent<EdxrfUI>();
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
            ToolFeedbackUI.Instance?.ShowMessage("EDXRF : glissez d'abord une pierre dans la chambre.");
            return;
        }

        result = EdxrfSpectrum.Measure(stone);
        viewLo = 0f;
        viewHi = EdxrfResult.MaxKeV;
        RebuildView();

        scanning = true;
        hasCurve = false;
        scanTime = 0f;
        headFraction = 0f;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        legendText.text = $"Spectre {stone.SpecimenId}";
        statusText.text = "Acquisition...";
        elementsText.text = "Acquisition en cours...";
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
        viewLo = 0f;
        viewHi = EdxrfResult.MaxKeV;
        RebuildView();
        cursorText.gameObject.SetActive(false);
        legendText.text = "";
        statusText.text = "Prêt";
        elementsText.text = stone == null ? "Aucune pierre dans la chambre." : "Appuyez sur Start pour lancer l'analyse.";
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

        elementsText.text = result.Elements.Count == 0
            ? "Aucun élément détecté (matériau trop léger)."
            : string.Join("  ", result.Elements) + "\n\nLes éléments légers (Be, Li, B, C, O, Na, Mg, Al, Si) ne sont pas vus par cette méthode.";
        var count = Mathf.Min(result.Analysis.Count, 4);
        analysisText.text = count == 0 ? "" : string.Join("\n", result.Analysis.GetRange(0, count));

        Render();
        LayoutPeakLabels();

        stone.RecordObservation(ExaminationTool.MeasurementType.ElementalAnalysis.ToString());
        if (result.RecordAuthenticity)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Authenticity.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"EDXRF — Authenticité : {stone.AuthenticityGrade} — {stone.AuthenticityDescription}");
        }
        if (result.RecordTreatment)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"EDXRF — Traitement détecté : {stone.TreatmentGrade} — {stone.TreatmentDescription}");
        }
        if (!result.RecordAuthenticity && !result.RecordTreatment)
            ToolFeedbackUI.Instance?.ShowMessage($"EDXRF — {EdxrfSpectrum.Summary(stone)}");
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.4f + 0.2f * Mathf.Sin(Time.time * 16f) : 0f;
        tubeGlow.color = new Color(1f, 0.85f, 0.25f, glow);

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
        var newSpan = Mathf.Clamp(span * (scroll > 0f ? 0.7f : 1.4f), 2f, EdxrfResult.MaxKeV);

        var lo = under - t * newSpan;
        var hi = lo + newSpan;
        if (lo < 0f) { hi -= lo; lo = 0f; }
        if (hi > EdxrfResult.MaxKeV) { lo -= hi - EdxrfResult.MaxKeV; hi = EdxrfResult.MaxKeV; }
        viewLo = Mathf.Max(0f, lo);
        viewHi = hi;

        RebuildView();
        hoverX = -1;
        Render();
        LayoutPeakLabels();
    }

    private void ResetZoom()
    {
        if (!hasCurve) return;
        viewLo = 0f;
        viewHi = EdxrfResult.MaxKeV;
        RebuildView();
        hoverX = -1;
        Render();
        LayoutPeakLabels();
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

    private int XOf(float keV)
    {
        return Mathf.RoundToInt((keV - viewLo) / Span * (PlotW - 1));
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
            var lo = Mathf.Max(0, Mathf.FloorToInt(viewLo / 0.02f));
            var hi = Mathf.Min(result.Spectrum.Length - 1, Mathf.CeilToInt(viewHi / 0.02f));
            var max = 0f;
            for (var i = lo; i <= hi; i++) max = Mathf.Max(max, result.Spectrum[i]);

            var raw = Mathf.Max(60f, max * 1.18f);
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
        HidePeakLabels();
    }

    private void BuildBase()
    {
        var white = new Color32(255, 255, 255, 255);
        for (var i = 0; i < basePixels.Length; i++) basePixels[i] = white;

        var grid = new Color32(228, 228, 232, 255);
        for (var v = yStep; v <= yMax + 0.01f; v += yStep)
        {
            var y = YOf(v);
            for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, grid);
        }

        var xStep = NiceStep(Span / 8f);
        for (var e = Mathf.Ceil(viewLo / xStep) * xStep; e <= viewHi + 0.001f; e += xStep)
        {
            var x = XOf(e);
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
        var first = Mathf.Max(0, Mathf.FloorToInt(viewLo / 0.02f) - 1);
        var last = Mathf.Min(result.Spectrum.Length - 1, Mathf.CeilToInt(viewHi / 0.02f) + 1);
        var ink = new Color32(40, 40, 140, 255);

        var prevX = 0;
        var prevY = 0;
        var havePrev = false;
        for (var i = first; i <= last; i++)
        {
            var x = XOf(i * 0.02f);
            var y = Mathf.Clamp(YOf(result.Spectrum[i]), 0, PlotH - 2);
            if (x > headX) break;

            if (havePrev && (x != prevX || y != prevY)) DrawSegment(prevX, prevY, x, y, ink);
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

        var energy = viewLo + hoverX / (float)(PlotW - 1) * Span;
        var index = Mathf.Clamp(Mathf.RoundToInt(energy / 0.02f), 0, result.Spectrum.Length - 1);
        cursorText.text = $"{energy:F2} keV   I = {result.Spectrum[index]:F0}";
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
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX - 48f, y), new Vector2(44f, 14f));
        }
        for (var i = count; i < yLabels.Length; i++) yLabels[i].gameObject.SetActive(false);

        var xStep = NiceStep(Span / 8f);
        count = 0;
        for (var e = Mathf.Ceil(viewLo / xStep) * xStep; e <= viewHi + 0.001f && count < xLabels.Length; e += xStep, count++)
        {
            var label = xLabels[count];
            label.gameObject.SetActive(true);
            label.text = e.ToString(xStep >= 1f ? "0" : "0.0");
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX + XOf(e) - 24f, PlotY - PlotH - 3f), new Vector2(48f, 14f));
        }
        for (var i = count; i < xLabels.Length; i++) xLabels[i].gameObject.SetActive(false);
    }

    private void HidePeakLabels()
    {
        foreach (var label in peakLabels) label.gameObject.SetActive(false);
    }

    // Element symbol above each peak of the view that stands out, strongest first, without overlapping.
    private void LayoutPeakLabels()
    {
        HidePeakLabels();
        if (result == null || !hasCurve) return;

        var peaks = new List<EdxrfPeak>(result.Peaks);
        peaks.Sort((a, b) => b.Height.CompareTo(a.Height));

        var placed = new List<int>();
        var count = 0;
        foreach (var peak in peaks)
        {
            if (count >= peakLabels.Length) break;
            if (peak.KeV < viewLo || peak.KeV > viewHi) continue;
            if (peak.Height < 0.04f * yMax) continue;

            var x = XOf(peak.KeV);
            var tooClose = false;
            foreach (var other in placed) if (Mathf.Abs(other - x) < 20) tooClose = true;
            if (tooClose) continue;

            var index = Mathf.Clamp(Mathf.RoundToInt(peak.KeV / 0.02f), 0, result.Spectrum.Length - 1);
            var top = 0f;
            for (var i = Mathf.Max(0, index - 3); i <= Mathf.Min(result.Spectrum.Length - 1, index + 3); i++) top = Mathf.Max(top, result.Spectrum[i]);
            var y = Mathf.Min(YOf(top) + 4, PlotH - 16);

            var label = peakLabels[count++];
            label.gameObject.SetActive(true);
            label.text = peak.Symbol;
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX + x - 30f, PlotY - (PlotH - 1 - y) + 14f), new Vector2(60f, 14f));
            placed.Add(x);
        }
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("EdxrfPanel", typeof(RectTransform));
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
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.35f, 0.22f, 0.05f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "EDXRF — Spectrométrie de fluorescence X", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(14f, 0f), new Vector2(520f, 26f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Menu", panel.transform, "Fichier     Acquisition     Quantification     Éléments     Aide", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -28f), new Vector2(520f, 20f));

        RuntimeUIKit.MakeBox("Toolbar", panel.transform, new Vector2(0f, -50f), new Vector2(PanelWidth, 28f), new Color(0.80f, 0.81f, 0.84f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Tube : Rh 50 kV      Filtre : aucun      Atmosphère : air      Temps : 3 s", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -50f), new Vector2(440f, 28f));
        var reset = RuntimeUIKit.MakeButton("ResetZoom", panel.transform, "Vue complète", 12, ButtonColor,
            new Vector2(PanelWidth - 290f, -53f), new Vector2(120f, 22f));
        reset.onClick.AddListener(ResetZoom);
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.1f, 0.3f, 0.1f),
            new Vector2(PanelWidth - 160f, -50f), new Vector2(150f, 28f));

        RuntimeUIKit.MakeText("YTitle", panel.transform, "Intensité (u.a.)", 11, TextAnchor.MiddleLeft, Ink, new Vector2(8f, -80f), new Vector2(160f, 14f));

        var plot = new GameObject("Plot", typeof(RectTransform), typeof(RawImage));
        plot.transform.SetParent(panel.transform, false);
        plotRect = plot.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(plotRect, new Vector2(PlotX, PlotY), new Vector2(PlotW, PlotH));
        plotImage = plot.GetComponent<RawImage>();
        plotImage.raycastTarget = false;

        yLabels = new Text[10];
        for (var i = 0; i < yLabels.Length; i++)
            yLabels[i] = RuntimeUIKit.MakeText("YLabel" + i, panel.transform, "", 11, TextAnchor.MiddleRight, Ink, Vector2.zero, new Vector2(44f, 14f));
        xLabels = new Text[MaxXLabels];
        for (var i = 0; i < xLabels.Length; i++)
            xLabels[i] = RuntimeUIKit.MakeText("XLabel" + i, panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(48f, 14f));
        peakLabels = new Text[MaxPeakLabels];
        for (var i = 0; i < peakLabels.Length; i++)
            peakLabels[i] = RuntimeUIKit.MakeText("PeakLabel" + i, panel.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.45f), Vector2.zero, new Vector2(60f, 14f));

        RuntimeUIKit.MakeText("XTitle", panel.transform, "Énergie (keV)  —  molette : zoom", 11, TextAnchor.MiddleCenter, Ink,
            new Vector2(PlotX, PlotY - PlotH - 18f), new Vector2(PlotW, 14f));

        RuntimeUIKit.MakeBox("LegendLine", panel.transform, new Vector2(PlotX + 14f, PlotY - 14f), new Vector2(26f, 3f), new Color(0.16f, 0.16f, 0.55f, 1f), false);
        legendText = RuntimeUIKit.MakeText("Legend", panel.transform, "", 11, TextAnchor.MiddleLeft, Ink, new Vector2(PlotX + 46f, PlotY - 8f), new Vector2(200f, 14f));

        cursorText = RuntimeUIKit.MakeText("Cursor", panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(false);
    }

    private void BuildHardware()
    {
        const float top = -408f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 124f), new Color(0.14f, 0.15f, 0.18f, 1f), false);

        // Benchtop unit: pale body, open lid over the sample chamber, X-ray tube and hazard sign.
        var body = RuntimeUIKit.MakeBox("Spectrometer", panel.transform, new Vector2(50f, top - 28f), new Vector2(400f, 88f), new Color(0.90f, 0.91f, 0.93f, 1f));
        body.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;

        RuntimeUIKit.MakeBox("Lid", panel.transform, new Vector2(150f, top - 8f), new Vector2(190f, 24f), new Color(0.74f, 0.76f, 0.80f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("LidWindow", panel.transform, new Vector2(160f, top - 12f), new Vector2(170f, 12f), new Color(0.3f, 0.34f, 0.2f, 0.85f), false).raycastTarget = false;

        RuntimeUIKit.MakeBox("Chamber", panel.transform, new Vector2(160f, top - 40f), new Vector2(170f, 62f), new Color(0.18f, 0.19f, 0.22f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("ChamberInner", panel.transform, new Vector2(168f, top - 46f), new Vector2(154f, 50f), new Color(0.30f, 0.31f, 0.36f, 1f)).raycastTarget = false;
        tubeGlow = RuntimeUIKit.MakeBox("TubeGlow", panel.transform, new Vector2(168f, top - 46f), new Vector2(154f, 50f), new Color(1f, 0.85f, 0.25f, 0f));
        tubeGlow.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(224f, top - 49f), new Vector2(40f, 40f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une pierre", 10, TextAnchor.MiddleCenter, new Color(0.75f, 0.77f, 0.85f),
            new Vector2(168f, top - 46f), new Vector2(154f, 50f));

        RuntimeUIKit.MakeText("Brand", panel.transform, "EDXRF", 14, TextAnchor.MiddleLeft, new Color(0.25f, 0.27f, 0.33f), new Vector2(64f, top - 38f), new Vector2(80f, 20f));
        RuntimeUIKit.MakeText("Model", panel.transform, "Rh 50 kV", 10, TextAnchor.MiddleLeft, new Color(0.4f, 0.42f, 0.48f), new Vector2(64f, top - 58f), new Vector2(80f, 14f));
        RuntimeUIKit.MakeBox("Hazard", panel.transform, new Vector2(366f, top - 44f), new Vector2(30f, 26f), new Color(0.97f, 0.82f, 0.1f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeText("HazardText", panel.transform, "X", 14, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.1f), new Vector2(366f, top - 44f), new Vector2(30f, 26f));
        RuntimeUIKit.MakeBox("Led", panel.transform, new Vector2(410f, top - 44f), new Vector2(12f, 12f), new Color(0.2f, 0.75f, 0.35f, 1f), false).raycastTarget = false;

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(520f, top - 22f), new Vector2(150f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la pierre", 13, ButtonColor, new Vector2(520f, top - 76f), new Vector2(150f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }

    private void BuildResults()
    {
        const float top = -540f;
        RuntimeUIKit.MakeBox("Results", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 120f), new Color(0.07f, 0.08f, 0.10f, 1f), false);

        RuntimeUIKit.MakeText("ElementsHeader", panel.transform, "Éléments détectés", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 6f), new Vector2(300f, 20f));
        elementsText = RuntimeUIKit.MakeText("Elements", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 28f), new Vector2(320f, 90f));

        RuntimeUIKit.MakeText("AnalysisHeader", panel.transform, "Analyse", 14, TextAnchor.UpperLeft, Gold, new Vector2(350f, top - 6f), new Vector2(300f, 20f));
        analysisText = RuntimeUIKit.MakeText("Analysis", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(350f, top - 28f), new Vector2(340f, 90f));
    }
}
