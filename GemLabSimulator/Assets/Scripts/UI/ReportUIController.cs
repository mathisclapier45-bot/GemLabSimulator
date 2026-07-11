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

        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Hardness, "Dureté", "F2");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.RefractiveIndex, "Indice de réfraction", "F3");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Density, "Densité", "F2");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Size, "Taille", "F1", " mm");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Weight, "Poids", "F2", " ct");

        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Pleochroism.ToString()))
            measured.Add(gem.pleochroic ? $"Pléochroïsme : {gem.pleochroicDescription}" : "Pléochroïsme : aucun");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.FluorescenceLongWave.ToString()))
            measured.Add($"Fluorescence UV longues : {gem.fluorescenceLongWave}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.FluorescenceShortWave.ToString()))
            measured.Add($"Fluorescence UV courtes : {gem.fluorescenceShortWave}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.OpticalCharacter.ToString()))
            measured.Add($"Polariscope : {gem.opticalCharacter}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.AbsorptionSpectrum.ToString()))
            measured.Add($"Spectroscope : {gem.absorptionSpectrum}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Inclusions.ToString()))
            measured.Add($"Microscope : {gem.inclusions}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.InterferenceFigure.ToString()))
            measured.Add($"Conoscope : {gem.interferenceFigure}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ChelseaFilter.ToString()))
            measured.Add($"Filtre Chelsea : {gem.chelseaFilterReaction}");

        measurementsText.text = measured.Count > 0
            ? $"Gemme sélectionnée : {gem.displayName}\n\nMesures relevées :\n" + string.Join("\n", measured)
            : $"Gemme sélectionnée : {gem.displayName}\n\nAucune mesure effectuée. Retourne examiner la gemme.";

        panel.SetActive(true);
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
