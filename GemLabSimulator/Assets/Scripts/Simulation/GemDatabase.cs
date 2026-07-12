using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GemDatabase", menuName = "GemLab/Gem Database")]
public class GemDatabase : ScriptableObject
{
    public List<GemDefinition> allGems = new List<GemDefinition>();

    private static GemDatabase instance;

    public static GemDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<GemDatabase>("GemDatabase");
            return instance;
        }
    }

    public GemDefinition FindByAssetName(string assetName)
    {
        foreach (var gem in allGems)
        {
            if (gem != null && gem.name == assetName) return gem;
        }
        return null;
    }
}
