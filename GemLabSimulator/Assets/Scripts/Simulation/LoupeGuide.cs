using System.Collections.Generic;
using System.Text;

// The tabs beside the loupe: each one is a piece of information the loupe gives about a stone (the luster, the colour, the transparency, the shape and the cut,
// the inclusions, the defects of the surface). A tab lists everything that can be found for it, with an explanation, and lights the line of the stone under the lens
// once it has been looked at. The text is rich text (bold and colours) for a Text of the interface.
public static class LoupeGuide
{
    public enum Tab { Luster, Color, Transparency, Shape, Inclusions, Surface }

    public static readonly Tab[] Tabs = { Tab.Luster, Tab.Color, Tab.Transparency, Tab.Shape, Tab.Inclusions, Tab.Surface };

    private const string TitleColor = "#F2D9A6";
    private const string TextColor = "#C9CED8";
    private const string MatchColor = "#9BE89F";
    private const string MatchText = "#DDF7DF";
    private const string Muted = "#8F96A6";

    public static string Name(Tab tab)
    {
        switch (tab)
        {
            case Tab.Luster: return "Éclat";
            case Tab.Color: return "Couleur";
            case Tab.Transparency: return "Transparence";
            case Tab.Shape: return "Forme et taille";
            case Tab.Inclusions: return "Inclusions";
            default: return "Surface";
        }
    }

    // The name under the icon of the tab (a few letters only).
    public static string ShortName(Tab tab)
    {
        switch (tab)
        {
            case Tab.Luster: return "Éclat";
            case Tab.Color: return "Couleur";
            case Tab.Transparency: return "Transp.";
            case Tab.Shape: return "Taille";
            case Tab.Inclusions: return "Inclus.";
            default: return "Surface";
        }
    }

    private static string Intro(Tab tab)
    {
        switch (tab)
        {
            case Tab.Luster: return "L'éclat, c'est la façon dont la surface de la pierre renvoie la lumière. On le juge à l'œil, sous la loupe, avant toute mesure.";
            case Tab.Color: return "La couleur est ce qu'on voit en premier, mais elle ne suffit jamais à identifier une pierre : plusieurs espèces ont la même.";
            case Tab.Transparency: return "La transparence dit si la lumière traverse la pierre. Posez-la sur un texte : peut-on le lire à travers ?";
            case Tab.Shape: return "Le contour est la forme de la pierre vue de dessus ; la taille est la façon dont ses facettes sont disposées. Un cristal non taillé est dit brut.";
            case Tab.Inclusions: return "Les inclusions sont tout ce qui est enfermé dans la pierre. Elles racontent son histoire : d'où elle vient, si elle est naturelle ou synthétique.";
            default: return "Les défauts de surface se voient sur l'extérieur de la pierre, à la loupe, en la tournant dans la lumière. Ils viennent de l'usage ou de la taille.";
        }
    }

    // The text of a tab for the stone under the lens (null when there is none). `looked`: the stone has been looked at long enough; `inspected`: it has been turned in all directions.
    public static string Page(Tab tab, GemInstance stone, bool looked, bool inspected, bool dirty)
    {
        var sb = new StringBuilder();
        sb.Append("<size=17><color=").Append(TitleColor).Append("><b>").Append(Name(tab)).Append("</b></color></size>\n");
        sb.Append("<color=").Append(TextColor).Append(">").Append(Intro(tab)).Append("</color>\n\n");

        var needsTurn = tab == Tab.Inclusions || tab == Tab.Surface;
        var known = stone != null && !dirty && (needsTurn ? inspected : looked);
        if (stone == null) Note(sb, "Déposez une gemme sous la loupe : la ligne qui la concerne s'allumera ici.");
        else if (dirty) Note(sb, "La pierre est couverte de traces : essuyez-la d'abord à la chamoisine.");
        else if (!known) Note(sb, needsTurn ? "Tournez la pierre dans tous les sens sous la loupe (un tour complet) : la ligne qui la concerne s'allumera ici."
                                              : "Regardez la pierre quelques secondes : la ligne qui la concerne s'allumera ici.");

        switch (tab)
        {
            case Tab.Luster: Luster(sb, stone, known); break;
            case Tab.Color: Color(sb, stone, known); break;
            case Tab.Transparency: Transparency(sb, stone, known); break;
            case Tab.Shape: Shape(sb, stone, known); break;
            case Tab.Inclusions: Inclusions(sb, stone, known); break;
            default: Surface(sb, stone, known); break;
        }
        return sb.ToString().TrimEnd('\n');
    }

    private static void Note(StringBuilder sb, string text)
    {
        sb.Append("<color=").Append(Muted).Append("><i>").Append(text).Append("</i></color>\n\n");
    }

    private static void Entry(StringBuilder sb, string name, string text, bool match)
    {
        if (match)
        {
            sb.Append("<color=").Append(MatchColor).Append("><b>> ").Append(name).Append("  (cette pierre)</b></color>\n");
            sb.Append("<color=").Append(MatchText).Append(">").Append(text).Append("</color>\n\n");
        }
        else
        {
            sb.Append("<color=").Append(TitleColor).Append("><b>").Append(name).Append("</b></color>\n");
            sb.Append("<color=").Append(TextColor).Append(">").Append(text).Append("</color>\n\n");
        }
    }

    private static void Result(StringBuilder sb, string label, string value)
    {
        sb.Append("<color=").Append(MatchColor).Append("><b>").Append(label).Append(" : ").Append(value).Append("</b></color>\n\n");
    }

    // ---- The tabs --------------------------------------------------------------------------------------------------------

    private static void Luster(StringBuilder sb, GemInstance stone, bool known)
    {
        var luster = known ? (stone.Definition.luster ?? "").ToLowerInvariant() : "";
        if (known) Result(sb, "Éclat de cette pierre", stone.Definition.luster);

        Entry(sb, "Vitreux", "Brillant comme du verre poli. C'est l'éclat de la plupart des gemmes transparentes : quartz, béryl, corindon, topaze...", luster.Contains("vitreux"));
        Entry(sb, "Adamantin", "Brillant vif, presque métallique, comme celui du diamant. La lumière est très fortement renvoyée : signe d'un indice de réfraction élevé.", luster.Contains("adamantin"));
        Entry(sb, "Résineux", "Brillant doux de résine durcie, comme l'ambre ou certains grenats jaunes.", luster.Contains("résineux"));
        Entry(sb, "Cireux", "Aspect de cire polie, doux et peu brillant. On le voit sur les pierres translucides comme la turquoise ou la calcédoine.", luster.Contains("cireux"));
        Entry(sb, "Gras", "La surface semble couverte d'un fin film d'huile. C'est le cas de la néphrite ou de certaines opales.", luster.Contains("gras"));
        Entry(sb, "Nacré", "Reflets irisés de l'intérieur d'un coquillage, dus à de fines couches superposées : la perle, ou les plans de clivage de certaines pierres.", luster.Contains("nacré"));
        Entry(sb, "Soyeux", "Reflets de soie, dus à des fibres toutes parallèles : l'œil-de-tigre, par exemple.", luster.Contains("soyeux"));
        Entry(sb, "Métallique", "Brillant d'un métal poli. Réservé aux minéraux opaques : hématite, pyrite.", luster.Contains("métallique"));
        Entry(sb, "Mat", "Aucun brillant : la surface diffuse la lumière dans tous les sens, comme de la craie.", luster.StartsWith("mat"));
    }

    private static void Color(StringBuilder sb, GemInstance stone, bool known)
    {
        if (known) Result(sb, "Couleur de cette pierre", stone.ColorName);

        Entry(sb, "Teinte", "La couleur de base : rouge, orange, jaune, vert, bleu, violet... C'est le premier mot à noter.", false);
        Entry(sb, "Ton", "Clair ou foncé. Une pierre trop claire paraît délavée, une pierre trop foncée paraît noire : le ton idéal est moyen.", false);
        Entry(sb, "Saturation", "Vive ou grisâtre. Plus la couleur est pure, plus la pierre est recherchée : une couleur grise ou brune la déprécie.", false);
        Entry(sb, "À retenir", "Une même couleur peut venir de plusieurs espèces, et une même espèce existe en plusieurs couleurs. La couleur n'identifie jamais une pierre à elle seule : il faudra des mesures.", false);
    }

    private static void Transparency(StringBuilder sb, GemInstance stone, bool known)
    {
        var level = known ? stone.Definition.Diaphaneity : (Diaphaneity?)null;
        if (known) Result(sb, "Transparence de cette pierre", stone.Definition.TransparencyLabel);

        Entry(sb, "Transparent", "On voit nettement à travers la pierre : un texte posé dessous se lit sans peine. C'est le cas de la plupart des pierres facettées.", level == Diaphaneity.Transparent);
        Entry(sb, "Semi-transparent (translucide)", "La lumière traverse, mais l'image est floue, comme derrière un verre dépoli. On taille souvent ces pierres en cabochon.", level == Diaphaneity.SemiTransparent);
        Entry(sb, "Semi-opaque", "Seule une faible lumière passe, sur les bords fins ou à contre-jour.", level == Diaphaneity.SemiOpaque);
        Entry(sb, "Opaque", "Aucune lumière ne traverse, même par une arête fine. Seule la surface renvoie la lumière : on juge alors la pierre à son éclat.", level == Diaphaneity.Opaque);
    }

    private static void Shape(StringBuilder sb, GemInstance stone, bool known)
    {
        var shape = known ? (stone.ShapeDisplay ?? "").ToLowerInvariant() : "";
        var cut = known ? (stone.CutDisplay ?? "").ToLowerInvariant() : "";
        if (known) Result(sb, "Cette pierre", stone.IsRough ? "brute, non taillée" : $"{stone.ShapeDisplay}, taille {stone.CutDisplay.ToLowerInvariant()}");

        Header(sb, "Le contour");
        Entry(sb, "Brut", "Un cristal non taillé, tel que la nature l'a fait. Pas de facettes polies : on reconnaît son habitus (octaèdre, prisme...).", shape == "brut");
        Entry(sb, "Rond", "Contour circulaire, le plus classique des contours.", shape == "rond");
        Entry(sb, "Ovale", "Un rond allongé, qui allonge aussi le doigt quand la pierre est montée en bague.", shape == "ovale");
        Entry(sb, "Coussin", "Un carré aux angles arrondis, comme un coussin.", shape == "coussin");
        Entry(sb, "Poire", "Une goutte : ronde d'un côté, pointue de l'autre.", shape == "poire");
        Entry(sb, "Marquise", "Un ovale pointu aux deux bouts, en forme de navette.", shape == "marquise");
        Entry(sb, "Carré", "Quatre côtés égaux.", shape == "carré");
        Entry(sb, "Baguette", "Un rectangle long et étroit.", shape == "baguette");
        Entry(sb, "Rectangle à pans coupés", "Un rectangle dont les angles sont coupés, comme la taille émeraude.", shape.StartsWith("rectangle"));

        Header(sb, "La taille");
        Entry(sb, "Brillant", "Des facettes triangulaires et en losange, disposées pour renvoyer le maximum de lumière vers l'œil. Pour les pierres transparentes.", cut == "brillant");
        Entry(sb, "Mixte", "Un mélange : facettes en brillant sur le dessus, en degrés dessous (ou l'inverse).", cut == "mixte");
        Entry(sb, "À degrés", "Des facettes allongées, parallèles au contour, en escalier. Elle met en valeur la couleur plus que l'éclat.", cut.StartsWith("à degrés") || cut.StartsWith("émeraude"));
        Entry(sb, "Princesse", "Un carré à facettes en brillant, aux angles vifs.", cut == "princesse");
        Entry(sb, "Cabochon", "Une surface polie bombée, sans facettes. C'est la taille des pierres translucides ou opaques.", cut == "cabochon");
    }

    private static void Inclusions(StringBuilder sb, GemInstance stone, bool known)
    {
        if (known)
        {
            if (stone.IsRough && !stone.HasWindow)
            {
                Note(sb, "La surface brute diffuse la lumière : on ne voit pas l'intérieur. Il faudrait une fenêtre polie.");
            }
            else
            {
                var names = LoupeFeatures.InclusionNames(stone, false, false);
                if (names.Count > 4) names.RemoveRange(4, names.Count - 4);
                Result(sb, "Dans cette pierre", names.Count > 0 ? string.Join(", ", names) : "aucune inclusion visible");
            }
        }

        Header(sb, "La pureté");
        var grade = known ? stone.ClarityGrade : "";
        Entry(sb, "Loupe clean", "Aucune inclusion visible, même à la loupe ×10. Très rare et très recherché.", grade == "Loupe clean");
        Entry(sb, "Eye-clean", "Aucune inclusion à l'œil nu, quelques-unes très fines à la loupe. Une belle pierre.", grade == "Eye-clean");
        Entry(sb, "Inclus", "Des inclusions bien visibles à la loupe, quelques-unes à l'œil nu.", grade == "Inclus");
        Entry(sb, "Fortement inclus", "De nombreuses inclusions, visibles à l'œil nu. Elles peuvent même fragiliser la pierre.", grade == "Fortement inclus");

        Header(sb, "Les inclusions les plus courantes");
        Entry(sb, "Cristaux", "De petits cristaux d'une autre espèce, pris dans la pierre pendant sa croissance (apatite, zircon, rutile...).", false);
        Entry(sb, "Aiguilles et fibres", "De fines aiguilles parallèles, appelées soie. Bien alignées, elles peuvent faire un astérisme (une étoile) sur un cabochon.", false);
        Entry(sb, "Fissures et plumes", "Des cassures internes, plates et irrégulières, comme une plume. Le signe d'une pierre qui a souffert.", false);
        Entry(sb, "Bulles et liquides", "Des cavités rondes remplies de gaz ou de liquide. Les bulles rondes trahissent souvent un verre ou une pierre synthétique.", false);
    }

    private static void Surface(StringBuilder sb, GemInstance stone, bool known)
    {
        var scratch = false;
        var chip = false;
        var pit = false;
        var abrasion = false;
        var none = false;
        if (known)
        {
            if (stone.IsRough)
            {
                Note(sb, "Surface brute naturelle : piqûres et zones d'usure.");
            }
            else
            {
                foreach (var feature in LoupeFeatures.Generate(stone))
                {
                    if (!feature.External) continue;
                    if (feature.Kind == FeatureKind.Scratch) scratch = true;
                    else if (feature.Kind == FeatureKind.Chip) chip = true;
                    else if (feature.Kind == FeatureKind.Pit) pit = true;
                    else if (feature.Kind == FeatureKind.Abrasion) abrasion = true;
                }
                none = !scratch && !chip && !pit && !abrasion;
                if (none) Result(sb, "Surface de cette pierre", "aucun défaut visible");
            }
        }

        Entry(sb, "Rayure", "Un trait fin, laissé par un grain plus dur que la pierre. Les pierres tendres se rayent facilement.", scratch);
        Entry(sb, "Éclat au rondiste", "Un petit morceau manquant sur l'arête qui fait le tour de la pierre : un choc, ou une pierre mal sertie.", chip);
        Entry(sb, "Piqûre", "Un petit creux dans le poli, comme une tête d'épingle.", pit);
        Entry(sb, "Facettes émoussées", "Des arêtes arrondies, qui ont perdu leur netteté : le signe d'une pierre longtemps portée.", abrasion);
        Entry(sb, "Surface nette", "Aucun défaut externe visible. Les pierres traitées en surface montrent parfois, elles, de drôles de traces : attention.", none);
    }

    private static void Header(StringBuilder sb, string text)
    {
        sb.Append("<size=14><color=").Append(MatchColor).Append("><b>— ").Append(text).Append(" —</b></color></size>\n\n");
    }
}
