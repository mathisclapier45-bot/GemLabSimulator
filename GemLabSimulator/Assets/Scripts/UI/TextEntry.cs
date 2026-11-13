using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// True while the player is typing in a text field (the search box of the report, the name of a save...): the keys of the game (E to talk, B, C, G, H, J, K for the
// windows) and the keys that move the character must not react to the letters being typed.
public static class TextEntry
{
    public static bool Typing
    {
        get
        {
            var system = EventSystem.current;
            if (system == null) return false;
            var selected = system.currentSelectedGameObject;
            if (selected == null) return false;
            var field = selected.GetComponent<InputField>();
            return field != null && field.isFocused;
        }
    }

    // Lower case, no accents: "Péridot" and "peridot" are the same word; "œ" is "oe". For the sort and the search of the lists.
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var decomposed = text.ToLowerInvariant().Replace("œ", "oe").Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) builder.Append(c);
        return builder.ToString();
    }

    // A key of the game: pressed this frame, and not typed into a text field.
    public static bool KeyDown(KeyCode key) => !Typing && Input.GetKeyDown(key);
}
