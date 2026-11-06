using System.Collections.Generic;

// What can be done to a species, for the grimoire: every treatment catalog is asked about a natural reference stone of the species. Only the
// refusals that come from the material itself count here (a rough stone, a clarity or a colour that does not suit are the player's business).
public static class TreatmentReference
{
    private static readonly Dictionary<string, List<string>> Cache = new Dictionary<string, List<string>>();

    // Words of the refusals that say "this material, never": the other refusals depend on the specimen.
    private static readonly string[] MaterialMarkers =
    {
        "Ce matériau", "ne se chauffe pas ici", "ne s'irradie pas ici", "ne se fait ici que sur les corindons",
    };

    private static bool Suits(string refusal)
    {
        if (string.IsNullOrEmpty(refusal)) return true;
        foreach (var marker in MaterialMarkers)
            if (refusal.Contains(marker)) return false;
        return !refusal.Contains("synthétique");
    }

    private static string Join(IList<string> items) => string.Join(", ", items);

    public static List<string> Lines(GemDefinition def)
    {
        if (def == null) return new List<string>();
        if (Cache.TryGetValue(def.name, out var cached)) return cached;

        var lines = new List<string>();
        GemInstance gem = null;
        try
        {
            var origin = def.origins != null && def.origins.Count > 0 ? def.origins[0] : null;
            gem = origin != null ? new GemInstance(def, origin) : new GemInstance(def);
            gem.MarkIdentified();
            gem.ClearTreatment();
        }
        catch (System.Exception)
        {
            Cache[def.name] = lines;
            return lines;
        }

        var name = def.name ?? "";
        var diamond = name == "Diamant";

        void Add(string title, System.Func<string> build)
        {
            try
            {
                var text = build();
                if (!string.IsNullOrEmpty(text)) lines.Add(title + " : " + text);
            }
            catch (System.Exception) { }
        }

        if (gem.IsSynthetic && name != "CZ")
        {
            lines.Add("Pierre de synthèse : aucun traitement ne s'y applique (on ne traite que les pierres naturelles).");
            Cache[def.name] = lines;
            return lines;
        }

        if (Suits(BleachingCatalog.Refusal(gem)))
        {
            var baths = new List<string>();
            foreach (var bath in BleachingCatalog.Baths) baths.Add(bath.Name);
            Add("Blanchiment", () => "éclaircit la matière (bains : " + Join(baths) + ")");
        }

        if (Suits(DyeingCatalog.Refusal(gem)))
        {
            var dyes = new List<string>();
            foreach (var bath in DyeingCatalog.Baths) dyes.Add(bath.Name.ToLower());
            Add("Teinture", () => "colore la pierre en profondeur (" + Join(dyes) + ")");
        }

        var coatings = CoatingCatalog.TargetNamesFor(def);
        if (coatings.Count > 0) Add("Revêtement", () => "cibles " + Join(coatings).ToLower() + (diamond ? " (un film de cobalt masque le jaune, tout autre film rend la pierre fancy)" : ""));

        var fillers = FillingCatalog.ProductNamesFor(def);
        if (fillers.Count > 0)
            Add(diamond ? "Comblement" : "Imprégnation / remplissage",
                () => Join(fillers).ToLower() + (diamond ? " (plumes visibles, pureté VS2 ou moins)" : ""));

        if (Suits(HeatingCatalog.Refusal(gem)))
        {
            var goals = HeatingCatalog.GoalsFor(gem);
            if (goals.Count > 0) Add("Chauffage", () => Join(goals));
        }

        if (Suits(DiffusionCatalog.Refusal(gem)))
        {
            var dopants = new List<string>();
            foreach (var dopant in DiffusionCatalog.Dopants) dopants.Add(dopant.Name.ToLower());
            Add("Diffusion", () => "dopants " + Join(dopants));
        }

        if (Suits(IrradiationCatalog.Refusal(gem)))
        {
            var goals = IrradiationCatalog.GoalsFor(gem);
            if (goals.Count > 0) Add("Irradiation", () => Join(goals) + (diamond ? " (la pierre devient fancy et perd son certificat)" : ""));
        }

        if (diamond)
        {
            Add("Laser", () => "perce les inclusions foncées pour les blanchir (il faut une inclusion foncée accessible)");
            Add("HPHT", () => "recuit sous haute pression pour éclaircir ou décolorer (diamant non fancy, azote ou teinte à corriger)");
        }

        if (lines.Count == 0) lines.Add("Aucun traitement connu dans le laboratoire pour cette pierre.");
        Cache[def.name] = lines;
        return lines;
    }
}
