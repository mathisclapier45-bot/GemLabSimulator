using UnityEngine;

// What a poster is, for the library desk and the shelf of the bag: its title, its blurb and its price. A poster is read on a wall (later) or from the bag:
// a click opens it in a window (PosterViewerUI), and the picture is drawn by PosterArt.
public class PosterInfo
{
    public string Id;
    public string Title;
    public string Subtitle;
    public string Blurb;
    public int Price;
    public string Caption;           // rich text under the picture; null for the poster of the colours (built from the grid)
}

public static class PosterCatalog
{
    // The ids of the posters live in the same list as the ids of the books (the shelf of the bag keeps both).
    public const string Prefix = "poster:";
    public const string Wheels = Prefix + "roues";
    public const string Garnets = Prefix + "grenats";

    public static readonly PosterInfo[] Infos =
    {
        new PosterInfo
        {
            Id = Wheels, Title = "Les roues des couleurs", Subtitle = "La grille de couleur du laboratoire, en grand",
            Price = 600,
            Blurb = "Deux roues de 72 teintes sur huit degrés, du clair au sombre : l'une avec le nom que le jeu donne à chaque zone, l'autre avec les angles. Pour comparer une pierre à la couleur de référence.",
        },
        new PosterInfo
        {
            Id = Garnets, Title = "La famille des grenats", Subtitle = "Schéma de Mackowsky : pôles, séries et variétés",
            Price = 450,
            Blurb = "Les six pôles purs, les séries continues et discontinues qui les relient, et les variétés de chaque mélange avec leurs couleurs : rhodolite, malaia, démantoïde, tsavorite, hessonite..",
            Caption = "<b>Pyralspite</b> : almandin, pyrope, spessartite (aluminium). <b>Ugrandite</b> : uvarovite, grossulaire, andradite (calcium).\n<color=#9AA3B5>Trait plein : série continue, tous les mélanges existent. Pointillé : série discontinue. Chaque échantillon de couleur donne l'étendue de teintes de l'espèce ou de la variété.</color>",
        },
    };

    public static bool IsPoster(string id) => !string.IsNullOrEmpty(id) && id.StartsWith(Prefix);

    public static PosterInfo Find(string id)
    {
        foreach (var info in Infos) if (info.Id == id) return info;
        return null;
    }
}
