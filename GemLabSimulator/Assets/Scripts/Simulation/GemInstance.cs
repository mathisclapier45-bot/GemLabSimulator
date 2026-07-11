using System.Collections.Generic;

public class GemInstance
{
    public GemDefinition Definition { get; }
    public bool IsIdentified { get; private set; }

    private readonly Dictionary<string, float> measuredValues = new Dictionary<string, float>();
    private readonly HashSet<string> observedProperties = new HashSet<string>();

    public GemInstance(GemDefinition definition)
    {
        Definition = definition;
    }

    public void MarkIdentified()
    {
        IsIdentified = true;
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
