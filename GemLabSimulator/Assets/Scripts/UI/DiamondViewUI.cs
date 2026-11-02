using UnityEngine;
using UnityEngine.UI;

// DiamondView of the laboratory: drop a diamond in the compartment, press Start, and the fluorescence image under deep UV appears
// from the top down in three seconds. The growth pattern tells a natural stone (blue, uneven), a CVD synthetic (orange, fine
// concentric striations) and an HPHT synthetic (green to blue-green, cross or hourglass).
public class DiamondViewUI : MonoBehaviour
{
    public static DiamondViewUI Instance { get; private set; }

    private const float PanelWidth = 700f;
    private const float PanelHeight = 540f;
    private const int ImageW = 220;
    private const int ImageH = 165;
    private const float ImageScale = 2f;
    private const float ImageX = 16f;
    private const float ImageY = -62f;
    private const float ScanSeconds = 3f;

    private static readonly Color WindowColor = new Color(0.09f, 0.10f, 0.13f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.31f, 0.38f, 1f);
    private static readonly Color StartColor = new Color(0.20f, 0.62f, 0.30f, 1f);
    private static readonly Color StartBusyColor = new Color(0.35f, 0.40f, 0.36f, 1f);
    private static readonly Color LightText = new Color(0.86f, 0.88f, 0.92f, 1f);

    private GameObject panel;
    private RawImage fluoImage;
    private RectTransform maskRect;
    private Image maskImage;
    private Text statusText;
    private Text observationText;
    private Text interpretationText;
    private Text imageCaption;
    private Image gemImage;
    private Image uvGlow;
    private Image startImage;
    private Text startLabel;
    private Text slotHint;

    private Texture2D texture;
    private GemInstance stone;
    private DiamondViewResult result;
    private bool scanning;
    private bool hasImage;
    private float scanTime;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<DiamondViewUI>();
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
        ClearImage();
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
        ClearImage();
        Refresh();
    }

    private void RemoveStone()
    {
        stone = null;
        ClearImage();
        Refresh();
    }

    private void StartScan()
    {
        if (scanning) return;
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("DiamondView : glissez d'abord une gemme dans le compartiment.");
            return;
        }

        result = DiamondViewImage.Generate(stone, ImageW, ImageH);
        texture.SetPixels32(result.Pixels);
        texture.Apply(false);

        scanning = true;
        hasImage = false;
        scanTime = 0f;
        statusText.text = "Excitation UV...";
        observationText.text = "Acquisition en cours...";
        interpretationText.text = "";
        imageCaption.text = "";
        SetMask(1f);
        SetStartBusy(true);
    }

    private void ClearImage()
    {
        scanning = false;
        hasImage = false;
        result = null;

        var black = new Color32[ImageW * ImageH];
        var back = new Color32(4, 5, 9, 255);
        for (var i = 0; i < black.Length; i++) black[i] = back;
        texture.SetPixels32(black);
        texture.Apply(false);

        SetMask(0f);
        statusText.text = "Prêt";
        observationText.text = stone == null ? "Aucune gemme dans le compartiment." : "Appuyez sur Start pour lancer l'imagerie.";
        interpretationText.text = "";
        imageCaption.text = "";
        SetStartBusy(false);
    }

    private void FinishScan()
    {
        scanning = false;
        hasImage = true;
        SetMask(0f);
        statusText.text = "Terminé";
        SetStartBusy(false);

        observationText.text = result.Observation;
        interpretationText.text = result.Interpretation;
        imageCaption.text = "UV 225 nm  ·  x10";

        if (result.Kind == DiamondViewKind.NotDiamond)
        {
            ToolFeedbackUI.Instance?.ShowMessage("DiamondView — Aucune fluorescence exploitable : la pierre absorbe l'UV court.");
            return;
        }

        stone.RecordObservation(ExaminationTool.MeasurementType.DiamondGrowthImaging.ToString());
        ToolFeedbackUI.Instance?.ShowMessage($"DiamondView — {result.Interpretation}");
    }

    // The image is uncovered from the top down, like a camera readout: `covered` is the share still hidden (0 to 1).
    private void SetMask(float covered)
    {
        var total = ImageH * ImageScale;
        var height = Mathf.Clamp01(covered) * total;
        RuntimeUIKit.SetTopLeft(maskRect, new Vector2(ImageX, ImageY - (total - height)), new Vector2(ImageW * ImageScale, height));
        maskImage.enabled = height > 0.5f;
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var glow = scanning ? 0.4f + 0.15f * Mathf.Sin(Time.time * 14f) : 0f;
        uvGlow.color = new Color(0.55f, 0.35f, 1f, glow);

        if (!scanning) return;

        scanTime += Time.deltaTime;
        var progress = Mathf.Clamp01(scanTime / ScanSeconds);
        SetMask(1f - progress);
        if (progress >= 1f) FinishScan();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            ClearImage();
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

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("DiamondViewPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-170f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), WindowColor, false);

        texture = new Texture2D(ImageW, ImageH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        BuildSoftwareWindow();
        BuildHardware();
    }

    private void BuildSoftwareWindow()
    {
        RuntimeUIKit.MakeBox("TitleBar", panel.transform, Vector2.zero, new Vector2(PanelWidth, 26f), new Color(0.04f, 0.16f, 0.34f, 1f), false);
        RuntimeUIKit.MakeText("Title", panel.transform, "DiamondView ZOOM — De Beers Group Verification", 13, TextAnchor.MiddleLeft, Color.white,
            new Vector2(14f, 0f), new Vector2(520f, 26f));

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 13, new Color(0.78f, 0.22f, 0.22f, 1f),
            new Vector2(PanelWidth - 30f, -3f), new Vector2(26f, 20f));
        close.onClick.AddListener(Close);

        RuntimeUIKit.MakeBox("Strip", panel.transform, new Vector2(0f, -26f), new Vector2(PanelWidth, 28f), new Color(0.13f, 0.14f, 0.18f, 1f), false);
        RuntimeUIKit.MakeText("Settings", panel.transform, "Excitation : 225 nm      Grossissement : x10      Exposition : automatique", 12,
            TextAnchor.MiddleLeft, LightText, new Vector2(10f, -26f), new Vector2(480f, 28f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "Prêt", 12, TextAnchor.MiddleRight, new Color(0.5f, 0.85f, 1f),
            new Vector2(PanelWidth - 190f, -26f), new Vector2(180f, 28f));

        // Image area: a black frame, the fluorescence picture, and a cover that lifts from the top as the image is read out.
        RuntimeUIKit.MakeBox("Frame", panel.transform, new Vector2(ImageX - 2f, ImageY + 2f),
            new Vector2(ImageW * ImageScale + 4f, ImageH * ImageScale + 4f), new Color(0.35f, 0.38f, 0.45f, 1f), false);

        var picture = new GameObject("Fluorescence", typeof(RectTransform), typeof(RawImage));
        picture.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(picture.GetComponent<RectTransform>(), new Vector2(ImageX, ImageY), new Vector2(ImageW * ImageScale, ImageH * ImageScale));
        fluoImage = picture.GetComponent<RawImage>();
        fluoImage.texture = texture;
        fluoImage.raycastTarget = false;

        maskImage = RuntimeUIKit.MakeBox("Cover", panel.transform, Vector2.zero, new Vector2(10f, 10f), new Color(0.016f, 0.02f, 0.035f, 1f), false);
        maskImage.raycastTarget = false;
        maskRect = maskImage.rectTransform;

        imageCaption = RuntimeUIKit.MakeText("Caption", panel.transform, "", 11, TextAnchor.LowerLeft, new Color(0.8f, 0.85f, 1f, 0.85f),
            new Vector2(ImageX + 8f, ImageY - ImageH * ImageScale + 22f), new Vector2(200f, 16f));

        // Reading panel on the right.
        RuntimeUIKit.MakeText("ObservationHeader", panel.transform, "Observation", 14, TextAnchor.UpperLeft, Gold, new Vector2(472f, -62f), new Vector2(210f, 20f));
        RuntimeUIKit.MakeBox("ObservationBox", panel.transform, new Vector2(470f, -86f), new Vector2(214f, 134f), new Color(0.04f, 0.05f, 0.07f, 1f));
        observationText = RuntimeUIKit.MakeText("Observation", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(478f, -92f), new Vector2(198f, 122f));

        RuntimeUIKit.MakeText("InterpretationHeader", panel.transform, "Interprétation", 14, TextAnchor.UpperLeft, Gold, new Vector2(472f, -226f), new Vector2(210f, 20f));
        RuntimeUIKit.MakeBox("InterpretationBox", panel.transform, new Vector2(470f, -250f), new Vector2(214f, 134f), new Color(0.04f, 0.05f, 0.07f, 1f));
        interpretationText = RuntimeUIKit.MakeText("Interpretation", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(478f, -256f), new Vector2(198f, 122f));

        RuntimeUIKit.MakeText("Tip", panel.transform,
            "Seul le diamant laisse passer l'UV court. Observez la couleur et la forme des marques de croissance.", 11, TextAnchor.UpperLeft,
            new Color(0.6f, 0.64f, 0.7f), new Vector2(470f, -392f), new Vector2(214f, 36f));
    }

    private void BuildHardware()
    {
        const float top = -404f;
        RuntimeUIKit.MakeBox("Bench", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 136f), new Color(0.03f, 0.12f, 0.26f, 1f), false);
        RuntimeUIKit.MakeBox("BenchLight", panel.transform, new Vector2(0f, top), new Vector2(PanelWidth, 40f), new Color(0.05f, 0.18f, 0.36f, 1f), false);

        // DiamondView ZOOM: glossy black dome with the compartment open at the front-right.
        var body = RuntimeUIKit.MakeBox("Instrument", panel.transform, new Vector2(40f, top - 16f), new Vector2(360f, 108f), new Color(0.04f, 0.04f, 0.06f, 1f));
        body.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = SetStone;
        RuntimeUIKit.MakeBox("Dome", panel.transform, new Vector2(78f, top - 8f), new Vector2(250f, 50f), new Color(0.07f, 0.07f, 0.09f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("DomeShine", panel.transform, new Vector2(96f, top - 12f), new Vector2(120f, 8f), new Color(0.2f, 0.21f, 0.26f, 0.7f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Button", panel.transform, new Vector2(176f, top - 26f), new Vector2(40f, 14f), new Color(0.1f, 0.1f, 0.13f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeText("Logo", panel.transform, "DiamondView ZOOM", 11, TextAnchor.MiddleCenter, new Color(0.9f, 0.92f, 1f),
            new Vector2(116f, top - 48f), new Vector2(150f, 16f));
        RuntimeUIKit.MakeText("Maker", panel.transform, "DE BEERS GROUP VERIFICATION", 8, TextAnchor.MiddleCenter, new Color(0.7f, 0.74f, 0.85f),
            new Vector2(106f, top - 64f), new Vector2(170f, 12f));

        // Compartment: recess, stage with the stone, and the drawer lip in front.
        var recess = RuntimeUIKit.MakeBox("Compartment", panel.transform, new Vector2(240f, top - 52f), new Vector2(136f, 52f), new Color(0.015f, 0.015f, 0.02f, 1f));
        recess.raycastTarget = false;
        uvGlow = RuntimeUIKit.MakeBox("UvGlow", panel.transform, new Vector2(244f, top - 54f), new Vector2(128f, 46f), new Color(0.55f, 0.35f, 1f, 0f));
        uvGlow.raycastTarget = false;
        RuntimeUIKit.MakeBox("Stage", panel.transform, new Vector2(288f, top - 76f), new Vector2(38f, 10f), new Color(0.16f, 0.16f, 0.2f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Lip", panel.transform, new Vector2(232f, top - 90f), new Vector2(156f, 12f), new Color(0.1f, 0.1f, 0.13f, 1f)).raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(295f, top - 50f), new Vector2(24f, 24f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        slotHint = RuntimeUIKit.MakeText("SlotHint", panel.transform, "Glissez un diamant", 10, TextAnchor.MiddleCenter, new Color(0.65f, 0.7f, 0.85f),
            new Vector2(244f, top - 54f), new Vector2(128f, 46f));

        var start = RuntimeUIKit.MakeButton("Start", panel.transform, "Start", 18, StartColor, new Vector2(520f, top - 22f), new Vector2(150f, 46f));
        startImage = start.GetComponent<Image>();
        startLabel = start.GetComponentInChildren<Text>();
        start.onClick.AddListener(StartScan);

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor, new Vector2(520f, top - 76f), new Vector2(150f, 26f));
        remove.onClick.AddListener(RemoveStone);
    }
}
