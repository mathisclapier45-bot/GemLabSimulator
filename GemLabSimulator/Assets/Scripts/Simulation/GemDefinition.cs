using UnityEngine;

[CreateAssetMenu(fileName = "NewGem", menuName = "GemLab/Gem Definition")]
public class GemDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;

    [Header("Propriétés physiques")]
    public float refractiveIndex;
    public float density;
    public float hardness;
    public float sizeMm;
    public float weightCarats;

    [Header("Propriétés optiques qualitatives")]
    public bool pleochroic;
    [TextArea] public string pleochroicDescription;
    [TextArea] public string fluorescenceLongWave;
    [TextArea] public string fluorescenceShortWave;
    [TextArea] public string opticalCharacter;
    [TextArea] public string absorptionSpectrum;
    [TextArea] public string inclusions;
    [TextArea] public string interferenceFigure;
    [TextArea] public string chelseaFilterReaction;

    [Header("Économie")]
    public int baseValue;
}
