// The Diamond Quarter tower: reception on the ground floor, six floors above, a rooftop reached from the top floor.
// The elevator takes the player to any floor once the diamond dealer licence has been bought at the reception.
public static class DiamondBuilding
{
    public const string LicenseId = "LicenceDiamantaire";
    public const int LicensePrice = 10000;

    public static readonly (string label, string scene)[] Floors =
    {
        ("Rez-de-chaussée — Accueil", "DiamantairesAccueil"),
        ("1er étage — Vendeurs de diamants", "Diamantaires1"),
        ("2e étage — Grading", "Diamantaires2"),
        ("3e étage — Diamants bruts", "Diamantaires3"),
        ("4e étage — Laboratoire de précision", "Diamantaires4"),
        ("5e étage — Centre de taille", "Diamantaires5"),
        ("6e étage — Bureau de Martin Rapaport", "Diamantaires6"),
    };

    // Temporary, to explore the tower freely: set to true to make the elevator require the licence again.
    public const bool LicenseRequiredForFloors = false;

    // Whether the elevator may take the player above the ground floor.
    public static bool FloorsUnlocked => !LicenseRequiredForFloors || HasLicense;

    public static bool HasLicense
    {
        get
        {
            var tools = PlayerOwnedTools.Ensure();
            return tools != null && tools.Has(LicenseId);
        }
    }
}
