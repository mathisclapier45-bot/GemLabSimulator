using System;
using System.Collections.Generic;
using UnityEngine;

public class GemCodex : MonoBehaviour
{
    public static GemCodex Instance { get; private set; }

    private readonly HashSet<string> discoveredNames = new HashSet<string>();

    public event Action OnCodexChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public bool IsDiscovered(string definitionAssetName)
    {
        return discoveredNames.Contains(definitionAssetName);
    }

    public void MarkDiscovered(string definitionAssetName)
    {
        if (!discoveredNames.Add(definitionAssetName)) return;
        OnCodexChanged?.Invoke();
    }

    public List<string> GetDiscoveredNames()
    {
        return new List<string>(discoveredNames);
    }

    public void LoadDiscovered(List<string> names)
    {
        discoveredNames.Clear();
        if (names != null)
            foreach (var name in names)
                discoveredNames.Add(name);
        OnCodexChanged?.Invoke();
    }
}
