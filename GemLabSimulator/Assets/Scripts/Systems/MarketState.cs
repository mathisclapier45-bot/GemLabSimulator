using System;
using System.Collections.Generic;
using UnityEngine;

public class MarketVendor
{
    public string CountryName;
    public readonly List<GemInstance> Stock = new List<GemInstance>();
}

public class MarketState : MonoBehaviour
{
    public static MarketState Instance { get; private set; }

    private const int VendorCount = 4;
    private const int ItemsPerVendor = 3;
    private const float PriceMarkup = 1.4f;

    public List<MarketVendor> Vendors { get; private set; } = new List<MarketVendor>();

    public event Action OnMarketChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += OnDayChanged;
        if (Vendors.Count == 0) GenerateVendors();
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged(int day)
    {
        GenerateVendors();
    }

    private void GenerateVendors()
    {
        Vendors.Clear();

        var database = GemDatabase.Instance;
        if (database == null || database.allGems.Count == 0) return;

        var pool = new List<MarketCountries.Country>(MarketCountries.Pool);
        for (var i = 0; i < VendorCount && pool.Count > 0; i++)
        {
            var pick = UnityEngine.Random.Range(0, pool.Count);
            var country = pool[pick];
            pool.RemoveAt(pick);

            var vendor = new MarketVendor { CountryName = country.Name };
            for (var j = 0; j < ItemsPerVendor; j++)
                vendor.Stock.Add(GenerateStockItem(country, database));
            Vendors.Add(vendor);
        }

        OnMarketChanged?.Invoke();
    }

    private static GemInstance GenerateStockItem(MarketCountries.Country country, GemDatabase database)
    {
        var candidates = new List<(GemDefinition def, GemOrigin origin)>();

        foreach (var def in database.allGems)
        {
            if (def == null || def.isTunnelExclusive) continue;
            foreach (var origin in def.origins)
            {
                foreach (var keyword in country.MatchKeywords)
                {
                    if (origin.name.Contains(keyword))
                    {
                        candidates.Add((def, origin));
                        break;
                    }
                }
            }
        }

        GemInstance instance;
        if (candidates.Count > 0)
        {
            var (def, origin) = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            instance = new GemInstance(def, origin);
        }
        else
        {
            GemDefinition def;
            do
            {
                def = database.allGems[UnityEngine.Random.Range(0, database.allGems.Count)];
            } while (def.isTunnelExclusive);
            instance = new GemInstance(def);
        }

        instance.MarkIdentified();
        return instance;
    }

    public int GetPrice(GemInstance gem)
    {
        return Mathf.RoundToInt(gem.FinalValue * PriceMarkup);
    }

    public bool TryPurchase(MarketVendor vendor, GemInstance item, PlayerInventory inventory, PlayerWallet wallet)
    {
        if (vendor == null || item == null || inventory == null || wallet == null) return false;
        if (!vendor.Stock.Contains(item)) return false;

        var price = GetPrice(item);
        if (!wallet.TrySpend(price)) return false;

        if (!inventory.TryAddGem(item))
        {
            wallet.AddMoney(price);
            return false;
        }

        vendor.Stock.Remove(item);
        GemCodex.Instance?.MarkDiscovered(item.Definition.name);
        OnMarketChanged?.Invoke();
        return true;
    }
}
