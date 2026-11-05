using System.Collections.Generic;

// What the lapidary's polished window shows of a rough stone's interior. Built from generic inclusion
// categories only, so it never names the species itself.
public static class InclusionWindowReport
{
    // Generic categories of what is found inside, so the window never names the species itself.
    private static string Category(FeatureKind kind)
    {
        switch (kind)
        {
            case FeatureKind.Crystal: return "cristaux inclus";
            case FeatureKind.Bubble: return "bulles de gaz";
            case FeatureKind.Fluid:
            case FeatureKind.TwoPhase:
            case FeatureKind.ThreePhase: return "inclusions fluides (cavités à une, deux ou trois phases)";
            case FeatureKind.NegativeCrystal: return "cavités à facettes (cristaux négatifs)";
            case FeatureKind.Needle: return "aiguilles et fibres";
            case FeatureKind.HorseTail: return "fibres rayonnantes";
            case FeatureKind.Fracture: return "fractures et fissures";
            case FeatureKind.Cleavage: return "clivage";
            case FeatureKind.Feather:
            case FeatureKind.Healing: return "givres de guérison (fractures cicatrisées)";
            case FeatureKind.Twinning: return "macles";
            case FeatureKind.Cloud: return "nuages de fines particules";
            case FeatureKind.LaserChannel: return "un canal droit très fin (trou de laser)";
            case FeatureKind.KmFeather: return "des fissures irrégulières en zig-zag ou en spirale";
            case FeatureKind.Lenticular: return "des disques de tension (éclatements lenticulaires)";
            default: return "structures de croissance (zones et stries)";
        }
    }

    public static string Build(GemInstance gem)
    {
        var def = gem.Definition;
        var text = ((def.inclusions ?? "") + " " + (gem.InclusionDescription ?? "")).ToLowerInvariant();

        if (text.Contains("non applicable"))
            return "Pierre opaque : la fenêtre ne laisse rien voir de l'intérieur.";

        var found = new List<string>();
        foreach (var f in LoupeFeatures.GenerateMicro(gem))
        {
            if (f.External) continue;
            var label = Category(f.Kind);
            if (!found.Contains(label)) found.Add(label);
        }

        // Treatments leave their own traces inside the stone.
        if (gem.HasTreatment(TreatmentKind.Heated) && !found.Contains("zones de tension (halos autour des inclusions)")) found.Add("zones de tension (halos autour des inclusions)");
        if (gem.HasTreatment(TreatmentKind.Filled) && !found.Contains("fractures et fissures")) found.Add("fractures et fissures");

        switch (gem.ClarityGrade)
        {
            case "Loupe clean":
                return "Pierre propre : la fenêtre ne révèle aucune inclusion, même à fort grossissement.";
            case "Eye-clean":
                return "Pierre non incluse à l'œil nu : quelques inclusions fines seulement" +
                       (found.Count > 0 ? " — " + string.Join(", ", found.GetRange(0, System.Math.Min(2, found.Count))) : "") + ".";
            case "Inclus":
                return "Pierre incluse" + Describe(found) + ".";
            default:
                return "Pierre fortement incluse" + Describe(found) + " — présents en grand nombre.";
        }
    }

    private static string Describe(List<string> found)
    {
        return found.Count > 0 ? " : " + string.Join(", ", found) : " : inclusions diffuses sans caractère marqué";
    }
}
