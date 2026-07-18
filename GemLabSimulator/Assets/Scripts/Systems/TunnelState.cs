using System;
using System.Collections.Generic;
using UnityEngine;

public class TunnelState : MonoBehaviour
{
    public static TunnelState Instance { get; private set; }

    // The tunnel merchant only appears late at night, between 22h and 6h.
    private const float NightStart = 22f / 24f;
    private const float NightEnd = 6f / 24f;
    private const float PriceMarkup = 3.5f;

    public GemInstance CurrentOffer { get; private set; }

    public event Action OnTunnelChanged;

    private bool wasNight;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Update()
    {
        var night = IsNight();
        if (night == wasNight) return;
        wasNight = night;

        if (night)
        {
            GenerateOffer();
        }
        else
        {
            CurrentOffer = null;
        }

        OnTunnelChanged?.Invoke();
    }

    public static bool IsNight()
    {
        var phase = DayNightOverlay.GetCurrentPhase();
        return phase >= NightStart || phase < NightEnd;
    }

    private void GenerateOffer()
    {
        var database = GemDatabase.Instance;
        if (database == null) return;

        var pool = new List<GemDefinition>();
        foreach (var def in database.allGems)
        {
            if (def != null && def.isTunnelExclusive) pool.Add(def);
        }
        if (pool.Count == 0) return;

        var definition = pool[UnityEngine.Random.Range(0, pool.Count)];
        CurrentOffer = new GemInstance(definition);
        CurrentOffer.MarkIdentified();
    }

    public int GetPrice(GemInstance gem)
    {
        var price = Mathf.RoundToInt(gem.FinalValue * PriceMarkup);
        return Mathf.Max(price, gem.Definition.tunnelMinPrice);
    }

    public bool TryPurchase(PlayerInventory inventory, PlayerWallet wallet)
    {
        if (CurrentOffer == null || inventory == null || wallet == null) return false;

        var price = GetPrice(CurrentOffer);
        if (!wallet.TrySpend(price)) return false;

        if (!inventory.TryAddGem(CurrentOffer))
        {
            wallet.AddMoney(price);
            return false;
        }

        GemCodex.Instance?.MarkDiscovered(CurrentOffer.Definition.name);
        CurrentOffer = null;
        OnTunnelChanged?.Invoke();
        return true;
    }
}
