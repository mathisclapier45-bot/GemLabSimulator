using System.Collections.Generic;
using System.Linq;

// A crystal habit: the shape a crystal has when it grows free, before it is cut (an octahedron for a diamond, a rhombic dodecahedron for a garnet...).
public class CrystalHabit
{
    public string Id;
    public string Name;
    public string Description;
    public string Short;             // for a caption under a small picture
}

public struct HabitChance
{
    public CrystalHabit Habit;
    public int Weight;
}

// The habits of each species, and the one a rough stone has. A species that does not grow in crystals (agate, jade, amber, pearls...) has none: its rough
// stones stay lumps. The habit of a rough stone is chosen from its number, so it is always the same.
public static class CrystalHabits
{
    public static readonly CrystalHabit[] All =
    {
        new CrystalHabit { Id = "octahedron", Short = "Octaèdre", Name = "Octaèdre", Description = "Huit faces triangulaires (système cubique) : l'habitus du diamant et du spinelle." },
        new CrystalHabit { Id = "octahedron_trigons", Short = "Octaèdre à trigones", Name = "Octaèdre à trigones", Description = "Octaèdre dont les faces portent des trigones : de petites figures de corrosion triangulaires, en creux et retournées par rapport à la face. C'est la marque de surface typique du diamant brut." },
        new CrystalHabit { Id = "cube", Short = "Cube", Name = "Cube", Description = "Six faces carrées (système cubique) : la fluorite, la pyrite ; le diamant de synthèse CVD pousse en blocs cubiques." },
        new CrystalHabit { Id = "cuboctahedron", Short = "Cuboctaèdre", Name = "Cuboctaèdre", Description = "Quatorze faces, six carrées et huit triangulaires : la forme des diamants de synthèse HPHT." },
        new CrystalHabit { Id = "rhombic_dodecahedron", Short = "Dodécaèdre rhombique", Name = "Dodécaèdre rhombique", Description = "Douze faces en losange (système cubique) : l'habitus typique du grenat." },
        new CrystalHabit { Id = "trapezohedron", Short = "Icositétraèdre", Name = "Icositétraèdre (trapézoèdre)", Description = "Vingt-quatre faces en cerf-volant (système cubique) : le second habitus des grenats." },
        new CrystalHabit { Id = "hex_prism", Short = "Prisme hexagonal", Name = "Prisme hexagonal", Description = "Prisme à six pans terminé par une face plate : les béryls (l'émeraude), l'apatite." },
        new CrystalHabit { Id = "quartz_prism", Short = "Prisme du quartz", Name = "Prisme hexagonal à terminaison rhomboédrique", Description = "Prisme à six pans, strié, terminé en pointe : les quartz." },
        new CrystalHabit { Id = "hex_bipyramid", Short = "Bipyramide hexagonale", Name = "Bipyramide hexagonale", Description = "Deux pyramides à six faces accolées, en tonneau : les saphirs." },
        new CrystalHabit { Id = "hex_tabular", Short = "Plaque hexagonale", Name = "Plaque hexagonale", Description = "Cristal aplati à six côtés : le rubis." },
        new CrystalHabit { Id = "tetragonal_prism", Short = "Prisme tétragonal", Name = "Prisme tétragonal bipyramidé", Description = "Prisme à section carrée terminé par une pyramide : le zircon." },
        new CrystalHabit { Id = "ortho_prism", Short = "Prisme orthorhombique", Name = "Prisme orthorhombique", Description = "Prisme à section rectangulaire ou losangique, aux terminaisons obliques : la topaze, le péridot, le chrysobéryl." },
        new CrystalHabit { Id = "monoclinic_prism", Short = "Prisme monoclinique", Name = "Prisme monoclinique", Description = "Prisme aplati aux terminaisons obliques : le spodumène (kunzite), les feldspaths, le diopside." },
        new CrystalHabit { Id = "trigonal_prism", Short = "Prisme trigonal", Name = "Prisme trigonal", Description = "Prisme à section triangulaire aux angles arrondis, strié : les tourmalines." },
        new CrystalHabit { Id = "rhombohedron", Short = "Rhomboèdre", Name = "Rhomboèdre", Description = "Six faces en losange, comme un cube écrasé : la rhodochrosite." },
    };

    public static CrystalHabit Find(string id)
    {
        foreach (var habit in All) if (habit.Id == id) return habit;
        return null;
    }

    private static List<HabitChance> Chance(params (string id, int weight)[] items)
    {
        var list = new List<HabitChance>();
        foreach (var item in items) list.Add(new HabitChance { Habit = Find(item.id), Weight = item.weight });
        return list;
    }

    // The habits a species can have, the most common first (empty if it does not grow in crystals).
    public static List<HabitChance> For(GemDefinition def)
    {
        if (def == null) return new List<HabitChance>();
        var name = def.name ?? "";

        switch (name)
        {
            case "Diamant": return Chance(("octahedron", 62), ("octahedron_trigons", 38));
            case "Diamant_Synthetique_HPHT": return Chance(("cuboctahedron", 100));
            case "Diamant_Synthetique_CVD": return Chance(("cube", 100));
            case "Zircon":
            case "Scapolite": return Chance(("tetragonal_prism", 100));
            case "Fluorite": return Chance(("cube", 50), ("octahedron", 40), ("rhombic_dodecahedron", 10));
            case "Spinelle":
            case "Spinelle_Bleu": return Chance(("octahedron", 100));
            case "Pyrite": return Chance(("cube", 70), ("octahedron", 30));
            case "Corindon_Rubis":
            case "Padparadscha": return Chance(("hex_tabular", 45), ("hex_bipyramid", 35), ("hex_prism", 20));
            case "Topaze":
            case "Peridot":
            case "Chrysoberyl":
            case "Chrysoberyl_Alexandrite":
            case "Andalousite":
            case "Sinhalite":
            case "Iolite":
            case "Tanzanite":
            case "Zoisite": return Chance(("ortho_prism", 100));
            case "Kunzite":
            case "Orthose":
            case "Pierre_De_Lune":
            case "Diopside_Chromifere": return Chance(("monoclinic_prism", 100));
            case "Apatite":
            case "Painite":
            case "Emeraude":
            case "Goshenite":
            case "Heliodore":
            case "Morganite":
            case "Beryl_Rouge": return Chance(("hex_prism", 100));
            case "Rhodocrosite": return Chance(("rhombohedron", 100));
        }

        if (name.StartsWith("Grenat_")) return Chance(("rhombic_dodecahedron", 55), ("trapezohedron", 45));
        if (name.StartsWith("Saphir_")) return Chance(("hex_bipyramid", 55), ("hex_prism", 30), ("hex_tabular", 15));
        if (name.StartsWith("Tourmaline_")) return Chance(("trigonal_prism", 100));

        // The quartzes grow in prisms; the agates, tiger's eye and rose quartz are masses.
        var quartz = def.isQuartzFamily || name == "Quartz" || name == "Amethyste" || name == "Citrine" || name == "Ametrine" || name == "Prasiolite" || name == "Quartz_Fume";
        if (quartz && name != "Agate" && name != "Oeil_de_Tigre" && name != "Quartz_Rose") return Chance(("quartz_prism", 100));

        return new List<HabitChance>();
    }

    public static bool Has(GemDefinition def) => For(def).Count > 0;

    // The habit of a rough stone of this species, from the number of the specimen (always the same one), or null.
    public static CrystalHabit Pick(GemDefinition def, string specimenId)
    {
        var chances = For(def);
        if (chances.Count == 0) return null;
        var total = chances.Sum(c => c.Weight);
        var roll = (int)(StoneOptics.StableHash((specimenId ?? "") + "|habit|" + (def.name ?? "")) % (uint)total);
        foreach (var chance in chances)
        {
            if (roll < chance.Weight) return chance.Habit;
            roll -= chance.Weight;
        }
        return chances[0].Habit;
    }

    // The most common habit of a species: the one its default rough icon shows.
    public static CrystalHabit Primary(GemDefinition def)
    {
        var chances = For(def);
        return chances.Count > 0 ? chances[0].Habit : null;
    }

    // "octaèdre, dodécaèdre rhombique, cube", for the sheet of a species.
    public static string NamesFor(GemDefinition def)
    {
        return string.Join(", ", For(def).Select(c => c.Habit.Name.ToLowerInvariant()));
    }
}
