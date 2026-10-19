using System.Collections.Generic;

// Dimensions taken with the micrometer, one position at a time: length, width and depth are logged on the
// stone separately, and the ratios appear as soon as the needed measurements exist.
public static class DimensionReport
{
    public const string LengthKey = "LengthMm";
    public const string WidthKey = "WidthMm";
    public const string DepthKey = "DepthMm";

    public static List<string> Lines(GemInstance gem)
    {
        var lines = new List<string>();
        var hasLength = gem.HasMeasured(LengthKey);
        var hasWidth = gem.HasMeasured(WidthKey);
        var hasDepth = gem.HasMeasured(DepthKey);
        var length = gem.GetMeasuredValue(LengthKey);
        var width = gem.GetMeasuredValue(WidthKey);
        var depth = gem.GetMeasuredValue(DepthKey);

        if (hasLength) lines.Add($"Longueur : {length:F2} mm");
        if (hasWidth) lines.Add($"Largeur : {width:F2} mm");
        if (hasDepth) lines.Add($"Profondeur : {depth:F2} mm");
        if (hasLength && hasWidth && width > 0f) lines.Add($"Ratio longueur/largeur : {length / width:F2}");
        if (hasDepth && hasWidth && width > 0f) lines.Add($"Profondeur/largeur : {depth / width * 100f:F0} %");

        // Older saves recorded all three dimensions at once.
        if (lines.Count == 0 && gem.HasObserved(ExaminationTool.MeasurementType.Dimensions.ToString()))
            lines.Add($"Dimensions : {gem.LengthMm:F1} x {gem.WidthMm:F1} x {gem.DepthMm:F1} mm");

        return lines;
    }
}
