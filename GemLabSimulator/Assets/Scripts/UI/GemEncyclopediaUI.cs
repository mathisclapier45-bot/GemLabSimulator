using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The encyclopedia of the gems, opened from the main menu (and from the pause menu): every species of the game can be looked up from a search box or a list, shown in
// any shape and cut style it allows (or, for a species that is never cut, in each form of its rough crystal), and its sheet gives all the colours it can have, how clean
// its stones are, the treatments, the deposits and its properties. Built at runtime; needs no scene.
public class GemEncyclopediaUI : MonoBehaviour
{
    public static GemEncyclopediaUI Instance { get; private set; }

    private const float W = 1260f;
    private const float H = 724f;
    private const float StoneBox = 300f;
    private const float Swatch = 38f;
    private const float SwatchSpacing = 41f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.82f, 0.78f, 0.70f);
    private static readonly Color Muted = new Color(0.62f, 0.62f, 0.68f);
    private static readonly Color RowColor = new Color(0.17f, 0.15f, 0.14f, 1f);
    private static readonly Color RowSelected = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color ButtonOff = new Color(0.30f, 0.26f, 0.23f, 1f);
    private static readonly Color ButtonOn = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color ButtonDisabled = new Color(0.20f, 0.19f, 0.19f, 1f);
    private static readonly Quaternion StartOrientation = Quaternion.Euler(-38f, 0f, 0f);

    private struct ListItem
    {
        public GemDefinition Def;
        public RectTransform Rect;
        public Image Back;
        public string Key;
    }

    private GameObject panel;
    private InputField search;
    private RectTransform listContent;
    private readonly List<ListItem> items = new List<ListItem>();

    private Text nameText;
    private Text subText;
    private Image stoneImage;
    private GemView3D view;
    private RectTransform shapeBox;
    private RectTransform styleBox;
    private Text shapeLabel;
    private Text styleLabel;

    private readonly Button[] tabButtons = new Button[3];
    private RectTransform tabViewport;
    private RectTransform tabContent;
    private Text colorInfo;

    private GemDefinition def;
    private string shape;
    private string style;
    private string habitId;
    private int tab;
    private GemColorValue selectedColor;
    private float selectedShare;
    private bool hasSelectedColor;
    private bool showSynthetic;                  // the colours of a synthetic stone of the species (every colour, for the materials grown in all of them)
    private Button syntheticButton;
    private string selectedNote;                 // what the info line says about the chosen colour, when it does not come from the palette (the diamonds)
    private Quaternion orientation = StartOrientation;
    private float lastTouch = float.MinValue;

    private bool lockedInput;

    public bool IsOpen => panel != null && panel.activeSelf;

    // Opens the encyclopedia over the canvas that holds `host` (the main menu, or a game scene).
    public static void Open(Transform host)
    {
        if (Instance == null)
        {
            var canvas = host != null ? host.GetComponentInParent<Canvas>() : null;
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            canvas.rootCanvas.gameObject.AddComponent<GemEncyclopediaUI>();
        }
        Instance?.Show();
    }

    private void Awake()
    {
        Instance = this;
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (lockedInput) PlayerController.InputLocked = false;
    }

    private void Show()
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (!PlayerController.InputLocked) { PlayerController.InputLocked = true; lockedInput = true; }      // no walking, no hotkeys behind the encyclopedia
        if (def == null)
        {
            var database = GemDatabase.Instance;
            var first = database != null ? database.allGems.Where(g => g != null).OrderBy(g => TextEntry.Normalize(g.displayName)).FirstOrDefault() : null;
            if (first != null) Select(first);
        }
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (lockedInput) { PlayerController.InputLocked = false; lockedInput = false; }
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) && !ConfirmDialog.ConsumesEscape) { EscapeGate.Consume(); Close(); return; }
        if (def == null || view == null) return;

        // The stone turns by itself, until the player takes hold of it.
        if (Time.unscaledTime - lastTouch > 1.5f) orientation = orientation * Quaternion.AngleAxis(Time.unscaledDeltaTime * 30f, Vector3.forward);
        DrawStone();
    }

    // ---- The stone ------------------------------------------------------------------------------------------------------------

    private bool IsRoughOnly => def != null && def.roughOnly;

    private bool IsDiamond => def != null && def.name.StartsWith("Diamant");

    private Color SelectedBody => hasSelectedColor ? selectedColor.ToColor() : (def != null && def.iconColor.a > 0f ? def.iconColor : new Color(0.75f, 0.75f, 0.8f));

    private Color SelectedFacet => hasSelectedColor ? GemColorSystem.FacetColor(selectedColor) : (def != null && def.iconColor.a > 0f ? def.iconFacetColor : new Color(0.92f, 0.92f, 0.96f));

    private void DrawStone()
    {
        if (def == null || view == null) return;
        GemMesh mesh;
        Vector3 half;
        GemLook look;
        if (IsRoughOnly || !string.IsNullOrEmpty(habitId))
        {
            mesh = !string.IsNullOrEmpty(habitId) ? GemMesh.GetHabit(habitId) : null;
            if (mesh == null) mesh = GemMesh.GetRough((int)StoneOptics.StableHash(def.name));
            half = Vector3.one;
            look = GemLook.ForCrystal(def);
        }
        else
        {
            mesh = GemMesh.Get(shape, style);
            var aspect = GemMesh.Aspect(shape);
            half = new Vector3(aspect.x, aspect.y, 0.6f);
            look = GemLook.For(def);
        }
        var body = SelectedBody;
        var facet = SelectedFacet;
        GemLook.ApplyPleochroism(def, orientation, ref body, ref facet);
        view.Draw(mesh, half, orientation, body, facet, def.Diaphaneity, 0, look, false);
    }

    private Sprite SwatchSprite(GemColorValue color)
    {
        var body = color.ToColor();
        var facet = GemColorSystem.FacetColor(color);
        if (IsRoughOnly || !string.IsNullOrEmpty(habitId))
            return !string.IsNullOrEmpty(habitId) ? GemIconFactory.GetHabit(def, habitId, body, facet) : GemIconFactory.GetRough(def, body);
        return GemIconFactory.Get(def, shape, style, body, facet);
    }

    // ---- Selecting ----------------------------------------------------------------------------------------------------------

    private void Select(GemDefinition target)
    {
        def = target;
        orientation = StartOrientation;
        lastTouch = float.MinValue;

        habitId = null;
        shape = !string.IsNullOrEmpty(def.shape) ? def.shape : "Ovale";
        style = !string.IsNullOrEmpty(def.cutStyle) ? def.cutStyle : "Cabochon";
        if (!LapidaryCatalog.IsStyleAllowed(def, shape, style)) style = "Cabochon";
        if (IsRoughOnly)
        {
            var habits = CrystalHabits.For(def);
            habitId = habits.Count > 0 ? habits[0].Habit.Id : null;
        }

        // The colour shown first: the most common one of the species (a diamond starts at D, the colourless end of its scale).
        selectedNote = null;
        showSynthetic = false;
        var cells = IsDiamond ? new List<GemColorSystem.PaletteCell>() : GemColorSystem.PaletteCells(def, 12);
        if (IsDiamond)
        {
            selectedColor = LetterColor(0);
            selectedNote = "<b>D</b>  ·  " + DiamondGrading.ColorDescription(0);
            selectedShare = 0f;
            hasSelectedColor = true;
        }
        else hasSelectedColor = cells.Count > 0;
        if (!IsDiamond && hasSelectedColor)
        {
            var best = cells[0];
            foreach (var cell in cells) if (cell.Share > best.Share) best = cell;
            selectedColor = best.Color;
            selectedShare = best.Share;
        }

        nameText.text = def.displayName;
        for (var i = 0; i < items.Count; i++) items[i].Back.color = items[i].Def == def ? RowSelected : RowColor;
        RefreshSelectors();
        SetTab(tab);
    }

    private void RefreshSelectors()
    {
        subText.text = IsRoughOnly ? "Jamais taillée : en cristal ou en fragment" : shape + "  ·  " + style;
        ClearChildren(shapeBox);
        ClearChildren(styleBox);

        if (IsRoughOnly)
        {
            shapeLabel.text = "Formes du brut";
            styleLabel.text = "";
            var habits = CrystalHabits.For(def);
            if (habits.Count == 0)
            {
                Txt(shapeBox, "NoHabit", "Un fragment sans forme précise.", 12, TextAnchor.UpperLeft, Muted, new Vector2(2f, -4f), new Vector2(320f, 20f));
                habitId = null;
                return;
            }
            for (var i = 0; i < habits.Count; i++)
            {
                var id = habits[i].Habit.Id;
                var selected = id == habitId;
                var button = RuntimeUIKit.MakeButton("Habit", shapeBox, habits[i].Habit.Short, 11, selected ? ButtonOn : ButtonOff, new Vector2((i % 3) * 108f, -(i / 3) * 28f), new Vector2(104f, 24f));
                button.onClick.AddListener(() => { habitId = id; RefreshSelectors(); RefreshColors(); });
            }
            return;
        }

        shapeLabel.text = "Forme";
        styleLabel.text = "Style de taille";
        for (var i = 0; i < LapidaryCatalog.Shapes.Length; i++)
        {
            var name = LapidaryCatalog.Shapes[i];
            var button = RuntimeUIKit.MakeButton("Shape", shapeBox, name == "Rectangle à pans coupés" ? "Émeraude" : name, 11, name == shape ? ButtonOn : ButtonOff,
                new Vector2((i % 3) * 108f, -(i / 3) * 27f), new Vector2(104f, 24f));
            button.onClick.AddListener(() =>
            {
                shape = name;
                if (!LapidaryCatalog.IsStyleAllowed(def, shape, style)) style = "Cabochon";
                RefreshSelectors();
                RefreshColors();
            });
        }
        for (var i = 0; i < LapidaryCatalog.Styles.Length; i++)
        {
            var name = LapidaryCatalog.Styles[i];
            var allowed = LapidaryCatalog.IsStyleAllowed(def, shape, name);
            var button = RuntimeUIKit.MakeButton("Style", styleBox, name, 11, !allowed ? ButtonDisabled : name == style ? ButtonOn : ButtonOff,
                new Vector2((i % 4) * 81f, -(i / 4) * 27f), new Vector2(78f, 24f));
            button.interactable = allowed;
            button.onClick.AddListener(() =>
            {
                style = name;
                RefreshSelectors();
                RefreshColors();
            });
        }
    }

    // ---- The tabs -----------------------------------------------------------------------------------------------------------------

    private void SetTab(int index)
    {
        tab = index;
        for (var i = 0; i < tabButtons.Length; i++)
            tabButtons[i].GetComponent<Image>().color = i == tab ? ButtonOn : ButtonOff;
        ClearChildren(tabContent);
        tabContent.anchoredPosition = Vector2.zero;
        colorInfo.gameObject.SetActive(tab == 0);
        if (syntheticButton != null)
        {
            var canBeSynthetic = def != null && !IsDiamond && !def.forcedSynthetic && GemColorSystem.GrownInAnyColor(def, true);
            syntheticButton.gameObject.SetActive(tab == 0 && canBeSynthetic);
            syntheticButton.GetComponentInChildren<Text>().text = showSynthetic ? "Synthétique" : "Naturelle";
        }
        if (tab == 0) RefreshColors();
        else if (tab == 1) ShowText(CleanlinessText());
        else ShowText(SheetText());
    }

    private void ShowText(string text)
    {
        var label = Txt(tabContent, "Body", text, 13, TextAnchor.UpperLeft, Ink, new Vector2(6f, -4f), new Vector2(tabViewport.rect.width - 20f, 100f));
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.rectTransform.sizeDelta = new Vector2(tabViewport.rect.width - 20f, Mathf.Max(60f, label.preferredHeight + 8f));
        tabContent.sizeDelta = new Vector2(0f, label.rectTransform.sizeDelta.y + 12f);
    }

    // Every colour of the species on the grid of the laboratory: one row for each tone, one column for each hue of its window.
    private void RefreshColors()
    {
        if (tab != 0 || def == null) return;
        ClearChildren(tabContent);
        if (IsDiamond) { RefreshDiamondColors(); return; }

        var cells = GemColorSystem.PaletteCells(def, 12, showSynthetic);
        var anyColor = GemColorSystem.GrownInAnyColor(def, showSynthetic);
        Txt(tabContent, "Count", cells.Count + (anyColor ? " couleurs possibles : une pierre de synthèse de cette matière peut avoir toutes les couleurs." : " couleurs possibles, du clair au sombre.") + " Cliquez sur une couleur pour voir la pierre dans cette couleur.", 12, TextAnchor.UpperLeft, Muted,
            new Vector2(6f, -2f), new Vector2(tabViewport.rect.width - 20f, 34f));

        var y = -40f;
        for (var row = 0; row < GemColorSystem.RowCount; row++)
        {
            var inRow = cells.Where(c => c.Row == row).OrderBy(c => c.Step).ToList();
            if (inRow.Count == 0) continue;
            Txt(tabContent, "RowLabel", GemColorSystem.RowLabel(row), 11, TextAnchor.MiddleLeft, Gold, new Vector2(6f, y - 8f), new Vector2(64f, 22f));
            foreach (var cell in inRow)
            {
                var color = cell.Color;
                var share = cell.Share;
                var box = RuntimeUIKit.MakeBox("Swatch", tabContent, new Vector2(72f + cell.Step * SwatchSpacing, y), new Vector2(Swatch, Swatch),
                    hasSelectedColor && Mathf.Approximately(color.Hue, selectedColor.Hue) && Mathf.Approximately(color.Saturation, selectedColor.Saturation) && Mathf.Approximately(color.Brightness, selectedColor.Brightness)
                        ? Gold : new Color(0.10f, 0.09f, 0.09f, 1f), false);
                var sprite = SwatchSprite(color);
                var icon = RuntimeUIKit.MakeBox("Icon", box.transform, new Vector2(2f, -2f), new Vector2(Swatch - 4f, Swatch - 4f), Color.white, false);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var button = box.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    selectedColor = color;
                    selectedShare = share;
                    hasSelectedColor = true;
                    RefreshColors();
                });
            }
            y -= Swatch + 6f;
        }
        tabContent.sizeDelta = new Vector2(0f, -y + 10f);

        if (hasSelectedColor)
        {
            var grade = GemColorSystem.Describe(selectedColor, false);
            colorInfo.text = "<b>" + grade.Name + "</b>  ·  teinte " + Mathf.RoundToInt(selectedColor.Hue) + "°, saturation " + Mathf.RoundToInt(selectedColor.Saturation * 100f) + " %, brillance " +
                             Mathf.RoundToInt(selectedColor.Brightness * 100f) + " %\n<color=#9AA3B5>Environ " + (selectedShare * 100f).ToString("0.0") + " % des pierres de cette espèce ont cette couleur.</color>";
        }
        else colorInfo.text = "Cette espèce n'a pas de couleur propre.";
    }

    // The colour of a diamond of the D to Z scale: from colourless to a light yellow (the same as a stone of the game).
    private static GemColorValue LetterColor(int index)
    {
        var t = Mathf.Clamp01(index / 22f);
        return new GemColorValue(Mathf.Lerp(215f, 52f, Mathf.Clamp01(index / 5f)), Mathf.Lerp(0.02f, 0.62f, t * t), Mathf.Lerp(0.96f, 0.90f, t));
    }

    private bool SameAsSelected(GemColorValue color)
    {
        return hasSelectedColor && Mathf.Abs(Mathf.DeltaAngle(color.Hue, selectedColor.Hue)) < 0.5f && Mathf.Abs(color.Saturation - selectedColor.Saturation) < 0.004f
               && Mathf.Abs(color.Brightness - selectedColor.Brightness) < 0.004f;
    }

    // A swatch of the grids of the diamond: a small stone of that colour; a click shows the big stone in it.
    private void DiamondSwatch(Vector2 position, float size, GemColorValue color, string note)
    {
        var box = RuntimeUIKit.MakeBox("Swatch", tabContent, position, new Vector2(size, size), SameAsSelected(color) ? Gold : new Color(0.10f, 0.09f, 0.09f, 1f), false);
        var icon = RuntimeUIKit.MakeBox("Icon", box.transform, new Vector2(2f, -2f), new Vector2(size - 4f, size - 4f), Color.white, false);
        icon.sprite = SwatchSprite(color);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        box.gameObject.AddComponent<Button>().onClick.AddListener(() =>
        {
            selectedColor = color;
            selectedNote = note;
            hasSelectedColor = true;
            RefreshColors();
        });
    }

    // The colours of a diamond: the scale D to Z, then the coloured diamonds of the GIA chart (27 hues, nine grades, browns and grays).
    private void RefreshDiamondColors()
    {
        var width = tabViewport.rect.width - 20f;
        Txt(tabContent, "Intro", "Un diamant est incolore à jaunâtre sur l'échelle D à Z ; quelques pour cent sont de couleur, d'après les 27 teintes et les neuf degrés de la grille GIA. Cliquez sur une couleur pour voir la pierre dans cette couleur.",
            12, TextAnchor.UpperLeft, Muted, new Vector2(6f, -2f), new Vector2(width, 48f));

        var y = -56f;
        Txt(tabContent, "ScaleTitle", "L'échelle D à Z", 13, TextAnchor.UpperLeft, Gold, new Vector2(6f, y), new Vector2(width, 18f)).fontStyle = FontStyle.Bold;
        y -= 24f;
        const int perRow = 12;
        for (var i = 0; i < DiamondGrading.ColorScale.Length; i++)
        {
            var column = i % perRow;
            var line = i / perRow;
            var x = 6f + column * SwatchSpacing;
            var swatchY = y - line * (Swatch + 16f);
            var index = i;
            DiamondSwatch(new Vector2(x, swatchY), Swatch, LetterColor(i), "<b>" + DiamondGrading.Color(index) + "</b>  ·  " + DiamondGrading.ColorDescription(index));
            Txt(tabContent, "Letter", DiamondGrading.Color(i), 10, TextAnchor.UpperCenter, Ink, new Vector2(x, swatchY - Swatch - 1f), new Vector2(Swatch, 14f));
        }
        y -= Mathf.CeilToInt(DiamondGrading.ColorScale.Length / (float)perRow) * (Swatch + 16f) + 10f;

        Txt(tabContent, "FancyTitle", "Les diamants de couleur", 13, TextAnchor.UpperLeft, Gold, new Vector2(6f, y), new Vector2(width, 18f)).fontStyle = FontStyle.Bold;
        y -= 22f;

        // The header of the nine grades.
        var shortNames = new[] { "Faint", "V. Light", "Light", "F. Light", "Fancy", "F. Intense", "F. Vivid", "F. Deep", "F. Dark" };
        const float labelWidth = 138f;
        const float cell = 34f;
        for (var g = 0; g < shortNames.Length; g++)
            Txt(tabContent, "Grade", shortNames[g], 8, TextAnchor.LowerCenter, Muted, new Vector2(labelWidth + g * (cell + 3f), y), new Vector2(cell + 3f, 14f));
        y -= 18f;

        var hues = GiaDiamondColors.Hues;
        for (var h = 0; h < hues.Length; h++)
        {
            var hue = hues[h];
            Txt(tabContent, "Hue", hue.French, 10, TextAnchor.MiddleLeft, Ink, new Vector2(6f, y - 8f), new Vector2(labelWidth - 8f, 18f));
            for (var g = 0; g < shortNames.Length; g++)
            {
                if (!hue.Pale && g < (int)GiaGrade.FancyLight) continue;
                var grade = (GiaGrade)g;
                var body = GiaDiamondColors.Typical(hue, grade);
                Color.RGBToHSV(body, out var hh, out var ss, out var vv);
                var value = new GemColorValue(hh * 360f, ss, vv);
                var named = GiaDiamondColors.Describe(value);
                var share = GiaDiamondColors.PerThousand(h) * GiaDiamondColors.GradeShare(hue, grade) / 100f;
                DiamondSwatch(new Vector2(labelWidth + g * (cell + 3f), y), cell, value,
                    "<b>" + named.Name + "</b>  ·  <color=#9AA3B5>environ " + share.ToString("0.0#") + " diamant sur mille parmi les diamants de couleur</color>");
            }
            y -= cell + 4f;
        }

        // Browns and grays.
        FamilyRow("Brun", GiaDiamondColors.BrownPerThousand, new[] { GiaGrade.FancyLight, GiaGrade.Fancy, GiaGrade.FancyDeep, GiaGrade.FancyDark }, true, ref y, labelWidth, cell);
        FamilyRow("Gris", GiaDiamondColors.GrayPerThousand, new[] { GiaGrade.FancyLight, GiaGrade.Fancy, GiaGrade.FancyDark }, false, ref y, labelWidth, cell);
        tabContent.sizeDelta = new Vector2(0f, -y + 14f);

        colorInfo.text = (selectedNote ?? "") + (selectedNote == null ? "" : "\n") + "<color=#9AA3B5>Les degrés pâles sont bien plus courants que les vifs ; les rouges, violets, orange et roses sont rares à extrêmement rares.</color>";
    }

    private void FamilyRow(string label, float perThousand, GiaGrade[] grades, bool brown, ref float y, float labelWidth, float cell)
    {
        Txt(tabContent, "Hue", label, 10, TextAnchor.MiddleLeft, Ink, new Vector2(6f, y - 8f), new Vector2(labelWidth - 8f, 18f));
        foreach (var grade in grades)
        {
            var body = brown ? GiaDiamondColors.TypicalBrown(grade) : GiaDiamondColors.TypicalGray(grade);
            Color.RGBToHSV(body, out var hh, out var ss, out var vv);
            var value = new GemColorValue(hh * 360f, ss, vv);
            var named = GiaDiamondColors.Describe(value);
            DiamondSwatch(new Vector2(labelWidth + (int)grade * (cell + 3f), y), cell, value,
                "<b>" + named.Name + "</b>  ·  <color=#9AA3B5>environ " + perThousand.ToString("0") + " diamants sur mille parmi les diamants de couleur, tous degrés confondus</color>");
        }
        y -= cell + 4f;
    }

    // ---- The texts ---------------------------------------------------------------------------------------------------------------

    private static string Heading(string title) => "<color=#F2D9A6><b>" + title + "</b></color>";

    private static string Percent(float value) => Mathf.RoundToInt(value).ToString() + " %";

    private string CleanlinessText()
    {
        var sb = new StringBuilder();
        var profile = InclusionCatalog.For(def, null, false);
        var total = 0f;
        foreach (var w in profile.Clarity) total += w;

        sb.AppendLine(Heading("La pureté des pierres naturelles"));
        sb.AppendLine("<color=#9AA3B5>La part de chaque degré parmi les pierres de cette espèce, et ce qu'il fait au prix.</color>");
        for (var i = 0; i < GemGrading.ClarityGrades.Length; i++)
        {
            var grade = GemGrading.ClarityGrades[i];
            sb.AppendLine("<b>" + grade.Name + "</b> : " + Percent(total > 0f ? profile.Clarity[i] / total * 100f : 0f) + "  ·  prix " + StoneEconomy.ClarityRangeLabel(def, GemGrading.ClarityGrades.Length - 1 - i) + "\n    <color=#9AA3B5>" + grade.Description + "</color>");
        }

        sb.AppendLine();
        sb.AppendLine(Heading("Les inclusions"));
        sb.AppendLine(string.IsNullOrEmpty(def.inclusions) ? "Aucune particularité." : def.inclusions);

        if (def.forcedSynthetic)
            sb.AppendLine("\n<color=#9AA3B5>Cette espèce n'existe que synthétique.</color>");
        else
        {
            var synthetic = InclusionCatalog.For(def, null, true);
            var syntheticTotal = 0f;
            foreach (var w in synthetic.Clarity) syntheticTotal += w;
            sb.AppendLine();
            sb.AppendLine(Heading("Une pierre de synthèse"));
            sb.AppendLine("Plus propre : " + Percent(syntheticTotal > 0f ? (synthetic.Clarity[2] + synthetic.Clarity[3]) / syntheticTotal * 100f : 0f) + " de ses pierres sont Eye-clean ou Loupe clean, contre " +
                          Percent(total > 0f ? (profile.Clarity[2] + profile.Clarity[3]) / total * 100f : 0f) + " pour une naturelle.");
        }

        sb.AppendLine();
        sb.AppendLine(Heading("Les traitements"));
        var treatmentTotal = 0f;
        foreach (var t in GemGrading.TreatmentGrades) treatmentTotal += t.Weight;
        foreach (var t in GemGrading.TreatmentGrades)
            sb.AppendLine("<b>" + t.Name + "</b> : " + Percent(treatmentTotal > 0f ? t.Weight / treatmentTotal * 100f : 0f) + "  ·  prix ×" + t.Multiplier.ToString("0.0#") + "\n    <color=#9AA3B5>" + t.Description + "</color>");
        sb.AppendLine();
        sb.AppendLine(GrimoireUI.BuildTreatmentsText(def));
        return sb.ToString();
    }

    private string SheetText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Heading(def.displayName));
        sb.AppendLine(def.description);
        sb.AppendLine();
        sb.AppendLine(Heading("Propriétés"));
        sb.AppendLine("<b>Indice de réfraction</b> : " + def.refractiveIndex.ToString("0.000") + "   <b>Biréfringence</b> : " + def.birefringence.ToString("0.000"));
        sb.AppendLine("<b>Densité</b> : " + def.density.ToString("0.00") + "   <b>Dureté</b> : " + def.hardness.ToString("0.##"));
        sb.AppendLine("<b>Éclat</b> : " + def.luster + "   <b>Transparence</b> : " + def.transparency);
        sb.AppendLine("<b>Couleur</b> : " + def.color);
        sb.AppendLine("<b>Caractère optique</b> : " + def.opticalCharacter);
        if (!string.IsNullOrEmpty(def.pleochroicDescription)) sb.AppendLine("<b>Pléochroïsme</b> : " + def.pleochroicDescription);
        sb.AppendLine("<b>Fluorescence</b> : longue " + FluorescenceLibrary.Text(def, true) + " / courte " + FluorescenceLibrary.Text(def, false));
        sb.AppendLine("<b>Spectre</b> : " + def.absorptionSpectrum);
        sb.AppendLine("<b>Filtre Chelsea</b> : " + def.chelseaFilterReaction);
        sb.AppendLine("<b>Aimant</b> : " + def.magnetism);

        sb.AppendLine();
        sb.AppendLine(Heading("Les gisements"));
        var originTotal = 0f;
        foreach (var origin in def.origins) originTotal += Mathf.Max(0f, origin.rarityWeight);
        foreach (var origin in def.origins)
        {
            sb.AppendLine("<b>" + origin.name + "</b> : " + Percent(originTotal > 0f ? origin.rarityWeight / originTotal * 100f : 0f) + " des pierres  ·  prix ×" + origin.valueMultiplier.ToString("0.0#"));
            if (!string.IsNullOrEmpty(origin.inclusionOverride)) sb.AppendLine("    <color=#9AA3B5>" + origin.inclusionOverride + "</color>");
        }

        var habits = CrystalHabits.For(def);
        if (habits.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(Heading("Les cristaux bruts"));
            foreach (var chance in habits) sb.AppendLine("<b>" + chance.Habit.Name + "</b> : " + chance.Weight + " %\n    <color=#9AA3B5>" + chance.Habit.Description + "</color>");
        }
        sb.AppendLine("\n<color=#9AA3B5>Valeur de base : " + def.baseValue + " € le spécimen type.</color>");
        return sb.ToString();
    }

    // ---- Building -------------------------------------------------------------------------------------------------------------------

    private static Text Txt(Transform parent, string name, string content, int size, TextAnchor anchor, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, parent, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }

    private RectTransform MakeScroll(Transform parent, Vector2 topLeft, Vector2 size, Color background, out RectTransform content)
    {
        var viewportGO = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
        viewportGO.transform.SetParent(parent, false);
        var viewport = viewportGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(viewport, topLeft, size);
        viewportGO.GetComponent<Image>().color = background;
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var scroll = viewportGO.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        return viewport;
    }

    private void Build()
    {
        panel = new GameObject("GemEncyclopedia", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(W, H);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Encyclopédie des gemmes", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -14f), new Vector2(520f, 30f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(W - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        BuildList();
        BuildCenter();
        BuildRight();
        StardewKit.Skin(panel.transform);
    }

    private void BuildList()
    {
        var boxGO = new GameObject("Search", typeof(RectTransform), typeof(Image), typeof(InputField));
        boxGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(boxGO.GetComponent<RectTransform>(), new Vector2(18f, -54f), new Vector2(280f, 28f));
        boxGO.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);
        var text = RuntimeUIKit.MakeText("Text", boxGO.transform, "", 13, TextAnchor.MiddleLeft, Color.white, Vector2.zero, Vector2.zero);
        var placeholder = RuntimeUIKit.MakeText("Placeholder", boxGO.transform, "Rechercher une gemme…", 13, TextAnchor.MiddleLeft, new Color(0.58f, 0.60f, 0.66f), Vector2.zero, Vector2.zero);
        placeholder.fontStyle = FontStyle.Italic;
        foreach (var t in new[] { text, placeholder })
        {
            var r = t.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(8f, 1f);
            r.offsetMax = new Vector2(-8f, -1f);
        }
        search = boxGO.GetComponent<InputField>();
        search.textComponent = text;
        search.placeholder = placeholder;
        search.characterLimit = 30;
        search.onValueChanged.AddListener(ApplyFilter);

        MakeScroll(panel.transform, new Vector2(18f, -90f), new Vector2(280f, H - 108f), new Color(0.07f, 0.07f, 0.09f, 1f), out listContent);

        var database = GemDatabase.Instance;
        if (database == null) return;
        var gems = database.allGems.Where(g => g != null).OrderBy(g => TextEntry.Normalize(g.displayName)).ToList();
        foreach (var gem in gems)
        {
            var target = gem;
            var row = RuntimeUIKit.MakeBox("Row", listContent, Vector2.zero, new Vector2(264f, 24f), RowColor);
            var chip = RuntimeUIKit.MakeBox("Chip", row.transform, new Vector2(6f, -6f), new Vector2(12f, 12f), gem.iconColor.a > 0f ? gem.iconColor : Color.gray, false);
            chip.raycastTarget = false;
            var label = RuntimeUIKit.MakeText("Name", row.transform, gem.displayName, 12, TextAnchor.MiddleLeft, Ink, new Vector2(26f, 0f), new Vector2(234f, 24f));
            label.raycastTarget = false;
            row.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(target));
            items.Add(new ListItem { Def = gem, Rect = row.rectTransform, Back = row, Key = TextEntry.Normalize(gem.displayName + " " + gem.name.Replace('_', ' ')) });
        }
        ApplyFilter("");
    }

    private void ApplyFilter(string text)
    {
        var key = TextEntry.Normalize(text).Trim();
        var y = -4f;
        foreach (var item in items)
        {
            var visible = key.Length == 0 || item.Key.Contains(key);
            item.Rect.gameObject.SetActive(visible);
            if (!visible) continue;
            RuntimeUIKit.SetTopLeft(item.Rect, new Vector2(4f, y), new Vector2(264f, 24f));
            y -= 26f;
        }
        listContent.sizeDelta = new Vector2(0f, -y + 6f);
        listContent.anchoredPosition = Vector2.zero;
    }

    private void BuildCenter()
    {
        const float x = 316f;
        nameText = Txt(panel.transform, "Name", "", 22, TextAnchor.UpperLeft, Gold, new Vector2(x, -52f), new Vector2(330f, 28f));
        nameText.fontStyle = FontStyle.Bold;
        subText = Txt(panel.transform, "Sub", "", 12, TextAnchor.UpperLeft, Muted, new Vector2(x, -82f), new Vector2(330f, 18f));

        var frame = RuntimeUIKit.MakeBox("StoneFrame", panel.transform, new Vector2(x, -106f), new Vector2(StoneBox + 8f, StoneBox + 8f), new Color(0.06f, 0.06f, 0.08f, 1f), false);
        frame.raycastTarget = false;
        var stone = new GameObject("Stone", typeof(RectTransform), typeof(Image), typeof(StoneDrag));
        stone.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stone.GetComponent<RectTransform>(), new Vector2(x + 4f, -110f), new Vector2(StoneBox, StoneBox));
        stoneImage = stone.GetComponent<Image>();
        view = new GemView3D(192) { Realistic = true };
        stoneImage.sprite = view.Sprite;
        stoneImage.preserveAspect = true;
        var drag = stone.GetComponent<StoneDrag>();
        drag.OnTilt = delta =>
        {
            lastTouch = Time.unscaledTime;
            var yaw = Quaternion.AngleAxis(-delta.x * 0.7f, Vector3.up);
            var pitch = Quaternion.AngleAxis(delta.y * 0.7f, Vector3.right);
            orientation = yaw * pitch * orientation;
        };
        Txt(panel.transform, "DragHint", "Glissez pour tourner la pierre.", 11, TextAnchor.UpperLeft, Muted, new Vector2(x, -424f), new Vector2(330f, 16f));

        shapeLabel = Txt(panel.transform, "ShapeLabel", "Forme", 13, TextAnchor.UpperLeft, Gold, new Vector2(x, -446f), new Vector2(330f, 18f));
        shapeLabel.fontStyle = FontStyle.Bold;
        shapeBox = new GameObject("Shapes", typeof(RectTransform)).GetComponent<RectTransform>();
        shapeBox.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(shapeBox, new Vector2(x, -468f), new Vector2(330f, 140f));

        styleLabel = Txt(panel.transform, "StyleLabel", "Style de taille", 13, TextAnchor.UpperLeft, Gold, new Vector2(x, -622f), new Vector2(330f, 18f));
        styleLabel.fontStyle = FontStyle.Bold;
        styleBox = new GameObject("Styles", typeof(RectTransform)).GetComponent<RectTransform>();
        styleBox.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(styleBox, new Vector2(x, -644f), new Vector2(330f, 60f));
    }

    private void BuildRight()
    {
        const float x = 668f;
        var labels = new[] { "Couleurs", "Pureté et traitements", "Fiche" };
        var widths = new[] { 130f, 190f, 110f };
        var offset = 0f;
        for (var i = 0; i < 3; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Tab" + i, panel.transform, labels[i], 13, ButtonOff, new Vector2(x + offset, -54f), new Vector2(widths[i], 30f));
            button.onClick.AddListener(() => SetTab(index));
            tabButtons[i] = button;
            offset += widths[i] + 8f;
        }

        syntheticButton = RuntimeUIKit.MakeButton("Synthetic", panel.transform, "Naturelle", 13, ButtonOff, new Vector2(x + offset, -54f), new Vector2(110f, 30f));
        syntheticButton.onClick.AddListener(() =>
        {
            showSynthetic = !showSynthetic;
            hasSelectedColor = false;
            var cells = GemColorSystem.PaletteCells(def, 12, showSynthetic);
            if (cells.Count > 0)
            {
                var best = cells[0];
                foreach (var cell in cells) if (cell.Share > best.Share) best = cell;
                selectedColor = best.Color;
                selectedShare = best.Share;
                hasSelectedColor = true;
            }
            SetTab(0);
        });
        tabViewport = MakeScroll(panel.transform, new Vector2(x, -92f), new Vector2(W - x - 18f, H - 92f - 90f), new Color(0.07f, 0.07f, 0.09f, 1f), out tabContent);
        colorInfo = Txt(panel.transform, "ColorInfo", "", 13, TextAnchor.UpperLeft, Ink, new Vector2(x, -(H - 84f)), new Vector2(W - x - 18f, 70f));
        colorInfo.supportRichText = true;
    }

    // The mouse dragging the stone.
    private class StoneDrag : MonoBehaviour, IDragHandler
    {
        public System.Action<Vector2> OnTilt;

        public void OnDrag(PointerEventData eventData)
        {
            OnTilt?.Invoke(eventData.delta);
        }
    }
}
