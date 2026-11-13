using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpectrumEntry
{
    public string key;               // the name of the species, or of a variant (Zircon_Chauffe, Verre_Rouge_Se...)
    public string source;            // where the spectrum was drawn from (a note, not used by the game)
    public string description;
    public List<SpectrumLine> lines = new List<SpectrumLine>();
}

[Serializable]
public class SpectrumFile
{
    public List<SpectrumEntry> entries = new List<SpectrumEntry>();
}

// The spectra of the spectroscope, read from Assets/Resources/Spectra/spectra.json (drawn from the plates of the diploma course). A species that is in the file uses
// its entry; the others keep the lines written on their asset. To change a spectrum, edit the JSON: no code and no scene to regenerate.
public static class SpectrumLibrary
{
    private static Dictionary<string, SpectrumEntry> entries;

    private static Dictionary<string, SpectrumEntry> Entries
    {
        get
        {
            if (entries != null) return entries;
            entries = new Dictionary<string, SpectrumEntry>();
            var asset = Resources.Load<TextAsset>("Spectra/spectra");
            if (asset == null) { Debug.LogWarning("Spectres introuvables : Resources/Spectra/spectra.json"); return entries; }
            try
            {
                var file = JsonUtility.FromJson<SpectrumFile>(asset.text);
                if (file != null && file.entries != null)
                    foreach (var entry in file.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.key)) entries[entry.key] = entry;
            }
            catch (Exception) { Debug.LogWarning("Spectres illisibles : Resources/Spectra/spectra.json"); }
            return entries;
        }
    }

    public static SpectrumEntry Entry(string key) => !string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out var entry) ? entry : null;

    // The lines of a species (the ones of the JSON, else the ones of the asset).
    public static List<SpectrumLine> Lines(GemDefinition def)
    {
        if (def == null) return new List<SpectrumLine>();
        var entry = Entry(def.name);
        return entry != null ? entry.lines : def.spectrumLines;
    }

    public static string Description(GemDefinition def)
    {
        if (def == null) return "";
        var entry = Entry(def.name);
        return entry != null && !string.IsNullOrEmpty(entry.description) ? entry.description : def.absorptionSpectrum;
    }

    // A blue glass is coloured by cobalt or by iron, told apart by the spectroscope and the Chelsea filter.
    public static bool IsCobaltGlass(GemInstance gem) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|verre bleu") % 2 == 0;

    // The entry that fits a stone: the spectrum of its species, or of its variant (a heated zircon, a dyed jadeite, the three kinds of red glass...).
    public static SpectrumEntry EntryFor(GemInstance gem, out bool variant)
    {
        variant = false;
        if (gem == null || gem.Definition == null) return null;

        var name = gem.Definition.name ?? "";
        string key = name;
        if (name == "Zircon" && gem.HasTreatment(TreatmentKind.Heated)) key = "Zircon_Chauffe";
        else if (name == "Jade_Jadeite" && gem.HasTreatment(TreatmentKind.Dyed) && gem.DyeName == "Vert chrome") key = "Jade_Jadeite_Teinte";
        else if (name == "Verre" && gem.HasColor)
        {
            var hue = gem.ColorHue;
            if (hue < 25f || hue > 335f)
            {
                // The three recipes of red glass, told apart by the spectroscope: gold, selenium, rare earths.
                var kinds = new[] { "Verre_Rouge_Au", "Verre_Rouge_Se", "Verre_Rouge_TR" };
                key = kinds[(int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|verre") % (uint)kinds.Length)];
            }
            else if (hue > 190f && hue < 265f && IsCobaltGlass(gem)) key = "Verre_Bleu_Cobalt";
        }

        variant = key != name;
        return Entry(key);
    }
}
