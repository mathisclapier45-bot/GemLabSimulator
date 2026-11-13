using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Spectroscope: drop a gem on the stage, its absorption spectrum appears, the absorption lines are listed on the
// side (in nm) and hovering the spectrum shows the wavelength under the mouse.
public class SpectroscopeUI : MonoBehaviour
{
    public static SpectroscopeUI Instance { get; private set; }

    private const float PanelWidth = 620f;
    private const float PanelHeight = 500f;
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

    // The index of the spectra, on the left of the instrument: small reference strips; a click puts one under the spectrum of the stone to compare.
    private const float IndexWidth = 176f;
    private const float ReferenceY = -334f;
    private Image referenceImage;
    private Image referenceFrame;
    private Image referenceMarker;
    private Text referenceCaption;
    private string referenceKey;
    private readonly Dictionary<string, Image> indexRows = new Dictionary<string, Image>();

    private static readonly (string group, string[] keys)[] IndexGroups =
    {
        ("Chrome", new[] { "Corindon_Rubis", "Corindon_Synthetique_Rubis", "Emeraude", "Spinelle", "Chrysoberyl_Alexandrite", "Corindon_Synthetique_CC" }),
        ("Fer", new[] { "Saphir_Bleu", "Saphir_Vert", "Chrysoberyl", "Peridot", "Grenat_Almandin", "Jade_Jadeite", "Jade_Jadeite_Teinte" }),
        ("Cobalt", new[] { "Spinelle_Bleu", "Verre_Bleu_Cobalt" }),
        ("Uranium et terres rares", new[] { "Zircon", "Zircon_Chauffe", "Apatite" }),
        ("Verres rouges", new[] { "Verre_Rouge_Au", "Verre_Rouge_Se", "Verre_Rouge_TR" }),
    };

    private static readonly Dictionary<string, string> IndexNames = new Dictionary<string, string>
    {
        { "Corindon_Rubis", "Rubis" }, { "Corindon_Synthetique_Rubis", "Rubis de synthèse" }, { "Emeraude", "Émeraude" }, { "Spinelle", "Spinelle rouge" },
        { "Chrysoberyl_Alexandrite", "Alexandrite" }, { "Corindon_Synthetique_CC", "Saphir synth. changement de couleur" },
        { "Saphir_Bleu", "Saphir bleu" }, { "Saphir_Vert", "Saphir vert" }, { "Chrysoberyl", "Chrysobéryl" }, { "Peridot", "Péridot" },
        { "Grenat_Almandin", "Grenat almandin" }, { "Jade_Jadeite", "Jadéite" }, { "Jade_Jadeite_Teinte", "Jadéite teintée (Fe + Cr)" },
        { "Spinelle_Bleu", "Spinelle bleu (cobalt)" }, { "Verre_Bleu_Cobalt", "Verre bleu (cobalt)" },
        { "Zircon", "Zircon" }, { "Zircon_Chauffe", "Zircon chauffé" }, { "Apatite", "Apatite (terres rares)" },
        { "Verre_Rouge_Au", "Verre rouge (or)" }, { "Verre_Rouge_Se", "Verre rouge (sélénium)" }, { "Verre_Rouge_TR", "Verre rouge (terres rares)" },
    };

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
        StoryState.Instance?.OnObserved(stone, ExaminationTool.MeasurementType.AbsorptionSpectrum.ToString());
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
            referenceMarker.enabled = false;
            hoverLabel.text = "";
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(spectrumRect, Input.mousePosition, null, out var local);
        var t = Mathf.Clamp01(local.x / SpectrumWidth);
        var nm = Mathf.Lerp(SpectrumRenderer.MinNm, SpectrumRenderer.MaxNm, t);

        marker.enabled = true;
        RuntimeUIKit.SetTopLeft(marker.rectTransform, new Vector2(SpectrumX + t * SpectrumWidth - 0.5f, SpectrumY), new Vector2(1f, SpectrumHeight));
        referenceMarker.enabled = referenceKey != null;
        RuntimeUIKit.SetTopLeft(referenceMarker.rectTransform, new Vector2(SpectrumX + t * SpectrumWidth - 0.5f, ReferenceY), new Vector2(1f, 22f));

        hoverLabel.text = $"{nm:F0} nm · {SpectrumRenderer.BandAt(nm).ToLowerInvariant()}";
        var labelX = Mathf.Clamp(SpectrumX + t * SpectrumWidth - 55f, SpectrumX - 10f, SpectrumX + SpectrumWidth - 100f);
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
        // Shifted a little to the left so the inventory stays reachable for drag and drop; the index of the spectra sits on its left.
        rect.anchoredPosition = new Vector2(-60f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Spectroscope", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildStage();
        BuildSpectrumWindow();
        BuildSidePanel();
        BuildReference();
        BuildIndex();

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(28f, -462f), new Vector2(160f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(204f, -462f), new Vector2(120f, 28f));
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
            new Vector2(SpectrumX, SpectrumY + 24f), new Vector2(130f, 20f));
        hoverLabel.fontStyle = FontStyle.Bold;

        // Wavelength scale: a tick every 10 nm, a label every 50 nm, the edges of the colour bands in gold.
        for (var nm = 400; nm <= 700; nm += 10)
        {
            var t = (nm - SpectrumRenderer.MinNm) / (SpectrumRenderer.MaxNm - SpectrumRenderer.MinNm);
            var x = SpectrumX + t * SpectrumWidth;
            var major = nm % 50 == 0;
            var bandEdge = IsBandEdge(nm);
            var tick = RuntimeUIKit.MakeBox("Tick", panel.transform, new Vector2(x - 0.5f, SpectrumY - SpectrumHeight - 2f),
                new Vector2(1f, major || bandEdge ? 10f : 5f), bandEdge ? Gold : new Color(0.8f, 0.8f, 0.85f, 1f), false);
            tick.raycastTarget = false;

            if (!major) continue;
            RuntimeUIKit.MakeText("TickLabel", panel.transform, nm.ToString(), 11, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.85f),
                new Vector2(x - 20f, SpectrumY - SpectrumHeight - 14f), new Vector2(40f, 14f));
        }

        // The colour bands under the scale: violet, blue, green, yellow, orange, red.
        var bandColors = new[]
        {
            new Color(0.50f, 0.32f, 0.95f), new Color(0.25f, 0.45f, 1f), new Color(0.32f, 0.78f, 0.30f),
            new Color(0.95f, 0.85f, 0.20f), new Color(1f, 0.58f, 0.15f), new Color(0.92f, 0.22f, 0.20f),
        };
        for (var i = 0; i < SpectrumRenderer.Bands.Length; i++)
        {
            var band = SpectrumRenderer.Bands[i];
            var x0 = SpectrumX + (band.lo - SpectrumRenderer.MinNm) / (SpectrumRenderer.MaxNm - SpectrumRenderer.MinNm) * SpectrumWidth;
            var x1 = SpectrumX + (band.hi - SpectrumRenderer.MinNm) / (SpectrumRenderer.MaxNm - SpectrumRenderer.MinNm) * SpectrumWidth;
            var bar = RuntimeUIKit.MakeBox("Band_" + band.name, panel.transform, new Vector2(x0 + 0.5f, SpectrumY - SpectrumHeight - 30f),
                new Vector2(x1 - x0 - 1f, 4f), bandColors[i], false);
            bar.raycastTarget = false;
            var label = RuntimeUIKit.MakeText("BandLabel_" + band.name, panel.transform, band.name, 10, TextAnchor.UpperCenter, bandColors[i],
                new Vector2(x0, SpectrumY - SpectrumHeight - 35f), new Vector2(x1 - x0, 12f));
            label.raycastTarget = false;
        }
    }

    private static bool IsBandEdge(int nm)
    {
        foreach (var band in SpectrumRenderer.Bands)
            if (Mathf.Approximately(band.lo, nm) && nm > 400) return true;
        return false;
    }

    // The reference strip under the spectrum of the stone, on the same scale (hidden until an entry of the index is chosen).
    private void BuildReference()
    {
        referenceFrame = RuntimeUIKit.MakeBox("ReferenceFrame", panel.transform, new Vector2(SpectrumX - 1f, ReferenceY + 1f), new Vector2(SpectrumWidth + 2f, 24f),
            new Color(0.03f, 0.03f, 0.04f, 1f), false);
        referenceFrame.raycastTarget = false;
        referenceFrame.enabled = false;

        var go = new GameObject("Reference", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), new Vector2(SpectrumX, ReferenceY), new Vector2(SpectrumWidth, 22f));
        referenceImage = go.GetComponent<Image>();
        referenceImage.raycastTarget = false;
        referenceImage.enabled = false;

        referenceMarker = RuntimeUIKit.MakeBox("ReferenceMarker", panel.transform, new Vector2(SpectrumX, ReferenceY), new Vector2(1f, 22f), new Color(1f, 1f, 1f, 0.75f), false);
        referenceMarker.raycastTarget = false;
        referenceMarker.enabled = false;

        referenceCaption = RuntimeUIKit.MakeText("ReferenceCaption", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.65f),
            new Vector2(SpectrumX, ReferenceY - 24f), new Vector2(SpectrumWidth, 16f));
        referenceCaption.raycastTarget = false;
    }

    private void BuildIndex()
    {
        var root = RuntimeUIKit.MakeBox("Index", panel.transform, new Vector2(-IndexWidth - 8f, 0f), new Vector2(IndexWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("IndexTitle", root.transform, "Index des spectres", 16, TextAnchor.UpperLeft, Gold, new Vector2(12f, -12f), new Vector2(IndexWidth - 20f, 24f));
        RuntimeUIKit.MakeText("IndexHint", root.transform, "Cliquez une pierre pour la comparer sous son spectre.", 10, TextAnchor.UpperLeft,
            new Color(0.6f, 0.6f, 0.65f), new Vector2(12f, -36f), new Vector2(IndexWidth - 20f, 28f));

        // A scrolling list, clipped to its window.
        var viewportGO = new GameObject("IndexView", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
        viewportGO.transform.SetParent(root.transform, false);
        RuntimeUIKit.SetTopLeft(viewportGO.GetComponent<RectTransform>(), new Vector2(6f, -68f), new Vector2(IndexWidth - 12f, PanelHeight - 76f));
        viewportGO.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 1f);

        var contentGO = new GameObject("IndexContent", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        var scroll = viewportGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportGO.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var rowWidth = IndexWidth - 24f;
        var y = -4f;
        foreach (var (group, keys) in IndexGroups)
        {
            var any = false;
            foreach (var key in keys) if (SpectrumLibrary.Entry(key) != null) any = true;
            if (!any) continue;

            RuntimeUIKit.MakeText("Group_" + group, content, group, 11, TextAnchor.UpperLeft, Gold, new Vector2(4f, y - 2f), new Vector2(rowWidth, 16f)).fontStyle = FontStyle.Bold;
            y -= 20f;
            foreach (var key in keys)
            {
                var entry = SpectrumLibrary.Entry(key);
                if (entry == null) continue;

                var capturedKey = key;
                var row = RuntimeUIKit.MakeBox("Row_" + key, content, new Vector2(2f, y), new Vector2(rowWidth, 30f), new Color(0.10f, 0.11f, 0.15f, 1f));
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                button.onClick.AddListener(() => SelectReference(capturedKey));
                indexRows[key] = row;

                var name = IndexNames.TryGetValue(key, out var shown) ? shown : key;
                var label = RuntimeUIKit.MakeText("Name", row.transform, name, 10, TextAnchor.UpperLeft, new Color(0.86f, 0.88f, 0.94f), new Vector2(4f, -2f), new Vector2(rowWidth - 8f, 13f));
                label.raycastTarget = false;
                var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
                strip.transform.SetParent(row.transform, false);
                RuntimeUIKit.SetTopLeft(strip.GetComponent<RectTransform>(), new Vector2(4f, -16f), new Vector2(rowWidth - 8f, 10f));
                var stripImage = strip.GetComponent<Image>();
                stripImage.sprite = SpectrumRenderer.BuildSpectrumSprite(entry.lines, 300, 8);
                stripImage.raycastTarget = false;
                y -= 33f;
            }
            y -= 4f;
        }
        content.sizeDelta = new Vector2(0f, -y + 6f);
    }

    private static readonly Color RowIdle = new Color(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Color RowChosen = new Color(0.40f, 0.31f, 0.12f, 1f);

    // Puts a reference spectrum under the one of the stone (a second click on the same entry takes it away).
    private void SelectReference(string key)
    {
        Sfx.Play(Sfx.Id.Click);
        referenceKey = referenceKey == key ? null : key;
        foreach (var pair in indexRows) pair.Value.color = pair.Key == referenceKey ? RowChosen : RowIdle;

        if (referenceImage.sprite != null && referenceImage.sprite.texture != null) Destroy(referenceImage.sprite.texture);
        referenceImage.sprite = null;

        var entry = referenceKey != null ? SpectrumLibrary.Entry(referenceKey) : null;
        var shown = entry != null;
        referenceImage.enabled = shown;
        referenceFrame.enabled = shown;
        if (!shown)
        {
            referenceCaption.text = "";
            return;
        }

        referenceImage.sprite = SpectrumRenderer.BuildSpectrumSprite(entry.lines, 768, 8);
        referenceCaption.text = "Référence : " + (IndexNames.TryGetValue(referenceKey, out var name) ? name : referenceKey);
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("LinesHeader", panel.transform, "Absorptions", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(440f, -48f), new Vector2(160f, 20f));
        RuntimeUIKit.MakeBox("LinesBox", panel.transform, new Vector2(436f, -72f), new Vector2(166f, 254f), new Color(0.04f, 0.05f, 0.07f, 1f));
        linesText = RuntimeUIKit.MakeText("Lines", panel.transform, "", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(446f, -80f), new Vector2(150f, 238f));

        // The sentence about the spectrum, across the whole panel under the scale.
        descriptionText = RuntimeUIKit.MakeText("Description", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f),
            new Vector2(28f, -376f), new Vector2(570f, 60f));

        RuntimeUIKit.MakeText("Tip", panel.transform, "Passez la souris sur le spectre pour lire la longueur d'onde (nm) et la couleur.", 11,
            TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.65f), new Vector2(28f, -432f), new Vector2(570f, 18f));
    }
}
