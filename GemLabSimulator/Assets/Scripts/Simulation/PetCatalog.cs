using System.Collections.Generic;
using UnityEngine;

// The animals sold at the pet shop. An animal bought there goes into the bag (H); equipped, it follows the player everywhere.
// Sprites live in Resources/Pets as <Sprite>_<n>.png facing right: frame 0 is the standing pose, the others are the walk cycle.
public static class PetCatalog
{
    public struct Pet
    {
        public string Id;
        public string Name;
        public string Description;
        public string Sprite;       // file prefix in Resources/Pets
        public int Price;
        public int Frames;          // number of sprite frames
        public bool WalkUsesAllFrames;   // a snake has no standing pose: every frame is part of the slither
        public float FrameSeconds;
    }

    public static readonly Pet[] All =
    {
        new Pet
        {
            Id = "PetChien", Name = "Petit chien", Sprite = "dog", Price = 150, Frames = 3, FrameSeconds = 0.14f,
            Description = "Un chiot joueur au pelage brun et aux oreilles tombantes. Fidèle et affectueux, il trottine toujours juste derrière vous.",
        },
        new Pet
        {
            Id = "PetChat", Name = "Chat tigré", Sprite = "cat", Price = 120, Frames = 3, FrameSeconds = 0.16f,
            Description = "Un chat gris tigré, élégant et indépendant. Il se promène à son rythme... mais finit toujours par vous suivre.",
        },
        new Pet
        {
            Id = "PetSerpent", Name = "Serpent vert", Sprite = "snake", Price = 200, Frames = 4, WalkUsesAllFrames = true, FrameSeconds = 0.12f,
            Description = "Un petit serpent des blés au motif en losanges. Inoffensif et silencieux, il ondule sur vos pas.",
        },
        new Pet
        {
            Id = "PetGrenouille", Name = "Grenouille verte", Sprite = "grenouille", Price = 90, Frames = 3, FrameSeconds = 0.2f,
            Description = "Une petite grenouille aux grands yeux. Elle avance par petits bonds et ne vous quitte pas d'une patte.",
        },
        new Pet
        {
            Id = "PetTortue", Name = "Tortue", Sprite = "tortue", Price = 180, Frames = 3, FrameSeconds = 0.26f,
            Description = "Une tortue à la carapace bien ronde. Elle prend son temps, mais elle arrive toujours.",
        },
        new Pet
        {
            Id = "PetSinge", Name = "Petit singe", Sprite = "singe", Price = 350, Frames = 3, FrameSeconds = 0.13f,
            Description = "Un petit singe malin à la longue queue enroulée. Curieux de tout, il adore vous suivre en sautillant.",
        },
        new Pet
        {
            Id = "PetMarabout", Name = "Marabout", Sprite = "marabout", Price = 600, Frames = 3, FrameSeconds = 0.2f,
            Description = "Un grand oiseau d'Afrique à la tête chauve et au bec énorme. Il marche d'un pas solennel, comme un vieux professeur.",
        },
        new Pet
        {
            Id = "PetVaran", Name = "Varan de Komodo", Sprite = "varan", Price = 900, Frames = 3, FrameSeconds = 0.15f,
            Description = "Le plus grand lézard du monde, venu d'Indonésie. Il flaire l'air de sa langue fourchue et ne perd jamais votre trace.",
        },
        new Pet
        {
            Id = "PetCrocodile", Name = "Crocodile", Sprite = "crocodile", Price = 1200, Frames = 3, FrameSeconds = 0.17f,
            Description = "Un crocodile du Nil, long et trapu. Il sourit de toutes ses dents mais se tient à votre pas.",
        },
        new Pet
        {
            Id = "PetTigre", Name = "Gros tigre", Sprite = "tigre", Price = 1500, Frames = 3, FrameSeconds = 0.16f,
            Description = "Un grand tigre du Bengale, rayé de noir. Puissant et silencieux, il marche juste derrière vous : personne n'ose s'approcher.",
        },
    };

    public static bool TryGet(string id, out Pet pet)
    {
        foreach (var p in All)
        {
            if (p.Id != id) continue;
            pet = p;
            return true;
        }
        pet = default;
        return false;
    }

    public static bool IsPet(string id) => TryGet(id, out _);

    public static string NameOf(string id) => TryGet(id, out var pet) ? pet.Name : id;

    public static string DescriptionOf(string id) => TryGet(id, out var pet) ? pet.Description : "";

    private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();

    public static Sprite[] LoadFrames(string id)
    {
        if (!TryGet(id, out var pet)) return new Sprite[0];
        if (FrameCache.TryGetValue(id, out var cached) && cached.Length > 0 && cached[0] != null) return cached;

        var frames = new Sprite[pet.Frames];
        for (var i = 0; i < pet.Frames; i++) frames[i] = Resources.Load<Sprite>($"Pets/{pet.Sprite}_{i}");
        FrameCache[id] = frames;
        return frames;
    }

    private static readonly Dictionary<string, Sprite[]> DirectionCache = new Dictionary<string, Sprite[]>();

    // The frames of the animal walking towards the camera ("down") or away from it ("up"), named <Sprite>_down_<n> and <Sprite>_up_<n>.
    // Empty when the animal has none: it then keeps its side view.
    public static Sprite[] LoadDirectionFrames(string id, string direction)
    {
        if (!TryGet(id, out var pet)) return new Sprite[0];
        var key = id + "|" + direction;
        if (DirectionCache.TryGetValue(key, out var cached) && cached.Length > 0 && cached[0] != null) return cached;

        var frames = new Sprite[pet.Frames];
        for (var i = 0; i < pet.Frames; i++)
        {
            frames[i] = Resources.Load<Sprite>($"Pets/{pet.Sprite}_{direction}_{i}");
            if (frames[i] == null)
            {
                frames = new Sprite[0];
                break;
            }
        }
        DirectionCache[key] = frames;
        return frames;
    }

    // The standing pose, for the bag and the shop.
    public static Sprite IconOf(string id)
    {
        var frames = LoadFrames(id);
        return frames.Length > 0 ? frames[0] : null;
    }
}
