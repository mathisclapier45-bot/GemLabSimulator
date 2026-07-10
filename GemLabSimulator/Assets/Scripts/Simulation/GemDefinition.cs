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

    [Header("Économie")]
    public int baseValue;
}
