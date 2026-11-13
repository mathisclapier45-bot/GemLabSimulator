using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The GemLab laboratory report of a stone, drawn as a sheet of paper (in the spirit of the GIA and SSEF reports):
// photo of the stone, top and profile drawings of its facets, the details, the results and the gemmological properties
// gathered with the lab instruments. Opened from the stone sheet. Built at runtime, so no scene regeneration is needed.
public class GemReportUI : MonoBehaviour
{
    public static GemReportUI Instance { get; private set; }

    private const float PageWidth = 1040f;
    private const float PageHeight = 660f;

    private static readonly Color PaperColor = new Color(0.975f, 0.968f, 0.945f, 1f);
    private static readonly Color Ink = new Color(0.13f, 0.13f, 0.16f, 1f);
    private static readonly Color Gold = new Color(0.66f, 0.55f, 0.34f, 1f);
    private static readonly Color Quiet = new Color(0.45f, 0.45f, 0.50f, 1f);
    private const string GoldTag = "#A88C57";

    private GameObject page;
    private GameObject sheet;
    private GemInstance gem;
    private bool labPage;          // second page: the advanced laboratory analyses

    public bool IsOpen => page != null && page.activeSelf;

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
        if (canvas != null && canvas.GetComponent<GemReportUI>() == null) canvas.AddComponent<GemReportUI>();
    }

    private void Awake()
    {
        Instance = this;
        page = new GameObject("GemReportPage", typeof(RectTransform), typeof(Image));
        page.transform.SetParent(transform, false);
        var rect = page.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PageWidth, PageHeight);
        rect.anchoredPosition = Vector2.zero;
        page.GetComponent<Image>().color = PaperColor;
        page.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(GemInstance target)
    {
        if (target == null || !target.HasReport) return;
        gem = target;
        labPage = false;
        Build();
        page.SetActive(true);
        page.transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (page != null) page.SetActive(false);
        gem = null;
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ---- Page ----

    private void Build()
    {
        if (sheet != null)
        {
            sheet.transform.SetParent(null, false);
            Destroy(sheet);
        }

        sheet = new GameObject("Sheet", typeof(RectTransform));
        sheet.transform.SetParent(page.transform, false);
        var rect = sheet.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        if (labPage)
        {
            BuildLabPage();
            return;
        }

        var def = gem.Definition;
        var parent = sheet.transform;

        // Frame.
        RuntimeUIKit.MakeBox("FrameTop", parent, new Vector2(0f, 0f), new Vector2(PageWidth, 6f), Gold, false);
        RuntimeUIKit.MakeBox("FrameBottom", parent, new Vector2(0f, -PageHeight + 6f), new Vector2(PageWidth, 6f), Gold, false);

        // Header: gold emblem (a brilliant cut) and the name of the laboratory.
        var emblem = new GameObject("Emblem", typeof(RectTransform), typeof(Image));
        emblem.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(emblem.GetComponent<RectTransform>(), new Vector2(24f, -22f), new Vector2(60f, 60f));
        var emblemImage = emblem.GetComponent<Image>();
        emblemImage.sprite = GemIconFactory.GetSolid("Rond", "Brillant", new Color(0.78f, 0.62f, 0.25f), new Color(0.98f, 0.88f, 0.55f));
        emblemImage.preserveAspect = true;
        emblemImage.raycastTarget = false;

        Label("GemLab", parent, "GemLab", 44, TextAnchor.UpperLeft, Ink, new Vector2(96f, -14f), new Vector2(240f, 54f), true);
        Label("Subtitle", parent, "Laboratoire de gemmologie", 15, TextAnchor.UpperLeft, Gold, new Vector2(99f, -64f), new Vector2(260f, 22f));

        // Title block.
        Label("Species", parent, def.displayName, 28, TextAnchor.UpperLeft, Gold, new Vector2(24f, -104f), new Vector2(370f, 36f), true);
        Label("Number", parent, $"Rapport GemLab N° {gem.ReportNumber}", 16, TextAnchor.UpperLeft, Ink, new Vector2(24f, -142f), new Vector2(370f, 22f), true);
        Label("Date", parent, $"Émis le jour {gem.ReportDay}", 14, TextAnchor.UpperLeft, Ink, new Vector2(24f, -165f), new Vector2(370f, 20f));

        // Photo of the stone.
        RuntimeUIKit.MakeBox("PhotoBack", parent, new Vector2(24f, -194f), new Vector2(360f, 170f), new Color(0.90f, 0.92f, 0.96f, 1f), false);
        var photo = new GameObject("Photo", typeof(RectTransform), typeof(Image));
        photo.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(photo.GetComponent<RectTransform>(), new Vector2(24f + 95f, -194f - 8f), new Vector2(150f, 154f));
        var photoImage = photo.GetComponent<Image>();
        photoImage.sprite = gem.DisplaySprite;
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;
        Label("PhotoNote", parent, "Image non à l'échelle. Couleur approximative.", 10, TextAnchor.UpperRight, Quiet, new Vector2(24f, -366f), new Vector2(360f, 16f));

        // Drawings of the geometry.
        Label("TopLabel", parent, "Vue de dessus", 13, TextAnchor.UpperLeft, Ink, new Vector2(24f, -392f), new Vector2(172f, 18f), true);
        Label("ProfileLabel", parent, "Vue de profil", 13, TextAnchor.UpperLeft, Ink, new Vector2(212f, -392f), new Vector2(172f, 18f), true);
        Diagram("TopView", parent, new Vector2(24f, -412f), GemDiagrams.TopView(gem));
        Diagram("ProfileView", parent, new Vector2(212f, -412f), GemDiagrams.ProfileView(gem));
        Label("DiagramNote", parent, "Contours des facettes d'après la géométrie réelle de la pierre.", 10, TextAnchor.UpperLeft, Quiet,
            new Vector2(24f, -590f), new Vector2(360f, 16f));

        // Details.
        Label("DetailsTitle", parent, "Détails", 22, TextAnchor.UpperLeft, Gold, new Vector2(412f, -22f), new Vector2(250f, 30f), true);
        var y = -60f;
        void Detail(string label, string value)
        {
            Label("L_" + label, parent, label, 14, TextAnchor.UpperLeft, Ink, new Vector2(412f, y), new Vector2(250f, 20f), true);
            Label("V_" + label, parent, value, 14, TextAnchor.UpperLeft, Ink, new Vector2(412f, y - 20f), new Vector2(250f, 38f));
            y -= value.Length > 30 ? 66f : 50f;
        }

        Detail("Description de l'objet", gem.IsRough ? "Une pierre brute" : "Une pierre taillée");
        Detail("Poids", $"{gem.CaratWeight:F2} carats");
        Detail("Dimensions", $"{MeasuredOrActual(DimensionReport.LengthKey, gem.LengthMm):F2} x {MeasuredOrActual(DimensionReport.WidthKey, gem.WidthMm):F2} x {MeasuredOrActual(DimensionReport.DepthKey, gem.DepthMm):F2} mm");
        Detail("Forme", gem.ShapeDisplay);
        if (gem.IsRough && gem.Habit != null) Detail("Habitus", gem.Habit.Name.ToLowerInvariant());
        Detail("Couleur", gem.ColorGrade != null ? $"{gem.ColorGrade} — {gem.ColorName}" : gem.ColorName);
        Detail("Transparence", def.TransparencyLabel);
        Detail("Style de taille", gem.IsRough ? "Non taillée" : gem.CutStyleName);
        if (gem.FacetSummary != null) Detail("Facettes", gem.FacetSummary);
        Detail("Éclat", def.luster);
        Detail("Dureté de Mohs", $"{def.hardness:0.##}");

        // Results box.
        var bar = RuntimeUIKit.MakeBox("ResultsBar", parent, new Vector2(686f, -22f), new Vector2(330f, 30f), Gold, false);
        Label("ResultsTitle", bar.transform, "Résultats", 18, TextAnchor.MiddleLeft, Color.white, new Vector2(10f, 0f), new Vector2(300f, 30f), true);
        var box = RuntimeUIKit.MakeBox("ResultsBorder", parent, new Vector2(686f, -52f), new Vector2(330f, 156f), Gold, false);
        RuntimeUIKit.MakeBox("ResultsBody", box.transform, new Vector2(1f, -1f), new Vector2(328f, 154f), PaperColor, false);

        var ry = -60f;
        void Result(string label, string value)
        {
            Label("RL_" + label, parent, label, 14, TextAnchor.UpperLeft, Ink, new Vector2(696f, ry), new Vector2(310f, 20f), true);
            Label("RV_" + label, parent, value, 14, TextAnchor.UpperLeft, Ink, new Vector2(696f, ry - 18f), new Vector2(310f, 20f));
            ry -= 38f;
        }

        Result("Espèce", Authenticity());
        Result("Origine géographique", gem.HasObserved(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString()) ? gem.Origin.name : "Non demandée");
        Result("Traitement", gem.HasObserved(ExaminationTool.MeasurementType.Treatment.ToString()) ? gem.TreatmentGrade : "Non analysé");
        Result("Authenticité", gem.HasObserved(ExaminationTool.MeasurementType.Authenticity.ToString()) ? gem.AuthenticityGrade : "Non analysée");

        // Gemmological properties.
        Label("PropsTitle", parent, "Propriétés gemmologiques", 18, TextAnchor.UpperLeft, Gold, new Vector2(686f, -222f), new Vector2(330f, 26f), true);
        var props = Label("Props", parent, BuildProperties(), 12, TextAnchor.UpperLeft, Ink, new Vector2(686f, -250f), new Vector2(330f, 340f));
        props.supportRichText = true;

        // Closing line.
        var seal = new GameObject("Seal", typeof(RectTransform), typeof(Image));
        seal.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(seal.GetComponent<RectTransform>(), new Vector2(686f, -604f), new Vector2(30f, 30f));
        var sealImage = seal.GetComponent<Image>();
        sealImage.sprite = RuntimeUIKit.Validated;
        sealImage.preserveAspect = true;
        sealImage.raycastTarget = false;
        Label("Signed", parent, "Rapport certifié par le laboratoire GemLab\nLe directeur", 12, TextAnchor.UpperLeft, Ink, new Vector2(724f, -604f), new Vector2(290f, 34f));

        Label("Disclaimer", parent,
            "Les résultats de ce rapport ne concernent que la pierre décrite, d'après les instruments du laboratoire GemLab à la date d'émission. " +
            "Il ne constitue ni une garantie ni une évaluation.",
            10, TextAnchor.UpperLeft, Quiet, new Vector2(24f, -618f), new Vector2(630f, 28f));

        // The advanced analyses are optional: the second page only exists once an instrument of the laboratory has been used.
        if (LabFindings.Any(gem))
        {
            var more = RuntimeUIKit.MakeButton("LabPage", parent, "Analyses avancées  >", 13, new Color(0.45f, 0.36f, 0.22f, 1f),
                new Vector2(PageWidth - 44f - 200f, -10f), new Vector2(190f, 26f));
            more.onClick.AddListener(() => SetLabPage(true));
        }

        var close = RuntimeUIKit.MakeButton("Close", parent, "X", 14, new Color(0.35f, 0.30f, 0.26f, 1f),
            new Vector2(PageWidth - 44f, -10f), new Vector2(32f, 26f));
        close.onClick.AddListener(Close);
    }

    private void SetLabPage(bool value)
    {
        labPage = value;
        Build();
    }

    // Page 2: what the spectrometers, the electron microscope and the other advanced instruments showed, instrument by instrument.
    private void BuildLabPage()
    {
        var parent = sheet.transform;
        var def = gem.Definition;

        RuntimeUIKit.MakeBox("FrameTop", parent, new Vector2(0f, 0f), new Vector2(PageWidth, 6f), Gold, false);
        RuntimeUIKit.MakeBox("FrameBottom", parent, new Vector2(0f, -PageHeight + 6f), new Vector2(PageWidth, 6f), Gold, false);

        var emblem = new GameObject("Emblem", typeof(RectTransform), typeof(Image));
        emblem.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(emblem.GetComponent<RectTransform>(), new Vector2(24f, -22f), new Vector2(60f, 60f));
        var emblemImage = emblem.GetComponent<Image>();
        emblemImage.sprite = GemIconFactory.GetSolid("Rond", "Brillant", new Color(0.78f, 0.62f, 0.25f), new Color(0.98f, 0.88f, 0.55f));
        emblemImage.preserveAspect = true;
        emblemImage.raycastTarget = false;

        Label("GemLab", parent, "GemLab", 44, TextAnchor.UpperLeft, Ink, new Vector2(96f, -14f), new Vector2(240f, 54f), true);
        Label("Subtitle", parent, "Laboratoire de gemmologie", 15, TextAnchor.UpperLeft, Gold, new Vector2(99f, -64f), new Vector2(260f, 22f));

        Label("Species", parent, def.displayName, 28, TextAnchor.UpperLeft, Gold, new Vector2(412f, -22f), new Vector2(380f, 36f), true);
        Label("Number", parent, $"Rapport GemLab N° {gem.ReportNumber}  —  page 2", 14, TextAnchor.UpperLeft, Ink, new Vector2(412f, -60f), new Vector2(380f, 22f), true);
        Label("Title", parent, "Analyses de laboratoire avancées", 24, TextAnchor.UpperLeft, Gold, new Vector2(24f, -112f), new Vector2(600f, 32f), true);
        Label("Optional", parent, "Analyses facultatives : seules celles réalisées sur cette pierre figurent sur cette page.", 12, TextAnchor.UpperLeft, Quiet,
            new Vector2(24f, -146f), new Vector2(700f, 18f));

        // The entries run down two columns.
        const float columnWidth = 470f;
        const float top = -184f;
        const float bottom = -590f;
        var column = 0;
        var y = top;
        foreach (var entry in LabFindings.Entries(gem))
        {
            var height = 30f + 6f;
            foreach (var line in entry.Lines) height += Mathf.Ceil(line.Length / 62f) * 16f + 2f;

            if (y - height < bottom && column == 0)
            {
                column = 1;
                y = top;
            }
            var x = 24f + column * (columnWidth + 26f);

            var iconGo = new GameObject("Icon_" + entry.Title, typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(parent, false);
            RuntimeUIKit.SetTopLeft(iconGo.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(28f, 28f));
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = Resources.Load<Sprite>($"ToolIcons/{entry.Icon}");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = iconImage.sprite != null;

            Label("T_" + entry.Title, parent, entry.Title, 15, TextAnchor.MiddleLeft, Ink, new Vector2(x + 36f, y - 2f), new Vector2(columnWidth - 40f, 24f), true);
            var ly = y - 30f;
            foreach (var line in entry.Lines)
            {
                var lineHeight = Mathf.Ceil(line.Length / 62f) * 16f;
                Label("L_" + entry.Title, parent, line, 12, TextAnchor.UpperLeft, Ink, new Vector2(x + 36f, ly), new Vector2(columnWidth - 40f, lineHeight));
                ly -= lineHeight + 2f;
            }
            y -= height;
        }

        Label("Disclaimer", parent,
            "Ces analyses complètent le rapport sans en être une condition. Les résultats ne concernent que la pierre décrite, d'après les instruments du laboratoire GemLab.",
            10, TextAnchor.UpperLeft, Quiet, new Vector2(24f, -618f), new Vector2(900f, 28f));

        var back = RuntimeUIKit.MakeButton("Back", parent, "<  Retour au rapport", 13, new Color(0.45f, 0.36f, 0.22f, 1f),
            new Vector2(PageWidth - 44f - 200f, -10f), new Vector2(190f, 26f));
        back.onClick.AddListener(() => SetLabPage(false));
        var close = RuntimeUIKit.MakeButton("Close", parent, "X", 14, new Color(0.35f, 0.30f, 0.26f, 1f),
            new Vector2(PageWidth - 44f, -10f), new Vector2(32f, 26f));
        close.onClick.AddListener(Close);
    }

    private float MeasuredOrActual(string key, float actual)
    {
        return gem.HasMeasured(key) ? gem.GetMeasuredValue(key) : actual;
    }

    // "Natural spinel", "Synthetic ruby"... the species line of the report.
    private string Authenticity()
    {
        var name = gem.Definition.displayName;
        if (!gem.HasObserved(ExaminationTool.MeasurementType.Authenticity.ToString())) return name;
        return gem.IsSynthetic && !name.Contains("Synth") ? $"{name} (synthétique)" : name;
    }

    private string BuildProperties()
    {
        var def = gem.Definition;
        var text = new StringBuilder();

        void Line(string label, string value) => text.Append($"<color={GoldTag}><b>{label}</b></color>  {value}\n");

        var refractiveKey = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
        if (gem.HasMeasured(refractiveKey)) Line("Indice de réfraction", $"{gem.GetMeasuredValue(refractiveKey):F3}");
        else Line("Indice de réfraction", "OTL (hors limite du prisme, > 1.81)");
        Line("Biréfringence", OpticalDescriptions.BirefringenceText(def));
        Line("Densité", $"{gem.GetMeasuredValue(ExaminationTool.MeasurementType.Density.ToString()):F2}");
        Line("Caractère optique", OpticalDescriptions.PolariscopeText(def));
        Line("Figure d'interférence", OpticalDescriptions.ConoscopeText(def));
        Line("Pléochroïsme", def.pleochroic ? def.pleochroicDescription : "Aucun");
        Line("Fluorescence UV longues", def.fluorescenceLongWave);
        Line("Fluorescence UV courtes", def.fluorescenceShortWave);
        Line("Spectre d'absorption", SpectrumLibrary.Description(def));
        Line("Filtre Chelsea", ChelseaLibrary.Reaction(def));
        Line("Aimant", def.magnetism);
        Line("Clarté", $"{gem.ClarityGrade} ({gem.ClarityDescription})");
        Line("Inclusions", gem.InclusionDescription);
        Line("Loupe", LoupeFeatures.InclusionSummary(gem));
        return text.ToString();
    }

    // ---- Widgets ----

    private static Text Label(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color,
        Vector2 position, Vector2 size, bool bold = false)
    {
        var text = RuntimeUIKit.MakeText(name, parent, content, fontSize, alignment, color, position, size);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.Bold;
        return text;
    }

    private static void Diagram(string name, Transform parent, Vector2 position, Sprite sprite)
    {
        RuntimeUIKit.MakeBox(name + "Border", parent, position, new Vector2(172f, 172f), Gold, false);
        RuntimeUIKit.MakeBox(name + "Back", parent, position + new Vector2(1f, -1f), new Vector2(170f, 170f), Color.white, false);

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position + new Vector2(4f, -4f), new Vector2(164f, 164f));
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}
