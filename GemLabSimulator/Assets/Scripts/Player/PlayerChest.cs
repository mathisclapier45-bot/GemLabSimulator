using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChest : MonoBehaviour, IGemContainer
{
    public static PlayerChest Instance { get; private set; }

    private const int MaxCapacity = 24;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public bool IsFull => gems.Count >= MaxCapacity;

    public event Action OnChestChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public bool CanAccept(GemInstance gem, out string reason)
    {
        reason = null;
        if (!gem.IsIdentified)
            reason = "Seules les gemmes identifiées peuvent être rangées dans le coffre.";
        else if (gem.IsDonation)
            reason = "Les dons se rangent dans l'armoire.";
        else if (gem.IsClientStone)
            reason = "Les pierres des clients se rangent dans l'armoire bleue.";
        return reason == null;
    }

    public bool TryAddGem(GemInstance instance)
    {
        if (IsFull) return false;

        gems.Add(instance);
        OnChestChanged?.Invoke();
        return true;
    }

    public void RemoveGem(GemInstance instance)
    {
        gems.Remove(instance);
        OnChestChanged?.Invoke();
    }

    public void LoadGems(List<GemInstance> restoredGems)
    {
        gems.Clear();
        gems.AddRange(restoredGems);
        OnChestChanged?.Invoke();
    }
}
