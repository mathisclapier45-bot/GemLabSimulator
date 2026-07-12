using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGem", menuName = "GemLab/Gem Definition")]
public class GemDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Propriétés physiques")]
    public float refractiveIndex;
    public float density;
    public float hardness;
    public float lengthMm;
    public float widthMm;
    public float depthMm;
    public float weightCarats;

    [Header("Examen visuel (loupe)")]
    public string luster;
    public string color;
    public string shape;
    public string cutStyle;
    public string transparency;

    [Header("Propriétés optiques qualitatives")]
    public bool pleochroic;
    [TextArea] public string pleochroicDescription;
    [TextArea] public string fluorescenceLongWave;
    [TextArea] public string fluorescenceShortWave;
    [TextArea] public string opticalCharacter;
    [TextArea] public string absorptionSpectrum;
    public List<SpectrumLine> spectrumLines = new List<SpectrumLine>();
    [TextArea] public string inclusions;
    [TextArea] public string interferenceFigure;
    [TextArea] public string chelseaFilterReaction;
    [TextArea] public string magnetism;

    [Header("Économie")]
    public int baseValue;

    [Header("Origines possibles")]
    public List<GemOrigin> origins = new List<GemOrigin>();
}
