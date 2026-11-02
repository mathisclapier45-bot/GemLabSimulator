using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, IGemContainer
{
    public static PlayerInventory Instance { get; private set; }

    // Number of slots of the bag (4 columns); the inventory window grows with it.
    public const int Capacity = 24;
    private const int MaxCapacity = Capacity;

    private readonly List<GemInstance> gems = new List<GemInstance>();

    public IReadOnlyList<GemInstance> Gems => gems;
    public GemInstance SelectedGem { get; private set; }
    public bool IsFull => gems.Count >= MaxCapacity;
    public int FreeSlots => Mathf.Max(0, MaxCapacity - gems.Count);

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
        Sfx.Play(Sfx.Id.Pickup);
        if (SelectedGem == null) SelectedGem = instance;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddGem(GemInstance instance)
    {
        if (IsFull) return false;

        gems.Add(instance);
        Sfx.Play(Sfx.Id.Pickup);
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

    // For changes made to a stone already in the bag (identified...) so the slots redraw their badges.
    public void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void RemoveGem(GemInstance instance)
    {
        gems.Remove(instance);
        if (SelectedGem == instance)
            SelectedGem = gems.Count > 0 ? gems[0] : null;

        OnInventoryChanged?.Invoke();
    }

    public void LoadGems(List<GemInstance> restoredGems)
    {
        gems.Clear();
        gems.AddRange(restoredGems);
        SelectedGem = gems.Count > 0 ? gems[0] : null;
        OnInventoryChanged?.Invoke();
    }
}
