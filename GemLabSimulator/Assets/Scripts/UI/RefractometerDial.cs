using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Rotary knob: dragging around its center (or scrolling over it) reports the rotation in radians.
public class RefractometerDial : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    public Action<float> OnRotate;

    private RectTransform rect;
    private float lastAngle;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    private float PointerAngle(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local);
        return Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastAngle = PointerAngle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var angle = PointerAngle(eventData);
        var delta = Mathf.DeltaAngle(lastAngle, angle);
        lastAngle = angle;
        OnRotate?.Invoke(delta * Mathf.Deg2Rad);
    }

    public void OnScroll(PointerEventData eventData)
    {
        OnRotate?.Invoke(eventData.scrollDelta.y * 0.12f);
    }
}
