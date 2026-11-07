using System;
using System.Collections.Generic;
using UnityEngine;

public class GemCodex : MonoBehaviour
{
    public static GemCodex Instance { get; private set; }

    private readonly HashSet<string> discoveredNames = new HashSet<string>();

    public event Action OnCodexChanged;

    // What the player has found out about each species, from the stones of that species examined with the instruments (the keys of the observations and the
    // measurements, see GemInstance.RecordObservation). The page of the grimoire shows only these.
    private readonly Dictionary<string, HashSet<string>> known = new Dictionary<string, HashSet<string>>();

    public void NoteKnown(string species, string key)
    {
        if (string.IsNullOrEmpty(species) || string.IsNullOrEmpty(key)) return;
        if (!known.TryGetValue(species, out var set))
        {
            set = new HashSet<string>();
            known[species] = set;
        }
        if (set.Add(key)) OnCodexChanged?.Invoke();
    }

    public bool IsKnown(string species, string key)
    {
        return known.TryGetValue(species, out var set) && set.Contains(key);
    }

    public bool IsKnownAny(string species, string[] keys)
    {
        if (keys == null || keys.Length == 0) return true;
        foreach (var key in keys)
            if (IsKnown(species, key)) return true;
        return false;
    }

    // "species|key" for the save.
    public List<string> GetKnown()
    {
        var list = new List<string>();
        foreach (var pair in known)
            foreach (var key in pair.Value)
                list.Add(pair.Key + "|" + key);
        return list;
    }

    public void LoadKnown(List<string> entries)
    {
        known.Clear();
        if (entries == null) return;
        foreach (var entry in entries)
        {
            var cut = entry.IndexOf('|');
            if (cut <= 0) continue;
            NoteKnown(entry.Substring(0, cut), entry.Substring(cut + 1));
        }
    }

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
