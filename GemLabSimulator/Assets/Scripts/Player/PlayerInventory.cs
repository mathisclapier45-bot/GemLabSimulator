using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private const int MaxCapacity = 5;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public GemInstance SelectedGem { get; private set; }
    public bool IsFull => gems.Count >= MaxCapacity;

    public event Action OnInventoryChanged;

    public bool AddGem(GemDefinition definition)
    {
        if (IsFull) return false;

        var instance = new GemInstance(definition);
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
