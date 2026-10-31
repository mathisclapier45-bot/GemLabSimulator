using System.Collections.Generic;
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
        if (dropPanel != null) dropPanel.SetActive(true);
        InventoryUI.Instance?.Open();
        RenderReport();
        panel.SetActive(true);
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
            measured.Add($"Transparence : {gem.transparency}");
            measured.Add($"Forme : {gemInstance.ShapeName}");
            measured.Add($"Style de taille : {gemInstance.CutStyleName}");
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
            measured.Add($"Spectroscope : {gem.absorptionSpectrum}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Inclusions.ToString()))
        {
            measured.Add($"Clarté : {gemInstance.ClarityGrade} ({gemInstance.ClarityDescription})");
            measured.Add($"Inclusions : {gemInstance.InclusionDescription}");
        }
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.InterferenceFigure.ToString()))
            measured.Add($"Conoscope : {OpticalDescriptions.ConoscopeText(gem)}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ChelseaFilter.ToString()))
            measured.Add($"Filtre Chelsea : {gem.chelseaFilterReaction}");
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
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.DiamondScreening.ToString()))
            measured.Add($"DiamondSure : {(gemInstance.IsSynthetic ? "Refer" : "Pass")}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.DiamondGrowthImaging.ToString()))
            measured.Add($"DiamondView : {(gemInstance.IsSynthetic ? "Motifs de synthèse (HPHT/CVD)" : "Motifs de croissance naturelle")}");

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
