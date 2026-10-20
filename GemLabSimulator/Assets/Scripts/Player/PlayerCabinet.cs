using System;
using System.Collections.Generic;
using UnityEngine;

// The armoire: where donated stones (the gifts that arrive by mail and are meant to be kept) are stored.
public class PlayerCabinet : MonoBehaviour, ICabinetStorage
{
    public static PlayerCabinet Instance { get; private set; }

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
        reason = null;
        if (!gem.IsDonation)
            reason = "L'armoire est réservée aux dons.";
        else if (!gem.IsIdentified)
            reason = "Identifiez d'abord ce don avant de le ranger.";
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
