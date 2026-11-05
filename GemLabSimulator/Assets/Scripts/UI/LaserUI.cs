using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The laser station of the treatments laboratory: the treatment of the dark inclusions of a diamond. The player drops a diamond in the slot, and the
// microscope of the station shows its inclusions (the dark ones, the ones the laser can reach, are marked). Pick the method (the classic laser
// that drills a channel, or the KM laser that cracks the inclusion open), how many dark inclusions to treat and the power of the beam, and the
// picture of the stone shows the treated inclusions bleached, with their channel (straight) or their fissures (zig-zag). The treated stone has to be
// graded again. One stone at a time; it comes out some days later, once the acid bath is done. Built at runtime.
public class LaserUI : MonoBehaviour
{
    public static LaserUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float ViewX = 206f;
    private const float ViewY = -92f;
    private const float ViewW = 300f;
    private const float ViewH = 300f;
    private const float ColumnX = 530f;
    private const float SliderWidth = 262f;
    private const float PlotScale = 100f;
    private const int PoolSize = 28;

    private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.09f, 0.98f);
    private static readonly Color Red = new Color(1.00f, 0.32f, 0.30f);
    private static readonly Color Cyan = new Color(0.55f, 0.90f, 1.00f);
    private static readonly Color Gold = new Color(0.98f, 0.86f, 0.50f);
    private static readonly Color TextSoft = new Color(0.86f, 0.90f, 0.96f);
    private static readonly Color ButtonIdle = new Color(0.12f, 0.13f, 0.20f, 1f);
    private static readonly Color ButtonOn = new Color(0.56f, 0.16f, 0.18f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text readoutText;
    private Text powerText;
    private Text treatedHint;
    private Image barFill;
    private Image viewStone;
    private Image beam;
    private Image beamHead;
    private Image beamSpot;
    private Slider slider;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] methodImages = new Image[2];
    private readonly Image[] treatedImages = new Image[LaserCatalog.MaxTreated];
    private readonly Image[] dots = new Image[PoolSize];
    private readonly Image[] segments = new Image[PoolSize];

    private GemInstance stone;
    private LaserMethod method = LaserMethod.Classic;
    private int treated = 1;
    private int power = 55;
    private LaserQuote quote;
    private List<PlotMark> marks = new List<PlotMark>();
    private List<PlotMark> dark = new List<PlotMark>();
    private Vector2 beamTarget;
    private static Sprite stripeSprite;
    private static Sprite dotSprite;
    private static Sprite discSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        LaserState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<LaserUI>();
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
        var state = LaserState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (LaserState.Instance != null) LaserState.Instance.OnJobChanged -= RefreshIfOpen;
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
        method = LaserMethod.Classic;
        treated = 1;
        power = 55;
        slider.SetValueWithoutNotify(power / (float)LaserCatalog.PowerStep);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez un diamant taillé : le microscope montre ses inclusions. Les foncées sont celles que le laser peut atteindre. »";
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
        AnimateView();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = LaserCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        marks = LaserCatalog.AllMarks(gem);
        dark = LaserCatalog.DarkInclusions(gem);
        treated = Mathf.Clamp(treated, 1, Mathf.Min(LaserCatalog.MaxTreated, dark.Count));
        technicianText.text = "« Les points noirs à cerner sont les inclusions foncées. Combien voulez-vous en traiter, et avec quelle méthode ? Le KM est plus violent. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        marks.Clear();
        dark.Clear();
        Refresh();
    }

    private void SelectMethod(int index)
    {
        method = (LaserMethod)index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void SelectTreated(int count)
    {
        if (stone == null || count > dark.Count) return;
        treated = count;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void OnPower(float value)
    {
        power = Mathf.Clamp(Mathf.RoundToInt(value) * LaserCatalog.PowerStep, LaserCatalog.MinPower, LaserCatalog.MaxPower);
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
            marks.Clear();
            dark.Clear();
        }

        RefreshSlot();
        RefreshControls();
        RefreshJob();
        RefreshQuote();
        RefreshView();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nun diamant\nici";
            stoneInfoText.text = "Le laser ne traite que les\ndiamants taillés, avec des\ninclusions foncées.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        var certified = stone.Certificate != null ? $"\n<color=#f5c06a>Certificat n° {stone.Certificate.number}\nsera annulé par le traitement.</color>" : "\nPas encore certifié.";
        stoneInfoText.text =
            $"{stone.CaratWeight:F2} ct — {stone.CutShape ?? stone.Definition.shape}\n{stone.ColorName}  ·  {stone.FinalValue} €\n" +
            $"Inclusions : {marks.Count}, dont <color=#ff7a70>{dark.Count} foncée{(dark.Count > 1 ? "s" : "")}</color>{certified}";
    }

    private void RefreshControls()
    {
        for (var i = 0; i < methodImages.Length; i++) methodImages[i].color = i == (int)method ? ButtonOn : ButtonIdle;
        for (var i = 0; i < treatedImages.Length; i++)
        {
            var available = stone != null && i + 1 <= dark.Count;
            treatedImages[i].color = !available ? new Color(0.08f, 0.08f, 0.10f, 1f) : (i + 1 == treated ? ButtonOn : ButtonIdle);
        }
        treatedHint.text = stone == null ? "" : $"{dark.Count} inclusion{(dark.Count > 1 ? "s" : "")} foncée{(dark.Count > 1 ? "s" : "")} atteignable{(dark.Count > 1 ? "s" : "")}";
        powerText.text = $"Puissance du faisceau : {power} %";
        slider.SetValueWithoutNotify(power / (float)LaserCatalog.PowerStep);
    }

    // ---- The picture of the stone --------------------------------------------------------------------------------------

    private Vector2 PlotToPanel(PlotMark mark)
    {
        var centre = new Vector2(ViewX + ViewW / 2f, ViewY - 150f);
        return centre + new Vector2(mark.X * PlotScale, -mark.Y * PlotScale);
    }

    private void DrawSegment(Image image, Vector2 a, Vector2 b, float thickness, Color color)
    {
        var rect = image.rectTransform;
        var delta = b - a;
        rect.sizeDelta = new Vector2(Mathf.Max(1f, delta.magnitude), thickness);
        rect.anchoredPosition = (a + b) / 2f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        image.color = color;
        image.gameObject.SetActive(true);
    }

    private void DrawDot(Image image, Vector2 position, float size, Color color)
    {
        var rect = image.rectTransform;
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = position;
        image.color = color;
        image.gameObject.SetActive(true);
    }

    // The stone seen from above under the microscope: its inclusions as dots (the dark ones black, the others pale), and the ones that would be
    // treated bleached, with the channel of the classic laser (a short straight line) or the fissures of the KM (a zig-zag to the edge).
    private void RefreshView()
    {
        var job = LaserState.Instance != null ? LaserState.Instance.Job : null;
        var shown = job != null ? job.Stone : stone;
        var shownMethod = job != null ? job.Method : method;
        var shownTreated = job != null ? job.Treated : treated;
        foreach (var dot in dots) dot.gameObject.SetActive(false);
        foreach (var segment in segments) segment.gameObject.SetActive(false);
        beam.gameObject.SetActive(false);
        beamSpot.gameObject.SetActive(false);
        viewStone.gameObject.SetActive(shown != null);
        if (shown == null) return;
        viewStone.sprite = shown.DisplaySprite;

        var plot = job != null ? LaserCatalog.AllMarks(shown) : marks;
        var targets = job != null ? LaserCatalog.DarkInclusions(shown) : dark;
        var used = 0;
        var segmentUsed = 0;
        var targetIndex = 0;
        beamTarget = new Vector2(ViewX + ViewW / 2f, ViewY - 150f);
        foreach (var mark in plot)
        {
            if (used >= dots.Length) break;
            var position = PlotToPanel(mark);
            var isDark = targets.Exists(t => Mathf.Approximately(t.X, mark.X) && Mathf.Approximately(t.Y, mark.Y));
            if (isDark)
            {
                var chosen = targetIndex < shownTreated;
                targetIndex++;
                if (chosen)
                {
                    // Bleached by the acid: white, with a red ring while the laser aims at it.
                    DrawDot(dots[used++], position, 13f, new Color(1f, 0.2f, 0.2f, 0.45f));
                    if (used < dots.Length) DrawDot(dots[used++], position, 7f, new Color(0.96f, 0.96f, 1f, 1f));
                    if (targetIndex == 1) beamTarget = position;
                    if (shownMethod == LaserMethod.Classic)
                    {
                        // A straight channel from the surface, seen from above as a short bright line.
                        if (segmentUsed < segments.Length) DrawSegment(segments[segmentUsed++], position, position + new Vector2(0f, 9f), 1.5f, new Color(0.9f, 0.95f, 1f, 0.9f));
                    }
                    else
                    {
                        // The fissures of the KM: a zig-zag towards the edge of the stone.
                        var edge = new Vector2(Mathf.Sign(mark.X + 0.001f), 0.35f).normalized;
                        var step = edge * 14f;
                        var a = position;
                        for (var k = 0; k < 4 && segmentUsed < segments.Length; k++)
                        {
                            var b = a + step + new Vector2(0f, k % 2 == 0 ? 8f : -8f);
                            DrawSegment(segments[segmentUsed++], a, b, 1.5f, new Color(0.88f, 0.92f, 1f, 0.85f));
                            a = b;
                        }
                    }
                }
                else
                {
                    DrawDot(dots[used++], position, 11f, new Color(0.04f, 0.04f, 0.06f, 0.95f));
                }
            }
            else
            {
                var internalMark = DiamondGradeModel.IsInternal(mark.Kind);
                DrawDot(dots[used++], position, internalMark ? 6f : 4f, internalMark ? new Color(0.85f, 0.85f, 0.9f, 0.55f) : new Color(0.5f, 0.85f, 0.55f, 0.5f));
            }
        }
        beamSpot.rectTransform.anchoredPosition = beamTarget;
    }

    private void RefreshJob()
    {
        var state = LaserState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "Le poste laser est libre.";
            readoutText.text = "Laser en veille  ·  prêt";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        var info = LaserCatalog.Methods[(int)job.Method];
        jobText.text = ready
            ? $"Diamant de {job.Stone.CaratWeight:F2} ct — {info.Name}, {job.Treated} inclusion{(job.Treated > 1 ? "s" : "")} : le bain d'acide est terminé."
            : $"Diamant de {job.Stone.CaratWeight:F2} ct — {info.Name}, {job.Treated} inclusion{(job.Treated > 1 ? "s" : "")} : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        readoutText.text = ready ? "Traitement terminé" : $"Laser, puis bain d'acide  ·  {Mathf.Max(1, days)} j";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - ViewX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Red;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = LaserState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "Le poste laser est occupé : sortez d'abord le diamant en cours.";
            return;
        }
        if (stone == null)
        {
            quoteText.text = $"<b>{LaserCatalog.Methods[(int)method].Name}</b>  <color=#80d8ff>{LaserCatalog.Methods[(int)method].Detail}</color>\n\n{LaserCatalog.Methods[(int)method].Description}\n\nPosez un diamant dans la case pour obtenir un devis.";
            return;
        }

        quote = LaserCatalog.GetQuote(method, power, treated, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var info = LaserCatalog.Methods[(int)method];
        var reactionColor = quote.Reaction < 0.3f ? "#ff9a70" : quote.Reaction < 0.7f ? "#f5c629" : "#a8e0a0";
        var riskColor = quote.Risk < 0.2f ? "#a8e0a0" : quote.Risk < 0.4f ? "#f5c629" : "#ff7060";
        quoteText.text =
            $"<b>{info.Name}</b>  ·  {quote.Treated} inclusion{(quote.Treated > 1 ? "s" : "")}  ·  {power} %\n" +
            $"Durée : {quote.Days} jours (laser puis bain d'acide)  (sortie le {GameCalendar.Short(Today + quote.Days)})    Prix : {quote.Fee} €\n" +
            $"Effet du faisceau : <color={reactionColor}>{quote.ReactionLabel}</color>    Risque d'endommager la pierre : <color={riskColor}>{quote.RiskLabel}</color>\n" +
            $"« {quote.Advice} »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        runButton.gameObject.SetActive(true);
        runButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ce traitement.";
    }

    private void Run()
    {
        if (stone == null || !quote.IsValid) return;
        var gem = stone;
        var current = quote;
        var chosen = method;
        var amount = power;
        var count = quote.Treated;
        var info = LaserCatalog.Methods[(int)chosen];
        var certificate = gem.Certificate != null ? "\nSon certificat sera annulé : il faudra la faire certifier à nouveau." : "";
        ConfirmDialog.Show("Lancer le laser",
            $"Traiter {count} inclusion{(count > 1 ? "s" : "")} de ce diamant ({gem.CaratWeight:F2} ct) ?\n\n{info.Name}, puissance {amount} %\nDurée : {current.Days} jours\nPrix : {current.Fee} €\nRisque : {current.RiskLabel}{certificate}\n\nUn diamant ne se perce qu'une fois.",
            "Lancer", () => StartRun(chosen, amount, count, gem, current));
    }

    private void StartRun(LaserMethod chosen, int amount, int count, GemInstance gem, LaserQuote current)
    {
        var state = LaserState.Ensure();
        if (state == null) return;
        if (state.TryStart(chosen, amount, count, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.Flame);
            technicianText.text = $"« Le diamant est sous le faisceau, puis dans l'acide. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
            stone = null;
            marks.Clear();
            dark.Clear();
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = LaserState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.IsRuined ? Sfx.Id.Error : Sfx.Id.Success);
            var certificate = result.Outcome == LaserOutcome.NoEffect ? "" : "\nÀ faire certifier à nouveau.";
            technicianText.text = result.Outcome == LaserOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »{certificate}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.IsRuined ? "Diamant abîmé par le traitement." : "Diamant sorti du bain, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The red beam of the laser comes down on the first treated inclusion and pulses (fast during a treatment); a sparkle at the point of impact.
    private void AnimateView()
    {
        var state = LaserState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var aiming = stone != null && (state == null || state.Job == null);
        var show = running || aiming;
        beam.gameObject.SetActive(show);
        beamHead.gameObject.SetActive(show);
        beamSpot.gameObject.SetActive(show);
        if (!show) return;

        var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (running ? 14f : 4f));
        var top = new Vector2(beamTarget.x, ViewY - 20f);
        var beamRect = beam.rectTransform;
        beamRect.sizeDelta = new Vector2(2f + 2f * pulse * (running ? 1f : 0.4f), Mathf.Max(1f, top.y - beamTarget.y));
        beamRect.anchoredPosition = new Vector2(beamTarget.x, (top.y + beamTarget.y) / 2f);
        beam.color = new Color(1f, 0.25f, 0.2f, running ? 0.9f : 0.35f + 0.25f * pulse);
        beamHead.rectTransform.anchoredPosition = new Vector2(beamTarget.x, ViewY - 14f);
        var spot = beamSpot.rectTransform;
        spot.sizeDelta = Vector2.one * (14f + 10f * pulse * (running ? 1f : 0.3f));
        spot.anchoredPosition = beamTarget;
        beamSpot.color = new Color(1f, 0.6f, 0.4f, running ? 0.9f : 0.4f);
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static Sprite DotSprite()
    {
        if (dotSprite == null) dotSprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
        return dotSprite;
    }

    private static Sprite DiscSprite()
    {
        if (discSprite == null) discSprite = RuntimeUIKit.MakeRoundedSprite(64, 32);
        return discSprite;
    }

    // Thin lines of red and white, tiled along the top and the bottom of the window: the beam of a laser.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 24;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var red = new Color32(230, 60, 56, 255);
        var dark = new Color32(30, 18, 24, 255);
        var white = new Color32(250, 230, 230, 255);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                texture.SetPixel(x, y, x % 12 < 6 ? (y >= 3 && y <= 6 ? (y == 4 || y == 5 ? white : red) : dark) : dark);
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("LaserPanel", typeof(RectTransform));
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

        Txt("Title", "Poste laser", 22, TextAnchor.UpperLeft, Red, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Rendre moins visibles les inclusions foncées d'un diamant : trou foré ou fissures, puis acide.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the diamond.
        Txt("SlotTitle", "Diamant à traiter", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.02f, 0.02f, 0.05f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.50f, 0.54f, 0.66f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 11, TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 1f), new Vector2(24f, -258f), new Vector2(172f, 124f));
        stoneInfoText.supportRichText = true;

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -386f), new Vector2(170f, PanelHeight - 386f - 24f), new Color(0.03f, 0.03f, 0.06f, 1f));
        technicianText = Txt("Technician", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 386f - 40f));

        BuildView();
        BuildControls();

        // The treatment in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(ViewX, -416f), new Vector2(PanelWidth - ViewX - 24f, 74f), new Color(0.03f, 0.03f, 0.06f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ViewX + 10f, -424f), new Vector2(PanelWidth - ViewX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir le diamant", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(ViewX + 4f, -476f), new Vector2(PanelWidth - ViewX - 24f - 8f, 6f), new Color(0.08f, 0.08f, 0.14f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Red, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(ViewX, -496f), new Vector2(PanelWidth - ViewX - 24f, 92f), new Color(0.03f, 0.03f, 0.06f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ViewX + 10f, -502f), new Vector2(PanelWidth - ViewX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer le laser", 14, new Color(0.62f, 0.16f, 0.16f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The microscope of the station: a round field with the diamond seen from above, its inclusions on it, and the beam of the laser from the head
    // at the top of the field. The readout of the laser is under it.
    private void BuildView()
    {
        var origin = new Vector2(ViewX, ViewY);
        RuntimeUIKit.MakeBox("ViewFrame", panel.transform, origin, new Vector2(ViewW, ViewH), new Color(0.08f, 0.09f, 0.14f, 1f));
        var field = RuntimeUIKit.MakeBox("Field", panel.transform, origin + new Vector2(14f, -14f), new Vector2(ViewW - 28f, 232f), new Color(0.015f, 0.015f, 0.03f, 1f), false);
        field.raycastTarget = false;
        var ring = RuntimeUIKit.MakeBox("FieldRing", panel.transform, origin + new Vector2(30f, -28f), new Vector2(ViewW - 60f, 204f), new Color(0.20f, 0.22f, 0.30f, 1f), false);
        ring.sprite = DiscSprite();
        ring.raycastTarget = false;
        var lens = RuntimeUIKit.MakeBox("Lens", panel.transform, origin + new Vector2(36f, -34f), new Vector2(ViewW - 72f, 192f), new Color(0.10f, 0.11f, 0.17f, 1f), false);
        lens.sprite = DiscSprite();
        lens.raycastTarget = false;

        var stoneGo = new GameObject("ViewStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), origin + new Vector2(ViewW / 2f - 106f, -44f), new Vector2(212f, 212f));
        viewStone = stoneGo.GetComponent<Image>();
        viewStone.preserveAspect = true;
        viewStone.raycastTarget = false;
        stoneGo.SetActive(false);

        for (var i = 0; i < PoolSize; i++)
        {
            dots[i] = MakeOverlay("Mark", DotSprite(), true);
            segments[i] = MakeOverlay("Channel", null, false);
        }

        // The laser head at the top of the field and its beam.
        beam = MakeOverlay("Beam", null, false);
        beam.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        beamSpot = MakeOverlay("BeamSpot", DotSprite(), true);
        beamHead = MakeOverlay("BeamHead", null, false);
        beamHead.rectTransform.sizeDelta = new Vector2(30f, 12f);
        beamHead.color = new Color(0.60f, 0.64f, 0.72f, 1f);

        RuntimeUIKit.MakeBox("Strip", panel.transform, origin + new Vector2(12f, -250f), new Vector2(ViewW - 24f, 38f), new Color(0.16f, 0.18f, 0.24f, 1f)).raycastTarget = false;
        readoutText = Txt("Readout", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(12f, -250f), new Vector2(ViewW - 24f, 38f));
    }

    private Image MakeOverlay(string name, Sprite sprite, bool round)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        go.SetActive(false);
        return image;
    }

    private void BuildControls()
    {
        Txt("MethodTitle", "Méthode", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        for (var i = 0; i < LaserCatalog.Methods.Length; i++)
        {
            var captured = i;
            var info = LaserCatalog.Methods[i];
            var button = RuntimeUIKit.MakeButton("Method" + i, panel.transform, "", 12, ButtonIdle, new Vector2(ColumnX, -100f - i * 56f), new Vector2(262f, 50f));
            button.onClick.AddListener(() => SelectMethod(captured));
            methodImages[i] = button.GetComponent<Image>();

            RuntimeUIKit.MakeBox("Swatch", button.transform, new Vector2(6f, -6f), new Vector2(6f, 38f), i == 0 ? Cyan : Red, false).raycastTarget = false;
            var name = RuntimeUIKit.MakeText("Name", button.transform, info.Name, 13, TextAnchor.UpperLeft, Color.white, new Vector2(20f, -4f), new Vector2(236f, 18f));
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            var detail = RuntimeUIKit.MakeText("Detail", button.transform, info.Detail, 11, TextAnchor.UpperLeft, new Color(0.78f, 0.82f, 0.92f), new Vector2(20f, -23f), new Vector2(236f, 14f));
            detail.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text.Length == 0) label.gameObject.SetActive(false);
        }

        Txt("TreatedTitle", "Inclusions à traiter", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -216f), new Vector2(260f, 18f));
        for (var i = 0; i < LaserCatalog.MaxTreated; i++)
        {
            var captured = i + 1;
            var button = RuntimeUIKit.MakeButton("Treated" + i, panel.transform, i == 0 ? "1" : $"{i + 1}", 13, ButtonIdle, new Vector2(ColumnX + i * 70f, -236f), new Vector2(64f, 28f));
            button.onClick.AddListener(() => SelectTreated(captured));
            treatedImages[i] = button.GetComponent<Image>();
        }
        treatedHint = Txt("TreatedHint", "", 11, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -268f), new Vector2(262f, 16f));

        powerText = Txt("Power", "", 14, TextAnchor.UpperLeft, Red, new Vector2(ColumnX, -296f), new Vector2(262f, 22f));
        powerText.fontStyle = FontStyle.Bold;
        slider = MakeSlider(new Vector2(ColumnX, -320f));
        Txt("PowerMin", $"{LaserCatalog.MinPower} %", 10, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -346f), new Vector2(80f, 14f));
        var max = Txt("PowerMax", $"{LaserCatalog.MaxPower} %", 10, TextAnchor.UpperRight, TextSoft, new Vector2(ColumnX + SliderWidth - 80f, -346f), new Vector2(80f, 14f));
        max.horizontalOverflow = HorizontalWrapMode.Overflow;
        Txt("Rules", "Les trous du laser classique se voient à la loupe 10× par le côté. Les fissures du KM ressemblent aux plumes naturelles.", 11, TextAnchor.UpperLeft,
            new Color(0.70f, 0.76f, 0.88f), new Vector2(ColumnX, -366f), new Vector2(262f, 44f));
    }

    // A slider for the power, in steps of 5 %.
    private Slider MakeSlider(Vector2 position)
    {
        var go = new GameObject("PowerSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, new Vector2(SliderWidth, 26f));

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);
        backgroundRect.anchoredPosition = Vector2.zero;
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = StripeSprite();
        backgroundImage.type = Image.Type.Tiled;
        backgroundImage.color = Color.white;

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0f);
        handleRect.anchorMax = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(18f, 0f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = RuntimeUIKit.Rounded;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.98f, 0.92f, 0.80f, 1f);

        var result = go.GetComponent<Slider>();
        result.handleRect = handleRect;
        result.targetGraphic = handleImage;
        result.direction = Slider.Direction.LeftToRight;
        result.wholeNumbers = true;
        result.minValue = LaserCatalog.MinPower / LaserCatalog.PowerStep;
        result.maxValue = LaserCatalog.MaxPower / LaserCatalog.PowerStep;
        result.value = power / (float)LaserCatalog.PowerStep;
        result.onValueChanged.AddListener(OnPower);
        return result;
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
