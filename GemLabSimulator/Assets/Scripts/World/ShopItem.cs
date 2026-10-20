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
        RefreshLook();
    }

    private void OnDestroy()
    {
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshLook;
    }

    // Already bought: the item stays on the shelf but looks faded.
    private void RefreshLook()
    {
        if (spriteRenderer == null) return;
        var tools = PlayerOwnedTools.Instance;
        var color = spriteRenderer.color;
        color.a = tools != null && tools.Has(toolId) ? 0.4f : 1f;
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
