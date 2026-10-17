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
        GemTooltipUI.Instance?.Show(BuildTooltipText(Gem), eventData.position);
    }

    private static string BuildTooltipText(GemInstance gem)
    {
        if (!gem.IsIdentified)
            return $"(gemme non identifiée)\nN° {gem.DisplayId}";

        var valueLine = gem.IsDonation
            ? "Don — à conserver, non destiné à être renvoyé"
            : $"Valeur estimée : {gem.FinalValue}€";

        return $"{gem.Definition.displayName}  (N° {gem.DisplayId})\n" +
               $"Origine : {gem.Origin.name}\n" +
               (gem.IsRough ? "Pierre brute\n" : $"Taille : {gem.ShapeName} — {gem.CutStyleName}\n") +
               $"Poids : {gem.CaratWeight:F2} ct\n" +
               $"Dimensions : {gem.LengthMm:F1} x {gem.WidthMm:F1} x {gem.DepthMm:F1} mm\n" +
               $"Pureté : {gem.ClarityGrade}\n" +
               $"Saturation : {gem.SaturationGrade}\n" +
               $"Traitement : {gem.TreatmentGrade}\n" +
               $"Authenticité : {gem.AuthenticityGrade}\n" +
               $"{valueLine}";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GemTooltipUI.Instance?.Hide();
    }
}
