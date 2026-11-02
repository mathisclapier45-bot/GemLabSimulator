using UnityEngine;
using UnityEngine.UI;

// UV-Vis-NIR spectrophotometer of the laboratory: drop a gem in the box, press Start, and the absorbance curve (300 to 1000 nm)
// is drawn in three seconds in a GLGemSpec-like window. Once the curve is complete, hover it to read wavelength and absorbance.
public class UvVisNirUI : MonoBehaviour
{
    public static UvVisNirUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 660f;
    private const int PlotW = 620;
    private const int PlotH = 270;
    private const float PlotX = 64f;
    private const float PlotY = -96f;
    private const float YMin = -0.4f;          // below zero: room for the wavelength color bar
    private const float ScanSeconds = 3f;

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
    private Text cursorText;
    private Text statusText;
    private Text bandsText;
    private Text analysisText;
    private Image gemImage;
    private Image lampGlow;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;

    private Texture2D plotTexture;
    private Color32[] basePixels;
    private Color32[] curvePixels;
    private Color32[] livePixels;

    private GemInstance stone;
    private UvVisNirResult result;
    private float yMax = 2f;
    private bool scanning;
    private bool hasCurve;
    private float scanTime;
    private int lastDrawn;
    private int hoverX = -1;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<UvVisNirUI>();
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
            ToolFeedbackUI.Instance?.ShowMessage("Spectrophotomètre : glissez d'abord une gemme dans le boîtier.");
            return;
        }

        result = UvVisNirSpectrum.Measure(stone);
        yMax = result.YMax;
        BuildBase();
        System.Array.Copy(basePixels, curvePixels, basePixels.Length);
        System.Array.Copy(basePixels, livePixels, basePixels.Length);
        LayoutAxisLabels();
        PushPixels(livePixels);

        scanning = true;
        hasCurve = false;
        scanTime = 0f;
        lastDrawn = -1;
        hoverX = -1;
        cursorText.gameObject.SetActive(false);
        statusText.text = "Acquisition...";
        bandsText.text = "Acquisition en cours...";
        analysisText.text = "";
        SetStartBusy(true);
    }

    private void ClearCurve()
    {
        scanning = false;
        hasCurve = false;
        result = null;
        hoverX = -1;
        yMax = 2f;
        BuildBase();
        System.Array.Copy(basePixels, curvePixels, basePixels.Length);
        System.Array.Copy(basePixels, livePixels, basePixels.Length);
        LayoutAxisLabels();
        PushPixels(livePixels);
        cursorText.gameObject.SetActive(false);
        statusText.text = "Prêt";
        bandsText.text = stone == null ? "Aucune gemme dans le boîtier." : "Appuyez sur Start pour lancer la mesure.";
        analysisText.text = "";
        SetStartBusy(false);
    }

    private void FinishScan()
    {
        scanning = false;
        hasCurve = true;
        statusText.text = "Terminé";
        SetStartBusy(false);

        var lines = UvVisNirSpectrum.BandLines(result);
        var shown = Mathf.Min(lines.Count, 6);
        var text = string.Join("\n", lines.GetRange(0, shown));
        if (lines.Count > shown) text += "\n...";
        bandsText.text = text;

        if (result.Opaque)
        {
            analysisText.text = "Échantillon opaque : aucune mesure exploitable, origine non déterminée.";
            return;
        }

        var analysis = $"Éléments traces : {UvVisNirSpectrum.TraceElements(result)}";
        if (result.IronLevel != null) analysis += $"\nFer : {result.IronLevel}";
        analysis += $"\nOrigine confirmée : {stone.Origin.name}";
        analysisText.text = analysis;

        stone.RecordObservation(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString());
        ToolFeedbackUI.Instance?.ShowMessage($"Spectrophotomètre — Origine confirmée (UV-Vis-NIR) : {stone.Origin.name}");
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.45f + 0.15f * Mathf.Sin(Time.time * 14f) : 0f;
        lampGlow.color = new Color(1f, 0.82f, 0.45f, glow);

        if (scanning)
        {
            scanTime += Time.deltaTime;
            var progress = Mathf.Clamp01(scanTime / ScanSeconds);
            var index = Mathf.Min(UvVisNirResult.Points - 1, Mathf.FloorToInt(progress * (UvVisNirResult.Points - 1)));

            for (var i = lastDrawn + 1; i <= index; i++)
            {
                if (i > 0) DrawSegment(curvePixels, i - 1, i);
                lastDrawn = i;
            }

            System.Array.Copy(curvePixels, livePixels, curvePixels.Length);
            DrawCursorLine(livePixels, XOf(UvVisNirResult.MinNm + index));
            PushPixels(livePixels);

            if (progress >= 1f) FinishScan();
            return;
        }

        if (hasCurve) HandleHover();
    }

    private void HandleHover()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(plotRect, Input.mousePosition, null, out var local);
        var inside = local.x >= 0f && local.x <= PlotW - 1 && local.y <= 0f && local.y >= -(PlotH - 1);
        var px = inside ? Mathf.RoundToInt(local.x) : -1;
        if (px == hoverX) return;
        hoverX = px;

        System.Array.Copy(curvePixels, livePixels, curvePixels.Length);
        if (px < 0)
        {
            cursorText.gameObject.SetActive(false);
        }
        else
        {
            DrawCursorLine(livePixels, px);

            var wavelength = UvVisNirResult.MinNm + px / (float)(PlotW - 1) * (UvVisNirResult.MaxNm - UvVisNirResult.MinNm);
            var index = Mathf.Clamp(Mathf.RoundToInt(wavelength - UvVisNirResult.MinNm), 0, UvVisNirResult.Points - 1);
            cursorText.text = $"{wavelength:F1} nm   A = {result.Absorbance[index]:F2}";
            var x = Mathf.Clamp(PlotX + px - 60f, PlotX + 2f, PlotX + PlotW - 124f);
            RuntimeUIKit.SetTopLeft(cursorText.rectTransform, new Vector2(x, PlotY - 3f), new Vector2(122f, 16f));
            cursorText.gameObject.SetActive(true);
        }
        PushPixels(livePixels);
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

    private static int XOf(float wavelength)
    {
        return Mathf.RoundToInt((wavelength - UvVisNirResult.MinNm) / (UvVisNirResult.MaxNm - UvVisNirResult.MinNm) * (PlotW - 1));
    }

    private int YOf(float absorbance)
    {
        return Mathf.RoundToInt((absorbance - YMin) / (yMax - YMin) * (PlotH - 1));
    }

    private float TickStep => yMax <= 2f ? 0.5f : yMax <= 5f ? 1f : 2f;

    // White plot with grid, zero line and the wavelength color bar (400 to 700 nm) under the axis.
    private void BuildBase()
    {
        var white = new Color32(255, 255, 255, 255);
        for (var i = 0; i < basePixels.Length; i++) basePixels[i] = white;

        var grid = new Color32(222, 222, 226, 255);
        for (var v = TickStep; v <= yMax + 0.001f; v += TickStep)
        {
            var y = YOf(v);
            for (var x = 0; x < PlotW; x++) SetPixel(basePixels, x, y, grid);
        }

        var vertical = new Color32(70, 70, 76, 255);
        for (var nm = 400; nm <= 900; nm += 100)
        {
            var x = XOf(nm);
            for (var y = 0; y < PlotH; y++) SetPixel(basePixels, x, y, vertical);
        }

        var zero = YOf(0f);
        var black = new Color32(0, 0, 0, 255);
        for (var x = 0; x < PlotW; x++)
        {
            SetPixel(basePixels, x, zero, black);
            SetPixel(basePixels, x, zero - 1, black);
        }

        var barBottom = YOf(-0.3f);
        var barTop = YOf(-0.1f);
        for (var x = XOf(400f); x <= XOf(700f); x++)
        {
            var nm = UvVisNirResult.MinNm + x / (float)(PlotW - 1) * (UvVisNirResult.MaxNm - UvVisNirResult.MinNm);
            Color32 c = Color.HSVToRGB(Mathf.Lerp(0.78f, 0f, Mathf.InverseLerp(400f, 700f, nm)), 1f, 1f);
            for (var y = barBottom; y <= barTop; y++) SetPixel(basePixels, x, y, c);
        }

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

    private void DrawSegment(Color32[] pixels, int from, int to)
    {
        var x0 = XOf(UvVisNirResult.MinNm + from);
        var x1 = XOf(UvVisNirResult.MinNm + to);
        var y0 = Mathf.Clamp(YOf(result.Absorbance[from]), 0, PlotH - 2);
        var y1 = Mathf.Clamp(YOf(result.Absorbance[to]), 0, PlotH - 2);

        var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        var ink = new Color32(20, 20, 24, 255);
        for (var s = 0; s <= steps; s++)
        {
            var t = s / (float)steps;
            var x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            var y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            SetPixel(pixels, x, y, ink);
            SetPixel(pixels, x, y + 1, ink);
        }
    }

    private static void DrawCursorLine(Color32[] pixels, int x)
    {
        var teal = new Color32(0, 120, 120, 255);
        for (var y = 1; y < PlotH - 1; y++) SetPixel(pixels, x, y, teal);
    }

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= PlotW || y >= PlotH) return;
        pixels[y * PlotW + x] = color;
    }

    private void PushPixels(Color32[] pixels)
    {
        plotTexture.SetPixels32(pixels);
        plotTexture.Apply(false);
    }

    private void LayoutAxisLabels()
    {
        var count = 0;
        for (var v = 0f; v <= yMax + 0.001f && count < yLabels.Length; v += TickStep, count++)
        {
            var label = yLabels[count];
            label.gameObject.SetActive(true);
            label.text = v.ToString(v % 1f == 0f ? "0" : "0.0");
            var y = PlotY - (PlotH - 1 - YOf(v)) + 7f;
            RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(PlotX - 46f, y), new Vector2(40f, 14f));
        }
        for (var i = count; i < yLabels.Length; i++) yLabels[i].gameObject.SetActive(false);
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("UvVisNirPanel", typeof(RectTransform));
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
        curvePixels = new Color32[PlotW * PlotH];
        livePixels = new Color32[PlotW * PlotH];
        plotImage.texture = plotTexture;
    }

    private void BuildSoftwareWindow()
    {
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.10f, 0.28f, 0.62f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "GLGemSpec — Spectrophotomètre UV-Vis-NIR", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(30f, 0f), new Vector2(520f, 26f));
        // Little blue/red logo of the software.
        RuntimeUIKit.MakeBox("LogoA", panel.transform, new Vector2(8f, -6f), new Vector2(7f, 7f), new Color(0.2f, 0.3f, 0.9f, 1f), false);
        RuntimeUIKit.MakeBox("LogoB", panel.transform, new Vector2(15f, -6f), new Vector2(7f, 7f), new Color(0.85f, 0.15f, 0.15f, 1f), false);
        RuntimeUIKit.MakeBox("LogoC", panel.transform, new Vector2(8f, -13f), new Vector2(7f, 7f), new Color(0.15f, 0.7f, 0.25f, 1f), false);
        RuntimeUIKit.MakeBox("LogoD", panel.transform, new Vector2(15f, -13f), new Vector2(7f, 7f), new Color(0.2f, 0.3f, 0.9f, 1f), false);

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeText("Menu", panel.transform, "Fichier     Édition     Affichage     Aide     Calibration", 12, TextAnchor.MiddleLeft, Ink,
            new Vector2(10f, -28f), new Vector2(500f, 20f));

        RuntimeUIKit.MakeBox("Toolbar", panel.transform, new Vector2(0f, -50f), new Vector2(PanelWidth, 28f), new Color(0.80f, 0.81f, 0.84f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Exposition (ms) : 40      BoxCar : 5      [x] Moyenne      [x] Soustraire le fond", 12,
            TextAnchor.MiddleLeft, Ink, new Vector2(10f, -50f), new Vector2(540f, 28f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.1f, 0.3f, 0.1f),
            new Vector2(PanelWidth - 140f, -50f), new Vector2(130f, 28f));

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

        for (var nm = 300; nm <= 1000; nm += 100)
        {
            var x = PlotX + XOf(nm);
            RuntimeUIKit.MakeText("XLabel" + nm, panel.transform, nm.ToString(), 11, TextAnchor.MiddleCenter, Ink,
                new Vector2(x - 22f, PlotY - PlotH - 3f), new Vector2(44f, 14f));
        }
        RuntimeUIKit.MakeText("XTitle", panel.transform, "Longueur d'onde (nm)", 11, TextAnchor.MiddleCenter, Ink,
            new Vector2(PlotX, PlotY - PlotH - 18f), new Vector2(PlotW, 14f));

        cursorText = RuntimeUIKit.MakeText("Cursor", panel.transform, "", 11, TextAnchor.MiddleCenter, Ink, Vector2.zero, new Vector2(122f, 16f));
        cursorText.gameObject.SetActive(false);
    }

    private void BuildHardware()
    {
        const float top = -408f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 124f), new Color(0.12f, 0.13f, 0.16f, 1f), false);

        // Halogen lamp, with its fiber optic running to the box.
        var fiber = new Color(0.62f, 0.64f, 0.68f, 1f);
        RuntimeUIKit.MakeBox("FiberUp", panel.transform, new Vector2(60f, top - 8f), new Vector2(3f, 22f), fiber, false);
        RuntimeUIKit.MakeBox("FiberAcross", panel.transform, new Vector2(60f, top - 8f), new Vector2(272f, 3f), fiber, false);
        RuntimeUIKit.MakeBox("FiberDown", panel.transform, new Vector2(329f, top - 8f), new Vector2(3f, 22f), fiber, false);
        RuntimeUIKit.MakeBox("Lamp", panel.transform, new Vector2(24f, top - 22f), new Vector2(74f, 88f), new Color(0.05f, 0.05f, 0.06f, 1f), false);
        RuntimeUIKit.MakeBox("LampSwitch", panel.transform, new Vector2(34f, top - 28f), new Vector2(24f, 12f), new Color(0.22f, 0.22f, 0.26f, 1f), false);
        RuntimeUIKit.MakeBox("LampLabelBar", panel.transform, new Vector2(30f, top - 50f), new Vector2(62f, 5f), new Color(0.85f, 0.2f, 0.2f, 1f), false);
        RuntimeUIKit.MakeBox("LampLabelBar2", panel.transform, new Vector2(30f, top - 55f), new Vector2(62f, 5f), new Color(0.2f, 0.7f, 0.3f, 1f), false);
        RuntimeUIKit.MakeBox("LampLabelBar3", panel.transform, new Vector2(30f, top - 60f), new Vector2(62f, 5f), new Color(0.25f, 0.4f, 0.9f, 1f), false);
        RuntimeUIKit.MakeText("LampText", panel.transform, "GL Halogen Light\n10W 12/15V DC", 9, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.85f),
            new Vector2(30f, top - 68f), new Vector2(66f, 24f));

        // Spectrometer box: the whole black body accepts the gem, the sample compartment shows it.
        var box = RuntimeUIKit.MakeBox("Spectrometer", panel.transform, new Vector2(120f, top - 24f), new Vector2(360f, 92f), new Color(0.05f, 0.05f, 0.07f, 1f));
        box.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;

        var sticker = RuntimeUIKit.MakeBox("Sticker", panel.transform, new Vector2(132f, top - 32f), new Vector2(190f, 76f), Color.white, false);
        sticker.raycastTarget = false;
        var rainbow = new[]
        {
            new Color(0.9f, 0.15f, 0.15f), new Color(0.95f, 0.6f, 0.1f), new Color(0.95f, 0.9f, 0.2f), new Color(0.2f, 0.7f, 0.3f), new Color(0.2f, 0.4f, 0.9f)
        };
        for (var i = 0; i < rainbow.Length; i++)
            RuntimeUIKit.MakeBox("Rainbow" + i, panel.transform, new Vector2(132f + i * 38f, top - 32f), new Vector2(38f, 12f), rainbow[i], false).raycastTarget = false;
        RuntimeUIKit.MakeText("StickerTitle", panel.transform, "GL Gem Spectrometer", 13, TextAnchor.MiddleLeft, Ink,
            new Vector2(138f, top - 46f), new Vector2(180f, 18f));
        RuntimeUIKit.MakeText("StickerRange", panel.transform, "300 - 1000 nm  FWHM ~1.0 nm", 10, TextAnchor.MiddleLeft, new Color(0.25f, 0.25f, 0.3f),
            new Vector2(138f, top - 64f), new Vector2(180f, 14f));
        RuntimeUIKit.MakeText("StickerMade", panel.transform, "Gemlab Research & Technology", 9, TextAnchor.MiddleLeft, new Color(0.35f, 0.35f, 0.4f),
            new Vector2(138f, top - 80f), new Vector2(180f, 14f));

        var recess = RuntimeUIKit.MakeBox("Compartment", panel.transform, new Vector2(340f, top - 32f), new Vector2(128f, 76f), new Color(0.22f, 0.23f, 0.27f, 1f));
        recess.raycastTarget = false;
        var inner = RuntimeUIKit.MakeBox("CompartmentInner", panel.transform, new Vector2(348f, top - 40f), new Vector2(112f, 60f), new Color(0.32f, 0.33f, 0.38f, 1f));
        inner.raycastTarget = false;
        lampGlow = RuntimeUIKit.MakeBox("LampGlow", panel.transform, new Vector2(348f, top - 40f), new Vector2(112f, 60f), new Color(1f, 0.82f, 0.45f, 0f));
        lampGlow.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(382f, top - 48f), new Vector2(44f, 44f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une gemme", 10, TextAnchor.MiddleCenter, new Color(0.75f, 0.77f, 0.85f),
            new Vector2(348f, top - 40f), new Vector2(112f, 60f));

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

        RuntimeUIKit.MakeText("BandsHeader", panel.transform, "Bandes détectées", 14, TextAnchor.UpperLeft, Gold, new Vector2(14f, top - 6f), new Vector2(300f, 20f));
        bandsText = RuntimeUIKit.MakeText("Bands", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(14f, top - 28f), new Vector2(330f, 90f));

        RuntimeUIKit.MakeText("AnalysisHeader", panel.transform, "Analyse", 14, TextAnchor.UpperLeft, Gold, new Vector2(366f, top - 6f), new Vector2(300f, 20f));
        analysisText = RuntimeUIKit.MakeText("Analysis", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(366f, top - 28f), new Vector2(320f, 90f));
    }
}
