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
    // Tone / saturation step of the color ("Vif", "Pastel", "Profond"...). The full description is ColorName.
    public string SaturationGrade { get; private set; }

    // Color in the HSB model of the gemology labs, graded like a lab report ("Rouge vif", "Rose pastel"...). Rare colors carry
    // a trade name ("Sang de pigeon", "Bleu royal", "Padparadscha"...), only awarded to clean, untreated stones.
    public bool HasColor { get; private set; }
    public float ColorHue { get; private set; }
    public float ColorSaturation { get; private set; }
    public float ColorBrightness { get; private set; }
    public bool IsFancyDiamond { get; private set; }
    public string ColorName { get; private set; }
    public string TradeName { get; private set; }

    // GemLab laboratory report printed for this stone (see ReportPrinterUI). It describes the stone as it was examined,
    // so cutting it again cancels it.
    public bool HasReport { get; private set; }
    public string ReportNumber { get; private set; }
    public int ReportDay { get; private set; }

    public void IssueReport(int day)
    {
        HasReport = true;
        ReportDay = day;
        ReportNumber = $"GL-{100000 + StoneOptics.StableHash((SpecimenId ?? "") + "|report") % 900000}";
    }

    private GemColorValue ColorValue => new GemColorValue(ColorHue, ColorSaturation, ColorBrightness);
    public Color BodyColor => HasColor ? GemColorSystem.BodyColor(ColorValue) : Definition.iconColor;
    public Color FacetColor => HasColor ? GemColorSystem.FacetColor(ColorValue) : Definition.iconFacetColor;

    // Color and trade name as one line: "Rouge vif — Sang de pigeon".
    public string ColorDescription => string.IsNullOrEmpty(TradeName) ? ColorName : $"{ColorName} — {TradeName}";

    public string ClarityGrade { get; private set; }
    public string ClarityDescription { get; private set; }
    public string InclusionDescription { get; private set; }
    public float CaratWeight { get; private set; }

    // What the identification report is worth: paid by the client when the identified stone is sent back to them.
    public int IdentificationReportFee => CaratWeight > 5f ? 90 : (CaratWeight >= 1f ? 70 : 50);
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

    // Diamonds sold by the dealers of the Diamond Quarter: colour grade (D..Z), clarity grade (FL..I3) and the certificate
    // delivered with the stone. All null for every other stone.
    public string ColorGrade { get; private set; }
    public string DiamondClarity { get; private set; }
    public DiamondCertificate Certificate { get; private set; }

    // Relative to the species' typical weight, clamped so icons stay legible within a fixed UI slot.
    public float SizeScale => Mathf.Clamp(CaratWeight / Mathf.Max(Definition.weightCarats, 0.01f), 0.6f, 1.15f);
    public bool HasWindow { get; private set; }
    public string CutShape { get; private set; }
    public string CutStyle { get; private set; }

    // Brilliant-type cuts end in a pointed culet; about one stone in fifteen has an open culet (one more facet).
    // Stable per specimen: the same stone always looks the same.
    public bool HasOpenCulet => !IsRough && GemMesh.HasCulet(CutStyleName) && StoneOptics.StableHash((SpecimenId ?? "") + "|culet") % 100 < 7;

    // Real number of facets of the cut (e.g. 57 / 58 for a round brilliant, 50 for an emerald cut); 0 for a cabochon or rough stone.
    public int FacetCount => IsRough || CutStyleName == "Cabochon" ? 0 : GemMesh.Get(ShapeName, CutStyleName, HasOpenCulet).FacetCount;

    // Short text for the loupe and microscope panels; null for a rough stone.
    public string FacetSummary
    {
        get
        {
            if (IsRough) return null;
            if (CutStyleName == "Cabochon") return "aucune (cabochon poli)";
            var count = FacetCount;
            if (!GemMesh.HasCulet(CutStyleName)) return $"{count} (pavillon à degrés, quille)";
            return HasOpenCulet ? $"{count} (colette ouverte, rare)" : $"{count} (colette pointue)";
        }
    }

    // Once cut at the lapidary the chosen shape/style replaces the species default.
    public string ShapeName => CutShape ?? Definition.shape;
    public string CutStyleName => CutStyle ?? Definition.cutStyle;

    public Sprite DisplaySprite
    {
        get
        {
            if (IsRough) return HasColor ? GemIconFactory.GetRough(Definition, BodyColor) : Definition.GetRoughIcon();
            return HasColor
                ? GemIconFactory.Get(Definition, ShapeName, CutStyleName, BodyColor, FacetColor)
                : GemIconFactory.Get(Definition, ShapeName, CutStyleName);
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

        // A few percent of the diamonds are colored ("fancy"); every other color follows the species' own color range.
        var fancy = definition.name == "Diamant" && Random.value < 0.04f;
        var colorValue = fancy
            ? GemColorSystem.SampleFancyDiamond(() => Random.value)
            : GemColorSystem.Sample(definition, () => Random.value);
        var colorGrade = SetColor(colorValue, fancy);

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
        FitDimensionsToShape();

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

        // Trade names are only awarded to clean stones with no treatment other than heating.
        var cleanStone = (clarity.Name == "Eye-clean" || clarity.Name == "Loupe clean")
                         && (treatment.Name == "Non traité" || treatment.Name == "Chauffé") && !IsSynthetic;
        TradeName = cleanStone ? GemColorSystem.TradeName(definition.name, colorValue) : null;

        var colorMultiplier = colorGrade.ValueMultiplier * GemColorSystem.TradeMultiplier(TradeName)
                              * (fancy ? GemColorSystem.FancyDiamondRarity(colorValue) : 1f);

        FinalValue = Mathf.RoundToInt(
            definition.baseValue * Origin.valueMultiplier * colorMultiplier * clarity.Multiplier *
            sizeValueMultiplier * treatment.Multiplier * authenticity.Multiplier);
    }

    private GemColorGrade SetColor(GemColorValue color, bool fancy)
    {
        HasColor = true;
        ColorHue = color.Hue;
        ColorSaturation = color.Saturation;
        ColorBrightness = color.Brightness;
        IsFancyDiamond = fancy;

        var grade = GemColorSystem.Describe(color, fancy);
        ColorName = grade.Name;
        SaturationGrade = grade.Tone;
        return grade;
    }

    // Dealer diamonds: the D-to-Z colorless scale, drawn as an ever so slightly yellow body from K on.
    private void SetDiamondLetterColor(int colorIndex, string letterDescription)
    {
        var t = Mathf.Clamp01(colorIndex / 22f);
        var color = new GemColorValue(Mathf.Lerp(215f, 52f, Mathf.Clamp01(colorIndex / 5f)), Mathf.Lerp(0.02f, 0.62f, t * t), Mathf.Lerp(0.96f, 0.90f, t));
        HasColor = true;
        ColorHue = color.Hue;
        ColorSaturation = color.Saturation;
        ColorBrightness = color.Brightness;
        IsFancyDiamond = false;
        ColorName = letterDescription;
    }

    private GemInstance()
    {
    }

    // Length and width follow the proportions of the stone's shape (a round stone is as wide as long), keeping the area.
    private void FitDimensionsToShape()
    {
        if (LengthMm <= 0f || WidthMm <= 0f) return;
        var ratio = GemMesh.AspectRatio(ShapeName);
        LengthMm = Mathf.Sqrt(LengthMm * WidthMm / ratio);
        WidthMm = LengthMm * ratio;
    }

    // A natural diamond as sold by a dealer: fully specified, identified, with its certificate.
    public static GemInstance CreateDiamond(GemDefinition definition, GemOrigin origin, float carat, int colorIndex, int clarityIndex,
        string shape, string style, float lengthMm, float widthMm, float depthMm, int value, string issuer, int issueDay, string certificateNumber)
    {
        var colorGrade = DiamondGrading.Color(colorIndex);
        var clarityGrade = DiamondGrading.Clarity(clarityIndex);
        var tier = DiamondGrading.GameClarityTier(clarityIndex);
        var tierDescription = "";
        foreach (var grade in GemGrading.ClarityGrades)
            if (grade.Name == tier) tierDescription = grade.Description;

        var instance = new GemInstance
        {
            Definition = definition,
            SpecimenId = $"id{nextSpecimenNumber:D4}",
            ClientName = issuer,
            IsIdentified = true,
            CutShape = shape,
            CutStyle = style,
            Origin = origin ?? GemGrading.PickOrigin(definition.origins),
            SaturationGrade = $"qualité {colorGrade}",
            ClarityGrade = tier,
            ClarityDescription = tierDescription,
            InclusionDescription = definition.inclusions,
            CaratWeight = carat,
            LengthMm = lengthMm,
            WidthMm = widthMm,
            DepthMm = depthMm,
            TreatmentGrade = "Non traité",
            TreatmentDescription = "Aucune trace de traitement détectée.",
            IsSynthetic = false,
            AuthenticityGrade = "Naturelle",
            AuthenticityDescription = "Signature Raman cohérente avec une croissance naturelle.",
            GrowthStructureDescription = "Zones de croissance irrégulières et courbes, typiques d'une cristallisation naturelle lente.",
            FinalValue = value,
            ColorGrade = colorGrade,
            DiamondClarity = clarityGrade,
            Certificate = new DiamondCertificate
            {
                number = certificateNumber,
                issuer = issuer,
                issueDay = issueDay,
                colorGrade = colorGrade,
                clarityGrade = clarityGrade,
                caratWeight = carat,
                shape = shape,
                cutStyle = style,
            },
        };
        nextSpecimenNumber++;
        instance.SetDiamondLetterColor(colorIndex, DiamondGrading.ColorDescription(colorIndex));
        return instance;
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
            ColorGrade = string.IsNullOrEmpty(data.colorGrade) ? null : data.colorGrade,
            DiamondClarity = string.IsNullOrEmpty(data.diamondClarity) ? null : data.diamondClarity,
            Certificate = string.IsNullOrEmpty(data.certificateNumber)
                ? null
                : new DiamondCertificate
                {
                    number = data.certificateNumber,
                    issuer = data.certificateIssuer,
                    issueDay = data.certificateDay,
                    colorGrade = data.colorGrade,
                    clarityGrade = data.diamondClarity,
                    caratWeight = data.caratWeight,
                    shape = data.cutShape,
                    cutStyle = data.cutStyle,
                },
        };

        if (data.hasColor)
        {
            instance.SetColor(new GemColorValue(data.colorHue, data.colorSaturation, data.colorBrightness), data.isFancyDiamond);
            if (!string.IsNullOrEmpty(instance.ColorGrade) && !data.isFancyDiamond)
                instance.SetDiamondLetterColor(DiamondGrading.ColorScale.IndexOf(instance.ColorGrade[0]), DiamondGrading.ColorDescription(DiamondGrading.ColorScale.IndexOf(instance.ColorGrade[0])));
            instance.TradeName = string.IsNullOrEmpty(data.tradeName) ? null : data.tradeName;
        }
        if (data.hasReport)
        {
            instance.HasReport = true;
            instance.ReportNumber = data.reportNumber;
            instance.ReportDay = data.reportDay;
        }
        else if (definition != null)
        {
            // Older saves have no color: give the stone a stable one from its specimen number.
            var rng = new System.Random((int)StoneOptics.StableHash(specimenId));
            instance.SetColor(GemColorSystem.Sample(definition, () => (float)rng.NextDouble()), false);
        }

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
            colorGrade = ColorGrade,
            hasColor = HasColor,
            colorHue = ColorHue,
            colorSaturation = ColorSaturation,
            colorBrightness = ColorBrightness,
            isFancyDiamond = IsFancyDiamond,
            tradeName = TradeName,
            hasReport = HasReport,
            reportNumber = ReportNumber,
            reportDay = ReportDay,
            diamondClarity = DiamondClarity,
            certificateNumber = Certificate?.number,
            certificateIssuer = Certificate?.issuer,
            certificateDay = Certificate != null ? Certificate.issueDay : 0,
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
        // Measurements taken before the cut no longer describe the stone.
        measuredValues.Remove(DimensionReport.LengthKey);
        measuredValues.Remove(DimensionReport.WidthKey);
        measuredValues.Remove(DimensionReport.DepthKey);
        observedProperties.Remove(ExaminationTool.MeasurementType.Dimensions.ToString());

        HasReport = false;
        CaratWeight = newCaratWeight;
        FinalValue = newFinalValue;
        CutShape = shape;
        CutStyle = style;
        IsRough = false;
        FitDimensionsToShape();
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
