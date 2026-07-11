using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class SceneBootstrap
{
    private const int PixelsPerUnit = 32;

    private const int RoomMinX = -6;
    private const int RoomMaxX = 20;
    private const int RoomMinY = -4;
    private const int RoomMaxY = 4;

    private const int DividerX = 6;
    private const int DoorGapY1 = -1;
    private const int DoorGapY2 = 0;

    [MenuItem("GemLab/Setup Home Scene")]
    public static void CreateHomeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.11f, 0.13f);
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var pixelPerfect = cam.gameObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = PixelsPerUnit;
        pixelPerfect.refResolutionX = 384;
        pixelPerfect.refResolutionY = 216;
        pixelPerfect.upscaleRT = true;
        pixelPerfect.pixelSnapping = true;
        pixelPerfect.cropFrameX = true;
        pixelPerfect.cropFrameY = true;

        var gridGO = new GameObject("Grid", typeof(Grid));

        var groundGO = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
        groundGO.transform.SetParent(gridGO.transform);
        var groundTilemap = groundGO.GetComponent<Tilemap>();

        var wallsGO = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallsGO.transform.SetParent(gridGO.transform);
        var wallsTilemap = wallsGO.GetComponent<Tilemap>();

        var floorTile = GetOrCreateTile("floor_pixel", FloorPixel);
        var wallTile = GetOrCreateTile("wall_pixel", WallPixel);

        for (var x = RoomMinX; x <= RoomMaxX; x++)
        {
            for (var y = RoomMinY; y <= RoomMaxY; y++)
            {
                var isPerimeter = x == RoomMinX || x == RoomMaxX || y == RoomMinY || y == RoomMaxY;
                var isDivider = x == DividerX && y != DoorGapY1 && y != DoorGapY2;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter || isDivider)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var playerSprite = GetOrCreatePixelSprite("player_pixel", PlayerPattern, PlayerPalette);

        var player = new GameObject("Player");
        player.tag = "Player";
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = playerSprite;
        sr.sortingOrder = 5;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.transform.position = Vector3.zero;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        var mailboxSprite = GetOrCreatePixelSprite("mailbox_pixel", MailboxPattern, MailboxPalette);
        var mailbox = new GameObject("Mailbox");
        var mailboxSr = mailbox.AddComponent<SpriteRenderer>();
        mailboxSr.sprite = mailboxSprite;
        mailboxSr.sortingOrder = 4;
        var mailboxCol = mailbox.AddComponent<BoxCollider2D>();
        mailboxCol.isTrigger = true;
        mailboxCol.size = new Vector2(1.2f, 1.2f);
        var mailboxScript = mailbox.AddComponent<Mailbox>();
        mailbox.transform.position = new Vector3(DividerX - 2, RoomMinY + 1, 0f);

        var gemPool = GetOrCreateGemPool();
        var so = new SerializedObject(mailboxScript);
        var poolProp = so.FindProperty("gemPool");
        poolProp.arraySize = gemPool.Length;
        for (var i = 0; i < gemPool.Length; i++)
            poolProp.GetArrayElementAtIndex(i).objectReferenceValue = gemPool[i];
        so.ApplyModifiedProperties();

        CreateExaminationStation("Loupe", ExaminationTool.MeasurementType.Hardness, new Vector3(9, 2, 0f));
        CreateExaminationStation("Refractometre", ExaminationTool.MeasurementType.RefractiveIndex, new Vector3(12, 2, 0f));
        CreateExaminationStation("BalanceHydrostatique", ExaminationTool.MeasurementType.Density, new Vector3(15, 2, 0f));
        CreateExaminationStation("Dichroscope", ExaminationTool.MeasurementType.Pleochroism, new Vector3(18, 2, 0f));
        CreateExaminationStation("LampeUV", ExaminationTool.MeasurementType.Fluorescence, new Vector3(10, -2.5f, 0f));

        CreateDirector(new Vector3(14, -2, 0f));
        CreateUI();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Home.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Home.unity", true)
        };

        AssetDatabase.SaveAssets();
        Debug.Log("Home scene created at Assets/Scenes/Home.unity");
    }

    private static void CreateExaminationStation(string toolName, ExaminationTool.MeasurementType measurementType, Vector3 position)
    {
        var sprite = GetToolSprite(toolName);

        var station = new GameObject(toolName);
        var stationSr = station.AddComponent<SpriteRenderer>();
        stationSr.sprite = sprite;
        stationSr.sortingOrder = 4;

        var stationCol = station.AddComponent<BoxCollider2D>();
        stationCol.isTrigger = true;
        stationCol.size = new Vector2(1.2f, 1.2f);

        var tool = station.AddComponent<ExaminationTool>();
        var so = new SerializedObject(tool);
        so.FindProperty("toolName").stringValue = toolName;
        so.FindProperty("measurementType").enumValueIndex = (int)measurementType;
        so.ApplyModifiedProperties();

        station.transform.position = position;
    }

    private static void CreateDirector(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("directeur_pixel", DirectorPattern, DirectorPalette);

        var director = new GameObject("Directeur");
        var sr = director.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;

        var col = director.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        director.AddComponent<DirectorNPC>();
        director.transform.position = position;
        director.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
    }

    private static readonly string[] ReportSpeciesOptions =
    {
        "Quartz", "Corindon (Rubis)", "Spinelle", "Grenat", "Verre"
    };

    private static void CreateUI()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var player = GameObject.FindWithTag("Player");
        var wallet = player.GetComponent<PlayerWallet>();
        var inventory = player.GetComponent<PlayerInventory>();

        var moneyGO = CreateUIText("MoneyText", canvasGO.transform, "Argent : 0€",
            new Vector2(20, -20), new Vector2(300, 30), 24, TextAnchor.UpperLeft, Color.white);
        var hud = moneyGO.AddComponent<HUDController>();
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("moneyText").objectReferenceValue = moneyGO.GetComponent<Text>();
        hudSo.FindProperty("wallet").objectReferenceValue = wallet;
        hudSo.ApplyModifiedProperties();

        CreateUIText("InventoryHint", canvasGO.transform, "Tab : Inventaire",
            new Vector2(20, -50), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));

        var panelGO = new GameObject("ReportPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(440, 480);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Rapport d'identification",
            new Vector2(0, -20), new Vector2(400, 34), 22, TextAnchor.UpperCenter, Color.white);

        var measurementsGO = CreateUIText("Measurements", panelGO.transform, "",
            new Vector2(0, -60), new Vector2(400, 70), 16, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f));

        for (var i = 0; i < ReportSpeciesOptions.Length; i++)
        {
            var y = -140 - i * 42;
            var buttonGO = CreateUIButton($"Option_{ReportSpeciesOptions[i]}", panelGO.transform, ReportSpeciesOptions[i],
                new Vector2(0, y), new Vector2(360, 34));
            var option = buttonGO.AddComponent<ReportOptionButton>();
            var optSo = new SerializedObject(option);
            optSo.FindProperty("speciesName").stringValue = ReportSpeciesOptions[i];
            optSo.ApplyModifiedProperties();
        }

        var resultGO = CreateUIText("Result", panelGO.transform, "",
            new Vector2(0, -360), new Vector2(400, 40), 16, TextAnchor.UpperCenter, new Color(0.9f, 0.85f, 0.3f));

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -420), new Vector2(200, 34));
        closeButtonGO.AddComponent<CloseReportButton>();

        var controller = canvasGO.AddComponent<ReportUIController>();
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("panel").objectReferenceValue = panelGO;
        ctrlSo.FindProperty("measurementsText").objectReferenceValue = measurementsGO.GetComponent<Text>();
        ctrlSo.FindProperty("resultText").objectReferenceValue = resultGO.GetComponent<Text>();
        ctrlSo.ApplyModifiedProperties();

        var feedbackGO = CreateUIText("ToolFeedback", canvasGO.transform, "",
            new Vector2(0, 20), new Vector2(700, 40), 18, TextAnchor.LowerCenter, new Color(0.95f, 0.95f, 0.8f));
        var feedbackRect = feedbackGO.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 0f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0f);
        feedbackRect.pivot = new Vector2(0.5f, 0f);
        var toolFeedback = feedbackGO.AddComponent<ToolFeedbackUI>();
        var feedbackSo = new SerializedObject(toolFeedback);
        feedbackSo.FindProperty("feedbackText").objectReferenceValue = feedbackGO.GetComponent<Text>();
        feedbackSo.ApplyModifiedProperties();

        CreateMeasurementMinigameUI(canvasGO.transform);
        CreateInventoryUI(canvasGO.transform, inventory);
    }

    private static void CreateInventoryUI(Transform canvasTransform, PlayerInventory inventory)
    {
        var panelGO = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 280);
        panelRect.anchoredPosition = new Vector2(-20, 0);
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Inventaire",
            new Vector2(0, -16), new Vector2(360, 30), 20, TextAnchor.UpperCenter, Color.white);

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 1f);
        slotsRect.anchorMax = new Vector2(0.5f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, -56);
        slotsRect.sizeDelta = new Vector2(360, 210);

        var inventoryUI = canvasTransform.gameObject.AddComponent<InventoryUI>();
        var so = new SerializedObject(inventoryUI);
        so.FindProperty("inventory").objectReferenceValue = inventory;
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        so.ApplyModifiedProperties();
    }

    private static void CreateMeasurementMinigameUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("MinigamePanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(440, 160);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        var instructionGO = CreateUIText("Instruction", panelGO.transform, "",
            new Vector2(0, -16), new Vector2(400, 40), 16, TextAnchor.UpperCenter, Color.white);

        var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackGO.transform.SetParent(panelGO.transform, false);
        var trackRect = trackGO.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = new Vector2(0, -30);
        trackRect.sizeDelta = new Vector2(360, 20);
        trackGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

        var targetZoneGO = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
        targetZoneGO.transform.SetParent(trackGO.transform, false);
        var targetRect = targetZoneGO.GetComponent<RectTransform>();
        targetRect.anchorMin = new Vector2(0.5f, 0.5f);
        targetRect.anchorMax = new Vector2(0.5f, 0.5f);
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        targetRect.sizeDelta = new Vector2(60, 20);
        targetZoneGO.GetComponent<Image>().color = new Color(0.35f, 0.75f, 0.4f, 0.7f);

        var needleGO = new GameObject("Needle", typeof(RectTransform), typeof(Image));
        needleGO.transform.SetParent(trackGO.transform, false);
        var needleRect = needleGO.GetComponent<RectTransform>();
        needleRect.anchorMin = new Vector2(0.5f, 0.5f);
        needleRect.anchorMax = new Vector2(0.5f, 0.5f);
        needleRect.pivot = new Vector2(0.5f, 0.5f);
        needleRect.sizeDelta = new Vector2(6, 28);
        needleGO.GetComponent<Image>().color = Color.white;

        var minigame = canvasTransform.gameObject.AddComponent<MeasurementMinigameUI>();
        var so = new SerializedObject(minigame);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("track").objectReferenceValue = trackRect;
        so.FindProperty("needle").objectReferenceValue = needleRect;
        so.FindProperty("targetZone").objectReferenceValue = targetRect;
        so.FindProperty("instructionText").objectReferenceValue = instructionGO.GetComponent<Text>();
        so.ApplyModifiedProperties();
    }

    private static GameObject CreateUIText(string name, Transform parent, string text, Vector2 anchoredPosition,
        Vector2 sizeDelta, int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        return go;
    }

    private static GameObject CreateUIButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f, 1f);

        var textGO = CreateUIText("Label", go.transform, label, Vector2.zero, sizeDelta, 16, TextAnchor.MiddleCenter, Color.white);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        return go;
    }

    private static GemDefinition[] GetOrCreateGemPool()
    {
        return new[]
        {
            GetOrCreateGem("Quartz", "Quartz",
                "Un cristal de quartz incolore, courant, envoyé pour un premier examen.",
                1.544f, 2.65f, 7f, false, "",
                "Aucune fluorescence notable sous UV.", 15),
            GetOrCreateGem("Corindon_Rubis", "Corindon (Rubis)",
                "Un corindon rouge intense, envoyé pour confirmer s'il s'agit d'un rubis naturel.",
                1.766f, 4.00f, 9f, true, "Rouge / Orangé-rouge selon l'axe d'observation",
                "Fluorescence rouge vif sous UV longue onde (indicateur classique du rubis).", 150),
            GetOrCreateGem("Spinelle", "Spinelle",
                "Une pierre rouge vif, historiquement souvent confondue avec le rubis.",
                1.715f, 3.60f, 8f, false, "",
                "Fluorescence rouge, souvent plus terne que le rubis, sous UV longue onde.", 45),
            GetOrCreateGem("Grenat", "Grenat",
                "Un grenat rouge sombre, à ne pas confondre avec un rubis ou un spinelle.",
                1.79f, 4.05f, 7.5f, false, "",
                "Aucune fluorescence sous UV (caractéristique du grenat).", 25),
            GetOrCreateGem("Verre", "Verre",
                "Un verre imitant l'apparence d'une pierre précieuse rouge.",
                1.50f, 2.50f, 5.5f, false, "",
                "Fluorescence variable ou absente, souvent avec des bulles visibles à la loupe.", 5)
        };
    }

    private static GemDefinition GetOrCreateGem(string assetName, string displayName, string description,
        float refractiveIndex, float density, float hardness, bool pleochroic, string pleochroicDescription,
        string fluorescenceDescription, int baseValue)
    {
        var path = $"Assets/Data/Gems/{assetName}.asset";
        var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
        var isNew = gem == null;
        if (isNew) gem = ScriptableObject.CreateInstance<GemDefinition>();

        gem.displayName = displayName;
        gem.description = description;
        gem.refractiveIndex = refractiveIndex;
        gem.density = density;
        gem.hardness = hardness;
        gem.pleochroic = pleochroic;
        gem.pleochroicDescription = pleochroicDescription;
        gem.fluorescenceDescription = fluorescenceDescription;
        gem.baseValue = baseValue;

        if (isNew)
        {
            Directory.CreateDirectory("Assets/Data/Gems");
            AssetDatabase.CreateAsset(gem, path);
        }
        else
        {
            EditorUtility.SetDirty(gem);
        }

        return gem;
    }

    private static Tile GetOrCreateTile(string name, Func<int, int, Color> pixelFunc)
    {
        var tilePath = $"Assets/Art/Tilesets/{name}.asset";
        var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existingTile != null) return existingTile;

        var sprite = GetOrCreateProceduralSprite(name, pixelFunc, 32);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;

        Directory.CreateDirectory("Assets/Art/Tilesets");
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    // ----- Pixel art generation -----

    private static string Row(params object[] parts)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < parts.Length; i += 2)
        {
            var count = (int)parts[i];
            var c = (char)parts[i + 1];
            sb.Append(c, count);
        }
        return sb.ToString();
    }

    private static Sprite GetOrCreatePixelSprite(string name, string[] pattern, Dictionary<char, Color> palette)
    {
        var spritePath = $"Assets/Art/Sprites/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        var tex = BuildPixelArtTexture(pattern, palette, 2);
        Directory.CreateDirectory("Assets/Art/Sprites");
        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        return ImportSprite(spritePath);
    }

    private static Sprite GetOrCreateProceduralSprite(string name, Func<int, int, Color> pixelFunc, int size)
    {
        var spritePath = $"Assets/Art/Sprites/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, pixelFunc(x, y));
        tex.Apply();

        Directory.CreateDirectory("Assets/Art/Sprites");
        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        return ImportSprite(spritePath);
    }

    private static Texture2D BuildPixelArtTexture(string[] pattern, IReadOnlyDictionary<char, Color> palette, int scale)
    {
        var srcSize = pattern.Length;
        var outSize = srcSize * scale;
        var tex = new Texture2D(outSize, outSize, TextureFormat.RGBA32, false);

        for (var y = 0; y < srcSize; y++)
        {
            var row = pattern[y];
            for (var x = 0; x < srcSize; x++)
            {
                var c = palette.TryGetValue(row[x], out var col) ? col : Color.clear;
                var texX = x * scale;
                var texY = (srcSize - 1 - y) * scale;
                for (var oy = 0; oy < scale; oy++)
                    for (var ox = 0; ox < scale; ox++)
                        tex.SetPixel(texX + ox, texY + oy, c);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Sprite ImportSprite(string spritePath)
    {
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static Color WallPixel(int x, int y)
    {
        var brick = new Color(0.28f, 0.27f, 0.30f);
        var brickDark = new Color(0.23f, 0.22f, 0.25f);
        var mortar = new Color(0.16f, 0.15f, 0.17f);

        var rowBrick = y / 8;
        var offset = rowBrick % 2 == 0 ? 0 : 8;
        var mortarLine = y % 8 == 7 || (x + offset) % 16 == 15;
        if (mortarLine) return mortar;
        return (x / 4 + y / 4) % 2 == 0 ? brick : brickDark;
    }

    private static Color FloorPixel(int x, int y)
    {
        var plankA = new Color(0.58f, 0.45f, 0.32f);
        var plankB = new Color(0.54f, 0.42f, 0.29f);
        var lineColor = new Color(0.40f, 0.31f, 0.21f);

        var plankRow = y / 8;
        if (y % 8 == 7) return lineColor;
        var baseColor = plankRow % 2 == 0 ? plankA : plankB;
        var grain = (x + plankRow * 5) % 11 == 0;
        return grain ? plankB : baseColor;
    }

    private static Sprite GetToolSprite(string toolName)
    {
        switch (toolName)
        {
            case "Loupe": return GetOrCreatePixelSprite("loupe_pixel", LoupePattern, LoupePalette);
            case "Refractometre": return GetOrCreatePixelSprite("refractometre_pixel", RefractometrePattern, RefractometrePalette);
            case "BalanceHydrostatique": return GetOrCreatePixelSprite("balance_pixel", BalancePattern, BalancePalette);
            case "Dichroscope": return GetOrCreatePixelSprite("dichroscope_pixel", DichroscopePattern, DichroscopePalette);
            case "LampeUV": return GetOrCreatePixelSprite("lampeuv_pixel", LampeUVPattern, LampeUVPalette);
            default: return GetOrCreatePixelSprite("loupe_pixel", LoupePattern, LoupePalette);
        }
    }

    // ----- Sprite patterns (16x16 logical pixels, upscaled 2x) -----

    private static readonly string[] PlayerPattern =
    {
        Row(16, '.'),
        Row(3, '.', 8, 'H', 5, '.'),
        Row(2, '.', 12, 'H', 2, '.'),
        Row(2, '.', 2, 'H', 8, 'S', 2, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 10, 'S', 1, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 3, 'S', 1, 'E', 2, 'S', 1, 'E', 3, 'S', 1, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 10, 'S', 1, 'H', 2, '.'),
        Row(3, '.', 10, 'S', 3, '.'),
        Row(4, '.', 8, 'C', 4, '.'),
        Row(3, '.', 10, 'C', 3, '.'),
        Row(3, '.', 1, 'D', 8, 'C', 1, 'D', 3, '.'),
        Row(3, '.', 1, 'D', 8, 'C', 1, 'D', 3, '.'),
        Row(3, '.', 10, 'C', 3, '.'),
        Row(4, '.', 3, 'C', 2, '.', 3, 'C', 4, '.'),
        Row(4, '.', 3, 'B', 2, '.', 3, 'B', 4, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> PlayerPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.22f, 0.15f, 0.10f) },
        { 'S', new Color(0.91f, 0.72f, 0.56f) },
        { 'E', new Color(0.10f, 0.10f, 0.10f) },
        { 'C', new Color(0.95f, 0.95f, 0.95f) },
        { 'D', new Color(0.78f, 0.78f, 0.80f) },
        { 'B', new Color(0.16f, 0.16f, 0.18f) },
    };

    private static readonly string[] DirectorPattern =
    {
        Row(16, '.'),
        Row(3, '.', 8, 'H', 5, '.'),
        Row(2, '.', 12, 'H', 2, '.'),
        Row(2, '.', 2, 'H', 8, 'S', 2, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 10, 'S', 1, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 3, 'S', 1, 'E', 2, 'S', 1, 'E', 3, 'S', 1, 'H', 2, '.'),
        Row(2, '.', 1, 'H', 10, 'S', 1, 'H', 2, '.'),
        Row(3, '.', 10, 'S', 3, '.'),
        Row(4, '.', 8, 'C', 4, '.'),
        Row(3, '.', 4, 'C', 2, 'T', 4, 'C', 3, '.'),
        Row(3, '.', 1, 'D', 3, 'C', 2, 'T', 3, 'C', 1, 'D', 3, '.'),
        Row(3, '.', 1, 'D', 3, 'C', 2, 'T', 3, 'C', 1, 'D', 3, '.'),
        Row(3, '.', 4, 'C', 2, 'T', 4, 'C', 3, '.'),
        Row(4, '.', 3, 'C', 2, '.', 3, 'C', 4, '.'),
        Row(4, '.', 3, 'B', 2, '.', 3, 'B', 4, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> DirectorPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.78f, 0.78f, 0.80f) },
        { 'S', new Color(0.91f, 0.72f, 0.56f) },
        { 'E', new Color(0.10f, 0.10f, 0.10f) },
        { 'C', new Color(0.17f, 0.19f, 0.22f) },
        { 'D', new Color(0.10f, 0.11f, 0.13f) },
        { 'T', new Color(0.63f, 0.16f, 0.16f) },
        { 'B', new Color(0.16f, 0.16f, 0.18f) },
    };

    private static readonly string[] MailboxPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(5, '.', 6, 'R', 5, '.'),
        Row(4, '.', 8, 'R', 4, '.'),
        Row(3, '.', 10, 'R', 3, '.'),
        Row(2, '.', 12, 'R', 2, '.'),
        Row(2, '.', 12, 'R', 2, '.'),
        Row(2, '.', 12, 'r', 2, '.'),
        Row(3, '.', 10, 'r', 3, '.'),
        Row(6, '.', 4, 'P', 6, '.'),
        Row(6, '.', 4, 'P', 6, '.'),
        Row(6, '.', 4, 'P', 6, '.'),
        Row(6, '.', 4, 'P', 6, '.'),
        Row(6, '.', 4, 'P', 6, '.'),
        Row(5, '.', 6, 'P', 5, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MailboxPalette = new Dictionary<char, Color>
    {
        { 'R', new Color(0.71f, 0.20f, 0.16f) },
        { 'r', new Color(0.52f, 0.15f, 0.12f) },
        { 'P', new Color(0.42f, 0.42f, 0.44f) },
    };

    private static readonly string[] LoupePattern =
    {
        Row(16, '.'),
        Row(4, '.', 6, 'R', 6, '.'),
        Row(3, '.', 1, 'R', 6, 'L', 1, 'R', 5, '.'),
        Row(2, '.', 1, 'R', 8, 'L', 1, 'R', 4, '.'),
        Row(2, '.', 1, 'R', 8, 'L', 1, 'R', 4, '.'),
        Row(2, '.', 1, 'R', 8, 'L', 1, 'R', 4, '.'),
        Row(2, '.', 1, 'R', 8, 'L', 1, 'R', 4, '.'),
        Row(3, '.', 1, 'R', 8, 'L', 1, 'R', 3, '.'),
        Row(4, '.', 1, 'R', 6, 'L', 1, 'R', 4, '.'),
        Row(5, '.', 8, 'R', 3, '.'),
        Row(7, '.', 2, 'R', 7, '.'),
        Row(8, '.', 2, 'H', 6, '.'),
        Row(9, '.', 2, 'H', 5, '.'),
        Row(10, '.', 2, 'H', 4, '.'),
        Row(11, '.', 2, 'H', 3, '.'),
        Row(12, '.', 2, 'H', 2, '.'),
    };

    private static readonly Dictionary<char, Color> LoupePalette = new Dictionary<char, Color>
    {
        { 'R', new Color(0.30f, 0.30f, 0.33f) },
        { 'L', new Color(0.75f, 0.92f, 0.93f) },
        { 'H', new Color(0.42f, 0.29f, 0.18f) },
    };

    private static readonly string[] RefractometrePattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(4, '.', 8, 'B', 4, '.'),
        Row(3, '.', 10, 'B', 3, '.'),
        Row(3, '.', 10, 'B', 3, '.'),
        Row(2, '.', 12, 'B', 2, '.'),
        Row(2, '.', 12, 'B', 2, '.'),
        Row(2, '.', 12, 'B', 2, '.'),
        Row(2, '.', 12, 'K', 2, '.'),
        Row(1, '.', 14, 'K', 1, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> RefractometrePalette = new Dictionary<char, Color>
    {
        { 'G', new Color(0.56f, 0.82f, 0.88f) },
        { 'B', new Color(0.23f, 0.29f, 0.36f) },
        { 'K', new Color(0.08f, 0.08f, 0.09f) },
    };

    private static readonly string[] BalancePattern =
    {
        Row(16, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(3, '.', 10, 'S', 3, '.'),
        Row(2, '.', 2, 'P', 6, '.', 2, 'P', 4, '.'),
        Row(2, '.', 2, 'P', 6, '.', 2, 'P', 4, '.'),
        Row(3, '.', 2, 'P', 6, '.', 2, 'P', 3, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(6, '.', 4, 'S', 6, '.'),
        Row(4, '.', 8, 'K', 4, '.'),
        Row(4, '.', 8, 'K', 4, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> BalancePalette = new Dictionary<char, Color>
    {
        { 'S', new Color(0.60f, 0.63f, 0.66f) },
        { 'P', new Color(0.42f, 0.44f, 0.47f) },
        { 'K', new Color(0.10f, 0.10f, 0.11f) },
    };

    private static readonly string[] DichroscopePattern =
    {
        Row(16, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(5, '.', 2, 'L', 2, 'D', 2, 'L', 5, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> DichroscopePalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.54f, 0.29f, 0.41f) },
        { 'L', new Color(0.88f, 0.63f, 0.75f) },
    };

    private static readonly string[] LampeUVPattern =
    {
        Row(16, '.'),
        Row(6, '.', 4, 'U', 6, '.'),
        Row(5, '.', 2, 'U', 2, 'u', 2, 'U', 5, '.'),
        Row(5, '.', 6, 'U', 5, '.'),
        Row(6, '.', 4, 'U', 6, '.'),
        Row(7, '.', 2, 'B', 7, '.'),
        Row(7, '.', 2, 'B', 7, '.'),
        Row(6, '.', 4, 'B', 6, '.'),
        Row(6, '.', 4, 'B', 6, '.'),
        Row(5, '.', 6, 'B', 5, '.'),
        Row(4, '.', 8, 'B', 4, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> LampeUVPalette = new Dictionary<char, Color>
    {
        { 'U', new Color(0.60f, 0.31f, 0.88f) },
        { 'u', new Color(0.79f, 0.56f, 1.00f) },
        { 'B', new Color(0.23f, 0.23f, 0.23f) },
    };
}
