using UnityEngine;
using UnityEngine.UI;

// The board at the entrance of the laboratory: the list of the lessons of Professor Thouvenot, to read again whenever the player wants. A lesson is open once the
// instrument it teaches has been presented (see LessonLibrary.Unlocked). Built at runtime.
public class LessonMenuUI : MonoBehaviour
{
    private static GameObject current;

    public static bool IsOpen => current != null;

    private const float PanelWidth = 620f;
    private const float RowHeight = 50f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private bool lockedInput;
    private bool wasLocked;

    public static void Show()
    {
        Close();
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var rows = LessonLibrary.Order.Length;
        var height = 150f + rows * RowHeight;

        var go = new GameObject("LessonMenuPanel", typeof(RectTransform), typeof(Image), typeof(LessonMenuUI));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(PanelWidth, height);
        StardewKit.FramePanel(go.GetComponent<Image>());

        var ui = go.GetComponent<LessonMenuUI>();
        ui.wasLocked = PlayerController.InputLocked;
        ui.lockedInput = true;
        PlayerController.InputLocked = true;
        ui.Build(height);
        current = go;
        Sfx.Play(Sfx.Id.Paper);
    }

    public static void Close()
    {
        if (current != null)
        {
            var ui = current.GetComponent<LessonMenuUI>();
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Build(float height)
    {
        var title = RuntimeUIKit.MakeText("Title", transform, "Les leçons de la professeure Thouvenot", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -18f), new Vector2(PanelWidth - 60f, 30f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Hint", transform, "Choisissez une leçon à revoir. Les autres s'ouvriront avec l'histoire.", 14, TextAnchor.UpperLeft,
            new Color(0.78f, 0.82f, 0.90f), new Vector2(30f, -54f), new Vector2(PanelWidth - 60f, 24f));

        for (var i = 0; i < LessonLibrary.Order.Length; i++)
        {
            var id = LessonLibrary.Order[i];
            var lesson = LessonLibrary.Get(id);
            if (lesson == null) continue;
            var unlocked = LessonLibrary.Unlocked(lesson);
            var label = $"{i + 1}.  {lesson.title}" + (unlocked ? "" : "   (pas encore présentée)");
            var color = unlocked ? new Color(0.25f, 0.45f, 0.30f, 1f) : new Color(0.28f, 0.28f, 0.30f, 1f);
            var button = RuntimeUIKit.MakeButton("Lesson_" + id, transform, label, 16, color, new Vector2(30f, -90f - i * RowHeight), new Vector2(PanelWidth - 60f, RowHeight - 8f));
            var text = button.GetComponentInChildren<Text>();
            if (!unlocked && text != null) text.color = new Color(0.65f, 0.65f, 0.68f, 1f);
            var lessonId = id;
            button.onClick.AddListener(() =>
            {
                if (!unlocked)
                {
                    ToolFeedbackUI.Instance?.ShowMessage("La professeure Thouvenot ne vous a pas encore présenté cette leçon.");
                    return;
                }
                Open(lessonId);
            });
        }

        var close = RuntimeUIKit.MakeButton("Close", transform, "Fermer", 15, new Color(0.45f, 0.25f, 0.22f, 1f), new Vector2(PanelWidth - 150f, -height + 50f), new Vector2(120f, 34f));
        close.onClick.AddListener(Close);
        StardewKit.Skin(transform);
    }

    // Plays the lesson, then comes back to the list.
    private static void Open(string id)
    {
        var lesson = LessonLibrary.Get(id);
        Close();
        if (lesson != null && lesson.animation == "RefractionLessonUI") RefractionLessonUI.Show(Show);
        else LessonUI.Show(id, Show);
    }
}
