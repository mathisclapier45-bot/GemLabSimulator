using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Spectroscope: drop a gem on the stage, its absorption spectrum appears, the absorption lines are listed on the
// side (in nm) and hovering the spectrum shows the wavelength under the mouse.
public class SpectroscopeUI : MonoBehaviour
{
    public static SpectroscopeUI Instance { get; private set; }

    private const float PanelWidth = 620f;
    private const float PanelHeight = 456f;
    private const float SpectrumX = 28f;
    private const float SpectrumY = -180f;
    private const float SpectrumWidth = 384f;
    private const float SpectrumHeight = 96f;

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private Image gemImage;
    private Text hintText;
    private Image spectrumImage;
    private RectTransform spectrumRect;
    private Image marker;
    private Text hoverLabel;
    private Text linesText;
    private Text descriptionText;
    private Image darkWindow;

    private GemInstance stone;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<SpectroscopeUI>();
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

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        UpdateHover();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        stone.RecordObservation(ExaminationTool.MeasurementType.AbsorptionSpectrum.ToString());
        Refresh();
        ToolFeedbackUI.Instance?.ShowMessage($"Spectroscope — {TreatmentSignatures.SpectrumDescription(gem)}");
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = "Déposez une gemme sur la platine";
            darkWindow.enabled = true;
            spectrumImage.enabled = false;
            linesText.text = "";
            descriptionText.text = "";
            marker.enabled = false;
            hoverLabel.text = "";
            return;
        }

        gemImage.enabled = true;
        gemImage.sprite = stone.DisplaySprite;
        hintText.text = "";

        var def = stone.Definition;
        if (spectrumImage.sprite != null && spectrumImage.sprite.texture != null) Destroy(spectrumImage.sprite.texture);
        spectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(TreatmentSignatures.SpectrumLines(stone), 768, 8);
        spectrumImage.enabled = true;
        darkWindow.enabled = false;

        descriptionText.text = TreatmentSignatures.SpectrumDescription(stone);
        linesText.text = BuildLinesList(TreatmentSignatures.SpectrumLines(stone));
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

    private static string BuildLinesList(List<SpectrumLine> lines)
    {
        if (lines == null || lines.Count == 0)
            return "Aucune raie d'absorption.";

        var sorted = new List<SpectrumLine>(lines);
        sorted.Sort((a, b) => a.wavelengthNm.CompareTo(b.wavelengthNm));

        var text = "";
        foreach (var line in sorted)
        {
            var kind = line.widthNm < 6f ? "raie fine" : line.widthNm < 20f ? "raie" : "bande large";
            text += $"{line.wavelengthNm:F0} nm  ({kind})\n";
        }
        return text.TrimEnd('\n');
    }

    // Wavelength under the mouse, shown above the spectrum with a thin marker line.
    private void UpdateHover()
    {
        if (stone == null || !RectTransformUtility.RectangleContainsScreenPoint(spectrumRect, Input.mousePosition, null))
        {
            marker.enabled = false;
            hoverLabel.text = "";
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(spectrumRect, Input.mousePosition, null, out var local);
        var t = Mathf.Clamp01(local.x / SpectrumWidth);
        var nm = Mathf.Lerp(SpectrumRenderer.MinNm, SpectrumRenderer.MaxNm, t);

        marker.enabled = true;
        RuntimeUIKit.SetTopLeft(marker.rectTransform, new Vector2(SpectrumX + t * SpectrumWidth - 0.5f, SpectrumY), new Vector2(1f, SpectrumHeight));

        hoverLabel.text = $"{nm:F0} nm";
        var labelX = Mathf.Clamp(SpectrumX + t * SpectrumWidth - 35f, SpectrumX - 10f, SpectrumX + SpectrumWidth - 60f);
        hoverLabel.rectTransform.anchoredPosition = new Vector2(labelX, SpectrumY + 24f);
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("SpectroscopePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Spectroscope", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildStage();
        BuildSpectrumWindow();
        BuildSidePanel();

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(28f, -416f), new Vector2(160f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(204f, -416f), new Vector2(120f, 28f));
        close.onClick.AddListener(Close);
    }

    private void BuildStage()
    {
        // Metal stage with a small lit spot where the stone sits in front of the slit.
        var stage = RuntimeUIKit.MakeBox("Stage", panel.transform, new Vector2(20f, -48f), new Vector2(400f, 84f),
            new Color(0.22f, 0.23f, 0.27f, 1f));
        stage.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        stage.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        var spot = RuntimeUIKit.MakeBox("LightSpot", panel.transform, new Vector2(170f, -58f), new Vector2(100f, 64f),
            new Color(0.95f, 0.92f, 0.80f, 0.20f));
        spot.raycastTarget = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gem.GetComponent<RectTransform>(), new Vector2(198f, -62f), new Vector2(44f, 44f));
        gemImage = gem.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.78f),
            new Vector2(20f, -112f), new Vector2(400f, 18f));
    }

    private void BuildSpectrumWindow()
    {
        RuntimeUIKit.MakeBox("Frame", panel.transform, new Vector2(20f, -148f), new Vector2(400f, 178f), new Color(0.03f, 0.03f, 0.04f, 1f));

        darkWindow = RuntimeUIKit.MakeBox("DarkWindow", panel.transform, new Vector2(SpectrumX, SpectrumY),
            new Vector2(SpectrumWidth, SpectrumHeight), new Color(0.02f, 0.02f, 0.03f, 1f), false);
        darkWindow.raycastTarget = false;

        var spectrumGO = new GameObject("Spectrum", typeof(RectTransform), typeof(Image));
        spectrumGO.transform.SetParent(panel.transform, false);
        spectrumRect = spectrumGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(spectrumRect, new Vector2(SpectrumX, SpectrumY), new Vector2(SpectrumWidth, SpectrumHeight));
        spectrumImage = spectrumGO.GetComponent<Image>();
        spectrumImage.raycastTarget = false;
        spectrumImage.enabled = false;

        marker = RuntimeUIKit.MakeBox("Marker", panel.transform, new Vector2(SpectrumX, SpectrumY), new Vector2(1f, SpectrumHeight),
            new Color(1f, 1f, 1f, 0.75f), false);
        marker.raycastTarget = false;
        marker.enabled = false;

        hoverLabel = RuntimeUIKit.MakeText("HoverLabel", panel.transform, "", 14, TextAnchor.MiddleCenter, Color.white,
            new Vector2(SpectrumX, SpectrumY + 24f), new Vector2(70f, 20f));
        hoverLabel.fontStyle = FontStyle.Bold;

        // Wavelength scale: a tick every 10 nm, a label every 50 nm.
        for (var nm = 400; nm <= 700; nm += 10)
        {
            var t = (nm - SpectrumRenderer.MinNm) / (SpectrumRenderer.MaxNm - SpectrumRenderer.MinNm);
            var x = SpectrumX + t * SpectrumWidth;
            var major = nm % 50 == 0;
            var tick = RuntimeUIKit.MakeBox("Tick", panel.transform, new Vector2(x - 0.5f, SpectrumY - SpectrumHeight - 2f),
                new Vector2(1f, major ? 10f : 5f), new Color(0.8f, 0.8f, 0.85f, 1f), false);
            tick.raycastTarget = false;

            if (!major) continue;
            RuntimeUIKit.MakeText("TickLabel", panel.transform, nm.ToString(), 11, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.85f),
                new Vector2(x - 20f, SpectrumY - SpectrumHeight - 14f), new Vector2(40f, 14f));
        }
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("LinesHeader", panel.transform, "Absorptions", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(440f, -48f), new Vector2(160f, 20f));
        RuntimeUIKit.MakeBox("LinesBox", panel.transform, new Vector2(436f, -72f), new Vector2(166f, 214f), new Color(0.04f, 0.05f, 0.07f, 1f));
        linesText = RuntimeUIKit.MakeText("Lines", panel.transform, "", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(446f, -80f), new Vector2(150f, 198f));

        descriptionText = RuntimeUIKit.MakeText("Description", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f),
            new Vector2(436f, -296f), new Vector2(166f, 70f));

        RuntimeUIKit.MakeText("Tip", panel.transform, "Passez la souris sur le spectre pour lire la longueur d'onde.", 11,
            TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.65f), new Vector2(28f, -346f), new Vector2(390f, 30f));
    }
}
