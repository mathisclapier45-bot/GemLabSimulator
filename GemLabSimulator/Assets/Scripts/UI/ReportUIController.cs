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

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Open(PlayerInventory inventory)
    {
        currentInventory = inventory;
        resultText.text = string.Empty;

        var gemInstance = inventory.SelectedGem;
        var gem = gemInstance.Definition;
        var measured = new List<string>();
        if (gemInstance.HasMeasured(ExaminationTool.MeasurementType.Hardness.ToString()))
            measured.Add($"Dureté ≈ {gemInstance.GetMeasuredValue(ExaminationTool.MeasurementType.Hardness.ToString()):F2}");
        if (gemInstance.HasMeasured(ExaminationTool.MeasurementType.RefractiveIndex.ToString()))
            measured.Add($"Indice de réfraction ≈ {gemInstance.GetMeasuredValue(ExaminationTool.MeasurementType.RefractiveIndex.ToString()):F3}");
        if (gemInstance.HasMeasured(ExaminationTool.MeasurementType.Density.ToString()))
            measured.Add($"Densité ≈ {gemInstance.GetMeasuredValue(ExaminationTool.MeasurementType.Density.ToString()):F2}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Pleochroism.ToString()))
            measured.Add(gem.pleochroic ? $"Pléochroïsme : {gem.pleochroicDescription}" : "Pléochroïsme : aucun");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Fluorescence.ToString()))
            measured.Add($"Fluorescence : {gem.fluorescenceDescription}");

        measurementsText.text = measured.Count > 0
            ? $"Gemme sélectionnée : {gem.displayName}\n\nMesures relevées :\n" + string.Join("\n", measured)
            : $"Gemme sélectionnée : {gem.displayName}\n\nAucune mesure effectuée. Retourne examiner la gemme.";

        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        currentInventory = null;
    }

    public void SubmitSpecies(string speciesName)
    {
        var gemInstance = currentInventory != null ? currentInventory.SelectedGem : null;
        if (gemInstance == null) return;

        var gem = gemInstance.Definition;
        if (speciesName == gem.displayName)
        {
            resultText.text = $"Rapport validé par le directeur ! +{gem.baseValue}€";
            currentInventory.GetComponent<PlayerWallet>()?.AddMoney(gem.baseValue);
            currentInventory.RemoveGem(gemInstance);
        }
        else
        {
            resultText.text = "Le directeur rejette le rapport : identification incorrecte.";
        }
    }
}
