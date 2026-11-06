using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The screens of the main menu, built at runtime over the ones of the scene: the title screen (the emblem, the name of the game, PLAY, SETTINGS, QUIT), the
// screen of the saves (with a way back) and the settings (the sound, the display and the controls). The slots of the saves are the ones of MainMenuUI, moved
// into their new frame. Escape goes back one screen. The old panels of the scene are hidden; nothing has to be regenerated.
public class MainMenuFlow : MonoBehaviour
{
    private enum Page { Title, Slots, Settings }

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Bronze = new Color(0.80f, 0.64f, 0.26f);
    private static readonly Color Cream = new Color(0.97f, 0.94f, 0.86f);
    private static readonly Color Muted = new Color(0.66f, 0.66f, 0.74f);
    private static readonly Color Green = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color Slate = new Color(0.28f, 0.30f, 0.38f, 1f);
    private static readonly Color Wine = new Color(0.55f, 0.24f, 0.22f, 1f);

    private const float SliderWidth = 300f;

    private RectTransform root;
    private GameObject titleScreen;
    private GameObject slotsScreen;
    private GameObject settingsScreen;
    private Page current;

    private Slider musicSlider;
    private Slider sfxSlider;
    private Text musicValue;
    private Text sfxValue;
    private Image musicToggle;
    private Text musicToggleLabel;
    private Image sfxToggle;
    private Text sfxToggleLabel;
    private Image fullToggle;
    private Text fullToggleLabel;

    private void Awake()
    {
        var menu = GetComponent<MainMenuUI>();
        Build(menu != null ? menu.SlotsContainer : null);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || MusicManager.IntroPlaying) return;
        if (current != Page.Title) Show(Page.Title);
    }

    // ---- Screens ---------------------------------------------------------------------------------------------------------

    private void Show(Page screen)
    {
        current = screen;
        titleScreen.SetActive(screen == Page.Title);
        slotsScreen.SetActive(screen == Page.Slots);
        settingsScreen.SetActive(screen == Page.Settings);
        if (screen == Page.Settings) RefreshSettings();
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Places an element at the top centre of its parent.
    private static void TopCenter(RectTransform rect, float x, float y, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = size;
    }

    private static Text CenterText(Transform parent, string content, int size, Color color, float y, float height, bool bold = false, bool italic = false)
    {
        var text = RuntimeUIKit.MakeText("Text", parent, content, size, TextAnchor.MiddleCenter, color, Vector2.zero, new Vector2(900f, height));
        TopCenter(text.rectTransform, 0f, y, new Vector2(900f, height));
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.fontStyle = bold && italic ? FontStyle.BoldAndItalic : bold ? FontStyle.Bold : italic ? FontStyle.Italic : FontStyle.Normal;
        return text;
    }

    private static Button CenterButton(Transform parent, string label, int size, Color color, float y, Vector2 dimensions, UnityAction onClick)
    {
        var button = RuntimeUIKit.MakeButton("Button", parent, label, size, color, Vector2.zero, dimensions);
        TopCenter(button.GetComponent<RectTransform>(), 0f, y, dimensions);
        button.onClick.AddListener(onClick);
        return button;
    }

    private static void Lozenge(Transform parent, float x, float y, float size, Color color)
    {
        var box = RuntimeUIKit.MakeBox("Lozenge", parent, Vector2.zero, new Vector2(size, size), color, false);
        box.raycastTarget = false;
        var rect = box.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static Sprite GradientSprite()
    {
        const int height = 128;
        var texture = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var top = new Color(0.05f, 0.06f, 0.13f);
        var middle = new Color(0.10f, 0.09f, 0.18f);
        var bottom = new Color(0.20f, 0.12f, 0.12f);
        for (var y = 0; y < height; y++)
        {
            var t = y / (float)(height - 1);                  // 0 at the bottom, 1 at the top
            var color = t < 0.5f ? Color.Lerp(bottom, middle, t * 2f) : Color.Lerp(middle, top, (t - 0.5f) * 2f);
            texture.SetPixel(0, y, color);
            texture.SetPixel(1, y, color);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Build(RectTransform slotsContainer)
    {
        // The old panels of the scene: the gems of the background are kept, the rest is hidden.
        var oldTitle = transform.Find("TitleScreenPanel");
        var oldSlots = transform.Find("SlotsPanel");
        Transform gems = oldTitle != null ? oldTitle.Find("BackgroundGems") : null;

        root = Stretch("MenuRoot", transform);
        root.SetAsFirstSibling();

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(root, false);
        var backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        var backdropImage = backdrop.GetComponent<Image>();
        backdropImage.sprite = GradientSprite();
        backdropImage.raycastTarget = false;

        if (gems != null)
        {
            gems.SetParent(root, false);
            gems.SetAsLastSibling();
        }

        BuildTitle();
        BuildSlots(slotsContainer);
        BuildSettings();

        if (oldTitle != null) oldTitle.gameObject.SetActive(false);
        if (oldSlots != null) oldSlots.gameObject.SetActive(false);

        StardewKit.Skin(root);
        Show(Page.Title);
    }

    // ---- The title screen ------------------------------------------------------------------------------------------------

    private void BuildTitle()
    {
        var layer = Stretch("TitleScreen", root);
        titleScreen = layer.gameObject;

        // The emblem: a cut stone in a gold lozenge, like the cover of the books.
        Lozenge(layer, 0f, -118f, 124f, Bronze);
        Lozenge(layer, 0f, -118f, 102f, new Color(0.10f, 0.14f, 0.28f));
        Lozenge(layer, 0f, -118f, 66f, new Color(0.62f, 0.86f, 0.98f));
        Lozenge(layer, -12f, -130f, 20f, new Color(0.94f, 0.99f, 1f));

        var title = CenterText(layer, "GEMLAB", 92, Cream, -196f, 100f, true);
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4f, -4f);
        CenterText(layer, "S I M U L A T O R", 30, Bronze, -292f, 36f, true);

        // A line with a lozenge in the middle.
        var left = RuntimeUIKit.MakeBox("RuleLeft", layer, Vector2.zero, new Vector2(150f, 2f), new Color(Bronze.r, Bronze.g, Bronze.b, 0.8f), false);
        left.raycastTarget = false;
        TopCenter(left.rectTransform, -90f, -344f, new Vector2(150f, 2f));
        var right = RuntimeUIKit.MakeBox("RuleRight", layer, Vector2.zero, new Vector2(150f, 2f), new Color(Bronze.r, Bronze.g, Bronze.b, 0.8f), false);
        right.raycastTarget = false;
        TopCenter(right.rectTransform, 90f, -344f, new Vector2(150f, 2f));
        Lozenge(layer, 0f, -343f, 12f, Bronze);

        CenterText(layer, "Un simulateur de gemmologie", 20, Muted, -362f, 28f, false, true);

        CenterButton(layer, "JOUER", 24, Green, -430f, new Vector2(300f, 58f), () => Show(Page.Slots));
        CenterButton(layer, "RÉGLAGES", 20, Slate, -498f, new Vector2(300f, 50f), () => Show(Page.Settings));
        CenterButton(layer, "QUITTER", 17, new Color(0.22f, 0.22f, 0.28f, 1f), -558f, new Vector2(300f, 44f), Quit);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- The saves --------------------------------------------------------------------------------------------------------

    private GameObject BuildPanel(string name, float width, float height, string heading)
    {
        var screen = Stretch(name, root);
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(screen, false);
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        var dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.35f);
        dimImage.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(screen, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Heading", panel.transform, heading, 30, TextAnchor.MiddleCenter, Gold, Vector2.zero, new Vector2(width, 40f));
        TopCenter(title.rectTransform, 0f, -22f, new Vector2(width, 40f));
        title.fontStyle = FontStyle.Bold;

        var back = RuntimeUIKit.MakeButton("Back", panel.transform, "< Retour", 15, Slate, new Vector2(22f, -20f), new Vector2(110f, 36f));
        back.onClick.AddListener(() => Show(Page.Title));
        var hint = RuntimeUIKit.MakeText("Hint", panel.transform, "Échap : retour", 11, TextAnchor.MiddleCenter, Muted, Vector2.zero, new Vector2(width, 16f));
        TopCenter(hint.rectTransform, 0f, -(height - 26f), new Vector2(width, 16f));
        return panel;
    }

    private void BuildSlots(RectTransform slotsContainer)
    {
        var panel = BuildPanel("SlotsScreen", 700f, 640f, "Choisir une partie");
        slotsScreen = panel.transform.parent.gameObject;
        RuntimeUIKit.MakeText("Subtitle", panel.transform, "Trois emplacements : une nouvelle aventure, ou la reprise d'une partie.", 13, TextAnchor.UpperCenter, Muted, new Vector2(0f, -68f), new Vector2(700f, 20f));

        if (slotsContainer == null) return;
        slotsContainer.SetParent(panel.transform, false);
        slotsContainer.anchorMin = slotsContainer.anchorMax = new Vector2(0.5f, 1f);
        slotsContainer.pivot = new Vector2(0.5f, 1f);
        slotsContainer.anchoredPosition = new Vector2(0f, -106f);
        slotsContainer.sizeDelta = new Vector2(560f, 450f);
    }

    // ---- The settings -----------------------------------------------------------------------------------------------------

    private void BuildSettings()
    {
        const float width = 860f;
        const float height = 660f;
        var panel = BuildPanel("SettingsScreen", width, height, "Réglages");
        settingsScreen = panel.transform.parent.gameObject;
        var p = panel.transform;

        Heading(p, "SON", 80f);
        musicSlider = Row(p, "Musique", 116f, out musicValue, out musicToggle, out musicToggleLabel, OnMusicChanged, ToggleMusic);
        sfxSlider = Row(p, "Effets sonores", 156f, out sfxValue, out sfxToggle, out sfxToggleLabel, OnSfxChanged, ToggleSfx);

        Heading(p, "AFFICHAGE", 212f);
        RuntimeUIKit.MakeText("FullLabel", p, "Plein écran", 16, TextAnchor.MiddleLeft, Cream, new Vector2(44f, -246f), new Vector2(200f, 30f));
        var full = RuntimeUIKit.MakeButton("FullToggle", p, "Activer", 14, Slate, new Vector2(width - 44f - 110f, -246f), new Vector2(110f, 30f));
        full.onClick.AddListener(ToggleFullscreen);
        fullToggle = full.GetComponent<Image>();
        fullToggleLabel = full.GetComponentInChildren<Text>();
        RuntimeUIKit.MakeText("FullHint", p, "ou F11, ou Alt + Entrée en jeu", 12, TextAnchor.MiddleLeft, Muted, new Vector2(240f, -246f), new Vector2(300f, 30f));

        Heading(p, "COMMANDES", 302f);
        var left = new[]
        {
            ("Flèches", "Se déplacer"),
            ("E", "Parler, ouvrir, utiliser"),
            ("H", "Le sac : pierres et outils"),
            ("Tab", "L'inventaire"),
            ("K", "Le calendrier"),
            ("G", "Le grimoire des pierres"),
            ("B", "Le classeur de cartes"),
        };
        var right = new[]
        {
            ("J", "Les quêtes"),
            ("C", "La grande carte de la ville"),
            ("Clic", "Sur la minicarte : la grande carte"),
            ("Espace", "Continuer un dialogue"),
            ("Échap", "Pause, ou fermer une fenêtre"),
            ("F11", "Plein écran"),
            ("Clic", "Tout le reste : boutons, objets"),
        };
        Keys(p, left, 44f, 338f);
        Keys(p, right, 448f, 338f);

        RuntimeUIKit.MakeText("Note", p, "Les réglages de son sont gardés d'une partie à l'autre.", 12, TextAnchor.UpperCenter, Muted, new Vector2(0f, -(height - 54f)), new Vector2(width, 16f));
    }

    private static void Heading(Transform parent, string title, float y)
    {
        var text = RuntimeUIKit.MakeText("Section", parent, title, 14, TextAnchor.MiddleLeft, Bronze, new Vector2(44f, -y), new Vector2(300f, 22f));
        text.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeBox("SectionLine", parent, new Vector2(44f, -(y + 24f)), new Vector2(772f, 1f), new Color(Bronze.r, Bronze.g, Bronze.b, 0.5f), false).raycastTarget = false;
    }

    private void Keys(Transform parent, (string, string)[] items, float x, float y)
    {
        for (var i = 0; i < items.Length; i++)
        {
            var rowY = y + i * 36f;
            var chip = RuntimeUIKit.MakeBox("Key", parent, new Vector2(x, -rowY), new Vector2(86f, 28f), new Color(0.20f, 0.18f, 0.16f, 1f), false);
            chip.raycastTarget = false;
            RuntimeUIKit.MakeBox("KeyEdge", parent, new Vector2(x, -rowY - 26f), new Vector2(86f, 2f), Bronze, false).raycastTarget = false;
            var key = RuntimeUIKit.MakeText("KeyText", parent, items[i].Item1, 14, TextAnchor.MiddleCenter, Gold, new Vector2(x, -rowY), new Vector2(86f, 28f));
            key.fontStyle = FontStyle.Bold;
            RuntimeUIKit.MakeText("KeyUse", parent, items[i].Item2, 14, TextAnchor.MiddleLeft, Cream, new Vector2(x + 98f, -rowY), new Vector2(290f, 28f));
        }
    }

    private Slider Row(Transform parent, string label, float y, out Text value, out Image toggle, out Text toggleLabel, UnityAction<float> onChange, UnityAction onToggle)
    {
        RuntimeUIKit.MakeText("Label", parent, label, 16, TextAnchor.MiddleLeft, Cream, new Vector2(44f, -y), new Vector2(160f, 30f));
        var slider = MakeSlider(parent, new Vector2(220f, -y), onChange);
        value = RuntimeUIKit.MakeText("Value", parent, "", 15, TextAnchor.MiddleLeft, Gold, new Vector2(220f + SliderWidth + 14f, -y), new Vector2(90f, 30f));
        var button = RuntimeUIKit.MakeButton("Toggle", parent, "Couper", 14, Slate, new Vector2(860f - 44f - 110f, -y), new Vector2(110f, 30f));
        button.onClick.AddListener(onToggle);
        toggle = button.GetComponent<Image>();
        toggleLabel = button.GetComponentInChildren<Text>();
        return slider;
    }

    private void RefreshSettings()
    {
        var music = MusicManager.Instance;
        musicSlider.SetValueWithoutNotify(music != null ? music.MusicVolume : 0.5f);
        sfxSlider.SetValueWithoutNotify(Sfx.Volume);
        RefreshValues();
        var full = UnityEngine.Screen.fullScreen;
        fullToggleLabel.text = full ? "Fenêtré" : "Activer";
        fullToggle.color = Slate;
    }

    private void RefreshValues()
    {
        var musicMuted = MusicManager.Instance != null && MusicManager.Instance.IsMuted;
        var sfxMuted = Sfx.Muted;
        musicValue.text = musicMuted ? "Coupée" : $"{Mathf.RoundToInt(musicSlider.value * 100f)} %";
        sfxValue.text = sfxMuted ? "Coupés" : $"{Mathf.RoundToInt(sfxSlider.value * 100f)} %";
        musicValue.color = musicMuted ? new Color(0.9f, 0.45f, 0.42f) : Gold;
        sfxValue.color = sfxMuted ? new Color(0.9f, 0.45f, 0.42f) : Gold;
        musicToggleLabel.text = musicMuted ? "Activer" : "Couper";
        sfxToggleLabel.text = sfxMuted ? "Activer" : "Couper";
        musicToggle.color = musicMuted ? Wine : Slate;
        sfxToggle.color = sfxMuted ? Wine : Slate;
    }

    private void OnMusicChanged(float value)
    {
        MusicManager.Instance?.SetMusicVolume(value);
        RefreshValues();
    }

    private void OnSfxChanged(float value)
    {
        Sfx.Volume = value;
        RefreshValues();
        Sfx.Play(Sfx.Id.Coin, 0.9f);
    }

    private void ToggleMusic()
    {
        var music = MusicManager.Instance;
        if (music == null) return;
        music.SetMuted(!music.IsMuted);
        RefreshValues();
    }

    private void ToggleSfx()
    {
        Sfx.Muted = !Sfx.Muted;
        RefreshValues();
        Sfx.Play(Sfx.Id.Coin, 0.9f);
    }

    private void ToggleFullscreen()
    {
        var target = !UnityEngine.Screen.fullScreen;
        UnityEngine.Screen.fullScreen = target;
        fullToggleLabel.text = target ? "Fenêtré" : "Activer";
    }

    private static Slider MakeSlider(Transform parent, Vector2 position, UnityAction<float> onChange)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, new Vector2(SliderWidth, 30f));

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 10f);
        backgroundRect.anchoredPosition = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(6f, -5f);
        fillAreaRect.offsetMax = new Vector2(-6f, 5f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1f);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.85f, 0.68f, 0.28f, 1f);

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0f);
        handleRect.anchorMax = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(20f, 0f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = RuntimeUIKit.Rounded;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.96f, 0.92f, 0.80f, 1f);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.onValueChanged.AddListener(onChange);
        return slider;
    }
}
