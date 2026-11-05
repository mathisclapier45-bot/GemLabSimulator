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

    // The coating of the pavilion given at the treatments laboratory (see CoatingCatalog): its name, and how strongly it shows the rainbow sheen.
    public string CoatingName { get; private set; }
    public float CoatingIridescence { get; private set; }
    public bool IsCoated => !string.IsNullOrEmpty(CoatingName);

    // The rutile that crystallised in the furnace of the treatments laboratory: a cabochon of this stone shows a star (see HeatingCatalog).
    public bool HeatStar { get; private set; }

    // The product that fills the fissures or the pores of the stone, put in at the treatments laboratory (see FillingCatalog).
    public string FillerName { get; private set; }
    public bool IsFilled => !string.IsNullOrEmpty(FillerName);
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

    // Rough diamonds sold on the 3rd floor of the tower: crystal form, share of the weight that will remain once cut, and the colour and
    // clarity (indexes in the diamond scales) that only the cut will reveal. Empty / -1 for every other stone.
    public string RoughForm { get; private set; }
    public float RoughYield { get; private set; }
    public int RoughColorIndex { get; private set; } = -1;
    public int RoughClarityIndex { get; private set; } = -1;

    // What the player paid for the stone and what its cutting cost (0 when unknown), shown under its estimated value.
    public int PurchasePrice { get; private set; }
    public int CuttingCost { get; private set; }

    public void RecordPurchase(int price)
    {
        PurchasePrice = Mathf.Max(0, price);
    }

    public void AddCuttingCost(int cost)
    {
        CuttingCost += Mathf.Max(0, cost);
    }

    public void SetCosts(int purchase, int cutting)
    {
        PurchasePrice = Mathf.Max(0, purchase);
        CuttingCost = Mathf.Max(0, cutting);
    }

    // Quality of the cut (0 = excellent ... 3 = fair) given by the diamond cutter, which decides the proportions, polish and symmetry the
    // grading office will find. -1 for a stone that was not cut by one of them.
    public int CutQualityIndex { get; private set; } = -1;

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
            if (CoatingIridescence >= 0.25f && HasColor)
                return GemIconFactory.GetIridescent(Definition, ShapeName, CutStyleName, BodyColor, FacetColor, CoatingIridescence);
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

        // Natural or synthetic first: the clarity of a stone and its inclusions depend on it, as on its species and deposit.
        var authenticity = definition.forcedSynthetic
            ? GemGrading.SyntheticGrade
            : GemGrading.PickWeighted(GemGrading.AuthenticityGrades);
        AuthenticityGrade = authenticity.Name;
        AuthenticityDescription = authenticity.Description;
        IsSynthetic = authenticity.Name == "Synthétique";

        var clarity = InclusionCatalog.PickClarity(definition, Origin, IsSynthetic);
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

    // Dense rutile silk in a cabochon makes a star (six rays; four in a garnet).
    public int AsterismRays
    {
        get
        {
            if (IsRough || IsSynthetic || CutStyleName != "Cabochon") return 0;
            if (HeatStar) return 6;
            if (ClarityGrade == "Loupe clean" || ClarityGrade == "Eye-clean") return 0;
            if (InclusionCatalog.ProfileFor(this).Share(FeatureKind.Needle, 0) < 0.18f) return 0;
            if (StoneOptics.StableHash((SpecimenId ?? "") + "|star") % 100 >= 65) return 0;
            return (Definition.displayName ?? "").ToLowerInvariant().Contains("grenat") ? 4 : 6;
        }
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

    // A rough natural diamond as sold by a rough dealer: identified, with no grade and no certificate. Its colour shows only as a tone.
    public static GemInstance CreateRoughDiamond(GemDefinition definition, float carat, int colorIndex, int clarityIndex, string form, float yield,
        float lengthMm, float widthMm, float depthMm, int value, string seller)
    {
        var tier = DiamondGrading.GameClarityTier(clarityIndex);
        var tierDescription = "";
        foreach (var grade in GemGrading.ClarityGrades)
            if (grade.Name == tier) tierDescription = grade.Description;

        var instance = new GemInstance
        {
            Definition = definition,
            SpecimenId = $"id{nextSpecimenNumber:D4}",
            ClientName = seller,
            IsIdentified = true,
            IsRough = true,
            Origin = GemGrading.PickOrigin(definition.origins),
            SaturationGrade = "brut",
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
            RoughForm = form,
            RoughYield = yield,
            RoughColorIndex = colorIndex,
            RoughClarityIndex = clarityIndex,
        };
        nextSpecimenNumber++;
        instance.SetDiamondLetterColor(colorIndex, DiamondGrading.ColorDescription(colorIndex));
        return instance;
    }

    // A diamond cut by one of the cutters of the tower: finished, but with no grade and no certificate yet. Its colour and clarity are
    // the ones of the rough (the cut can have improved or damaged the clarity), kept hidden until the grading office finds them.
    public static GemInstance CreateCutDiamond(GemDefinition definition, float carat, int colorIndex, int clarityIndex, string shape, string style,
        float lengthMm, float widthMm, float depthMm, int value, string cutter, int cutQuality)
    {
        var tier = DiamondGrading.GameClarityTier(clarityIndex);
        var tierDescription = "";
        foreach (var grade in GemGrading.ClarityGrades)
            if (grade.Name == tier) tierDescription = grade.Description;

        var instance = new GemInstance
        {
            Definition = definition,
            SpecimenId = $"id{nextSpecimenNumber:D4}",
            ClientName = cutter,
            IsIdentified = true,
            CutShape = shape,
            CutStyle = style,
            Origin = GemGrading.PickOrigin(definition.origins),
            SaturationGrade = "taillé",
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
            RoughColorIndex = colorIndex,
            RoughClarityIndex = clarityIndex,
            CutQualityIndex = cutQuality,
        };
        nextSpecimenNumber++;
        instance.SetDiamondLetterColor(colorIndex, DiamondGrading.ColorDescription(colorIndex));
        return instance;
    }

    // The stone has been graded by the grading office: its grades are fixed, it carries the certificate and it is worth a little more.
    public void Certify(DiamondCertificate certificate, DiamondGrade grade)
    {
        Certificate = certificate;
        DepthMm = grade.DepthMm;
        DiamondClarity = grade.ClarityGrade;
        if (grade.Fancy)
        {
            ColorGrade = "Fancy";
        }
        else
        {
            ColorGrade = grade.ColorGrade;
            SetDiamondLetterColor(grade.ColorIndex, grade.ColorText);
        }
        FinalValue = Mathf.RoundToInt(FinalValue * 1.15f);
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
            RoughForm = string.IsNullOrEmpty(data.roughForm) ? null : data.roughForm,
            RoughYield = data.roughYield,
            RoughColorIndex = data.roughColor - 1,
            RoughClarityIndex = data.roughClarity - 1,
            CutQualityIndex = data.cutQuality - 1,
            PurchasePrice = data.purchasePrice,
            CuttingCost = data.cuttingCost,
            HeatStar = data.heatStar,
            FillerName = string.IsNullOrEmpty(data.fillerName) ? null : data.fillerName,
            CoatingName = string.IsNullOrEmpty(data.coatingName) ? null : data.coatingName,
            CoatingIridescence = data.coatingIridescence,
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
            roughForm = RoughForm,
            roughYield = RoughYield,
            roughColor = RoughColorIndex + 1,
            roughClarity = RoughClarityIndex + 1,
            cutQuality = CutQualityIndex + 1,
            purchasePrice = PurchasePrice,
            cuttingCost = CuttingCost,
            heatStar = HeatStar,
            fillerName = FillerName,
            coatingName = CoatingName,
            coatingIridescence = CoatingIridescence,
            certificateNumber = Certificate?.number,
            certificateIssuer = Certificate?.issuer,
            certificateDay = Certificate != null ? Certificate.issueDay : 0,
            measuredKeys = new List<string>(measuredValues.Keys),
            measuredValues = new List<float>(measuredValues.Values),
            observedKeys = new List<string>(observedProperties),
        };
    }

    // A stone the laboratory hands over to be treated: it has no treatment yet, and its value is the one of an untreated stone.
    public void ClearTreatment()
    {
        foreach (var grade in GemGrading.TreatmentGrades)
        {
            if (grade.Name != TreatmentGrade) continue;
            FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue / Mathf.Max(0.01f, grade.Multiplier)));
            break;
        }
        TreatmentGrade = GemGrading.TreatmentGrades[0].Name;
        TreatmentDescription = GemGrading.TreatmentGrades[0].Description;
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

    // Takes what a bleaching bath did to the stone: its colour is lightened (or dulled when the bath attacked it), its value follows, and it
    // carries the treatment from then on. A bath without effect leaves the stone as it was.
    public void ApplyBleaching(BleachResult result)
    {
        if (result.Outcome == BleachOutcome.NoEffect) return;

        HasReport = false;
        TradeName = null;
        FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue * result.ValueFactor));
        TreatmentGrade = result.IsRuined ? "Blanchi (surface attaquée)" : "Blanchi";
        TreatmentDescription = result.Description;

        if (!HasColor) return;
        var saturation = result.IsRuined ? ColorSaturation * 0.8f : ColorSaturation * (1f - 0.7f * result.Quality);
        var brightness = result.IsRuined ? Mathf.Max(0.05f, ColorBrightness - 0.1f) : ColorBrightness + (1f - ColorBrightness) * 0.55f * result.Quality;
        SetColor(new GemColorValue(ColorHue, Mathf.Clamp01(saturation), Mathf.Clamp01(brightness)), IsFancyDiamond);
    }

    // Takes what a dye bath did to the stone: the dye colours it (unevenly in a compact material), its value follows, and it carries the
    // treatment from then on, after the bleaching it may have had. A bath without effect leaves the stone as it was.
    public void ApplyDyeing(DyeResult result)
    {
        if (result.Outcome == DyeOutcome.NoEffect) return;

        var wasBleached = !string.IsNullOrEmpty(TreatmentGrade) && TreatmentGrade.StartsWith("Blanchi");
        HasReport = false;
        TradeName = null;
        FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue * result.ValueFactor));
        TreatmentGrade = result.IsRuined ? "Teint (instable)" : wasBleached ? "Blanchi puis teint" : "Teint";
        TreatmentDescription = wasBleached ? $"{TreatmentDescription} {result.Description}" : result.Description;

        if (!HasColor) return;
        SetColor(new GemColorValue(Mathf.Repeat(result.Hue, 360f), Mathf.Clamp01(result.Saturation), Mathf.Clamp01(result.Brightness)), IsFancyDiamond);
    }

    // Takes the coating the vacuum chamber has put on the pavilion: the colour changes, the facets show a rainbow sheen if the coating is
    // iridescent, the value follows, and the stone carries the treatment from then on. A stone whose coating failed is still coated.
    public void ApplyCoating(CoatResult result)
    {
        HasReport = false;
        TradeName = null;
        FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue * result.ValueFactor));
        var previous = TreatmentGrade;
        var untreated = string.IsNullOrEmpty(previous) || previous == "Non traité";
        var label = result.Outcome == CoatOutcome.Fragile ? "Revêtu (fragile)" : result.Outcome == CoatOutcome.Ruined ? "Revêtu (écaillé)" : "Revêtu";
        TreatmentGrade = untreated ? label : $"{previous} + revêtu";
        TreatmentDescription = untreated ? result.Description : $"{TreatmentDescription} {result.Description}";
        CoatingName = result.CoatingName;
        CoatingIridescence = result.Iridescence;

        if (!HasColor) return;
        SetColor(new GemColorValue(Mathf.Repeat(result.Hue, 360f), Mathf.Clamp01(result.Saturation), Mathf.Clamp01(result.Brightness)), IsFancyDiamond);
    }

    // Takes what the autoclave did: the fissures are hidden, so the stone looks cleaner (the grade shown goes up), a coloured oil deepens the colour,
    // the value follows, and the stone carries the treatment from then on. A filling that failed leaves the stone damaged.
    public void ApplyFilling(FillResult result)
    {
        if (result.Outcome == FillOutcome.NoEffect) return;

        HasReport = false;
        TradeName = null;
        FillerName = result.Product;
        FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue * result.ValueFactor));
        var previous = TreatmentGrade;
        var untreated = string.IsNullOrEmpty(previous) || previous == "Non traité";
        TreatmentGrade = untreated ? result.Label : result.Label.StartsWith("B-jade") || result.Label.StartsWith("C-jade") ? result.Label : $"{previous} + {result.Label}";
        TreatmentDescription = untreated ? result.Description : $"{TreatmentDescription} {result.Description}";

        if (result.ClarityGain != 0)
        {
            var grades = GemGrading.ClarityGrades;
            var index = Mathf.Clamp(FillingCatalog.ClarityIndexOf(ClarityGrade) + result.ClarityGain, 0, grades.Length - 1);
            ClarityGrade = grades[index].Name;
            ClarityDescription = result.ClarityGain > 0 ? $"{grades[index].Description} (apparente : les fissures sont remplies)" : grades[index].Description;
        }

        if (HasColor && result.SaturationGain > 0f)
            SetColor(new GemColorValue(ColorHue, Mathf.Clamp01(ColorSaturation + result.SaturationGain), ColorBrightness), IsFancyDiamond);
    }

    // Takes what the furnace did: the colour moves to the one the process gives, the silk or the cracks change the clarity, a star may have
    // appeared, the value follows, and the stone carries the treatment from then on.
    public void ApplyHeating(HeatResult result)
    {
        if (result.Outcome == HeatOutcome.NoEffect) return;

        HasReport = false;
        TradeName = null;
        FinalValue = Mathf.Max(1, Mathf.RoundToInt(FinalValue * result.ValueFactor));
        var previous = TreatmentGrade;
        var untreated = string.IsNullOrEmpty(previous) || previous == "Non traité";
        TreatmentGrade = untreated ? "Chauffé" : $"{previous} + chauffé";
        TreatmentDescription = untreated ? result.Description : $"{TreatmentDescription} {result.Description}";
        if (result.Star) HeatStar = true;

        if (result.ClarityGain != 0)
        {
            var grades = GemGrading.ClarityGrades;
            var index = Mathf.Clamp(FillingCatalog.ClarityIndexOf(ClarityGrade) + result.ClarityGain, 0, grades.Length - 1);
            ClarityGrade = grades[index].Name;
            ClarityDescription = grades[index].Description;
        }

        if (HasColor && result.ChangesColor)
            SetColor(new GemColorValue(Mathf.Repeat(result.Hue, 360f), Mathf.Clamp01(result.Saturation), Mathf.Clamp01(result.Brightness)), IsFancyDiamond);
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
