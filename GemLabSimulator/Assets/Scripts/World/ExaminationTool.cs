using UnityEngine;

public class ExaminationTool : MonoBehaviour
{
    public enum MeasurementType
    {
        RefractiveIndex,
        Density,
        Hardness,
        Pleochroism,
        Fluorescence
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
            case MeasurementType.Pleochroism:
                RevealQualitative(gemInstance, gem, "Pléochroïsme",
                    gem.pleochroic ? gem.pleochroicDescription : "Aucun pléochroïsme visible.");
                break;
            case MeasurementType.Fluorescence:
                RevealQualitative(gemInstance, gem, "Fluorescence UV", gem.fluorescenceDescription);
                break;
            default:
                StartNumericMeasurement(gemInstance, gem);
                break;
        }
    }

    private void RevealQualitative(GemInstance gemInstance, GemDefinition gem, string label, string description)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} : {description}");
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
            case MeasurementType.Density:
                label = "Densité";
                trueValue = gem.density;
                break;
            default:
                label = "Dureté";
                trueValue = gem.hardness;
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
