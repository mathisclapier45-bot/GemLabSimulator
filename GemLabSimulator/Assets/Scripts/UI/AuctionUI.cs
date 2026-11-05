using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The auction room of the bank. On the day of the sale (every Saturday) the lots are shown with their catalogue: pick one, raise the paddle and the
// price climbs while the buyers of the room drop out; lower the paddle to give up. The other days the room shows the theme of the next sale and
// takes the stones of the player for it. A third page holds what the sales brought. Built at runtime.
public class AuctionUI : MonoBehaviour
{
    public static AuctionUI Instance { get; private set; }

    private const float PanelWidth = 760f;
    private const float PanelHeight = 600f;
    private const float RowHeight = 48f;
    private const int PageSize = 7;
    private const float TickSeconds = 0.9f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.30f, 0.38f, 1f);
    private static readonly Color GreenColor = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color RowColor = new Color(0.14f, 0.15f, 0.19f, 1f);

    private enum Tab { Sale, Consign, Mine }

    private GameObject panel;
    private RectTransform layer;
    private Text subtitle;

    private Tab tab;
    private int page;

    // The lot being auctioned.
    private AuctionLot lot;
    private int price;
    private bool paddleUp;
    private bool running;
    private bool ended;
    private float clock;
    private Text priceText;
    private Text buyersText;
    private Text logText;
    private Text paddleLabel;

    public static void TryOpen()
    {
        AuctionState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<AuctionUI>();
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
        var state = AuctionState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (AuctionState.Instance != null) AuctionState.Instance.OnChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
        lot = null;
        tab = AuctionState.IsAuctionToday ? Tab.Sale : Tab.Consign;
        page = 0;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        running = false;
        paddleUp = false;
        lot = null;
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf && lot == null) Refresh();
    }

    private void RefreshMoney(int _)
    {
        if (panel != null && panel.activeSelf && lot == null) Refresh();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape) && !running) { Close(); return; }

        if (lot == null || !running || ended) return;
        clock += Time.deltaTime;
        if (clock < TickSeconds) return;
        clock = 0f;
        Advance();
    }

    // ---- The clock -----------------------------------------------------------------------------------------------

    private static int Money => PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;

    private void OpenLot(AuctionLot chosen)
    {
        lot = chosen;
        price = chosen.Start;
        paddleUp = false;
        running = false;
        ended = false;
        clock = 0f;
        BuildBidding($"Les enchères s'ouvrent à {price:N0} €. Levez la palette pour entrer dans la vente.");
    }

    private void TogglePaddle()
    {
        if (lot == null || ended) return;

        if (!paddleUp)
        {
            if (AuctionCatalog.WithPremium(price) > Money)
            {
                ToolFeedbackUI.Instance?.ShowMessage("Vous n'avez pas assez d'argent pour enchérir (frais de 10 % compris).");
                Sfx.Play(Sfx.Id.Error);
                return;
            }
            paddleUp = true;
            running = true;
            clock = TickSeconds * 0.6f;
            Say($"Palette levée à {price:N0} €.");
        }
        else
        {
            GiveUp("Vous baissez la palette.");
        }
        UpdateTexts();
    }

    private void Pass()
    {
        if (lot == null || ended) return;
        GiveUp("Vous laissez passer ce lot.");
    }

    // The player is out: the room goes on without them.
    private void GiveUp(string reason)
    {
        paddleUp = false;
        running = false;
        var hammer = AuctionCatalog.HammerWithoutPlayer(lot, out var sold);
        if (!sold)
        {
            lot.Sold = true;
            lot.ToPlayer = false;
            lot.Hammer = 0;
            End($"{reason} Personne n'enchérit : lot retiré de la vente.");
            return;
        }
        hammer = Mathf.Max(hammer, price);
        AuctionState.Instance?.SellToRoom(lot, hammer);
        End($"{reason} Adjugé à la salle pour {hammer:N0} €.");
    }

    private void Advance()
    {
        var next = price + lot.Step;
        if (AuctionCatalog.BuyersStillIn(lot, next) == 0)
        {
            Win(price);
            return;
        }

        price = next;
        if (AuctionCatalog.WithPremium(price) > Money)
        {
            GiveUp($"Un acheteur surenchérit à {price:N0} € : vous ne pouvez plus suivre.");
            return;
        }
        Say($"Un acheteur surenchérit : {price:N0} €.");
        UpdateTexts();
    }

    private void Win(int hammer)
    {
        paddleUp = false;
        running = false;
        string message = null;
        if (AuctionState.Instance != null && AuctionState.Instance.TryBuy(lot, hammer, out message))
        {
            Sfx.Play(Sfx.Id.Success);
            End($"Adjugé ! Vous remportez le lot pour {hammer:N0} €, soit {AuctionCatalog.WithPremium(hammer):N0} € avec les frais de 10 %.");
            return;
        }

        ToolFeedbackUI.Instance?.ShowMessage(message ?? "Achat impossible.");
        End($"Personne ne vous suit à {hammer:N0} €, mais {message ?? "l'achat est impossible"} Le lot reste à vendre.");
        ended = true;
    }

    private void End(string text)
    {
        ended = true;
        running = false;
        paddleUp = false;
        Say(text);
        BuildBidding(text);
    }

    private void Say(string text)
    {
        if (logText != null) logText.text = text;
    }

    private void UpdateTexts()
    {
        if (lot == null || priceText == null) return;
        priceText.text = $"{price:N0} €";
        buyersText.text = $"Acheteurs en lice dans la salle : {AuctionCatalog.BuyersStillIn(lot, price)}";
        paddleLabel.text = paddleUp ? "Baisser la palette" : "Lever la palette";
    }

    // ---- Pages ---------------------------------------------------------------------------------------------------

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
        priceText = buyersText = logText = paddleLabel = null;

        var date = GameCalendar.Today;
        subtitle.text = AuctionState.IsAuctionToday
            ? $"Vente du jour — {AuctionCatalog.ThemeFor(date).Name}"
            : $"Prochaine vente : {GameCalendar.Long(AuctionCatalog.NextAuction(date))} — {AuctionCatalog.ThemeFor(AuctionCatalog.NextAuction(date)).Name}";

        BuildTabs();
        if (lot != null) { BuildBidding(null); return; }
        switch (tab)
        {
            case Tab.Sale: BuildSale(); break;
            case Tab.Consign: BuildConsign(); break;
            default: BuildMine(); break;
        }
    }

    private void BuildTabs()
    {
        var state = AuctionState.Instance;
        var pending = state != null ? state.Results.Count : 0;
        TabButton("Vente du jour", Tab.Sale, 30f);
        TabButton("Consigner une pierre", Tab.Consign, 190f);
        TabButton(pending > 0 ? $"Mes ventes ({pending})" : "Mes ventes", Tab.Mine, 370f);
    }

    private void TabButton(string label, Tab target, float x)
    {
        var on = tab == target && lot == null;
        var button = RuntimeUIKit.MakeButton("Tab", layer, label, 13, on ? GreenColor : new Color(0.22f, 0.22f, 0.27f, 1f), new Vector2(x, -84f), new Vector2(150f, 30f));
        button.onClick.AddListener(() =>
        {
            if (running) return;
            tab = target;
            lot = null;
            page = 0;
            Refresh();
        });
    }

    // The sale of the day: the lots.
    private void BuildSale()
    {
        var state = AuctionState.Instance;
        var date = GameCalendar.Today;
        if (state == null) return;

        if (!AuctionState.IsAuctionToday)
        {
            var next = AuctionCatalog.NextAuction(date);
            var theme = AuctionCatalog.ThemeFor(next);
            Txt("Next", $"La prochaine vente aux enchères a lieu {GameCalendar.Long(next)}.\n\nThème : {theme.Name}\n{theme.Description}\n\nPierres déposées pour cette vente : {state.Consignments.Count}",
                15, new Vector2(40f, -150f), new Vector2(PanelWidth - 80f, 200f), new Color(0.95f, 0.90f, 0.78f));
            return;
        }

        var theme0 = AuctionCatalog.ThemeFor(date);
        Txt("Theme", $"Thème du jour : {theme0.Name} — {theme0.Description}", 12, new Vector2(30f, -124f), new Vector2(PanelWidth - 60f, 30f), new Color(0.78f, 0.82f, 0.92f));
        Txt("Money", $"Sur vous : {Money:N0} €   ·   frais acheteur 10 %", 11, new Vector2(30f, -152f), new Vector2(PanelWidth - 60f, 16f), new Color(0.62f, 0.62f, 0.68f));

        var lots = state.EnsureLots();
        if (lots.Count == 0)
        {
            Txt("None", "La salle est vide aujourd'hui.", 15, new Vector2(40f, -190f), new Vector2(400f, 24f), Color.white);
            return;
        }
        for (var i = 0; i < lots.Count && i < 8; i++)
        {
            var l = lots[i];
            var y = -176f - i * (RowHeight + 3f);
            Row(y);
            var status = l.Sold ? (l.ToPlayer ? $"Adjugé à vous : {l.Hammer:N0} €" : l.Hammer > 0 ? $"Adjugé : {l.Hammer:N0} €" : "Retiré")
                : $"Estimation {l.EstimateLow:N0} – {l.EstimateHigh:N0} €";
            Txt("Lot", $"Lot {l.Number} — {l.Name}, {l.Gem.CaratWeight:F2} ct\n{status}  ·  {AuctionCatalog.TreatmentLine(l)}", 12,
                new Vector2(40f, -182f - i * (RowHeight + 3f)), new Vector2(480f, 40f), l.Sold ? new Color(0.62f, 0.62f, 0.68f) : Color.white);
            if (l.Sold) continue;
            var captured = l;
            RuntimeUIKit.MakeButton("Inspect", layer, "Examiner", 12, ButtonColor, new Vector2(540f, y - 9f), new Vector2(86f, 30f)).onClick.AddListener(() => Inspect(captured));
            RuntimeUIKit.MakeButton("Bid", layer, "Enchérir", 12, GreenColor, new Vector2(634f, y - 9f), new Vector2(92f, 30f)).onClick.AddListener(() => OpenLot(captured));
        }
    }

    // The window of one lot.
    private void BuildBidding(string message)
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
        BuildTabs();
        if (lot == null) return;

        var sprite = lot.Gem.DisplaySprite;
        var icon = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(layer, false);
        RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(40f, -136f), new Vector2(96f, 96f));
        var image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        Txt("Catalog", $"Lot {lot.Number}\n{AuctionCatalog.Catalog(lot)}\nEstimation : {lot.EstimateLow:N0} – {lot.EstimateHigh:N0} €", 14,
            new Vector2(156f, -134f), new Vector2(560f, 110f), new Color(0.95f, 0.90f, 0.78f));

        Txt("PriceLabel", "Enchère actuelle", 13, new Vector2(40f, -262f), new Vector2(300f, 18f), new Color(0.7f, 0.7f, 0.76f));
        priceText = Txt("Price", $"{price:N0} €", 44, new Vector2(40f, -282f), new Vector2(420f, 56f), Color.white, true);
        buyersText = Txt("Buyers", "", 13, new Vector2(40f, -342f), new Vector2(520f, 18f), Gold);
        logText = Txt("Log", message ?? "", 14, new Vector2(40f, -372f), new Vector2(PanelWidth - 80f, 56f), new Color(0.82f, 0.88f, 1f));

        if (!ended)
        {
            var paddle = RuntimeUIKit.MakeButton("Paddle", layer, paddleUp ? "Baisser la palette" : "Lever la palette", 18, GreenColor, new Vector2(40f, -450f), new Vector2(250f, 52f));
            paddleLabel = paddle.GetComponentInChildren<Text>();
            paddle.onClick.AddListener(TogglePaddle);
            RuntimeUIKit.MakeButton("Pass", layer, "Laisser passer", 14, new Color(0.46f, 0.32f, 0.22f, 1f), new Vector2(310f, -450f), new Vector2(170f, 52f)).onClick.AddListener(Pass);
            Txt("Info", $"Pas : {lot.Step:N0} €  ·  sur vous : {Money:N0} €", 11, new Vector2(500f, -456f), new Vector2(230f, 40f), new Color(0.62f, 0.62f, 0.68f));
            RuntimeUIKit.MakeButton("Back", layer, "Retour à la liste", 12, ButtonColor, new Vector2(PanelWidth - 190f, -530f), new Vector2(160f, 30f)).onClick.AddListener(Leave);
        }
        else
        {
            RuntimeUIKit.MakeButton("Back", layer, "Retour à la liste", 16, GreenColor, new Vector2(40f, -450f), new Vector2(250f, 52f)).onClick.AddListener(Leave);
        }
        UpdateTexts();
    }

    private void Leave()
    {
        if (running) return;
        lot = null;
        Refresh();
    }

    private void Inspect(AuctionLot chosen)
    {
        if (panel != null) panel.SetActive(false);
        LoupeUI.OpenForMarket(chosen.Gem, () =>
        {
            if (panel != null) panel.SetActive(true);
            Refresh();
        });
    }

    // Leaves a stone for the next sale.
    private void BuildConsign()
    {
        var state = AuctionState.Instance;
        if (state == null) return;

        var next = AuctionCatalog.NextAuction(GameCalendar.Today);
        if (AuctionState.IsAuctionToday) next = AuctionCatalog.AuctionAfter(GameCalendar.Today);
        Txt("Head", $"Pierres déposées pour la vente du {GameCalendar.Long(next)} ({AuctionCatalog.ThemeFor(next).Name}) : {state.Consignments.Count} / {AuctionState.MaxConsignments}.\nCommission de la maison : {Mathf.RoundToInt(AuctionCatalog.SellerCommission * 100)} % sur le prix d'adjudication. Vous fixez un prix de réserve.",
            12, new Vector2(30f, -122f), new Vector2(PanelWidth - 60f, 40f), new Color(0.78f, 0.82f, 0.92f));

        if (AuctionState.IsAuctionToday)
            Txt("Today", "La vente a lieu aujourd'hui : les dépôts pour la suivante se prennent à partir de demain.", 12, new Vector2(30f, -168f), new Vector2(PanelWidth - 60f, 18f), Gold);

        var sellable = new List<GemInstance>();
        if (PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems)
                if (MarketState.CanSell(gem)) sellable.Add(gem);

        if (sellable.Count == 0)
        {
            Txt("Empty", "Aucune pierre identifiée à déposer.\n(Les pierres de clients repartent par courrier.)", 14, new Vector2(40f, -200f), new Vector2(500f, 50f), Color.white);
            return;
        }

        var pages = Mathf.CeilToInt(sellable.Count / (float)PageSize);
        page = Mathf.Clamp(page, 0, pages - 1);
        for (var i = 0; i < PageSize; i++)
        {
            var index = page * PageSize + i;
            if (index >= sellable.Count) break;
            var gem = sellable[index];
            var y = -190f - i * (RowHeight + 3f);
            Row(y);
            var state0 = gem.IsRough ? "brute" : $"{gem.ShapeName}, {gem.CutStyleName.ToLowerInvariant()}";
            var note = gem.IsTreated && gem.IsTreatmentKnown ? "  ·  traitement connu" : "";
            Txt("Stone", $"{gem.Definition.displayName} — {gem.CaratWeight:F2} ct ({state0}){note}\nValeur : {gem.FinalValue:N0} €", 12, new Vector2(40f, y - 6f), new Vector2(520f, 40f), Color.white);
            var captured = gem;
            var enabled = !AuctionState.IsAuctionToday && state.Consignments.Count < AuctionState.MaxConsignments;
            var button = RuntimeUIKit.MakeButton("Consign", layer, "Déposer", 13, enabled ? GreenColor : new Color(0.25f, 0.25f, 0.28f, 1f), new Vector2(600f, y - 9f), new Vector2(120f, 30f));
            if (enabled) button.onClick.AddListener(() => BeginConsign(captured));
        }

        if (pages > 1)
        {
            RuntimeUIKit.MakeButton("Prev", layer, "<", 14, ButtonColor, new Vector2(30f, -560f), new Vector2(36f, 28f)).onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
            Txt("Page", $"{page + 1} / {pages}", 13, new Vector2(72f, -562f), new Vector2(80f, 22f), Color.white, false, TextAnchor.UpperCenter);
            RuntimeUIKit.MakeButton("Next", layer, ">", 14, ButtonColor, new Vector2(158f, -560f), new Vector2(36f, 28f)).onClick.AddListener(() => { page = Mathf.Min(pages - 1, page + 1); Refresh(); });
        }
    }

    private void BeginConsign(GemInstance gem)
    {
        if (!gem.IsTreated || !gem.IsTreatmentKnown)
        {
            AskReserve(gem, false);
            return;
        }

        var risk = TreatmentDisclosure.RiskLabel(TreatmentDisclosure.ChanceFor(gem, 1f));
        ConfirmDialog.ShowChoice("Pierre traitée",
            $"{gem.TreatmentGrade}\nLe catalogue peut l'indiquer. Si vous ne dites rien, la salle paie plus, mais un acheteur peut tester la pierre (risque {risk}).",
            "Déclarer", () => AskReserve(gem, true),
            "Ne rien dire", () => AskReserve(gem, false));
    }

    private void AskReserve(GemInstance gem, bool declare)
    {
        var honesty = !gem.IsTreated ? CatalogHonesty.Untreated : declare ? CatalogHonesty.Declared : CatalogHonesty.Silent;
        var believed = Mathf.RoundToInt(AuctionCatalog.BelievedValue(AuctionCatalog.MakeLot(1, gem, 1, honesty)));
        var low = Mathf.Max(1, Mathf.RoundToInt(believed * 0.55f));
        var high = Mathf.Max(low + 1, Mathf.RoundToInt(believed * 0.85f));
        ConfirmDialog.ShowChoice("Prix de réserve",
            $"Estimation de la salle : environ {believed:N0} €.\nRéserve basse : {low:N0} € (la pierre part presque sûrement).\nRéserve haute : {high:N0} € (elle peut rester invendue).",
            $"Basse {low:N0} €", () => Consign(gem, low, declare),
            $"Haute {high:N0} €", () => Consign(gem, high, declare));
    }

    private void Consign(GemInstance gem, int reserve, bool declare)
    {
        var state = AuctionState.Instance;
        if (state == null) return;
        if (state.TryConsign(gem, reserve, declare, out var message))
        {
            Sfx.Play(Sfx.Id.Success);
            ToolFeedbackUI.Instance?.ShowMessage($"{gem.Definition.displayName} déposé pour la prochaine vente, réserve {reserve:N0} €.");
        }
        else if (!string.IsNullOrEmpty(message)) ToolFeedbackUI.Instance?.ShowMessage(message);
        Refresh();
    }

    // What waits at the house: the stones left for the next sale and the results of the last ones.
    private void BuildMine()
    {
        var state = AuctionState.Instance;
        if (state == null) return;

        var y = -126f;
        if (state.Consignments.Count > 0)
        {
            Txt("PendingTitle", "En attente de vente", 14, new Vector2(30f, y), new Vector2(300f, 20f), Gold, true);
            y -= 24f;
            foreach (var c in state.Consignments)
            {
                Txt("Pending", $"{c.Gem.Definition.displayName} — {c.Gem.CaratWeight:F2} ct · réserve {c.Reserve:N0} € · vente du {GameCalendar.Long(GameCalendar.DateOf(c.AuctionDay))}",
                    12, new Vector2(40f, y), new Vector2(PanelWidth - 80f, 18f), Color.white);
                y -= 20f;
            }
            y -= 10f;
        }

        if (state.Results.Count == 0)
        {
            if (state.Consignments.Count == 0)
                Txt("Nothing", "Rien en attente. Déposez une pierre pour la prochaine vente.", 14, new Vector2(40f, y), new Vector2(560f, 24f), new Color(0.82f, 0.80f, 0.72f));
            return;
        }

        Txt("ResultTitle", "Résultats des ventes", 14, new Vector2(30f, y), new Vector2(300f, 20f), Gold, true);
        y -= 24f;
        var total = 0;
        foreach (var r in state.Results)
        {
            var height = 18f + 15f * Mathf.CeilToInt(r.Message.Length / 95f);
            Txt("Result", r.Message, 12, new Vector2(40f, y), new Vector2(PanelWidth - 80f, height), r.Sold ? new Color(0.62f, 0.95f, 0.66f) : new Color(1f, 0.78f, 0.62f));
            y -= height + 4f;
            total += r.Proceeds - r.Fine;
        }

        var label = total > 0 ? $"Encaisser {total:N0} € et reprendre les pierres" : "Reprendre les pierres";
        RuntimeUIKit.MakeButton("Collect", layer, label, 15, GreenColor, new Vector2(40f, -540f), new Vector2(360f, 40f)).onClick.AddListener(() =>
        {
            var paid = state.Collect(out var message);
            Sfx.Play(Sfx.Id.Success);
            ToolFeedbackUI.Instance?.ShowMessage(message ?? (paid > 0 ? $"Vous touchez {paid:N0} € de la salle des ventes." : "Les pierres sont de retour dans votre sac."));
            Refresh();
        });
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private void Row(float y)
    {
        RuntimeUIKit.MakeBox("Row", layer, new Vector2(30f, y + 2f), new Vector2(PanelWidth - 60f, RowHeight), RowColor, false).raycastTarget = false;
    }

    private Text Txt(string name, string content, int size, Vector2 position, Vector2 dimensions, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var text = RuntimeUIKit.MakeText(name, layer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void Build()
    {
        panel = new GameObject("AuctionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.10f, 0.98f);

        RuntimeUIKit.MakeBox("Edge", panel.transform, Vector2.zero, new Vector2(PanelWidth, 4f), new Color(0.72f, 0.28f, 0.30f, 1f), false).raycastTarget = false;
        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Salle des ventes — Banque de la Ville", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -16f), new Vector2(PanelWidth - 160f, 30f));
        title.fontStyle = FontStyle.Bold;
        subtitle = RuntimeUIKit.MakeText("Subtitle", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(30f, -50f), new Vector2(PanelWidth - 60f, 18f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(() =>
        {
            if (!running) Close();
        });

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }
}
