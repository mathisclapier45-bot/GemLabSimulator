using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Gemological microscope: 10x / 20x / 40x objectives (+ 60x once bought at the GemStore), reflected light or darkfield, fine focus through the
// depth of the stone, stage movement, and a view drawn in detail (inclusions, growth structure, surface
// defects). Rough stones need a polished window first. With the microscope camera (GemStore) the view can be
// photographed and sent to the lab computer.
public class MicroscopeUI : MonoBehaviour
{
    public static MicroscopeUI Instance { get; private set; }

    private const float PanelWidth = 640f;
    private const float PanelHeight = 500f;
    private const float AreaX = 20f;
    private const float AreaY = -44f;
    private const int ViewSize = 300;
    private const float HoleRadius = 148f;
    private const float BasePxPerMm = 34f;              // 10x
    private const float ObservationTime = 3f;

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color ButtonOn = new Color(0.30f, 0.55f, 0.35f, 1f);

    private enum Lighting { Reflected, Darkfield }

    private class MicroFeature
    {
        public LoupeFeature Feature;
        public List<InclusionPart> Parts;
    }

    private GameObject panel;
    private Image viewImage;
    private Texture2D viewTexture;
    private Color32[] buffer;
    private FeaturePainter painter;
    private float[] coverageMap;         // how much of each pixel is stone: inclusions never show outside it
    private GemView3D miniMap;
    private RectTransform miniRect;
    private Image fovMarker;
    private Text hintText;
    private Text resultText;
    private Text focusText;
    private Text magnificationText;
    private readonly Button[] objectiveButtons = new Button[4];
    private readonly Button[] lightingButtons = new Button[2];
    private readonly float[] magnifications = { 10f, 20f, 40f, 60f };
    private const string Objective60Id = "Objectif60X";
    private const string CameraId = "CameraMicroscope";
    private Button photoButton;
    private Text photoCountText;

    private GemInstance stone;
    private readonly List<MicroFeature> features = new List<MicroFeature>();
    private Quaternion orientation = Quaternion.identity;
    private Vector2 pan;                 // view center, in view pixels at the current magnification
    private int objective;               // index into magnifications
    private Lighting lighting = Lighting.Reflected;
    private float focus;                 // mm, relative to the middle of the stone
    private float watched;
    private bool observed;
    private bool dirty = true;

    private float PxPerMm => BasePxPerMm * magnifications[objective] / 10f;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<MicroscopeUI>();
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
        var tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += RefreshIfOpen;
        var library = MicroPhotoLibrary.Ensure();
        if (library != null) library.OnPhotosChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshIfOpen;
        if (MicroPhotoLibrary.Instance != null) MicroPhotoLibrary.Instance.OnPhotosChanged -= RefreshIfOpen;
    }

    private static bool Owns(string toolId)
    {
        var tools = PlayerOwnedTools.Ensure();
        return tools != null && tools.Has(toolId);
    }

    // Photographs what the eyepiece shows (round field, with a scale bar) and sends it to the lab computer.
    private void TakePhoto()
    {
        if (stone == null || !Owns(CameraId)) return;
        var library = MicroPhotoLibrary.Ensure();
        if (library == null) return;

        if (dirty)
        {
            dirty = false;
            DrawView();
        }

        var pixels = new Color32[buffer.Length];
        var center = ViewSize * 0.5f;
        for (var y = 0; y < ViewSize; y++)
        {
            for (var x = 0; x < ViewSize; x++)
            {
                var inside = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(center, center)).magnitude <= HoleRadius;
                pixels[y * ViewSize + x] = inside ? buffer[y * ViewSize + x] : new Color32(0, 0, 0, 255);
            }
        }

        // Scale bar: the longest round length that fits in 90 pixels.
        var barMm = 0f;
        foreach (var candidate in new[] { 1f, 0.5f, 0.2f, 0.1f, 0.05f, 0.02f, 0.01f })
        {
            if (candidate * PxPerMm <= 90f) { barMm = candidate; break; }
        }
        var barPx = Mathf.Max(2, Mathf.RoundToInt(barMm * PxPerMm));
        for (var y = 30; y < 34; y++)
            for (var x = 60; x < 60 + barPx; x++)
                pixels[y * ViewSize + x] = new Color32(245, 245, 245, 255);

        var image = new Texture2D(ViewSize, ViewSize, TextureFormat.RGBA32, false);
        image.SetPixels32(pixels);
        image.Apply();

        Sfx.Play(Sfx.Id.Shutter, 0.9f);
        library.AddPhoto(image, new MicroPhotoEntry
        {
            gemName = stone.IsIdentified ? stone.Definition.displayName : "Spécimen non identifié",
            gemId = stone.DisplayId,
            magnification = magnifications[objective],
            lighting = lighting == Lighting.Darkfield ? "Fond sombre" : "Lumière réfléchie",
            day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1,
        });
        Destroy(image);

        ToolFeedbackUI.Instance?.ShowMessage($"Microphotographie ×{magnifications[objective]:F0} envoyée à l'ordinateur du laboratoire.");
        UpdateControls();
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

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        if (gem.IsRough && !gem.HasWindow)
        {
            ToolFeedbackUI.Instance?.ShowMessage(
                "Microscope — La surface brute diffuse la lumière : faites polir une fenêtre à l'atelier lapidaire.");
            return;
        }

        stone = gem;
        orientation = Quaternion.identity;
        pan = Vector2.zero;
        focus = 0f;
        watched = 0f;
        observed = gem.HasObserved(ExaminationTool.MeasurementType.Inclusions.ToString());
        BuildFeatures();
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void SelectObjective(int index)
    {
        // Keep the same point of the stone in the middle of the view when zooming.
        var scale = magnifications[index] / magnifications[objective];
        pan *= scale;
        objective = index;
        dirty = true;
        UpdateControls();
    }

    private void SelectLighting(Lighting mode)
    {
        lighting = mode;
        dirty = true;
        UpdateControls();
    }

    private void MoveStage(Vector2 delta)
    {
        if (stone == null) return;
        // Dragging moves the stage under the lens: the view center goes the other way.
        pan = ClampPan(pan - delta);
        dirty = true;
    }

    private void Nudge(Vector2 delta)
    {
        if (stone == null) return;
        pan = ClampPan(pan + delta * (ViewSize * 0.25f));
        dirty = true;
    }

    private Vector2 ClampPan(Vector2 value)
    {
        var limit = GemView3D.MaxDimensionMm(stone) * PxPerMm * 0.5f + 30f;
        return Vector2.ClampMagnitude(value, limit);
    }

    private void Tilt(Vector2 delta)
    {
        if (stone == null) return;
        var yaw = Quaternion.AngleAxis(-delta.x * 0.6f, Vector3.up);
        var pitch = Quaternion.AngleAxis(delta.y * 0.6f, Vector3.right);
        orientation = yaw * pitch * orientation;
        dirty = true;
    }

    private void Roll(float degrees)
    {
        if (stone == null) return;
        orientation = Quaternion.AngleAxis(degrees, Vector3.forward) * orientation;
        dirty = true;
    }

    private void ResetView()
    {
        orientation = Quaternion.identity;
        pan = Vector2.zero;
        dirty = true;
    }

    private void ChangeFocus(float delta)
    {
        if (stone == null) return;
        var half = Mathf.Max(stone.DepthMm, 0.5f) * 0.5f;
        focus = Mathf.Clamp(focus + delta, -half, half);
        dirty = true;
    }

    private void OnWheel(float scroll)
    {
        // The wheel is the fine focus knob.
        ChangeFocus(scroll * 0.012f);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || stone == null) return;

        watched += Time.deltaTime;
        if (!observed && watched >= ObservationTime) FinishObservation();

        if (dirty)
        {
            dirty = false;
            DrawView();
            UpdateControls();
        }
    }

    private void FinishObservation()
    {
        observed = true;
        stone.RecordObservation(ExaminationTool.MeasurementType.Inclusions.ToString());
        ToolFeedbackUI.Instance?.ShowMessage(
            $"Microscope — Clarté : {stone.ClarityGrade} ({stone.ClarityDescription}) | " +
            $"Inclusions : {stone.InclusionDescription}");
        UpdateTexts();
    }

    // ---- Feature shapes --------------------------------------------------------------------------------------

    private void BuildFeatures()
    {
        features.Clear();
        if (stone == null) return;

        var index = 0;
        foreach (var feature in LoupeFeatures.GenerateMicro(stone))
        {
            var rng = new System.Random((int)StoneOptics.StableHash((stone.SpecimenId ?? "") + "shape" + index));
            index++;
            features.Add(new MicroFeature { Feature = feature, Parts = InclusionShapes.Build(feature, rng) });
        }
    }

    // ---- Drawing ---------------------------------------------------------------------------------------------

    private void DrawView()
    {
        if (stone == null) return;

        var mmToPx = PxPerMm;
        var half = new Vector3(stone.LengthMm, stone.WidthMm, Mathf.Max(stone.DepthMm, 0.5f)) * 0.5f;
        GemView3D.Colors(stone, out var body, out _);
        var dark = lighting == Lighting.Darkfield;

        // Silhouette of the stone: the projection of its ellipsoid on the lens plane (in mm).
        float cxx = 0f, cxy = 0f, cyy = 0f;
        var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
        for (var i = 0; i < 3; i++)
        {
            var p = (orientation * axes[i]) * half[i];
            cxx += p.x * p.x;
            cxy += p.x * p.y;
            cyy += p.y * p.y;
        }

        var mean = (cxx + cyy) * 0.5f;
        var spread = Mathf.Sqrt(Mathf.Pow((cxx - cyy) * 0.5f, 2f) + cxy * cxy);
        var major = Mathf.Sqrt(Mathf.Max(mean + spread, 0.01f));
        var minor = Mathf.Sqrt(Mathf.Max(mean - spread, 0.01f));
        var angle = 0.5f * Mathf.Atan2(2f * cxy, cxx - cyy);
        var cos = Mathf.Cos(-angle);
        var sin = Mathf.Sin(-angle);
        var edgeSoft = Mathf.Max(1f, Mathf.Min(major, minor) * mmToPx * 0.05f);

        var stage = dark ? new Color(0f, 0f, 0f) : new Color(0.10f, 0.10f, 0.12f);
        var center = ViewSize * 0.5f;

        for (var y = 0; y < ViewSize; y++)
        {
            for (var x = 0; x < ViewSize; x++)
            {
                var mmX = (x + 0.5f - center + pan.x) / mmToPx;
                var mmY = (y + 0.5f - center + pan.y) / mmToPx;
                var rx = (mmX * cos - mmY * sin) / major;
                var ry = (mmX * sin + mmY * cos) / minor;
                var d2 = rx * rx + ry * ry;
                var coverage = Mathf.Clamp01((1f - Mathf.Sqrt(d2)) * Mathf.Min(major, minor) * mmToPx / edgeSoft);

                Color color;
                if (d2 >= 1.2f && coverage <= 0f)
                {
                    color = stage;
                }
                else
                {
                    var shade = 0.55f + 0.35f * (1f - Mathf.Min(1f, d2));
                    var noise = Hash(x, y) * 0.05f;
                    Color inside;
                    if (dark)
                    {
                        // Darkfield: the stone is almost black, with a faint glow along its edge.
                        var rim = Mathf.Pow(Mathf.Clamp01(Mathf.Sqrt(d2)), 6f) * 0.25f;
                        inside = body * (0.04f + rim) + new Color(noise * 0.2f, noise * 0.2f, noise * 0.25f);
                    }
                    else
                    {
                        inside = body * shade + new Color(noise, noise, noise);
                    }
                    inside.a = 1f;
                    color = Color.Lerp(stage, inside, coverage);
                }

                buffer[y * ViewSize + x] = color;
                coverageMap[y * ViewSize + x] = coverage;
            }
        }

        DrawFeatures(half, dark, body);

        // The inclusions are inside the stone: whatever spilled over its outline goes back to the bare stage.
        var stage32 = (Color32)stage;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (coverageMap[i] <= 0f) buffer[i] = stage32;
        }

        viewTexture.SetPixels32(buffer);
        viewTexture.Apply();
        UpdateMiniMap();
    }

    private void DrawFeatures(Vector3 half, bool dark, Color body)
    {
        var mmToPx = PxPerMm;
        var tolerance = 9f / magnifications[objective];
        var tint = Color.Lerp(Color.white, body, 0.30f);

        foreach (var mf in features)
        {
            var f = mf.Feature;
            Vector3 position;
            if (f.External)
            {
                var normal = orientation * f.Position;
                if (normal.z <= 0.12f) continue;
                position = orientation * Vector3.Scale(f.Position, half);
            }
            else
            {
                position = orientation * Vector3.Scale(f.Position, half);
            }

            // Fine focus: features away from the focal plane are blurred and faint.
            var focusFactor = Mathf.Clamp01(1f - Mathf.Abs(position.z - focus) / tolerance);
            var blur = (1f - focusFactor);
            var alphaBase = Mathf.Lerp(0.12f, 1f, focusFactor);
            var soft = 0.8f + blur * blur * 9f;

            // Position and in-plane frame of the feature.
            var screenCenter = new Vector2(position.x, position.y) * mmToPx - pan + new Vector2(ViewSize * 0.5f, ViewSize * 0.5f);
            var projected = orientation * f.Direction;
            var along = new Vector2(projected.x, projected.y);
            var foreshortening = Mathf.Max(0.35f, along.magnitude);
            along = along.sqrMagnitude < 1e-6f ? Vector2.right : along.normalized;
            var across = new Vector2(-along.y, along.x);

            // Skip features that are entirely outside the view.
            if (screenCenter.x < -150f || screenCenter.y < -150f || screenCenter.x > ViewSize + 150f || screenCenter.y > ViewSize + 150f) continue;

            foreach (var part in mf.Parts)
                painter.DrawPart(part, f.External, screenCenter, along * foreshortening * mmToPx, across * mmToPx, alphaBase, soft, dark, tint, mmToPx);
        }
    }

    private static float Hash(int x, int y)
    {
        var h = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    // ---- Display ---------------------------------------------------------------------------------------------

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            features.Clear();
        }

        if (stone == null)
        {
            hintText.text = "Déposez une gemme sur la platine";
            // Empty stage.
            var empty = lighting == Lighting.Darkfield ? new Color32(0, 0, 0, 255) : new Color32(26, 26, 31, 255);
            for (var i = 0; i < buffer.Length; i++) buffer[i] = empty;
            viewTexture.SetPixels32(buffer);
            viewTexture.Apply();
            miniRect.gameObject.SetActive(false);
            fovMarker.enabled = false;
        }
        else
        {
            hintText.text = "";
            miniRect.gameObject.SetActive(true);
            fovMarker.enabled = true;
            dirty = true;
        }

        UpdateControls();
        UpdateTexts();
    }

    private void UpdateControls()
    {
        for (var i = 0; i < objectiveButtons.Length; i++)
        {
            objectiveButtons[i].gameObject.SetActive(i < 3 || Owns(Objective60Id));
            objectiveButtons[i].GetComponent<Image>().color = objective == i ? ButtonOn : ButtonColor;
        }

        var hasCamera = Owns(CameraId);
        photoButton.gameObject.SetActive(hasCamera);
        photoCountText.gameObject.SetActive(hasCamera);
        photoButton.interactable = stone != null;
        if (hasCamera)
        {
            var library = MicroPhotoLibrary.Instance;
            photoCountText.text = $"Photos envoyées : {(library != null ? library.Photos.Count : 0)}";
        }
        for (var i = 0; i < lightingButtons.Length; i++)
            lightingButtons[i].GetComponent<Image>().color = (int)lighting == i ? ButtonOn : ButtonColor;

        magnificationText.text = $"×{magnifications[objective]:F0}";
        focusText.text = stone == null ? "" : $"Mise au point : {focus:+0.00;-0.00;0.00} mm";
    }

    private void UpdateMiniMap()
    {
        if (stone == null) return;

        GemView3D.Colors(stone, out var body, out var facet);
        miniMap.Draw(GemView3D.MeshFor(stone), GemView3D.HalfExtents(stone), orientation, body, facet, stone.Definition.Diaphaneity);

        // Where the view sits over the stone: a glowing disc as big as the field of view.
        var maxDim = GemView3D.MaxDimensionMm(stone);
        var miniSide = miniRect.sizeDelta.x;
        var pxPerMmMini = miniSide * GemView3D.FitFraction / Mathf.Max(maxDim, 0.1f);
        var fovMm = ViewSize / PxPerMm;
        fovMarker.rectTransform.sizeDelta = Vector2.one * Mathf.Max(6f, fovMm * pxPerMmMini);
        fovMarker.rectTransform.anchoredPosition = new Vector2(pan.x / PxPerMm * pxPerMmMini, pan.y / PxPerMm * pxPerMmMini);
    }

    private void UpdateTexts()
    {
        if (stone == null)
        {
            resultText.text = "Aucune gemme sur la platine.";
            return;
        }

        if (!observed)
        {
            resultText.text = "Observez la gemme : changez d'objectif et d'éclairage, déplacez la platine et faites la mise au point sur les inclusions.";
            return;
        }

        var text = $"Clarté : {stone.ClarityGrade}\n{stone.ClarityDescription}\n\nInclusions : {stone.InclusionDescription}" +
                   (stone.FacetSummary != null ? $"\n\nFacettes : {stone.FacetSummary}" : "");

        var names = LoupeFeatures.InclusionNames(stone, true, true);
        if (stone.AsterismRays > 0)
            names.Insert(0, $"astérisme à {(stone.AsterismRays == 6 ? "six" : "quatre")} branches");

        if (names.Count > 0)
        {
            text += "\n\nObservées :";
            foreach (var name in names) text += $"\n• {name}";
        }

        resultText.text = text;
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
        panel = new GameObject("MicroscopePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-160f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Microscope", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        buffer = new Color32[ViewSize * ViewSize];
        painter = new FeaturePainter(buffer, ViewSize, ViewSize, FeaturePainter.Mode.Opaque);
        coverageMap = new float[ViewSize * ViewSize];
        viewTexture = new Texture2D(ViewSize, ViewSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        BuildView();
        BuildControls();
        BuildSidePanel();
    }

    private void BuildView()
    {
        // The whole viewing area takes drops, drags (move the stage), right drags (tilt) and the wheel (focus).
        var area = RuntimeUIKit.MakeBox("Area", panel.transform, new Vector2(AreaX, AreaY), new Vector2(ViewSize + 30f, ViewSize + 30f), new Color(0.04f, 0.04f, 0.05f, 1f));
        area.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        var drag = area.gameObject.AddComponent<DichroscopeDragArea>();
        drag.OnDragDelta = MoveStage;
        drag.OnPanDelta = Tilt;
        drag.OnRoll = OnWheel;

        var viewGO = new GameObject("View", typeof(RectTransform), typeof(Image));
        viewGO.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(viewGO.GetComponent<RectTransform>(), new Vector2(AreaX + 15f, AreaY - 15f), new Vector2(ViewSize, ViewSize));
        viewImage = viewGO.GetComponent<Image>();
        viewImage.sprite = Sprite.Create(viewTexture, new Rect(0, 0, ViewSize, ViewSize), new Vector2(0.5f, 0.5f), 100f);
        viewImage.raycastTarget = false;

        // Eyepiece mask: dark plate with a round aperture and a thin ring.
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(frame.GetComponent<RectTransform>(), new Vector2(AreaX + 15f, AreaY - 15f), new Vector2(ViewSize, ViewSize));
        var frameImage = frame.GetComponent<Image>();
        frameImage.sprite = MakeApertureSprite(256, HoleRadius / (ViewSize / 2f));
        frameImage.raycastTarget = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.78f),
            new Vector2(AreaX + 15f, AreaY - ViewSize / 2f - 20f), new Vector2(ViewSize, 18f));
        magnificationText = RuntimeUIKit.MakeText("Magnification", panel.transform, "×10", 16, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.95f),
            new Vector2(AreaX + 24f, AreaY - 20f), new Vector2(70f, 22f));
        magnificationText.fontStyle = FontStyle.Bold;
    }

    private void BuildControls()
    {
        var top = AreaY - ViewSize - 40f;

        RuntimeUIKit.MakeText("StageLabel", panel.transform, "Platine :", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.67f, 0.72f),
            new Vector2(AreaX, top + 5f), new Vector2(60f, 16f));
        MakeNudge("StageLeft", "<", new Vector2(AreaX + 62f, top), new Vector2(-1f, 0f));
        MakeNudge("StageUp", "^", new Vector2(AreaX + 94f, top), new Vector2(0f, 1f));
        MakeNudge("StageDown", "v", new Vector2(AreaX + 126f, top), new Vector2(0f, -1f));
        MakeNudge("StageRight", ">", new Vector2(AreaX + 158f, top), new Vector2(1f, 0f));

        var rollLeft = RuntimeUIKit.MakeButton("RollLeft", panel.transform, "Rouler <", 12, ButtonColor, new Vector2(AreaX + 200f, top), new Vector2(66f, 24f));
        rollLeft.onClick.AddListener(() => Roll(15f));
        var rollRight = RuntimeUIKit.MakeButton("RollRight", panel.transform, "Rouler >", 12, ButtonColor, new Vector2(AreaX + 270f, top), new Vector2(66f, 24f));
        rollRight.onClick.AddListener(() => Roll(-15f));

        RuntimeUIKit.MakeText("Tip", panel.transform,
            "Glisser : déplacer la platine. Clic droit + glisser : incliner la gemme. Molette : mise au point.",
            11, TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f), new Vector2(AreaX, top - 34f), new Vector2(340f, 30f));
    }

    private void MakeNudge(string name, string label, Vector2 position, Vector2 direction)
    {
        var button = RuntimeUIKit.MakeButton(name, panel.transform, label, 12, ButtonColor, position, new Vector2(28f, 24f));
        button.onClick.AddListener(() => Nudge(direction));
    }

    private void BuildSidePanel()
    {
        const float x = 392f;

        RuntimeUIKit.MakeText("ObjectiveHeader", panel.transform, "Objectif", 14, TextAnchor.UpperLeft, Gold, new Vector2(x + 4f, -44f), new Vector2(200f, 20f));
        for (var i = 0; i < objectiveButtons.Length; i++)
        {
            var captured = i;
            objectiveButtons[i] = RuntimeUIKit.MakeButton("Objective" + i, panel.transform, $"×{magnifications[i]:F0}", 14, ButtonColor,
                new Vector2(x + i * 58f, -68f), new Vector2(54f, 28f));
            objectiveButtons[i].onClick.AddListener(() => SelectObjective(captured));
        }

        RuntimeUIKit.MakeText("LightingHeader", panel.transform, "Éclairage", 14, TextAnchor.UpperLeft, Gold, new Vector2(x + 4f, -108f), new Vector2(200f, 20f));
        var names = new[] { "Lumière réfléchie", "Fond sombre (darkfield)" };
        for (var i = 0; i < 2; i++)
        {
            var captured = (Lighting)i;
            lightingButtons[i] = RuntimeUIKit.MakeButton("Lighting" + i, panel.transform, names[i], 12, ButtonColor,
                new Vector2(x, -132f - i * 32f), new Vector2(228f, 26f));
            lightingButtons[i].onClick.AddListener(() => SelectLighting(captured));
        }

        focusText = RuntimeUIKit.MakeText("Focus", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f), new Vector2(x + 4f, -200f), new Vector2(160f, 16f));
        var focusUp = RuntimeUIKit.MakeButton("FocusUp", panel.transform, "+", 13, ButtonColor, new Vector2(x + 168f, -197f), new Vector2(28f, 22f));
        focusUp.onClick.AddListener(() => ChangeFocus(0.05f));
        var focusDown = RuntimeUIKit.MakeButton("FocusDown", panel.transform, "-", 13, ButtonColor, new Vector2(x + 200f, -197f), new Vector2(28f, 22f));
        focusDown.onClick.AddListener(() => ChangeFocus(-0.05f));

        // Mini map: shows where the view sits over the stone.
        RuntimeUIKit.MakeBox("MiniBack", panel.transform, new Vector2(x, -228f), new Vector2(96f, 96f), new Color(0.04f, 0.05f, 0.07f, 1f));
        var miniGO = new GameObject("MiniMap", typeof(RectTransform), typeof(Image));
        miniGO.transform.SetParent(panel.transform, false);
        miniRect = miniGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(miniRect, new Vector2(x + 4f, -232f), new Vector2(88f, 88f));
        miniMap = new GemView3D(48);
        var miniImage = miniGO.GetComponent<Image>();
        miniImage.sprite = miniMap.Sprite;
        miniImage.raycastTarget = false;

        var markerGO = new GameObject("FovMarker", typeof(RectTransform), typeof(Image));
        markerGO.transform.SetParent(miniGO.transform, false);
        var markerRect = markerGO.GetComponent<RectTransform>();
        markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        fovMarker = markerGO.GetComponent<Image>();
        fovMarker.sprite = RuntimeUIKit.Glow;
        fovMarker.color = new Color(1f, 0.85f, 0.2f, 0.75f);
        fovMarker.raycastTarget = false;
        fovMarker.enabled = false;

        RuntimeUIKit.MakeText("MapLabel", panel.transform, "Zone observée", 11, TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f),
            new Vector2(x + 102f, -236f), new Vector2(130f, 16f));

        // Microscope camera (bought at the GemStore): photographs the field of view and sends it to the lab computer.
        photoButton = RuntimeUIKit.MakeButton("Photo", panel.transform, "Photographier", 12, new Color(0.30f, 0.38f, 0.58f, 1f),
            new Vector2(x + 102f, -258f), new Vector2(128f, 28f));
        photoButton.onClick.AddListener(TakePhoto);
        photoCountText = RuntimeUIKit.MakeText("PhotoCount", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.65f, 0.72f, 0.9f),
            new Vector2(x + 102f, -290f), new Vector2(130f, 32f));

        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(x - 4f, -332f), new Vector2(240f, 110f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 11, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(x + 2f, -336f), new Vector2(228f, 102f));

        var reset = RuntimeUIKit.MakeButton("Reset", panel.transform, "Remettre à plat", 12, ButtonColor, new Vector2(x, -450f), new Vector2(112f, 26f));
        reset.onClick.AddListener(ResetView);
        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer", 12, ButtonColor, new Vector2(x + 116f, -450f), new Vector2(54f, 26f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 12, ButtonColor, new Vector2(x + 174f, -450f), new Vector2(56f, 26f));
        close.onClick.AddListener(Close);
    }

    // Dark plate with a round aperture and a thin metal ring.
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
                var shade = ring ? 0.45f + 0.15f * (-dx - dy) * 0.5f : 0.08f;
                texture.SetPixel(x, y, new Color(shade, shade, shade * 1.05f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
