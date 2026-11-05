using UnityEngine;

public enum DiffusionOutcome { Ruined, NoEffect, Weak, Good, Excellent }
public enum DiffusionDepth { Surface, Shallow, PartialCore, FullCore }

// One of the elements the corundum can be given by diffusion: a powder of oxides in which the stone is buried before it goes in the furnace.
public class Dopant
{
    public string Name;
    public string Detail;
    public string Description;
    public Color Powder;
    public float Hue, Saturation, Brightness;
    public float Rate;               // millimetres of penetration at 1800 °C after one day: a small ion like beryllium goes much further
    public int Fee;
    public float ValueGain;
}

public struct DiffusionQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float DepthMm;            // how far the element goes in
    public float Coverage;           // 0..1, the share of the half-thickness of the stone it reaches
    public float Quality;            // 0..1, how strong the colour is
    public bool TooHot;
    public DiffusionDepth Depth;
    public string Advice;

    public string DepthLabel => Depth == DiffusionDepth.Surface ? "surface (couche mince)" : Depth == DiffusionDepth.Shallow ? "peu profonde"
        : Depth == DiffusionDepth.PartialCore ? "à cœur partielle" : "à cœur complète";
    public string QualityLabel => Quality < 0.2f ? "nulle" : Quality < 0.45f ? "faible" : Quality < 0.7f ? "moyenne" : "forte";
}

public struct DiffusionResult
{
    public DiffusionOutcome Outcome;
    public DiffusionDepth Depth;
    public float DepthMm;
    public float Quality;
    public bool Star;
    public float ValueFactor;
    public bool ChangesColor;
    public float Hue, Saturation, Brightness;
    public string Description;
    public string Message;
    public string Element;           // the element diffused: what the detection tools read of it
}

// The diffusion of gems, as taught in the course: the colour of a stone is changed with colouring elements that come from an outside source.
// It mostly concerns corundum. The stone is covered with aluminium oxide and the right compounds, then heated at a very high temperature for
// several days: iron and titanium give blue, chromium red, nickel yellow, and a surplus of titanium an asterism. The penetration is minimal, a
// fraction of a millimetre, because the process is slow even at very high temperature: this is the surface diffusion, whose colour a later
// repolishing can remove. Beryllium goes much deeper and can colour the whole stone (the "core" diffusion, or beryllium treatment): a pale sapphire
// becomes orange or pink, a pink one orange, a blue one deeper. Immersion in a liquid of the right refractive index shows the colour gathered
// along the facet edges (the spider web) in a surface-diffused stone.
public static class DiffusionCatalog
{
    public const int MinTemperature = 1200;
    public const int MaxTemperature = 2000;
    public const int TemperatureStep = 50;
    public static readonly int[] DurationDays = { 3, 7, 14, 21 };

    public const int IronTitanium = 0;
    public const int Chromium = 1;
    public const int Nickel = 2;
    public const int TitaniumSurplus = 3;
    public const int Beryllium = 4;
    public const int DopantCount = 5;

    public static readonly Dopant[] Dopants =
    {
        new Dopant { Name = "Fer + titane", Detail = "Oxydes de fer et de titane", Powder = new Color(0.30f, 0.42f, 0.78f), Hue = 225f, Saturation = 0.88f, Brightness = 0.50f, Rate = 0.07f, Fee = 35, ValueGain = 2.6f,
            Description = "Le bleu par diffusion : une couche mince, qui ne dépasse guère le dixième de millimètre." },
        new Dopant { Name = "Chrome", Detail = "Oxyde de chrome", Powder = new Color(0.78f, 0.22f, 0.26f), Hue = 355f, Saturation = 0.92f, Brightness = 0.75f, Rate = 0.08f, Fee = 40, ValueGain = 2.4f,
            Description = "Le rouge par diffusion de chrome, à la surface d'un saphir pâle ou d'un rubis." },
        new Dopant { Name = "Nickel", Detail = "Oxyde de nickel", Powder = new Color(0.88f, 0.76f, 0.24f), Hue = 48f, Saturation = 0.92f, Brightness = 0.95f, Rate = 0.08f, Fee = 35, ValueGain = 1.8f,
            Description = "Le jaune par diffusion de nickel." },
        new Dopant { Name = "Titane en excès", Detail = "Pour l'astérisme", Powder = new Color(0.70f, 0.72f, 0.80f), Hue = 215f, Saturation = 0.25f, Brightness = 0.80f, Rate = 0.07f, Fee = 30, ValueGain = 1.6f,
            Description = "Un surplus de titane pour provoquer un astérisme à la surface d'un cabochon." },
        new Dopant { Name = "Béryllium", Detail = "Composés de béryllium", Powder = new Color(0.94f, 0.58f, 0.22f), Hue = 22f, Saturation = 0.88f, Brightness = 0.95f, Rate = 0.30f, Fee = 120, ValueGain = 2.2f,
            Description = "Un petit ion qui va loin : il colore toute la pierre, saphir rose en orange, saphir pâle en orange ou en rose, bleu renforcé." },
    };

    public static bool IsCorundum(GemDefinition definition)
    {
        var name = definition != null ? definition.name : "";
        return name == "Corindon_Rubis" || name == "Padparadscha" || name.StartsWith("Saphir_");
    }

    public static bool IsDiffused(GemInstance gem) => gem != null && gem.DiffusionDepthMm > 0f;

    public static bool CanDiffuse(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into the diffusion furnace, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (!IsCorundum(gem.Definition)) return "La diffusion ne se fait ici que sur les corindons : rubis et saphirs.";
        if (gem.IsRough) return "Une pierre brute ne se diffuse pas ici : il faut la tailler ou la polir d'abord.";
        if (gem.IsSynthetic) return "Inutile de diffuser une pierre synthétique.";
        if (IsDiffused(gem)) return "Cette pierre est déjà diffusée : un second traitement noierait la couleur.";
        return null;
    }

    private static float TemperatureFactor(int temperature) => Mathf.Pow(Mathf.Clamp(temperature - 1400f, 0f, 560f) / 400f, 1.5f);

    private static float MaxSafe => 1950f;

    // Half of the thickness of the stone: how far a full core diffusion has to go.
    private static float HalfThickness(GemInstance gem) => Mathf.Max(1.2f, gem.DepthMm * 0.5f);

    public static DiffusionDepth Classify(float depthMm, float halfThickness)
    {
        if (depthMm < 0.3f) return DiffusionDepth.Surface;
        if (depthMm < 0.5f) return DiffusionDepth.Shallow;
        return depthMm >= halfThickness * 0.9f ? DiffusionDepth.FullCore : DiffusionDepth.PartialCore;
    }

    // The penetration of an element after some days at a temperature, in millimetres: it grows with the square root of the time.
    public static float DepthOf(int dopant, int temperature, int days)
    {
        return Dopants[dopant].Rate * TemperatureFactor(temperature) * Mathf.Sqrt(days);
    }

    public static DiffusionQuote GetQuote(int dopant, int temperature, int days, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal == null && (dopant < 0 || dopant >= DopantCount)) refusal = "Choisissez un élément à diffuser.";
        if (refusal != null) return new DiffusionQuote { IsValid = false, Reason = refusal };

        var item = Dopants[dopant];
        var depth = DepthOf(dopant, temperature, days);
        var half = HalfThickness(gem);
        var f = Mathf.Clamp(TemperatureFactor(temperature), 0f, 1.4f);
        var quality = Mathf.Clamp01(Mathf.Pow(f, 0.7f) * (0.55f + 0.45f * Mathf.Min(1f, days / 7f)));
        var depthClass = Classify(depth, half);

        var advice = dopant == Beryllium
            ? "Le béryllium diffuse loin : toute la pierre peut être colorée. Les effets se verront en immersion, et il est très difficile à détecter."
            : "Un élément lourd : il reste à la surface, à peine une fraction de millimètre. Un repolissage pourra emporter une partie de la couleur, ou toute la couleur.";
        if (dopant == TitaniumSurplus) advice = "L'astérisme demande un cabochon et un surplus de titane ; il reste à la surface.";

        return new DiffusionQuote
        {
            IsValid = true,
            Days = days,
            Fee = 80 + Mathf.RoundToInt(temperature * 0.05f) + days * 25 + item.Fee + Mathf.RoundToInt(gem.CaratWeight * 5f),
            DepthMm = depth,
            Coverage = Mathf.Clamp01(depth / half),
            Quality = quality,
            Depth = depthClass,
            TooHot = temperature > MaxSafe,
            Advice = advice,
        };
    }

    // What comes out of the furnace. The same stone with the same settings always gives the same result.
    public static DiffusionResult Resolve(int dopant, int temperature, int days, GemInstance gem)
    {
        var quote = GetQuote(dopant, temperature, days, gem);
        if (!quote.IsValid) return new DiffusionResult { Outcome = DiffusionOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        if (quote.TooHot)
        {
            return new DiffusionResult
            {
                Outcome = DiffusionOutcome.Ruined,
                ValueFactor = 0.3f,
                Description = "Chauffé au-delà de ce que supporte le corindon : surface fondue et piquée, fissures étendues.",
                Message = "Le four était bien trop chaud : la surface de la pierre a fondu. Elle est gravement abîmée.",
            };
        }

        var item = Dopants[dopant];
        if (quote.Quality < 0.2f || quote.DepthMm < 0.01f)
        {
            return new DiffusionResult
            {
                Outcome = DiffusionOutcome.NoEffect,
                ValueFactor = 1f,
                Description = "Passage au four sans effet visible.",
                Message = "Rien n'a pris : trop froid ou trop court, l'élément n'est presque pas entré dans la pierre.",
            };
        }

        var q = quote.Quality;
        var outcome = q >= 0.7f ? DiffusionOutcome.Excellent : q >= 0.45f ? DiffusionOutcome.Good : DiffusionOutcome.Weak;
        var result = new DiffusionResult { Outcome = outcome, Depth = quote.Depth, DepthMm = quote.DepthMm, Quality = q };
        var deep = quote.Depth == DiffusionDepth.PartialCore || quote.Depth == DiffusionDepth.FullCore;

        if (dopant == TitaniumSurplus)
        {
            // The star shows in a cabochon; in a faceted stone the diffused titanium only tints it a little.
            var cabochon = gem.CutStyleName == "Cabochon";
            result.Star = cabochon;
            result.ChangesColor = !cabochon;
            result.Hue = gem.ColorHue;
            result.Saturation = gem.ColorSaturation;
            result.Brightness = gem.ColorBrightness;
            result.ValueFactor = cabochon ? Mathf.Lerp(1f, item.ValueGain, q) * 0.8f : 0.9f;
        }
        else
        {
            var target = TargetFor(dopant, gem);
            var blend = Mathf.Clamp01(q * 1.2f);
            result.ChangesColor = true;
            result.Hue = Mathf.LerpAngle(gem.ColorHue, target.x, blend);
            result.Saturation = Mathf.Lerp(gem.ColorSaturation, target.y, blend);
            result.Brightness = Mathf.Lerp(gem.ColorBrightness, target.z, blend);
            result.ValueFactor = Mathf.Lerp(1f, item.ValueGain, q) * (deep ? 0.85f : 0.65f);
        }

        result.ValueFactor = Mathf.Max(0.3f, result.ValueFactor);
        result.Description = DescriptionFor(dopant, quote.Depth, gem);
        result.Message = outcome == DiffusionOutcome.Excellent ? $"Remarquable : « {item.Name} », la couleur a pris franchement."
            : outcome == DiffusionOutcome.Good ? $"Beau résultat : « {item.Name} », la couleur a bien pris."
            : $"La couleur n'a pris qu'à moitié : « {item.Name} », il aurait fallu plus de chaleur ou plus de temps.";
        if (deep) result.Message += " Elle est entrée profondément dans la pierre.";
        else result.Message += " Mais elle reste à la surface : un repolissage l'emporterait.";
        return result;
    }

    // The colour an element gives, which depends on the stone it goes into (hue in degrees, saturation, brightness).
    private static Vector3 TargetFor(int dopant, GemInstance gem)
    {
        var item = Dopants[dopant];
        if (dopant != Beryllium) return new Vector3(item.Hue, item.Saturation, item.Brightness);

        var name = gem.Definition.name;
        if (name == "Saphir_Bleu") return new Vector3(gem.ColorHue, Mathf.Clamp01(gem.ColorSaturation + 0.12f), Mathf.Max(0.3f, gem.ColorBrightness - 0.05f));
        if (name == "Saphir_Rose" || name == "Padparadscha") return new Vector3(24f, 0.90f, 0.95f);
        if (name == "Saphir_Incolore") return new Vector3(20f, 0.62f, 0.96f);
        return new Vector3(26f, 0.85f, 0.92f);
    }

    private static string DescriptionFor(int dopant, DiffusionDepth depth, GemInstance gem)
    {
        var item = Dopants[dopant];
        if (dopant == TitaniumSurplus)
            return "Surplus de titane diffusé à la surface : astérisme superficiel dans un cabochon. En immersion, la couleur se concentre dans les arêtes et le feuilletis. Un repolissage peut l'enlever.";
        switch (depth)
        {
            case DiffusionDepth.Surface:
                return $"Diffusion en surface ({item.Name.ToLowerInvariant()}) : couche de couleur d'une fraction de millimètre ; en immersion, la couleur se concentre dans les arêtes de facettes et le feuilletis, en toile d'araignée, et un repolissage peut en enlever une partie ou toute la couleur.";
            case DiffusionDepth.Shallow:
                return $"Diffusion peu profonde ({item.Name.ToLowerInvariant()}) : la couleur est confinée près de la surface et se concentre dans les arêtes de facettes ; en immersion, un contour coloré se voit.";
            default:
                return dopant == Beryllium
                    ? $"Diffusion à cœur au béryllium ({(depth == DiffusionDepth.FullCore ? "toute la pierre" : "en partie")}) : zones de couleur liées à la forme de la pierre, avec un pourtour orange classique en immersion ; le béryllium n'est pas détectable en FTIR ni en EDXRF, il faut des méthodes avancées."
                    : $"Diffusion à cœur ({item.Name.ToLowerInvariant()}) : la couleur est répartie dans une grande partie de la pierre ; des méthodes avancées sont nécessaires pour le confirmer.";
        }
    }
}
