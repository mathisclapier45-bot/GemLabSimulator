using System.Collections.Generic;
using UnityEngine;

public static class GemGrading
{
    public struct Grade
    {
        public string Name;
        public float Weight;
        public float Multiplier;
        public string Description;
    }

    public static readonly Grade[] SaturationGrades =
    {
        new Grade { Name = "Pâle", Weight = 30f, Multiplier = 0.6f },
        new Grade { Name = "Claire", Weight = 25f, Multiplier = 0.8f },
        new Grade { Name = "Moyenne", Weight = 20f, Multiplier = 1.0f },
        new Grade { Name = "Intense", Weight = 15f, Multiplier = 1.4f },
        new Grade { Name = "Vive", Weight = 10f, Multiplier = 2.0f },
    };

    public static readonly Grade[] ClarityGrades =
    {
        new Grade { Name = "Fortement inclus", Weight = 25f, Multiplier = 0.5f, Description = "Nombreuses inclusions visibles à l'œil nu." },
        new Grade { Name = "Inclus", Weight = 30f, Multiplier = 0.8f, Description = "Inclusions visibles à la loupe, quelques-unes à l'œil nu." },
        new Grade { Name = "Eye-clean", Weight = 30f, Multiplier = 1.2f, Description = "Aucune inclusion visible à l'œil nu, quelques-unes à la loupe." },
        new Grade { Name = "Loupe clean", Weight = 15f, Multiplier = 1.8f, Description = "Aucune inclusion visible même à la loupe (10x)." },
    };

    public static Grade PickWeighted(Grade[] grades)
    {
        var total = 0f;
        foreach (var g in grades) total += g.Weight;

        var roll = Random.value * total;
        var cumulative = 0f;
        foreach (var g in grades)
        {
            cumulative += g.Weight;
            if (roll <= cumulative) return g;
        }
        return grades[grades.Length - 1];
    }

    public static GemOrigin PickOrigin(List<GemOrigin> origins)
    {
        if (origins == null || origins.Count == 0)
        {
            return new GemOrigin { name = "Origine inconnue", rarityWeight = 1f, valueMultiplier = 1f, inclusionOverride = "" };
        }

        var total = 0f;
        foreach (var o in origins) total += o.rarityWeight;

        var roll = Random.value * total;
        var cumulative = 0f;
        foreach (var o in origins)
        {
            cumulative += o.rarityWeight;
            if (roll <= cumulative) return o;
        }
        return origins[origins.Count - 1];
    }
}
