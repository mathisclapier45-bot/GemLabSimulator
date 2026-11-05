using System.Collections.Generic;
using UnityEngine;

public enum IrradiationSource { Gamma, Electrons, Neutrons }
public enum IrradiationOutcome { Ruined, NoEffect, Weak, Good, Excellent }

public class IrradiationSourceInfo
{
    public string Name;
    public string Detail;
    public string Description;
    public Color Glow;
    public int Fee;
}

public struct IrradiationQuote
{
    public bool IsValid;
    public string Reason;
    public int ExposureDays;
    public int CoolingDays;
    public int Days => ExposureDays + CoolingDays;
    public int Fee;
    public float Reaction;           // 0..1, how close the settings are to a known process for this stone
    public bool Overdose;
    public string Advice;

    public string ReactionLabel => Reaction < 0.2f ? "aucune" : Reaction < 0.45f ? "faible" : Reaction < 0.7f ? "probable" : "forte";
}

public struct IrradiationResult
{
    public IrradiationOutcome Outcome;
    public string Name;              // the colour obtained: "Fumé", "Bleu Londres"...
    public float Quality;
    public float ValueFactor;
    public bool ChangesColor;
    public float Hue, Saturation, Brightness;
    public int FadeDays;             // 0: the colour is stable; otherwise it fades after this many days of the game
    public string Description;
    public string Message;

    public bool IsRuined => Outcome == IrradiationOutcome.Ruined;
}

// The irradiation of gems, as taught in the course: a high-energy radiation ejects an electron (Fe2+ becomes Fe3+) or displaces atoms, and so
// produces colour centres. Three sources are used: neutrons from a nuclear reactor, electrons from a linear accelerator, and gamma rays from a
// radioisotope such as cobalt-60. The colour depends on the stone, the source and the dose; some of the colours are stable, others temporary and
// they fade in days with heat or light. The stone has to wait until its radioactivity is gone (a long time after the neutrons), and heating can
// invert the treatment (the "annealing"). A colourless or pale diamond turns green or blue and becomes a fancy colour, stable but traced by the GR1
// line of the photoluminescence; the stone loses its certificate.
public static class IrradiationCatalog
{
    public const int MinDose = 50;
    public const int MaxDose = 1000;
    public const int DoseStep = 50;

    public static readonly IrradiationSourceInfo[] Sources =
    {
        new IrradiationSourceInfo { Name = "Rayons gamma", Detail = "Cobalt-60", Glow = new Color(0.45f, 0.95f, 0.55f), Fee = 40,
            Description = "Un radioisotope, le cobalt-60, émet des rayons gamma très pénétrants : ils traversent toute la pierre, sans la rendre radioactive." },
        new IrradiationSourceInfo { Name = "Électrons", Detail = "Accélérateur linéaire", Glow = new Color(0.45f, 0.70f, 1.00f), Fee = 70,
            Description = "Un faisceau d'électrons d'un accélérateur de particules linéaire : rapide, il colore surtout les couches proches de la surface." },
        new IrradiationSourceInfo { Name = "Neutrons", Detail = "Réacteur nucléaire", Glow = new Color(0.95f, 0.62f, 0.25f), Fee = 150,
            Description = "Des neutrons à haute énergie produits dans un réacteur nucléaire : la couleur pénètre toute la pierre, mais celle-ci reste radioactive longtemps." },
    };

    private class Recipe
    {
        public string Name;
        public string[] Species;
        public IrradiationSource Source;
        public float DoseMin, DoseMax;
        public float Hue, Saturation, Brightness;
        public float ValueGain = 1f;
        public int FadeDays;
        public bool Sometimes;        // some stones of the species hold the colour, others lose it
        public string Description;
    }

    private static readonly Recipe[] Recipes =
    {
        // ---- Quartz ----
        new Recipe { Name = "Quartz fumé", Species = new[] { "Quartz" }, Source = IrradiationSource.Gamma, DoseMin = 150, DoseMax = 500,
            Hue = 30, Saturation = 0.45f, Brightness = 0.40f, ValueGain = 1.8f,
            Description = "Quartz incolore irradié : l'aluminium qu'il contient lui donne la couleur du quartz fumé. Difficile à distinguer d'un quartz fumé naturel." },
        new Recipe { Name = "Quartz jaune-vert (oro verde)", Species = new[] { "Quartz" }, Source = IrradiationSource.Electrons, DoseMin = 150, DoseMax = 450,
            Hue = 70, Saturation = 0.70f, Brightness = 0.85f, ValueGain = 2.2f,
            Description = "Quartz irradié : jaune nuancé de vert, le quartz « oro verde ». La plupart des quartz verts, synthétiques compris, doivent leur couleur à l'irradiation." },
        new Recipe { Name = "Améthyste (quartz irradié)", Species = new[] { "Citrine" }, Source = IrradiationSource.Gamma, DoseMin = 250, DoseMax = 650,
            Hue = 275, Saturation = 0.65f, Brightness = 0.55f, ValueGain = 1.4f,
            Description = "Quartz jaune ramené à l'état d'améthyste violette par l'irradiation : le transfert de charges entre le fer ferrique et ferreux absorbe le jaune et le vert." },
        new Recipe { Name = "Quartz bleu violacé (« blueberry »)", Species = new[] { "Prasiolite" }, Source = IrradiationSource.Gamma, DoseMin = 250, DoseMax = 650,
            Hue = 262, Saturation = 0.62f, Brightness = 0.48f, ValueGain = 1.6f,
            Description = "Prasiolite irradiée : un violet bleuté, que le commerce a vendu sous le nom de « blueberry quartz »." },

        // ---- Beryl ----
        new Recipe { Name = "Héliodore (béryl jaune)", Species = new[] { "Goshenite" }, Source = IrradiationSource.Gamma, DoseMin = 150, DoseMax = 500,
            Hue = 55, Saturation = 0.60f, Brightness = 0.95f, ValueGain = 2.0f,
            Description = "Béryl incolore irradié : un électron du fer est éjecté, le Fe2+ devient Fe3+ jaune. C'est ainsi que l'on produit du béryl jaune." },
        new Recipe { Name = "Bleu maxixe", Species = new[] { "Goshenite" }, Source = IrradiationSource.Gamma, DoseMin = 600, DoseMax = 1000,
            Hue = 215, Saturation = 0.80f, Brightness = 0.60f, ValueGain = 1.8f, FadeDays = 4,
            Description = "Béryl irradié à forte dose : un bleu royal soutenu, très dichroïque (bleu et incolore), qui fane en quelques jours à la lumière." },
        new Recipe { Name = "Rose intensifié (morganite)", Species = new[] { "Morganite" }, Source = IrradiationSource.Gamma, DoseMin = 50, DoseMax = 250,
            Hue = 338, Saturation = 0.50f, Brightness = 0.92f, ValueGain = 1.5f,
            Description = "Morganite irradiée : le rose est intensifié." },
        new Recipe { Name = "Orange (rose à orange)", Species = new[] { "Morganite" }, Source = IrradiationSource.Gamma, DoseMin = 300, DoseMax = 600,
            Hue = 26, Saturation = 0.60f, Brightness = 0.92f, ValueGain = 1.3f,
            Description = "Morganite irradiée plus fort : de rose à orange." },

        // ---- Topaz ----
        new Recipe { Name = "Topaze brune", Species = new[] { "Topaze" }, Source = IrradiationSource.Gamma, DoseMin = 300, DoseMax = 700,
            Hue = 28, Saturation = 0.80f, Brightness = 0.55f, ValueGain = 1.2f,
            Description = "Topaze irradiée aux rayons gamma : une couleur brune. Chauffée, elle peut donner un bleu beaucoup plus soutenu, le « bleu Suisse »." },
        new Recipe { Name = "Topaze bleu pâle grisâtre", Species = new[] { "Topaze" }, Source = IrradiationSource.Electrons, DoseMin = 100, DoseMax = 400,
            Hue = 208, Saturation = 0.20f, Brightness = 0.72f, ValueGain = 1.5f,
            Description = "Topaze irradiée aux électrons : un bleu pâle grisâtre. Difficile à distinguer d'une topaze bleue naturelle." },
        new Recipe { Name = "Topaze bleu Londres", Species = new[] { "Topaze" }, Source = IrradiationSource.Neutrons, DoseMin = 200, DoseMax = 600,
            Hue = 210, Saturation = 0.88f, Brightness = 0.55f, ValueGain = 3.2f,
            Description = "Topaze irradiée aux neutrons : le bleu soutenu qu'on qualifie de « bleu Londres », utilisable tel quel. Elle reste radioactive pendant des semaines." },

        // ---- Diamond ----
        new Recipe { Name = "Vert pâle (diamant irradié)", Species = new[] { "Diamant" }, Source = IrradiationSource.Gamma, DoseMin = 400, DoseMax = 800,
            Hue = 135, Saturation = 0.30f, Brightness = 0.84f, ValueGain = 1.0f,
            Description = "Diamant pâle irradié aux rayons gamma : des lacunes dans le réseau du carbone absorbent le rouge et donnent un vert pâle, stable. L'irradiation se lit à la raie GR1 (741 nm) en photoluminescence." },
        new Recipe { Name = "Bleu-vert (électrons)", Species = new[] { "Diamant" }, Source = IrradiationSource.Electrons, DoseMin = 200, DoseMax = 500,
            Hue = 178, Saturation = 0.55f, Brightness = 0.68f, ValueGain = 1.6f,
            Description = "Diamant irradié aux électrons : un bleu-vert, stable, qui reste près de la culasse. L'« effet parapluie » (un anneau plus sombre autour de la colette) et la raie GR1 trahissent le traitement." },
        new Recipe { Name = "Vert foncé (neutrons)", Species = new[] { "Diamant" }, Source = IrradiationSource.Neutrons, DoseMin = 300, DoseMax = 700,
            Hue = 150, Saturation = 0.85f, Brightness = 0.30f, ValueGain = 1.8f,
            Description = "Diamant irradié aux neutrons : un vert foncé qui traverse toute la pierre. Elle reste radioactive longtemps, et la raie GR1 est très forte." },

        // ---- Tourmaline, pearl, corundum ----
        new Recipe { Name = "Rouge-rose intensifié", Species = new[] { "Tourmaline_Rose" }, Source = IrradiationSource.Gamma, DoseMin = 200, DoseMax = 500,
            Hue = 345, Saturation = 0.88f, Brightness = 0.70f, ValueGain = 1.6f, Sometimes = true, FadeDays = 20,
            Description = "Tourmaline irradiée : le rouge-rose est intensifié, mais les résultats sont tantôt stables, tantôt instables. La plupart des tourmalines rouges du commerce ont été irradiées." },
        new Recipe { Name = "Perle gris-bleu", Species = new[] { "Perle", "Perle_Tahiti" }, Source = IrradiationSource.Gamma, DoseMin = 100, DoseMax = 350,
            Hue = 215, Saturation = 0.15f, Brightness = 0.55f, ValueGain = 1.3f,
            Description = "Perle irradiée : plus foncée, dans les tons de bleu et de gris." },
        new Recipe { Name = "Perle noire", Species = new[] { "Perle", "Perle_Tahiti" }, Source = IrradiationSource.Gamma, DoseMin = 400, DoseMax = 800,
            Hue = 220, Saturation = 0.08f, Brightness = 0.14f, ValueGain = 1.8f,
            Description = "Perle irradiée à forte dose : gris très foncé à noir." },
        new Recipe { Name = "Saphir jaune soutenu", Species = new[] { "Saphir_Incolore", "Saphir_Jaune" }, Source = IrradiationSource.Gamma, DoseMin = 200, DoseMax = 500,
            Hue = 46, Saturation = 0.92f, Brightness = 0.95f, ValueGain = 1.8f, FadeDays = 6,
            Description = "Saphir incolore à jaune pâle irradié : un jaune soutenu, qui peut pâlir rapidement s'il est exposé à la chaleur ou à la lumière. On ne reconnaît guère un saphir jaune irradié d'un naturel." },
        new Recipe { Name = "Saphir jaune-brun", Species = new[] { "Saphir_Incolore", "Saphir_Jaune" }, Source = IrradiationSource.Electrons, DoseMin = 100, DoseMax = 350,
            Hue = 38, Saturation = 0.85f, Brightness = 0.68f, ValueGain = 1.5f, FadeDays = 5,
            Description = "Saphir irradié aux électrons : un jaune brun, qui pâlit vite à la chaleur ou à la lumière." },
    };

    private static bool Matches(Recipe recipe, string name)
    {
        foreach (var species in recipe.Species)
            if (species == name) return true;
        return false;
    }

    private static IEnumerable<Recipe> RecipesFor(GemInstance gem)
    {
        var name = gem.Definition != null ? gem.Definition.name : "";
        foreach (var recipe in Recipes)
            if (Matches(recipe, name)) yield return recipe;
    }

    private static bool IsDiamond(GemInstance gem) => gem != null && gem.Definition != null && gem.Definition.name == "Diamant";

    public static bool CanIrradiate(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into the irradiator, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        var any = false;
        foreach (var _ in RecipesFor(gem)) { any = true; break; }
        if (!any)
            return "Cette pierre ne s'irradie pas ici : quartz, béryl, topaze, tourmaline, perle, corindon jaune, diamant.";
        if (gem.IsRough) return "Une pierre brute ne s'irradie pas ici : il faut la tailler ou la polir d'abord.";
        if (gem.IsSynthetic) return "Inutile d'irradier une pierre synthétique.";
        if (IsDiamond(gem))
        {
            if (!DiamondGradeModel.CanGrade(gem)) return "L'irradiation ne traite ici que les diamants taillés.";
            if (DiamondGradeModel.Official(gem).Fancy) return "Un diamant de couleur fancy ne s'irradie pas ici : on le perdrait.";
        }
        if (gem.IsIrradiated) return "Cette pierre a déjà été irradiée : chauffez-la d'abord, la couleur peut être inversée.";
        return null;
    }

    // The names of what can be tried on a stone (not how).
    public static List<string> GoalsFor(GemInstance gem)
    {
        var goals = new List<string>();
        foreach (var recipe in RecipesFor(gem))
            if (!goals.Contains(recipe.Name)) goals.Add(recipe.Name);
        return goals;
    }

    public static int ExposureDays(int dose) => Mathf.Max(1, Mathf.CeilToInt(dose / 250f));

    public static int CoolingDays(IrradiationSource source, int dose)
    {
        switch (source)
        {
            case IrradiationSource.Neutrons: return 6 + Mathf.CeilToInt(dose / 150f);
            case IrradiationSource.Electrons: return 1;
            default: return 0;
        }
    }

    private static float DoseScore(Recipe recipe, int dose, out bool overdose)
    {
        overdose = false;
        if (dose >= recipe.DoseMin && dose <= recipe.DoseMax) return 1f;
        if (dose < recipe.DoseMin) return Mathf.Clamp01((float)dose / recipe.DoseMin) * 0.8f;
        if (dose > recipe.DoseMax * 1.6f)
        {
            overdose = true;
            return 0.3f;
        }
        return 0.85f;
    }

    private static Recipe Best(GemInstance gem, IrradiationSource source, int dose, out float score, out bool overdose)
    {
        Recipe best = null;
        score = 0f;
        overdose = false;
        foreach (var recipe in RecipesFor(gem))
        {
            if (recipe.Source != source) continue;
            var s = DoseScore(recipe, dose, out var over);
            if (s > score)
            {
                score = s;
                best = recipe;
                overdose = over;
            }
        }
        return best;
    }

    public static IrradiationQuote GetQuote(IrradiationSource source, int dose, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal != null) return new IrradiationQuote { IsValid = false, Reason = refusal };

        Best(gem, source, dose, out var score, out var overdose);
        var info = Sources[(int)source];
        var cooling = CoolingDays(source, dose);
        var advice = source == IrradiationSource.Neutrons
            ? "Les neutrons rendent la pierre radioactive : elle doit attendre longtemps avant de sortir. Un lapidaire qui tailleraît une pierre trop récemment irradiée serait en danger."
            : source == IrradiationSource.Electrons
                ? "Les électrons ne pénètrent pas profondément : la couleur reste dans les premières couches."
                : "Les rayons gamma traversent toute la pierre sans la rendre radioactive.";
        return new IrradiationQuote
        {
            IsValid = true,
            ExposureDays = ExposureDays(dose),
            CoolingDays = cooling,
            Fee = info.Fee + Mathf.RoundToInt(dose * 0.15f) + Mathf.RoundToInt(gem.CaratWeight * (IsDiamond(gem) ? 40f : 3f)),
            Reaction = score,
            Overdose = overdose,
            Advice = advice,
        };
    }

    // What comes out of the irradiator. The same stone with the same settings always gives the same result.
    public static IrradiationResult Resolve(IrradiationSource source, int dose, GemInstance gem)
    {
        var quote = GetQuote(source, dose, gem);
        if (!quote.IsValid) return new IrradiationResult { Outcome = IrradiationOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        var recipe = Best(gem, source, dose, out var score, out var overdose);
        if (recipe == null || score < 0.3f)
        {
            return new IrradiationResult
            {
                Outcome = IrradiationOutcome.NoEffect,
                ValueFactor = 1f,
                Description = "Passage à l'irradiateur sans effet visible.",
                Message = "Rien n'a changé : cette source, à cette dose, n'a presque pas coloré la pierre.",
            };
        }

        if (overdose)
        {
            return new IrradiationResult
            {
                Outcome = IrradiationOutcome.Ruined,
                Name = "Surdosée",
                ValueFactor = 0.5f,
                ChangesColor = true,
                Hue = Mathf.LerpAngle(gem.ColorHue, recipe.Hue, 0.7f),
                Saturation = recipe.Saturation * 0.7f,
                Brightness = Mathf.Max(0.05f, recipe.Brightness * 0.5f),
                Description = "Dose bien trop forte : couleur trop sombre, presque noire, et matière fragilisée par le bombardement.",
                Message = "Beaucoup trop de rayonnement : la pierre est devenue sombre et terne.",
            };
        }

        var blend = Mathf.Clamp01(score * 1.1f);
        var outcome = score >= 0.85f ? IrradiationOutcome.Excellent : score >= 0.6f ? IrradiationOutcome.Good : IrradiationOutcome.Weak;
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|irradiate|{source}|{dose}");
        var fade = recipe.FadeDays;
        if (recipe.Sometimes) fade = (hash % 100u) < 50u ? recipe.FadeDays : 0;
        else if (fade > 0) fade += (int)(hash % 4u);

        var darker = dose > recipe.DoseMax ? 0.08f : 0f;
        var result = new IrradiationResult
        {
            Outcome = outcome,
            Name = recipe.Name,
            Quality = score,
            ChangesColor = true,
            Hue = Mathf.LerpAngle(gem.ColorHue, recipe.Hue, blend),
            Saturation = Mathf.Lerp(gem.ColorSaturation, recipe.Saturation, blend),
            Brightness = Mathf.Lerp(gem.ColorBrightness, recipe.Brightness, blend) - darker,
            FadeDays = fade,
            ValueFactor = Mathf.Lerp(1f, recipe.ValueGain, score) * (fade > 0 ? 0.8f : 1f),
            Description = recipe.Description + (fade > 0 ? " Couleur instable : elle pâlit en quelques jours." : " Couleur stable."),
        };
        if (IsDiamond(gem))
        {
            // A treated colour is worth what it adds to the body colour of the stone: a lot for a brownish one, less than the colourless value for a D.
            var oldIndex = DiamondGradeModel.Official(gem).ColorIndex;
            result.ValueFactor = Mathf.Clamp(Mathf.Lerp(0.5f, recipe.ValueGain, score) / Mathf.Max(0.01f, DiamondGrading.ColorFactorOf(oldIndex)) * 0.85f, 0.3f, 4f);
        }
        result.Message = outcome == IrradiationOutcome.Excellent ? $"Remarquable : « {recipe.Name} », la couleur est franche."
            : outcome == IrradiationOutcome.Good ? $"Beau résultat : « {recipe.Name} », un peu moins soutenu qu'espéré."
            : $"La couleur n'a pris qu'à moitié : « {recipe.Name} », il aurait fallu plus de dose.";
        if (fade > 0) result.Message += " Attention : cette couleur ne tiendra pas longtemps.";
        return result;
    }
}
