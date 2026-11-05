using System.Collections.Generic;
using UnityEngine;

public enum FeatureKind
{
    // Internal
    Crystal, Fluid, Bubble, Fracture, Cleavage, Healing, Needle, Zoning, Striae,
    // External (surface)
    Scratch, Chip, Pit, Abrasion,
    // Internal, added with the inclusion atlas
    TwoPhase, ThreePhase, NegativeCrystal, HorseTail, Feather, Growth, Twinning, Cloud,
    // The traces of the treatments of the laboratory: the channel of a laser, the fissures of a KM laser, the lenticular bursts of heated amber (internal);
    // the colour gathered in the cracks of a dyed stone, the pale lines of a coating, the pits of an acid, the colour along the edges of a diffusion (external)
    LaserChannel, KmFeather, Lenticular,
    DyeVein, CoatingWear, AcidEtch, EdgeColor,
}

public class LoupeFeature
{
    public FeatureKind Kind;
    // Which look of the kind (a crystal's mineral, a needle's family...): see InclusionShapes.
    public int Variant;
    public bool External;
    // Internal: position inside the stone, each axis in [-1, 1] of its half size.
    // External: unit vector pointing to the spot on the surface.
    public Vector3 Position;
    // Direction of the feature in the stone's own frame (unit vector), for elongated features.
    public Vector3 Direction;
    public float Size;     // 0..1
    public bool Heated;    // the stone was heated: halos around crystals, silk dissolved into dashes
    public bool Filled;    // fractures filled with glass or resin: the blue/orange flash
    public Color Tint = Color.white;    // the colour of a treatment trace (the dye in a crack, the diffused colour along an edge)
}

// What the 10x loupe shows of a stone: inclusions inside (number from its clarity, types from its species and its
// deposit) and defects on the surface (scratches, chips...). Everything is generated from the specimen so it is stable.
public static class LoupeFeatures
{
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

    private static bool IsOpaque(GemInstance gem)
    {
        var def = gem.Definition;
        return def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque
               || (!string.IsNullOrEmpty(def.transparency) && def.transparency.ToLowerInvariant().StartsWith("opaque"));
    }

    public static List<LoupeFeature> Generate(GemInstance gem)
    {
        var rng = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "loupe"));
        var features = new List<LoupeFeature>();

        // Internal inclusions: nothing visible inside an opaque stone.
        if (!IsOpaque(gem))
        {
            var small = gem.ClarityGrade == "Eye-clean";
            AddInternal(features, gem, rng, InternalCount(gem), 0.8f, 0.7f, small ? 0.15f : 0.25f, small ? 0.4f : 1f);
        }

        // What the treatments left in the stone, inside and on its surface.
        AddTreatmentFeatures(features, gem, true, true);

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

        HideBehindSetting(features, gem, false);
        return features;
    }

    // A stone in a jewel: the metal covers a part of it. Prongs hide little, a bezel the edge of the stone and the defects of the girdle, a closed back the whole
    // pavilion with the surface defects and the traces of the laser, the fillings and the coatings that are there. The microscope looks through the table, and
    // sees more of it. What is hidden is always the same for the same stone.
    private static void HideBehindSetting(List<LoupeFeature> features, GemInstance gem, bool micro)
    {
        if (gem == null || !gem.IsSet) return;
        var setting = JewelCatalog.SettingIndex(gem.JewelSetting);
        features.RemoveAll(f =>
        {
            var radial = Mathf.Sqrt(f.Position.x * f.Position.x + f.Position.y * f.Position.y);
            var low = f.Position.z < 0f;
            var hash = StoneOptics.StableHash((gem.SpecimenId ?? "") + "|hide|" + f.Kind + "|" + f.Position.x.ToString("0.00") + f.Position.y.ToString("0.00"));
            var roll = (hash % 100u) / 100f;
            switch (setting)
            {
                case 0: return f.External && radial > 0.72f && roll < 0.35f;
                case 1: return (radial > 0.7f && roll < (micro ? 0.5f : 0.85f)) || (f.External && f.Kind == FeatureKind.Chip);
                case 2:
                    if (f.External) return true;
                    if (f.Kind == FeatureKind.LaserChannel || f.Kind == FeatureKind.KmFeather) return !micro || roll < 0.7f;
                    return low && (!micro || roll < 0.6f);
                default: return false;
            }
        });
    }

    // Features seen under the microscope: many more than with the loupe, including very fine ones (a "loupe clean"
    // stone still shows a few specks at 40x). Synthetic stones show curved growth striae instead of angular marks.
    public static List<LoupeFeature> GenerateMicro(GemInstance gem)
    {
        var rng = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "micro"));
        var features = new List<LoupeFeature>();

        if (!IsOpaque(gem))
        {
            int count;
            float minSize;
            switch (gem.ClarityGrade)
            {
                case "Loupe clean": count = 5; minSize = 0.05f; break;
                case "Eye-clean": count = 14; minSize = 0.1f; break;
                case "Inclus": count = 26; minSize = 0.15f; break;
                default: count = 44; minSize = 0.2f; break;
            }

            var small = gem.ClarityGrade == "Loupe clean" || gem.ClarityGrade == "Eye-clean";
            AddInternal(features, gem, rng, count, 0.85f, 0.85f, minSize, small ? 0.45f : 1f);
            AddTreatmentFeatures(features, gem, false, true);

            // Growth structure: curved striae in a synthetic stone, angular growth marks and zones in a natural one.
            var profile = InclusionCatalog.ProfileFor(gem);
            if (gem.IsSynthetic)
            {
                for (var i = 0; i < 4; i++) AddGrowth(features, rng, FeatureKind.Striae, 0);
            }
            else
            {
                var zoned = profile.Share(FeatureKind.Zoning) > 0.08f || profile.Share(FeatureKind.Growth) > 0.08f;
                AddGrowth(features, rng, FeatureKind.Growth, 0);
                AddGrowth(features, rng, zoned ? FeatureKind.Zoning : FeatureKind.Growth, rng.NextDouble() < 0.35 ? 1 : 0);
            }
        }

        // Surface defects are the same as under the loupe, seen in more detail.
        foreach (var f in Generate(gem))
        {
            if (f.External) features.Add(f);
        }

        HideBehindSetting(features, gem, true);
        return features;
    }

    private static void AddGrowth(List<LoupeFeature> features, System.Random rng, FeatureKind kind, int variant)
    {
        features.Add(new LoupeFeature
        {
            Kind = kind,
            Variant = variant,
            External = false,
            Position = new Vector3(Range(rng, -0.6f, 0.6f), Range(rng, -0.6f, 0.6f), Range(rng, -0.6f, 0.6f)),
            Direction = RandomDirection(rng),
            Size = Range(rng, 0.5f, 1f),
        });
    }

    // Picks the inclusions of a stone from its profile. Needles, twin planes and growth marks of one stone follow the
    // same crystal axes (at 60° to each other); crystals and cavities sometimes sit in groups.
    private static void AddInternal(List<LoupeFeature> features, GemInstance gem, System.Random rng, int count,
        float spreadXY, float spreadZ, float minSize, float maxSize)
    {
        var profile = InclusionCatalog.ProfileFor(gem);
        var axisAngle = (float)(new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "axes")).NextDouble()) * 180f;
        var heated = gem.HasTreatment(TreatmentKind.Heated);
        var filled = gem.HasTreatment(TreatmentKind.Filled);
        var previous = new Dictionary<FeatureKind, Vector3>();

        for (var i = 0; i < count; i++)
        {
            var (kind, variant) = profile.Pick(rng);
            if (heated && rng.NextDouble() < 0.12) { kind = FeatureKind.Crystal; variant = (int)CrystalVariant.Zircon; }
            if (filled && rng.NextDouble() < 0.15) { kind = FeatureKind.Fracture; variant = 1; }

            var position = new Vector3(Range(rng, -spreadXY, spreadXY), Range(rng, -spreadXY, spreadXY), Range(rng, -spreadZ, spreadZ));
            var groups = kind == FeatureKind.Crystal || kind == FeatureKind.Fluid || kind == FeatureKind.Bubble
                         || kind == FeatureKind.TwoPhase || kind == FeatureKind.ThreePhase || kind == FeatureKind.NegativeCrystal;
            if (groups && previous.TryGetValue(kind, out var near) && rng.NextDouble() < 0.4)
                position = near + new Vector3(Range(rng, -0.25f, 0.25f), Range(rng, -0.25f, 0.25f), Range(rng, -0.2f, 0.2f));
            position = new Vector3(Mathf.Clamp(position.x, -0.9f, 0.9f), Mathf.Clamp(position.y, -0.9f, 0.9f), Mathf.Clamp(position.z, -0.9f, 0.9f));
            previous[kind] = position;

            var aligned = kind == FeatureKind.Needle || kind == FeatureKind.Twinning || kind == FeatureKind.Growth || kind == FeatureKind.Zoning
                          || kind == FeatureKind.Cleavage || kind == FeatureKind.TwoPhase || kind == FeatureKind.ThreePhase
                          || (kind == FeatureKind.Crystal && (variant == (int)CrystalVariant.Apatite || variant == (int)CrystalVariant.Rutile));

            features.Add(new LoupeFeature
            {
                Kind = kind,
                Variant = variant,
                External = false,
                Position = position,
                Direction = aligned ? AlignedDirection(axisAngle, rng) : RandomDirection(rng),
                Size = Range(rng, minSize, maxSize),
                Heated = heated,
                Filled = filled,
            });
        }
    }

    // The traces of the treatments, always at the same places for the same stone. includeInternal adds what is inside (the loupe and the microscope show
    // it), includeExternal what is on the surface.
    private static void AddTreatmentFeatures(List<LoupeFeature> features, GemInstance gem, bool includeExternal, bool includeInternal)
    {
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return;
        var rng = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "|treatment-traces"));
        var opaque = IsOpaque(gem);
        var color = gem.BodyColor;
        Color.RGBToHSV(color, out var h, out var s, out var v);
        var dye = Color.HSVToRGB(h, Mathf.Clamp01(s + 0.2f), Mathf.Clamp(v * 0.8f, 0.15f, 1f));
        var description = gem.TreatmentDescription ?? "";

        if (includeInternal && !opaque)
        {
            // The laser: one to three straight channels from the surface to a bleached inclusion; the KM laser: zig-zag or spiral fissures.
            if (gem.HasTreatment(TreatmentKind.Laser))
            {
                var km = gem.LaserName != null && gem.LaserName.Contains("KM");
                var count = 1 + (int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|laser-count") % 3);
                for (var i = 0; i < count; i++)
                {
                    features.Add(new LoupeFeature
                    {
                        Kind = km ? FeatureKind.KmFeather : FeatureKind.LaserChannel,
                        Variant = km ? rng.Next(2) : 0,
                        External = false,
                        Position = new Vector3(Range(rng, -0.45f, 0.45f), Range(rng, -0.45f, 0.45f), Range(rng, 0.05f, 0.45f)),
                        Direction = km ? RandomDirection(rng) : new Vector3(Range(rng, -0.12f, 0.12f), Range(rng, -0.12f, 0.12f), 1f).normalized,
                        Size = Range(rng, 0.5f, 1f),
                    });
                }
            }

            // The fissures of a heating that was cooled too fast, and the lenticular bursts of an amber that was cooled fast on purpose.
            if (gem.HasTreatment(TreatmentKind.Heated))
            {
                var lenticular = description.Contains("lenticulaires");
                var cracked = description.Contains("fissures de tension");
                for (var i = 0; i < (lenticular ? 4 : 0) + (cracked ? 2 : 0); i++)
                {
                    features.Add(new LoupeFeature
                    {
                        Kind = lenticular && i < 4 ? FeatureKind.Lenticular : FeatureKind.Fracture,
                        External = false,
                        Position = new Vector3(Range(rng, -0.6f, 0.6f), Range(rng, -0.6f, 0.6f), Range(rng, -0.4f, 0.4f)),
                        Direction = RandomDirection(rng),
                        Size = Range(rng, 0.4f, 0.9f),
                        Heated = true,
                    });
                }
            }
        }

        if (!includeExternal) return;

        // On the surface: the colour of a dye gathered in the cracks and the pores, the pale lines of a coating, the pits of an acid, the colour of a
        // diffusion along the edges of the facets.
        var wanted = new List<FeatureKind>();
        if (gem.HasTreatment(TreatmentKind.Dyed)) AddKinds(wanted, FeatureKind.DyeVein, 3 + rng.Next(3));
        if (gem.HasTreatment(TreatmentKind.Coated)) AddKinds(wanted, FeatureKind.CoatingWear, 3 + rng.Next(3));
        if (gem.HasTreatment(TreatmentKind.Bleached)) AddKinds(wanted, FeatureKind.AcidEtch, 3 + rng.Next(3));
        if (gem.HasTreatment(TreatmentKind.Diffused) && gem.DiffusionDepthMm < 0.5f) AddKinds(wanted, FeatureKind.EdgeColor, 4 + rng.Next(3));
        foreach (var kind in wanted)
        {
            var normal = RandomDirection(rng);
            normal = new Vector3(normal.x * 0.7f, normal.y * 0.7f, Mathf.Abs(normal.z) + 0.55f).normalized;
            features.Add(new LoupeFeature
            {
                Kind = kind,
                External = true,
                Position = normal,
                Direction = RandomDirection(rng),
                Size = Range(rng, 0.35f, 1f),
                Tint = kind == FeatureKind.DyeVein || kind == FeatureKind.EdgeColor ? dye : Color.white,
            });
        }
    }

    private static void AddKinds(List<FeatureKind> list, FeatureKind kind, int count)
    {
        for (var i = 0; i < count; i++) list.Add(kind);
    }

    private static Vector3 AlignedDirection(float axisAngle, System.Random rng)
    {
        var angle = (axisAngle + rng.Next(3) * 60f + Range(rng, -3f, 3f)) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), Range(rng, -0.15f, 0.15f)).normalized;
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

        // The traces of the treatments.
        if (counts.ContainsKey(FeatureKind.DyeVein)) lines.Add("Couleur concentrée dans les fissures et les pores de la surface");
        if (counts.ContainsKey(FeatureKind.CoatingWear)) lines.Add("Lignes et taches plus pâles sur les arêtes des facettes (couche mince rayée)");
        if (counts.ContainsKey(FeatureKind.AcidEtch)) lines.Add("Surface finement piquée et mate, comme rongée");
        if (counts.ContainsKey(FeatureKind.EdgeColor)) lines.Add("Couleur plus soutenue le long des arêtes de facettes");
        return lines;
    }

    // What the treatments left on the surface of the stone, in words (the loupe and the microscope list them with the other defects).
    public static List<string> TreatmentTraceLines(GemInstance gem)
    {
        var lines = new List<string>();
        var seen = new HashSet<FeatureKind>();
        foreach (var f in Generate(gem))
        {
            if (!f.External || !seen.Add(f.Kind)) continue;
            switch (f.Kind)
            {
                case FeatureKind.DyeVein: lines.Add("couleur concentrée dans les fissures et les pores (teinture)"); break;
                case FeatureKind.CoatingWear: lines.Add("lignes et taches pâles sur les arêtes des facettes (revêtement rayé)"); break;
                case FeatureKind.AcidEtch: lines.Add("surface finement piquée et mate (attaque acide)"); break;
                case FeatureKind.EdgeColor: lines.Add("couleur concentrée le long des arêtes de facettes (diffusion en surface)"); break;
            }
        }
        return lines;
    }

    // The kinds of inclusions a stone carries, most numerous first, as seen with the loupe or the microscope.
    public static List<string> InclusionNames(GemInstance gem, bool micro, bool withCounts)
    {
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        foreach (var f in micro ? GenerateMicro(gem) : Generate(gem))
        {
            if (f.External) continue;
            var label = InclusionShapes.Label(f.Kind, f.Variant);
            if (!counts.ContainsKey(label))
            {
                counts[label] = 0;
                order.Add(label);
            }
            counts[label]++;
        }

        order.Sort((a, b) => counts[b].CompareTo(counts[a]));
        var names = new List<string>();
        foreach (var label in order) names.Add(withCounts ? $"{label} ({counts[label]})" : label);
        return names;
    }

    public static string InclusionSummary(GemInstance gem)
    {
        string head;
        switch (gem.ClarityGrade)
        {
            case "Loupe clean": head = "Aucune inclusion visible à la loupe (10x)."; break;
            case "Eye-clean": head = "Quelques inclusions très fines, invisibles à l'œil nu."; break;
            case "Inclus": head = "Inclusions bien visibles à la loupe."; break;
            default: head = "Nombreuses inclusions, certaines visibles à l'œil nu."; break;
        }

        var names = InclusionNames(gem, false, false);
        if (names.Count > 4) names.RemoveRange(4, names.Count - 4);
        if (names.Count > 0) head += " Observées : " + string.Join(", ", names) + ".";

        if (gem.AsterismRays > 0)
            head += $" Astérisme à {(gem.AsterismRays == 6 ? "six" : "quatre")} branches (soie dense).";
        return head;
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
