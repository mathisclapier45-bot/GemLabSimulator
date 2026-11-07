using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Loupe (10x at the lab, 10x or 20x from the bag, with the bag's maglite and long-wave UV lamp when owned): the gem lies under the lens. Turn it in every direction (left drag = tilt, wheel = roll), move the
// loupe over it (right drag or the arrows) and look at the inclusions inside and the defects on the surface.
public class LoupeUI : MonoBehaviour
{
    public static LoupeUI Instance { get; private set; }

    private const float PanelWidth = 620f;
    private const float PanelHeight = 512f;
    private const float LoupeCenterX = 20f;
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

    // The chamois cloth: a stone that was never wiped is covered with whitish traces, and nothing can be seen through them until it is rubbed with the cloth.
    private Button clothButton;
    private Text clothLabel;
    private bool clothMode;
    private bool fromBag;               // opened from the bag: the cloth is the one the player bought at the GemStore
    private const string ClothId = "Chamoisine";

    // The tabs beside the loupe: one for each piece of information the loupe gives (see LoupeGuide).
    private const float GuideTabSize = 44f;
    private const float GuidePageWidth = 286f;
    private GameObject guideRoot;
    private Text guideText;
    private ScrollRect guideScroll;
    private LoupeGuide.Tab? guideTab;       // the tab whose page is open (none: the page is closed)
    private GameObject guidePage;
    private readonly List<Image> guideTabImages = new List<Image>();
    private float wipe;                 // 0 = dirty, 1 = clean
    private RawImage hazeImage;
    private Texture2D hazeTexture;
    private Color32[] hazeBuffer;
    private const float WipeLength = 1100f;    // pixels of rubbing needed to clean a stone

    private GemInstance stone;
    private Quaternion orientation = Quaternion.identity;
    private Vector2 pan;
    private float explored;
    private float watched;
    private bool visualObserved;

    // `power`: magnification of the lens (10 at the lab desk; 10 or 20 for the bag loupes; 0 for no lens).
    public static bool TryOpen(float power = 10f, bool fromBag = false)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<LoupeUI>();
        }

        if (Instance == null) return false;
        Instance.loupePower = power;
        Instance.fromBag = fromBag;
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
        guideRoot.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (overlayTexture != null) Destroy(overlayTexture);
        if (hazeTexture != null) Destroy(hazeTexture);
    }

    public void Open()
    {
        stone = null;
        SetMode(false);
        // The pages of the tabs are closed each time the loupe opens: a tab has to be clicked.
        guideTab = null;
        guidePage.SetActive(false);
        UpdateGuideTabColors();
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
        uvInfo = TreatmentSignatures.Fluorescence(gem, true);
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
        clothMode = false;
        clothButton.gameObject.SetActive(!market);
        guideRoot.SetActive(!market);
        tipText.gameObject.SetActive(!market && !hasTorch && !hasUv);
        UpdateToggleColors();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    // The stone under the lens is covered with traces (the viewer of the market shows stones as they are sold, cleaned by the vendor).
    // At the desk of the laboratory the cloth lies on the table; in the field, the player needs to have bought it.
    private bool HasCloth => !fromBag || Owns(ClothId);

    private bool Dirty => stone != null && !marketMode && stone.NeedsCleaning;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (guideRoot != null) guideRoot.SetActive(false);
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
        wipe = 0f;
        clothMode = false;
        uvInfo = TreatmentSignatures.Fluorescence(gem, true);
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
        if (clothMode)
        {
            Wipe(delta.magnitude);
            return;
        }

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
        if (stone == null || explored < InspectionRotation || Dirty) return;
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
        ToolFeedbackUI.Instance?.ShowMessage("Loupe — Examen terminé : voir le résumé à droite et les onglets.");
        UpdateTexts();
        StoryState.Instance?.OnObserved(stone, InspectionKey);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || stone == null) return;

        if (Dirty) return;
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
                $"Transparence : {def.TransparencyLabel} | {stone.FormInline}");
            UpdateTexts();
            StoryState.Instance?.OnObserved(stone, ExaminationTool.MeasurementType.VisualExamination.ToString());
        }
    }

    // ---- The chamois cloth -----------------------------------------------------------------------------------

    private void ToggleCloth()
    {
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Déposez d'abord une gemme sous la loupe.");
            return;
        }
        if (!stone.NeedsCleaning)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Cette pierre est déjà propre.");
            return;
        }
        if (!HasCloth)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Vous n'avez pas de chamoisine dans votre sac : on en vend au GemStore. Au laboratoire, il y en a une sur le bureau de la loupe.");
            return;
        }

        clothMode = !clothMode;
        Sfx.Play(Sfx.Id.Click, 0.6f);
        UpdateClothButton();
    }

    private void UpdateClothButton()
    {
        if (clothButton == null) return;
        var needs = Dirty;
        clothLabel.text = !HasCloth && needs ? "Pas de chamoisine (à acheter au GemStore)" : clothMode ? "Chamoisine en main : frottez la pierre"
            : needs ? "Prendre la chamoisine (pierre sale)" : "Chamoisine (pierre propre)";
        clothButton.GetComponent<Image>().color = !HasCloth ? new Color(0.30f, 0.27f, 0.27f, 1f) : clothMode ? new Color(0.62f, 0.48f, 0.22f, 1f) : needs ? new Color(0.45f, 0.36f, 0.18f, 1f) : ButtonColor;
    }

    // The cloth rubs the stone: the traces fade as the hand moves over it.
    private void Wipe(float distance)
    {
        if (!Dirty || !HasCloth) return;

        wipe = Mathf.Min(1f, wipe + distance / WipeLength);
        Sfx.Play(Sfx.Id.Rub, 0.8f, Random.Range(0.85f, 1.15f));
        if (wipe >= 1f)
        {
            stone.NeedsCleaning = false;
            clothMode = false;
            Sfx.Play(Sfx.Id.Success, 0.5f);
            ToolFeedbackUI.Instance?.ShowMessage("La pierre est propre : vous pouvez l'examiner à la loupe.");
            Refresh();
            return;
        }
        UpdateView();
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static float ValueNoise(float x, float y, int seed)
    {
        var xi = Mathf.FloorToInt(x);
        var yi = Mathf.FloorToInt(y);
        var fx = x - xi;
        var fy = y - yi;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        var top = Mathf.Lerp(Hash01(xi, yi, seed), Hash01(xi + 1, yi, seed), fx);
        var bottom = Mathf.Lerp(Hash01(xi, yi + 1, seed), Hash01(xi + 1, yi + 1, seed), fx);
        return Mathf.Lerp(top, bottom, fy);
    }

    // The whitish traces: blotches of milky white over the stone only, which shrink and vanish as the cloth passes.
    private void UpdateHaze(float side)
    {
        UpdateClothButton();
        if (!Dirty)
        {
            hazeImage.enabled = false;
            return;
        }

        var source = view3D.Pixels;
        var size = view3D.Size;
        if (hazeTexture == null || hazeTexture.width != size)
        {
            if (hazeTexture != null) Destroy(hazeTexture);
            hazeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            hazeBuffer = new Color32[size * size];
            hazeImage.texture = hazeTexture;
        }

        var seed = (int)StoneOptics.StableHash(stone.SpecimenId ?? "") & 0x7FFFFFFF;
        var threshold = 0.42f + wipe * 0.62f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (source[i].a < 8)
                {
                    hazeBuffer[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                var n = 0.62f * ValueNoise(x / 7f, y / 7f, seed) + 0.38f * ValueNoise(x / 2.6f, y / 2.6f, seed + 7);
                var alpha = Mathf.Clamp01((n - threshold) * 5.5f) * 0.82f;
                hazeBuffer[i] = new Color32(236, 236, 230, (byte)(alpha * 255f));
            }
        }

        hazeTexture.SetPixels32(hazeBuffer);
        hazeTexture.Apply();
        hazeImage.rectTransform.sizeDelta = new Vector2(side, side);
        hazeImage.enabled = true;
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
            hazeImage.enabled = false;
            clothMode = false;
            UpdateClothButton();
            hintText.text = "Déposez une gemme sous la loupe";
        }
        else
        {
            stoneImage.enabled = true;
            hintText.text = Dirty ? (HasCloth ? "Traces blanchâtres : essuyez la pierre à la chamoisine" : "Traces blanchâtres : il faut une chamoisine") : "";
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
        featureVisibility = Dirty ? 0f : !marketMode ? ((loupeOn && !uvOn) ? 1f : 0f) : (loupeOn && !uvOn) ? (maglite ? 1f : 0.5f) : 0f;

        UpdateFeatures(half);
        UpdateHaze(side);

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
        UpdateResultText();
        RefreshGuide();
    }

    private void UpdateResultText()
    {
        if (stone == null)
        {
            resultText.text = "Aucune gemme sous la loupe.";
            return;
        }

        if (Dirty)
        {
            var advice = HasCloth
                ? "Prenez la chamoisine (bouton en bas à gauche), puis frottez la pierre en glissant dessus, avant de la regarder."
                : "Vous n'avez pas de chamoisine sur vous : achetez-en une au GemStore, ou essuyez la pierre au laboratoire, où il y en a une sur le bureau de la loupe.";
            resultText.text = "La pierre est couverte de traces blanchâtres (poussière, gras des doigts) : on ne distingue rien à travers.\n\n" + advice;
            return;
        }

        var def = stone.Definition;
        var text = marketMode ? $"Poids : {stone.CaratWeight:F2} ct\n\n" : "";
        if (visualObserved)
        {
            text += $"Éclat : {def.luster}\nCouleur : {stone.ColorName}\nTransparence : {def.TransparencyLabel}\n" +
                    stone.FormBlock +
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
        // Shifted from the centre: the inventory (right side of the screen) stays reachable for drag and drop, and the tabs of the guide fit on the left.
        rect.anchoredPosition = new Vector2(LoupeCenterX, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "Loupe ×10", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildViewport();
        BuildControls();
        BuildSidePanel();
        BuildGuide();
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

        // The whitish traces of a stone that was not wiped, on top of everything that is in the stone.
        var hazeGO = new GameObject("Haze", typeof(RectTransform), typeof(RawImage));
        hazeGO.transform.SetParent(content, false);
        var hazeRect = hazeGO.GetComponent<RectTransform>();
        hazeRect.anchorMin = hazeRect.anchorMax = new Vector2(0.5f, 0.5f);
        hazeRect.pivot = new Vector2(0.5f, 0.5f);
        hazeImage = hazeGO.GetComponent<RawImage>();
        hazeImage.raycastTarget = false;
        hazeImage.enabled = false;

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

        // The chamois cloth (third row): it has to be used on a stone before looking at it.
        clothButton = RuntimeUIKit.MakeButton("Cloth", panel.transform, "Chamoisine", 13, ButtonColor, new Vector2(AreaX, row - 36f), new Vector2(AreaSize, 30f));
        clothLabel = clothButton.GetComponentInChildren<Text>();
        clothButton.onClick.AddListener(ToggleCloth);
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

    // ---- The tabs of the loupe ---------------------------------------------------------------------------------------

    private void BuildGuide()
    {
        guideRoot = new GameObject("LoupeGuidePanel", typeof(RectTransform));
        guideRoot.transform.SetParent(transform, false);
        var rect = guideRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(GuidePageWidth + 4f + GuideTabSize, PanelHeight);
        // To the left of the loupe, with the same top: the tabs touch the loupe, the page opens on their left.
        rect.anchoredPosition = new Vector2(LoupeCenterX - PanelWidth / 2f - 2f, PanelHeight / 2f);

        // The page (closed until a tab is clicked): a dark sheet with a scrolling text.
        guidePage = new GameObject("Page", typeof(RectTransform));
        guidePage.transform.SetParent(rect, false);
        RuntimeUIKit.SetTopLeft(guidePage.GetComponent<RectTransform>(), Vector2.zero, new Vector2(GuidePageWidth, PanelHeight));
        RuntimeUIKit.MakeBox("Sheet", guidePage.transform, Vector2.zero, new Vector2(GuidePageWidth, PanelHeight), PanelColor);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(guidePage.transform, false);
        RuntimeUIKit.SetTopLeft(viewport.GetComponent<RectTransform>(), new Vector2(12f, -10f), new Vector2(GuidePageWidth - 24f, PanelHeight - 20f));
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.004f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        textGO.transform.SetParent(viewport.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        guideText = textGO.GetComponent<Text>();
        guideText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        guideText.fontSize = 12;
        guideText.alignment = TextAnchor.UpperLeft;
        guideText.color = Color.white;
        guideText.supportRichText = true;
        guideText.horizontalOverflow = HorizontalWrapMode.Wrap;
        guideText.verticalOverflow = VerticalWrapMode.Overflow;
        guideText.raycastTarget = false;
        guideText.lineSpacing = 1.05f;
        textGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        guideScroll = viewport.AddComponent<ScrollRect>();
        guideScroll.viewport = viewport.GetComponent<RectTransform>();
        guideScroll.content = textRect;
        guideScroll.horizontal = false;
        guideScroll.vertical = true;
        guideScroll.movementType = ScrollRect.MovementType.Clamped;
        guideScroll.scrollSensitivity = 28f;
        StardewKit.Skin(guidePage.transform);

        // The tabs, one under the other, each with its picture and its name.
        const float tabHeight = 52f;
        var tabX = GuidePageWidth + 4f;
        for (var i = 0; i < LoupeGuide.Tabs.Length; i++)
        {
            var tab = LoupeGuide.Tabs[i];
            var button = RuntimeUIKit.MakeButton("Tab_" + tab, rect, "", 9, ButtonColor, new Vector2(tabX, -i * (tabHeight + 5f)), new Vector2(GuideTabSize, tabHeight));
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(button.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -4f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = MakeGuideIcon(tab);
            iconImage.raycastTarget = false;

            var label = button.GetComponentInChildren<Text>();
            label.text = LoupeGuide.ShortName(tab);
            label.fontSize = 9;
            label.alignment = TextAnchor.LowerCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(0f, 2f);
            labelRect.offsetMax = Vector2.zero;

            guideTabImages.Add(button.GetComponent<Image>());
            var captured = tab;
            button.onClick.AddListener(() => ToggleGuideTab(captured));
        }

        StardewKit.Skin(rect);
        guidePage.SetActive(false);
        UpdateGuideTabColors();
    }

    // Clicking a tab opens its page; clicking the open tab closes the page.
    private void ToggleGuideTab(LoupeGuide.Tab tab)
    {
        Sfx.Play(Sfx.Id.Click, 0.5f);
        guideTab = guideTab.HasValue && guideTab.Value == tab ? (LoupeGuide.Tab?)null : tab;
        guidePage.SetActive(guideTab.HasValue);
        UpdateGuideTabColors();
        RefreshGuide();
        if (guideScroll != null) guideScroll.verticalNormalizedPosition = 1f;
    }

    private void UpdateGuideTabColors()
    {
        for (var i = 0; i < guideTabImages.Count; i++)
            guideTabImages[i].color = guideTab.HasValue && LoupeGuide.Tabs[i] == guideTab.Value ? new Color(0.62f, 0.46f, 0.20f, 1f) : ButtonColor;
    }

    private void RefreshGuide()
    {
        if (guideText == null || !guideTab.HasValue) return;
        var inspected = stone != null && !marketMode && stone.HasObserved(InspectionKey);
        guideText.text = LoupeGuide.Page(guideTab.Value, stone, stone != null && visualObserved, inspected, Dirty);
    }

    // Is the point inside a five-pointed star pointing up, with the given outer and inner radii?
    private static bool InStar(float x, float y, float outer, float inner)
    {
        var points = new Vector2[10];
        for (var i = 0; i < 10; i++)
        {
            var angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            var radius = i % 2 == 0 ? outer : inner;
            points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        var inside = false;
        for (int i = 0, j = 9; i < 10; j = i++)
        {
            if ((points[i].y > y) != (points[j].y > y) && x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                inside = !inside;
        }
        return inside;
    }

    // The small picture of a tab, drawn pixel by pixel.
    private static readonly Dictionary<LoupeGuide.Tab, Sprite> GuideIcons = new Dictionary<LoupeGuide.Tab, Sprite>();

    private static Sprite MakeGuideIcon(LoupeGuide.Tab tab)
    {
        if (GuideIcons.TryGetValue(tab, out var cached) && cached != null) return cached;

        const int n = 16;
        var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color(0f, 0f, 0f, 0f);
        var c = (n - 1) / 2f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var color = clear;
                switch (tab)
                {
                    case LoupeGuide.Tab.Luster:
                    {
                        // A small five-pointed star (the outline of the points is tested against the lines between the tips and the inner corners).
                        if (InStar(dx, dy + 0.4f, 7.6f, 3.1f)) color = InStar(dx, dy + 0.4f, 3.6f, 1.5f) ? new Color(1f, 0.97f, 0.7f, 1f) : new Color(1f, 0.82f, 0.25f, 1f);
                        break;
                    }
                    case LoupeGuide.Tab.Color:
                    {
                        // A disc of colours: the hue turns around it.
                        var r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r <= 7.2f)
                        {
                            var hue = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) % 1f;
                            color = Color.HSVToRGB(hue, Mathf.Clamp01(0.35f + r / 9f), 1f);
                        }
                        break;
                    }
                    case LoupeGuide.Tab.Transparency:
                    {
                        // A pane of glass: a pale frame, a transparent middle, two glints.
                        var edge = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                        if (edge <= 7.2f) color = edge >= 6f ? new Color(0.85f, 0.95f, 1f, 1f) : new Color(0.6f, 0.85f, 1f, 0.28f);
                        if (edge < 6f && (Mathf.Abs(dx - dy - 1f) < 0.8f && dy > -4f && dy < 4f || Mathf.Abs(dx - dy + 3f) < 0.6f && dy > -2f && dy < 2f)) color = new Color(1f, 1f, 1f, 0.85f);
                        break;
                    }
                    case LoupeGuide.Tab.Shape:
                    {
                        // A cut stone seen from the side: a flat top, a pointed bottom, a facet line.
                        var halfTop = 7.2f - Mathf.Max(0f, dy - 0.5f) * 1.5f;
                        var top = dy > -2f && dy <= 3.2f && Mathf.Abs(dx) <= halfTop;
                        var bottom = dy <= -2f && dy >= -7.2f && Mathf.Abs(dx) <= (dy + 7.2f) * 1.0f;
                        if (top || bottom)
                        {
                            var line = Mathf.Abs(dy + 2f) < 0.6f || (bottom && Mathf.Abs(dx) < 0.6f);
                            color = line ? new Color(1f, 1f, 1f, 1f) : new Color(0.55f, 0.85f, 1f, 1f);
                        }
                        break;
                    }
                    case LoupeGuide.Tab.Inclusions:
                    {
                        // A stone with needles and dots inside.
                        var r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r <= 7.4f) color = r >= 6.2f ? new Color(0.85f, 0.95f, 1f, 1f) : new Color(0.50f, 0.78f, 0.95f, 0.55f);
                        if (r < 6.2f && (Mathf.Abs(dx + dy * 1.5f + 1f) < 0.7f || Mathf.Abs(dx + dy * 1.5f - 3f) < 0.6f && dy < 3f && dy > -3f)) color = new Color(0.95f, 0.85f, 0.5f, 1f);
                        if (r < 6.2f && ((int)Mathf.Round(dx) == 3 && (int)Mathf.Round(dy) == -3 || (int)Mathf.Round(dx) == -4 && (int)Mathf.Round(dy) == 3)) color = new Color(0.25f, 0.25f, 0.35f, 1f);
                        break;
                    }
                    default:
                    {
                        // A facet scratched: a pale square with dark lines across it.
                        var edge = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                        if (edge <= 7.2f) color = new Color(0.78f, 0.92f, 1f, 1f);
                        if (edge <= 7.2f && (Mathf.Abs(dx - dy * 0.6f + 1f) < 0.55f || Mathf.Abs(dx - dy * 0.6f - 4f) < 0.5f && dy > -2f)) color = new Color(0.30f, 0.34f, 0.46f, 1f);
                        break;
                    }
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        GuideIcons[tab] = sprite;
        return sprite;
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
