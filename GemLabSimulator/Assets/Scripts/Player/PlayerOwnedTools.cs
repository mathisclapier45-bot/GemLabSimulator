using System;
using System.Collections.Generic;
using UnityEngine;

// Instruments bought at the GemStore. Desk stations in the lab appear once their tool is owned.
public class PlayerOwnedTools : MonoBehaviour
{
    public static PlayerOwnedTools Instance { get; private set; }

    public const int BagSlotCount = 18;

    private readonly HashSet<string> owned = new HashSet<string>();
    private readonly string[] bagSlots = new string[BagSlotCount];

    // The 18 field-equipment slots of the bag: an item id, or null when the slot is empty.
    public IReadOnlyList<string> BagSlots => bagSlots;

    public IEnumerable<string> Owned => owned;

    // The lantern of the bag, lit or put out. It can only be lit once it has been bought.
    public const string LanternId = "Lanterne";
    public bool LanternLit { get; private set; }
    public event Action OnToolsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    // Scenes generated before this component existed do not carry it: add it to the player on demand.
    public static PlayerOwnedTools Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<PlayerOwnedTools>();
        return Instance;
    }

    public bool Has(string toolId) => owned.Contains(toolId);

    public void Add(string toolId)
    {
        if (!owned.Add(toolId)) return;
        OnToolsChanged?.Invoke();
    }

    public void SetLantern(bool lit)
    {
        lit = lit && owned.Contains(LanternId);
        if (LanternLit == lit) return;
        LanternLit = lit;
        OnToolsChanged?.Invoke();
    }

    public bool BagHasRoom => FirstEmptyBagSlot() >= 0;

    private int FirstEmptyBagSlot()
    {
        for (var i = 0; i < bagSlots.Length; i++)
            if (string.IsNullOrEmpty(bagSlots[i])) return i;
        return -1;
    }

    // Buys into the bag: the item is owned and takes the first free field-equipment slot.
    public bool TryAddToBag(string toolId)
    {
        var slot = FirstEmptyBagSlot();
        if (slot < 0 || owned.Contains(toolId)) return false;

        owned.Add(toolId);
        bagSlots[slot] = toolId;
        OnToolsChanged?.Invoke();
        return true;
    }

    public void LoadTools(IEnumerable<string> toolIds, IList<string> savedBagSlots = null)
    {
        owned.Clear();
        if (toolIds != null)
            foreach (var id in toolIds) owned.Add(id);

        for (var i = 0; i < bagSlots.Length; i++)
            bagSlots[i] = savedBagSlots != null && i < savedBagSlots.Count && !string.IsNullOrEmpty(savedBagSlots[i]) ? savedBagSlots[i] : null;
        OnToolsChanged?.Invoke();
    }
}
