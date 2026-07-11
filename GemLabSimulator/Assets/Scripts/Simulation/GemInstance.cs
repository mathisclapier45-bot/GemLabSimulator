using System.Collections.Generic;
using UnityEngine;

public class GemInstance
{
    public GemDefinition Definition { get; }
    public bool IsIdentified { get; private set; }

    public GemOrigin Origin { get; }
    public string SaturationGrade { get; }
    public string ClarityGrade { get; }
    public string ClarityDescription { get; }
    public string InclusionDescription { get; }
    public float CaratWeight { get; }
    public float LengthMm { get; }
    public float WidthMm { get; }
    public float DepthMm { get; }
    public int FinalValue { get; }

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
        FinalValue = Mathf.RoundToInt(
            definition.baseValue * Origin.valueMultiplier * saturation.Multiplier * clarity.Multiplier * sizeValueMultiplier);
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
