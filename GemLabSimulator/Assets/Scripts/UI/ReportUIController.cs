using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ReportUIController : MonoBehaviour
{
    public static ReportUIController Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text measurementsText;
    [SerializeField] private Text resultText;

    private PlayerInventory currentInventory;

    // The stone handed to the professor: dropped into the slot of the side panel.
    private GemInstance slotGem;
    private GameObject dropPanel;
    private Image slotIcon;
    private Text slotHint;

    // The list of species: sorted from A to Z, with a search box above it.
    private struct SpeciesOption
    {
        public RectTransform Rect;
        public string Key;          // the name without accents or capitals, for the sort and the search
    }

    private readonly List<SpeciesOption> speciesOptions = new List<SpeciesOption>();
    private RectTransform speciesContent;
    private InputField searchField;
    private Text noResultText;
    private bool indexBuilt;

    private void Awake()
    {
        Instance = this;
        BuildDropPanel();
        if (panel != null) panel.SetActive(false);
        if (dropPanel != null) dropPanel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        if (panel == null || !panel.activeSelf || slotGem == null) return;

        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, slotGem)) return;
        }

        // The stone left the inventory (sent back, sold...): empty the slot.
        slotGem = null;
        RenderReport();
    }

    public void Open(PlayerInventory inventory)
    {
        currentInventory = inventory;
        slotGem = null;
        BuildSpeciesIndex();
        if (searchField != null) searchField.text = "";
        if (dropPanel != null) dropPanel.SetActive(true);
        InventoryUI.Instance?.Open();
        RenderReport();
        panel.SetActive(true);
    }

    // ---- The list of species -----------------------------------------------------------------------------------

    private const float OptionWidth = 195f;
    private const float OptionHeight = 34f;
    private const float OptionGap = 6f;
    private const float ListHeight = 210f;

    // Takes the buttons of the scene (they keep their clicks), sorts them from A to Z and puts a search box above them. Done once, when the panel first opens.
    private void BuildSpeciesIndex()
    {
        if (indexBuilt || panel == null) return;

        var scroll = panel.transform.Find("SpeciesScrollView") as RectTransform;
        var content = scroll != null ? scroll.Find("Content") as RectTransform : null;
        if (scroll == null || content == null) return;
        indexBuilt = true;

        speciesContent = content;
        foreach (Transform child in content)
        {
            var label = child.GetComponentInChildren<Text>();
            if (label == null) continue;
            speciesOptions.Add(new SpeciesOption { Rect = child as RectTransform, Key = Normalize(label.text) });
        }
        speciesOptions.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

        // The search box takes the place of the top of the list, which gets a little shorter.
        scroll.anchoredPosition = new Vector2(0f, -358f);
        scroll.sizeDelta = new Vector2(420f, ListHeight);

        var boxGO = new GameObject("SpeciesSearch", typeof(RectTransform), typeof(Image), typeof(InputField));
        boxGO.transform.SetParent(panel.transform, false);
        var boxRect = boxGO.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 1f);
        boxRect.pivot = new Vector2(0.5f, 1f);
        boxRect.anchoredPosition = new Vector2(0f, -324f);
        boxRect.sizeDelta = new Vector2(420f, 28f);
        boxGO.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);

        var text = RuntimeUIKit.MakeText("Text", boxGO.transform, "", 14, TextAnchor.MiddleLeft, Color.white, Vector2.zero, Vector2.zero);
        var placeholder = RuntimeUIKit.MakeText("Placeholder", boxGO.transform, "Rechercher une pierre (ex. : gre, rubis, quartz)…", 13, TextAnchor.MiddleLeft,
            new Color(0.58f, 0.60f, 0.66f), Vector2.zero, Vector2.zero);
        placeholder.fontStyle = FontStyle.Italic;
        foreach (var t in new[] { text, placeholder })
        {
            var r = t.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(10f, 2f);
            r.offsetMax = new Vector2(-10f, -2f);
        }

        searchField = boxGO.GetComponent<InputField>();
        searchField.textComponent = text;
        searchField.placeholder = placeholder;
        searchField.characterLimit = 30;
        searchField.onValueChanged.AddListener(ApplySpeciesFilter);

        noResultText = RuntimeUIKit.MakeText("NoResult", content, "Aucune pierre ne correspond.", 14, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0f, 0f), new Vector2(420f, 30f));
        var noResultRect = noResultText.rectTransform;
        noResultRect.anchorMin = noResultRect.anchorMax = new Vector2(0.5f, 1f);
        noResultRect.pivot = new Vector2(0.5f, 1f);
        noResultRect.anchoredPosition = new Vector2(0f, -20f);
        noResultText.gameObject.SetActive(false);

        ApplySpeciesFilter("");
    }

    // Keeps the species whose name holds every word typed, and packs them in two columns, in order.
    private void ApplySpeciesFilter(string query)
    {
        var words = Normalize(query).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        var shown = 0;
        foreach (var option in speciesOptions)
        {
            var match = true;
            foreach (var word in words)
                if (!option.Key.Contains(word)) { match = false; break; }

            option.Rect.gameObject.SetActive(match);
            if (!match) continue;

            var column = shown % 2;
            var row = shown / 2;
            option.Rect.anchoredPosition = new Vector2(column == 0 ? -(OptionWidth + 10f) / 2f : (OptionWidth + 10f) / 2f, -8f - row * (OptionHeight + OptionGap));
            shown++;
        }

        if (noResultText != null) noResultText.gameObject.SetActive(shown == 0);
        var rows = Mathf.CeilToInt(shown / 2f);
        speciesContent.sizeDelta = new Vector2(speciesContent.sizeDelta.x, Mathf.Max(ListHeight, rows * (OptionHeight + OptionGap) + 8f));
        speciesContent.anchoredPosition = Vector2.zero;
    }

    // Lower case, no accents: "Péridot" and "peridot" are the same word; "œ" is "oe".
    private static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var decomposed = text.ToLowerInvariant().Replace("œ", "oe").Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        return builder.ToString();
    }

    private void SetSlotGem(GemInstance gem)
    {
        slotGem = gem;
        RenderReport();
    }

    private void ClearSlot()
    {
        slotGem = null;
        RenderReport();
    }

    private void RenderReport()
    {
        resultText.text = string.Empty;

        if (slotIcon != null)
        {
            slotIcon.enabled = slotGem != null;
            if (slotGem != null) slotIcon.sprite = slotGem.DisplaySprite;
            slotHint.text = slotGem == null ? "Déposez la gemme ici" : "";
        }

        if (slotGem == null)
        {
            measurementsText.text = "Le directeur : \"Remettez-moi la gemme à examiner : glissez-la dans le slot à gauche.\"";
            return;
        }

        var gemInstance = slotGem;
        var gem = gemInstance.Definition;
        var measured = new List<string>();

        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.RefractiveIndex, "Indice de réfraction", "F3");
        if (gemInstance.HasObserved("RefractiveIndexOTL"))
            measured.Add("Indice de réfraction : OTL (hors limite du prisme, IR > 1.81)");
        if (gemInstance.HasMeasured(ExaminationTool.MeasurementType.RefractiveIndex.ToString()) || gemInstance.HasObserved("RefractiveIndexOTL"))
            measured.Add($"Biréfringence : {OpticalDescriptions.BirefringenceText(gem)}");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Density, "Densité", "F2");

        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.VisualExamination.ToString()))
        {
            measured.Add($"Éclat : {gem.luster}");
            measured.Add($"Couleur : {gemInstance.ColorDescription}");
            measured.Add($"Transparence : {gem.TransparencyLabel}");
            if (gemInstance.IsRough)
            {
                measured.Add("Forme : brut");
                if (gemInstance.Habit != null) measured.Add($"Habitus : {gemInstance.Habit.Name.ToLowerInvariant()}");
            }
            else
            {
                measured.Add($"Forme : {gemInstance.ShapeName}");
                measured.Add($"Style de taille : {gemInstance.CutStyleName}");
            }
        }
        if (gemInstance.HasObserved("LoupeInspection"))
        {
            measured.Add($"Loupe 10x — {LoupeFeatures.InclusionSummary(gemInstance)}");
            foreach (var line in LoupeFeatures.ExternalDefectLines(gemInstance)) measured.Add($"Loupe 10x — {line}");
        }
        measured.AddRange(DimensionReport.Lines(gemInstance));
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Weight.ToString()))
            measured.Add($"Poids : {gemInstance.CaratWeight:F2} ct");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Pleochroism.ToString()))
            measured.Add(gem.pleochroic ? $"Pléochroïsme : {gem.pleochroicDescription}" : "Pléochroïsme : aucun");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.FluorescenceLongWave.ToString()))
            measured.Add($"Fluorescence UV longues : {gem.fluorescenceLongWave}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.FluorescenceShortWave.ToString()))
            measured.Add($"Fluorescence UV courtes : {gem.fluorescenceShortWave}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.OpticalCharacter.ToString()))
            measured.Add($"Polariscope : {OpticalDescriptions.PolariscopeText(gem)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.AbsorptionSpectrum.ToString()))
            measured.Add($"Spectroscope : {TreatmentSignatures.SpectrumDescription(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Inclusions.ToString()))
        {
            measured.Add($"Clarté : {gemInstance.ClarityGrade} ({gemInstance.ClarityDescription})");
            measured.Add($"Inclusions : {gemInstance.InclusionDescription}");
        }
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.InterferenceFigure.ToString()))
            measured.Add($"Conoscope : {OpticalDescriptions.ConoscopeText(gem)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ChelseaFilter.ToString()))
            measured.Add($"Filtre Chelsea : {ChelseaLibrary.Reaction(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Magnetism.ToString()))
            measured.Add($"Aimant : {gem.magnetism}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Treatment.ToString()))
            measured.Add($"Traitement (FTIR) : {gemInstance.TreatmentGrade}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Authenticity.ToString()))
            measured.Add($"Authenticité (Raman) : {gemInstance.AuthenticityGrade}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString()))
            measured.Add($"Origine confirmée (spectrophotomètre UV-Vis-NIR) : {gemInstance.Origin.name}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ThermalConductivity.ToString()))
            measured.Add($"Conductivité thermique : {(gem.isThermallyConductiveLikeDiamond ? "Élevée (type diamant)" : "Faible")}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ElectricalConductivity.ToString()))
            measured.Add($"Conductivité électrique : {(gem.isElectricallyConductive ? "Conductrice" : "Non conductrice")}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.GrowthStructure.ToString()))
            measured.Add($"Structure de croissance (MEB) : {gemInstance.GrowthStructureDescription}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ElementalAnalysis.ToString()))
            measured.Add($"EDXRF : {EdxrfSpectrum.Summary(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.TraceElements.ToString()))
            measured.Add($"LA-ICP-MS : {LaIcpMsAnalysis.Summary(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Photoluminescence.ToString()))
            measured.Add($"Photoluminescence : {PhotoluminescenceSpectrum.Summary(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.DiamondScreening.ToString()))
            measured.Add($"DiamondSure : {DiamondSure.Label(gemInstance)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.DiamondGrowthImaging.ToString()))
            measured.Add($"DiamondView : {DiamondViewImage.Summary(gemInstance)}");

        var identityLine = gemInstance.IsIdentified
            ? $"Gemme : {gem.displayName} (identifiée) — N° {gemInstance.DisplayId}"
            : $"Gemme sélectionnée : spécimen non identifié — N° {gemInstance.DisplayId}";

        measurementsText.text = measured.Count > 0
            ? $"{identityLine}\n\nMesures relevées :\n" + string.Join("\n", measured)
            : $"{identityLine}\n\nAucune mesure effectuée. Retourne examiner la gemme.";
    }

    // Side panel built at runtime (so existing scenes need no regeneration) with the slot where the stone is dropped.
    private void BuildDropPanel()
    {
        dropPanel = new GameObject("ProfessorDropPanel", typeof(RectTransform));
        dropPanel.transform.SetParent(transform, false);
        var rect = dropPanel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(170f, 230f);
        rect.anchoredPosition = new Vector2(-335f, 200f);

        RuntimeUIKit.MakeBox("Body", dropPanel.transform, Vector2.zero, new Vector2(170f, 230f), new Color(0.08f, 0.08f, 0.10f, 0.97f));
        RuntimeUIKit.MakeText("Title", dropPanel.transform, "Rendre la gemme au professeur", 13, TextAnchor.UpperCenter,
            new Color(0.95f, 0.85f, 0.65f), new Vector2(10f, -10f), new Vector2(150f, 36f));

        var slot = RuntimeUIKit.MakeBox("Slot", dropPanel.transform, new Vector2(41f, -58f), new Vector2(88f, 88f), new Color(0.20f, 0.20f, 0.24f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetSlotGem;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearSlot);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slot.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 10f);
        iconRect.offsetMax = new Vector2(-10f, -10f);
        slotIcon = iconGO.GetComponent<Image>();
        slotIcon.preserveAspect = true;
        slotIcon.raycastTarget = false;
        slotIcon.enabled = false;

        slotHint = RuntimeUIKit.MakeText("Hint", slot.transform, "Déposez la gemme ici", 11, TextAnchor.MiddleCenter,
            new Color(0.65f, 0.65f, 0.7f), Vector2.zero, new Vector2(88f, 88f));
        var hintRect = slotHint.rectTransform;
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.offsetMin = new Vector2(4f, 0f);
        hintRect.offsetMax = new Vector2(-4f, 0f);

        var remove = RuntimeUIKit.MakeButton("Remove", dropPanel.transform, "Reprendre la gemme", 12, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(15f, -160f), new Vector2(140f, 26f));
        remove.onClick.AddListener(ClearSlot);

        RuntimeUIKit.MakeText("Tip", dropPanel.transform, "Les mesures déjà relevées s'affichent à droite.", 10, TextAnchor.UpperCenter,
            new Color(0.6f, 0.62f, 0.68f), new Vector2(10f, -194f), new Vector2(150f, 28f));
    }

    private static void AddIfMeasured(List<string> measured, GemInstance gemInstance, ExaminationTool.MeasurementType type,
        string label, string format, string suffix = "")
    {
        var key = type.ToString();
        if (!gemInstance.HasMeasured(key)) return;
        measured.Add($"{label} ≈ {gemInstance.GetMeasuredValue(key).ToString(format)}{suffix}");
    }

    public void Close()
    {
        panel.SetActive(false);
        if (dropPanel != null) dropPanel.SetActive(false);
        currentInventory = null;
        slotGem = null;
    }

    public void SubmitSpecies(string speciesName)
    {
        var gemInstance = slotGem;
        if (gemInstance == null)
        {
            resultText.text = "Le directeur : \"Déposez d'abord la gemme dans le slot.\"";
            return;
        }

        if (gemInstance.IsIdentified)
        {
            resultText.text = "Le directeur : \"Cette gemme a déjà été identifiée.\"";
            return;
        }

        var gem = gemInstance.Definition;
        if (speciesName == gem.displayName)
        {
            resultText.text = $"Rapport validé par le directeur ! Identifiée comme {gem.displayName}. " +
                               "Renvoyez-la à son client depuis la boîte aux lettres pour être payé.";
            gemInstance.MarkIdentified();
            PlayerInventory.Instance?.NotifyChanged();
            ReputationState.Instance?.AddPoint();
            StoryState.Instance?.OnStoneIdentified(gemInstance);

            var isNewDiscovery = GemCodex.Instance != null && !GemCodex.Instance.IsDiscovered(gem.name);
            GemCodex.Instance?.MarkDiscovered(gem.name);
            QuestState.Instance?.ReportIdentification();
            if (isNewDiscovery)
            {
                QuestState.Instance?.ReportDiscovery();
                ToolFeedbackUI.Instance?.ShowMessage($"✨ Nouvelle espèce découverte : {gem.displayName} ! Consultez le Grimoire (touche G).");
            }
        }
        else
        {
            resultText.text = $"Le directeur rejette le rapport : identification incorrecte.";
        }
    }
}
