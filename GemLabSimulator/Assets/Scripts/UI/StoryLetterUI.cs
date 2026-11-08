using UnityEngine;
using UnityEngine.UI;

// The personal mail of the player, read in the mailbox of the house: the letters of the story on a list at the left, the paper at the right. The first time the
// letter of Vincent is read, the prologue is over. Built at runtime.
public class StoryLetterUI : MonoBehaviour
{
    public static StoryLetterUI Instance { get; private set; }

    private const float PanelWidth = 880f;
    private const float PanelHeight = 580f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Paper = new Color(0.96f, 0.92f, 0.80f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color Burgundy = new Color(0.48f, 0.16f, 0.14f, 1f);
    private static readonly Color RowColor = new Color(0.20f, 0.18f, 0.17f, 1f);
    private static readonly Color ChosenColor = new Color(0.62f, 0.46f, 0.16f, 1f);

    private GameObject panel;
    private RectTransform layer;
    private string selected;

    public static bool HasMail => StoryState.Instance != null && StoryState.Instance.Received.Count > 0;

    public static void TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<StoryLetterUI>();
        }
        Instance?.Open();
    }

    private void Awake()
    {
        Instance = this;
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
        var state = StoryState.Instance;
        if (state == null) return;

        // The first unread letter, or the last one received.
        selected = null;
        foreach (var id in state.Received)
            if (!state.IsRead(id)) { selected = id; break; }
        if (selected == null && state.Received.Count > 0) selected = state.Received[state.Received.Count - 1];

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        var state = StoryState.Instance;
        if (state == null) return;

        // The list of the letters.
        var y = -84f;
        foreach (var id in state.Received)
        {
            var letter = StoryLetters.Find(id);
            if (letter == null) continue;
            var unread = !state.IsRead(id);
            var chosen = id == selected;
            var button = RuntimeUIKit.MakeButton("Letter", layer, (unread ? "● " : "") + letter.Title, 13, chosen ? ChosenColor : RowColor, new Vector2(24f, y), new Vector2(250f, 40f));
            var captured = id;
            button.onClick.AddListener(() => { selected = captured; Refresh(); });
            y -= 46f;
        }

        // The paper.
        RuntimeUIKit.MakeBox("PaperEdge", layer, new Vector2(292f, -80f), new Vector2(PanelWidth - 316f, PanelHeight - 104f), new Color(0.60f, 0.50f, 0.34f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Paper", layer, new Vector2(295f, -83f), new Vector2(PanelWidth - 322f, PanelHeight - 110f), Paper, false).raycastTarget = false;

        var current = StoryLetters.Find(selected);
        if (current == null) return;

        var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollGO.transform.SetParent(layer, false);
        RuntimeUIKit.SetTopLeft(scrollGO.GetComponent<RectTransform>(), new Vector2(316f, -100f), new Vector2(PanelWidth - 364f, PanelHeight - 144f));
        scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.004f);

        var text = RuntimeUIKit.MakeText("Text", scrollGO.transform, "<b><color=#7A2E2A>" + current.Title + "</color></b>\n\n" + current.Body, 16, TextAnchor.UpperLeft, Ink, Vector2.zero, new Vector2(100f, 10f));
        // The letters are written in the hand of the intro cinematic (IM Fell English Italic), a little bigger since this face is small.
        var handwriting = Resources.Load<Font>("Fonts/IMFellEnglish-Italic");
        if (handwriting != null)
        {
            text.font = handwriting;
            text.fontSize = 21;
        }
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.lineSpacing = 1.12f;
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-8f, 0f);
        rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.content = rect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        scroll.verticalNormalizedPosition = 1f;

        // Reading the letter of Vincent for the first time ends the prologue.
        if (!state.IsRead(selected))
        {
            state.MarkRead(selected);
            if (selected == StoryLetters.Welcome && state.Stage == StoryState.GoHome)
            {
                state.SetStage(StoryState.Settled);
                state.SetPhase(StoryState.PhaseSleep);
                ToolFeedbackUI.Instance?.ShowMessage("Il est tard : dormez dans votre lit (touche E), la journée de demain sera longue.");
            }
        }
    }

    private void Build()
    {
        panel = new GameObject("StoryLetterPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Courrier personnel", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -16f), new Vector2(PanelWidth - 180f, 30f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", panel.transform, "Les lettres que vous avez reçues", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(30f, -50f), new Vector2(PanelWidth - 60f, 18f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        StardewKit.Skin(panel.transform);
    }
}
