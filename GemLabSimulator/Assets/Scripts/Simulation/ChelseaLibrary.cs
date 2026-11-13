using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChelseaEntry
{
    public string key;            // the name of the species, or of a variant (Spinelle_Bleu_Synthetique, Verre_Bleu_Cobalt...)
    public string source;
    public string reaction;       // what the player reads
    public string color;          // the colour the stone takes under the filter when it reacts fully (#RRGGBB)
    public float intensity;       // 0 = nothing red, 1 = the full colour
    public string dull;           // the colour of the stone when it does not react (default: dull green)
}

[Serializable]
public class ChelseaFile
{
    public List<ChelseaEntry> entries = new List<ChelseaEntry>();
}

// The reactions of the Chelsea filter, read from Assets/Resources/Chelsea/chelsea.json (the table of the diploma course). A species that is in the file
// uses its entry; the others keep the sentence written on their asset. To change a reaction, edit the JSON.
public static class ChelseaLibrary
{
    private static Dictionary<string, ChelseaEntry> entries;

    private static Dictionary<string, ChelseaEntry> Entries
    {
        get
        {
            if (entries != null) return entries;
            entries = new Dictionary<string, ChelseaEntry>();
            var asset = Resources.Load<TextAsset>("Chelsea/chelsea");
            if (asset == null) { Debug.LogWarning("Table du filtre Chelsea introuvable : Resources/Chelsea/chelsea.json"); return entries; }
            try
            {
                var file = JsonUtility.FromJson<ChelseaFile>(asset.text);
                if (file != null && file.entries != null)
                    foreach (var entry in file.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.key)) entries[entry.key] = entry;
            }
            catch (Exception) { Debug.LogWarning("Table du filtre Chelsea illisible : Resources/Chelsea/chelsea.json"); }
            return entries;
        }
    }

    public static ChelseaEntry Entry(string key) => !string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out var entry) ? entry : null;

    public static string Reaction(GemDefinition def)
    {
        if (def == null) return "";
        var entry = Entry(def.name);
        return entry != null && !string.IsNullOrEmpty(entry.reaction) ? entry.reaction : def.chelseaFilterReaction;
    }

    public static string Reaction(GemInstance gem)
    {
        if (gem == null) return "";
        var entry = EntryFor(gem);
        return entry != null && !string.IsNullOrEmpty(entry.reaction) ? entry.reaction : gem.Definition.chelseaFilterReaction;
    }

    // The entry that fits a stone: the one of its species, or of its variant (a synthetic blue spinel, a blue topaz, the kinds of glass...).
    public static ChelseaEntry EntryFor(GemInstance gem)
    {
        if (gem == null || gem.Definition == null) return null;
        var name = gem.Definition.name ?? "";
        var key = name;

        if (name == "Spinelle_Bleu" && gem.IsSynthetic) key = "Spinelle_Bleu_Synthetique";
        else if (name == "Jade_Jadeite" && gem.HasTreatment(TreatmentKind.Dyed)) key = "Jade_Jadeite_Teinte";
        else if (name == "Topaze" && gem.HasColor && gem.ColorHue > 190f && gem.ColorHue < 265f) key = "Topaze_Bleue";
        else if (name == "Verre" && gem.HasColor)
        {
            var hue = gem.ColorHue;
            if (hue < 25f || hue > 335f) key = "Verre_Rouge";
            else if (hue > 190f && hue < 265f) key = SpectrumLibrary.IsCobaltGlass(gem) ? "Verre_Bleu_Cobalt" : "Verre_Bleu_Fer";
            else if (hue > 75f && hue < 175f) key = "Verre_Vert";
        }
        return Entry(key);
    }

    public static Color ParseColor(string hex, Color fallback) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
}
