using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrimoireUI : MonoBehaviour
{
    public static GrimoireUI Instance { get; private set; }

    private const int Columns = 5;
    private const float CardSize = 96f;
    private const float CardSpacing = 10f;

    [SerializeField] private GameObject gridPanel;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private Text progressText;
    [SerializeField] private Sprite lockedIcon;

    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailBodyText;
    [SerializeField] private Image detailSpectrumImage;

    private ScrollRect bodyScroll;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged -= RefreshIfOpen;
    }

    // The grimoire lives in a tab of the bag (G opens it there): the bag takes its two windows over.
    public GameObject GridPanel => gridPanel;
    public GameObject DetailPanel => detailPanel;

    public void OpenInBag()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
        Refresh();
    }

    private void Toggle()
    {
        if (detailPanel != null && detailPanel.activeSelf)
        {
            HideDetail();
            return;
        }

        var opening = gridPanel != null && !gridPanel.activeSelf;
        if (gridPanel != null) gridPanel.SetActive(opening);
        if (opening) Refresh();
    }

    private void RefreshIfOpen()
    {
        if (gridPanel != null && gridPanel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        if (contentContainer == null || GemDatabase.Instance == null) return;

        for (var i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        var allGems = GemDatabase.Instance.allGems;
        var discoveredCount = 0;

        for (var i = 0; i < allGems.Count; i++)
        {
            var def = allGems[i];
            if (def == null) continue;
            var discovered = GemCodex.Instance != null && GemCodex.Instance.IsDiscovered(def.name);
            if (discovered) discoveredCount++;
            CreateCard(def, discovered, i);
        }

        var totalRows = Mathf.CeilToInt(allGems.Count / (float)Columns);
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, totalRows * (CardSize + CardSpacing) + 10f);

        if (progressText != null)
            progressText.text = $"{discoveredCount} / {allGems.Count} espèces découvertes";
    }

    private void CreateCard(GemDefinition def, bool discovered, int index)
    {
        var col = index % Columns;
        var row = index / Columns;
        var x = 10f + col * (CardSize + CardSpacing);
        var y = -10f - row * (CardSize + CardSpacing);

        var go = new GameObject($"Card_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(contentContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(CardSize, CardSize);
        go.GetComponent<Image>().color = discovered
            ? new Color(0.20f, 0.20f, 0.24f, 1f)
            : new Color(0.10f, 0.10f, 0.12f, 1f);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -6f);
        iconRect.sizeDelta = new Vector2(CardSize - 24f, CardSize - 24f);
        var iconImage = iconGO.GetComponent<Image>();
        iconImage.sprite = discovered ? def.GetIcon() : lockedIcon;
        iconImage.preserveAspect = true;

        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(go.transform, false);
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0, 2f);
        nameRect.sizeDelta = new Vector2(0, 22f);
        var nameText = nameGO.AddComponent<Text>();
        nameText.text = discovered ? def.FullName : "???";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 10;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = discovered ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        var button = go.GetComponent<Button>();
        if (discovered)
        {
            var assetName = def.name;
            button.onClick.AddListener(() => ShowDetail(assetName));
        }
        else
        {
            button.interactable = false;
        }
    }

    public void ShowDetail(string definitionAssetName)
    {
        var def = GemDatabase.Instance != null ? GemDatabase.Instance.FindByAssetName(definitionAssetName) : null;
        if (def == null) return;

        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailTitleText != null) detailTitleText.text = def.FullName;
        BuildPage(def);
        if (detailSpectrumImage != null)
        {
            detailSpectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(def.spectrumLines);
            // The spectrum is drawn on the page only once the player has seen it with the spectroscope (a save from before the story knows everything).
            var story = StoryState.Instance;
            var seen = story == null || story.Legacy || GemCodex.Instance == null || GemCodex.Instance.IsKnown(def.name, "AbsorptionSpectrum");
            detailSpectrumImage.enabled = seen;
        }
    }

    // ---- The page of a species ---------------------------------------------------------------------------------------------

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Grey = new Color(0.50f, 0.53f, 0.60f);
    private static readonly Color TextColor = new Color(0.88f, 0.90f, 0.95f);

    private RectTransform rowsContent;
    private ScrollRect rowsScroll;

    // The page is a list of lines (a picture, then the text), in a scroll view: the photo of the stone first, then each piece of data with the instrument that gives it.
    private void EnsureRows()
    {
        if (rowsScroll != null || detailBodyText == null) return;

        var body = detailBodyText.rectTransform;
        detailBodyText.gameObject.SetActive(false);

        var viewGO = new GameObject("RowsScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewGO.transform.SetParent(body.parent, false);
        var view = viewGO.GetComponent<RectTransform>();
        view.anchorMin = body.anchorMin;
        view.anchorMax = body.anchorMax;
        view.pivot = body.pivot;
        view.anchoredPosition = body.anchoredPosition;
        view.sizeDelta = body.sizeDelta;
        viewGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        var contentGO = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewGO.transform, false);
        rowsContent = contentGO.GetComponent<RectTransform>();
        rowsContent.anchorMin = new Vector2(0f, 1f);
        rowsContent.anchorMax = new Vector2(1f, 1f);
        rowsContent.pivot = new Vector2(0.5f, 1f);
        rowsContent.anchoredPosition = Vector2.zero;
        rowsContent.sizeDelta = Vector2.zero;
        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 7f;
        layout.padding = new RectOffset(10, 22, 6, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rowsScroll = viewGO.GetComponent<ScrollRect>();
        rowsScroll.viewport = view;
        rowsScroll.content = rowsContent;
        rowsScroll.horizontal = false;
        rowsScroll.vertical = true;
        rowsScroll.movementType = ScrollRect.MovementType.Clamped;
        rowsScroll.scrollSensitivity = 30f;
    }

    private RectTransform NewRow(string name, float spacing = 10f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(rowsContent, false);
        var group = go.GetComponent<HorizontalLayoutGroup>();
        group.spacing = spacing;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return go.GetComponent<RectTransform>();
    }

    private static Image AddIcon(RectTransform row, Sprite sprite, bool dim, float width = 42f, float height = 32f)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(row, false);
        var element = go.GetComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = width;
        element.minHeight = element.preferredHeight = height;
        element.flexibleWidth = 0f;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = dim ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        return image;
    }

    private static Text AddText(RectTransform row, string text, int size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(row, false);
        go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.UpperLeft;
        label.color = color;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.lineSpacing = 1.08f;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private static Sprite LoadIcon(string path) => string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);

    private void AddSection(string title, string iconPath)
    {
        var row = NewRow("Section");
        AddIcon(row, LoadIcon(iconPath), false);
        var text = AddText(row, $"<b>{title}</b>", 15, Gold);
        text.alignment = TextAnchor.MiddleLeft;
        text.GetComponent<LayoutElement>().minHeight = 32f;
    }

    private void AddDataRow(string iconPath, string label, string value, bool known)
    {
        var row = NewRow("Data");
        AddIcon(row, LoadIcon(iconPath), !known);
        if (known)
            AddText(row, $"<color=#F2D9A6><b>{label}</b></color>   {value}", 13, TextColor);
        else
            AddText(row, $"<b>{label}</b>   ??? — pas encore mesuré", 13, Grey);
    }

    // The photo of the stone: the one of the player when they hold a stone of the species, else a reference specimen. Drawn by the same renderer as the loupe.
    private static readonly Dictionary<string, GemView3D> photoViews = new Dictionary<string, GemView3D>();
    private static readonly Dictionary<string, GemInstance> photoStones = new Dictionary<string, GemInstance>();

    private static Sprite PhotoOf(GemDefinition def, out bool owned)
    {
        GemInstance stone = null;
        if (PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems)
                if (gem != null && gem.Definition == def && (stone == null || (stone.IsRough && !gem.IsRough))) stone = gem;
        owned = stone != null;
        if (stone == null && !photoStones.TryGetValue(def.name, out stone))
        {
            stone = new GemInstance(def);
            photoStones[def.name] = stone;
        }

        if (!photoViews.TryGetValue(def.name, out var view))
        {
            view = new GemView3D(96);
            photoViews[def.name] = view;
        }

        GemView3D.Colors(stone, out var body, out var facet);
        var half = new Vector3(stone.LengthMm, stone.WidthMm, Mathf.Max(stone.DepthMm, 0.5f)) * 0.5f;
        view.Draw(GemView3D.MeshFor(stone), half, Quaternion.Euler(-32f, 16f, 8f), body, facet, def.Diaphaneity, stone.AsterismRays);
        return view.Sprite;
    }

    // The photo, stuck on the page with a bit of tape, and what the page says about itself.
    private void AddPhotoHeader(GemDefinition def, int known, int total, bool complete)
    {
        var row = NewRow("Header", 18f);
        var photo = PhotoOf(def, out var owned);

        var frameGO = new GameObject("Photo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frameGO.transform.SetParent(row, false);
        var element = frameGO.GetComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = 140f;
        element.minHeight = element.preferredHeight = 164f;
        var frame = frameGO.GetComponent<Image>();
        frame.color = new Color(0.94f, 0.91f, 0.82f, 1f);
        frame.raycastTarget = false;
        frameGO.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -2f);

        var inner = RuntimeUIKit.MakeBox("Inner", frameGO.transform, new Vector2(9f, -9f), new Vector2(122f, 122f), new Color(0.11f, 0.12f, 0.16f, 1f), false);
        inner.raycastTarget = false;
        var stoneGO = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGO.transform.SetParent(inner.transform, false);
        var stoneRect = stoneGO.GetComponent<RectTransform>();
        stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        stoneRect.pivot = new Vector2(0.5f, 0.5f);
        stoneRect.sizeDelta = new Vector2(116f, 116f);
        var stoneImage = stoneGO.GetComponent<Image>();
        stoneImage.sprite = photo;
        stoneImage.preserveAspect = true;
        stoneImage.raycastTarget = false;

        var caption = RuntimeUIKit.MakeText("Caption", frameGO.transform, owned ? "Votre pierre" : "Spécimen de référence", 11, TextAnchor.MiddleCenter, new Color(0.30f, 0.22f, 0.14f), new Vector2(4f, -135f), new Vector2(132f, 24f));
        caption.fontStyle = FontStyle.Italic;
        RuntimeUIKit.MakeBox("Tape", frameGO.transform, new Vector2(48f, -2f), new Vector2(44f, 14f), new Color(0.95f, 0.88f, 0.55f, 0.75f), false).raycastTarget = false;

        var text = complete
            ? "<color=#F2D9A6><b>Fiche complète</b></color>\nCette partie connaît toutes les informations sur cette espèce."
            : $"<color=#F2D9A6><b>Page de grimoire</b></color>\n{known} / {total} informations récoltées sur cette espèce.\n<color=#8F96A6>Chaque mesure faite sur une pierre de cette espèce remplit une ligne. Les lignes grisées ne sont pas encore connues.</color>";
        var label = AddText(row, text, 14, TextColor);
        label.alignment = TextAnchor.MiddleLeft;
    }

    private void BuildPage(GemDefinition def)
    {
        EnsureRows();
        if (rowsContent == null) return;
        for (var i = rowsContent.childCount - 1; i >= 0; i--) Destroy(rowsContent.GetChild(i).gameObject);

        var codex = GemCodex.Instance;
        var story = StoryState.Instance;
        var everything = codex == null || story == null || story.Legacy;

        // A stone of this species that the player still holds may know more than the grimoire: the grimoire learns from it.
        if (!everything && PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems)
                if (gem != null && gem.Definition == def)
                    foreach (var key in gem.RecordedKeys) codex.NoteKnown(def.name, key);

        var rows = SheetRows(def);
        var known = 0;
        var total = 0;
        foreach (var row in rows)
        {
            if (row.Keys == null) continue;
            total++;
            if (everything || codex.IsKnownAny(def.name, row.Keys)) known++;
        }

        AddPhotoHeader(def, known, total, everything);

        AddSection("Ce que l'on sait de la pierre", "MinimapIcons/diamant");
        foreach (var row in rows)
        {
            var isKnown = everything || row.Keys == null || codex.IsKnownAny(def.name, row.Keys);
            AddDataRow(row.Icon, row.Label, row.Value, isKnown);
        }

        if (!string.IsNullOrEmpty(def.description))
        {
            AddSection("Notes", "MinimapIcons/livre");
            var notes = NewRow("Notes");
            AddIcon(notes, null, false);
            AddText(notes, def.description, 13, TextColor);
        }

        AddSection("Traitements possibles", "MinimapIcons/fiole");
        foreach (var line in TreatmentReference.Lines(def))
        {
            var row = NewRow("Treatment");
            AddIcon(row, null, false);
            AddText(row, "· " + line, 13, TextColor);
        }

        // The advanced instruments, once used on a stone of the species.
        var entries = LabFindings.ReferenceEntries(def);
        var shown = new List<LabEntry>();
        foreach (var entry in entries)
            if (everything || codex.IsKnown(def.name, KeyOfInstrument(entry.Icon))) shown.Add(entry);
        if (shown.Count > 0)
        {
            AddSection("Instruments du laboratoire", "ToolIcons/Microscope");
            foreach (var entry in shown)
            {
                var row = NewRow("Instrument");
                AddIcon(row, LoadIcon("ToolIcons/" + entry.Icon), false);
                var text = $"<color=#F2D9A6><b>{entry.Title}</b></color>";
                foreach (var line in entry.Lines) text += $"\n· {line}";
                AddText(row, text, 13, TextColor);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContent);
        rowsScroll.verticalNormalizedPosition = 1f;
    }

    // The sheet is long once the instruments of the laboratory are in it: the text goes into a scroll view (mouse wheel, or drag).
    private void EnsureScroll()
    {
        if (bodyScroll != null || detailBodyText == null) return;

        var body = detailBodyText.rectTransform;
        var viewGO = new GameObject("BodyScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewGO.transform.SetParent(body.parent, false);
        var view = viewGO.GetComponent<RectTransform>();
        view.anchorMin = body.anchorMin;
        view.anchorMax = body.anchorMax;
        view.pivot = body.pivot;
        view.anchoredPosition = body.anchoredPosition;
        view.sizeDelta = body.sizeDelta;
        viewGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        body.SetParent(viewGO.transform, false);
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(1f, 1f);
        body.pivot = new Vector2(0.5f, 1f);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(-18f, 0f);
        detailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        detailBodyText.supportRichText = true;
        var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyScroll = viewGO.GetComponent<ScrollRect>();
        bodyScroll.content = body;
        bodyScroll.horizontal = false;
        bodyScroll.vertical = true;
        bodyScroll.movementType = ScrollRect.MovementType.Clamped;
        bodyScroll.scrollSensitivity = 30f;
    }

    public void HideDetail()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
    }

    public void Close()
    {
        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    // The whole sheet of a species: its properties, the treatments that can be done to it, what the instruments of the laboratory show.
    public static string BuildDetailText(GemDefinition def)
    {
        return BuildSheetText(def) + "\n\n" + BuildTreatmentsText(def) + BuildInstrumentsText(def, true);
    }

    // One line of the sheet of a species: its name, the picture of the instrument (or of the kind of data) beside it, the whole line, and the keys of the
    // instruments that show it (null: known as soon as the species is identified).
    private struct Row
    {
        public string Label;
        public string Icon;        // "ToolIcons/Loupe", "MinimapIcons/pioche"...
        public string Value;       // what is shown after the name on the page
        public string Line;        // the whole line as the books print it
        public string[] Keys;
    }

    private static readonly string[] VisualKeys = { "VisualExamination" };
    private static readonly string[] RefractiveKeys = { "RefractiveIndex", "RefractiveIndexOTL" };
    private static readonly string[] InclusionKeys = { "LoupeInspection", "Inclusions" };

    private static Row R(string label, string icon, string value, string[] keys = null, string linePrefix = null, string shown = null)
    {
        return new Row { Label = label, Icon = icon, Value = shown ?? value, Line = (linePrefix ?? label) + " : " + value, Keys = keys };
    }

    private static List<Row> SheetRows(GemDefinition def)
    {
        const string Loupe = "ToolIcons/Loupe";
        var rows = new List<Row>
        {
            R("Éclat", Loupe, def.luster, VisualKeys),
            R("Couleur", Loupe, def.color, VisualKeys),
            R("Forme et taille", Loupe, $"{def.shape}    Style de taille : {def.cutStyle}", VisualKeys, "Forme", $"{def.shape}, taille {def.cutStyle.ToLowerInvariant()}"),
            R("Transparence", Loupe, def.TransparencyLabel, VisualKeys),
            R("Indice de réfraction", "ToolIcons/Refractometre", $"{def.refractiveIndex:F3}", RefractiveKeys),
            R("Biréfringence", "ToolIcons/Refractometre", OpticalDescriptions.BirefringenceText(def), RefractiveKeys),
            R("Densité", "ToolIcons/BalanceHydrostatique", $"{def.density:F2}", new[] { "Density" }),
            R("Dureté (Mohs)", "MinimapIcons/pioche", $"{def.hardness:F2}"),
            R("Caractère optique", "ToolIcons/Polariscope", def.opticalCharacter, new[] { "OpticalCharacter" }),
            R("Pléochroïsme", "ToolIcons/Dichroscope", def.pleochroic ? def.pleochroicDescription : "Aucun.", new[] { "Pleochroism" }),
            R("Fluorescence UV longue onde", "ToolIcons/CabineUV", def.fluorescenceLongWave, new[] { "FluorescenceLongWave" }),
            R("Fluorescence UV courte onde", "ToolIcons/CabineUV", def.fluorescenceShortWave, new[] { "FluorescenceShortWave" }),
            R("Spectre d'absorption", "ToolIcons/Spectroscope", def.absorptionSpectrum, new[] { "AbsorptionSpectrum" }),
            R("Inclusions typiques", Loupe, def.inclusions, InclusionKeys),
            R("Figure d'interférence", "ToolIcons/Polariscope", def.interferenceFigure, new[] { "InterferenceFigure" }),
            R("Filtre Chelsea", "ToolIcons/FiltreChelsea", def.chelseaFilterReaction, new[] { "ChelseaFilter" }),
            R("Magnétisme", "ToolIcons/Aimant", def.magnetism, new[] { "Magnetism" }),
            R("Conductivité thermique", "ToolIcons/TesteurThermique", def.isThermallyConductiveLikeDiamond ? "élevée, comme le diamant" : "faible", new[] { "ThermalConductivity" }),
            R("Conductivité électrique", "ToolIcons/TesteurElectrique", def.isElectricallyConductive ? "conductrice" : "isolante", new[] { "ElectricalConductivity" }),
            R("Valeur de base", "MinimapIcons/rubis_dollar", $"{def.baseValue}€/ct (avant modificateurs d'origine, pureté, taille...)"),
        };

        // The habits a crystal of the species can have when it is rough (nothing for a species that does not grow in crystals).
        var habits = CrystalHabits.NamesFor(def);
        if (!string.IsNullOrEmpty(habits)) rows.Insert(3, R("Habitus cristallins", Loupe, habits, VisualKeys));
        return rows;
    }

    // The properties of the species and its notes.
    public static string BuildSheetText(GemDefinition def)
    {
        var lines = new List<string>();
        foreach (var row in SheetRows(def)) lines.Add(row.Line);

        var text = string.Join("\n", lines);
        if (!string.IsNullOrEmpty(def.description))
            text += $"\n\nNotes : {def.description}";
        return text;
    }

    // The ToolIcons sprite of an advanced instrument -> the key it records on a stone.
    private static string KeyOfInstrument(string icon)
    {
        switch (icon)
        {
            case "UvVisNir": return "ConfirmedOrigin";
            case "FTIR": return "Treatment";
            case "Raman": return "Authenticity";
            case "MEB": return "GrowthStructure";
            case "EDXRF": return "ElementalAnalysis";
            case "LAICPMS": return "TraceElements";
            case "Photoluminescence": return "Photoluminescence";
            case "DiamondView": return "DiamondGrowthImaging";
            case "DiamondSure": return "DiamondScreening";
            default: return icon;
        }
    }

    // The treatments the laboratory can apply to the species.
    public static string BuildTreatmentsText(GemDefinition def)
    {
        var text = "<color=#F2D9A6><b>Traitements possibles</b></color>";
        foreach (var line in TreatmentReference.Lines(def)) text += $"\n· {line}";
        return text;
    }

    // What the advanced instruments of the laboratory show for the species (empty when there is nothing to show).
    public static string BuildInstrumentsText(GemDefinition def, bool leadingBreak, System.Func<LabEntry, bool> keep = null)
    {
        var text = "";
        var entries = LabFindings.ReferenceEntries(def);
        if (keep != null) entries = entries.FindAll(entry => keep(entry));
        if (entries.Count > 0)
        {
            var origin = LabFindings.ReferenceOrigin(def);
            text += (leadingBreak ? "\n\n" : "") + "<color=#F2D9A6><b>Instruments du laboratoire</b></color>\n";
            text += $"<color=#9AA3B5>Pierre de référence naturelle, non traitée{(string.IsNullOrEmpty(origin) ? "" : $", d'origine {origin}")} : une pierre réelle varie selon son gisement, sa couleur et ses traitements.</color>";
            foreach (var entry in entries)
            {
                text += $"\n\n<color=#F2D9A6><b>{entry.Title}</b></color>";
                foreach (var line in entry.Lines) text += $"\n· {line}";
            }
        }
        return text;
    }
}
