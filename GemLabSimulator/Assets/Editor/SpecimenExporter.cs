using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu GemLab > Export Test Specimens : dessine les pierres de test de l'armoire de la sauvegarde 1 (celles que tools/add_specimens.py y a rangées, client « Spécimen test »)
// avec le rendu réaliste, chacune sous cinq angles, dans Docs/planches_gemmes/specimens_test_1.png (puis _2, _3 s'il y en a plus de douze) et la liste dans specimens_test.txt.
// Pas besoin de lancer le jeu : on juge le rendu, et on peut montrer l'image pour le corriger.
public static class SpecimenExporter
{
    private const int Cell = 200;
    private const int Gap = 6;
    private const int RowsPerSheet = 12;
    private const string TestClient = "Spécimen test";

    // Face-up, the angle the detail view starts with, two tilts, and a nearly side view.
    private static readonly Vector3[] Angles =
    {
        new Vector3(0f, 0f, 0f), new Vector3(-38f, 0f, 0f), new Vector3(-25f, 40f, 0f), new Vector3(-60f, -30f, 15f), new Vector3(-82f, 18f, 0f),
    };

    [MenuItem("GemLab/Export Test Specimens")]
    public static void Export()
    {
        var database = GemDatabase.Instance;
        if (database == null) { Debug.LogError("GemDatabase introuvable."); return; }

        var path = Path.Combine(Application.persistentDataPath, "savegame_1.json");
        if (!File.Exists(path)) { Debug.LogError("Pas de sauvegarde 1 : " + path); return; }
        var save = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        var stones = new List<GemInstance>();
        foreach (var data in save.cabinetGems)
        {
            if (data.clientName != TestClient) continue;
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == data.definitionName) definition = candidate;
            if (definition == null) continue;
            stones.Add(GemInstance.FromSaveData(definition, data));
        }
        if (stones.Count == 0) { Debug.LogWarning("Aucun spécimen de test dans l'armoire de la sauvegarde 1 (voir tools/add_specimens.py)."); return; }

        var folder = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Docs", "planches_gemmes");
        Directory.CreateDirectory(folder);
        var view = new GemView3D(Cell) { Realistic = true };
        var report = new StringBuilder("Colonnes : de face | départ de la fiche | incliné | très incliné | presque de côté\n");

        var sheets = (stones.Count + RowsPerSheet - 1) / RowsPerSheet;
        for (var sheetIndex = 0; sheetIndex < sheets; sheetIndex++)
        {
            var first = sheetIndex * RowsPerSheet;
            var rows = Mathf.Min(RowsPerSheet, stones.Count - first);
            var width = Angles.Length * (Cell + Gap) + Gap;
            var height = rows * (Cell + Gap) + Gap;
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var background = new Color32[width * height];
            for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
            sheet.SetPixels32(background);

            report.AppendLine("\nPlanche " + (sheetIndex + 1) + " (de haut en bas) :");
            for (var row = 0; row < rows; row++)
            {
                var gem = stones[first + row];
                report.AppendLine((row + 1) + ". " + gem.SpecimenId + " — " + gem.Definition.displayName + ", " + gem.Origin.name + ", " + gem.ClarityGrade + ", " + gem.CutShape + " " + gem.CutStyle
                                  + " " + gem.CaratWeight.ToString("0.0") + " ct" + (gem.AsterismRays > 0 ? ", étoile" : "") + ", soie " + gem.Quality.Silk.ToString("0.00"));
                for (var column = 0; column < Angles.Length; column++)
                {
                    var orientation = Quaternion.Euler(Angles[column]);
                    GemView3D.Colors(gem, out var body, out var facet);
                    GemLook.ApplyPleochroism(gem.Definition, orientation, ref body, ref facet);
                    view.Draw(GemView3D.MeshFor(gem), GemView3D.HalfExtents(gem), orientation, body, facet, gem.VisualDiaphaneity, gem.AsterismRays, GemLook.For(gem.Definition), gem.CatsEye, gem.Quality);

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
            File.WriteAllBytes(Path.Combine(folder, "specimens_test_" + (sheetIndex + 1) + ".png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        File.WriteAllText(Path.Combine(folder, "specimens_test.txt"), report.ToString());
        Debug.Log("Spécimens de test exportés : " + stones.Count + " pierres, " + sheets + " planche(s) dans " + folder + (RealGemRenderer.Available ? "" : " (le shader n'est pas disponible : rendu pixel)"));
        EditorUtility.RevealInFinder(Path.Combine(folder, "specimens_test_1.png"));
    }
}
