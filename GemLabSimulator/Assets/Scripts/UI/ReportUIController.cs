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

        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.RefractiveIndex, "Indice de réfraction", "F3");
        if (gemInstance.HasObserved("RefractiveIndexOTL"))
            measured.Add("Indice de réfraction : OTL (hors limite du prisme, IR > 1.81)");
        AddIfMeasured(measured, gemInstance, ExaminationTool.MeasurementType.Density, "Densité", "F2");

        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.VisualExamination.ToString()))
        {
            measured.Add($"Éclat : {gem.luster}");
            measured.Add($"Couleur : {gem.color} ({gemInstance.SaturationGrade})");
            measured.Add($"Transparence : {gem.transparency}");
            measured.Add($"Forme : {gem.shape}");
            measured.Add($"Style de taille : {gem.cutStyle}");
        }
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Dimensions.ToString()))
            measured.Add($"Dimensions : {gemInstance.LengthMm:F1} x {gemInstance.WidthMm:F1} x {gemInstance.DepthMm:F1} mm");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Weight.ToString()))
            measured.Add($"Poids : {gemInstance.CaratWeight:F2} ct");
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
        {
            measured.Add($"Clarté : {gemInstance.ClarityGrade} ({gemInstance.ClarityDescription})");
            measured.Add($"Inclusions : {gemInstance.InclusionDescription}");
            measured.Add($"Origine probable : {gemInstance.Origin.name}");
        }
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.InterferenceFigure.ToString()))
            measured.Add($"Conoscope : {gem.interferenceFigure}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ChelseaFilter.ToString()))
            measured.Add($"Filtre Chelsea : {gem.chelseaFilterReaction}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Magnetism.ToString()))
            measured.Add($"Aimant : {gem.magnetism}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Treatment.ToString()))
            measured.Add($"Traitement (FTIR) : {gemInstance.TreatmentGrade}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.Authenticity.ToString()))
            measured.Add($"Authenticité (Raman) : {gemInstance.AuthenticityGrade}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString()))
            measured.Add($"Origine confirmée (UV-Vis-NIR) : {gemInstance.Origin.name}");
        if (gemInstance.HasObserved(ExaminationTool.MeasurementType.GrowthStructure.ToString()))
            measured.Add($"Structure de croissance (MEB) : {gemInstance.GrowthStructureDescription}");

        var identityLine = gemInstance.IsIdentified
            ? $"Gemme : {gem.displayName} (identifiée)"
            : "Gemme sélectionnée : spécimen non identifié";

        measurementsText.text = measured.Count > 0
            ? $"{identityLine}\n\nMesures relevées :\n" + string.Join("\n", measured)
            : $"{identityLine}\n\nAucune mesure effectuée. Retourne examiner la gemme.";

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
            resultText.text = $"Rapport validé par le directeur ! Identifiée comme {gem.displayName}. +{gemInstance.FinalValue}€";
            currentInventory.GetComponent<PlayerWallet>()?.AddMoney(gemInstance.FinalValue);
            gemInstance.MarkIdentified();
        }
        else
        {
            resultText.text = "Le directeur rejette le rapport : identification incorrecte.";
        }
    }
}
