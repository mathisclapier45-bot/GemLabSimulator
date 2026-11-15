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
}

public static class PosterCatalog
{
    // The ids of the posters live in the same list as the ids of the books (the shelf of the bag keeps both).
    public const string Prefix = "poster:";
    public const string Wheels = Prefix + "roues";

    public static readonly PosterInfo[] Infos =
    {
        new PosterInfo
        {
            Id = Wheels, Title = "Les roues des couleurs", Subtitle = "La grille de couleur du laboratoire, en grand",
            Price = 600,
            Blurb = "Deux roues de 72 teintes sur huit degrés, du clair au sombre : l'une avec le nom que le jeu donne à chaque zone, l'autre avec les angles. Pour comparer une pierre à la couleur de référence.",
        },
    };

    public static bool IsPoster(string id) => !string.IsNullOrEmpty(id) && id.StartsWith(Prefix);

    public static PosterInfo Find(string id)
    {
        foreach (var info in Infos) if (info.Id == id) return info;
        return null;
    }
}
