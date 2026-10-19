using UnityEngine;

public class RoughMarketVendorSpot : MonoBehaviour
{
    [SerializeField] private int vendorIndex;
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Transform tableItemsRoot;

    private bool playerInRange;

    private void Start()
    {
        if (RoughMarketState.Instance != null) RoughMarketState.Instance.OnMarketChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (RoughMarketState.Instance != null) RoughMarketState.Instance.OnMarketChanged -= Refresh;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        RoughMarketUI.Instance?.Open(GetVendor());
    }

    private RoughMarketVendor GetVendor()
    {
        if (RoughMarketState.Instance == null) return null;
        return vendorIndex < RoughMarketState.Instance.Vendors.Count ? RoughMarketState.Instance.Vendors[vendorIndex] : null;
    }

    private void Refresh()
    {
        var vendor = GetVendor();

        if (flagRenderer != null)
            flagRenderer.sprite = vendor != null ? MarketFlagLookup.GetFlag(vendor.CountryName) : null;

        if (tableItemsRoot == null) return;

        for (var i = tableItemsRoot.childCount - 1; i >= 0; i--)
            Destroy(tableItemsRoot.GetChild(i).gameObject);

        if (vendor == null) return;

        for (var i = 0; i < vendor.Stock.Count; i++)
        {
            var item = vendor.Stock[i];
            var go = new GameObject($"Item_{i}", typeof(SpriteRenderer));
            go.transform.SetParent(tableItemsRoot, false);
            go.transform.localPosition = new Vector3((i - 1) * 0.32f, 0f, 0f);
            go.transform.localScale = Vector3.one * 0.45f;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = item.DisplaySprite;
            sr.sortingOrder = 5;
        }
    }
}
