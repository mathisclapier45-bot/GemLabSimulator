using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The workshop of the jeweller. First page: pick a stone, then the jewel (ring, pendant, brooch), the metal (silver, gold, platinum) and the setting (prongs,
// bezel, closed back), read what it costs and what it hides, and order it. Second page: the jewels in the making, to collect when they are ready. Third
// page: the jewels the player has, to sell to the jeweller (the treatment is declared or not, as at the markets) or to have taken out of their setting.
// Built at runtime.
public class JewelerUI : MonoBehaviour
{
    public static JewelerUI Instance { get; private set; }

    private const float PanelWidth = 780f;
    private const float PanelHeight = 590f;
    private const float RowHeight = 46f;
    private const int PageSize = 7;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.30f, 0.38f, 1f);
    private static readonly Color GreenColor = new Color(0.25f, 0.50f, 0.30f, 1f);
    private static readonly Color ChosenColor = new Color(0.62f, 0.46f, 0.16f, 1f);
    private static readonly Color RowColor = new Color(0.14f, 0.15f, 0.19f, 1f);

    private enum Tab { Set, Workshop, Jewels }

    private GameObject panel;
    private RectTransform layer;
    private Text subtitle;

    private Tab tab;
    private int page;
    private GemInstance selected;
    private int kind;
    private int metal;
    private int setting;

    public static void TryOpen()
    {
        JewelerState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<JewelerUI>();
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
        var state = JewelerState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (JewelerState.Instance != null) JewelerState.Instance.OnChanged -= RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
        tab = Tab.Set;
        page = 0;
        selected = null;
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

    // ---- Pages ---------------------------------------------------------------------------------------------------

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        subtitle.text = $"Sur vous : {Money:N0} €   ·   la monture cache une partie de la pierre, et de ce qu'on lui a fait";
        if (selected != null && (!JewelCatalog.CanSet(selected) || !InBag(selected))) selected = null;

        var state = JewelerState.Instance;
        var jobs = state != null ? state.Jobs.Count : 0;
        TabButton("Sertir une pierre", Tab.Set, 30f);
        TabButton(jobs > 0 ? $"Atelier ({jobs})" : "Atelier", Tab.Workshop, 190f);
        TabButton("Mes bijoux", Tab.Jewels, 350f);

        switch (tab)
        {
            case Tab.Set: BuildSet(); break;
            case Tab.Workshop: BuildWorkshop(); break;
            default: BuildJewels(); break;
        }
        StardewKit.Skin(layer);
    }

    private static bool InBag(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems) if (ReferenceEquals(g, gem)) return true;
        return false;
    }

    private void TabButton(string label, Tab target, float x)
    {
        var on = tab == target;
        var button = RuntimeUIKit.MakeButton("Tab", layer, label, 13, on ? GreenColor : new Color(0.22f, 0.22f, 0.27f, 1f), new Vector2(x, -84f), new Vector2(150f, 30f));
        button.onClick.AddListener(() =>
        {
            tab = target;
            page = 0;
            Refresh();
        });
    }

    // Choose a stone, then the jewel.
    private void BuildSet()
    {
        var stones = new List<GemInstance>();
        if (PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems)
                if (JewelCatalog.CanSet(gem)) stones.Add(gem);

        Txt("LeftHead", "Pierres taillées à sertir", 14, new Vector2(30f, -126f), new Vector2(330f, 20f), Gold, true);
        if (stones.Count == 0)
        {
            Txt("None", "Aucune pierre à sertir.\n(Une pierre identifiée, taillée, qui est à vous : ni brute, ni de client, ni un don.)", 12,
                new Vector2(30f, -154f), new Vector2(330f, 60f), new Color(0.82f, 0.80f, 0.72f));
            return;
        }

        var pages = Mathf.CeilToInt(stones.Count / (float)PageSize);
        page = Mathf.Clamp(page, 0, pages - 1);
        for (var i = 0; i < PageSize; i++)
        {
            var index = page * PageSize + i;
            if (index >= stones.Count) break;
            var gem = stones[index];
            var y = -152f - i * (RowHeight + 3f);
            var chosen = ReferenceEquals(gem, selected);
            RuntimeUIKit.MakeBox("Row", layer, new Vector2(30f, y + 2f), new Vector2(340f, RowHeight), chosen ? ChosenColor : RowColor, false).raycastTarget = false;
            Txt("Stone", $"{gem.Definition.displayName} — {gem.CaratWeight:F2} ct\n{gem.ShapeName}, {gem.CutStyleName.ToLowerInvariant()}  ·  {gem.FinalValue:N0} €", 12,
                new Vector2(38f, y - 4f), new Vector2(250f, 40f), Color.white);
            var captured = gem;
            RuntimeUIKit.MakeButton("Pick", layer, chosen ? "Choisie" : "Choisir", 12, chosen ? GreenColor : ButtonColor, new Vector2(292f, y - 8f), new Vector2(72f, 30f)).onClick.AddListener(() =>
            {
                selected = captured;
                Refresh();
            });
        }

        if (pages > 1)
        {
            RuntimeUIKit.MakeButton("Prev", layer, "<", 14, ButtonColor, new Vector2(30f, -540f), new Vector2(34f, 28f)).onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
            Txt("Page", $"{page + 1} / {pages}", 12, new Vector2(70f, -543f), new Vector2(70f, 20f), Color.white, false, TextAnchor.UpperCenter);
            RuntimeUIKit.MakeButton("Next", layer, ">", 14, ButtonColor, new Vector2(146f, -540f), new Vector2(34f, 28f)).onClick.AddListener(() => { page = Mathf.Min(pages - 1, page + 1); Refresh(); });
        }

        BuildOptions();
    }

    // The jewel, the metal and the setting of the stone chosen, and what it comes to.
    private void BuildOptions()
    {
        const float x = 400f;
        if (selected == null)
        {
            Txt("Pick", "Choisissez une pierre à gauche.", 14, new Vector2(x, -150f), new Vector2(350f, 24f), new Color(0.82f, 0.80f, 0.72f));
            return;
        }

        Txt("Chosen", $"{selected.Definition.displayName} — {selected.CaratWeight:F2} ct", 15, new Vector2(x, -126f), new Vector2(350f, 22f), Gold, true);

        Txt("KindHead", "Bijou", 12, new Vector2(x, -156f), new Vector2(120f, 16f), new Color(0.78f, 0.82f, 0.92f));
        for (var i = 0; i < JewelCatalog.Kinds.Length; i++) OptionButton(i, JewelCatalog.Kinds[i].Name, kind == i, new Vector2(x + i * 118f, -174f), n => kind = n);

        Txt("MetalHead", "Métal", 12, new Vector2(x, -214f), new Vector2(120f, 16f), new Color(0.78f, 0.82f, 0.92f));
        for (var i = 0; i < JewelCatalog.Metals.Length; i++) OptionButton(i, JewelCatalog.Metals[i].Name, metal == i, new Vector2(x + i * 118f, -232f), n => metal = n);

        Txt("SettingHead", "Serti", 12, new Vector2(x, -272f), new Vector2(120f, 16f), new Color(0.78f, 0.82f, 0.92f));
        for (var i = 0; i < JewelCatalog.Settings.Length; i++) OptionButton(i, JewelCatalog.Settings[i].Name, setting == i, new Vector2(x + i * 118f, -290f), n => setting = n);

        var quote = JewelCatalog.GetQuote(kind, metal, setting, selected);
        if (!quote.IsValid)
        {
            Txt("Refused", quote.Reason, 13, new Vector2(x, -340f), new Vector2(350f, 60f), new Color(1f, 0.78f, 0.62f));
            return;
        }

        var s = JewelCatalog.Settings[setting];
        Txt("Quote", $"Prix : {quote.Fee:N0} €  (dont métal {quote.MetalCost:N0} €)\nDélai : {(quote.Days > 0 ? $"{quote.Days} jour{(quote.Days > 1 ? "s" : "")}" : "le jour même")}\nCache : {quote.Hides}\nRisque pour la pierre : {quote.RiskLabel}", 12,
            new Vector2(x, -336f), new Vector2(350f, 76f), new Color(0.95f, 0.90f, 0.78f));
        Txt("Advice", quote.Advice, 11, new Vector2(x, -418f), new Vector2(350f, 64f), new Color(0.72f, 0.80f, 0.95f));

        var enough = Money >= quote.Fee;
        var order = RuntimeUIKit.MakeButton("Order", layer, enough ? $"Commander — {quote.Fee:N0} €" : "Pas assez d'argent", 15, enough ? GreenColor : new Color(0.25f, 0.25f, 0.28f, 1f),
            new Vector2(x, -500f), new Vector2(350f, 42f));
        order.interactable = enough;
        order.onClick.AddListener(Order);
    }

    private void OptionButton(int index, string label, bool chosen, Vector2 position, System.Action<int> set)
    {
        var button = RuntimeUIKit.MakeButton("Option", layer, label, 12, chosen ? ChosenColor : ButtonColor, position, new Vector2(112f, 30f));
        button.onClick.AddListener(() =>
        {
            set(index);
            Refresh();
        });
    }

    private void Order()
    {
        var state = JewelerState.Instance;
        if (state == null || selected == null) return;
        var name = selected.Definition.displayName;
        if (state.TryStart(selected, kind, metal, setting, out var message))
        {
            Sfx.Play(Sfx.Id.Success);
            ToolFeedbackUI.Instance?.ShowMessage($"{name} confié au bijoutier : {JewelCatalog.Kinds[kind].Name.ToLowerInvariant()} en {JewelCatalog.Metals[metal].Name.ToLowerInvariant()}.");
            selected = null;
        }
        else if (!string.IsNullOrEmpty(message))
        {
            Sfx.Play(Sfx.Id.Error);
            ToolFeedbackUI.Instance?.ShowMessage(message);
        }
        Refresh();
    }

    // The jewels in the making.
    private void BuildWorkshop()
    {
        var state = JewelerState.Instance;
        if (state == null || state.Jobs.Count == 0)
        {
            Txt("None", "Rien en cours à l'atelier. Confiez-lui une pierre dans l'onglet « Sertir une pierre ».", 14, new Vector2(40f, -150f), new Vector2(640f, 44f), new Color(0.82f, 0.80f, 0.72f));
            return;
        }

        for (var i = 0; i < state.Jobs.Count; i++)
        {
            var job = state.Jobs[i];
            var y = -136f - i * (RowHeight + 6f);
            RuntimeUIKit.MakeBox("Row", layer, new Vector2(30f, y + 2f), new Vector2(PanelWidth - 60f, RowHeight + 6f), RowColor, false).raycastTarget = false;
            var ready = state.IsReady(job);
            var status = ready ? "Prêt" : $"prêt dans {state.DaysRemaining(job)} jour{(state.DaysRemaining(job) > 1 ? "s" : "")}";
            Txt("Job", $"{job.Stone.Definition.displayName} — {JewelCatalog.Kinds[job.Kind].Name.ToLowerInvariant()} en {JewelCatalog.Metals[job.Metal].Name.ToLowerInvariant()}, serti {JewelCatalog.Settings[job.Setting].Name.ToLowerInvariant()}\n{status}  ·  payé {job.Fee:N0} €", 12,
                new Vector2(40f, y - 2f), new Vector2(560f, 40f), ready ? new Color(0.62f, 0.95f, 0.66f) : Color.white);
            var captured = job;
            var collect = RuntimeUIKit.MakeButton("Collect", layer, "Récupérer", 13, ready ? GreenColor : new Color(0.25f, 0.25f, 0.28f, 1f), new Vector2(PanelWidth - 160f, y - 4f), new Vector2(110f, 32f));
            collect.interactable = ready;
            collect.onClick.AddListener(() => Collect(captured));
        }
    }

    private void Collect(JewelJob job)
    {
        var state = JewelerState.Instance;
        if (state == null) return;
        if (state.TryCollect(job, out var result, out var message))
        {
            Sfx.Play(result.Outcome == SetOutcome.Fine ? Sfx.Id.Success : Sfx.Id.Error);
            ToolFeedbackUI.Instance?.ShowMessage(result.Message);
        }
        else if (!string.IsNullOrEmpty(message)) ToolFeedbackUI.Instance?.ShowMessage(message);
        Refresh();
    }

    // The jewels of the player.
    private void BuildJewels()
    {
        var jewels = new List<GemInstance>();
        if (PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems)
                if (gem.IsSet) jewels.Add(gem);

        if (jewels.Count == 0)
        {
            Txt("None", "Vous n'avez aucun bijou. Faites sertir une pierre : un bijou se revend mieux qu'une pierre seule, et le sertissage cache une partie de la pierre.", 14,
                new Vector2(40f, -150f), new Vector2(640f, 60f), new Color(0.82f, 0.80f, 0.72f));
            return;
        }

        var pages = Mathf.CeilToInt(jewels.Count / (float)PageSize);
        page = Mathf.Clamp(page, 0, pages - 1);
        var state = JewelerState.Instance;
        for (var i = 0; i < PageSize; i++)
        {
            var index = page * PageSize + i;
            if (index >= jewels.Count) break;
            var gem = jewels[index];
            var y = -130f - i * (RowHeight + 4f);
            RuntimeUIKit.MakeBox("Row", layer, new Vector2(30f, y + 2f), new Vector2(PanelWidth - 60f, RowHeight), RowColor, false).raycastTarget = false;
            var offer = state != null ? state.ListedOffer(gem) : 0;
            var note = gem.IsTreated && gem.IsTreatmentKnown ? " (traitement déclaré)" : "";
            Txt("Jewel", $"{gem.Definition.displayName} — {gem.CaratWeight:F2} ct  ·  {JewelCatalog.Describe(gem)}\nOffre du bijoutier : {offer:N0} €{note}", 12,
                new Vector2(40f, y - 4f), new Vector2(480f, 40f), Color.white);
            var captured = gem;
            RuntimeUIKit.MakeButton("Sell", layer, "Vendre", 12, GreenColor, new Vector2(PanelWidth - 262f, y - 8f), new Vector2(96f, 30f)).onClick.AddListener(() => Sell(captured));
            RuntimeUIKit.MakeButton("Unset", layer, $"Défaire ({JewelCatalog.UnsetFee} €)", 11, ButtonColor, new Vector2(PanelWidth - 160f, y - 8f), new Vector2(110f, 30f)).onClick.AddListener(() => Unset(captured));
        }

        if (pages > 1)
        {
            RuntimeUIKit.MakeButton("Prev", layer, "<", 14, ButtonColor, new Vector2(30f, -548f), new Vector2(34f, 28f)).onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
            Txt("Page", $"{page + 1} / {pages}", 12, new Vector2(70f, -551f), new Vector2(70f, 20f), Color.white, false, TextAnchor.UpperCenter);
            RuntimeUIKit.MakeButton("Next", layer, ">", 14, ButtonColor, new Vector2(146f, -548f), new Vector2(34f, 28f)).onClick.AddListener(() => { page = Mathf.Min(pages - 1, page + 1); Refresh(); });
        }
    }

    private void Unset(GemInstance gem)
    {
        var state = JewelerState.Instance;
        if (state == null) return;
        if (state.TryUnset(gem, out var message)) ToolFeedbackUI.Instance?.ShowMessage($"{gem.Definition.displayName} est sorti de sa monture : la pierre est à nouveau seule.");
        else if (!string.IsNullOrEmpty(message)) ToolFeedbackUI.Instance?.ShowMessage(message);
        Refresh();
    }

    // As at the markets: an untreated stone is simply sold; a treated one the player knows about is declared or not as they choose; one they do not know
    // about goes without a word, and the jeweller may find the treatment.
    private void Sell(GemInstance gem)
    {
        var state = JewelerState.Instance;
        if (state == null) return;

        if (!gem.IsTreated)
        {
            Finish(gem, state.SellDeclared(gem));
            return;
        }
        if (!gem.IsTreatmentKnown)
        {
            SellUndisclosed(gem);
            return;
        }

        var declared = state.Offer(gem, true);
        var hidden = state.Offer(gem, false);
        var risk = TreatmentDisclosure.RiskLabel(state.TestChance(gem));
        ConfirmDialog.ShowChoice("Bijou traité",
            $"{gem.TreatmentGrade}\nDéclarer le traitement : {declared:N0} €\nNe rien dire : {hidden:N0} € (risque de contrôle {risk}, la monture en cache une part)",
            "Déclarer", () => Finish(gem, state.SellDeclared(gem)),
            "Ne rien dire", () => SellUndisclosed(gem));
    }

    private void SellUndisclosed(GemInstance gem)
    {
        var state = JewelerState.Instance;
        if (state == null) return;
        var asked = state.Offer(gem, false);
        var outcome = state.SellUndisclosed(gem);

        if (outcome.Sold)
        {
            Finish(gem, outcome);
            return;
        }
        if (!outcome.FoundTreatment) return;

        if (outcome.Deliberate)
        {
            ToolFeedbackUI.Instance?.ShowMessage($"Le bijoutier a testé la pierre : {gem.TreatmentGrade} non déclaré ! Vente annulée, amende de {outcome.Fine} € et {outcome.ReputationLost * ReputationState.XpPerStone} XP de réputation perdus.");
            Refresh();
            return;
        }

        ConfirmDialog.Show("Traitement trouvé",
            $"Le bijoutier a trouvé un traitement dont vous ignoriez l'existence : {gem.TreatmentGrade}.\nNouvelle offre : {outcome.Price:N0} € (au lieu de {asked:N0} €).",
            "Accepter", () => Finish(gem, state.SellDeclared(gem)));
        Refresh();
    }

    private void Finish(GemInstance gem, SaleOutcome outcome)
    {
        if (outcome.Sold)
        {
            Sfx.Play(Sfx.Id.Coin);
            ToolFeedbackUI.Instance?.ShowMessage($"Vente : {gem.Definition.displayName} ({JewelCatalog.Describe(gem)}) pour {outcome.Price:N0} €.");
        }
        Refresh();
    }

    // ---- Construction ------------------------------------------------------------------------------------------

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
        panel = new GameObject("JewelerPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Bijouterie de la Ville", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -16f), new Vector2(PanelWidth - 160f, 30f));
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
