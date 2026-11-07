using UnityEngine;
using UnityEngine.UI;

// Refractometer: drop a gem on the hemicylinder, read the shadow edge(s) on the graduated scale and turn the
// lens (the stone's orientation) to follow them. The side panel logs the extreme readings (n-alpha, n-beta,
// n-gamma) as the edges sweep, plus the resulting birefringence.
public class RefractometerUI : MonoBehaviour
{
    public static RefractometerUI Instance { get; private set; }

    private const float PanelWidth = 500f;
    private const float PanelHeight = 540f;

    private const float ScaleMin = 1.30f;
    private const float ScaleMax = 1.81f;      // upper limit of the contact liquid / prism
    private const float ViewX = 32f;
    private const float ViewY = -164f;
    private const float ViewWidth = 154f;
    private const float ViewHeight = 324f;

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color DarkZone = new Color(0.13f, 0.13f, 0.15f, 1f);
    private static readonly Color MidZone = new Color(0.52f, 0.52f, 0.55f, 1f);
    private static readonly Color BrightZone = new Color(0.96f, 0.96f, 0.94f, 1f);
    private static readonly Color InkOnBright = new Color(0.10f, 0.10f, 0.12f, 1f);
    private static readonly Color InkOnDark = new Color(0.88f, 0.88f, 0.9f, 1f);

    private enum Kind { Single, Uniaxial, Biaxial }

    private GameObject panel;
    private Image gemImage;
    private Text hintText;
    private Image darkZone;
    private Image midZone;
    private Image brightZone;
    private Text noGemText;
    private RectTransform dialVisual;
    private Text angleText;
    private Text alphaText;
    private Text betaText;
    private Text gammaText;
    private Text birefText;
    private Text statusText;

    private readonly System.Collections.Generic.List<Image> tickImages = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<float> tickValues = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<Text> labelTexts = new System.Collections.Generic.List<Text>();
    private readonly System.Collections.Generic.List<float> labelValues = new System.Collections.Generic.List<float>();

    private GemInstance stone;

    // The drop of contact liquid: it has to be on the hemicylinder before a stone is laid on it.
    private bool hasDrop;
    private Image dropImage;
    private RectTransform vialRect;
    private Text contactText;
    private bool dropping;

    // The tabs at the left: the table of the species and their indices, then the situations that can be met.
    private GameObject guideRoot;
    private GameObject guidePage;
    private RectTransform guideContent;
    private string guideKey;
    private ScrollRect guideScroll;
    private RefractoGuide.Tab? guideTab;
    private readonly System.Collections.Generic.List<Image> guideTabImages = new System.Collections.Generic.List<Image>();
    private const float CenterX = 40f;
    private const float GuideTabSize = 44f;
    private const float GuidePageWidth = 340f;

    // Optical model of the current stone.
    private Kind kind;
    private bool polycrystalline;
    private float nAlpha;
    private float nBeta;
    private float nGamma;
    private bool fixedEdgeIsLow;
    private float phase;
    private float offset;

    private float dialAngle;                   // radians
    private bool hasLow, hasHigh;              // visible edges at the current angle
    private float currentLow, currentHigh;
    private bool trackedLow, trackedHigh;
    private float lowMin, lowMax, highMin, highMax;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<RefractometerUI>();
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
        guideRoot.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        stone = null;
        hasDrop = false;
        guideTab = null;
        guidePage.SetActive(false);
        guideRoot.SetActive(true);
        UpdateGuideTabColors();
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (guideRoot != null) guideRoot.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Optical model ---------------------------------------------------------------------------------------

    private static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    private static float Unit(uint hash, int salt)
    {
        var h = hash ^ (uint)(salt * 2654435761u);
        h ^= h >> 13;
        h *= 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFF) / 65535f;
    }

    private void SetupOptics(GemInstance gem)
    {
        var def = gem.Definition;
        var speciesHash = StableHash(def.name);
        var stoneHash = StableHash(gem.SpecimenId ?? "");

        polycrystalline = def.isPolycrystalline;
        nAlpha = def.refractiveIndex;

        var anisotropic = !polycrystalline
                          && def.opticalCharacterClass != GemDefinition.OpticalCharacterClass.Isotropic
                          && def.opticalCharacterClass != GemDefinition.OpticalCharacterClass.Opaque
                          && def.birefringence > 0.0005f;

        if (!anisotropic)
        {
            kind = Kind.Single;
            nBeta = nGamma = nAlpha;
        }
        else if (def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Uniaxial)
        {
            kind = Kind.Uniaxial;
            nGamma = nAlpha + def.birefringence;
            nBeta = nAlpha;
        }
        else
        {
            kind = Kind.Biaxial;
            nGamma = nAlpha + def.birefringence;
            // The optical sign decides where the middle index lies: nearer nα (positive) or nearer nγ (negative).
            var positive = OpticalSigns.SignOf(def) == '+';
            nBeta = nAlpha + def.birefringence * (positive ? Mathf.Lerp(0.2f, 0.4f, Unit(speciesHash, 1)) : Mathf.Lerp(0.6f, 0.8f, Unit(speciesHash, 1)));
        }

        // Uniaxial: in a positive stone the lower edge (nω) stays still and the upper one moves; in a negative stone it is the other way round.
        fixedEdgeIsLow = OpticalSigns.SignOf(def) == '+';
        phase = Mathf.Lerp(0.6f, 1.2f, Unit(speciesHash, 3));
        offset = Unit(stoneHash, 4) * Mathf.PI;     // each specimen starts at its own orientation
        dialAngle = 0f;
    }

    // Edges (lower, upper) at the current lens angle. Values above the scale limit are not visible.
    private void ComputeEdges(float angle, out bool lowVisible, out bool highVisible, out float low, out float high)
    {
        var theta = angle + offset;
        var s1 = Mathf.Sin(theta) * Mathf.Sin(theta);
        var s2 = Mathf.Sin(theta + phase) * Mathf.Sin(theta + phase);

        switch (kind)
        {
            case Kind.Uniaxial:
            {
                var fixedEdge = fixedEdgeIsLow ? nAlpha : nGamma;
                var other = fixedEdgeIsLow ? nGamma : nAlpha;
                var moving = other + (fixedEdge - other) * s1;
                low = Mathf.Min(fixedEdge, moving);
                high = Mathf.Max(fixedEdge, moving);
                break;
            }
            case Kind.Biaxial:
                low = nAlpha + (nBeta - nAlpha) * s1;
                high = nGamma - (nGamma - nBeta) * s2;
                break;
            default:
                low = nAlpha;
                high = nAlpha;
                break;
        }

        lowVisible = low >= ScaleMin && low <= ScaleMax;
        highVisible = kind != Kind.Single && high >= ScaleMin && high <= ScaleMax;
    }

    private void ResetTracking()
    {
        trackedLow = trackedHigh = false;
        lowMin = highMin = float.MaxValue;
        lowMax = highMax = float.MinValue;
        Sample(dialAngle);
    }

    private void Sample(float angle)
    {
        ComputeEdges(angle, out var lowVisible, out var highVisible, out var low, out var high);

        if (lowVisible)
        {
            trackedLow = true;
            lowMin = Mathf.Min(lowMin, low);
            lowMax = Mathf.Max(lowMax, low);
        }

        if (highVisible)
        {
            trackedHigh = true;
            highMin = Mathf.Min(highMin, high);
            highMax = Mathf.Max(highMax, high);
        }
    }

    private void OnDialRotated(float deltaRadians)
    {
        if (stone == null) return;

        // Sample along the way so a fast drag cannot jump over an extreme.
        var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(deltaRadians) / 0.03f));
        for (var i = 1; i <= steps; i++)
            Sample(dialAngle + deltaRadians * i / steps);

        dialAngle += deltaRadians;
        RecordReading();
        UpdateView();
    }

    private void Rotate(float degrees)
    {
        OnDialRotated(degrees * Mathf.Deg2Rad);
    }

    // ---- Display ---------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        if (gem != null && gem.IsSet)
        {
            ToolFeedbackUI.Instance?.ShowMessage(JewelCatalog.SetRefusal);
            stone = null;
            Refresh();
            return;
        }

        // A rough stone has no polished facet to lay on the glass: the refractometer cannot read it.
        if (gem != null && gem.IsRough)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — Une pierre brute n'a pas de facette polie à poser sur le verre : impossible de lire son indice. Faites-la tailler d'abord.");
            Sfx.Play(Sfx.Id.Error);
            stone = null;
            Refresh();
            return;
        }

        // Without liquid there is air between the stone and the glass: no reading. The drop comes first.
        if (gem != null && !hasDrop)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — Déposez d'abord une goutte de liquide de contact sur l'hémicylindre (la fiole), puis la pierre : sans liquide, une lame d'air fausse la lecture.");
            Sfx.Play(Sfx.Id.Error);
            stone = null;
            Refresh();
            return;
        }

        stone = gem;
        SetupOptics(gem);
        ResetTracking();
        Refresh();

        if (!trackedLow)
        {
            stone.RecordObservation("RefractiveIndexOTL");
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — Indice hors limite (> 1.81) : aucune limite d'ombre.");
            StoryState.Instance?.OnObserved(stone, "RefractiveIndex");
        }
        else
        {
            RecordReading();
        }
    }

    // The lowest index seen so far is logged on the stone, so it shows up in its description and in the report.
    private void RecordReading()
    {
        if (stone == null || !trackedLow) return;
        stone.RecordMeasurement(ExaminationTool.MeasurementType.RefractiveIndex.ToString(), lowMin);
        StoryState.Instance?.OnObserved(stone, "RefractiveIndex");
    }

    private void ClearStone()
    {
        // Taking the stone off wipes the glass: the next one needs its own drop.
        stone = null;
        hasDrop = false;
        Refresh();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = hasDrop ? "Déposez une gemme sur la goutte" : "Une goutte de liquide d'abord (fiole), puis la gemme";
        }
        else
        {
            gemImage.enabled = true;
            gemImage.sprite = stone.DisplaySprite;
            hintText.text = "";
        }

        UpdateView();
        UpdateContact();
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    private float YOf(float n)
    {
        return Mathf.Clamp01((n - ScaleMin) / (ScaleMax - ScaleMin)) * ViewHeight;
    }

    private void UpdateView()
    {
        dialVisual.localEulerAngles = new Vector3(0f, 0f, dialAngle * Mathf.Rad2Deg);
        var degrees = Mathf.Repeat(dialAngle * Mathf.Rad2Deg, 360f);
        angleText.text = $"{degrees:F0}°";

        hasLow = hasHigh = false;
        currentLow = currentHigh = 0f;
        if (stone != null)
        {
            ComputeEdges(dialAngle, out hasLow, out hasHigh, out currentLow, out currentHigh);
            if (kind == Kind.Single) hasHigh = false;
        }

        UpdateZones();
        UpdateTicks();
        UpdateReadouts();
        RefreshGuide();
    }

    private void SetZone(Image zone, float top, float bottom)
    {
        var height = Mathf.Max(0f, bottom - top);
        zone.enabled = height > 0.01f;
        RuntimeUIKit.SetTopLeft(zone.rectTransform, new Vector2(ViewX, ViewY - top), new Vector2(ViewWidth, height));
    }

    private void UpdateZones()
    {
        noGemText.enabled = stone == null;

        if (stone == null)
        {
            // Empty field: uniformly lit.
            SetZone(darkZone, 0f, 0f);
            SetZone(midZone, 0f, 0f);
            SetZone(brightZone, 0f, ViewHeight);
            return;
        }

        if (!hasLow)
        {
            // Out of limit: no shadow edge, the whole field stays bright.
            SetZone(darkZone, 0f, 0f);
            SetZone(midZone, 0f, 0f);
            SetZone(brightZone, 0f, ViewHeight);
            return;
        }

        var yLow = YOf(currentLow);
        SetZone(darkZone, 0f, yLow);

        if (kind == Kind.Single)
        {
            SetZone(midZone, 0f, 0f);
            SetZone(brightZone, yLow, ViewHeight);
        }
        else if (hasHigh)
        {
            var yHigh = YOf(currentHigh);
            SetZone(midZone, yLow, yHigh);
            SetZone(brightZone, yHigh, ViewHeight);
        }
        else
        {
            // The upper edge lies beyond the scale: everything below the first edge stays in the intermediate zone.
            SetZone(midZone, yLow, ViewHeight);
            SetZone(brightZone, 0f, 0f);
        }
    }

    private Color InkAt(float n)
    {
        if (stone == null || !hasLow) return InkOnBright;
        if (n < currentLow) return InkOnDark;
        if (kind != Kind.Single && (!hasHigh || n < currentHigh)) return new Color(0.05f, 0.05f, 0.07f, 1f);
        return InkOnBright;
    }

    private void UpdateTicks()
    {
        for (var i = 0; i < tickImages.Count; i++)
            tickImages[i].color = InkAt(tickValues[i]);
        for (var i = 0; i < labelTexts.Count; i++)
            labelTexts[i].color = InkAt(labelValues[i]);
    }

    private static string Format(float value)
    {
        return value.ToString("F3");
    }

    private void UpdateReadouts()
    {
        const string none = "—";

        if (stone == null)
        {
            alphaText.text = $"nα = {none}";
            betaText.text = $"nβ = {none}";
            gammaText.text = $"nγ = {none}";
            birefText.text = $"Δn = {none}";
            statusText.text = "Aucune gemme sur l'hémicylindre.";
            return;
        }

        if (!trackedLow)
        {
            alphaText.text = $"nα = {none}";
            betaText.text = $"nβ = {none}";
            gammaText.text = $"nγ = {none}";
            birefText.text = $"Δn = {none}";
            statusText.text = "Hors limite (OTL) : aucune limite d'ombre sur l'échelle.";
            return;
        }

        string alpha, beta, gamma, biref;
        if (kind == Kind.Single)
        {
            alpha = Format(lowMin);
            beta = Format(lowMin);
            gamma = Format(lowMin);
            biref = polycrystalline ? none : "0.000";
        }
        else
        {
            alpha = Format(lowMin);
            gamma = trackedHigh ? Format(highMax) : "> 1.81";
            beta = kind == Kind.Biaxial && lowMax - lowMin > 0.0005f ? Format(lowMax) : none;
            biref = trackedHigh ? Format(highMax - lowMin) : none;
        }

        alphaText.text = $"nα = {alpha}";
        betaText.text = $"nβ = {beta}";
        gammaText.text = $"nγ = {gamma}";
        birefText.text = $"Δn = {biref}";

        var visible = (hasLow ? 1 : 0) + (hasHigh ? 1 : 0);
        var moves = "";
        if (kind != Kind.Single && trackedHigh)
        {
            var lowMoves = lowMax - lowMin > 0.0005f;
            var highMoves = highMax - highMin > 0.0005f;
            moves = "\n" + (lowMoves && highMoves ? "Les deux limites bougent."
                : lowMoves ? "La limite basse bouge, la haute est fixe."
                : highMoves ? "La limite haute bouge, la basse est fixe."
                : "Aucune limite n'a bougé : tournez la lentille.");
        }
        statusText.text = (visible == 0
            ? "Hors limite (OTL) : aucune limite d'ombre visible."
            : visible == 1
                ? "1 limite d'ombre visible sur l'échelle."
                : "2 limites d'ombre visibles sur l'échelle.") + moves;
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("RefractometerPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted a little to the right of the middle: the tabs of the guide fit on the left, and the inventory (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(CenterX, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Réfractomètre", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildHemicylinder();
        BuildEyepiece();
        BuildDial();
        BuildReadouts();
        BuildVial();
        BuildGuide();

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 14, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2((PanelWidth - 160f) / 2f, -PanelHeight + 44f), new Vector2(160f, 30f));
        close.onClick.AddListener(Close);
    }

    private void BuildHemicylinder()
    {
        var box = RuntimeUIKit.MakeBox("Hemicylinder", panel.transform, new Vector2(24f, -44f), new Vector2(252f, 96f),
            new Color(0.50f, 0.53f, 0.58f, 1f));
        box.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        box.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        var glass = RuntimeUIKit.MakeBox("Glass", panel.transform, new Vector2(50f, -58f), new Vector2(200f, 46f),
            new Color(0.32f, 0.52f, 0.66f, 0.95f));
        glass.raycastTarget = false;
        var shine = RuntimeUIKit.MakeBox("Shine", panel.transform, new Vector2(58f, -62f), new Vector2(184f, 6f),
            new Color(0.85f, 0.93f, 1f, 0.35f));
        shine.raycastTarget = false;

        var gemGO = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gemGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(gemGO.GetComponent<RectTransform>(), new Vector2(130f, -60f), new Vector2(40f, 40f));
        gemImage = gemGO.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.15f),
            new Vector2(24f, -118f), new Vector2(252f, 18f));
    }

    private void BuildEyepiece()
    {
        RuntimeUIKit.MakeBox("EyepieceFrame", panel.transform, new Vector2(ViewX - 8f, ViewY + 8f),
            new Vector2(ViewWidth + 16f, ViewHeight + 16f), new Color(0.03f, 0.03f, 0.04f, 1f));

        brightZone = RuntimeUIKit.MakeBox("BrightZone", panel.transform, new Vector2(ViewX, ViewY), new Vector2(ViewWidth, ViewHeight), BrightZone, false);
        midZone = RuntimeUIKit.MakeBox("MidZone", panel.transform, new Vector2(ViewX, ViewY), new Vector2(ViewWidth, 0f), MidZone, false);
        darkZone = RuntimeUIKit.MakeBox("DarkZone", panel.transform, new Vector2(ViewX, ViewY), new Vector2(ViewWidth, 0f), DarkZone, false);
        brightZone.raycastTarget = false;
        midZone.raycastTarget = false;
        darkZone.raycastTarget = false;

        // Graduations every 0.01, longer and labelled every 0.05.
        for (var i = 0; i <= 51; i++)
        {
            var n = ScaleMin + i * 0.01f;
            var major = i % 5 == 0;
            var y = ViewY - YOf(n);
            var length = major ? 30f : 14f;
            var tick = RuntimeUIKit.MakeBox("Tick", panel.transform, new Vector2(ViewX + 8f, y), new Vector2(length, major ? 2f : 1f),
                InkOnBright, false);
            tick.raycastTarget = false;
            tickImages.Add(tick);
            tickValues.Add(n);

            if (!major || i == 0) continue;
            var label = RuntimeUIKit.MakeText("Label", panel.transform, n.ToString("F2").Replace('.', ','), 12,
                TextAnchor.MiddleLeft, InkOnBright, new Vector2(ViewX + 44f, y + 8f), new Vector2(50f, 16f));
            labelTexts.Add(label);
            labelValues.Add(n);
        }

        noGemText = RuntimeUIKit.MakeText("NoGem", panel.transform, "Aucune gemme", 12, TextAnchor.MiddleCenter,
            new Color(0.4f, 0.4f, 0.45f), new Vector2(ViewX + 50f, ViewY - ViewHeight / 2f + 10f), new Vector2(100f, 20f));
    }

    private void BuildDial()
    {
        const float size = 84f;
        var cx = 242f;
        var cy = -236f;

        var holder = new GameObject("Dial", typeof(RectTransform), typeof(Image), typeof(RefractometerDial));
        holder.transform.SetParent(panel.transform, false);
        var holderRect = holder.GetComponent<RectTransform>();
        // Centered pivot: the drag handler measures the pointer angle around the middle of the knob.
        holderRect.anchorMin = new Vector2(0f, 1f);
        holderRect.anchorMax = new Vector2(0f, 1f);
        holderRect.pivot = new Vector2(0.5f, 0.5f);
        holderRect.sizeDelta = new Vector2(size, size);
        holderRect.anchoredPosition = new Vector2(cx, cy);
        holder.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        holder.GetComponent<RefractometerDial>().OnRotate = OnDialRotated;

        var visual = new GameObject("DialVisual", typeof(RectTransform), typeof(Image));
        visual.transform.SetParent(holder.transform, false);
        dialVisual = visual.GetComponent<RectTransform>();
        dialVisual.anchorMin = Vector2.zero;
        dialVisual.anchorMax = Vector2.one;
        dialVisual.offsetMin = Vector2.zero;
        dialVisual.offsetMax = Vector2.zero;
        var visualImage = visual.GetComponent<Image>();
        visualImage.sprite = MakeDialSprite(128);
        visualImage.raycastTarget = false;

        RuntimeUIKit.MakeText("DialLabel", panel.transform, "Lentille", 12, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.85f),
            new Vector2(cx - 40f, cy - size / 2f - 4f), new Vector2(80f, 16f));

        var left = RuntimeUIKit.MakeButton("RotateLeft", panel.transform, "<", 14, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(cx - 44f, cy - size / 2f - 24f), new Vector2(40f, 28f));
        left.onClick.AddListener(() => Rotate(-8f));
        var right = RuntimeUIKit.MakeButton("RotateRight", panel.transform, ">", 14, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(cx + 4f, cy - size / 2f - 24f), new Vector2(40f, 28f));
        right.onClick.AddListener(() => Rotate(8f));

        angleText = RuntimeUIKit.MakeText("Angle", panel.transform, "0°", 12, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.85f),
            new Vector2(cx - 40f, cy - size / 2f - 56f), new Vector2(80f, 16f));
    }

    private void BuildReadouts()
    {
        RuntimeUIKit.MakeText("ReadHeader", panel.transform, "Indices relevés", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(300f, -44f), new Vector2(180f, 20f));
        RuntimeUIKit.MakeBox("ReadBox", panel.transform, new Vector2(296f, -68f), new Vector2(184f, 148f),
            new Color(0.04f, 0.05f, 0.07f, 1f));

        var ink = new Color(0.55f, 0.95f, 0.65f);
        alphaText = RuntimeUIKit.MakeText("Alpha", panel.transform, "", 17, TextAnchor.MiddleLeft, ink, new Vector2(308f, -76f), new Vector2(166f, 28f));
        betaText = RuntimeUIKit.MakeText("Beta", panel.transform, "", 17, TextAnchor.MiddleLeft, ink, new Vector2(308f, -106f), new Vector2(166f, 28f));
        gammaText = RuntimeUIKit.MakeText("Gamma", panel.transform, "", 17, TextAnchor.MiddleLeft, ink, new Vector2(308f, -136f), new Vector2(166f, 28f));
        birefText = RuntimeUIKit.MakeText("Biref", panel.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.95f, 0.85f, 0.45f), new Vector2(308f, -176f), new Vector2(166f, 28f));
        RuntimeUIKit.MakeText("BirefName", panel.transform, "(biréfringence)", 10, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.65f),
            new Vector2(380f, -204f), new Vector2(100f, 14f));

        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 12, TextAnchor.UpperLeft, Color.white,
            new Vector2(296f, -236f), new Vector2(184f, 50f));
        RuntimeUIKit.MakeText("Tip", panel.transform,
            "Tournez la lentille : les limites d'ombre se déplacent. Les valeurs extrêmes observées donnent nα, nβ et nγ.",
            11, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.65f), new Vector2(296f, -296f), new Vector2(184f, 70f));
    }

    // ---- The contact liquid ------------------------------------------------------------------------------------------------

    private void BuildVial()
    {
        // The bottle of contact liquid, under the readings, and the drop on the glass.
        var vial = new GameObject("Vial", typeof(RectTransform), typeof(Image));
        vial.transform.SetParent(panel.transform, false);
        vialRect = vial.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(vialRect, new Vector2(302f, -378f), new Vector2(34f, 66f));
        vialRect.pivot = new Vector2(0.5f, 0.5f);
        vialRect.anchoredPosition = new Vector2(319f, -411f);
        var vialImage = vial.GetComponent<Image>();
        vialImage.sprite = MakeVialSprite();
        vialImage.preserveAspect = true;
        vialImage.raycastTarget = false;

        RuntimeUIKit.MakeText("VialLabel", panel.transform, "Liquide de contact (n ≈ 1,81)", 11, TextAnchor.UpperLeft, new Color(0.7f, 0.85f, 0.9f),
            new Vector2(348f, -374f), new Vector2(140f, 30f));
        var drop = RuntimeUIKit.MakeButton("DropButton", panel.transform, "Déposer une goutte", 13, new Color(0.22f, 0.42f, 0.50f, 1f),
            new Vector2(348f, -406f), new Vector2(136f, 30f));
        drop.onClick.AddListener(PutDrop);
        contactText = RuntimeUIKit.MakeText("Contact", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f),
            new Vector2(348f, -440f), new Vector2(140f, 30f));

        // The drop itself, on the glass of the hemicylinder.
        var dropGO = new GameObject("Drop", typeof(RectTransform), typeof(Image));
        dropGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(dropGO.GetComponent<RectTransform>(), new Vector2(130f, -76f), new Vector2(40f, 20f));
        dropImage = dropGO.GetComponent<Image>();
        dropImage.sprite = RuntimeUIKit.Rounded;
        dropImage.type = Image.Type.Sliced;
        dropImage.color = new Color(0.78f, 0.95f, 0.92f, 0.85f);
        dropImage.raycastTarget = false;
        dropImage.enabled = false;
    }

    // A click on the bottle: it tips, a drop falls on the glass.
    private void PutDrop()
    {
        if (dropping) return;
        if (stone != null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — Il y a déjà une pierre sur le verre : retirez-la (clic sur l'hémicylindre) avant de déposer une nouvelle goutte.");
            return;
        }
        if (hasDrop)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — La goutte est déjà sur le verre : déposez la pierre dessus.");
            return;
        }
        StartCoroutine(DropRoutine());
    }

    private System.Collections.IEnumerator DropRoutine()
    {
        dropping = true;

        // The bottle tips.
        for (var t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            vialRect.localEulerAngles = new Vector3(0f, 0f, Mathf.SmoothStep(0f, 38f, t / 0.35f));
            yield return null;
        }

        // The drop falls from the bottle onto the middle of the glass.
        var fall = new GameObject("FallingDrop", typeof(RectTransform), typeof(Image));
        fall.transform.SetParent(panel.transform, false);
        var fallRect = fall.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(fallRect, Vector2.zero, new Vector2(8f, 12f));
        fallRect.pivot = new Vector2(0.5f, 0.5f);
        var fallImage = fall.GetComponent<Image>();
        fallImage.sprite = RuntimeUIKit.Rounded;
        fallImage.type = Image.Type.Sliced;
        fallImage.color = new Color(0.78f, 0.95f, 0.92f, 0.95f);
        fallImage.raycastTarget = false;
        var from = new Vector2(150f, -44f);
        var to = new Vector2(150f, -82f);
        for (var t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            var u = t / 0.4f;
            fallRect.anchoredPosition = Vector2.Lerp(from, to, u * u);
            yield return null;
        }
        Destroy(fall);

        Sfx.Play(Sfx.Id.Drop, 0.8f);
        hasDrop = true;
        UpdateContact();
        Refresh();

        for (var t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            vialRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(38f, 0f, t / 0.3f));
            yield return null;
        }
        vialRect.localEulerAngles = Vector3.zero;
        dropping = false;
    }

    private void UpdateContact()
    {
        if (dropImage != null) dropImage.enabled = hasDrop && stone == null;
        if (contactText != null) contactText.text = hasDrop ? "Goutte sur le verre : posez la pierre." : "Pas de goutte sur le verre.";
    }

    private static Sprite MakeVialSprite()
    {
        const int w = 17;
        const int h = 33;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
        void Put(int x, int yFromTop, Color32 c) { if (x >= 0 && x < w && yFromTop >= 0 && yFromTop < h) pixels[(h - 1 - yFromTop) * w + x] = c; }

        var cap = new Color32(40, 40, 46, 255);
        var capLight = new Color32(78, 78, 88, 255);
        var glass = new Color32(120, 82, 40, 255);
        var glassLight = new Color32(176, 126, 66, 255);
        var liquid = new Color32(190, 236, 228, 255);
        var label = new Color32(236, 230, 210, 255);
        var ink = new Color32(70, 60, 50, 255);

        // The dropper cap and the neck.
        for (var x = 5; x <= 11; x++) { Put(x, 0, cap); Put(x, 1, cap); Put(x, 2, capLight); }
        for (var x = 3; x <= 13; x++) Put(x, 3, cap);
        for (var y = 4; y <= 6; y++) for (var x = 6; x <= 10; x++) Put(x, y, glass);
        // The body: brown glass with the liquid showing at the bottom.
        for (var y = 7; y <= 31; y++)
            for (var x = 1; x <= 15; x++)
            {
                var edge = x == 1 || x == 15 || y == 31;
                var c = edge ? glass : (x == 3 ? glassLight : glass);
                if (y > 24 && !edge) c = new Color32(160, 214, 206, 255);
                Put(x, y, c);
            }
        for (var y = 25; y <= 29; y++) for (var x = 3; x <= 13; x++) Put(x, y, liquid);
        // The label.
        for (var y = 12; y <= 21; y++) for (var x = 3; x <= 13; x++) Put(x, y, label);
        for (var x = 5; x <= 11; x++) { Put(x, 15, ink); Put(x, 18, ink); }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    // ---- The tabs of the guide ---------------------------------------------------------------------------------------------

    private void BuildGuide()
    {
        guideRoot = new GameObject("RefractoGuidePanel", typeof(RectTransform));
        guideRoot.transform.SetParent(transform, false);
        var rect = guideRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(GuidePageWidth + 4f + GuideTabSize, PanelHeight);
        // To the left of the refractometer, with the same top: the tabs touch the panel, the page opens on their left.
        rect.anchoredPosition = new Vector2(CenterX - PanelWidth / 2f - 2f, PanelHeight / 2f);

        guidePage = new GameObject("Page", typeof(RectTransform));
        guidePage.transform.SetParent(rect, false);
        RuntimeUIKit.SetTopLeft(guidePage.GetComponent<RectTransform>(), Vector2.zero, new Vector2(GuidePageWidth, PanelHeight));
        RuntimeUIKit.MakeBox("Sheet", guidePage.transform, Vector2.zero, new Vector2(GuidePageWidth, PanelHeight), PanelColor);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(guidePage.transform, false);
        RuntimeUIKit.SetTopLeft(viewport.GetComponent<RectTransform>(), new Vector2(12f, -10f), new Vector2(GuidePageWidth - 24f, PanelHeight - 20f));
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.004f);

        // The page is a list of blocks, one under the other: texts, and the pictures of the eyepiece.
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewport.transform, false);
        guideContent = contentGO.GetComponent<RectTransform>();
        guideContent.anchorMin = new Vector2(0f, 1f);
        guideContent.anchorMax = new Vector2(1f, 1f);
        guideContent.pivot = new Vector2(0f, 1f);
        guideContent.anchoredPosition = Vector2.zero;
        guideContent.sizeDelta = Vector2.zero;
        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        guideScroll = viewport.AddComponent<ScrollRect>();
        guideScroll.viewport = viewport.GetComponent<RectTransform>();
        guideScroll.content = guideContent;
        guideScroll.horizontal = false;
        guideScroll.vertical = true;
        guideScroll.movementType = ScrollRect.MovementType.Clamped;
        guideScroll.scrollSensitivity = 28f;
        StardewKit.Skin(guidePage.transform);

        const float tabHeight = 52f;
        var tabX = GuidePageWidth + 4f;
        for (var i = 0; i < RefractoGuide.Tabs.Length; i++)
        {
            var tab = RefractoGuide.Tabs[i];
            var button = RuntimeUIKit.MakeButton("Tab_" + tab, rect, "", 9, new Color(0.25f, 0.25f, 0.32f, 1f), new Vector2(tabX, -i * (tabHeight + 5f)), new Vector2(GuideTabSize, tabHeight));
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(button.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -4f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = MakeTabIcon(tab);
            iconImage.raycastTarget = false;

            var label = button.GetComponentInChildren<Text>();
            label.text = RefractoGuide.ShortName(tab);
            label.fontSize = 9;
            label.alignment = TextAnchor.LowerCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.rectTransform.offsetMin = new Vector2(0f, 2f);
            label.rectTransform.offsetMax = Vector2.zero;

            guideTabImages.Add(button.GetComponent<Image>());
            var captured = tab;
            button.onClick.AddListener(() => ToggleGuideTab(captured));
        }

        StardewKit.Skin(rect);
        guidePage.SetActive(false);
        UpdateGuideTabColors();
    }

    private void ToggleGuideTab(RefractoGuide.Tab tab)
    {
        Sfx.Play(Sfx.Id.Click, 0.5f);
        guideTab = guideTab.HasValue && guideTab.Value == tab ? (RefractoGuide.Tab?)null : tab;
        guidePage.SetActive(guideTab.HasValue);
        UpdateGuideTabColors();
        guideKey = null;
        RefreshGuide();
        if (guideScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(guideContent);
            guideScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void UpdateGuideTabColors()
    {
        for (var i = 0; i < guideTabImages.Count; i++)
            guideTabImages[i].color = guideTab.HasValue && RefractoGuide.Tabs[i] == guideTab.Value ? new Color(0.62f, 0.46f, 0.20f, 1f) : new Color(0.25f, 0.25f, 0.32f, 1f);
    }

    private RefractoGuide.Situation CurrentSituation()
    {
        var s = new RefractoGuide.Situation { HasStone = stone != null };
        if (stone == null) return s;
        s.Read = trackedLow;
        s.OutOfLimit = !trackedLow;
        s.NAlpha = trackedLow ? lowMin : 0f;
        s.NGamma = trackedHigh ? highMax : s.NAlpha;
        s.LowMoves = trackedLow && lowMax - lowMin > 0.0005f;
        s.HighMoves = trackedHigh && highMax - highMin > 0.0005f;
        s.NBeta = s.LowMoves ? lowMax : 0f;
        s.HasLowEdge = hasLow;
        s.HasHighEdge = hasHigh && kind != Kind.Single;
        s.CurrentLow = currentLow;
        s.CurrentHigh = currentHigh;
        s.AngleDegrees = Mathf.Repeat(dialAngle * Mathf.Rad2Deg, 360f);
        s.Double = kind != Kind.Single && trackedHigh;
        // What the player has seen decides which of the two it is: one edge that moves is uniaxial, both is biaxial.
        s.Uniaxial = s.Double && s.LowMoves != s.HighMoves;
        s.Biaxial = s.Double && s.LowMoves && s.HighMoves;
        return s;
    }

    private void RefreshGuide()
    {
        if (guideContent == null || !guideTab.HasValue) return;

        var situation = CurrentSituation();
        var text = RefractoGuide.Page(guideTab.Value, situation);
        // Nothing to rebuild while nothing on the page has changed.
        var key = guideTab.Value + "|" + text.GetHashCode() + "|" + situation.HasStone + situation.CurrentLow.ToString("F3") + situation.CurrentHigh.ToString("F3") + situation.HasLowEdge + situation.HasHighEdge;
        if (key == guideKey) return;
        guideKey = key;

        for (var i = guideContent.childCount - 1; i >= 0; i--) Destroy(guideContent.GetChild(i).gameObject);

        // The text is cut at the markers of the pictures.
        var rest = text;
        while (rest.Length > 0)
        {
            var marker = rest.IndexOf("[[pic:", System.StringComparison.Ordinal);
            if (marker < 0)
            {
                AddGuideText(rest);
                break;
            }

            if (marker > 0) AddGuideText(rest.Substring(0, marker));
            var end = rest.IndexOf("]]", marker, System.StringComparison.Ordinal);
            if (end < 0) break;
            var id = rest.Substring(marker + 6, end - marker - 6);
            RefractoPictures.Build(id, guideContent, situation);
            rest = rest.Substring(end + 2).TrimStart('\n');
        }
    }

    private void AddGuideText(string text)
    {
        text = text.TrimEnd('\n');
        if (text.Length == 0) return;
        var go = new GameObject("Block", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(guideContent, false);
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 12;
        label.alignment = TextAnchor.UpperLeft;
        label.color = Color.white;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.lineSpacing = 1.05f;
        label.text = text;
    }

    // The small picture of a tab, drawn pixel by pixel.
    private static readonly System.Collections.Generic.Dictionary<RefractoGuide.Tab, Sprite> TabIcons = new System.Collections.Generic.Dictionary<RefractoGuide.Tab, Sprite>();

    private static Sprite MakeTabIcon(RefractoGuide.Tab tab)
    {
        if (TabIcons.TryGetValue(tab, out var cached) && cached != null) return cached;

        const int n = 16;
        var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[n * n];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);
        void Put(int x, int y, Color c) { if (x >= 0 && x < n && y >= 0 && y < n) pixels[y * n + x] = c; }
        void Hline(int x0, int x1, int y, Color c) { for (var x = x0; x <= x1; x++) Put(x, y, c); }
        void Vline(int x, int y0, int y1, Color c) { for (var y = y0; y <= y1; y++) Put(x, y, c); }
        var ink = new Color(0.90f, 0.93f, 1f, 1f);
        var gold = new Color(1f, 0.82f, 0.30f, 1f);
        var teal = new Color(0.45f, 0.85f, 0.80f, 1f);
        var dark = new Color(0.15f, 0.17f, 0.22f, 1f);

        switch (tab)
        {
            case RefractoGuide.Tab.Stones:
                // A list: three lines, each with a bullet.
                for (var row = 0; row < 3; row++)
                {
                    var y = 12 - row * 5;
                    Put(2, y, gold); Put(3, y, gold); Put(2, y + 1, gold); Put(3, y + 1, gold);
                    Hline(6, 14, y, ink); Hline(6, 14, y + 1, ink);
                }
                break;
            case RefractoGuide.Tab.OutOfLimit:
                // The scale, and an arrow that goes out at the top.
                Vline(4, 1, 14, ink);
                for (var y = 1; y <= 13; y += 3) Hline(4, 6, y, ink);
                Vline(11, 3, 12, gold);
                Put(10, 11, gold); Put(12, 11, gold); Put(9, 10, gold); Put(13, 10, gold);
                break;
            case RefractoGuide.Tab.Single:
                // The field of view cut by one shadow edge.
                for (var y = 2; y <= 13; y++) Hline(2, 13, y, y > 8 ? ink : dark);
                Hline(2, 13, 8, gold);
                break;
            case RefractoGuide.Tab.Double:
                for (var y = 2; y <= 13; y++) Hline(2, 13, y, y > 10 ? ink : (y > 6 ? new Color(0.55f, 0.58f, 0.64f, 1f) : dark));
                Hline(2, 13, 10, gold); Hline(2, 13, 6, gold);
                break;
            case RefractoGuide.Tab.Uniaxial:
                // One axis.
                Vline(8, 1, 14, teal); Vline(7, 1, 14, teal);
                Put(6, 13, teal); Put(9, 13, teal); Put(5, 12, teal); Put(10, 12, teal);
                break;
            case RefractoGuide.Tab.Biaxial:
                // Two axes opening like a V.
                for (var i = 0; i < 13; i++) { Put(3 + i / 2, 2 + i, teal); Put(13 - i / 2, 2 + i, teal); }
                break;
            default:
                // A plus and a minus.
                Hline(2, 7, 11, gold); Vline(4, 8, 13, gold); Vline(5, 8, 13, gold);
                Hline(9, 14, 4, ink); Hline(9, 14, 3, ink);
                break;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        TabIcons[tab] = sprite;
        return sprite;
    }

    // Knurled steel knob with a marker so the rotation is visible.
    private static Sprite MakeDialSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                var angle = Mathf.Atan2(dy, dx);
                var shade = r > 0.86f ? 0.45f + 0.18f * Mathf.Sin(angle * 24f)    // knurled rim
                    : r > 0.80f ? 0.22f                                            // groove
                    : 0.55f + 0.12f * (-dx - dy) * 0.5f;                           // face

                var color = new Color(shade, shade, shade * 1.05f, Mathf.Clamp01((1f - r) * center));

                // Marker notch at the top (angle = 90 degrees).
                if (r > 0.25f && r < 0.78f && Mathf.Abs(dx) < 0.07f && dy > 0f)
                    color = new Color(0.95f, 0.62f, 0.15f, color.a);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
