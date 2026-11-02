using System.Collections.Generic;

public static class ToolInfo
{
    private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        { "Loupe", "Loupe grossissante (x10) utilisée pour l'examen visuel : éclat, couleur, transparence, forme et style de taille." },
        { "Refractometre", "Mesure l'indice de réfraction par l'angle critique de réflexion totale entre la gemme et un prisme de verre dense. Limité aux indices inférieurs à environ 1,81 (au-delà, la lecture sort de la plage du prisme)." },
        { "BalanceHydrostatique", "Détermine la densité par pesée hydrostatique : on compare le poids de la gemme dans l'air à son poids une fois immergée dans l'eau (principe d'Archimède)." },
        { "Dichroscope", "Petit instrument optique séparant la lumière en deux faisceaux polarisés observés côte à côte à travers un prisme de calcite, révélant un éventuel pléochroïsme (couleurs différentes selon l'axe d'observation)." },
        { "Polariscope", "Deux filtres polarisants croisés entre lesquels on fait tourner la gemme sous tous les angles : une gemme isotrope reste sombre, un agrégat polycristallin reste éclairé, une gemme anisotrope s'allume et s'éteint quatre fois par tour. Le conoscope intégré (une lentille sphérique) montre, lorsqu'on regarde le long d'un axe optique, la figure d'interférence qui distingue les gemmes uniaxes des biaxes." },
        { "Spectroscope", "Décompose la lumière traversant ou réfléchie par la gemme en un spectre, révélant d'éventuelles bandes d'absorption liées aux éléments traces responsables de la couleur." },
        { "Microscope", "Fort grossissement permettant d'observer les inclusions internes, la texture et la pureté de la gemme." },
        { "CabineUV", "Cabine ultraviolette à deux lampes : UV longs (365 nm) et UV courts (254 nm). Dans l'obscurité, certaines gemmes émettent une luminescence colorée sous les UV, ce qui aide à les distinguer et à repérer certains traitements ou synthèses." },
        { "FiltreChelsea", "Filtre dichromatique ne laissant passer que le rouge profond et le vert-jaune, utilisé pour repérer certaines réactions colorées liées au chrome." },
        { "Micrometre", "Pied à coulisse mesurant les dimensions de la gemme (longueur x largeur x profondeur) en millimètres." },
        { "BalanceCarat", "Balance de précision mesurant le poids de la gemme en carats (1 carat = 0,2 gramme)." },
        { "Aimant", "Aimant très puissant permettant de tester la sensibilité magnétique de la gemme, liée à sa teneur en fer." },
        { "TesteurThermique", "Diamond Selector : sonde mesurant la conductivité thermique de la gemme. Le diamant conduit exceptionnellement bien la chaleur, ce qui permet de le distinguer rapidement de la plupart de ses imitations (mais pas de la moissanite, qui conduit aussi la chaleur)." },
        { "TesteurElectrique", "Testeur Moissanite/Diamant : sonde mesurant la conductivité électrique de la gemme. Contrairement au diamant, qui est un isolant électrique, la moissanite synthétique (carbure de silicium) est conductrice : c'est le test qui les départage." },
        { "Objectif60X", "Objectif de microscope ×60 (ouverture numérique 0,85) : le plus fort grossissement du microscope gemmologique. Il révèle les plus fines inclusions, mais la profondeur de champ devient très faible : la mise au point doit être précise." },
        { "CameraMicroscope", "Caméra numérique pour microscope (microphotographie) : fixée sur le tube du microscope, elle photographie ce que l'on observe et envoie l'image directement à l'ordinateur du laboratoire." },
        { "FTIR", "Spectroscopie infrarouge à transformée de Fourier, identifiant les liaisons moléculaires révélatrices de traitements (huile, résine, comblement de fractures)." },
        { "Raman", "Spectroscopie Raman identifiant la signature vibratoire exacte du réseau cristallin, permettant de distinguer une croissance naturelle d'une synthèse en laboratoire." },
        { "Spectrophotometre", "Spectrophotomètre UV-Vis-NIR : mesure l'absorption de la lumière de l'ultraviolet au proche infrarouge. Les spectres révèlent la signature des éléments traces (fer, chrome, vanadium, titane...) dont les proportions varient selon le gisement : c'est l'instrument qui permet de déterminer l'origine géographique d'une gemme." },
        { "LAICPMS", "Spectrométrie de masse à plasma à couplage inductif avec ablation laser (LA-ICP-MS) : un laser vaporise une quantité microscopique de matière à la surface de la gemme, analysée pour mesurer précisément la concentration des éléments traces. C'est la méthode de référence en gemmologie pour déterminer l'origine géographique et détecter certains traitements comme la diffusion au béryllium." },
        { "EDXRF", "Spectrométrie de fluorescence X à dispersion d'énergie : un tube à rayons X excite la pierre et chaque élément chimique répond par ses raies caractéristiques, d'autant plus intenses qu'il est abondant. Elle révèle les éléments traces (fer, gallium, vanadium, chrome, cuivre, plomb...) qui trahissent le gisement ou la synthèse, mais ne voit pas les éléments légers." },
        { "MEB", "Microscope électronique à balayage révélant les structures de croissance microscopiques, permettant de distinguer une gemme naturelle d'une gemme synthétique." },
        { "DiamondView", "Instrument d'imagerie par fluorescence UV développé pour l'industrie diamantaire, révélant les motifs de croissance internes et distinguant un diamant naturel d'un diamant synthétique (HPHT ou CVD)." },
        { "PolisseuseFenetre", "Machine de l'atelier lapidaire qui polit une petite facette plane (une « fenêtre ») sur une pierre brute. La surface naturelle diffuse la lumière ; à travers la fenêtre, on peut observer les inclusions et la clarté de l'intérieur au microscope." },
        { "DiamondSure", "Instrument de dépistage rapide analysant la transmission UV du diamant. Il classe chaque pierre en \"Pass\" (cohérent avec un diamant naturel courant) ou \"Refer\" (nécessite un examen complémentaire, par exemple au DiamondView)." },
    };

    public static string GetDescription(string toolName)
    {
        return Descriptions.TryGetValue(toolName, out var text)
            ? text
            : "Aucune information disponible pour cet outil.";
    }
}
