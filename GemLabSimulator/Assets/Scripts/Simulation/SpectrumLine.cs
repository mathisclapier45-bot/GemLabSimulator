using System;
using UnityEngine;

// One absorption line or band of a spectrum: where it sits (nm), how wide it is, how dark it is and how soft its edges are.
[Serializable]
public class SpectrumLine
{
    public float wavelengthNm;
    public float widthNm = 8f;
    public float depth = 1f;      // 1 = black, 0.3 = a faint line
    public float edgeNm = 0f;     // width of the soft edge on each side, 0 = automatic (the narrower the line, the sharper)

    public float Depth => depth > 0f ? Mathf.Clamp01(depth) : 1f;
}
