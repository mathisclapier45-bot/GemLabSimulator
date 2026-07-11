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
    private const int RoomMaxX = 28;
    private const int RoomMinY = -4;
    private const int RoomMaxY = 4;

    private const int DividerX = 6;
    private const int DoorGapY1 = -1;
    private const int DoorGapY2 = 0;

    private const int TownMinX = -20;
    private const int TownMaxX = 20;
    private const int TownMinY = -14;
    private const int TownMaxY = 14;

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

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = Vector3.zero;

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
        player.AddComponent<PlayerChest>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateExitDoor(new Vector3(RoomMinX + 1, RoomMinY + 1, 0f));
        CreateChest(new Vector3(3, 2, 0f));

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

        CreateExaminationStation("Loupe", ExaminationTool.MeasurementType.VisualExamination, new Vector3(9, 2, 0f));
        CreateExaminationStation("Refractometre", ExaminationTool.MeasurementType.RefractiveIndex, new Vector3(11.5f, 2, 0f));
        CreateExaminationStation("BalanceHydrostatique", ExaminationTool.MeasurementType.Density, new Vector3(14, 2, 0f));
        CreateExaminationStation("Dichroscope", ExaminationTool.MeasurementType.Pleochroism, new Vector3(16.5f, 2, 0f));
        CreateExaminationStation("Polariscope", ExaminationTool.MeasurementType.OpticalCharacter, new Vector3(19, 2, 0f));
        CreateExaminationStation("Spectroscope", ExaminationTool.MeasurementType.AbsorptionSpectrum, new Vector3(21.5f, 2, 0f));
        CreateExaminationStation("Microscope", ExaminationTool.MeasurementType.Inclusions, new Vector3(24, 2, 0f));

        CreateExaminationStation("Conoscope", ExaminationTool.MeasurementType.InterferenceFigure, new Vector3(9, -2, 0f));
        CreateExaminationStation("UVLongues", ExaminationTool.MeasurementType.FluorescenceLongWave, new Vector3(12, -2, 0f));
        CreateExaminationStation("UVCourtes", ExaminationTool.MeasurementType.FluorescenceShortWave, new Vector3(15, -2, 0f));
        CreateExaminationStation("FiltreChelsea", ExaminationTool.MeasurementType.ChelseaFilter, new Vector3(18, -2, 0f));
        CreateExaminationStation("Micrometre", ExaminationTool.MeasurementType.Dimensions, new Vector3(21, -2, 0f));
        CreateExaminationStation("BalanceCarat", ExaminationTool.MeasurementType.Weight, new Vector3(24, -2, 0f));

        CreateDirector(new Vector3(26, 0, 0f));
        CreateUI(true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Home.unity");
        AddSceneToBuildSettings("Assets/Scenes/Home.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Home scene created at Assets/Scenes/Home.unity");
    }

    [MenuItem("GemLab/Setup Town Scene")]
    public static void CreateTownScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
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

        var boundaryGO = new GameObject("Boundary", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        boundaryGO.transform.SetParent(gridGO.transform);
        var boundaryTilemap = boundaryGO.GetComponent<Tilemap>();

        var grassTile = GetOrCreateTile("grass_pixel", GrassPixel);
        var hedgeTile = GetOrCreateTile("hedge_pixel", HedgePixel);

        for (var x = TownMinX; x <= TownMaxX; x++)
        {
            for (var y = TownMinY; y <= TownMaxY; y++)
            {
                var isPerimeter = x == TownMinX || x == TownMaxX || y == TownMinY || y == TownMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    boundaryTilemap.SetTile(cell, hedgeTile);
                else
                    groundTilemap.SetTile(cell, grassTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(-16, 4, 0f);

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
        player.AddComponent<PlayerChest>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateBuilding("Maison & Bureau de Gemmologie", new Vector3(-16, 8, 0f),
            new Color(0.65f, 0.22f, 0.18f), new Color(0.85f, 0.78f, 0.62f), "Home", true);
        CreateBuilding("Laboratoire de Gemmologie", new Vector3(-8, 8, 0f),
            new Color(0.30f, 0.45f, 0.55f), new Color(0.75f, 0.78f, 0.80f), "", false);
        CreateBuilding("Atelier Lapidaire", new Vector3(0, 8, 0f),
            new Color(0.45f, 0.40f, 0.35f), new Color(0.65f, 0.60f, 0.55f), "", false);
        CreateBuilding("Musee", new Vector3(8, 8, 0f),
            new Color(0.75f, 0.72f, 0.60f), new Color(0.92f, 0.90f, 0.85f), "", false);
        CreateBuilding("Quartier des Diamantaires", new Vector3(16, 8, 0f),
            new Color(0.25f, 0.30f, 0.55f), new Color(0.70f, 0.75f, 0.90f), "", false);
        CreateBuilding("Marche aux Pierres", new Vector3(-12, -6, 0f),
            new Color(0.65f, 0.42f, 0.18f), new Color(0.88f, 0.70f, 0.45f), "", false);
        CreateBuilding("Le Manoir", new Vector3(12, -6, 0f),
            new Color(0.35f, 0.18f, 0.42f), new Color(0.70f, 0.60f, 0.75f), "", false);
        CreateTunnelEntrance(new Vector3(0, -11, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Town.unity");
        AddSceneToBuildSettings("Assets/Scenes/Town.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Town scene created at Assets/Scenes/Town.unity");
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateExitDoor(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("exitdoor_pixel", DoorPattern, DoorPalette);

        var door = new GameObject("Sortie");
        var sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        door.transform.position = position;

        var col = door.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        var trigger = door.AddComponent<DoorTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("sceneName").stringValue = "Town";
        so.FindProperty("implemented").boolValue = true;
        so.ApplyModifiedProperties();
    }

    private static void CreateChest(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("chest_pixel", ChestPattern, ChestPalette);

        var chest = new GameObject("Coffre");
        var sr = chest.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        chest.transform.position = position;

        var col = chest.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        chest.AddComponent<ChestInteractable>();
    }

    private static void CreateBuilding(string label, Vector3 position, Color roof, Color wall, string targetScene, bool implemented)
    {
        var key = label.Replace(" ", "").Replace("&", "").ToLowerInvariant();
        var sprite = GetBuildingSprite(key, roof, wall);

        var go = new GameObject(label);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 0.6f);
        col.offset = new Vector2(0f, -0.5f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        so.FindProperty("sceneName").stringValue = targetScene;
        so.FindProperty("implemented").boolValue = implemented;
        so.FindProperty("lockedMessage").stringValue = $"{label} : ce lieu n'est pas encore accessible.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel(label, position + new Vector3(0, 1.3f, 0));
    }

    private static void CreateTunnelEntrance(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("tunnel_pixel", TunnelPattern, TunnelPalette);

        var go = new GameObject("Le Tunnel");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 0.6f);
        col.offset = new Vector2(0f, -0.3f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        so.FindProperty("sceneName").stringValue = "";
        so.FindProperty("implemented").boolValue = false;
        so.FindProperty("lockedMessage").stringValue = "Le Tunnel : un marchand mystérieux rôderait ici... pas encore accessible.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Le Tunnel", position + new Vector3(0, 1.3f, 0));
    }

    private static void CreateBuildingLabel(string text, Vector3 position)
    {
        var go = new GameObject("Label", typeof(TextMesh));
        go.transform.position = position;
        var tm = go.GetComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.1f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        go.GetComponent<MeshRenderer>().sortingOrder = 6;
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
        "Quartz", "Corindon (Rubis)", "Spinelle", "Grenat Almandin", "Verre",
        "Péridot", "Spinelle Bleu", "Saphir Bleu", "Zircon", "Diamant",
        "Améthyste", "Grenat Spessartite", "Émeraude",
        "Grenat Pyrope", "Grenat Tsavorite", "Grenat Hessonite"
    };

    private static void CreateUI(bool includeExaminationUI)
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var moneyGO = CreateUIText("MoneyText", canvasGO.transform, "Argent : 0€",
            new Vector2(20, -20), new Vector2(300, 30), 24, TextAnchor.UpperLeft, Color.white);
        moneyGO.AddComponent<HUDController>();
        var hudSo = new SerializedObject(moneyGO.GetComponent<HUDController>());
        hudSo.FindProperty("moneyText").objectReferenceValue = moneyGO.GetComponent<Text>();
        hudSo.ApplyModifiedProperties();

        CreateUIText("InventoryHint", canvasGO.transform, "Tab : Inventaire",
            new Vector2(20, -50), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));

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

        CreateGemTooltipUI(canvasGO.transform);
        CreateInventoryUI(canvasGO.transform);

        if (!includeExaminationUI) return;

        var panelGO = new GameObject("ReportPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        const float buttonWidth = 195f;
        const float buttonHeight = 34f;
        const float colGap = 10f;
        const float rowGap = 6f;
        const float buttonsStartY = -388f;
        var totalRows = Mathf.CeilToInt(ReportSpeciesOptions.Length / 2f);
        var buttonsEndY = buttonsStartY - (totalRows - 1) * (buttonHeight + rowGap) - buttonHeight;
        var panelHeight = -buttonsEndY + 110f;

        panelRect.sizeDelta = new Vector2(480, panelHeight);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Rapport d'identification",
            new Vector2(0, -18), new Vector2(440, 28), 20, TextAnchor.UpperCenter, Color.white);

        var measurementsGO = CreateUIText("Measurements", panelGO.transform, "",
            new Vector2(0, -52), new Vector2(440, 320), 12, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f));

        for (var i = 0; i < ReportSpeciesOptions.Length; i++)
        {
            var col = i % 2;
            var row = i / 2;
            var x = col == 0 ? -(buttonWidth + colGap) / 2f : (buttonWidth + colGap) / 2f;
            var y = buttonsStartY - row * (buttonHeight + rowGap);
            var buttonGO = CreateUIButton($"Option_{i}", panelGO.transform, ReportSpeciesOptions[i],
                new Vector2(x, y), new Vector2(buttonWidth, buttonHeight), 12);
            var option = buttonGO.AddComponent<ReportOptionButton>();
            var optSo = new SerializedObject(option);
            optSo.FindProperty("speciesName").stringValue = ReportSpeciesOptions[i];
            optSo.ApplyModifiedProperties();
        }

        var resultGO = CreateUIText("Result", panelGO.transform, "",
            new Vector2(0, buttonsEndY - 26f), new Vector2(440, 32), 15, TextAnchor.UpperCenter, new Color(0.9f, 0.85f, 0.3f));

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, buttonsEndY - 66f), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseReportButton>();

        var controller = canvasGO.AddComponent<ReportUIController>();
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("panel").objectReferenceValue = panelGO;
        ctrlSo.FindProperty("measurementsText").objectReferenceValue = measurementsGO.GetComponent<Text>();
        ctrlSo.FindProperty("resultText").objectReferenceValue = resultGO.GetComponent<Text>();
        ctrlSo.ApplyModifiedProperties();

        CreateMeasurementMinigameUI(canvasGO.transform);
        CreateChestUI(canvasGO.transform);
        CreateSpectrumUI(canvasGO.transform);
    }

    private static void CreateGemTooltipUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("GemTooltip", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(220, 30);
        panelGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
        panelGO.GetComponent<Image>().raycastTarget = false;
        panelGO.SetActive(false);

        var labelGO = CreateUIText("Label", panelGO.transform, "",
            Vector2.zero, new Vector2(220, 30), 14, TextAnchor.MiddleCenter, Color.white);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        labelGO.GetComponent<Text>().raycastTarget = false;

        var tooltip = canvasTransform.gameObject.AddComponent<GemTooltipUI>();
        var so = new SerializedObject(tooltip);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("label").objectReferenceValue = labelGO.GetComponent<Text>();
        so.FindProperty("rect").objectReferenceValue = panelRect;
        so.ApplyModifiedProperties();
    }

    private static void CreateInventoryUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(300, 260);
        panelRect.anchoredPosition = new Vector2(-20, 0);
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Inventaire",
            new Vector2(0, -14), new Vector2(260, 26), 18, TextAnchor.UpperCenter, Color.white);

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 1f);
        slotsRect.anchorMax = new Vector2(0.5f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, -46);
        slotsRect.sizeDelta = new Vector2(248, 184);

        var dropZone = panelGO.AddComponent<GemDropZone>();

        var mysteryIcon = GetGemIconSprite("mystery", new Color(0.42f, 0.42f, 0.45f), new Color(0.55f, 0.55f, 0.58f));

        var inventoryUI = canvasTransform.gameObject.AddComponent<InventoryUI>();
        var so = new SerializedObject(inventoryUI);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        so.FindProperty("dropZone").objectReferenceValue = dropZone;
        so.FindProperty("mysteryIcon").objectReferenceValue = mysteryIcon;
        so.ApplyModifiedProperties();
    }

    private static void CreateChestUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("ChestPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(300, 260);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Coffre",
            new Vector2(0, -14), new Vector2(260, 26), 18, TextAnchor.UpperCenter, Color.white);

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 1f);
        slotsRect.anchorMax = new Vector2(0.5f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, -46);
        slotsRect.sizeDelta = new Vector2(248, 184);

        var dropZone = panelGO.AddComponent<GemDropZone>();

        var chestUI = canvasTransform.gameObject.AddComponent<ChestUI>();
        var so = new SerializedObject(chestUI);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        so.FindProperty("dropZone").objectReferenceValue = dropZone;
        so.ApplyModifiedProperties();
    }

    private static void CreateSpectrumUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("SpectrumPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 240);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Spectroscope — Spectre d'absorption",
            new Vector2(0, -16), new Vector2(420, 28), 18, TextAnchor.UpperCenter, Color.white);

        var descGO = CreateUIText("Description", panelGO.transform, "",
            new Vector2(0, -50), new Vector2(420, 70), 13, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f));

        var barGO = new GameObject("SpectrumBar", typeof(RectTransform), typeof(Image));
        barGO.transform.SetParent(panelGO.transform, false);
        var barRect = barGO.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0, -140);
        barRect.sizeDelta = new Vector2(380, 40);
        var barImage = barGO.GetComponent<Image>();
        barImage.preserveAspect = false;

        CreateUIText("MinLabel", panelGO.transform, "400 nm",
            new Vector2(-190, -186), new Vector2(80, 20), 11, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.6f));
        CreateUIText("MaxLabel", panelGO.transform, "700 nm",
            new Vector2(190, -186), new Vector2(80, 20), 11, TextAnchor.UpperRight, new Color(0.6f, 0.6f, 0.6f));

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -206), new Vector2(160, 28));
        closeButtonGO.AddComponent<CloseSpectrumButton>();

        var spectrumUI = canvasTransform.gameObject.AddComponent<SpectrumUI>();
        var spectrumSo = new SerializedObject(spectrumUI);
        spectrumSo.FindProperty("panel").objectReferenceValue = panelGO;
        spectrumSo.FindProperty("spectrumImage").objectReferenceValue = barImage;
        spectrumSo.FindProperty("descriptionText").objectReferenceValue = descGO.GetComponent<Text>();
        spectrumSo.ApplyModifiedProperties();
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

    private static GameObject CreateUIButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize = 16)
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

        var textGO = CreateUIText("Label", go.transform, label, Vector2.zero, sizeDelta, fontSize, TextAnchor.MiddleCenter, Color.white);
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
            BuildGem("Quartz", g =>
            {
                g.displayName = "Quartz";
                g.description = "Un cristal de quartz incolore, courant, envoyé pour un premier examen.";
                g.icon = GetGemIconSprite("quartz", new Color(0.85f, 0.87f, 0.88f), new Color(0.97f, 0.98f, 0.99f));
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 7.5f;
                g.widthMm = 5.8f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.8f;
                g.luster = "Vitreux";
                g.color = "Incolore";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, parfois inclusions fluides ou en plumes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière (incolore).";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 50, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "États-Unis", rarityWeight = 20, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Corindon_Rubis", g =>
            {
                g.displayName = "Corindon (Rubis)";
                g.description = "Un corindon rouge intense, envoyé pour confirmer s'il s'agit d'un rubis naturel.";
                g.icon = GetGemIconSprite("corindon", new Color(0.75f, 0.12f, 0.12f), new Color(0.95f, 0.35f, 0.30f));
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 6.8f;
                g.widthMm = 5.9f;
                g.depthMm = 4.2f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Rouge";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rouge / Orangé-rouge selon l'axe d'observation.";
                g.fluorescenceLongWave = "Fluorescence rouge vif sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence rouge plus faible sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.absorptionSpectrum = "Raies d'absorption du chrome visibles dans le rouge.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 694f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 668f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 620f, widthNm = 12f },
                    new SpectrumLine { wavelengthNm = 560f, widthNm = 40f },
                };
                g.inclusions = "Soie de rutile (inclusions aciculaires), parfois halos autour de cristaux.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Réaction rouge vif sous filtre Chelsea (due au chrome).";
                g.baseValue = 150;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Birmanie (Mogok)", rarityWeight = 10, valueMultiplier = 3.0f,
                        inclusionOverride = "Soie de rutile fine et dense (\"soie birmane\"), fluorescence rouge marquée — signature du gisement de Mogok." },
                    new GemOrigin { name = "Mozambique", rarityWeight = 45, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Thaïlande (Chanthaburi)", rarityWeight = 25, valueMultiplier = 0.8f,
                        inclusionOverride = "Peu d'inclusions, souvent chauffé, soie généralement absente ou résorbée." },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 20, valueMultiplier = 1.3f },
                };
            }),
            BuildGem("Spinelle", g =>
            {
                g.displayName = "Spinelle";
                g.description = "Une pierre rouge vif, historiquement souvent confondue avec le rubis.";
                g.icon = GetGemIconSprite("spinelle", new Color(0.80f, 0.30f, 0.30f), new Color(0.95f, 0.55f, 0.55f));
                g.refractiveIndex = 1.715f;
                g.density = 3.60f;
                g.hardness = 8f;
                g.lengthMm = 6.0f;
                g.widthMm = 6.0f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux";
                g.color = "Rouge";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence rouge, d'intensité modérée, sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence rouge faible sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Raies de fer/chrome faibles.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 685f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 675f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 550f, widthNm = 35f },
                };
                g.inclusions = "Cristaux octaédriques, inclusions en \"empreintes digitales\" possibles.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Réaction rouge à rosâtre, d'intensité modérée.";
                g.baseValue = 45;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Birmanie", rarityWeight = 20, valueMultiplier = 1.8f,
                        inclusionOverride = "Cristaux octaédriques nets, couleur rouge vif associée aux gisements de la région de Mogok." },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 40, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Grenat_Almandin", g =>
            {
                g.displayName = "Grenat Almandin";
                g.description = "Un grenat rouge sombre, à ne pas confondre avec un rubis ou un spinelle.";
                g.icon = GetGemIconSprite("grenat", new Color(0.45f, 0.10f, 0.12f), new Color(0.65f, 0.25f, 0.20f));
                g.refractiveIndex = 1.815f;
                g.density = 4.05f;
                g.hardness = 7.5f;
                g.lengthMm = 8.0f;
                g.widthMm = 5.2f;
                g.depthMm = 3.6f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux";
                g.color = "Rouge sombre / brun-rouge";
                g.shape = "Poire";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Larges bandes d'absorption du fer.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 573f, widthNm = 18f },
                    new SpectrumLine { wavelengthNm = 526f, widthNm = 18f },
                    new SpectrumLine { wavelengthNm = 505f, widthNm = 16f },
                };
                g.inclusions = "Cristaux d'apatite ou de zircon entourés de halos, fissures internes fréquentes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction rouge notable.";
                g.baseValue = 25;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Inde", rarityWeight = 40, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis (Idaho)", rarityWeight = 25, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Grenat_Pyrope", g =>
            {
                g.displayName = "Grenat Pyrope";
                g.description = "Un grenat rouge profond, plus léger et de plus faible IR que l'almandin.";
                g.icon = GetGemIconSprite("pyrope", new Color(0.55f, 0.08f, 0.10f), new Color(0.75f, 0.25f, 0.22f));
                g.refractiveIndex = 1.74f;
                g.density = 3.78f;
                g.hardness = 7.25f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.7f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Rouge profond";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Bandes d'absorption du fer et du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 512f, widthNm = 30f },
                };
                g.inclusions = "Parfois des cristaux de rutile en aiguilles, généralement peu abondants.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction rouge notable.";
                g.baseValue = 30;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afrique du Sud", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "République Tchèque (Bohême)", rarityWeight = 30, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement historique d'Europe centrale, exploité depuis des siècles pour la joaillerie." },
                    new GemOrigin { name = "États-Unis (Arizona)", rarityWeight = 30, valueMultiplier = 0.9f,
                        inclusionOverride = "Petits cristaux alluviaux de couleur intense, souvent trouvés en petite taille près de la surface." },
                };
            }),
            BuildGem("Grenat_Tsavorite", g =>
            {
                g.displayName = "Grenat Tsavorite";
                g.description = "Un grenat vert vif, rare et recherché, à ne pas confondre avec l'émeraude ou le péridot.";
                g.icon = GetGemIconSprite("tsavorite", new Color(0.12f, 0.55f, 0.25f), new Color(0.35f, 0.78f, 0.45f));
                g.refractiveIndex = 1.74f;
                g.density = 3.61f;
                g.hardness = 7.25f;
                g.lengthMm = 6.0f;
                g.widthMm = 5.0f;
                g.depthMm = 3.3f;
                g.weightCarats = 1.0f;
                g.luster = "Vitreux";
                g.color = "Vert vif";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Raies du vanadium et du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 630f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 690f, widthNm = 8f },
                };
                g.inclusions = "Fibres d'amphibole, inclusions caractéristiques des gisements est-africains.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Réaction rougeâtre possible si coloré au chrome.";
                g.baseValue = 120;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Kenya", rarityWeight = 40, valueMultiplier = 1.5f,
                        inclusionOverride = "Gisement historique d'Afrique de l'Est, source majeure pour cette teinte de vert." },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 40, valueMultiplier = 1.3f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 20, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Grenat_Hessonite", g =>
            {
                g.displayName = "Grenat Hessonite";
                g.description = "Un grenat brun-orangé (\"cinnamon stone\"), à ne pas confondre avec la spessartite.";
                g.icon = GetGemIconSprite("hessonite", new Color(0.70f, 0.42f, 0.15f), new Color(0.85f, 0.60f, 0.35f));
                g.refractiveIndex = 1.74f;
                g.density = 3.68f;
                g.hardness = 6.75f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.7f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux à résineux";
                g.color = "Brun-orangé (cannelle)";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Bandes d'absorption du fer et du manganèse.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 420f, widthNm = 40f },
                };
                g.inclusions = "Aspect interne \"mélasse\" (treacle) caractéristique, tourbillons de croissance visibles au microscope.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.baseValue = 40;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 50, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement historique, extraction alluviale traditionnelle." },
                    new GemOrigin { name = "Canada", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Verre", g =>
            {
                g.displayName = "Verre";
                g.description = "Un verre imitant l'apparence d'une pierre précieuse rouge.";
                g.icon = GetGemIconSprite("verre", new Color(0.75f, 0.80f, 0.82f), new Color(0.90f, 0.93f, 0.95f));
                g.refractiveIndex = 1.50f;
                g.density = 2.50f;
                g.hardness = 5.5f;
                g.lengthMm = 6.8f;
                g.widthMm = 6.8f;
                g.depthMm = 4.4f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Rouge";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence variable ou absente, souvent avec des bulles visibles à la loupe.";
                g.fluorescenceShortWave = "Fluorescence variable ou absente sous UV courte onde.";
                g.opticalCharacter = "Isotrope (matériau amorphe, non cristallin).";
                g.absorptionSpectrum = "Spectre continu sans raies nettes, ou raies artificielles selon le colorant.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Bulles rondes et stries d'écoulement, signature typique du verre.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau amorphe).";
                g.chelseaFilterReaction = "Réaction variable et non diagnostique.";
                g.baseValue = 5;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Peridot", g =>
            {
                g.displayName = "Péridot";
                g.description = "Un péridot vert olive, envoyé pour confirmation d'espèce.";
                g.icon = GetGemIconSprite("peridot", new Color(0.45f, 0.65f, 0.25f), new Color(0.65f, 0.85f, 0.40f));
                g.refractiveIndex = 1.67f;
                g.density = 3.34f;
                g.hardness = 6.5f;
                g.lengthMm = 8.5f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux, légèrement gras";
                g.color = "Vert olive";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune-vert / Vert selon l'axe d'observation (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.absorptionSpectrum = "Triplet d'absorption du fer dans le bleu.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 453f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 473f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 493f, widthNm = 10f },
                };
                g.inclusions = "Petites fractures discoïdes en forme de disque autour de cristaux inclus.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.baseValue = 35;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Pakistan (vallée de Kashmir/Sopat)", rarityWeight = 20, valueMultiplier = 1.6f,
                        inclusionOverride = "Cristaux de belle taille et couleur intense, gisement réputé de la vallée de Kashmir." },
                    new GemOrigin { name = "États-Unis (Arizona)", rarityWeight = 40, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Myanmar", rarityWeight = 40, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Spinelle_Bleu", g =>
            {
                g.displayName = "Spinelle Bleu";
                g.description = "Une pierre bleu vif, à distinguer du saphir et du zircon bleu.";
                g.icon = GetGemIconSprite("spinellebleu", new Color(0.15f, 0.40f, 0.80f), new Color(0.45f, 0.65f, 0.95f));
                g.refractiveIndex = 1.715f;
                g.density = 3.61f;
                g.hardness = 8f;
                g.lengthMm = 6.3f;
                g.widthMm = 5.5f;
                g.depthMm = 3.8f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux";
                g.color = "Bleu";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte, parfois faible réaction rougeâtre en présence de traces de chrome.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Bandes d'absorption du cobalt caractéristiques (trois bandes dans le rouge/orangé) si coloré au cobalt.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 460f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 580f, widthNm = 40f },
                    new SpectrumLine { wavelengthNm = 635f, widthNm = 20f },
                };
                g.inclusions = "Cristaux octaédriques, cristaux négatifs.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Réaction rouge marquée sous filtre Chelsea si coloré au cobalt.";
                g.baseValue = 90;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 40, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Vietnam", rarityWeight = 30, valueMultiplier = 1.3f,
                        inclusionOverride = "Coloration au cobalt, gisement réputé pour cette teinte bleue intense." },
                };
            }),
            BuildGem("Saphir_Bleu", g =>
            {
                g.displayName = "Saphir Bleu";
                g.description = "Un corindon bleu intense, envoyé pour confirmer s'il s'agit d'un saphir naturel.";
                g.icon = GetGemIconSprite("saphirbleu", new Color(0.08f, 0.18f, 0.55f), new Color(0.30f, 0.42f, 0.78f));
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 7.2f;
                g.widthMm = 5.6f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Bleu";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu / Bleu-vert selon l'axe d'observation (marqué).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde (le fer inhibe la fluorescence).";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.absorptionSpectrum = "Triplet d'absorption du fer vers 450 nm.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 450f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 460f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 470f, widthNm = 6f },
                };
                g.inclusions = "Soie de rutile, zonation de couleur en bandes hexagonales.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction rouge.";
                g.baseValue = 140;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Cachemire (Kashmir)", rarityWeight = 5, valueMultiplier = 4.0f,
                        inclusionOverride = "Couleur bleu velouté intense caractéristique ; gisement de Kashmir aujourd'hui quasiment épuisé, extrêmement recherché." },
                    new GemOrigin { name = "Sri Lanka (Ceylan)", rarityWeight = 35, valueMultiplier = 1.4f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Australie", rarityWeight = 25, valueMultiplier = 0.7f,
                        inclusionOverride = "Zonation de couleur marquée, teinte souvent trop sombre à contre-jour." },
                };
            }),
            BuildGem("Zircon", g =>
            {
                g.displayName = "Zircon";
                g.description = "Un zircon bleu, souvent confondu avec le saphir ou le spinelle bleu.";
                g.icon = GetGemIconSprite("zircon", new Color(0.20f, 0.55f, 0.75f), new Color(0.45f, 0.78f, 0.92f));
                g.refractiveIndex = 1.95f;
                g.density = 4.65f;
                g.hardness = 7.5f;
                g.lengthMm = 5.8f;
                g.widthMm = 5.8f;
                g.depthMm = 4.5f;
                g.weightCarats = 1.6f;
                g.luster = "Adamantin à sub-adamantin";
                g.color = "Bleu";
                g.shape = "Carré";
                g.cutStyle = "Princesse";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Dichroïsme faible à modéré selon la teinte.";
                g.fluorescenceLongWave = "Fluorescence jaunâtre faible possible selon l'origine.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial — biréfringence très élevée provoquant un dédoublement visible des arêtes des facettes arrière.";
                g.absorptionSpectrum = "Nombreuses raies fines d'absorption, liées à des traces d'éléments radioactifs.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 653f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 620f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 590f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 560f, widthNm = 8f },
                };
                g.inclusions = "Généralement propre ; dédoublement net des facettes du pavillon visible à la loupe.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.baseValue = 30;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Cambodge (Ratanakiri)", rarityWeight = 60, valueMultiplier = 1.0f,
                        inclusionOverride = "Gisement alluvial classique pour cette teinte obtenue par traitement thermique, généralement propre à l'œil nu." },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 40, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Diamant", g =>
            {
                g.displayName = "Diamant";
                g.description = "Une pierre incolore à l'éclat exceptionnel, envoyée pour authentification.";
                g.icon = GetGemIconSprite("diamant", new Color(0.92f, 0.94f, 0.97f), new Color(0.99f, 0.99f, 1.00f));
                g.refractiveIndex = 2.417f;
                g.density = 3.52f;
                g.hardness = 10f;
                g.lengthMm = 5.5f;
                g.widthMm = 5.5f;
                g.depthMm = 3.4f;
                g.weightCarats = 0.9f;
                g.luster = "Adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence bleue fréquente sous UV longue onde (variable selon les spécimens).";
                g.fluorescenceShortWave = "Fluorescence plus faible sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Spectre variable selon le type ; raies dans le violet possibles (série Cape).";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 415f, widthNm = 10f },
                };
                g.inclusions = "Inclusions cristallines sombres possibles, généralement peu abondantes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.baseValue = 500;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Botswana", rarityWeight = 35, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Russie", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Canada", rarityWeight = 20, valueMultiplier = 1.3f,
                        inclusionOverride = "Extraction réputée pour sa traçabilité éthique et sa haute pureté." },
                    new GemOrigin { name = "Afrique du Sud", rarityWeight = 15, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Amethyste", g =>
            {
                g.displayName = "Améthyste";
                g.description = "Un quartz violet, variété courante mais toujours appréciée.";
                g.icon = GetGemIconSprite("amethyste", new Color(0.45f, 0.25f, 0.65f), new Color(0.68f, 0.48f, 0.85f));
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 9.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.7f;
                g.luster = "Vitreux";
                g.color = "Violet";
                g.shape = "Poire";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Violet / Rougeâtre-violet selon l'axe d'observation (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en bandes parallèles.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.baseValue = 20;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 45, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Uruguay", rarityWeight = 25, valueMultiplier = 1.2f,
                        inclusionOverride = "Couleur violette profonde et uniforme, très recherchée." },
                    new GemOrigin { name = "Zambie", rarityWeight = 30, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Grenat_Spessartite", g =>
            {
                g.displayName = "Grenat Spessartite";
                g.description = "Un grenat orangé vif, à ne pas confondre avec l'almandin rouge.";
                g.icon = GetGemIconSprite("spessartite", new Color(0.85f, 0.45f, 0.15f), new Color(0.95f, 0.65f, 0.35f));
                g.refractiveIndex = 1.80f;
                g.density = 4.15f;
                g.hardness = 7.5f;
                g.lengthMm = 9.5f;
                g.widthMm = 5.0f;
                g.depthMm = 3.3f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux à résineux";
                g.color = "Orange";
                g.shape = "Marquise";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.absorptionSpectrum = "Bandes d'absorption du manganèse.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 412f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 460f, widthNm = 15f },
                    new SpectrumLine { wavelengthNm = 485f, widthNm = 15f },
                };
                g.inclusions = "Fissures internes possibles, généralement peu abondantes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction rouge notable.";
                g.baseValue = 55;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Namibie", rarityWeight = 40, valueMultiplier = 1.3f,
                        inclusionOverride = "Teinte orangée vive et intense, très recherchée sur le marché." },
                    new GemOrigin { name = "Nigeria", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Emeraude", g =>
            {
                g.displayName = "Émeraude";
                g.description = "Un béryl vert intense, envoyé pour authentification.";
                g.icon = GetGemIconSprite("emeraude", new Color(0.10f, 0.45f, 0.30f), new Color(0.30f, 0.65f, 0.45f));
                g.refractiveIndex = 1.580f;
                g.density = 2.72f;
                g.hardness = 7.7f;
                g.lengthMm = 8.2f;
                g.widthMm = 6.2f;
                g.depthMm = 4.5f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Vert";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude (à degrés)";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu-vert / Jaune-vert selon l'axe d'observation (marqué).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.absorptionSpectrum = "Doublet d'absorption du chrome dans le rouge.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 683f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 662f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 637f, widthNm = 8f },
                };
                g.inclusions = "Inclusions en \"jardin\" (fissures et inclusions multiphases), très caractéristiques.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Réaction faible ou nulle (contrairement à de nombreuses imitations et synthèses, qui réagissent rouge vif).";
                g.baseValue = 180;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Colombie (Muzo/Chivor)", rarityWeight = 25, valueMultiplier = 2.5f,
                        inclusionOverride = "Inclusions triphasées (liquide, gaz, cristal de halite) en \"jardin\"." },
                    new GemOrigin { name = "Zambie", rarityWeight = 35, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Brésil", rarityWeight = 25, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Afghanistan (Panjshir)", rarityWeight = 15, valueMultiplier = 1.5f },
                };
            }),
        };
    }

    private static GemDefinition BuildGem(string assetName, Action<GemDefinition> configure)
    {
        var path = $"Assets/Data/Gems/{assetName}.asset";
        var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
        var isNew = gem == null;
        if (isNew) gem = ScriptableObject.CreateInstance<GemDefinition>();

        configure(gem);

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
            case "Polariscope": return GetOrCreatePixelSprite("polariscope_pixel", PolariscopePattern, PolariscopePalette);
            case "Spectroscope": return GetOrCreatePixelSprite("spectroscope_pixel", DichroscopePattern, SpectroscopePalette);
            case "Microscope": return GetOrCreatePixelSprite("microscope_pixel", MicroscopePattern, MicroscopePalette);
            case "Conoscope": return GetOrCreatePixelSprite("conoscope_pixel", DichroscopePattern, ConoscopePalette);
            case "UVLongues": return GetOrCreatePixelSprite("uvlongues_pixel", LampeUVPattern, LampeUVPalette);
            case "UVCourtes": return GetOrCreatePixelSprite("uvcourtes_pixel", LampeUVPattern, UVCourtesPalette);
            case "FiltreChelsea": return GetOrCreatePixelSprite("chelsea_pixel", LoupePattern, ChelseaPalette);
            case "Micrometre": return GetOrCreatePixelSprite("micrometre_pixel", MicrometrePattern, MicrometrePalette);
            case "BalanceCarat": return GetOrCreatePixelSprite("balancecarat_pixel", BalanceCaratPattern, BalanceCaratPalette);
            default: return GetOrCreatePixelSprite("loupe_pixel", LoupePattern, LoupePalette);
        }
    }

    private static Color GrassPixel(int x, int y)
    {
        var grassA = new Color(0.29f, 0.47f, 0.27f);
        var grassB = new Color(0.26f, 0.43f, 0.24f);
        var speck = new Color(0.34f, 0.52f, 0.30f);

        var noise = (x * 7 + y * 13) % 17;
        if (noise == 0) return speck;
        return (x / 4 + y / 4) % 2 == 0 ? grassA : grassB;
    }

    private static Color HedgePixel(int x, int y)
    {
        var leafA = new Color(0.16f, 0.32f, 0.16f);
        var leafB = new Color(0.13f, 0.27f, 0.13f);
        var noise = (x * 5 + y * 11) % 13;
        return noise < 3 ? leafB : leafA;
    }

    private static Sprite GetBuildingSprite(string key, Color roof, Color wall)
    {
        var palette = new Dictionary<char, Color>
        {
            { 'R', roof },
            { 'W', wall },
            { 'N', new Color(0.25f, 0.35f, 0.55f) },
            { 'O', new Color(0.36f, 0.24f, 0.14f) },
            { 'F', new Color(0.20f, 0.20f, 0.22f) },
        };
        return GetOrCreatePixelSprite($"building_{key}_pixel", BuildingPattern, palette);
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

    private static readonly string[] BuildingPattern =
    {
        Row(16, '.'),
        Row(7, '.', 2, 'R', 7, '.'),
        Row(6, '.', 4, 'R', 6, '.'),
        Row(5, '.', 6, 'R', 5, '.'),
        Row(4, '.', 8, 'R', 4, '.'),
        Row(3, '.', 10, 'R', 3, '.'),
        Row(2, '.', 12, 'W', 2, '.'),
        Row(2, '.', 12, 'W', 2, '.'),
        Row(2, '.', 5, 'W', 2, 'N', 5, 'W', 2, '.'),
        Row(2, '.', 5, 'W', 2, 'N', 5, 'W', 2, '.'),
        Row(2, '.', 12, 'W', 2, '.'),
        Row(2, '.', 4, 'W', 4, 'O', 4, 'W', 2, '.'),
        Row(2, '.', 4, 'W', 4, 'O', 4, 'W', 2, '.'),
        Row(2, '.', 4, 'W', 4, 'O', 4, 'W', 2, '.'),
        Row(1, '.', 14, 'F', 1, '.'),
        Row(16, '.'),
    };

    private static readonly string[] TunnelPattern =
    {
        Row(16, '.'),
        Row(5, '.', 6, 'C', 5, '.'),
        Row(4, '.', 8, 'C', 4, '.'),
        Row(3, '.', 10, 'C', 3, '.'),
        Row(2, '.', 12, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 3, 'C', 6, 'K', 3, 'C', 2, '.'),
        Row(2, '.', 12, 'C', 2, '.'),
        Row(1, '.', 14, 'C', 1, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> TunnelPalette = new Dictionary<char, Color>
    {
        { 'C', new Color(0.30f, 0.26f, 0.22f) },
        { 'K', new Color(0.03f, 0.03f, 0.04f) },
    };

    private static readonly string[] DoorPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(4, '.', 8, 'O', 4, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 4, 'O', 2, 'K', 4, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(3, '.', 10, 'O', 3, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> DoorPalette = new Dictionary<char, Color>
    {
        { 'O', new Color(0.42f, 0.28f, 0.16f) },
        { 'K', new Color(0.85f, 0.72f, 0.25f) },
    };

    private static readonly string[] PolariscopePattern =
    {
        Row(16, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(3, '.', 10, 'D', 3, '.'),
        Row(2, '.', 12, 'D', 2, '.'),
        Row(2, '.', 5, 'D', 2, 'X', 5, 'D', 2, '.'),
        Row(2, '.', 12, 'D', 2, '.'),
        Row(2, '.', 12, 'D', 2, '.'),
        Row(3, '.', 10, 'D', 3, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(7, '.', 2, 'S', 7, '.'),
        Row(6, '.', 4, 'S', 6, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> PolariscopePalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.22f, 0.22f, 0.26f) },
        { 'X', new Color(0.42f, 0.42f, 0.48f) },
        { 'S', new Color(0.33f, 0.33f, 0.35f) },
    };

    private static readonly Dictionary<char, Color> SpectroscopePalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.22f, 0.28f, 0.22f) },
        { 'L', new Color(0.85f, 0.35f, 0.30f) },
    };

    private static readonly Dictionary<char, Color> ConoscopePalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.55f, 0.36f, 0.18f) },
        { 'L', new Color(0.90f, 0.75f, 0.45f) },
    };

    private static readonly string[] MicroscopePattern =
    {
        Row(16, '.'),
        Row(7, '.', 2, 'E', 7, '.'),
        Row(7, '.', 2, 'E', 7, '.'),
        Row(6, '.', 4, 'E', 6, '.'),
        Row(6, '.', 4, 'B', 6, '.'),
        Row(6, '.', 4, 'B', 6, '.'),
        Row(5, '.', 6, 'B', 5, '.'),
        Row(5, '.', 6, 'B', 5, '.'),
        Row(4, '.', 8, 'B', 4, '.'),
        Row(3, '.', 10, 'K', 3, '.'),
        Row(3, '.', 10, 'K', 3, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MicroscopePalette = new Dictionary<char, Color>
    {
        { 'E', new Color(0.65f, 0.68f, 0.72f) },
        { 'B', new Color(0.18f, 0.32f, 0.24f) },
        { 'K', new Color(0.10f, 0.10f, 0.10f) },
    };

    private static readonly Dictionary<char, Color> UVCourtesPalette = new Dictionary<char, Color>
    {
        { 'U', new Color(0.29f, 0.24f, 0.88f) },
        { 'u', new Color(0.58f, 0.50f, 1.00f) },
        { 'B', new Color(0.23f, 0.23f, 0.23f) },
    };

    private static readonly Dictionary<char, Color> ChelseaPalette = new Dictionary<char, Color>
    {
        { 'R', new Color(0.30f, 0.30f, 0.33f) },
        { 'L', new Color(0.75f, 0.28f, 0.35f) },
        { 'H', new Color(0.42f, 0.29f, 0.18f) },
    };

    private static readonly string[] MicrometrePattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(3, '.', 10, 'B', 3, '.'),
        Row(3, '.', 10, 'B', 3, '.'),
        Row(3, '.', 1, 'J', 8, '.', 1, 'J', 3, '.'),
        Row(3, '.', 1, 'J', 8, '.', 1, 'J', 3, '.'),
        Row(3, '.', 1, 'J', 8, '.', 1, 'J', 3, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MicrometrePalette = new Dictionary<char, Color>
    {
        { 'B', new Color(0.55f, 0.55f, 0.58f) },
        { 'J', new Color(0.35f, 0.35f, 0.38f) },
    };

    private static readonly string[] BalanceCaratPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(3, '.', 10, 'P', 3, '.'),
        Row(2, '.', 12, 'P', 2, '.'),
        Row(2, '.', 12, 'P', 2, '.'),
        Row(3, '.', 10, 'K', 3, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> BalanceCaratPalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.15f, 0.35f, 0.20f) },
        { 'P', new Color(0.62f, 0.64f, 0.68f) },
        { 'K', new Color(0.20f, 0.20f, 0.22f) },
    };

    private static readonly string[] GemIconPattern =
    {
        Row(16, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(1, '.', 14, 'G', 1, '.'),
        Row(1, '.', 6, 'G', 2, 'F', 6, 'G', 1, '.'),
        Row(1, '.', 14, 'G', 1, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static Sprite GetGemIconSprite(string key, Color gemColor, Color facetColor)
    {
        var palette = new Dictionary<char, Color> { { 'G', gemColor }, { 'F', facetColor } };
        return GetOrCreatePixelSprite($"gemicon_{key}_pixel", GemIconPattern, palette);
    }

    private static readonly string[] ChestPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(2, '.', 12, 'L', 2, '.'),
        Row(2, '.', 12, 'L', 2, '.'),
        Row(1, '.', 14, 'L', 1, '.'),
        Row(1, '.', 14, 'N', 1, '.'),
        Row(1, '.', 6, 'B', 2, 'K', 6, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(2, '.', 12, 'B', 2, '.'),
        Row(3, '.', 10, 'B', 3, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> ChestPalette = new Dictionary<char, Color>
    {
        { 'L', new Color(0.45f, 0.30f, 0.16f) },
        { 'N', new Color(0.25f, 0.16f, 0.08f) },
        { 'B', new Color(0.38f, 0.25f, 0.13f) },
        { 'K', new Color(0.85f, 0.72f, 0.25f) },
    };
}
