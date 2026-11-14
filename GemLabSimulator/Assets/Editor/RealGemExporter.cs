using System.IO;
using UnityEditor;
using UnityEngine;

// Menu GemLab > Export Realistic Gems : dessine quelques pierres avec le rendu réaliste (shader de tracé de rayons) dans Docs/planches_gemmes/rendu_realiste.png,
// de face puis inclinées, pour juger le rendu sans lancer le jeu.
public static class RealGemExporter
{
    // The cabochons: a six-rayed star on the corundums, four rays on a garnet, a cat's eye on a chrysoberyl and a quartz, each tilted several ways to see the star move.
    private static void ExportDomes(GemDatabase database, string folder)
    {
        var rows = new (string name, int rays, bool eye)[]
        {
            ("Corindon_Rubis", 6, false), ("Corindon_Rubis", 0, false), ("Saphir_Bleu", 6, false), ("Saphir_Rose", 6, false), ("Grenat_Almandin", 4, false),
            ("Chrysoberyl", 0, true), ("Quartz", 0, true), ("Emeraude", 0, false),
        };
        var angles = new[] { new Vector3(0f, 0f, 0f), new Vector3(22f, 0f, 0f), new Vector3(0f, 28f, 0f), new Vector3(-26f, -22f, 12f) };
        var width = angles.Length * (Cell + Gap) + Gap;
        var height = rows.Length * (Cell + Gap) + Gap;
        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        sheet.SetPixels32(background);

        var view = new GemView3D(Cell) { Realistic = true };
        var mesh = GemMesh.Get("Ovale", "Cabochon");
        for (var row = 0; row < rows.Length; row++)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == rows[row].name) definition = candidate;
            if (definition == null) continue;

            for (var column = 0; column < angles.Length; column++)
            {
                var half = new Vector3(3.6f, 2.8f, 1.7f);
                view.Draw(mesh, half, Quaternion.Euler(angles[column]), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, rows[row].rays, GemLook.For(definition), rows[row].eye);
                var pixels = view.Pixels;
                var x0 = Gap + column * (Cell + Gap);
                var y0 = height - (row + 1) * (Cell + Gap);
                for (var y = 0; y < Cell; y++)
                    for (var x = 0; x < Cell; x++)
                    {
                        var p = pixels[y * Cell + x];
                        if (p.a == 0) continue;
                        sheet.SetPixel(x0 + x, y0 + y, Color.Lerp(sheet.GetPixel(x0 + x, y0 + y), p, p.a / 255f));
                    }
            }
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(folder, "rendu_realiste_cabochons.png"), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
    }

    // The rough corundums: every habit, for a ruby and a sapphire, seen from two sides.
    private static void ExportRough(GemDatabase database, string folder)
    {
        var habits = new[] { "corundum_barrel", "corundum_bipyramid", "corundum_prism", "corundum_plate", "corundum_pebble", "corundum_fragment" };
        var species = new[] { "Corindon_Rubis", "Saphir_Bleu" };
        var angles = new[] { new Vector3(-28f, 24f, 8f), new Vector3(40f, -35f, 20f) };
        var width = habits.Length * (Cell + Gap) + Gap;
        var height = species.Length * angles.Length * (Cell + Gap) + Gap;
        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        sheet.SetPixels32(background);

        var view = new GemView3D(Cell) { Realistic = true };
        var row = 0;
        foreach (var name in species)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == name) definition = candidate;
            if (definition == null) continue;

            foreach (var angle in angles)
            {
                for (var column = 0; column < habits.Length; column++)
                {
                    var mesh = GemMesh.GetHabit(habits[column]);
                    if (mesh == null) continue;
                    view.Draw(mesh, Vector3.one * 3f, Quaternion.Euler(angle), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, GemLook.For(definition));
                    var pixels = view.Pixels;
                    var x0 = Gap + column * (Cell + Gap);
                    var y0 = height - (row + 1) * (Cell + Gap);
                    for (var y = 0; y < Cell; y++)
                        for (var x = 0; x < Cell; x++)
                        {
                            var p = pixels[y * Cell + x];
                            if (p.a == 0) continue;
                            sheet.SetPixel(x0 + x, y0 + y, Color.Lerp(sheet.GetPixel(x0 + x, y0 + y), p, p.a / 255f));
                        }
                }
                row++;
            }
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(folder, "rendu_realiste_bruts.png"), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
    }

    // The other families: metals (pyrite, hematite), patterned stones, the effects of light (opal, labradorite, moonstone, pearl...), a lump of rough. Dome front, dome tilted, a
    // faceted cushion, a rough lump.
    private static void ExportOthers(GemDatabase database, string folder)
    {
        var names = new[]
        {
            "Pyrite", "Hematite", "Lapis_Lazuli", "Turquoise", "Agate", "Opale_Noble", "Labradorite", "Pierre_De_Lune", "Perle", "Perle_Tahiti", "Oeil_de_Tigre", "Ambre",
            "Ivoire_Elephant", "Corail", "Jade_Jadeite", "Obsidienne", "Chrysoberyl_Alexandrite", "Ametrine", "Verre", "Ecaille_Tortue",
        };
        var columns = 4;
        var width = columns * (Cell + Gap) + Gap;
        var height = names.Length * (Cell + Gap) + Gap;
        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        sheet.SetPixels32(background);

        var view = new GemView3D(Cell) { Realistic = true };
        var dome = GemMesh.Get("Ovale", "Cabochon");
        var cushion = GemMesh.Get("Coussin", "Mixte");
        var lump = GemMesh.GetRough(5);
        var report = new System.Text.StringBuilder("Colonnes : cabochon de face | cabochon incliné | coussin facetté | brut\n");
        for (var row = 0; row < names.Length; row++)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == names[row]) definition = candidate;
            if (definition == null) continue;
            report.AppendLine($"ligne {row + 1} : {definition.displayName}");

            var look = GemLook.For(definition);
            for (var column = 0; column < columns; column++)
            {
                var half = new Vector3(3.4f, 2.7f, 1.8f);
                switch (column)
                {
                    case 0: view.Draw(dome, half, Quaternion.identity, definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, look, look.Pattern == GemPattern.Chatoyant); break;
                    case 1: view.Draw(dome, half, Quaternion.Euler(-28f, 30f, 12f), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, look, look.Pattern == GemPattern.Chatoyant); break;
                    case 2: view.Draw(cushion, new Vector3(3f, 3f, 1.8f), Quaternion.Euler(20f, -18f, 6f), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, look); break;
                    default: view.Draw(lump, Vector3.one * 3f, Quaternion.Euler(-24f, 28f, 10f), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, look); break;
                }

                var pixels = view.Pixels;
                var x0 = Gap + column * (Cell + Gap);
                var y0 = height - (row + 1) * (Cell + Gap);
                for (var y = 0; y < Cell; y++)
                    for (var x = 0; x < Cell; x++)
                    {
                        var p = pixels[y * Cell + x];
                        if (p.a == 0) continue;
                        sheet.SetPixel(x0 + x, y0 + y, Color.Lerp(sheet.GetPixel(x0 + x, y0 + y), p, p.a / 255f));
                    }
            }
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(folder, "rendu_realiste_autres.png"), sheet.EncodeToPNG());
        File.WriteAllText(Path.Combine(folder, "rendu_realiste_autres.txt"), report.ToString());
        Object.DestroyImmediate(sheet);
    }

    // The rough crystals of the other species: every habit of each (up to five), in one view.
    private static void ExportHabits(GemDatabase database, string folder)
    {
        var names = new[] { "Emeraude", "Quartz", "Amethyste", "Tourmaline_Rose", "Topaze", "Grenat_Almandin", "Grenat_Demantoide", "Spinelle", "Spinelle_Bleu", "Heliodore", "Diamant", "Fluorite", "Pyrite", "Zircon", "Peridot" };
        var columns = 5;
        var width = columns * (Cell + Gap) + Gap;
        var height = names.Length * (Cell + Gap) + Gap;
        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        sheet.SetPixels32(background);

        var view = new GemView3D(Cell) { Realistic = true };
        var report = new System.Text.StringBuilder();
        for (var row = 0; row < names.Length; row++)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == names[row]) definition = candidate;
            if (definition == null) continue;

            var habits = CrystalHabits.For(definition);
            var line = $"ligne {row + 1} : {definition.displayName} :";
            for (var column = 0; column < habits.Count && column < columns; column++)
            {
                var mesh = GemMesh.GetHabit(habits[column].Habit.Id);
                if (mesh == null) continue;
                line += " " + habits[column].Habit.Id;
                view.Draw(mesh, Vector3.one * 3f, Quaternion.Euler(-26f, 26f, 10f), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, GemLook.For(definition));
                var pixels = view.Pixels;
                var x0 = Gap + column * (Cell + Gap);
                var y0 = height - (row + 1) * (Cell + Gap);
                for (var y = 0; y < Cell; y++)
                    for (var x = 0; x < Cell; x++)
                    {
                        var p = pixels[y * Cell + x];
                        if (p.a == 0) continue;
                        sheet.SetPixel(x0 + x, y0 + y, Color.Lerp(sheet.GetPixel(x0 + x, y0 + y), p, p.a / 255f));
                    }
            }
            report.AppendLine(line);
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(folder, "rendu_realiste_cristaux.png"), sheet.EncodeToPNG());
        File.WriteAllText(Path.Combine(folder, "rendu_realiste_cristaux.txt"), report.ToString());
        Object.DestroyImmediate(sheet);
    }

    private const int Cell = 192;
    private const int Gap = 6;

    [MenuItem("GemLab/Export Realistic Gems")]
    public static void Export()
    {
        var database = GemDatabase.Instance;
        if (database == null) { Debug.LogError("GemDatabase introuvable."); return; }

        var names = new[] { "Diamant", "CZ", "Moissanite_Synthetique", "Corindon_Rubis", "Saphir_Bleu", "Emeraude", "Amethyste", "Citrine", "Tanzanite", "Spinelle", "Peridot", "Grenat_Almandin", "Heliodore", "Grenat_Demantoide", "Spinelle_Bleu" };
        var views = new (string shape, string style, Vector3 euler, string label)[]
        {
            ("Rond", "Brillant", new Vector3(0f, 0f, 0f), "Rond, de face"),
            ("Rond", "Brillant", new Vector3(22f, -14f, 0f), "Rond, incliné"),
            ("Rond", "Brillant", new Vector3(-30f, 26f, 8f), "Rond, très incliné"),
            ("Coussin", "Mixte", new Vector3(18f, 20f, 6f), "Coussin mixte"),
            ("Rectangle à pans coupés", "Émeraude", new Vector3(24f, -18f, 0f), "Taille émeraude"),
        };

        var folder = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Docs", "planches_gemmes");
        Directory.CreateDirectory(folder);
        var width = views.Length * (Cell + Gap) + Gap;
        var height = names.Length * (Cell + Gap) + Gap;
        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        sheet.SetPixels32(background);

        var view = new GemView3D(Cell) { Realistic = true };
        var report = new System.Text.StringBuilder("Rendu réaliste. Colonnes : " + string.Join(" | ", System.Array.ConvertAll(views, v => v.label)) + "\n");
        for (var row = 0; row < names.Length; row++)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == names[row]) definition = candidate;
            if (definition == null) continue;
            report.AppendLine($"ligne {row + 1} : {definition.displayName}");

            for (var column = 0; column < views.Length; column++)
            {
                var mesh = GemMesh.Get(views[column].shape, views[column].style);
                var aspect = GemMesh.Aspect(views[column].shape);
                var half = new Vector3(aspect.x * 3f, aspect.y * 3f, 1.8f);
                view.Draw(mesh, half, Quaternion.Euler(views[column].euler), definition.iconColor, definition.iconFacetColor, definition.Diaphaneity, 0, GemLook.For(definition));

                var pixels = view.Pixels;
                var x0 = Gap + column * (Cell + Gap);
                var y0 = height - (row + 1) * (Cell + Gap);
                for (var y = 0; y < Cell; y++)
                    for (var x = 0; x < Cell; x++)
                    {
                        var p = pixels[y * Cell + x];
                        if (p.a == 0) continue;
                        sheet.SetPixel(x0 + x, y0 + y, Color.Lerp(sheet.GetPixel(x0 + x, y0 + y), p, p.a / 255f));
                    }
            }
        }

        sheet.Apply();
        File.WriteAllBytes(Path.Combine(folder, "rendu_realiste.png"), sheet.EncodeToPNG());
        File.WriteAllText(Path.Combine(folder, "rendu_realiste.txt"), report.ToString());
        Object.DestroyImmediate(sheet);
        ExportDomes(database, folder);
        ExportRough(database, folder);
        ExportOthers(database, folder);
        ExportHabits(database, folder);
        Debug.Log("Rendu réaliste exporté : " + Path.Combine(folder, "rendu_realiste.png") + (RealGemRenderer.Available ? "" : " (le shader n'est pas disponible : rendu pixel)"));
        EditorUtility.RevealInFinder(Path.Combine(folder, "rendu_realiste.png"));
    }
}
