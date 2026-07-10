using System.IO;
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
        if (light != null) Object.DestroyImmediate(light);

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

        var floorTile = GetOrCreateTile("floor_placeholder", new Color(0.55f, 0.5f, 0.42f), new Color(0.5f, 0.45f, 0.38f));
        var wallTile = GetOrCreateTile("wall_placeholder", new Color(0.32f, 0.3f, 0.34f), new Color(0.18f, 0.17f, 0.2f));

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

        var playerSprite = GetOrCreatePlaceholderSprite("player_placeholder", new Color(0.85f, 0.3f, 0.6f), new Color(0.4f, 0.1f, 0.25f));

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
        player.AddComponent<PlayerGemHolder>();
        player.AddComponent<PlayerWallet>();
        player.transform.position = Vector3.zero;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        var mailboxSprite = GetOrCreatePlaceholderSprite("mailbox_placeholder", new Color(0.85f, 0.75f, 0.2f), new Color(0.5f, 0.4f, 0.1f));
        var mailbox = new GameObject("Mailbox");
        var mailboxSr = mailbox.AddComponent<SpriteRenderer>();
        mailboxSr.sprite = mailboxSprite;
        mailboxSr.sortingOrder = 4;
        var mailboxCol = mailbox.AddComponent<BoxCollider2D>();
        mailboxCol.isTrigger = true;
        mailboxCol.size = new Vector2(1.2f, 1.2f);
        var mailboxScript = mailbox.AddComponent<Mailbox>();
        mailbox.transform.position = new Vector3(DividerX - 2, RoomMinY + 1, 0f);

        var exampleGem = GetOrCreateExampleGem();
        var so = new SerializedObject(mailboxScript);
        so.FindProperty("gemToDeliver").objectReferenceValue = exampleGem;
        so.ApplyModifiedProperties();

        CreateExaminationStation("Loupe", ExaminationTool.MeasurementType.Hardness, new Vector3(10, 2, 0f), new Color(0.3f, 0.7f, 0.75f));
        CreateExaminationStation("Refractometre", ExaminationTool.MeasurementType.RefractiveIndex, new Vector3(14, 2, 0f), new Color(0.35f, 0.55f, 0.85f));
        CreateExaminationStation("BalanceHydrostatique", ExaminationTool.MeasurementType.Density, new Vector3(18, 2, 0f), new Color(0.6f, 0.45f, 0.8f));

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

    private static void CreateExaminationStation(string toolName, ExaminationTool.MeasurementType measurementType, Vector3 position, Color color)
    {
        var sprite = GetOrCreatePlaceholderSprite($"{toolName.ToLowerInvariant()}_placeholder", color, color * 0.6f);

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
        var sprite = GetOrCreatePlaceholderSprite("directeur_placeholder", new Color(0.85f, 0.85f, 0.9f), new Color(0.3f, 0.3f, 0.35f));

        var director = new GameObject("Directeur");
        var sr = director.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;

        var col = director.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 1.4f);

        director.AddComponent<DirectorNPC>();
        director.transform.position = position;
        director.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
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

        var moneyGO = CreateUIText("MoneyText", canvasGO.transform, "Argent : 0€",
            new Vector2(20, -20), new Vector2(300, 30), 24, TextAnchor.UpperLeft, Color.white);
        var hud = moneyGO.AddComponent<HUDController>();
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("moneyText").objectReferenceValue = moneyGO.GetComponent<Text>();
        hudSo.FindProperty("wallet").objectReferenceValue = wallet;
        hudSo.ApplyModifiedProperties();

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

    private static GemDefinition GetOrCreateExampleGem()
    {
        const string path = "Assets/Data/Gems/Quartz.asset";
        var existing = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
        if (existing != null) return existing;

        var gem = ScriptableObject.CreateInstance<GemDefinition>();
        gem.displayName = "Quartz";
        gem.description = "Un cristal de quartz incolore, courant, envoyé pour un premier examen.";
        gem.refractiveIndex = 1.544f;
        gem.density = 2.65f;
        gem.hardness = 7f;
        gem.baseValue = 15;

        Directory.CreateDirectory("Assets/Data/Gems");
        AssetDatabase.CreateAsset(gem, path);
        return gem;
    }

    private static Tile GetOrCreateTile(string name, Color fillColor, Color outlineColor)
    {
        var tilePath = $"Assets/Art/Tilesets/{name}.asset";
        var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existingTile != null) return existingTile;

        var sprite = GetOrCreatePlaceholderSprite(name, fillColor, outlineColor);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;

        Directory.CreateDirectory("Assets/Art/Tilesets");
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    private static Sprite GetOrCreatePlaceholderSprite(string name, Color fillColor, Color outlineColor)
    {
        var spritePath = $"Assets/Art/Sprites/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var isEdge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                tex.SetPixel(x, y, isEdge ? outlineColor : fillColor);
            }
        }
        tex.Apply();

        Directory.CreateDirectory("Assets/Art/Sprites");
        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

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
}
