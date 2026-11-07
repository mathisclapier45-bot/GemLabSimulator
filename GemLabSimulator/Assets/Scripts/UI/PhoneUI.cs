using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The mobile phone of the player: a small phone at the left of the minimap. It rings (it shakes, flashes and rings) until the player clicks on it; the story
// uses it for the calls of Professor Thouvenot. Clicked when it is quiet, it opens the phone: the time, the contact to call, the objective of the moment
// and the calls received. Built at runtime on the Canvas of every scene but the main menu. It exists from the first call (an old save has it from the start).
public class PhoneUI : MonoBehaviour
{
    public static PhoneUI Instance { get; private set; }

    private const string Speaker = "Professeure Thouvenot";
    private const float IconWidth = 44f;
    private const float IconHeight = 70f;
    private const float PanelWidth = 380f;
    private const float PanelHeight = 660f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Body = new Color(0.10f, 0.10f, 0.13f, 1f);
    private static readonly Color Edge = new Color(0.62f, 0.62f, 0.68f, 1f);
    private static readonly Color ScreenDim = new Color(0.15f, 0.28f, 0.40f, 1f);
    private static readonly Color ScreenLit = new Color(0.40f, 0.78f, 1f, 1f);
    private static readonly Color Green = new Color(0.25f, 0.50f, 0.30f, 1f);

    private RectTransform icon;
    private Image screen;
    private Image glow;
    private Text ringText;
    private GameObject panel;
    private RectTransform layer;
    private GameObject face;

    private bool ringing;
    private float ringTimer;
    private Action onAnswered;

    public bool IsRinging => ringing;
    public bool IsOpen => panel != null && panel.activeSelf;

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
        if (canvas != null && canvas.GetComponent<PhoneUI>() == null) canvas.AddComponent<PhoneUI>();
    }

    private void Awake()
    {
        Instance = this;

        // The face of the caller for the dialogue box: the sprite of Thouvenot on an object that is never drawn.
        face = new GameObject("PhoneFace", typeof(SpriteRenderer));
        face.transform.SetParent(transform, false);
        var renderer = face.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Story/Thouvenot");
        renderer.enabled = false;

        BuildIcon();
        BuildPanel();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---- The calls ---------------------------------------------------------------------------------------------------------

    // The phone rings until the player clicks on it; onAnswered is then called (the story plays the call).
    public void Ring(string caller, Action answered)
    {
        ringing = true;
        ringTimer = 0f;
        onAnswered = answered;
        ringText.text = "Appel : " + caller;
        Close();
    }

    public void StopRinging()
    {
        ringing = false;
        onAnswered = null;
    }

    // Says the lines of a call one after the other (each one stays until it is clicked), then calls done.
    public void Speak(string[] lines, Action done)
    {
        Speak(Speaker, Resources.Load<Sprite>("Story/Thouvenot"), lines, done);
    }

    public void Speak(string speaker, Sprite portrait, string[] lines, Action done)
    {
        face.GetComponent<SpriteRenderer>().sprite = portrait;
        var index = 0;
        void Next()
        {
            if (index >= lines.Length)
            {
                DialogueBoxUI.Hide();
                Sfx.Play(Sfx.Id.PhoneHangUp, 0.7f);
                done?.Invoke();
                return;
            }
            DialogueBoxUI.SayAndWait(speaker, lines[index++], face, Next);
        }
        Next();
    }

    private static string Stamp()
    {
        var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        return $"{GameCalendar.Short(day)}, {DayNightOverlay.GetClockLabel()}";
    }

    // The player calls Anselme.
    private void CallAnselme()
    {
        Close();
        var state = StoryState.Instance;
        Sfx.Play(Sfx.Id.Click);
        state?.AddCall($"Sortant — Anselme — {Stamp()}");
        Speak("Anselme", AnselmeSprite(), new[]
        {
            "Allô ? Ah, c'est vous, le neveu de Vincent ! Pas de nouvelle pierre pour l'instant, mais je garde l'œil ouvert quand je me promène près du pont.",
            "Je vous appelle si je trouve quelque chose. Et passez le bonjour à la professeure !",
        }, null);
    }

    private static Sprite AnselmeSprite()
    {
        var frames = Resources.LoadAll<Sprite>("Walkers/W3/Down");
        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        return frames.Length > 0 ? frames[0] : null;
    }

    // The player calls Thouvenot: she says where to find her, and what to do now.
    private void CallThouvenot()
    {
        Close();
        var state = StoryState.Instance;
        string[] lines;
        if (state != null && state.Phase == StoryState.PhaseLab)
            lines = new[]
            {
                "Allô ? Je suis déjà à la Maison et Bureau de Gemmologie, le laboratoire de Vincent, au nord-ouest de la ville, sur la grande route du nord. Je vous attends.",
            };
        else
            lines = new[]
            {
                "Allô ? Je suis au laboratoire, si vous avez besoin de moi. Souvenez-vous : la chamoisine d'abord, la loupe ensuite !",
                "Quand vous aurez examiné une pierre, remettez votre rapport au directeur : c'est ainsi qu'on gagne sa réputation.",
            };

        Sfx.Play(Sfx.Id.Click);
        state?.AddCall($"Sortant — {Speaker} — {Stamp()}");
        Speak(lines, null);
    }

    // ---- The icon ----------------------------------------------------------------------------------------------------------

    private void BuildIcon()
    {
        var root = new GameObject("PhoneIcon", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        icon = root.GetComponent<RectTransform>();
        icon.anchorMin = icon.anchorMax = new Vector2(1f, 1f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(IconWidth, IconHeight);
        // At the left of the minimap, at its height.
        const float minimapWidth = 140f;
        icon.anchoredPosition = new Vector2(-(ClockHudUI.Margin + minimapWidth + 10f + IconWidth / 2f),
            -(ClockHudUI.Margin + ClockHudUI.Height + 4f + MoneyHudUI.Height + 6f + IconHeight / 2f));

        var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glowGO.transform.SetParent(icon, false);
        var glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(150f, 150f);
        glow = glowGO.GetComponent<Image>();
        glow.sprite = RuntimeUIKit.Glow;
        glow.raycastTarget = false;
        glow.color = new Color(0.5f, 0.85f, 1f, 0f);

        Piece("Edge", 0f, 0f, IconWidth, IconHeight, Edge);
        Piece("Body", 2f, 2f, IconWidth - 4f, IconHeight - 4f, Body);
        screen = Piece("Screen", 5f, 9f, IconWidth - 10f, IconHeight - 24f, ScreenDim);
        Piece("Speaker", 15f, 4f, 14f, 2f, Edge);
        Piece("HomeButton", 18f, IconHeight - 12f, 8f, 8f, Edge);
        // A small glyph on the screen: a receiver drawn with two boxes.
        Piece("GlyphA", 13f, 28f, 18f, 5f, Color.white);
        Piece("GlyphB", 13f, 38f, 18f, 5f, Color.white);

        ringText = RuntimeUIKit.MakeText("RingText", icon, "", 14, TextAnchor.MiddleRight, Color.white, new Vector2(-244f, -22f), new Vector2(236f, 26f));
        ringText.fontStyle = FontStyle.Bold;
        ringText.rectTransform.anchorMin = ringText.rectTransform.anchorMax = new Vector2(0f, 1f);
        var halo = ringText.gameObject.AddComponent<Outline>();
        halo.effectColor = new Color(0f, 0f, 0f, 0.9f);
        halo.effectDistance = new Vector2(1f, -1f);

        // The whole icon is a button.
        var hit = RuntimeUIKit.MakeBox("Hit", icon, Vector2.zero, new Vector2(IconWidth, IconHeight), new Color(1f, 1f, 1f, 0.004f), false);
        var button = hit.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnIconClicked);
    }

    private Image Piece(string name, float x, float y, float w, float h, Color color)
    {
        var box = RuntimeUIKit.MakeBox(name, icon, new Vector2(x, -y), new Vector2(w, h), color, false);
        box.raycastTarget = false;
        // The boxes are placed from the top left of the icon, which turns around its centre.
        var rect = box.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        return box;
    }

    private void OnIconClicked()
    {
        if (ringing)
        {
            var answered = onAnswered;
            StopRinging();
            ringText.text = "";
            Sfx.Play(Sfx.Id.Click);
            answered?.Invoke();
            return;
        }

        if (IsOpen) Close();
        else Open();
    }

    private void Update()
    {
        var state = StoryState.Instance;
        var visible = ringing || (state != null && state.PhoneUnlocked);
        if (icon.gameObject.activeSelf != visible) icon.gameObject.SetActive(visible);
        if (!visible) return;

        if (ringing)
        {
            ringTimer -= Time.unscaledDeltaTime;
            if (ringTimer <= 0f)
            {
                ringTimer = 1.6f;
                Sfx.Play(Sfx.Id.PhoneRing, 1f);
            }

            // It shakes while it trills, and rests between the two trills.
            var cycle = 1.6f - ringTimer;
            var shaking = cycle < 0.42f || (cycle > 0.58f && cycle < 1.0f);
            icon.localRotation = Quaternion.Euler(0f, 0f, shaking ? Mathf.Sin(Time.unscaledTime * 40f) * 10f : 0f);
            var pulse = shaking ? 1f : 0.4f;
            screen.color = Color.Lerp(ScreenDim, ScreenLit, pulse);
            glow.color = new Color(0.5f, 0.85f, 1f, 0.55f * pulse);
        }
        else
        {
            icon.localRotation = Quaternion.identity;
            screen.color = ScreenDim;
            glow.color = new Color(0.5f, 0.85f, 1f, 0f);
            ringText.text = "";
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ---- The phone ---------------------------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("PhonePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        var back = panel.GetComponent<Image>();
        back.sprite = RuntimeUIKit.Rounded;
        back.type = Image.Type.Sliced;
        back.color = Edge;

        var inner = RuntimeUIKit.MakeBox("Body", panel.transform, new Vector2(5f, -5f), new Vector2(PanelWidth - 10f, PanelHeight - 10f), Body);
        inner.raycastTarget = false;
        var screenBox = RuntimeUIKit.MakeBox("Screen", panel.transform, new Vector2(16f, -34f), new Vector2(PanelWidth - 32f, PanelHeight - 76f), new Color(0.11f, 0.15f, 0.22f, 1f), false);
        screenBox.raycastTarget = false;
        RuntimeUIKit.MakeBox("Speaker", panel.transform, new Vector2(PanelWidth / 2f - 24f, -15f), new Vector2(48f, 5f), Edge).raycastTarget = false;
        RuntimeUIKit.MakeBox("HomeButton", panel.transform, new Vector2(PanelWidth / 2f - 14f, -(PanelHeight - 36f)), new Vector2(28f, 28f), Edge).raycastTarget = false;

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }

    public void Open()
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Sfx.Play(Sfx.Id.Click, 0.6f);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private Text Label(string name, string text, int size, TextAnchor anchor, Color color, float x, float y, float w, float h, bool bold = false)
    {
        var label = RuntimeUIKit.MakeText(name, layer, text, size, anchor, color, new Vector2(x, -y), new Vector2(w, h));
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) label.fontStyle = FontStyle.Bold;
        return label;
    }

    // A card of the list of contacts: the face, the name, the role, and the button to call.
    private void ContactCard(string name, string label, string role, Sprite face, float y, Action call)
    {
        const float left = 30f;
        const float width = PanelWidth - 60f;
        RuntimeUIKit.MakeBox("Card_" + name, layer, new Vector2(left, -y), new Vector2(width, 64f), new Color(0.17f, 0.21f, 0.30f, 1f)).raycastTarget = false;
        var portrait = new GameObject("Portrait_" + name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        portrait.transform.SetParent(layer, false);
        RuntimeUIKit.SetTopLeft(portrait.rectTransform, new Vector2(left + 8f, -(y + 6f)), new Vector2(52f, 52f));
        portrait.sprite = face;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        Label("Name_" + name, label, 15, TextAnchor.UpperLeft, Color.white, left + 70f, y + 8f, width - 70f - 96f, 22f, true);
        Label("Role_" + name, role, 11, TextAnchor.UpperLeft, new Color(0.7f, 0.76f, 0.88f), left + 70f, y + 30f, width - 70f - 96f, 18f);
        var button = RuntimeUIKit.MakeButton("Call_" + name, layer, "Appeler", 14, Green, new Vector2(PanelWidth - 30f - 8f - 84f, -(y + 14f)), new Vector2(84f, 36f));
        button.onClick.AddListener(() => call());
    }

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        var state = StoryState.Instance;
        var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        const float left = 30f;
        const float width = PanelWidth - 60f;

        // The status bar.
        Label("Clock", DayNightOverlay.GetClockLabel(), 20, TextAnchor.UpperLeft, Color.white, left, 44f, 120f, 26f, true);
        Label("Date", GameCalendar.Short(day), 14, TextAnchor.UpperRight, new Color(0.75f, 0.8f, 0.9f), PanelWidth - 30f - 200f, 48f, 200f, 20f);
        Label("Title", "Téléphone", 22, TextAnchor.UpperCenter, Gold, left, 84f, width, 30f, true);

        // The contacts: Thouvenot, and the people whose number the player has been given.
        var y = 130f;
        Label("ContactsHead", "CONTACTS", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.75f, 0.9f), left, y, width, 18f, true);
        y += 22f;
        ContactCard("Thouvenot", Speaker, "Gemmologue, enseignante", Resources.Load<Sprite>("Story/Thouvenot"), y, CallThouvenot);
        y += 70f;
        if (state != null && state.HasContact("anselme"))
        {
            ContactCard("Anselme", "Anselme", "Habitant de Crystal Grove", AnselmeSprite(), y, CallAnselme);
            y += 70f;
        }

        // The objective of the moment.
        y += 4f;
        Label("ObjectiveHead", "OBJECTIF", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.75f, 0.9f), left, y, width, 18f, true);
        y += 22f;
        RuntimeUIKit.MakeBox("ObjectiveCard", layer, new Vector2(left, -y), new Vector2(width, 96f), new Color(0.17f, 0.21f, 0.30f, 1f)).raycastTarget = false;
        Label("Objective", state != null ? state.Objective : "", 14, TextAnchor.UpperLeft, Color.white, left + 12f, y + 10f, width - 24f, 78f);
        y += 108f;

        // The calls received.
        Label("CallsHead", "APPELS", 12, TextAnchor.UpperLeft, new Color(0.65f, 0.75f, 0.9f), left, y, width, 18f, true);
        y += 22f;
        if (state == null || state.Calls.Count == 0)
        {
            Label("NoCall", "Aucun appel pour l'instant.", 13, TextAnchor.UpperLeft, new Color(0.65f, 0.7f, 0.8f), left, y, width, 20f);
        }
        else
        {
            for (var i = state.Calls.Count - 1; i >= 0 && i >= state.Calls.Count - 3; i--)
            {
                Label("Call", state.Calls[i], 12, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f), left, y, width, 32f);
                y += 34f;
            }
        }

        var close = RuntimeUIKit.MakeButton("Close", layer, "Fermer", 14, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth / 2f - 50f, -(PanelHeight - 78f)), new Vector2(100f, 32f));
        close.onClick.AddListener(Close);
        StardewKit.Skin(layer);
    }
}
