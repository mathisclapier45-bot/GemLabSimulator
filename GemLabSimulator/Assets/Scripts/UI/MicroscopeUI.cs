using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Gemological microscope: 10x / 20x / 40x objectives, reflected light or darkfield, fine focus through the
// depth of the stone, stage movement, and a view drawn in detail (inclusions, growth structure, surface
// defects). Rough stones need a polished window first.
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

    // A drawing element of a feature, in the feature's own frame (u along its direction, v across), in mm.
    private class Primitive
    {
        public enum Kind { Line, Dot, Ring, Polygon, Band }
        public Kind Type;
        public Vector2 A;
        public Vector2 B;
        public float Radius;
        public float Width;
        public Vector2[] Points;
        public bool Dark;            // drawn dark in reflected light (pits, chips)
    }

    private class MicroFeature
    {
        public LoupeFeature Feature;
        public readonly List<Primitive> Primitives = new List<Primitive>();
    }

    private GameObject panel;
    private Image viewImage;
    private Texture2D viewTexture;
    private Color32[] buffer;
    private GemView3D miniMap;
    private RectTransform miniRect;
    private Image fovMarker;
    private Text hintText;
    private Text resultText;
    private Text focusText;
    private Text magnificationText;
    private readonly Button[] objectiveButtons = new Button[3];
    private readonly Button[] lightingButtons = new Button[2];
    private readonly float[] magnifications = { 10f, 20f, 40f };

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
            $"Inclusions : {stone.InclusionDescription} | Origine probable : {stone.Origin.name}");
        UpdateTexts();
    }

    // ---- Feature shapes --------------------------------------------------------------------------------------

    private void BuildFeatures()
    {
        features.Clear();
        if (stone == null) return;

        var heated = stone.TreatmentGrade == "Chauffé";
        var index = 0;
        foreach (var feature in LoupeFeatures.GenerateMicro(stone))
        {
            var rng = new System.Random((int)StoneOptics.StableHash((stone.SpecimenId ?? "") + "shape" + index));
            index++;
            var mf = new MicroFeature { Feature = feature };
            var size = feature.Size;

            switch (feature.Kind)
            {
                case FeatureKind.Crystal:
                {
                    var length = 0.12f + 0.55f * size;
                    var width = length * R(rng, 0.45f, 0.7f);
                    mf.Primitives.Add(new Primitive
                    {
                        Type = Primitive.Kind.Polygon,
                        Points = new[]
                        {
                            new Vector2(length / 2f, 0f), new Vector2(length / 4f, width / 2f), new Vector2(-length / 4f, width / 2f),
                            new Vector2(-length / 2f, 0f), new Vector2(-length / 4f, -width / 2f), new Vector2(length / 4f, -width / 2f),
                        },
                    });
                    // Heated stones show a discoid fracture ("halo") around their crystals.
                    if (heated) mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Ring, Radius = length * 0.95f, Width = 0.006f });
                    break;
                }
                case FeatureKind.Fluid:
                {
                    var dots = 3 + rng.Next(4);
                    for (var i = 0; i < dots; i++)
                        mf.Primitives.Add(new Primitive
                        {
                            Type = Primitive.Kind.Dot,
                            A = new Vector2(R(rng, -0.1f, 0.1f), R(rng, -0.07f, 0.07f)),
                            Radius = R(rng, 0.012f, 0.035f) * (0.6f + size),
                        });
                    break;
                }
                case FeatureKind.Bubble:
                {
                    var radius = 0.03f + 0.12f * size;
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Ring, Radius = radius, Width = 0.008f });
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Dot, A = new Vector2(-radius * 0.35f, radius * 0.35f), Radius = radius * 0.2f });
                    break;
                }
                case FeatureKind.Fracture:
                {
                    var length = 0.5f + 1.5f * size;
                    AddJaggedLine(mf, rng, length, 0.008f, 6);
                    if (rng.NextDouble() < 0.5) AddJaggedLine(mf, rng, length * 0.45f, 0.006f, 4, new Vector2(length * 0.1f, 0f));
                    break;
                }
                case FeatureKind.Cleavage:
                {
                    var length = 0.8f + 1.2f * size;
                    foreach (var v in new[] { -0.07f, 0f, 0.08f })
                        mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Line, A = new Vector2(-length / 2f, v), B = new Vector2(length / 2f, v + R(rng, -0.01f, 0.01f)), Width = 0.006f });
                    break;
                }
                case FeatureKind.Healing:
                {
                    var span = 0.9f * (0.6f + size);
                    var count = 14;
                    for (var i = 0; i < count; i++)
                    {
                        var t = i / (count - 1f) - 0.5f;
                        mf.Primitives.Add(new Primitive
                        {
                            Type = Primitive.Kind.Dot,
                            A = new Vector2(t * span, Mathf.Sin(t * 5f) * 0.09f + R(rng, -0.015f, 0.015f)),
                            Radius = R(rng, 0.01f, 0.022f),
                        });
                    }
                    break;
                }
                case FeatureKind.Needle:
                {
                    var length = 0.4f + 1.0f * size;
                    var lines = 5 + rng.Next(3);
                    for (var i = 0; i < lines; i++)
                    {
                        var v = (i - lines / 2f) * 0.028f;
                        var offset = R(rng, -0.12f, 0.12f);
                        var l = length * R(rng, 0.6f, 1f);
                        mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Line, A = new Vector2(offset - l / 2f, v), B = new Vector2(offset + l / 2f, v), Width = 0.004f });
                    }
                    break;
                }
                case FeatureKind.Zoning:
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Band, A = new Vector2(-1.2f, 0f), B = new Vector2(1.2f, 0f), Width = 0.12f + 0.2f * size });
                    break;
                case FeatureKind.Striae:
                {
                    var radius = R(rng, 0.8f, 1.4f);
                    for (var arc = 0; arc < 4; arc++)
                    {
                        var r = radius + arc * 0.06f;
                        var previous = Vector2.zero;
                        for (var i = 0; i <= 8; i++)
                        {
                            var angle = (-35f + i * (70f / 8f)) * Mathf.Deg2Rad;
                            var point = new Vector2(Mathf.Sin(angle) * r, Mathf.Cos(angle) * r - radius);
                            if (i > 0) mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Line, A = previous, B = point, Width = 0.005f });
                            previous = point;
                        }
                    }
                    break;
                }
                case FeatureKind.Scratch:
                {
                    var length = 0.3f + 0.8f * size;
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Line, A = new Vector2(-length / 2f, 0f), B = new Vector2(length / 2f, R(rng, -0.03f, 0.03f)), Width = 0.006f });
                    break;
                }
                case FeatureKind.Chip:
                {
                    var s = 0.1f + 0.15f * size;
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Polygon, Points = new[] { new Vector2(-s, 0f), new Vector2(s, 0.3f * s), new Vector2(0f, -s) }, Dark = true });
                    break;
                }
                case FeatureKind.Pit:
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Dot, A = Vector2.zero, Radius = 0.02f + 0.03f * size, Dark = true });
                    break;
                default:    // Abrasion
                    mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Band, A = new Vector2(-0.15f, 0f), B = new Vector2(0.15f, 0f), Width = 0.14f });
                    break;
            }

            features.Add(mf);
        }
    }

    private static float R(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static void AddJaggedLine(MicroFeature mf, System.Random rng, float length, float width, int segments, Vector2? origin = null)
    {
        var start = origin ?? new Vector2(-length / 2f, 0f);
        var previous = start;
        for (var i = 1; i <= segments; i++)
        {
            var point = start + new Vector2(length * i / segments, R(rng, -0.1f, 0.1f) * length * 0.35f);
            mf.Primitives.Add(new Primitive { Type = Primitive.Kind.Line, A = previous, B = point, Width = width });
            previous = point;
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
            }
        }

        DrawFeatures(half, dark, body);

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

            foreach (var p in mf.Primitives)
                DrawPrimitive(p, f, screenCenter, along * foreshortening * mmToPx, across * mmToPx, alphaBase, soft, dark, tint);
        }
    }

    private void DrawPrimitive(Primitive p, LoupeFeature f, Vector2 center, Vector2 uAxis, Vector2 vAxis,
        float alpha, float soft, bool dark, Color tint)
    {
        Vector2 Map(Vector2 local) => center + uAxis * local.x + vAxis * local.y;

        // Colors per lighting. Reflected light: inclusions are lighter or darker than the host; darkfield: they glow.
        var surface = f.External;
        Color color;
        float strength;
        if (p.Dark)
        {
            color = new Color(0.04f, 0.04f, 0.05f);
            strength = dark ? 0.12f : 0.9f;
            if (dark) color = new Color(0.8f, 0.85f, 0.9f);
        }
        else if (dark)
        {
            color = tint;
            strength = surface ? 0.35f : 1f;
        }
        else
        {
            color = Color.Lerp(tint, Color.white, 0.55f);
            strength = surface ? 0.95f : 0.8f;
        }

        var a = alpha * strength;
        var mmToPx = PxPerMm;

        switch (p.Type)
        {
            case Primitive.Kind.Line:
                DrawLine(Map(p.A), Map(p.B), Mathf.Max(1f, p.Width * mmToPx), color, a, soft, dark);
                break;
            case Primitive.Kind.Dot:
                DrawDisc(Map(p.A), Mathf.Max(1f, p.Radius * mmToPx), color, a, soft, dark, false, 0f);
                break;
            case Primitive.Kind.Ring:
                DrawDisc(Map(p.A), Mathf.Max(2f, p.Radius * mmToPx), color, a * 0.9f, soft, dark, true, Mathf.Max(1f, p.Width * mmToPx));
                break;
            case Primitive.Kind.Band:
                DrawLine(Map(p.A), Map(p.B), Mathf.Max(2f, p.Width * mmToPx), dark ? body_Tint(tint) : Color.Lerp(tint, Color.black, 0.45f),
                    a * (dark ? 0.18f : 0.22f), soft + 3f, dark);
                break;
            case Primitive.Kind.Polygon:
            {
                var points = new Vector2[p.Points.Length];
                for (var i = 0; i < points.Length; i++) points[i] = Map(p.Points[i]);

                // Fill (faint) and outline (strong).
                DrawPolygonFill(points, p.Dark ? color : Color.Lerp(tint, Color.black, dark ? 0.4f : 0.55f), a * (dark ? 0.25f : 0.5f), soft, dark);
                for (var i = 0; i < points.Length; i++)
                    DrawLine(points[i], points[(i + 1) % points.Length], Mathf.Max(1f, 0.008f * mmToPx), color, a, soft, dark);
                break;
            }
        }
    }

    private static Color body_Tint(Color tint)
    {
        return Color.Lerp(tint, Color.white, 0.2f);
    }

    private void Blend(int x, int y, Color c, float a, bool additive)
    {
        if (a <= 0f || x < 0 || y < 0 || x >= ViewSize || y >= ViewSize) return;

        var index = y * ViewSize + x;
        var existing = buffer[index];
        if (additive)
        {
            buffer[index] = new Color32(
                (byte)Mathf.Min(255, existing.r + c.r * a * 255f),
                (byte)Mathf.Min(255, existing.g + c.g * a * 255f),
                (byte)Mathf.Min(255, existing.b + c.b * a * 255f), 255);
        }
        else
        {
            buffer[index] = new Color32(
                (byte)Mathf.Lerp(existing.r, c.r * 255f, a),
                (byte)Mathf.Lerp(existing.g, c.g * 255f, a),
                (byte)Mathf.Lerp(existing.b, c.b * 255f, a), 255);
        }
    }

    private void DrawLine(Vector2 a, Vector2 b, float width, Color color, float alpha, float soft, bool additive)
    {
        var reach = width * 0.5f + soft;
        var minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - reach);
        var maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + reach);
        var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - reach);
        var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + reach);
        if (maxX < 0 || maxY < 0 || minX >= ViewSize || minY >= ViewSize) return;

        var ab = b - a;
        var lengthSq = Mathf.Max(ab.sqrMagnitude, 1e-4f);
        for (var y = Mathf.Max(0, minY); y <= Mathf.Min(ViewSize - 1, maxY); y++)
        {
            for (var x = Mathf.Max(0, minX); x <= Mathf.Min(ViewSize - 1, maxX); x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var distance = (p - (a + ab * t)).magnitude;
                var coverage = Mathf.Clamp01((width * 0.5f + soft - distance) / soft);
                if (coverage > 0f) Blend(x, y, color, alpha * coverage, additive);
            }
        }
    }

    private void DrawDisc(Vector2 center, float radius, Color color, float alpha, float soft, bool additive, bool ring, float thickness)
    {
        var reach = radius + thickness + soft;
        var minX = Mathf.FloorToInt(center.x - reach);
        var maxX = Mathf.CeilToInt(center.x + reach);
        var minY = Mathf.FloorToInt(center.y - reach);
        var maxY = Mathf.CeilToInt(center.y + reach);
        if (maxX < 0 || maxY < 0 || minX >= ViewSize || minY >= ViewSize) return;

        for (var y = Mathf.Max(0, minY); y <= Mathf.Min(ViewSize - 1, maxY); y++)
        {
            for (var x = Mathf.Max(0, minX); x <= Mathf.Min(ViewSize - 1, maxX); x++)
            {
                var distance = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude;
                float coverage;
                if (ring) coverage = Mathf.Clamp01((thickness * 0.5f + soft - Mathf.Abs(distance - radius)) / soft);
                else coverage = Mathf.Clamp01((radius + soft - distance) / soft);
                if (coverage > 0f) Blend(x, y, color, alpha * coverage, additive);
            }
        }
    }

    private void DrawPolygonFill(Vector2[] points, Color color, float alpha, float soft, bool additive)
    {
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        foreach (var p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        for (var y = Mathf.Max(0, Mathf.FloorToInt(minY - soft)); y <= Mathf.Min(ViewSize - 1, Mathf.CeilToInt(maxY + soft)); y++)
        {
            for (var x = Mathf.Max(0, Mathf.FloorToInt(minX - soft)); x <= Mathf.Min(ViewSize - 1, Mathf.CeilToInt(maxX + soft)); x++)
            {
                if (PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points)) Blend(x, y, color, alpha, additive);
            }
        }
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];
            if ((pi.y > point.y) != (pj.y > point.y) && point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                inside = !inside;
        }
        return inside;
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
            objectiveButtons[i].GetComponent<Image>().color = objective == i ? ButtonOn : ButtonColor;
        for (var i = 0; i < lightingButtons.Length; i++)
            lightingButtons[i].GetComponent<Image>().color = (int)lighting == i ? ButtonOn : ButtonColor;

        magnificationText.text = $"×{magnifications[objective]:F0}";
        focusText.text = stone == null ? "" : $"Mise au point : {focus:+0.00;-0.00;0.00} mm";
    }

    private void UpdateMiniMap()
    {
        if (stone == null) return;

        GemView3D.Colors(stone, out var body, out var facet);
        miniMap.Draw(GemView3D.MeshFor(stone), GemView3D.HalfExtents(stone), orientation, body, facet);

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

        var text = $"Clarté : {stone.ClarityGrade}\n{stone.ClarityDescription}\n\nInclusions : {stone.InclusionDescription}\n\n" +
                   $"Origine probable : {stone.Origin.name}";

        var kinds = new Dictionary<FeatureKind, int>();
        foreach (var mf in features)
        {
            if (mf.Feature.External) continue;
            kinds[mf.Feature.Kind] = kinds.TryGetValue(mf.Feature.Kind, out var n) ? n + 1 : 1;
        }

        if (kinds.Count > 0)
        {
            text += "\n\nObservées :";
            foreach (var pair in kinds) text += $"\n• {Label(pair.Key)} ({pair.Value})";
        }

        resultText.text = text;
    }

    private static string Label(FeatureKind kind)
    {
        switch (kind)
        {
            case FeatureKind.Crystal: return "cristaux";
            case FeatureKind.Fluid: return "inclusions fluides";
            case FeatureKind.Bubble: return "bulles de gaz";
            case FeatureKind.Fracture: return "fractures";
            case FeatureKind.Cleavage: return "clivage";
            case FeatureKind.Healing: return "givres de guérison";
            case FeatureKind.Needle: return "aiguilles (soie)";
            case FeatureKind.Zoning: return "zonation de croissance";
            case FeatureKind.Striae: return "stries de croissance courbes";
            default: return kind.ToString();
        }
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
        for (var i = 0; i < 3; i++)
        {
            var captured = i;
            objectiveButtons[i] = RuntimeUIKit.MakeButton("Objective" + i, panel.transform, $"×{magnifications[i]:F0}", 14, ButtonColor,
                new Vector2(x + i * 78f, -68f), new Vector2(72f, 28f));
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
