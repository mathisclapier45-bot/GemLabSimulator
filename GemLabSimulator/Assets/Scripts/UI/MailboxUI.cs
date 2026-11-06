using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The mailbox of the laboratory. The mail comes in envelopes of four colours: green for the donations, blue for the stones of the clients to identify,
// cream for the letters of the characters of the town (to come), gold for the weekly catalogue of the auctions. Opening a letter shows its message
// on a sheet of paper (the request of the client), and the stone lies in a gemologist's fold next to it. The other page sends the identified stones
// of the clients back. Built at runtime: the panel of the scene, if it has one, is replaced.
public class MailboxUI : MonoBehaviour
{
    public static MailboxUI Instance { get; private set; }

    private const float WindowWidth = 700f;
    private const float WindowHeight = 560f;
    private const float CardHeight = 74f;
    private const float CardGap = 8f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.22f, 0.17f, 0.12f, 1f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.24f, 0.18f, 1f);
    private static readonly Color GreenButton = new Color(0.25f, 0.50f, 0.30f, 1f);

    // The panel the scene used to carry: it is dropped for the one built here.
    [SerializeField] private GameObject panel;

    private GameObject window;
    private RectTransform layer;
    private bool showingReceive = true;
    private GemInstance openedLetter;
    private bool openedCatalogue;

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            Destroy(panel);
            panel = null;
        }
        Build();
        window.SetActive(false);
    }

    private void Start()
    {
        if (MailboxState.Instance != null) MailboxState.Instance.OnMailboxChanged += RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (MailboxState.Instance != null) MailboxState.Instance.OnMailboxChanged -= RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    private void Update()
    {
        if (window == null || !window.activeSelf || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (openedLetter != null || openedCatalogue)
        {
            openedLetter = null;
            openedCatalogue = false;
            Refresh();
        }
        else Close();
    }

    public void Toggle()
    {
        var opening = window != null && !window.activeSelf;
        if (window != null) window.SetActive(opening);
        if (!opening) return;

        window.transform.SetAsLastSibling();
        MailboxState.Instance?.EnsureStocked();
        AuctionState.Ensure()?.EnsureLots();
        ShowReceiveTab();
    }

    public void Close()
    {
        if (window != null) window.SetActive(false);
    }

    public void ShowReceiveTab()
    {
        showingReceive = true;
        openedLetter = null;
        openedCatalogue = false;
        Refresh();
    }

    public void ShowSendTab()
    {
        showingReceive = false;
        openedLetter = null;
        openedCatalogue = false;
        Refresh();
    }

    private void RefreshIfOpen()
    {
        if (window != null && window.activeSelf) Refresh();
    }

    // ---- Pages ---------------------------------------------------------------------------------------------------

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        var state = MailboxState.Instance;
        var pending = state != null ? state.PendingLetters.Count : 0;
        var remaining = state != null ? state.LettersRemainingToday : 0;

        Tab("Courrier reçu" + (pending > 0 ? $" ({pending})" : ""), true, 24f);
        Tab("Colis à renvoyer", false, 214f);
        Txt("Hint", showingReceive ? $"{remaining} lettre(s) à récupérer encore aujourd'hui" : "Les pierres de clients identifiées, avec leur rapport", 11,
            new Vector2(WindowWidth - 330f, -90f), new Vector2(310f, 16f), new Color(0.70f, 0.64f, 0.55f), TextAnchor.UpperRight);

        if (!showingReceive) BuildSend();
        else if (openedCatalogue) BuildCatalogueLetter();
        else if (openedLetter != null) BuildLetter(openedLetter);
        else BuildList();
        StardewKit.Skin(layer);
    }

    private void Tab(string label, bool receive, float x)
    {
        var on = showingReceive == receive;
        var button = RuntimeUIKit.MakeButton("Tab", layer, label, 13, on ? new Color(0.55f, 0.38f, 0.16f, 1f) : new Color(0.24f, 0.19f, 0.15f, 1f), new Vector2(x, -84f), new Vector2(180f, 30f));
        button.onClick.AddListener(() =>
        {
            if (receive) ShowReceiveTab();
            else ShowSendTab();
        });
    }

    // The envelopes waiting: the catalogue of the auctions (gold) first, then the letters.
    private void BuildList()
    {
        var index = 0;
        var auction = AuctionState.Instance;
        if (auction != null && StoryGate.AuctionsOpen)
        {
            var date = auction.CatalogueDate;
            var theme = AuctionCatalog.ThemeFor(date);
            Card(index++, LetterKind.Catalogue, $"Bulletin des ventes n° {auction.CatalogueIssue}", $"Vente du {GameCalendar.Long(date)} — thème : {theme.Name}",
                "Ouvrir", true, () => { openedCatalogue = true; Refresh(); });
        }

        var letters = MailboxState.Instance != null ? MailboxState.Instance.PendingLetters : null;
        if (letters == null || letters.Count == 0)
        {
            Txt("Empty", "Aucune autre lettre en attente.", 13, new Vector2(30f, -140f - index * (CardHeight + CardGap)), new Vector2(400f, 20f), new Color(0.72f, 0.66f, 0.56f));
            return;
        }

        foreach (var letter in letters)
        {
            var captured = letter;
            var kind = letter.IsDonation ? LetterKind.Donation : LetterKind.Client;
            var title = letter.IsDonation ? "Don d'un généreux mécène" : $"Lettre de {letter.ClientName}";
            var label = letter.IsRough && !letter.IsDonation ? "Pierre brute à identifier" : MailLetters.KindLabel(kind);
            Card(index++, kind, $"{label} — {title}", MailLetters.Preview(letter), "Ouvrir", true, () => { openedLetter = captured; Refresh(); });
        }
    }

    private void Card(int index, LetterKind kind, string title, string subtitle, string buttonLabel, bool enabled, Action onClick)
    {
        var y = -128f - index * (CardHeight + CardGap);
        var tint = Color.Lerp(new Color(0.12f, 0.10f, 0.09f, 1f), MailLetters.BodyColor(kind), 0.16f);
        RuntimeUIKit.MakeBox("Card", layer, new Vector2(24f, y), new Vector2(WindowWidth - 48f, CardHeight), tint).raycastTarget = false;
        RuntimeUIKit.MakeBox("Stripe", layer, new Vector2(24f, y), new Vector2(5f, CardHeight), MailLetters.BodyColor(kind), false).raycastTarget = false;

        Icon(MailSprites.Envelope(kind), new Vector2(42f, y - 17f), new Vector2(68f, 48f));

        var head = Txt("Title", title, 14, new Vector2(124f, y - 10f), new Vector2(WindowWidth - 124f - 150f, 20f), Color.Lerp(MailLetters.BodyColor(kind), Color.white, 0.5f));
        head.fontStyle = FontStyle.Bold;
        Txt("Sub", subtitle, 11, new Vector2(124f, y - 32f), new Vector2(WindowWidth - 124f - 150f, 34f), new Color(0.80f, 0.76f, 0.68f));

        var button = RuntimeUIKit.MakeButton("Open", layer, buttonLabel, 13, enabled ? ButtonColor : new Color(0.22f, 0.22f, 0.24f, 1f), new Vector2(WindowWidth - 134f, y - 20f), new Vector2(100f, 34f));
        button.interactable = enabled;
        if (enabled) button.onClick.AddListener(() => onClick());
    }

    // An opened letter: its message on the paper, the stone in its fold.
    private void BuildLetter(GemInstance letter)
    {
        var kind = letter.IsDonation ? LetterKind.Donation : LetterKind.Client;
        Paper(MailLetters.PaperColor(kind), kind, MailLetters.MessageFor(letter));

        Icon(MailSprites.Fold(), new Vector2(470f, -150f), new Vector2(132f, 132f));
        Txt("FoldTitle", "Pli de gemmologue", 14, new Vector2(430f, -292f), new Vector2(220f, 20f), Gold, TextAnchor.UpperCenter).fontStyle = FontStyle.Bold;
        Txt("FoldText", letter.IsDonation ? "Une pierre à conserver une fois identifiée." : letter.IsRough ? "Un cristal brut : pas de facette polie, donc pas de réfractomètre. Une fenêtre polie à l'atelier laisse voir l'intérieur." : "La pierre du client, à identifier puis à lui renvoyer avec son rapport.", 11,
            new Vector2(430f, -314f), new Vector2(220f, 44f), new Color(0.80f, 0.76f, 0.68f), TextAnchor.UpperCenter);

        var remaining = MailboxState.Instance != null ? MailboxState.Instance.LettersRemainingToday : 0;
        var take = RuntimeUIKit.MakeButton("Take", layer, "Prendre la pierre", 15, remaining > 0 ? GreenButton : new Color(0.25f, 0.25f, 0.28f, 1f), new Vector2(430f, -380f), new Vector2(220f, 42f));
        take.interactable = remaining > 0;
        take.onClick.AddListener(() => TakeLetter(letter));
        RuntimeUIKit.MakeButton("Back", layer, "Retour au courrier", 13, ButtonColor, new Vector2(430f, -430f), new Vector2(220f, 34f)).onClick.AddListener(() =>
        {
            openedLetter = null;
            Refresh();
        });
    }

    private void BuildCatalogueLetter()
    {
        var auction = AuctionState.Instance;
        if (auction == null)
        {
            openedCatalogue = false;
            BuildList();
            return;
        }

        var date = auction.CatalogueDate;
        var theme = AuctionCatalog.ThemeFor(date);
        Paper(MailLetters.PaperColor(LetterKind.Catalogue), LetterKind.Catalogue, MailLetters.CatalogueMessage(date, theme.Name, theme.Description, auction.CatalogueIssue));

        Icon(MailSprites.Envelope(LetterKind.Catalogue), new Vector2(470f, -170f), new Vector2(160f, 114f));
        Txt("Gold", "Enveloppe dorée", 14, new Vector2(430f, -300f), new Vector2(220f, 20f), Gold, TextAnchor.UpperCenter).fontStyle = FontStyle.Bold;
        Txt("GoldText", "Le catalogue des lots de la semaine est dans votre sac.", 11, new Vector2(430f, -322f), new Vector2(220f, 34f), new Color(0.80f, 0.76f, 0.68f), TextAnchor.UpperCenter);

        RuntimeUIKit.MakeButton("Read", layer, "Lire le bulletin", 15, GreenButton, new Vector2(430f, -380f), new Vector2(220f, 42f)).onClick.AddListener(() =>
        {
            Close();
            BagUI.Instance?.ShowTab("Enchères");
        });
        RuntimeUIKit.MakeButton("Back", layer, "Retour au courrier", 13, ButtonColor, new Vector2(430f, -430f), new Vector2(220f, 34f)).onClick.AddListener(() =>
        {
            openedCatalogue = false;
            Refresh();
        });
    }

    // The sheet of paper with the message.
    private void Paper(Color paper, LetterKind kind, string message)
    {
        RuntimeUIKit.MakeBox("PaperShadow", layer, new Vector2(30f, -134f), new Vector2(384f, 396f), new Color(0f, 0f, 0f, 0.35f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Paper", layer, new Vector2(26f, -128f), new Vector2(384f, 396f), paper).raycastTarget = false;
        RuntimeUIKit.MakeBox("PaperTop", layer, new Vector2(26f, -128f), new Vector2(384f, 6f), MailLetters.BodyColor(kind), false).raycastTarget = false;
        Icon(MailSprites.Envelope(kind), new Vector2(350f, -140f), new Vector2(50f, 36f));
        var text = Txt("Message", message, 13, new Vector2(44f, -146f), new Vector2(296f, 360f), Ink);
        text.fontStyle = FontStyle.Italic;
        text.lineSpacing = 1.1f;
    }

    // The stones to send back.
    private void BuildSend()
    {
        var inventory = PlayerInventory.Instance;
        var sendable = new List<GemInstance>();
        if (inventory != null)
            foreach (var gem in inventory.Gems)
                if (gem.IsIdentified && gem.IsClientStone) sendable.Add(gem);

        if (sendable.Count == 0)
        {
            Txt("None", "Aucune gemme de client identifiée à renvoyer.", 14, new Vector2(30f, -140f), new Vector2(500f, 22f), new Color(0.72f, 0.66f, 0.56f));
            return;
        }

        for (var i = 0; i < sendable.Count && i < 6; i++)
        {
            var gem = sendable[i];
            var y = -128f - i * (CardHeight + CardGap);
            RuntimeUIKit.MakeBox("Card", layer, new Vector2(24f, y), new Vector2(WindowWidth - 48f, CardHeight), new Color(0.15f, 0.13f, 0.12f, 1f)).raycastTarget = false;
            Icon(MailSprites.Fold(), new Vector2(40f, y - 14f), new Vector2(46f, 46f));

            var head = Txt("Name", $"{gem.Definition.displayName} (N° {gem.DisplayId})", 14, new Vector2(104f, y - 10f), new Vector2(WindowWidth - 104f - 170f, 20f), Color.white);
            head.fontStyle = FontStyle.Bold;
            Txt("Report", gem.HasReport ? $"Rapport prêt : {gem.IdentificationReportFee} € à recevoir du client." : "Le client attend le rapport GemLab : imprimez-le d'abord à l'imprimante du laboratoire.", 11,
                new Vector2(104f, y - 32f), new Vector2(WindowWidth - 104f - 170f, 34f), gem.HasReport ? new Color(0.62f, 0.95f, 0.66f) : new Color(1f, 0.78f, 0.62f));

            var captured = gem;
            var button = RuntimeUIKit.MakeButton("Send", layer, "Envoyer", 13, gem.HasReport ? GreenButton : new Color(0.25f, 0.25f, 0.28f, 1f), new Vector2(WindowWidth - 134f, y - 20f), new Vector2(100f, 34f));
            button.interactable = gem.HasReport;
            button.onClick.AddListener(() => SendGem(captured));
        }
    }

    // ---- Actions -------------------------------------------------------------------------------------------------

    private void TakeLetter(GemInstance letter)
    {
        if (PlayerInventory.Instance == null || MailboxState.Instance == null) return;

        if (MailboxState.Instance.LettersRemainingToday <= 0)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Vous avez déjà reçu vos 5 lettres aujourd'hui. Revenez demain !");
            return;
        }

        if (!MailboxState.Instance.TryTakeLetter(letter, PlayerInventory.Instance))
        {
            ToolFeedbackUI.Instance?.ShowMessage("Inventaire plein : traitez d'abord une gemme.");
            return;
        }

        Sfx.Play(Sfx.Id.Paper);
        ToolFeedbackUI.Instance?.ShowMessage(letter.IsDonation
            ? "Vous avez récupéré un don anonyme, à conserver une fois identifié."
            : $"Vous avez récupéré le pli de {letter.ClientName}.");
        MailboxState.Instance.EnsureStocked();
        openedLetter = null;
        Refresh();
    }

    private void SendGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null || PlayerWallet.Instance == null) return;

        if (!gem.HasReport)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Le client attend le rapport GemLab : imprimez-le d'abord à l'imprimante du laboratoire.");
            return;
        }

        // The client pays for the identification report, by weight of the stone.
        var fee = gem.IdentificationReportFee;
        PlayerInventory.Instance.RemoveGem(gem);
        PlayerWallet.Instance.AddMoney(fee);
        QuestState.Instance?.ReportGemSent(fee);
        ToolFeedbackUI.Instance?.ShowMessage($"Colis envoyé à {gem.ClientName}. Rapport d'identification payé : +{fee}€");
    }

    // ---- Construction --------------------------------------------------------------------------------------------

    private Text Txt(string name, string content, int size, Vector2 position, Vector2 dimensions, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var text = RuntimeUIKit.MakeText(name, layer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void Icon(Sprite sprite, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layer, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void Build()
    {
        window = new GameObject("MailboxWindow", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(transform, false);
        var rect = window.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(WindowWidth, WindowHeight);
        StardewKit.FramePanel(window.GetComponent<Image>());

        // The header: the logo of the mail and the title.
        RuntimeUIKit.MakeBox("Band", window.transform, new Vector2(12f, -12f), new Vector2(WindowWidth - 24f, 60f), new Color(0.27f, 0.18f, 0.11f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Edge", window.transform, new Vector2(12f, -72f), new Vector2(WindowWidth - 24f, 3f), new Color(0.80f, 0.64f, 0.26f, 1f), false).raycastTarget = false;
        var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
        logo.transform.SetParent(window.transform, false);
        RuntimeUIKit.SetTopLeft(logo.GetComponent<RectTransform>(), new Vector2(24f, -10f), new Vector2(76f, 54f));
        var logoImage = logo.GetComponent<Image>();
        logoImage.sprite = MailSprites.Logo();
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        var title = RuntimeUIKit.MakeText("Title", window.transform, "Boîte aux lettres", 24, TextAnchor.UpperLeft, Gold, new Vector2(112f, -10f), new Vector2(420f, 32f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", window.transform, "Courrier du laboratoire de gemmologie", 12, TextAnchor.UpperLeft, new Color(0.80f, 0.70f, 0.55f), new Vector2(112f, -44f), new Vector2(420f, 18f));
        RuntimeUIKit.MakeButton("Close", window.transform, "Fermer", 13, new Color(0.45f, 0.20f, 0.16f, 1f), new Vector2(WindowWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(window.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        StardewKit.Skin(window.transform);
    }
}
