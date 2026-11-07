using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the game as an application to play without opening Unity: the menu GemLab > Construire le jeu (macOS) or (Windows) builds every scene of the Build
// Settings into Builds/macOS or Builds/Windows (the folder is ignored by git) and shows it in the Finder. The Windows build needs the module "Windows Build
// Support (Mono)" of the editor, installed from the Unity Hub. The saves of the macOS build are the same as in the editor.
public static class BuildPlayers
{
    private const string MacFolder = "Builds/macOS";
    private const string MacApp = "GemLab Simulator.app";
    private const string WindowsFolder = "Builds/Windows/GemLab Simulator";
    private const string WindowsExe = "GemLab Simulator.exe";

    [MenuItem("GemLab/Construire le jeu (macOS)")]
    public static void BuildMacFromMenu() => RunFromMenu(BuildTarget.StandaloneOSX);

    [MenuItem("GemLab/Construire le jeu (Windows)")]
    public static void BuildWindowsFromMenu() => RunFromMenu(BuildTarget.StandaloneWindows64);

    // For the command line (with Unity closed): Unity -batchmode -quit -projectPath <project> -executeMethod BuildPlayers.BuildMacFromCommandLine
    public static void BuildMacFromCommandLine() => RunFromCommandLine(BuildTarget.StandaloneOSX);

    public static void BuildWindowsFromCommandLine() => RunFromCommandLine(BuildTarget.StandaloneWindows64);

    private static void RunFromMenu(BuildTarget target)
    {
        var path = Build(target, out var message);
        if (path != null)
        {
            Debug.Log(message);
            EditorUtility.RevealInFinder(path);
            var hint = target == BuildTarget.StandaloneWindows64
                ? "\n\nPour l'envoyer à un ami : compressez le dossier « GemLab Simulator » (clic droit, Compresser) et envoyez le .zip."
                : "\n\nDouble-cliquez sur l'application pour jouer, sans Unity.";
            EditorUtility.DisplayDialog("Jeu construit", message + hint, "OK");
        }
        else
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("La construction n'a pas pu se faire", message, "OK");
        }
    }

    private static void RunFromCommandLine(BuildTarget target)
    {
        var path = Build(target, out var message);
        if (path != null) Debug.Log(message);
        else Debug.LogError(message);
        EditorApplication.Exit(path != null ? 0 : 1);
    }

    private static string Build(BuildTarget target, out string message)
    {
        var windows = target == BuildTarget.StandaloneWindows64;
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            message = windows
                ? "Le module « Windows Build Support (Mono) » n'est pas installé. Dans Unity Hub : Installs, l'engrenage de votre version d'Unity, Add modules, cochez Windows Build Support (Mono), Install."
                : "Le module de construction macOS n'est pas installé : ajoutez-le dans Unity Hub (Add modules).";
            return null;
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            message = "Aucune scène dans les Build Settings : générez d'abord les scènes (menu GemLab).";
            return null;
        }

        var root = Directory.GetParent(Application.dataPath).FullName;
        var folder = Path.Combine(root, windows ? WindowsFolder : MacFolder);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, windows ? WindowsExe : MacApp);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        });

        var summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            message = $"Résultat : {summary.result}, {summary.totalErrors} erreur(s). La console d'Unity donne le détail.";
            return null;
        }

        message = $"Jeu construit : {path} ({summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.Minutes} min {summary.totalTime.Seconds} s).";
        return path;
    }
}
