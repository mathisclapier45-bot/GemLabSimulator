using System.Collections.Generic;
using UnityEngine;

public enum CoatOutcome { Ruined, Fragile, Good, Excellent }

// One of the targets of the vacuum chamber: a metal that is vaporised onto the stone, or a special coating.
public class CoatTarget
{
    public string Name;
    public string Detail;
    public string Description;
    public Color Glow;               // the colour of the plasma in the chamber
    public bool IsMetal;
    public int Fee;
}

public struct CoatQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float Adhesion;           // 0..1, how well the coating holds
    public float Iridescence;        // 0..1, how strongly the facets show the rainbow
    public string CoatingName;       // "Mystic", "Aqua aura"...
    public string Advice;
    public float Hue;
    public float Saturation;
    public float Brightness;

    public string AdhesionLabel => Adhesion < 0.4f ? "fragile" : Adhesion < 0.6f ? "moyenne" : Adhesion < 0.8f ? "bonne" : "très bonne";
    public string IridescenceLabel => Iridescence < 0.15f ? "aucune" : Iridescence < 0.4f ? "légère" : Iridescence < 0.7f ? "marquée" : "arc-en-ciel";
}

public struct CoatResult
{
    public CoatOutcome Outcome;
    public string CoatingName;
    public float Iridescence;
    public float ValueFactor;
    public float Hue;
    public float Saturation;
    public float Brightness;
    public bool IsDiamond;           // a diamond: the colour is a grade of the diamond scale, or a fancy one
    public int MaskIndex;            // the colour grade a masking film gives (-1: none, the diamond is a fancy colour now)
    public string Description;
    public string Message;
}

// The surface coating of gems, as taught in the course: a very thin layer of gold, titanium or cobalt is deposited under vacuum on the stone,
// usually only on the facets of the pavilion. It gives a pale topaz or a quartz nearly any colour, and a rainbow sheen: the "mystic" topaz. It
// is only a surface treatment, not a diffusion, and its durability depends on the process: scratches and gaps in the layer show as paler lines
// and spots, above all along the facet edges, and the spectrum is the one of the coating. The same idea gives the black paint under a
// translucent opal, the greenish base of an amber, and the synthetic emerald grown over a pale beryl (the Lechleitner emerald), whose layer
// tends to crack. On a diamond, a very thin film of cobalt masks the yellow tint (the stone looks whiter), and any coloured film makes it a fancy
// colour that is not natural; the film wears off, and the certificate no longer describes the stone.
public static class CoatingCatalog
{
    public const int TargetCount = 6;
    public const int ThicknessCount = 3;
    public const int Gold = 0;
    public const int Titanium = 1;
    public const int Cobalt = 2;
    public const int BlackLacquer = 3;
    public const int GreenLacquer = 4;
    public const int Emerald = 5;

    public static readonly CoatTarget[] Targets =
    {
        new CoatTarget { Name = "Or", Detail = "Cible d'or", IsMetal = true, Fee = 40, Glow = new Color(1.00f, 0.82f, 0.35f),
            Description = "Une couche d'or de quelques atomes : sur un quartz, c'est l'« aqua aura », un bleu-vert irisé." },
        new CoatTarget { Name = "Titane", Detail = "Cible de titane", IsMetal = true, Fee = 25, Glow = new Color(0.78f, 0.55f, 1.00f),
            Description = "Le métal de la topaze « mystique » : il donne l'effet d'irisation aux couleurs de l'arc-en-ciel." },
        new CoatTarget { Name = "Cobalt", Detail = "Cible de cobalt", IsMetal = true, Fee = 25, Glow = new Color(0.40f, 0.62f, 1.00f),
            Description = "Un revêtement bleu profond, à peine irisé, comme un bleu de saphir." },
        new CoatTarget { Name = "Laque noire", Detail = "Peinture sous la pierre", IsMetal = false, Fee = 20, Glow = new Color(0.30f, 0.30f, 0.38f),
            Description = "Une couche de peinture noire sous une opale translucide : elle imite l'opale noire." },
        new CoatTarget { Name = "Laque verte", Detail = "Base teintée", IsMetal = false, Fee = 15, Glow = new Color(0.45f, 0.78f, 0.35f),
            Description = "Un fond verdâtre sous un cabochon d'ambre pour lui donner une couleur verte." },
        new CoatTarget { Name = "Émeraude synthétique", Detail = "Surcouche Lechleitner", IsMetal = false, Fee = 70, Glow = new Color(0.25f, 0.85f, 0.50f),
            Description = "Une couche d'émeraude synthétique sur un béryl pâle : verte et profonde, mais elle a tendance à craqueler." },
    };

    public static readonly string[] ThicknessNames = { "Fine", "Moyenne", "Épaisse" };
    private static readonly string[] ThicknessHints = { "Plus d'irisation, couche fragile", "Bon compromis", "Couche solide, couleur dense, moins d'éclat" };
    private static readonly float[] Coverage = { 0.45f, 0.70f, 0.90f };
    private static readonly float[] Adhesion = { 0.45f, 0.70f, 0.90f };
    private static readonly float[] IridescenceFactor = { 1.0f, 0.8f, 0.5f };
    private static readonly float[] FeeFactor = { 1.0f, 1.5f, 2.0f };

    public static string ThicknessHint(int thickness) => ThicknessHints[Mathf.Clamp(thickness, 0, ThicknessCount - 1)];

    private class Recipe
    {
        public string Name;
        public float Hue, Saturation, Brightness, Iridescence, Value;
    }

    // The colour a set of metals gives. The keys are the targets, in order, joined by commas.
    private static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>
    {
        { "0", new Recipe { Name = "Aqua aura", Hue = 190f, Saturation = 0.55f, Brightness = 0.85f, Iridescence = 0.20f, Value = 1.25f } },
        { "1", new Recipe { Name = "Mystic", Hue = 300f, Saturation = 0.50f, Brightness = 0.80f, Iridescence = 1.00f, Value = 1.45f } },
        { "2", new Recipe { Name = "Bleu cobalt", Hue = 225f, Saturation = 0.70f, Brightness = 0.75f, Iridescence = 0.30f, Value = 1.25f } },
        { "0,1", new Recipe { Name = "Mystic feu", Hue = 25f, Saturation = 0.60f, Brightness = 0.90f, Iridescence = 0.90f, Value = 1.50f } },
        { "0,2", new Recipe { Name = "Aqua profond", Hue = 165f, Saturation = 0.65f, Brightness = 0.70f, Iridescence = 0.45f, Value = 1.40f } },
        { "1,2", new Recipe { Name = "Crépuscule", Hue = 260f, Saturation = 0.65f, Brightness = 0.70f, Iridescence = 0.85f, Value = 1.50f } },
        { "3", new Recipe { Name = "Opale noire (laque)", Hue = 240f, Saturation = 0.10f, Brightness = 0.12f, Iridescence = 0f, Value = 1.30f } },
        { "4", new Recipe { Name = "Ambre vert (fond teinté)", Hue = 95f, Saturation = 0.50f, Brightness = 0.60f, Iridescence = 0f, Value = 1.20f } },
        { "5", new Recipe { Name = "Émeraude Lechleitner", Hue = 140f, Saturation = 0.85f, Brightness = 0.55f, Iridescence = 0f, Value = 1.60f } },
    };

    public static string KeyOf(IReadOnlyList<int> targets)
    {
        var sorted = new List<int>(targets);
        sorted.Sort();
        return string.Join(",", sorted);
    }

    private static bool IsMetalSet(IReadOnlyList<int> targets)
    {
        foreach (var t in targets)
            if (t < 0 || t >= TargetCount || !Targets[t].IsMetal) return false;
        return targets.Count > 0;
    }

    private static bool FacetedCoatable(GemDefinition definition)
    {
        var name = definition != null ? definition.name : "";
        if (definition != null && definition.isQuartzFamily) return name != "Agate" && name != "Oeil_de_Tigre";
        switch (name)
        {
            case "Topaze": case "CZ": case "Tanzanite": case "Kunzite": case "Tourmaline_Rose": case "Tourmaline_Paraiba":
            case "Goshenite": case "Heliodore": case "Morganite":
                return true;
        }
        return false;
    }

    private static bool IsDiamond(GemInstance gem) => gem != null && gem.Definition != null && gem.Definition.name == "Diamant";

    private static bool IsCabochon(GemInstance gem) => gem.CutStyleName == "Cabochon";

    public static bool CanCoat(GemInstance gem) => string.IsNullOrEmpty(StoneRefusal(gem));

    // Why a stone cannot go in the chamber at all, or null.
    public static string StoneRefusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        var name = gem.Definition != null ? gem.Definition.name : "";
        var special = name == "Opale_Noble" || name == "Opale_De_Feu" || name == "Ambre" || name == "Goshenite" || name == "Heliodore";
        if (!FacetedCoatable(gem.Definition) && !special && !IsDiamond(gem))
            return "Ce matériau ne se revêt pas ici : topaze, quartz, zircone, béryl, tanzanite, kunzite, tourmaline, diamant, et, avec une laque, l'opale et l'ambre.";
        if (IsDiamond(gem) && !gem.IsRough)
        {
            if (!DiamondGradeModel.CanGrade(gem)) return "Le revêtement ne traite ici que les diamants taillés.";
            if (DiamondGradeModel.Official(gem).Fancy) return "Un diamant de couleur fancy ne se revêt pas ici : on le perdrait.";
        }
        if (gem.IsRough) return "Une pierre brute ne se revêt pas : il faut des facettes, ou un cabochon poli.";
        if (gem.IsSynthetic && name != "CZ") return "Inutile de revêtir une pierre synthétique.";
        if (gem.IsCoated) return "Cette pierre est déjà revêtue : une seconde couche ferait des plaques.";
        return null;
    }

    // Why the chosen targets do not suit the stone, or null.
    private static string SelectionRefusal(GemInstance gem, IReadOnlyList<int> targets)
    {
        if (targets == null || targets.Count == 0) return "Choisissez au moins une cible.";
        var name = gem.Definition.name;

        if (IsMetalSet(targets))
        {
            if (targets.Count > 2) return "Deux métaux au plus dans la chambre.";
            if (!FacetedCoatable(gem.Definition) && !IsDiamond(gem)) return "Les métaux se déposent sur les facettes de la culasse : cette pierre n'en convient pas. Essayez une laque.";
            if (IsCabochon(gem)) return "Un cabochon n'a pas de facettes de culasse : le dépôt de métal n'y accroche pas bien. Taillez-la d'abord.";
            return null;
        }

        if (targets.Count != 1) return "Une laque ou la surcouche d'émeraude se fait seule, sans métal.";
        switch (targets[0])
        {
            case BlackLacquer: return name == "Opale_Noble" || name == "Opale_De_Feu" ? null : "La laque noire se met sous une opale translucide.";
            case GreenLacquer: return name == "Ambre" ? null : "La laque verte se met sous l'ambre.";
            case Emerald: return name == "Goshenite" || name == "Heliodore" ? null : "La surcouche d'émeraude se fait sur un béryl pâle (goshénite, héliodore).";
        }
        return "Cible inconnue.";
    }

    public static CoatQuote GetQuote(IReadOnlyList<int> targets, int thickness, GemInstance gem)
    {
        var refusal = StoneRefusal(gem);
        if (refusal == null) refusal = SelectionRefusal(gem, targets);
        if (refusal != null) return new CoatQuote { IsValid = false, Reason = refusal };

        thickness = Mathf.Clamp(thickness, 0, ThicknessCount - 1);
        Recipes.TryGetValue(KeyOf(targets), out var recipe);
        if (recipe == null) return new CoatQuote { IsValid = false, Reason = "Cette combinaison de cibles n'existe pas." };

        var fee = 0;
        foreach (var t in targets) fee += Targets[t].Fee;
        var special = !IsMetalSet(targets);
        var adhesion = special && targets[0] == Emerald ? 0.35f : Adhesion[thickness];
        var cover = Coverage[thickness];
        var iridescence = recipe.Iridescence * IridescenceFactor[thickness];

        return new CoatQuote
        {
            IsValid = true,
            Days = 1 + thickness + (targets.Count > 1 ? 1 : 0),
            Fee = Mathf.RoundToInt(fee * FeeFactor[thickness]) + Mathf.RoundToInt(gem.CaratWeight * (IsDiamond(gem) ? 35f : 3f)),
            Adhesion = adhesion,
            Iridescence = iridescence,
            CoatingName = recipe.Name,
            Advice = IsDiamond(gem) ? DiamondAdvice(targets, thickness) : AdviceFor(targets, thickness),
            Hue = Mathf.LerpAngle(gem.ColorHue, recipe.Hue, cover),
            Saturation = Mathf.Lerp(gem.ColorSaturation, recipe.Saturation, cover),
            Brightness = Mathf.Lerp(gem.ColorBrightness, recipe.Brightness, cover * 0.9f),
        };
    }

    private static string DiamondAdvice(IReadOnlyList<int> targets, int thickness)
    {
        if (targets.Count == 1 && targets[0] == Cobalt && thickness <= 1)
            return "Un voile très fin de cobalt neutralise le jaune d'un diamant teinté : il paraît plus blanc. Il s'éraille et part à l'acide chaud. Le certificat est perdu.";
        return "Un film coloré fait d'un diamant une pierre fancy qui n'est pas naturelle : couleur trop régulière, faible adhérence. Le certificat est perdu. Pour masquer le jaune, essayez un voile fin de cobalt seul.";
    }

    private static string AdviceFor(IReadOnlyList<int> targets, int thickness)
    {
        if (!IsMetalSet(targets))
        {
            if (targets[0] == Emerald) return "Cette couche craquelle facilement : c'est ce qui trahit l'émeraude Lechleitner. Résultat incertain.";
            return "Un fond peint : il ne se voit que par transparence, et seulement sous la pierre.";
        }
        if (thickness == 0) return "Couche fine : l'irisation est à son maximum, mais elle s'érafle vite, surtout sur les arêtes des facettes.";
        if (thickness == 2) return "Couche épaisse : très solide, mais la couleur est dense et l'éclat un peu terne.";
        return "Couche moyenne : un bon compromis entre l'irisation et la solidité.";
    }

    // What comes out of the chamber. The same stone with the same settings always gives the same result.
    public static CoatResult Resolve(IReadOnlyList<int> targets, int thickness, GemInstance gem)
    {
        var quote = GetQuote(targets, thickness, gem);
        if (!quote.IsValid) return new CoatResult { Outcome = CoatOutcome.Good, ValueFactor = 1f, Message = quote.Reason };

        Recipes.TryGetValue(KeyOf(targets), out var recipe);
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|coat|{KeyOf(targets)}|{thickness}");
        var roll = (hash % 1000u) / 1000f;
        var adhesion = Mathf.Clamp01(quote.Adhesion + (roll - 0.5f) * 0.3f);
        var outcome = adhesion >= 0.75f ? CoatOutcome.Excellent : adhesion >= 0.5f ? CoatOutcome.Good : adhesion >= 0.3f ? CoatOutcome.Fragile : CoatOutcome.Ruined;
        var thick = Mathf.Clamp(thickness, 0, ThicknessCount - 1) == 2 ? 0.92f : 1f;

        var result = new CoatResult
        {
            Outcome = outcome,
            CoatingName = recipe.Name,
            Iridescence = quote.Iridescence,
            Hue = quote.Hue,
            Saturation = quote.Saturation,
            Brightness = quote.Brightness,
        };
        switch (outcome)
        {
            case CoatOutcome.Excellent:
                result.ValueFactor = recipe.Value * thick;
                result.Description = "Revêtement de surface régulier et bien adhérent sur les facettes de la culasse ; le spectre est celui du revêtement et la couche fine donne des irisations visibles à la loupe.";
                result.Message = $"Superbe : « {recipe.Name} », une couche régulière qui accroche parfaitement.";
                break;
            case CoatOutcome.Good:
                result.ValueFactor = recipe.Value * 0.92f * thick;
                result.Description = "Revêtement de surface assez régulier, avec de petits manques plus pâles sur les arêtes des facettes ; le spectre est celui du revêtement.";
                result.Message = $"Beau résultat : « {recipe.Name} », avec de petits manques sur les arêtes.";
                break;
            case CoatOutcome.Fragile:
                result.ValueFactor = recipe.Value * 0.65f * thick;
                result.Description = "Revêtement fragile : lignes et taches plus pâles (rayures et manques dans la couche) et arêtes de facettes dégarnies ; le spectre est celui du revêtement.";
                result.Message = $"La couche est fragile : « {recipe.Name} », mais elle s'érafle déjà, avec des lignes pâles sur les facettes.";
                break;
            default:
                result.ValueFactor = 0.4f;
                result.Iridescence *= 0.5f;
                result.Saturation *= 0.8f;
                result.Brightness = Mathf.Max(0.05f, result.Brightness - 0.1f);
                result.Description = "Revêtement écaillé : de larges manques dans la couche, des taches pâles et des craquelures sur les facettes de la culasse.";
                result.Message = "Raté : la couche n'a pas tenu, elle s'est écaillée en plaques et la pierre est tachée.";
                break;
        }
        return IsDiamond(gem) ? ForDiamond(result, gem, targets, thickness, recipe) : result;
    }

    // A diamond: a very thin cobalt film masks the yellow and improves the colour grade; any other film turns the diamond into a fancy colour.
    private static CoatResult ForDiamond(CoatResult result, GemInstance gem, IReadOnlyList<int> targets, int thickness, Recipe recipe)
    {
        result.IsDiamond = true;
        result.MaskIndex = -1;
        var old = DiamondGradeModel.Official(gem).ColorIndex;
        var masks = targets.Count == 1 && targets[0] == Cobalt && thickness <= 1 && old >= 6 && result.Outcome != CoatOutcome.Ruined;
        if (masks)
        {
            var gain = result.Outcome == CoatOutcome.Excellent ? 3 : result.Outcome == CoatOutcome.Good ? 2 : 1;
            var target = Mathf.Max(3, old - gain);
            result.MaskIndex = target;
            result.ValueFactor = Mathf.Clamp(DiamondGrading.ColorFactorOf(target) / Mathf.Max(0.01f, DiamondGrading.ColorFactorOf(old)) * 0.75f, 0.3f, 4f);
            result.Description = "Voile de cobalt très fin sur la culasse : sa teinte bleutée neutralise le jaune du diamant, qui paraît plus blanc. Il s'éraille sur les arêtes des facettes et part à l'acide chaud ; le spectre est celui du revêtement ; le certificat ne décrit plus la pierre.";
            result.Message = result.Outcome == CoatOutcome.Excellent ? $"Remarquable : le jaune a presque disparu, le diamant gagne {gain} degrés de couleur."
                : result.Outcome == CoatOutcome.Good ? $"Beau résultat : le diamant paraît nettement plus blanc, {gain} degrés de couleur gagnés."
                : "Un voile inégal : le diamant gagne un degré de couleur, mais la couche s'éraille déjà.";
            return result;
        }

        result.ValueFactor = Mathf.Clamp(result.ValueFactor / Mathf.Max(0.01f, DiamondGrading.ColorFactorOf(old)) * 0.7f, 0.25f, 3f);
        result.Description += " Sur un diamant, le film en fait une pierre de couleur fancy qui n'est pas naturelle ; le certificat ne décrit plus la pierre.";
        return result;
    }
}
