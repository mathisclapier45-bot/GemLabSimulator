using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Free rotation of a stone: dragging turns it around the horizontal and vertical axes, the wheel rolls it.
public class DichroscopeDragArea : MonoBehaviour, IDragHandler, IScrollHandler
{
    public Action<Vector2> OnDragDelta;
    public Action<float> OnRoll;
    public Action<Vector2> OnPanDelta;     // right button drag, when set
    public Action<Vector2> OnScrollDelta;  // the raw wheel, when set (OnRoll is then not called): macOS turns shift + wheel into a horizontal scroll

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && OnPanDelta != null)
            OnPanDelta.Invoke(eventData.delta);
        else
            OnDragDelta?.Invoke(eventData.delta);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (OnScrollDelta != null) OnScrollDelta.Invoke(eventData.scrollDelta * 6f);
        else OnRoll?.Invoke(eventData.scrollDelta.y * 6f);
    }
}
