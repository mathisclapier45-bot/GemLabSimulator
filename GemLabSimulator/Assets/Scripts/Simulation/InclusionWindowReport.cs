using System.Collections.Generic;

// What the lapidary's polished window shows of a rough stone's interior. Built from generic inclusion
// categories only, so it never names the species itself.
public static class InclusionWindowReport
{
    private static readonly (string label, string[] keywords)[] Categories =
    {
        ("clivage", new[] { "clivage" }),
        ("fractures et fissures", new[] { "fracture", "fissure" }),
        ("givres de guérison (fractures cicatrisées)", new[] { "empreintes digitales", "plume", "voiles" }),
        ("zones de tension (halos autour des inclusions)", new[] { "halo", "discoïde", "disque" }),
        ("cristaux inclus", new[] { "cristaux", "cristal", "cristallines", "aiguille", "aciculaire", "soie", "octa" }),
        ("inclusions fluides", new[] { "liquide", "fluide", "multiphas", "triphas", "jardin" }),
        ("bulles de gaz", new[] { "bulle" }),
        ("fibres", new[] { "fibre", "tubulaire" }),
        ("macles", new[] { "macl" }),
        ("zonation de couleur", new[] { "zonation", "bandes", "secteurs" }),
    };

    public static string Build(GemInstance gem)
    {
        var def = gem.Definition;
        var text = ((def.inclusions ?? "") + " " + (gem.InclusionDescription ?? "")).ToLowerInvariant();

        if (text.Contains("non applicable"))
            return "Pierre opaque : la fenêtre ne laisse rien voir de l'intérieur.";

        var found = new List<string>();
        foreach (var (label, keywords) in Categories)
        {
            foreach (var keyword in keywords)
            {
                if (!text.Contains(keyword)) continue;
                found.Add(label);
                break;
            }
        }

        // Treatments leave their own traces inside the stone.
        if (gem.TreatmentGrade == "Chauffé" && !found.Contains(Categories[3].label)) found.Add(Categories[3].label);
        if (gem.TreatmentGrade == "Fracture comblée" && !found.Contains(Categories[1].label)) found.Add(Categories[1].label);

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
