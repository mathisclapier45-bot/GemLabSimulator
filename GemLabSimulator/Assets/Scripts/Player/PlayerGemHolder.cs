using System.Collections.Generic;
using UnityEngine;

public class PlayerGemHolder : MonoBehaviour
{
    public GemDefinition CurrentGem { get; private set; }
    public bool HasGem => CurrentGem != null;

    private readonly Dictionary<string, float> measuredValues = new Dictionary<string, float>();
    private readonly HashSet<string> observedProperties = new HashSet<string>();

    public void ReceiveGem(GemDefinition gem)
    {
        CurrentGem = gem;
        measuredValues.Clear();
        observedProperties.Clear();
        Debug.Log($"Gemme reçue : {gem.displayName}");
    }

    public void ClearGem()
    {
        CurrentGem = null;
        measuredValues.Clear();
        observedProperties.Clear();
    }

    public void RecordMeasurement(string propertyKey, float value)
    {
        measuredValues[propertyKey] = value;
    }

    public bool HasMeasured(string propertyKey)
    {
        return measuredValues.ContainsKey(propertyKey);
    }

    public float GetMeasuredValue(string propertyKey)
    {
        return measuredValues.TryGetValue(propertyKey, out var value) ? value : 0f;
    }

    public void RecordObservation(string propertyKey)
    {
        observedProperties.Add(propertyKey);
    }

    public bool HasObserved(string propertyKey)
    {
        return observedProperties.Contains(propertyKey);
    }
}
