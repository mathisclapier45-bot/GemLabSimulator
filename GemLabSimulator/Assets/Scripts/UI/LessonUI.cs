using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A small immediate-mode drawing board for the lessons: every frame the figure asks for rectangles, lines, circles and labels, and they are taken from a pool.
public class LessonCanvas
{
    private readonly RectTransform imageRoot;
    private readonly RectTransform textRoot;
    private readonly List<Image> images = new List<Image>();
    private readonly List<Text> texts = new List<Text>();
    private int usedImages;
    private int usedTexts;
    private static Sprite circleSprite;

    public LessonCanvas(RectTransform area)
    {
        imageRoot = Layer(area, "Shapes");
        textRoot = Layer(area, "Labels");
    }

    private static RectTransform Layer(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size / 2f - distance + 0.5f)));
            }
        }
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    public void Begin()
    {
        usedImages = 0;
        usedTexts = 0;
    }

    public void End()
    {
        for (var i = usedImages; i < images.Count; i++) images[i].gameObject.SetActive(false);
        for (var i = usedTexts; i < texts.Count; i++) texts[i].gameObject.SetActive(false);
    }

    private Image NextImage()
    {
        if (usedImages == images.Count)
        {
            var go = new GameObject("Shape", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(imageRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var created = go.GetComponent<Image>();
            created.raycastTarget = false;
            images.Add(created);
        }
        var image = images[usedImages++];
        image.gameObject.SetActive(true);
        return image;
    }

    public void Rect(Vector2 center, Vector2 size, Color color, float angle = 0f)
    {
        var image = NextImage();
        image.sprite = null;
        image.color = color;
        var rect = image.rectTransform;
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        rect.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    public void Line(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var delta = to - from;
        Rect((from + to) / 2f, new Vector2(delta.magnitude, thickness), color, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    public void Circle(Vector2 center, float radius, Color color)
    {
        var image = NextImage();
        image.sprite = CircleSprite();
        image.color = color;
        var rect = image.rectTransform;
        rect.anchoredPosition = center;
        rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        rect.localEulerAngles = Vector3.zero;
    }

    // A picture (a sprite) drawn on the board.
    public void Picture(Sprite sprite, Vector2 center, Vector2 size)
    {
        var image = NextImage();
        image.sprite = sprite;
        image.color = Color.white;
        var rect = image.rectTransform;
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        rect.localEulerAngles = Vector3.zero;
    }

    public void Label(string content, Vector2 center, int size, Color color, float width = 220f)
    {
        if (usedTexts == texts.Count)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(textRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var created = go.GetComponent<Text>();
            created.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            created.alignment = TextAnchor.MiddleCenter;
            created.horizontalOverflow = HorizontalWrapMode.Wrap;
            created.verticalOverflow = VerticalWrapMode.Overflow;
            created.supportRichText = true;
            created.raycastTarget = false;
            texts.Add(created);
        }
        var text = texts[usedTexts++];
        text.gameObject.SetActive(true);
        text.text = content;
        text.fontSize = size;
        text.color = color;
        var textRect = text.rectTransform;
        textRect.anchoredPosition = center;
        textRect.sizeDelta = new Vector2(width, size * 1.6f);
    }
}

// A lesson of Professor Thouvenot, read from its JSON file: the drawing of the step on the left, the text on the right, the card of the instrument under the drawing.
// A step can carry a picture (made by the artist) that replaces the drawing. Built at runtime.
public class LessonUI : MonoBehaviour
{
    private static GameObject current;

    public static bool IsOpen => current != null;

    private const float PanelWidth = 820f;
    private const float PanelHeight = 560f;
    private const float AreaWidth = 480f;
    private const float AreaHeight = 340f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private LessonData lesson;
    private Action onDone;
    private int stepIndex;
    private float clock;
    private RectTransform area;
    private LessonCanvas drawing;
    private Image pictureView;
    private Text stepTitle;
    private Text bodyText;
    private Button previous;
    private Text nextLabel;
    private bool lockedInput;
    private bool wasLocked;

    // Opens the lesson. Returns false (and calls `done` at once) when the lesson cannot be shown.
    public static bool Show(string lessonId, Action done = null)
    {
        Close();
        var data = LessonLibrary.Get(lessonId);
        var canvas = GameObject.Find("Canvas");
        if (data == null || data.steps == null || data.steps.Length == 0 || canvas == null)
        {
            done?.Invoke();
            return false;
        }

        var go = new GameObject("LessonPanel", typeof(RectTransform), typeof(Image), typeof(LessonUI));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(go.GetComponent<Image>());

        var ui = go.GetComponent<LessonUI>();
        ui.lesson = data;
        ui.onDone = done;
        ui.wasLocked = PlayerController.InputLocked;
        ui.lockedInput = true;
        PlayerController.InputLocked = true;
        ui.Build();
        current = go;
        ui.ShowStep(0);
        return true;
    }

    public static void Close()
    {
        if (current != null)
        {
            var ui = current.GetComponent<LessonUI>();
            if (ui != null) ui.Release();
            // Inactive at once, so the pause menu does not take the Escape that closed it.
            current.SetActive(false);
            Destroy(current);
        }
        current = null;
    }

    private void Release()
    {
        if (!lockedInput) return;
        lockedInput = false;
        PlayerController.InputLocked = wasLocked;
    }

    private void OnDestroy()
    {
        Release();
        if (current == gameObject) current = null;
    }

    private void Build()
    {
        var title = RuntimeUIKit.MakeText("Title", transform, "Leçon : " + lesson.title, 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -18f), new Vector2(PanelWidth - 190f, 30f));
        title.fontStyle = FontStyle.Bold;

        var close = RuntimeUIKit.MakeButton("Close", transform, "Fermer", 14, new Color(0.45f, 0.25f, 0.22f, 1f), new Vector2(PanelWidth - 120f, -16f), new Vector2(90f, 30f));
        close.onClick.AddListener(Finish);

        var board = RuntimeUIKit.MakeBox("Board", transform, new Vector2(30f, -62f), new Vector2(AreaWidth, AreaHeight), new Color(0.05f, 0.06f, 0.09f, 1f));
        board.raycastTarget = false;
        var areaGO = new GameObject("Area", typeof(RectTransform), typeof(RectMask2D));
        areaGO.transform.SetParent(board.transform, false);
        area = areaGO.GetComponent<RectTransform>();
        area.anchorMin = area.anchorMax = new Vector2(0.5f, 0.5f);
        area.pivot = new Vector2(0.5f, 0.5f);
        area.sizeDelta = new Vector2(AreaWidth, AreaHeight);
        area.anchoredPosition = Vector2.zero;
        drawing = new LessonCanvas(area);

        // The picture of the artist, when a step has one.
        var pictureGO = new GameObject("Picture", typeof(RectTransform), typeof(Image));
        pictureGO.transform.SetParent(board.transform, false);
        var pictureRect = pictureGO.GetComponent<RectTransform>();
        pictureRect.anchorMin = Vector2.zero;
        pictureRect.anchorMax = Vector2.one;
        pictureRect.offsetMin = new Vector2(6f, 6f);
        pictureRect.offsetMax = new Vector2(-6f, -6f);
        pictureView = pictureGO.GetComponent<Image>();
        pictureView.preserveAspect = true;
        pictureView.raycastTarget = false;
        pictureGO.SetActive(false);

        // The card of the instrument.
        if (lesson.tool != null && !string.IsNullOrEmpty(lesson.tool.name))
        {
            var card = RuntimeUIKit.MakeText("Card", transform, CardText(lesson.tool), 13, TextAnchor.UpperLeft, new Color(0.85f, 0.88f, 0.95f),
                new Vector2(30f, -412f), new Vector2(AreaWidth, 120f));
            card.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.verticalOverflow = VerticalWrapMode.Overflow;
            card.supportRichText = true;
        }

        stepTitle = RuntimeUIKit.MakeText("StepTitle", transform, "", 18, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(530f, -62f), new Vector2(PanelWidth - 560f, 50f));
        stepTitle.fontStyle = FontStyle.Bold;
        stepTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText = RuntimeUIKit.MakeText("StepText", transform, "", 14, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 0.98f), new Vector2(530f, -116f), new Vector2(PanelWidth - 560f, 340f));
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.supportRichText = true;
        bodyText.lineSpacing = 1.08f;

        previous = RuntimeUIKit.MakeButton("Previous", transform, "< Précédent", 14, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(530f, -PanelHeight + 60f), new Vector2(120f, 34f));
        previous.onClick.AddListener(() => ShowStep(stepIndex - 1));
        var next = RuntimeUIKit.MakeButton("Next", transform, "Suivant >", 14, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(660f, -PanelHeight + 60f), new Vector2(130f, 34f));
        nextLabel = next.GetComponentInChildren<Text>();
        next.onClick.AddListener(Advance);
        StardewKit.Skin(transform);
    }

    private static string CardText(LessonToolData tool)
    {
        var text = "<b>Instrument :</b> " + tool.name;
        if (!string.IsNullOrEmpty(tool.measures)) text += "\n<b>Il mesure :</b> " + tool.measures;
        if (!string.IsNullOrEmpty(tool.where)) text += "\n<b>Où le trouver :</b> " + tool.where;
        if (tool.records != null && tool.records.Length > 0) text += "\n<b>Dans le grimoire :</b> " + string.Join(", ", tool.records);
        return text;
    }

    private void Advance()
    {
        if (stepIndex < lesson.steps.Length - 1) ShowStep(stepIndex + 1);
        else Finish();
    }

    private void ShowStep(int index)
    {
        stepIndex = Mathf.Clamp(index, 0, lesson.steps.Length - 1);
        clock = 0f;
        var step = lesson.steps[stepIndex];
        stepTitle.text = $"{stepIndex + 1} / {lesson.steps.Length}   {step.title}";
        bodyText.text = (step.text ?? "").Replace("\\n", "\n");
        previous.interactable = stepIndex > 0;
        nextLabel.text = stepIndex < lesson.steps.Length - 1 ? "Suivant >" : "Terminer";
        Sfx.Play(Sfx.Id.Click, 0.4f);

        // The picture of the artist replaces the drawing when it exists.
        Sprite picture = null;
        if (!string.IsNullOrEmpty(step.image)) picture = Resources.Load<Sprite>("Lessons/Images/" + step.image);
        pictureView.gameObject.SetActive(picture != null);
        if (picture != null) pictureView.sprite = picture;
        area.gameObject.SetActive(picture == null);
    }

    private void Finish()
    {
        var done = onDone;
        onDone = null;
        Close();
        done?.Invoke();
    }

    private void Update()
    {
        if (area == null) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Finish();
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { Advance(); return; }
        if (Input.GetKeyDown(KeyCode.LeftArrow) && stepIndex > 0) { ShowStep(stepIndex - 1); return; }

        clock += Time.unscaledDeltaTime;
        if (!area.gameObject.activeSelf) return;
        var step = lesson.steps[stepIndex];
        drawing.Begin();
        LessonFigures.Draw(step.figure, drawing, step.focus, clock);
        drawing.End();
    }
}
