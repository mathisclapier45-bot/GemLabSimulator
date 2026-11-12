using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// What the laser is pointed at: the host crystal, an inclusion (found under the microscope) or a fracture where a filler may sit.
public enum RamanTarget { Host, Inclusion, Fracture }

public class RamanMatch
{
    public string Name;
    public int Score;            // 0-100, how close the library spectrum is to the measured one
    public float[] Reference;    // library spectrum, normalized to 1
}

public class RamanResult
{
    public const float MinShift = 100f;
    public const float MaxShift = 3500f;
    public const int Points = 3401;      // one point per cm-1; index i is shift MinShift + i

    public float[] Intensity;            // counts
    public bool HasSignal;
    public string Description;           // what was measured
    public List<RamanMatch> Matches = new List<RamanMatch>();
    public List<string> PeakLines = new List<string>();
    public List<string> Analysis = new List<string>();
    public bool RecordAuthenticity;      // the spectrum settles natural against synthetic
    public bool RecordTreatment;         // a filler was identified
}

// Library of reference spectra: peaks as (shift cm-1, width cm-1, relative height). Species, minerals found as inclusions,
// fluids and fillers share the same library, which is also what the measured spectra are built from.
public static class RamanLibrary
{
    private struct Peak
    {
        public float Shift;
        public float Width;
        public float Height;
    }

    private static readonly Dictionary<string, Peak[]> entries = new Dictionary<string, Peak[]>();
    private static bool built;

    private static void Define(string name, string peaks)
    {
        var list = new List<Peak>();
        foreach (var part in peaks.Split(','))
        {
            var f = part.Trim().Split(':');
            list.Add(new Peak
            {
                Shift = float.Parse(f[0], CultureInfo.InvariantCulture),
                Width = float.Parse(f[1], CultureInfo.InvariantCulture),
                Height = float.Parse(f[2], CultureInfo.InvariantCulture),
            });
        }
        entries[name] = list.ToArray();
    }

    private static void Build()
    {
        if (built) return;
        built = true;

        // Gem species.
        Define("Diamant", "1332:5:1");
        Define("Corindon", "378:5:0.35,418:5:1,432:5:0.4,451:5:0.45,578:6:0.3,645:6:0.35,751:6:0.25");
        Define("Quartz", "128:8:0.3,206:10:0.35,464:8:1,1081:12:0.15");
        Define("Béryl", "324:10:0.3,686:8:1,1010:10:0.25,1068:10:0.4");
        Define("Topaze", "267:8:0.5,331:8:0.4,405:10:0.25,928:8:1,1100:10:0.2");
        Define("Grenat almandin", "346:10:0.45,556:8:0.4,636:8:0.15,923:6:1,1050:12:0.25");
        Define("Grenat pyrope", "358:8:0.5,557:8:0.4,919:6:1,1050:12:0.2");
        Define("Grenat spessartite", "349:8:0.5,553:8:0.4,912:6:1");
        Define("Grenat grossulaire", "372:8:0.6,543:8:0.5,880:6:1,1042:10:0.2");
        Define("Grenat andradite", "369:8:0.55,513:8:0.3,875:6:1,1034:10:0.2");
        Define("Spinelle", "310:12:0.3,407:25:0.4,665:30:1,767:15:0.3");
        Define("Péridot", "435:8:0.3,545:8:0.2,824:6:1,856:6:0.85,920:8:0.2,962:8:0.15");
        Define("Tourmaline", "372:10:0.35,712:10:1,1010:10:0.2,1065:12:0.4");
        Define("Zircon", "357:6:0.3,439:8:0.2,975:6:0.3,1008:5:1");
        Define("Chrysobéryl", "304:8:0.4,566:8:0.3,636:8:0.5,926:8:0.3,1003:8:1");
        Define("Apatite", "430:8:0.35,589:8:0.4,965:5:1,1045:8:0.3");
        Define("Zoïsite", "330:10:0.3,450:10:0.3,559:8:0.5,660:8:0.4,695:10:0.35,917:10:1,1070:10:0.3");
        Define("Cordiérite", "420:12:0.4,570:15:0.4,968:12:1,1180:20:0.2");
        Define("Kyanite", "300:10:0.3,487:10:0.4,897:12:1,950:10:0.5");
        Define("Fluorite", "322:8:1");
        Define("Jadéite", "375:8:0.3,700:8:1,989:7:0.8,1037:7:0.6");
        Define("Néphrite", "420:10:0.3,675:6:0.9,920:8:0.4,1060:10:0.4");
        Define("Serpentine", "230:10:0.4,390:8:0.7,690:8:1,1100:12:0.3");
        Define("Opale", "330:150:0.5,780:200:0.3");
        Define("Turquoise", "420:20:0.3,640:10:0.3,1040:15:1,1090:12:0.3");
        Define("Feldspath", "288:10:0.3,475:10:1,512:10:0.8");
        Define("Rhodonite", "340:10:0.3,998:6:1,670:8:0.4");
        Define("Rhodocrosite", "285:8:0.4,716:6:0.3,1086:6:1");
        Define("Hématite", "225:8:0.6,293:8:0.8,411:10:0.5,497:10:0.3,611:10:0.3");
        Define("Pyrite", "343:8:0.8,379:8:1,430:8:0.3");
        Define("Ivoire / matière organique", "960:8:1,1070:10:0.3,1450:20:0.5,1670:40:0.5,2940:50:0.6");

        // Synthetics and imitations.
        Define("Cubic zirconia (CZ)", "260:60:0.5,470:60:0.5,620:70:1");
        Define("Moissanite (SiC)", "766:6:0.6,789:6:1,797:6:0.5,967:8:0.4");
        Define("YAG", "162:6:0.3,261:8:0.4,372:8:1,690:12:0.35,783:10:0.4");
        Define("GGG", "175:8:0.4,300:10:0.5,375:8:0.4,550:10:0.3,745:12:1");
        Define("Verre (amorphe)", "450:200:1,800:150:0.5,1100:120:0.4");

        // Minerals found as inclusions, fluids and fillers.
        Define("Rutile", "143:8:0.4,235:30:0.35,447:12:0.8,612:15:1");
        Define("Calcite", "156:8:0.3,282:8:0.5,712:6:0.2,1086:6:1");
        Define("Graphite", "1350:30:0.4,1580:20:1,2700:60:0.5");
        Define("Mica (muscovite)", "264:10:0.4,705:10:1,1100:20:0.2");
        Define("Albite", "290:8:0.3,479:8:1,507:8:0.9");
        Define("CO2 (gaz)", "1285:6:0.6,1388:6:1");
        Define("Eau", "3400:250:1");
        Define("Résine époxy", "1004:5:0.6,1112:8:0.3,1186:8:0.5,1250:15:0.4,1450:15:0.6,1610:8:1,2930:40:0.5,3065:20:0.4");
        Define("Huile de cèdre", "1300:15:0.5,1440:12:0.8,1660:10:0.3,2850:15:0.6,2880:15:0.9,2930:20:1");
        Define("Verre au plomb", "150:30:0.3,350:80:0.5,970:150:1");
        Define("Paraffine (cire)", "1063:6:0.5,1130:6:0.4,1296:8:0.6,1440:10:0.7,2850:12:1,2883:12:0.9");
        Define("Verre borate (fondant)", "500:120:0.8,770:60:0.4,1300:80:0.5");
    }

    public static IEnumerable<string> Names
    {
        get
        {
            Build();
            return entries.Keys;
        }
    }

    public static bool Has(string name)
    {
        Build();
        return entries.ContainsKey(name);
    }

    // A species with no entry of its own gets a stable, plausible set of peaks, drawn from its name.
    public static void EnsureSpecies(string key)
    {
        Build();
        if (entries.ContainsKey(key)) return;
        var rng = new System.Random((int)(StoneOptics.StableHash(key) & 0x7FFFFFFF));
        var sb = new StringBuilder();
        var count = 3 + rng.Next(3);
        for (var i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            var shift = 200 + rng.Next(900);
            sb.Append(shift).Append(':').Append(6 + rng.Next(8)).Append(':').Append((i == 0 ? 1f : 0.2f + (float)rng.NextDouble() * 0.5f).ToString(CultureInfo.InvariantCulture));
        }
        Define(key, sb.ToString());
    }

    // Spectrum of the entry, normalized to 1, with every peak moved by `jitter` cm-1 (calibration and strain).
    public static float[] Render(string name, float jitter = 0f)
    {
        Build();
        var a = new float[RamanResult.Points];
        foreach (var p in entries[name])
        {
            var center = p.Shift + jitter;
            var sigma = p.Width / 2.3548f;
            var lo = Mathf.Max(0, Mathf.FloorToInt(center - 4f * p.Width - RamanResult.MinShift));
            var hi = Mathf.Min(a.Length - 1, Mathf.CeilToInt(center + 4f * p.Width - RamanResult.MinShift));
            for (var i = lo; i <= hi; i++)
            {
                var d = (RamanResult.MinShift + i - center) / sigma;
                a[i] += p.Height * Mathf.Exp(-0.5f * d * d);
            }
        }

        var max = 0f;
        foreach (var v in a) max = Mathf.Max(max, v);
        if (max > 0f) for (var i = 0; i < a.Length; i++) a[i] /= max;
        return a;
    }
}

// Raman spectrum (100 to 3500 cm-1, 532 nm laser) of what the laser hits in a gem.
public static class RamanSpectrum
{
    private const float Counts = 9000f;

    public static RamanResult Measure(GemInstance gem, RamanTarget target)
    {
        var def = gem.Definition;
        var name = def.name ?? "";
        var synthetic = gem.IsSynthetic || def.forcedSynthetic;
        var rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|raman|" + target) & 0x7FFFFFFF));
        uint Hash(string salt) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|" + salt);

        var result = new RamanResult { Description = target == RamanTarget.Host ? "Cristal hôte" : target == RamanTarget.Inclusion ? "Inclusion" : "Fissure" };
        var hostKey = HostKey(name, def.displayName);
        RamanLibrary.EnsureSpecies(hostKey);

        // Components under the laser, with their share of the signal.
        var parts = new List<KeyValuePair<string, float>>();
        var forcedSynthetic = IsSyntheticMaterial(name);

        switch (target)
        {
            case RamanTarget.Host:
                parts.Add(new KeyValuePair<string, float>(hostKey, 1f));
                result.HasSignal = true;
                result.Analysis.Add($"Matériau : {hostKey}.");
                if (forcedSynthetic)
                {
                    result.Analysis.Add(hostKey == "Verre (amorphe)"
                        ? "Verre : imitation, aucun minéral."
                        : "Matériau produit uniquement en laboratoire : synthétique.");
                    result.RecordAuthenticity = true;
                }
                else
                {
                    result.Analysis.Add("L'hôte identifie l'espèce, pas la variété ni l'origine : pointez une inclusion.");
                }
                break;

            case RamanTarget.Inclusion:
                if (!HasInclusion(gem, Hash("incl")))
                {
                    result.Analysis.Add("Aucune inclusion exploitable dans cette pierre.");
                    break;
                }
                result.HasSignal = true;
                result.RecordAuthenticity = true;
                if (synthetic)
                {
                    if (Hash("synthincl") % 2 == 0)
                    {
                        result.Analysis.Add("Bulle de gaz : aucun signal Raman, aucun minéral.");
                        result.Description = "Inclusion (bulle)";
                        parts.Add(new KeyValuePair<string, float>(hostKey, 0.12f));
                    }
                    else
                    {
                        result.Analysis.Add("Résidu de fondant amorphe, aucun minéral naturel.");
                        result.Description = "Inclusion (fondant)";
                        parts.Add(new KeyValuePair<string, float>("Verre (amorphe)", 0.9f));
                        parts.Add(new KeyValuePair<string, float>(hostKey, 0.25f));
                    }
                }
                else
                {
                    var inclusion = PickInclusion(def, name, Hash("inclkind"));
                    result.Description = $"Inclusion ({inclusion.label})";
                    foreach (var key in inclusion.keys) RamanLibrary.EnsureSpecies(key);
                    parts.Add(new KeyValuePair<string, float>(inclusion.keys[0], 1f));
                    if (inclusion.keys.Length > 1) parts.Add(new KeyValuePair<string, float>(inclusion.keys[1], 0.6f));
                    parts.Add(new KeyValuePair<string, float>(hostKey, 0.3f));
                    result.Analysis.Add($"Inclusion identifiée : {inclusion.label}.");
                }
                break;

            default:
                var filler = FillerOf(gem, name, Hash("filler"));
                if (filler == null)
                {
                    result.Analysis.Add("Fissure vide : aucune substance de comblement.");
                    result.HasSignal = true;
                    parts.Add(new KeyValuePair<string, float>(hostKey, 0.2f));
                    break;
                }
                result.HasSignal = true;
                result.RecordTreatment = true;
                result.Description = $"Fissure ({filler})";
                parts.Add(new KeyValuePair<string, float>(filler, 1f));
                parts.Add(new KeyValuePair<string, float>(hostKey, 0.25f));
                result.Analysis.Add($"Substance de comblement identifiée : {filler}.");
                break;
        }

        Build(result, parts, gem, hostKey, target, synthetic, rng);
        return result;
    }

    // ---- What is in the stone ---------------------------------------------------------------------------------

    private static bool IsSyntheticMaterial(string name)
    {
        return name == "CZ" || name == "YAG" || name == "GGG" || name == "Moissanite_Synthetique" || name == "Verre"
            || name.StartsWith("Diamant_Synthetique") || name.StartsWith("Corindon_Synthetique");
    }

    private static bool HasInclusion(GemInstance gem, uint hash)
    {
        switch (gem.ClarityGrade)
        {
            case "Fortement inclus":
            case "Inclus": return true;
            case "Eye-clean": return hash % 100 < 50;
            default: return hash % 100 < 15;
        }
    }

    private static (string label, string[] keys) PickInclusion(GemDefinition def, string name, uint hash)
    {
        if (name == "Emeraude" && hash % 3 == 0) return ("fluide triphasé : eau et CO2", new[] { "CO2 (gaz)", "Eau" });

        string[] pool;
        if (name.StartsWith("Corindon") || name.StartsWith("Saphir") || name == "Padparadscha")
            pool = new[] { "Rutile", "Zircon", "Apatite", "Spinelle", "Calcite", "Mica (muscovite)" };
        else if (name == "Emeraude" || name == "Goshenite" || name == "Morganite" || name == "Heliodore" || name == "Beryl_Rouge")
            pool = new[] { "Calcite", "Mica (muscovite)", "Albite", "Quartz", "Pyrite" };
        else if (name == "Diamant")
            pool = new[] { "Graphite", "Péridot", "Grenat pyrope" };
        else if (name.StartsWith("Grenat"))
            pool = new[] { "Zircon", "Apatite", "Rutile", "Quartz" };
        else
            pool = new[] { "Quartz", "Apatite", "Zircon", "Mica (muscovite)", "Calcite" };

        var pick = pool[(int)(hash % (uint)pool.Length)];
        return (pick, new[] { pick });
    }

    // What fills the fractures, when the stone is treated.
    public static string FillerOf(GemInstance gem, string name, uint hash)
    {
        // A stone that went through the autoclave of the laboratory has its own filler.
        if (gem.IsFilled)
        {
            switch (gem.FillerKind)
            {
                case FillerFamily.Oil: return "Huile de cèdre";
                case FillerFamily.Wax: return "Paraffine (cire)";
                case FillerFamily.Resin: return "Résine époxy";
                case FillerFamily.Glass: return "Verre au plomb";
                case FillerFamily.Flux: return "Verre borate (fondant)";
            }
        }

        var grade = gem.TreatmentGrade;
        var filled = grade == "Huilé / imprégné" || grade == "Fracture comblée" || name == "Turquoise_Stabilisee";
        if (!filled) return null;

        var corundum = name.StartsWith("Corindon") || name.StartsWith("Saphir") || name == "Padparadscha";
        if (corundum && grade == "Fracture comblée" && hash % 100 < 70) return "Verre au plomb";

        var polymer = name == "Turquoise_Stabilisee" || name.StartsWith("Jade") || grade == "Fracture comblée" || hash % 100 < 35;
        return polymer ? "Résine époxy" : "Huile de cèdre";
    }

    private static string HostKey(string name, string displayName)
    {
        switch (name)
        {
            case "Diamant": case "Diamant_Synthetique_HPHT": case "Diamant_Synthetique_CVD": return "Diamant";
            case "Corindon_Rubis": case "Corindon_Synthetique_Rubis": case "Corindon_Synthetique_Saphir":
            case "Saphir_Bleu": case "Saphir_Incolore": case "Saphir_Jaune": case "Saphir_Orange": case "Saphir_Rose":
            case "Saphir_Vert": case "Saphir_Violet": case "Padparadscha": return "Corindon";
            case "Amethyste": case "Ametrine": case "Citrine": case "Prasiolite": case "Quartz": case "Quartz_Fume": case "Quartz_Rose": return "Quartz";
            case "Emeraude": case "Goshenite": case "Morganite": case "Heliodore": case "Beryl_Rouge": case "Pezzottaite": return "Béryl";
            case "Topaze": return "Topaze";
            case "Grenat_Almandin": case "Grenat_Rhodolite": return "Grenat almandin";
            case "Grenat_Pyrope": return "Grenat pyrope";
            case "Grenat_Spessartite": case "Grenat_Malaya": return "Grenat spessartite";
            case "Grenat_Tsavorite": case "Grenat_Hessonite": case "Grenat_Hydrogrossulaire": return "Grenat grossulaire";
            case "Grenat_Demantoide": return "Grenat andradite";
            case "Spinelle": case "Spinelle_Bleu": return "Spinelle";
            case "Peridot": return "Péridot";
            case "Tourmaline_Paraiba": case "Tourmaline_Rose": return "Tourmaline";
            case "Zircon": return "Zircon";
            case "Chrysoberyl": case "Chrysoberyl_Alexandrite": return "Chrysobéryl";
            case "Apatite": return "Apatite";
            case "Tanzanite": case "Zoisite": return "Zoïsite";
            case "Iolite": return "Cordiérite";
            case "Kyanite": return "Kyanite";
            case "Fluorite": return "Fluorite";
            case "Jade_Jadeite": return "Jadéite";
            case "Jade_Nephrite": return "Néphrite";
            case "Serpentine_Bowenite": return "Serpentine";
            case "Opale_Noble": case "Opale_De_Feu": return "Opale";
            case "Turquoise": case "Turquoise_Stabilisee": return "Turquoise";
            case "Orthose": case "Labradorite": case "Pierre_De_Lune": return "Feldspath";
            case "Rhodonite": return "Rhodonite";
            case "Rhodocrosite": return "Rhodocrosite";
            case "Hematite": return "Hématite";
            case "Pyrite": return "Pyrite";
            case "Ivoire_Elephant": case "Ivoire_Hippopotame": case "Ivoire_Mammouth": case "Ivoire_Morse": case "Defense_Narval":
            case "Ecaille_Tortue": case "Corne_Rhinoceros": return "Ivoire / matière organique";
            case "CZ": return "Cubic zirconia (CZ)";
            case "Moissanite_Synthetique": return "Moissanite (SiC)";
            case "YAG": return "YAG";
            case "GGG": return "GGG";
            case "Verre": case "Obsidienne": return "Verre (amorphe)";
            default: return string.IsNullOrEmpty(displayName) ? name : displayName;
        }
    }

    // ---- Spectrum -----------------------------------------------------------------------------------------------

    private static void Build(RamanResult result, List<KeyValuePair<string, float>> parts, GemInstance gem, string hostKey,
        RamanTarget target, bool synthetic, System.Random rng)
    {
        var n = RamanResult.Points;
        var clean = new float[n];

        // A little calibration shift, the same for every component of one measurement.
        var jitter = ((float)rng.NextDouble() - 0.5f) * 3f;
        foreach (var part in parts)
        {
            var r = RamanLibrary.Render(part.Key, jitter);
            for (var i = 0; i < n; i++) clean[i] += r[i] * part.Value;
        }

        var photoluminescence = target == RamanTarget.Host && hostKey == "Diamant" ? AddDiamondLuminescence(clean, gem, synthetic, result) : 0f;

        var max = 0f;
        for (var i = 0; i < n; i++) max = Mathf.Max(max, clean[i]);
        var scale = max > 0f ? Counts * Mathf.Clamp01(max) / max : 0f;

        // Fluorescence background of the stone: stronger toward the high shifts, depending on its color.
        var fluo = target == RamanTarget.Host || target == RamanTarget.Inclusion ? 150f + 900f * gem.ColorSaturation : 120f;
        var intensity = new float[n];
        for (var i = 0; i < n; i++)
        {
            var x = i / (float)(n - 1);
            var signal = clean[i] * scale;
            var background = fluo * (0.6f + 1.2f * x) + 40f * Mathf.Sin(i * 0.004f);
            var noise = Gauss(rng) * (18f + 1.1f * Mathf.Sqrt(signal + background));
            intensity[i] = Mathf.Max(0f, signal + background + noise);
        }
        result.Intensity = intensity;

        // Peaks found by the software: local maxima of the clean curve.
        var peaks = new List<KeyValuePair<float, float>>();
        for (var i = 3; i < n - 3; i++)
        {
            var v = clean[i];
            if (v < max * 0.12f) continue;
            if (v > clean[i - 1] && v >= clean[i + 1] && v > clean[i - 3] && v >= clean[i + 3]) peaks.Add(new KeyValuePair<float, float>(v, RamanResult.MinShift + i));
        }
        peaks.Sort((p, q) => q.Key.CompareTo(p.Key));
        if (peaks.Count > 0)
        {
            var sb = new StringBuilder("Pics (cm-1) : ");
            for (var i = 0; i < peaks.Count && i < 6; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Mathf.RoundToInt(peaks[i].Value));
            }
            result.PeakLines.Add(sb.ToString());
        }
        else
        {
            result.PeakLines.Add(result.HasSignal ? "Aucun pic net : signal trop faible ou matériau amorphe." : "Aucun signal exploitable.");
        }
        if (photoluminescence > 0f) result.PeakLines.Add("Photoluminescence NV0 (1406) et NV- (3098) détectée.");

        if (max >= 0.2f) MatchLibrary(result, clean);
        else if (result.HasSignal) result.PeakLines.Add("Signal trop faible : aucune correspondance dans la bibliothèque.");
    }

    // Natural diamonds show little nitrogen luminescence; an HPHT-treated one shows NV0 and NV- strongly.
    private static float AddDiamondLuminescence(float[] clean, GemInstance gem, bool synthetic, RamanResult result)
    {
        float nv0;
        float nvMinus;
        if ((gem.HasTreatment(TreatmentKind.Heated) || gem.HasTreatment(TreatmentKind.Hpht)) && !synthetic) { nv0 = 0.9f; nvMinus = 0.3f; }
        else if (synthetic) { nv0 = 0.25f; nvMinus = 0.4f; }
        else { nv0 = 0.06f; nvMinus = 0.05f; }

        AddGauss(clean, 1406f, 9f, nv0);
        AddGauss(clean, 1750f, 400f, nv0 * 0.25f);
        AddGauss(clean, 3098f, 16f, nvMinus);
        if (nv0 + nvMinus > 0.3f)
        {
            var ratio = nv0 / Mathf.Max(nvMinus, 0.01f);
            result.Analysis.Add(ratio > 2f
                ? $"Rapport NV0/NV- = {ratio:F1} : élevé, compatible avec un traitement HPHT."
                : $"Rapport NV0/NV- = {ratio:F1} : bas, pas de signature de recuit HPHT.");
        }
        return nv0 + nvMinus > 0.3f ? 1f : 0f;
    }

    private static void AddGauss(float[] a, float center, float fwhm, float height)
    {
        var sigma = fwhm / 2.3548f;
        var lo = Mathf.Max(0, Mathf.FloorToInt(center - 4f * fwhm - RamanResult.MinShift));
        var hi = Mathf.Min(a.Length - 1, Mathf.CeilToInt(center + 4f * fwhm - RamanResult.MinShift));
        for (var i = lo; i <= hi; i++)
        {
            var d = (RamanResult.MinShift + i - center) / sigma;
            a[i] += height * Mathf.Exp(-0.5f * d * d);
        }
    }

    // Cosine similarity between the measured curve (without background) and every library spectrum.
    private static void MatchLibrary(RamanResult result, float[] clean)
    {
        var scores = new List<KeyValuePair<float, string>>();
        var normSample = 0f;
        foreach (var v in clean) normSample += v * v;
        normSample = Mathf.Sqrt(normSample);
        if (normSample <= 0f || !result.HasSignal) return;

        foreach (var name in new List<string>(RamanLibrary.Names))
        {
            var r = RamanLibrary.Render(name);
            var dot = 0f;
            var norm = 0f;
            for (var i = 0; i < r.Length; i++)
            {
                dot += r[i] * clean[i];
                norm += r[i] * r[i];
            }
            if (norm <= 0f) continue;
            scores.Add(new KeyValuePair<float, string>(dot / (Mathf.Sqrt(norm) * normSample), name));
        }

        scores.Sort((p, q) => q.Key.CompareTo(p.Key));
        for (var i = 0; i < scores.Count && i < 4; i++)
        {
            result.Matches.Add(new RamanMatch
            {
                Name = scores[i].Value,
                Score = Mathf.Clamp(Mathf.RoundToInt(scores[i].Key * 100f), 0, 100),
                Reference = RamanLibrary.Render(scores[i].Value),
            });
        }
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
