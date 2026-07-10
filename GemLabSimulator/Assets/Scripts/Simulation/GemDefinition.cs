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

    [Header("Propriétés optiques qualitatives")]
    public bool pleochroic;
    [TextArea] public string pleochroicDescription;
    [TextArea] public string fluorescenceDescription;

    [Header("Économie")]
    public int baseValue;
}
