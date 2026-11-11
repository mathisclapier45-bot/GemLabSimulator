using System.Collections.Generic;
using UnityEngine;

// The interference figures of the conoscope, drawn pixel by pixel for the lessons (the same model as the polariscope of the game: dark brushes where the vibration direction
// matches a polarizer, coloured rings where the light is retarded). A uniaxial stone has one optic axis (c1), a biaxial one has two (c1 and c2), in a field that is the unit circle;
// a quartz shows, in its middle, a pale round spot (the bull's eye) instead of the sharp crossing of the cross.
public static class InterferenceImages
{
    public const int Size = 112;

    private class Entry
    {
        public Texture2D Texture;
        public Sprite Sprite;
        public bool Filled;
    }

    private static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();

    // The sprite of a figure. `animated` figures are drawn again at each call; the others only the first time.
    public static Sprite Get(string key, bool animated, Vector2 c1, Vector2 c2, bool biaxial, bool bullsEye, float edgeRetardation)
    {
        if (!cache.TryGetValue(key, out var entry) || entry.Texture == null)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            entry = new Entry { Texture = texture, Sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f) };
            cache[key] = entry;
        }

        if (animated || !entry.Filled)
        {
            Fill(entry.Texture, c1, c2, biaxial, bullsEye, edgeRetardation);
            entry.Filled = true;
        }
        return entry.Sprite;
    }

    private static float Channel(float retardationNm, float wavelengthNm)
    {
        var s = Mathf.Sin(Mathf.PI * retardationNm / wavelengthNm);
        return s * s;
    }

    private static void Fill(Texture2D texture, Vector2 c1, Vector2 c2, bool biaxial, bool bullsEye, float edgeRetardation)
    {
        var pixels = new Color32[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var u = (x + 0.5f) / Size * 2f - 1f;
                var v = (y + 0.5f) / Size * 2f - 1f;
                var r = Mathf.Sqrt(u * u + v * v);
                if (r >= 1f)
                {
                    pixels[y * Size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                var p = new Vector2(u, v);
                var rel1 = p - c1;
                float retardation, psi;
                if (!biaxial)
                {
                    retardation = edgeRetardation * rel1.sqrMagnitude;
                    psi = Mathf.Atan2(rel1.y, rel1.x);
                }
                else
                {
                    var rel2 = p - c2;
                    retardation = edgeRetardation * rel1.magnitude * rel2.magnitude;
                    psi = (Mathf.Atan2(rel1.y, rel1.x) + Mathf.Atan2(rel2.y, rel2.x)) * 0.5f;
                }

                var brush = Mathf.Pow(Mathf.Sin(2f * psi), 2f);
                var cr = Channel(retardation, 650f) * brush;
                var cg = Channel(retardation, 550f) * brush;
                var cb = Channel(retardation, 450f) * brush;

                // The bull's eye of the quartz: the middle of the cross is a pale round spot.
                if (bullsEye && !biaxial)
                {
                    var spot = Mathf.Exp(-rel1.sqrMagnitude / 0.012f);
                    cr = Mathf.Max(cr, spot * 0.80f);
                    cg = Mathf.Max(cg, spot * 0.72f);
                    cb = Mathf.Max(cb, spot * 0.78f);
                }

                var alpha = Mathf.Clamp01((1f - r) * Size * 0.5f);
                pixels[y * Size + x] = new Color32(
                    (byte)(Mathf.Clamp01(cr * 1.2f + 0.02f) * 255f),
                    (byte)(Mathf.Clamp01(cg * 1.2f + 0.02f) * 255f),
                    (byte)(Mathf.Clamp01(cb * 1.2f + 0.03f) * 255f),
                    (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
    }
}
