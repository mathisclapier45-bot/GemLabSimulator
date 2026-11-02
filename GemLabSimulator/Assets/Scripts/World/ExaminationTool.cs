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
        ThermalConductivity,
        ElectricalConductivity,
        GrowthStructure,
        Magnetism,
        DiamondScreening,
        DiamondGrowthImaging,
        ElementalAnalysis
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
        ToolInfoUI.Instance?.ShowButtonFor(toolName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() != playerInventory) return;
        if (measurementType == MeasurementType.Weight) WeightScaleUI.Instance?.Close();
        if (measurementType == MeasurementType.RefractiveIndex) RefractometerUI.Instance?.Close();
        if (measurementType == MeasurementType.OpticalCharacter) PolariscopeUI.Instance?.Close();
        if (measurementType == MeasurementType.AbsorptionSpectrum) SpectroscopeUI.Instance?.Close();
        if (measurementType == MeasurementType.Density) HydrostaticBalanceUI.Instance?.Close();
        if (measurementType == MeasurementType.FluorescenceLongWave || measurementType == MeasurementType.FluorescenceShortWave)
            UvCabinUI.Instance?.Close();
        if (measurementType == MeasurementType.ChelseaFilter) ChelseaFilterUI.Instance?.Close();
        if (measurementType == MeasurementType.Dimensions) MicrometerUI.Instance?.Close();
        if (measurementType == MeasurementType.Magnetism) MagnetUI.Instance?.Close();
        if (measurementType == MeasurementType.Pleochroism) DichroscopeUI.Instance?.Close();
        if (measurementType == MeasurementType.VisualExamination) LoupeUI.Instance?.Close();
        if (measurementType == MeasurementType.Inclusions) MicroscopeUI.Instance?.Close();
        if (measurementType == MeasurementType.ConfirmedOrigin) UvVisNirUI.Instance?.Close();
        if (measurementType == MeasurementType.Treatment) FtirUI.Instance?.Close();
        if (measurementType == MeasurementType.Authenticity) RamanUI.Instance?.Close();
        if (measurementType == MeasurementType.DiamondGrowthImaging) DiamondViewUI.Instance?.Close();
        if (measurementType == MeasurementType.DiamondScreening) DiamondSureUI.Instance?.Close();
        if (measurementType == MeasurementType.GrowthStructure) MebUI.Instance?.Close();
        if (measurementType == MeasurementType.ElementalAnalysis) EdxrfUI.Instance?.Close();
        if (measurementType == MeasurementType.ThermalConductivity || measurementType == MeasurementType.ElectricalConductivity) ConductivityTesterUI.Instance?.Close();
        playerInRange = false;
        playerInventory = null;
        ToolInfoUI.Instance?.HideButtonIfCurrent(toolName);
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        Sfx.Play(Sfx.Id.ToolOpen);
        // The carat balance has its own drag-and-drop interface.
        if (measurementType == MeasurementType.Weight && WeightScaleUI.TryOpen()) return;

        // Same for the refractometer: hemicylinder, graduated scale and rotating lens.
        if (measurementType == MeasurementType.RefractiveIndex && RefractometerUI.TryOpen()) return;

        // And the polariscope: rotate the stone between crossed polarizers.
        if (measurementType == MeasurementType.OpticalCharacter && PolariscopeUI.TryOpen()) return;

        // The spectroscope shows the absorption spectrum with hover wavelengths.
        if (measurementType == MeasurementType.AbsorptionSpectrum && SpectroscopeUI.TryOpen()) return;

        // The hydrostatic balance: weight in water, then density once the air weight is known.
        if (measurementType == MeasurementType.Density && HydrostaticBalanceUI.TryOpen()) return;

        // Both UV stations open the same UV cabinet, with a long-wave and a short-wave lamp.
        if ((measurementType == MeasurementType.FluorescenceLongWave || measurementType == MeasurementType.FluorescenceShortWave)
            && UvCabinUI.TryOpen()) return;

        // Chelsea filter: reflected light and a red reaction for chromium-colored stones.
        if (measurementType == MeasurementType.ChelseaFilter && ChelseaFilterUI.TryOpen()) return;

        // Micrometer: place the stone in three positions to read length, width and depth.
        if (measurementType == MeasurementType.Dimensions && MicrometerUI.TryOpen()) return;

        // Magnet: the gem slides toward it if it is attracted.
        if (measurementType == MeasurementType.Magnetism && MagnetUI.TryOpen()) return;

        // Dichroscope: turn the stone in every direction to see two or three colors.
        if (measurementType == MeasurementType.Pleochroism && DichroscopeUI.TryOpen()) return;

        // 10x loupe: rotate the stone, move the lens, look for inclusions and surface defects.
        if (measurementType == MeasurementType.VisualExamination && LoupeUI.TryOpen()) return;

        // Microscope: 10x-40x, reflected light or darkfield, fine focus. Rough stones need a window (handled inside).
        if (measurementType == MeasurementType.Inclusions && MicroscopeUI.TryOpen()) return;

        // UV-Vis-NIR spectrophotometer: drop the stone in the box, press Start, read the absorbance curve.
        if (measurementType == MeasurementType.ConfirmedOrigin && UvVisNirUI.TryOpen()) return;

        // FTIR spectrometer: drop the stone in the compartment, press Start, read the infrared curve (fillers, heating, diamond type).
        if (measurementType == MeasurementType.Treatment && FtirUI.TryOpen()) return;

        // Raman spectrometer: drop the stone in the chamber, choose host / inclusion / fracture, press Start, match the spectrum.
        if (measurementType == MeasurementType.Authenticity && RamanUI.TryOpen()) return;

        // DiamondView: deep-UV fluorescence image of a diamond, whose growth pattern tells natural, CVD and HPHT apart.
        if (measurementType == MeasurementType.DiamondGrowthImaging && DiamondViewUI.TryOpen()) return;

        // DiamondSure: UV transmission screening, Pass (type I diamond) or Refer (needs further testing).
        if (measurementType == MeasurementType.DiamondScreening && DiamondSureUI.TryOpen()) return;

        // Scanning electron microscope: surface image, cathodoluminescence (growth structure) or X-ray analysis (elements).
        if (measurementType == MeasurementType.GrowthStructure && MebUI.TryOpen()) return;

        // EDXRF: X-ray fluorescence spectrum of the trace elements (deposit, synthesis, lead glass fillings).
        if (measurementType == MeasurementType.ElementalAnalysis && EdxrfUI.TryOpen()) return;

        // Thermal and electrical testers: drop the stone in the box, grab the tester and put its tip on the stone.
        if (measurementType == MeasurementType.ThermalConductivity && ConductivityTesterUI.TryOpen(ConductivityTesterUI.Mode.Thermal)) return;
        if (measurementType == MeasurementType.ElectricalConductivity && ConductivityTesterUI.TryOpen(ConductivityTesterUI.Mode.Electrical)) return;

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
                RevealOverTheLimit(gemInstance, gem);
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
                RevealQualitative(gemInstance, "Caractère optique", OpticalDescriptions.PolariscopeText(gem));
                break;
            case MeasurementType.AbsorptionSpectrum:
                RevealQualitative(gemInstance, "Spectre d'absorption", gem.absorptionSpectrum);
                SpectrumUI.Instance?.Show(gem);
                break;
            case MeasurementType.Inclusions when gemInstance.IsRough && !gemInstance.HasWindow:
                ToolFeedbackUI.Instance.ShowMessage(
                    $"{toolName} — La surface brute diffuse la lumière : impossible de voir à l'intérieur. " +
                    "Faites polir une fenêtre à l'atelier lapidaire.");
                break;
            case MeasurementType.Inclusions:
                RevealInclusions(gemInstance);
                break;
            case MeasurementType.InterferenceFigure:
                RevealQualitative(gemInstance, "Figure d'interférence", OpticalDescriptions.ConoscopeText(gem));
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
                RevealQualitative(gemInstance, "Origine confirmée (spectrophotométrie UV-Vis-NIR)", gemInstance.Origin.name);
                break;
            case MeasurementType.GrowthStructure:
                RevealQualitative(gemInstance, "Structure de croissance", gemInstance.GrowthStructureDescription);
                break;
            case MeasurementType.Magnetism:
                RevealQualitative(gemInstance, "Réaction au magnétisme", gem.magnetism);
                break;
            case MeasurementType.ThermalConductivity:
                RevealQualitative(gemInstance, "Conductivité thermique",
                    gem.isThermallyConductiveLikeDiamond
                        ? "Conduction thermique très élevée, comparable à celle d'un diamant (à confirmer par un test de conductivité électrique)."
                        : "Conduction thermique faible, incompatible avec un diamant.");
                break;
            case MeasurementType.ElectricalConductivity:
                RevealQualitative(gemInstance, "Conductivité électrique",
                    gem.isElectricallyConductive
                        ? "Conductrice à l'électricité — signature typique d'un carbure de silicium synthétique."
                        : "Non conductrice à l'électricité — cohérent avec un diamant ou la plupart des autres gemmes.");
                break;
            case MeasurementType.DiamondScreening:
                RevealQualitative(gemInstance, "Écran DiamondSure",
                    gemInstance.IsSynthetic
                        ? "Résultat : Refer — transmission UV atypique, examen complémentaire nécessaire."
                        : "Résultat : Pass — fluorescence UV cohérente avec un diamant naturel de type Ia.");
                break;
            case MeasurementType.DiamondGrowthImaging:
                RevealQualitative(gemInstance, "Imagerie DiamondView",
                    gemInstance.IsSynthetic
                        ? "Motifs de croissance concentriques ou sectorisés sous fluorescence UV, typiques d'une synthèse HPHT ou CVD."
                        : "Zonation de croissance irrégulière sous fluorescence UV, typique d'une cristallisation naturelle lente.");
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
            $"{toolName} — Éclat : {gem.luster} | Couleur : {gemInstance.ColorName} | " +
            $"Transparence : {gem.TransparencyLabel} | Forme : {gemInstance.ShapeName} | Style de taille : {gemInstance.CutStyleName}");
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

    private void RevealOverTheLimit(GemInstance gemInstance, GemDefinition gem)
    {
        gemInstance.RecordObservation("RefractiveIndexOTL");
        ToolFeedbackUI.Instance.ShowMessage(
            $"{toolName} — Indice de réfraction : OTL (hors limite du prisme, IR > 1.81) | " +
            $"Biréfringence : {OpticalDescriptions.BirefringenceText(gem)}");
    }

    private void RevealInclusions(GemInstance gemInstance)
    {
        gemInstance.RecordObservation(measurementType.ToString());
        ToolFeedbackUI.Instance.ShowMessage(
            $"{toolName} — Clarté : {gemInstance.ClarityGrade} ({gemInstance.ClarityDescription}) | " +
            $"Inclusions : {gemInstance.InclusionDescription}");
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
            var suffix = measurementType == MeasurementType.RefractiveIndex
                ? $" | Biréfringence : {OpticalDescriptions.BirefringenceText(gem)}"
                : "";
            ToolFeedbackUI.Instance.ShowMessage($"{toolName} — {label} ≈ {measured:F3} ({tierLabel}){suffix}");
        });
    }
}
