using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public struct EdxrfPeak
{
    public string Symbol;
    public float KeV;
    public float Height;
    public bool Main;         // the strongest line of the element: the one that carries the label
}

public class EdxrfResult
{
    public const float MaxKeV = 40f;
    public const int Points = 2001;       // one point per 0.02 keV; index i is energy i * 0.02

    public float[] Spectrum;
    public List<EdxrfPeak> Peaks = new List<EdxrfPeak>();
    public List<string> Elements = new List<string>();      // strongest first
    public List<string> Analysis = new List<string>();
    public bool RecordAuthenticity;                         // the trace elements settle natural against synthetic
    public bool RecordTreatment;                            // a lead glass filler was found
}

// Energy-dispersive X-ray fluorescence spectrum (0 to 40 keV) of a gem: an X-ray tube excites the stone and every element answers with
// its own characteristic lines, the more intense the more of it there is. The method sees the trace elements that color the stone and
// betray its deposit or its synthesis, and is blind to the light elements (Be, Li, B, C, O, Na, Mg, Al, Si). A given stone always gives
// the same spectrum.
public static class EdxrfSpectrum
{
    // Lines of each element: (energy keV, height relative to the strongest line).
    private static readonly Dictionary<string, (float keV, float rel)[]> Lines = new Dictionary<string, (float, float)[]>
    {
        { "K", new[] { (3.31f, 1f), (3.59f, 0.1f) } },
        { "Ca", new[] { (3.69f, 1f), (4.01f, 0.13f) } },
        { "Sc", new[] { (4.09f, 1f) } },
        { "Cs", new[] { (4.29f, 1f) } },
        { "Ti", new[] { (4.51f, 1f), (4.93f, 0.15f) } },
        { "V", new[] { (4.95f, 1f), (5.43f, 0.15f) } },
        { "Cr", new[] { (5.41f, 1f), (5.95f, 0.15f) } },
        { "Mn", new[] { (5.90f, 1f), (6.49f, 0.15f) } },
        { "Gd", new[] { (6.06f, 1f) } },
        { "Fe", new[] { (6.40f, 1f), (7.06f, 0.15f) } },
        { "Co", new[] { (6.93f, 1f), (7.65f, 0.15f) } },
        { "Ni", new[] { (7.47f, 1f), (8.26f, 0.15f) } },
        { "Hf", new[] { (7.90f, 1f) } },
        { "Cu", new[] { (8.05f, 1f), (8.91f, 0.15f) } },
        { "Zn", new[] { (8.64f, 1f), (9.57f, 0.15f) } },
        { "Ga", new[] { (9.25f, 1f), (10.26f, 0.15f) } },
        { "Au", new[] { (9.71f, 1f), (11.44f, 0.55f) } },
        { "Pb", new[] { (10.55f, 1f), (12.61f, 0.8f) } },
        { "Se", new[] { (11.22f, 1f) } },
        { "Rb", new[] { (13.40f, 1f), (14.96f, 0.2f) } },
        { "Sr", new[] { (14.16f, 1f), (15.83f, 0.2f) } },
        { "Y", new[] { (14.96f, 1f), (16.74f, 0.2f) } },
        { "Zr", new[] { (15.77f, 1f), (17.67f, 0.2f) } },
        { "Mo", new[] { (17.48f, 1f), (19.61f, 0.2f) } },
    };

    private const float Counts = 1000f;
    private const float Detection = 0.015f;

    public static EdxrfResult Measure(GemInstance gem)
    {
        var def = gem.Definition;
        var name = def.name ?? "";
        var synthetic = gem.IsSynthetic || def.forcedSynthetic;
        var rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|edxrf") & 0x7FFFFFFF));
        uint Hash(string salt) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|" + salt);

        var weights = new Dictionary<string, float>();
        void Add(string symbol, float weight)
        {
            if (weight <= 0f) return;
            weights[symbol] = weights.TryGetValue(symbol, out var current) ? current + weight : weight;
        }

        var result = new EdxrfResult();
        var conclusive = false;
        Traces(gem, name, synthetic, Hash, Add, result, ref conclusive);

        // A lead glass filling a fracture brings its own elements.
        var filler = RamanSpectrum.FillerOf(gem, name, Hash("filler"));
        if (filler == "Verre au plomb")
        {
            Add("Pb", 0.35f);
            Add("K", 0.1f);
            result.RecordTreatment = true;
            result.Analysis.Add("Plomb détecté dans une pierre qui n'en contient pas : verre au plomb de comblement.");
        }

        TreatmentElements(gem, Add, result);

        result.RecordAuthenticity = conclusive;
        Build(result, weights, rng);
        return result;
    }

    // The elements the treatments brought: the metal of a coating, the element of a surface diffusion, the colouring matter of a mineral dye. The light
    // elements (beryllium, carbon, oxygen...) stay invisible: a beryllium diffusion, an organic dye, a heating or a laser leave nothing for this method.
    private static void TreatmentElements(GemInstance gem, Action<string, float> add, EdxrfResult result)
    {
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return;

        if (gem.HasTreatment(TreatmentKind.Coated))
        {
            var found = new List<string>();
            switch (gem.CoatingName)
            {
                case "Aqua aura": add("Au", 0.45f); found.Add("or"); break;
                case "Mystic": add("Ti", 0.5f); found.Add("titane"); break;
                case "Bleu cobalt": add("Co", 0.5f); found.Add("cobalt"); break;
                case "Mystic feu": add("Au", 0.4f); add("Ti", 0.45f); found.Add("or"); found.Add("titane"); break;
                case "Aqua profond": add("Au", 0.4f); add("Co", 0.45f); found.Add("or"); found.Add("cobalt"); break;
                case "Crépuscule": add("Ti", 0.45f); add("Co", 0.45f); found.Add("titane"); found.Add("cobalt"); break;
                case "Émeraude Lechleitner": add("Cr", 0.5f); found.Add("chrome"); break;
            }
            if (found.Count > 0)
            {
                result.RecordTreatment = true;
                result.Analysis.Add($"{string.Join(" et ", found)} en surface, que la pierre ne contient pas : revêtement déposé sous vide.");
            }
        }

        if (gem.HasTreatment(TreatmentKind.Diffused))
        {
            switch (gem.DiffusionElement)
            {
                case "Fer + titane": add("Fe", 0.35f); add("Ti", 0.4f); break;
                case "Chrome": add("Cr", 0.45f); break;
                case "Nickel": add("Ni", 0.4f); break;
                case "Titane en excès": add("Ti", 0.45f); break;
                case "Béryllium":
                    result.Analysis.Add("Le béryllium est un élément trop léger pour cette méthode : une diffusion à cœur n'apparaît pas.");
                    break;
            }
            if (gem.DiffusionElement != null && gem.DiffusionElement != "Béryllium")
            {
                result.RecordTreatment = true;
                result.Analysis.Add($"Teneur anormale en {gem.DiffusionElement.ToLowerInvariant()} dans une pierre de ce type : diffusion en surface probable.");
            }
        }

        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Bleu de Prusse": add("Fe", 0.35f); add("K", 0.1f); result.RecordTreatment = true; result.Analysis.Add("Fer et potassium en surface : bleu de Prusse, teinture minérale."); break;
                case "Vert chrome": add("Cr", 0.4f); result.RecordTreatment = true; result.Analysis.Add("Chrome en surface dans une pierre qui n'en contient pas : teinture au chrome."); break;
                default: result.Analysis.Add("Aucun élément de teinture visible : un colorant organique ou le carbone ne se lisent pas ici."); break;
            }
        }
    }

    // One line for the stone sheet and the report.
    public static string Summary(GemInstance gem)
    {
        var result = Measure(gem);
        return result.Elements.Count == 0 ? "aucun élément détecté" : $"éléments traces : {string.Join(", ", result.Elements)}";
    }

    // ---- Trace elements of each species ---------------------------------------------------------------------------

    private static bool IsCorundum(string n) => n.StartsWith("Corindon") || n.StartsWith("Saphir") || n == "Padparadscha";

    private static void Traces(GemInstance g, string name, bool synthetic, Func<string, uint> hash, Action<string, float> add,
        EdxrfResult result, ref bool conclusive)
    {
        var iron = UvVisNirSpectrum.DepositIron(g);
        var tone = Mathf.Lerp(0.5f, 1.4f, Mathf.Clamp01(g.ColorSaturation * 1.3f));
        bool Has(string keyword) => g.Origin != null && g.Origin.name != null && g.Origin.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        var basalt = Has("Tha") || Has("Australie") || Has("Nigeria") || Has("Cambodge");

        if (IsCorundum(name))
        {
            conclusive = true;
            var ruby = name == "Corindon_Rubis" || name == "Corindon_Synthetique_Rubis" || name == "Saphir_Rose";
            if (ruby) add("Cr", 0.45f + 0.4f * tone * (name == "Saphir_Rose" ? 0.4f : 1f));
            if (name == "Saphir_Bleu" || name == "Corindon_Synthetique_Saphir" || name == "Saphir_Violet") add("Ti", 0.3f);

            if (synthetic)
            {
                if (ruby && hash("flux") % 100 < 25)
                {
                    add("Pb", 0.28f);
                    result.Analysis.Add("Plomb sans fer : résidu du fondant d'un corindon synthétique (croissance en flux).");
                }
                else result.Analysis.Add("Ni fer, ni gallium, ni vanadium : profil d'un corindon de synthèse (fusion à la flamme).");
                return;
            }

            add("Fe", 0.04f + 0.7f * iron * (name == "Saphir_Jaune" || name == "Saphir_Vert" ? 1.5f : 1f));
            add("Ga", basalt ? 0.24f : 0.05f);
            if (ruby) add("V", Has("Birmanie") ? 0.22f : 0.06f);
            add("Ti", 0.06f);

            if (iron < 0.25f) result.Analysis.Add("Fer très faible : gisement métamorphique de type Cachemire ou Mogok.");
            else if (iron < 0.55f) result.Analysis.Add("Fer faible : gisement métamorphique de type Sri Lanka.");
            else if (basalt) result.Analysis.Add("Fer élevé et gallium net : gisement lié au basalte (Thaïlande, Australie, Nigeria...).");
            else result.Analysis.Add("Fer modéré : gisement intermédiaire (Madagascar, Mozambique...).");
            return;
        }

        switch (name)
        {
            case "Emeraude":
                conclusive = true;
                add("Cr", 0.55f * tone);
                if (synthetic)
                {
                    if (hash("flux") % 100 < 55)
                    {
                        add("V", 0.3f);
                        add("Mo", 0.2f);
                        result.Analysis.Add("Molybdène sans fer ni alcalins : résidu du fondant d'une émeraude de synthèse (flux).");
                    }
                    else
                    {
                        add("Cu", 0.35f);
                        add("Ni", 0.3f);
                        result.Analysis.Add("Cuivre et nickel, sans fer : émeraude de synthèse hydrothermale.");
                    }
                    break;
                }
                add("V", Has("Colombie") ? 0.45f : 0.18f);
                add("Fe", 0.05f + 0.6f * iron);
                if (!Has("Colombie")) { add("Rb", 0.1f); add("Cs", 0.08f); }
                result.Analysis.Add(Has("Colombie")
                    ? "Fer très faible, vanadium net : profil des émeraudes de Colombie."
                    : "Fer, rubidium et césium présents : émeraude naturelle riche en alcalins.");
                break;

            case "Spinelle":
            case "Spinelle_Bleu":
                conclusive = true;
                if (name == "Spinelle") add("Cr", 0.45f * tone); else add("Co", 0.5f * tone);
                if (synthetic) { result.Analysis.Add("Ni zinc ni gallium : spinelle de synthèse (fusion à la flamme)."); break; }
                add("Fe", 0.05f + 0.5f * iron);
                add("Zn", 0.35f);
                add("Ga", 0.12f);
                add("V", 0.05f);
                result.Analysis.Add("Zinc, gallium et fer présents : spinelle naturel.");
                break;

            case "Diamant":
            case "Diamant_Synthetique_HPHT":
            case "Diamant_Synthetique_CVD":
                if (synthetic && DiamondViewImage.KindOf(g) == DiamondViewKind.Hpht)
                {
                    conclusive = true;
                    add("Fe", 0.25f);
                    add("Ni", 0.30f);
                    add("Co", 0.18f);
                    result.Analysis.Add("Fer, nickel et cobalt : résidus du catalyseur métallique d'un diamant HPHT de synthèse.");
                }
                else result.Analysis.Add("Aucun élément détecté : le carbone est trop léger. Naturel ou CVD : voir DiamondView et FTIR.");
                break;

            case "Moissanite_Synthetique":
                result.Analysis.Add("Silicium et carbone, trop légers : aucun élément détecté.");
                break;
            case "CZ": add("Zr", 1f); add("Y", 0.2f); add("Hf", 0.03f); conclusive = true; result.Analysis.Add("Zirconium et yttrium : zircone cubique, matériau de synthèse."); break;
            case "YAG": add("Y", 1f); conclusive = true; result.Analysis.Add("Yttrium : grenat de synthèse (YAG)."); break;
            case "GGG": add("Gd", 1f); add("Ga", 0.9f); conclusive = true; result.Analysis.Add("Gadolinium et gallium : grenat de synthèse (GGG)."); break;
            case "Verre":
                add("K", 0.9f); add("Ca", 0.5f); add("Ti", 0.08f); add("Fe", 0.15f); add("Cu", 0.25f); add("Pb", 0.35f); add("Rb", 0.12f); add("Sr", 0.2f);
                result.Analysis.Add("Potassium et plomb : verre à forte teneur en potassium (comme le verre rouge du Moyen-Âge).");
                break;
            case "Obsidienne": add("Fe", 0.5f); add("K", 0.4f); add("Ca", 0.2f); add("Rb", 0.1f); add("Sr", 0.05f); add("Zr", 0.08f); add("Ti", 0.1f); break;

            case "Amethyste": case "Ametrine": add("Fe", 0.12f); break;
            case "Citrine": case "Prasiolite": case "Quartz_Fume": case "Quartz_Rose": case "Quartz": add("Fe", 0.06f); break;

            case "Grenat_Almandin": case "Grenat_Rhodolite": add("Fe", 1f); add("Mn", 0.2f); add("Ca", 0.15f); add("Ti", 0.05f); add("Y", 0.03f); break;
            case "Grenat_Pyrope": add("Cr", 0.2f); add("Fe", 0.25f); add("Ca", 0.15f); add("Ti", 0.05f); break;
            case "Grenat_Spessartite": case "Grenat_Malaya": add("Mn", 1f); add("Fe", 0.35f); break;
            case "Grenat_Tsavorite": add("Ca", 0.5f); add("V", 0.2f); add("Cr", 0.1f); add("Fe", 0.05f); break;
            case "Grenat_Demantoide": add("Fe", 0.6f); add("Ca", 0.3f); add("Cr", 0.08f); break;
            case "Grenat_Hessonite": case "Grenat_Hydrogrossulaire": add("Ca", 0.5f); add("Fe", 0.3f); break;

            case "Topaze": add("Fe", 0.06f); add("Ga", 0.08f); add("Mn", 0.02f); break;
            case "Tourmaline_Paraiba": add("Cu", 0.8f); add("Mn", 0.3f); add("Fe", 0.05f); add("Zn", 0.15f); add("Ga", 0.06f); break;
            case "Tourmaline_Rose": add("Mn", 0.5f); add("Fe", 0.1f); add("Ga", 0.08f); add("Zn", 0.1f); add("Ca", 0.1f); break;
            case "Zircon": add("Zr", 1f); add("Hf", 0.08f); add("Y", 0.05f); add("Fe", 0.05f); break;
            case "Peridot": add("Fe", 0.6f); add("Ni", 0.15f); add("Ca", 0.08f); add("Cr", 0.05f); add("Mn", 0.05f); break;
            case "Apatite": add("Ca", 1f); add("Sr", 0.15f); add("Mn", 0.05f); add("Y", 0.06f); add("Fe", 0.03f); break;
            case "Fluorite": add("Ca", 1f); add("Sr", 0.1f); add("Y", 0.1f); break;
            case "Opale_Noble": case "Opale_De_Feu": add("Fe", 0.06f); add("Ca", 0.05f); add("Ti", 0.03f); break;
            case "Turquoise": case "Turquoise_Stabilisee": add("Cu", 1f); add("Fe", 0.35f); add("Zn", 0.12f); add("Ca", 0.05f); break;
            case "Jade_Jadeite": add("Cr", 0.15f); add("Fe", 0.3f); add("Ca", 0.1f); add("Ti", 0.03f); break;
            case "Jade_Nephrite": add("Fe", 0.9f); add("Ni", 0.12f); add("Mn", 0.1f); add("Cr", 0.05f); add("Ca", 0.5f); break;
            case "Ivoire_Elephant": case "Ivoire_Hippopotame": case "Ivoire_Mammouth": case "Ivoire_Morse": case "Defense_Narval":
            case "Ecaille_Tortue": case "Corne_Rhinoceros": add("Ca", 0.5f); add("Sr", 0.05f); add("Zn", 0.04f); add("Fe", 0.03f); break;
            default: add("Fe", 0.1f); add("Ca", 0.05f); break;
        }

        if (conclusive && !synthetic && result.Analysis.Count == 0) result.Analysis.Add("Composition en éléments traces d'une pierre naturelle.");
    }

    // ---- Spectrum -------------------------------------------------------------------------------------------------

    private static void Build(EdxrfResult result, Dictionary<string, float> weights, System.Random rng)
    {
        var n = EdxrfResult.Points;
        var clean = new float[n];

        // Background: scattered tube radiation, falling with the energy.
        for (var i = 0; i < n; i++)
        {
            var e = i * 0.02f;
            clean[i] = 38f * Mathf.Exp(-e / 7f) + 9f;
        }

        // Rayleigh and Compton scattering of the rhodium tube: present on every spectrum, they are not elements of the stone.
        AddPeak(clean, 20.22f, 0.45f * Counts * 0.5f);
        AddPeak(clean, 22.72f, 0.12f * Counts * 0.5f);
        AddPeak(clean, 19.6f, 0.10f * Counts * 0.5f, 1.8f);
        result.Peaks.Add(new EdxrfPeak { Symbol = "Rh (tube)", KeV = 20.22f, Height = 0.45f * Counts * 0.5f, Main = true });

        var order = new List<KeyValuePair<string, float>>();
        foreach (var pair in weights)
        {
            if (pair.Value < Detection || !Lines.ContainsKey(pair.Key)) continue;
            order.Add(pair);
            var first = true;
            foreach (var line in Lines[pair.Key])
            {
                var height = pair.Value * line.rel * Counts;
                AddPeak(clean, line.keV, height);
                result.Peaks.Add(new EdxrfPeak { Symbol = pair.Key, KeV = line.keV, Height = height, Main = first });
                first = false;
            }
        }

        order.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var pair in order) result.Elements.Add(pair.Key);

        result.Spectrum = new float[n];
        for (var i = 0; i < n; i++)
            result.Spectrum[i] = Mathf.Max(0f, clean[i] + Gauss(rng) * (1.5f + 0.7f * Mathf.Sqrt(clean[i])));
    }

    private static void AddPeak(float[] a, float keV, float height, float fwhm = -1f)
    {
        if (fwhm < 0f) fwhm = 0.14f + 0.012f * keV;
        var sigma = fwhm / 2.3548f;
        var lo = Mathf.Max(0, Mathf.FloorToInt((keV - 4f * fwhm) / 0.02f));
        var hi = Mathf.Min(a.Length - 1, Mathf.CeilToInt((keV + 4f * fwhm) / 0.02f));
        for (var i = lo; i <= hi; i++)
        {
            var d = (i * 0.02f - keV) / sigma;
            a[i] += height * Mathf.Exp(-0.5f * d * d);
        }
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
