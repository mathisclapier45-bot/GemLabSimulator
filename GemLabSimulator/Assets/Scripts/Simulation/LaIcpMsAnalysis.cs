using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LaIcpMsElement
{
    public string Symbol;
    public float Ppm;           // concentration at the surface of the stone, in parts per million
    public bool Light;         // an element the EDXRF cannot see
}

public class LaIcpMsResult
{
    public const int ProfilePoints = 100;
    public const float ProfileDepthUm = 500f;          // the laser digs 500 micrometres down, 5 per point
    public const float DetectionLimit = 0.05f;         // ppm

    public List<LaIcpMsElement> Elements = new List<LaIcpMsElement>();   // strongest first
    public List<string> Analysis = new List<string>();
    public bool RecordAuthenticity;                    // the trace elements settle natural against synthetic
    public bool RecordTreatment;                       // a diffusion, a coating, a dye or a lead glass was found

    public readonly Dictionary<string, float> Bulk = new Dictionary<string, float>();       // ppm in the heart of the stone
    public readonly Dictionary<string, float> Surface = new Dictionary<string, float>();    // ppm a treatment added on the surface
    public readonly Dictionary<string, float> DecayUm = new Dictionary<string, float>();    // how deep that surplus goes (1/e distance)

    // Concentration of an element at every depth of the crater: the heart of the stone plus what the treatment left in the first microns.
    public float[] ProfileOf(string symbol)
    {
        var profile = new float[ProfilePoints];
        Bulk.TryGetValue(symbol, out var bulk);
        Surface.TryGetValue(symbol, out var extra);
        DecayUm.TryGetValue(symbol, out var decay);
        if (decay <= 0f) decay = 1f;

        var seed = 0;
        foreach (var c in symbol) seed += c;
        for (var i = 0; i < ProfilePoints; i++)
        {
            var depth = i * ProfileDepthUm / ProfilePoints;
            var value = bulk + extra * Mathf.Exp(-depth / decay);
            var wobble = 1f + 0.05f * Mathf.Sin(i * 1.9f + seed) + 0.03f * Mathf.Sin(i * 4.3f + seed * 0.7f);
            profile[i] = Mathf.Max(DetectionLimit * 0.5f, value * wobble);
        }
        return profile;
    }

    public float SurfacePpm(string symbol)
    {
        Bulk.TryGetValue(symbol, out var bulk);
        Surface.TryGetValue(symbol, out var extra);
        return bulk + extra;
    }
}

// Laser ablation coupled to a mass spectrometer: a laser vaporises a speck of the stone (a crater a few micrometres wide, 500 deep at the most), a plasma
// ionises it and the spectrometer counts the atoms, element by element, down to a fraction of a part per million. Unlike the X-ray fluorescence it sees
// the light elements too (beryllium, lithium, boron, magnesium, sodium), and the crater gives a depth profile: that is how a beryllium diffusion is
// caught, with the beryllium high at the surface and falling with the depth, or too high all the way down for a natural stone. A given stone always
// gives the same analysis.
public static class LaIcpMsAnalysis
{
    private const float PpmPerWeight = 12000f;

    private static readonly HashSet<string> LightElements = new HashSet<string> { "Be", "Li", "B", "Na", "Mg" };

    private static bool IsCorundum(string n) => n.StartsWith("Corindon") || n.StartsWith("Saphir") || n == "Padparadscha";

    public static LaIcpMsResult Measure(GemInstance gem)
    {
        var result = new LaIcpMsResult();
        var name = gem.Definition.name ?? "";
        var synthetic = gem.IsSynthetic || gem.Definition.forcedSynthetic;
        float Unit(string salt) => (StoneOptics.StableHash((gem.SpecimenId ?? "") + "|lams|" + salt) % 1000u) / 1000f;

        var notes = new List<string>();
        var weights = EdxrfSpectrum.BaseWeights(gem, notes, out var conclusive);
        foreach (var pair in weights) AddBulk(result, pair.Key, pair.Value * PpmPerWeight * Mathf.Lerp(0.85f, 1.15f, Unit(pair.Key)));

        var corundum = IsCorundum(name);
        AddLightElements(result, name, corundum, synthetic, Unit);

        // The origin notes of the stone (deposit, synthesis): the same reading as the X-ray fluorescence, but for diamonds only the conclusive ones.
        if (!name.StartsWith("Diamant") || conclusive) result.Analysis.AddRange(notes);

        TreatmentTraces(result, gem, name, corundum, synthetic, Unit);

        result.RecordAuthenticity = conclusive || corundum;
        Finish(result);
        return result;
    }

    // One line for the stone sheet and the report.
    public static string Summary(GemInstance gem)
    {
        var result = Measure(gem);
        if (result.Elements.Count == 0) return "aucun élément détecté";

        var sb = new StringBuilder("éléments : ");
        for (var i = 0; i < result.Elements.Count && i < 5; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(result.Elements[i].Symbol).Append(' ').Append(FormatPpm(result.Elements[i].Ppm)).Append(" ppm");
        }
        if (result.RecordTreatment) sb.Append(" — traitement : ").Append(gem.TreatmentGrade);
        return sb.ToString();
    }

    public static string FormatPpm(float ppm)
    {
        if (ppm >= 100f) return ppm.ToString("0");
        if (ppm >= 10f) return ppm.ToString("0.#");
        return ppm.ToString("0.0#");
    }

    private static void AddBulk(LaIcpMsResult result, string symbol, float ppm)
    {
        if (ppm <= 0f) return;
        result.Bulk[symbol] = result.Bulk.TryGetValue(symbol, out var current) ? current + ppm : ppm;
    }

    private static void AddSurface(LaIcpMsResult result, string symbol, float ppm, float decayUm)
    {
        result.Surface[symbol] = result.Surface.TryGetValue(symbol, out var current) ? current + ppm : ppm;
        result.DecayUm[symbol] = decayUm;
    }

    // The light elements the X-ray method cannot see.
    private static void AddLightElements(LaIcpMsResult result, string name, bool corundum, bool synthetic, System.Func<string, float> unit)
    {
        if (corundum)
        {
            if (synthetic)
            {
                AddBulk(result, "Mg", 1f + 2f * unit("mg"));
                return;
            }
            AddBulk(result, "Mg", 30f + 90f * unit("mg"));
            AddBulk(result, "Be", 0.3f + 2.2f * unit("be"));
            AddBulk(result, "Li", 0.1f + 0.5f * unit("li"));
            AddBulk(result, "Na", 5f + 20f * unit("na"));
            return;
        }

        if (name == "Emeraude")
        {
            AddBulk(result, "Be", synthetic ? 49000f : 51000f);
            AddBulk(result, "Na", synthetic ? 20f : 2500f + 1800f * unit("na"));
            AddBulk(result, "Li", synthetic ? 0.5f : 60f + 200f * unit("li"));
            AddBulk(result, "Mg", synthetic ? 4f : 2500f + 1500f * unit("mg"));
            return;
        }

        if (name.StartsWith("Tourmaline"))
        {
            AddBulk(result, "B", 32000f);
            AddBulk(result, "Na", 9000f + 3000f * unit("na"));
            AddBulk(result, "Li", 200f + 1800f * unit("li"));
            AddBulk(result, "Mg", 150f + 500f * unit("mg"));
            return;
        }

        if (name.StartsWith("Diamant")) return;

        AddBulk(result, "Na", 30f + 200f * unit("na"));
        AddBulk(result, "Mg", 20f + 120f * unit("mg"));
        AddBulk(result, "Li", 0.5f + 3f * unit("li"));
    }

    // What the treatments left in the first micrometres: the element of a diffusion, the metal of a coating, the colouring matter of a dye, the lead glass of a filling.
    private static void TreatmentTraces(LaIcpMsResult result, GemInstance gem, string name, bool corundum, bool synthetic, System.Func<string, float> unit)
    {
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité")
        {
            NaturalBeryllium(result, corundum, synthetic);
            return;
        }

        var treated = false;

        if (gem.HasTreatment(TreatmentKind.Diffused) && !string.IsNullOrEmpty(gem.DiffusionElement))
        {
            treated = true;
            var depthMm = gem.DiffusionDepthMm;
            var decay = Mathf.Max(8f, depthMm * 1000f / 2.5f);
            switch (gem.DiffusionElement)
            {
                case "Béryllium":
                {
                    var surface = 25f + 90f * unit("beSurface");
                    AddSurface(result, "Be", surface, decay);
                    var atBottom = result.ProfileOf("Be")[LaIcpMsResult.ProfilePoints - 1];
                    if (depthMm >= 0.5f)
                        result.Analysis.Add($"Béryllium : {FormatPpm(result.SurfacePpm("Be"))} ppm en surface et encore {FormatPpm(atBottom)} ppm à 500 µm. Un corindon naturel en contient moins de 3 ppm, constant : diffusion à cœur au béryllium.");
                    else
                        result.Analysis.Add($"Béryllium : {FormatPpm(result.SurfacePpm("Be"))} ppm en surface, qui retombe en quelques dizaines de microns (naturel : moins de 3 ppm, constant) : diffusion superficielle au béryllium.");
                    break;
                }
                case "Fer + titane":
                    AddSurface(result, "Fe", 2500f, decay);
                    AddSurface(result, "Ti", 1800f, decay);
                    result.Analysis.Add("Fer et titane très élevés en surface, retombant avec la profondeur : diffusion fer + titane (saphir bleu de surface).");
                    break;
                case "Chrome":
                    AddSurface(result, "Cr", 3500f, decay);
                    result.Analysis.Add("Chrome très élevé en surface, retombant avec la profondeur : diffusion au chrome (rubis de surface).");
                    break;
                case "Nickel":
                    AddSurface(result, "Ni", 2000f, decay);
                    result.Analysis.Add("Nickel en surface dans un corindon qui n'en contient pas : diffusion au nickel (saphir jaune de surface).");
                    break;
                case "Titane en excès":
                    AddSurface(result, "Ti", 3000f, decay);
                    result.Analysis.Add("Titane en excès à la surface : diffusion de titane (astérisme de surface).");
                    break;
            }
        }
        else NaturalBeryllium(result, corundum, synthetic);

        if (gem.HasTreatment(TreatmentKind.Coated))
        {
            var found = new List<string>();
            void Metal(string symbol, string label, float ppm)
            {
                AddSurface(result, symbol, ppm, 1.5f);
                found.Add(label);
            }
            switch (gem.CoatingName)
            {
                case "Aqua aura": Metal("Au", "or", 8000f); break;
                case "Mystic": Metal("Ti", "titane", 7000f); break;
                case "Bleu cobalt": Metal("Co", "cobalt", 7000f); break;
                case "Mystic feu": Metal("Au", "or", 6000f); Metal("Ti", "titane", 6500f); break;
                case "Aqua profond": Metal("Au", "or", 6000f); Metal("Co", "cobalt", 6500f); break;
                case "Crépuscule": Metal("Ti", "titane", 6500f); Metal("Co", "cobalt", 6500f); break;
                case "Émeraude Lechleitner": Metal("Cr", "chrome", 7000f); break;
            }
            if (found.Count > 0)
            {
                treated = true;
                result.Analysis.Add($"{Capitalise(string.Join(" et ", found))} concentré dans le premier micron, absent dès 5 µm : revêtement déposé sous vide.");
            }
        }

        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Bleu de Prusse":
                    AddSurface(result, "Fe", 3000f, 30f);
                    AddSurface(result, "K", 800f, 30f);
                    treated = true;
                    result.Analysis.Add("Fer et potassium dans les fissures de surface : teinture au bleu de Prusse.");
                    break;
                case "Vert chrome":
                    AddSurface(result, "Cr", 3500f, 30f);
                    treated = true;
                    result.Analysis.Add("Chrome dans les fissures de surface, retombant vite : teinture au chrome.");
                    break;
                default:
                    result.Analysis.Add("Aucun élément de teinture : un colorant organique ne se lit pas ici.");
                    break;
            }
        }

        var filler = RamanSpectrum.FillerOf(gem, name, StoneOptics.StableHash((gem.SpecimenId ?? "") + "|filler"));
        if (filler == "Verre au plomb")
        {
            AddSurface(result, "Pb", 9000f, 40f);
            AddSurface(result, "K", 1500f, 40f);
            treated = true;
            result.Analysis.Add("Plomb à plusieurs milliers de ppm, très irrégulier d'un point à l'autre : verre au plomb dans les fractures.");
        }

        if (!treated && result.Analysis.Count == 0 && (gem.HasTreatment(TreatmentKind.Heated) || gem.HasTreatment(TreatmentKind.Irradiated)
            || gem.HasTreatment(TreatmentKind.Bleached) || gem.HasTreatment(TreatmentKind.Laser) || gem.HasTreatment(TreatmentKind.Hpht)))
            result.Analysis.Add("Aucun élément étranger : chauffage, irradiation, blanchiment, laser et HPHT ne laissent pas de trace chimique.");

        result.RecordTreatment = treated;
    }

    private static void NaturalBeryllium(LaIcpMsResult result, bool corundum, bool synthetic)
    {
        if (!corundum) return;
        if (synthetic)
            result.Analysis.Add("Béryllium, lithium et sodium sous la limite de détection : profil d'un corindon de synthèse.");
        else
            result.Analysis.Add($"Béryllium : {FormatPpm(result.SurfacePpm("Be"))} ppm, constant sur les 500 µm : béryllium naturel, aucune diffusion.");
    }

    private static string Capitalise(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

    private static void Finish(LaIcpMsResult result)
    {
        var symbols = new HashSet<string>(result.Bulk.Keys);
        foreach (var key in result.Surface.Keys) symbols.Add(key);
        foreach (var symbol in symbols)
        {
            var ppm = result.SurfacePpm(symbol);
            if (ppm < LaIcpMsResult.DetectionLimit) continue;
            result.Elements.Add(new LaIcpMsElement { Symbol = symbol, Ppm = ppm, Light = LightElements.Contains(symbol) });
        }
        result.Elements.Sort((a, b) => b.Ppm.CompareTo(a.Ppm));
    }
}
