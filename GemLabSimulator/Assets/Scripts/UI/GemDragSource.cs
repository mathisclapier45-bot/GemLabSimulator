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
            return $"(gemme non identifiée)\nN° {gem.DisplayId}{BuildMeasurementLines(gem)}";

        var valueLine = gem.IsDonation
            ? "Don — à conserver, non destiné à être renvoyé"
            : gem.IsClientStone
                ? $"Rapport d'identification : {gem.IdentificationReportFee}€ (payé par le client à la livraison)" +
                  (gem.HasReport ? "" : "\nRapport GemLab à imprimer avant l'envoi")
                : $"Valeur estimée : {gem.FinalValue}€ — spécimen personnel";

        return $"{gem.Definition.displayName}  (N° {gem.DisplayId})\n" +
               $"Origine : {(gem.HasObserved(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString()) ? gem.Origin.name : "non déterminée (spectrophotomètre)")}\n" +
               (gem.IsRough ? $"Pierre {gem.RoughLabel}\n" : $"Taille : {gem.ShapeName} — {gem.CutStyleName}\n") +
               (gem.IsSet ? $"Bijou : {JewelCatalog.Describe(gem)}\n" : "") +
               $"Poids : {gem.CaratWeight:F2} ct\n" +
               $"Dimensions : {gem.LengthMm:F1} x {gem.WidthMm:F1} x {gem.DepthMm:F1} mm\n" +
               (gem.ColorGrade != null
                   ? $"Couleur : {gem.ColorGrade}   Pureté : {gem.DiamondClarity}\n"
                   : $"Pureté : {gem.ClarityGrade}\nCouleur : {gem.ColorName}\n" +
                     (gem.IsIdentified && !string.IsNullOrEmpty(gem.TradeName) ? $"Appellation : {gem.TradeName}\n" : "")) +
               (gem.Certificate != null ? $"Certificat n° {gem.Certificate.number} ({gem.Certificate.issuer})\n" : "") +
               $"Traitement : {gem.TreatmentGrade}\n" +
               $"Authenticité : {gem.AuthenticityGrade}\n" +
               $"{valueLine}";
    }

    // Measurements taken with the lab tools are added to the description of a stone that is not identified yet.
    private static string BuildMeasurementLines(GemInstance gem)
    {
        var lines = "";

        if (gem.HasObserved(ExaminationTool.MeasurementType.Weight.ToString()))
            lines += $"\nPoids : {gem.CaratWeight:F2} ct ({gem.CaratWeight * 0.2f:F3} g)";

        foreach (var line in DimensionReport.Lines(gem))
            lines += "\n" + line;

        var refractiveKey = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
        if (gem.HasMeasured(refractiveKey))
            lines += $"\nIndice de réfraction : {gem.GetMeasuredValue(refractiveKey):F3}";
        else if (gem.HasObserved("RefractiveIndexOTL"))
            lines += "\nIndice de réfraction : OTL (> 1.81)";

        var densityKey = ExaminationTool.MeasurementType.Density.ToString();
        if (gem.HasMeasured(densityKey))
            lines += $"\nDensité : {gem.GetMeasuredValue(densityKey):F2}";

        return lines;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GemTooltipUI.Instance?.Hide();
    }
}
