using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One analysis the classic laboratory must have done before a GemLab report can be printed.
public struct ReportRequirement
{
    public string Icon;      // sprite in Resources/ToolIcons, or GemObservation.Validated
    public string Label;
    public bool Done;
}

public static class ReportRequirements
{
    public static List<ReportRequirement> Evaluate(GemInstance gem)
    {
        var list = new List<ReportRequirement>();
        bool Seen(ExaminationTool.MeasurementType type) => gem.HasObserved(type.ToString());
        void Add(string icon, string label, bool done) => list.Add(new ReportRequirement { Icon = icon, Label = label, Done = done });

        var refractiveKey = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
        var densityKey = ExaminationTool.MeasurementType.Density.ToString();

        Add(GemObservation.Validated, "Identifiée par le professeur", gem.IsIdentified);
        Add("BalanceCarat", "Poids", Seen(ExaminationTool.MeasurementType.Weight));
        Add("Micrometre", "Dimensions (3 mesures)", Seen(ExaminationTool.MeasurementType.Dimensions));
        Add("Refractometre", "Indice de réfraction", gem.HasMeasured(refractiveKey) || gem.HasObserved("RefractiveIndexOTL"));
        Add("BalanceHydrostatique", "Densité", gem.HasMeasured(densityKey));
        Add("Loupe", "Examen visuel", Seen(ExaminationTool.MeasurementType.VisualExamination));
        Add("Loupe", "Examen à la loupe", gem.HasObserved("LoupeInspection"));
        Add("Dichroscope", "Pléochroïsme", Seen(ExaminationTool.MeasurementType.Pleochroism));
        Add("Polariscope", "Caractère optique", Seen(ExaminationTool.MeasurementType.OpticalCharacter));
        Add("Polariscope", "Figure d'interférence", Seen(ExaminationTool.MeasurementType.InterferenceFigure));
        Add("Spectroscope", "Spectre d'absorption", Seen(ExaminationTool.MeasurementType.AbsorptionSpectrum));
        Add("Microscope", "Inclusions", Seen(ExaminationTool.MeasurementType.Inclusions));
        Add("CabineUV", "Fluorescence UV longues", Seen(ExaminationTool.MeasurementType.FluorescenceLongWave));
        Add("CabineUV", "Fluorescence UV courtes", Seen(ExaminationTool.MeasurementType.FluorescenceShortWave));
        Add("FiltreChelsea", "Filtre Chelsea", Seen(ExaminationTool.MeasurementType.ChelseaFilter));
        Add("Aimant", "Réaction à l'aimant", Seen(ExaminationTool.MeasurementType.Magnetism));
        return list;
    }

    public static bool AllDone(List<ReportRequirement> requirements)
    {
        foreach (var requirement in requirements)
            if (!requirement.Done) return false;
        return true;
    }
}

// The printer next to the professor: once a stone is identified and every analysis of the classic laboratory is done,
// it prints the GemLab report, which is then attached to the stone sheet. Built at runtime and added to the scene's
// Canvas automatically.
public class ReportPrinterUI : MonoBehaviour
{
    public static ReportPrinterUI Instance { get; private set; }

    private const float PanelWidth = 740f;
    private const float PanelHeight = 420f;

    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private Image slotIcon;
    private Text slotHint;
    private Text stoneText;
    private RectTransform checklist;
    private Button printButton;
    private Image printButtonImage;
    private Text printLabel;
    private GemInstance gem;

    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

    public bool IsOpen => panel != null && panel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (scene.name == "MainMenu") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<ReportPrinterUI>() == null) canvas.AddComponent<ReportPrinterUI>();
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open()
    {
        // The stone selected in the inventory is put on the printer right away.
        gem = PlayerInventory.Instance != null ? PlayerInventory.Instance.SelectedGem : null;
        Refresh();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void BuildPanel()
    {
        panel = new GameObject("ReportPrinterPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = new Vector2(-100f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        RuntimeUIKit.MakeText("Title", panel.transform, "Imprimante — Rapport GemLab", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(480f, 30f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 14, new Color(0.30f, 0.22f, 0.20f, 1f),
            new Vector2(PanelWidth - 56f, -12f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        // Left: the stone on the printer, dropped from the inventory.
        RuntimeUIKit.MakeText("SlotLabel", panel.transform, "Pierre à documenter", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -58f), new Vector2(190f, 20f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -84f), new Vector2(96f, 96f), new Color(0.20f, 0.20f, 0.24f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetGem;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearGem);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slot.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 10f);
        iconRect.offsetMax = new Vector2(-10f, -10f);
        slotIcon = iconGO.GetComponent<Image>();
        slotIcon.preserveAspect = true;
        slotIcon.raycastTarget = false;
        slotIcon.enabled = false;

        slotHint = RuntimeUIKit.MakeText("Hint", slot.transform, "Déposez la gemme ici", 11, TextAnchor.MiddleCenter,
            new Color(0.65f, 0.65f, 0.7f), Vector2.zero, new Vector2(96f, 96f));
        var hintRect = slotHint.rectTransform;
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.offsetMin = hintRect.offsetMax = Vector2.zero;

        stoneText = RuntimeUIKit.MakeText("StoneText", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.80f),
            new Vector2(24f, -190f), new Vector2(190f, 90f));

        printButton = RuntimeUIKit.MakeButton("Print", panel.transform, "Imprimer le rapport", 15, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(24f, -330f), new Vector2(190f, 48f));
        printButtonImage = printButton.GetComponent<Image>();
        printLabel = printButton.GetComponentInChildren<Text>();
        printButton.onClick.AddListener(OnPrintClicked);

        // Right: what the classic laboratory must have done.
        RuntimeUIKit.MakeText("CheckTitle", panel.transform, "Analyses du laboratoire classique", 15, TextAnchor.UpperLeft, Gold,
            new Vector2(250f, -58f), new Vector2(460f, 22f));

        var listGO = new GameObject("Checklist", typeof(RectTransform));
        listGO.transform.SetParent(panel.transform, false);
        checklist = listGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(checklist, new Vector2(250f, -88f), new Vector2(470f, 300f));
    }

    private void SetGem(GemInstance dropped)
    {
        gem = dropped;
        Refresh();
    }

    private void ClearGem()
    {
        gem = null;
        Refresh();
    }

    private void Refresh()
    {
        slotIcon.enabled = gem != null;
        if (gem != null) slotIcon.sprite = gem.DisplaySprite;
        slotHint.text = gem == null ? "Déposez la gemme ici" : "";

        for (var i = checklist.childCount - 1; i >= 0; i--)
        {
            var old = checklist.GetChild(i);
            old.SetParent(null, false);
            Destroy(old.gameObject);
        }

        if (gem == null)
        {
            stoneText.text = "Aucune pierre.\nGlissez-la depuis l'inventaire.";
            SetButton("Imprimer le rapport", false);
            return;
        }

        var requirements = ReportRequirements.Evaluate(gem);
        var column = 0;
        var row = 0;
        const int rowsPerColumn = 8;
        foreach (var requirement in requirements)
        {
            AddRequirement(requirement, new Vector2(column * 236f, -row * 36f));
            row++;
            if (row == rowsPerColumn)
            {
                row = 0;
                column++;
            }
        }

        var done = 0;
        foreach (var requirement in requirements)
            if (requirement.Done) done++;

        stoneText.text = $"{(gem.IsIdentified ? gem.Definition.displayName : "Gemme non identifiée")}\nN° {gem.DisplayId}\n\nAnalyses : {done} / {requirements.Count}";

        if (gem.HasReport) SetButton("Voir le rapport", true);
        else SetButton("Imprimer le rapport", ReportRequirements.AllDone(requirements));
    }

    private void SetButton(string label, bool enabled)
    {
        printLabel.text = label;
        printButton.interactable = enabled;
        printButtonImage.color = enabled ? new Color(0.25f, 0.50f, 0.30f, 1f) : new Color(0.28f, 0.27f, 0.27f, 1f);
    }

    private void OnPrintClicked()
    {
        if (gem == null) return;

        if (gem.HasReport)
        {
            GemReportUI.Instance?.Open(gem);
            return;
        }

        if (!ReportRequirements.AllDone(ReportRequirements.Evaluate(gem))) return;

        Sfx.Play(Sfx.Id.Printer);
        gem.IssueReport(GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1);
        ToolFeedbackUI.Instance?.ShowMessage($"Rapport GemLab n° {gem.ReportNumber} imprimé : il est ajouté à la fiche de la pierre.");
        Refresh();
        PlayerInventory.Instance?.Select(gem);
    }

    private void AddRequirement(ReportRequirement requirement, Vector2 position)
    {
        var rowGO = new GameObject("Requirement", typeof(RectTransform));
        rowGO.transform.SetParent(checklist, false);
        RuntimeUIKit.SetTopLeft(rowGO.GetComponent<RectTransform>(), position, new Vector2(226f, 32f));

        RuntimeUIKit.MakeBox("Back", rowGO.transform, Vector2.zero, new Vector2(226f, 32f),
            requirement.Done ? new Color(0.14f, 0.20f, 0.15f, 1f) : new Color(0.17f, 0.15f, 0.13f, 1f), false);

        var sprite = IconFor(requirement.Icon);
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(rowGO.transform, false);
        RuntimeUIKit.SetTopLeft(iconGO.GetComponent<RectTransform>(), new Vector2(4f, -2f), new Vector2(34f, 28f));
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = sprite != null;
        icon.color = requirement.Done ? Color.white : new Color(1f, 1f, 1f, 0.35f);

        RuntimeUIKit.MakeText("Label", rowGO.transform, requirement.Label, 12, TextAnchor.MiddleLeft,
            requirement.Done ? new Color(0.92f, 0.90f, 0.84f) : new Color(0.55f, 0.52f, 0.48f), new Vector2(44f, 0f), new Vector2(150f, 32f));

        if (requirement.Done)
        {
            var checkGO = new GameObject("Done", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(rowGO.transform, false);
            RuntimeUIKit.SetTopLeft(checkGO.GetComponent<RectTransform>(), new Vector2(200f, -8f), new Vector2(16f, 16f));
            var check = checkGO.GetComponent<Image>();
            check.sprite = RuntimeUIKit.Validated;
            check.raycastTarget = false;
        }
    }

    private Sprite IconFor(string iconName)
    {
        if (iconName == GemObservation.Validated) return RuntimeUIKit.Validated;
        if (!iconCache.TryGetValue(iconName, out var sprite))
        {
            sprite = Resources.Load<Sprite>($"ToolIcons/{iconName}");
            iconCache[iconName] = sprite;
        }
        return sprite;
    }
}
