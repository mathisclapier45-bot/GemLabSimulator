using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Free rotation of a stone: dragging turns it around the horizontal and vertical axes, the wheel rolls it.
public class DichroscopeDragArea : MonoBehaviour, IDragHandler, IScrollHandler
{
    public Action<Vector2> OnDragDelta;
    public Action<float> OnRoll;
    public Action<Vector2> OnPanDelta;     // right button drag, when set

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && OnPanDelta != null)
            OnPanDelta.Invoke(eventData.delta);
        else
            OnDragDelta?.Invoke(eventData.delta);
    }

    public void OnScroll(PointerEventData eventData)
    {
        OnRoll?.Invoke(eventData.scrollDelta.y * 6f);
    }
}
