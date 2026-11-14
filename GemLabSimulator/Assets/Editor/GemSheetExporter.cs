using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu GemLab > Export Gem Sheets : dessine toutes les pierres du jeu (une ligne par espèce, une colonne par taille) dans des planches PNG, dans Docs/planches_gemmes,
// pour voir d'un coup d'œil à quoi elles ressemblent. Un fichier index.txt dit quelle espèce est sur quelle ligne de quelle planche.
public static class GemSheetExporter
{
    private const int Scale = 3;
    private const int Cell = GemPixelArt.Size * Scale;
    private const int Gap = 6;
    private const int RowsPerSheet = 12;

    private static readonly (string shape, string style, string label)[] Columns =
    {
        ("Rond", "Brillant", "Rond brillant"),
        ("Ovale", "Brillant", "Ovale brillant"),
        ("Coussin", "Mixte", "Coussin mixte"),
        ("Rectangle à pans coupés", "Émeraude", "Émeraude"),
        ("Poire", "Brillant", "Poire"),
        ("Ovale", "Cabochon", "Cabochon"),
        ("", "", "Brut"),
    };

    [MenuItem("GemLab/Export Gem Sheets")]
    public static void Export()
    {
        var database = GemDatabase.Instance;
        if (database == null)
        {
            Debug.LogError("GemDatabase introuvable (Resources/GemDatabase).");
            return;
        }

        var species = new List<GemDefinition>();
        foreach (var definition in database.allGems)
            if (definition != null) species.Add(definition);
        species.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));

        var folder = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Docs", "planches_gemmes");
        Directory.CreateDirectory(folder);
        foreach (var old in Directory.GetFiles(folder, "planche_*.png")) File.Delete(old);

        var index = new StringBuilder();
        index.AppendLine("Colonnes : " + string.Join(" | ", System.Array.ConvertAll(Columns, c => c.label)));
        index.AppendLine("Taille d'une case : " + Cell + " px (la pierre fait " + GemPixelArt.Size + " px, agrandie " + Scale + " fois)");

        var sheets = Mathf.CeilToInt(species.Count / (float)RowsPerSheet);
        for (var sheet = 0; sheet < sheets; sheet++)
        {
            var width = Columns.Length * (Cell + Gap) + Gap;
            var height = RowsPerSheet * (Cell + Gap) + Gap;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var background = new Color32[width * height];
            for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
            image.SetPixels32(background);

            index.AppendLine();
            index.AppendLine($"== planche_{sheet + 1}.png ==");
            for (var row = 0; row < RowsPerSheet; row++)
            {
                var number = sheet * RowsPerSheet + row;
                if (number >= species.Count) break;
                var definition = species[number];
                index.AppendLine($"ligne {row + 1} : {definition.displayName} ({definition.name}) — {definition.color} ; {definition.transparency} ; {definition.luster}");

                for (var column = 0; column < Columns.Length; column++)
                {
                    Sprite sprite;
                    try
                    {
                        sprite = Columns[column].shape.Length == 0
                            ? GemIconFactory.GetRough(definition)
                            : GemIconFactory.Get(definition, Columns[column].shape, Columns[column].style);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"{definition.name} / {Columns[column].label} : {e.Message}");
                        sprite = null;
                    }
                    if (sprite == null || sprite.texture == null) continue;
                    Blit(image, sprite.texture, Gap + column * (Cell + Gap), height - (Gap + (row + 1) * (Cell + Gap) - Gap) - 0);
                }
            }

            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, $"planche_{sheet + 1}.png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }

        ExportEffects(database, folder, index);
        File.WriteAllText(Path.Combine(folder, "index.txt"), index.ToString(), Encoding.UTF8);
        Debug.Log($"Planches de gemmes : {species.Count} espèces, {sheets} planches dans {folder}");
        EditorUtility.RevealInFinder(Path.Combine(folder, "index.txt"));
    }

    // The cabochons with their effects: a plain dome, a six-rayed star, a four-rayed star, a cat's eye, for a few species.
    private static void ExportEffects(GemDatabase database, string folder, StringBuilder index)
    {
        var names = new[] { "Corindon_Rubis", "Saphir_Bleu", "Saphir_Rose", "Grenat_Almandin", "Diopside_Chromifere", "Chrysoberyl", "Quartz", "Tourmaline_Rose", "Apatite", "Emeraude", "Diamant", "Zircon" };
        var labels = new[] { "Dôme lisse", "Étoile à 6 branches", "Étoile à 4 branches", "Œil-de-chat", "Taille ronde" };
        var width = labels.Length * (Cell + Gap) + Gap;
        var height = names.Length * (Cell + Gap) + Gap;
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color32[width * height];
        for (var i = 0; i < background.Length; i++) background[i] = new Color32(52, 56, 66, 255);
        image.SetPixels32(background);

        index.AppendLine();
        index.AppendLine("== planche_effets.png ==");
        index.AppendLine("Colonnes : " + string.Join(" | ", labels));
        for (var row = 0; row < names.Length; row++)
        {
            GemDefinition definition = null;
            foreach (var candidate in database.allGems)
                if (candidate != null && candidate.name == names[row]) definition = candidate;
            if (definition == null) continue;
            index.AppendLine($"ligne {row + 1} : {definition.displayName}");

            var sprites = new[]
            {
                GemIconFactory.Get(definition, "Ovale", "Cabochon", definition.iconColor, definition.iconFacetColor, 0, false),
                GemIconFactory.Get(definition, "Ovale", "Cabochon", definition.iconColor, definition.iconFacetColor, 6, false),
                GemIconFactory.Get(definition, "Ovale", "Cabochon", definition.iconColor, definition.iconFacetColor, 4, false),
                GemIconFactory.Get(definition, "Ovale", "Cabochon", definition.iconColor, definition.iconFacetColor, 0, true),
                GemIconFactory.Get(definition, "Rond", "Brillant", definition.iconColor, definition.iconFacetColor, 0, false),
            };
            for (var column = 0; column < sprites.Length; column++)
                if (sprites[column] != null && sprites[column].texture != null)
                    Blit(image, sprites[column].texture, Gap + column * (Cell + Gap), height - (row + 1) * (Cell + Gap));
        }
        image.Apply();
        File.WriteAllBytes(Path.Combine(folder, "planche_effets.png"), image.EncodeToPNG());
        Object.DestroyImmediate(image);
    }

    // Copies a texture, enlarged without smoothing, at the given position (from the bottom left).
    private static void Blit(Texture2D target, Texture2D source, int x0, int y0)
    {
        Color32[] pixels;
        try { pixels = source.GetPixels32(); }
        catch (UnityException) { return; }

        var size = source.width;
        for (var y = 0; y < source.height; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixel = pixels[y * size + x];
                if (pixel.a == 0) continue;
                for (var dy = 0; dy < Scale; dy++)
                    for (var dx = 0; dx < Scale; dx++)
                    {
                        var tx = x0 + x * Scale + dx;
                        var ty = y0 + y * Scale + dy;
                        if (tx < 0 || ty < 0 || tx >= target.width || ty >= target.height) continue;
                        var back = target.GetPixel(tx, ty);
                        var alpha = pixel.a / 255f;
                        target.SetPixel(tx, ty, Color.Lerp(back, pixel, alpha));
                    }
            }
        }
    }
}
