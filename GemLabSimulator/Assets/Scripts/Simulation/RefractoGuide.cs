using System.Collections.Generic;
using System.Text;
using UnityEngine;

// What the refractometer needs to know about the species: the optical sign (positive or negative) of each anisotropic one, the table of the indices by species
// (the first tab of the refractometer), and the texts of the other tabs (out of limit, one reading, two readings, uniaxial, biaxial, optical sign).
public static class OpticalSigns
{
    // '+' or '-' for the species that have two readings (uniaxial: the sign of the birefringence; biaxial: where the middle index lies).
    private static readonly Dictionary<string, char> Signs = new Dictionary<string, char>
    {
        // Uniaxial positive: quartz and its varieties, zircon, moissanite.
        { "Amethyste", '+' }, { "Ametrine", '+' }, { "Citrine", '+' }, { "Prasiolite", '+' }, { "Quartz", '+' }, { "Quartz_Fume", '+' }, { "Quartz_Rose", '+' },
        { "Agate", '+' }, { "Oeil_de_Tigre", '+' }, { "Zircon", '+' }, { "Moissanite_Synthetique", '+' },
        // Uniaxial negative: corundum, beryl, tourmaline, apatite, scapolite...
        { "Apatite", '-' }, { "Corindon_Rubis", '-' }, { "Corindon_Synthetique_Rubis", '-' }, { "Corindon_Synthetique_Saphir", '-' }, { "Padparadscha", '-' },
        { "Saphir_Bleu", '-' }, { "Saphir_Incolore", '-' }, { "Saphir_Jaune", '-' }, { "Saphir_Orange", '-' }, { "Saphir_Rose", '-' }, { "Saphir_Vert", '-' }, { "Saphir_Violet", '-' },
        { "Emeraude", '-' }, { "Goshenite", '-' }, { "Heliodore", '-' }, { "Morganite", '-' }, { "Pezzottaite", '-' }, { "Rhodocrosite", '-' }, { "Scapolite", '-' },
        { "Tourmaline_Paraiba", '-' }, { "Tourmaline_Rose", '-' },
        // Biaxial positive.
        { "Chrysoberyl", '+' }, { "Chrysoberyl_Alexandrite", '+' }, { "Diopside_Chromifere", '+' }, { "Jade_Jadeite", '+' }, { "Kunzite", '+' }, { "Labradorite", '+' },
        { "Peridot", '+' }, { "Rhodonite", '+' }, { "Tanzanite", '+' }, { "Topaze", '+' }, { "Zoisite", '+' },
        // Biaxial negative.
        { "Andalousite", '-' }, { "Iolite", '-' }, { "Jade_Nephrite", '-' }, { "Kyanite", '-' }, { "Orthose", '-' }, { "Pierre_De_Lune", '-' }, { "Serpentine_Bowenite", '-' }, { "Sinhalite", '-' },
    };

    public static char SignOf(GemDefinition def)
    {
        if (def != null && Signs.TryGetValue(def.name ?? "", out var sign)) return sign;
        return '-';
    }

    public static string Label(char sign) => sign == '+' ? "positif (+)" : "négatif (−)";
}

// One line of the table of the indices: the species that read the same on the refractometer.
public class RefractoGroup
{
    public string Names;
    public float Low;                  // nα (the lowest reading)
    public float High;                 // nγ (the highest)
    public string Kind;                // "SR", "DR uniaxe", "DR biaxe" or "SR (agrégat)"
    public char Sign;                  // for the two readings
    public bool OutOfLimit;            // above the limit of the scale
}

public static class RefractoReference
{
    public const float ScaleLimit = 1.81f;

    private static List<RefractoGroup> cache;

    // The species that can be read, by increasing index, with the ones that read the same put together.
    public static List<RefractoGroup> Groups()
    {
        if (cache != null) return cache;
        var list = new List<RefractoGroup>();
        var database = GemDatabase.Instance;
        if (database == null) return list;

        var byKey = new Dictionary<string, RefractoGroup>();
        foreach (var def in database.allGems)
        {
            if (def == null || def.isTunnelExclusive || def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Opaque) continue;

            var anisotropic = !def.isPolycrystalline
                              && def.opticalCharacterClass != GemDefinition.OpticalCharacterClass.Isotropic
                              && def.birefringence > 0.0005f;
            var kind = anisotropic
                ? (def.opticalCharacterClass == GemDefinition.OpticalCharacterClass.Uniaxial ? "DR uniaxe" : "DR biaxe")
                : (def.isPolycrystalline ? "SR (agrégat, lecture floue)" : "SR");
            var sign = anisotropic ? OpticalSigns.SignOf(def) : ' ';
            var high = anisotropic ? def.refractiveIndex + def.birefringence : def.refractiveIndex;

            var key = $"{def.refractiveIndex:F3}|{high:F3}|{kind}|{sign}";
            if (!byKey.TryGetValue(key, out var group))
            {
                group = new RefractoGroup
                {
                    Names = def.displayName, Low = def.refractiveIndex, High = high, Kind = kind, Sign = sign,
                    OutOfLimit = def.refractiveIndex > ScaleLimit,
                };
                byKey[key] = group;
                list.Add(group);
            }
            else if (!group.Names.Contains(def.displayName)) group.Names += ", " + def.displayName;
        }

        list.Sort((a, b) => a.Low.CompareTo(b.Low));
        cache = list;
        return list;
    }
}

// The pages of the tabs at the left of the refractometer.
public static class RefractoGuide
{
    public enum Tab { Stones, OutOfLimit, Single, Double, Uniaxial, Biaxial, Sign }

    public static readonly Tab[] Tabs = { Tab.Stones, Tab.OutOfLimit, Tab.Single, Tab.Double, Tab.Uniaxial, Tab.Biaxial, Tab.Sign };

    // What the refractometer shows now of the stone on the glass (nothing before a stone is placed and read).
    public struct Situation
    {
        public bool HasStone;
        public bool Read;              // a first reading exists
        public bool OutOfLimit;        // no shadow edge on the scale
        public bool Double;            // two readings (two edges seen at some angle)
        public bool Uniaxial;
        public bool Biaxial;
        public float NAlpha, NBeta, NGamma;   // the values read so far (0 when not read yet)
        public bool LowMoves, HighMoves;      // which edge has moved while the lens turned
        public bool HasLowEdge, HasHighEdge;  // the shadow edges seen now on the scale
        public float CurrentLow, CurrentHigh; // where they are now
        public float AngleDegrees;            // the angle of the lens now
    }

    private const string TitleColor = "#F2D9A6";
    private const string TextColor = "#C9CED8";
    private const string MatchColor = "#9BE89F";
    private const string MatchText = "#DDF7DF";
    private const string Muted = "#8F96A6";

    public static string Name(Tab tab)
    {
        switch (tab)
        {
            case Tab.Stones: return "Pierres et indices";
            case Tab.OutOfLimit: return "Hors limite (OTL)";
            case Tab.Single: return "Une lecture (SR)";
            case Tab.Double: return "Deux lectures (DR)";
            case Tab.Uniaxial: return "Uniaxe";
            case Tab.Biaxial: return "Biaxe";
            default: return "Signe optique";
        }
    }

    public static string ShortName(Tab tab)
    {
        switch (tab)
        {
            case Tab.Stones: return "Pierres";
            case Tab.OutOfLimit: return "OTL";
            case Tab.Single: return "SR";
            case Tab.Double: return "DR";
            case Tab.Uniaxial: return "Uniaxe";
            case Tab.Biaxial: return "Biaxe";
            default: return "Signe";
        }
    }

    public static string Page(Tab tab, Situation s)
    {
        var sb = new StringBuilder();
        sb.Append("<size=17><color=").Append(TitleColor).Append("><b>").Append(Name(tab)).Append("</b></color></size>\n");
        switch (tab)
        {
            case Tab.Stones: StonesPage(sb, s); break;
            case Tab.OutOfLimit: OutOfLimitPage(sb, s); break;
            case Tab.Single: SinglePage(sb, s); break;
            case Tab.Double: DoublePage(sb, s); break;
            case Tab.Uniaxial: UniaxialPage(sb, s); break;
            case Tab.Biaxial: BiaxialPage(sb, s); break;
            default: SignPage(sb, s); break;
        }
        return sb.ToString().TrimEnd('\n');
    }

    private static void Para(StringBuilder sb, string text)
    {
        sb.Append("<color=").Append(TextColor).Append(">").Append(text).Append("</color>\n\n");
    }

    private static void Note(StringBuilder sb, string text)
    {
        sb.Append("<color=").Append(Muted).Append("><i>").Append(text).Append("</i></color>\n\n");
    }

    private static void Yours(StringBuilder sb, string text)
    {
        sb.Append("<color=").Append(MatchColor).Append("><b>Votre pierre : ").Append(text).Append("</b></color>\n\n");
    }

    private static string F(float value) => value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

    private static void StonesPage(StringBuilder sb, Situation s)
    {
        Para(sb, "Chaque espèce a son indice de réfraction : ses valeurs, et le signe optique des pierres à deux lectures. Comparez avec vos relevés (nα est le plus bas). Les lignes vertes correspondent à votre pierre.");
        if (!s.HasStone) Note(sb, "Posez une pierre sur l'hémicylindre pour voir la ligne qui la concerne.");

        var groups = RefractoReference.Groups();
        var shownLimit = false;
        foreach (var group in groups)
        {
            if (group.OutOfLimit && !shownLimit)
            {
                shownLimit = true;
                sb.Append("<color=").Append(TitleColor).Append("><b>Au-delà de l'échelle (> 1,81) : hors limite</b></color>\n\n");
            }

            var match = s.HasStone && s.Read && !s.OutOfLimit && !group.OutOfLimit && Mathf.Abs(group.Low - s.NAlpha) <= 0.006f
                        || s.HasStone && s.OutOfLimit && group.OutOfLimit;
            var range = group.High > group.Low + 0.0005f ? $"{F(group.Low)} – {F(group.High)}" : F(group.Low);
            var info = group.Kind + (group.Sign == '+' ? ", positif (+)" : group.Sign == '-' ? ", négatif (−)" : "");
            if (match)
            {
                sb.Append("<color=").Append(MatchColor).Append("><b>").Append(range).Append("</b>  ").Append(group.Names).Append("</color>\n");
                sb.Append("<color=").Append(MatchText).Append(">").Append(info).Append("</color>\n\n");
            }
            else
            {
                sb.Append("<color=").Append(TitleColor).Append("><b>").Append(range).Append("</b></color>  <color=").Append(TextColor).Append(">").Append(group.Names).Append("</color>\n");
                sb.Append("<color=").Append(Muted).Append(">").Append(info).Append("</color>\n\n");
            }
        }
    }

    // A picture of the eyepiece, where the text puts the marker (the refractometer draws it there).
    private static void Pic(StringBuilder sb, string id)
    {
        sb.Append("[[pic:").Append(id).Append("]]\n");
    }

    // The picture of what the eyepiece shows now, above a situation that matches the stone on the glass.
    private static void YoursPic(StringBuilder sb, Situation s)
    {
        if (!s.HasStone) return;
        sb.Append("<color=").Append(MatchColor).Append("><b>Ce que vous voyez maintenant :</b></color>\n");
        Pic(sb, "yours");
        sb.Append("\n");
    }

    private static void OutOfLimitPage(StringBuilder sb, Situation s)
    {
        Para(sb, "Le liquide de contact et le verre du réfractomètre ne vont pas au-delà de 1,81. Si l'indice de la pierre est plus élevé, la lumière ne fait jamais la réflexion totale : il n'y a aucune limite d'ombre sur l'échelle, tout le champ reste clair. On note « OTL » (over the limit, hors limite).");
        Para(sb, "<b>Ce que ça apprend :</b> la pierre a un indice supérieur à 1,81. Dans la pratique, cela ne laisse que quelques espèces : le diamant, le zircon, la moissanite, certains grenats, les imitations comme le cubic zirconia (CZ), le GGG et le YAG. Il faudra d'autres mesures pour trancher.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Ce que l'on voit sur l'échelle</b></color>\n");
        Pic(sb, "otl");
        sb.Append("\n");
        if (s.HasStone && s.OutOfLimit) Yours(sb, "hors limite : aucune limite d'ombre sur l'échelle.");
        YoursPic(sb, s);
    }

    private static void SinglePage(StringBuilder sb, Situation s)
    {
        Para(sb, "Une seule limite d'ombre, qui <b>ne bouge pas</b> quand on tourne la lentille ou la pierre : c'est une lecture simple (SR, single reading). La lumière va à la même vitesse dans toutes les directions : la pierre est <b>isotrope</b>.");
        Para(sb, "<b>Isotropes :</b> les grenats, le spinelle, le diamant, la fluorite, les verres et les matériaux amorphes (opale, ambre, obsidienne). Les roches et agrégats (jade, agate...) donnent aussi une lecture simple, mais floue.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Ce que l'on voit sur l'échelle</b></color>\n");
        Pic(sb, "sr");
        sb.Append("\n");
        if (s.HasStone && s.Read && !s.Double) Yours(sb, "une seule limite d'ombre, immobile : lecture simple (SR).");
        YoursPic(sb, s);
    }

    private static void DoublePage(StringBuilder sb, Situation s)
    {
        Para(sb, "Deux limites d'ombre qui se déplacent quand on tourne la lentille : c'est une double lecture (DR, double reading). La lumière est dédoublée dans la pierre : elle est <b>anisotrope</b>, ou biréfringente.");
        Para(sb, "<b>La biréfringence</b> est l'écart entre le plus grand et le plus petit indice lus : nγ − nα. Elle est la même pour toute la pierre et caractérise l'espèce (0,009 pour le quartz, 0,036 pour le péridot, 0,059 pour le zircon...).");
        Para(sb, "Tournez la lentille d'un tour complet et notez les <b>valeurs extrêmes</b> de chaque limite : c'est là qu'on lit nα, nβ et nγ. Puis voyez si la pierre est uniaxe ou biaxe.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Ce que l'on voit sur l'échelle</b></color>\n");
        Para(sb, "Sombre au-dessus de la première limite (indices plus faibles que la pierre), grise entre les deux limites, claire sous la seconde.");
        Pic(sb, "dr");
        sb.Append("\n");
        if (s.HasStone && s.Read && s.Double) Yours(sb, $"deux lectures (DR), nα = {F(s.NAlpha)}, nγ = {F(s.NGamma)}.");
        YoursPic(sb, s);
    }

    private static void UniaxialPage(StringBuilder sb, Situation s)
    {
        Para(sb, "Pierre <b>uniaxe</b> (un seul axe optique : cristaux quadratiques, hexagonaux, trigonaux) : en tournant la lentille, <b>une des deux limites reste immobile</b> et l'autre se déplace entre deux valeurs extrêmes.");
        Para(sb, "On lit donc <b>deux indices</b> : celui de la limite immobile, appelé nω (rayon ordinaire), et le plus éloigné atteint par la limite mobile, nε (rayon extraordinaire). La biréfringence vaut |nε − nω|.");
        Para(sb, "<b>Exemples :</b> quartz (et ses variétés), béryl (émeraude, aigue-marine), corindon (rubis, saphir), tourmaline, zircon, apatite.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Uniaxe négatif (exemple : tourmaline)</b></color>\n");
        Para(sb, "La limite haute reste immobile ; la limite basse se déplace vers elle, puis revient.");
        Pic(sb, "uni-neg");
        sb.Append("\n");
        sb.Append("<color=").Append(TitleColor).Append("><b>Uniaxe positif (exemple : quartz)</b></color>\n");
        Para(sb, "La limite basse reste immobile ; la limite haute se déplace vers elle, puis revient.");
        Pic(sb, "uni-pos");
        sb.Append("\n");
        if (s.HasStone && s.Double && s.Uniaxial) Yours(sb, "uniaxe : une limite immobile, l'autre mobile.");
        YoursPic(sb, s);
    }

    private static void BiaxialPage(StringBuilder sb, Situation s)
    {
        Para(sb, "Pierre <b>biaxe</b> (deux axes optiques : cristaux orthorhombiques, monocliniques, tricliniques) : en tournant la lentille, <b>les deux limites se déplacent</b>. La limite basse varie entre nα et nβ, la haute entre nβ et nγ.");
        Para(sb, "On lit <b>trois indices</b> : nα (le plus bas de la limite basse), nγ (le plus haut de la limite haute) et nβ, qui est le plus haut de la limite basse (ou le plus bas de la haute). La biréfringence vaut nγ − nα.");
        Para(sb, "<b>Exemples :</b> péridot, topaze, tanzanite, kunzite, chrysobéryl, iolite, jade, spodumène.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Biaxe (exemple : péridot)</b></color>\n");
        Para(sb, "Les deux limites bougent à la fois, entre leurs valeurs extrêmes. Relevez le plus bas, le plus haut, et la valeur où la limite basse s'arrête (nβ).");
        Pic(sb, "bi");
        sb.Append("\n");
        if (s.HasStone && s.Double && s.Biaxial) Yours(sb, $"biaxe : les deux limites bougent (nα = {F(s.NAlpha)}, nβ = {F(s.NBeta)}, nγ = {F(s.NGamma)}).");
        YoursPic(sb, s);
    }

    private static void SignPage(StringBuilder sb, Situation s)
    {
        Para(sb, "Le <b>signe optique</b> dit si le plus grand indice est celui de la direction « spéciale » du cristal. On le tire des relevés.");
        sb.Append("<color=").Append(TitleColor).Append("><b>Uniaxe</b></color>\n");
        Pic(sb, "sign-uni");
        sb.Append("\n");
        Para(sb, "· <b>Négatif (−)</b> : la limite <b>basse</b> bouge (nε est plus petit), la haute reste fixe (nω est le plus grand).\n· <b>Positif (+)</b> : la limite <b>haute</b> bouge (nε est plus grand), la basse reste fixe (nω est le plus petit).");
        sb.Append("<color=").Append(TitleColor).Append("><b>Biaxe</b></color>\n");
        Pic(sb, "sign-bi");
        sb.Append("\n");
        Para(sb, "On compare nβ aux deux autres :\n· <b>Positif (+)</b> : nβ est plus proche de nα (nβ − nα plus petit que nγ − nβ).\n· <b>Négatif (−)</b> : nβ est plus proche de nγ (nγ − nβ plus petit que nβ − nα).");
        Para(sb, "Avec l'indice et le signe optique, la liste de l'onglet « Pierres et indices » ne laisse plus que quelques espèces possibles.");
        if (s.HasStone && s.Double && s.Uniaxial)
            Note(sb, s.LowMoves && !s.HighMoves ? "Vos relevés : la limite basse a bougé, la haute est restée fixe."
                : s.HighMoves && !s.LowMoves ? "Vos relevés : la limite haute a bougé, la basse est restée fixe."
                : "Tournez la lentille d'un tour complet pour voir laquelle des deux limites bouge.");
        if (s.HasStone && s.Double && s.Biaxial && s.NBeta > 0f)
            Note(sb, $"Vos relevés : nβ − nα = {F(s.NBeta - s.NAlpha)} et nγ − nβ = {F(s.NGamma - s.NBeta)}.");
    }
}
