using System;
using UnityEngine;
using UnityEngine.EventSystems;

// A single-stone slot fed by dragging a gem out of the player's inventory.
public class LapidarySlotDropZone : MonoBehaviour, IDropHandler
{
    public Action<GemInstance> OnGemDropped;

    public void OnDrop(PointerEventData eventData)
    {
        var source = GemDragSource.CurrentDrag;
        if (source != null && source.Gem != null && ReferenceEquals(source.SourceContainer, PlayerInventory.Instance))
            OnGemDropped?.Invoke(source.Gem);

        GemDragSource.CleanupDrag();
    }
}
