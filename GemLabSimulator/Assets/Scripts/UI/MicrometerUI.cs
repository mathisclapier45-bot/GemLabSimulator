using UnityEngine;
using UnityEngine.UI;

// Electronic gemstone gauge: drop a gem between the jaws, choose its position (length, width or depth), then
// close the jaws to measure. Each reading shows on the dial and is logged in the side panel; the length/width
// ratio appears once both are known.
public class MicrometerUI : MonoBehaviour
{
    public static MicrometerUI Instance { get; private set; }

    private enum Position { Length, Width, Depth }

    private const float PanelWidth = 600f;
    private const float PanelHeight = 470f;
    private const float PxPerMm = 6f;
    private const float JawSpeed = 360f;          // pixels per second
    private const float MaxGap = 280f;            // fully open jaws
    private const float FixedJawX = 20f;
    private const float JawWidth = 14f;
    private const float JawTop = -48f;
    private const float JawHeight = 116f;

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Silver = new Color(0.80f, 0.82f, 0.86f, 1f);
    private static readonly Color ButtonOff = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color ButtonOn = new Color(0.30f, 0.55f, 0.35f, 1f);
    private static readonly Color Ink = new Color(0.08f, 0.10f, 0.10f, 1f);
    private static readonly Color Ghost = new Color(0.08f, 0.10f, 0.10f, 0.07f);

    private GameObject panel;
    private Image gemImage;
    private GemView3D view3D;
    private RectTransform gemRect;
    private RectTransform movingJaw;
    private Text hintText;
    private Text readingsText;
    private Button measureButton;
    private SevenSegmentDisplay display;
    private readonly Button[] positionButtons = new Button[3];

    private GemInstance stone;
    private Position position = Position.Length;
    private float gap = MaxGap;
    private float targetGap = MaxGap;
    private bool measuring;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<MicrometerUI>();
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
        position = Position.Length;
        gap = targetGap = MaxGap;
        measuring = false;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Geometry --------------------------------------------------------------------------------------------

    private string KeyOf(Position p)
    {
        return p == Position.Length ? DimensionReport.LengthKey : p == Position.Width ? DimensionReport.WidthKey : DimensionReport.DepthKey;
    }

    private float ValueOf(Position p)
    {
        if (stone == null) return 0f;
        var mm = p == Position.Length ? stone.LengthMm : p == Position.Width ? stone.WidthMm : stone.DepthMm;
        return Mathf.Round(mm * 100f) / 100f;
    }

    // The stone is drawn in 3D pixel art, turned so that the chosen dimension lies across the jaws.
    private void LayoutStone()
    {
        if (stone == null) return;

        var l = stone.LengthMm * PxPerMm;
        var w = stone.WidthMm * PxPerMm;
        var d = stone.DepthMm * PxPerMm;

        Quaternion orientation;
        float across, vertical;
        switch (position)
        {
            case Position.Width:
                orientation = Quaternion.Euler(0f, 0f, 90f);     // quarter turn: the width lies across the jaws
                across = w;
                vertical = l;
                break;
            case Position.Depth:
                orientation = Quaternion.Euler(0f, 90f, 0f);     // on its side: the depth lies across the jaws
                across = d;
                vertical = w;
                break;
            default:
                orientation = Quaternion.identity;
                across = l;
                vertical = w;
                break;
        }

        GemView3D.Colors(stone, out var body, out var facet);
        view3D.Draw(GemView3D.MeshFor(stone), GemView3D.HalfExtents(stone), orientation, body, facet, stone.Definition.Diaphaneity);

        var side = GemView3D.MaxDimensionMm(stone) * PxPerMm / GemView3D.FitFraction;
        gemRect.sizeDelta = new Vector2(side, side);
        gemRect.localEulerAngles = Vector3.zero;
        // Rests against the fixed jaw, on the bottom of the jaws.
        gemRect.anchoredPosition = new Vector2(FixedJawX + JawWidth + across / 2f, JawTop - JawHeight + 8f + vertical / 2f);
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        measuring = false;
        targetGap = MaxGap;
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        measuring = false;
        targetGap = MaxGap;
        Refresh();
    }

    private void SelectPosition(Position p)
    {
        position = p;
        measuring = false;
        targetGap = MaxGap;     // reopen so the stone can be turned
        Refresh();
    }

    private void Measure()
    {
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Micromètre : déposez d'abord une gemme entre les mâchoires.");
            return;
        }

        measuring = true;
        targetGap = ValueOf(position) * PxPerMm;
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (!Mathf.Approximately(gap, targetGap))
        {
            gap = Mathf.MoveTowards(gap, targetGap, JawSpeed * Time.deltaTime);
            ApplyJaw();

            if (measuring && Mathf.Approximately(gap, targetGap)) FinishMeasurement();
        }
    }

    private void FinishMeasurement()
    {
        measuring = false;
        var value = ValueOf(position);
        stone.RecordMeasurement(KeyOf(position), value);
        stone.RecordObservation($"Dim{position}");

        if (stone.HasMeasured(DimensionReport.LengthKey) && stone.HasMeasured(DimensionReport.WidthKey)
            && stone.HasMeasured(DimensionReport.DepthKey))
            stone.RecordObservation(ExaminationTool.MeasurementType.Dimensions.ToString());

        var label = position == Position.Length ? "Longueur" : position == Position.Width ? "Largeur" : "Profondeur";
        ToolFeedbackUI.Instance?.ShowMessage($"Micromètre — {label} : {value:F2} mm");
        UpdateReadings();
    }

    private void ApplyJaw()
    {
        RuntimeUIKit.SetTopLeft(movingJaw, new Vector2(FixedJawX + JawWidth + gap, JawTop), new Vector2(JawWidth, JawHeight));
        // The dial always shows the opening between the jaws, in hundredths of a millimeter.
        display.ShowNumber(Mathf.RoundToInt(gap / PxPerMm * 100f), 1);
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            measuring = false;
            targetGap = MaxGap;
        }

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = "Déposez une gemme entre les mâchoires";
        }
        else
        {
            gemImage.enabled = true;
            hintText.text = "";
            LayoutStone();
        }

        for (var i = 0; i < positionButtons.Length; i++)
            positionButtons[i].GetComponent<Image>().color = (int)position == i ? ButtonOn : ButtonOff;

        ApplyJaw();
        UpdateReadings();
    }

    private void UpdateReadings()
    {
        if (stone == null)
        {
            readingsText.text = "—";
            return;
        }

        var lines = DimensionReport.Lines(stone);
        var text = "";
        foreach (var line in lines) text += line + "\n";
        readingsText.text = text.Length == 0 ? "Aucune mesure prise." : text.TrimEnd('\n');
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
        panel = new GameObject("MicrometerPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Micromètre", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildGauge();
        BuildSidePanel();
    }

    private void BuildGauge()
    {
        // Silver caliper: top bar, fixed jaw, moving jaw.
        var zone = RuntimeUIKit.MakeBox("DropZone", panel.transform, new Vector2(FixedJawX, JawTop), new Vector2(340f, JawHeight + 6f), new Color(0.04f, 0.05f, 0.07f, 1f));
        zone.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        zone.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        RuntimeUIKit.MakeBox("Bar", panel.transform, new Vector2(FixedJawX, JawTop), new Vector2(340f, 16f), Silver).raycastTarget = false;
        RuntimeUIKit.MakeBox("FixedJaw", panel.transform, new Vector2(FixedJawX, JawTop), new Vector2(JawWidth, JawHeight), Silver).raycastTarget = false;
        var jaw = RuntimeUIKit.MakeBox("MovingJaw", panel.transform, new Vector2(FixedJawX + JawWidth + MaxGap, JawTop), new Vector2(JawWidth, JawHeight), Silver);
        jaw.raycastTarget = false;
        movingJaw = jaw.rectTransform;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        gemRect = gem.GetComponent<RectTransform>();
        gemRect.anchorMin = gemRect.anchorMax = new Vector2(0f, 1f);
        gemRect.pivot = new Vector2(0.5f, 0.5f);
        gemImage = gem.GetComponent<Image>();
        gemImage.raycastTarget = false;
        gemImage.preserveAspect = false;
        gemImage.enabled = false;
        view3D = new GemView3D(48);
        gemImage.sprite = view3D.Sprite;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.55f, 0.58f, 0.65f),
            new Vector2(FixedJawX + JawWidth, JawTop - 50f), new Vector2(300f, 18f));

        // Handle and round body with the dial.
        RuntimeUIKit.MakeBox("Handle", panel.transform, new Vector2(150f, JawTop - JawHeight - 4f), new Vector2(60f, 44f), new Color(0.06f, 0.06f, 0.07f, 1f));
        var body = new GameObject("DialBody", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(body.GetComponent<RectTransform>(), new Vector2(75f, -208f), new Vector2(210f, 210f));
        var bodyImage = body.GetComponent<Image>();
        bodyImage.sprite = MakeDiscSprite(128);
        bodyImage.raycastTarget = false;

        RuntimeUIKit.MakeText("Brand", panel.transform, "GEMLAB", 17, TextAnchor.MiddleCenter, Color.white,
            new Vector2(95f, -262f), new Vector2(170f, 24f)).fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", panel.transform, "Electronic Gemstone Gauge", 10, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f),
            new Vector2(95f, -284f), new Vector2(170f, 16f));

        RuntimeUIKit.MakeBox("LcdFrame", panel.transform, new Vector2(112f, -318f), new Vector2(136f, 50f), new Color(0.78f, 0.80f, 0.82f, 1f));
        RuntimeUIKit.MakeBox("Lcd", panel.transform, new Vector2(116f, -322f), new Vector2(128f, 42f), new Color(0.62f, 0.66f, 0.64f, 1f), false);
        display = new SevenSegmentDisplay(panel.transform, new Vector2(122f, -326f), 4, 18f, 30f, 6f, 3f, Ink, Ghost);
        RuntimeUIKit.MakeText("Unit", panel.transform, "mm", 11, TextAnchor.UpperRight, Ink, new Vector2(212f, -324f), new Vector2(28f, 14f)).fontStyle = FontStyle.Bold;
        display.ShowNumber(0, 1);
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("PositionHeader", panel.transform, "Position de la gemme", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(380f, -44f), new Vector2(204f, 20f));

        var labels = new[] { "Longueur", "Largeur", "Profondeur (sur la tranche)" };
        for (var i = 0; i < 3; i++)
        {
            var captured = (Position)i;
            positionButtons[i] = RuntimeUIKit.MakeButton("Pos" + i, panel.transform, labels[i], 13, ButtonOff,
                new Vector2(380f, -68f - i * 34f), new Vector2(204f, 28f));
            positionButtons[i].onClick.AddListener(() => SelectPosition(captured));
        }

        measureButton = RuntimeUIKit.MakeButton("Measure", panel.transform, "Mesurer", 15, new Color(0.30f, 0.42f, 0.65f, 1f),
            new Vector2(380f, -176f), new Vector2(204f, 36f));
        measureButton.onClick.AddListener(Measure);

        RuntimeUIKit.MakeText("ReadingsHeader", panel.transform, "Mesures", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(380f, -226f), new Vector2(204f, 20f));
        RuntimeUIKit.MakeBox("ReadingsBox", panel.transform, new Vector2(376f, -250f), new Vector2(212f, 126f), new Color(0.04f, 0.05f, 0.07f, 1f));
        readingsText = RuntimeUIKit.MakeText("Readings", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(386f, -256f), new Vector2(196f, 116f));

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonOff,
            new Vector2(380f, -388f), new Vector2(204f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonOff,
            new Vector2(380f, -422f), new Vector2(204f, 28f));
        close.onClick.AddListener(Close);
    }

    // Black round gauge body with a thin light ring.
    public static Sprite MakeDiscSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                var shade = r > 0.86f ? 0.05f : 0.09f + 0.02f * (-dx - dy) * 0.5f;
                if (r > 0.82f && r < 0.845f) shade = 0.55f;                // thin light ring
                texture.SetPixel(x, y, new Color(shade, shade, shade * 1.1f, Mathf.Clamp01((1f - r) * center)));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
