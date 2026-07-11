using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, IGemContainer
{
    public static PlayerInventory Instance { get; private set; }

    private const int MaxCapacity = 12;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public GemInstance SelectedGem { get; private set; }
    public bool IsFull => gems.Count >= MaxCapacity;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public bool AddGem(GemDefinition definition)
    {
        if (IsFull) return false;

        var instance = new GemInstance(definition);
        gems.Add(instance);
        if (SelectedGem == null) SelectedGem = instance;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddGem(GemInstance instance)
    {
        if (IsFull) return false;

        gems.Add(instance);
        if (SelectedGem == null) SelectedGem = instance;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void Select(GemInstance instance)
    {
        if (!gems.Contains(instance)) return;
        SelectedGem = instance;
        OnInventoryChanged?.Invoke();
    }

    public void RemoveGem(GemInstance instance)
    {
        gems.Remove(instance);
        if (SelectedGem == instance)
            SelectedGem = gems.Count > 0 ? gems[0] : null;

        OnInventoryChanged?.Invoke();
    }
}
