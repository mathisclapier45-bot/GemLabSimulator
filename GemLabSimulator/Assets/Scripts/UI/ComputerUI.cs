using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The lab computer's screen. It starts with a three-second boot screen (GemLab logo), then shows a Windows XP style
// desktop: the "Fichiers" icon in the top-right corner opens the list of microphotographs (named after the stone, its
// lab number and the photo number) and a viewer to look at them and scroll from one to the next. Built at runtime and
// added to the scene's Canvas automatically.
public class ComputerUI : MonoBehaviour
{
    public static ComputerUI Instance { get; private set; }

    private const float BootDuration = 3f;
    private const float WindowWidth = 840f;
    private const float WindowHeight = 520f;
    private const float RowHeight = 24f;
    private const float ListWidth = 282f;
    private const float ListHeight = 356f;
    private const float PhotoSize = 340f;
    private const float SlideshowInterval = 2.5f;

    private static readonly Color TitleBlue = Rgb(0, 84, 227);
    private static readonly Color FrameBlue = Rgb(0, 60, 200);
    private static readonly Color Beige = Rgb(236, 233, 216);
    private static readonly Color Ink = Rgb(20, 20, 24);
    private static readonly Color Highlight = Rgb(49, 106, 197);
    private static readonly Color ButtonGrey = Rgb(214, 210, 192);

    private struct PhotoFile
    {
        public MicroPhotoEntry entry;
        public string name;
    }

    private GameObject panel;
    private GameObject boot;
    private GameObject desktop;
    private GameObject startMenu;
    private GameObject filesWindow;
    private GameObject taskButton;
    private RectTransform[] bootBlocks;
    private Image wallpaper;
    private Text clockText;
    private RectTransform listContent;
    private RectTransform rightPane;
    private RawImage photoImage;
    private Text emptyText;
    private Text infoText;
    private Text countText;
    private Text statusText;
    private Image slideshowButtonImage;
    private Text slideshowLabel;

    private readonly List<PhotoFile> files = new List<PhotoFile>();
    private readonly List<Image> rowBackgrounds = new List<Image>();
    private readonly List<Text> rowTexts = new List<Text>();
    private Texture2D photoTexture;
    private Canvas canvas;

    private bool booting;
    private float bootTimer;
    private bool slideshow;
    private float slideshowTimer;
    private int selected = -1;

    private static Sprite folderSprite;
    private static Sprite fileSprite;

    private static int escapeFrame = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    // True while the computer is on, or in the frame where it just handled Esc: the pause menu must not open as well.
    public static bool ConsumesEscape => (Instance != null && Instance.IsOpen) || escapeFrame == Time.frameCount;

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
        if (canvas != null && canvas.GetComponent<ComputerUI>() == null) canvas.AddComponent<ComputerUI>();
    }

    private void Awake()
    {
        Instance = this;
        canvas = GetComponent<Canvas>();
        BuildPanel();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        PlayerController.InputLocked = false;
        ReleasePhoto();
    }

    public void Open()
    {
        if (IsOpen) return;

        EnsureWallpaper();
        startMenu.SetActive(false);
        filesWindow.SetActive(false);
        taskButton.SetActive(false);
        slideshow = false;

        booting = true;
        bootTimer = 0f;
        boot.SetActive(true);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        PlayerController.InputLocked = true;
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        PlayerController.InputLocked = false;
        slideshow = false;
        ReleasePhoto();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapeFrame = Time.frameCount;
            if (startMenu.activeSelf) startMenu.SetActive(false);
            else if (filesWindow.activeSelf) CloseWindow();
            else Close();
            return;
        }

        if (booting)
        {
            bootTimer += Time.unscaledDeltaTime;
            AnimateBoot();
            if (bootTimer >= BootDuration)
            {
                booting = false;
                boot.SetActive(false);
            }
            return;
        }

        clockText.text = DateTime.Now.ToString("HH:mm");

        if (!filesWindow.activeSelf || files.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Step(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Step(1);

        var wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f && PointerOver(rightPane)) Step(wheel > 0f ? -1 : 1);

        if (slideshow)
        {
            slideshowTimer += Time.unscaledDeltaTime;
            if (slideshowTimer >= SlideshowInterval)
            {
                slideshowTimer = 0f;
                Select((selected + 1) % files.Count, true);
            }
        }
    }

    private bool PointerOver(RectTransform rect)
    {
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
    }

    private void AnimateBoot()
    {
        const float step = 18f;
        const float clipWidth = 196f;
        var travel = clipWidth + bootBlocks.Length * step;
        var head = (bootTimer * 150f) % travel;
        for (var i = 0; i < bootBlocks.Length; i++)
            RuntimeUIKit.SetTopLeft(bootBlocks[i], new Vector2(head - step * (i + 1), -2f), new Vector2(14f, 12f));
    }

    // ---------------------------------------------------------------- building

    private void BuildPanel()
    {
        panel = new GameObject("ComputerPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = Color.black;

        BuildDesktop();
        BuildWindow();
        BuildBoot();
    }

    private void BuildBoot()
    {
        boot = new GameObject("Boot", typeof(RectTransform), typeof(Image));
        boot.transform.SetParent(panel.transform, false);
        Stretch(boot.GetComponent<RectTransform>());
        boot.GetComponent<Image>().color = Color.black;

        var center = Container("Center", boot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 280f));

        var emblem = RuntimeUIKit.MakeBox("Emblem", center, new Vector2(150f, -10f), new Vector2(120f, 120f), Color.white, false);
        emblem.sprite = GemIconFactory.GetSolid("Rond", "Brillant", new Color(0.78f, 0.62f, 0.25f), new Color(0.98f, 0.88f, 0.55f));
        emblem.preserveAspect = true;
        emblem.raycastTarget = false;

        var title = RuntimeUIKit.MakeText("Title", center, "GemLab", 52, TextAnchor.UpperCenter, Color.white, new Vector2(0f, -134f), new Vector2(420f, 64f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", center, "Laboratoire de gemmologie", 16, TextAnchor.UpperCenter, new Color(0.62f, 0.66f, 0.78f),
            new Vector2(0f, -198f), new Vector2(420f, 22f));

        RuntimeUIKit.MakeBox("Track", center, new Vector2(110f, -232f), new Vector2(200f, 20f), Rgb(150, 156, 180));
        var clip = new GameObject("Clip", typeof(RectTransform), typeof(RectMask2D));
        clip.transform.SetParent(center, false);
        RuntimeUIKit.SetTopLeft(clip.GetComponent<RectTransform>(), new Vector2(112f, -234f), new Vector2(196f, 16f));

        bootBlocks = new RectTransform[3];
        for (var i = 0; i < bootBlocks.Length; i++)
        {
            var block = RuntimeUIKit.MakeBox("Block" + i, clip.transform, Vector2.zero, new Vector2(14f, 12f), Rgb(52, 120, 238), false);
            block.raycastTarget = false;
            bootBlocks[i] = block.GetComponent<RectTransform>();
        }

        RuntimeUIKit.MakeText("Loading", center, "Démarrage de l'ordinateur du laboratoire…", 12, TextAnchor.UpperCenter, new Color(0.5f, 0.53f, 0.62f),
            new Vector2(0f, -262f), new Vector2(420f, 18f));
    }

    private void BuildDesktop()
    {
        desktop = new GameObject("Desktop", typeof(RectTransform), typeof(Image), typeof(Button));
        desktop.transform.SetParent(panel.transform, false);
        Stretch(desktop.GetComponent<RectTransform>());
        wallpaper = desktop.GetComponent<Image>();
        wallpaper.color = Rgb(58, 110, 165);
        var background = desktop.GetComponent<Button>();
        background.transition = Selectable.Transition.None;
        background.onClick.AddListener(() => startMenu.SetActive(false));

        // "Fichiers" icon, top right.
        var icon = Container("FilesIcon", desktop.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(84f, 84f));
        var hit = icon.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        var iconButton = icon.gameObject.AddComponent<Button>();
        iconButton.transition = Selectable.Transition.None;
        iconButton.onClick.AddListener(OpenWindow);

        var folder = RuntimeUIKit.MakeBox("Folder", icon, new Vector2(10f, -2f), new Vector2(64f, 64f), Color.white, false);
        folder.sprite = FolderSprite();
        folder.preserveAspect = true;
        folder.raycastTarget = false;
        var label = RuntimeUIKit.MakeText("Label", icon, "Fichiers", 14, TextAnchor.UpperCenter, Color.white, new Vector2(0f, -64f), new Vector2(84f, 20f));
        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);

        BuildTaskbar();
    }

    private void BuildTaskbar()
    {
        var bar = Container("Taskbar", desktop.transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 32f));
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 32f);
        var barImage = bar.gameObject.AddComponent<Image>();
        barImage.color = Rgb(36, 94, 220);

        var start = RuntimeUIKit.MakeButton("Start", bar, "démarrer", 17, Rgb(57, 160, 45), Vector2.zero, new Vector2(116f, 32f));
        var startLabel = start.GetComponentInChildren<Text>();
        startLabel.fontStyle = FontStyle.BoldAndItalic;
        start.onClick.AddListener(() =>
        {
            startMenu.SetActive(!startMenu.activeSelf);
            startMenu.transform.SetAsLastSibling();
        });

        var task = RuntimeUIKit.MakeBox("TaskButton", bar, new Vector2(124f, -4f), new Vector2(170f, 24f), Rgb(60, 124, 238));
        taskButton = task.gameObject;
        var taskLabel = RuntimeUIKit.MakeText("Label", task.transform, "Fichiers", 13, TextAnchor.MiddleLeft, Color.white, new Vector2(8f, 0f), new Vector2(160f, 24f));
        taskLabel.raycastTarget = false;
        task.raycastTarget = false;

        var tray = Container("Tray", bar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(96f, 32f));
        tray.gameObject.AddComponent<Image>().color = Rgb(15, 140, 230);
        clockText = RuntimeUIKit.MakeText("Clock", tray, "", 13, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(96f, 32f));

        // Start menu.
        var menu = Container("StartMenu", desktop.transform, Vector2.zero, Vector2.zero, new Vector2(0f, 32f), new Vector2(240f, 128f));
        startMenu = menu.gameObject;
        menu.gameObject.AddComponent<Image>().color = Rgb(250, 250, 250);
        RuntimeUIKit.MakeBox("Header", menu, Vector2.zero, new Vector2(240f, 36f), Rgb(31, 90, 210), false);
        var user = RuntimeUIKit.MakeText("User", menu, "GemLab", 16, TextAnchor.MiddleLeft, Color.white, new Vector2(12f, 0f), new Vector2(200f, 36f));
        user.fontStyle = FontStyle.Bold;

        var menuFiles = RuntimeUIKit.MakeButton("MenuFiles", menu, "Fichiers", 14, Rgb(250, 250, 250), new Vector2(8f, -44f), new Vector2(224f, 32f));
        menuFiles.GetComponentInChildren<Text>().color = Ink;
        menuFiles.onClick.AddListener(OpenWindow);
        var off = RuntimeUIKit.MakeButton("MenuOff", menu, "Éteindre l'ordinateur", 14, Rgb(250, 250, 250), new Vector2(8f, -84f), new Vector2(224f, 32f));
        off.GetComponentInChildren<Text>().color = Ink;
        off.onClick.AddListener(Close);
    }

    private void BuildWindow()
    {
        var window = Container("FilesWindow", desktop.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(WindowWidth, WindowHeight));
        filesWindow = window.gameObject;
        var frame = window.gameObject.AddComponent<Image>();
        frame.color = FrameBlue;

        // Title bar.
        RuntimeUIKit.MakeBox("TitleBar", window, new Vector2(2f, -2f), new Vector2(WindowWidth - 4f, 28f), TitleBlue, false);
        RuntimeUIKit.MakeBox("TitleShine", window, new Vector2(2f, -2f), new Vector2(WindowWidth - 4f, 3f), Rgb(80, 160, 255), false);
        var titleIcon = RuntimeUIKit.MakeBox("TitleIcon", window, new Vector2(8f, -7f), new Vector2(18f, 18f), Color.white, false);
        titleIcon.sprite = FolderSprite();
        titleIcon.preserveAspect = true;
        titleIcon.raycastTarget = false;
        var title = RuntimeUIKit.MakeText("Title", window, "Fichiers — Microphotographies", 14, TextAnchor.MiddleLeft, Color.white,
            new Vector2(32f, -2f), new Vector2(500f, 28f));
        title.fontStyle = FontStyle.Bold;
        var close = RuntimeUIKit.MakeButton("Close", window, "X", 14, Rgb(204, 62, 38), new Vector2(WindowWidth - 30f, -6f), new Vector2(22f, 22f));
        close.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        close.onClick.AddListener(CloseWindow);

        // Body.
        RuntimeUIKit.MakeBox("Body", window, new Vector2(3f, -30f), new Vector2(WindowWidth - 6f, WindowHeight - 33f), Beige, false);

        // Toolbar.
        var previous = RuntimeUIKit.MakeButton("Previous", window, "◄ Précédente", 13, ButtonGrey, new Vector2(10f, -36f), new Vector2(112f, 26f));
        previous.GetComponentInChildren<Text>().color = Ink;
        previous.onClick.AddListener(() => Step(-1));
        var next = RuntimeUIKit.MakeButton("Next", window, "Suivante ►", 13, ButtonGrey, new Vector2(128f, -36f), new Vector2(112f, 26f));
        next.GetComponentInChildren<Text>().color = Ink;
        next.onClick.AddListener(() => Step(1));
        var show = RuntimeUIKit.MakeButton("Slideshow", window, "Diaporama", 13, ButtonGrey, new Vector2(246f, -36f), new Vector2(112f, 26f));
        slideshowButtonImage = show.GetComponent<Image>();
        slideshowLabel = show.GetComponentInChildren<Text>();
        slideshowLabel.color = Ink;
        show.onClick.AddListener(ToggleSlideshow);
        countText = RuntimeUIKit.MakeText("Count", window, "", 13, TextAnchor.MiddleRight, Ink, new Vector2(WindowWidth - 220f, -36f), new Vector2(206f, 26f));

        // File list: header, then a scrolling list of rows.
        RuntimeUIKit.MakeBox("ListFrame", window, new Vector2(9f, -70f), new Vector2(ListWidth + 2f, ListHeight + 24f), Rgb(127, 157, 185), false);
        RuntimeUIKit.MakeBox("ListHeader", window, new Vector2(10f, -71f), new Vector2(ListWidth, 22f), Rgb(235, 234, 224), false);
        RuntimeUIKit.MakeText("ListHeaderText", window, "Nom", 13, TextAnchor.MiddleLeft, Ink, new Vector2(18f, -71f), new Vector2(200f, 22f));

        var scrollGO = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(window, false);
        RuntimeUIKit.SetTopLeft(scrollGO.GetComponent<RectTransform>(), new Vector2(10f, -93f), new Vector2(ListWidth, ListHeight));
        scrollGO.GetComponent<Image>().color = Color.white;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGO.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewport.transform, false);
        listContent = contentGO.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = Vector2.zero;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = listContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        emptyText = RuntimeUIKit.MakeText("Empty", window, "Ce dossier est vide.\nPrenez des photos avec la caméra du microscope.", 13,
            TextAnchor.UpperCenter, new Color(0.4f, 0.4f, 0.45f), new Vector2(18f, -130f), new Vector2(ListWidth - 16f, 60f));

        // Viewer.
        var paneGO = new GameObject("Viewer", typeof(RectTransform), typeof(Image));
        paneGO.transform.SetParent(window, false);
        rightPane = paneGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(rightPane, new Vector2(302f, -70f), new Vector2(WindowWidth - 312f, 380f));
        paneGO.GetComponent<Image>().color = Rgb(28, 30, 36);

        var photoGO = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
        photoGO.transform.SetParent(paneGO.transform, false);
        RuntimeUIKit.SetTopLeft(photoGO.GetComponent<RectTransform>(), new Vector2((WindowWidth - 312f - PhotoSize) * 0.5f, -8f), new Vector2(PhotoSize, PhotoSize));
        photoImage = photoGO.GetComponent<RawImage>();
        photoImage.raycastTarget = false;
        photoImage.enabled = false;

        infoText = RuntimeUIKit.MakeText("Info", paneGO.transform, "", 12, TextAnchor.UpperCenter, new Color(0.85f, 0.88f, 0.95f),
            new Vector2(8f, -352f), new Vector2(WindowWidth - 328f, 26f));

        // Status bar.
        statusText = RuntimeUIKit.MakeText("Status", window, "", 12, TextAnchor.MiddleLeft, Ink, new Vector2(12f, -476f), new Vector2(400f, 22f));

        filesWindow.SetActive(false);
    }

    // ---------------------------------------------------------------- window and photos

    private void OpenWindow()
    {
        startMenu.SetActive(false);
        filesWindow.SetActive(true);
        filesWindow.transform.SetAsLastSibling();
        taskButton.SetActive(true);
        slideshow = false;
        RefreshSlideshowButton();
        RebuildList();
    }

    private void CloseWindow()
    {
        filesWindow.SetActive(false);
        taskButton.SetActive(false);
        slideshow = false;
        ReleasePhoto();
    }

    private void ToggleSlideshow()
    {
        slideshow = !slideshow && files.Count > 1;
        slideshowTimer = 0f;
        RefreshSlideshowButton();
    }

    private void RefreshSlideshowButton()
    {
        slideshowButtonImage.color = slideshow ? Rgb(150, 190, 245) : ButtonGrey;
        slideshowLabel.text = slideshow ? "Arrêter" : "Diaporama";
    }

    // "Stone_labNumber_photoNumber.png": the photo number counts the photos of that stone, in the order they were taken.
    private static string LabNumber(MicroPhotoEntry entry)
    {
        var id = entry.gemId ?? "";
        var cut = id.IndexOf(" — ", StringComparison.Ordinal);
        return cut > 0 ? id.Substring(0, cut) : id;
    }

    private void BuildFiles()
    {
        files.Clear();
        var library = MicroPhotoLibrary.Ensure();
        if (library == null) return;

        var counters = new Dictionary<string, int>();
        foreach (var entry in library.Photos)
        {
            var lab = LabNumber(entry);
            counters.TryGetValue(lab, out var number);
            number++;
            counters[lab] = number;

            var stone = string.IsNullOrEmpty(entry.gemName) ? "Pierre" : entry.gemName.Trim().Replace(' ', '_');
            files.Add(new PhotoFile { entry = entry, name = $"{stone}_{lab}_{number:D2}.png" });
        }

        files.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }

    private void RebuildList()
    {
        for (var i = listContent.childCount - 1; i >= 0; i--)
        {
            var child = listContent.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        rowBackgrounds.Clear();
        rowTexts.Clear();

        BuildFiles();
        listContent.sizeDelta = new Vector2(0f, files.Count * RowHeight);
        listContent.anchoredPosition = Vector2.zero;

        for (var i = 0; i < files.Count; i++)
        {
            var index = i;
            var row = RuntimeUIKit.MakeBox("Row" + i, listContent, new Vector2(0f, -i * RowHeight), new Vector2(ListWidth, RowHeight), Color.white, false);
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Select(index, false));

            var icon = RuntimeUIKit.MakeBox("Icon", row.transform, new Vector2(6f, -4f), new Vector2(16f, 16f), Color.white, false);
            icon.sprite = FileSprite();
            icon.raycastTarget = false;
            var label = RuntimeUIKit.MakeText("Name", row.transform, files[i].name, 13, TextAnchor.MiddleLeft, Ink, new Vector2(28f, 0f), new Vector2(ListWidth - 32f, RowHeight));

            rowBackgrounds.Add(row);
            rowTexts.Add(label);
        }

        emptyText.gameObject.SetActive(files.Count == 0);
        selected = -1;
        if (files.Count > 0) Select(0, false);
        else ShowSelection();
    }

    private void Step(int direction)
    {
        if (files.Count == 0) return;
        Select((selected + direction + files.Count) % files.Count, true);
        slideshowTimer = 0f;
    }

    private void Select(int index, bool scrollTo)
    {
        if (index < 0 || index >= files.Count) return;
        selected = index;
        ShowSelection();

        if (!scrollTo) return;

        // Keep the selected row inside the list's window.
        var top = index * RowHeight;
        var offset = listContent.anchoredPosition.y;
        if (top < offset) offset = top;
        else if (top + RowHeight > offset + ListHeight) offset = top + RowHeight - ListHeight;
        listContent.anchoredPosition = new Vector2(0f, Mathf.Max(0f, offset));
    }

    private void ShowSelection()
    {
        for (var i = 0; i < rowBackgrounds.Count; i++)
        {
            var on = i == selected;
            rowBackgrounds[i].color = on ? Highlight : Color.white;
            rowTexts[i].color = on ? Color.white : Ink;
        }

        statusText.text = $"{files.Count} objet(s)";
        countText.text = files.Count == 0 ? "" : $"Photo {selected + 1} / {files.Count}";
        ReleasePhoto();

        if (selected < 0 || selected >= files.Count)
        {
            photoImage.enabled = false;
            infoText.text = "Aucune photo à afficher.";
            return;
        }

        var file = files[selected];
        var library = MicroPhotoLibrary.Instance;
        photoTexture = library != null ? library.LoadImage(file.entry) : null;
        photoImage.texture = photoTexture;
        photoImage.enabled = photoTexture != null;

        var entry = file.entry;
        var line = photoTexture != null ? file.name : $"{file.name} — fichier introuvable";
        infoText.text = $"{line}\n{entry.gemName} ({LabNumber(entry)}) — ×{entry.magnification:F0} — {entry.lighting} — jour {entry.day}";
    }

    private void ReleasePhoto()
    {
        if (photoTexture != null) Destroy(photoTexture);
        photoTexture = null;
        if (photoImage != null) photoImage.texture = null;
    }

    // ---------------------------------------------------------------- drawing helpers

    private static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform Container(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    // Bliss-like wallpaper: blue sky with clouds above a green hill. Drawn once, at low resolution, and stretched.
    private void EnsureWallpaper()
    {
        if (wallpaper == null || wallpaper.sprite != null) return;

        const int width = 256;
        const int height = 144;
        var pixels = new Color32[width * height];
        var skyTop = new Color(0.14f, 0.38f, 0.85f);
        var skyLow = new Color(0.62f, 0.80f, 0.97f);
        var grassDark = new Color(0.16f, 0.42f, 0.08f);
        var grassLight = new Color(0.46f, 0.74f, 0.14f);

        for (var y = 0; y < height; y++)
        {
            var v = y / (float)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var u = x / (float)(width - 1);
                var hill = 0.30f + 0.20f * Mathf.Exp(-(u - 0.30f) * (u - 0.30f) / 0.10f) + 0.015f * Mathf.Sin(u * 9f);

                Color color;
                if (v < hill)
                {
                    var depth = (hill - v) / hill;
                    var lit = Mathf.Clamp01(0.85f - depth * 0.9f + 0.25f * Mathf.Sin(u * 3f + 1f) * (1f - u));
                    color = Color.Lerp(grassDark, grassLight, lit);
                }
                else
                {
                    var sky = Color.Lerp(skyLow, skyTop, Mathf.Clamp01((v - hill) / (1f - hill)));
                    var cloud = Mathf.PerlinNoise(u * 4.5f + 3f, v * 7f + 7f) * 0.62f + Mathf.PerlinNoise(u * 11f, v * 16f) * 0.38f;
                    var edge = Mathf.InverseLerp(0.52f, 0.78f, cloud);
                    var amount = edge * edge * (3f - 2f * edge) * (0.35f + 0.65f * v);
                    color = Color.Lerp(sky, Color.white, amount);
                }

                pixels[y * width + x] = color;
            }
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        wallpaper.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        wallpaper.color = Color.white;
    }

    private static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
    {
        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                pixels[y * size + x] = color;
    }

    private static Sprite MakeSprite(Color32[] pixels, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Yellow folder, 32 px.
    private static Sprite FolderSprite()
    {
        if (folderSprite != null) return folderSprite;

        const int size = 32;
        var pixels = new Color32[size * size];
        var edge = new Color32(176, 128, 20, 255);
        var back = new Color32(226, 176, 52, 255);
        var body = new Color32(250, 214, 96, 255);
        var light = new Color32(255, 236, 160, 255);

        Fill(pixels, size, 2, 22, 14, 27, edge);
        Fill(pixels, size, 3, 22, 13, 26, back);
        Fill(pixels, size, 2, 4, 30, 24, edge);
        Fill(pixels, size, 3, 5, 29, 23, back);
        Fill(pixels, size, 3, 5, 29, 20, body);
        Fill(pixels, size, 3, 15, 29, 20, light);

        folderSprite = MakeSprite(pixels, size);
        return folderSprite;
    }

    // Small picture file, 16 px: white page with a sky and a hill.
    private static Sprite FileSprite()
    {
        if (fileSprite != null) return fileSprite;

        const int size = 16;
        var pixels = new Color32[size * size];
        Fill(pixels, size, 2, 1, 14, 15, new Color32(120, 128, 140, 255));
        Fill(pixels, size, 3, 2, 13, 14, new Color32(255, 255, 255, 255));
        Fill(pixels, size, 4, 8, 12, 13, new Color32(90, 150, 240, 255));
        Fill(pixels, size, 4, 3, 12, 8, new Color32(70, 160, 60, 255));

        fileSprite = MakeSprite(pixels, size);
        return fileSprite;
    }
}
