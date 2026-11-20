using System.Collections.Generic;

// The logo of a species: the shape and the cut its icon is drawn with (user, 2026-11-20). It does not change the shape of the stones: a stone keeps the shape it
// was cut in, only the picture of the species in the lists, the grimoire and the encyclopedia follows these rules.
//   beryls: emerald cut in the colour of the variety (the pezzottaite: cabochon) - corundums: oval - garnets: octagon - diamond, zircon, moissanite, YAG, GGG, CZ...: round brilliant
//   scapolites: trilliant - quartz: marquise (dutch cut), cabochon for the polycrystalline ones - fluorites: a cube - chrysoberyl and zoisite: cushion - opaque stones: cabochon
public struct GemIconStyleInfo
{
    public string Shape;
    public string Cut;
    public string Habit;       // a rough crystal drawn instead of a cut stone (the cube of the fluorite)
}

public static class GemIconStyle
{
    private static readonly HashSet<string> Beryls = new HashSet<string> { "Emeraude", "Aigue_Marine", "Beryl_Bleu", "Beryl_Vert", "Beryl_Rouge", "Goshenite", "Heliodore", "Morganite" };
    private static readonly HashSet<string> Quartz = new HashSet<string> { "Quartz", "Amethyste", "Ametrine", "Citrine", "Prasiolite", "Quartz_Fume", "Quartz_Rose" };
    private static readonly HashSet<string> Round = new HashSet<string> { "YAG", "GGG", "CZ", "Titanate_Strontium" };

    public static GemIconStyleInfo For(GemDefinition def)
    {
        var name = def != null ? def.name ?? "" : "";
        if (name.StartsWith("Fluorite")) return new GemIconStyleInfo { Shape = "Carré", Cut = "Brillant", Habit = "cube" };
        if (name == "Pezzottaite") return Cabochon("Ovale");
        if (Beryls.Contains(name)) return new GemIconStyleInfo { Shape = "Rectangle à pans coupés", Cut = "À degrés" };
        if (Quartz.Contains(name) && !(def != null && def.isPolycrystalline)) return new GemIconStyleInfo { Shape = "Marquise", Cut = "Brillant" };
        if (name.StartsWith("Scapolite")) return new GemIconStyleInfo { Shape = "Triangle", Cut = "Brillant" };
        if (name.StartsWith("Chrysoberyl") || name == "Zoisite" || name == "Tanzanite") return new GemIconStyleInfo { Shape = "Coussin", Cut = "Mixte" };
        if (name.StartsWith("Corindon") || name.StartsWith("Saphir") || name == "Padparadscha") return new GemIconStyleInfo { Shape = "Ovale", Cut = "Mixte" };
        if (name.StartsWith("Grenat")) return new GemIconStyleInfo { Shape = "Octogone", Cut = "Mixte" };
        if (name.StartsWith("Diamant") || name.StartsWith("Moissanite") || name.StartsWith("Zircon") || Round.Contains(name)) return new GemIconStyleInfo { Shape = "Rond", Cut = "Brillant" };
        // The polycrystalline quartz (agate, jasper, onyx, carnelian, tiger's eye, aventurine) and every opaque stone are cabochons.
        var polyQuartz = name == "Agate" || name == "Cornaline" || name == "Oeil_de_Tigre" || name == "Jaspe" || name == "Onyx" || name == "Aventurine";
        if (polyQuartz || (def != null && def.Diaphaneity == Diaphaneity.Opaque)) return Cabochon(def != null && def.cutStyle == "Cabochon" ? def.shape : "Ovale");
        return new GemIconStyleInfo { Shape = def != null ? def.shape : "", Cut = def != null ? def.cutStyle : "" };
    }

    private static GemIconStyleInfo Cabochon(string shape) => new GemIconStyleInfo { Shape = string.IsNullOrEmpty(shape) ? "Ovale" : shape, Cut = "Cabochon" };
}
