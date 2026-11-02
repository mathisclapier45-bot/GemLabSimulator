using UnityEngine;
using UnityEngine.EventSystems;

// An item on a GemStore shelf. Click it with the mouse to see its description and price and to buy it.
public class ShopItem : MonoBehaviour
{
    [SerializeField] private string toolId;
    [SerializeField] private string displayName;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private int price = 100;
    [SerializeField] private string purchaseMessage;
    [SerializeField] private bool addsToBag;
    [SerializeField] private bool isPet;
    [SerializeField] private bool isBooster;

    private const float HoverScale = 1.12f;

    private SpriteRenderer spriteRenderer;
    private Collider2D clickArea;
    private Vector3 baseScale;

    public string ToolId => toolId;
    public string DisplayName => displayName;
    public string Description => description;
    public int Price => price;
    public string PurchaseMessage => purchaseMessage;
    public bool AddsToBag => addsToBag;
    public bool IsPet => isPet;
    public bool IsBooster => isBooster;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        clickArea = GetComponent<Collider2D>();
        baseScale = transform.localScale;
    }

    private void Start()
    {
        var tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += RefreshLook;
        var pets = PlayerPets.Ensure();
        if (pets != null) pets.OnPetsChanged += RefreshLook;
        RefreshLook();
    }

    private void OnDestroy()
    {
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshLook;
        if (PlayerPets.Instance != null) PlayerPets.Instance.OnPetsChanged -= RefreshLook;
    }

    // Already bought (a tool, or an animal waiting in the bag or following the player).
    public bool IsOwned()
    {
        if (isBooster) return false;     // boosters can be bought again every day
        if (isPet) return PlayerPets.Instance != null && PlayerPets.Instance.Has(toolId);
        return PlayerOwnedTools.Instance != null && PlayerOwnedTools.Instance.Has(toolId);
    }

    // Already bought: the item stays on the shelf but looks faded.
    private void RefreshLook()
    {
        if (spriteRenderer == null) return;
        var color = spriteRenderer.color;
        color.a = IsOwned() ? 0.4f : 1f;
        spriteRenderer.color = color;
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null || clickArea == null) return;

        var overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        var hovered = !overUI && clickArea.OverlapPoint(cam.ScreenToWorldPoint(Input.mousePosition));

        // The item grows slightly under the cursor.
        transform.localScale = hovered ? baseScale * HoverScale : baseScale;

        if (hovered && Input.GetMouseButtonDown(0)) ShopUI.TryOpen(this);
    }
}
