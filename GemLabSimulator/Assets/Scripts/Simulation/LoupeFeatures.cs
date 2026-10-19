using System.Collections.Generic;
using UnityEngine;

public enum FeatureKind
{
    // Internal
    Crystal, Fluid, Bubble, Fracture, Cleavage, Healing, Needle, Zoning, Striae,
    // External (surface)
    Scratch, Chip, Pit, Abrasion,
}

public class LoupeFeature
{
    public FeatureKind Kind;
    public bool External;
    // Internal: position inside the stone, each axis in [-1, 1] of its half size.
    // External: unit vector pointing to the spot on the surface.
    public Vector3 Position;
    // Direction of the feature in the stone's own frame (unit vector), for elongated features.
    public Vector3 Direction;
    public float Size;     // 0..1
}

// What the 10x loupe shows of a stone: inclusions inside (number from its clarity, types from its species)
// and defects on the surface (scratches, chips...). Everything is generated from the specimen so it is stable.
public static class LoupeFeatures
{
    private static readonly (FeatureKind kind, string[] keywords)[] InclusionTypes =
    {
        (FeatureKind.Cleavage, new[] { "clivage" }),
        (FeatureKind.Fracture, new[] { "fracture", "fissure" }),
        (FeatureKind.Healing, new[] { "empreintes digitales", "plume", "voiles" }),
        (FeatureKind.Fracture, new[] { "halo", "discoïde", "disque" }),
        (FeatureKind.Needle, new[] { "aiguille", "aciculaire", "soie", "fibre", "tubulaire" }),
        (FeatureKind.Crystal, new[] { "cristaux", "cristal", "cristallines", "octa", "macl" }),
        (FeatureKind.Fluid, new[] { "liquide", "fluide", "multiphas", "triphas", "jardin" }),
        (FeatureKind.Bubble, new[] { "bulle" }),
        (FeatureKind.Zoning, new[] { "zonation", "bandes", "secteurs" }),
    };

    public static int InternalCount(GemInstance gem)
    {
        switch (gem.ClarityGrade)
        {
            case "Loupe clean": return 0;
            case "Eye-clean": return 3;
            case "Inclus": return 9;
            default: return 18;
        }
    }

    public static List<LoupeFeature> Generate(GemInstance gem)
    {
        var rng = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "loupe"));
        var features = new List<LoupeFeature>();
        var def = gem.Definition;

        var opaque = def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque
                     || (!string.IsNullOrEmpty(def.transparency) && def.transparency.ToLowerInvariant().StartsWith("opaque"));

        // Internal inclusions: nothing visible inside an opaque stone.
        if (!opaque)
        {
            var kinds = TypesFor(gem);
            var count = InternalCount(gem);
            for (var i = 0; i < count; i++)
            {
                var kind = kinds[rng.Next(kinds.Count)];
                features.Add(new LoupeFeature
                {
                    Kind = kind,
                    External = false,
                    Position = new Vector3(Range(rng, -0.8f, 0.8f), Range(rng, -0.8f, 0.8f), Range(rng, -0.7f, 0.7f)),
                    Direction = RandomDirection(rng),
                    Size = gem.ClarityGrade == "Eye-clean" ? Range(rng, 0.15f, 0.4f) : Range(rng, 0.25f, 1f),
                });
            }
        }

        // External defects.
        var defects = DefectPlan(gem, rng);
        foreach (var kind in defects)
        {
            var normal = kind == FeatureKind.Chip
                ? new Vector3(Range(rng, -1f, 1f), Range(rng, -1f, 1f), Range(rng, -0.15f, 0.15f)).normalized     // girdle
                : RandomDirection(rng);
            if (kind == FeatureKind.Scratch && rng.NextDouble() < 0.6) normal = new Vector3(normal.x * 0.4f, normal.y * 0.4f, Mathf.Abs(normal.z) + 0.6f).normalized;

            features.Add(new LoupeFeature
            {
                Kind = kind,
                External = true,
                Position = normal,
                Direction = RandomDirection(rng),
                Size = Range(rng, 0.3f, 1f),
            });
        }

        return features;
    }

    // Features seen under the microscope: many more than with the loupe, including very fine ones (a "loupe clean"
    // stone still shows a few specks at 40x). Synthetic stones show curved growth striae instead of angular zoning.
    public static List<LoupeFeature> GenerateMicro(GemInstance gem)
    {
        var rng = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "micro"));
        var features = new List<LoupeFeature>();
        var def = gem.Definition;

        var opaque = def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque
                     || (!string.IsNullOrEmpty(def.transparency) && def.transparency.ToLowerInvariant().StartsWith("opaque"));

        if (!opaque)
        {
            var kinds = TypesFor(gem);
            int count;
            float minSize;
            switch (gem.ClarityGrade)
            {
                case "Loupe clean": count = 5; minSize = 0.05f; break;
                case "Eye-clean": count = 14; minSize = 0.1f; break;
                case "Inclus": count = 26; minSize = 0.15f; break;
                default: count = 44; minSize = 0.2f; break;
            }

            for (var i = 0; i < count; i++)
            {
                var kind = kinds[rng.Next(kinds.Count)];
                var small = gem.ClarityGrade == "Loupe clean" || gem.ClarityGrade == "Eye-clean";
                features.Add(new LoupeFeature
                {
                    Kind = kind,
                    External = false,
                    Position = new Vector3(Range(rng, -0.85f, 0.85f), Range(rng, -0.85f, 0.85f), Range(rng, -0.85f, 0.85f)),
                    Direction = RandomDirection(rng),
                    Size = small ? Range(rng, minSize, 0.45f) : Range(rng, minSize, 1f),
                });
            }

            // Growth structure: curved striae in a synthetic stone, a few angular growth bands in a natural one.
            var growthKind = gem.IsSynthetic ? FeatureKind.Striae : FeatureKind.Zoning;
            var growthCount = gem.IsSynthetic ? 4 : 2;
            for (var i = 0; i < growthCount; i++)
            {
                features.Add(new LoupeFeature
                {
                    Kind = growthKind,
                    External = false,
                    Position = new Vector3(Range(rng, -0.6f, 0.6f), Range(rng, -0.6f, 0.6f), Range(rng, -0.6f, 0.6f)),
                    Direction = RandomDirection(rng),
                    Size = Range(rng, 0.5f, 1f),
                });
            }
        }

        // Surface defects are the same as under the loupe, seen in more detail.
        foreach (var f in Generate(gem))
        {
            if (f.External) features.Add(f);
        }

        return features;
    }

    public static List<FeatureKind> TypesFor(GemInstance gem)
    {
        var text = ((gem.Definition.inclusions ?? "") + " " + (gem.InclusionDescription ?? "")).ToLowerInvariant();
        var result = new List<FeatureKind>();
        foreach (var (kind, keywords) in InclusionTypes)
        {
            foreach (var keyword in keywords)
            {
                if (!text.Contains(keyword)) continue;
                result.Add(kind);
                break;
            }
        }

        if (gem.TreatmentGrade == "Chauffé") result.Add(FeatureKind.Fluid);
        if (gem.TreatmentGrade == "Fracture comblée") result.Add(FeatureKind.Fracture);
        if (result.Count == 0)
        {
            result.Add(FeatureKind.Fluid);
            result.Add(FeatureKind.Crystal);
        }
        return result;
    }

    // Surface defects of a specimen: random but stable, and mostly minor.
    private static List<FeatureKind> DefectPlan(GemInstance gem, System.Random rng)
    {
        var plan = new List<FeatureKind>();
        if (gem.IsRough)
        {
            var natural = 5 + rng.Next(4);
            for (var i = 0; i < natural; i++) plan.Add(rng.NextDouble() < 0.6 ? FeatureKind.Pit : FeatureKind.Abrasion);
            return plan;
        }

        var count = rng.Next(0, 4);
        for (var i = 0; i < count; i++)
        {
            var roll = rng.NextDouble();
            plan.Add(roll < 0.45 ? FeatureKind.Scratch : roll < 0.65 ? FeatureKind.Chip : roll < 0.85 ? FeatureKind.Pit : FeatureKind.Abrasion);
        }
        return plan;
    }

    public static List<string> ExternalDefectLines(GemInstance gem)
    {
        var counts = new Dictionary<FeatureKind, int>();
        foreach (var f in Generate(gem))
        {
            if (!f.External) continue;
            counts[f.Kind] = counts.TryGetValue(f.Kind, out var n) ? n + 1 : 1;
        }

        var lines = new List<string>();
        if (gem.IsRough)
        {
            lines.Add("Surface brute naturelle : piqûres et zones d'usure.");
            return lines;
        }

        if (counts.Count == 0)
        {
            lines.Add("Aucun défaut externe visible à la loupe.");
            return lines;
        }

        if (counts.TryGetValue(FeatureKind.Scratch, out var scratches)) lines.Add(scratches == 1 ? "Une rayure fine" : $"{scratches} rayures fines");
        if (counts.TryGetValue(FeatureKind.Chip, out var chips)) lines.Add(chips == 1 ? "Un petit éclat au rondiste" : $"{chips} petits éclats au rondiste");
        if (counts.TryGetValue(FeatureKind.Pit, out var pits)) lines.Add(pits == 1 ? "Une piqûre de surface" : $"{pits} piqûres de surface");
        if (counts.TryGetValue(FeatureKind.Abrasion, out var abrasions)) lines.Add(abrasions == 1 ? "Facettes légèrement émoussées" : "Facettes émoussées par endroits");
        return lines;
    }

    public static string InclusionSummary(GemInstance gem)
    {
        switch (gem.ClarityGrade)
        {
            case "Loupe clean": return "Aucune inclusion visible à la loupe (10x).";
            case "Eye-clean": return "Quelques inclusions très fines, invisibles à l'œil nu.";
            case "Inclus": return "Inclusions bien visibles à la loupe.";
            default: return "Nombreuses inclusions, certaines visibles à l'œil nu.";
        }
    }

    private static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static Vector3 RandomDirection(System.Random rng)
    {
        var v = new Vector3(Range(rng, -1f, 1f), Range(rng, -1f, 1f), Range(rng, -1f, 1f));
        return v.sqrMagnitude < 1e-4f ? Vector3.right : v.normalized;
    }
}
