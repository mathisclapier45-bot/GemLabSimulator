using System.Collections.Generic;
using UnityEngine;

public class GemInstance
{
    private static int nextSpecimenNumber = 1;
    private const float DonationChance = 0.03f;

    public GemDefinition Definition { get; private set; }
    public string SpecimenId { get; private set; }
    public string ClientName { get; private set; }
    public string DisplayId => $"{SpecimenId} — {ClientName}";
    public bool IsDonation { get; private set; }
    public bool IsRough { get; private set; }
    // Stones that arrive by mail belong to a client: they are only identified/appraised, then sent back.
    // Every other stone is one of the player's own specimens.
    public bool IsClientStone { get; private set; }
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

    // Relative to the species' typical weight, clamped so icons stay legible within a fixed UI slot.
    public float SizeScale => Mathf.Clamp(CaratWeight / Mathf.Max(Definition.weightCarats, 0.01f), 0.6f, 1.15f);
    public bool HasWindow { get; private set; }
    public string CutShape { get; private set; }
    public string CutStyle { get; private set; }

    // Once cut at the lapidary the chosen shape/style replaces the species default.
    public string ShapeName => CutShape ?? Definition.shape;
    public string CutStyleName => CutStyle ?? Definition.cutStyle;

    public Sprite DisplaySprite
    {
        get
        {
            if (IsRough) return Definition.roughIcon;
            if (CutShape != null) return GemIconFactory.Get(Definition, CutShape, CutStyle);
            return Definition.icon;
        }
    }

    private readonly Dictionary<string, float> measuredValues = new Dictionary<string, float>();
    private readonly HashSet<string> observedProperties = new HashSet<string>();

    public GemInstance(GemDefinition definition, GemOrigin forcedOrigin = null)
    {
        Definition = definition;
        SpecimenId = $"id{nextSpecimenNumber:D4}";
        nextSpecimenNumber++;
        IsDonation = Random.value < DonationChance;
        ClientName = IsDonation ? "Don anonyme" : ClientNames.GetRandom();

        Origin = forcedOrigin ?? GemGrading.PickOrigin(definition.origins);

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

        var treatment = definition.forcedTreatment
            ? GemGrading.ImpregnatedGrade
            : GemGrading.PickWeighted(GemGrading.TreatmentGrades);
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
        var specimenId = string.IsNullOrEmpty(data.specimenId)
            ? $"id{nextSpecimenNumber:D4}"
            : data.specimenId;

        var digitsOnly = new string(System.Array.FindAll(specimenId.ToCharArray(), char.IsDigit));
        if (int.TryParse(digitsOnly, out var specimenNumber) && specimenNumber >= nextSpecimenNumber)
            nextSpecimenNumber = specimenNumber + 1;

        var instance = new GemInstance
        {
            Definition = definition,
            SpecimenId = specimenId,
            ClientName = string.IsNullOrEmpty(data.clientName) ? ClientNames.GetRandom() : data.clientName,
            IsDonation = data.isDonation,
            IsRough = data.isRough,
            // Older saves have no flag: anything that is not rough or a donation is treated as a client's stone.
            IsClientStone = !data.isPersonalSpecimen && !data.isRough && !data.isDonation,
            IsIdentified = data.isIdentified,
            HasWindow = data.hasWindow,
            CutShape = string.IsNullOrEmpty(data.cutShape) ? null : data.cutShape,
            CutStyle = string.IsNullOrEmpty(data.cutStyle) ? null : data.cutStyle,
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
            specimenId = SpecimenId,
            clientName = ClientName,
            isDonation = IsDonation,
            isRough = IsRough,
            isPersonalSpecimen = !IsClientStone,
            isIdentified = IsIdentified,
            hasWindow = HasWindow,
            cutShape = CutShape,
            cutStyle = CutStyle,
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

    // A polished window lets light into a rough stone so its inclusions and clarity can be observed.
    public void OpenWindow()
    {
        HasWindow = true;
    }

    public void MarkAsClientStone()
    {
        IsClientStone = true;
    }

    public void MarkAsRough()
    {
        IsRough = true;
    }

    // Turns a rough stone into a cut one: weight and dimensions shrink, value is recomputed by the lapidary.
    public void ApplyCut(string shape, string style, float newCaratWeight, int newFinalValue)
    {
        var ratio = Mathf.Max(newCaratWeight / Mathf.Max(CaratWeight, 0.001f), 0.01f);
        var dimensionFactor = Mathf.Pow(ratio, 1f / 3f);
        LengthMm *= dimensionFactor;
        WidthMm *= dimensionFactor;
        DepthMm *= dimensionFactor;
        CaratWeight = newCaratWeight;
        FinalValue = newFinalValue;
        CutShape = shape;
        CutStyle = style;
        IsRough = false;
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
