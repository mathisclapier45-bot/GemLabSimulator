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
    private PlayerGemHolder playerGemHolder;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var holder = other.GetComponent<PlayerGemHolder>();
        if (holder == null) return;
        playerGemHolder = holder;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerGemHolder>() != playerGemHolder) return;
        playerInRange = false;
        playerGemHolder = null;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        if (!playerGemHolder.HasGem)
        {
            ToolFeedbackUI.Instance.ShowMessage($"{toolName} : aucune gemme à examiner.");
            return;
        }

        var gem = playerGemHolder.CurrentGem;

        switch (measurementType)
        {
            case MeasurementType.Pleochroism:
                RevealQualitative(gem, "Pléochroïsme",
                    gem.pleochroic ? gem.pleochroicDescription : "Aucun pléochroïsme visible.");
                break;
            case MeasurementType.Fluorescence:
                RevealQualitative(gem, "Fluorescence UV", gem.fluorescenceDescription);
                break;
            default:
                StartNumericMeasurement(gem);
                break;
        }
    }

    private void RevealQualitative(GemDefinition gem, string label, string description)
    {
        playerGemHolder.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} : {description}");
    }

    private void StartNumericMeasurement(GemDefinition gem)
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

        var holder = playerGemHolder;
        var propertyKey = measurementType.ToString();

        MeasurementMinigameUI.Instance.Open(toolName, precision =>
        {
            float error;
            if (precision >= 1f) error = 0f;
            else if (precision >= 0.6f) error = trueValue * Random.Range(-0.03f, 0.03f);
            else error = trueValue * Random.Range(-0.12f, 0.12f);

            var measured = trueValue + error;
            holder.RecordMeasurement(propertyKey, measured);

            var tierLabel = precision >= 1f ? "Parfait" : precision >= 0.6f ? "Bonne lecture" : "Lecture imprécise";
            ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} ≈ {measured:F3} ({tierLabel})");
        });
    }
}
