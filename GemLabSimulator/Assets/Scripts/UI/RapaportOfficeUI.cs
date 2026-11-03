using UnityEngine;
using UnityEngine.UI;

// The desk of Martin Rapaport: the monthly subscription to the Rapaport price list. Subscribing (or renewing) adds a month; the list then
// appears in the bag, in its own tab. Built at runtime.
public class RapaportOfficeUI : MonoBehaviour
{
    public static RapaportOfficeUI Instance { get; private set; }

    private const float PanelWidth = 560f;
    private const float PanelHeight = 400f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private Text statusText;
    private Text quoteText;
    private Text subscribeLabel;
    private Button subscribeButton;

    public static void TryOpen()
    {
        RapaportState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<RapaportOfficeUI>();
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
        var state = RapaportState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (RapaportState.Instance != null) RapaportState.Instance.OnChanged -= RefreshIfOpen;
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

    private void Refresh()
    {
        var state = RapaportState.Instance;
        if (state == null) return;

        var today = GameCalendar.Today;
        var latest = RapaportPrices.IssueNumber(RapaportPrices.IssueDate(today));
        if (RapaportState.HasAccess)
        {
            statusText.text = $"Abonnement actif jusqu'au {GameCalendar.Long(state.ExpiryDate)}.\nLa liste est dans votre sac (H), onglet « Rapaport ». Dernier numéro : N° {latest}.";
            subscribeLabel.text = $"Prolonger d'un mois — {RapaportState.MonthlyPrice} €";
        }
        else
        {
            statusText.text = state.WasSubscribed
                ? $"Votre abonnement a pris fin le {GameCalendar.Long(state.ExpiryDate)}. L'onglet a disparu de votre sac."
                : "Vous n'êtes pas abonné. Sans la liste, vous achetez et vendez à l'aveugle.";
            subscribeLabel.text = $"S'abonner pour un mois — {RapaportState.MonthlyPrice} €";
        }

        quoteText.text =
            $"« Chaque jeudi, mon rapport paraît : le prix du carat de tous les diamants ronds, par poids, couleur et pureté. " +
            $"C'est la référence de tout le marché. {RapaportState.MonthlyPrice} € par mois, payables d'avance. »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        subscribeButton.interactable = money >= RapaportState.MonthlyPrice;
        subscribeButton.GetComponent<Image>().color = subscribeButton.interactable ? new Color(0.25f, 0.50f, 0.30f, 1f) : new Color(0.28f, 0.27f, 0.27f, 1f);
    }

    private void Subscribe()
    {
        var state = RapaportState.Ensure();
        if (state == null) return;

        if (state.TrySubscribe(out var message))
        {
            ToolFeedbackUI.Instance?.ShowMessage($"Abonnement au Rapaport jusqu'au {GameCalendar.Long(state.ExpiryDate)}.");
            Sfx.Play(Sfx.Id.Success);
        }
        else if (!string.IsNullOrEmpty(message))
        {
            ToolFeedbackUI.Instance?.ShowMessage(message);
        }
        Refresh();
    }

    private void Build()
    {
        panel = new GameObject("RapaportOfficePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panel.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.07f, 0.98f);

        RuntimeUIKit.MakeBox("Edge", panel.transform, Vector2.zero, new Vector2(PanelWidth, 4f), new Color(0.80f, 0.64f, 0.26f, 1f), false).raycastTarget = false;
        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Rapaport Diamond Report", 24, TextAnchor.UpperLeft, Gold, new Vector2(28f, -20f), new Vector2(PanelWidth - 120f, 32f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Subtitle", panel.transform, "Bureau de Martin Rapaport — abonnement mensuel", 13, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(28f, -54f), new Vector2(PanelWidth - 56f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -18f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        quoteText = RuntimeUIKit.MakeText("Quote", panel.transform, "", 14, TextAnchor.UpperLeft, new Color(0.95f, 0.90f, 0.78f), new Vector2(28f, -92f), new Vector2(PanelWidth - 56f, 96f));
        quoteText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 14, TextAnchor.UpperLeft, Color.white, new Vector2(28f, -204f), new Vector2(PanelWidth - 56f, 80f));
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

        subscribeButton = RuntimeUIKit.MakeButton("Subscribe", panel.transform, "", 16, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(28f, -306f), new Vector2(PanelWidth - 56f, 46f));
        subscribeButton.onClick.AddListener(Subscribe);
        subscribeLabel = subscribeButton.GetComponentInChildren<Text>();
        RuntimeUIKit.MakeText("Hint", panel.transform, "Un nouveau numéro chaque jeudi. Les douze derniers sont consultables.", 11, TextAnchor.UpperCenter,
            new Color(0.55f, 0.52f, 0.46f), new Vector2(0f, -366f), new Vector2(PanelWidth, 16f));
    }
}
