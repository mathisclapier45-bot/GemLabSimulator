using UnityEngine;

// Per-specimen optical orientation, shared by every instrument so they all agree: the same stone has the same
// principal directions (and optic axes) whichever tool is used.
public static class StoneOptics
{
    public static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    public static float Unit(uint hash, int salt)
    {
        var h = hash ^ (uint)(salt * 2654435761u);
        h ^= h >> 13;
        h *= 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFF) / 65535f;
    }

    // Rotation from the stone's own frame to its optical frame: the optic axis (uniaxial) or the acute bisectrix
    // (biaxial) is the Z axis of this frame.
    public static Quaternion OpticFrame(GemInstance gem)
    {
        var h = StableHash(gem.SpecimenId ?? "");
        return Quaternion.Euler(Unit(h, 1) * 360f, Unit(h, 2) * 360f, Unit(h, 3) * 360f);
    }

    // Half the angle between the two optic axes of a biaxial species, in degrees.
    public static float HalfOpticAngle(GemDefinition def)
    {
        return Mathf.Lerp(12f, 34f, Unit(StableHash(def.name), 7));
    }
}
