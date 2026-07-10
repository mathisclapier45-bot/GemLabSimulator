using UnityEngine;

public class ExaminationTool : MonoBehaviour
{
    public enum MeasurementType
    {
        RefractiveIndex,
        Density,
        Hardness
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
            Debug.Log($"{toolName} : aucune gemme à examiner.");
            return;
        }

        var gem = playerGemHolder.CurrentGem;
        string label;
        float value;

        switch (measurementType)
        {
            case MeasurementType.RefractiveIndex:
                label = "Indice de réfraction";
                value = gem.refractiveIndex;
                break;
            case MeasurementType.Density:
                label = "Densité";
                value = gem.density;
                break;
            default:
                label = "Dureté";
                value = gem.hardness;
                break;
        }

        playerGemHolder.RecordMeasurement(measurementType.ToString());
        Debug.Log($"{toolName} sur {gem.displayName} : {label} ≈ {value}");
    }
}
