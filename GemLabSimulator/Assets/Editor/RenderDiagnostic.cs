using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu GemLab > Diagnostic du rendu realiste : liste les tailles (forme x style) et les habitus de cristaux que le rendu 3D transparent ne sait pas dessiner
// (le jeu retombe alors sur l'ancien dessin en pixels), puis les especes concernees. Ecrit Docs/planches_gemmes/diagnostic_rendu.txt.
public static class RenderDiagnostic
{
    [MenuItem("GemLab/Diagnostic du rendu realiste")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var bad = new System.Collections.Generic.HashSet<string>();

        sb.AppendLine("== Tailles (forme | style) : OK = rendu 3D transparent, PIXELS = ancien dessin ==");
        foreach (var shape in LapidaryCatalog.Shapes)
            foreach (var style in LapidaryCatalog.Styles)
            {
                var mesh = GemMesh.Get(shape, style);
                var ok = RealGemRenderer.CanRender(mesh, out var planes);
                if (!ok) bad.Add(shape + "|" + style);
                sb.AppendLine($"{(ok ? "OK    " : "PIXELS")}  {shape} | {style}  (plans : {planes}{(mesh != null && mesh.Smooth ? ", dome" : "")})");
            }

        sb.AppendLine();
        sb.AppendLine("== Habitus de cristaux bruts ==");
        foreach (var habit in CrystalHabits.All)
        {
            var mesh = GemMesh.GetHabit(habit.Id);
            var ok = RealGemRenderer.CanRender(mesh, out var planes);
            sb.AppendLine($"{(ok ? "OK    " : "PIXELS")}  {habit.Id}  (plans : {planes})");
        }

        sb.AppendLine();
        sb.AppendLine("== Taille par defaut de chaque espece ==");
        var database = GemDatabase.Instance;
        if (database != null)
            foreach (var def in database.allGems)
            {
                if (def == null) continue;
                var key = def.shape + "|" + def.cutStyle;
                sb.AppendLine($"{(bad.Contains(key) ? "PIXELS" : "OK    ")}  {def.name}  :  {def.shape} | {def.cutStyle}  [{def.transparency}]");
            }

        var folder = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Docs", "planches_gemmes");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "diagnostic_rendu.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log("Diagnostic du rendu realiste ecrit : " + path);
    }
}
