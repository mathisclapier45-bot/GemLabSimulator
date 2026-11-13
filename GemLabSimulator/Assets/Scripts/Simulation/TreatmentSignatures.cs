using System.Collections.Generic;
using UnityEngine;

// A band that a treatment adds to the spectrum of a stone.
public struct TreatmentLine
{
    public float Nm;
    public float Width;
    public string Label;
}

// How the stone looks in a liquid of the right refractive index: the colour spread through the stone, or gathered where a treatment put it.
public enum ImmersionPattern { Uniform, SurfaceWeb, CoreRim, Patchy }

// What the detection tools read of the treatments of a stone, in one place: the UV lamp, the Chelsea filter, the dichroscope, the spectroscope and the
// UV-Vis-NIR spectrometer, and the look of the stone in immersion. The data follows the course: a dye shows the band of its colouring matter and
// glows (or is quenched) under the lamp, an irradiated colour is made of colour centres with their own bands, a coating gives the spectrum of its
// layer and of its metal, a heated tanzanite shows two colours of pleochroism instead of three, and so on. A stone with no treatment is read as
// its species is.
public static class TreatmentSignatures
{
    private static readonly Color BluishWhite = new Color(0.78f, 0.88f, 1f);

    // ---- UV lamp --------------------------------------------------------------------------------------------------

    public static FluorescenceInfo Fluorescence(GemInstance gem, bool longWave)
    {
        var info = FluorescenceInfo.ForGem(gem.Definition, longWave);
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return info;

        // An HPHT diamond: the nitrogen centres glow a strong green.
        if (gem.HasTreatment(TreatmentKind.Hpht))
        {
            info.Glows = true;
            info.Color = new Color(0.40f, 1f, 0.25f);
            info.Intensity = 1f;
            return info;
        }

        // A dye: the organic ones glow (an orange-red or a pink), the mineral ones and the carbon black quench the stone.
        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Rouge": return Glow(info, new Color(1f, 0.42f, 0.18f), 0.65f);
                case "Rose": return Glow(info, new Color(1f, 0.55f, 0.65f), 0.5f);
                case "Violet lavande": return Glow(info, new Color(0.78f, 0.45f, 0.95f), 0.4f);
                default:
                    info.Glows = false;
                    info.Intensity = 0f;
                    return info;
            }
        }

        // A resin or a wax in the fissures glows bluish-white, and so do the bleached pearls and corals.
        var filler = gem.FillerKind;
        if (filler == FillerFamily.Resin || filler == FillerFamily.Wax) return Glow(info, BluishWhite, 0.4f);
        if (gem.HasTreatment(TreatmentKind.Bleached)) return Glow(info, BluishWhite, 0.4f);
        return info;
    }

    private static FluorescenceInfo Glow(FluorescenceInfo info, Color color, float intensity)
    {
        var already = info.Glows && info.Intensity > 0f;
        info.Glows = true;
        info.Color = already ? Color.Lerp(info.Color, color, 0.6f) : color;
        info.Intensity = Mathf.Max(info.Intensity, intensity);
        return info;
    }

    // ---- Chelsea filter ----------------------------------------------------------------------------------------

    public static ChelseaInfo Chelsea(GemInstance gem)
    {
        var entry = ChelseaLibrary.EntryFor(gem);
        var info = entry != null ? ChelseaInfo.FromEntry(entry) : ChelseaInfo.ForGem(gem.Definition);
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return info;

        // The chromium of a Lechleitner emerald, of a chrome dye, shows red through the filter; a pink or a red dye shows pink.
        if (gem.HasTreatment(TreatmentKind.Coated) && gem.CoatingName == "Émeraude Lechleitner") return React(info, new Color(0.96f, 0.10f, 0.12f), 0.9f);
        if (gem.HasTreatment(TreatmentKind.Coated) && gem.CoatingName != null && gem.CoatingName.Contains("cobalt")) return React(info, new Color(1f, 0.38f, 0.52f), 0.35f);
        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Vert chrome": return React(info, new Color(0.96f, 0.10f, 0.12f), 0.55f);
                case "Rose": return React(info, new Color(1f, 0.38f, 0.52f), 0.5f);
                case "Rouge": return React(info, new Color(0.96f, 0.10f, 0.12f), 0.45f);
                case "Violet lavande": return React(info, new Color(1f, 0.38f, 0.52f), 0.3f);
            }
        }
        return info;
    }

    private static ChelseaInfo React(ChelseaInfo info, Color color, float intensity)
    {
        info.Reacts = true;
        info.Color = color;
        info.Intensity = Mathf.Max(info.Intensity, intensity);
        return info;
    }

    // ---- Dichroscope -------------------------------------------------------------------------------------------

    public static PleochroismInfo Pleochroism(GemInstance gem)
    {
        var info = PleochroismInfo.ForGem(gem.Definition);
        var name = gem.Definition.name ?? "";

        // A heated tanzanite shows two colours (a deep blue and a violet), the untreated one shows three.
        if (gem.HasTreatment(TreatmentKind.Heated) && (name == "Tanzanite" || name == "Zoisite"))
        {
            var blue = new Color(0.18f, 0.32f, 0.86f);
            return new PleochroismInfo { Pleochroic = true, X = blue, Y = blue, Z = new Color(0.52f, 0.28f, 0.78f) };
        }

        // The heating of a green tourmaline pales its colour and weakens its dichroism.
        if (gem.HasTreatment(TreatmentKind.Heated) && name.StartsWith("Tourmaline") && info.Pleochroic)
        {
            var mean = (info.X + info.Y + info.Z) / 3f;
            info.X = Color.Lerp(info.X, mean, 0.65f);
            info.Y = Color.Lerp(info.Y, mean, 0.65f);
            info.Z = Color.Lerp(info.Z, mean, 0.65f);
            return info;
        }

        // The deep blue of an irradiated beryl (maxixe) is very dichroic: blue and colourless or pale pink.
        if (gem.IsIrradiated && gem.IrradiationName.StartsWith("Bleu maxixe"))
        {
            var blue = new Color(0.14f, 0.30f, 0.88f);
            return new PleochroismInfo { Pleochroic = true, X = blue, Y = blue, Z = new Color(0.96f, 0.90f, 0.94f) };
        }
        return info;
    }

    // The words of the dichroscope for a stone: the species' own, or what its treatment changed.
    public static string PleochroismText(GemInstance gem)
    {
        var def = gem.Definition;
        var name = def.name ?? "";
        if (gem.HasTreatment(TreatmentKind.Heated) && (name == "Tanzanite" || name == "Zoisite"))
            return "Pléochroïsme : bleu intense / violet (deux couleurs : la tanzanite chauffée n'en montre plus que deux)";
        if (gem.IsIrradiated && gem.IrradiationName.StartsWith("Bleu maxixe"))
            return "Pléochroïsme : bleu / incolore ou rose pâle (dichroïsme marqué du bleu maxixe)";
        if (gem.HasTreatment(TreatmentKind.Heated) && name.StartsWith("Tourmaline") && def.pleochroic)
            return $"Pléochroïsme : {def.pleochroicDescription} (affaibli par le chauffage)";
        return def.pleochroic ? $"Pléochroïsme : {def.pleochroicDescription}" : "Pléochroïsme : aucun";
    }

    // ---- Spectroscope and UV-Vis-NIR ---------------------------------------------------------------------------------

    // The bands the treatments add to the spectrum of the stone.
    public static List<TreatmentLine> TreatmentLines(GemInstance gem)
    {
        var lines = new List<TreatmentLine>();
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return lines;

        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Rouge": lines.Add(Line(540f, 40f, "Colorant rouge")); break;
                case "Rose": lines.Add(Line(548f, 40f, "Colorant rose")); break;
                case "Bleu de Prusse": lines.Add(Line(700f, 90f, "Bleu de Prusse")); break;
                case "Vert chrome": lines.Add(Line(650f, 30f, "Chrome (colorant)")); lines.Add(Line(430f, 40f, "Chrome (colorant)")); break;
                case "Noir carbone": lines.Add(Line(550f, 240f, "Carbone (noir)")); break;
                case "Violet lavande": lines.Add(Line(570f, 50f, "Colorant violet")); break;
            }
        }

        if (gem.IsIrradiated)
        {
            var name = gem.IrradiationName;
            if (name.StartsWith("Quartz fumé")) lines.Add(Line(500f, 140f, "Centre fumé (Al)"));
            else if (name.StartsWith("Quartz jaune-vert")) lines.Add(Line(430f, 80f, "Centre jaune-vert"));
            else if (name.StartsWith("Améthyste")) lines.Add(Line(545f, 90f, "Centre améthyste (Fe4+)"));
            else if (name.StartsWith("Quartz bleu")) lines.Add(Line(580f, 100f, "Centre violacé"));
            else if (name.StartsWith("Héliodore")) lines.Add(Line(410f, 70f, "Fe3+ (béryl jaune)"));
            else if (name.StartsWith("Bleu maxixe")) { lines.Add(Line(695f, 60f, "Centre maxixe (CO3)")); lines.Add(Line(615f, 40f, "Centre maxixe (CO3)")); }
            else if (name.StartsWith("Rose")) lines.Add(Line(550f, 60f, "Mn3+ (morganite)"));
            else if (name.StartsWith("Orange")) lines.Add(Line(450f, 70f, "Centre orange"));
            else if (name.StartsWith("Topaze")) lines.Add(Line(620f, 80f, "Centre de la topaze irradiée"));
            else if (name.StartsWith("Rouge-rose")) lines.Add(Line(520f, 70f, "Mn3+ (tourmaline)"));
            else if (name.StartsWith("Saphir")) { lines.Add(Line(450f, 60f, "Centre jaune (corindon)")); lines.Add(Line(470f, 40f, "Centre jaune (corindon)")); }
            else if (name.StartsWith("Perle")) lines.Add(Line(500f, 200f, "Assombrissement de la nacre"));
        }

        if (gem.HasTreatment(TreatmentKind.Coated))
        {
            switch (gem.CoatingName)
            {
                case "Aqua aura": lines.Add(Line(520f, 100f, "Couche d'or (revêtement)")); break;
                case "Mystic": lines.Add(Line(440f, 60f, "Couche de titane (revêtement)")); lines.Add(Line(580f, 80f, "Couche de titane (revêtement)")); break;
                case "Bleu cobalt":
                    lines.Add(Line(540f, 25f, "Couche de cobalt (revêtement)")); lines.Add(Line(590f, 25f, "Couche de cobalt (revêtement)")); lines.Add(Line(640f, 25f, "Couche de cobalt (revêtement)"));
                    break;
                case "Mystic feu": lines.Add(Line(460f, 60f, "Couches d'or et de titane")); lines.Add(Line(560f, 80f, "Couches d'or et de titane")); break;
                case "Aqua profond": lines.Add(Line(530f, 90f, "Couches d'or et de cobalt")); lines.Add(Line(590f, 25f, "Couches d'or et de cobalt")); break;
                case "Crépuscule":
                    lines.Add(Line(440f, 60f, "Couches de titane et de cobalt")); lines.Add(Line(540f, 25f, "Couches de titane et de cobalt"));
                    lines.Add(Line(590f, 25f, "Couches de titane et de cobalt")); lines.Add(Line(640f, 25f, "Couches de titane et de cobalt"));
                    break;
                case "Émeraude Lechleitner": lines.Add(Line(430f, 60f, "Chrome (couche d'émeraude)")); lines.Add(Line(600f, 70f, "Chrome (couche d'émeraude)")); lines.Add(Line(683f, 6f, "Chrome (couche d'émeraude)")); break;
                default: lines.Add(Line(500f, 150f, "Couche de peinture")); break;
            }
        }

        // The diamonds: the H3 centre of an HPHT treatment.
        if (gem.HasTreatment(TreatmentKind.Hpht)) lines.Add(Line(503.2f, 4f, "H3 (traitement HPHT)"));
        return lines;
    }

    private static TreatmentLine Line(float nm, float width, string label) => new TreatmentLine { Nm = nm, Width = width, Label = label };

    // The lines of the spectroscope: the ones of the species and the ones of its treatments.
    public static List<SpectrumLine> SpectrumLines(GemInstance gem)
    {
        var entry = SpectrumLibrary.EntryFor(gem, out var variant);
        var all = new List<SpectrumLine>(entry != null ? entry.lines : gem.Definition.spectrumLines);
        // A variant (dyed jadeite...) already has the lines of its dye.
        if (variant && gem.HasTreatment(TreatmentKind.Dyed)) return all;
        foreach (var line in TreatmentLines(gem)) all.Add(new SpectrumLine { wavelengthNm = line.Nm, widthNm = Mathf.Max(4f, line.Width * 0.5f) });
        return all;
    }

    // The sentence that goes with the spectrum, with what the treatments add to it.
    public static string SpectrumDescription(GemInstance gem)
    {
        var entry = SpectrumLibrary.EntryFor(gem, out var variant);
        var text = (entry != null && !string.IsNullOrEmpty(entry.description) ? entry.description : gem.Definition.absorptionSpectrum) ?? "";
        var lines = TreatmentLines(gem);
        if (lines.Count == 0 || (variant && gem.HasTreatment(TreatmentKind.Dyed))) return text;

        string extra;
        if (gem.HasTreatment(TreatmentKind.Coated)) extra = "Le spectre est celui du revêtement (couche de métal ou de peinture), pas celui de la pierre.";
        else if (gem.HasTreatment(TreatmentKind.Dyed)) extra = "Une bande large de colorant s'ajoute au spectre : teinture.";
        else if (gem.IsIrradiated) extra = "Une bande de centres colorés s'ajoute au spectre : couleur d'irradiation.";
        else extra = "Une raie de traitement s'ajoute au spectre.";
        return string.IsNullOrEmpty(text) ? extra : $"{text} {extra}";
    }

    // ---- Immersion ----------------------------------------------------------------------------------------------

    // How the colour is spread when the stone sits in a liquid of the right refractive index: even in a natural or heated stone, along the edges of the
    // facets in a surface diffusion (the "spider web"), in zones that follow the shape of the stone with an orange rim in a beryllium treatment, and in
    // patches along the cracks in a dyed one.
    public static ImmersionPattern Immersion(GemInstance gem)
    {
        if (gem.HasTreatment(TreatmentKind.Diffused))
            return gem.DiffusionDepthMm >= 0.5f ? ImmersionPattern.CoreRim : ImmersionPattern.SurfaceWeb;
        if (gem.HasTreatment(TreatmentKind.Dyed)) return ImmersionPattern.Patchy;
        return ImmersionPattern.Uniform;
    }

    // The colour of the rim of a beryllium diffusion in immersion (orange), and a note for the microscope.
    public static string ImmersionNote(GemInstance gem)
    {
        switch (Immersion(gem))
        {
            case ImmersionPattern.SurfaceWeb: return "En immersion, la couleur se concentre le long des arêtes de facettes, en toile d'araignée : diffusion en surface.";
            case ImmersionPattern.CoreRim: return "En immersion, les zones de couleur suivent la forme de la pierre, avec un pourtour orange : diffusion à cœur (béryllium).";
            case ImmersionPattern.Patchy: return "En immersion, la couleur forme des plages concentrées dans les fissures : teinture.";
            default: return "En immersion, la couleur est répartie dans toute la pierre.";
        }
    }
}
