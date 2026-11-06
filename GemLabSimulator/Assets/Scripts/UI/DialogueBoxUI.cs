using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The window shown when a character talks: an off-white box at the bottom of the screen with the speaker's name on a small plate, the
// speaker's face in a frame beside it and the text writing itself letter by letter. A click on the window finishes the line, then moves on
// to the next one (or closes it). It also closes by itself after a while or when the player walks away. Built at runtime on the Canvas.
public class DialogueBoxUI : MonoBehaviour
{
    public static DialogueBoxUI Instance { get; private set; }

    private const float BoxWidth = 880f;
    private const float BoxHeight = 140f;
    private const float PortraitSize = 120f;
    private const float PortraitGap = 10f;
    private const float BottomMargin = 30f;
    private const float CharactersPerSecond = 70f;
    private const float WalkAwayDistance = 3.5f;

    private static readonly Color Border = new Color(0.30f, 0.22f, 0.16f, 1f);
    private static readonly Color Paper = new Color(0.97f, 0.95f, 0.89f, 1f);
    private static readonly Color Rule = new Color(0.80f, 0.72f, 0.58f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.17f, 0.12f, 1f);

    private RectTransform root;
    private RectTransform boxRect;
    private GameObject portraitFrame;
    private RawImage portrait;
    private Action next;
    private CanvasGroup group;
    private Image namePlate;
    private Text nameLabel;
    private Text body;
    private RectTransform pointer;

    private string fullText = "";
    private float shown;
    private float hold;
    private bool visible;
    private bool sticky;                 // a line of the story: it stays until the player clicks it, whatever the player does
    private bool nextSticky;
    private float voicePitch = 1f;       // the voice of the speaker: each one has its own pitch for the small ticks of the text
    private float nextBlip;
    private Vector2 origin;
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (scene.name == "MainMenu") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<DialogueBoxUI>() == null) canvas.AddComponent<DialogueBoxUI>();
    }

    // Shows a line said by a character. Without a speaker, "Name : text" at the start of the text is split into the plate and the line.
    // source is the character talking (its sprite gives the face); next is what a click does once the line is read, nothing meaning
    // that the click closes the window.
    public static void Say(string speaker, string text, GameObject source = null, Action next = null)
    {
        if (Instance == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage(string.IsNullOrEmpty(speaker) ? text : $"{speaker} : {text}");
            return;
        }
        Instance.Show(speaker, text, source, next);
    }

    // A line that stays on the screen until the player clicks it (or presses Enter or Space), even if the player walks away: for the story, where every line has to be read.
    public static void SayAndWait(string speaker, string text, GameObject source, Action next)
    {
        if (Instance == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage(string.IsNullOrEmpty(speaker) ? text : $"{speaker} : {text}");
            next?.Invoke();
            return;
        }
        Instance.nextSticky = true;
        Instance.Show(speaker, text, source, next);
    }

    public static bool IsShowing => Instance != null && Instance.visible;

    // Closes the window (the story does it after its last line).
    public static void Hide()
    {
        if (Instance == null) return;
        Instance.visible = false;
        Instance.next = null;
    }

    public static void Say(string combined, GameObject source = null, Action next = null)
    {
        var separator = combined.IndexOf(" : ", StringComparison.Ordinal);
        var firstBreak = combined.IndexOf('\n');
        if (separator > 0 && separator <= 28 && (firstBreak < 0 || separator < firstBreak))
            Say(combined.Substring(0, separator), combined.Substring(separator + 3), source, next);
        else
            Say("", combined, source, next);
    }

    private void Awake()
    {
        Instance = this;
        Build();
        root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Build()
    {
        var go = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, BottomMargin);
        root.sizeDelta = new Vector2(BoxWidth, BoxHeight);
        group = go.GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;
        go.AddComponent<ClickRelay>().onClick = OnClicked;

        // The face of the speaker, in a frame of the same style as the box.
        portraitFrame = RuntimeUIKit.MakeBox("PortraitFrame", root, new Vector2(0f, -(BoxHeight - PortraitSize)), new Vector2(PortraitSize, PortraitSize), Border).gameObject;
        RuntimeUIKit.MakeBox("PortraitPaper", portraitFrame.transform, new Vector2(4f, -4f), new Vector2(PortraitSize - 8f, PortraitSize - 8f), Paper);
        RuntimeUIKit.MakeBox("PortraitRule", portraitFrame.transform, new Vector2(8f, -8f), new Vector2(PortraitSize - 16f, PortraitSize - 16f), Rule);
        RuntimeUIKit.MakeBox("PortraitBackdrop", portraitFrame.transform, new Vector2(10f, -10f), new Vector2(PortraitSize - 20f, PortraitSize - 20f),
            new Color(0.78f, 0.84f, 0.87f, 1f));
        var faceGo = new GameObject("Face", typeof(RectTransform), typeof(RawImage));
        faceGo.transform.SetParent(portraitFrame.transform, false);
        RuntimeUIKit.SetTopLeft(faceGo.GetComponent<RectTransform>(), new Vector2(12f, -12f), new Vector2(PortraitSize - 24f, PortraitSize - 24f));
        portrait = faceGo.GetComponent<RawImage>();
        portrait.raycastTarget = false;

        var boxGo = new GameObject("Box", typeof(RectTransform));
        boxGo.transform.SetParent(root, false);
        boxRect = boxGo.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(boxRect, Vector2.zero, new Vector2(BoxWidth, BoxHeight));

        // Dark frame, then the paper, a fine golden rule and the paper again for the text.
        RuntimeUIKit.MakeBox("Frame", boxRect, Vector2.zero, new Vector2(BoxWidth, BoxHeight), Border);
        RuntimeUIKit.MakeBox("Paper", boxRect, new Vector2(4f, -4f), new Vector2(BoxWidth - 8f, BoxHeight - 8f), Paper);
        RuntimeUIKit.MakeBox("Rule", boxRect, new Vector2(9f, -9f), new Vector2(BoxWidth - 18f, BoxHeight - 18f), Rule);
        RuntimeUIKit.MakeBox("Inner", boxRect, new Vector2(11f, -11f), new Vector2(BoxWidth - 22f, BoxHeight - 22f), Paper);

        body = RuntimeUIKit.MakeText("Text", boxRect, "", 21, TextAnchor.UpperLeft, Ink, new Vector2(34f, -28f), new Vector2(BoxWidth - 80f, BoxHeight - 50f));
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.1f;

        namePlate = RuntimeUIKit.MakeBox("NamePlate", boxRect, new Vector2(30f, 20f), new Vector2(160f, 36f), Border);
        RuntimeUIKit.MakeBox("NamePlateInner", namePlate.transform, new Vector2(3f, -3f), new Vector2(154f, 30f), new Color(0.46f, 0.34f, 0.22f, 1f));
        nameLabel = RuntimeUIKit.MakeText("Name", namePlate.transform, "", 19, TextAnchor.MiddleCenter, Paper, Vector2.zero, new Vector2(160f, 36f));
        nameLabel.fontStyle = FontStyle.Bold;

        // A small diamond in the corner that bobs once the whole line is written.
        var pointerImage = RuntimeUIKit.MakeBox("Pointer", boxRect, new Vector2(BoxWidth - 46f, -BoxHeight + 40f), new Vector2(12f, 12f), Border, false);
        pointer = pointerImage.rectTransform;
        pointer.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void Show(string speaker, string text, GameObject source, Action onNext)
    {
        if (root == null) return;

        // The characters speak in quotation marks in their lines; the box already says who talks.
        text = text.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"') text = text.Substring(1, text.Length - 2);

        sticky = nextSticky;
        nextSticky = false;
        voicePitch = PitchOf(speaker);
        next = onNext;
        fullText = text;
        shown = 0f;
        hold = Mathf.Max(3.5f, text.Length * 0.05f);
        body.text = "";

        var hasName = !string.IsNullOrEmpty(speaker);
        namePlate.gameObject.SetActive(hasName);
        if (hasName)
        {
            nameLabel.text = speaker;
            var width = Mathf.Max(110f, nameLabel.preferredWidth + 44f);
            namePlate.rectTransform.sizeDelta = new Vector2(width, 36f);
            var inner = namePlate.transform.GetChild(0).GetComponent<RectTransform>();
            inner.sizeDelta = new Vector2(width - 6f, 30f);
            nameLabel.rectTransform.sizeDelta = new Vector2(width, 36f);
        }

        ShowFace(source);

        if (player == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }
        origin = player != null ? (Vector2)player.position : Vector2.zero;

        visible = true;
        if (!root.gameObject.activeSelf)
        {
            group.alpha = 0f;
            root.gameObject.SetActive(true);
        }
        if (!PauseMenuUI.IsOpen) root.SetAsLastSibling();
    }

    // The face is the top square of the character's sprite (the head and the shoulders), shown as it is without any smoothing.
    private void ShowFace(GameObject source)
    {
        var sprite = source != null ? source.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
        var hasFace = sprite != null && sprite.texture != null;
        portraitFrame.SetActive(hasFace);
        if (hasFace)
        {
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            var side = Mathf.Min(rect.width, rect.height);
            portrait.texture = texture;
            portrait.uvRect = new Rect(rect.x / texture.width, (rect.yMax - side) / texture.height, side / texture.width, side / texture.height);
        }

        var offset = hasFace ? PortraitSize + PortraitGap : 0f;
        RuntimeUIKit.SetTopLeft(boxRect, new Vector2(offset, 0f), new Vector2(BoxWidth, BoxHeight));
        root.sizeDelta = new Vector2(BoxWidth + offset, BoxHeight);
    }

    // A pitch for a speaker, always the same one: between 0.85 and 1.3.
    private static float PitchOf(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return 1f;
        var hash = 17;
        foreach (var c in speaker) hash = hash * 31 + c;
        return 0.85f + (Mathf.Abs(hash) % 100) / 100f * 0.45f;
    }

    // A soft tick as the letters appear: not for every letter, and not for spaces.
    private void TypeTick(int before, int after)
    {
        if (after <= before || Time.unscaledTime < nextBlip) return;
        for (var i = before; i < after && i < fullText.Length; i++)
        {
            if (!char.IsLetterOrDigit(fullText[i])) continue;
            Sfx.Play(Sfx.Id.Blip, 0.5f, voicePitch * UnityEngine.Random.Range(0.94f, 1.08f));
            nextBlip = Time.unscaledTime + 0.055f;
            return;
        }
    }

    // A click: the first one finishes writing the line, the next one goes on to the following line or closes the window.
    private void OnClicked()
    {
        if (!visible) return;
        if (shown < fullText.Length)
        {
            shown = fullText.Length;
            body.text = fullText;
            return;
        }

        var action = next;
        next = null;
        if (action != null) action();
        else visible = false;
    }

    private class ClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public Action onClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke();
        }
    }

    private void Update()
    {
        if (root == null || !root.gameObject.activeSelf) return;

        var dt = Time.unscaledDeltaTime;
        if (visible)
        {
            group.blocksRaycasts = true;
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt * 8f);
            if (shown < fullText.Length)
            {
                var before = Mathf.FloorToInt(shown);
                shown = Mathf.Min(fullText.Length, shown + CharactersPerSecond * dt);
                var after = Mathf.FloorToInt(shown);
                body.text = fullText.Substring(0, after);
                TypeTick(before, after);
            }
            else
            {
                body.text = fullText;
                hold -= dt;
                if (hold <= 0f && !sticky) visible = false;
            }

            if (!sticky && player != null && Vector2.Distance(player.position, origin) > WalkAwayDistance) visible = false;
            if (sticky && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) OnClicked();
        }
        else
        {
            group.blocksRaycasts = false;
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt * 5f);
            if (group.alpha <= 0f) root.gameObject.SetActive(false);
        }

        var done = shown >= fullText.Length;
        pointer.gameObject.SetActive(done);
        if (done)
        {
            var bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 4f;
            pointer.anchoredPosition = new Vector2(BoxWidth - 46f, -BoxHeight + 40f + bob);
        }
    }
}
