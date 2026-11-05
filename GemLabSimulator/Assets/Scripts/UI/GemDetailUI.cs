using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One line of the stone sheet: the logo of the instrument that revealed it, and the text.
public struct GemObservation
{
    public string Icon;   // name of a sprite in Resources/ToolIcons, or Validated for what the professor confirmed
    public string Text;

    public const string Validated = "@validated";

    public GemObservation(string icon, string text)
    {
        Icon = icon;
        Text = text;
    }
}

// Everything the player knows about a stone, each fact tied to the instrument it came from.
public static class GemObservations
{
    public static List<GemObservation> Build(GemInstance gem)
    {
        var list = new List<GemObservation>();
        var def = gem.Definition;

        bool Seen(ExaminationTool.MeasurementType type) => gem.HasObserved(type.ToString());
        void Add(string icon, string text) => list.Add(new GemObservation(icon, text));

        // What the professor's identification reveals, whatever instruments were used.
        if (gem.IsIdentified)
        {
            if (!gem.IsRough) Add(GemObservation.Validated, $"Taille : {gem.ShapeName} — {gem.CutStyleName}");
            if (!Seen(ExaminationTool.MeasurementType.Weight)) Add(GemObservation.Validated, $"Poids : {gem.CaratWeight:F2} ct");
            if (DimensionReport.Lines(gem).Count == 0)
                Add(GemObservation.Validated, $"Dimensions : {gem.LengthMm:F1} x {gem.WidthMm:F1} x {gem.DepthMm:F1} mm");

            if (gem.ColorGrade != null)
            {
                Add(GemObservation.Validated, $"Couleur : {gem.ColorGrade} ({gem.ColorName})");
                Add(GemObservation.Validated, $"Pureté : {gem.DiamondClarity}");
            }
            else
            {
                if (!Seen(ExaminationTool.MeasurementType.VisualExamination)) Add(GemObservation.Validated, $"Couleur : {gem.ColorName}");
                if (!Seen(ExaminationTool.MeasurementType.Inclusions)) Add(GemObservation.Validated, $"Pureté : {gem.ClarityGrade}");
            }
            if (!string.IsNullOrEmpty(gem.TradeName)) Add(GemObservation.Validated, $"Appellation : {gem.TradeName}");
            if (!Seen(ExaminationTool.MeasurementType.Treatment)) Add(GemObservation.Validated, $"Traitement : {gem.TreatmentGrade}");
            if (!Seen(ExaminationTool.MeasurementType.Authenticity)) Add(GemObservation.Validated, $"Authenticité : {gem.AuthenticityGrade}");
            if (gem.Certificate != null) Add(GemObservation.Validated, $"Certificat : n° {gem.Certificate.number} ({gem.Certificate.issuer})");
        }

        // Micrometer, carat scale and hydrostatic balance.
        foreach (var line in DimensionReport.Lines(gem)) Add("Micrometre", line);
        if (Seen(ExaminationTool.MeasurementType.Weight))
            Add("BalanceCarat", $"Poids : {gem.CaratWeight:F2} ct ({gem.CaratWeight * 0.2f:F3} g)");

        var refractiveKey = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
        if (gem.HasMeasured(refractiveKey)) Add("Refractometre", $"Indice de réfraction : {gem.GetMeasuredValue(refractiveKey):F3}");
        else if (gem.HasObserved("RefractiveIndexOTL")) Add("Refractometre", "Indice de réfraction : OTL (hors limite du prisme, IR > 1.81)");
        if (gem.HasMeasured(refractiveKey) || gem.HasObserved("RefractiveIndexOTL"))
            Add("Refractometre", $"Biréfringence : {OpticalDescriptions.BirefringenceText(def)}");

        var densityKey = ExaminationTool.MeasurementType.Density.ToString();
        if (gem.HasMeasured(densityKey)) Add("BalanceHydrostatique", $"Densité : {gem.GetMeasuredValue(densityKey):F2}");

        // Eye and loupe.
        if (Seen(ExaminationTool.MeasurementType.VisualExamination))
        {
            Add("Loupe", $"Éclat : {def.luster}");
            Add("Loupe", $"Couleur : {gem.ColorDescription}");
            Add("Loupe", $"Transparence : {def.TransparencyLabel}");
            Add("Loupe", $"Forme : {gem.ShapeName}");
            Add("Loupe", $"Style de taille : {gem.CutStyleName}");
            if (gem.FacetSummary != null) Add("Loupe", $"Facettes : {gem.FacetSummary}");
        }
        if (gem.HasObserved("LoupeInspection"))
        {
            Add("Loupe", $"Loupe — {LoupeFeatures.InclusionSummary(gem)}");
            foreach (var line in LoupeFeatures.ExternalDefectLines(gem)) Add("Loupe", $"Loupe — {line}");
        }

        // Optical instruments.
        if (Seen(ExaminationTool.MeasurementType.Pleochroism))
            Add("Dichroscope", TreatmentSignatures.PleochroismText(gem));
        if (Seen(ExaminationTool.MeasurementType.OpticalCharacter))
            Add("Polariscope", $"Polariscope : {OpticalDescriptions.PolariscopeText(def)}");
        if (Seen(ExaminationTool.MeasurementType.InterferenceFigure))
            Add("Polariscope", $"Conoscope : {OpticalDescriptions.ConoscopeText(def)}");
        if (Seen(ExaminationTool.MeasurementType.AbsorptionSpectrum))
            Add("Spectroscope", $"Spectroscope : {TreatmentSignatures.SpectrumDescription(gem)}");
        if (Seen(ExaminationTool.MeasurementType.Inclusions))
        {
            Add("Microscope", $"Clarté : {gem.ClarityGrade} ({gem.ClarityDescription})");
            Add("Microscope", $"Inclusions : {gem.InclusionDescription}");
        }
        if (Seen(ExaminationTool.MeasurementType.FluorescenceLongWave))
            Add("CabineUV", $"Fluorescence UV longues : {def.fluorescenceLongWave}");
        if (Seen(ExaminationTool.MeasurementType.FluorescenceShortWave))
            Add("CabineUV", $"Fluorescence UV courtes : {def.fluorescenceShortWave}");
        if (Seen(ExaminationTool.MeasurementType.ChelseaFilter))
            Add("FiltreChelsea", $"Filtre Chelsea : {def.chelseaFilterReaction}");
        if (Seen(ExaminationTool.MeasurementType.Magnetism))
            Add("Aimant", $"Aimant : {def.magnetism}");

        // Conductivity testers.
        if (Seen(ExaminationTool.MeasurementType.ThermalConductivity))
            Add("TesteurThermique", $"Conductivité thermique : {(def.isThermallyConductiveLikeDiamond ? "Élevée (type diamant)" : "Faible")}");
        if (Seen(ExaminationTool.MeasurementType.ElectricalConductivity))
            Add("TesteurElectrique", $"Conductivité électrique : {(def.isElectricallyConductive ? "Conductrice" : "Non conductrice")}");

        // Advanced instruments.
        if (Seen(ExaminationTool.MeasurementType.Treatment)) Add("FTIR", $"Traitement (FTIR) : {gem.TreatmentGrade}");
        if (Seen(ExaminationTool.MeasurementType.Authenticity)) Add("Raman", $"Authenticité (Raman) : {gem.AuthenticityGrade}");
        if (Seen(ExaminationTool.MeasurementType.ConfirmedOrigin)) Add("UvVisNir", $"Origine confirmée (UV-Vis-NIR) : {gem.Origin.name}");
        if (Seen(ExaminationTool.MeasurementType.GrowthStructure)) Add("MEB", $"Structure de croissance (MEB) : {gem.GrowthStructureDescription}");
        if (Seen(ExaminationTool.MeasurementType.ElementalAnalysis)) Add("EDXRF", $"EDXRF : {EdxrfSpectrum.Summary(gem)}");
        if (Seen(ExaminationTool.MeasurementType.TraceElements)) Add("LAICPMS", $"LA-ICP-MS : {LaIcpMsAnalysis.Summary(gem)}");
        if (Seen(ExaminationTool.MeasurementType.Photoluminescence)) Add("Photoluminescence", $"Photoluminescence : {PhotoluminescenceSpectrum.Summary(gem)}");
        if (Seen(ExaminationTool.MeasurementType.DiamondScreening)) Add("DiamondSure", $"DiamondSure : {DiamondSure.Label(gem)}");
        if (Seen(ExaminationTool.MeasurementType.DiamondGrowthImaging))
            Add("DiamondView", $"DiamondView : {DiamondViewImage.Summary(gem)}");

        // What the advanced instruments showed in detail (bands, peaks, elements, images): optional, only for the instruments used.
        foreach (var entry in LabFindings.Entries(gem))
            foreach (var line in entry.Lines) Add(entry.Icon, $"{entry.Title} — {line}");

        return list;
    }
}

// Stone sheet, opened by clicking a gem in the inventory: the stone turning on the left, and on the right every fact
// learned about it so far, each next to the logo of the instrument that revealed it. Built at runtime and added to the
// scene's Canvas automatically, so no scene regeneration is needed.
public class GemDetailUI : MonoBehaviour
{
    public static GemDetailUI Instance { get; private set; }

    private const float PanelWidth = 800f;
    private const float PanelHeight = 520f;
    private const float StoneBox = 256f;
    private const float AutoSpinDelay = 1.5f;       // seconds without touching the stone before it turns by itself again
    private static readonly Quaternion StartOrientation = Quaternion.Euler(-38f, 0f, 0f);

    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color RowColor = new Color(0.17f, 0.15f, 0.13f, 1f);
    private const string LabelColor = "#E8CF9B";

    private GameObject panel;
    private Image stoneImage;
    private Text infoText;
    private RectTransform content;
    private Text countText;
    private Button reportButton;
    private Button certificateButton;
    private Text certificateLabel;
    private Text reportLabel;
    private GemView3D view;
    private GemInstance gem;
    private Quaternion orientation;
    private float lastTouch;

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
        if (canvas != null && canvas.GetComponent<GemDetailUI>() == null) canvas.AddComponent<GemDetailUI>();
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

    public void Open(GemInstance target)
    {
        if (target == null) return;

        // The bag and the stone sheet share the same place on screen.
        if (BagUI.Instance != null) BagUI.Instance.Close();

        gem = target;
        orientation = StartOrientation;
        lastTouch = float.MinValue;
        Rebuild();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        DrawStone();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        gem = null;
    }

    private void Update()
    {
        if (!IsOpen) return;

        var inventory = PlayerInventory.Instance;
        if (gem == null || (inventory != null && !Holds(inventory, gem)) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H))
        {
            Close();
            return;
        }

        // The stone turns by itself around its own axis, until the player takes hold of it.
        if (Time.unscaledTime - lastTouch > AutoSpinDelay)
            orientation = orientation * Quaternion.AngleAxis(Time.unscaledDeltaTime * 38f, Vector3.forward);
        DrawStone();
    }

    private static bool Holds(PlayerInventory inventory, GemInstance stone)
    {
        for (var i = 0; i < inventory.Gems.Count; i++)
            if (inventory.Gems[i] == stone) return true;
        return false;
    }

    // Same controls as the loupe: dragging tilts the stone in every direction, the wheel rolls it.
    private void OnTilt(Vector2 delta)
    {
        if (gem == null) return;
        lastTouch = Time.unscaledTime;
        var yaw = Quaternion.AngleAxis(-delta.x * 0.7f, Vector3.up);
        var pitch = Quaternion.AngleAxis(delta.y * 0.7f, Vector3.right);
        orientation = yaw * pitch * orientation;
        DrawStone();
    }

    private void Roll(float degrees)
    {
        if (gem == null) return;
        lastTouch = Time.unscaledTime;
        orientation = Quaternion.AngleAxis(degrees, Vector3.forward) * orientation;
        DrawStone();
    }

    private void DrawStone()
    {
        if (gem == null || view == null) return;
        GemView3D.Colors(gem, out var body, out var facet);
        view.Draw(GemView3D.MeshFor(gem), GemView3D.HalfExtents(gem), orientation, body, facet, gem.Definition.Diaphaneity, gem.AsterismRays);
    }

    private void BuildPanel()
    {
        panel = new GameObject("GemDetailPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left like the bag, so the inventory (right side of the screen) stays reachable.
        rect.anchoredPosition = new Vector2(-190f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        RuntimeUIKit.MakeText("Title", panel.transform, "Fiche de la pierre", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(300f, 30f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 14, new Color(0.30f, 0.22f, 0.20f, 1f),
            new Vector2(PanelWidth - 56f, -12f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        // Left: the stone turning in a frame, then what the player can tell about it at a glance.
        RuntimeUIKit.MakeBox("StoneBorder", panel.transform, new Vector2(21f, -55f), new Vector2(StoneBox + 6f, StoneBox + 6f), new Color(0.55f, 0.43f, 0.28f, 1f));
        var frame = RuntimeUIKit.MakeBox("StoneFrame", panel.transform, new Vector2(24f, -58f), new Vector2(StoneBox, StoneBox), new Color(0.13f, 0.16f, 0.22f, 1f));
        var dragArea = frame.gameObject.AddComponent<DichroscopeDragArea>();
        dragArea.OnDragDelta = OnTilt;
        dragArea.OnRoll = Roll;

        var stoneGO = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGO.GetComponent<RectTransform>(), new Vector2(32f, -66f), new Vector2(StoneBox - 16f, StoneBox - 16f));
        stoneImage = stoneGO.GetComponent<Image>();
        stoneImage.preserveAspect = true;
        stoneImage.raycastTarget = false;
        view = new GemView3D(128);
        stoneImage.sprite = view.Sprite;

        RuntimeUIKit.MakeText("RotateHint", panel.transform, "Glisser : tourner la pierre — molette : pivoter", 10, TextAnchor.MiddleCenter,
            new Color(0.60f, 0.62f, 0.68f), new Vector2(28f, -58f - StoneBox + 20f), new Vector2(StoneBox - 8f, 14f));

        reportButton = RuntimeUIKit.MakeButton("ReportButton", panel.transform, "", 12, new Color(0.30f, 0.26f, 0.18f, 1f),
            new Vector2(24f, -322f), new Vector2(StoneBox, 44f));
        reportButton.onClick.AddListener(() => GemReportUI.Instance?.Open(gem));
        reportLabel = reportButton.GetComponentInChildren<Text>();
        reportLabel.alignment = TextAnchor.MiddleLeft;
        reportLabel.rectTransform.offsetMin = new Vector2(48f, 0f);
        var reportIconGO = new GameObject("ReportIcon", typeof(RectTransform), typeof(Image));
        reportIconGO.transform.SetParent(reportButton.transform, false);
        RuntimeUIKit.SetTopLeft(reportIconGO.GetComponent<RectTransform>(), new Vector2(10f, -6f), new Vector2(32f, 32f));
        var reportIcon = reportIconGO.GetComponent<Image>();
        reportIcon.sprite = RuntimeUIKit.ReportIcon;
        reportIcon.preserveAspect = true;
        reportIcon.raycastTarget = false;
        reportButton.gameObject.SetActive(false);

        // The diamond certificate of the grading office, under the report.
        certificateButton = RuntimeUIKit.MakeButton("CertificateButton", panel.transform, "", 12, new Color(0.20f, 0.28f, 0.42f, 1f),
            new Vector2(24f, -374f), new Vector2(StoneBox, 44f));
        certificateButton.onClick.AddListener(() => DiamondCertificateUI.Instance?.Open(gem));
        certificateLabel = certificateButton.GetComponentInChildren<Text>();
        certificateLabel.alignment = TextAnchor.MiddleLeft;
        certificateLabel.rectTransform.offsetMin = new Vector2(48f, 0f);
        var certificateIconGO = new GameObject("CertificateIcon", typeof(RectTransform), typeof(Image));
        certificateIconGO.transform.SetParent(certificateButton.transform, false);
        RuntimeUIKit.SetTopLeft(certificateIconGO.GetComponent<RectTransform>(), new Vector2(10f, -6f), new Vector2(32f, 32f));
        var certificateIcon = certificateIconGO.GetComponent<Image>();
        certificateIcon.sprite = GemIconFactory.GetSolid("Rond", "Brillant", new Color(0.30f, 0.50f, 0.80f), new Color(0.75f, 0.88f, 1f));
        certificateIcon.preserveAspect = true;
        certificateIcon.raycastTarget = false;
        certificateButton.gameObject.SetActive(false);

        infoText = RuntimeUIKit.MakeText("Info", panel.transform, "", 13, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.80f),
            new Vector2(24f, -326f), new Vector2(StoneBox, 180f));
        infoText.supportRichText = true;

        // Right: the scrolling list of what is known.
        RuntimeUIKit.MakeText("ListTitle", panel.transform, "Analyses réalisées", 15, TextAnchor.UpperLeft, Gold, new Vector2(310f, -58f), new Vector2(240f, 22f));
        countText = RuntimeUIKit.MakeText("Count", panel.transform, "", 12, TextAnchor.UpperRight, new Color(0.65f, 0.62f, 0.58f),
            new Vector2(PanelWidth - 24f - 200f, -61f), new Vector2(200f, 20f));
        BuildList();
    }

    private void BuildList()
    {
        var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(scrollGO.GetComponent<RectTransform>(), new Vector2(310f, -86f), new Vector2(PanelWidth - 310f - 24f, PanelHeight - 86f - 20f));
        scrollGO.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.05f, 1f);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-4f, -4f);

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
    }

    private void Rebuild()
    {
        // Detached first: Destroy only takes effect at the end of the frame, and the rows are counted right below.
        for (var i = content.childCount - 1; i >= 0; i--)
        {
            var old = content.GetChild(i);
            old.SetParent(null, false);
            Destroy(old.gameObject);
        }

        infoText.text = BuildSummary(gem);

        // The GemLab report, once printed, hangs under the picture of the stone.
        reportButton.gameObject.SetActive(gem.HasReport);
        if (gem.HasReport) reportLabel.text = $"Rapport GemLab\n{gem.ReportNumber}";

        // The buttons stack under the picture; the text of the sheet starts under the last one.
        var nextY = -322f;
        if (gem.HasReport) nextY -= 52f;
        var hasCertificate = gem.Certificate != null;
        certificateButton.gameObject.SetActive(hasCertificate);
        if (hasCertificate)
        {
            certificateLabel.text = $"Certificat de diamant\n{gem.Certificate.number}";
            RuntimeUIKit.SetTopLeft(certificateButton.GetComponent<RectTransform>(), new Vector2(24f, nextY), new Vector2(StoneBox, 44f));
            nextY -= 52f;
        }

        var infoRect = infoText.rectTransform;
        infoRect.anchoredPosition = new Vector2(24f, nextY - 4f);
        infoRect.sizeDelta = new Vector2(StoneBox, Mathf.Max(60f, 180f + (nextY + 322f)));

        var entries = GemObservations.Build(gem);
        countText.text = entries.Count > 0 ? $"{entries.Count} information{(entries.Count > 1 ? "s" : "")}" : "";

        if (entries.Count == 0)
        {
            AddRow(null, "Aucune analyse réalisée sur cette pierre.\nExamine-la avec les instruments du laboratoire ou ceux de ton sac : chaque résultat s'affichera ici avec le logo de l'outil utilisé.");
            return;
        }

        // Everything seen through the loupe shares one row, under a single logo.
        var loupeLines = new List<string>();
        var loupeIndex = -1;
        foreach (var entry in entries)
        {
            if (entry.Icon == "Loupe")
            {
                loupeLines.Add(Colorize(entry.Text));
                if (loupeIndex < 0) loupeIndex = content.childCount;
                continue;
            }
            AddRow(entry.Icon, Colorize(entry.Text));
        }
        if (loupeLines.Count > 0)
        {
            AddRow("Loupe", string.Join("\n", loupeLines));
            content.GetChild(content.childCount - 1).SetSiblingIndex(loupeIndex);
        }
    }

    // Name, number and value of the stone.
    private static string BuildSummary(GemInstance stone)
    {
        var title = stone.IsIdentified ? stone.Definition.displayName : "Gemme non identifiée";
        var text = $"<size=16><b>{title}</b></size>\nN° {stone.DisplayId}\n";

        if (stone.IsDonation) text += "Don — à conserver, non destiné à être renvoyé";
        else if (stone.IsClientStone)
            text += stone.IsIdentified
                ? $"Pierre d'un client\nRapport d'identification : {stone.IdentificationReportFee}€ (payé par le client à la livraison)" + (stone.HasReport ? "" : "\nRapport GemLab à imprimer avant l'envoi")
                : $"Pierre d'un client\nRapport d'identification : {stone.IdentificationReportFee}€ une fois la pierre renvoyée";
        else if (stone.IsIdentified)
        {
            text += $"Spécimen personnel\nValeur estimée : {stone.FinalValue}€";
            if (stone.PurchasePrice > 0) text += $"\nPrix d'achat : {stone.PurchasePrice}€";
            if (stone.CuttingCost > 0) text += $"\nCoût de la taille : {stone.CuttingCost}€";
            if (stone.PurchasePrice > 0 || stone.CuttingCost > 0)
            {
                var gain = stone.FinalValue - stone.PurchasePrice - stone.CuttingCost;
                text += $"\nBilan estimé : <color={(gain >= 0 ? "#8CE68C" : "#F07A70")}>{(gain >= 0 ? "+" : "-")}{Mathf.Abs(gain)}€</color>";
            }
        }
        else text += "Spécimen personnel";

        return text;
    }

    // "Label : value" with the label in gold.
    private static string Colorize(string line)
    {
        var split = line.IndexOf(" : ");
        return split > 0 ? $"<color={LabelColor}>{line.Substring(0, split)}</color> : {line.Substring(split + 3)}" : line;
    }

    private void AddRow(string iconName, string text)
    {
        var rowGO = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(content, false);
        rowGO.GetComponent<Image>().color = RowColor;
        rowGO.GetComponent<Image>().raycastTarget = false;
        rowGO.GetComponent<LayoutElement>().minHeight = 44f;

        var group = rowGO.GetComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(6, 8, 4, 4);
        group.spacing = 10f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        var sprite = iconName != null ? IconFor(iconName) : null;
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGO.transform.SetParent(rowGO.transform, false);
        var icon = iconGO.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = sprite != null;
        var iconLayout = iconGO.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 44f;
        iconLayout.minWidth = 44f;
        iconLayout.preferredHeight = 36f;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textGO.transform.SetParent(rowGO.transform, false);
        var label = textGO.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 13;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.90f, 0.88f, 0.82f);
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        textGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
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
