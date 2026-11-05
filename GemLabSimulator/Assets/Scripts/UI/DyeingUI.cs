using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The dye room of the treatments laboratory: six pots of dye, side by side. The player drops a stone in the slot; each pot then shows the
// colour the stone would take in it. Pick a pot, read the quote (duration, price, how much the stone takes, how evenly, the risk) and dip the
// stone; it comes out some days of the game later. Built at runtime.
public class DyeingUI : MonoBehaviour
{
    public static DyeingUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float CardWidth = 186f;
    private const float CardHeight = 196f;
    private const float CardGap = 8f;
    private const float GridX = 202f;
    private const float GridY = -98f;

    private static readonly Color PanelColor = new Color(0.09f, 0.06f, 0.10f, 0.98f);
    private static readonly Color CardColor = new Color(0.16f, 0.11f, 0.17f, 1f);
    private static readonly Color CardSelected = new Color(0.30f, 0.19f, 0.30f, 1f);
    private static readonly Color Accent = new Color(0.96f, 0.72f, 0.88f);
    private static readonly Color Gold = new Color(0.96f, 0.82f, 0.45f);
    private static readonly Color TextSoft = new Color(0.88f, 0.82f, 0.90f);

    private class PotView
    {
        public Image Frame;
        public Image LiquidImage;
        public Image StoneIcon;
        public Image Swatch;
        public Text Status;
        public Image BarFill;
    }

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Button dipButton;
    private Button collectButton;
    private readonly PotView[] pots = new PotView[DyeingCatalog.PotCount];

    private GemInstance stone;
    private int selectedPot = -1;
    private DyeQuote quote;
    private readonly Dictionary<int, Sprite> previews = new Dictionary<int, Sprite>();
    private static Sprite stripeSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        DyeingState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<DyeingUI>();
        }
        Instance?.Open();
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
        var state = DyeingState.Ensure();
        if (state != null) state.OnJobsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (DyeingState.Instance != null) DyeingState.Instance.OnJobsChanged -= RefreshIfOpen;
    }

    private void RefreshIfOpen(int _)
    {
        RefreshIfOpen();
    }

    private void RefreshIfOpen()
    {
        if (IsOpen) Refresh();
    }

    public void Open()
    {
        stone = null;
        selectedPot = -1;
        previews.Clear();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre dans la case : chaque pot vous montrera la couleur qu'elle prendrait. La teinture aime les matières poreuses. »";
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }
        AnimatePots();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = DyeingCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        previews.Clear();
        technicianText.text = DyeingCatalog.IsBleached(gem)
            ? "« Une pierre déjà blanchie : la couleur prendra plus régulièrement. Quel pot ? »"
            : "« Une pierre à teindre. Regardez l'aperçu de chaque pot : plus la matière est poreuse, plus elle boit la couleur. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        previews.Clear();
        Refresh();
    }

    private void SelectPot(int index)
    {
        selectedPot = index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
            if (ReferenceEquals(g, gem)) return true;
        return false;
    }

    private void Refresh()
    {
        // The stone can leave the bag behind our back: forget it then.
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            previews.Clear();
        }

        RefreshSlot();
        RefreshPots();
        RefreshDetail();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nici";
            stoneInfoText.text = "Matériaux qui se teignent :\nperle, corail, jade, agate,\nopale, turquoise, lapis,\nambre...";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n{stone.CaratWeight:F2} ct\nCouleur : {stone.ColorName.ToLowerInvariant()}\n" +
            $"Valeur estimée : {stone.FinalValue} €\n(clic sur la case pour retirer)";
    }

    // The sprite of the stone in the colour a pot would give it.
    private Sprite PreviewOf(int pot)
    {
        if (stone == null) return null;
        if (previews.TryGetValue(pot, out var cached) && cached != null) return cached;

        var q = DyeingCatalog.GetQuote(pot, stone);
        if (!q.IsValid) return null;
        var value = new GemColorValue(Mathf.Repeat(q.PreviewHue, 360f), Mathf.Clamp01(q.PreviewSaturation), Mathf.Clamp01(q.PreviewBrightness));
        var body = GemColorSystem.BodyColor(value);
        var facet = GemColorSystem.FacetColor(value);
        var sprite = stone.IsRough
            ? GemIconFactory.GetRough(stone.Definition, body)
            : GemIconFactory.Get(stone.Definition, stone.ShapeName, stone.CutStyleName, body, facet);
        previews[pot] = sprite;
        return sprite;
    }

    private void RefreshPots()
    {
        var state = DyeingState.Instance;
        for (var i = 0; i < pots.Length; i++)
        {
            var view = pots[i];
            var job = state != null ? state.JobOf(i) : null;
            view.Frame.color = i == selectedPot ? CardSelected : CardColor;
            view.BarFill.transform.parent.gameObject.SetActive(job != null);

            if (job != null)
            {
                view.StoneIcon.gameObject.SetActive(true);
                view.StoneIcon.sprite = job.Stone.DisplaySprite;
                var days = state.DaysRemaining(job);
                var ready = state.IsReady(job);
                view.Status.text = ready ? "<color=#f5c629>Terminé !</color>" : $"{job.Stone.Definition.displayName}\nencore {days} jour{(days > 1 ? "s" : "")}";
                var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
                var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
                var bar = (RectTransform)view.BarFill.transform;
                bar.sizeDelta = new Vector2((CardWidth - 28f) * (ready ? 1f : done), bar.sizeDelta.y);
                view.BarFill.color = ready ? Gold : Accent;
                view.Swatch.gameObject.SetActive(false);
                continue;
            }

            // A free pot shows what the stone in the slot would become in it.
            var preview = PreviewOf(i);
            view.StoneIcon.gameObject.SetActive(preview != null);
            view.Swatch.gameObject.SetActive(false);
            if (preview != null)
            {
                view.StoneIcon.sprite = preview;
                view.Status.text = "<color=#e8c8e0>Aperçu de la couleur</color>";
            }
            else
            {
                view.Status.text = "<color=#b090a8>Libre</color>";
            }
        }
    }

    private void RefreshDetail()
    {
        var state = DyeingState.Instance;
        dipButton.gameObject.SetActive(false);
        collectButton.gameObject.SetActive(false);
        quote = default;

        if (selectedPot < 0)
        {
            quoteText.text = "Choisissez un pot de teinture en cliquant dessus.";
            return;
        }

        var bath = DyeingCatalog.Baths[selectedPot];
        var job = state != null ? state.JobOf(selectedPot) : null;
        if (job != null)
        {
            var ready = state.IsReady(job);
            var days = state.DaysRemaining(job);
            quoteText.text =
                $"<b>{bath.Name}</b>  <color=#e8c8e0>{bath.Chemistry}</color>\n\n" +
                $"{job.Stone.Definition.displayName} ({job.Stone.CaratWeight:F2} ct) trempe dans le pot.\n" +
                (ready ? "La teinture est terminée : vous pouvez sortir la pierre." : $"Elle sortira le {GameCalendar.Short(job.ReadyDay)} (encore {days} jour{(days > 1 ? "s" : "")}).");
            collectButton.gameObject.SetActive(true);
            collectButton.interactable = ready;
            return;
        }

        if (stone == null)
        {
            quoteText.text = $"<b>{bath.Name}</b>  <color=#e8c8e0>{bath.Chemistry}</color>\n\n{bath.Description}\n\nPosez une pierre dans la case pour obtenir un devis.";
            return;
        }

        quote = DyeingCatalog.GetQuote(selectedPot, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var uptakeColor = quote.Uptake < 0.25f ? "#ff9a70" : quote.Uptake < 0.5f ? "#f5c629" : "#a8e0a0";
        var regularColor = quote.Regularity < 0.45f ? "#ff9a70" : quote.Regularity < 0.65f ? "#f5c629" : "#a8e0a0";
        var riskColor = quote.Risk < 0.2f ? "#a8e0a0" : quote.Risk < 0.4f ? "#f5c629" : "#ff7060";
        quoteText.text =
            $"<b>{bath.Name}</b>  <color=#e8c8e0>{bath.Chemistry}</color>\n" +
            $"Durée : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Colorants : {quote.Fee} €\n" +
            $"Prise : <color={uptakeColor}>{quote.UptakeLabel}</color>   Régularité : <color={regularColor}>{quote.RegularityLabel}</color>   Risque de déteindre : <color={riskColor}>{quote.RiskLabel}</color>\n" +
            $"« {quote.Advice} »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        dipButton.gameObject.SetActive(true);
        dipButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ces colorants.";
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void Dip()
    {
        if (stone == null || selectedPot < 0 || !quote.IsValid) return;
        var pot = selectedPot;
        var gem = stone;
        var current = quote;
        var bath = DyeingCatalog.Baths[pot];
        ConfirmDialog.Show("Teindre la pierre",
            $"Plonger {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans le pot « {bath.Name} » ?\n\nDurée : {current.Days} jour{(current.Days > 1 ? "s" : "")}\nColorants : {current.Fee} €\nRisque de déteindre : {current.RiskLabel}\n\nUne pierre teinte ne se reteint pas.",
            "Teindre", () => StartBath(pot, gem, current));
    }

    private void StartBath(int pot, GemInstance gem, DyeQuote current)
    {
        var state = DyeingState.Ensure();
        if (state == null) return;
        if (state.TryStart(pot, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.Flame);
            technicianText.text = $"« La pierre trempe dans le pot. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
            stone = null;
            previews.Clear();
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = DyeingState.Instance;
        var job = state != null && selectedPot >= 0 ? state.JobOf(selectedPot) : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(job, out var result, out var message))
        {
            Sfx.Play(result.IsRuined ? Sfx.Id.Error : Sfx.Id.Success);
            technicianText.text = result.Outcome == DyeOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.IsRuined ? "Pierre tachée par la teinture." : "Pierre sortie du pot, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The stone in each pot drifts gently in its dye.
    private void AnimatePots()
    {
        for (var i = 0; i < pots.Length; i++)
        {
            var view = pots[i];
            if (!view.StoneIcon.gameObject.activeSelf) continue;
            var rect = (RectTransform)view.StoneIcon.transform;
            rect.anchoredPosition = new Vector2(CardWidth / 2f - 32f + Mathf.Sin(Time.unscaledTime * 1.1f + i) * 4f, -(36f + Mathf.Sin(Time.unscaledTime * 1.6f + i * 2f) * 3f));
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    // A strip of the six dye colours, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int segment = 6;
        var w = segment * DyeingCatalog.PotCount;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (var x = 0; x < w; x++)
        {
            var color = DyeingCatalog.Baths[x / segment].Liquid;
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("DyeingPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);

        foreach (var y in new[] { 0f, -(PanelHeight - 10f) })
        {
            var stripe = RuntimeUIKit.MakeBox("Stripe", panel.transform, new Vector2(0f, y), new Vector2(PanelWidth, 10f), Color.white, false);
            stripe.sprite = StripeSprite();
            stripe.type = Image.Type.Tiled;
            stripe.raycastTarget = false;
        }

        Txt("Title", "Bains de teinture", 22, TextAnchor.UpperLeft, Accent, new Vector2(24f, -20f), new Vector2(460f, 30f));
        Txt("Subtitle", "Colorer une matière poreuse ou fissurée, ou lui donner une couleur plus régulière.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à teindre", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.05f, 0.03f, 0.06f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.60f, 0.50f, 0.62f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.95f, 0.90f, 0.96f), new Vector2(24f, -260f), new Vector2(170f, 110f));

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -384f), new Vector2(170f, PanelHeight - 384f - 24f), new Color(0.06f, 0.04f, 0.07f, 1f));
        technicianText = Txt("Technician", "", 12, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 384f - 40f));

        for (var i = 0; i < pots.Length; i++)
            BuildPot(i);

        // The quote and the buttons.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(GridX, -500f), new Vector2(PanelWidth - GridX - 24f, 88f), new Color(0.06f, 0.04f, 0.07f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(GridX + 10f, -506f), new Vector2(PanelWidth - GridX - 220f, 80f));
        quoteText.supportRichText = true;
        dipButton = RuntimeUIKit.MakeButton("Dip", panel.transform, "Teindre la pierre", 14, new Color(0.55f, 0.25f, 0.50f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        dipButton.onClick.AddListener(Dip);
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 14, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        collectButton.onClick.AddListener(Collect);
    }

    private void BuildPot(int index)
    {
        var bath = DyeingCatalog.Baths[index];
        var column = index % 3;
        var row = index / 3;
        var origin = new Vector2(GridX + column * (CardWidth + CardGap), GridY - row * (CardHeight + CardGap));
        var view = new PotView();
        pots[index] = view;

        view.Frame = RuntimeUIKit.MakeBox("Pot" + index, panel.transform, origin, new Vector2(CardWidth, CardHeight), CardColor);
        var captured = index;
        view.Frame.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectPot(captured));
        var card = view.Frame.transform;

        // The jar: a cream ceramic pot full of dye, with a dark rim and a stirring stick leaning on it.
        RuntimeUIKit.MakeBox("Jar", card, new Vector2(16f, -22f), new Vector2(CardWidth - 32f, 84f), new Color(0.83f, 0.78f, 0.70f, 1f)).raycastTarget = false;
        var liquid = RuntimeUIKit.MakeBox("Dye", card, new Vector2(22f, -28f), new Vector2(CardWidth - 44f, 72f), bath.Liquid, false);
        liquid.raycastTarget = false;
        view.LiquidImage = liquid;
        RuntimeUIKit.MakeBox("Sheen", card, new Vector2(22f, -28f), new Vector2(CardWidth - 44f, 4f), Color.Lerp(bath.Liquid, Color.white, 0.5f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Rim", card, new Vector2(10f, -16f), new Vector2(CardWidth - 20f, 8f), new Color(0.62f, 0.56f, 0.48f, 1f)).raycastTarget = false;
        var stick = RuntimeUIKit.MakeBox("Stick", card, new Vector2(CardWidth - 34f, -4f), new Vector2(5f, 40f), new Color(0.58f, 0.40f, 0.24f, 1f), false);
        stick.raycastTarget = false;
        stick.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -16f);

        // The stone in the pot: the preview of its colour, or the stone itself while it soaks.
        var stoneGo = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(card, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), new Vector2(CardWidth / 2f - 32f, -36f), new Vector2(64f, 52f));
        view.StoneIcon = stoneGo.GetComponent<Image>();
        view.StoneIcon.preserveAspect = true;
        view.StoneIcon.raycastTarget = false;
        stoneGo.SetActive(false);

        var swatchGo = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatchGo.transform.SetParent(card, false);
        RuntimeUIKit.SetTopLeft(swatchGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f));
        view.Swatch = swatchGo.GetComponent<Image>();
        view.Swatch.raycastTarget = false;
        swatchGo.SetActive(false);

        // Name plate, chemistry and state.
        RuntimeUIKit.MakeBox("Plate", card, new Vector2(8f, -112f), new Vector2(CardWidth - 16f, 4f), bath.Liquid, false).raycastTarget = false;
        var name = RuntimeUIKit.MakeText("Name", card, bath.Name, 12, TextAnchor.UpperCenter, Color.white, new Vector2(4f, -120f), new Vector2(CardWidth - 8f, 18f));
        name.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Chemistry", card, bath.Chemistry, 10, TextAnchor.UpperCenter, new Color(0.84f, 0.70f, 0.84f), new Vector2(4f, -137f), new Vector2(CardWidth - 8f, 16f));
        view.Status = RuntimeUIKit.MakeText("Status", card, "", 11, TextAnchor.UpperCenter, TextSoft, new Vector2(4f, -155f), new Vector2(CardWidth - 8f, 30f));
        view.Status.supportRichText = true;

        var barBack = RuntimeUIKit.MakeBox("BarBack", card, new Vector2(14f, -CardHeight + 12f), new Vector2(CardWidth - 28f, 5f), new Color(0.05f, 0.03f, 0.06f, 1f), false);
        barBack.raycastTarget = false;
        view.BarFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 5f), Accent, false);
        view.BarFill.raycastTarget = false;
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
