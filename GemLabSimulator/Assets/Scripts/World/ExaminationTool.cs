using UnityEngine;

public class ExaminationTool : MonoBehaviour
{
    public enum MeasurementType
    {
        RefractiveIndex,
        Density,
        Dimensions,
        Weight,
        Pleochroism,
        FluorescenceLongWave,
        FluorescenceShortWave,
        OpticalCharacter,
        AbsorptionSpectrum,
        Inclusions,
        InterferenceFigure,
        ChelseaFilter,
        VisualExamination,
        Treatment,
        Authenticity,
        ConfirmedOrigin,
        GrowthStructure
    }

    [SerializeField] private string toolName;
    [SerializeField] private MeasurementType measurementType;

    private bool playerInRange;
    private PlayerInventory playerInventory;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) return;
        playerInventory = inventory;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() != playerInventory) return;
        playerInRange = false;
        playerInventory = null;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        var gemInstance = playerInventory.SelectedGem;
        if (gemInstance == null)
        {
            ToolFeedbackUI.Instance.ShowMessage($"{toolName} : aucune gemme sélectionnée (Tab pour l'inventaire).");
            return;
        }

        var gem = gemInstance.Definition;

        switch (measurementType)
        {
            case MeasurementType.RefractiveIndex when gem.refractiveIndex > 1.81f:
                RevealOverTheLimit(gemInstance);
                break;
            case MeasurementType.Pleochroism:
                RevealQualitative(gemInstance, "Pléochroïsme",
                    gem.pleochroic ? gem.pleochroicDescription : "Aucun pléochroïsme visible.");
                break;
            case MeasurementType.FluorescenceLongWave:
                RevealQualitative(gemInstance, "Fluorescence UV longues ondes", gem.fluorescenceLongWave);
                break;
            case MeasurementType.FluorescenceShortWave:
                RevealQualitative(gemInstance, "Fluorescence UV courtes ondes", gem.fluorescenceShortWave);
                break;
            case MeasurementType.OpticalCharacter:
                RevealQualitative(gemInstance, "Caractère optique", gem.opticalCharacter);
                break;
            case MeasurementType.AbsorptionSpectrum:
                RevealQualitative(gemInstance, "Spectre d'absorption", gem.absorptionSpectrum);
                SpectrumUI.Instance?.Show(gem);
                break;
            case MeasurementType.Inclusions:
                RevealInclusions(gemInstance);
                break;
            case MeasurementType.InterferenceFigure:
                RevealQualitative(gemInstance, "Figure d'interférence", gem.interferenceFigure);
                break;
            case MeasurementType.ChelseaFilter:
                RevealQualitative(gemInstance, "Réaction au filtre Chelsea", gem.chelseaFilterReaction);
                break;
            case MeasurementType.VisualExamination:
                RevealVisualExamination(gemInstance, gem);
                break;
            case MeasurementType.Weight:
                RevealWeight(gemInstance, gem);
                break;
            case MeasurementType.Dimensions:
                RevealDimensions(gemInstance, gem);
                break;
            case MeasurementType.Treatment:
                RevealQualitative(gemInstance, "Traitement détecté",
                    $"{gemInstance.TreatmentGrade} — {gemInstance.TreatmentDescription}");
                break;
            case MeasurementType.Authenticity:
                RevealQualitative(gemInstance, "Authenticité",
                    $"{gemInstance.AuthenticityGrade} — {gemInstance.AuthenticityDescription}");
                break;
            case MeasurementType.ConfirmedOrigin:
                RevealQualitative(gemInstance, "Origine confirmée (analyse des éléments traces)", gemInstance.Origin.name);
                break;
            case MeasurementType.GrowthStructure:
                RevealQualitative(gemInstance, "Structure de croissance", gemInstance.GrowthStructureDescription);
                break;
            default:
                StartNumericMeasurement(gemInstance, gem);
                break;
        }
    }

    private void RevealQualitative(GemInstance gemInstance, string label, string description)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} : {description}");
    }

    private void RevealVisualExamination(GemInstance gemInstance, GemDefinition gem)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage(
            $"{toolName} — Éclat : {gem.luster} | Couleur : {gem.color} ({gemInstance.SaturationGrade}) | " +
            $"Transparence : {gem.transparency} | Forme : {gem.shape} | Style de taille : {gem.cutStyle}");
    }

    private void RevealWeight(GemInstance gemInstance, GemDefinition gem)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage($"{toolName} — Poids : {gemInstance.CaratWeight:F2} ct");
    }

    private void RevealDimensions(GemInstance gemInstance, GemDefinition gem)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage(
            $"{toolName} — Dimensions : {gemInstance.LengthMm:F1} x {gemInstance.WidthMm:F1} x {gemInstance.DepthMm:F1} mm");
    }

    private void RevealOverTheLimit(GemInstance gemInstance)
    {
        gemInstance.RecordObservation("RefractiveIndexOTL");
        ToolFeedbackUI.Instance.ShowMessage($"{toolName} — Indice de réfraction : OTL (hors limite du prisme, IR > 1.81)");
    }

    private void RevealInclusions(GemInstance gemInstance)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage(
            $"{toolName} — Clarté : {gemInstance.ClarityGrade} ({gemInstance.ClarityDescription}) | " +
            $"Inclusions : {gemInstance.InclusionDescription} | Origine probable : {gemInstance.Origin.name}");
    }

    private void StartNumericMeasurement(GemInstance gemInstance, GemDefinition gem)
    {
        string label;
        float trueValue;
        switch (measurementType)
        {
            case MeasurementType.RefractiveIndex:
                label = "Indice de réfraction";
                trueValue = gem.refractiveIndex;
                break;
            default:
                label = "Densité";
                trueValue = gem.density;
                break;
        }

        var propertyKey = measurementType.ToString();

        MeasurementMinigameUI.Instance.Open(toolName, precision =>
        {
            float error;
            if (precision >= 1f) error = 0f;
            else if (precision >= 0.6f) error = trueValue * Random.Range(-0.03f, 0.03f);
            else error = trueValue * Random.Range(-0.12f, 0.12f);

            var measured = trueValue + error;
            gemInstance.RecordMeasurement(propertyKey, measured);

            var tierLabel = precision >= 1f ? "Parfait" : precision >= 0.6f ? "Bonne lecture" : "Lecture imprécise";
            ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} ≈ {measured:F3} ({tierLabel})");
        });
    }
}
