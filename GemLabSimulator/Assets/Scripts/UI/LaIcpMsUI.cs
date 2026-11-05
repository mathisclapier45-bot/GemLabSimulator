using UnityEngine;
using UnityEngine.UI;

// LA-ICP-MS of the laboratory: drop a gem in the ablation cell, press Start, and in three seconds the laser digs a crater while the mass spectrometer
// counts the atoms. The bars give the composition of the surface (parts per million, logarithmic scale); click an element to read its depth
// profile: beryllium high at the surface and falling with the depth, or too high all the way down, gives away a beryllium diffusion.
public class LaIcpMsUI : MonoBehaviour
{
    public static LaIcpMsUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 660f;
    private const int PlotW = 600;
    private const int PlotH = 120;
    private const float PlotX = 64f;
    private const float PlotY = -278f;
    private const float ScanSeconds = 3f;
    private const int MaxBars = 14;
    private const float BarsBaseY = -240f;
    private const float BarsHeight = 136f;
    private const float BarsX = 30f;
    private const float BarStep = 46f;
    private const float MinLog = -1f;          // 0.1 ppm
    private const float MaxLog = 5f;           // 100 000 ppm

    private static readonly Color WindowColor = new Color(0.86f, 0.88f, 0.88f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color Ink = new Color(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Color HeavyBar = new Color(0.22f, 0.45f, 0.62f, 1f);
    private static readonly Color LightBar = new Color(0.55f, 0.32f, 0.62f, 1f);
    private static readonly Color SelectedBar = new Color(0.92f, 0.70f, 0.18f, 1f);

    private GameObject panel;
    private RawImage plotImage;
    private RectTransform plotRect;
    private Button[] barButtons;
    private Image[] barImages;
    private Text[] barSymbols;
    private Text[] barValues;
    private Text[] plotYLabels;
    private Text[] plotXLabels;
    private Text profileTitle;
    private Text cursorText;
    private Text statusText;
    private Text elementsText;
    private Text analysisText;
    private Image gemImage;
    private Image laserBeam;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;

    private Texture2D plotTexture;
    private Color32[] basePixels;
    private Color32[] livePixels;

    private GemInstance stone;
    private LaIcpMsResult result;
    private string selected;
    private bool scanning;
    private bool hasData;
    private float scanTime;
    private float headFraction;
    private int hoverX = -1;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<LaIcpMsUI>();
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
        ClearData();
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
        ClearData();
        Refresh();
    }

    private void RemoveStone()
    {
        stone = null;
        ClearData();
        Refresh();
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("LA-ICP-MS : glissez d'abord une pierre dans la cellule d'ablation.");
            return;
        }

        result = LaIcpMsAnalysis.Measure(stone);
        selected = ChooseDefaultElement();
        scanning = true;
        hasData = false;
        scanTime = 0f;
        headFraction = 0f;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        statusText.text = "Ablation...";
        elementsText.text = "Ablation en cours...";
        analysisText.text = "";
        SetStartBusy(true);
        LayoutBars(0f);
        RebuildBase();
        Render();
    }

    // Beryllium if it was seen: it is what the instrument is here for. Otherwise the strongest element.
    private string ChooseDefaultElement()
    {
        foreach (var e in result.Elements) if (e.Symbol == "Be") return "Be";
        return result.Elements.Count > 0 ? result.Elements[0].Symbol : "Be";
    }

    private void ClearData()
    {
        scanning = false;
        hasData = false;
        result = null;
        selected = null;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        statusText.text = "Prêt";
        elementsText.text = stone == null ? "Aucune pierre dans la cellule." : "Appuyez sur Start pour lancer l'ablation.";
        analysisText.text = "";
        profileTitle.text = "Profil en profondeur";
        SetStartBusy(false);
        LayoutBars(0f);
        RebuildBase();
        Render();
    }

    private void FinishScan()
    {
        scanning = false;
        hasData = true;
        statusText.text = "Terminé";
        SetStartBusy(false);
        headFraction = 1f;
        laserBeam.color = new Color(0.4f, 1f, 0.55f, 0f);

        var sb = new System.Text.StringBuilder();
        var count = Mathf.Min(result.Elements.Count, 9);
        for (var i = 0; i < count; i++)
        {
            var e = result.Elements[i];
            sb.Append(e.Symbol).Append("  ").Append(LaIcpMsAnalysis.FormatPpm(e.Ppm)).Append(" ppm");
            if (e.Light) sb.Append("  (léger)");
            sb.Append('\n');
        }
        elementsText.text = count == 0 ? "Aucun élément détecté." : sb.ToString().TrimEnd('\n');
        var lines = Mathf.Min(result.Analysis.Count, 4);
        analysisText.text = lines == 0 ? "" : string.Join("\n", result.Analysis.GetRange(0, lines));

        LayoutBars(1f);
        RebuildBase();
        Render();

        stone.RecordObservation(ExaminationTool.MeasurementType.TraceElements.ToString());
        if (result.RecordAuthenticity)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Authenticity.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"LA-ICP-MS — Authenticité : {stone.AuthenticityGrade} — {stone.AuthenticityDescription}");
        }
        if (result.RecordTreatment)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"LA-ICP-MS — Traitement détecté : {stone.TreatmentGrade} — {stone.TreatmentDescription}");
        }
        if (!result.RecordAuthenticity && !result.RecordTreatment)
            ToolFeedbackUI.Instance?.ShowMessage($"LA-ICP-MS — {LaIcpMsAnalysis.Summary(stone)}");
    }

    private void SelectElement(int index)
    {
        if (!hasData || result == null || index >= result.Elements.Count) return;
        selected = result.Elements[index].Symbol;
        hoverX = -1;
        LayoutBars(1f);
        RebuildBase();
        Render();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (scanning)
        {
            scanTime += Time.deltaTime;
            headFraction = Mathf.Clamp01(scanTime / ScanSeconds);
            laserBeam.color = new Color(0.4f, 1f, 0.55f, 0.45f + 0.3f * Mathf.Sin(Time.time * 30f));
            LayoutBars(headFraction);
            Render();
            if (headFraction >= 1f) FinishScan();
            return;
        }

        if (hasData) HandleHover();
    }

    private bool PointerInPlot(out Vector2 local)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(plotRect, Input.mousePosition, null, out local);
        return local.x >= 0f && local.x <= PlotW - 1 && local.y <= 0f && local.y >= -(PlotH - 1);
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
            ClearData();
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
        if (!busy) laserBeam.color = new Color(0.4f, 1f, 0.55f, 0f);
    }

    // ---- Bars (composition) ----------------------------------------------------------------------------------

    private static float LogFraction(float ppm)
    {
        return Mathf.Clamp01((Mathf.Log10(Mathf.Max(ppm, 0.01f)) - MinLog) / (MaxLog - MinLog));
    }

    private void LayoutBars(float fraction)
    {
        var count = result == null ? 0 : Mathf.Min(result.Elements.Count, MaxBars);
        for (var i = 0; i < MaxBars; i++)
        {
            var active = i < count;
            barButtons[i].gameObject.SetActive(active);
            barSymbols[i].gameObject.SetActive(active);
            barValues[i].gameObject.SetActive(active);
            if (!active) continue;

            var e = result.Elements[i];
            var h = Mathf.Max(3f, LogFraction(e.Ppm) * BarsHeight * fraction);
            var x = BarsX + i * BarStep;
            RuntimeUIKit.SetTopLeft(barImages[i].rectTransform, new Vector2(x, BarsBaseY + h), new Vector2(34f, h));
            barImages[i].color = e.Symbol == selected && !scanning ? SelectedBar : e.Light ? LightBar : HeavyBar;
            barSymbols[i].text = e.Symbol;
            RuntimeUIKit.SetTopLeft(barSymbols[i].rectTransform, new Vector2(x - 6f, BarsBaseY - 1f), new Vector2(46f, 14f));
            barValues[i].text = fraction >= 1f ? LaIcpMsAnalysis.FormatPpm(e.Ppm) : "";
            RuntimeUIKit.SetTopLeft(barValues[i].rectTransform, new Vector2(x - 8f, BarsBaseY + h + 13f), new Vector2(50f, 13f));
        }
    }

    // ---- Depth profile ---------------------------------------------------------------------------------------

    private int XOf(float depthUm) => Mathf.RoundToInt(depthUm / LaIcpMsResult.ProfileDepthUm * (PlotW - 1));

    private int YOf(float ppm) => Mathf.RoundToInt(LogFraction(ppm) * (PlotH - 1));

    private bool ShowNaturalBand()
    {
        if (stone == null || selected != "Be") return false;
        var n = stone.Definition.name ?? "";
        return n.StartsWith("Corindon") || n.StartsWith("Saphir") || n == "Padparadscha";
    }

    private void RebuildBase()
    {
        var white = new Color32(255, 255, 255, 255);
        for (var i = 0; i < basePixels.Length; i++) basePixels[i] = white;

        if (ShowNaturalBand())
        {
            var band = new Color32(222, 244, 226, 255);
            for (var y = YOf(0.3f); y <= YOf(3f); y++)
                for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, band);
        }

        var grid = new Color32(228, 228, 232, 255);
        for (var d = MinLog; d <= MaxLog + 0.01f; d += 1f)
        {
            var y = YOf(Mathf.Pow(10f, d));
            for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, grid);
        }
        for (var depth = 0f; depth <= LaIcpMsResult.ProfileDepthUm + 0.1f; depth += 100f)
        {
            var x = XOf(depth);
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

        profileTitle.text = selected == null
            ? "Profil en profondeur"
            : ShowNaturalBand() ? $"Profil en profondeur : {selected}  (bande verte : béryllium naturel, 0,3 à 3 ppm)" : $"Profil en profondeur : {selected}";
    }

    private void Render()
    {
        System.Array.Copy(basePixels, livePixels, basePixels.Length);

        if (result != null && selected != null)
        {
            var headX = scanning ? Mathf.RoundToInt(headFraction * (PlotW - 1)) : PlotW;
            DrawProfile(result.ProfileOf(selected), headX);
        }

        if (hasData && hoverX >= 0)
        {
            var teal = new Color32(0, 120, 120, 255);
            for (var y = 1; y < PlotH - 1; y++) SetPixel(livePixels, hoverX, y, teal);
        }

        UpdateCursorText();
        plotTexture.SetPixels32(livePixels);
        plotTexture.Apply(false);
    }

    private void DrawProfile(float[] profile, int headX)
    {
        var ink = new Color32(40, 40, 140, 255);
        var prevX = 0;
        var prevY = 0;
        for (var i = 0; i < profile.Length; i++)
        {
            var x = XOf(i * LaIcpMsResult.ProfileDepthUm / LaIcpMsResult.ProfilePoints);
            var y = Mathf.Clamp(YOf(profile[i]), 0, PlotH - 2);
            if (x > headX) break;
            if (i > 0) DrawSegment(prevX, prevY, x, y, ink);
            prevX = x;
            prevY = y;
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

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= PlotW || y >= PlotH) return;
        pixels[y * PlotW + x] = color;
    }

    private void UpdateCursorText()
    {
        if (!hasData || hoverX < 0 || scanning || result == null || selected == null)
        {
            cursorText.gameObject.SetActive(false);
            return;
        }

        var depth = hoverX / (float)(PlotW - 1) * LaIcpMsResult.ProfileDepthUm;
        var profile = result.ProfileOf(selected);
        var index = Mathf.Clamp(Mathf.RoundToInt(depth / LaIcpMsResult.ProfileDepthUm * (LaIcpMsResult.ProfilePoints - 1)), 0, profile.Length - 1);
        var ppm = profile[index];
        cursorText.text = ppm < LaIcpMsResult.DetectionLimit ? $"{depth:0} µm   < 0,05 ppm" : $"{depth:0} µm   {LaIcpMsAnalysis.FormatPpm(ppm)} ppm";
        var x = Mathf.Clamp(PlotX + hoverX - 70f, PlotX + 2f, PlotX + PlotW - 150f);
        RuntimeUIKit.SetTopLeft(cursorText.rectTransform, new Vector2(x, PlotY - 3f), new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(true);
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("LaIcpMsPanel", typeof(RectTransform));
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
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.10f, 0.30f, 0.32f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "LA-ICP-MS — Ablation laser et spectrométrie de masse", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(14f, 0f), new Vector2(520f, 26f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Menu", panel.transform, "Fichier     Ablation     Isotopes     Profil     Aide", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -28f), new Vector2(520f, 20f));

        RuntimeUIKit.MakeBox("Toolbar", panel.transform, new Vector2(0f, -50f), new Vector2(PanelWidth, 28f), new Color(0.80f, 0.82f, 0.84f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Laser ArF 193 nm      Cratère : 40 µm      Gaz : argon      Durée : 3 s", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -50f), new Vector2(480f, 28f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.1f, 0.3f, 0.1f),
            new Vector2(PanelWidth - 160f, -50f), new Vector2(150f, 28f));

        RuntimeUIKit.MakeText("BarsTitle", panel.transform, "Composition de la surface (ppm, échelle logarithmique) — cliquez sur un élément", 11,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -80f), new Vector2(PanelWidth - 20f, 14f));

        // Decade lines and labels behind the bars.
        var labels = new[] { "0,1", "1", "10", "100", "1 000", "10⁴", "10⁵" };
        for (var d = 0; d < labels.Length; d++)
        {
            var y = BarsBaseY + d / (MaxLog - MinLog) * BarsHeight;
            RuntimeUIKit.MakeBox("BarGrid" + d, panel.transform, new Vector2(BarsX - 4f, y), new Vector2(BarStep * MaxBars + 4f, 1f), new Color(0.74f, 0.77f, 0.80f, 1f), false)
                .raycastTarget = false;
            RuntimeUIKit.MakeText("BarGridLabel" + d, panel.transform, labels[d], 9, TextAnchor.MiddleRight, new Color(0.3f, 0.32f, 0.36f),
                new Vector2(-6f, y + 6f), new Vector2(32f, 12f));
        }

        barButtons = new Button[MaxBars];
        barImages = new Image[MaxBars];
        barSymbols = new Text[MaxBars];
        barValues = new Text[MaxBars];
        for (var i = 0; i < MaxBars; i++)
        {
            var index = i;
            barButtons[i] = RuntimeUIKit.MakeButton("Bar" + i, panel.transform, "", 10, HeavyBar, new Vector2(BarsX + i * BarStep, BarsBaseY + 3f), new Vector2(34f, 3f));
            barButtons[i].onClick.AddListener(() => SelectElement(index));
            barImages[i] = barButtons[i].GetComponent<Image>();
            barSymbols[i] = RuntimeUIKit.MakeText("BarSymbol" + i, panel.transform, "", 12, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(46f, 14f));
            barValues[i] = RuntimeUIKit.MakeText("BarValue" + i, panel.transform, "", 10, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(50f, 13f));
            barButtons[i].gameObject.SetActive(false);
            barSymbols[i].gameObject.SetActive(false);
            barValues[i].gameObject.SetActive(false);
        }

        profileTitle = RuntimeUIKit.MakeText("ProfileTitle", panel.transform, "Profil en profondeur", 11, TextAnchor.MiddleLeft, Ink,
            new Vector2(10f, PlotY + 16f), new Vector2(PanelWidth - 20f, 14f));

        var plot = new GameObject("Plot", typeof(RectTransform), typeof(RawImage));
        plot.transform.SetParent(panel.transform, false);
        plotRect = plot.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(plotRect, new Vector2(PlotX, PlotY), new Vector2(PlotW, PlotH));
        plotImage = plot.GetComponent<RawImage>();
        plotImage.raycastTarget = false;

        plotYLabels = new Text[labels.Length];
        for (var d = 0; d < labels.Length; d++)
        {
            var y = PlotY - (PlotH - 1 - YOf(Mathf.Pow(10f, MinLog + d))) + 6f;
            plotYLabels[d] = RuntimeUIKit.MakeText("PlotYLabel" + d, panel.transform, labels[d], 9, TextAnchor.MiddleRight, Ink,
                new Vector2(PlotX - 46f, y), new Vector2(42f, 12f));
        }
        plotXLabels = new Text[6];
        for (var i = 0; i < plotXLabels.Length; i++)
            plotXLabels[i] = RuntimeUIKit.MakeText("PlotXLabel" + i, panel.transform, (i * 100).ToString(), 10, TextAnchor.MiddleCenter, Ink,
                new Vector2(PlotX + XOf(i * 100f) - 24f, PlotY - PlotH - 2f), new Vector2(48f, 12f));
        RuntimeUIKit.MakeText("PlotXTitle", panel.transform, "Profondeur du cratère (µm)      ppm (échelle log)", 10, TextAnchor.MiddleCenter, Ink,
            new Vector2(PlotX, PlotY - PlotH - 14f), new Vector2(PlotW, 12f));

        cursorText = RuntimeUIKit.MakeText("Cursor", panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(148f, 16f));
        cursorText.gameObject.SetActive(false);
    }

    private void BuildHardware()
    {
        const float top = -440f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 100f), new Color(0.14f, 0.15f, 0.18f, 1f), false);

        // Ablation cell: a small closed chamber under the laser head, with the stone on its stage; the beam flashes during the ablation.
        var body = RuntimeUIKit.MakeBox("Cell", panel.transform, new Vector2(50f, top - 12f), new Vector2(330f, 78f), new Color(0.86f, 0.88f, 0.90f, 1f));
        body.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;

        RuntimeUIKit.MakeBox("LaserHead", panel.transform, new Vector2(160f, top - 4f), new Vector2(60f, 20f), new Color(0.42f, 0.46f, 0.54f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Chamber", panel.transform, new Vector2(110f, top - 28f), new Vector2(160f, 46f), new Color(0.18f, 0.19f, 0.22f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("ChamberInner", panel.transform, new Vector2(118f, top - 33f), new Vector2(144f, 36f), new Color(0.30f, 0.31f, 0.36f, 1f)).raycastTarget = false;
        laserBeam = RuntimeUIKit.MakeBox("Beam", panel.transform, new Vector2(187f, top - 22f), new Vector2(6f, 34f), new Color(0.4f, 1f, 0.55f, 0f), false);
        laserBeam.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(170f, top - 40f), new Vector2(34f, 34f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une pierre", 10, TextAnchor.MiddleCenter, new Color(0.75f, 0.77f, 0.85f),
            new Vector2(118f, top - 33f), new Vector2(144f, 36f));

        RuntimeUIKit.MakeText("Brand", panel.transform, "LA-ICP-MS", 13, TextAnchor.MiddleLeft, new Color(0.25f, 0.27f, 0.33f), new Vector2(60f, top - 18f), new Vector2(100f, 18f));
        RuntimeUIKit.MakeText("Model", panel.transform, "ArF 193 nm", 10, TextAnchor.MiddleLeft, new Color(0.4f, 0.42f, 0.48f), new Vector2(60f, top - 66f), new Vector2(80f, 14f));
        RuntimeUIKit.MakeBox("Led", panel.transform, new Vector2(350f, top - 22f), new Vector2(12f, 12f), new Color(0.2f, 0.75f, 0.35f, 1f), false).raycastTarget = false;

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(520f, top - 14f), new Vector2(150f, 42f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la pierre", 13, ButtonColor, new Vector2(520f, top - 62f), new Vector2(150f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }

    private void BuildResults()
    {
        const float top = -540f;
        RuntimeUIKit.MakeBox("Results", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 120f), new Color(0.07f, 0.08f, 0.10f, 1f), false);

        RuntimeUIKit.MakeText("ElementsHeader", panel.transform, "Éléments détectés (surface)", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 6f), new Vector2(300f, 20f));
        elementsText = RuntimeUIKit.MakeText("Elements", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 28f), new Vector2(240f, 90f));

        RuntimeUIKit.MakeText("AnalysisHeader", panel.transform, "Analyse", 14, TextAnchor.UpperLeft, Gold, new Vector2(270f, top - 6f), new Vector2(300f, 20f));
        analysisText = RuntimeUIKit.MakeText("Analysis", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(270f, top - 28f), new Vector2(420f, 90f));
    }
}
