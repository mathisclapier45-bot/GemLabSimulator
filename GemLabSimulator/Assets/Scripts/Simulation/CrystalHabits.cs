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
        new CrystalHabit { Id = "corundum_barrel", Short = "Tonneau", Name = "Tonneau de corindon", Description = "Un long tonneau hexagonal aux extrémités étagées, strié en travers : la forme classique du saphir brut." },
        new CrystalHabit { Id = "corundum_bipyramid", Short = "Bipyramide courte", Name = "Bipyramide courte de corindon", Description = "Une bipyramide trapue, les pointes émoussées : un saphir de gisement primaire." },
        new CrystalHabit { Id = "corundum_prism", Short = "Prisme trapu", Name = "Prisme trapu de corindon", Description = "Un prisme à six pans, trapu, aux extrémités plates ou rhomboédriques, dépoli et mat : le rubis brut." },
        new CrystalHabit { Id = "corundum_plate", Short = "Plaque", Name = "Plaque hexagonale de corindon", Description = "Une plaque aplatie à six côtés, au bord biseauté : les rubis de Mong Hsu et de Madagascar." },
        new CrystalHabit { Id = "corundum_pebble", Short = "Galet", Name = "Galet de corindon", Description = "Un galet roulé par la rivière, arrondi, aux faces usées : les graviers à gemmes de Ceylan et de Madagascar." },
        new CrystalHabit { Id = "tagua_nut", Short = "Noix", Name = "Noix de tagua", Description = "La graine entière du palmier tagua, encore dans sa peau brune, sèche et craquelée : l'ivoire végétal est dessous, blanc et dur." },
        new CrystalHabit { Id = "corundum_fragment", Short = "Fragment", Name = "Fragment de corindon", Description = "Un éclat anguleux aux grandes cassures plates, parfois le long des plans de séparation." },
        new CrystalHabit { Id = "spinel_twin", Short = "Macle du spinelle", Name = "Macle du spinelle", Description = "Deux octaèdres accolés et aplatis : une plaque triangulaire aux coins tronqués. C'est la macle classique du spinelle, qui lui a donné son nom de loi de macle." },
        new CrystalHabit { Id = "octahedron_modified", Short = "Octaèdre tronqué", Name = "Octaèdre à sommets tronqués", Description = "Un octaèdre dont les six pointes sont coupées par de petites faces carrées (le cube)." },
        new CrystalHabit { Id = "beryl_short", Short = "Prisme court de béryl", Name = "Prisme hexagonal court de béryl", Description = "Un prisme hexagonal régulier, trapu : six pans égaux et deux faces hexagonales plates aux extrémités (pinacoïde basal)." },
        new CrystalHabit { Id = "beryl_prism", Short = "Prisme de béryl", Name = "Prisme hexagonal de béryl", Description = "Un prisme hexagonal régulier allongé dans l'axe de croissance : six pans égaux, terminé de chaque côté par une face hexagonale plate (pinacoïde basal)." },
        new CrystalHabit { Id = "beryl_long", Short = "Prisme long de béryl", Name = "Prisme hexagonal long de béryl", Description = "Un long prisme hexagonal régulier, strié le long de l'axe, aux deux extrémités plates (pinacoïde basal) : le crayon du béryl." },
        new CrystalHabit { Id = "quartz_double", Short = "Quartz biterminé", Name = "Quartz à deux pointes", Description = "Un court prisme hexagonal pointé aux deux bouts, comme les diamants d'Herkimer : un cristal qui a poussé libre, sans support." },
        new CrystalHabit { Id = "hex_pencil", Short = "Crayon hexagonal", Name = "Crayon hexagonal", Description = "Un long cristal fin à six pans, aux fines stries le long de l'axe : l'aigue-marine, l'émeraude, l'apatite." },
        new CrystalHabit { Id = "trigonal_pencil", Short = "Tourmaline allongée", Name = "Crayon de tourmaline", Description = "Un long prisme à section de triangle aux angles arrondis, profondément strié dans sa longueur : la tourmaline." },
        new CrystalHabit { Id = "ortho_wedge", Short = "Prisme à coin", Name = "Prisme à terminaison en coin", Description = "Un prisme rectangulaire qui finit en coin, strié dans sa longueur : la topaze." },
        new CrystalHabit { Id = "tetragonal_prism", Short = "Prisme tétragonal", Name = "Prisme tétragonal bipyramidé", Description = "Prisme à section carrée terminé par une pyramide : le zircon." },
        new CrystalHabit { Id = "ortho_prism", Short = "Prisme orthorhombique", Name = "Prisme orthorhombique", Description = "Prisme à section rectangulaire ou losangique, aux terminaisons obliques : la topaze, le péridot, le chrysobéryl." },
        new CrystalHabit { Id = "monoclinic_prism", Short = "Prisme monoclinique", Name = "Prisme monoclinique", Description = "Prisme aplati aux terminaisons obliques : le spodumène (kunzite), les feldspaths, le diopside." },
        new CrystalHabit { Id = "druse", Short = "Druse", Name = "Druse de cristaux", Description = "Une croûte de minuscules cristaux serrés sur leur roche, scintillante : la seule forme de l'uvarovite." },
        new CrystalHabit { Id = "tourmaline_slice", Short = "Tranche de tourmaline", Name = "Tranche de tourmaline", Description = "Une tranche mince coupée en travers du cristal : un cœur rose, un anneau pâle et une bordure verte, pour la tourmaline melon d'eau." },
        new CrystalHabit { Id = "trigonal_prism", Short = "Prisme trigonal", Name = "Prisme trigonal", Description = "Prisme à section triangulaire aux angles arrondis, strié : les tourmalines." },
        new CrystalHabit { Id = "cleaved_octahedron", Short = "Octaèdre clivé", Name = "Octaèdre clivé de fluorite", Description = "Un fragment d'octaèdre taillé par le clivage parfait de la fluorite : grandes faces planes, marques en escaliers à la surface." },
        new CrystalHabit { Id = "rhombohedral_plate", Short = "Plaque rhomboédrique", Name = "Plaque rhomboédrique", Description = "Un rhomboèdre aplati entre deux grandes faces plates : la pezzottaïte, béryl au césium rose de Madagascar." },
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
            case "Fluorite":
            case "Fluorite_Verte":
            case "Fluorite_Bleue":
            case "Fluorite_Jaune": return Chance(("cube", 40), ("octahedron", 28), ("cleaved_octahedron", 28), ("rhombic_dodecahedron", 4));
            case "Fluorite_Bandee":
            case "Fluorite_Blue_John": return new List<HabitChance>();   // massive: nodules and veins, no crystal faces
            case "Iolite":
            case "Iolite_Bloodshot": return new List<HabitChance>();    // rarely crystals with faces: pebbles and shapeless fragments
            case "Ivoire_Vegetal": return Chance(("tagua_nut", 100));
            case "Spinelle":
            case "Spinelle_Bleu": return Chance(("octahedron", 55), ("spinel_twin", 25), ("octahedron_modified", 20));
            case "Pyrite": return Chance(("cube", 70), ("octahedron", 30));
            case "Corindon_Rubis": return Chance(("corundum_prism", 35), ("corundum_plate", 25), ("corundum_pebble", 15), ("corundum_fragment", 15), ("corundum_bipyramid", 10));
            case "Padparadscha": return Chance(("corundum_pebble", 35), ("corundum_prism", 20), ("corundum_barrel", 20), ("corundum_fragment", 15), ("corundum_bipyramid", 10));
            case "Topaze": return Chance(("ortho_prism", 50), ("ortho_wedge", 50));
            case "Peridot":
            case "Chrysoberyl":
            case "Chrysoberyl_Alexandrite":
            case "Andalousite":
            case "Sinhalite":
            case "Tanzanite":
            case "Zoisite": return Chance(("ortho_prism", 100));
            case "Kunzite":
            case "Orthose":
            case "Pierre_De_Lune":
            case "Diopside_Chromifere": return Chance(("monoclinic_prism", 100));
            case "Heliodore":
            case "Aigue_Marine":
            case "Beryl_Bleu":
            case "Emeraude":
            case "Goshenite":
            case "Morganite":
            case "Beryl_Rouge": return Chance(("beryl_prism", 50), ("beryl_long", 30), ("beryl_short", 20));
            case "Apatite":
            case "Painite": return Chance(("hex_prism", 55), ("hex_pencil", 30), ("hex_tabular", 15));
            case "Pezzottaite": return Chance(("rhombohedral_plate", 80), ("rhombohedron", 20));
            case "Rhodocrosite": return Chance(("rhombohedron", 100));
            case "Calcite": return Chance(("rhombohedron", 100));
            case "Sphalerite": return Chance(("rhombic_dodecahedron", 60), ("octahedron", 20), ("cleaved_octahedron", 20));
            case "Grenat_Uvarovite": return Chance(("druse", 100));
            case "Gypse":
            case "Selenite":
            case "Sphene": return Chance(("monoclinic_prism", 100));
            case "Celestine": return Chance(("ortho_prism", 100));
            // <new-species-2026-11-20>
            case "Amazonite": return Chance(("ortho_prism", 100));
            case "Hiddenite": return Chance(("monoclinic_prism", 100));
            case "Enstatite": return Chance(("ortho_prism", 100));
            case "Benitoite": return Chance(("hex_bipyramid", 70), ("hex_tabular", 30));
            case "Grandidierite": return Chance(("ortho_prism", 100));
            case "Diaspore_Zultanite": return Chance(("ortho_prism", 100));
            case "Euclase": return Chance(("monoclinic_prism", 100));
            case "Danburite": return Chance(("ortho_prism", 100));
            case "Kornerupine": return Chance(("ortho_prism", 100));
            // </new-species-2026-11-20>
            case "Taaffeite": return Chance(("hex_prism", 60), ("hex_bipyramid", 40));
        }

        // The rhombic dodecahedron is the habit of the garnets; the icositetrahedron is a rarer one.
        if (name.StartsWith("Grenat_")) return Chance(("rhombic_dodecahedron", 90), ("trapezohedron", 10));
        if (name.StartsWith("Saphir_")) return Chance(("corundum_barrel", 35), ("corundum_bipyramid", 20), ("corundum_pebble", 20), ("corundum_prism", 15), ("corundum_fragment", 10));
        if (name == "Tourmaline_Melon_Eau") return Chance(("trigonal_pencil", 40), ("tourmaline_slice", 35), ("trigonal_prism", 25));
        if (name.StartsWith("Tourmaline_")) return Chance(("trigonal_prism", 55), ("trigonal_pencil", 45));

        // The quartzes grow in prisms; the agates, tiger's eye and rose quartz are masses.
        var quartz = def.isQuartzFamily || name == "Quartz" || name == "Amethyste" || name == "Citrine" || name == "Ametrine" || name == "Prasiolite" || name == "Quartz_Fume";
        if (quartz && name != "Agate" && name != "Oeil_de_Tigre" && name != "Quartz_Rose" && name != "Cornaline") return Chance(("quartz_prism", 60), ("quartz_double", 40));

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
