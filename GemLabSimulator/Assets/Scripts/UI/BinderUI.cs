using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The card binder (B, or the "Classeur" button of the inventory): two pages side by side, nine cards per page (3 x 3), in the order of
// the collection. A card not owned yet leaves an empty slot. A card owned several times shows a counter at the top right of its artwork.
// Click a card to see it large. Built at runtime and added to the scene's Canvas automatically. The bag adopts the binder as one of its
// tabs (B): it then lives inside the bag panel and the bag handles the key, the closing and the Escape key.
public class BinderUI : MonoBehaviour
{
    public static BinderUI Instance { get; private set; }

    private const int CardsPerPage = 9;
    private const int CardsPerSpread = CardsPerPage * 2;
    private const float CardWidth = 122f;
    private const float CardHeight = 170f;
    private const float CardGap = 10f;
    private const float PageWidth = 3 * CardWidth + 2 * CardGap;
    private const float PageHeight = 3 * CardHeight + 2 * CardGap;
    private const float PanelWidth = 900f;
    private const float PanelHeight = 690f;
    private const float PageTop = 96f;
    private const float LeftPageX = 34f;
    private const float RightPageX = PanelWidth - 34f - PageWidth;

    private static readonly Color PanelColor = new Color(0.16f, 0.10f, 0.07f, 0.98f);
    private static readonly Color PageColor = new Color(0.24f, 0.17f, 0.12f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.36f, 0.26f, 0.18f, 1f);

    private static int escapeFrame = -1;
    public static bool ConsumesEscape => (Instance != null && Instance.IsOpen) || escapeFrame == Time.frameCount;

    private GameObject panel;
    private Transform pageContainer;
    private Text titleText;
    private Text progressText;
    private Text pageText;
    private GameObject detail;
    private int spread;
    private bool lockedInput;

    private bool inBag;

    public bool IsOpen => !inBag && panel != null && panel.activeSelf;
    public GameObject Panel => panel;
    public bool HasDetail => detail != null;
    private bool Showing => panel != null && (inBag ? panel.activeInHierarchy : panel.activeSelf);

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
        if (canvas != null && canvas.GetComponent<BinderUI>() == null) canvas.AddComponent<BinderUI>();
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        var cards = PlayerCards.Ensure();
        if (cards != null) cards.OnCardsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerCards.Instance != null) PlayerCards.Instance.OnCardsChanged -= RefreshIfOpen;
        if (lockedInput) PlayerController.InputLocked = false;
    }

    private void RefreshIfOpen()
    {
        if (Showing) Rebuild();
    }

    // The bag takes the binder panel as a tab: it fills the bag panel, whose tabs replace the title and the close button.
    public void AttachToBag(Transform bagPanel)
    {
        inBag = true;
        Close();
        panel.transform.SetParent(bagPanel, false);
        panel.transform.SetAsFirstSibling();
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        titleText.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        var close = panel.transform.Find("Close");
        if (close != null) close.gameObject.SetActive(false);
    }

    public void ShowInBag()
    {
        Sfx.Play(Sfx.Id.BookOpen);
        Rebuild();
    }

    public void HideInBag()
    {
        CloseDetail();
    }

    public void CloseDetailInBag()
    {
        CloseDetail();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (inBag)
        {
            BagUI.Instance?.ShowTab("Classeur");
            return;
        }
        Sfx.Play(Sfx.Id.BookOpen);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (!PlayerController.InputLocked)
        {
            PlayerController.InputLocked = true;
            lockedInput = true;
        }
        Rebuild();
    }

    public void Close()
    {
        CloseDetail();
        if (panel != null && panel.activeSelf) Sfx.Play(Sfx.Id.BookClose);
        if (panel != null) panel.SetActive(false);
        if (lockedInput)
        {
            PlayerController.InputLocked = false;
            lockedInput = false;
        }
    }

    private void Update()
    {
        if (inBag)
        {
            if (panel.activeInHierarchy && detail == null)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) GoToSpread(spread + 1);
                if (Input.GetKeyDown(KeyCode.LeftArrow)) GoToSpread(spread - 1);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.B) && !BoosterUI.IsShowing && StoryState.BinderOpen) Toggle();
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapeFrame = Time.frameCount;
            if (detail != null) CloseDetail();
            else Close();
            return;
        }

        if (detail != null) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) GoToSpread(spread + 1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) GoToSpread(spread - 1);
    }

    private int SpreadCount => Mathf.Max(1, Mathf.CeilToInt(CardCatalog.Count / (float)CardsPerSpread));

    private void GoToSpread(int index)
    {
        var clamped = Mathf.Clamp(index, 0, SpreadCount - 1);
        if (clamped == spread && pageContainer.childCount > 0) return;
        if (pageContainer.childCount > 0) Sfx.Play(Mathf.Abs(clamped - spread) > 1 ? Sfx.Id.PageFlurry : Sfx.Id.PageTurn);
        spread = clamped;
        Rebuild();
    }

    private void JumpToSeries(CardSeries series)
    {
        for (var i = 0; i < CardCatalog.All.Count; i++)
        {
            if (CardCatalog.All[i].Series != series) continue;
            GoToSpread(i / CardsPerSpread);
            return;
        }
    }

    // ---- Content -------------------------------------------------------------------------------------------------

    private void Rebuild()
    {
        for (var i = pageContainer.childCount - 1; i >= 0; i--) Destroy(pageContainer.GetChild(i).gameObject);

        var cards = PlayerCards.Instance;
        var all = CardCatalog.All;
        for (var slot = 0; slot < CardsPerSpread; slot++)
        {
            var index = spread * CardsPerSpread + slot;
            if (index >= all.Count) break;

            var card = all[index];
            var pageX = slot < CardsPerPage ? LeftPageX : RightPageX;
            var local = slot % CardsPerPage;
            var position = new Vector2(pageX + (local % 3) * (CardWidth + CardGap), -(PageTop + (local / 3) * (CardHeight + CardGap)));
            var count = cards != null ? cards.Count(card.Id) : 0;

            if (count > 0) MakeOwnedSlot(card, position, count);
            else MakeEmptySlot(card, position);
        }

        UpdateTexts(cards);
    }

    private void MakeOwnedSlot(CardDef card, Vector2 position, int count)
    {
        var view = CardView.Create(pageContainer, card, new Vector2(CardWidth, CardHeight), true, count);
        RuntimeUIKit.SetTopLeft(view.GetComponent<RectTransform>(), position, new Vector2(CardWidth, CardHeight));
        var button = view.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => ShowDetail(card));
    }

    private void MakeEmptySlot(CardDef card, Vector2 position)
    {
        var slot = RuntimeUIKit.MakeBox("Empty_" + card.Id, pageContainer, position, new Vector2(CardWidth, CardHeight), new Color(0.12f, 0.09f, 0.07f, 1f));
        var number = RuntimeUIKit.MakeText("Number", slot.transform, $"n° {card.Order}", 15, TextAnchor.MiddleCenter, new Color(0.5f, 0.42f, 0.34f),
            new Vector2(0f, -CardHeight * 0.38f), new Vector2(CardWidth, 26f));
        number.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Question", slot.transform, "?", 34, TextAnchor.MiddleCenter, new Color(0.36f, 0.30f, 0.25f),
            new Vector2(0f, -CardHeight * 0.18f), new Vector2(CardWidth, 50f));
        RuntimeUIKit.MakeText("Series", slot.transform, CardCatalog.SeriesName(card.Series), 10, TextAnchor.MiddleCenter, new Color(0.45f, 0.38f, 0.30f),
            new Vector2(0f, -CardHeight * 0.70f), new Vector2(CardWidth, 16f));
    }

    private void UpdateTexts(PlayerCards cards)
    {
        var unique = cards != null ? cards.UniqueCards : 0;
        var total = CardCatalog.Count;
        titleText.text = "Classeur de cartes";
        var progress = $"{unique} / {total} cartes  ·  {Mathf.RoundToInt(100f * unique / Mathf.Max(1, total))} %";
        progressText.text = progress;

        var first = spread * CardsPerSpread + 1;
        var last = Mathf.Min(total, (spread + 1) * CardsPerSpread);
        var leftPage = spread * 2 + 1;
        var pages = $"Pages {leftPage}-{leftPage + 1}  ·  cartes {first} à {last}  ·  {spread + 1} / {SpreadCount}";
        pageText.text = inBag ? $"{progress}\n{pages}" : pages;
    }

    // ---- Large card ------------------------------------------------------------------------------------------------

    private void ShowDetail(CardDef card)
    {
        CloseDetail();
        Sfx.Play(Sfx.Id.CardSwish);

        detail = new GameObject("CardDetail", typeof(RectTransform), typeof(Image), typeof(Button));
        detail.transform.SetParent(panel.transform, false);
        var rect = detail.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        detail.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        detail.GetComponent<Button>().onClick.AddListener(CloseDetail);

        var count = PlayerCards.Instance != null ? PlayerCards.Instance.Count(card.Id) : 1;
        var size = new Vector2(300f, 420f);
        var view = CardView.Create(detail.transform, card, size, false, count);
        RuntimeUIKit.SetTopLeft(view.GetComponent<RectTransform>(), new Vector2((PanelWidth - size.x) / 2f, -(PanelHeight - size.y) / 2f + 20f), size);
        view.GetComponent<Image>().raycastTarget = false;

        var info = RuntimeUIKit.MakeText("Copies", detail.transform,
            $"{CardCatalog.SeriesName(card.Series)}  ·  {count} exemplaire{(count > 1 ? "s" : "")}   —   clic pour fermer", 14,
            TextAnchor.MiddleCenter, Gold, new Vector2(0f, -PanelHeight + 44f), new Vector2(PanelWidth, 24f));
        info.raycastTarget = false;
    }

    private void CloseDetail()
    {
        if (detail != null) Destroy(detail);
        detail = null;
    }

    // ---- Construction ----------------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("BinderPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays visible.
        rect.anchoredPosition = new Vector2(-150f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "", 22, TextAnchor.UpperLeft, Gold, new Vector2(34f, -14f), new Vector2(360f, 30f));
        progressText = RuntimeUIKit.MakeText("Progress", panel.transform, "", 14, TextAnchor.UpperRight, new Color(0.85f, 0.78f, 0.66f),
            new Vector2(PanelWidth - 400f, -18f), new Vector2(330f, 24f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 14, new Color(0.45f, 0.20f, 0.18f, 1f),
            new Vector2(PanelWidth - 58f, -12f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        // Jump to a series.
        var series = new[] { CardSeries.Element, CardSeries.MasterStone, CardSeries.Gem, CardSeries.Famous, CardSeries.Crystal, CardSeries.Synthesis, CardSeries.Tool };
        for (var i = 0; i < series.Length; i++)
        {
            var captured = series[i];
            var button = RuntimeUIKit.MakeButton("Series" + i, panel.transform, CardCatalog.SeriesName(series[i]), 12, ButtonColor,
                new Vector2(34f + i * 120f, -52f), new Vector2(114f, 26f));
            button.onClick.AddListener(() => JumpToSeries(captured));
        }

        // The two pages and the spine between them.
        RuntimeUIKit.MakeBox("PageLeft", panel.transform, new Vector2(LeftPageX - 10f, -(PageTop - 10f)), new Vector2(PageWidth + 20f, PageHeight + 20f), PageColor);
        RuntimeUIKit.MakeBox("PageRight", panel.transform, new Vector2(RightPageX - 10f, -(PageTop - 10f)), new Vector2(PageWidth + 20f, PageHeight + 20f), PageColor);
        var spine = RuntimeUIKit.MakeBox("Spine", panel.transform, new Vector2(LeftPageX + PageWidth + 12f, -(PageTop - 10f)),
            new Vector2(RightPageX - LeftPageX - PageWidth - 24f, PageHeight + 20f), new Color(0.10f, 0.06f, 0.04f, 1f), false);
        spine.raycastTarget = false;

        var container = new GameObject("Cards", typeof(RectTransform));
        container.transform.SetParent(panel.transform, false);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        pageContainer = container.transform;

        // Footer.
        var footerY = -(PageTop + PageHeight + 22f);
        var previous = RuntimeUIKit.MakeButton("Previous", panel.transform, "<  Page précédente", 14, ButtonColor, new Vector2(34f, footerY), new Vector2(180f, 30f));
        previous.onClick.AddListener(() => GoToSpread(spread - 1));
        var next = RuntimeUIKit.MakeButton("Next", panel.transform, "Page suivante  >", 14, ButtonColor, new Vector2(PanelWidth - 34f - 180f, footerY), new Vector2(180f, 30f));
        next.onClick.AddListener(() => GoToSpread(spread + 1));
        pageText = RuntimeUIKit.MakeText("PageText", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.85f, 0.78f, 0.66f),
            new Vector2(230f, footerY), new Vector2(PanelWidth - 460f, 30f));
    }
}
