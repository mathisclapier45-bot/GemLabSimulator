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
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
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
            nBeta = nAlpha + def.birefringence * Mathf.Lerp(0.3f, 0.7f, Unit(speciesHash, 1));
        }

        fixedEdgeIsLow = Unit(speciesHash, 2) > 0.5f;
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
        stone = gem;
        SetupOptics(gem);
        ResetTracking();
        Refresh();

        if (!trackedLow)
        {
            stone.RecordObservation("RefractiveIndexOTL");
            ToolFeedbackUI.Instance?.ShowMessage("Réfractomètre — Indice hors limite (> 1.81) : aucune limite d'ombre.");
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
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = "Déposez une gemme sur l'hémicylindre";
        }
        else
        {
            gemImage.enabled = true;
            gemImage.sprite = stone.DisplaySprite;
            hintText.text = "";
        }

        UpdateView();
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
        statusText.text = visible == 0
            ? "Hors limite (OTL) : aucune limite d'ombre visible."
            : visible == 1
                ? "1 limite d'ombre visible sur l'échelle."
                : "2 limites d'ombre visibles sur l'échelle.";
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
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-130f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Réfractomètre", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildHemicylinder();
        BuildEyepiece();
        BuildDial();
        BuildReadouts();

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
