using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The grading desk of the second floor of the Diamond Quarter tower. A diamond is put on the tray, then classified step by step, each
// step in its own tab that can be visited in any order: weight on the carat balance, dimensions and ratio, the plot of the inclusions
// under the loupe, fluorescence under long-wave ultraviolet, color against the master stones, estimation of the proportions, symmetry
// and polish. Once all of them are validated, the office issues the certificate (for a fee), which is attached to the stone.
// Built at runtime, so no scene regeneration is needed.
public class GradingUI : MonoBehaviour
{
    public static GradingUI Instance { get; private set; }

    private const float PanelWidth = 880f;
    private const float PanelHeight = 660f;
    private const float MainX = 244f;

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.13f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color LightText = new Color(0.86f, 0.88f, 0.92f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.30f, 0.38f, 1f);
    private static readonly Color SelectedColor = new Color(0.20f, 0.48f, 0.32f, 1f);
    private static readonly Color DoneColor = new Color(0.22f, 0.50f, 0.30f, 1f);
    private static readonly Color Dim = new Color(0.20f, 0.21f, 0.25f, 1f);
    private static readonly Color Warning = new Color(0.95f, 0.7f, 0.45f);
    private static readonly Color Good = new Color(0.55f, 0.9f, 0.6f);
    private static readonly Color Bad = new Color(0.95f, 0.5f, 0.45f);
    private static readonly Color ActionColor = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color DarkBox = new Color(0.05f, 0.06f, 0.08f, 1f);

    private enum Step { Weight, Dimensions, Plot, Fluorescence, Color, Proportions, Symmetry, Polish, Certificate }

    private const int StepCount = 9;
    private static readonly string[] TabNames =
    {
        "1  Poids", "2  Dimensions", "3  Plot", "4  Fluorescence", "5  Couleur", "6  Proportions", "7  Symétrie", "8  Poli", "9  Certificat",
    };

    // What the player has done on a stone, kept while the game runs so a tab can be left and found again.
    private class Progress
    {
        public bool[] Done = new bool[StepCount];
        public bool Weighed;
        public bool[] Gauge = new bool[3];                // maximal diameter, minimal diameter, depth measured
        public List<PlotMark> Marks = new List<PlotMark>();
        public bool PlotOk;
        public int ClarityPick = 5;
        public bool Lamp;
        public int FluorescencePick = -1;
        public int ColorPick = 8;
        public float[] Estimate = { 60f, 12f, 3f, 40f, 1f };   // table, crown, girdle, pavilion, culet
        public int ProportionPick = -1;
        public bool SymmetryMeasured;
        public bool PolishMeasured;
        public int[] SymmetryPick;
        public int[] PolishPick;
    }

    private class LevelTab
    {
        public GameObject Root;
        public Text[] Names;
        public Text[] Measures;
        public Image[][] Buttons;
        public Button MeasureButton;
        public Text Result;
    }

    private static readonly Dictionary<string, Progress> Sessions = new Dictionary<string, Progress>();

    private GameObject panel;
    private GameObject[] groups;
    private Image[] tabs;
    private Image[] tabMarks;
    private Image trayImage;
    private Text trayHint;
    private Text statusText;
    private Button validateButton;
    private Text validateLabel;
    private Text hintText;

    // Weight: the carat balance.
    private SevenSegmentDisplay balanceDisplay;
    private Image balanceStone;
    private Text weightInfo;
    private bool weighing;
    private float weighTimer;
    private float settleTick;

    // Dimensions: the gemstone gauge of the laboratory.
    private enum GaugePosition { Length, Width, Depth }
    private const float GaugePxPerMm = 6f;
    private const float GaugeSpeed = 360f;
    private const float GaugeMaxGap = 280f;
    private const float GaugeJawWidth = 14f;
    private const float GaugeJawHeight = 116f;
    private const float GaugeX = MainX;
    private const float GaugeY = -100f;
    private Image gaugeStone;
    private RectTransform gaugeStoneRect;
    private RectTransform movingJaw;
    private SevenSegmentDisplay gaugeDisplay;
    private GemView3D gaugeView;
    private Image[] gaugePositionButtons;
    private Text gaugeReadings;
    private Text gaugeHint;
    private GaugePosition gaugePosition = GaugePosition.Length;
    private float gaugeGap = GaugeMaxGap;
    private float gaugeTarget = GaugeMaxGap;
    private bool gaugeMeasuring;

    // Plot.
    private RectTransform plotRect;
    private Image plotTop;
    private RectTransform marksRoot;
    private Image[] hiddenMarks;
    private Image loupeRing;
    private Text loupeText;
    private Image[] paletteButtons;
    private Text plotCount;
    private Image[] clarityButtons;
    private Text clarityDescription;
    private PlotKind plotKind = PlotKind.Point;

    // Fluorescence.
    private Image fluoStone;
    private Image fluoGlow;
    private Image[] fluoReferenceGlow;
    private Text lampLabel;
    private Image[] fluoButtons;
    private Text fluoInfo;

    // Color.
    private Image comparedStone;
    private Image comparedMaster;
    private Text masterLabel;
    private Image[] masterButtons;
    private Text colorInfo;

    // Proportions.
    private RawImage profileImage;
    private Texture2D profileTexture;
    private Text[] estimateValues;
    private RectTransform[] estimateMarkers;   // crown line, girdle line, pavilion line, table left, table right, culet left, culet right
    private Text proportionInfo;
    private Image[] proportionButtons;

    // Symmetry and polish.
    private LevelTab symmetryTab;
    private LevelTab polishTab;

    // Certificate.
    private Text[] checklist;
    private Text certificateSummary;

    private GemInstance stone;
    private DiamondGrade grade;
    private Progress progress;
    private Step step;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<GradingUI>();
        }
        if (Instance == null) return false;
        Instance.Open();
        return true;
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        SetStone(null);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) && (DiamondCertificateUI.Instance == null || !DiamondCertificateUI.Instance.IsOpen))
        {
            Close();
            return;
        }
        if (stone == null) return;

        if (weighing) UpdateWeighing();
        if (step == Step.Dimensions) UpdateGauge();
        if (step == Step.Plot) UpdateLoupe();
    }

    private void RefreshIfOpen()
    {
        if (!IsOpen || stone == null) return;
        foreach (var g in PlayerInventory.Instance.Gems)
            if (ReferenceEquals(g, stone)) return;
        SetStone(null);
    }

    // ---- Stone ---------------------------------------------------------------------------------------------------

    private void OnDropped(GemInstance gem, Vector2 screenPosition)
    {
        SetStone(gem);
    }

    private void SetStone(GemInstance gem)
    {
        stone = null;
        grade = null;
        progress = null;
        weighing = false;
        gaugeMeasuring = false;
        gaugeGap = gaugeTarget = GaugeMaxGap;
        gaugePosition = GaugePosition.Length;

        if (gem != null && !DiamondGradeModel.CanGrade(gem))
        {
            statusText.text = "Le bureau de grading ne classe que les diamants taillés.";
            statusText.color = Bad;
        }
        else if (gem != null)
        {
            stone = gem;
            grade = DiamondGradeModel.Official(gem);
            var key = gem.SpecimenId ?? "";
            if (!Sessions.TryGetValue(key, out progress))
            {
                progress = new Progress
                {
                    SymmetryPick = NewPicks(DiamondGradeModel.SymmetryItems.Length),
                    PolishPick = NewPicks(DiamondGradeModel.PolishItems.Length),
                };
                Sessions[key] = progress;
            }
            if (gem.Certificate != null) FillFromOfficial();
            statusText.text = gem.Certificate != null ? "Ce diamant est déjà classé : tous les résultats sont validés." : "";
            statusText.color = LightText;
        }
        else
        {
            statusText.text = "Déposez un diamant taillé sur le plateau.";
            statusText.color = LightText;
        }

        step = Step.Weight;
        RebuildStoneViews();
        RefreshAll();
    }

    private static int[] NewPicks(int count)
    {
        var picks = new int[count];
        for (var i = 0; i < count; i++) picks[i] = -1;
        return picks;
    }

    // A stone that already has its certificate: everything is validated, with the official values.
    private void FillFromOfficial()
    {
        var p = progress;
        for (var i = 0; i < StepCount - 1; i++) p.Done[i] = true;
        p.Weighed = true;
        p.Gauge = new[] { true, true, true };
        p.Marks = new List<PlotMark>(grade.Plot);
        p.PlotOk = true;
        p.ClarityPick = grade.ClarityIndex;
        p.Lamp = true;
        p.FluorescencePick = grade.FluorescenceIndex;
        p.ColorPick = grade.ColorIndex;
        p.Estimate = new[] { grade.TablePercent, grade.CrownPercent, grade.GirdlePercent, grade.PavilionPercent, grade.CuletPercent };
        p.ProportionPick = grade.ProportionRank;
        p.SymmetryMeasured = true;
        p.PolishMeasured = true;
        p.SymmetryPick = (int[])grade.SymmetryLevels.Clone();
        p.PolishPick = (int[])grade.PolishLevels.Clone();
    }

    private bool AllDone()
    {
        for (var i = 0; i < StepCount - 1; i++)
            if (!progress.Done[i]) return false;
        return true;
    }

    private void GoTo(Step target)
    {
        step = target;
        hintText.text = "";
        RefreshAll();
    }

    private void Say(string message, bool good)
    {
        hintText.text = message;
        hintText.color = good ? Good : Warning;
        Sfx.Play(good ? Sfx.Id.Success : Sfx.Id.Error);
    }

    // ---- Refresh -------------------------------------------------------------------------------------------------

    private void RebuildStoneViews()
    {
        if (stone == null) return;
        BuildHiddenMarks();
        BuildProfileTexture();
    }

    private void RefreshAll()
    {
        trayImage.sprite = stone != null ? stone.DisplaySprite : null;
        trayImage.enabled = stone != null;
        trayHint.gameObject.SetActive(stone == null);

        var hasStone = stone != null;
        for (var i = 0; i < StepCount; i++)
        {
            tabs[i].color = i == (int)step && hasStone ? SelectedColor : (hasStone ? ButtonColor : Dim);
            tabs[i].GetComponent<Button>().interactable = hasStone;
            var done = hasStone && (i < StepCount - 1 ? progress.Done[i] : stone.Certificate != null);
            tabMarks[i].enabled = done;
            groups[i].SetActive(hasStone && i == (int)step);
        }
        validateButton.gameObject.SetActive(hasStone);
        hintText.gameObject.SetActive(hasStone);
        if (!hasStone) return;

        switch (step)
        {
            case Step.Weight: RefreshWeight(); break;
            case Step.Dimensions: RefreshDimensions(); break;
            case Step.Plot: RefreshPlot(); break;
            case Step.Fluorescence: RefreshFluorescence(); break;
            case Step.Color: RefreshColor(); break;
            case Step.Proportions: RefreshProportions(); break;
            case Step.Symmetry: RefreshLevels(symmetryTab, DiamondGradeModel.SymmetryItems, progress.SymmetryPick, grade.SymmetryMeasures, progress.SymmetryMeasured, Step.Symmetry); break;
            case Step.Polish: RefreshLevels(polishTab, DiamondGradeModel.PolishItems, progress.PolishPick, grade.PolishMeasures, progress.PolishMeasured, Step.Polish); break;
            default: RefreshCertificate(); break;
        }
        RefreshValidateButton();
    }

    private void RefreshValidateButton()
    {
        string label;
        var interactable = true;
        var done = step != Step.Certificate && progress.Done[(int)step];
        switch (step)
        {
            case Step.Weight: label = "Peser la pierre"; break;
            case Step.Dimensions: label = "Mesurer"; break;
            case Step.Plot: label = progress.PlotOk ? "Valider la pureté" : "Valider le plan"; break;
            case Step.Fluorescence: label = "Valider la fluorescence"; break;
            case Step.Color: label = "Valider la couleur"; break;
            case Step.Proportions: label = "Valider les proportions"; break;
            case Step.Symmetry: label = "Valider la symétrie"; break;
            case Step.Polish: label = "Valider le poli"; break;
            default:
                if (stone.Certificate != null) label = $"Voir le certificat  {stone.Certificate.number}";
                else
                {
                    label = $"Établir le certificat — {DiamondGradeModel.Fee(stone)} €";
                    interactable = AllDone();
                }
                break;
        }
        if (done)
        {
            label = "Étape validée";
            interactable = false;
        }
        validateLabel.text = label;
        validateButton.interactable = interactable;
        validateButton.GetComponent<Image>().color = done ? DoneColor : (interactable ? ActionColor : Dim);
    }

    private void Validate()
    {
        if (stone == null) return;
        switch (step)
        {
            case Step.Weight: StartWeighing(); break;
            case Step.Dimensions: MeasureGauge(); break;
            case Step.Plot: ValidatePlot(); break;
            case Step.Fluorescence: ValidateFluorescence(); break;
            case Step.Color: ValidateColor(); break;
            case Step.Proportions: ValidateProportions(); break;
            case Step.Symmetry: ValidateLevels(Step.Symmetry, grade.SymmetryLevels, progress.SymmetryPick, symmetryTab, "symétrie"); break;
            case Step.Polish: ValidateLevels(Step.Polish, grade.PolishLevels, progress.PolishPick, polishTab, "poli"); break;
            default: IssueOrShowCertificate(); break;
        }
    }

    private void Complete(Step finished, string message)
    {
        progress.Done[(int)finished] = true;
        RefreshAll();
        Say(message, true);
    }

    // ---- 1. Weight -----------------------------------------------------------------------------------------------

    private void RefreshWeight()
    {
        balanceStone.sprite = stone.DisplaySprite;
        balanceStone.enabled = true;
        if (!weighing) balanceDisplay.ShowNumber(progress.Weighed ? Mathf.RoundToInt(stone.CaratWeight * 100f) : 0, 2);
        weightInfo.text = progress.Weighed
            ? $"Poids enregistré : {grade.Carat:0.00} ct  ({grade.Carat * 0.2f:0.000} g)."
            : "La pierre est sur le plateau. Lancez la pesée : l'affichage se stabilise puis le poids est reporté tout seul sur la fiche.";
    }

    private void StartWeighing()
    {
        if (stone == null || weighing || progress.Weighed) return;
        weighing = true;
        weighTimer = 0f;
        settleTick = 0f;
    }

    // The reading flickers around the real value for a moment, like a real balance settling.
    private void UpdateWeighing()
    {
        weighTimer += Time.unscaledDeltaTime;
        var target = stone.CaratWeight;
        const float duration = 0.9f;
        if (weighTimer >= duration)
        {
            weighing = false;
            progress.Weighed = true;
            balanceDisplay.ShowNumber(Mathf.RoundToInt(target * 100f), 2);
            stone.RecordObservation(ExaminationTool.MeasurementType.Weight.ToString());
            progress.Done[(int)Step.Weight] = true;
            RefreshAll();
            if (step == Step.Weight) Say($"Poids enregistré : {grade.Carat:0.00} ct.", true);
            return;
        }
        settleTick -= Time.unscaledDeltaTime;
        if (settleTick > 0f) return;
        settleTick = 0.06f;
        var remaining = 1f - weighTimer / duration;
        var shown = Mathf.Max(0f, target + Random.Range(-1f, 1f) * Mathf.Max(0.5f, target * 0.3f) * remaining);
        balanceDisplay.ShowNumber(Mathf.RoundToInt(shown * 100f), 2);
    }

    // ---- 2. Dimensions ---------------------------------------------------------------------------------------------

    private static readonly string[] GaugeNames = { "Diamètre maximal", "Diamètre minimal", "Profondeur" };

    private float GaugeValue(GaugePosition position)
    {
        return position == GaugePosition.Length ? grade.DiameterMax : position == GaugePosition.Width ? grade.DiameterMin : grade.DepthMm;
    }

    // The stone is drawn in 3D pixel art, turned so that the chosen dimension lies across the jaws.
    private void LayoutGaugeStone()
    {
        var l = grade.DiameterMax * GaugePxPerMm;
        var w = grade.DiameterMin * GaugePxPerMm;
        var d = grade.DepthMm * GaugePxPerMm;

        Quaternion orientation;
        float across, vertical;
        switch (gaugePosition)
        {
            case GaugePosition.Width:
                orientation = Quaternion.Euler(0f, 0f, 90f);
                across = w;
                vertical = l;
                break;
            case GaugePosition.Depth:
                orientation = Quaternion.Euler(0f, 90f, 0f);
                across = d;
                vertical = w;
                break;
            default:
                orientation = Quaternion.identity;
                across = l;
                vertical = w;
                break;
        }

        GemView3D.Colors(stone, out var body, out var facet);
        var half = new Vector3(grade.DiameterMax, grade.DiameterMin, grade.DepthMm) * 0.5f;
        gaugeView.Draw(GemView3D.MeshFor(stone), half, orientation, body, facet, stone.Definition.Diaphaneity);

        var side = Mathf.Max(grade.DiameterMax, Mathf.Max(grade.DiameterMin, grade.DepthMm)) * GaugePxPerMm / GemView3D.FitFraction;
        gaugeStoneRect.sizeDelta = new Vector2(side, side);
        gaugeStoneRect.anchoredPosition = new Vector2(GaugeX + GaugeJawWidth + across / 2f, GaugeY - GaugeJawHeight + 8f + vertical / 2f);
    }

    private void RefreshDimensions()
    {
        gaugeStone.enabled = true;
        LayoutGaugeStone();
        for (var i = 0; i < gaugePositionButtons.Length; i++)
            gaugePositionButtons[i].color = (int)gaugePosition == i ? SelectedColor : ButtonColor;
        ApplyGaugeJaw();

        var text = "";
        for (var i = 0; i < 3; i++)
            if (progress.Gauge[i]) text += $"{GaugeNames[i]} : {GaugeValue((GaugePosition)i):0.00} mm\n";
        if (progress.Gauge[0] && progress.Gauge[1]) text += $"Rapport : {grade.Ratio:0.00}\n";
        if (progress.Gauge[0] && progress.Gauge[1] && progress.Gauge[2])
        {
            var depthPercent = grade.DepthMm / ((grade.DiameterMax + grade.DiameterMin) * 0.5f) * 100f;
            text += $"Profondeur totale : {depthPercent:0.0} %\nPoids calculé : {grade.CalculatedCarat:0.00} ct";
        }
        gaugeReadings.text = text.Length == 0 ? "Aucune mesure prise." : text.TrimEnd('\n');
        gaugeHint.text = progress.Done[(int)Step.Dimensions] ? "" : "Choisissez la position de la pierre puis fermez les mâchoires.";
    }

    private void SelectGaugePosition(GaugePosition position)
    {
        if (stone == null) return;
        gaugePosition = position;
        gaugeMeasuring = false;
        gaugeTarget = GaugeMaxGap;      // reopen so the stone can be turned
        RefreshDimensions();
    }

    private void MeasureGauge()
    {
        if (stone == null) return;
        gaugeMeasuring = true;
        gaugeTarget = GaugeValue(gaugePosition) * GaugePxPerMm;
    }

    private void UpdateGauge()
    {
        if (Mathf.Approximately(gaugeGap, gaugeTarget)) return;
        gaugeGap = Mathf.MoveTowards(gaugeGap, gaugeTarget, GaugeSpeed * Time.unscaledDeltaTime);
        ApplyGaugeJaw();
        if (gaugeMeasuring && Mathf.Approximately(gaugeGap, gaugeTarget)) FinishGauge();
    }

    private void ApplyGaugeJaw()
    {
        RuntimeUIKit.SetTopLeft(movingJaw, new Vector2(GaugeX + GaugeJawWidth + gaugeGap, GaugeY), new Vector2(GaugeJawWidth, GaugeJawHeight));
        // The dial shows the opening between the jaws, in hundredths of a millimeter.
        gaugeDisplay.ShowNumber(Mathf.RoundToInt(gaugeGap / GaugePxPerMm * 100f), 1);
    }

    // Each reading is logged; once the three are taken the ratio and the depth are worked out and the step is validated.
    private void FinishGauge()
    {
        gaugeMeasuring = false;
        progress.Gauge[(int)gaugePosition] = true;
        stone.RecordObservation($"Dim{gaugePosition}");
        if (progress.Gauge[0] && progress.Gauge[1] && progress.Gauge[2])
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.Dimensions.ToString());
            progress.Done[(int)Step.Dimensions] = true;
            RefreshAll();
            Say($"Dimensions enregistrées : {grade.DiameterMax:0.00} – {grade.DiameterMin:0.00} x {grade.DepthMm:0.00} mm, rapport {grade.Ratio:0.00}.", true);
            return;
        }
        RefreshAll();
        Say($"{GaugeNames[(int)gaugePosition]} : {GaugeValue(gaugePosition):0.00} mm. Mesurez les autres dimensions.", true);
    }

    // ---- 3. Plot -------------------------------------------------------------------------------------------------

    private const float PlotX = MainX;
    private const float PlotY = -100f;
    private const float PlotSize = 300f;
    private const float PlotRadius = 118f;
    private const float LoupeRadius = 46f;
    private const float MatchDistance = 0.17f;

    private static Vector2 PlotToTopLeft(float x, float y)
    {
        return new Vector2(PlotX + PlotSize * 0.5f + x * PlotRadius, PlotY - PlotSize * 0.5f + y * PlotRadius);
    }

    private void BuildHiddenMarks()
    {
        if (hiddenMarks != null)
            foreach (var old in hiddenMarks)
                if (old != null) Destroy(old.gameObject);

        hiddenMarks = new Image[grade.Plot.Count];
        for (var i = 0; i < hiddenMarks.Length; i++)
        {
            var mark = grade.Plot[i];
            var position = PlotToTopLeft(mark.X, mark.Y);
            var image = MakeSymbol("Hidden" + i, groups[(int)Step.Plot].transform, mark.Kind, new Color(0.18f, 0.18f, 0.24f, 0.95f), position - new Vector2(13f, -13f), 26f);
            image.gameObject.SetActive(false);
            hiddenMarks[i] = image;
        }
        plotTop.sprite = GemDiagrams.TopView(stone);
        loupeRing.transform.SetAsLastSibling();
    }

    private static Image MakeSymbol(string name, Transform parent, PlotKind kind, Color color, Vector2 topLeft, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), topLeft, new Vector2(size, size));
        var image = go.GetComponent<Image>();
        image.sprite = PlotSymbols.Get(kind);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void RefreshPlot()
    {
        RebuildPlayerMarks();
        for (var i = 0; i < paletteButtons.Length; i++)
            paletteButtons[i].color = (int)plotKind == i ? SelectedColor : new Color(0.22f, 0.24f, 0.30f, 1f);
        plotCount.text = $"Symboles posés : {progress.Marks.Count}";
        for (var i = 0; i < clarityButtons.Length; i++)
        {
            var picked = i == progress.ClarityPick;
            var final = progress.Done[(int)Step.Plot] && i == grade.ClarityIndex;
            clarityButtons[i].color = final ? DoneColor : (picked ? SelectedColor : ButtonColor);
        }
        var clarity = DiamondGrading.Clarity(progress.ClarityPick);
        clarityDescription.text = progress.PlotOk
            ? $"{clarity} : {DiamondGradeModel.ClarityDescription(clarity)}"
            : "Passez la loupe sur la pierre, repérez chaque inclusion (rouge) et chaque défaut de surface (vert), puis posez le bon symbole à sa place.";
        loupeText.text = progress.PlotOk ? "" : loupeText.text;
    }

    private void RebuildPlayerMarks()
    {
        for (var i = marksRoot.childCount - 1; i >= 0; i--)
            Destroy(marksRoot.GetChild(i).gameObject);
        foreach (var mark in progress.Marks)
        {
            var position = PlotToTopLeft(mark.X, mark.Y) - new Vector2(PlotX, PlotY) - new Vector2(11f, -11f);
            MakeSymbol("Mark", marksRoot, mark.Kind, PlotSymbols.ColorOf(mark.Kind), position, 22f);
        }
    }

    private bool MouseOnPlot(out Vector2 normalized, out Vector2 local)
    {
        normalized = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(plotRect, Input.mousePosition, null, out local);
        if (local.x < 0f || local.x > PlotSize || local.y > 0f || local.y < -PlotSize) return false;
        normalized = new Vector2((local.x - PlotSize * 0.5f) / PlotRadius, (local.y + PlotSize * 0.5f) / PlotRadius);
        return true;
    }

    private void UpdateLoupe()
    {
        Vector2 normalized = Vector2.zero;
        Vector2 local = Vector2.zero;
        var on = !progress.PlotOk && MouseOnPlot(out normalized, out local);
        loupeRing.gameObject.SetActive(on);
        if (!on)
        {
            foreach (var mark in hiddenMarks) mark.gameObject.SetActive(false);
            return;
        }

        loupeRing.rectTransform.anchoredPosition = new Vector2(PlotX + local.x - LoupeRadius, PlotY + local.y + LoupeRadius);
        string seen = null;
        var best = 1e9f;
        for (var i = 0; i < hiddenMarks.Length; i++)
        {
            var mark = grade.Plot[i];
            var distance = Vector2.Distance(new Vector2(mark.X, mark.Y), normalized) * PlotRadius;
            hiddenMarks[i].gameObject.SetActive(distance <= LoupeRadius - 6f);
            if (distance < 22f && distance < best)
            {
                best = distance;
                seen = DiamondGradeModel.KindLoupe(mark.Kind);
            }
        }
        loupeText.text = seen != null ? $"Dans la loupe : {seen}." : "Dans la loupe : rien de visible ici.";
    }

    private void OnPlotClicked()
    {
        if (stone == null || progress.PlotOk) return;
        if (!MouseOnPlot(out var normalized, out _)) return;
        if (normalized.magnitude > 1.02f) return;

        // A click on a symbol already placed removes it.
        for (var i = 0; i < progress.Marks.Count; i++)
        {
            if (Vector2.Distance(new Vector2(progress.Marks[i].X, progress.Marks[i].Y), normalized) * PlotRadius < 11f)
            {
                progress.Marks.RemoveAt(i);
                RefreshPlot();
                return;
            }
        }
        progress.Marks.Add(new PlotMark { Kind = plotKind, X = normalized.x, Y = normalized.y });
        hintText.text = "";
        RefreshPlot();
    }

    private void ValidatePlot()
    {
        if (!progress.PlotOk)
        {
            var truth = grade.Plot;
            var matched = new bool[progress.Marks.Count];
            var missing = 0;
            var wrong = 0;
            foreach (var real in truth)
            {
                var index = -1;
                var best = MatchDistance;
                for (var i = 0; i < progress.Marks.Count; i++)
                {
                    if (matched[i]) continue;
                    var distance = Vector2.Distance(new Vector2(real.X, real.Y), new Vector2(progress.Marks[i].X, progress.Marks[i].Y));
                    if (distance < best)
                    {
                        best = distance;
                        index = i;
                    }
                }
                if (index < 0)
                {
                    missing++;
                    continue;
                }
                matched[index] = true;
                if (progress.Marks[index].Kind != real.Kind) wrong++;
            }
            var extra = 0;
            foreach (var m in matched) if (!m) extra++;

            if (missing == 0 && wrong == 0 && extra == 0)
            {
                progress.PlotOk = true;
                RefreshAll();
                Say(truth.Count == 0 ? "Plan validé : aucune inclusion, aucun défaut. Déterminez maintenant la pureté." : "Plan validé. Déterminez maintenant la pureté d'après ce que vous avez relevé.", true);
                return;
            }
            var parts = new List<string>();
            if (missing > 0) parts.Add($"{missing} marque(s) manquante(s)");
            if (wrong > 0) parts.Add($"{wrong} symbole(s) qui ne correspond(ent) pas à ce que montre la loupe");
            if (extra > 0) parts.Add($"{extra} symbole(s) posé(s) là où il n'y a rien");
            Say("Le plan n'est pas juste : " + string.Join(", ", parts) + ".", false);
            return;
        }

        var difference = progress.ClarityPick - grade.ClarityIndex;
        if (difference == 0)
        {
            Complete(Step.Plot, $"Pureté {grade.ClarityGrade} confirmée.");
            return;
        }
        Say(difference < 0
            ? "Trop indulgent : les inclusions relevées sont plus nombreuses ou plus marquées. Descendez dans l'échelle."
            : "Trop sévère : les inclusions relevées sont plus discrètes. Remontez dans l'échelle.", false);
    }

    private void SelectClarity(int index)
    {
        if (stone == null || progress.Done[(int)Step.Plot] || !progress.PlotOk) return;
        progress.ClarityPick = index;
        hintText.text = "";
        RefreshPlot();
    }

    // ---- 4. Fluorescence ---------------------------------------------------------------------------------------------

    private static readonly float[] GlowLevels = { 0f, 0.22f, 0.55f, 0.95f };

    private void RefreshFluorescence()
    {
        fluoStone.sprite = stone.DisplaySprite;
        fluoStone.color = progress.Lamp ? new Color(0.62f, 0.66f, 0.85f, 1f) : Color.white;
        var glow = progress.Lamp ? GlowLevels[grade.FluorescenceIndex] : 0f;
        fluoGlow.color = new Color(0.35f, 0.55f, 1f, glow);
        for (var i = 0; i < fluoReferenceGlow.Length; i++)
            fluoReferenceGlow[i].color = new Color(0.35f, 0.55f, 1f, progress.Lamp ? GlowLevels[i] : 0f);
        lampLabel.text = progress.Lamp ? "Éteindre la lampe UV" : "Allumer la lampe UV longs (365 nm)";
        for (var i = 0; i < fluoButtons.Length; i++)
        {
            var final = progress.Done[(int)Step.Fluorescence] && i == grade.FluorescenceIndex;
            fluoButtons[i].color = final ? DoneColor : (i == progress.FluorescencePick ? SelectedColor : ButtonColor);
        }
        fluoInfo.text = progress.Lamp
            ? "Comparez la lueur bleue de la pierre à celle des pierres de référence."
            : "Allumez la lampe : la fluorescence ne se voit que dans le noir, sous l'ultraviolet.";
    }

    private void ToggleLamp()
    {
        if (stone == null || progress.Done[(int)Step.Fluorescence]) return;
        progress.Lamp = !progress.Lamp;
        RefreshFluorescence();
    }

    private void SelectFluorescence(int index)
    {
        if (stone == null || progress.Done[(int)Step.Fluorescence]) return;
        progress.FluorescencePick = index;
        hintText.text = "";
        RefreshFluorescence();
    }

    private void ValidateFluorescence()
    {
        if (!progress.Lamp)
        {
            Say("Allumez la lampe UV pour observer la pierre.", false);
            return;
        }
        if (progress.FluorescencePick < 0)
        {
            Say("Choisissez l'intensité de fluorescence observée.", false);
            return;
        }
        var difference = progress.FluorescencePick - grade.FluorescenceIndex;
        if (difference == 0)
        {
            Complete(Step.Fluorescence, $"Fluorescence : {DiamondGradeModel.FluorescenceEnglish[grade.FluorescenceIndex]} ({grade.Fluorescence.ToLowerInvariant()}).");
            return;
        }
        Say(difference > 0 ? "La pierre brille moins que cela : choisissez une intensité plus faible." : "La pierre brille davantage : choisissez une intensité plus forte.", false);
    }

    // ---- 5. Color ------------------------------------------------------------------------------------------------

    private void RefreshColor()
    {
        comparedStone.sprite = stone.DisplaySprite;
        comparedStone.enabled = true;
        var done = progress.Done[(int)Step.Color];

        if (grade.Fancy)
        {
            colorInfo.text = $"Diamant de couleur fantaisie : {grade.ColorText}.\nIl sort de l'échelle D à Z : le bureau l'établit directement. Validez pour continuer.";
            comparedMaster.enabled = false;
            masterLabel.text = "";
            foreach (var button in masterButtons) button.gameObject.SetActive(false);
            return;
        }

        colorInfo.text = "Lumière du jour (6500 K), fond blanc. Choisissez la master stone qui a exactement la même couleur que la pierre.";
        foreach (var button in masterButtons) button.gameObject.SetActive(true);
        comparedMaster.enabled = true;
        comparedMaster.sprite = DiamondGradeModel.MasterSprite(progress.ColorPick);
        masterLabel.text = $"Master {DiamondGrading.Color(progress.ColorPick)}";
        for (var i = 0; i < masterButtons.Length; i++)
            masterButtons[i].color = i == progress.ColorPick ? SelectedColor : (done && i == grade.ColorIndex ? DoneColor : new Color(0.93f, 0.94f, 0.97f, 1f));
    }

    private void SelectMaster(int index)
    {
        if (stone == null || progress.Done[(int)Step.Color] || grade.Fancy) return;
        progress.ColorPick = index;
        hintText.text = "";
        RefreshColor();
    }

    private void ValidateColor()
    {
        if (grade.Fancy)
        {
            Complete(Step.Color, $"Couleur fantaisie enregistrée : {grade.ColorText}.");
            return;
        }
        var difference = progress.ColorPick - grade.ColorIndex;
        if (difference == 0)
        {
            Complete(Step.Color, $"Couleur {grade.ColorGrade} confirmée : {grade.ColorText.ToLowerInvariant()}.");
            return;
        }
        var strength = Mathf.Abs(difference) >= 4 ? "nettement" : Mathf.Abs(difference) >= 2 ? "un peu" : "très légèrement";
        Say(difference > 0
            ? $"La pierre est {strength} plus incolore que la master {DiamondGrading.Color(progress.ColorPick)} : remontez dans l'échelle, vers D."
            : $"La pierre est {strength} plus jaune que la master {DiamondGrading.Color(progress.ColorPick)} : descendez dans l'échelle, vers Z.", false);
    }

    // ---- 6. Proportions -------------------------------------------------------------------------------------------

    private const float ProfileScale = 3f;
    private const float ProfileCenter = 190f;
    private const float ProfileTop = 48f;
    private const int ProfileW = 360;
    private const int ProfileH = 300;
    private static readonly float[] EstimateMin = { 40f, 5f, 0f, 30f, 0f };
    private static readonly float[] EstimateMax = { 75f, 25f, 12f, 55f, 10f };
    private static readonly float[] EstimateTolerance = { 1.0f, 0.7f, 0.7f, 0.8f, 0.8f };
    private static readonly string[] EstimateNames = { "Largeur de table", "Hauteur de couronne", "Épaisseur de ceinture", "Profondeur de pavillon", "Taille de colette" };
    private static readonly Color[] EstimateColors =
    {
        new Color(1f, 0.9f, 0.3f), new Color(0.35f, 0.9f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.4f, 0.85f), new Color(0.8f, 0.8f, 0.85f),
    };

    private void BuildProfileTexture()
    {
        profileTexture = DiamondProfileDrawing.Draw(grade, ProfileW, ProfileH, ProfileScale, ProfileCenter, ProfileTop, true, profileTexture);
        profileImage.texture = profileTexture;
    }

    private static float RowY(float percent) => -96f - (ProfileTop + percent * ProfileScale);

    private void RefreshProportions()
    {
        var e = progress.Estimate;
        for (var i = 0; i < estimateValues.Length; i++) estimateValues[i].text = $"{e[i]:0.0} %";

        // The marks of the estimates, placed over the drawing.
        PlaceLine(estimateMarkers[0], RowY(e[1]));
        PlaceLine(estimateMarkers[1], RowY(e[1] + e[2]));
        PlaceLine(estimateMarkers[2], RowY(e[1] + e[2] + e[3]));
        PlaceTick(estimateMarkers[3], MainX + ProfileCenter - e[0] * 0.5f * ProfileScale, RowY(0f), 24f);
        PlaceTick(estimateMarkers[4], MainX + ProfileCenter + e[0] * 0.5f * ProfileScale, RowY(0f), 24f);
        var bottom = RowY(e[1] + e[2] + e[3]);
        PlaceTick(estimateMarkers[5], MainX + ProfileCenter - e[4] * 0.5f * ProfileScale, bottom, 16f);
        PlaceTick(estimateMarkers[6], MainX + ProfileCenter + e[4] * 0.5f * ProfileScale, bottom, 16f);

        var beta = DiamondGradeModel.CrownAngle(e[1], e[0]);
        var alpha = DiamondGradeModel.PavilionAngle(e[3], e[4]);
        var total = e[1] + e[2] + e[3];
        var girdle = DiamondGradeModel.GirdleClassOf(e[2]);
        var culet = DiamondGradeModel.CuletClassOf(e[4]);
        string R(int rank) => DiamondGradeModel.CutShort[Mathf.Clamp(rank, 0, 3)];
        proportionInfo.text =
            $"Table {e[0]:0.0} %   [{R(DiamondGradeModel.BandRank(e[0], DiamondGradeModel.TableBands))}]\n" +
            $"Angle de couronne β : {beta:0.0}°   [{R(DiamondGradeModel.BandRank(beta, DiamondGradeModel.CrownAngleBands))}]\n" +
            $"Angle de pavillon α : {alpha:0.0}°   [{R(DiamondGradeModel.BandRank(alpha, DiamondGradeModel.PavilionAngleBands))}]\n" +
            $"Profondeur totale : {total:0.0} %   [{R(DiamondGradeModel.BandRank(total, DiamondGradeModel.DepthBands))}]\n" +
            $"Ceinture {DiamondGradeModel.GirdleNames[girdle].ToLowerInvariant()}   [{R(DiamondGradeModel.GirdleRank(girdle))}]\n" +
            $"Colette {DiamondGradeModel.CuletNames[culet].ToLowerInvariant()}   [{R(DiamondGradeModel.CuletRank(culet))}]";

        for (var i = 0; i < proportionButtons.Length; i++)
        {
            var final = progress.Done[(int)Step.Proportions] && i == grade.ProportionRank;
            proportionButtons[i].color = final ? DoneColor : (i == progress.ProportionPick ? SelectedColor : ButtonColor);
        }
    }

    private static void PlaceLine(RectTransform marker, float y)
    {
        marker.anchoredPosition = new Vector2(MainX + 14f, y + 1f);
        marker.sizeDelta = new Vector2(ProfileW - 28f, 2f);
    }

    private static void PlaceTick(RectTransform marker, float x, float yCenter, float height)
    {
        marker.anchoredPosition = new Vector2(x - 1f, yCenter + height * 0.5f);
        marker.sizeDelta = new Vector2(2f, height);
    }

    private void Adjust(int index, float delta)
    {
        if (stone == null || progress.Done[(int)Step.Proportions]) return;
        progress.Estimate[index] = Mathf.Clamp(Mathf.Round((progress.Estimate[index] + delta) * 2f) / 2f, EstimateMin[index], EstimateMax[index]);
        hintText.text = "";
        RefreshProportions();
    }

    private void PickProportion(int rank)
    {
        if (stone == null || progress.Done[(int)Step.Proportions]) return;
        progress.ProportionPick = rank;
        hintText.text = "";
        RefreshProportions();
    }

    private void ValidateProportions()
    {
        var truth = new[] { grade.TablePercent, grade.CrownPercent, grade.GirdlePercent, grade.PavilionPercent, grade.CuletPercent };
        var off = new List<string>();
        for (var i = 0; i < truth.Length; i++)
        {
            var difference = progress.Estimate[i] - truth[i];
            if (Mathf.Abs(difference) > EstimateTolerance[i])
                off.Add($"{EstimateNames[i].ToLowerInvariant()} ({(difference > 0 ? "trop grande" : "trop petite")})");
        }
        if (off.Count > 0)
        {
            Say("Les repères ne coïncident pas encore avec le dessin : " + string.Join(", ", off) + ".", false);
            return;
        }
        if (progress.ProportionPick < 0)
        {
            Say("Choisissez la qualité des proportions : c'est le plus bas des critères du tableau.", false);
            return;
        }
        if (progress.ProportionPick != grade.ProportionRank)
        {
            Say(progress.ProportionPick < grade.ProportionRank
                ? "Trop indulgent : un des critères tombe dans une classe plus basse du tableau."
                : "Trop sévère : tous les critères sont meilleurs que cela d'après le tableau.", false);
            return;
        }
        Complete(Step.Proportions, $"Proportions : {DiamondGradeModel.CutScale[grade.ProportionRank]} (β {grade.CrownAngle:0.0}°, α {grade.PavilionAngle:0.0}°).");
    }

    // ---- 7 and 8. Symmetry and polish ----------------------------------------------------------------------------

    private void RefreshLevels(LevelTab tab, string[] items, int[] picks, float[] measures, bool measured, Step which)
    {
        var done = progress.Done[(int)which];
        var truth = which == Step.Symmetry ? grade.SymmetryLevels : grade.PolishLevels;
        tab.MeasureButton.interactable = !measured;
        tab.MeasureButton.GetComponentInChildren<Text>().text = measured ? "Mesures relevées" : "Mesurer à la loupe";
        for (var i = 0; i < items.Length; i++)
        {
            tab.Measures[i].text = measured ? $"{measures[i]:0.0} %".Replace('.', ',') : "—";
            for (var b = 0; b < 4; b++)
            {
                var final = done && b == truth[i];
                tab.Buttons[i][b].color = final ? DoneColor : (picks[i] == b ? SelectedColor : ButtonColor);
            }
        }
        tab.Result.text = done
            ? (which == Step.Symmetry ? $"Symétrie : {grade.Symmetry}" : $"Poli : {grade.Polish}")
            : "Classez chaque ligne d'après l'écart mesuré.";
    }

    private void MeasureLevels(Step which)
    {
        if (stone == null) return;
        if (which == Step.Symmetry) progress.SymmetryMeasured = true;
        else progress.PolishMeasured = true;
        RefreshAll();
    }

    private void PickLevel(Step which, int item, int level)
    {
        if (stone == null || progress.Done[(int)which]) return;
        var picks = which == Step.Symmetry ? progress.SymmetryPick : progress.PolishPick;
        picks[item] = level;
        hintText.text = "";
        var tab = which == Step.Symmetry ? symmetryTab : polishTab;
        tab.Names[item].color = LightText;
        RefreshAll();
    }

    private void ValidateLevels(Step which, int[] truth, int[] picks, LevelTab tab, string label)
    {
        var measured = which == Step.Symmetry ? progress.SymmetryMeasured : progress.PolishMeasured;
        if (!measured)
        {
            Say("Mesurez d'abord les écarts à la loupe.", false);
            return;
        }
        var missing = 0;
        var wrong = 0;
        for (var i = 0; i < truth.Length; i++)
        {
            if (picks[i] < 0)
            {
                missing++;
                tab.Names[i].color = Warning;
            }
            else if (picks[i] != truth[i])
            {
                wrong++;
                tab.Names[i].color = Bad;
            }
            else
            {
                tab.Names[i].color = LightText;
            }
        }
        if (missing > 0)
        {
            Say($"Il reste {missing} ligne(s) à classer.", false);
            return;
        }
        if (wrong > 0)
        {
            Say($"{wrong} ligne(s) en rouge : comparez l'écart mesuré au tableau des seuils.", false);
            return;
        }
        Complete(which, which == Step.Symmetry ? $"Symétrie : {grade.Symmetry}." : $"Poli : {grade.Polish}.");
    }

    // ---- 9. Certificate ---------------------------------------------------------------------------------------------

    private void RefreshCertificate()
    {
        var labels = new[]
        {
            "Poids", "Dimensions et rapport", "Plot et pureté", "Fluorescence", "Couleur", "Proportions", "Symétrie", "Poli",
        };
        var values = new[]
        {
            $"{grade.Carat:0.00} ct",
            $"{grade.DiameterMax:0.00} – {grade.DiameterMin:0.00} x {grade.DepthMm:0.00} mm  (rapport {grade.Ratio:0.00})",
            grade.ClarityGrade,
            DiamondGradeModel.FluorescenceEnglish[grade.FluorescenceIndex],
            grade.Fancy ? grade.ColorText : grade.ColorGrade,
            grade.Proportions,
            grade.Symmetry,
            grade.Polish,
        };
        for (var i = 0; i < checklist.Length; i++)
        {
            var done = progress.Done[i];
            checklist[i].text = $"{labels[i]} :  {(done ? values[i] : "à faire")}";
            checklist[i].color = done ? Good : Warning;
        }

        if (AllDone())
        {
            certificateSummary.text = $"Taille : {grade.Cut}    ·    Croissance : {grade.Growth}\n\n" +
                (stone.Certificate != null
                    ? "Le certificat est attaché à la pierre."
                    : $"Toutes les étapes sont validées. Le bureau établit le certificat pour {DiamondGradeModel.Fee(stone)} €.");
        }
        else
        {
            certificateSummary.text = "Validez toutes les étapes, dans l'ordre que vous voulez, pour que le bureau établisse le certificat.";
        }
    }

    private void IssueOrShowCertificate()
    {
        if (!AllDone()) return;

        if (stone.Certificate == null)
        {
            var fee = DiamondGradeModel.Fee(stone);
            var wallet = PlayerWallet.Instance;
            if (wallet == null || !wallet.TrySpend(fee))
            {
                Say($"Fonds insuffisants : le certificat coûte {fee} €.", false);
                return;
            }

            var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            var certificate = new DiamondCertificate
            {
                number = $"GL-{100000 + StoneOptics.StableHash((stone.SpecimenId ?? "") + "|cert") % 900000}",
                issuer = "Bureau de grading — Immeuble des Diamantaires",
                issueDay = day,
                colorGrade = grade.ColorGrade,
                clarityGrade = grade.ClarityGrade,
                caratWeight = grade.Carat,
                shape = stone.ShapeName,
                cutStyle = stone.CutStyleName,
            };
            stone.Certify(certificate, grade);
            statusText.text = "Certificat établi. Il est attaché à la pierre.";
            statusText.color = Good;
            ToolFeedbackUI.Instance?.ShowMessage($"Certificat de diamant n° {certificate.number} établi pour {stone.SpecimenId}.");
            RefreshAll();
        }

        DiamondCertificateUI.Instance?.Open(stone);
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private Button Btn(Transform parent, string label, int fontSize, Color color, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var button = RuntimeUIKit.MakeButton("Btn", parent, label, fontSize, color, position, size);
        button.onClick.AddListener(action);
        return button;
    }

    private static Text Txt(Transform parent, string content, int fontSize, TextAnchor alignment, Color color, Vector2 position, Vector2 size)
    {
        var text = RuntimeUIKit.MakeText("Text", parent, content, fontSize, alignment, color, position, size);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Image Pic(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private GameObject MakeGroup(string name)
    {
        var group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(panel.transform, false);
        var rect = group.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        return group;
    }

    private void BuildPanel()
    {
        panel = new GameObject("GradingPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-180f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Bureau de grading — Immeuble des Diamantaires", 20, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(620f, 28f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 14, new Color(0.45f, 0.20f, 0.18f, 1f), new Vector2(PanelWidth - 52f, -10f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        // Tray, on the left.
        RuntimeUIKit.MakeText("TrayTitle", panel.transform, "Plateau de grading", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -92f), new Vector2(200f, 18f));
        var tray = RuntimeUIKit.MakeBox("Tray", panel.transform, new Vector2(24f, -114f), new Vector2(200f, 200f), new Color(0.94f, 0.95f, 0.97f, 1f));
        tray.gameObject.AddComponent<MagnetDropZone>().OnGemDropped = OnDropped;
        trayImage = Pic("TrayStone", panel.transform, new Vector2(54f, -144f), new Vector2(140f, 140f));
        trayImage.enabled = false;
        trayHint = RuntimeUIKit.MakeText("TrayHint", panel.transform, "Glissez un diamant ici", 13, TextAnchor.MiddleCenter, new Color(0.45f, 0.48f, 0.55f), new Vector2(24f, -114f), new Vector2(200f, 200f));
        Btn(panel.transform, "Retirer le diamant", 13, ButtonColor, new Vector2(24f, -322f), new Vector2(200f, 28f), () => SetStone(null));
        statusText = Txt(panel.transform, "", 12, TextAnchor.UpperLeft, LightText, new Vector2(24f, -362f), new Vector2(200f, 90f));
        Txt(panel.transform, "Les étapes se font dans l'ordre que vous voulez : passez d'un onglet à l'autre. Les indices vous guident.", 11, TextAnchor.UpperLeft,
            new Color(0.6f, 0.64f, 0.7f), new Vector2(24f, -470f), new Vector2(200f, 80f));

        // Tabs, like the tabs of a web page.
        tabs = new Image[StepCount];
        tabMarks = new Image[StepCount];
        for (var i = 0; i < StepCount; i++)
        {
            var captured = (Step)i;
            var button = Btn(panel.transform, TabNames[i], 11, ButtonColor, new Vector2(16f + i * 96.4f, -48f), new Vector2(94f, 28f), () => GoTo(captured));
            tabs[i] = button.GetComponent<Image>();
            var mark = Pic("Done", button.transform, new Vector2(78f, -2f), new Vector2(14f, 14f), RuntimeUIKit.Validated);
            mark.enabled = false;
            tabMarks[i] = mark;
        }

        // Validation bar, shared by the tabs.
        validateButton = Btn(panel.transform, "", 15, ActionColor, new Vector2(MainX, -592f), new Vector2(250f, 40f), Validate);
        validateLabel = validateButton.GetComponentInChildren<Text>();
        hintText = Txt(panel.transform, "", 13, TextAnchor.UpperLeft, Warning, new Vector2(MainX + 262f, -586f), new Vector2(354f, 62f));

        groups = new GameObject[StepCount];
        for (var i = 0; i < StepCount; i++) groups[i] = MakeGroup("Step" + i);

        BuildWeightGroup();
        BuildDimensionsGroup();
        BuildPlotGroup();
        BuildFluorescenceGroup();
        BuildColorGroup();
        BuildProportionsGroup();
        symmetryTab = BuildLevelGroup(groups[(int)Step.Symmetry].transform, Step.Symmetry, DiamondGradeModel.SymmetryItems, "Mesure");
        polishTab = BuildLevelGroup(groups[(int)Step.Polish].transform, Step.Polish, DiamondGradeModel.PolishItems, "Mesure");
        BuildCertificateGroup();
    }

    private void BuildWeightGroup()
    {
        var t = groups[(int)Step.Weight].transform;

        // The precision pocket scale of the laboratory: glass lid over the pan, and the LCD.
        RuntimeUIKit.MakeBox("Body", t, new Vector2(MainX, -100f), new Vector2(280f, 360f), new Color(0.07f, 0.07f, 0.08f, 1f));
        RuntimeUIKit.MakeBox("Lid", t, new Vector2(MainX + 12f, -112f), new Vector2(256f, 200f), new Color(0.80f, 0.86f, 0.92f, 0.22f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Platform", t, new Vector2(MainX + 22f, -122f), new Vector2(236f, 180f), new Color(0.04f, 0.04f, 0.05f, 1f)).raycastTarget = false;
        Pic("Pan", t, new Vector2(MainX + 22f + 118f - 70f, -122f - 90f + 70f), new Vector2(140f, 140f), WeightScaleUI.MakePanSprite(96));
        balanceStone = Pic("BalanceStone", t, new Vector2(MainX + 22f + 118f - 36f, -122f - 90f + 36f), new Vector2(72f, 72f));
        RuntimeUIKit.MakeText("Brand", t, "GEMLAB", 17, TextAnchor.MiddleLeft, Color.white, new Vector2(MainX + 22f, -270f), new Vector2(120f, 24f)).fontStyle = FontStyle.Bold;

        RuntimeUIKit.MakeBox("LcdFrame", t, new Vector2(MainX + 16f, -300f), new Vector2(248f, 90f), new Color(0.02f, 0.02f, 0.03f, 1f));
        RuntimeUIKit.MakeBox("Lcd", t, new Vector2(MainX + 22f, -306f), new Vector2(236f, 78f), new Color(0.78f, 0.85f, 0.87f, 1f), false);
        balanceDisplay = new SevenSegmentDisplay(t, new Vector2(MainX + 34f, -316f), 5, 28f, 52f, 10f, 5f,
            new Color(0.08f, 0.12f, 0.30f, 1f), new Color(0.08f, 0.12f, 0.30f, 0.07f));
        balanceDisplay.ShowNumber(0, 2);
        RuntimeUIKit.MakeText("Unit", t, "ct", 14, TextAnchor.LowerRight, new Color(0.08f, 0.12f, 0.30f, 1f), new Vector2(MainX + 226f, -358f), new Vector2(30f, 20f)).fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Max", t, "Max: 50 ct", 12, TextAnchor.MiddleLeft, Color.white, new Vector2(MainX + 22f, -400f), new Vector2(110f, 18f));
        RuntimeUIKit.MakeText("Resolution", t, "d: 0.01 ct", 12, TextAnchor.MiddleRight, Color.white, new Vector2(MainX + 148f, -400f), new Vector2(110f, 18f));

        Txt(t, "Balance à carats", 14, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 310f, -100f), new Vector2(300f, 20f));
        weightInfo = Txt(t, "", 13, TextAnchor.UpperLeft, LightText, new Vector2(MainX + 310f, -128f), new Vector2(300f, 110f));
    }

    private void BuildDimensionsGroup()
    {
        var t = groups[(int)Step.Dimensions].transform;
        var silver = new Color(0.80f, 0.82f, 0.86f, 1f);
        var ink = new Color(0.08f, 0.10f, 0.10f, 1f);

        // The gemstone gauge of the laboratory: silver caliper, round body with its dial.
        RuntimeUIKit.MakeBox("Zone", t, new Vector2(GaugeX, GaugeY), new Vector2(340f, GaugeJawHeight + 6f), new Color(0.04f, 0.05f, 0.07f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Bar", t, new Vector2(GaugeX, GaugeY), new Vector2(340f, 16f), silver).raycastTarget = false;
        RuntimeUIKit.MakeBox("FixedJaw", t, new Vector2(GaugeX, GaugeY), new Vector2(GaugeJawWidth, GaugeJawHeight), silver).raycastTarget = false;
        var jaw = RuntimeUIKit.MakeBox("MovingJaw", t, new Vector2(GaugeX + GaugeJawWidth + GaugeMaxGap, GaugeY), new Vector2(GaugeJawWidth, GaugeJawHeight), silver);
        jaw.raycastTarget = false;
        movingJaw = jaw.rectTransform;

        var stoneGo = new GameObject("GaugeStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(t, false);
        gaugeStoneRect = stoneGo.GetComponent<RectTransform>();
        gaugeStoneRect.anchorMin = gaugeStoneRect.anchorMax = new Vector2(0f, 1f);
        gaugeStoneRect.pivot = new Vector2(0.5f, 0.5f);
        gaugeStone = stoneGo.GetComponent<Image>();
        gaugeStone.raycastTarget = false;
        gaugeStone.preserveAspect = false;
        gaugeView = new GemView3D(48);
        gaugeStone.sprite = gaugeView.Sprite;
        gaugeHint = Txt(t, "", 12, TextAnchor.MiddleCenter, new Color(0.55f, 0.58f, 0.65f), new Vector2(GaugeX + GaugeJawWidth, GaugeY - 50f), new Vector2(300f, 18f));

        RuntimeUIKit.MakeBox("Handle", t, new Vector2(GaugeX + 130f, GaugeY - GaugeJawHeight - 4f), new Vector2(60f, 44f), new Color(0.06f, 0.06f, 0.07f, 1f));
        Pic("DialBody", t, new Vector2(GaugeX + 55f, GaugeY - 160f), new Vector2(210f, 210f), MicrometerUI.MakeDiscSprite(128));
        RuntimeUIKit.MakeText("Brand", t, "GEMLAB", 17, TextAnchor.MiddleCenter, Color.white, new Vector2(GaugeX + 75f, GaugeY - 214f), new Vector2(170f, 24f)).fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", t, "Electronic Gemstone Gauge", 10, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f), new Vector2(GaugeX + 75f, GaugeY - 236f), new Vector2(170f, 16f));
        RuntimeUIKit.MakeBox("LcdFrame", t, new Vector2(GaugeX + 92f, GaugeY - 270f), new Vector2(136f, 50f), new Color(0.78f, 0.80f, 0.82f, 1f));
        RuntimeUIKit.MakeBox("Lcd", t, new Vector2(GaugeX + 96f, GaugeY - 274f), new Vector2(128f, 42f), new Color(0.62f, 0.66f, 0.64f, 1f), false);
        gaugeDisplay = new SevenSegmentDisplay(t, new Vector2(GaugeX + 102f, GaugeY - 278f), 4, 18f, 30f, 6f, 3f, ink, new Color(0.08f, 0.10f, 0.10f, 0.07f));
        gaugeDisplay.ShowNumber(0, 1);
        RuntimeUIKit.MakeText("Unit", t, "mm", 11, TextAnchor.UpperRight, ink, new Vector2(GaugeX + 192f, GaugeY - 276f), new Vector2(28f, 14f)).fontStyle = FontStyle.Bold;

        // Side panel: position of the stone, readings.
        const float sx = MainX + 370f;
        Txt(t, "Position de la pierre", 14, TextAnchor.UpperLeft, Gold, new Vector2(sx, -100f), new Vector2(240f, 20f));
        gaugePositionButtons = new Image[3];
        var labels = new[] { "Diamètre maximal", "Diamètre minimal", "Profondeur (sur la tranche)" };
        for (var i = 0; i < 3; i++)
        {
            var captured = (GaugePosition)i;
            gaugePositionButtons[i] = Btn(t, labels[i], 13, ButtonColor, new Vector2(sx, -124f - i * 34f), new Vector2(240f, 28f), () => SelectGaugePosition(captured)).GetComponent<Image>();
        }
        Btn(t, "Mesurer", 15, new Color(0.30f, 0.42f, 0.65f, 1f), new Vector2(sx, -232f), new Vector2(240f, 36f), MeasureGauge);
        Txt(t, "Mesures", 14, TextAnchor.UpperLeft, Gold, new Vector2(sx, -282f), new Vector2(240f, 20f));
        RuntimeUIKit.MakeBox("ReadingsBox", t, new Vector2(sx - 4f, -306f), new Vector2(248f, 176f), DarkBox);
        gaugeReadings = Txt(t, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f), new Vector2(sx + 6f, -312f), new Vector2(232f, 164f));
    }

    private void BuildPlotGroup()
    {
        var t = groups[(int)Step.Plot].transform;

        // The top view of the stone, on which the symbols are placed.
        var back = RuntimeUIKit.MakeBox("PlotBack", t, new Vector2(PlotX, PlotY), new Vector2(PlotSize, PlotSize), new Color(0.96f, 0.97f, 0.99f, 1f));
        plotRect = back.rectTransform;
        var click = back.gameObject.AddComponent<Button>();
        click.transition = Selectable.Transition.None;
        click.onClick.AddListener(OnPlotClicked);
        plotTop = Pic("PlotTop", t, new Vector2(PlotX + 8f, PlotY - 8f), new Vector2(PlotSize - 16f, PlotSize - 16f));
        marksRoot = new GameObject("PlayerMarks", typeof(RectTransform)).GetComponent<RectTransform>();
        marksRoot.SetParent(t, false);
        marksRoot.anchorMin = marksRoot.anchorMax = new Vector2(0f, 1f);
        marksRoot.pivot = new Vector2(0f, 1f);
        marksRoot.anchoredPosition = new Vector2(PlotX, PlotY);
        marksRoot.sizeDelta = new Vector2(PlotSize, PlotSize);
        hiddenMarks = new Image[0];

        loupeRing = Pic("Loupe", t, Vector2.zero, new Vector2(LoupeRadius * 2f, LoupeRadius * 2f), PlotSymbols.Ring);
        loupeRing.color = new Color(0.2f, 0.45f, 0.9f, 0.9f);
        loupeRing.gameObject.SetActive(false);

        // The symbols to choose from: red inside the stone, green on its surface.
        var kinds = (PlotKind[])System.Enum.GetValues(typeof(PlotKind));
        paletteButtons = new Image[kinds.Length];
        Txt(t, "Inclusions (rouge)", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 320f, -100f), new Vector2(240f, 18f));
        Txt(t, "Défauts de surface (vert)", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 320f, -228f), new Vector2(240f, 18f));
        for (var i = 0; i < kinds.Length; i++)
        {
            var kind = kinds[i];
            var internalKind = DiamondGradeModel.IsInternal(kind);
            var slot = internalKind ? i : i - 6;
            var origin = new Vector2(MainX + 320f + (slot % 2) * 150f, (internalKind ? -122f : -250f) - (slot / 2) * 32f);
            var button = Btn(t, DiamondGradeModel.KindName(kind), 12, new Color(0.22f, 0.24f, 0.30f, 1f), origin, new Vector2(144f, 28f), () => { plotKind = kind; if (stone != null) RefreshPlot(); });
            paletteButtons[i] = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleRight;
            label.rectTransform.offsetMax = new Vector2(-10f, 0f);
            MakeSymbol("Symbol", button.transform, kind, PlotSymbols.ColorOf(kind), new Vector2(8f, -3f), 22f);
        }
        Btn(t, "Effacer le dernier", 12, ButtonColor, new Vector2(MainX + 320f, -358f), new Vector2(144f, 26f), () =>
        {
            if (stone == null || progress.PlotOk || progress.Marks.Count == 0) return;
            progress.Marks.RemoveAt(progress.Marks.Count - 1);
            RefreshPlot();
        });
        Btn(t, "Tout effacer", 12, ButtonColor, new Vector2(MainX + 470f, -358f), new Vector2(144f, 26f), () =>
        {
            if (stone == null || progress.PlotOk) return;
            progress.Marks.Clear();
            RefreshPlot();
        });
        plotCount = Txt(t, "", 12, TextAnchor.UpperLeft, LightText, new Vector2(MainX + 320f, -388f), new Vector2(290f, 16f));
        loupeText = Txt(t, "Dans la loupe : passez la souris sur la pierre.", 12, TextAnchor.UpperLeft, new Color(0.7f, 0.85f, 1f), new Vector2(MainX, -406f), new Vector2(616f, 16f));

        // The clarity grade, read once the plot is done.
        Txt(t, "Pureté", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -428f), new Vector2(200f, 18f));
        clarityButtons = new Image[DiamondGrading.ClarityScale.Length];
        for (var i = 0; i < clarityButtons.Length; i++)
        {
            var captured = i;
            var button = Btn(t, DiamondGrading.ClarityScale[i], 12, ButtonColor, new Vector2(MainX + i * 56f, -448f), new Vector2(52f, 28f), () => SelectClarity(captured));
            clarityButtons[i] = button.GetComponent<Image>();
        }
        clarityDescription = Txt(t, "", 13, TextAnchor.UpperLeft, LightText, new Vector2(MainX, -486f), new Vector2(616f, 60f));
    }

    private void BuildFluorescenceGroup()
    {
        var t = groups[(int)Step.Fluorescence].transform;

        RuntimeUIKit.MakeBox("UvBox", t, new Vector2(MainX, -100f), new Vector2(300f, 250f), new Color(0.02f, 0.02f, 0.06f, 1f));
        Txt(t, "Chambre noire", 12, TextAnchor.UpperLeft, new Color(0.5f, 0.55f, 0.7f), new Vector2(MainX + 10f, -106f), new Vector2(200f, 16f));
        fluoGlow = Pic("Glow", t, new Vector2(MainX + 40f, -120f), new Vector2(220f, 220f), RuntimeUIKit.Glow);
        fluoStone = Pic("UvStone", t, new Vector2(MainX + 95f, -175f), new Vector2(110f, 110f));
        lampLabel = Btn(t, "", 13, new Color(0.30f, 0.30f, 0.62f, 1f), new Vector2(MainX, -358f), new Vector2(300f, 32f), ToggleLamp).GetComponentInChildren<Text>();

        Txt(t, "Pierres de référence", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 320f, -100f), new Vector2(240f, 18f));
        fluoReferenceGlow = new Image[4];
        for (var i = 0; i < 4; i++)
        {
            var origin = new Vector2(MainX + 320f + (i % 2) * 150f, -124f - (i / 2) * 118f);
            RuntimeUIKit.MakeBox("Ref" + i, t, origin, new Vector2(144f, 112f), new Color(0.02f, 0.02f, 0.06f, 1f));
            fluoReferenceGlow[i] = Pic("RefGlow" + i, t, origin + new Vector2(22f, -4f), new Vector2(100f, 100f), RuntimeUIKit.Glow);
            var master = Pic("RefStone" + i, t, origin + new Vector2(42f, -24f), new Vector2(60f, 60f), DiamondGradeModel.MasterSprite(3));
            master.color = new Color(0.62f, 0.66f, 0.85f, 1f);
            Txt(t, DiamondGradeModel.FluorescenceEnglish[i], 12, TextAnchor.MiddleCenter, LightText, origin + new Vector2(0f, -88f), new Vector2(144f, 20f));
        }

        Txt(t, "Fluorescence", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -406f), new Vector2(200f, 18f));
        fluoButtons = new Image[4];
        for (var i = 0; i < 4; i++)
        {
            var captured = i;
            var label = $"{DiamondGradeModel.FluorescenceScale[i]} ({DiamondGradeModel.FluorescenceEnglish[i]})";
            var button = Btn(t, label, 13, ButtonColor, new Vector2(MainX + i * 154f, -428f), new Vector2(148f, 34f), () => SelectFluorescence(captured));
            fluoButtons[i] = button.GetComponent<Image>();
        }
        fluoInfo = Txt(t, "", 13, TextAnchor.UpperLeft, LightText, new Vector2(MainX, -474f), new Vector2(616f, 40f));
    }

    private void BuildColorGroup()
    {
        var t = groups[(int)Step.Color].transform;

        RuntimeUIKit.MakeBox("StoneCard", t, new Vector2(MainX, -100f), new Vector2(170f, 180f), Color.white);
        comparedStone = Pic("ComparedStone", t, new Vector2(MainX + 24f, -112f), new Vector2(122f, 122f));
        Txt(t, "La pierre", 13, TextAnchor.MiddleCenter, new Color(0.2f, 0.22f, 0.28f), new Vector2(MainX, -246f), new Vector2(170f, 22f));

        RuntimeUIKit.MakeBox("MasterCard", t, new Vector2(MainX + 186f, -100f), new Vector2(170f, 180f), Color.white);
        comparedMaster = Pic("ComparedMaster", t, new Vector2(MainX + 210f, -112f), new Vector2(122f, 122f));
        masterLabel = Txt(t, "", 13, TextAnchor.MiddleCenter, new Color(0.2f, 0.22f, 0.28f), new Vector2(MainX + 186f, -246f), new Vector2(170f, 22f));

        colorInfo = Txt(t, "", 13, TextAnchor.UpperLeft, LightText, new Vector2(MainX + 376f, -100f), new Vector2(240f, 180f));

        Txt(t, "Master stones", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -304f), new Vector2(200f, 18f));
        masterButtons = new Image[DiamondGrading.ColorScale.Length];
        for (var i = 0; i < masterButtons.Length; i++)
        {
            var captured = i;
            var button = Btn(t, "", 10, new Color(0.93f, 0.94f, 0.97f, 1f), new Vector2(MainX + i * 26.6f, -326f), new Vector2(25f, 25f), () => SelectMaster(captured));
            masterButtons[i] = button.GetComponent<Image>();
            var icon = Pic("Icon", button.transform, new Vector2(1f, -1f), new Vector2(23f, 23f), DiamondGradeModel.MasterSprite(i));
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(1f, 1f);
            icon.rectTransform.offsetMax = new Vector2(-1f, -1f);
            Txt(t, DiamondGrading.Color(i), 10, TextAnchor.MiddleCenter, LightText, new Vector2(MainX + i * 26.6f - 2f, -354f), new Vector2(29f, 14f));
        }
    }

    private void BuildProportionsGroup()
    {
        var t = groups[(int)Step.Proportions].transform;

        profileImage = new GameObject("Profile", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        profileImage.transform.SetParent(t, false);
        RuntimeUIKit.SetTopLeft(profileImage.rectTransform, new Vector2(MainX, -96f), new Vector2(ProfileW, ProfileH));
        profileImage.raycastTarget = false;

        // Graduations of the rulers.
        for (var pct = 0; pct <= 70; pct += 10)
            Txt(t, pct.ToString(), 9, TextAnchor.MiddleRight, new Color(0.3f, 0.32f, 0.4f), new Vector2(MainX + 2f, -96f - ProfileTop - pct * ProfileScale - 7f), new Vector2(22f, 14f));
        Txt(t, "0 %", 9, TextAnchor.MiddleCenter, new Color(0.3f, 0.32f, 0.4f), new Vector2(MainX + ProfileCenter - 150f - 20f, -96f - 10f), new Vector2(40f, 14f));
        Txt(t, "50 %", 9, TextAnchor.MiddleCenter, new Color(0.3f, 0.32f, 0.4f), new Vector2(MainX + ProfileCenter - 20f, -96f - 10f), new Vector2(40f, 14f));
        Txt(t, "100 %", 9, TextAnchor.MiddleCenter, new Color(0.3f, 0.32f, 0.4f), new Vector2(MainX + ProfileCenter + 150f - 20f, -96f - 10f), new Vector2(40f, 14f));

        // The marks of the estimates, drawn above the profile.
        estimateMarkers = new RectTransform[7];
        var markerColors = new[] { EstimateColors[1], EstimateColors[2], EstimateColors[3], EstimateColors[0], EstimateColors[0], EstimateColors[4], EstimateColors[4] };
        for (var i = 0; i < estimateMarkers.Length; i++)
        {
            var mark = RuntimeUIKit.MakeBox("Marker" + i, t, Vector2.zero, new Vector2(10f, 2f), markerColors[i], false);
            mark.raycastTarget = false;
            estimateMarkers[i] = mark.rectTransform;
        }

        // The estimates: move each mark until it falls on the matching feature of the drawing.
        Txt(t, "Estimations (en % du diamètre)", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 372f, -96f), new Vector2(244f, 18f));
        estimateValues = new Text[5];
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var y = -118f - i * 38f;
            Txt(t, EstimateNames[i], 11, TextAnchor.MiddleLeft, EstimateColors[i], new Vector2(MainX + 372f, y), new Vector2(150f, 16f));
            estimateValues[i] = RuntimeUIKit.MakeText("Value" + i, t, "", 13, TextAnchor.MiddleLeft, Color.white, new Vector2(MainX + 372f, y + 16f), new Vector2(60f, 18f));
            Btn(t, "--", 11, ButtonColor, new Vector2(MainX + 438f, y + 15f), new Vector2(40f, 22f), () => Adjust(index, -2f));
            Btn(t, "-", 11, ButtonColor, new Vector2(MainX + 482f, y + 15f), new Vector2(40f, 22f), () => Adjust(index, -0.5f));
            Btn(t, "+", 11, ButtonColor, new Vector2(MainX + 526f, y + 15f), new Vector2(40f, 22f), () => Adjust(index, 0.5f));
            Btn(t, "++", 11, ButtonColor, new Vector2(MainX + 570f, y + 15f), new Vector2(40f, 22f), () => Adjust(index, 2f));
        }

        proportionInfo = Txt(t, "", 11, TextAnchor.UpperLeft, LightText, new Vector2(MainX + 372f, -314f), new Vector2(244f, 100f));

        // The grade of the proportions: the lowest of the criteria.
        Txt(t, "Qualité des proportions", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 372f, -412f), new Vector2(244f, 18f));
        proportionButtons = new Image[4];
        for (var i = 0; i < 4; i++)
        {
            var rank = i;
            var button = Btn(t, DiamondGradeModel.CutShort[i], 13, ButtonColor, new Vector2(MainX + 372f + i * 62f, -432f), new Vector2(58f, 28f), () => PickProportion(rank));
            proportionButtons[i] = button.GetComponent<Image>();
        }

        // The grading table.
        var header = new[] { "Critère", "Excellent", "Très bon", "Bon (sinon Moyen)" };
        var rows = new[]
        {
            new[] { "Largeur de table (%)", "53 – 62", "51 – 65", "49 – 68" },
            new[] { "Angle de couronne β (°)", "32 – 36", "30 – 38", "27 – 40" },
            new[] { "Angle de pavillon α (°)", "40,2 – 41,8", "39,6 – 42,4", "38,8 – 43,4" },
            new[] { "Profondeur totale (%)", "58 – 63,5", "56 – 65,5", "53 – 68" },
            new[] { "Ceinture", "mince à épaisse", "très mince, très épaisse", "extrêmes" },
            new[] { "Colette", "pointue à petite", "moyenne", "grande" },
        };
        var xs = new[] { 0f, 170f, 290f, 440f };
        var widths = new[] { 170f, 120f, 150f, 176f };
        for (var c = 0; c < 4; c++)
            Txt(t, header[c], 10, TextAnchor.UpperLeft, Gold, new Vector2(MainX + xs[c], -404f), new Vector2(widths[c], 14f));
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < 4; c++)
                Txt(t, rows[r][c], 10, TextAnchor.UpperLeft, c == 0 ? LightText : new Color(0.7f, 0.74f, 0.8f), new Vector2(MainX + xs[c], -420f - r * 16f), new Vector2(widths[c], 14f));
    }

    private LevelTab BuildLevelGroup(Transform t, Step which, string[] items, string measureTitle)
    {
        var tab = new LevelTab
        {
            Root = t.gameObject,
            Names = new Text[items.Length],
            Measures = new Text[items.Length],
            Buttons = new Image[items.Length][],
        };

        Txt(t, which == Step.Symmetry ? "Symétrie — 13 écarts à relever" : "Poli — caractéristiques de surface", 13, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -98f), new Vector2(380f, 18f));
        tab.MeasureButton = Btn(t, "Mesurer à la loupe", 12, new Color(0.30f, 0.38f, 0.58f, 1f), new Vector2(MainX + 456f, -94f), new Vector2(160f, 26f), () => MeasureLevels(which));
        Txt(t, "Seuils :  Aucune < 0,8 %   ·   Faible 0,8 à 2 %   ·   Moyenne 2 à 3,5 %   ·   Forte > 3,5 %", 11, TextAnchor.UpperLeft, new Color(0.7f, 0.74f, 0.8f), new Vector2(MainX, -122f), new Vector2(616f, 16f));
        Txt(t, measureTitle, 11, TextAnchor.UpperLeft, Gold, new Vector2(MainX + 270f, -144f), new Vector2(70f, 16f));

        var rowHeight = which == Step.Symmetry ? 28f : 36f;
        for (var i = 0; i < items.Length; i++)
        {
            var y = -162f - i * rowHeight;
            tab.Names[i] = Txt(t, $"{i + 1}. {items[i]}", 12, TextAnchor.MiddleLeft, LightText, new Vector2(MainX, y), new Vector2(262f, 24f));
            tab.Measures[i] = RuntimeUIKit.MakeText("Measure" + i, t, "—", 12, TextAnchor.MiddleCenter, new Color(0.45f, 1f, 0.55f), new Vector2(MainX + 270f, y), new Vector2(64f, 24f));
            tab.Buttons[i] = new Image[4];
            for (var b = 0; b < 4; b++)
            {
                var item = i;
                var level = b;
                var button = Btn(t, DiamondGradeModel.LevelNames[b], 11, ButtonColor, new Vector2(MainX + 344f + b * 68f, y), new Vector2(64f, 24f), () => PickLevel(which, item, level));
                tab.Buttons[i][b] = button.GetComponent<Image>();
            }
        }
        tab.Result = Txt(t, "", 14, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -162f - items.Length * rowHeight - 8f), new Vector2(616f, 24f));
        return tab;
    }

    private void BuildCertificateGroup()
    {
        var t = groups[(int)Step.Certificate].transform;
        Txt(t, "Récapitulatif de la classification", 14, TextAnchor.UpperLeft, Gold, new Vector2(MainX, -98f), new Vector2(400f, 20f));
        RuntimeUIKit.MakeBox("ListBack", t, new Vector2(MainX, -124f), new Vector2(616f, 250f), DarkBox);
        checklist = new Text[8];
        for (var i = 0; i < checklist.Length; i++)
            checklist[i] = Txt(t, "", 14, TextAnchor.MiddleLeft, Warning, new Vector2(MainX + 16f, -134f - i * 29f), new Vector2(590f, 26f));
        certificateSummary = Txt(t, "", 14, TextAnchor.UpperLeft, LightText, new Vector2(MainX, -392f), new Vector2(616f, 90f));
    }
}
