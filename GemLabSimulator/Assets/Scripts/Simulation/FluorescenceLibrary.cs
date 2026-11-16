using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FluorescenceEntry
{
    public string key;                 // the name of the species, or of a variant (Spinelle_Bleu_Synthetique...)
    public string source;
    public string longText;            // what the player reads for the long-wave lamp (365 nm)
    public string longColor;           // #RRGGBB of the glow
    public float longIntensity;        // 0 = nothing
    public string shortText;           // ... and the short-wave lamp (254 nm)
    public string shortColor;
    public float shortIntensity;
    public bool phosphorescent;        // the glow lasts after the lamp
}

[Serializable]
public class FluorescenceFile
{
    public List<FluorescenceEntry> entries = new List<FluorescenceEntry>();
}

// The reactions to the UV lamps, read from Assets/Resources/Fluorescence/fluorescence.json (the table of the diploma course). A species that is in the file uses its entry;
// the others keep the sentences written on their asset. A synthetic stone uses the entry "<species>_Synthetique" when there is one. To change a reaction, edit the JSON.
public static class FluorescenceLibrary
{
    private static Dictionary<string, FluorescenceEntry> entries;

    private static Dictionary<string, FluorescenceEntry> Entries
    {
        get
        {
            if (entries != null) return entries;
            var loaded = new Dictionary<string, FluorescenceEntry>();
            var asset = Resources.Load<TextAsset>("Fluorescence/fluorescence");
            if (asset == null) { Debug.LogWarning("Table de fluorescence introuvable : Resources/Fluorescence/fluorescence.json"); return loaded; }
            try
            {
                var file = JsonUtility.FromJson<FluorescenceFile>(asset.text);
                if (file != null && file.entries != null)
                    foreach (var entry in file.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.key)) loaded[entry.key] = entry;
            }
            catch (Exception) { Debug.LogWarning("Table de fluorescence illisible : Resources/Fluorescence/fluorescence.json"); return loaded; }
            entries = loaded;
            return entries;
        }
    }

    public static IEnumerable<FluorescenceEntry> All => Entries.Values;

    public static FluorescenceEntry Entry(string key) => !string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out var entry) ? entry : null;

    // The entry of a stone: its species, or the synthetic variant of its species.
    public static FluorescenceEntry EntryFor(GemInstance gem)
    {
        if (gem == null || gem.Definition == null) return null;
        var name = gem.Definition.name ?? "";
        if (gem.IsSynthetic)
        {
            var synthetic = Entry(name + "_Synthetique");
            if (synthetic != null) return synthetic;
        }
        return Entry(name);
    }

    public static string Text(GemDefinition def, bool longWave)
    {
        if (def == null) return "";
        var entry = Entry(def.name);
        var text = entry != null ? (longWave ? entry.longText : entry.shortText) : null;
        return !string.IsNullOrEmpty(text) ? text : (longWave ? def.fluorescenceLongWave : def.fluorescenceShortWave);
    }

    public static string Text(GemInstance gem, bool longWave)
    {
        if (gem == null) return "";
        var entry = EntryFor(gem);
        var text = entry != null ? (longWave ? entry.longText : entry.shortText) : null;
        return !string.IsNullOrEmpty(text) ? text : (longWave ? gem.Definition.fluorescenceLongWave : gem.Definition.fluorescenceShortWave);
    }

    public static FluorescenceInfo Info(FluorescenceEntry entry, bool longWave)
    {
        var intensity = Mathf.Clamp01(longWave ? entry.longIntensity : entry.shortIntensity);
        var hex = longWave ? entry.longColor : entry.shortColor;
        var color = !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var parsed) ? parsed : new Color(0.78f, 0.88f, 1f);
        return new FluorescenceInfo { Glows = intensity >= 0.05f, Color = color, Intensity = intensity, Phosphorescent = entry.phosphorescent && intensity >= 0.05f };
    }

    public static FluorescenceInfo Info(GemDefinition def, bool longWave)
    {
        var entry = Entry(def.name);
        return entry != null ? Info(entry, longWave) : FluorescenceInfo.ForGem(def, longWave);
    }
}
