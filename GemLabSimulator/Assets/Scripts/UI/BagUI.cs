using System;
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
    private CalendarTabUI calendar;
    private Image bagTabImage;
    private Image calendarTabImage;
    private GameObject returnButton;
    private readonly Image[] equipmentIcons = new Image[PlayerOwnedTools.BagSlotCount];
    private Text infoText;
    private Image equippedPetIcon;
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
    }

    private void Start()
    {
        var tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += RefreshEquipment;
        RefreshEquipment();

        var pets = PlayerPets.Ensure();
        if (pets != null) pets.OnPetsChanged += RefreshPets;
        RefreshPets();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshEquipment;
        if (PlayerPets.Instance != null) PlayerPets.Instance.OnPetsChanged -= RefreshPets;
    }

    private void RefreshEquipment()
    {
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

        // H opens the bag, K the calendar: the same key closes it again.
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (panel.activeSelf && bagContent.activeSelf) panel.SetActive(false);
            else
            {
                panel.SetActive(true);
                SelectTab(false);
            }
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            if (panel.activeSelf && calendar.Root.activeSelf) panel.SetActive(false);
            else
            {
                panel.SetActive(true);
                SelectTab(true);
            }
        }
        else if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) panel.SetActive(false);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
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

    // Two tabs at the top: the bag itself, and the calendar. What the bag panel holds goes into one container, shown or hidden by the tabs.
    private void BuildTabs()
    {
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

        var bagTab = RuntimeUIKit.MakeButton("TabBag", panel.transform, "Sac", 14, SlotColor, new Vector2(120f, -12f), new Vector2(110f, 30f));
        bagTab.onClick.AddListener(() => SelectTab(false));
        bagTabImage = bagTab.GetComponent<Image>();
        var calendarTab = RuntimeUIKit.MakeButton("TabCalendar", panel.transform, "Calendrier (K)", 14, SlotColor, new Vector2(236f, -12f), new Vector2(150f, 30f));
        calendarTab.onClick.AddListener(() => SelectTab(true));
        calendarTabImage = calendarTab.GetComponent<Image>();
        SelectTab(false);
    }

    private void SelectTab(bool showCalendar)
    {
        bagContent.SetActive(!showCalendar);
        calendar.Root.SetActive(showCalendar);
        bagTabImage.color = showCalendar ? SlotColor : new Color(0.45f, 0.35f, 0.22f, 1f);
        calendarTabImage.color = showCalendar ? new Color(0.45f, 0.35f, 0.22f, 1f) : SlotColor;
        if (showCalendar) calendar.Show();
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
        RuntimeUIKit.SetTopLeft(portrait.GetComponent<RectTransform>(), new Vector2(frameX + 14f, frameY - 40f), new Vector2(140f, 280f));
        var portraitImage = portrait.GetComponent<Image>();
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
            MakeSlot($"Habit_{labels[i]}", new Vector2(frameX + frameW - SlotSize - 14f, slotY), hints[i]);
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
            var slot = RuntimeUIKit.MakeBox($"AnimalSac_{i}", panel.transform, new Vector2(x + 4f, -292f - i * 56f), new Vector2(48f, 48f), SlotColor);
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
