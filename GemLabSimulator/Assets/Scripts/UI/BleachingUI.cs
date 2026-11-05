using UnityEngine;
using UnityEngine.UI;

// The bleaching room of the treatments laboratory: six vats of acids and oxidants, side by side. The player drops a stone in the slot, picks a
// vat, reads the quote (duration, price, efficiency, risk) and dips the stone; it comes out some days of the game later. Built at runtime.
public class BleachingUI : MonoBehaviour
{
    public static BleachingUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float CardWidth = 186f;
    private const float CardHeight = 196f;
    private const float CardGap = 8f;
    private const float GridX = 202f;
    private const float GridY = -98f;
    private const int BubblesPerVat = 7;

    private static readonly Color PanelColor = new Color(0.05f, 0.09f, 0.10f, 0.98f);
    private static readonly Color CardColor = new Color(0.09f, 0.15f, 0.16f, 1f);
    private static readonly Color CardSelected = new Color(0.16f, 0.27f, 0.22f, 1f);
    private static readonly Color Acid = new Color(0.58f, 0.92f, 0.45f);
    private static readonly Color Hazard = new Color(0.96f, 0.78f, 0.16f);
    private static readonly Color TextSoft = new Color(0.78f, 0.88f, 0.86f);

    private class VatView
    {
        public Image Frame;
        public RectTransform Liquid;
        public Image LiquidImage;
        public Image StoneIcon;
        public Text Status;
        public Image BarFill;
        public Image[] Bubbles;
        public float[] BubbleSpeed;
        public float[] BubbleOffset;
    }

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Button dipButton;
    private Button collectButton;
    private readonly VatView[] vats = new VatView[BleachingCatalog.VatCount];

    private GemInstance stone;
    private int selectedVat = -1;
    private BleachQuote quote;
    private static Sprite stripeSprite;
    private static Sprite bubbleSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        BleachingState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<BleachingUI>();
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
        var state = BleachingState.Ensure();
        if (state != null) state.OnJobsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (BleachingState.Instance != null) BleachingState.Instance.OnJobsChanged -= RefreshIfOpen;
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
        selectedVat = -1;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre dans la case, puis choisissez un bain. Les acides ne pardonnent pas : lisez bien les conseils. »";
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
        AnimateVats();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = BleachingCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = "« Une pierre à blanchir. Quel bain voulez-vous ? Je vous conseille selon le matériau. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        Refresh();
    }

    private void SelectVat(int index)
    {
        selectedVat = index;
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
        if (stone != null && !ContainsGem(stone)) stone = null;

        RefreshSlot();
        RefreshVats();
        RefreshDetail();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nici";
            stoneInfoText.text = "Matériaux blanchissables :\nperle, corail, jadéite,\nœil-de-tigre.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n{stone.CaratWeight:F2} ct\nCouleur : {stone.ColorName.ToLowerInvariant()}\n" +
            $"Valeur estimée : {stone.FinalValue} €\n(clic sur la case pour retirer)";
    }

    private void RefreshVats()
    {
        var state = BleachingState.Instance;
        for (var i = 0; i < vats.Length; i++)
        {
            var view = vats[i];
            var job = state != null ? state.JobOf(i) : null;
            view.Frame.color = i == selectedVat ? CardSelected : CardColor;
            view.StoneIcon.gameObject.SetActive(job != null);
            view.BarFill.transform.parent.gameObject.SetActive(job != null);

            if (job == null)
            {
                view.Status.text = "<color=#7fb07a>Libre</color>";
                continue;
            }

            view.StoneIcon.sprite = job.Stone.DisplaySprite;
            var days = state.DaysRemaining(job);
            var ready = state.IsReady(job);
            view.Status.text = ready ? "<color=#f5c629>Terminé !</color>" : $"{job.Stone.Definition.displayName}\nencore {days} jour{(days > 1 ? "s" : "")}";
            var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
            var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
            var bar = (RectTransform)view.BarFill.transform;
            bar.sizeDelta = new Vector2((CardWidth - 28f) * (ready ? 1f : done), bar.sizeDelta.y);
            view.BarFill.color = ready ? Hazard : Acid;
        }
    }

    private void RefreshDetail()
    {
        var state = BleachingState.Instance;
        dipButton.gameObject.SetActive(false);
        collectButton.gameObject.SetActive(false);
        quote = default;

        if (selectedVat < 0)
        {
            quoteText.text = "Choisissez un bain en cliquant sur une cuve.";
            return;
        }

        var bath = BleachingCatalog.Baths[selectedVat];
        var job = state != null ? state.JobOf(selectedVat) : null;
        if (job != null)
        {
            var ready = state.IsReady(job);
            var days = state.DaysRemaining(job);
            quoteText.text =
                $"<b>{bath.Name}</b>  <color=#9fd6a0>{bath.Formula}</color>\n\n" +
                $"{job.Stone.Definition.displayName} ({job.Stone.CaratWeight:F2} ct) est dans le bain.\n" +
                (ready ? "Le bain est terminé : vous pouvez sortir la pierre." : $"Elle sortira le {GameCalendar.Short(job.ReadyDay)} (encore {days} jour{(days > 1 ? "s" : "")}).");
            collectButton.gameObject.SetActive(true);
            collectButton.interactable = ready;
            return;
        }

        if (stone == null)
        {
            quoteText.text = $"<b>{bath.Name}</b>  <color=#9fd6a0>{bath.Formula}</color>\n\n{bath.Description}\n\nPosez une pierre dans la case pour obtenir un devis.";
            return;
        }

        quote = BleachingCatalog.GetQuote(selectedVat, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var riskColor = quote.Risk < 0.2f ? "#9fd6a0" : quote.Risk < 0.4f ? "#f5c629" : "#ff7060";
        var effColor = quote.Effectiveness < 0.3f ? "#ff9a70" : quote.Effectiveness < 0.55f ? "#f5c629" : "#9fd6a0";
        quoteText.text =
            $"<b>{bath.Name}</b>  <color=#9fd6a0>{bath.Formula}</color>\n" +
            $"Durée : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Produits : {quote.Fee} €\n" +
            $"Efficacité : <color={effColor}>{quote.EffectivenessLabel}</color>    Risque d'attaque : <color={riskColor}>{quote.RiskLabel}</color>\n" +
            $"« {quote.Advice} »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        dipButton.gameObject.SetActive(true);
        dipButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ces produits.";
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void Dip()
    {
        if (stone == null || selectedVat < 0 || !quote.IsValid) return;
        var vat = selectedVat;
        var gem = stone;
        var current = quote;
        var bath = BleachingCatalog.Baths[vat];
        ConfirmDialog.Show("Plonger la pierre",
            $"Plonger {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans « {bath.Name} » ?\n\nDurée : {current.Days} jour{(current.Days > 1 ? "s" : "")}\nProduits : {current.Fee} €\nRisque d'attaque : {current.RiskLabel}\n\nUn bain trop fort ne se rattrape pas.",
            "Plonger", () => StartBath(vat, gem, current));
    }

    private void StartBath(int vat, GemInstance gem, BleachQuote current)
    {
        var state = BleachingState.Ensure();
        if (state == null) return;
        if (state.TryStart(vat, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.Flame);
            technicianText.text = $"« La pierre est dans la cuve. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
            stone = null;
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = BleachingState.Instance;
        var job = state != null && selectedVat >= 0 ? state.JobOf(selectedVat) : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(job, out var result, out var message))
        {
            Sfx.Play(result.IsRuined ? Sfx.Id.Error : Sfx.Id.Success);
            technicianText.text = result.Outcome == BleachOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.IsRuined ? "Pierre abîmée par le bain." : "Pierre sortie du bain, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // Bubbles rise in each vat, faster and more numerous where a stone is bathing; the stone drifts a little in its liquid.
    private void AnimateVats()
    {
        var state = BleachingState.Instance;
        for (var i = 0; i < vats.Length; i++)
        {
            var view = vats[i];
            var busy = state != null && state.JobOf(i) != null;
            var lively = BleachingCatalog.Baths[i].Bubbles;
            for (var b = 0; b < view.Bubbles.Length; b++)
            {
                var active = b < (busy ? BubblesPerVat : Mathf.Min(BubblesPerVat, 2 + lively / 3));
                var bubble = view.Bubbles[b];
                bubble.enabled = active;
                if (!active) continue;
                view.BubbleOffset[b] += Time.unscaledDeltaTime * view.BubbleSpeed[b] * (busy ? 1.6f : 1f);
                var t = view.BubbleOffset[b] % 1f;
                var rect = (RectTransform)bubble.transform;
                rect.anchoredPosition = new Vector2(8f + ((b * 37 + 11) % 100) / 100f * 140f + Mathf.Sin(t * 9f + b) * 3f, -(72f - t * 64f));
                var c = bubble.color;
                c.a = Mathf.Sin(t * Mathf.PI) * 0.75f;
                bubble.color = c;
            }

            if (view.StoneIcon.gameObject.activeSelf)
            {
                var rect = (RectTransform)view.StoneIcon.transform;
                rect.anchoredPosition = new Vector2(CardWidth / 2f - 30f + Mathf.Sin(Time.unscaledTime * 1.2f + i) * 4f, -(50f + Mathf.Sin(Time.unscaledTime * 1.7f + i * 2f) * 3f));
            }
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 32;
        const int h = 12;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var yellow = new Color32(245, 199, 41, 255);
        var black = new Color32(32, 32, 40, 255);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                texture.SetPixel(x, y, ((x + y) % 16) < 8 ? yellow : black);
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private static Sprite BubbleSprite()
    {
        if (bubbleSprite == null) bubbleSprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
        return bubbleSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("BleachingPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);

        // Hazard stripes at the top and the bottom of the window.
        foreach (var y in new[] { 0f, -(PanelHeight - 10f) })
        {
            var stripe = RuntimeUIKit.MakeBox("Stripe", panel.transform, new Vector2(0f, y), new Vector2(PanelWidth, 10f), Color.white, false);
            stripe.sprite = StripeSprite();
            stripe.type = Image.Type.Tiled;
            stripe.raycastTarget = false;
        }

        Txt("Title", "Bains de blanchiment", 22, TextAnchor.UpperLeft, Acid, new Vector2(24f, -20f), new Vector2(460f, 30f));
        Txt("Subtitle", "Éclaircir ou éliminer la couleur d'une gemme avec des produits chimiques.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à blanchir", 13, TextAnchor.UpperLeft, Hazard, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.03f, 0.05f, 0.06f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.45f, 0.55f, 0.55f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.95f, 0.92f), new Vector2(24f, -260f), new Vector2(170f, 110f));

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -384f), new Vector2(170f, PanelHeight - 384f - 24f), new Color(0.03f, 0.06f, 0.07f, 1f));
        technicianText = Txt("Technician", "", 12, TextAnchor.UpperLeft, new Color(0.95f, 0.92f, 0.78f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 384f - 40f));

        // The six vats.
        for (var i = 0; i < vats.Length; i++)
            BuildVat(i);

        // The quote and the buttons.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(GridX, -500f), new Vector2(PanelWidth - GridX - 24f, 88f), new Color(0.03f, 0.06f, 0.07f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(GridX + 10f, -506f), new Vector2(PanelWidth - GridX - 220f, 80f));
        quoteText.supportRichText = true;
        dipButton = RuntimeUIKit.MakeButton("Dip", panel.transform, "Plonger la pierre", 14, new Color(0.22f, 0.50f, 0.32f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        dipButton.onClick.AddListener(Dip);
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 14, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        collectButton.onClick.AddListener(Collect);
    }

    private void BuildVat(int index)
    {
        var bath = BleachingCatalog.Baths[index];
        var column = index % 3;
        var row = index / 3;
        var origin = new Vector2(GridX + column * (CardWidth + CardGap), GridY - row * (CardHeight + CardGap));
        var view = new VatView();
        vats[index] = view;

        view.Frame = RuntimeUIKit.MakeBox("Vat" + index, panel.transform, origin, new Vector2(CardWidth, CardHeight), CardColor);
        var captured = index;
        view.Frame.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectVat(captured));
        var card = view.Frame.transform;

        // The glass tank: a dark frame, the liquid, a lighter surface and the bubbles that rise in it.
        RuntimeUIKit.MakeBox("Tank", card, new Vector2(12f, -10f), new Vector2(CardWidth - 24f, 96f), new Color(0.55f, 0.72f, 0.78f, 1f), false).raycastTarget = false;
        var liquid = RuntimeUIKit.MakeBox("Liquid", card, new Vector2(14f, -26f), new Vector2(CardWidth - 28f, 78f), bath.Liquid, false);
        liquid.raycastTarget = false;
        view.LiquidImage = liquid;
        view.Liquid = (RectTransform)liquid.transform;
        RuntimeUIKit.MakeBox("Surface", card, new Vector2(14f, -26f), new Vector2(CardWidth - 28f, 3f),
            Color.Lerp(bath.Liquid, Color.white, 0.55f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Rim", card, new Vector2(8f, -6f), new Vector2(CardWidth - 16f, 6f), new Color(0.45f, 0.5f, 0.58f, 1f), false).raycastTarget = false;

        var bubbleRoot = new GameObject("Bubbles", typeof(RectTransform));
        bubbleRoot.transform.SetParent(card, false);
        RuntimeUIKit.SetTopLeft(bubbleRoot.GetComponent<RectTransform>(), new Vector2(14f, -26f), new Vector2(CardWidth - 28f, 78f));
        view.Bubbles = new Image[BubblesPerVat];
        view.BubbleSpeed = new float[BubblesPerVat];
        view.BubbleOffset = new float[BubblesPerVat];
        var lighter = Color.Lerp(bath.Liquid, Color.white, 0.75f);
        for (var b = 0; b < BubblesPerVat; b++)
        {
            var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bubbleRoot.transform, false);
            RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), Vector2.zero, new Vector2(3f + b % 3, 3f + b % 3));
            var image = go.GetComponent<Image>();
            image.sprite = BubbleSprite();
            image.color = lighter;
            image.raycastTarget = false;
            view.Bubbles[b] = image;
            view.BubbleSpeed[b] = 0.18f + 0.07f * ((b * 5) % 7);
            view.BubbleOffset[b] = (b * 0.37f) % 1f;
        }

        // The stone bathing in the vat.
        var stoneGo = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(card, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), new Vector2(CardWidth / 2f - 30f, -50f), new Vector2(60f, 46f));
        view.StoneIcon = stoneGo.GetComponent<Image>();
        view.StoneIcon.preserveAspect = true;
        view.StoneIcon.raycastTarget = false;
        stoneGo.SetActive(false);

        // Name plate, formula and state.
        var plate = RuntimeUIKit.MakeBox("Plate", card, new Vector2(8f, -112f), new Vector2(CardWidth - 16f, 4f), bath.Liquid, false);
        plate.raycastTarget = false;
        var name = RuntimeUIKit.MakeText("Name", card, bath.Name, 12, TextAnchor.UpperCenter, Color.white, new Vector2(4f, -120f), new Vector2(CardWidth - 8f, 18f));
        name.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeText("Formula", card, bath.Formula, 11, TextAnchor.UpperCenter, new Color(0.62f, 0.84f, 0.65f), new Vector2(4f, -137f), new Vector2(CardWidth - 8f, 16f));
        view.Status = RuntimeUIKit.MakeText("Status", card, "", 11, TextAnchor.UpperCenter, TextSoft, new Vector2(4f, -155f), new Vector2(CardWidth - 8f, 30f));
        view.Status.supportRichText = true;

        var barBack = RuntimeUIKit.MakeBox("BarBack", card, new Vector2(14f, -CardHeight + 12f), new Vector2(CardWidth - 28f, 5f), new Color(0.03f, 0.05f, 0.06f, 1f), false);
        barBack.raycastTarget = false;
        view.BarFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 5f), Acid, false);
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
