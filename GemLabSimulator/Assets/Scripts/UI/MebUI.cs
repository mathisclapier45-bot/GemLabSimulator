using UnityEngine;
using UnityEngine.UI;

// Scanning electron microscope (MEB) of the laboratory: drop a stone in the chamber, choose a signal (secondary electrons, cathodoluminescence
// or X-ray analysis) and a magnification, press Start. The chamber is pumped down and the stone coated with gold, then the picture is
// read out: line by line for an image, left to right for the X-ray spectrum.
public class MebUI : MonoBehaviour
{
    public static MebUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 556f;
    private const int ImageW = MebResult.ImageWidth;
    private const int ImageH = MebResult.ImageHeight;
    private const int PlotW = 440;
    private const int PlotH = 330;
    private const float ImageScale = 2f;
    private const float ImageX = 16f;
    private const float ImageY = -62f;
    private const float PreparationSeconds = 0.9f;
    private const float ReadoutSeconds = 2.1f;
    private const int SpectrumLeft = 24;
    private const int SpectrumRight = 430;
    private const int SpectrumBase = 34;
    private const int SpectrumTop = 316;
    private const int MaxPeakLabels = 12;

    private static readonly int[] Magnifications = { 1000, 5000, 20000 };
    private static readonly Color WindowColor = new Color(0.10f, 0.11f, 0.13f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color SelectedColor = new Color(0.16f, 0.46f, 0.30f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color LightText = new Color(0.86f, 0.88f, 0.92f, 1f);

    private GameObject panel;
    private RawImage display;
    private Image maskImage;
    private RectTransform maskRect;
    private Text statusText;
    private Text observationText;
    private Text interpretationText;
    private Text captionText;
    private Image scaleBar;
    private Text scaleText;
    private Text[] tickLabels;
    private Text[] peakLabels;
    private Text axisTitle;
    private Image gemImage;
    private Image beamGlow;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;
    private Image[] modeImages;
    private Image[] magImages;
    private Text magHeader;

    private Texture2D imageTexture;
    private Texture2D plotTexture;
    private Color32[] plotPixels;

    private GemInstance stone;
    private MebResult result;
    private MebMode mode = MebMode.Surface;
    private int magIndex = 1;
    private bool scanning;
    private bool hasResult;
    private float scanTime;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<MebUI>();
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
        ClearDisplay();
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
        ClearDisplay();
        Refresh();
    }

    private void RemoveStone()
    {
        stone = null;
        ClearDisplay();
        Refresh();
    }

    private void SetMode(MebMode newMode)
    {
        if (scanning) return;
        mode = newMode;
        UpdateSelectors();
        ClearDisplay();
    }

    private void SetMagnification(int index)
    {
        if (scanning) return;
        magIndex = index;
        UpdateSelectors();
        if (mode == MebMode.Surface) ClearDisplay();
    }

    private void UpdateSelectors()
    {
        for (var i = 0; i < modeImages.Length; i++) modeImages[i].color = i == (int)mode ? SelectedColor : ButtonColor;
        var usesMagnification = mode == MebMode.Surface;
        for (var i = 0; i < magImages.Length; i++)
            magImages[i].color = !usesMagnification ? new Color(0.2f, 0.21f, 0.25f, 1f) : i == magIndex ? SelectedColor : ButtonColor;
        magHeader.color = usesMagnification ? Gold : new Color(0.45f, 0.45f, 0.5f);
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("MEB : glissez d'abord une pierre dans la chambre.");
            return;
        }

        result = MebImaging.Measure(stone, mode, Magnifications[magIndex]);
        PrepareDisplay(result);

        scanning = true;
        hasResult = false;
        scanTime = 0f;
        statusText.text = "Mise sous vide...";
        observationText.text = "Préparation de l'échantillon...";
        interpretationText.text = "";
        HideOverlays();
        SetMask(1f, mode == MebMode.XRay);
        SetStartBusy(true);
    }

    // Pushes the measurement to the right texture and shows the matching overlays.
    private void PrepareDisplay(MebResult r)
    {
        if (r.Mode == MebMode.XRay)
        {
            DrawSpectrum(r);
            display.texture = plotTexture;
        }
        else
        {
            imageTexture.SetPixels32(r.Pixels);
            imageTexture.Apply(false);
            display.texture = imageTexture;
        }
    }

    private void ClearDisplay()
    {
        scanning = false;
        hasResult = false;
        result = null;

        var black = new Color32[ImageW * ImageH];
        var back = new Color32(6, 7, 11, 255);
        for (var i = 0; i < black.Length; i++) black[i] = back;
        imageTexture.SetPixels32(black);
        imageTexture.Apply(false);
        display.texture = imageTexture;

        SetMask(0f, false);
        HideOverlays();
        statusText.text = "Prêt";
        observationText.text = stone == null ? "Aucune pierre dans la chambre." : "Choisissez un signal, puis appuyez sur Start.";
        interpretationText.text = "";
        SetStartBusy(false);
        UpdateSelectors();
    }

    private void FinishScan()
    {
        scanning = false;
        hasResult = true;
        SetMask(0f, false);
        statusText.text = "Terminé";
        SetStartBusy(false);

        observationText.text = result.Observation;
        interpretationText.text = result.Interpretation;
        ShowOverlays();

        if (result.RecordGrowth)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.GrowthStructure.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"MEB — Structure de croissance : {stone.GrowthStructureDescription}");
        }
        else if (result.RecordTreatment)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"MEB — Traitement détecté : {stone.TreatmentGrade} — {stone.TreatmentDescription}");
        }
        else
        {
            ToolFeedbackUI.Instance?.ShowMessage($"MEB — {result.Observation}");
        }
    }

    // `covered` (0 to 1) is the share of the picture still hidden: from the bottom for an image, from the right for the spectrum.
    private void SetMask(float covered, bool horizontal)
    {
        var totalW = PlotW;
        var totalH = PlotH;
        var share = Mathf.Clamp01(covered);
        if (horizontal)
        {
            var width = share * totalW;
            RuntimeUIKit.SetTopLeft(maskRect, new Vector2(ImageX + (totalW - width), ImageY), new Vector2(width, totalH));
            maskImage.enabled = width > 0.5f;
        }
        else
        {
            var height = share * totalH;
            RuntimeUIKit.SetTopLeft(maskRect, new Vector2(ImageX, ImageY - (totalH - height)), new Vector2(totalW, height));
            maskImage.enabled = height > 0.5f;
        }
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning && scanTime > PreparationSeconds ? 0.5f + 0.2f * Mathf.Sin(Time.time * 18f) : 0f;
        beamGlow.color = new Color(0.6f, 0.8f, 1f, glow);

        if (!scanning) return;

        scanTime += Time.deltaTime;
        if (scanTime < PreparationSeconds * 0.55f) statusText.text = "Mise sous vide...";
        else if (scanTime < PreparationSeconds) statusText.text = "Métallisation (or)...";
        else statusText.text = "Balayage...";

        var progress = Mathf.Clamp01((scanTime - PreparationSeconds) / ReadoutSeconds);
        SetMask(1f - progress, mode == MebMode.XRay);
        if (progress >= 1f) FinishScan();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            ClearDisplay();
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

    // ---- Overlays ----------------------------------------------------------------------------------------------

    private void HideOverlays()
    {
        captionText.text = "";
        scaleBar.gameObject.SetActive(false);
        scaleText.gameObject.SetActive(false);
        axisTitle.gameObject.SetActive(false);
        foreach (var t in tickLabels) t.gameObject.SetActive(false);
        foreach (var t in peakLabels) t.gameObject.SetActive(false);
    }

    private void ShowOverlays()
    {
        if (result.Mode == MebMode.XRay)
        {
            axisTitle.gameObject.SetActive(true);
            for (var i = 0; i < tickLabels.Length; i++)
            {
                var e = i * 2f;
                tickLabels[i].gameObject.SetActive(true);
                tickLabels[i].text = e.ToString("0");
                RuntimeUIKit.SetTopLeft(tickLabels[i].rectTransform, new Vector2(ImageX + SpectrumX(e) - 20f, ImageY - (PlotH - SpectrumBase) - 3f), new Vector2(40f, 14f));
            }

            // Labels above the strongest lines.
            var count = 0;
            foreach (var peak in result.Peaks)
            {
                if (count >= MaxPeakLabels) break;
                if (peak.Height < 250f) continue;
                var label = peakLabels[count++];
                label.gameObject.SetActive(true);
                label.text = peak.Symbol;
                var py = SpectrumY(peak.Height + 70f * Mathf.Exp(-peak.KeV / 2.6f) + 12f);
                RuntimeUIKit.SetTopLeft(label.rectTransform, new Vector2(ImageX + SpectrumX(peak.KeV) - 20f, ImageY - (PlotH - py) - 18f), new Vector2(40f, 14f));
            }
            captionText.text = "Analyse X (EDS)  ·  15 kV";
            return;
        }

        var mag = Magnifications[magIndex];
        captionText.text = result.Mode == MebMode.Surface ? $"SE  ·  x{mag:N0}  ·  15 kV" : "Cathodoluminescence  ·  15 kV";

        if (result.Mode == MebMode.Surface)
        {
            // Scale bar: the nicest length that gives a bar between 40 and 120 pixels.
            var candidates = new[] { 0.5f, 1f, 2f, 5f, 10f, 20f };
            var chosen = candidates[0];
            foreach (var length in candidates)
            {
                var pixels = length / result.FieldWidthMicrons * PlotW;
                if (pixels >= 40f && pixels <= 120f) chosen = length;
                else if (pixels < 40f) chosen = length;
            }
            var barPixels = Mathf.Clamp(chosen / result.FieldWidthMicrons * PlotW, 12f, 160f);
            scaleBar.gameObject.SetActive(true);
            scaleText.gameObject.SetActive(true);
            RuntimeUIKit.SetTopLeft(scaleBar.rectTransform, new Vector2(ImageX + 14f, ImageY - PlotH + 22f), new Vector2(barPixels, 4f));
            scaleText.text = chosen < 1f ? $"{chosen * 1000f:0} nm" : $"{chosen:0.#} µm";
            RuntimeUIKit.SetTopLeft(scaleText.rectTransform, new Vector2(ImageX + 14f, ImageY - PlotH + 40f), new Vector2(100f, 14f));
        }
    }

    // ---- X-ray spectrum drawing --------------------------------------------------------------------------------------

    private static int SpectrumX(float keV)
    {
        return Mathf.RoundToInt(SpectrumLeft + keV / MebResult.MaxKeV * (SpectrumRight - SpectrumLeft));
    }

    private float spectrumMax = 1f;

    private int SpectrumY(float counts)
    {
        return Mathf.RoundToInt(SpectrumBase + counts / spectrumMax * (SpectrumTop - SpectrumBase));
    }

    private void DrawSpectrum(MebResult r)
    {
        var back = new Color32(8, 9, 14, 255);
        for (var i = 0; i < plotPixels.Length; i++) plotPixels[i] = back;

        spectrumMax = 1f;
        foreach (var v in r.Spectrum) spectrumMax = Mathf.Max(spectrumMax, v);
        spectrumMax *= 1.18f;

        var grid = new Color32(34, 36, 46, 255);
        for (var e = 0; e <= 10; e += 2)
        {
            var x = SpectrumX(e);
            for (var y = SpectrumBase; y <= SpectrumTop; y++) SetPlotPixel(x, y, grid);
        }

        var axis = new Color32(150, 152, 165, 255);
        for (var x = SpectrumLeft; x <= SpectrumRight; x++)
        {
            SetPlotPixel(x, SpectrumBase, axis);
            SetPlotPixel(x, SpectrumBase - 1, axis);
        }
        for (var e = 0; e <= 10; e++)
            for (var y = SpectrumBase - 6; y < SpectrumBase; y++) SetPlotPixel(SpectrumX(e), y, axis);

        var ink = new Color32(240, 240, 120, 255);
        var prevX = SpectrumX(0f);
        var prevY = SpectrumY(r.Spectrum[0]);
        for (var i = 1; i < r.Spectrum.Length; i++)
        {
            var x = SpectrumX(i * 0.01f);
            var y = Mathf.Clamp(SpectrumY(r.Spectrum[i]), SpectrumBase + 1, PlotH - 2);
            var steps = Mathf.Max(Mathf.Abs(x - prevX), Mathf.Abs(y - prevY), 1);
            for (var s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                SetPlotPixel(Mathf.RoundToInt(Mathf.Lerp(prevX, x, t)), Mathf.RoundToInt(Mathf.Lerp(prevY, y, t)), ink);
            }
            prevX = x;
            prevY = y;
        }

        plotTexture.SetPixels32(plotPixels);
        plotTexture.Apply(false);
    }

    private void SetPlotPixel(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= PlotW || y >= PlotH) return;
        plotPixels[y * PlotW + x] = color;
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("MebPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-170f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), WindowColor, false);

        imageTexture = new Texture2D(ImageW, ImageH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        plotTexture = new Texture2D(PlotW, PlotH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        plotPixels = new Color32[PlotW * PlotH];

        BuildSoftwareWindow();
        BuildSidePanel();
        BuildHardware();
    }

    private void BuildSoftwareWindow()
    {
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.22f, 0.24f, 0.28f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "MEB — Microscope électronique à balayage", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(14f, 0f), new Vector2(520f, 26f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeBox("Strip", panel.transform, new Vector2(0f, -26f), new Vector2(PanelWidth, 28f), new Color(0.15f, 0.16f, 0.19f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Tension : 15 kV      Distance de travail : 10 mm      Vide : 10-5 Pa", 12,
            TextAnchor.MiddleLeft, LightText, new Vector2(10f, -26f), new Vector2(460f, 28f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.5f, 0.85f, 1f),
            new Vector2(PanelWidth - 190f, -26f), new Vector2(180f, 28f));

        RuntimeUIKit.MakeBox("Frame", panel.transform, new Vector2(ImageX - 2f, ImageY + 2f), new Vector2(PlotW + 4f, PlotH + 4f), new Color(0.35f, 0.38f, 0.45f, 1f), false);

        var picture = new GameObject("Display", typeof(RectTransform), typeof(RawImage));
        picture.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(picture.GetComponent<RectTransform>(), new Vector2(ImageX, ImageY), new Vector2(PlotW, PlotH));
        display = picture.GetComponent<RawImage>();
        display.texture = imageTexture;
        display.raycastTarget = false;

        maskImage = RuntimeUIKit.MakeBox("Cover", panel.transform, Vector2.zero, new Vector2(10f, 10f), new Color(0.024f, 0.027f, 0.043f, 1f), false);
        maskImage.raycastTarget = false;
        maskRect = maskImage.rectTransform;

        captionText = RuntimeUIKit.MakeText("Caption", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 1f, 0.9f),
            new Vector2(ImageX + 8f, ImageY - 6f), new Vector2(300f, 16f));

        var barGo = RuntimeUIKit.MakeBox("ScaleBar", panel.transform, Vector2.zero, new Vector2(60f, 4f), Color.white, false);
        barGo.raycastTarget = false;
        scaleBar = barGo;
        scaleText = RuntimeUIKit.MakeText("ScaleText", panel.transform, "", 12, TextAnchor.MiddleLeft, Color.white, Vector2.zero, new Vector2(100f, 14f));

        tickLabels = new Text[6];
        for (var i = 0; i < tickLabels.Length; i++)
            tickLabels[i] = RuntimeUIKit.MakeText("Tick" + i, panel.transform, "", 11, TextAnchor.MiddleCenter, LightText, Vector2.zero, new Vector2(40f, 14f));
        peakLabels = new Text[MaxPeakLabels];
        for (var i = 0; i < peakLabels.Length; i++)
            peakLabels[i] = RuntimeUIKit.MakeText("PeakLabel" + i, panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.6f), Vector2.zero, new Vector2(40f, 14f));
        axisTitle = RuntimeUIKit.MakeText("AxisTitle", panel.transform, "Énergie (keV)", 11, TextAnchor.MiddleCenter, LightText,
            new Vector2(ImageX + SpectrumLeft, ImageY - (PlotH - SpectrumBase) - 17f), new Vector2(SpectrumRight - SpectrumLeft, 14f));
        HideOverlays();
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("ModeHeader", panel.transform, "Signal", 14, TextAnchor.UpperLeft, Gold, new Vector2(472f, -60f), new Vector2(200f, 20f));
        var names = new[] { "Électrons secondaires", "Cathodoluminescence", "Analyse X (EDS)" };
        modeImages = new Image[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Mode" + i, panel.transform, names[i], 12, i == 0 ? SelectedColor : ButtonColor,
                new Vector2(470f, -82f - i * 30f), new Vector2(214f, 26f));
            modeImages[i] = button.GetComponent<Image>();
            button.onClick.AddListener(() => SetMode((MebMode)index));
        }

        magHeader = RuntimeUIKit.MakeText("MagHeader", panel.transform, "Grossissement", 12, TextAnchor.UpperLeft, Gold, new Vector2(472f, -176f), new Vector2(200f, 16f));
        magImages = new Image[Magnifications.Length];
        for (var i = 0; i < Magnifications.Length; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Mag" + i, panel.transform, $"x{Magnifications[i]:N0}", 11, i == magIndex ? SelectedColor : ButtonColor,
                new Vector2(470f + i * 73f, -196f), new Vector2(68f, 24f));
            magImages[i] = button.GetComponent<Image>();
            button.onClick.AddListener(() => SetMagnification(index));
        }

        RuntimeUIKit.MakeText("ObservationHeader", panel.transform, "Observation", 13, TextAnchor.UpperLeft, Gold, new Vector2(472f, -228f), new Vector2(200f, 18f));
        RuntimeUIKit.MakeBox("ObservationBox", panel.transform, new Vector2(470f, -248f), new Vector2(214f, 72f), new Color(0.04f, 0.05f, 0.07f, 1f));
        observationText = RuntimeUIKit.MakeText("Observation", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(476f, -252f), new Vector2(202f, 66f));

        RuntimeUIKit.MakeText("InterpretationHeader", panel.transform, "Interprétation", 13, TextAnchor.UpperLeft, Gold, new Vector2(472f, -324f), new Vector2(200f, 18f));
        RuntimeUIKit.MakeBox("InterpretationBox", panel.transform, new Vector2(470f, -344f), new Vector2(214f, 72f), new Color(0.04f, 0.05f, 0.07f, 1f));
        interpretationText = RuntimeUIKit.MakeText("Interpretation", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(476f, -348f), new Vector2(202f, 66f));
    }

    private void BuildHardware()
    {
        const float top = -420f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 136f), new Color(0.16f, 0.17f, 0.20f, 1f), false);

        // The microscope: a white column on top of a chamber with a round door, and the control screens on the right.
        RuntimeUIKit.MakeBox("ColumnTop", panel.transform, new Vector2(110f, top - 4f), new Vector2(46f, 30f), new Color(0.92f, 0.92f, 0.90f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("ColumnRing", panel.transform, new Vector2(106f, top - 26f), new Vector2(54f, 6f), new Color(0.72f, 0.74f, 0.76f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("ColumnMid", panel.transform, new Vector2(110f, top - 32f), new Vector2(46f, 28f), new Color(0.92f, 0.92f, 0.90f, 1f)).raycastTarget = false;

        var chamber = RuntimeUIKit.MakeBox("Chamber", panel.transform, new Vector2(60f, top - 58f), new Vector2(210f, 66f), new Color(0.90f, 0.90f, 0.88f, 1f));
        chamber.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;
        RuntimeUIKit.MakeBox("Door", panel.transform, new Vector2(150f, top - 66f), new Vector2(100f, 50f), new Color(0.78f, 0.80f, 0.82f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("DoorWindow", panel.transform, new Vector2(166f, top - 72f), new Vector2(68f, 38f), new Color(0.05f, 0.05f, 0.07f, 1f)).raycastTarget = false;
        beamGlow = RuntimeUIKit.MakeBox("BeamGlow", panel.transform, new Vector2(166f, top - 72f), new Vector2(68f, 38f), new Color(0.6f, 0.8f, 1f, 0f));
        beamGlow.raycastTarget = false;
        RuntimeUIKit.MakeBox("DoorHandle", panel.transform, new Vector2(240f, top - 84f), new Vector2(8f, 22f), new Color(0.5f, 0.52f, 0.56f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Detector", panel.transform, new Vector2(70f, top - 70f), new Vector2(70f, 14f), new Color(0.55f, 0.57f, 0.6f, 1f)).raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(184f, top - 74f), new Vector2(32f, 32f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez une pierre", 10, TextAnchor.MiddleCenter, new Color(0.65f, 0.7f, 0.8f),
            new Vector2(166f, top - 72f), new Vector2(68f, 38f));

        for (var i = 0; i < 3; i++)
        {
            RuntimeUIKit.MakeBox("Monitor" + i, panel.transform, new Vector2(300f + i * 56f, top - 30f), new Vector2(50f, 38f), new Color(0.04f, 0.05f, 0.08f, 1f)).raycastTarget = false;
            RuntimeUIKit.MakeBox("MonitorStand" + i, panel.transform, new Vector2(320f + i * 56f, top - 68f), new Vector2(10f, 8f), new Color(0.3f, 0.3f, 0.34f, 1f), false).raycastTarget = false;
        }
        RuntimeUIKit.MakeBox("Panel", panel.transform, new Vector2(300f, top - 82f), new Vector2(110f, 18f), new Color(0.85f, 0.85f, 0.83f, 1f)).raycastTarget = false;

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(520f, top - 20f), new Vector2(150f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la pierre", 13, ButtonColor, new Vector2(520f, top - 74f), new Vector2(150f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }
}
