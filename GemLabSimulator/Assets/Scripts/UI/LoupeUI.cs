using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Loupe (10x at the lab, 10x or 20x from the bag, with the bag's maglite and long-wave UV lamp when owned): the gem lies under the lens. Turn it in every direction (left drag = tilt, wheel = roll), move the
// loupe over it (right drag or the arrows) and look at the inclusions inside and the defects on the surface.
public class LoupeUI : MonoBehaviour
{
    public static LoupeUI Instance { get; private set; }

    private const float PanelWidth = 620f;
    private const float PanelHeight = 480f;
    private const float AreaX = 20f;
    private const float AreaY = -44f;
    private const float AreaSize = 330f;
    private const float HoleRadius = 150f;
    private float PxPerMm => loupeOn ? 3.4f * (marketMode ? 10f : loupePower) : 20f;   // magnification of the view (x1 without a lens)
    private const float ObservationTime = 2.5f;
    private const float InspectionRotation = 200f;     // degrees turned before the loupe findings are logged
    private const string InspectionKey = "LoupeInspection";

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color StageColor = new Color(0.20f, 0.21f, 0.24f, 1f);

    private class FeatureView
    {
        public LoupeFeature Feature;
        public List<InclusionPart> Parts;
    }

    private GameObject panel;
    private RectTransform content;       // everything that moves with the loupe
    private RectTransform stoneRect;
    private Image stoneImage;
    private GemView3D view3D;
    private readonly List<FeatureView> features = new List<FeatureView>();
    private RawImage overlayImage;       // the stone's picture with its inclusions painted inside it
    private Texture2D overlayTexture;
    private Color32[] overlayBuffer;
    private Color32[] overlayBase;           // the stone alone, before its inclusions
    private FeaturePainter overlayPainter;
    private Text hintText;
    private Text resultText;
    private Text rotationText;
    private Text titleText;
    private Text magnificationText;

    // Market mode: the same viewer, opened on a vendor's stone with a loupe, a torch and a long-wave UV lamp.
    private bool marketMode;
    private System.Action onMarketClose;
    private bool loupeOn = true;
    private float loupePower = 10f;     // lens magnification in lab/bag mode; 0 = no lens (naked eye)
    private const string MagliteId = "Maglite";
    private const string UvLampId = "LampeUVLongs";
    private bool maglite;
    private bool uvOn;
    private bool inspectedLocal;
    private bool uvSeen;
    private FluorescenceInfo uvInfo;
    private GemView3D view3DLit;
    private Image litImage;
    private RectTransform litRect;
    private Image lightGlow;
    private Image uvTint;
    private Button loupeToggle;
    private Button magliteToggle;
    private Button uvToggle;
    private Button removeButton;
    private Text tipText;
    private float featureVisibility = 1f;

    private GemInstance stone;
    private Quaternion orientation = Quaternion.identity;
    private Vector2 pan;
    private float explored;
    private float watched;
    private bool visualObserved;

    // `power`: magnification of the lens (10 at the lab desk; 10 or 20 for the bag loupes; 0 for no lens).
    public static bool TryOpen(float power = 10f)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<LoupeUI>();
        }

        if (Instance == null) return false;
        Instance.loupePower = power;
        Instance.Open();
        return true;
    }

    private static bool Owns(string toolId)
    {
        var tools = PlayerOwnedTools.Ensure();
        return tools != null && tools.Has(toolId);
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
        if (overlayTexture != null) Destroy(overlayTexture);
    }

    public void Open()
    {
        stone = null;
        SetMode(false);
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    // Opens the viewer on a stone that is not in the inventory (a vendor's stone). `onClose` brings the vendor back.
    public static void OpenForMarket(GemInstance gem, System.Action onClose)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                onClose?.Invoke();
                return;
            }
            canvas.AddComponent<LoupeUI>();
        }

        Instance.OpenMarket(gem, onClose);
    }

    private void OpenMarket(GemInstance gem, System.Action onClose)
    {
        SetMode(true);
        onMarketClose = onClose;
        stone = gem;
        orientation = Quaternion.identity;
        pan = Vector2.zero;
        explored = 0f;
        watched = 0f;
        visualObserved = false;
        inspectedLocal = false;
        uvSeen = false;
        uvInfo = FluorescenceInfo.ForGem(gem.Definition, true);
        BuildFeatureViews();
        panel.SetActive(true);
        Refresh();
    }

    // Switches between the lab loupe (stone dropped from the inventory, always magnified) and the market viewer.
    private void SetMode(bool market)
    {
        marketMode = market;
        loupeOn = !market && loupePower > 0f;
        maglite = false;
        uvOn = false;
        titleText.text = market ? "Examen de la pierre" : loupePower > 0f ? $"Loupe ×{loupePower:F0}" : "Examen à l'œil nu";
        // Leave no market lighting effect behind when switching back to the lab loupe.
        litImage.enabled = false;
        lightGlow.color = new Color(1f, 1f, 1f, 0f);
        uvTint.color = new Color(0f, 0f, 0f, 0f);
        stoneImage.color = Color.white;
        // Outside the market, the torch and the UV lamp are there only if the player bought them (they live in the bag).
        var hasTorch = market || Owns(MagliteId);
        var hasUv = market || Owns(UvLampId);
        loupeToggle.gameObject.SetActive(market);
        magliteToggle.gameObject.SetActive(hasTorch);
        uvToggle.gameObject.SetActive(hasUv);
        removeButton.gameObject.SetActive(!market);
        tipText.gameObject.SetActive(!market && !hasTorch && !hasUv);
        UpdateToggleColors();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();

        if (marketMode)
        {
            var callback = onMarketClose;
            onMarketClose = null;
            marketMode = false;
            stone = null;
            ClearFeatureViews();
            callback?.Invoke();
        }
    }

    private void UpdateToggleColors()
    {
        var on = new Color(0.30f, 0.55f, 0.35f, 1f);
        loupeToggle.GetComponent<Image>().color = loupeOn ? on : ButtonColor;
        magliteToggle.GetComponent<Image>().color = maglite ? on : ButtonColor;
        uvToggle.GetComponent<Image>().color = uvOn ? on : ButtonColor;
        magnificationText.text = loupeOn ? $"×{(marketMode ? 10f : loupePower):F0}" : "×1";
    }

    private void ToggleLoupe()
    {
        loupeOn = !loupeOn;
        pan = Vector2.zero;
        UpdateToggleColors();
        UpdateView();
    }

    private void ToggleMaglite()
    {
        maglite = !maglite;
        if (maglite) uvOn = false;
        UpdateToggleColors();
        UpdateView();
    }

    private void ToggleUv()
    {
        uvOn = !uvOn;
        if (uvOn)
        {
            maglite = false;
            uvSeen = true;
            // With the bag lamp the fluorescence is recorded like a lab measurement.
            if (!marketMode && stone != null)
                stone.RecordObservation(ExaminationTool.MeasurementType.FluorescenceLongWave.ToString());
            UpdateTexts();
        }
        UpdateToggleColors();
        UpdateView();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        uvInfo = FluorescenceInfo.ForGem(gem.Definition, true);
        uvSeen = uvOn;
        orientation = Quaternion.identity;
        pan = Vector2.zero;
        explored = 0f;
        watched = 0f;
        visualObserved = gem.HasObserved(ExaminationTool.MeasurementType.VisualExamination.ToString());
        BuildFeatureViews();
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void ResetView()
    {
        orientation = Quaternion.identity;
        pan = Vector2.zero;
        UpdateView();
    }

    private void OnTilt(Vector2 delta)
    {
        if (stone == null) return;

        var yaw = Quaternion.AngleAxis(-delta.x * 0.7f, Vector3.up);
        var pitch = Quaternion.AngleAxis(delta.y * 0.7f, Vector3.right);
        orientation = yaw * pitch * orientation;
        AfterRotation(delta.magnitude * 0.7f);
    }

    private void Roll(float degrees)
    {
        if (stone == null) return;

        orientation = Quaternion.AngleAxis(degrees, Vector3.forward) * orientation;
        AfterRotation(Mathf.Abs(degrees));
    }

    private void AfterRotation(float degrees)
    {
        explored += degrees;
        CheckInspection();
        UpdateView();
    }

    private void OnPan(Vector2 delta)
    {
        if (stone == null) return;
        Pan(delta);
    }

    private void Pan(Vector2 delta)
    {
        // The loupe moves, so the view of the stone moves the other way; keep the lens over the stone.
        var limit = Mathf.Max(stone.LengthMm, stone.WidthMm) * PxPerMm * 0.5f + 20f;
        pan = Vector2.ClampMagnitude(pan + delta, limit);
        UpdateView();
    }

    private void CheckInspection()
    {
        if (stone == null || explored < InspectionRotation) return;
        // Without a lens nothing small can be seen, so the examination does not count.
        if (!marketMode && !loupeOn) return;

        if (marketMode)
        {
            if (inspectedLocal) return;
            inspectedLocal = true;
            UpdateTexts();
            return;
        }

        if (stone.HasObserved(InspectionKey)) return;

        stone.RecordObservation(InspectionKey);
        ToolFeedbackUI.Instance?.ShowMessage("Loupe — Examen terminé : voir le résumé à droite.");
        UpdateTexts();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || stone == null) return;

        watched += Time.deltaTime;
        if (!visualObserved && watched >= ObservationTime)
        {
            visualObserved = true;
            if (marketMode)
            {
                UpdateTexts();
                return;
            }

            var def = stone.Definition;
            stone.RecordObservation(ExaminationTool.MeasurementType.VisualExamination.ToString());
            ToolFeedbackUI.Instance?.ShowMessage(
                $"Loupe — Éclat : {def.luster} | Couleur : {stone.ColorName} | " +
                $"Transparence : {def.TransparencyLabel} | Forme : {stone.ShapeName} | Style de taille : {stone.CutStyleName}");
            UpdateTexts();
        }
    }

    // ---- Feature drawing -------------------------------------------------------------------------------------

    private void ClearFeatureViews()
    {
        features.Clear();
    }

    private void BuildFeatureViews()
    {
        ClearFeatureViews();
        if (stone == null) return;

        var index = 0;
        foreach (var feature in LoupeFeatures.Generate(stone))
        {
            var rng = new System.Random((int)StoneOptics.StableHash((stone.SpecimenId ?? "") + "shape" + index));
            index++;
            features.Add(new FeatureView { Feature = feature, Parts = InclusionShapes.Build(feature, rng) });
        }
    }

    // ---- Display ---------------------------------------------------------------------------------------------

    private void Refresh()
    {
        if (stone != null && !marketMode && !ContainsGem(stone))
        {
            stone = null;
            ClearFeatureViews();
        }

        if (stone == null)
        {
            stoneImage.enabled = false;
            overlayImage.enabled = false;
            hintText.text = "Déposez une gemme sous la loupe";
        }
        else
        {
            stoneImage.enabled = true;
            hintText.text = "";
        }

        UpdateView();
        UpdateTexts();
    }

    private void UpdateView()
    {
        if (stone == null)
        {
            rotationText.text = "";
            return;
        }

        // Half sizes of the stone in screen pixels (the features below use them too).
        var half = new Vector3(stone.LengthMm, stone.WidthMm, Mathf.Max(stone.DepthMm, 0.5f)) * (PxPerMm * 0.5f);

        GemView3D.Colors(stone, out var body, out var facet);
        var halfMm = new Vector3(stone.LengthMm, stone.WidthMm, Mathf.Max(stone.DepthMm, 0.5f)) * 0.5f;
        var mesh = GemView3D.MeshFor(stone);
        var uvMode = uvOn;

        // Under UV the stone itself is dark; its luminescence is a second, tinted render on top.
        view3D.Draw(mesh, halfMm, orientation, uvMode ? GemPixelArt.Dim(body, 0.12f) : body, uvMode ? GemPixelArt.Dim(facet, 0.18f) : facet, stone.Definition.Diaphaneity, uvMode ? 0 : stone.AsterismRays);

        var side = Mathf.Max(half.x, half.y, half.z) * 2f / GemView3D.FitFraction;
        stoneRect.sizeDelta = new Vector2(side, side);
        stoneRect.localEulerAngles = Vector3.zero;
        content.anchoredPosition = -pan;

        // Lighting (market): torch brightens the stone and the inclusions, UV shows its fluorescence, nothing = dim room.
        stoneImage.color = !marketMode || maglite || uvMode ? Color.white : new Color(0.68f, 0.68f, 0.74f, 1f);
        lightGlow.color = maglite ? new Color(1f, 0.93f, 0.7f, 0.5f) : new Color(1f, 1f, 1f, 0f);
        lightGlow.rectTransform.sizeDelta = new Vector2(side * 1.5f, side * 1.5f);
        uvTint.color = uvMode ? new Color(0.45f, 0.2f, 0.9f, 0.28f) : new Color(0f, 0f, 0f, 0f);

        litImage.enabled = uvMode && uvInfo.Glows;
        if (litImage.enabled)
        {
            var lit = uvInfo.Color;
            view3DLit.Draw(mesh, halfMm, orientation, lit, Color.Lerp(lit, Color.white, 0.5f));
            litRect.sizeDelta = new Vector2(side, side);
            litImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(uvInfo.Intensity));
        }

        // Inclusions and surface defects need the loupe; the torch makes them stand out, UV hides them.
        featureVisibility = !marketMode ? ((loupeOn && !uvOn) ? 1f : 0f) : (loupeOn && !uvOn) ? (maglite ? 1f : 0.5f) : 0f;

        UpdateFeatures(half);

        rotationText.text = $"Parcouru : {Mathf.Min(explored, 360f):F0}° / 360°";
    }

    private void EnsureOverlay(int size)
    {
        if (overlayTexture != null && overlayTexture.width == size) return;

        if (overlayTexture != null) Destroy(overlayTexture);
        overlayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        overlayBase = new Color32[size * size];
        overlayBuffer = new Color32[size * size];
        overlayPainter = new FeaturePainter(overlayBuffer, size, size, FeaturePainter.Mode.Composite);
        overlayImage.texture = overlayTexture;
    }

    // The inclusions are painted into the picture of the stone itself, so they are really inside it: they only show where
    // there is stone, deeper ones fade into the body color, and the facets' shade and glints stay in front of them.
    // Surface defects (scratches, chips...) are painted last, on top.
    private void UpdateFeatures(Vector3 half)
    {
        if (featureVisibility <= 0f || stone == null)
        {
            overlayImage.enabled = false;
            stoneImage.enabled = stone != null;
            return;
        }

        var side = Mathf.Max(half.x, half.y, half.z) * 2f / GemView3D.FitFraction;
        var size = Mathf.Clamp(Mathf.CeilToInt(side), 64, 512);
        EnsureOverlay(size);
        var ratio = size / side;
        overlayImage.rectTransform.sizeDelta = new Vector2(side, side);
        overlayImage.color = stoneImage.color;

        // The stone as drawn, enlarged to the working size.
        var source = view3D.Pixels;
        var sourceSize = view3D.Size;
        for (var y = 0; y < size; y++)
        {
            var row = (y * sourceSize / size) * sourceSize;
            for (var x = 0; x < size; x++) overlayBase[y * size + x] = source[row + x * sourceSize / size];
        }
        System.Array.Copy(overlayBase, overlayBuffer, overlayBase.Length);

        GemView3D.Colors(stone, out var body, out _);
        var host = Color.Lerp(Color.white, body, 0.30f);
        var pxPerMm = PxPerMm * ratio;
        var hideInside = stone.IsRough && !stone.HasWindow;

        for (var pass = 0; pass < 2; pass++)
        {
            var surfacePass = pass == 1;
            foreach (var view in features)
            {
                var f = view.Feature;
                if (f.External != surfacePass) continue;

                Vector2 center;
                float depth;    // 0 = at the back of the stone, 1 = at the front

                // A rough surface scatters light: the inside of an unwindowed rough stone cannot be seen.
                if (!f.External && hideInside) continue;

                if (f.External)
                {
                    var worldNormal = orientation * f.Position;
                    if (worldNormal.z <= 0.12f) continue;

                    var surface = orientation * Vector3.Scale(f.Position, half);
                    center = new Vector2(surface.x, surface.y);
                    depth = Mathf.Clamp01(worldNormal.z * 1.6f);
                }
                else
                {
                    var position = orientation * Vector3.Scale(f.Position, half);
                    center = new Vector2(position.x, position.y);
                    depth = Mathf.InverseLerp(-half.z, half.z, position.z);
                }

                // In-plane direction and foreshortening of an elongated feature.
                var projected = orientation * f.Direction;
                var along = new Vector2(projected.x, projected.y);
                var foreshortening = Mathf.Max(0.35f, along.magnitude);
                along = along.sqrMagnitude < 1e-6f ? Vector2.right : along.normalized;
                var across = new Vector2(-along.y, along.x);

                // Seen through the stone, a deep inclusion is fainter and takes on the color of the body.
                var tint = f.External ? host : Color.Lerp(host, body, (1f - depth) * 0.55f);
                var alpha = (f.External ? depth : Mathf.Lerp(0.35f, 1f, depth)) * featureVisibility;
                var screen = center * ratio + new Vector2(size * 0.5f, size * 0.5f);
                foreach (var part in view.Parts)
                    overlayPainter.DrawPart(part, f.External, screen, along * foreshortening * pxPerMm, across * pxPerMm, alpha, 0.8f, false, tint, pxPerMm);
            }

            if (pass == 0) ShadeInclusions(size);
        }

        overlayTexture.SetPixels32(overlayBuffer);
        overlayTexture.Apply();
        overlayImage.enabled = true;
        stoneImage.enabled = false;
    }

    // What the inclusions added to the picture is seen through the facets: dark facets hide it more, and the bright
    // glints of the crown stay in front of it.
    private void ShadeInclusions(int size)
    {
        for (var i = 0; i < size * size; i++)
        {
            var b = overlayBase[i];
            var p = overlayBuffer[i];
            if (p.r == b.r && p.g == b.g && p.b == b.b) continue;

            var luma = (b.r * 0.30f + b.g * 0.59f + b.b * 0.11f) / 255f;
            var k = (0.45f + 0.55f * luma) * (luma > 0.92f ? 0.4f : 1f);
            overlayBuffer[i] = new Color32(
                (byte)Mathf.Clamp(b.r + (p.r - b.r) * k, 0f, 255f),
                (byte)Mathf.Clamp(b.g + (p.g - b.g) * k, 0f, 255f),
                (byte)Mathf.Clamp(b.b + (p.b - b.b) * k, 0f, 255f),
                p.a);
        }
    }

    private void UpdateTexts()
    {
        if (stone == null)
        {
            resultText.text = "Aucune gemme sous la loupe.";
            return;
        }

        var def = stone.Definition;
        var text = marketMode ? $"Poids : {stone.CaratWeight:F2} ct\n\n" : "";
        if (visualObserved)
        {
            text += $"Éclat : {def.luster}\nCouleur : {stone.ColorName}\nTransparence : {def.TransparencyLabel}\n" +
                    $"Forme : {stone.ShapeName}\nTaille : {stone.CutStyleName}\n" +
                    (stone.FacetSummary != null ? $"Facettes : {stone.FacetSummary}\n" : "") + "\n";
        }

        if (marketMode ? inspectedLocal : stone.HasObserved(InspectionKey))
        {
            text += (stone.IsRough && !stone.HasWindow
                ? "La surface brute diffuse la lumière : on ne voit pas l'intérieur (il faudrait une fenêtre polie).\n"
                : LoupeFeatures.InclusionSummary(stone) + "\n");
            foreach (var line in LoupeFeatures.ExternalDefectLines(stone)) text += "• " + line + "\n";
        }
        else
        {
            text += marketMode
                ? "Tournez la pierre dans tous les sens. Avec la loupe, cherchez les inclusions et les défauts de surface ; la maglite les fait ressortir."
                : "Tournez la gemme et déplacez la loupe pour repérer les inclusions et les défauts de surface.";
        }

        if (uvSeen)
            text += $"\n\nUV longs : {def.fluorescenceLongWave}";

        resultText.text = text.TrimEnd('\n');
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

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("LoupePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "Loupe ×10", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildViewport();
        BuildControls();
        BuildSidePanel();
    }

    private void BuildViewport()
    {
        // Stage under the loupe; the whole area takes drops and drags.
        var area = RuntimeUIKit.MakeBox("Area", panel.transform, new Vector2(AreaX, AreaY), new Vector2(AreaSize, AreaSize), StageColor);
        area.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        var drag = area.gameObject.AddComponent<DichroscopeDragArea>();
        drag.OnDragDelta = OnTilt;
        drag.OnRoll = Roll;
        drag.OnPanDelta = OnPan;

        // Everything inside the lens is clipped to the area, then masked by the round aperture.
        var clip = new GameObject("Clip", typeof(RectTransform), typeof(RectMask2D));
        clip.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(clip.GetComponent<RectTransform>(), new Vector2(AreaX, AreaY), new Vector2(AreaSize, AreaSize));

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(clip.transform, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1f, 1f);

        var stoneGO = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGO.transform.SetParent(content, false);
        stoneRect = stoneGO.GetComponent<RectTransform>();
        stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        stoneRect.pivot = new Vector2(0.5f, 0.5f);
        stoneImage = stoneGO.GetComponent<Image>();
        stoneImage.raycastTarget = false;
        stoneImage.enabled = false;

        // The stone is drawn in 3D pixel art, so it has real volume when turned.
        view3D = new GemView3D(64);
        stoneImage.sprite = view3D.Sprite;

        // The picture of the stone with its inclusions painted inside it (replaces the plain stone while they show).
        var overlayGO = new GameObject("Inclusions", typeof(RectTransform), typeof(RawImage));
        overlayGO.transform.SetParent(content, false);
        var overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayImage = overlayGO.GetComponent<RawImage>();
        overlayImage.raycastTarget = false;
        overlayImage.enabled = false;

        // Torch glow behind the stone, UV luminescence on top of it.
        var glowGO = new GameObject("TorchGlow", typeof(RectTransform), typeof(Image));
        glowGO.transform.SetParent(content, false);
        glowGO.transform.SetAsFirstSibling();
        var glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        lightGlow = glowGO.GetComponent<Image>();
        lightGlow.sprite = RuntimeUIKit.Glow;
        lightGlow.raycastTarget = false;
        lightGlow.color = new Color(1f, 1f, 1f, 0f);

        var litGO = new GameObject("StoneLit", typeof(RectTransform), typeof(Image));
        litGO.transform.SetParent(content, false);
        litRect = litGO.GetComponent<RectTransform>();
        litRect.anchorMin = litRect.anchorMax = new Vector2(0.5f, 0.5f);
        litRect.pivot = new Vector2(0.5f, 0.5f);
        litImage = litGO.GetComponent<Image>();
        view3DLit = new GemView3D(64);
        litImage.sprite = view3DLit.Sprite;
        litImage.raycastTarget = false;
        litImage.enabled = false;

        var tintGO = new GameObject("UvTint", typeof(RectTransform), typeof(Image));
        tintGO.transform.SetParent(clip.transform, false);
        var tintRect = tintGO.GetComponent<RectTransform>();
        tintRect.anchorMin = Vector2.zero;
        tintRect.anchorMax = Vector2.one;
        tintRect.offsetMin = Vector2.zero;
        tintRect.offsetMax = Vector2.zero;
        uvTint = tintGO.GetComponent<Image>();
        uvTint.raycastTarget = false;
        uvTint.color = new Color(0f, 0f, 0f, 0f);

        // Loupe body: dark plate with a round hole, plus a thin ring.
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(frame.GetComponent<RectTransform>(), new Vector2(AreaX, AreaY), new Vector2(AreaSize, AreaSize));
        var frameImage = frame.GetComponent<Image>();
        frameImage.sprite = MakeApertureSprite(256, HoleRadius / (AreaSize / 2f));
        frameImage.raycastTarget = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.78f),
            new Vector2(AreaX, AreaY - AreaSize / 2f - 8f), new Vector2(AreaSize, 18f));
        magnificationText = RuntimeUIKit.MakeText("Magnification", panel.transform, "×10", 14, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f),
            new Vector2(AreaX + 10f, AreaY - 6f), new Vector2(60f, 20f));
        magnificationText.fontStyle = FontStyle.Bold;
    }

    private void BuildControls()
    {
        var top = AreaY - AreaSize - 8f;

        var rollLeft = RuntimeUIKit.MakeButton("RollLeft", panel.transform, "Rouler <", 12, ButtonColor, new Vector2(AreaX, top), new Vector2(76f, 28f));
        rollLeft.onClick.AddListener(() => Roll(15f));
        var rollRight = RuntimeUIKit.MakeButton("RollRight", panel.transform, "Rouler >", 12, ButtonColor, new Vector2(AreaX + 82f, top), new Vector2(76f, 28f));
        rollRight.onClick.AddListener(() => Roll(-15f));
        var reset = RuntimeUIKit.MakeButton("Reset", panel.transform, "Remettre à plat", 12, ButtonColor, new Vector2(AreaX + 164f, top), new Vector2(110f, 28f));
        reset.onClick.AddListener(ResetView);

        // Moving the loupe over the stone (second row).
        var row = top - 36f;
        RuntimeUIKit.MakeText("PanLabel", panel.transform, "Déplacer la loupe :", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.67f, 0.72f),
            new Vector2(AreaX, row + 6f), new Vector2(130f, 16f));
        var step = 30f;
        MakePanButton("PanLeft", "<", new Vector2(AreaX + 140f, row), new Vector2(-step, 0f));
        MakePanButton("PanUp", "^", new Vector2(AreaX + 174f, row), new Vector2(0f, step));
        MakePanButton("PanDown", "v", new Vector2(AreaX + 208f, row), new Vector2(0f, -step));
        MakePanButton("PanRight", ">", new Vector2(AreaX + 242f, row), new Vector2(step, 0f));
    }

    private void MakePanButton(string name, string label, Vector2 position, Vector2 delta)
    {
        var button = RuntimeUIKit.MakeButton(name, panel.transform, label, 12, ButtonColor, position, new Vector2(28f, 24f));
        button.onClick.AddListener(() => { if (stone != null) Pan(delta); });
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("Header", panel.transform, "Ce que l'on voit", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(380f, -44f), new Vector2(220f, 20f));
        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(376f, -68f), new Vector2(234f, 220f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(386f, -74f), new Vector2(216f, 208f));

        rotationText = RuntimeUIKit.MakeText("Rotation", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(380f, -296f), new Vector2(220f, 16f));
        tipText = RuntimeUIKit.MakeText("Tip", panel.transform,
            "Clic gauche + glisser : incliner la gemme. Molette : la faire pivoter à plat. Clic droit + glisser ou flèches : déplacer la loupe.",
            11, TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f), new Vector2(380f, -318f), new Vector2(226f, 56f));

        removeButton = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor,
            new Vector2(380f, -392f), new Vector2(226f, 28f));
        removeButton.onClick.AddListener(ClearStone);

        // Market viewer tools: loupe, torch (reflected light) and long-wave UV.
        loupeToggle = RuntimeUIKit.MakeButton("LoupeToggle", panel.transform, "Loupe ×10", 12, ButtonColor, new Vector2(380f, -318f), new Vector2(110f, 28f));
        loupeToggle.onClick.AddListener(ToggleLoupe);
        magliteToggle = RuntimeUIKit.MakeButton("MagliteToggle", panel.transform, "Maglite", 12, ButtonColor, new Vector2(496f, -318f), new Vector2(110f, 28f));
        magliteToggle.onClick.AddListener(ToggleMaglite);
        uvToggle = RuntimeUIKit.MakeButton("UvToggle", panel.transform, "UV longs (365 nm)", 12, ButtonColor, new Vector2(380f, -352f), new Vector2(226f, 28f));
        uvToggle.onClick.AddListener(ToggleUv);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonColor,
            new Vector2(380f, -426f), new Vector2(226f, 28f));
        close.onClick.AddListener(Close);
    }

    // Dark plate with a round aperture and a thin metal ring around it.
    private static Sprite MakeApertureSprite(int size, float holeRatio)
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
                var alpha = Mathf.Clamp01((r - holeRatio) * center + 0.5f);
                var ring = r > holeRatio && r < holeRatio + 0.045f;
                var shade = ring ? 0.45f + 0.15f * (-dx - dy) * 0.5f : 0.10f;
                texture.SetPixel(x, y, new Color(shade, shade, shade * 1.05f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
