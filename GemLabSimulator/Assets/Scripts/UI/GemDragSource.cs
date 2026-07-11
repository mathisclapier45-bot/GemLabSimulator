using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static GemDragSource CurrentDrag { get; private set; }

    public GemInstance Gem;
    public IGemContainer SourceContainer;

    private static GameObject dragGhost;
    private Image iconImage;

    public void SetIcon(Image icon)
    {
        iconImage = icon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Gem == null || iconImage == null) return;

        CurrentDrag = this;

        var canvas = GetComponentInParent<Canvas>();
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        dragGhost.transform.SetParent(canvas.transform, false);
        var img = dragGhost.GetComponent<Image>();
        img.sprite = iconImage.sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        dragGhost.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);
        dragGhost.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null) dragGhost.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CleanupDrag();
    }

    public static void CleanupDrag()
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }
        CurrentDrag = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Gem == null) return;
        var text = Gem.IsIdentified ? Gem.Definition.displayName : "(gemme non identifiée)";
        GemTooltipUI.Instance?.Show(text, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GemTooltipUI.Instance?.Hide();
    }
}
