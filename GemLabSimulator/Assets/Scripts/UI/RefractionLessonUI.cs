using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The lesson on the principle of the refractometer, given by Professor Thouvenot with an animation of the light: the refraction at the surface of two media, the part
// that is reflected, the passage from a high index to a lower one, the critical angle and the total reflection, the shadow edge that results (what the eyepiece shows),
// and why a drop of contact liquid is needed. Built at runtime; each step has its text and its own animation.
public class RefractionLessonUI : MonoBehaviour
{
    private static GameObject current;

    public static bool IsOpen => current != null;

    private const float PanelWidth = 820f;
    private const float PanelHeight = 520f;
    private const float AreaWidth = 480f;
    private const float AreaHeight = 340f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Incident = new Color(1f, 0.90f, 0.35f, 1f);
    private static readonly Color Reflected = new Color(0.55f, 0.90f, 1f, 1f);
    private static readonly Color Refracted = new Color(1f, 0.58f, 0.20f, 1f);

    private class Step
    {
        public string Title;
        public string Text;
        public int Mode;
        public float N1, N2;                 // the index above and below the surface
        public string Top, Bottom;           // the names of the two media
        public string[] Pics;                // mode 7: the pictures of the eyepiece to show (ids of RefractoPictures)
    }

    private static readonly Step[] Steps =
    {
        new Step
        {
            Title = "La réfraction", Mode = 0, N1 = 1.00f, N2 = 1.50f, Top = "Air  (n = 1,00)", Bottom = "Verre  (n = 1,50)",
            Text = "Quand la lumière passe d'un milieu à un autre, elle change de direction : c'est la <b>réfraction</b>.\\n\\n" +
                   "Le trait pointillé est la <b>normale</b>, perpendiculaire à la surface. En entrant dans un milieu d'indice plus élevé, le rayon se rapproche de la normale.\\n\\n" +
                   "La loi de Descartes : <b>n₁ × sin θ₁ = n₂ × sin θ₂</b>.",
        },
        new Step
        {
            Title = "La réflexion", Mode = 1, N1 = 1.00f, N2 = 1.50f, Top = "Air  (n = 1,00)", Bottom = "Verre  (n = 1,50)",
            Text = "Une partie de la lumière ne traverse pas la surface : elle est <b>réfléchie</b>, avec le même angle que le rayon qui arrive, de l'autre côté de la normale.\\n\\n" +
                   "Un rayon qui arrive donne donc deux rayons : un réfracté, un réfléchi.",
        },
        new Step
        {
            Title = "De l'indice fort à l'indice faible", Mode = 2, N1 = 1.81f, N2 = 1.54f, Top = "Verre du réfractomètre  (n = 1,81)", Bottom = "Pierre  (n = 1,54)",
            Text = "Dans le réfractomètre, la lumière arrive par le verre (indice <b>1,81</b>, élevé) et rencontre la pierre (indice plus faible).\\n\\n" +
                   "Cette fois, le rayon qui entre dans la pierre <b>s'écarte de la normale</b>. Plus l'angle d'arrivée augmente, plus il se couche sur la surface.\\n\\n" +
                   "Il arrive un moment où il la rase : c'est l'<b>angle critique</b> θc, avec sin θc = n(pierre) ÷ n(verre).",
        },
        new Step
        {
            Title = "L'angle critique", Mode = 3, N1 = 1.81f, N2 = 1.54f, Top = "Verre du réfractomètre  (n = 1,81)", Bottom = "Pierre  (n = 1,54)",
            Text = "Au-delà de l'angle critique, plus rien n'entre dans la pierre : toute la lumière est renvoyée dans le verre. C'est la <b>réflexion totale</b>.\\n\\n" +
                   "L'angle critique ne dépend que des deux indices : en le mesurant, on retrouve l'indice de la pierre.",
        },
        new Step
        {
            Title = "La limite d'ombre", Mode = 4, N1 = 1.81f, N2 = 1.54f, Top = "Verre du réfractomètre  (n = 1,81)", Bottom = "Pierre  (n = 1,54)",
            Text = "On regarde la lumière réfléchie. Les rayons plus inclinés que θc sont totalement réfléchis : ils arrivent à l'oculaire, <b>zone claire</b>. Les autres entrent dans la pierre et manquent à l'appel : <b>zone sombre</b>.\\n\\n" +
                   "La frontière est la <b>limite d'ombre</b>. Sa position sur l'échelle donne directement l'indice : n(pierre) = 1,81 × sin θc. L'échelle de l'oculaire est déjà graduée en indices.",
        },
        new Step
        {
            Title = "La goutte de liquide", Mode = 5, N1 = 1.81f, N2 = 1.54f, Top = "Verre du réfractomètre  (n = 1,81)", Bottom = "Pierre  (n = 1,54)",
            Text = "Une pierre posée à nu sur le verre en reste séparée par une mince <b>lame d'air</b> : le rayon se heurte à l'air (indice 1,00), il est réfléchi, et la lecture est fausse.\\n\\n" +
                   "On dépose donc d'abord <b>une goutte de liquide de contact</b> (indice élevé, vers 1,81) : elle remplit l'espace, et la lumière passe du verre à la pierre. <b>La goutte d'abord, la pierre ensuite.</b>",
        },
        new Step
        {
            Title = "Une ou deux limites", Mode = 6, N1 = 1.81f, N2 = 1.54f, Top = "Pierre isotrope", Bottom = "Pierre anisotrope",
            Text = "Une pierre <b>isotrope</b> (grenat, spinelle, verre) a un seul indice : une seule limite d'ombre, qui ne bouge pas.\\n\\n" +
                   "Une pierre <b>anisotrope</b> a plusieurs indices selon la direction : on voit <b>deux limites</b> qui se déplacent quand on tourne la lentille. On note les valeurs extrêmes : ce sont nα, nβ, nγ.\\n\\n" +
                   "Les onglets, à gauche du réfractomètre, vous aideront à interpréter vos relevés.",
        },
    };

    private Action onDone;
    private int stepIndex;
    private float clock;
    private RectTransform area;
    private Text titleText;
    private Text bodyText;
    private Text topLabel;
    private Text bottomLabel;
    private Text infoLabel;
    private Text angleLabel;
    private Button previous;
    private Button next;
    private Text nextLabel;
    private Image topMedium;
    private Image bottomMedium;
    private Image gap;
    private Image stoneBlock;
    private Image surfaceLine;
    private readonly List<Image> rays = new List<Image>();
    private readonly List<Image> normalDashes = new List<Image>();
    private readonly List<Image> bars = new List<Image>();
    private Text edgeLabel;
    private RectTransform picturesRoot;

    public static void Show(Action done)
    {
        Close();
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            done?.Invoke();
            return;
        }

        var go = new GameObject("RefractionLessonPanel", typeof(RectTransform), typeof(Image), typeof(RefractionLessonUI));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(go.GetComponent<Image>());
        var ui = go.GetComponent<RefractionLessonUI>();
        ui.onDone = done;
        ui.Build();
        current = go;
        ui.ShowStep(0);
    }

    public static void Close()
    {
        if (current != null)
        {
            // Inactive at once, so the pause menu does not take the Escape that closed it.
            current.SetActive(false);
            Destroy(current);
        }
        current = null;
    }

    private void OnDestroy()
    {
        if (current == gameObject) current = null;
    }

    private void Build()
    {
        var title = RuntimeUIKit.MakeText("Title", transform, "Le principe du réfractomètre", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -18f), new Vector2(PanelWidth - 60f, 30f));
        title.fontStyle = FontStyle.Bold;

        // The picture: a dark board where the light is drawn from the middle (the surface is the horizontal line through the centre).
        var board = RuntimeUIKit.MakeBox("Board", transform, new Vector2(30f, -62f), new Vector2(AreaWidth, AreaHeight), new Color(0.05f, 0.06f, 0.09f, 1f));
        board.raycastTarget = false;
        var areaGO = new GameObject("Area", typeof(RectTransform), typeof(RectMask2D));
        areaGO.transform.SetParent(board.transform, false);
        area = areaGO.GetComponent<RectTransform>();
        area.anchorMin = area.anchorMax = new Vector2(0.5f, 0.5f);
        area.pivot = new Vector2(0.5f, 0.5f);
        area.sizeDelta = new Vector2(AreaWidth, AreaHeight);
        area.anchoredPosition = Vector2.zero;

        topMedium = Slab("Top", new Vector2(0f, AreaHeight / 4f), new Vector2(AreaWidth, AreaHeight / 2f), new Color(0.35f, 0.55f, 0.75f, 0.28f));
        bottomMedium = Slab("Bottom", new Vector2(0f, -AreaHeight / 4f), new Vector2(AreaWidth, AreaHeight / 2f), new Color(0.75f, 0.65f, 0.45f, 0.30f));
        gap = Slab("Gap", new Vector2(0f, 0f), new Vector2(AreaWidth, 8f), new Color(1f, 1f, 1f, 0.25f));
        stoneBlock = Slab("Stone", new Vector2(0f, -AreaHeight / 4f - 6f), new Vector2(AreaWidth, AreaHeight / 2f - 12f), new Color(0.70f, 0.40f, 0.85f, 0.35f));
        surfaceLine = Slab("Surface", Vector2.zero, new Vector2(AreaWidth, 2f), new Color(0.9f, 0.9f, 0.95f, 0.8f));

        // The normal, dashed.
        for (var i = 0; i < 16; i++)
        {
            var dash = Slab("Dash", new Vector2(0f, -AreaHeight / 2f + 10f + i * 21.2f), new Vector2(2f, 10f), new Color(1f, 1f, 1f, 0.45f));
            normalDashes.Add(dash);
        }

        // The bars of the eyepiece, for the shadow edge.
        for (var i = 0; i < 12; i++) bars.Add(Slab("Bar", Vector2.zero, new Vector2(10f, 10f), Color.white));
        for (var i = 0; i < 16; i++)
        {
            var ray = Slab("Ray", Vector2.zero, new Vector2(10f, 3f), Color.white);
            ray.rectTransform.pivot = new Vector2(0f, 0.5f);
            rays.Add(ray);
        }

        // For the steps with pictures of the eyepiece: they replace the drawing of the light.
        var picturesGO = new GameObject("Pictures", typeof(RectTransform), typeof(VerticalLayoutGroup));
        picturesGO.transform.SetParent(board.transform, false);
        picturesRoot = picturesGO.GetComponent<RectTransform>();
        picturesRoot.anchorMin = Vector2.zero;
        picturesRoot.anchorMax = Vector2.one;
        picturesRoot.offsetMin = new Vector2(10f, 8f);
        picturesRoot.offsetMax = new Vector2(-10f, -8f);
        var picturesLayout = picturesGO.GetComponent<VerticalLayoutGroup>();
        picturesLayout.spacing = 8f;
        picturesLayout.childAlignment = TextAnchor.MiddleCenter;
        picturesLayout.childControlWidth = true;
        picturesLayout.childControlHeight = true;
        picturesLayout.childForceExpandWidth = false;
        picturesLayout.childForceExpandHeight = false;
        picturesGO.SetActive(false);

        topLabel = AreaText("TopLabel", new Vector2(-AreaWidth / 2f + 8f, AreaHeight / 2f - 26f), TextAnchor.UpperLeft, 13, new Color(0.80f, 0.90f, 1f));
        bottomLabel = AreaText("BottomLabel", new Vector2(-AreaWidth / 2f + 8f, -AreaHeight / 2f + 8f), TextAnchor.LowerLeft, 13, new Color(1f, 0.92f, 0.75f));
        infoLabel = AreaText("Info", new Vector2(-AreaWidth / 2f + 8f, 6f), TextAnchor.LowerLeft, 13, new Color(0.95f, 0.95f, 1f));
        angleLabel = AreaText("Angles", new Vector2(AreaWidth / 2f - 220f, AreaHeight / 2f - 26f), TextAnchor.UpperRight, 13, Incident);
        edgeLabel = AreaText("Edge", Vector2.zero, TextAnchor.MiddleLeft, 12, new Color(0.60f, 1f, 0.70f));
        edgeLabel.gameObject.SetActive(false);

        // The text of the step.
        titleText = RuntimeUIKit.MakeText("StepTitle", transform, "", 18, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(530f, -62f), new Vector2(PanelWidth - 560f, 26f));
        titleText.fontStyle = FontStyle.Bold;
        bodyText = RuntimeUIKit.MakeText("StepText", transform, "", 14, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 0.98f), new Vector2(530f, -94f), new Vector2(PanelWidth - 560f, 310f));
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.supportRichText = true;
        bodyText.lineSpacing = 1.08f;

        previous = RuntimeUIKit.MakeButton("Previous", transform, "< Précédent", 14, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(530f, -PanelHeight + 60f), new Vector2(120f, 34f));
        previous.onClick.AddListener(() => ShowStep(stepIndex - 1));
        next = RuntimeUIKit.MakeButton("Next", transform, "Suivant >", 14, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(660f, -PanelHeight + 60f), new Vector2(130f, 34f));
        nextLabel = next.GetComponentInChildren<Text>();
        next.onClick.AddListener(() =>
        {
            if (stepIndex < Steps.Length - 1) ShowStep(stepIndex + 1);
            else Finish();
        });
        StardewKit.Skin(transform);
    }

    private Image Slab(string name, Vector2 center, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(area, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text AreaText(string name, Vector2 position, TextAnchor anchor, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(area, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(AreaWidth - 20f, 24f);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void ShowStep(int index)
    {
        stepIndex = Mathf.Clamp(index, 0, Steps.Length - 1);
        clock = 0f;
        var step = Steps[stepIndex];
        titleText.text = $"{stepIndex + 1} / {Steps.Length}   {step.Title}";
        bodyText.text = step.Text.Replace("\\n", "\n");
        topLabel.text = step.Top;
        bottomLabel.text = step.Bottom;
        previous.interactable = stepIndex > 0;
        nextLabel.text = stepIndex < Steps.Length - 1 ? "Suivant >" : "Terminer";
        Sfx.Play(Sfx.Id.Click, 0.4f);

        // What shows depends on the step.
        gap.enabled = step.Mode == 5;
        stoneBlock.enabled = step.Mode == 5;
        var pictures = step.Mode == 7;
        topMedium.enabled = step.Mode != 6 && !pictures;
        bottomMedium.enabled = step.Mode != 5 && step.Mode != 6 && !pictures;
        surfaceLine.enabled = step.Mode != 6 && !pictures;
        topLabel.text = step.Mode == 6 || pictures ? "" : step.Top;
        bottomLabel.text = step.Mode == 6 || pictures ? "" : step.Bottom;
        foreach (var dash in normalDashes) dash.enabled = step.Mode <= 3 || step.Mode == 5 || step.Mode == 8;
        edgeLabel.gameObject.SetActive(step.Mode == 4);

        // The pictures of the eyepiece, for the steps that show the situations of the shadow edges.
        for (var i = picturesRoot.childCount - 1; i >= 0; i--) Destroy(picturesRoot.GetChild(i).gameObject);
        picturesRoot.gameObject.SetActive(pictures);
        if (pictures && step.Pics != null)
            foreach (var id in step.Pics) RefractoPictures.Build(id, picturesRoot, new RefractoGuide.Situation());
    }

    private void Finish()
    {
        var done = onDone;
        onDone = null;
        Close();
        done?.Invoke();
    }

    // ---- The animation -----------------------------------------------------------------------------------------------------

    private int used;

    private void Ray(Vector2 from, Vector2 to, Color color, float thickness = 3f)
    {
        if (used >= rays.Count) return;
        var ray = rays[used++];
        ray.enabled = true;
        var rect = ray.rectTransform;
        rect.anchoredPosition = from;
        var delta = to - from;
        rect.sizeDelta = new Vector2(delta.magnitude, thickness);
        rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        ray.color = color;
    }

    // A ray coming from the upper left with the angle theta (degrees, from the normal) toward the point of impact, at the middle of the board.
    private static Vector2 Source(float thetaDegrees, float length)
    {
        var t = thetaDegrees * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(t), Mathf.Cos(t)) * length;
    }

    private static Vector2 Under(float thetaDegrees, float length)
    {
        var t = thetaDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(t), -Mathf.Cos(t)) * length;
    }

    private void Update()
    {
        if (area == null) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Finish();
            return;
        }
        clock += Time.unscaledDeltaTime;
        used = 0;
        var step = Steps[stepIndex];
        var criticalAngle = step.N1 > step.N2 ? Mathf.Asin(step.N2 / step.N1) * Mathf.Rad2Deg : 90f;
        var wave = 0.5f + 0.5f * Mathf.Sin(clock * 0.9f);
        angleLabel.text = "";
        infoLabel.text = "";

        switch (step.Mode)
        {
            case 0:
            case 1:
            {
                var theta1 = Mathf.Lerp(12f, 70f, wave);
                var sin2 = step.N1 / step.N2 * Mathf.Sin(theta1 * Mathf.Deg2Rad);
                var theta2 = Mathf.Asin(Mathf.Clamp01(sin2)) * Mathf.Rad2Deg;
                Ray(Source(theta1, 200f), Vector2.zero, Incident);
                Ray(Vector2.zero, Under(theta2, 190f), Refracted);
                if (step.Mode == 1) Ray(Vector2.zero, Source(-theta1, 190f), Reflected);
                angleLabel.text = $"θ₁ = {theta1:F0}°     θ₂ = {theta2:F0}°";
                infoLabel.text = step.Mode == 1 ? "Rayon réfléchi : même angle que le rayon incident" : "n₁ sin θ₁ = n₂ sin θ₂";
                break;
            }
            case 2:
            {
                var theta1 = Mathf.Lerp(10f, criticalAngle - 0.8f, wave);
                var theta2 = Mathf.Asin(Mathf.Clamp01(step.N1 / step.N2 * Mathf.Sin(theta1 * Mathf.Deg2Rad))) * Mathf.Rad2Deg;
                Ray(Source(theta1, 200f), Vector2.zero, Incident);
                Ray(Vector2.zero, Under(theta2, 190f), Refracted);
                Ray(Vector2.zero, Source(-theta1, 190f), new Color(Reflected.r, Reflected.g, Reflected.b, 0.35f));
                angleLabel.text = $"θ₁ = {theta1:F0}°     θ₂ = {theta2:F0}°";
                infoLabel.text = $"Angle critique θc = {criticalAngle:F1}°   (sin θc = {step.N2:F2} ÷ {step.N1:F2})";
                break;
            }
            case 3:
            {
                var theta1 = Mathf.Lerp(30f, 82f, wave);
                var total = theta1 >= criticalAngle;
                Ray(Source(theta1, 200f), Vector2.zero, Incident);
                if (!total)
                {
                    var theta2 = Mathf.Asin(Mathf.Clamp01(step.N1 / step.N2 * Mathf.Sin(theta1 * Mathf.Deg2Rad))) * Mathf.Rad2Deg;
                    Ray(Vector2.zero, Under(theta2, 190f), Refracted);
                    Ray(Vector2.zero, Source(-theta1, 190f), new Color(Reflected.r, Reflected.g, Reflected.b, 0.35f));
                }
                else Ray(Vector2.zero, Source(-theta1, 190f), Reflected, 4f);
                angleLabel.text = $"θ₁ = {theta1:F0}°   (θc = {criticalAngle:F1}°)";
                infoLabel.text = total ? "Réflexion totale : rien n'entre dans la pierre" : "Une partie entre dans la pierre";
                break;
            }
            case 4:
                FanScene(step, criticalAngle);
                break;
            case 5:
                ContactScene(step);
                break;
            case 7:
                break;
            case 8:
                SplitScene();
                break;
            default:
                TwoEdgesScene();
                break;
        }

        for (var i = used; i < rays.Count; i++) rays[i].enabled = false;
        if (step.Mode != 4 && step.Mode != 6) foreach (var bar in bars) bar.enabled = false;
    }

    // Rays of all the angles: the ones under the critical angle enter the stone (dark), the others come back to the eyepiece (bright).
    private void FanScene(Step step, float criticalAngle)
    {
        const float barX = 150f;
        var first = 38f;
        var last = 86f;
        var count = bars.Count;
        var breathe = 0.85f + 0.15f * Mathf.Sin(clock * 2f);
        for (var i = 0; i < count; i++)
        {
            var theta = Mathf.Lerp(first, last, i / (float)(count - 1));
            var bright = theta >= criticalAngle;
            var y = barX / Mathf.Tan(theta * Mathf.Deg2Rad);
            var yNext = barX / Mathf.Tan(Mathf.Lerp(first, last, Mathf.Min(count - 1, i + 1) / (float)(count - 1)) * Mathf.Deg2Rad);
            var height = Mathf.Max(6f, Mathf.Abs(y - yNext) * 0.95f);

            if (bright) Ray(Vector2.zero, new Vector2(barX, y), new Color(Reflected.r, Reflected.g, Reflected.b, 0.55f * breathe), 2f);
            else Ray(Vector2.zero, Under(Mathf.Asin(Mathf.Clamp01(step.N1 / step.N2 * Mathf.Sin(theta * Mathf.Deg2Rad))) * Mathf.Rad2Deg, 120f), new Color(Refracted.r, Refracted.g, Refracted.b, 0.5f), 2f);

            var bar = bars[i];
            bar.enabled = true;
            bar.rectTransform.anchoredPosition = new Vector2(barX + 14f, y);
            bar.rectTransform.sizeDelta = new Vector2(16f, height);
            bar.color = bright ? new Color(0.97f, 0.97f, 0.92f, 1f) : new Color(0.12f, 0.12f, 0.15f, 1f);
        }

        // The incident ray that sweeps over them.
        var sweep = Mathf.Lerp(first, last, 0.5f + 0.5f * Mathf.Sin(clock * 0.8f));
        Ray(Source(sweep, 200f), Vector2.zero, Incident, 2f);

        var edgeY = barX / Mathf.Tan(criticalAngle * Mathf.Deg2Rad);
        edgeLabel.rectTransform.anchoredPosition = new Vector2(barX - 120f, edgeY + 6f);
        edgeLabel.text = "limite d'ombre  →";
        infoLabel.text = $"Oculaire : zone sombre sous θc = {criticalAngle:F1}°, zone claire au-dessus";
    }

    // With air between the glass and the stone, the ray is reflected; with the drop of liquid it goes through.
    private void ContactScene(Step step)
    {
        var withLiquid = Mathf.Repeat(clock, 6f) > 3f;
        gap.color = withLiquid ? new Color(0.50f, 0.90f, 0.85f, 0.75f) : new Color(1f, 1f, 1f, 0.30f);
        gap.rectTransform.anchoredPosition = new Vector2(0f, -4f);
        gap.rectTransform.sizeDelta = new Vector2(AreaWidth, 8f);
        topMedium.color = new Color(0.35f, 0.55f, 0.75f, 0.28f);
        stoneBlock.color = new Color(0.70f, 0.40f, 0.85f, 0.35f);

        var theta = 48f;
        Ray(Source(theta, 190f), new Vector2(0f, 0f), Incident);
        if (withLiquid)
        {
            var theta2 = Mathf.Asin(Mathf.Clamp01(1.81f / 1.54f * Mathf.Sin(theta * Mathf.Deg2Rad) * 0.9f)) * Mathf.Rad2Deg;
            Ray(new Vector2(0f, -4f), new Vector2(0f, -4f) + Under(theta2, 150f), Refracted);
            infoLabel.text = "Avec la goutte : la lumière passe du verre à la pierre (lecture juste)";
        }
        else
        {
            Ray(Vector2.zero, Source(-theta, 190f), Reflected, 4f);
            infoLabel.text = "Avec une lame d'air : tout est réfléchi, la lecture est fausse";
        }
        angleLabel.text = withLiquid ? "Liquide de contact" : "Air (n = 1,00)";
    }

    // A ray that enters an anisotropic crystal and splits in two: each one has its own index, so its own direction (the gap is exaggerated).
    private void SplitScene()
    {
        const float nOrdinary = 1.50f;
        const float nExtraordinary = 1.66f;
        var theta1 = Mathf.Lerp(18f, 68f, 0.5f + 0.5f * Mathf.Sin(clock * 0.9f));
        var sin1 = Mathf.Sin(theta1 * Mathf.Deg2Rad);
        var theta2 = Mathf.Asin(sin1 / nOrdinary) * Mathf.Rad2Deg;
        var theta3 = Mathf.Asin(sin1 / nExtraordinary) * Mathf.Rad2Deg;
        Ray(Source(theta1, 200f), Vector2.zero, Incident);
        Ray(Vector2.zero, Under(theta2, 190f), Refracted);
        Ray(Vector2.zero, Under(theta3, 190f), Reflected);
        Ray(Vector2.zero, Source(-theta1, 150f), new Color(1f, 1f, 1f, 0.25f));
        angleLabel.text = $"Δn = nγ − nα = {nExtraordinary - nOrdinary:F2}  (exagéré)";
        infoLabel.text = "Un rayon entre, deux rayons en sortent : la lumière est dédoublée";
        edgeLabel.gameObject.SetActive(true);
        edgeLabel.rectTransform.anchoredPosition = new Vector2(-AreaWidth / 2f + 8f, -AreaHeight / 2f + 34f);
        edgeLabel.text = "rayon ordinaire (orange)  /  rayon extraordinaire (bleu)";
    }

    // One edge that stands still (isotropic), two edges that move (anisotropic): two small eyepieces side by side.
    private void TwoEdgesScene()
    {
        const float width = 150f;
        const float height = 200f;
        // Draw with the bars: left eyepiece (one edge), right eyepiece (two edges).
        var move = Mathf.Sin(clock * 1.2f);
        var left = new[] { 0.52f };
        var right = new[] { 0.40f + 0.08f * move, 0.62f - 0.07f * move };
        for (var i = 0; i < bars.Count; i++) bars[i].enabled = false;

        void Eyepiece(int index, float centerX, float[] edges)
        {
            // dark above the first edge, grey between, bright under the last
            var top = height / 2f;
            var bottom = -height / 2f;
            var firstEdge = Mathf.Lerp(top, bottom, edges[0]);
            var lastEdge = Mathf.Lerp(top, bottom, edges[edges.Length - 1]);
            Bar(index * 3, centerX, (top + firstEdge) / 2f, width, top - firstEdge, new Color(0.12f, 0.12f, 0.15f, 1f));
            if (edges.Length > 1) Bar(index * 3 + 1, centerX, (firstEdge + lastEdge) / 2f, width, firstEdge - lastEdge, new Color(0.52f, 0.52f, 0.55f, 1f));
            Bar(index * 3 + 2, centerX, (lastEdge + bottom) / 2f, width, lastEdge - bottom, new Color(0.96f, 0.96f, 0.92f, 1f));
        }

        Eyepiece(0, -110f, left);
        Eyepiece(1, 110f, right);
        infoLabel.text = "Une limite fixe (à gauche)    Deux limites qui bougent (à droite)";
    }

    private void Bar(int index, float centerX, float centerY, float width, float height, Color color)
    {
        if (index >= bars.Count) return;
        var bar = bars[index];
        bar.enabled = height > 0.5f;
        bar.rectTransform.anchoredPosition = new Vector2(centerX, centerY);
        bar.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(0f, height));
        bar.color = color;
    }
}
