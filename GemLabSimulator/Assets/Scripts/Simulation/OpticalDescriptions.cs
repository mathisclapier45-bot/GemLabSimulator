public static class OpticalDescriptions
{
    public static string PolariscopeText(GemDefinition gem)
    {
        if (gem.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque)
            return "Non applicable (minéral opaque).";
        if (gem.isPolycrystalline)
            return "Anisotrope — agrégat polycristallin (lecture diffuse, sans croix d'extinction nette).";

        switch (gem.opticalCharacterClass)
        {
            case GemDefinition.OpticalCharacterClass.Isotropic:
                return "Isotrope (réseau cubique ou matériau amorphe).";
            default:
                return "Anisotrope.";
        }
    }

    public static string ConoscopeText(GemDefinition gem)
    {
        if (gem.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque)
            return "Non applicable (minéral opaque).";
        if (gem.isPolycrystalline)
            return "Non applicable (agrégat polycristallin, aucune figure d'interférence exploitable).";

        switch (gem.opticalCharacterClass)
        {
            case GemDefinition.OpticalCharacterClass.Isotropic:
                return "Aucune figure d'interférence (gemme isotrope).";
            case GemDefinition.OpticalCharacterClass.Uniaxial:
                return gem.isQuartzFamily
                    ? "Figure uniaxe, avec l'\"œil-de-taureau\" (bull's eye) caractéristique du quartz : le centre de la croix est une tache ronde, pâle."
                    : "Figure uniaxe.";
            default:
                return "Figure biaxe.";
        }
    }

    public static string BirefringenceText(GemDefinition gem)
    {
        return gem.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque
            ? "Non applicable (minéral opaque)"
            : $"{gem.birefringence:F3}";
    }
}
