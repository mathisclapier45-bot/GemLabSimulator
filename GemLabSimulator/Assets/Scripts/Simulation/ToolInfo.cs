using System.Collections.Generic;

public static class ToolInfo
{
    private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        { "Loupe", "Loupe grossissante (x10) utilisée pour l'examen visuel : éclat, couleur, transparence, forme et style de taille." },
        { "Refractometre", "Mesure l'indice de réfraction par l'angle critique de réflexion totale entre la gemme et un prisme de verre dense. Limité aux indices inférieurs à environ 1,81 (au-delà, la lecture sort de la plage du prisme)." },
        { "BalanceHydrostatique", "Détermine la densité par pesée hydrostatique : on compare le poids de la gemme dans l'air à son poids une fois immergée dans l'eau (principe d'Archimède)." },
        { "Dichroscope", "Petit instrument optique séparant la lumière en deux faisceaux polarisés observés côte à côte à travers un prisme de calcite, révélant un éventuel pléochroïsme (couleurs différentes selon l'axe d'observation)." },
        { "Polariscope", "Deux filtres polarisants croisés entre lesquels on fait tourner la gemme : une gemme isotrope reste sombre en continu, une gemme anisotrope alterne clair/sombre à chaque quart de tour." },
        { "Spectroscope", "Décompose la lumière traversant ou réfléchie par la gemme en un spectre, révélant d'éventuelles bandes d'absorption liées aux éléments traces responsables de la couleur." },
        { "Microscope", "Fort grossissement permettant d'observer les inclusions internes, la texture et la pureté de la gemme." },
        { "Conoscope", "Lumière convergente fortement polarisée permettant d'observer la figure d'interférence optique du cristal, confirmant s'il est uniaxe, biaxe ou isotrope." },
        { "UVLongues", "Lampe ultraviolette à ondes longues (365 nm) utilisée pour observer la fluorescence de la gemme." },
        { "UVCourtes", "Lampe ultraviolette à ondes courtes (254 nm) utilisée pour observer la fluorescence de la gemme." },
        { "FiltreChelsea", "Filtre dichromatique ne laissant passer que le rouge profond et le vert-jaune, utilisé pour repérer certaines réactions colorées liées au chrome." },
        { "Micrometre", "Pied à coulisse mesurant les dimensions de la gemme (longueur x largeur x profondeur) en millimètres." },
        { "BalanceCarat", "Balance de précision mesurant le poids de la gemme en carats (1 carat = 0,2 gramme)." },
        { "Aimant", "Aimant très puissant permettant de tester la sensibilité magnétique de la gemme, liée à sa teneur en fer." },
        { "FTIR", "Spectroscopie infrarouge à transformée de Fourier, identifiant les liaisons moléculaires révélatrices de traitements (huile, résine, comblement de fractures)." },
        { "Raman", "Spectroscopie Raman identifiant la signature vibratoire exacte du réseau cristallin, permettant de distinguer une croissance naturelle d'une synthèse en laboratoire." },
        { "UVVisNIR", "Spectrophotométrie UV-Visible-Proche Infrarouge, utilisée pour l'analyse des éléments traces et l'estimation de l'origine géographique probable." },
        { "MEB", "Microscope électronique à balayage révélant les structures de croissance microscopiques, permettant de distinguer une gemme naturelle d'une gemme synthétique." },
        { "TesteurThermique", "Sonde mesurant la conductivité thermique de la gemme. Le diamant conduit exceptionnellement bien la chaleur, ce qui permet de le distinguer rapidement de la plupart de ses imitations — à l'exception de la moissanite synthétique, qui conduit également très bien la chaleur." },
        { "TesteurElectrique", "Sonde mesurant la conductivité électrique de la gemme. Contrairement au diamant qui est un isolant électrique, la moissanite synthétique (carbure de silicium) est conductrice, ce qui permet de départager les deux lorsque le test thermique seul ne suffit pas." },
        { "DiamondView", "Instrument d'imagerie par fluorescence UV développé pour l'industrie diamantaire, révélant les motifs de croissance internes et distinguant un diamant naturel d'un diamant synthétique (HPHT ou CVD)." },
        { "DiamondSure", "Instrument de dépistage rapide analysant la transmission UV du diamant. Il classe chaque pierre en \"Pass\" (cohérent avec un diamant naturel courant) ou \"Refer\" (nécessite un examen complémentaire, par exemple au DiamondView)." },
    };

    public static string GetDescription(string toolName)
    {
        return Descriptions.TryGetValue(toolName, out var text)
            ? text
            : "Aucune information disponible pour cet outil.";
    }
}
