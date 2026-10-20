using System.Collections.Generic;

// Field equipment sold at the GemStore that goes into the player's bag (H): name and short description of each item.
public static class BagItemCatalog
{
    private static readonly Dictionary<string, (string name, string description)> Items = new Dictionary<string, (string, string)>
    {
        { "Loupe10X", ("Loupe 10× (Hastings)", "Loupe de poche à triplet aplanétique, grossissement ×10 : l'outil de base pour observer une gemme sur le terrain.") },
        { "Loupe20X", ("Loupe 20× (triplet)", "Loupe de poche à triplet, grossissement ×20 : pour distinguer les fines inclusions, au prix d'un champ plus réduit et d'une mise au point plus délicate.") },
        { "DichroscopeCalcite", ("Dichroscope à calcite", "Deux fenêtres côte à côte à travers un cristal de calcite : on y compare les deux couleurs d'un pléochroïsme en faisant tourner la gemme.") },
        { "DichroscopeLondres", ("Dichroscope de Londres", "Dichroscope tubulaire classique (modèle londonien), à rhomboèdre de calcite : il sépare la lumière en deux faisceaux polarisés pour révéler le pléochroïsme.") },
        { "SpectroscopePoche", ("Spectroscope de poche", "Spectroscope à réseau de diffraction : décompose la lumière traversant la gemme et laisse voir ses bandes d'absorption.") },
        { "Maglite", ("Maglite", "Lampe torche compacte et puissante, indispensable pour éclairer une gemme (diaphanéité, inclusions, éclat) en dehors du laboratoire.") },
        { "Chamoisine", ("Chamoisine (gem cloth)", "Carré de feutre doux pour essuyer une gemme sans la rayer : poussières, traces de doigts et résidus gras disparaissent avant l'examen.") },
        { "AimantGemmologie", ("Aimant de gemmologie", "Aimant au néodyme très puissant : il attire les gemmes riches en fer (grenats, par exemple) et sert à un premier tri rapide sur le terrain.") },
        { "BalanceCaratPoche", ("Balance à carat de poche", "Petite balance électronique de précision, donnant le poids d'une gemme en carats : pratique pour peser une pierre n'importe où.") },
        { "LampeUVLongs", ("Lampe UV longs (365 nm)", "Torche à LED ultraviolettes de 365 nm : révèle la fluorescence de certaines gemmes (rubis, par exemple) dans la pénombre, hors du laboratoire.") },
        { "FiltreChelseaPortable", ("Filtre Chelsea", "Filtre dichromatique laissant passer le rouge profond et le vert-jaune, pour repérer certaines gemmes colorées par le chrome et démasquer des imitations.") },
    };

    public static string NameOf(string id) => Items.TryGetValue(id, out var item) ? item.name : id;

    public static string DescriptionOf(string id) => Items.TryGetValue(id, out var item) ? item.description : "";
}
