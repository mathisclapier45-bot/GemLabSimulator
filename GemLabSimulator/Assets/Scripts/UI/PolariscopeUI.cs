using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Polariscope between crossed polarizers, with the conoscope built in.
//  - Drag a gem onto the lens and turn it in every direction (drag = tilt, wheel / ring = roll).
//  - isotropic (cubic / amorphous): stays dark
//  - polycrystalline aggregate: stays lit
//  - anisotropic: lights up and goes dark four times per full turn; looking along an optic axis the stone stays
//    dark and shows interference colors
//  - pick up the conoscope and pass it in front of a stone looking along an optic axis to see its figure.
public class PolariscopeUI : MonoBehaviour
{
    public static PolariscopeUI Instance { get; private set; }

    private const float PanelWidth = 420f;
    private const float PanelHeight = 580f;
    private const float RingDiameter = 320f;
    private const float LensDiameter = 224f;
    private const float CenterX = 210f;
    private const float CenterY = -234f;
    private const float ConclusionRotation = 200f;       // degrees explored before the polariscope result is logged
    private const float ConoscopeReach = 80f;            // how close the conoscope must be to the stone, in px
    private const float ConoscopeHold = 1.2f;            // seconds in front of the stone before the figure is logged
    private const float FieldHalfAngle = 30f;            // conoscope field: axes up to 30 degrees from the viewing direction
    private const int FigureSize = 112;

    private static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.08f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color UnlitStone = new Color(0.10f, 0.11f, 0.17f, 1f);
    private static readonly Color LitStone = new Color(0.86f, 0.90f, 1f, 1f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color ButtonOn = new Color(0.30f, 0.55f, 0.35f, 1f);

    private enum LightBehaviour { Opaque, Dark, AlwaysLit, Blinking }

    private GameObject panel;
    private RectTransform panelRect;
    private RectTransform ringVisual;
    private GemView3D view3D;
    private Image stoneImage;
    private RectTransform stoneRect;
    private Image glowImage;
    private Image figureImage;
    private Texture2D figureTexture;
    private Text hintText;
    private Text statusText;
    private Text rotationText;
    private Text conoscopeText;
    private Button conoscopeButton;
    private Text conoscopeButtonLabel;
    private RectTransform conoscopeTool;

    private GemInstance stone;
    private LightBehaviour behaviour;
    private Quaternion opticFrame = Quaternion.identity;
    private Quaternion orientation = Quaternion.identity;
    private bool biaxial;
    private float halfAxisAngle;
    private float birefringence;
    private float ringAngle;          // degrees, only for the knob's look
    private float explored;           // degrees

    private bool conoscopeHeld;
    private float conoscopeTime;
    private bool figureVisible;
    private Quaternion lastFigureOrientation = new Quaternion(0f, 0f, 0f, 0f);

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<PolariscopeUI>();
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
        conoscopeHeld = false;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        conoscopeHeld = false;
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        var def = gem.Definition;

        if (def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque) behaviour = LightBehaviour.Opaque;
        else if (def.isPolycrystalline) behaviour = LightBehaviour.AlwaysLit;
        else if (def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Isotropic) behaviour = LightBehaviour.Dark;
        else behaviour = LightBehaviour.Blinking;

        biaxial = def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Biaxial;
        halfAxisAngle = StoneOptics.HalfOpticAngle(def);
        birefringence = Mathf.Max(def.birefringence, 0.002f);
        opticFrame = StoneOptics.OpticFrame(gem);

        orientation = Quaternion.identity;
        ringAngle = 0f;
        explored = 0f;
        conoscopeTime = 0f;
        lastFigureOrientation = new Quaternion(0f, 0f, 0f, 0f);
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void ResetOrientation()
    {
        if (stone == null) return;
        orientation = Quaternion.identity;
        UpdateView();
    }

    // Drag on the lens: tilt the stone around the horizontal and vertical axes.
    private void OnTilt(Vector2 delta)
    {
        if (stone == null) return;

        var yaw = Quaternion.AngleAxis(-delta.x * 0.7f, Vector3.up);
        var pitch = Quaternion.AngleAxis(delta.y * 0.7f, Vector3.right);
        orientation = yaw * pitch * orientation;
        AfterRotation(delta.magnitude * 0.7f);
    }

    // Wheel, ring or buttons: roll the stone in the plane of the lens.
    private void Roll(float degrees)
    {
        if (stone == null) return;

        orientation = Quaternion.AngleAxis(degrees, Vector3.forward) * orientation;
        ringAngle += degrees;
        AfterRotation(Mathf.Abs(degrees));
    }

    private void OnRingRotated(float deltaRadians)
    {
        Roll(deltaRadians * Mathf.Rad2Deg);
    }

    private void AfterRotation(float degrees)
    {
        explored += degrees;

        var key = ExaminationTool.MeasurementType.OpticalCharacter.ToString();
        if (explored >= ConclusionRotation && !stone.HasObserved(key))
        {
            stone.RecordObservation(key);
            ToolFeedbackUI.Instance?.ShowMessage($"Polariscope — {OpticalDescriptions.PolariscopeText(stone.Definition)}");
            StoryState.Instance?.OnObserved(stone, key);
        }

        UpdateView();
    }

    private void ToggleConoscope()
    {
        conoscopeHeld = !conoscopeHeld;
        conoscopeTime = 0f;
        UpdateConoscopeUi();
    }

    // ---- Optics ----------------------------------------------------------------------------------------------

    // Optic axes in world space, oriented toward the viewer (+Z).
    private List<Vector3> OpticAxes()
    {
        var axes = new List<Vector3>();
        if (behaviour != LightBehaviour.Blinking) return axes;

        var frame = orientation * opticFrame;
        if (!biaxial)
        {
            axes.Add(frame * Vector3.forward);
        }
        else
        {
            axes.Add(frame * (Quaternion.AngleAxis(halfAxisAngle, Vector3.up) * Vector3.forward));
            axes.Add(frame * (Quaternion.AngleAxis(-halfAxisAngle, Vector3.up) * Vector3.forward));
        }

        for (var i = 0; i < axes.Count; i++)
        {
            if (axes[i].z < 0f) axes[i] = -axes[i];
        }
        return axes;
    }

    // Color of the stone between crossed polarizers, including interference colors close to an optic axis.
    private Color StoneColor(out float brightness)
    {
        switch (behaviour)
        {
            case LightBehaviour.AlwaysLit:
                brightness = 0.78f;
                var lit = LitStone * 0.78f;
                lit.a = 1f;
                return lit;
            case LightBehaviour.Blinking:
                break;
            default:
                brightness = 0f;
                return UnlitStone;
        }

        var axes = OpticAxes();
        var sines = new float[axes.Count];
        for (var i = 0; i < axes.Count; i++)
            sines[i] = Mathf.Sqrt(Mathf.Max(0f, 1f - axes[i].z * axes[i].z));

        // Effective birefringence: nil along an optic axis, maximal across it.
        var effective = biaxial ? birefringence * sines[0] * sines[1] : birefringence * sines[0] * sines[0];
        var depth = stone != null ? Mathf.Max(stone.DepthMm, 1f) : 4f;
        var retardation = depth * 1e6f * effective;                       // nanometers

        // Extinction positions: four per turn, set by the projection of the optic direction on the lens plane.
        var bisector = orientation * opticFrame * Vector3.forward;
        var psi = new Vector2(bisector.x, bisector.y).sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(bisector.y, bisector.x);
        var pattern = Mathf.Pow(Mathf.Sin(2f * psi), 2f);

        // Thick or far from an axis: high-order white. Near an axis: first-order colors.
        var white = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2500f, 5000f, retardation));
        var color = new Color(
            Mathf.Lerp(Channel(retardation, 650f), 0.5f, white),
            Mathf.Lerp(Channel(retardation, 550f), 0.5f, white),
            Mathf.Lerp(Channel(retardation, 450f), 0.5f, white), 1f);

        color = new Color(color.r * pattern, color.g * pattern, color.b * pattern, 1f) * 1.25f;
        color.a = 1f;
        brightness = Mathf.Clamp01(Mathf.Max(color.r, color.g, color.b));
        return new Color(
            Mathf.Max(UnlitStone.r, Mathf.Clamp01(color.r)),
            Mathf.Max(UnlitStone.g, Mathf.Clamp01(color.g)),
            Mathf.Max(UnlitStone.b, Mathf.Clamp01(color.b)), 1f);
    }

    private static float Channel(float retardationNm, float wavelengthNm)
    {
        var s = Mathf.Sin(Mathf.PI * retardationNm / wavelengthNm);
        return s * s;
    }

    // ---- Conoscope -------------------------------------------------------------------------------------------

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (!conoscopeHeld)
        {
            conoscopeTool.gameObject.SetActive(false);
            if (figureVisible) HideFigure();
            return;
        }

        // The conoscope follows the mouse, its ball lens at the cursor.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, Input.mousePosition, null, out var local);
        conoscopeTool.gameObject.SetActive(true);
        conoscopeTool.anchoredPosition = new Vector2(local.x + PanelWidth / 2f, local.y - PanelHeight / 2f);

        if (stone == null)
        {
            conoscopeText.text = "Déposez d'abord une gemme sur la lentille.";
            return;
        }

        var lensCenter = new Vector2(CenterX - PanelWidth / 2f, CenterY + PanelHeight / 2f);
        var near = Vector2.Distance(local, lensCenter) <= ConoscopeReach;
        if (!near)
        {
            conoscopeTime = 0f;
            if (figureVisible) HideFigure();
            conoscopeText.text = "Passez le conoscope devant la gemme.";
            return;
        }

        var definitive = EvaluateFigure(out var message);
        conoscopeText.text = message;
        if (!definitive)
        {
            conoscopeTime = 0f;
            return;
        }

        conoscopeTime += Time.deltaTime;
        var key = ExaminationTool.MeasurementType.InterferenceFigure.ToString();
        if (conoscopeTime >= ConoscopeHold && !stone.HasObserved(key))
        {
            stone.RecordObservation(key);
            ToolFeedbackUI.Instance?.ShowMessage($"Conoscope — {OpticalDescriptions.ConoscopeText(stone.Definition)}");
            StoryState.Instance?.OnObserved(stone, key);
        }
    }

    // Draws the figure when an optic axis is within the conoscope's field. Returns true once the result is conclusive
    // (a figure on screen, or a stone that cannot give one).
    private bool EvaluateFigure(out string message)
    {
        switch (behaviour)
        {
            case LightBehaviour.Opaque:
                HideFigure();
                message = "Pierre opaque : aucune figure.";
                return true;
            case LightBehaviour.Dark:
                HideFigure();
                message = "Aucune figure d'interférence : pierre isotrope.";
                return true;
            case LightBehaviour.AlwaysLit:
                HideFigure();
                message = "Lumière diffuse, pas de figure exploitable (agrégat).";
                return true;
        }

        var axes = OpticAxes();
        var points = new List<Vector2>();
        var edge = Mathf.Sin(FieldHalfAngle * Mathf.Deg2Rad);
        foreach (var a in axes)
        {
            var p = new Vector2(a.x, a.y) / edge;
            if (p.magnitude <= 1.15f) points.Add(p);
        }

        // Uniaxial needs its axis in the field; biaxial shows a figure as soon as one axis is visible.
        if (points.Count == 0)
        {
            HideFigure();
            message = "Pas de figure : aucun axe optique dans le champ. Inclinez la pierre.";
            return false;
        }

        if (!figureVisible || Quaternion.Dot(lastFigureOrientation, orientation) < 0.99999f)
        {
            DrawFigure(axes);
            lastFigureOrientation = orientation;
        }

        figureImage.enabled = true;
        figureVisible = true;
        stoneImage.enabled = false;
        glowImage.enabled = false;
        message = biaxial ? "Figure d'interférence biaxe." : "Figure d'interférence uniaxe.";
        return true;
    }

    private void HideFigure()
    {
        figureVisible = false;
        if (figureImage != null) figureImage.enabled = false;
        if (stoneImage != null && stone != null)
        {
            stoneImage.enabled = true;
            glowImage.enabled = true;
        }
    }

    private void DrawFigure(List<Vector3> axes)
    {
        var edge = Mathf.Sin(FieldHalfAngle * Mathf.Deg2Rad);
        var c1 = new Vector2(axes[0].x, axes[0].y) / edge;
        var c2 = axes.Count > 1 ? new Vector2(axes[1].x, axes[1].y) / edge : c1;

        var depth = stone != null ? Mathf.Max(stone.DepthMm, 1f) : 4f;
        var edgeRetardation = Mathf.Clamp(birefringence * depth * 40000f, 700f, 7000f);

        var bullsEye = !biaxial && stone != null && stone.Definition != null && stone.Definition.isQuartzFamily;
        var pixels = new Color32[FigureSize * FigureSize];
        for (var y = 0; y < FigureSize; y++)
        {
            for (var x = 0; x < FigureSize; x++)
            {
                var u = (x + 0.5f) / FigureSize * 2f - 1f;
                var v = (y + 0.5f) / FigureSize * 2f - 1f;
                var r = Mathf.Sqrt(u * u + v * v);
                if (r >= 1f)
                {
                    pixels[y * FigureSize + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                var p = new Vector2(u, v);
                var rel1 = p - c1;
                float retardation, psi;
                if (!biaxial)
                {
                    retardation = edgeRetardation * rel1.sqrMagnitude;
                    psi = Mathf.Atan2(rel1.y, rel1.x);
                }
                else
                {
                    var rel2 = p - c2;
                    retardation = edgeRetardation * rel1.magnitude * rel2.magnitude;
                    psi = (Mathf.Atan2(rel1.y, rel1.x) + Mathf.Atan2(rel2.y, rel2.x)) * 0.5f;
                }

                // Isogyres (dark brushes) where the vibration direction matches a polarizer; colored rings elsewhere.
                var brush = Mathf.Pow(Mathf.Sin(2f * psi), 2f);
                var cr = Channel(retardation, 650f) * brush;
                var cg = Channel(retardation, 550f) * brush;
                var cb = Channel(retardation, 450f) * brush;

                // A quartz (and only a quartz) shows the bull's eye: the middle of the cross is a pale round spot.
                if (bullsEye)
                {
                    var spot = Mathf.Exp(-rel1.sqrMagnitude / 0.012f);
                    cr = Mathf.Max(cr, spot * 0.80f);
                    cg = Mathf.Max(cg, spot * 0.72f);
                    cb = Mathf.Max(cb, spot * 0.78f);
                }

                var alpha = Mathf.Clamp01((1f - r) * FigureSize * 0.5f);
                pixels[y * FigureSize + x] = new Color32(
                    (byte)(Mathf.Clamp01(cr * 1.2f + 0.02f) * 255f),
                    (byte)(Mathf.Clamp01(cg * 1.2f + 0.02f) * 255f),
                    (byte)(Mathf.Clamp01(cb * 1.2f + 0.03f) * 255f),
                    (byte)(alpha * 255f));
            }
        }

        figureTexture.SetPixels32(pixels);
        figureTexture.Apply();
    }

    private void UpdateConoscopeUi()
    {
        conoscopeButtonLabel.text = conoscopeHeld ? "Reposer le conoscope" : "Prendre le conoscope";
        conoscopeButton.GetComponent<Image>().color = conoscopeHeld ? ButtonOn : ButtonColor;
        if (!conoscopeHeld)
        {
            conoscopeTool.gameObject.SetActive(false);
            conoscopeText.text = "Le conoscope (lentille sphérique) montre la figure d'interférence quand on regarde le long d'un axe optique.";
            HideFigure();
        }
    }

    // ---- Display ---------------------------------------------------------------------------------------------

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            stoneImage.enabled = false;
            glowImage.enabled = false;
            HideFigure();
            hintText.text = "Déposez une gemme sur la lentille";
        }
        else
        {
            stoneImage.enabled = true;
            glowImage.enabled = true;
            hintText.text = "";
        }

        UpdateConoscopeUi();
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

    private void UpdateView()
    {
        ringVisual.localEulerAngles = new Vector3(0f, 0f, ringAngle);

        if (stone == null)
        {
            statusText.text = "";
            rotationText.text = "";
            return;
        }

        view3D.Draw(GemView3D.MeshFor(stone), GemView3D.HalfExtents(stone), orientation, Color.white, new Color(0.9f, 0.9f, 0.94f));
        var longest = Mathf.Max(stone.LengthMm, stone.WidthMm, 0.1f);
        var k = Mathf.Min(14f, 120f / longest);
        var side = GemView3D.MaxDimensionMm(stone) * k / GemView3D.FitFraction;
        stoneRect.sizeDelta = new Vector2(side, side);
        stoneRect.localEulerAngles = Vector3.zero;

        var color = StoneColor(out var brightness);
        stoneImage.color = color;
        glowImage.color = new Color(color.r, color.g, color.b, brightness * 0.5f);

        // While the conoscope's figure is on screen it replaces the stone.
        if (figureVisible)
        {
            stoneImage.enabled = false;
            glowImage.enabled = false;
        }
        else
        {
            stoneImage.enabled = true;
            glowImage.enabled = true;
        }

        var turned = Mathf.Min(explored, 360f);
        rotationText.text = $"Parcouru : {turned:F0}° / 360°";

        if (stone.HasObserved(ExaminationTool.MeasurementType.OpticalCharacter.ToString()))
            statusText.text = Conclusion();
        else if (behaviour == LightBehaviour.Opaque)
            statusText.text = "La pierre est opaque : aucune lumière ne la traverse.\nFaites-la tourner pour confirmer.";
        else
            statusText.text = "Faites tourner la gemme sous tous les angles et observez-la.";
    }

    private string Conclusion()
    {
        switch (behaviour)
        {
            case LightBehaviour.Opaque: return "Opaque : aucune lumière ne traverse la pierre.";
            case LightBehaviour.Dark: return "Reste éteinte pendant toute la rotation : isotrope (cubique ou amorphe).";
            case LightBehaviour.AlwaysLit: return "Reste éclairée pendant toute la rotation : agrégat polycristallin.";
            default: return "S'allume et s'éteint quatre fois par tour : anisotrope.";
        }
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("PolariscopePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        panelRect.anchoredPosition = new Vector2(-130f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Polariscope", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        // Black brushed plate like the real instrument.
        RuntimeUIKit.MakeBox("Plate", panel.transform, new Vector2(20f, -44f), new Vector2(380f, 380f), new Color(0.04f, 0.04f, 0.045f, 1f));

        BuildRing();
        BuildLens();

        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 13, TextAnchor.UpperLeft, Color.white,
            new Vector2(24f, -434f), new Vector2(372f, 36f));
        rotationText = RuntimeUIKit.MakeText("Rotation", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(24f, -470f), new Vector2(372f, 16f));
        conoscopeText = RuntimeUIKit.MakeText("ConoscopeStatus", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.85f, 1f),
            new Vector2(24f, -490f), new Vector2(372f, 32f));

        conoscopeButton = RuntimeUIKit.MakeButton("Conoscope", panel.transform, "Prendre le conoscope", 13, ButtonColor,
            new Vector2(24f, -536f), new Vector2(150f, 30f));
        conoscopeButtonLabel = conoscopeButton.GetComponentInChildren<Text>();
        conoscopeButton.onClick.AddListener(ToggleConoscope);

        var reset = RuntimeUIKit.MakeButton("Reset", panel.transform, "Remettre à plat", 13, ButtonColor,
            new Vector2(180f, -536f), new Vector2(100f, 30f));
        reset.onClick.AddListener(ResetOrientation);
        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer", 13, ButtonColor,
            new Vector2(286f, -536f), new Vector2(54f, 30f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonColor,
            new Vector2(346f, -536f), new Vector2(60f, 30f));
        close.onClick.AddListener(Close);

        var left = RuntimeUIKit.MakeButton("RotateLeft", panel.transform, "<", 14, new Color(0.18f, 0.18f, 0.22f, 1f),
            new Vector2(32f, -384f), new Vector2(40f, 28f));
        left.onClick.AddListener(() => Roll(10f));
        var right = RuntimeUIKit.MakeButton("RotateRight", panel.transform, ">", 14, new Color(0.18f, 0.18f, 0.22f, 1f),
            new Vector2(348f, -384f), new Vector2(40f, 28f));
        right.onClick.AddListener(() => Roll(-10f));

        BuildConoscopeTool();
        UpdateConoscopeUi();
    }

    private void BuildRing()
    {
        var holder = new GameObject("RingDial", typeof(RectTransform), typeof(Image), typeof(RefractometerDial));
        holder.transform.SetParent(panel.transform, false);
        CenterAt(holder.GetComponent<RectTransform>(), RingDiameter);
        holder.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        holder.GetComponent<RefractometerDial>().OnRotate = OnRingRotated;

        var visual = new GameObject("RingVisual", typeof(RectTransform), typeof(Image));
        visual.transform.SetParent(holder.transform, false);
        ringVisual = visual.GetComponent<RectTransform>();
        ringVisual.anchorMin = Vector2.zero;
        ringVisual.anchorMax = Vector2.one;
        ringVisual.offsetMin = Vector2.zero;
        ringVisual.offsetMax = Vector2.zero;
        var image = visual.GetComponent<Image>();
        image.sprite = MakeRingSprite(256);
        image.raycastTarget = false;

        // Engraved name along the top of the ring (fixed, it does not rotate with the knob).
        const string engraved = "POLARISCOPE";
        const float radius = 128f;
        for (var i = 0; i < engraved.Length; i++)
        {
            var theta = (90f - (i - (engraved.Length - 1) / 2f) * 10.5f) * Mathf.Deg2Rad;
            var letter = RuntimeUIKit.MakeText("Letter", panel.transform, engraved[i].ToString(), 17, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, new Vector2(24f, 24f));
            letter.fontStyle = FontStyle.Bold;
            var letterRect = letter.rectTransform;
            letterRect.pivot = new Vector2(0.5f, 0.5f);
            letterRect.anchoredPosition = new Vector2(CenterX + radius * Mathf.Cos(theta), CenterY + radius * Mathf.Sin(theta));
            letterRect.localEulerAngles = new Vector3(0f, 0f, theta * Mathf.Rad2Deg - 90f);
        }
    }

    private void BuildLens()
    {
        var lens = new GameObject("Lens", typeof(RectTransform), typeof(Image), typeof(LapidarySlotDropZone), typeof(DichroscopeDragArea));
        lens.transform.SetParent(panel.transform, false);
        CenterAt(lens.GetComponent<RectTransform>(), LensDiameter);
        lens.GetComponent<Image>().sprite = MakeFieldSprite(192);
        lens.GetComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        var drag = lens.GetComponent<DichroscopeDragArea>();
        drag.OnDragDelta = OnTilt;
        drag.OnRoll = Roll;

        var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(lens.transform, false);
        var glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(170f, 170f);
        glowRect.anchoredPosition = Vector2.zero;
        glowImage = glow.GetComponent<Image>();
        glowImage.sprite = RuntimeUIKit.Glow;
        glowImage.raycastTarget = false;
        glowImage.enabled = false;

        var gem = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gem.transform.SetParent(lens.transform, false);
        stoneRect = gem.GetComponent<RectTransform>();
        stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        stoneRect.sizeDelta = new Vector2(84f, 84f);
        stoneRect.anchoredPosition = Vector2.zero;
        stoneImage = gem.GetComponent<Image>();
        stoneImage.raycastTarget = false;
        stoneImage.enabled = false;
        // The stone is drawn in 3D pixel art (white, tinted by the polarized light), so it has real volume.
        view3D = new GemView3D(48);
        stoneImage.sprite = view3D.Sprite;

        // Interference figure seen through the conoscope.
        var figure = new GameObject("Figure", typeof(RectTransform), typeof(Image));
        figure.transform.SetParent(lens.transform, false);
        var figureRect = figure.GetComponent<RectTransform>();
        figureRect.anchorMin = figureRect.anchorMax = new Vector2(0.5f, 0.5f);
        figureRect.sizeDelta = new Vector2(190f, 190f);
        figureRect.anchoredPosition = Vector2.zero;
        figureTexture = new Texture2D(FigureSize, FigureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        figureImage = figure.GetComponent<Image>();
        figureImage.sprite = Sprite.Create(figureTexture, new Rect(0, 0, FigureSize, FigureSize), new Vector2(0.5f, 0.5f), 100f);
        figureImage.raycastTarget = false;
        figureImage.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", lens.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.55f, 0.65f, 0.85f),
            Vector2.zero, new Vector2(LensDiameter, 24f));
        var hintRect = hintText.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.sizeDelta = new Vector2(LensDiameter - 20f, 24f);
        hintRect.anchoredPosition = new Vector2(0f, -60f);
    }

    // The conoscope: a clear rod ending in a glass ball. Its ball lens sits at the cursor.
    private void BuildConoscopeTool()
    {
        var tool = new GameObject("ConoscopeTool", typeof(RectTransform));
        tool.transform.SetParent(panel.transform, false);
        conoscopeTool = tool.GetComponent<RectTransform>();
        conoscopeTool.anchorMin = conoscopeTool.anchorMax = new Vector2(0f, 1f);
        conoscopeTool.pivot = new Vector2(0.5f, 0.5f);
        conoscopeTool.sizeDelta = new Vector2(1f, 1f);

        var rod = new GameObject("Rod", typeof(RectTransform), typeof(Image));
        rod.transform.SetParent(tool.transform, false);
        var rodRect = rod.GetComponent<RectTransform>();
        rodRect.anchorMin = rodRect.anchorMax = new Vector2(0.5f, 0.5f);
        rodRect.pivot = new Vector2(0f, 0.5f);
        rodRect.sizeDelta = new Vector2(150f, 16f);
        rodRect.anchoredPosition = new Vector2(10f, -4f);
        rodRect.localEulerAngles = new Vector3(0f, 0f, -35f);
        var rodImage = rod.GetComponent<Image>();
        rodImage.sprite = RuntimeUIKit.Rounded;
        rodImage.type = Image.Type.Sliced;
        rodImage.color = new Color(0.88f, 0.92f, 0.96f, 0.80f);
        rodImage.raycastTarget = false;

        var ball = new GameObject("Ball", typeof(RectTransform), typeof(Image));
        ball.transform.SetParent(tool.transform, false);
        var ballRect = ball.GetComponent<RectTransform>();
        ballRect.anchorMin = ballRect.anchorMax = new Vector2(0.5f, 0.5f);
        ballRect.sizeDelta = new Vector2(46f, 46f);
        ballRect.anchoredPosition = Vector2.zero;
        var ballImage = ball.GetComponent<Image>();
        ballImage.sprite = MakeBallSprite(64);
        ballImage.raycastTarget = false;

        tool.SetActive(false);
    }

    // Centered (pivot 0.5) square placed relative to the panel's top-left corner.
    private static void CenterAt(RectTransform rect, float size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(CenterX, CenterY);
    }

    // Knurled black ring with a white index mark at the top.
    private static Sprite MakeRingSprite(int size)
    {
        var texture = NewTexture(size);
        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01((1f - r) * center);
                if (r > 1f || r < 0.64f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                var angle = Mathf.Atan2(dy, dx);
                var shade = r > 0.93f ? 0.20f + 0.07f * Mathf.Sin(angle * 90f)
                    : 0.13f + 0.03f * (-dx - dy) * 0.5f;
                var color = new Color(shade, shade, shade * 1.05f, alpha);

                if (r > 0.66f && r < 0.74f && Mathf.Abs(dx) < 0.025f && dy > 0f)
                    color = new Color(0.95f, 0.95f, 0.95f, alpha);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Dark blue field of the analyzer.
    private static Sprite MakeFieldSprite(int size)
    {
        var texture = NewTexture(size);
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

                var sheen = Mathf.Clamp01(1f - r) * 0.5f + 0.1f * Mathf.Clamp01(-dy);
                var color = new Color(0.03f + 0.05f * sheen, 0.06f + 0.12f * sheen, 0.12f + 0.28f * sheen, Mathf.Clamp01((1f - r) * center));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Clear glass ball with a soft highlight.
    private static Sprite MakeBallSprite(int size)
    {
        var texture = NewTexture(size);
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

                var rim = Mathf.SmoothStep(0.6f, 1f, r);
                var highlight = Mathf.Clamp01(1f - Mathf.Sqrt(Mathf.Pow(dx + 0.35f, 2f) + Mathf.Pow(dy - 0.35f, 2f)) * 2.2f);
                var shade = 0.86f + 0.10f * highlight - 0.12f * rim;
                var alpha = Mathf.Clamp01((1f - r) * center) * (0.30f + 0.45f * rim + 0.4f * highlight);
                texture.SetPixel(x, y, new Color(shade, shade, shade * 1.02f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D NewTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
    }
}
