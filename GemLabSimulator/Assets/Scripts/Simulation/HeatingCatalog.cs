using System.Collections.Generic;
using UnityEngine;

public enum HeatAtmosphere { Oxidizing, Reducing, Neutral }
public enum HeatCooling { Fast, Slow }
public enum HeatOutcome { Ruined, NoEffect, Partial, Good, Excellent }

public struct HeatQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float Reaction;           // 0..1, how close the settings are to a known process for this stone
    public float CrackRisk;          // 0..1, chance that the stone cracks
    public bool TooHot;

    public string ReactionLabel => Reaction < 0.2f ? "aucune" : Reaction < 0.45f ? "faible" : Reaction < 0.7f ? "probable" : "forte";
    public string RiskLabel => TooHot ? "fusion" : CrackRisk < 0.08f ? "très faible" : CrackRisk < 0.2f ? "faible" : CrackRisk < 0.4f ? "modéré" : "élevé";
}

public struct HeatResult
{
    public HeatOutcome Outcome;
    public string Goal;
    public bool Cracked;
    public float Quality;
    public int ClarityGain;
    public bool Star;
    public float ValueFactor;
    public bool ChangesColor;
    public float Hue;
    public float Saturation;
    public float Brightness;
    public string Description;
    public string Message;
}

// The heating of gems, as taught in the course: a very old treatment that produces, removes or evens out the colour, and can also produce or
// destroy inclusions. It changes the chemistry of the elements of the stone (diffusion, dehydration, exsolution, dissolution of the rutile...),
// and depends on three things the player sets in the furnace: the temperature, the atmosphere (oxidising: with oxygen, the Fe2+ becomes Fe3+ and
// a blue gets paler; reducing: with hydrogen or carbon monoxide, the Fe3+ becomes Fe2+ and the blue deepens) and the cooling (too fast, the
// stone can crack; slow, the rutile can crystallise and make a star). The diffusion of the corundum is left for later.
public static class HeatingCatalog
{
    public const int MinTemperature = 100;
    public const int MaxTemperature = 2000;
    public const int TemperatureStep = 50;
    public static readonly int[] DurationDays = { 1, 3, 7, 14 };

    private enum Need { Any, Oxidizing, Reducing }
    private enum Cold { Any, Fast, Slow }

    private class Recipe
    {
        public string Goal;
        public string[] Species;                 // asset names, or prefixes ending with "*"
        public float TMin, TMax;
        public Need Atmosphere = Need.Any;
        public Cold Cooling = Cold.Any;
        public int MinDays = 1;
        public bool Relative;                    // the colour moves by SatMul and BriAdd instead of going to a target
        public float Hue, Saturation, Brightness;
        public float SatMul = 1f, BriAdd;
        public bool ChangesColor = true;
        public int ClarityGain;
        public bool Star;
        public float ValueGain = 1f;
        public float HueFrom = -1f, HueTo = -1f; // optional condition on the hue of the stone
        public string Description;
    }

    private const string CorundumMarks = "Traitement thermique : aiguilles de rutile en pointillé, fissures de tension en halo autour des inclusions cristallines, inclusions blanchâtres ou nuageuses ; les givres de guérison peuvent être affectés. Stable.";

    private static readonly string[] Corundum = { "Corindon_Rubis", "Padparadscha", "Saphir_*" };

    private static readonly Recipe[] Recipes =
    {
        // ---- Corundum (the summary table of the course) ----
        new Recipe { Goal = "Éliminer les soies", Species = Corundum, TMin = 1600, TMax = 1900, Cooling = Cold.Fast, ChangesColor = false, ClarityGain = 1, ValueGain = 1.25f,
            Description = "Soies dissoutes par un chauffage à très haute température : la pierre nuageuse est devenue transparente. " + CorundumMarks },
        new Recipe { Goal = "Provoquer des soies (astérisme)", Species = Corundum, TMin = 1100, TMax = 1400, Cooling = Cold.Slow, MinDays = 3, ChangesColor = false, Star = true, ValueGain = 1.35f,
            Description = "Le rutile a cristallisé en aiguilles au refroidissement lent dans une pierre riche en titane : soies fines et, dans un cabochon, un astérisme. " + CorundumMarks },
        new Recipe { Goal = "Obtenir une couleur bleue", Species = new[] { "Saphir_Incolore", "Saphir_Bleu", "Saphir_Violet" }, TMin = 1600, TMax = 1900, Atmosphere = Need.Reducing, Cooling = Cold.Slow,
            Hue = 225, Saturation = 0.90f, Brightness = 0.50f, ValueGain = 2.5f,
            Description = "Chauffage en atmosphère réductrice : le fer ferrique est passé à l'état ferreux, le bleu est intense et stable. " + CorundumMarks },
        new Recipe { Goal = "Atténuer le bleu des saphirs", Species = new[] { "Saphir_Bleu" }, TMin = 800, TMax = 1900, Atmosphere = Need.Oxidizing, Relative = true, SatMul = 0.55f, BriAdd = 0.20f, ValueGain = 1.4f,
            Description = "Chauffage en milieu oxydant : le fer ferreux est devenu ferrique, le bleu d'encre est plus pâle. " + CorundumMarks },
        new Recipe { Goal = "Éliminer le bleu du rubis violacé", Species = new[] { "Corindon_Rubis" }, TMin = 800, TMax = 1900, Atmosphere = Need.Oxidizing, HueFrom = 320, HueTo = 352,
            Hue = 354, Saturation = 0.92f, Brightness = 0.80f, ValueGain = 1.5f,
            Description = "Chauffage en milieu oxydant : la composante bleue a disparu, le rubis est d'un rouge plus pur. " + CorundumMarks },
        new Recipe { Goal = "Éliminer la teinte de brun", Species = new[] { "Corindon_Rubis" }, TMin = 800, TMax = 1900, Atmosphere = Need.Oxidizing, HueFrom = 352, HueTo = 380,
            Hue = 356, Saturation = 0.95f, Brightness = 0.82f, ValueGain = 1.4f,
            Description = "Chauffage en milieu oxydant : la teinte de brun est partie, le rouge se rapproche du rouge « idéal ». " + CorundumMarks },
        new Recipe { Goal = "Éliminer le bleu des saphirs verts", Species = new[] { "Saphir_Vert" }, TMin = 800, TMax = 1900, Atmosphere = Need.Oxidizing,
            Hue = 55, Saturation = 0.85f, Brightness = 0.85f, ValueGain = 1.3f,
            Description = "Chauffage en milieu oxydant : la composante bleue a disparu, le jaune est renforcé. " + CorundumMarks },
        new Recipe { Goal = "Obtenir une couleur jaune", Species = new[] { "Saphir_Incolore", "Saphir_Jaune" }, TMin = 1600, TMax = 1900, Atmosphere = Need.Oxidizing,
            Hue = 48, Saturation = 0.92f, Brightness = 0.95f, ValueGain = 2.0f,
            Description = "Chauffage en milieu oxydant : jaune intense, jaune or. " + CorundumMarks },
        new Recipe { Goal = "Obtenir une couleur orange", Species = new[] { "Saphir_Rose" }, TMin = 1600, TMax = 1900, Atmosphere = Need.Oxidizing,
            Hue = 24, Saturation = 0.90f, Brightness = 0.95f, ValueGain = 1.6f,
            Description = "Chauffage en milieu oxydant : le saphir rose est devenu orange. " + CorundumMarks },

        // ---- Beryl, quartz, topaz, tourmaline, zircon, tanzanite, amber ----
        new Recipe { Goal = "Aigue-marine (bleu stable)", Species = new[] { "Heliodore" }, TMin = 350, TMax = 500,
            Hue = 195, Saturation = 0.55f, Brightness = 0.90f, ValueGain = 2.2f,
            Description = "Béryl jaunâtre ou verdâtre chauffé : il est devenu bleu, d'une couleur stable. Difficile à détecter." },
        new Recipe { Goal = "Morganite rose pur", Species = new[] { "Morganite" }, TMin = 350, TMax = 500,
            Hue = 340, Saturation = 0.35f, Brightness = 0.95f, ValueGain = 1.4f,
            Description = "Béryl chauffé : la composante orange a disparu, le rose est plus pur et stable. Difficile à détecter." },
        new Recipe { Goal = "Citrine (jaune)", Species = new[] { "Amethyste" }, TMin = 450, TMax = 560, Atmosphere = Need.Oxidizing,
            Hue = 40, Saturation = 0.80f, Brightness = 0.90f, ValueGain = 1.2f,
            Description = "Améthyste chauffée pour produire la couleur jaune des citrines." },
        new Recipe { Goal = "Prasiolite (vert)", Species = new[] { "Amethyste" }, TMin = 500, TMax = 620, Atmosphere = Need.Reducing,
            Hue = 105, Saturation = 0.30f, Brightness = 0.80f, ValueGain = 1.3f,
            Description = "Améthyste chauffée en atmosphère réductrice : elle est devenue verte, vendue sous le nom de prasiolite (et à tort d'« améthyste verte »)." },
        new Recipe { Goal = "Cornaline (rouge)", Species = new[] { "Agate" }, TMin = 300, TMax = 450, Atmosphere = Need.Oxidizing,
            Hue = 12, Saturation = 0.85f, Brightness = 0.80f, ValueGain = 1.6f,
            Description = "Quartz microcristallin chauffé : l'agate a pris la couleur rougeâtre de la cornaline." },
        new Recipe { Goal = "Topaze rose (chrome)", Species = new[] { "Topaze" }, TMin = 450, TMax = 600,
            Hue = 335, Saturation = 0.50f, Brightness = 0.92f, ValueGain = 1.8f,
            Description = "Topaze jaune ou brune chromifère chauffée : le jaune est éliminé et le rose, dû au chrome résiduel, apparaît. Stable." },
        new Recipe { Goal = "Électriser la tourmaline de Paraïba", Species = new[] { "Tourmaline_Paraiba" }, TMin = 450, TMax = 600, Atmosphere = Need.Oxidizing,
            Hue = 185, Saturation = 0.80f, Brightness = 0.85f, ValueGain = 1.5f,
            Description = "Tourmaline au cuivre chauffée : le rose est éliminé, il reste le bleu électrique ou le vert de Paraïba. Stable." },
        new Recipe { Goal = "Zircon bleu", Species = new[] { "Zircon" }, TMin = 900, TMax = 1050, Atmosphere = Need.Reducing,
            Hue = 195, Saturation = 0.60f, Brightness = 0.90f, ValueGain = 2.0f,
            Description = "Zircon brun-rouge chauffé en atmosphère réductrice : il est devenu bleu. La couleur peut se faner avec le temps." },
        new Recipe { Goal = "Zircon jaune or", Species = new[] { "Zircon" }, TMin = 850, TMax = 1000, Atmosphere = Need.Oxidizing,
            Hue = 45, Saturation = 0.80f, Brightness = 0.95f, ValueGain = 1.6f,
            Description = "Zircon brun-rouge chauffé en milieu oxydant : jaune or. La couleur peut se faner avec le temps." },
        new Recipe { Goal = "Zircon incolore", Species = new[] { "Zircon" }, TMin = 1000, TMax = 1150, Atmosphere = Need.Oxidizing,
            Hue = 215, Saturation = 0.03f, Brightness = 0.97f, ValueGain = 1.4f,
            Description = "Zircon brun-rouge chauffé en milieu oxydant : il est devenu incolore. La couleur peut se faner avec le temps." },
        new Recipe { Goal = "Tanzanite bleue à violette", Species = new[] { "Tanzanite", "Zoisite" }, TMin = 550, TMax = 700, Atmosphere = Need.Oxidizing,
            Hue = 255, Saturation = 0.80f, Brightness = 0.60f, ValueGain = 2.5f,
            Description = "Zoïsite chauffée : elle est devenue bleue à violette, avec deux couleurs de pléochroïsme (bleu intense et violet) au lieu de trois." },
        new Recipe { Goal = "Foncer l'ambre", Species = new[] { "Ambre" }, TMin = 150, TMax = 250, Cooling = Cold.Slow,
            Hue = 22, Saturation = 0.90f, Brightness = 0.50f, ValueGain = 1.2f,
            Description = "Ambre chauffé pour le foncer : la couleur est plus profonde, le contour plus sombre." },
        new Recipe { Goal = "Clarifier l'ambre", Species = new[] { "Ambre" }, TMin = 100, TMax = 150, Cooling = Cold.Slow, ChangesColor = false, ClarityGain = 1, ValueGain = 1.3f,
            Description = "Ambre chauffé doucement pour le « clarifier » : les bulles et les nuages ont diminué." },
        new Recipe { Goal = "Éclatements lenticulaires", Species = new[] { "Ambre" }, TMin = 150, TMax = 250, Cooling = Cold.Fast, ChangesColor = false, ValueGain = 1.25f,
            Description = "Refroidissement rapide de l'ambre : fissures de tension circulaires, les « éclatements lenticulaires », recherchés pour leur effet attrayant." },
    };

    // The highest temperature a species stands; above it, the stone melts or bursts. And how much a fast cooling threatens it.
    private static float MaxSafe(string name)
    {
        if (name == "Corindon_Rubis" || name == "Padparadscha" || name.StartsWith("Saphir_")) return 1950f;
        if (name == "Zircon") return 1200f;
        if (name == "Tanzanite" || name == "Zoisite") return 800f;
        if (name == "Ambre") return 320f;
        if (name == "Agate") return 600f;
        return 700f;
    }

    private static float FastCoolingRisk(string name)
    {
        if (name == "Corindon_Rubis" || name == "Padparadscha" || name.StartsWith("Saphir_")) return 0.10f;
        if (name == "Zircon") return 0.15f;
        if (name == "Ambre") return 0f;
        if (name == "Topaze") return 0.50f;
        if (name == "Tanzanite" || name == "Zoisite") return 0.40f;
        if (name == "Heliodore" || name == "Morganite") return 0.35f;
        if (name == "Amethyste" || name == "Agate") return 0.30f;
        return 0.25f;
    }

    private static bool Matches(Recipe recipe, string name)
    {
        foreach (var species in recipe.Species)
        {
            if (species.EndsWith("*") ? name.StartsWith(species.Substring(0, species.Length - 1)) : species == name) return true;
        }
        return false;
    }

    private static bool HueFits(Recipe recipe, GemInstance gem)
    {
        if (recipe.HueFrom < 0f) return true;
        var hue = gem.ColorHue;
        if (hue < 180f) hue += 360f;
        return hue >= recipe.HueFrom && hue < recipe.HueTo;
    }

    private static List<Recipe> RecipesFor(GemInstance gem)
    {
        var found = new List<Recipe>();
        var name = gem.Definition != null ? gem.Definition.name : "";
        foreach (var recipe in Recipes)
        {
            if (!Matches(recipe, name)) continue;
            // The ruby recipes depend on its hue; the first one that fits is the one that counts.
            if (!HueFits(recipe, gem)) continue;
            found.Add(recipe);
        }
        return found;
    }

    public static bool CanHeat(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into the furnace, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        var name = gem.Definition != null ? gem.Definition.name : "";
        var known = false;
        foreach (var recipe in Recipes)
            if (Matches(recipe, name)) { known = true; break; }
        if (!known)
            return "Cette pierre ne se chauffe pas ici : corindon, béryl, quartz, topaze, tourmaline, zircon, tanzanite, ambre. (Fluorite, grenat, jade, spodumène : plus tard.)";
        if (gem.IsRough) return "Une pierre brute ne se chauffe pas ici : il faut la tailler ou la polir d'abord.";
        if (gem.IsSynthetic) return "Inutile de chauffer une pierre synthétique.";
        if (!string.IsNullOrEmpty(gem.TreatmentGrade) && gem.TreatmentGrade.Contains("Chauffé"))
            return "Cette pierre a déjà été chauffée : un second passage la fragiliserait.";
        return null;
    }

    // The names of what can be tried on a stone, in the order of the course. The conditions are for the player to know.
    public static List<string> GoalsFor(GemInstance gem)
    {
        var goals = new List<string>();
        foreach (var recipe in RecipesFor(gem))
            if (!goals.Contains(recipe.Goal)) goals.Add(recipe.Goal);
        return goals;
    }

    private static float RangeScore(float value, float min, float max)
    {
        if (value >= min && value <= max) return 1f;
        var tolerance = Mathf.Max(30f, max * 0.08f);
        var distance = value < min ? min - value : value - max;
        return Mathf.Clamp01(1f - distance / tolerance);
    }

    private static float Score(Recipe recipe, int temperature, HeatAtmosphere atmosphere, HeatCooling cooling, int days)
    {
        var t = RangeScore(temperature, recipe.TMin, recipe.TMax);
        var a = recipe.Atmosphere == Need.Any ? 1f
            : recipe.Atmosphere == Need.Oxidizing ? (atmosphere == HeatAtmosphere.Oxidizing ? 1f : 0f)
            : (atmosphere == HeatAtmosphere.Reducing ? 1f : 0f);
        var c = recipe.Cooling == Cold.Any ? 1f : recipe.Cooling == Cold.Fast ? (cooling == HeatCooling.Fast ? 1f : 0.45f) : (cooling == HeatCooling.Slow ? 1f : 0.45f);
        var d = days >= recipe.MinDays ? 1f : 0.6f;
        return t * a * c * d;
    }

    private static Recipe Best(GemInstance gem, int temperature, HeatAtmosphere atmosphere, HeatCooling cooling, int days, out float score)
    {
        Recipe best = null;
        score = 0f;
        foreach (var recipe in RecipesFor(gem))
        {
            var s = Score(recipe, temperature, atmosphere, cooling, days);
            if (s > score)
            {
                score = s;
                best = recipe;
            }
        }
        return best;
    }

    public static HeatQuote GetQuote(int temperature, HeatAtmosphere atmosphere, HeatCooling cooling, int days, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal != null) return new HeatQuote { IsValid = false, Reason = refusal };

        var name = gem.Definition.name;
        Best(gem, temperature, atmosphere, cooling, days, out var score);
        var clarity = FillingCatalog.ClarityIndexOf(gem.ClarityGrade);
        var crack = cooling == HeatCooling.Fast ? Mathf.Clamp01(FastCoolingRisk(name) * (1.4f - 0.2f * clarity)) : 0f;
        var fee = 10 + Mathf.RoundToInt(temperature * 0.03f) + days * 12 + Mathf.RoundToInt(gem.CaratWeight * 2f) + (atmosphere == HeatAtmosphere.Reducing ? 10 : 0);

        return new HeatQuote
        {
            IsValid = true,
            Days = days + (cooling == HeatCooling.Slow ? 1 : 0),
            Fee = fee,
            Reaction = score,
            CrackRisk = crack,
            TooHot = temperature > MaxSafe(name) + 50f,
        };
    }

    // What comes out of the furnace. The same stone with the same settings always gives the same result.
    public static HeatResult Resolve(int temperature, HeatAtmosphere atmosphere, HeatCooling cooling, int days, GemInstance gem)
    {
        var quote = GetQuote(temperature, atmosphere, cooling, days, gem);
        if (!quote.IsValid) return new HeatResult { Outcome = HeatOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        if (quote.TooHot)
        {
            return new HeatResult
            {
                Outcome = HeatOutcome.Ruined,
                ValueFactor = 0.3f,
                Description = "Chauffé trop fort : surface fondue ou éclatée, fissures étendues, la pierre est gravement abîmée.",
                Message = "Le four était trop chaud : la pierre a fondu en surface et éclaté. Elle est gravement abîmée.",
            };
        }

        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|heat|{temperature}|{atmosphere}|{cooling}|{days}");
        var cracked = cooling == HeatCooling.Fast && (hash % 1000u) / 1000f < quote.CrackRisk;
        var recipe = Best(gem, temperature, atmosphere, cooling, days, out var score);
        var heatNote = "Traitement thermique : couleur stable, difficile à détecter sans analyse de laboratoire.";

        var result = new HeatResult { Outcome = HeatOutcome.NoEffect, Cracked = cracked, ValueFactor = 1f };
        if (recipe != null && score >= 0.45f)
        {
            result.Outcome = score >= 0.85f ? HeatOutcome.Excellent : score >= 0.65f ? HeatOutcome.Good : HeatOutcome.Partial;
            result.Goal = recipe.Goal;
            result.Quality = score;
            result.ClarityGain = recipe.ClarityGain;
            result.Description = recipe.Description;
            var blend = Mathf.Clamp01(Mathf.Pow(score, 0.8f));

            if (recipe.ChangesColor)
            {
                result.ChangesColor = true;
                if (recipe.Relative)
                {
                    result.Hue = gem.ColorHue;
                    result.Saturation = gem.ColorSaturation * Mathf.Lerp(1f, recipe.SatMul, blend);
                    result.Brightness = gem.ColorBrightness + recipe.BriAdd * blend;
                }
                else
                {
                    result.Hue = Mathf.LerpAngle(gem.ColorHue, recipe.Hue, blend);
                    result.Saturation = Mathf.Lerp(gem.ColorSaturation, recipe.Saturation, blend);
                    result.Brightness = Mathf.Lerp(gem.ColorBrightness, recipe.Brightness, blend);
                }
            }

            result.ValueFactor = Mathf.Lerp(1f, recipe.ValueGain, score) * 0.9f;
            if (recipe.Star)
            {
                // The star only shows in a cabochon; in a faceted stone the silk only clouds it.
                var cabochon = gem.CutStyleName == "Cabochon";
                result.Star = cabochon;
                result.ClarityGain = cabochon ? 0 : -1;
                if (!cabochon) result.ValueFactor = 0.8f;
            }

            result.Message = result.Outcome == HeatOutcome.Excellent
                ? $"Remarquable : « {recipe.Goal} », exactement l'effet recherché."
                : result.Outcome == HeatOutcome.Good
                    ? $"Beau résultat : « {recipe.Goal} », un peu moins net qu'espéré."
                    : $"Un effet partiel : « {recipe.Goal} » n'est qu'à moitié obtenu. Les réglages n'étaient pas tout à fait les bons.";
        }
        else
        {
            result.Description = "Passage au four sans effet visible.";
            result.Message = "Rien n'a changé : à ces réglages, la pierre n'a pas réagi.";
        }

        if (result.Outcome == HeatOutcome.NoEffect && !cracked) return result;

        if (cracked)
        {
            result.ClarityGain -= 1;
            result.ValueFactor *= 0.7f;
            result.Description = $"{result.Description} Refroidissement trop rapide : fissures de tension dans la pierre.";
            result.Message += " Mais le refroidissement trop rapide a fissuré la pierre.";
            if (result.Outcome == HeatOutcome.NoEffect) result.Outcome = HeatOutcome.Partial;
        }
        if (string.IsNullOrEmpty(result.Description)) result.Description = heatNote;
        return result;
    }
}
