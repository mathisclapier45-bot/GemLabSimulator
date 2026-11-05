using UnityEngine;
using UnityEngine.UI;

// The window of the banker: open an account, then deposit and withdraw. It shows the rate of the month, the interest it should bring, and the
// rates of the last twelve months. Built at runtime.
public class BankUI : MonoBehaviour
{
    public static BankUI Instance { get; private set; }

    private const float PanelWidth = 660f;
    private const float PanelHeight = 520f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.30f, 0.38f, 1f);
    private static readonly int[] Amounts = { 100, 500, 1000, 5000 };

    private GameObject panel;
    private RectTransform layer;
    private Text subtitle;

    public static void TryOpen()
    {
        BankState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<BankUI>();
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
        var state = BankState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (BankState.Instance != null) BankState.Instance.OnChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void RefreshIfOpen(int _) => RefreshIfOpen();

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

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

    // ---- Content -------------------------------------------------------------------------------------------------

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
        var bank = BankState.Instance;
        if (bank == null) return;

        subtitle.text = $"Aujourd'hui : {GameCalendar.Long(GameCalendar.Today)}";
        if (!bank.HasAccount) BuildOffer();
        else BuildAccount(bank);
        StardewKit.Skin(layer);
    }

    private void BuildOffer()
    {
        var rate = BankState.Instance.CurrentRate;
        Txt("Pitch",
            "« Bienvenue à la Banque de la Ville ! Ouvrez un compte : c'est gratuit, et votre argent y travaille pour vous.\n\n" +
            "Chaque mois, nous annonçons un taux. Il est parfois modeste... et parfois très intéressant : jusqu'à 10 % en un mois !\n\n" +
            "Les intérêts sont versés le 1er du mois suivant, sur le solde le plus bas du mois. Le compte est plafonné à " + BankState.Ceiling.ToString("N0") + " €. »",
            14, new Vector2(30f, -96f), new Vector2(PanelWidth - 60f, 190f), new Color(0.95f, 0.90f, 0.78f));
        Txt("Rate", $"Taux de ce mois : {Percent(rate)}", 20, new Vector2(30f, -304f), new Vector2(PanelWidth - 60f, 28f), Gold, true);

        var open = RuntimeUIKit.MakeButton("Open", layer, "Ouvrir un compte (gratuit)", 17, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(30f, -380f), new Vector2(PanelWidth - 60f, 50f));
        open.onClick.AddListener(() =>
        {
            BankState.Instance.Open();
            Sfx.Play(Sfx.Id.Success);
            ToolFeedbackUI.Instance?.ShowMessage("Compte ouvert à la Banque de la Ville.");
        });
    }

    private void BuildAccount(BankState bank)
    {
        // Left: the balance and the rate.
        Txt("BalanceLabel", "Solde du compte", 13, new Vector2(30f, -92f), new Vector2(280f, 18f), new Color(0.7f, 0.7f, 0.76f));
        Txt("Balance", $"{bank.Balance:N0} €", 34, new Vector2(30f, -112f), new Vector2(300f, 44f), Color.white, true);
        Txt("Ceiling", $"Plafond : {BankState.Ceiling:N0} €   ·   sur vous : {(PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0):N0} €", 12, new Vector2(30f, -158f), new Vector2(330f, 16f), new Color(0.62f, 0.62f, 0.68f));

        var rate = bank.CurrentRate;
        var good = rate >= 0.05f;
        Txt("RateLabel", "Taux de ce mois", 13, new Vector2(30f, -190f), new Vector2(280f, 18f), new Color(0.7f, 0.7f, 0.76f));
        Txt("Rate", Percent(rate), 34, new Vector2(30f, -210f), new Vector2(200f, 44f), good ? new Color(0.50f, 0.95f, 0.55f) : Gold, true);
        if (good) Txt("Good", "Un très bon mois !", 13, new Vector2(190f, -226f), new Vector2(180f, 18f), new Color(0.50f, 0.95f, 0.55f));
        Txt("Projected", $"Intérêts prévus le 1er : environ {bank.ProjectedInterest:N0} €\n(sur le solde le plus bas du mois : {bank.MinBalance:N0} €)", 12, new Vector2(30f, -258f), new Vector2(330f, 32f), new Color(0.82f, 0.80f, 0.72f));
        if (bank.LastRate > 0f)
            Txt("Last", $"Dernier versement : +{bank.LastInterest:N0} € (taux de {Percent(bank.LastRate)})", 12, new Vector2(30f, -298f), new Vector2(330f, 16f), new Color(0.62f, 0.62f, 0.68f));

        // Right: the rates of the last twelve months.
        Txt("ChartTitle", "Taux des 12 derniers mois", 13, new Vector2(390f, -92f), new Vector2(250f, 18f), Gold);
        var current = BankState.CurrentMonthKey;
        for (var i = 0; i < 12; i++)
        {
            var key = current - 11 + i;
            var value = BankState.RateOf(key);
            var height = 8f + value / 0.10f * 120f;
            var x = 390f + i * 21f;
            var isNow = i == 11;
            RuntimeUIKit.MakeBox("Bar", layer, new Vector2(x, -270f + height), new Vector2(17f, height),
                isNow ? new Color(0.95f, 0.75f, 0.30f, 1f) : (value >= 0.05f ? new Color(0.35f, 0.70f, 0.40f, 1f) : new Color(0.42f, 0.36f, 0.22f, 1f)), false).raycastTarget = false;
            var month = ((key % 12) + 12) % 12;
            Txt("M" + i, GameCalendar.MonthNames[month].Substring(0, 1).ToUpperInvariant(), 10, new Vector2(x, -273f), new Vector2(17f, 14f), new Color(0.62f, 0.62f, 0.68f), false, TextAnchor.UpperCenter);
        }
        Txt("Axis", "0 %  –  10 %", 10, new Vector2(390f, -122f), new Vector2(250f, 14f), new Color(0.5f, 0.5f, 0.56f));

        // Deposits and withdrawals.
        Txt("DepositTitle", "Déposer", 14, new Vector2(30f, -346f), new Vector2(200f, 20f), Gold, true);
        for (var i = 0; i < Amounts.Length; i++) AmountButton(i, Amounts[i], true, -372f);
        AmountButton(Amounts.Length, int.MaxValue, true, -372f);
        Txt("WithdrawTitle", "Retirer", 14, new Vector2(30f, -420f), new Vector2(200f, 20f), Gold, true);
        for (var i = 0; i < Amounts.Length; i++) AmountButton(i, Amounts[i], false, -446f);
        AmountButton(Amounts.Length, int.MaxValue, false, -446f);

        Txt("Footnote", "Intérêts versés le 1er de chaque mois, sur le solde le plus bas du mois.", 10, new Vector2(30f, -490f), new Vector2(PanelWidth - 60f, 14f), new Color(0.5f, 0.5f, 0.56f));
    }

    private void AmountButton(int index, int amount, bool deposit, float y)
    {
        var all = amount == int.MaxValue;
        var label = all ? "Tout" : $"{amount:N0} €";
        var button = RuntimeUIKit.MakeButton("Amount", layer, label, 14, deposit ? new Color(0.25f, 0.46f, 0.30f, 1f) : new Color(0.46f, 0.32f, 0.22f, 1f),
            new Vector2(30f + index * 120f, y), new Vector2(112f, 34f));
        button.onClick.AddListener(() => Transfer(amount, deposit));
    }

    private void Transfer(int amount, bool deposit)
    {
        var bank = BankState.Instance;
        if (bank == null) return;
        var wallet = PlayerWallet.Instance;
        if (amount == int.MaxValue) amount = deposit ? (wallet != null ? wallet.Money : 0) : bank.Balance;

        string message;
        var done = deposit ? bank.TryDeposit(amount, out message) : bank.TryWithdraw(amount, out message);
        if (!done && !string.IsNullOrEmpty(message))
        {
            ToolFeedbackUI.Instance?.ShowMessage(message);
            Sfx.Play(Sfx.Id.Error);
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static string Percent(float rate) => $"{rate * 100f:0.0} %".Replace('.', ',');

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
        panel = new GameObject("BankPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(panel.GetComponent<Image>());
        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Banque de la Ville", 24, TextAnchor.UpperLeft, Gold, new Vector2(30f, -18f), new Vector2(PanelWidth - 150f, 32f));
        title.fontStyle = FontStyle.Bold;
        subtitle = RuntimeUIKit.MakeText("Subtitle", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(30f, -52f), new Vector2(PanelWidth - 60f, 18f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -16f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        StardewKit.Skin(panel.transform);
    }
}
