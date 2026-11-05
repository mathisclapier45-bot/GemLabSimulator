using UnityEngine;

public enum FillOutcome { Ruined, NoEffect, Slight, Good, Excellent }

// One of the products of the autoclave: an oil, a wax, a resin, a lead glass or a flux that goes into the pores and the fissures of a stone.
public class FillProduct
{
    public string Name;
    public string Detail;
    public string Description;
    public Color Liquid;
    public int Fee;
    public int Days;
    public float ValuePenalty;       // what a treated stone is worth against an untreated one
    public bool Durable;
}

public struct FillQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float Effectiveness;      // 0..1, how well the product fills this material
    public float Risk;               // 0..1, chance that the treatment damages the stone
    public bool Durable;
    public string Advice;

    public string EffectivenessLabel => Effectiveness < 0.3f ? "faible" : Effectiveness < 0.55f ? "moyenne" : Effectiveness < 0.8f ? "forte" : "très forte";
    public string RiskLabel => Risk < 0.08f ? "très faible" : Risk < 0.2f ? "faible" : Risk < 0.4f ? "modéré" : "élevé";
}

public struct FillResult
{
    public FillOutcome Outcome;
    public string Product;
    public string Label;             // the treatment grade of the stone sheet
    public float Quality;
    public int ClarityGain;          // grades of clarity the filling hides
    public float ValueFactor;
    public float SaturationGain;
    public string Description;
    public string Message;
}

// The impregnation and the filling of gems, as taught in the course: a stone that is porous or has fissures reaching the surface is treated with
// oil, wax, resin, glass or a flux, to improve its look or its resistance. Oil hides the fissures of an emerald; a wax or a resin stabilises a
// turquoise, a jadeite (the "B-jade"), a coral or a lapis; an epoxy fills an emerald; borax "heals" the fissures of a ruby and leaves a glassy
// residue; a lead glass fills the very many fissures of a poor ruby. Oils and waxes can drain away when the stone is cleaned.
public static class FillingCatalog
{
    public const int ProductCount = 7;
    public const int ProcedureCount = 3;

    public const int Oil = 0;
    public const int ColoredOil = 1;
    public const int Wax = 2;
    public const int Epoxy = 3;
    public const int Resin = 4;
    public const int LeadGlass = 5;
    public const int Borax = 6;

    public static readonly FillProduct[] Products =
    {
        new FillProduct { Name = "Huile de cèdre", Detail = "Incolore, temporaire", Liquid = new Color(0.86f, 0.74f, 0.36f), Fee = 20, Days = 1, ValuePenalty = 0.85f, Durable = false,
            Description = "L'huile entre dans les fissures : elle supprime les reflets de la lumière et améliore la pureté, mais elle peut sécher ou s'écouler au nettoyage." },
        new FillProduct { Name = "Huile colorée", Detail = "Verte, temporaire", Liquid = new Color(0.20f, 0.64f, 0.34f), Fee = 35, Days = 1, ValuePenalty = 0.75f, Durable = false,
            Description = "Comme l'huile, mais colorée : elle renforce aussi la couleur de l'émeraude ou du rubis." },
        new FillProduct { Name = "Cire / paraffine", Detail = "En surface, temporaire", Liquid = new Color(0.95f, 0.92f, 0.78f), Fee = 10, Days = 1, ValuePenalty = 0.95f, Durable = false,
            Description = "Le plus simple des traitements : on recouvre la surface de cire ou de paraffine pour donner de l'éclat et de la translucidité." },
        new FillProduct { Name = "Résine époxy", Detail = "Type Opticon, stable", Liquid = new Color(0.78f, 0.90f, 1.00f), Fee = 45, Days = 2, ValuePenalty = 0.85f, Durable = true,
            Description = "Un polymère qui remplit les fissures, durci ensuite : l'émeraude traitée à l'Opticon. Il est difficile à repérer en infrarouge." },
        new FillProduct { Name = "Résine de stabilisation", Detail = "Polymère, durable", Liquid = new Color(0.92f, 0.86f, 0.66f), Fee = 40, Days = 3, ValuePenalty = 0.90f, Durable = true,
            Description = "Une résine synthétique qui imprègne une matière poreuse pour la durcir et l'éclaircir : turquoise stabilisée, B-jade, corail, lapis." },
        new FillProduct { Name = "Verre au plomb", Detail = "Sous vide et chaleur", Liquid = new Color(0.96f, 0.46f, 0.16f), Fee = 55, Days = 3, ValuePenalty = 0.60f, Durable = true,
            Description = "Un verre très réfractif qui remplit les nombreuses fissures d'un rubis médiocre. Spectaculaire, mais sensible à la chaleur et aux acides." },
        new FillProduct { Name = "Borax (fondant)", Detail = "Chauffage au flux", Liquid = new Color(0.88f, 0.88f, 0.94f), Fee = 30, Days = 3, ValuePenalty = 0.75f, Durable = true,
            Description = "À haute température, le rubis se dissout un peu dans ce fondant : certaines fissures « guérissent », avec un résidu vitreux." },
    };

    public static readonly string[] ProcedureNames = { "Bain simple", "Sous vide", "Vide + pression" };
    private static readonly string[] ProcedureHints =
    {
        "Le produit s'infiltre seul : moins profond, mais pas cher.",
        "Le vide chasse l'air des fissures avant le remplissage.",
        "Vide puis pression : le produit va partout, plus long et plus cher.",
    };
    private static readonly float[] EffectFactor = { 0.80f, 1.00f, 1.15f };
    private static readonly float[] FeeFactor = { 1.0f, 1.5f, 2.2f };
    private static readonly int[] ExtraDays = { 0, 0, 1 };
    private static readonly float[] ExtraRisk = { 0f, 0f, 0.05f };

    public static string ProcedureHint(int procedure) => ProcedureHints[Mathf.Clamp(procedure, 0, ProcedureCount - 1)];

    private enum Group { None, Beryl, Corundum, Opal, Porous, Feldspar, Gem, Diamond }

    // Effectiveness and risk of each product on each group of materials (-1: the product does not suit the group).
    private static readonly float[][] Eff =
    {
        //            none  beryl  corundum  opal   porous  feldspar  gem    diamond
        new[] { 0f, 0.70f, 0.50f, 0.60f, 0.50f, 0.35f, 0.60f, -1f },      // oil
        new[] { 0f, 0.80f, 0.60f, -1f,   -1f,   -1f,   0.60f, -1f },      // coloured oil
        new[] { 0f, -1f,   -1f,   0.40f, 0.65f, 0.50f, -1f,   -1f },      // wax
        new[] { 0f, 0.85f, 0.60f, -1f,   -1f,   -1f,   0.80f, -1f },      // epoxy
        new[] { 0f, -1f,   -1f,   0.60f, 0.90f, 0.70f, -1f,   -1f },      // resin
        new[] { 0f, -1f,   0.95f, -1f,   -1f,   -1f,   -1f,   -1f },      // lead glass
        new[] { 0f, -1f,   0.60f, -1f,   -1f,   -1f,   -1f,   -1f },      // borax
    };

    private static readonly float[][] Hazard =
    {
        new[] { 0f, 0.05f, 0.05f, 0.20f, 0.05f, 0.05f, 0.05f, 0f },
        new[] { 0f, 0.08f, 0.10f, 0f,    0f,    0f,    0.10f, 0f },
        new[] { 0f, 0f,    0f,    0.10f, 0.03f, 0.03f, 0f,    0f },
        new[] { 0f, 0.07f, 0.08f, 0f,    0f,    0f,    0.07f, 0f },
        new[] { 0f, 0f,    0f,    0.15f, 0.05f, 0.05f, 0f,    0f },
        new[] { 0f, 0f,    0.12f, 0f,    0f,    0f,    0f,    0f },
        new[] { 0f, 0f,    0.30f, 0f,    0f,    0f,    0f,    0f },
    };

    private static Group GroupOf(GemDefinition definition)
    {
        var name = definition != null ? definition.name : "";
        switch (name)
        {
            case "Emeraude": case "Goshenite": case "Heliodore": case "Morganite": case "Beryl_Rouge": return Group.Beryl;
            case "Corindon_Rubis": case "Padparadscha": return Group.Corundum;
            case "Opale_Noble": case "Opale_De_Feu": return Group.Opal;
            case "Turquoise": case "Turquoise_Stabilisee": case "Jade_Jadeite": case "Jade_Nephrite": case "Lapis_Lazuli": case "Corail": return Group.Porous;
            case "Orthose": case "Labradorite": case "Pierre_De_Lune": return Group.Feldspar;
            case "Topaze": case "Tourmaline_Rose": case "Tourmaline_Paraiba": case "Peridot": case "Chrysoberyl": case "Chrysoberyl_Alexandrite": case "Fluorite": return Group.Gem;
            case "Diamant": return Group.Diamond;
        }
        if (name.StartsWith("Saphir_") && name != "Saphir_Incolore") return Group.Corundum;
        return Group.None;
    }

    public static bool CanFill(GemInstance gem) => string.IsNullOrEmpty(StoneRefusal(gem));

    // Why a stone cannot go into the autoclave at all, or null.
    public static string StoneRefusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        var group = GroupOf(gem.Definition);
        if (group == Group.Diamond) return "Le remplissage des diamants se fait dans une chambre à part : pas encore au programme.";
        if (group == Group.None)
            return "Ce matériau ne s'imprègne pas ici : émeraude, rubis, saphir, turquoise, jade, lapis, corail, opale, topaze, tourmaline, péridot, fluorite, feldspath...";
        if (gem.IsRough) return "Une pierre brute ne s'imprègne pas : il faut la tailler ou la polir d'abord.";
        if (gem.IsSynthetic) return "Inutile d'imprégner une pierre synthétique.";
        if (gem.IsFilled || gem.TreatmentGrade == "Fracture comblée" || gem.TreatmentGrade == "Huilé / imprégné")
            return "Cette pierre est déjà imprégnée ou remplie : un second remplissage la noierait.";
        return null;
    }

    private static string SelectionRefusal(GemInstance gem, int product)
    {
        if (product < 0 || product >= ProductCount) return "Choisissez un produit.";
        var group = GroupOf(gem.Definition);
        if (Eff[product][(int)group] >= 0f) return null;
        switch (product)
        {
            case LeadGlass: return "Le verre au plomb se met dans les rubis et les saphirs, pas dans cette pierre.";
            case Borax: return "Le borax ne sert qu'à guérir les fissures du rubis et du saphir.";
            case ColoredOil: return "L'huile colorée ne convient qu'aux pierres transparentes fissurées (émeraude, rubis, topaze...).";
            case Epoxy: return "La résine époxy remplit les fissures des pierres transparentes (émeraude, rubis, topaze, tourmaline...).";
            case Wax: return "La cire convient aux matières poreuses et opaques (turquoise, jade, lapis, corail, opale, feldspath).";
            default: return "La résine de stabilisation convient aux matières poreuses (turquoise, jade, lapis, corail, opale, feldspath).";
        }
    }

    public static FillQuote GetQuote(int product, int procedure, GemInstance gem)
    {
        var refusal = StoneRefusal(gem);
        if (refusal == null) refusal = SelectionRefusal(gem, product);
        if (refusal != null) return new FillQuote { IsValid = false, Reason = refusal };

        procedure = Mathf.Clamp(procedure, 0, ProcedureCount - 1);
        var group = (int)GroupOf(gem.Definition);
        var item = Products[product];
        return new FillQuote
        {
            IsValid = true,
            Days = Mathf.Max(1, item.Days + ExtraDays[procedure]),
            Fee = Mathf.RoundToInt(item.Fee * FeeFactor[procedure]) + Mathf.RoundToInt(gem.CaratWeight * 3f),
            Effectiveness = Mathf.Clamp01(Eff[product][group] * EffectFactor[procedure]),
            Risk = Mathf.Clamp01(Hazard[product][group] + ExtraRisk[procedure]),
            Durable = item.Durable,
            Advice = AdviceFor(product, GroupOf(gem.Definition), gem),
        };
    }

    private static string AdviceFor(int product, Group group, GemInstance gem)
    {
        switch (product)
        {
            case Oil:
                return group == Group.Opal ? "L'huile cache les craquelures d'une opale, mais l'opale craquelle si elle sèche : un traitement fragile."
                    : "L'huile améliore la pureté et la couleur ; elle sèche ou s'écoule au nettoyage, et il faudra alors retraiter la pierre.";
            case ColoredOil: return "Une huile colorée intensifie la couleur, mais c'est un traitement qui se repère : à ne pas vendre sous le nom de pierre naturelle.";
            case Wax: return "Un traitement de surface : l'éclat monte, la cire fond à la chaleur.";
            case Epoxy: return "Plus stable que l'huile, mais jamais d'ultrasons : on voit ensuite des reflets bleus et jaunes dans les fissures.";
            case Resin:
                return gem.Definition.name == "Jade_Jadeite" && gem.TreatmentGrade != null && gem.TreatmentGrade.StartsWith("Blanchi")
                    ? "Une jadéite blanchie et imprégnée de résine : c'est le « B-jade »."
                    : "La stabilisation durcit et éclaircit une matière poreuse, pour de bon.";
            case LeadGlass: return "Très spectaculaire sur un rubis médiocre, mais le verre est plus tendre : il s'abîme à la chaleur, aux acides et aux ultrasons.";
            default: return "Le borax guérit certaines fissures mais laisse des résidus vitreux : on peut les confondre avec ceux d'un rubis synthétique.";
        }
    }

    public static bool UsesClarity(GemInstance gem)
    {
        var group = GroupOf(gem.Definition);
        return group == Group.Beryl || group == Group.Corundum || group == Group.Gem || group == Group.Opal;
    }

    // What comes out of the autoclave. The same stone with the same settings always gives the same result.
    public static FillResult Resolve(int product, int procedure, GemInstance gem)
    {
        var quote = GetQuote(product, procedure, gem);
        if (!quote.IsValid) return new FillResult { Outcome = FillOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        var item = Products[product];
        var group = GroupOf(gem.Definition);
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|fill|{product}|{procedure}");
        var rollRisk = (hash % 1000u) / 1000f;
        var rollQuality = ((hash / 1000u) % 1000u) / 1000f;
        var label = LabelFor(product, gem);

        if (rollRisk < quote.Risk)
        {
            return new FillResult
            {
                Outcome = FillOutcome.Ruined,
                Product = item.Name,
                Label = label + " (altéré)",
                ClarityGain = -1,
                ValueFactor = 0.45f,
                Description = group == Group.Opal
                    ? "Opale craquelée : l'imprégnation a provoqué un réseau de fines craquelures."
                    : "Remplissage raté : poches de produit, taches et résidus visibles, fissures plus marquées.",
                Message = group == Group.Opal
                    ? "L'opale a craquelé dans le bain. Elle est abîmée."
                    : "Le remplissage a mal tourné : poches de produit et taches, la pierre est abîmée.",
            };
        }

        var quality = Mathf.Clamp01(quote.Effectiveness * (0.85f + 0.3f * rollQuality));
        var outcome = quality >= 0.75f ? FillOutcome.Excellent : quality >= 0.5f ? FillOutcome.Good : quality >= 0.3f ? FillOutcome.Slight : FillOutcome.NoEffect;
        if (outcome == FillOutcome.NoEffect)
            return new FillResult { Outcome = outcome, Product = item.Name, Quality = quality, ValueFactor = 1f, Description = "Imprégnation sans effet visible.",
                Message = "Le produit est à peine entré : l'effet est invisible." };

        var gain = outcome == FillOutcome.Excellent ? 2 : outcome == FillOutcome.Good ? 1 : 0;
        var result = new FillResult
        {
            Outcome = outcome,
            Product = item.Name,
            Label = label,
            Quality = quality,
            ClarityGain = UsesClarity(gem) ? gain : 0,
            SaturationGain = product == ColoredOil ? 0.2f * quality : 0f,
            Description = DescriptionFor(product, gem),
        };

        // A stone that shows its clarity is worth what its apparent clarity is worth; a porous stone is worth more for its luster and its toughness.
        if (UsesClarity(gem)) result.ValueFactor = ClarityFactor(gem, result.ClarityGain) * item.ValuePenalty * (product == ColoredOil ? 1.1f : 1f);
        else result.ValueFactor = (1f + (product == Wax ? 0.30f : 0.45f) * quality) * item.ValuePenalty;
        result.ValueFactor = Mathf.Max(0.3f, result.ValueFactor);

        switch (outcome)
        {
            case FillOutcome.Excellent: result.Message = "Remarquable : les fissures ont presque disparu, la pierre est nette et vive."; break;
            case FillOutcome.Good: result.Message = "Beau travail : la pierre est plus nette et son éclat a monté."; break;
            default: result.Message = "Un léger mieux : l'éclat est un peu meilleur, rien de plus."; break;
        }
        return result;
    }

    private static float ClarityFactor(GemInstance gem, int gain)
    {
        if (gain <= 0) return 1f;
        var index = ClarityIndexOf(gem.ClarityGrade);
        var next = Mathf.Min(GemGrading.ClarityGrades.Length - 1, index + gain);
        return GemGrading.ClarityGrades[next].Multiplier / Mathf.Max(0.01f, GemGrading.ClarityGrades[index].Multiplier);
    }

    public static int ClarityIndexOf(string grade)
    {
        for (var i = 0; i < GemGrading.ClarityGrades.Length; i++)
            if (GemGrading.ClarityGrades[i].Name == grade) return i;
        return 1;
    }

    private static string LabelFor(int product, GemInstance gem)
    {
        var name = gem.Definition.name;
        var bleached = gem.TreatmentGrade != null && gem.TreatmentGrade.StartsWith("Blanchi");
        var dyed = gem.TreatmentGrade != null && gem.TreatmentGrade.Contains("teint");
        if (name == "Jade_Jadeite" && product == Resin) return bleached && dyed ? "C-jade (blanchie, teinte, imprégnée)" : bleached ? "B-jade (blanchie, imprégnée)" : "Jadéite imprégnée";
        if ((name == "Turquoise" || name == "Turquoise_Stabilisee") && product == Resin) return "Turquoise stabilisée";
        switch (product)
        {
            case Oil: case ColoredOil: case Wax: return "Huilé / imprégné";
            case Borax: return "Guéri au borax";
            default: return "Fracture comblée";
        }
    }

    private static string DescriptionFor(int product, GemInstance gem)
    {
        switch (product)
        {
            case Oil: return "Fissures remplies d'huile : reflets jaunes dans les fissures, résidus qui peuvent s'écouler au nettoyage.";
            case ColoredOil: return "Fissures remplies d'une huile colorée : couleur renforcée, reflets verts ou jaunes dans les fissures, qui se voient en tournant la pierre.";
            case Wax: return "Cire en surface : éclat et translucidité augmentés, la cire fond à la chaleur.";
            case Epoxy: return "Fissures remplies d'une résine époxy : reflets bleus et jaunes, bulles, lignes ondulantes et zones nuageuses dans le remplissage ; IR proche de la pierre.";
            case Resin: return "Imprégnation de résine synthétique : durcie et éclaircie, signature infrarouge du polymère.";
            case LeadGlass: return "Remplissage au verre au plomb : très nombreuses fissures en bas relief, reflets bleus, reflet orange et bulles, éclat moindre dans le verre ; sensible à la chaleur, aux acides et aux ultrasons.";
            default: return "Fissures guéries au borax : résidus vitreux dans les fissures et zones de moindre éclat, semblables à ceux d'un rubis synthétique au fondant.";
        }
    }
}
