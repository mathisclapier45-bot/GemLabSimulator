using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The diamond classification report of the grading office, drawn as a sheet of paper in the spirit of the GIA reports: the stone, its
// proportions and the plot of its inclusions, the grades of color, clarity and cut, and the two scales with the grade of the stone
// marked on them. Opened from the grading desk and from the stone sheet. Built at runtime, so no scene regeneration is needed.
public class DiamondCertificateUI : MonoBehaviour
{
    public static DiamondCertificateUI Instance { get; private set; }

    private const float PageWidth = 1120f;
    private const float PageHeight = 680f;

    private static readonly Color PaperColor = new Color(0.97f, 0.97f, 0.95f, 1f);
    private static readonly Color Ink = new Color(0.13f, 0.14f, 0.18f, 1f);
    private static readonly Color Blue = new Color(0.20f, 0.32f, 0.52f, 1f);
    private static readonly Color Quiet = new Color(0.45f, 0.46f, 0.52f, 1f);

    private GameObject page;
    private GameObject sheet;
    private GemInstance gem;

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
        if (canvas != null && canvas.GetComponent<DiamondCertificateUI>() == null) canvas.AddComponent<DiamondCertificateUI>();
    }

    private void Awake()
    {
        Instance = this;
        page = new GameObject("DiamondCertificatePage", typeof(RectTransform), typeof(Image));
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
        if (target == null || target.Certificate == null) return;
        gem = target;
        Sfx.Play(Sfx.Id.Paper);
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

    private static readonly Color Red = new Color(0.85f, 0.15f, 0.15f, 1f);

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

        var parent = sheet.transform;
        var certificate = gem.Certificate;
        var grade = DiamondGradeModel.Official(gem);

        // Frame.
        RuntimeUIKit.MakeBox("FrameTop", parent, new Vector2(0f, 0f), new Vector2(PageWidth, 8f), Blue, false);
        RuntimeUIKit.MakeBox("FrameBottom", parent, new Vector2(0f, -PageHeight + 8f), new Vector2(PageWidth, 8f), Blue, false);

        // Header.
        var emblem = new GameObject("Emblem", typeof(RectTransform), typeof(Image));
        emblem.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(emblem.GetComponent<RectTransform>(), new Vector2(24f, -18f), new Vector2(56f, 56f));
        var emblemImage = emblem.GetComponent<Image>();
        emblemImage.sprite = GemIconFactory.GetSolid("Rond", "Brillant", new Color(0.30f, 0.50f, 0.80f), new Color(0.75f, 0.88f, 1f));
        emblemImage.preserveAspect = true;
        emblemImage.raycastTarget = false;

        Label("Office", parent, "Bureau de grading", 34, TextAnchor.UpperLeft, Ink, new Vector2(92f, -12f), new Vector2(420f, 42f), true);
        Label("Building", parent, "Immeuble des Diamantaires — Rapport de classification du diamant taillé", 14, TextAnchor.UpperLeft, Blue, new Vector2(95f, -56f), new Vector2(560f, 22f));
        Label("Number", parent, $"Rapport N° {certificate.number}", 18, TextAnchor.UpperRight, Ink, new Vector2(PageWidth - 450f, -18f), new Vector2(390f, 26f), true);
        Label("Date", parent, $"Émis le jour {certificate.issueDay}  ·  {certificate.issuer}", 12, TextAnchor.UpperRight, Ink, new Vector2(PageWidth - 550f, -46f), new Vector2(490f, 20f));

        ColumnIdentity(parent, grade);
        ColumnPlotAndProportions(parent, grade);
        ColumnResults(parent, grade);

        Label("Disclaimer", parent,
            "Les résultats de ce rapport ne concernent que le diamant décrit, d'après les instruments et les pierres de référence du bureau de grading à la date d'émission. " +
            "Il ne constitue ni une garantie ni une évaluation.",
            10, TextAnchor.UpperLeft, Quiet, new Vector2(24f, -632f), new Vector2(690f, 30f));

        var close = RuntimeUIKit.MakeButton("Close", parent, "X", 14, new Color(0.30f, 0.34f, 0.44f, 1f),
            new Vector2(PageWidth - 44f, -12f), new Vector2(32f, 26f));
        close.onClick.AddListener(Close);
    }

    private void Section(Transform parent, string title, Vector2 position, float width)
    {
        var bar = RuntimeUIKit.MakeBox("Section_" + title, parent, position, new Vector2(width, 22f), Blue, false);
        Label("SectionTitle", bar.transform, title, 13, TextAnchor.MiddleLeft, Color.white, new Vector2(8f, 0f), new Vector2(width - 16f, 22f), true);
    }

    private void Line(Transform parent, string label, string value, Vector2 position, float width, bool bold = false)
    {
        Label("L_" + label, parent, label, 12, TextAnchor.UpperLeft, Quiet, position, new Vector2(width * 0.55f, 18f));
        Label("V_" + label, parent, value, 12, TextAnchor.UpperRight, Ink, position + new Vector2(width * 0.45f, 0f), new Vector2(width * 0.55f, 18f), bold);
    }

    // Left column: the stone, its measurements and the two scales.
    private void ColumnIdentity(Transform parent, DiamondGrade grade)
    {
        const float x = 24f;
        const float w = 300f;

        Section(parent, "Identification", new Vector2(x, -96f), w);
        RuntimeUIKit.MakeBox("PhotoBack", parent, new Vector2(x, -124f), new Vector2(120f, 100f), new Color(0.90f, 0.92f, 0.96f, 1f), false);
        var photo = new GameObject("Photo", typeof(RectTransform), typeof(Image));
        photo.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(photo.GetComponent<RectTransform>(), new Vector2(x + 10f, -128f), new Vector2(100f, 92f));
        var photoImage = photo.GetComponent<Image>();
        photoImage.sprite = gem.DisplaySprite;
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;
        Label("Identity", parent, $"Pierre n° {gem.SpecimenId}\n{gem.ShapeName} — {gem.CutStyleName}\nDiamant {grade.Growth.ToLowerInvariant()}", 12, TextAnchor.UpperLeft, Ink,
            new Vector2(x + 132f, -126f), new Vector2(170f, 70f));

        Section(parent, "Mesures", new Vector2(x, -236f), w);
        var y = -262f;
        var average = (grade.DiameterMax + grade.DiameterMin) * 0.5f;
        Line(parent, "Diamètre minimal", $"{grade.DiameterMin:0.00} mm", new Vector2(x, y), w); y -= 18f;
        Line(parent, "Diamètre maximal", $"{grade.DiameterMax:0.00} mm", new Vector2(x, y), w); y -= 18f;
        Line(parent, "Diamètre moyen", $"{average:0.00} mm", new Vector2(x, y), w); y -= 18f;
        Line(parent, "Rapport", $"{grade.Ratio:0.00}", new Vector2(x, y), w); y -= 18f;
        Line(parent, "Profondeur totale", $"{grade.DepthMm:0.00} mm", new Vector2(x, y), w); y -= 18f;
        Line(parent, "Poids", $"{grade.Carat:0.00} ct", new Vector2(x, y), w, true); y -= 18f;
        Line(parent, "Poids calculé", $"{grade.CalculatedCarat:0.00} ct", new Vector2(x, y), w);

        Section(parent, "Échelle de pureté", new Vector2(x, -400f), w);
        ClarityScale(parent, new Vector2(x, -428f), grade);
        Section(parent, "Échelle des couleurs", new Vector2(x, -468f), w);
        ColorScale(parent, new Vector2(x, -496f), grade);

        var seal = new GameObject("Seal", typeof(RectTransform), typeof(Image));
        seal.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(seal.GetComponent<RectTransform>(), new Vector2(x, -562f), new Vector2(34f, 34f));
        var sealImage = seal.GetComponent<Image>();
        sealImage.sprite = RuntimeUIKit.Validated;
        sealImage.preserveAspect = true;
        sealImage.raycastTarget = false;
        Label("Signed", parent, "Classification établie par le bureau de grading\nde l'Immeuble des Diamantaires", 12, TextAnchor.UpperLeft, Ink,
            new Vector2(x + 44f, -564f), new Vector2(300f, 34f));
    }

    // Middle column: the plot of the inclusions and the proportions.
    private void ColumnPlotAndProportions(Transform parent, DiamondGrade grade)
    {
        const float x = 340f;
        const float w = 380f;

        Section(parent, "Plan des inclusions (plot)", new Vector2(x, -96f), w);
        var origin = new Vector2(x, -124f);
        Diagram("TopView", parent, origin, GemDiagrams.TopView(gem), 190f);
        const float radius = 70f;
        var legend = new System.Text.StringBuilder();
        for (var i = 0; i < grade.Plot.Count; i++)
        {
            var mark = grade.Plot[i];
            var center = origin + new Vector2(95f + mark.X * radius, -95f + mark.Y * radius);
            var symbol = new GameObject("Symbol" + i, typeof(RectTransform), typeof(Image));
            symbol.transform.SetParent(parent, false);
            RuntimeUIKit.SetTopLeft(symbol.GetComponent<RectTransform>(), center + new Vector2(-9f, 9f), new Vector2(18f, 18f));
            var image = symbol.GetComponent<Image>();
            image.sprite = PlotSymbols.Get(mark.Kind);
            image.color = PlotSymbols.ColorOf(mark.Kind);
            image.raycastTarget = false;
            Label("N" + i, parent, (i + 1).ToString(), 9, TextAnchor.UpperLeft, PlotSymbols.ColorOf(mark.Kind), center + new Vector2(8f, 10f), new Vector2(16f, 12f), true);
            legend.AppendLine($"{i + 1}  {DiamondGradeModel.KindName(mark.Kind)}  ({(DiamondGradeModel.IsInternal(mark.Kind) ? "interne" : "externe")})");
        }
        if (grade.Plot.Count == 0) legend.AppendLine("Aucune inclusion ni défaut de surface.");
        Label("Legend", parent, legend.ToString(), 10, TextAnchor.UpperLeft, Ink, new Vector2(x + 200f, -126f), new Vector2(180f, 150f));
        Label("PlotNote", parent, "Rouge : inclusion interne.\nVert : défaut de surface.", 10, TextAnchor.UpperLeft, Quiet, new Vector2(x + 200f, -276f), new Vector2(180f, 30f));

        Section(parent, "Proportions", new Vector2(x, -326f), w);
        var profile = new GameObject("Profile", typeof(RectTransform), typeof(RawImage));
        profile.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(profile.GetComponent<RectTransform>(), new Vector2(x, -354f), new Vector2(190f, 170f));
        var raw = profile.GetComponent<RawImage>();
        raw.texture = DiamondProfileDrawing.Draw(grade, 190, 170, 1.5f, 95f, 22f, false);
        raw.raycastTarget = false;

        var y = -354f;
        float tx = x + 200f;
        const float tw = 180f;
        Line(parent, "Table", $"{grade.TablePercent:0.0} %", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Hauteur de couronne", $"{grade.CrownPercent:0.0} %", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Ceinture", $"{DiamondGradeModel.GirdleNames[grade.GirdleClass]}", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Profondeur de pavillon", $"{grade.PavilionPercent:0.0} %", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Colette", $"{DiamondGradeModel.CuletNames[grade.CuletClass]}", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Profondeur totale", $"{grade.DepthPercent:0.0} %", new Vector2(tx, y), tw, true); y -= 18f;
        Line(parent, "Angle de couronne β", $"{grade.CrownAngle:0.0}°", new Vector2(tx, y), tw); y -= 18f;
        Line(parent, "Angle de pavillon α", $"{grade.PavilionAngle:0.0}°", new Vector2(tx, y), tw); y -= 22f;
        Label("Check", parent, $"Profondeur = couronne + ceinture + pavillon\n{grade.CrownPercent:0.0} + {grade.GirdlePercent:0.0} + {grade.PavilionPercent:0.0} = {grade.DepthPercent:0.0} %",
            10, TextAnchor.UpperLeft, Quiet, new Vector2(tx, y), new Vector2(tw, 30f));
        Label("ProportionGrade", parent, $"Qualité des proportions : {grade.Proportions}", 15, TextAnchor.UpperLeft, Blue, new Vector2(x, -532f), new Vector2(w, 24f), true);
    }

    // Right column: the results and the deviations found.
    private void ColumnResults(Transform parent, DiamondGrade grade)
    {
        const float x = 740f;
        const float w = 360f;

        Section(parent, "Résultats de la classification", new Vector2(x, -96f), w);
        var y = -126f;
        void Result(string label, string value, bool big)
        {
            Label("RL_" + label, parent, label, 13, TextAnchor.UpperLeft, Quiet, new Vector2(x + 4f, y), new Vector2(130f, 20f), true);
            Label("RV_" + label, parent, value, big ? 17 : 14, TextAnchor.UpperLeft, Ink, new Vector2(x + 140f, y - (big ? 2f : 0f)), new Vector2(220f, 24f), big);
            y -= big ? 26f : 22f;
        }
        Result("Couleur", grade.Fancy ? grade.ColorText : $"{grade.ColorGrade}  ({grade.ColorText})", true);
        Result("Pureté", grade.ClarityGrade, true);
        Result("Taille", grade.Cut, true);
        Result("Fluorescence", $"{DiamondGradeModel.FluorescenceEnglish[grade.FluorescenceIndex]}  ({grade.Fluorescence.ToLowerInvariant()})", false);
        Result("Proportions", grade.Proportions, false);
        Result("Poli", grade.Polish, false);
        Result("Symétrie", grade.Symmetry, false);

        LevelGrid(parent, $"Symétrie — {grade.Symmetry}", DiamondGradeModel.SymmetryItems, grade.SymmetryLevels, new Vector2(x, -300f), w);
        LevelGrid(parent, $"Poli — {grade.Polish}", DiamondGradeModel.PolishItems, grade.PolishLevels, new Vector2(x, -530f), w);
    }

    // A grid of characteristics, one row each, with a mark in the column of the level found.
    private void LevelGrid(Transform parent, string title, string[] items, int[] levels, Vector2 origin, float width)
    {
        Section(parent, title, origin, width);
        const float labelWidth = 192f;
        var cell = (width - labelWidth) / 4f;
        for (var b = 0; b < 4; b++)
            Label("H" + b, parent, DiamondGradeModel.LevelNames[b], 9, TextAnchor.UpperCenter, Quiet, origin + new Vector2(labelWidth + b * cell, -24f), new Vector2(cell, 12f));
        for (var i = 0; i < items.Length; i++)
        {
            var y = origin.y - 38f - i * 13f;
            Label("I" + i, parent, items[i], 9, TextAnchor.UpperLeft, Ink, new Vector2(origin.x + 2f, y), new Vector2(labelWidth, 12f));
            for (var b = 0; b < 4; b++)
            {
                var selected = levels[i] == b;
                RuntimeUIKit.MakeBox("C" + i + "_" + b, parent, new Vector2(origin.x + labelWidth + b * cell + 3f, y), new Vector2(cell - 6f, 10f),
                    selected ? (b == 0 ? Blue : Red) : new Color(0.87f, 0.89f, 0.94f, 1f), false).raycastTarget = false;
            }
        }
    }

    private void ColorScale(Transform parent, Vector2 origin, DiamondGrade grade)
    {
        const float box = 12f;
        for (var i = 0; i < DiamondGrading.ColorScale.Length; i++)
        {
            DiamondGradeModel.MasterColors(i, out var body, out _);
            var selected = !grade.Fancy && i == grade.ColorIndex;
            // The stone's letter stands taller, inside a red frame.
            if (selected)
                RuntimeUIKit.MakeBox("Mark" + i, parent, origin + new Vector2(i * (box + 1f) - 1f, 1f), new Vector2(box + 2f, 36f), new Color(0.85f, 0.15f, 0.15f, 1f), false).raycastTarget = false;
            var cell = RuntimeUIKit.MakeBox("C" + i, parent, origin + new Vector2(i * (box + 1f), selected ? 0f : -6f), new Vector2(box, selected ? 34f : 24f), body, false);
            cell.raycastTarget = false;
        }
        Label("ScaleD", parent, "D", 10, TextAnchor.UpperLeft, Quiet, origin + new Vector2(0f, -38f), new Vector2(20f, 14f));
        Label("ScaleZ", parent, "Z", 10, TextAnchor.UpperRight, Quiet, origin + new Vector2(DiamondGrading.ColorScale.Length * (box + 1f) - 20f, -38f), new Vector2(20f, 14f));
        if (grade.Fancy) Label("FancyNote", parent, "Couleur fantaisie : hors échelle", 10, TextAnchor.UpperCenter, Quiet, origin + new Vector2(60f, -38f), new Vector2(200f, 14f));
    }

    private void ClarityScale(Transform parent, Vector2 origin, DiamondGrade grade)
    {
        const float box = 26f;
        for (var i = 0; i < DiamondGrading.ClarityScale.Length; i++)
        {
            var selected = i == grade.ClarityIndex;
            var cell = RuntimeUIKit.MakeBox("K" + i, parent, origin + new Vector2(i * (box + 1f), 0f), new Vector2(box, 24f),
                selected ? new Color(0.85f, 0.15f, 0.15f, 1f) : new Color(0.84f, 0.86f, 0.92f, 1f), false);
            cell.raycastTarget = false;
            Label("KL" + i, cell.transform, DiamondGrading.ClarityScale[i], 9, TextAnchor.MiddleCenter, selected ? Color.white : Ink, Vector2.zero, new Vector2(box, 24f), selected);
        }
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

    private static void Diagram(string name, Transform parent, Vector2 position, Sprite sprite, float size = 172f)
    {
        RuntimeUIKit.MakeBox(name + "Border", parent, position, new Vector2(size, size), Blue, false);
        RuntimeUIKit.MakeBox(name + "Back", parent, position + new Vector2(1f, -1f), new Vector2(size - 2f, size - 2f), Color.white, false);

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position + new Vector2(4f, -4f), new Vector2(size - 8f, size - 8f));
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}
