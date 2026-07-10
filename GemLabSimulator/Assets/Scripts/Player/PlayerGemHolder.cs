using System.Collections.Generic;
using UnityEngine;

public class PlayerGemHolder : MonoBehaviour
{
    public GemDefinition CurrentGem { get; private set; }
    public bool HasGem => CurrentGem != null;

    private readonly HashSet<string> measuredProperties = new HashSet<string>();

    public void ReceiveGem(GemDefinition gem)
    {
        CurrentGem = gem;
        measuredProperties.Clear();
        Debug.Log($"Gemme reçue : {gem.displayName}");
    }

    public void RecordMeasurement(string propertyKey)
    {
        measuredProperties.Add(propertyKey);
    }

    public void ClearGem()
    {
        CurrentGem = null;
        measuredProperties.Clear();
    }

    public bool HasMeasured(string propertyKey)
    {
        return measuredProperties.Contains(propertyKey);
    }
}
