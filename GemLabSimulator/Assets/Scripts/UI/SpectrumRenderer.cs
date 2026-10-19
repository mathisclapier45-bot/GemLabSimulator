using System.Collections.Generic;
using UnityEngine;

public static class SpectrumRenderer
{
    public const float MinNm = 400f;
    public const float MaxNm = 700f;
    private const int TextureWidth = 300;
    private const int TextureHeight = 40;

    public static Sprite BuildSpectrumSprite(List<SpectrumLine> lines)
    {
        return BuildSpectrumSprite(lines, TextureWidth, TextureHeight);
    }

    public static Sprite BuildSpectrumSprite(List<SpectrumLine> lines, int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (var x = 0; x < width; x++)
        {
            var t = x / (float)(width - 1);
            var wavelength = Mathf.Lerp(MinNm, MaxNm, t);
            var baseColor = WavelengthToColor(wavelength);

            var darkness = 0f;
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    var dist = Mathf.Abs(wavelength - line.wavelengthNm);
                    var halfWidth = Mathf.Max(line.widthNm, 1f) * 0.5f;
                    if (dist <= halfWidth)
                        darkness = Mathf.Max(darkness, 1f - dist / halfWidth);
                }
            }

            var finalColor = Color.Lerp(baseColor, Color.black, darkness * 0.85f);
            for (var y = 0; y < height; y++)
                tex.SetPixel(x, y, finalColor);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Color WavelengthToColor(float wavelength)
    {
        float r, g, b;

        if (wavelength < 440f)
        {
            r = -(wavelength - 440f) / (440f - 400f);
            g = 0f;
            b = 1f;
        }
        else if (wavelength < 490f)
        {
            r = 0f;
            g = (wavelength - 440f) / (490f - 440f);
            b = 1f;
        }
        else if (wavelength < 510f)
        {
            r = 0f;
            g = 1f;
            b = -(wavelength - 510f) / (510f - 490f);
        }
        else if (wavelength < 580f)
        {
            r = (wavelength - 510f) / (580f - 510f);
            g = 1f;
            b = 0f;
        }
        else if (wavelength < 645f)
        {
            r = 1f;
            g = -(wavelength - 645f) / (645f - 580f);
            b = 0f;
        }
        else
        {
            r = 1f;
            g = 0f;
            b = 0f;
        }

        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
    }
}
