using System.Collections.Generic;
using UnityEngine;

public struct PlPeak
{
    public string Label;
    public float Nm;
    public float Height;
}

public class PlResult
{
    public const float MinNm = 480f;
    public const float MaxNm = 900f;
    public const int Points = 421;               // one point per nanometre; index i is wavelength MinNm + i

    public float[] Spectrum;
    public List<PlPeak> Peaks = new List<PlPeak>();
    public List<string> Analysis = new List<string>();
    public bool HasSignal;
    public bool RecordAuthenticity;              // natural against synthetic diamond
    public bool RecordTreatment;                 // an HPHT annealing was found
}

// Photoluminescence spectrum of a stone excited by a 488 nm laser and cooled in liquid nitrogen (77 K): the defects of the lattice answer with sharp
// lines. In diamonds, the nitrogen-vacancy centres (NV0 at 575 nm, NV- at 637 nm), the H3 centre (503 nm), the silicon-vacancy centre of the CVD
// growth (737 nm) and the nickel of the HPHT catalyst (883 nm) tell a natural stone from a synthetic one and betray an HPHT annealing, which
// makes the nitrogen centres glow: strong H3 and NV0 stronger than NV-, or strong NV lines in a diamond without nitrogen. In rubies and emeralds
// the chromium gives its R lines. A given stone always gives the same spectrum.
public static class PhotoluminescenceSpectrum
{
    public static PlResult Measure(GemInstance gem)
    {
        var result = new PlResult();
        var name = gem.Definition.name ?? "";
        var rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|pl") & 0x7FFFFFFF));
        float Unit(string salt) => (StoneOptics.StableHash((gem.SpecimenId ?? "") + "|pl|" + salt) % 1000u) / 1000f;

        var clean = new float[PlResult.Points];
        for (var i = 0; i < clean.Length; i++)
        {
            var nm = PlResult.MinNm + i;
            clean[i] = 18f + 22f * Mathf.Exp(-(nm - 480f) / 160f);
        }

        var kind = DiamondViewImage.KindOf(gem);
        if (kind != DiamondViewKind.NotDiamond) Diamond(result, clean, gem, kind, Unit);
        else NonDiamond(result, clean, gem, name);

        result.Spectrum = new float[PlResult.Points];
        for (var i = 0; i < clean.Length; i++)
            result.Spectrum[i] = Mathf.Max(0f, clean[i] + Gauss(rng) * (1.2f + 0.5f * Mathf.Sqrt(clean[i])));
        return result;
    }

    // One line for the stone sheet and the report.
    public static string Summary(GemInstance gem)
    {
        var result = Measure(gem);
        if (!result.HasSignal) return "aucune photoluminescence exploitable";
        return result.Analysis.Count > 0 ? result.Analysis[0] : "spectre enregistré";
    }

    private static void Diamond(PlResult result, float[] clean, GemInstance gem, DiamondViewKind kind, System.Func<string, float> unit)
    {
        result.HasSignal = true;
        result.RecordAuthenticity = true;

        // The Raman line of the diamond itself, at 522 nm under a 488 nm laser: it is on every spectrum.
        AddLine(clean, result, null, 522.2f, 2.2f, 38f);

        var type = DiamondSure.DiamondTypeOf(gem);
        var nitrogen = type == "Ia";
        float h3 = 0f, nv0 = 0f, nvMinus = 0f, siv = 0f, nickel = 0f;

        switch (kind)
        {
            case DiamondViewKind.Natural:
                if (nitrogen) h3 = 70f + 90f * unit("h3");
                nv0 = 18f + 16f * unit("nv0");
                nvMinus = 14f + 12f * unit("nvm");
                break;
            case DiamondViewKind.NaturalAnnealed:
                if (nitrogen) h3 = 330f + 160f * unit("h3");
                nv0 = 280f + 110f * unit("nv0");
                nvMinus = 90f + 50f * unit("nvm");
                break;
            case DiamondViewKind.Cvd:
                siv = 330f + 120f * unit("siv");
                nv0 = 110f + 50f * unit("nv0");
                nvMinus = 130f + 60f * unit("nvm");
                break;
            case DiamondViewKind.Hpht:
                nickel = 300f + 120f * unit("ni");
                nvMinus = 50f + 30f * unit("nvm");
                nv0 = 14f + 10f * unit("nv0");
                break;
        }

        if (h3 > 0f)
        {
            AddLine(clean, result, "H3", 503.2f, 1.6f, h3);
            AddBand(clean, 520f, 30f, h3 * 0.30f);
        }
        if (nv0 > 0f)
        {
            AddLine(clean, result, "NV0", 575.0f, 1.6f, nv0);
            AddBand(clean, 605f, 45f, nv0 * 0.35f);
        }
        if (nvMinus > 0f)
        {
            AddLine(clean, result, "NV-", 637.0f, 1.8f, nvMinus);
            AddBand(clean, 690f, 60f, nvMinus * 0.45f);
        }
        if (siv > 0f) AddLine(clean, result, "Si-V", 737.0f, 1.8f, siv);

        // An irradiated diamond shows the neutral vacancy: the GR1 line at 741 nm, strongest after neutrons.
        var irradiated = kind != DiamondViewKind.Cvd && kind != DiamondViewKind.Hpht && gem.HasTreatment(TreatmentKind.Irradiated);
        if (irradiated)
        {
            var strong = gem.IrradiationName != null && gem.IrradiationName.Contains("neutrons");
            AddLine(clean, result, "GR1", 741.0f, 2.2f, strong ? 520f + 120f * unit("gr1") : 240f + 90f * unit("gr1"));
            result.RecordTreatment = true;
        }
        if (nickel > 0f) AddLine(clean, result, "Ni", 883.0f, 3f, nickel);

        switch (kind)
        {
            case DiamondViewKind.Natural when irradiated:
                result.Analysis.Add("Raie GR1 à 741 nm : la lacune neutre d'un diamant irradié. La couleur n'est pas naturelle.");
                break;
            case DiamondViewKind.Natural:
                result.Analysis.Add(nitrogen
                    ? "H3 faible, NV0 et NV- très faibles : diamant naturel qui n'a pas subi de recuit."
                    : "NV0 et NV- très faibles, pas de H3 : diamant naturel de type IIa, non traité.");
                break;
            case DiamondViewKind.NaturalAnnealed:
                var ratio = nv0 / Mathf.Max(1f, nvMinus);
                result.RecordTreatment = true;
                result.Analysis.Add(nitrogen
                    ? $"H3 intense à 503 nm, NV0 (575) nettement plus fort que NV- (637), rapport {ratio:F1} : signature d'un recuit HPHT."
                    : $"NV0 (575) et NV- (637) intenses dans un diamant sans azote, rapport {ratio:F1} : un recuit HPHT a recréé ces centres.");
                break;
            case DiamondViewKind.Cvd:
                result.Analysis.Add("Doublet Si-V à 737 nm : marqueur de la croissance CVD en réacteur. Diamant de synthèse.");
                break;
            case DiamondViewKind.Hpht:
                result.Analysis.Add("Nickel à 883 nm, azote isolé faible : résidu du catalyseur métallique d'un diamant HPHT de synthèse.");
                break;
        }
    }

    private static void NonDiamond(PlResult result, float[] clean, GemInstance gem, string name)
    {
        var ruby = name == "Corindon_Rubis" || name == "Corindon_Synthetique_Rubis" || name == "Saphir_Rose";
        if (ruby)
        {
            result.HasSignal = true;
            AddLine(clean, result, "Cr R2", 692.8f, 0.9f, 260f);
            AddLine(clean, result, "Cr R1", 694.2f, 0.9f, 520f);
            AddBand(clean, 710f, 24f, 70f);
            result.Analysis.Add("Raies R du chrome à 692,8 et 694,2 nm : corindon rouge au chrome. Les lignes ne séparent ni naturel ni traité.");
            return;
        }
        if (name == "Emeraude")
        {
            result.HasSignal = true;
            AddLine(clean, result, "Cr", 680.5f, 1.2f, 160f);
            AddLine(clean, result, "Cr", 683.1f, 1.2f, 230f);
            result.Analysis.Add("Raies du chrome à 680 et 683 nm : béryl au chrome. Elles ne disent rien du traitement.");
            return;
        }
        result.Analysis.Add("Aucune photoluminescence exploitable à 488 nm : cet instrument sert surtout pour les diamants.");
    }

    private static void AddLine(float[] a, PlResult result, string label, float nm, float fwhm, float height)
    {
        AddGauss(a, nm, fwhm, height);
        if (label != null) result.Peaks.Add(new PlPeak { Label = label, Nm = nm, Height = height });
    }

    private static void AddBand(float[] a, float nm, float fwhm, float height) => AddGauss(a, nm, fwhm, height);

    private static void AddGauss(float[] a, float center, float fwhm, float height)
    {
        var sigma = fwhm / 2.3548f;
        var lo = Mathf.Max(0, Mathf.FloorToInt(center - 4f * fwhm - PlResult.MinNm));
        var hi = Mathf.Min(a.Length - 1, Mathf.CeilToInt(center + 4f * fwhm - PlResult.MinNm));
        for (var i = lo; i <= hi; i++)
        {
            var d = (PlResult.MinNm + i - center) / sigma;
            a[i] += height * Mathf.Exp(-0.5f * d * d);
        }
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
    }
}
