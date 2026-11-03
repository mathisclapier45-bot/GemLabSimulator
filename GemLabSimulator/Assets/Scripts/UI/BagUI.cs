using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The player's bag (H): the character in a frame with slots for clothes (hat, top, trousers, shoes), three badges for
// the diplomas, a pet slot, and 18 slots for field gemology equipment (loupe, maglite, tweezers...). Equipment bought
// at the GemStore lands in the field slots; the other slots are empty placeholders for now. Built at runtime and added to the scene's Canvas
// automatically, so no scene regeneration is needed.
public class BagUI : MonoBehaviour
{
    public static BagUI Instance { get; private set; }

    private static int escapeFrame = -1;
    public static bool ConsumesEscape => escapeFrame == Time.frameCount;

    private const float PanelWidth = 800f;
    private const float PanelHeight = 520f;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 8f;

    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color SlotColor = new Color(0.20f, 0.17f, 0.15f, 1f);
    private static readonly Color SlotHint = new Color(0.55f, 0.50f, 0.45f, 0.8f);

    private GameObject panel;
    private GameObject bagContent;
    private RectTransform panelRect;
    private CalendarTabUI calendar;
    private RapaportTabUI rapaport;

    // The tabs of the panel: the bag, the calendar, the grimoire of the gems, the card binder and, for a subscriber, the Rapaport price list.
    private class BagTab
    {
        public string Label;
        public KeyCode Key;
        public GameObject Content;       // null for the grimoire, whose windows are shown by its own code
        public Button Button;
        public Image Image;
        public float Width;
        public float Height;
        public float PanelWidth;
        public float OffsetX;
        public Action OnShow;
        public Action OnHide;
        public Func<bool> Visible;
    }

    private readonly List<BagTab> tabs = new List<BagTab>();
    private int activeTab = -1;
    private bool rapaportShown;
    private GameObject returnButton;
    private readonly Image[] equipmentIcons = new Image[PlayerOwnedTools.BagSlotCount];
    private Text infoText;
    private Image equippedPetIcon;
    private Image portraitImage;
    private RectTransform portraitRect;
    private Image shoeIcon;
    private readonly Image[] collectionIcons = new Image[PlayerPets.CollectionSlots];

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
        if (canvas != null && canvas.GetComponent<BagUI>() == null) canvas.AddComponent<BagUI>();
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
        BuildReturnButton();
    }

    // "Retour au sac": shown at the top of the screen while a tool opened from the bag is on screen.
    private void BuildReturnButton()
    {
        var button = RuntimeUIKit.MakeButton("ReturnToBag", transform, "← Retour au sac", 15, new Color(0.30f, 0.38f, 0.58f, 1f),
            Vector2.zero, new Vector2(190f, 36f));
        returnButton = button.gameObject;
        var rect = returnButton.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(-150f, -14f);
        button.onClick.AddListener(ReturnToBag);
        returnButton.SetActive(false);
    }

    private void ReturnToBag()
    {
        BagToolLauncher.CloseTool();
        returnButton.SetActive(false);
        panel.SetActive(true);
        SelectTab(0);
    }

    private void Start()
    {
        var tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += RefreshEquipment;
        RefreshEquipment();

        var pets = PlayerPets.Ensure();
        if (pets != null) pets.OnPetsChanged += RefreshPets;
        RefreshPets();
        AdoptBinder();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshEquipment;
        if (PlayerPets.Instance != null) PlayerPets.Instance.OnPetsChanged -= RefreshPets;
    }

    // The character in the frame wears what it has on: the sneakers on its feet, or it sits on its bicycle.
    private void RefreshLook()
    {
        if (portraitImage == null) return;
        var tools = PlayerOwnedTools.Instance;
        var worn = tools != null ? tools.Mobility : "";
        const float frameX = 510f;
        const float frameY = -58f;

        var riding = worn == PlayerOwnedTools.BikeId ? Resources.Load<Sprite>("PlayerIdleBike/Right") : null;
        var sprite = riding != null ? riding
            : worn == PlayerOwnedTools.SneakersId ? Resources.Load<Sprite>("PlayerIdleBaskets/Down") ?? Resources.Load<Sprite>("PlayerIdle/Down")
            : Resources.Load<Sprite>("PlayerIdle/Down");
        portraitImage.sprite = sprite;
        portraitImage.enabled = sprite != null;

        // The bicycle is wide: it is drawn smaller than the standing character, on the same ground line.
        if (riding != null) RuntimeUIKit.SetTopLeft(portraitRect, new Vector2(frameX + 4f, frameY - 40f - 280f + 166f), new Vector2(166f, 166f));
        else RuntimeUIKit.SetTopLeft(portraitRect, new Vector2(frameX + 14f, frameY - 40f), new Vector2(140f, 280f));

        if (shoeIcon == null) return;
        var shoes = worn == PlayerOwnedTools.SneakersId ? Resources.Load<Sprite>("BagIcons/" + PlayerOwnedTools.SneakersId) : null;
        shoeIcon.sprite = shoes;
        shoeIcon.enabled = shoes != null;
    }

    private void TakeOffSneakers()
    {
        var tools = PlayerOwnedTools.Instance;
        if (tools == null || tools.Mobility != PlayerOwnedTools.SneakersId) return;
        tools.SetMobility("");
        Sfx.Play(Sfx.Id.Pickup);
        HideInfo();
    }

    private void ShowShoeInfo()
    {
        var tools = PlayerOwnedTools.Instance;
        infoText.text = tools != null && tools.Mobility == PlayerOwnedTools.SneakersId
            ? $"{BagItemCatalog.NameOf(PlayerOwnedTools.SneakersId)}\n\n{BagItemCatalog.DescriptionOf(PlayerOwnedTools.SneakersId)}\n\nChaussées — clic : retirer"
            : "Chaussures\n\nAucune paire n'est chaussée. Cliquez sur des baskets dans l'équipement de terrain pour les enfiler.";
    }

    private void RefreshEquipment()
    {
        RefreshLook();
        var tools = PlayerOwnedTools.Instance;
        for (var i = 0; i < equipmentIcons.Length; i++)
        {
            var id = tools != null && i < tools.BagSlots.Count ? tools.BagSlots[i] : null;
            var sprite = string.IsNullOrEmpty(id) ? null : Resources.Load<Sprite>($"BagIcons/{id}");
            equipmentIcons[i].sprite = sprite;
            equipmentIcons[i].enabled = sprite != null;
        }
    }

    private void ShowInfo(int slotIndex)
    {
        var tools = PlayerOwnedTools.Instance;
        var id = tools != null && slotIndex < tools.BagSlots.Count ? tools.BagSlots[slotIndex] : null;
        infoText.text = string.IsNullOrEmpty(id)
            ? ""
            : PlayerOwnedTools.IsMobilityItem(id)
                ? $"{BagItemCatalog.NameOf(id)}\n\n{BagItemCatalog.DescriptionOf(id)}\n\n{(tools.Mobility == id ? "Utilisée" : "Rangée")} — clic : {(tools.Mobility == id ? "ranger" : "utiliser")}"
            : id == "Lanterne"
                ? $"{BagItemCatalog.NameOf(id)}\n\n{BagItemCatalog.DescriptionOf(id)}\n\n{(tools.LanternLit ? "Allumée" : "Éteinte")} — clic : {(tools.LanternLit ? "éteindre" : "allumer")}"
                : $"{BagItemCatalog.NameOf(id)}\n\n{BagItemCatalog.DescriptionOf(id)}\n\nClic : utiliser";
    }

    // Click on a field-equipment slot: use the item (its tool window opens, the bag closes).
    private void UseSlot(int slotIndex)
    {
        var tools = PlayerOwnedTools.Instance;
        var id = tools != null && slotIndex < tools.BagSlots.Count ? tools.BagSlots[slotIndex] : null;
        if (string.IsNullOrEmpty(id)) return;

        if (BagToolLauncher.Use(id)) Close();
    }

    // The equipped animal and the ones waiting in the bag.
    private void RefreshPets()
    {
        var pets = PlayerPets.Instance;
        if (equippedPetIcon == null) return;

        var equippedSprite = pets != null && !string.IsNullOrEmpty(pets.Equipped) ? PetCatalog.IconOf(pets.Equipped) : null;
        equippedPetIcon.sprite = equippedSprite;
        equippedPetIcon.enabled = equippedSprite != null;

        var collection = pets != null ? pets.Collection : new System.Collections.Generic.List<string>();
        for (var i = 0; i < collectionIcons.Length; i++)
        {
            var sprite = i < collection.Count ? PetCatalog.IconOf(collection[i]) : null;
            collectionIcons[i].sprite = sprite;
            collectionIcons[i].enabled = sprite != null;
        }
    }

    private string CollectionPetAt(int index)
    {
        var pets = PlayerPets.Instance;
        if (pets == null) return null;
        var collection = pets.Collection;
        return index < collection.Count ? collection[index] : null;
    }

    private void ShowPetInfo(string petId, bool equipped)
    {
        if (string.IsNullOrEmpty(petId)) return;
        infoText.text = $"{PetCatalog.NameOf(petId)}\n\n{PetCatalog.DescriptionOf(petId)}\n\n" +
                        (equipped ? "Clic : ranger dans le sac" : "Clic : équiper (il vous suivra partout)");
    }

    private void UseCollectionSlot(int index)
    {
        var id = CollectionPetAt(index);
        if (!string.IsNullOrEmpty(id)) PlayerPets.Instance?.Equip(id);
    }

    private void HideInfo()
    {
        infoText.text = "";
    }

    private void Update()
    {
        var toolOpen = BagToolLauncher.ToolOpenFromBag;
        if (toolOpen != returnButton.activeSelf)
        {
            returnButton.SetActive(toolOpen);
            if (toolOpen) returnButton.transform.SetAsLastSibling();
        }

        // The Rapaport tab exists only while the subscription lasts.
        var access = RapaportState.HasAccess;
        if (access != rapaportShown)
        {
            rapaportShown = access;
            LayoutTabs();
            if (!access && activeTab >= 0 && !TabVisible(tabs[activeTab])) SelectTab(0);
        }

        // H opens the bag, K the calendar, G the grimoire: the same key closes the panel again.
        var keyed = false;
        for (var i = 0; i < tabs.Count; i++)
        {
            if (!Input.GetKeyDown(tabs[i].Key) || !TabVisible(tabs[i])) continue;
            if (tabs[i].Key == KeyCode.B && BoosterUI.IsShowing) continue;
            keyed = true;
            if (panel.activeSelf && activeTab == i) ClosePanel();
            else
            {
                panel.SetActive(true);
                SelectTab(i);
            }
            break;
        }
        if (!keyed && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            // A large card of the binder closes first.
            var binder = BinderUI.Instance;
            if (activeTab >= 0 && tabs[activeTab].Key == KeyCode.B && binder != null && binder.HasDetail)
            {
                escapeFrame = Time.frameCount;
                binder.CloseDetailInBag();
            }
            else ClosePanel();
        }
    }

    // Opens the bag on the tab whose label starts with this text.
    public void ShowTab(string labelStart)
    {
        for (var i = 0; i < tabs.Count; i++)
        {
            if (!tabs[i].Label.StartsWith(labelStart) || !TabVisible(tabs[i])) continue;
            panel.SetActive(true);
            SelectTab(i);
            return;
        }
    }

    private void ClosePanel()
    {
        if (activeTab >= 0 && activeTab < tabs.Count) tabs[activeTab].OnHide?.Invoke();
        if (panel != null) panel.SetActive(false);
    }

    public void Close()
    {
        ClosePanel();
    }

    private void BuildPanel()
    {
        panel = new GameObject("BagPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable.
        rect.anchoredPosition = new Vector2(-190f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        RuntimeUIKit.MakeText("Title", panel.transform, "Sac", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(200f, 30f));
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "X", 14, new Color(0.30f, 0.22f, 0.20f, 1f),
            new Vector2(PanelWidth - 56f, -12f), new Vector2(32f, 28f));
        close.onClick.AddListener(Close);

        BuildFieldEquipment();
        BuildPortraitFrame();
        BuildBadges();
        BuildPetSlot();
        BuildTabs();
    }

    // The tabs at the top. What the bag panel holds goes into one container, shown or hidden by the tabs.
    private void BuildTabs()
    {
        panelRect = panel.GetComponent<RectTransform>();
        var keep = new[] { "Title", "Close" };
        bagContent = new GameObject("BagContent", typeof(RectTransform));
        bagContent.transform.SetParent(panel.transform, false);
        var contentRect = bagContent.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        for (var i = panel.transform.childCount - 1; i >= 0; i--)
        {
            var child = panel.transform.GetChild(i);
            if (child.gameObject == bagContent || Array.IndexOf(keep, child.name) >= 0) continue;
            child.SetParent(bagContent.transform, false);
        }

        calendar = new CalendarTabUI(panel.transform);
        rapaport = new RapaportTabUI(panel.transform);

        AddTab("Sac", KeyCode.H, bagContent, 90f, PanelHeight, null, null, null);
        AddTab("Calendrier (K)", KeyCode.K, calendar.Root, 130f, PanelHeight, calendar.Show, null, null);
        if (AdoptGrimoire())
            AddTab("Grimoire (G)", KeyCode.G, null, 120f, 624f, () => GrimoireUI.Instance.OpenInBag(), () => GrimoireUI.Instance.Close(), null);
        AddTab("Rapaport", KeyCode.None, rapaport.Root, 110f, PanelHeight, rapaport.Show, null, () => RapaportState.HasAccess);
        LayoutTabs();
        SelectTab(0);
    }

    private void AddTab(string label, KeyCode key, GameObject content, float width, float height, Action onShow, Action onHide, Func<bool> visible,
        float panelWidth = PanelWidth, float offsetX = -190f, int insertAt = -1)
    {
        var button = RuntimeUIKit.MakeButton("Tab" + label, panel.transform, label, 14, SlotColor, Vector2.zero, new Vector2(width, 30f));
        var tab = new BagTab
        {
            Label = label, Key = key, Content = content, Button = button, Image = button.GetComponent<Image>(), Width = width, Height = height,
            PanelWidth = panelWidth, OffsetX = offsetX, OnShow = onShow, OnHide = onHide, Visible = visible,
        };
        button.onClick.AddListener(() => SelectTab(tabs.IndexOf(tab)));
        if (insertAt >= 0 && insertAt <= tabs.Count) tabs.Insert(insertAt, tab);
        else tabs.Add(tab);
    }

    // The card binder moves into the bag as a tab, just before the Rapaport one. It is adopted in Start, once every interface exists.
    private void AdoptBinder()
    {
        var binder = BinderUI.Instance;
        if (binder == null || binder.Panel == null) return;

        binder.AttachToBag(panel.transform);
        var before = tabs.FindIndex(t => t.Label.StartsWith("Rapaport"));
        AddTab("Classeur (B)", KeyCode.B, binder.Panel, 120f, 690f, binder.ShowInBag, binder.HideInBag, null, 900f, -150f, before);
        LayoutTabs();
    }

    private static bool TabVisible(BagTab tab) => tab.Visible == null || tab.Visible();

    private void LayoutTabs()
    {
        var x = 120f;
        foreach (var tab in tabs)
        {
            var visible = TabVisible(tab);
            tab.Button.gameObject.SetActive(visible);
            if (!visible) continue;
            RuntimeUIKit.SetTopLeft(tab.Button.GetComponent<RectTransform>(), new Vector2(x, -12f), new Vector2(tab.Width, 30f));
            x += tab.Width + 6f;
        }
    }

    // The grimoire of the gems moves into the bag: its two windows become children of the panel, below the tabs.
    private bool AdoptGrimoire()
    {
        var grimoire = GrimoireUI.Instance;
        if (grimoire == null || grimoire.GridPanel == null) return false;

        foreach (var window in new[] { grimoire.GridPanel, grimoire.DetailPanel })
        {
            if (window == null) continue;
            window.transform.SetParent(panel.transform, false);
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -50f);
            rect.sizeDelta = new Vector2(700f, 560f);
            var background = window.GetComponent<Image>();
            if (background != null) background.color = Color.clear;
        }
        // The grid has no close button of its own any more: the tab and the X of the bag do that.
        var closeButton = grimoire.GridPanel.transform.Find("CloseButton");
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        return true;
    }

    private void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        if (activeTab >= 0 && activeTab < tabs.Count) tabs[activeTab].OnHide?.Invoke();
        foreach (var tab in tabs)
            if (tab.Content != null) tab.Content.SetActive(false);

        activeTab = index;
        var current = tabs[index];
        if (current.Content != null) current.Content.SetActive(true);
        panelRect.sizeDelta = new Vector2(current.PanelWidth, current.Height);
        panelRect.anchoredPosition = new Vector2(current.OffsetX, 0f);
        var closeButton = panel.transform.Find("Close");
        if (closeButton != null)
            RuntimeUIKit.SetTopLeft(closeButton.GetComponent<RectTransform>(), new Vector2(current.PanelWidth - 56f, -12f), new Vector2(32f, 28f));
        for (var i = 0; i < tabs.Count; i++)
            tabs[i].Image.color = i == index ? new Color(0.45f, 0.35f, 0.22f, 1f) : SlotColor;
        current.OnShow?.Invoke();
    }

    // Left: 18 slots (6 columns x 3 rows) for the field equipment.
    private void BuildFieldEquipment()
    {
        RuntimeUIKit.MakeText("EquipLabel", panel.transform, "Équipement de terrain", 13, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -58f), new Vector2(200f, 20f));
        for (var i = 0; i < PlayerOwnedTools.BagSlotCount; i++)
        {
            var column = i % 6;
            var row = i / 6;
            var slot = MakeSlot($"Terrain_{i}", new Vector2(24f + column * (SlotSize + SlotSpacing), -84f - row * (SlotSize + SlotSpacing)), "");

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slot.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            var icon = iconGO.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            equipmentIcons[i] = icon;

            var captured = i;
            slot.gameObject.AddComponent<Button>().onClick.AddListener(() => UseSlot(captured));
            var hover = slot.gameObject.AddComponent<BagSlotHover>();
            hover.OnEnter = () => ShowInfo(captured);
            hover.OnExit = HideInfo;
        }

        RuntimeUIKit.MakeBox("InfoBack", panel.transform, new Vector2(24f, -292f), new Vector2(376f, 190f), new Color(0.07f, 0.06f, 0.05f, 1f));
        infoText = RuntimeUIKit.MakeText("Info", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.80f),
            new Vector2(32f, -298f), new Vector2(360f, 178f));
    }

    // Middle: the character in a frame, with the four clothing slots running from top to bottom beside it.
    private void BuildPortraitFrame()
    {
        const float frameX = 510f;
        const float frameY = -58f;
        const float frameW = 250f;
        const float frameH = 330f;

        RuntimeUIKit.MakeBox("FrameBorder", panel.transform, new Vector2(frameX - 3f, frameY + 3f), new Vector2(frameW + 6f, frameH + 6f),
            new Color(0.55f, 0.43f, 0.28f, 1f));
        RuntimeUIKit.MakeBox("Frame", panel.transform, new Vector2(frameX, frameY), new Vector2(frameW, frameH), new Color(0.13f, 0.16f, 0.22f, 1f));

        // Soft floor glow under the character.
        var glow = RuntimeUIKit.MakeBox("Glow", panel.transform, new Vector2(frameX + 8f, frameY - 250f), new Vector2(150f, 50f),
            new Color(0.5f, 0.6f, 0.8f, 0.22f), false);
        glow.sprite = RuntimeUIKit.Glow;

        var sprite = Resources.Load<Sprite>("PlayerIdle/Down");
        var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portrait.transform.SetParent(panel.transform, false);
        portraitRect = portrait.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(portraitRect, new Vector2(frameX + 14f, frameY - 40f), new Vector2(140f, 280f));
        portraitImage = portrait.GetComponent<Image>();
        portraitImage.sprite = sprite;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portraitImage.enabled = sprite != null;

        var labels = new[] { "Couvre-chef", "Haut", "Pantalon", "Chaussures" };
        var hints = new[] { "Tête", "Haut", "Bas", "Pieds" };
        var spacing = (frameH - 4 * SlotSize) / 5f;
        for (var i = 0; i < 4; i++)
        {
            var slotY = frameY - spacing - i * (SlotSize + spacing);
            var clothesSlot = MakeSlot($"Habit_{labels[i]}", new Vector2(frameX + frameW - SlotSize - 14f, slotY), hints[i]);

            // The shoes slot holds the sneakers worn by the character.
            if (i != 3) continue;
            shoeIcon = AddIcon(clothesSlot);
            clothesSlot.gameObject.AddComponent<Button>().onClick.AddListener(TakeOffSneakers);
            var shoeHover = clothesSlot.gameObject.AddComponent<BagSlotHover>();
            shoeHover.OnEnter = ShowShoeInfo;
            shoeHover.OnExit = HideInfo;
        }
    }

    // Below the frame: three badges for the diplomas.
    private void BuildBadges()
    {
        const float frameX = 510f;
        const float frameW = 250f;
        const float y = -58f - 330f - 40f;

        RuntimeUIKit.MakeText("BadgeLabel", panel.transform, "Insignes (diplômes)", 13, TextAnchor.UpperLeft, Gold,
            new Vector2(frameX, y + 22f), new Vector2(frameW, 20f));
        var total = 3 * SlotSize + 2 * 16f;
        var startX = frameX + (frameW - total) / 2f;
        for (var i = 0; i < 3; i++)
            MakeSlot($"Insigne_{i}", new Vector2(startX + i * (SlotSize + 16f), y - 4f), "Insigne");
    }

    // Left of the frame, at the character's height: the equipped animal, and under it the animals waiting in the bag.
    private void BuildPetSlot()
    {
        const float x = 424f;
        RuntimeUIKit.MakeText("PetLabel", panel.transform, "Animal", 13, TextAnchor.UpperLeft, Gold, new Vector2(x, -178f), new Vector2(80f, 20f));
        var equipped = MakeSlot("Animal", new Vector2(x, -204f), "Animal");
        equippedPetIcon = AddIcon(equipped);
        equipped.gameObject.AddComponent<Button>().onClick.AddListener(() => PlayerPets.Instance?.Unequip());
        var equippedHover = equipped.gameObject.AddComponent<BagSlotHover>();
        equippedHover.OnEnter = () => ShowPetInfo(PlayerPets.Instance != null ? PlayerPets.Instance.Equipped : null, true);
        equippedHover.OnExit = HideInfo;

        RuntimeUIKit.MakeText("PetCollectionLabel", panel.transform, "Animaux", 13, TextAnchor.UpperLeft, Gold, new Vector2(x, -268f), new Vector2(80f, 20f));
        for (var i = 0; i < collectionIcons.Length; i++)
        {
            var captured = i;
            // Two columns of small slots: up to ten animals wait in the bag.
            var slot = RuntimeUIKit.MakeBox($"AnimalSac_{i}", panel.transform, new Vector2(x + 2f + (i % 2) * 42f, -292f - (i / 2) * 42f), new Vector2(38f, 38f), SlotColor);
            collectionIcons[i] = AddIcon(slot);
            slot.gameObject.AddComponent<Button>().onClick.AddListener(() => UseCollectionSlot(captured));
            var hover = slot.gameObject.AddComponent<BagSlotHover>();
            hover.OnEnter = () => ShowPetInfo(CollectionPetAt(captured), false);
            hover.OnExit = HideInfo;
        }
    }

    private static Image AddIcon(Image slot)
    {
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slot.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);
        var icon = iconGO.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        return icon;
    }

    private Image MakeSlot(string name, Vector2 position, string hint)
    {
        var slot = RuntimeUIKit.MakeBox(name, panel.transform, position, new Vector2(SlotSize, SlotSize), SlotColor);
        if (string.IsNullOrEmpty(hint)) return slot;

        var text = RuntimeUIKit.MakeText("Hint", slot.transform, hint, 10, TextAnchor.MiddleCenter, SlotHint, Vector2.zero, new Vector2(SlotSize, SlotSize));
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return slot;
    }
}

// Reports the pointer entering / leaving a bag slot, to show the item's description.
public class BagSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action OnEnter;
    public Action OnExit;

    public void OnPointerEnter(PointerEventData eventData) { OnEnter?.Invoke(); }
    public void OnPointerExit(PointerEventData eventData) { OnExit?.Invoke(); }
}
