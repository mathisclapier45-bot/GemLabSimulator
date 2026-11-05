using System.Collections.Generic;

// What the advanced instruments of the laboratory showed about a stone, in words: for the stone sheet and for the second page of
// the GemLab report. None of it is required to print a report; each instrument only shows up once the player has used it on the stone.
public class LabEntry
{
    public string Icon;           // sprite in Resources/ToolIcons
    public string Title;
    public List<string> Lines = new List<string>();
}

public static class LabFindings
{
    // Observation keys recorded by the instruments (besides the ones of ExaminationTool.MeasurementType).
    public const string FtirKey = "FtirRun";
    public static string RamanKey(RamanTarget target) => "Raman_" + target;
    public static string MebKey(MebMode mode, int magnification = 0) => mode == MebMode.Surface ? $"Meb_Surface_{magnification}" : "Meb_" + mode;

    private static readonly int[] Magnifications = { 20000, 5000, 1000 };

    // Computing a spectrum or an image is not free: the text of an entry is built once per stone and instrument.
    private static readonly Dictionary<string, LabEntry> Cache = new Dictionary<string, LabEntry>();

    public static bool Any(GemInstance gem)
    {
        if (gem == null) return false;
        foreach (var key in AllKeys())
            if (gem.HasObserved(key)) return true;
        return false;
    }

    private static IEnumerable<string> AllKeys()
    {
        yield return ExaminationTool.MeasurementType.ConfirmedOrigin.ToString();
        yield return FtirKey;
        foreach (RamanTarget target in System.Enum.GetValues(typeof(RamanTarget))) yield return RamanKey(target);
        yield return ExaminationTool.MeasurementType.DiamondGrowthImaging.ToString();
        yield return ExaminationTool.MeasurementType.DiamondScreening.ToString();
        foreach (var mag in Magnifications) yield return MebKey(MebMode.Surface, mag);
        yield return MebKey(MebMode.Cathodoluminescence);
        yield return MebKey(MebMode.XRay);
        yield return ExaminationTool.MeasurementType.ElementalAnalysis.ToString();
        yield return ExaminationTool.MeasurementType.TraceElements.ToString();
        yield return ExaminationTool.MeasurementType.Photoluminescence.ToString();
    }

    public static List<LabEntry> Entries(GemInstance gem)
    {
        var list = new List<LabEntry>();
        if (gem == null) return list;

        if (gem.HasObserved(ExaminationTool.MeasurementType.ConfirmedOrigin.ToString())) list.Add(Cached(gem, "uvvis", UvVis));
        if (gem.HasObserved(FtirKey)) list.Add(Cached(gem, "ftir", Ftir));

        foreach (RamanTarget target in System.Enum.GetValues(typeof(RamanTarget)))
        {
            if (!gem.HasObserved(RamanKey(target))) continue;
            var captured = target;
            list.Add(Cached(gem, "raman" + target, g => Raman(g, captured)));
        }

        foreach (var mag in Magnifications)
        {
            if (!gem.HasObserved(MebKey(MebMode.Surface, mag))) continue;
            var captured = mag;
            list.Add(Cached(gem, "mebsurface" + mag, g => Meb(g, MebMode.Surface, captured)));
            break;      // the strongest magnification used
        }
        if (gem.HasObserved(MebKey(MebMode.Cathodoluminescence))) list.Add(Cached(gem, "mebcl", g => Meb(g, MebMode.Cathodoluminescence, 0)));
        if (gem.HasObserved(MebKey(MebMode.XRay))) list.Add(Cached(gem, "mebxray", g => Meb(g, MebMode.XRay, 0)));

        if (gem.HasObserved(ExaminationTool.MeasurementType.ElementalAnalysis.ToString())) list.Add(Cached(gem, "edxrf", Edxrf));
        if (gem.HasObserved(ExaminationTool.MeasurementType.TraceElements.ToString())) list.Add(Cached(gem, "laicpms", LaIcpMs));
        if (gem.HasObserved(ExaminationTool.MeasurementType.Photoluminescence.ToString())) list.Add(Cached(gem, "pl", Photoluminescence));
        if (gem.HasObserved(ExaminationTool.MeasurementType.DiamondGrowthImaging.ToString())) list.Add(Cached(gem, "diamondview", DiamondView));
        if (gem.HasObserved(ExaminationTool.MeasurementType.DiamondScreening.ToString())) list.Add(Cached(gem, "diamondsure", DiamondSureEntry));
        return list;
    }

    private static LabEntry Cached(GemInstance gem, string name, System.Func<GemInstance, LabEntry> build)
    {
        var key = $"{gem.SpecimenId}|{gem.ShapeName}|{gem.CutStyleName}|{name}";
        if (!Cache.TryGetValue(key, out var entry))
        {
            entry = build(gem);
            Cache[key] = entry;
        }
        return entry;
    }

    // ---- One entry per instrument ------------------------------------------------------------------------------------

    private static LabEntry UvVis(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "UvVisNir", Title = "UV-Vis-NIR" };
        var r = UvVisNirSpectrum.Measure(gem);
        if (r.Opaque)
        {
            entry.Lines.Add("Pierre opaque : aucune transmission mesurable.");
            return entry;
        }

        var bands = UvVisNirSpectrum.BandLines(r);
        var shown = System.Math.Min(3, bands.Count);
        entry.Lines.Add("Bandes d'absorption : " + string.Join(" ; ", bands.GetRange(0, shown)));
        var elements = $"Éléments traces : {UvVisNirSpectrum.TraceElements(r)}";
        if (r.IronLevel != null) elements += $" — fer : {r.IronLevel}";
        entry.Lines.Add(elements);
        return entry;
    }

    private static LabEntry Ftir(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "FTIR", Title = "FTIR" };
        var r = FtirSpectrum.Measure(gem);
        foreach (var line in r.Analysis) entry.Lines.Add(line);
        if (entry.Lines.Count == 0) entry.Lines.Add("Aucun élément notable.");
        return entry;
    }

    private static LabEntry Raman(GemInstance gem, RamanTarget target)
    {
        var where = target == RamanTarget.Host ? "cristal hôte" : target == RamanTarget.Inclusion ? "inclusion" : "fissure";
        var entry = new LabEntry { Icon = "Raman", Title = $"Raman ({where})" };
        var r = RamanSpectrum.Measure(gem, target);
        foreach (var line in r.Analysis) entry.Lines.Add(line);
        if (r.Matches.Count > 0) entry.Lines.Add($"Meilleure correspondance : {r.Matches[0].Name} ({r.Matches[0].Score} %)");
        return entry;
    }

    private static LabEntry Meb(GemInstance gem, MebMode mode, int magnification)
    {
        var title = mode == MebMode.Surface ? $"MEB (surface, x{magnification:N0})"
            : mode == MebMode.Cathodoluminescence ? "MEB (cathodoluminescence)" : "MEB (analyse X)";
        var entry = new LabEntry { Icon = "MEB", Title = title };
        var r = MebImaging.Measure(gem, mode, magnification);
        if (!string.IsNullOrEmpty(r.Observation)) entry.Lines.Add(r.Observation);
        if (!string.IsNullOrEmpty(r.Interpretation)) entry.Lines.Add(r.Interpretation);
        return entry;
    }

    private static LabEntry Edxrf(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "EDXRF", Title = "EDXRF" };
        var r = EdxrfSpectrum.Measure(gem);
        entry.Lines.Add(r.Elements.Count == 0 ? "Aucun élément détecté (matériau trop léger)." : $"Éléments détectés : {string.Join(", ", r.Elements)}");
        foreach (var line in r.Analysis) entry.Lines.Add(line);
        return entry;
    }

    private static LabEntry LaIcpMs(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "LAICPMS", Title = "LA-ICP-MS" };
        var r = LaIcpMsAnalysis.Measure(gem);
        var count = System.Math.Min(r.Elements.Count, 8);
        var parts = new List<string>();
        for (var i = 0; i < count; i++) parts.Add($"{r.Elements[i].Symbol} {LaIcpMsAnalysis.FormatPpm(r.Elements[i].Ppm)} ppm");
        entry.Lines.Add(count == 0 ? "Aucun élément détecté." : $"Éléments détectés : {string.Join(", ", parts)}");
        foreach (var line in r.Analysis) entry.Lines.Add(line);
        return entry;
    }

    private static LabEntry Photoluminescence(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "Photoluminescence", Title = "Photoluminescence (488 nm, 77 K)" };
        var r = PhotoluminescenceSpectrum.Measure(gem);
        if (r.Peaks.Count > 0)
        {
            var parts = new List<string>();
            foreach (var peak in r.Peaks) parts.Add($"{peak.Label} {peak.Nm:0.0} nm");
            entry.Lines.Add($"Raies : {string.Join(", ", parts)}");
        }
        foreach (var line in r.Analysis) entry.Lines.Add(line);
        return entry;
    }

    private static LabEntry DiamondView(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "DiamondView", Title = "DiamondView" };
        entry.Lines.Add(DiamondViewImage.Summary(gem));
        return entry;
    }

    private static LabEntry DiamondSureEntry(GemInstance gem)
    {
        var entry = new LabEntry { Icon = "DiamondSure", Title = "DiamondSure" };
        var verdict = DiamondSure.Label(gem);
        entry.Lines.Add(DiamondSure.VerdictOf(gem) == DiamondSureVerdict.NotDiamond
            ? verdict
            : $"{verdict} — transmission UV à 225 nm : {DiamondSure.TransmissionPercent(gem)} %");
        return entry;
    }
}
