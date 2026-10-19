using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Drop target that also reports where on screen the gem was released.
public class MagnetDropZone : MonoBehaviour, IDropHandler
{
    public Action<GemInstance, Vector2> OnGemDropped;

    public void OnDrop(PointerEventData eventData)
    {
        var source = GemDragSource.CurrentDrag;
        if (source != null && source.Gem != null && ReferenceEquals(source.SourceContainer, PlayerInventory.Instance))
            OnGemDropped?.Invoke(source.Gem, eventData.position);

        GemDragSource.CleanupDrag();
    }
}
