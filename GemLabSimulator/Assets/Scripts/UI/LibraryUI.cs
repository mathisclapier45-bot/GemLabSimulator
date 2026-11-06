using UnityEngine;
using UnityEngine.UI;

// The desk of the library: the Guide of the gemmologist, with its cover, what it holds and its price. Once bought, the book is in the bag (its own
// tab, with a page for every species of the grimoire). Built at runtime.
public class LibraryUI : MonoBehaviour
{
    public static LibraryUI Instance { get; private set; }

    private const float PanelWidth = 640f;
    private const float PanelHeight = 430f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.82f, 0.78f, 0.70f);
    private static readonly Color Leather = new Color(0.13f, 0.34f, 0.30f, 1f);
    private static readonly Color LeatherDark = new Color(0.08f, 0.22f, 0.19f, 1f);

    private GameObject panel;
    private RectTransform layer;
    private Text subtitle;

    public static void TryOpen()
    {
        GuideBookState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<LibraryUI>();
        }
        Instance?.Open();
    }

    private void Awake()
    {
        Instance = this;
        Build();
        panel.SetActive(false);
    }

    private void Start()
    {
        var state = GuideBookState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GuideBookState.Instance != null) GuideBookState.Instance.OnChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
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

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void RefreshMoney(int _) => RefreshIfOpen();

    private static int Money => PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;

    private Text Txt(string name, string content, int size, Vector2 position, Vector2 dimensions, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var text = RuntimeUIKit.MakeText(name, layer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        subtitle.text = $"Sur vous : {Money:N0} €";
        var owned = GuideBookState.HasBook;
        var species = GemDatabase.Instance != null ? GemDatabase.Instance.allGems.Count : 0;

        DrawCover(34f, -92f);

        const float x = 270f;
        Txt("Name", "Le Guide du gemmologue", 20, new Vector2(x, -92f), new Vector2(340f, 28f), Gold, true);
        Txt("Sub", "Le grimoire, relié en un seul volume", 12, new Vector2(x, -120f), new Vector2(340f, 18f), new Color(0.62f, 0.82f, 1f));
        Txt("About",
            $"Une page pour chacune des {species} espèces du grimoire, qu'on les ait déjà rencontrées ou non : l'éclat, la couleur, l'indice de réfraction, la densité, la dureté, " +
            "la fluorescence, le spectre d'absorption, les inclusions typiques...\n\n" +
            "Chaque page dit aussi quels traitements le laboratoire sait appliquer à la pierre, et ce que montrent les instruments (spectromètres UV-visible, infrarouge, Raman, " +
            "fluorescence X, MEB, LA-ICP-MS, photoluminescence) sur une pierre naturelle de référence.\n\n" +
            "Une fois acheté, il reste dans votre sac, dans l'onglet des livres.",
            12, new Vector2(x, -150f), new Vector2(340f, 190f), Ink);

        if (owned)
        {
            Txt("Owned", "Vous possédez ce livre : ouvrez le sac (H), onglet « Livres ».", 13, new Vector2(x, -352f), new Vector2(340f, 40f), new Color(0.55f, 0.85f, 0.55f), true);
        }
        else
        {
            Txt("Price", $"{GuideBookState.Price:N0} €", 22, new Vector2(x, -350f), new Vector2(140f, 30f), Gold, true);
            var can = Money >= GuideBookState.Price;
            var buy = RuntimeUIKit.MakeButton("Buy", layer, can ? "Acheter" : "Pas assez d'argent", 14, can ? new Color(0.25f, 0.50f, 0.30f, 1f) : new Color(0.30f, 0.28f, 0.28f, 1f),
                new Vector2(x + 150f, -346f), new Vector2(190f, 36f));
            buy.interactable = can;
            buy.onClick.AddListener(Buy);
        }
        StardewKit.Skin(layer);
    }

    private void Buy()
    {
        var state = GuideBookState.Ensure();
        if (state == null) return;
        if (state.TryBuy(out var message))
        {
            Sfx.Play(Sfx.Id.Chat);
            ToolFeedbackUI.Instance?.ShowMessage("Le Guide du gemmologue est dans votre sac (H), onglet des livres.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            ToolFeedbackUI.Instance?.ShowMessage(message);
        }
        Refresh();
    }

    // The cover: green leather, a gold frame, a gold diamond and the title.
    private void DrawCover(float x, float y)
    {
        RuntimeUIKit.MakeBox("Back", layer, new Vector2(x + 6f, y - 6f), new Vector2(200f, 270f), new Color(0f, 0f, 0f, 0.35f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Cover", layer, new Vector2(x, y), new Vector2(200f, 270f), Leather, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Spine", layer, new Vector2(x, y), new Vector2(18f, 270f), LeatherDark, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameTop", layer, new Vector2(x + 30f, y - 16f), new Vector2(158f, 2f), Gold, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameBottom", layer, new Vector2(x + 30f, y - 252f), new Vector2(158f, 2f), Gold, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameLeft", layer, new Vector2(x + 30f, y - 16f), new Vector2(2f, 238f), Gold, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameRight", layer, new Vector2(x + 186f, y - 16f), new Vector2(2f, 238f), Gold, false).raycastTarget = false;

        // A cut diamond, drawn with turned squares.
        DrawDiamond("Gem", new Vector2(x + 109f, y - 64f), 40f, new Color(0.62f, 0.86f, 0.98f));
        DrawDiamond("Glint", new Vector2(x + 102f, y - 57f), 14f, new Color(0.92f, 0.98f, 1f));

        var title = Txt("CoverTitle", "GUIDE\ndu\nGEMMOLOGUE", 20, new Vector2(x + 34f, y - 112f), new Vector2(150f, 90f), Gold, true, TextAnchor.UpperCenter);
        title.lineSpacing = 1.05f;
        Txt("CoverSub", "toutes les pierres\ndu grimoire", 11, new Vector2(x + 34f, y - 208f), new Vector2(150f, 36f), new Color(0.80f, 0.90f, 0.82f), false, TextAnchor.UpperCenter);
    }

    // A square turned by 45 degrees, around its centre (given from the top left of the layer).
    private void DrawDiamond(string name, Vector2 center, float size, Color color)
    {
        var box = RuntimeUIKit.MakeBox(name, layer, Vector2.zero, new Vector2(size, size), color, false);
        box.raycastTarget = false;
        var rect = box.rectTransform;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void Build()
    {
        panel = new GameObject("LibraryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Bibliothèque de la Ville", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -16f), new Vector2(PanelWidth - 160f, 30f));
        title.fontStyle = FontStyle.Bold;
        subtitle = RuntimeUIKit.MakeText("Subtitle", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(30f, -50f), new Vector2(PanelWidth - 60f, 18f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        StardewKit.Skin(panel.transform);
    }
}
