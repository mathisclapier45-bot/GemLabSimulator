using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChest : MonoBehaviour, IGemContainer
{
    public static PlayerChest Instance { get; private set; }

    private const int MaxCapacity = 12;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public bool IsFull => gems.Count >= MaxCapacity;

    public event Action OnChestChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
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
}
