using System.Collections.Generic;
using UnityEngine;

public class GemInstance
{
    public GemDefinition Definition { get; private set; }
    public bool IsIdentified { get; private set; }

    public GemOrigin Origin { get; private set; }
    public string SaturationGrade { get; private set; }
    public string ClarityGrade { get; private set; }
    public string ClarityDescription { get; private set; }
    public string InclusionDescription { get; private set; }
    public float CaratWeight { get; private set; }
    public float LengthMm { get; private set; }
    public float WidthMm { get; private set; }
    public float DepthMm { get; private set; }
    public string TreatmentGrade { get; private set; }
    public string TreatmentDescription { get; private set; }
    public bool IsSynthetic { get; private set; }
    public string AuthenticityGrade { get; private set; }
    public string AuthenticityDescription { get; private set; }
    public string GrowthStructureDescription { get; private set; }
    public int FinalValue { get; private set; }

    private readonly Dictionary<string, float> measuredValues = new Dictionary<string, float>();
    private readonly HashSet<string> observedProperties = new HashSet<string>();

    public GemInstance(GemDefinition definition)
    {
        Definition = definition;

        Origin = GemGrading.PickOrigin(definition.origins);

        var saturation = GemGrading.PickWeighted(GemGrading.SaturationGrades);
        SaturationGrade = saturation.Name;

        var clarity = GemGrading.PickWeighted(GemGrading.ClarityGrades);
        ClarityGrade = clarity.Name;
        ClarityDescription = clarity.Description;

        InclusionDescription = string.IsNullOrEmpty(Origin.inclusionOverride)
            ? definition.inclusions
            : Origin.inclusionOverride;

        // Skewed toward the average size; large stones are rare, matching real gem economics.
        var sizeFactor = Mathf.Lerp(0.5f, 2.5f, Mathf.Pow(Random.value, 2f));
        CaratWeight = definition.weightCarats * sizeFactor;

        var dimensionFactor = Mathf.Pow(sizeFactor, 1f / 3f);
        LengthMm = definition.lengthMm * dimensionFactor;
        WidthMm = definition.widthMm * dimensionFactor;
        DepthMm = definition.depthMm * dimensionFactor;

        var sizeValueMultiplier = Mathf.Pow(sizeFactor, 1.5f);

        var treatment = GemGrading.PickWeighted(GemGrading.TreatmentGrades);
        TreatmentGrade = treatment.Name;
        TreatmentDescription = treatment.Description;

        var authenticity = definition.forcedSynthetic
            ? GemGrading.SyntheticGrade
            : GemGrading.PickWeighted(GemGrading.AuthenticityGrades);
        AuthenticityGrade = authenticity.Name;
        AuthenticityDescription = authenticity.Description;
        IsSynthetic = authenticity.Name == "Synthétique";
        GrowthStructureDescription = IsSynthetic
            ? "Zones de croissance régulières, motifs anguleux ou courbes concentriques artificielles, typiques d'une croissance accélérée en laboratoire."
            : "Zones de croissance irrégulières et courbes, typiques d'une cristallisation naturelle lente.";

        FinalValue = Mathf.RoundToInt(
            definition.baseValue * Origin.valueMultiplier * saturation.Multiplier * clarity.Multiplier *
            sizeValueMultiplier * treatment.Multiplier * authenticity.Multiplier);
    }

    private GemInstance()
    {
    }

    public static GemInstance FromSaveData(GemDefinition definition, GemInstanceSaveData data)
    {
        var instance = new GemInstance
        {
            Definition = definition,
            IsIdentified = data.isIdentified,
            Origin = new GemOrigin
            {
                name = data.originName,
                valueMultiplier = data.originValueMultiplier,
                inclusionOverride = data.originInclusionOverride,
            },
            SaturationGrade = data.saturationGrade,
            ClarityGrade = data.clarityGrade,
            ClarityDescription = data.clarityDescription,
            InclusionDescription = data.inclusionDescription,
            CaratWeight = data.caratWeight,
            LengthMm = data.lengthMm,
            WidthMm = data.widthMm,
            DepthMm = data.depthMm,
            TreatmentGrade = data.treatmentGrade,
            TreatmentDescription = data.treatmentDescription,
            IsSynthetic = data.isSynthetic,
            AuthenticityGrade = data.authenticityGrade,
            AuthenticityDescription = data.authenticityDescription,
            GrowthStructureDescription = data.growthStructureDescription,
            FinalValue = data.finalValue,
        };

        for (var i = 0; i < data.measuredKeys.Count; i++)
            instance.measuredValues[data.measuredKeys[i]] = data.measuredValues[i];
        foreach (var key in data.observedKeys)
            instance.observedProperties.Add(key);

        return instance;
    }

    public GemInstanceSaveData ToSaveData()
    {
        return new GemInstanceSaveData
        {
            definitionName = Definition.name,
            isIdentified = IsIdentified,
            originName = Origin.name,
            originValueMultiplier = Origin.valueMultiplier,
            originInclusionOverride = Origin.inclusionOverride,
            saturationGrade = SaturationGrade,
            clarityGrade = ClarityGrade,
            clarityDescription = ClarityDescription,
            inclusionDescription = InclusionDescription,
            caratWeight = CaratWeight,
            lengthMm = LengthMm,
            widthMm = WidthMm,
            depthMm = DepthMm,
            treatmentGrade = TreatmentGrade,
            treatmentDescription = TreatmentDescription,
            isSynthetic = IsSynthetic,
            authenticityGrade = AuthenticityGrade,
            authenticityDescription = AuthenticityDescription,
            growthStructureDescription = GrowthStructureDescription,
            finalValue = FinalValue,
            measuredKeys = new List<string>(measuredValues.Keys),
            measuredValues = new List<float>(measuredValues.Values),
            observedKeys = new List<string>(observedProperties),
        };
    }

    public void MarkIdentified()
    {
        IsIdentified = true;
    }

    public void RecordMeasurement(string propertyKey, float value)
    {
        measuredValues[propertyKey] = value;
    }

    public bool HasMeasured(string propertyKey)
    {
        return measuredValues.ContainsKey(propertyKey);
    }

    public float GetMeasuredValue(string propertyKey)
    {
        return measuredValues.TryGetValue(propertyKey, out var value) ? value : 0f;
    }

    public void RecordObservation(string propertyKey)
    {
        observedProperties.Add(propertyKey);
    }

    public bool HasObserved(string propertyKey)
    {
        return observedProperties.Contains(propertyKey);
    }
}
