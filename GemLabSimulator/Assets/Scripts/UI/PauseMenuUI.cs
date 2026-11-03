using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The pause menu, opened with Escape: the volume of the music and the volume of the sound effects, a way back to the game and a way back
// to the main menu (which asks for a confirmation). Built at runtime on the Canvas of every game scene.
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private const float PanelWidth = 460f;
    private const float PanelHeight = 330f;
    private const float SliderWidth = 230f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.30f, 0.38f, 1f);

    private GameObject dim;
    private GameObject panel;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text musicValue;
    private Text sfxValue;
    private Image musicToggle;
    private Text musicToggleLabel;
    private Image sfxToggle;
    private Text sfxToggleLabel;
    private bool lockedInput;

    public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

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
        if (canvas != null && canvas.GetComponent<PauseMenuUI>() == null) canvas.AddComponent<PauseMenuUI>();
    }

    // Escape: opens the menu, closes it, or backs out of the confirmation to return to the main menu.
    public static void Toggle()
    {
        if (ReturnToMenuUI.Instance != null && ReturnToMenuUI.Instance.IsOpen)
        {
            ReturnToMenuUI.Instance.Cancel();
            return;
        }
        if (Instance == null)
        {
            ReturnToMenuUI.Instance?.Toggle();
            return;
        }
        if (IsOpen) Instance.Close();
        else Instance.Open();
    }

    private void Awake()
    {
        Instance = this;
        Build();
        dim.SetActive(false);
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (lockedInput) PlayerController.InputLocked = false;
    }

    private void Open()
    {
        dim.SetActive(true);
        panel.SetActive(true);
        dim.transform.SetAsLastSibling();
        panel.transform.SetAsLastSibling();
        if (!PlayerController.InputLocked)
        {
            PlayerController.InputLocked = true;
            lockedInput = true;
        }

        var music = MusicManager.Instance;
        musicSlider.SetValueWithoutNotify(music != null ? music.MusicVolume : 0.5f);
        sfxSlider.SetValueWithoutNotify(Sfx.Volume);
        RefreshValues();
    }

    public void Close()
    {
        if (dim != null) dim.SetActive(false);
        if (panel != null) panel.SetActive(false);
        if (lockedInput)
        {
            PlayerController.InputLocked = false;
            lockedInput = false;
        }
    }

    private void ToMainMenu()
    {
        Close();
        ReturnToMenuUI.Instance?.Toggle();
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
        musicToggle.color = musicMuted ? new Color(0.55f, 0.24f, 0.22f, 1f) : ButtonColor;
        sfxToggle.color = sfxMuted ? new Color(0.55f, 0.24f, 0.22f, 1f) : ButtonColor;
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
        // Back on: a sample.
        Sfx.Play(Sfx.Id.Coin, 0.9f);
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
        // A sample, so the level can be judged.
        Sfx.Play(Sfx.Id.Coin, 0.9f);
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private void Build()
    {
        dim = new GameObject("PauseDim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(transform, false);
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.12f, 0.98f);

        RuntimeUIKit.MakeBox("Edge", panel.transform, Vector2.zero, new Vector2(PanelWidth, 4f), new Color(0.80f, 0.64f, 0.26f, 1f), false).raycastTarget = false;
        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Pause", 26, TextAnchor.UpperCenter, Gold, new Vector2(0f, -20f), new Vector2(PanelWidth, 34f));
        title.fontStyle = FontStyle.Bold;

        RuntimeUIKit.MakeText("MusicLabel", panel.transform, "Volume de la musique", 15, TextAnchor.UpperLeft, Color.white, new Vector2(40f, -78f), new Vector2(300f, 22f));
        musicSlider = MakeSlider(new Vector2(40f, -106f), 0.5f, OnMusicChanged);
        musicValue = RuntimeUIKit.MakeText("MusicValue", panel.transform, "", 14, TextAnchor.MiddleLeft, Gold, new Vector2(40f + SliderWidth + 10f, -106f), new Vector2(64f, 26f));
        var musicButton = RuntimeUIKit.MakeButton("MusicToggle", panel.transform, "Couper", 13, ButtonColor, new Vector2(PanelWidth - 40f - 76f, -105f), new Vector2(76f, 28f));
        musicButton.onClick.AddListener(ToggleMusic);
        musicToggle = musicButton.GetComponent<Image>();
        musicToggleLabel = musicButton.GetComponentInChildren<Text>();

        RuntimeUIKit.MakeText("SfxLabel", panel.transform, "Volume des bruitages", 15, TextAnchor.UpperLeft, Color.white, new Vector2(40f, -156f), new Vector2(300f, 22f));
        sfxSlider = MakeSlider(new Vector2(40f, -184f), 0.45f, OnSfxChanged);
        sfxValue = RuntimeUIKit.MakeText("SfxValue", panel.transform, "", 14, TextAnchor.MiddleLeft, Gold, new Vector2(40f + SliderWidth + 10f, -184f), new Vector2(64f, 26f));
        var sfxButton = RuntimeUIKit.MakeButton("SfxToggle", panel.transform, "Couper", 13, ButtonColor, new Vector2(PanelWidth - 40f - 76f, -183f), new Vector2(76f, 28f));
        sfxButton.onClick.AddListener(ToggleSfx);
        sfxToggle = sfxButton.GetComponent<Image>();
        sfxToggleLabel = sfxButton.GetComponentInChildren<Text>();

        var resume = RuntimeUIKit.MakeButton("Resume", panel.transform, "Reprendre", 17, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(40f, -250f), new Vector2(180f, 42f));
        resume.onClick.AddListener(Close);
        var menu = RuntimeUIKit.MakeButton("MainMenu", panel.transform, "Menu principal", 17, ButtonColor, new Vector2(PanelWidth - 220f, -250f), new Vector2(180f, 42f));
        menu.onClick.AddListener(ToMainMenu);
        RuntimeUIKit.MakeText("Hint", panel.transform, "Échap : fermer", 11, TextAnchor.UpperCenter, new Color(0.55f, 0.55f, 0.62f), new Vector2(0f, -304f), new Vector2(PanelWidth, 16f));
    }

    private Slider MakeSlider(Vector2 position, float initial, UnityAction<float> onChange)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, new Vector2(SliderWidth, 26f));

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);
        backgroundRect.anchoredPosition = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(6f, -4f);
        fillAreaRect.offsetMax = new Vector2(-6f, 4f);

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
        handleRect.sizeDelta = new Vector2(18f, 0f);
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
        slider.value = initial;
        slider.onValueChanged.AddListener(onChange);
        return slider;
    }
}
