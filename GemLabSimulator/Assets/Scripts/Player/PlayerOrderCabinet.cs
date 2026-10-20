using System;
using System.Collections.Generic;
using UnityEngine;

// The blue wardrobes: stones that clients sent in for an order, kept apart from the lab's own stones and the donations.
public class PlayerOrderCabinet : MonoBehaviour, ICabinetStorage
{
    public static PlayerOrderCabinet Instance { get; private set; }

    private const int MaxCapacity = 24;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public bool IsFull => gems.Count >= MaxCapacity;

    public event Action OnCabinetChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public bool CanAccept(GemInstance gem, out string reason)
    {
        reason = gem.IsClientStone ? null : "Cette armoire est réservée aux pierres des clients.";
        return reason == null;
    }

    public bool TryAddGem(GemInstance instance)
    {
        if (IsFull) return false;

        gems.Add(instance);
        OnCabinetChanged?.Invoke();
        return true;
    }

    public void RemoveGem(GemInstance instance)
    {
        gems.Remove(instance);
        OnCabinetChanged?.Invoke();
    }

    public void LoadGems(List<GemInstance> restoredGems)
    {
        gems.Clear();
        gems.AddRange(restoredGems);
        OnCabinetChanged?.Invoke();
    }
}
