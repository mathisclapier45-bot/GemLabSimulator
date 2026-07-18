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

    [MenuItem("GemLab/Setup Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var titlePanelGO = new GameObject("TitleScreenPanel", typeof(RectTransform));
        titlePanelGO.transform.SetParent(canvasGO.transform, false);
        var titlePanelRect = titlePanelGO.GetComponent<RectTransform>();
        titlePanelRect.anchorMin = Vector2.zero;
        titlePanelRect.anchorMax = Vector2.one;
        titlePanelRect.sizeDelta = Vector2.zero;
        titlePanelRect.anchoredPosition = Vector2.zero;

        var backgroundGemsGO = new GameObject("BackgroundGems", typeof(RectTransform));
        backgroundGemsGO.transform.SetParent(titlePanelGO.transform, false);
        var backgroundGemsRect = backgroundGemsGO.GetComponent<RectTransform>();
        backgroundGemsRect.anchorMin = Vector2.zero;
        backgroundGemsRect.anchorMax = Vector2.one;
        backgroundGemsRect.sizeDelta = Vector2.zero;
        backgroundGemsRect.anchoredPosition = Vector2.zero;
        var backgroundGems = backgroundGemsGO.AddComponent<MenuBackgroundGems>();
        var backgroundGemsSo = new SerializedObject(backgroundGems);
        backgroundGemsSo.FindProperty("container").objectReferenceValue = backgroundGemsRect;
        backgroundGemsSo.ApplyModifiedProperties();

        CreateUIText("Title", titlePanelGO.transform, "GemLab Simulator",
            new Vector2(0, -180), new Vector2(700, 70), 48, TextAnchor.UpperCenter, Color.white);
        CreateUIText("Tagline", titlePanelGO.transform, "Un simulateur de gemmologie",
            new Vector2(0, -250), new Vector2(700, 30), 18, TextAnchor.UpperCenter, new Color(0.75f, 0.75f, 0.75f));

        var playButtonGO = CreateUIButton("PlayButton", titlePanelGO.transform, "Jouer",
            new Vector2(0, -340), new Vector2(220, 50), 20);

        var slotsPanelGO = new GameObject("SlotsPanel", typeof(RectTransform));
        slotsPanelGO.transform.SetParent(canvasGO.transform, false);
        var slotsPanelRect = slotsPanelGO.GetComponent<RectTransform>();
        slotsPanelRect.anchorMin = Vector2.zero;
        slotsPanelRect.anchorMax = Vector2.one;
        slotsPanelRect.sizeDelta = Vector2.zero;
        slotsPanelRect.anchoredPosition = Vector2.zero;
        slotsPanelGO.SetActive(false);

        CreateUIText("Title", slotsPanelGO.transform, "GemLab Simulator",
            new Vector2(0, -60), new Vector2(700, 60), 40, TextAnchor.UpperCenter, Color.white);
        CreateUIText("Subtitle", slotsPanelGO.transform, "Choisissez une sauvegarde",
            new Vector2(0, -120), new Vector2(700, 30), 18, TextAnchor.UpperCenter, new Color(0.75f, 0.75f, 0.75f));

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(slotsPanelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotsRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, 180);
        slotsRect.sizeDelta = new Vector2(560, 450);

        var menuUI = canvasGO.AddComponent<MainMenuUI>();
        var menuSo = new SerializedObject(menuUI);
        menuSo.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        menuSo.ApplyModifiedProperties();

        var titleScreen = playButtonGO.AddComponent<TitleScreenController>();
        var titleScreenSo = new SerializedObject(titleScreen);
        titleScreenSo.FindProperty("titlePanel").objectReferenceValue = titlePanelGO;
        titleScreenSo.FindProperty("slotsPanel").objectReferenceValue = slotsPanelGO;
        titleScreenSo.ApplyModifiedProperties();

        CreateMusicManager();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        AddSceneToBuildSettings("Assets/Scenes/MainMenu.unity", true);

        AssetDatabase.SaveAssets();
        Debug.Log("Main menu scene created at Assets/Scenes/MainMenu.unity");
    }

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

        var playerSprite = GetPlayerSprite();

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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, RoomMinY + 1, 0f), "SpawnPoint_Home");
        CreateChest(new Vector3(3, 2, 0f));
        CreateBed(new Vector3(-3, 2, 0f));

        var mailboxSprite = GetOrCreatePixelSprite("mailbox_pixel", MailboxPattern, MailboxPalette);
        var mailbox = new GameObject("Mailbox");
        var mailboxSr = mailbox.AddComponent<SpriteRenderer>();
        mailboxSr.sprite = mailboxSprite;
        mailboxSr.sortingOrder = 4;
        var mailboxCol = mailbox.AddComponent<BoxCollider2D>();
        mailboxCol.isTrigger = true;
        mailboxCol.size = new Vector2(1.2f, 1.2f);
        mailbox.AddComponent<Mailbox>();
        mailbox.transform.position = new Vector3(DividerX - 2, RoomMinY + 1, 0f);

        var gemPool = GetOrCreateGemPool();
        UpdateGemDatabase(gemPool);

        CreateExaminationStation("Loupe", ExaminationTool.MeasurementType.VisualExamination, new Vector3(9, 2, 0f));
        CreateExaminationStation("Refractometre", ExaminationTool.MeasurementType.RefractiveIndex, new Vector3(11.5f, 2, 0f));
        CreateExaminationStation("BalanceHydrostatique", ExaminationTool.MeasurementType.Density, new Vector3(14, 2, 0f));
        CreateExaminationStation("Dichroscope", ExaminationTool.MeasurementType.Pleochroism, new Vector3(16.5f, 2, 0f));
        CreateExaminationStation("Polariscope", ExaminationTool.MeasurementType.OpticalCharacter, new Vector3(19, 2, 0f));
        CreateExaminationStation("Spectroscope", ExaminationTool.MeasurementType.AbsorptionSpectrum, new Vector3(21.5f, 2, 0f));
        CreateExaminationStation("Microscope", ExaminationTool.MeasurementType.Inclusions, new Vector3(24, 2, 0f));

        CreateExaminationStation("Conoscope", ExaminationTool.MeasurementType.InterferenceFigure, new Vector3(9, -2, 0f));
        CreateExaminationStation("UVLongues", ExaminationTool.MeasurementType.FluorescenceLongWave, new Vector3(11.5f, -2, 0f));
        CreateExaminationStation("UVCourtes", ExaminationTool.MeasurementType.FluorescenceShortWave, new Vector3(14, -2, 0f));
        CreateExaminationStation("FiltreChelsea", ExaminationTool.MeasurementType.ChelseaFilter, new Vector3(16.5f, -2, 0f));
        CreateExaminationStation("Micrometre", ExaminationTool.MeasurementType.Dimensions, new Vector3(19, -2, 0f));
        CreateExaminationStation("BalanceCarat", ExaminationTool.MeasurementType.Weight, new Vector3(21.5f, -2, 0f));
        CreateExaminationStation("Aimant", ExaminationTool.MeasurementType.Magnetism, new Vector3(24, -2, 0f));

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
        pixelPerfect.refResolutionX = 576;
        pixelPerfect.refResolutionY = 324;
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

        var grassTile = GetOrCreateTileFromSprite("grass_pack", LoadPackSprite(PackTerrains + "Grass_1_9.png"));
        var hedgeTile = GetOrCreateTile("hedge_pixel", HedgePixel);
        var pathTile = GetOrCreateTileFromSprite("path_pack", LoadPackSprite(PackTerrains + "Grass_1_21.png"));

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

        void PaintPathRect(int minX, int maxX, int minY, int maxY)
        {
            for (var x = minX; x <= maxX; x++)
                for (var y = minY; y <= maxY; y++)
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), pathTile);
        }

        PaintPathRect(-18, 18, 4, 5);
        PaintPathRect(-14, 14, -3, -2);
        PaintPathRect(-1, 0, -10, 5);
        PaintPathRect(-17, -16, 3, 5);

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(-16, 4, 0f);

        var playerSprite = GetPlayerSprite();

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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateBuilding("Maison & Bureau de Gemmologie", new Vector3(-16, 8, 0f),
            new Color(0.65f, 0.22f, 0.18f), new Color(0.85f, 0.78f, 0.62f), "Home", true);
        CreateBuilding("Laboratoire de Gemmologie", new Vector3(-8, 8, 0f),
            new Color(0.30f, 0.45f, 0.55f), new Color(0.75f, 0.78f, 0.80f), "Laboratoire", true);
        CreateBuilding("Atelier Lapidaire", new Vector3(0, 8, 0f),
            new Color(0.45f, 0.40f, 0.35f), new Color(0.65f, 0.60f, 0.55f), "", false);
        CreateBuilding("Musee", new Vector3(8, 8, 0f),
            new Color(0.75f, 0.72f, 0.60f), new Color(0.92f, 0.90f, 0.85f), "Musee", true);
        CreateBuilding("Quartier des Diamantaires", new Vector3(16, 8, 0f),
            new Color(0.25f, 0.30f, 0.55f), new Color(0.70f, 0.75f, 0.90f), "", false);
        CreateBuilding("Marche aux Pierres", new Vector3(-12, -6, 0f),
            new Color(0.65f, 0.42f, 0.18f), new Color(0.88f, 0.70f, 0.45f), "Marche", true);
        CreateBuilding("Ecole", new Vector3(-4, -6, 0f),
            new Color(0.30f, 0.45f, 0.65f), new Color(0.90f, 0.85f, 0.70f), "", false);
        CreateBuilding("Marche aux Brutes", new Vector3(4, -6, 0f),
            new Color(0.50f, 0.35f, 0.22f), new Color(0.75f, 0.65f, 0.50f), "MarcheBrutes", true);

        CreateBuilding("Le Manoir", new Vector3(12, -6, 0f),
            new Color(0.35f, 0.18f, 0.42f), new Color(0.70f, 0.60f, 0.75f), "Manoir", true);
        CreateTunnelEntrance(new Vector3(0, -11, 0f));

        CreateNamedSpawnPoint("SpawnPoint_Home", new Vector3(-16, 5, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Laboratoire", new Vector3(-8, 5, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Musee", new Vector3(8, 5, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Marche", new Vector3(-12, -4, 0f));
        CreateNamedSpawnPoint("SpawnPoint_MarcheBrutes", new Vector3(4, -4, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Manoir", new Vector3(12, -4, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Tunnel", new Vector3(0, -9, 0f));

        var bushVariant = 0;
        foreach (var pos in new[]
                 {
                     new Vector3(-19, 11, 0f), new Vector3(-12, 11, 0f), new Vector3(-4, 11, 0f),
                     new Vector3(4, 11, 0f), new Vector3(12, 11, 0f), new Vector3(19, 11, 0f),
                     new Vector3(-16, -9, 0f), new Vector3(-8, -9, 0f), new Vector3(8, -9, 0f), new Vector3(16, -9, 0f),
                     new Vector3(-19, 9, 0f), new Vector3(19, 9, 0f), new Vector3(-19, -9, 0f), new Vector3(19, -9, 0f),
                 })
        {
            CreateTree(pos, bushVariant);
            bushVariant++;
        }

        foreach (var pos in new[]
                 {
                     new Vector3(-18, 6, 0f), new Vector3(-17, 6, 0f), new Vector3(-16, 6, 0f),
                     new Vector3(-15, 6, 0f), new Vector3(-14, 6, 0f),
                 })
            CreateFence(pos);

        foreach (var pos in new[]
                 {
                     new Vector3(-12, 6, 0f), new Vector3(-4, 6, 0f), new Vector3(4, 6, 0f), new Vector3(12, 6, 0f),
                     new Vector3(-8, -4, 0f), new Vector3(8, -4, 0f),
                 })
            CreateLamppost(pos);

        var flowerVariant = 0;
        foreach (var pos in new[]
                 {
                     new Vector3(-17.5f, 6.5f, 0f), new Vector3(-9.5f, 6.5f, 0f), new Vector3(2.5f, 6.5f, 0f),
                     new Vector3(10.5f, 6.5f, 0f), new Vector3(17.5f, 6.5f, 0f),
                     new Vector3(-13.5f, -4.5f, 0f), new Vector3(-5.5f, -4.5f, 0f),
                     new Vector3(5.5f, -4.5f, 0f), new Vector3(13.5f, -4.5f, 0f),
                 })
        {
            CreateFlowerCluster(pos, flowerVariant);
            flowerVariant++;
        }

        CreateUI(false);

        var townCanvas = GameObject.Find("Canvas");
        if (townCanvas != null)
        {
            var overlayGO = new GameObject("DayNightOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(townCanvas.transform, false);
            overlayGO.transform.SetSiblingIndex(0);
            var overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            var overlayImage = overlayGO.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = false;

            var dayNight = overlayGO.AddComponent<DayNightOverlay>();
            var dayNightSo = new SerializedObject(dayNight);
            dayNightSo.FindProperty("overlayImage").objectReferenceValue = overlayImage;
            dayNightSo.ApplyModifiedProperties();
        }

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Town.unity");
        AddSceneToBuildSettings("Assets/Scenes/Town.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Town scene created at Assets/Scenes/Town.unity");
    }

    private const int LabMinX = -10;
    private const int LabMaxX = 10;
    private const int LabMinY = -6;
    private const int LabMaxY = 6;

    private const int MarketMinX = -14;
    private const int MarketMaxX = 14;
    private const int MarketMinY = -6;
    private const int MarketMaxY = 6;

    private const int RoughMarketMinX = -14;
    private const int RoughMarketMaxX = 14;
    private const int RoughMarketMinY = -6;
    private const int RoughMarketMaxY = 6;

    private const int MuseumMinX = -14;
    private const int MuseumMaxX = 14;
    private const int MuseumMinY = -6;
    private const int MuseumMaxY = 6;

    private const int TunnelMinX = -2;
    private const int TunnelMaxX = 2;
    private const int TunnelMinY = -16;
    private const int TunnelMaxY = 16;

    private const int ManoirMinX = -10;
    private const int ManoirMaxX = 10;
    private const int ManoirMinY = -6;
    private const int ManoirMaxY = 6;

    [MenuItem("GemLab/Setup Laboratory Scene")]
    public static void CreateLaboratoryScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.10f, 0.14f);
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

        var floorTile = GetOrCreateTile("labfloor_pixel", LabFloorPixel);
        var wallTile = GetOrCreateTile("wall_pixel", WallPixel);

        for (var x = LabMinX; x <= LabMaxX; x++)
        {
            for (var y = LabMinY; y <= LabMaxY; y++)
            {
                var isPerimeter = x == LabMinX || x == LabMaxX || y == LabMinY || y == LabMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, LabMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, LabMinY + 1, 0f), "SpawnPoint_Laboratoire");

        CreateExaminationStation("TesteurThermique", ExaminationTool.MeasurementType.ThermalConductivity, new Vector3(-8, 2, 0f));
        CreateExaminationStation("FTIR", ExaminationTool.MeasurementType.Treatment, new Vector3(-5, 2, 0f));
        CreateExaminationStation("Raman", ExaminationTool.MeasurementType.Authenticity, new Vector3(0, 2, 0f));
        CreateExaminationStation("UVVisNIR", ExaminationTool.MeasurementType.ConfirmedOrigin, new Vector3(5, 2, 0f));
        CreateExaminationStation("TesteurElectrique", ExaminationTool.MeasurementType.ElectricalConductivity, new Vector3(8, 2, 0f));
        CreateExaminationStation("DiamondView", ExaminationTool.MeasurementType.DiamondGrowthImaging, new Vector3(-5, -2, 0f));
        CreateExaminationStation("MEB", ExaminationTool.MeasurementType.GrowthStructure, new Vector3(0, -2, 0f));
        CreateExaminationStation("DiamondSure", ExaminationTool.MeasurementType.DiamondScreening, new Vector3(5, -2, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Laboratoire.unity");
        AddSceneToBuildSettings("Assets/Scenes/Laboratoire.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Laboratory scene created at Assets/Scenes/Laboratoire.unity");
    }

    [MenuItem("GemLab/Setup Market Scene")]
    public static void CreateMarketScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
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

        var floorTile = GetOrCreateTile("labfloor_pixel", LabFloorPixel);
        var wallTile = GetOrCreateTile("wall_pixel", WallPixel);

        for (var x = MarketMinX; x <= MarketMaxX; x++)
        {
            for (var y = MarketMinY; y <= MarketMaxY; y++)
            {
                var isPerimeter = x == MarketMinX || x == MarketMaxX || y == MarketMinY || y == MarketMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, MarketMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, MarketMinY + 1, 0f), "SpawnPoint_Marche");

        BakeAllFlags();
        CreateMarketVendorSpot(0, new Vector3(-9, 1, 0f));
        CreateMarketVendorSpot(1, new Vector3(-3, 1, 0f));
        CreateMarketVendorSpot(2, new Vector3(3, 1, 0f));
        CreateMarketVendorSpot(3, new Vector3(9, 1, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Marche.unity");
        AddSceneToBuildSettings("Assets/Scenes/Marche.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Market scene created at Assets/Scenes/Marche.unity");
    }

    [MenuItem("GemLab/Setup Rough Market Scene")]
    public static void CreateRoughMarketScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.11f, 0.08f);
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

        var floorTile = GetOrCreateTile("labfloor_pixel", LabFloorPixel);
        var wallTile = GetOrCreateTile("wall_pixel", WallPixel);

        for (var x = RoughMarketMinX; x <= RoughMarketMaxX; x++)
        {
            for (var y = RoughMarketMinY; y <= RoughMarketMaxY; y++)
            {
                var isPerimeter = x == RoughMarketMinX || x == RoughMarketMaxX || y == RoughMarketMinY || y == RoughMarketMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, RoughMarketMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, RoughMarketMinY + 1, 0f), "SpawnPoint_MarcheBrutes");

        BakeAllFlags();
        CreateRoughMarketVendorSpot(0, new Vector3(-9, 1, 0f));
        CreateRoughMarketVendorSpot(1, new Vector3(-3, 1, 0f));
        CreateRoughMarketVendorSpot(2, new Vector3(3, 1, 0f));
        CreateRoughMarketVendorSpot(3, new Vector3(9, 1, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MarcheBrutes.unity");
        AddSceneToBuildSettings("Assets/Scenes/MarcheBrutes.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Rough market scene created at Assets/Scenes/MarcheBrutes.unity");
    }

    [MenuItem("GemLab/Setup Museum Scene")]
    public static void CreateMuseumScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.10f, 0.13f);
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

        var floorTile = GetOrCreateTile("labfloor_pixel", LabFloorPixel);
        var wallTile = GetOrCreateTile("wall_pixel", WallPixel);

        for (var x = MuseumMinX; x <= MuseumMaxX; x++)
        {
            for (var y = MuseumMinY; y <= MuseumMaxY; y++)
            {
                var isPerimeter = x == MuseumMinX || x == MuseumMaxX || y == MuseumMinY || y == MuseumMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, MuseumMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, MuseumMinY + 1, 0f), "SpawnPoint_Musee");

        CreateMuseumPedestal(0, new Vector3(-9, 2, 0f));
        CreateMuseumPedestal(1, new Vector3(-3, 2, 0f));
        CreateMuseumPedestal(2, new Vector3(3, 2, 0f));
        CreateMuseumPedestal(3, new Vector3(9, 2, 0f));
        CreateMuseumPedestal(4, new Vector3(-9, -2, 0f));
        CreateMuseumPedestal(5, new Vector3(-3, -2, 0f));
        CreateMuseumPedestal(6, new Vector3(3, -2, 0f));
        CreateMuseumPedestal(7, new Vector3(9, -2, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Musee.unity");
        AddSceneToBuildSettings("Assets/Scenes/Musee.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Museum scene created at Assets/Scenes/Musee.unity");
    }

    [MenuItem("GemLab/Setup Tunnel Scene")]
    public static void CreateTunnelScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.05f);
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

        var floorTile = GetOrCreateTile("tunnelfloor_pixel", TunnelFloorPixel);
        var wallTile = GetOrCreateTile("tunnelwall_pixel", TunnelWallPixel);

        for (var x = TunnelMinX; x <= TunnelMaxX; x++)
        {
            for (var y = TunnelMinY; y <= TunnelMaxY; y++)
            {
                var isPerimeter = x == TunnelMinX || x == TunnelMaxX || y == TunnelMinY || y == TunnelMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, TunnelMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, TunnelMinY + 1, 0f), "SpawnPoint_Tunnel");
        CreateExitDoor(new Vector3(0, TunnelMaxY - 1, 0f), "SpawnPoint_Tunnel");

        CreateTunnelMerchant(new Vector3(0, 0, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Tunnel.unity");
        AddSceneToBuildSettings("Assets/Scenes/Tunnel.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Tunnel scene created at Assets/Scenes/Tunnel.unity");
    }

    [MenuItem("GemLab/Setup Manoir Scene")]
    public static void CreateManoirScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) UnityEngine.Object.DestroyImmediate(light);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.08f, 0.11f);
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

        for (var x = ManoirMinX; x <= ManoirMaxX; x++)
        {
            for (var y = ManoirMinY; y <= ManoirMaxY; y++)
            {
                var isPerimeter = x == ManoirMinX || x == ManoirMaxX || y == ManoirMinY || y == ManoirMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, ManoirMinY + 2, 0f);

        var playerSprite = GetPlayerSprite();
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
        col.size = new Vector2(0.7f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
        player.AddComponent<PlayerWallet>();
        player.AddComponent<PlayerChest>();
        player.AddComponent<GemCodex>();
        player.AddComponent<MailboxState>();
        player.AddComponent<GameClock>();
        player.AddComponent<ReturnToMenuController>();
        player.AddComponent<MarketState>();
        player.AddComponent<QuestState>();
        player.AddComponent<RoughMarketState>();
        player.AddComponent<TunnelState>();
        player.AddComponent<ManoirRequestState>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, ManoirMinY + 1, 0f), "SpawnPoint_Manoir");

        CreateOldMan(new Vector3(-3, 1, 0f));
        CreateOldLady(new Vector3(3, 1, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Manoir.unity");
        AddSceneToBuildSettings("Assets/Scenes/Manoir.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Manoir scene created at Assets/Scenes/Manoir.unity");
    }

    private static readonly Dictionary<char, Color> OldManPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.80f, 0.80f, 0.82f) },
        { 'S', new Color(0.80f, 0.65f, 0.55f) },
        { 'E', new Color(0.10f, 0.10f, 0.10f) },
        { 'C', new Color(0.35f, 0.10f, 0.14f) },
        { 'D', new Color(0.22f, 0.06f, 0.09f) },
        { 'B', new Color(0.12f, 0.10f, 0.10f) },
    };

    private static readonly Dictionary<char, Color> OldLadyPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.88f, 0.88f, 0.88f) },
        { 'S', new Color(0.82f, 0.67f, 0.57f) },
        { 'E', new Color(0.10f, 0.10f, 0.10f) },
        { 'C', new Color(0.25f, 0.20f, 0.45f) },
        { 'D', new Color(0.16f, 0.13f, 0.30f) },
        { 'B', new Color(0.14f, 0.12f, 0.14f) },
    };

    private static void CreateOldMan(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("oldman_pixel", PlayerPattern, OldManPalette);

        var go = new GameObject("Vieil Homme");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        go.AddComponent<ManoirOldMan>();
    }

    private static void CreateOldLady(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("oldlady_pixel", PlayerPattern, OldLadyPalette);

        var go = new GameObject("Vieille Dame");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        go.AddComponent<ManoirOldLady>();
    }

    private static void AddSceneToBuildSettings(string path, bool insertFirst = false)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        if (insertFirst)
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        else
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateExitDoor(Vector3 position, string spawnPointName = "")
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
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        var spawnPointProp = so.FindProperty("spawnPointName");
        if (sceneNameProp != null) sceneNameProp.stringValue = "Town";
        if (implementedProp != null) implementedProp.boolValue = true;
        if (spawnPointProp != null) spawnPointProp.stringValue = spawnPointName;
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

    private static void CreateBed(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("bed_pixel", BedPattern, BedPalette);

        var bed = new GameObject("Lit");
        var sr = bed.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        bed.transform.position = position;

        var col = bed.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        bed.AddComponent<BedInteractable>();
    }

    private static void UpdateGemDatabase(GemDefinition[] allGems)
    {
        const string path = "Assets/Resources/GemDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<GemDatabase>(path);
        var isNew = db == null;
        if (isNew) db = ScriptableObject.CreateInstance<GemDatabase>();

        db.allGems = new List<GemDefinition>(allGems);

        if (isNew)
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(db, path);
        }
        else
        {
            EditorUtility.SetDirty(db);
        }
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
        so.FindProperty("sceneName").stringValue = "Tunnel";
        so.FindProperty("implemented").boolValue = true;
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Le Tunnel", position + new Vector3(0, 1.3f, 0));
    }

    private static readonly Dictionary<char, Color> TunnelMerchantPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.12f, 0.08f, 0.16f) },
        { 'S', new Color(0.55f, 0.52f, 0.55f) },
        { 'E', new Color(0.75f, 0.15f, 0.15f) },
        { 'C', new Color(0.10f, 0.07f, 0.14f) },
        { 'D', new Color(0.05f, 0.03f, 0.08f) },
        { 'B', new Color(0.06f, 0.05f, 0.07f) },
    };

    private static void CreateTunnelMerchant(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("tunnelmerchant_pixel", PlayerPattern, TunnelMerchantPalette);

        var go = new GameObject("Marchand du Tunnel");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        sr.enabled = false;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);
        col.enabled = false;

        var merchant = go.AddComponent<TunnelMerchant>();
        var so = new SerializedObject(merchant);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("interactionCollider").objectReferenceValue = col;
        so.ApplyModifiedProperties();
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

    private static void CreateNamedSpawnPoint(string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.position = position;
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

    private static readonly Dictionary<char, Color>[] MerchantPalettes =
    {
        new Dictionary<char, Color>
        {
            { 'H', new Color(0.75f, 0.55f, 0.15f) },
            { 'S', new Color(0.65f, 0.45f, 0.30f) },
            { 'E', new Color(0.10f, 0.10f, 0.10f) },
            { 'C', new Color(0.55f, 0.15f, 0.15f) },
            { 'D', new Color(0.40f, 0.10f, 0.10f) },
            { 'B', new Color(0.16f, 0.16f, 0.18f) },
        },
        new Dictionary<char, Color>
        {
            { 'H', new Color(0.20f, 0.35f, 0.55f) },
            { 'S', new Color(0.85f, 0.68f, 0.52f) },
            { 'E', new Color(0.10f, 0.10f, 0.10f) },
            { 'C', new Color(0.30f, 0.50f, 0.30f) },
            { 'D', new Color(0.20f, 0.38f, 0.20f) },
            { 'B', new Color(0.16f, 0.16f, 0.18f) },
        },
        new Dictionary<char, Color>
        {
            { 'H', new Color(0.45f, 0.20f, 0.55f) },
            { 'S', new Color(0.35f, 0.22f, 0.15f) },
            { 'E', new Color(0.10f, 0.10f, 0.10f) },
            { 'C', new Color(0.85f, 0.65f, 0.20f) },
            { 'D', new Color(0.65f, 0.48f, 0.12f) },
            { 'B', new Color(0.16f, 0.16f, 0.18f) },
        },
        new Dictionary<char, Color>
        {
            { 'H', new Color(0.15f, 0.15f, 0.15f) },
            { 'S', new Color(0.92f, 0.75f, 0.60f) },
            { 'E', new Color(0.10f, 0.10f, 0.10f) },
            { 'C', new Color(0.20f, 0.20f, 0.50f) },
            { 'D', new Color(0.14f, 0.14f, 0.35f) },
            { 'B', new Color(0.16f, 0.16f, 0.18f) },
        },
    };

    private static readonly string[] MarketTablePattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(1, '.', 14, 'T', 1, '.'),
        Row(1, '.', 14, 'T', 1, '.'),
        Row(1, '.', 14, 'T', 1, '.'),
        Row(1, '.', 14, 'T', 1, '.'),
        Row(1, '.', 14, 'T', 1, '.'),
        Row(1, '.', 14, 'N', 1, '.'),
        Row(2, '.', 2, 'L', 8, '.', 2, 'L', 2, '.'),
        Row(2, '.', 2, 'L', 8, '.', 2, 'L', 2, '.'),
        Row(2, '.', 2, 'L', 8, '.', 2, 'L', 2, '.'),
        Row(2, '.', 2, 'L', 8, '.', 2, 'L', 2, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MarketTablePalette = new Dictionary<char, Color>
    {
        { 'T', new Color(0.75f, 0.35f, 0.15f) },
        { 'N', new Color(0.45f, 0.20f, 0.08f) },
        { 'L', new Color(0.35f, 0.22f, 0.12f) },
    };

    private static void CreateMarketVendorSpot(int vendorIndex, Vector3 position)
    {
        var tableSprite = GetOrCreatePixelSprite("markettable_pixel", MarketTablePattern, MarketTablePalette);
        var tableGO = new GameObject($"MarketTable_{vendorIndex}");
        var tableSr = tableGO.AddComponent<SpriteRenderer>();
        tableSr.sprite = tableSprite;
        tableSr.sortingOrder = 4;
        tableGO.transform.position = position;

        var palette = MerchantPalettes[vendorIndex % MerchantPalettes.Length];
        var merchantSprite = GetOrCreatePixelSprite($"merchant{vendorIndex}_pixel", PlayerPattern, palette);
        var merchantGO = new GameObject($"Merchant_{vendorIndex}");
        var merchantSr = merchantGO.AddComponent<SpriteRenderer>();
        merchantSr.sprite = merchantSprite;
        merchantSr.sortingOrder = 4;
        merchantGO.transform.position = position + new Vector3(0f, 0.6f, 0f);

        var flagGO = new GameObject("Flag", typeof(SpriteRenderer));
        flagGO.transform.SetParent(merchantGO.transform, false);
        flagGO.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        flagGO.transform.localScale = Vector3.one * 0.5f;
        var flagSr = flagGO.GetComponent<SpriteRenderer>();
        flagSr.sortingOrder = 6;

        var tableItemsGO = new GameObject("TableItems");
        tableItemsGO.transform.SetParent(tableGO.transform, false);
        tableItemsGO.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        var col = tableGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 1.4f);
        col.offset = new Vector2(0f, 0.3f);

        var spot = tableGO.AddComponent<MarketVendorSpot>();
        var so = new SerializedObject(spot);
        so.FindProperty("vendorIndex").intValue = vendorIndex;
        so.FindProperty("flagRenderer").objectReferenceValue = flagSr;
        so.FindProperty("tableItemsRoot").objectReferenceValue = tableItemsGO.transform;
        so.ApplyModifiedProperties();
    }

    private static void CreateRoughMarketVendorSpot(int vendorIndex, Vector3 position)
    {
        var tableSprite = GetOrCreatePixelSprite("markettable_pixel", MarketTablePattern, MarketTablePalette);
        var tableGO = new GameObject($"RoughMarketTable_{vendorIndex}");
        var tableSr = tableGO.AddComponent<SpriteRenderer>();
        tableSr.sprite = tableSprite;
        tableSr.sortingOrder = 4;
        tableGO.transform.position = position;

        var palette = MerchantPalettes[vendorIndex % MerchantPalettes.Length];
        var merchantSprite = GetOrCreatePixelSprite($"roughmerchant{vendorIndex}_pixel", PlayerPattern, palette);
        var merchantGO = new GameObject($"RoughMerchant_{vendorIndex}");
        var merchantSr = merchantGO.AddComponent<SpriteRenderer>();
        merchantSr.sprite = merchantSprite;
        merchantSr.sortingOrder = 4;
        merchantGO.transform.position = position + new Vector3(0f, 0.6f, 0f);

        var flagGO = new GameObject("Flag", typeof(SpriteRenderer));
        flagGO.transform.SetParent(merchantGO.transform, false);
        flagGO.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        flagGO.transform.localScale = Vector3.one * 0.5f;
        var flagSr = flagGO.GetComponent<SpriteRenderer>();
        flagSr.sortingOrder = 6;

        var tableItemsGO = new GameObject("TableItems");
        tableItemsGO.transform.SetParent(tableGO.transform, false);
        tableItemsGO.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        var col = tableGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 1.4f);
        col.offset = new Vector2(0f, 0.3f);

        var spot = tableGO.AddComponent<RoughMarketVendorSpot>();
        var so = new SerializedObject(spot);
        so.FindProperty("vendorIndex").intValue = vendorIndex;
        so.FindProperty("flagRenderer").objectReferenceValue = flagSr;
        so.FindProperty("tableItemsRoot").objectReferenceValue = tableItemsGO.transform;
        so.ApplyModifiedProperties();
    }

    private static readonly string[] MuseumPedestalPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(5, '.', 6, 'D', 5, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(6, '.', 4, 'D', 6, '.'),
        Row(4, '.', 8, 'K', 4, '.'),
        Row(3, '.', 10, 'K', 3, '.'),
        Row(3, '.', 10, 'K', 3, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MuseumPedestalPalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.78f, 0.74f, 0.60f) },
        { 'K', new Color(0.55f, 0.50f, 0.38f) },
    };

    private static void CreateMuseumPedestal(int rank, Vector3 position)
    {
        var pedestalSprite = GetOrCreatePixelSprite("museumpedestal_pixel", MuseumPedestalPattern, MuseumPedestalPalette);
        var pedestalGO = new GameObject($"MuseumPedestal_{rank}");
        var pedestalSr = pedestalGO.AddComponent<SpriteRenderer>();
        pedestalSr.sprite = pedestalSprite;
        pedestalSr.sortingOrder = 4;
        pedestalGO.transform.position = position;

        var iconGO = new GameObject("Icon", typeof(SpriteRenderer));
        iconGO.transform.SetParent(pedestalGO.transform, false);
        iconGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        iconGO.transform.localScale = Vector3.one * 0.6f;
        var iconSr = iconGO.GetComponent<SpriteRenderer>();
        iconSr.sortingOrder = 5;

        var col = pedestalGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);
        col.offset = new Vector2(0f, 0.2f);

        var pedestal = pedestalGO.AddComponent<MuseumPedestal>();
        var so = new SerializedObject(pedestal);
        so.FindProperty("rank").intValue = rank;
        so.FindProperty("iconRenderer").objectReferenceValue = iconSr;
        so.ApplyModifiedProperties();
    }

    private static readonly string[] ReportSpeciesOptions =
    {
        "Quartz", "Corindon (Rubis)", "Spinelle", "Grenat Almandin", "Verre",
        "Péridot", "Spinelle Bleu", "Saphir Bleu", "Zircon", "Diamant",
        "Améthyste", "Grenat Spessartite", "Émeraude",
        "Grenat Pyrope", "Grenat Tsavorite", "Grenat Hessonite",
        "Tanzanite", "Iolite", "Kunzite", "Tourmaline Rose", "Diopside Chromifère",
        "Héliodore", "Morganite", "Goshénite", "Apatite", "Andalousite",
        "Chrysobéryl", "Chrysobéryl Alexandrite", "CZ", "Moissanite Synthétique",
        "Fluorite", "Grenat Démantoïde", "Hématite", "Pezzottaite", "Pyrite",
        "Corindon Synthétique (Rubis)", "Corindon Synthétique (Saphir)",
        "Saphir Jaune", "Saphir Rose", "Saphir Violet", "Saphir Orange",
        "Padparadscha", "Saphir Vert", "Saphir Incolore",
        "YAG", "GGG", "Grenat Malaya", "Grenat Rhodolite",
        "Citrine", "Amétrine", "Quartz Fumé", "Prasiolite", "Quartz Rose", "Zoïsite",
        "Jade Jadéite", "Jade Néphrite", "Grenat Hydrogrossulaire", "Sinhalite", "Scapolite",
        "Pierre de Lune", "Orthose", "Labradorite", "Serpentine Bowénite",
        "Opale Noble", "Opale de Feu", "Kyanite", "Turquoise", "Turquoise Stabilisée",
        "Obsidienne", "Rhodocrosite", "Rhodonite",
        "Diamant Synthétique (CVD)", "Diamant Synthétique (HPHT)",
        "Painite", "Béryl Rouge (Bixbite)", "Ivoire d'Éléphant", "Ivoire de Mammouth",
        "Ivoire de Morse", "Ivoire d'Hippopotame", "Défense de Narval",
        "Corne de Rhinocéros", "Écaille de Tortue"
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
        var dayGO = CreateUIText("DayText", canvasGO.transform, "Jour 1",
            new Vector2(20, -50), new Vector2(300, 24), 16, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.85f));
        moneyGO.AddComponent<HUDController>();
        var hudSo = new SerializedObject(moneyGO.GetComponent<HUDController>());
        hudSo.FindProperty("moneyText").objectReferenceValue = moneyGO.GetComponent<Text>();
        hudSo.FindProperty("dayText").objectReferenceValue = dayGO.GetComponent<Text>();
        hudSo.ApplyModifiedProperties();

        var moneyDeltaAnchorGO = new GameObject("MoneyDeltaAnchor", typeof(RectTransform));
        moneyDeltaAnchorGO.transform.SetParent(canvasGO.transform, false);
        var moneyDeltaAnchorRect = moneyDeltaAnchorGO.GetComponent<RectTransform>();
        moneyDeltaAnchorRect.anchorMin = new Vector2(0f, 1f);
        moneyDeltaAnchorRect.anchorMax = new Vector2(0f, 1f);
        moneyDeltaAnchorRect.pivot = new Vector2(0f, 1f);
        moneyDeltaAnchorRect.anchoredPosition = new Vector2(230, -20);
        moneyDeltaAnchorRect.sizeDelta = new Vector2(160, 30);

        var floatingMoneyUI = canvasGO.AddComponent<FloatingMoneyUI>();
        var floatingMoneySo = new SerializedObject(floatingMoneyUI);
        floatingMoneySo.FindProperty("spawnAnchor").objectReferenceValue = moneyDeltaAnchorRect;
        floatingMoneySo.ApplyModifiedProperties();

        CreateUIText("InventoryHint", canvasGO.transform, "Tab : Inventaire",
            new Vector2(20, -76), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));
        CreateUIText("GrimoireHint", canvasGO.transform, "G : Grimoire",
            new Vector2(20, -98), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));
        CreateUIText("MenuHint", canvasGO.transform, "Échap : Menu (sauvegarde)",
            new Vector2(20, -120), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));
        CreateUIText("QuestHint", canvasGO.transform, "J : Quêtes",
            new Vector2(20, -142), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));

        var feedbackGO = CreateUIText("ToolFeedback", canvasGO.transform, "",
            new Vector2(0, 20), new Vector2(760, 90), 16, TextAnchor.LowerCenter, new Color(0.95f, 0.95f, 0.8f));
        var feedbackRect = feedbackGO.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 0f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0f);
        feedbackRect.pivot = new Vector2(0.5f, 0f);
        feedbackGO.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        var toolFeedback = feedbackGO.AddComponent<ToolFeedbackUI>();
        var feedbackSo = new SerializedObject(toolFeedback);
        feedbackSo.FindProperty("feedbackText").objectReferenceValue = feedbackGO.GetComponent<Text>();
        feedbackSo.ApplyModifiedProperties();

        CreateGemTooltipUI(canvasGO.transform);
        CreateInventoryUI(canvasGO.transform);
        CreateGrimoireUI(canvasGO.transform);
        CreateReturnToMenuUI(canvasGO.transform);
        CreateMarketUI(canvasGO.transform);
        CreateRoughMarketUI(canvasGO.transform);
        CreateQuestUI(canvasGO.transform);
        CreateToolInfoUI(canvasGO.transform);
        CreateTunnelUI(canvasGO.transform);
        CreateSceneFader();

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
        const float panelHeight = 660f;

        panelRect.sizeDelta = new Vector2(480, panelHeight);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Rapport d'identification",
            new Vector2(0, -18), new Vector2(440, 28), 20, TextAnchor.UpperCenter, Color.white);

        var measurementsGO = CreateUIText("Measurements", panelGO.transform, "",
            new Vector2(0, -52), new Vector2(440, 260), 12, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f));

        var scrollViewGO = new GameObject("SpeciesScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(panelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -326);
        scrollViewRect.sizeDelta = new Vector2(420, 240);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        var totalRows = Mathf.CeilToInt(ReportSpeciesOptions.Length / 2f);
        var contentHeight = Mathf.Max(240f, totalRows * (buttonHeight + rowGap) + 8f);
        contentRect.sizeDelta = new Vector2(420, contentHeight);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        for (var i = 0; i < ReportSpeciesOptions.Length; i++)
        {
            var col = i % 2;
            var row = i / 2;
            var x = col == 0 ? -(buttonWidth + colGap) / 2f : (buttonWidth + colGap) / 2f;
            var y = -8f - row * (buttonHeight + rowGap);
            var buttonGO = CreateUIButton($"Option_{i}", contentGO.transform, ReportSpeciesOptions[i],
                new Vector2(x, y), new Vector2(buttonWidth, buttonHeight), 12);
            var option = buttonGO.AddComponent<ReportOptionButton>();
            var optSo = new SerializedObject(option);
            optSo.FindProperty("speciesName").stringValue = ReportSpeciesOptions[i];
            optSo.ApplyModifiedProperties();
        }

        var resultGO = CreateUIText("Result", panelGO.transform, "",
            new Vector2(0, -580), new Vector2(440, 32), 15, TextAnchor.UpperCenter, new Color(0.9f, 0.85f, 0.3f));

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -620), new Vector2(180, 32));
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
        CreateMailboxUI(canvasGO.transform);
    }

    private static void CreateGemTooltipUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("GemTooltip", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(240, 30);
        panelGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
        panelGO.GetComponent<Image>().raycastTarget = false;
        panelGO.SetActive(false);

        var labelGO = CreateUIText("Label", panelGO.transform, "",
            Vector2.zero, new Vector2(240, 30), 13, TextAnchor.UpperLeft, Color.white);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(-16f, -8f);
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
        panelRect.sizeDelta = new Vector2(300, 280);
        panelRect.anchoredPosition = new Vector2(-20, 0);
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Inventaire",
            new Vector2(0, -14), new Vector2(260, 22), 18, TextAnchor.UpperCenter, Color.white);

        var clockGO = CreateUIText("Clock", panelGO.transform, "",
            new Vector2(0, -38), new Vector2(260, 18), 12, TextAnchor.UpperCenter, new Color(0.75f, 0.75f, 0.75f));

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 1f);
        slotsRect.anchorMax = new Vector2(0.5f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, -64);
        slotsRect.sizeDelta = new Vector2(248, 184);

        var dropZone = panelGO.AddComponent<GemDropZone>();

        var mysteryIcon = GetGemIconSprite("mystery", new Color(0.42f, 0.42f, 0.45f), new Color(0.55f, 0.55f, 0.58f));

        var inventoryUI = canvasTransform.gameObject.AddComponent<InventoryUI>();
        var so = new SerializedObject(inventoryUI);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        so.FindProperty("dropZone").objectReferenceValue = dropZone;
        so.FindProperty("mysteryIcon").objectReferenceValue = mysteryIcon;
        so.FindProperty("clockText").objectReferenceValue = clockGO.GetComponent<Text>();
        so.ApplyModifiedProperties();
    }

    private static void CreateGrimoireUI(Transform canvasTransform)
    {
        const float panelWidth = 700f;
        const float panelHeight = 560f;

        var gridPanelGO = new GameObject("GrimoireGridPanel", typeof(RectTransform), typeof(Image));
        gridPanelGO.transform.SetParent(canvasTransform, false);
        var gridPanelRect = gridPanelGO.GetComponent<RectTransform>();
        gridPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridPanelRect.pivot = new Vector2(0.5f, 0.5f);
        gridPanelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        gridPanelRect.anchoredPosition = Vector2.zero;
        gridPanelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        gridPanelGO.SetActive(false);

        CreateUIText("Title", gridPanelGO.transform, "Grimoire des gemmes",
            new Vector2(0, -16), new Vector2(640, 28), 20, TextAnchor.UpperCenter, Color.white);

        var progressGO = CreateUIText("Progress", gridPanelGO.transform, "",
            new Vector2(0, -46), new Vector2(640, 22), 13, TextAnchor.UpperCenter, new Color(0.75f, 0.75f, 0.75f));

        var scrollViewGO = new GameObject("GridScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(gridPanelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -76);
        scrollViewRect.sizeDelta = new Vector2(660, 428);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(660, 428);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var closeButtonGO = CreateUIButton("CloseButton", gridPanelGO.transform, "Fermer (G)",
            new Vector2(0, -520), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseGrimoireButton>();

        var lockedIcon = GetGemIconSprite("grimoirelocked", new Color(0.30f, 0.30f, 0.34f), new Color(0.42f, 0.42f, 0.46f));

        var detailPanelGO = new GameObject("GrimoireDetailPanel", typeof(RectTransform), typeof(Image));
        detailPanelGO.transform.SetParent(canvasTransform, false);
        var detailPanelRect = detailPanelGO.GetComponent<RectTransform>();
        detailPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailPanelRect.pivot = new Vector2(0.5f, 0.5f);
        detailPanelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        detailPanelRect.anchoredPosition = Vector2.zero;
        detailPanelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        detailPanelGO.SetActive(false);

        var detailTitleGO = CreateUIText("Title", detailPanelGO.transform, "",
            new Vector2(0, -16), new Vector2(640, 28), 20, TextAnchor.UpperCenter, Color.white);

        var spectrumBarGO = new GameObject("SpectrumBar", typeof(RectTransform), typeof(Image));
        spectrumBarGO.transform.SetParent(detailPanelGO.transform, false);
        var spectrumBarRect = spectrumBarGO.GetComponent<RectTransform>();
        spectrumBarRect.anchorMin = new Vector2(0.5f, 1f);
        spectrumBarRect.anchorMax = new Vector2(0.5f, 1f);
        spectrumBarRect.pivot = new Vector2(0.5f, 1f);
        spectrumBarRect.anchoredPosition = new Vector2(0, -54);
        spectrumBarRect.sizeDelta = new Vector2(620, 34);
        var spectrumBarImage = spectrumBarGO.GetComponent<Image>();
        spectrumBarImage.preserveAspect = false;

        CreateUIText("MinLabel", detailPanelGO.transform, "400 nm",
            new Vector2(-310, -90), new Vector2(80, 18), 11, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.6f));
        CreateUIText("MaxLabel", detailPanelGO.transform, "700 nm",
            new Vector2(310, -90), new Vector2(80, 18), 11, TextAnchor.UpperRight, new Color(0.6f, 0.6f, 0.6f));

        var detailBodyGO = CreateUIText("Body", detailPanelGO.transform, "",
            new Vector2(0, -114), new Vector2(640, 396), 12, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.85f));

        var backButtonGO = CreateUIButton("BackButton", detailPanelGO.transform, "Retour",
            new Vector2(0, -520), new Vector2(180, 32));
        backButtonGO.AddComponent<BackToGrimoireGridButton>();

        var grimoire = canvasTransform.gameObject.AddComponent<GrimoireUI>();
        var so = new SerializedObject(grimoire);
        so.FindProperty("gridPanel").objectReferenceValue = gridPanelGO;
        so.FindProperty("contentContainer").objectReferenceValue = contentRect;
        so.FindProperty("progressText").objectReferenceValue = progressGO.GetComponent<Text>();
        so.FindProperty("lockedIcon").objectReferenceValue = lockedIcon;
        so.FindProperty("detailPanel").objectReferenceValue = detailPanelGO;
        so.FindProperty("detailTitleText").objectReferenceValue = detailTitleGO.GetComponent<Text>();
        so.FindProperty("detailBodyText").objectReferenceValue = detailBodyGO.GetComponent<Text>();
        so.FindProperty("detailSpectrumImage").objectReferenceValue = spectrumBarImage;
        so.ApplyModifiedProperties();
    }

    private static void CreateReturnToMenuUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("ReturnToMenuPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(360, 160);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        CreateUIText("Message", panelGO.transform, "Retour au menu ?\n(la partie sera sauvegardée)",
            new Vector2(0, -24), new Vector2(320, 60), 16, TextAnchor.UpperCenter, Color.white);

        var yesButtonGO = CreateUIButton("YesButton", panelGO.transform, "Oui",
            new Vector2(-85, -110), new Vector2(140, 36));
        yesButtonGO.AddComponent<ConfirmReturnToMenuButton>();

        var noButtonGO = CreateUIButton("NoButton", panelGO.transform, "Non",
            new Vector2(85, -110), new Vector2(140, 36));
        noButtonGO.AddComponent<CancelReturnToMenuButton>();

        var controller = canvasTransform.gameObject.AddComponent<ReturnToMenuUI>();
        var returnSo = new SerializedObject(controller);
        returnSo.FindProperty("panel").objectReferenceValue = panelGO;
        returnSo.ApplyModifiedProperties();
    }

    private static void CreateMarketUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("MarketPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 380);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        var titleGO = CreateUIText("Title", panelGO.transform, "",
            new Vector2(0, -14), new Vector2(420, 26), 18, TextAnchor.UpperCenter, Color.white);

        var scrollViewGO = new GameObject("MarketScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(panelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -50);
        scrollViewRect.sizeDelta = new Vector2(420, 240);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(400, 240);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -340), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseMarketButton>();

        var marketUI = canvasTransform.gameObject.AddComponent<MarketUI>();
        var marketSo = new SerializedObject(marketUI);
        marketSo.FindProperty("panel").objectReferenceValue = panelGO;
        marketSo.FindProperty("titleText").objectReferenceValue = titleGO.GetComponent<Text>();
        marketSo.FindProperty("content").objectReferenceValue = contentRect;
        marketSo.ApplyModifiedProperties();
    }

    private static void CreateRoughMarketUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("RoughMarketPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 380);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        var titleGO = CreateUIText("Title", panelGO.transform, "",
            new Vector2(0, -14), new Vector2(420, 26), 18, TextAnchor.UpperCenter, Color.white);

        var scrollViewGO = new GameObject("RoughMarketScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(panelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -50);
        scrollViewRect.sizeDelta = new Vector2(420, 240);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(400, 240);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -340), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseRoughMarketButton>();

        var roughMarketUI = canvasTransform.gameObject.AddComponent<RoughMarketUI>();
        var roughMarketSo = new SerializedObject(roughMarketUI);
        roughMarketSo.FindProperty("panel").objectReferenceValue = panelGO;
        roughMarketSo.FindProperty("titleText").objectReferenceValue = titleGO.GetComponent<Text>();
        roughMarketSo.FindProperty("content").objectReferenceValue = contentRect;
        roughMarketSo.ApplyModifiedProperties();
    }

    private static void CreateTunnelUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("TunnelPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(380, 220);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.97f);
        panelGO.SetActive(false);

        var titleGO = CreateUIText("Title", panelGO.transform, "Le Marchand du Tunnel",
            new Vector2(0, -16), new Vector2(340, 26), 18, TextAnchor.UpperCenter, new Color(0.85f, 0.75f, 0.95f));

        var itemGO = CreateUIText("ItemText", panelGO.transform, "",
            new Vector2(0, -60), new Vector2(340, 90), 14, TextAnchor.UpperCenter, Color.white);

        var buyButtonGO = CreateUIButton("BuyButton", panelGO.transform, "Acheter",
            new Vector2(0, -150), new Vector2(200, 34));
        var buyButtonText = buyButtonGO.GetComponentInChildren<Text>();

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -192), new Vector2(160, 30));
        closeButtonGO.AddComponent<CloseTunnelButton>();

        var tunnelUI = canvasTransform.gameObject.AddComponent<TunnelUI>();
        var tunnelSo = new SerializedObject(tunnelUI);
        tunnelSo.FindProperty("panel").objectReferenceValue = panelGO;
        tunnelSo.FindProperty("titleText").objectReferenceValue = titleGO.GetComponent<Text>();
        tunnelSo.FindProperty("itemText").objectReferenceValue = itemGO.GetComponent<Text>();
        tunnelSo.FindProperty("buyButton").objectReferenceValue = buyButtonGO.GetComponent<Button>();
        tunnelSo.FindProperty("buyButtonText").objectReferenceValue = buyButtonText;
        tunnelSo.ApplyModifiedProperties();
    }

    private static void CreateSceneFader()
    {
        var canvasGO = new GameObject("SceneFaderCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        var imageGO = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        var rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        var image = imageGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        var fader = canvasGO.AddComponent<SceneFader>();
        var so = new SerializedObject(fader);
        var fadeImageProp = so.FindProperty("fadeImage");
        if (fadeImageProp != null) fadeImageProp.objectReferenceValue = image;
        so.ApplyModifiedProperties();
    }

    private static void CreateQuestUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("QuestPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 380);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Quêtes",
            new Vector2(0, -14), new Vector2(420, 26), 18, TextAnchor.UpperCenter, Color.white);

        var scrollViewGO = new GameObject("QuestScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(panelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -50);
        scrollViewRect.sizeDelta = new Vector2(420, 240);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(400, 240);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -340), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseQuestButton>();

        var questUI = canvasTransform.gameObject.AddComponent<QuestUI>();
        var questSo = new SerializedObject(questUI);
        questSo.FindProperty("panel").objectReferenceValue = panelGO;
        questSo.FindProperty("content").objectReferenceValue = contentRect;
        questSo.ApplyModifiedProperties();
    }

    private static void CreateToolInfoUI(Transform canvasTransform)
    {
        var buttonGO = CreateUIButton("ToolInfoButton", canvasTransform, "i",
            new Vector2(-20, 20), new Vector2(40, 40), 20);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonGO.GetComponent<Image>().color = new Color(0.30f, 0.45f, 0.65f, 1f);
        buttonGO.AddComponent<OpenToolInfoButton>();

        var panelGO = new GameObject("ToolInfoPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(440, 260);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        var titleGO = CreateUIText("Title", panelGO.transform, "",
            new Vector2(0, -16), new Vector2(400, 28), 18, TextAnchor.UpperCenter, Color.white);

        var bodyGO = CreateUIText("Body", panelGO.transform, "",
            new Vector2(0, -56), new Vector2(400, 150), 14, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.85f));
        bodyGO.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -220), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseToolInfoButton>();

        var toolInfoUI = canvasTransform.gameObject.AddComponent<ToolInfoUI>();
        var toolInfoSo = new SerializedObject(toolInfoUI);
        toolInfoSo.FindProperty("infoButton").objectReferenceValue = buttonGO;
        toolInfoSo.FindProperty("panel").objectReferenceValue = panelGO;
        toolInfoSo.FindProperty("titleText").objectReferenceValue = titleGO.GetComponent<Text>();
        toolInfoSo.FindProperty("bodyText").objectReferenceValue = bodyGO.GetComponent<Text>();
        toolInfoSo.ApplyModifiedProperties();
    }

    private static void CreateChestUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("ChestPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 320);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Coffre",
            new Vector2(0, -14), new Vector2(360, 26), 18, TextAnchor.UpperCenter, Color.white);

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panelGO.transform, false);
        var slotsRect = slotsGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 1f);
        slotsRect.anchorMax = new Vector2(0.5f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.anchoredPosition = new Vector2(0, -46);
        slotsRect.sizeDelta = new Vector2(376, 248);

        var dropZone = panelGO.AddComponent<GemDropZone>();

        var chestUI = canvasTransform.gameObject.AddComponent<ChestUI>();
        var so = new SerializedObject(chestUI);
        so.FindProperty("panel").objectReferenceValue = panelGO;
        so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
        so.FindProperty("dropZone").objectReferenceValue = dropZone;
        so.ApplyModifiedProperties();
    }

    private static void CreateMailboxUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("MailboxPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460, 460);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Boîte aux lettres",
            new Vector2(0, -14), new Vector2(420, 26), 18, TextAnchor.UpperCenter, Color.white);

        var receiveTabGO = CreateUIButton("ReceiveTab", panelGO.transform, "Récupérer",
            new Vector2(-105, -46), new Vector2(190, 32), 13);
        receiveTabGO.AddComponent<MailboxReceiveTabButton>();

        var sendTabGO = CreateUIButton("SendTab", panelGO.transform, "Envoyer",
            new Vector2(105, -46), new Vector2(190, 32), 13);
        sendTabGO.AddComponent<MailboxSendTabButton>();

        var hintGO = CreateUIText("TabHint", panelGO.transform, "",
            new Vector2(0, -84), new Vector2(420, 20), 12, TextAnchor.UpperCenter, new Color(0.7f, 0.7f, 0.7f));

        var scrollViewGO = new GameObject("MailboxScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(panelGO.transform, false);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 1f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 1f);
        scrollViewRect.pivot = new Vector2(0.5f, 1f);
        scrollViewRect.anchoredPosition = new Vector2(0, -108);
        scrollViewRect.sizeDelta = new Vector2(420, 300);
        scrollViewGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.6f);
        scrollViewGO.GetComponent<Mask>().showMaskGraphic = true;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(400, 300);
        contentRect.anchoredPosition = Vector2.zero;

        var scroll = scrollViewGO.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -420), new Vector2(180, 32));
        closeButtonGO.AddComponent<CloseMailboxButton>();

        var mailboxUI = canvasTransform.gameObject.AddComponent<MailboxUI>();
        var mailboxSo = new SerializedObject(mailboxUI);
        mailboxSo.FindProperty("panel").objectReferenceValue = panelGO;
        mailboxSo.FindProperty("content").objectReferenceValue = contentRect;
        mailboxSo.FindProperty("tabHintText").objectReferenceValue = hintGO.GetComponent<Text>();
        mailboxSo.ApplyModifiedProperties();
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
                g.icon = GetGemIconSprite("quartz", new Color(0.85f, 0.87f, 0.88f), new Color(0.97f, 0.98f, 0.99f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("quartz", new Color(0.85f, 0.87f, 0.88f), "Ovale");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, parfois inclusions fluides ou en plumes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière (incolore).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("corindon", new Color(0.75f, 0.12f, 0.12f), new Color(0.95f, 0.35f, 0.30f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("corindon", new Color(0.75f, 0.12f, 0.12f), "Coussin");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
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
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("spinelle", new Color(0.80f, 0.30f, 0.30f), new Color(0.95f, 0.55f, 0.55f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("spinelle", new Color(0.80f, 0.30f, 0.30f), "Rond");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
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
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("grenat", new Color(0.45f, 0.10f, 0.12f), new Color(0.65f, 0.25f, 0.20f), "Poire", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("grenat", new Color(0.45f, 0.10f, 0.12f), "Poire");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
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
                g.magnetism = "Fortement attiré par un aimant puissant (forte teneur en fer).";
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
                g.icon = GetGemIconSprite("pyrope", new Color(0.55f, 0.08f, 0.10f), new Color(0.75f, 0.25f, 0.22f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("pyrope", new Color(0.55f, 0.08f, 0.10f), "Rond");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Bandes d'absorption du fer et du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 512f, widthNm = 30f },
                };
                g.inclusions = "Parfois des cristaux de rutile en aiguilles, généralement peu abondants.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction rouge notable.";
                g.magnetism = "Modérément attiré par un aimant puissant (fer et magnésium).";
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
                g.icon = GetGemIconSprite("tsavorite", new Color(0.12f, 0.55f, 0.25f), new Color(0.35f, 0.78f, 0.45f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("tsavorite", new Color(0.12f, 0.55f, 0.25f), "Rond");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Raies du vanadium et du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 630f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 690f, widthNm = 8f },
                };
                g.inclusions = "Fibres d'amphibole, inclusions caractéristiques des gisements est-africains.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Réaction rougeâtre possible si coloré au chrome.";
                g.magnetism = "Aucune réaction notable à l'aimant (grenat pauvre en fer).";
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
                g.icon = GetGemIconSprite("hessonite", new Color(0.70f, 0.42f, 0.15f), new Color(0.85f, 0.60f, 0.35f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("hessonite", new Color(0.70f, 0.42f, 0.15f), "Coussin");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Bandes d'absorption du fer et du manganèse.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 420f, widthNm = 40f },
                };
                g.inclusions = "Aspect interne \"mélasse\" (treacle) caractéristique, tourbillons de croissance visibles au microscope.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Faible réaction à l'aimant (moins de fer que l'almandin).";
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
                g.icon = GetGemIconSprite("verre", new Color(0.75f, 0.80f, 0.82f), new Color(0.90f, 0.93f, 0.95f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("verre", new Color(0.75f, 0.80f, 0.82f), "Rond");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Spectre continu sans raies nettes, ou raies artificielles selon le colorant.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Bulles rondes et stries d'écoulement, signature typique du verre.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau amorphe).";
                g.chelseaFilterReaction = "Réaction variable et non diagnostique.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("peridot", new Color(0.45f, 0.65f, 0.25f), new Color(0.65f, 0.85f, 0.40f), "Rectangle à pans coupés", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("peridot", new Color(0.45f, 0.65f, 0.25f), "Rectangle à pans coupés");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.036f;
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
                g.magnetism = "Légèrement attiré par un aimant puissant (teneur en fer modérée).";
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
                g.icon = GetGemIconSprite("spinellebleu", new Color(0.15f, 0.40f, 0.80f), new Color(0.45f, 0.65f, 0.95f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("spinellebleu", new Color(0.15f, 0.40f, 0.80f), "Coussin");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
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
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("saphirbleu", new Color(0.08f, 0.18f, 0.55f), new Color(0.30f, 0.42f, 0.78f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("saphirbleu", new Color(0.08f, 0.18f, 0.55f), "Ovale");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
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
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
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
                g.icon = GetGemIconSprite("zircon", new Color(0.20f, 0.55f, 0.75f), new Color(0.45f, 0.78f, 0.92f), "Carré", "Princesse");
                g.roughIcon = GetRoughGemIconSprite("zircon", new Color(0.20f, 0.55f, 0.75f), "Carré");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.059f;
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
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("diamant", new Color(0.92f, 0.94f, 0.97f), new Color(0.99f, 0.99f, 1.00f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("diamant", new Color(0.92f, 0.94f, 0.97f), "Rond");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.isThermallyConductiveLikeDiamond = true;
                g.absorptionSpectrum = "Spectre variable selon le type ; raies dans le violet possibles (série Cape).";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 415f, widthNm = 10f },
                };
                g.inclusions = "Inclusions cristallines sombres possibles, généralement peu abondantes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction (diamagnétique).";
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
                g.icon = GetGemIconSprite("amethyste", new Color(0.45f, 0.25f, 0.65f), new Color(0.68f, 0.48f, 0.85f), "Poire", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("amethyste", new Color(0.45f, 0.25f, 0.65f), "Poire");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en bandes parallèles.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
                g.icon = GetGemIconSprite("spessartite", new Color(0.85f, 0.45f, 0.15f), new Color(0.95f, 0.65f, 0.35f), "Marquise", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("spessartite", new Color(0.85f, 0.45f, 0.15f), "Marquise");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
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
                g.magnetism = "Attiré par un aimant puissant (fer et manganèse).";
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
                g.icon = GetGemIconSprite("emeraude", new Color(0.10f, 0.45f, 0.30f), new Color(0.30f, 0.65f, 0.45f), "Rectangle à pans coupés", "Émeraude (à degrés)");
                g.roughIcon = GetRoughGemIconSprite("emeraude", new Color(0.10f, 0.45f, 0.30f), "Rectangle à pans coupés");
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
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.006f;
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
                g.magnetism = "Aucune réaction notable à l'aimant.";
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
            BuildGem("Tanzanite", g =>
            {
                g.displayName = "Tanzanite";
                g.description = "Une pierre bleu-violet, presque toujours traitée thermiquement.";
                g.icon = GetGemIconSprite("tanzanite", new Color(0.25f, 0.15f, 0.55f), new Color(0.45f, 0.35f, 0.75f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("tanzanite", new Color(0.25f, 0.15f, 0.55f), "Ovale");
                g.refractiveIndex = 1.695f;
                g.density = 3.35f;
                g.hardness = 6.5f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux";
                g.color = "Bleu-violet";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu / Violet / Rouge-brun selon les trois axes (très marqué).";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.009f;
                g.absorptionSpectrum = "Bandes d'absorption du vanadium.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 455f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 528f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 595f, widthNm = 25f },
                };
                g.inclusions = "Généralement propre, parfois fines inclusions aciculaires.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 90;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Tanzanie", rarityWeight = 100, valueMultiplier = 1.0f,
                        inclusionOverride = "Seul gisement connu au monde, situé au pied du Kilimandjaro." },
                };
            }),
            BuildGem("Iolite", g =>
            {
                g.displayName = "Iolite";
                g.description = "Une pierre bleu-violet à fort pléochroïsme, parfois appelée pierre des Vikings.";
                g.icon = GetGemIconSprite("iolite", new Color(0.30f, 0.30f, 0.60f), new Color(0.50f, 0.50f, 0.80f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("iolite", new Color(0.30f, 0.30f, 0.60f), "Ovale");
                g.refractiveIndex = 1.545f;
                g.density = 2.61f;
                g.hardness = 7.25f;
                g.lengthMm = 7.5f;
                g.widthMm = 5.8f;
                g.depthMm = 3.8f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux";
                g.color = "Bleu-violet";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu / Violet / Jaune pâle selon les trois axes (très marqué, visible à l'œil nu).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.010f;
                g.absorptionSpectrum = "Bandes d'absorption du fer.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 435f, widthNm = 15f },
                    new SpectrumLine { wavelengthNm = 585f, widthNm = 20f },
                };
                g.inclusions = "Parfois de fines paillettes orientées, donnant un effet chatoyant.";
                g.interferenceFigure = "Figure d'interférence biaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 25;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Inde", rarityWeight = 60, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 15, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Kunzite", g =>
            {
                g.displayName = "Kunzite";
                g.description = "Une pierre rose-violet pâle, sensible à la lumière prolongée (risque de décoloration).";
                g.icon = GetGemIconSprite("kunzite", new Color(0.75f, 0.45f, 0.65f), new Color(0.90f, 0.65f, 0.82f), "Rectangle à pans coupés", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("kunzite", new Color(0.75f, 0.45f, 0.65f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.665f;
                g.density = 3.18f;
                g.hardness = 6.75f;
                g.lengthMm = 9.0f;
                g.widthMm = 6.5f;
                g.depthMm = 4.2f;
                g.weightCarats = 1.6f;
                g.luster = "Vitreux";
                g.color = "Rose-violet pâle";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose intense / Incolore selon l'axe d'observation (marqué).";
                g.fluorescenceLongWave = "Fluorescence orangée à rosée possible sous UV longue onde.";
                g.fluorescenceShortWave = "Faible ou absente sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.015f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Clivage net dans une direction, fractures parallèles possibles.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 35;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afghanistan (Nuristan)", rarityWeight = 40, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Brésil", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis (Californie)", rarityWeight = 25, valueMultiplier = 1.1f,
                        inclusionOverride = "Gisement historique où cette variété a été identifiée pour la première fois." },
                };
            }),
            BuildGem("Tourmaline_Rose", g =>
            {
                g.displayName = "Tourmaline Rose";
                g.description = "Une tourmaline rose à rouge, variété appelée rubellite.";
                g.icon = GetGemIconSprite("tourmalinerose", new Color(0.80f, 0.25f, 0.45f), new Color(0.95f, 0.50f, 0.65f), "Baguette", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("tourmalinerose", new Color(0.80f, 0.25f, 0.45f), "Baguette");
                g.refractiveIndex = 1.625f;
                g.density = 3.06f;
                g.hardness = 7.25f;
                g.lengthMm = 8.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.7f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Rose à rouge";
                g.shape = "Baguette";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose clair / Rose foncé selon l'axe d'observation (faible à modéré).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.018f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Inclusions liquides fragmentées, fractures internes fréquentes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 50;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 35, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Afghanistan", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Nigeria", rarityWeight = 15, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Diopside_Chromifere", g =>
            {
                g.displayName = "Diopside Chromifère";
                g.description = "Un diopside vert intense coloré par le chrome, souvent taillé en petites pierres.";
                g.icon = GetGemIconSprite("diopside", new Color(0.15f, 0.55f, 0.20f), new Color(0.35f, 0.78f, 0.40f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("diopside", new Color(0.15f, 0.55f, 0.20f), "Rond");
                g.refractiveIndex = 1.685f;
                g.density = 3.29f;
                g.hardness = 5.75f;
                g.lengthMm = 6.0f;
                g.widthMm = 4.8f;
                g.depthMm = 3.2f;
                g.weightCarats = 0.9f;
                g.luster = "Vitreux";
                g.color = "Vert intense";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert jaunâtre / Vert bleuté selon l'axe (marqué).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.029f;
                g.absorptionSpectrum = "Bandes d'absorption du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 505f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 690f, widthNm = 15f },
                };
                g.inclusions = "Fines inclusions fibreuses, parfois structure en \"plumes\".";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Réaction rougeâtre possible (coloration au chrome).";
                g.magnetism = "Modérément attiré par un aimant puissant (silicate ferromagnésien).";
                g.baseValue = 30;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Russie (Sibérie)", rarityWeight = 50, valueMultiplier = 1.3f,
                        inclusionOverride = "Gisement sibérien classique, pierres généralement de petite taille en raison de la saturation de couleur." },
                    new GemOrigin { name = "Myanmar", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Pakistan", rarityWeight = 25, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Heliodore", g =>
            {
                g.displayName = "Héliodore";
                g.description = "Un béryl jaune doré, coloré par le fer.";
                g.icon = GetGemIconSprite("heliodore", new Color(0.85f, 0.68f, 0.15f), new Color(0.98f, 0.85f, 0.40f), "Rectangle à pans coupés", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("heliodore", new Color(0.85f, 0.68f, 0.15f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.575f;
                g.density = 2.70f;
                g.hardness = 7.75f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux";
                g.color = "Jaune doré";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Jaune doré selon l'axe (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.006f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Inclusions liquides et fissures en \"empreintes digitales\" possibles.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 25;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Namibie", rarityWeight = 25, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Ukraine", rarityWeight = 20, valueMultiplier = 1.2f },
                };
            }),
            BuildGem("Morganite", g =>
            {
                g.displayName = "Morganite";
                g.description = "Un béryl rose, coloré par le manganèse.";
                g.icon = GetGemIconSprite("morganite", new Color(0.85f, 0.60f, 0.65f), new Color(0.97f, 0.78f, 0.82f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("morganite", new Color(0.85f, 0.60f, 0.65f), "Ovale");
                g.refractiveIndex = 1.585f;
                g.density = 2.80f;
                g.hardness = 7.75f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux";
                g.color = "Rose";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose pâle / Bleu-rose selon l'axe (faible à modéré).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement propre, parfois inclusions liquides allongées.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 40;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Afghanistan", rarityWeight = 20, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis (Californie)", rarityWeight = 15, valueMultiplier = 1.2f },
                };
            }),
            BuildGem("Goshenite", g =>
            {
                g.displayName = "Goshénite";
                g.description = "Un béryl incolore, la variété la plus pure du groupe.";
                g.icon = GetGemIconSprite("goshenite", new Color(0.88f, 0.90f, 0.92f), new Color(0.98f, 0.99f, 1.00f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("goshenite", new Color(0.88f, 0.90f, 0.92f), "Rond");
                g.refractiveIndex = 1.577f;
                g.density = 2.72f;
                g.hardness = 7.75f;
                g.lengthMm = 7.5f;
                g.widthMm = 5.8f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.006f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis (Maine)", rarityWeight = 30, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement historique ayant donné son nom à cette variété." },
                    new GemOrigin { name = "Russie", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Apatite", g =>
            {
                g.displayName = "Apatite";
                g.description = "Un phosphate bleu-vert, tendre et souvent confondu avec d'autres pierres (d'où son nom, du grec \"tromper\").";
                g.icon = GetGemIconSprite("apatite", new Color(0.20f, 0.60f, 0.55f), new Color(0.40f, 0.80f, 0.75f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("apatite", new Color(0.20f, 0.60f, 0.55f), "Ovale");
                g.refractiveIndex = 1.635f;
                g.density = 3.19f;
                g.hardness = 5f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.6f;
                g.weightCarats = 1.1f;
                g.luster = "Vitreux à résineux";
                g.color = "Bleu-vert";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu / Jaune-vert selon l'axe (modéré).";
                g.fluorescenceLongWave = "Fluorescence jaune à mauve possible sous UV longue onde.";
                g.fluorescenceShortWave = "Faible ou absente sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.absorptionSpectrum = "Fines raies d'absorption dans le jaune-vert.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 512f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 490f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 465f, widthNm = 8f },
                };
                g.inclusions = "Nombreuses inclusions et fractures, pierre généralement incluse.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 20;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Myanmar", rarityWeight = 20, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Mexique", rarityWeight = 20, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Andalousite", g =>
            {
                g.displayName = "Andalousite";
                g.description = "Une pierre à très fort trichroïsme (vert, rouge, jaune-brun selon l'angle).";
                g.icon = GetGemIconSprite("andalousite", new Color(0.45f, 0.40f, 0.20f), new Color(0.65f, 0.58f, 0.35f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("andalousite", new Color(0.45f, 0.40f, 0.20f), "Coussin");
                g.refractiveIndex = 1.635f;
                g.density = 3.17f;
                g.hardness = 7.25f;
                g.lengthMm = 7.5f;
                g.widthMm = 5.8f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux";
                g.color = "Vert-brun";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert / Rouge / Jaune-brun selon les trois axes (extrêmement marqué).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.010f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Inclusions carbonées possibles.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 30;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Canada", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Chrysoberyl", g =>
            {
                g.displayName = "Chrysobéryl";
                g.description = "Une pierre jaune-vert extrêmement dure, troisième minéral le plus dur après le corindon et le diamant.";
                g.icon = GetGemIconSprite("chrysoberyl", new Color(0.65f, 0.65f, 0.20f), new Color(0.82f, 0.82f, 0.45f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("chrysoberyl", new Color(0.65f, 0.65f, 0.20f), "Ovale");
                g.refractiveIndex = 1.745f;
                g.density = 3.73f;
                g.hardness = 8.5f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.7f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Jaune-vert";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune / Vert pâle selon l'axe (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.009f;
                g.absorptionSpectrum = "Fine raie d'absorption dans le bleu.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 445f, widthNm = 8f },
                };
                g.inclusions = "Cristaux maclés en \"roue\", fines inclusions liquides.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 45;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 35, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Chrysoberyl_Alexandrite", g =>
            {
                g.displayName = "Chrysobéryl Alexandrite";
                g.description = "La variété à changement de couleur du chrysobéryl : vert sous lumière du jour, rouge sous éclairage incandescent.";
                g.icon = GetGemIconSprite("alexandrite", new Color(0.15f, 0.45f, 0.35f), new Color(0.55f, 0.20f, 0.30f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("alexandrite", new Color(0.15f, 0.45f, 0.35f), "Coussin");
                g.refractiveIndex = 1.745f;
                g.density = 3.73f;
                g.hardness = 8.5f;
                g.lengthMm = 6.0f;
                g.widthMm = 4.8f;
                g.depthMm = 3.2f;
                g.weightCarats = 0.8f;
                g.luster = "Vitreux";
                g.color = "Vert à rouge (changement de couleur)";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert / Orange / Rouge selon les trois axes (très marqué).";
                g.fluorescenceLongWave = "Fluorescence rouge faible possible sous UV longue onde.";
                g.fluorescenceShortWave = "Faible ou absente sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.009f;
                g.absorptionSpectrum = "Raies d'absorption du chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 680f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 660f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 630f, widthNm = 15f },
                };
                g.inclusions = "Cristaux maclés, fines inclusions en \"empreintes digitales\".";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Réaction rouge marquée sous filtre Chelsea (due au chrome).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 250;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Russie (Monts Oural)", rarityWeight = 20, valueMultiplier = 3.0f,
                        inclusionOverride = "Gisement historique originel, matériau exceptionnel aujourd'hui quasiment épuisé." },
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 35, valueMultiplier = 1.3f },
                    new GemOrigin { name = "Brésil", rarityWeight = 25, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Inde", rarityWeight = 20, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("CZ", g =>
            {
                g.displayName = "CZ";
                g.description = "Zircone cubique : un simulant synthétique du diamant, bon marché mais très différent physiquement.";
                g.icon = GetGemIconSprite("cz", new Color(0.85f, 0.90f, 0.95f), new Color(0.95f, 0.97f, 1.00f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("cz", new Color(0.85f, 0.90f, 0.95f), "Rond");
                g.refractiveIndex = 2.16f;
                g.density = 5.8f;
                g.hardness = 8.25f;
                g.lengthMm = 6.0f;
                g.widthMm = 6.0f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.2f;
                g.luster = "Adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence variable ou absente.";
                g.fluorescenceShortWave = "Fluorescence variable ou absente.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, poussières de polissage possibles en surface.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 8;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Moissanite_Synthetique", g =>
            {
                g.displayName = "Moissanite Synthétique";
                g.description = "Carbure de silicium synthétique, un autre simulant du diamant, mais biréfringent contrairement à celui-ci.";
                g.icon = GetGemIconSprite("moissanite", new Color(0.88f, 0.88f, 0.80f), new Color(0.97f, 0.97f, 0.90f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("moissanite", new Color(0.88f, 0.88f, 0.80f), "Rond");
                g.refractiveIndex = 2.67f;
                g.density = 3.21f;
                g.hardness = 9.25f;
                g.lengthMm = 5.8f;
                g.widthMm = 5.8f;
                g.depthMm = 3.6f;
                g.weightCarats = 1.0f;
                g.luster = "Adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV.";
                g.fluorescenceShortWave = "Généralement inerte sous UV.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent) — dédoublement des arêtes parfois visible à fort grossissement.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.043f;
                g.isThermallyConductiveLikeDiamond = true;
                g.isElectricallyConductive = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, inclusions métalliques rares.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 10;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Fluorite", g =>
            {
                g.displayName = "Fluorite";
                g.description = "Un minéral tendre et coloré, dont le nom a donné le mot \"fluorescence\".";
                g.icon = GetGemIconSprite("fluorite", new Color(0.55f, 0.30f, 0.70f), new Color(0.72f, 0.50f, 0.85f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("fluorite", new Color(0.55f, 0.30f, 0.70f), "Ovale");
                g.refractiveIndex = 1.434f;
                g.density = 3.18f;
                g.hardness = 4f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.7f;
                g.weightCarats = 1.1f;
                g.luster = "Vitreux";
                g.color = "Violet";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence bleue à violette souvent intense sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence variable sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Clivage parfait dans quatre directions, fractures internes fréquentes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Chine", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Mexique", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Angleterre", rarityWeight = 20, valueMultiplier = 1.3f,
                        inclusionOverride = "Gisement historique connu pour ses bandes de couleur multicolores." },
                    new GemOrigin { name = "États-Unis", rarityWeight = 15, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Grenat_Demantoide", g =>
            {
                g.displayName = "Grenat Démantoïde";
                g.description = "Un grenat vert à très fort éclat, l'un des plus prisés de la famille des grenats.";
                g.icon = GetGemIconSprite("demantoide", new Color(0.15f, 0.50f, 0.25f), new Color(0.35f, 0.72f, 0.42f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("demantoide", new Color(0.15f, 0.50f, 0.25f), "Rond");
                g.refractiveIndex = 1.885f;
                g.density = 3.85f;
                g.hardness = 6.75f;
                g.lengthMm = 5.5f;
                g.widthMm = 4.5f;
                g.depthMm = 3.0f;
                g.weightCarats = 0.8f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Vert";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV.";
                g.fluorescenceShortWave = "Généralement inerte sous UV.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Forte absorption dans le violet et le bleu, transparence dans le vert.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 440f, widthNm = 45f },
                };
                g.inclusions = "Inclusions fibreuses radiales caractéristiques possibles.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Réaction rougeâtre possible si coloré au chrome.";
                g.magnetism = "Attiré par un aimant puissant (grenat riche en fer).";
                g.baseValue = 150;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Russie (Monts Oural)", rarityWeight = 30, valueMultiplier = 1.8f,
                        inclusionOverride = "Célèbre pour ses inclusions fibreuses radiales en \"queue de cheval\", très recherchées." },
                    new GemOrigin { name = "Namibie", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 30, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Hematite", g =>
            {
                g.displayName = "Hématite";
                g.description = "Un oxyde de fer opaque à éclat métallique, souvent taillé en cabochon plutôt qu'à facettes.";
                g.icon = GetGemIconSprite("hematite", new Color(0.25f, 0.22f, 0.22f), new Color(0.42f, 0.38f, 0.38f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("hematite", new Color(0.25f, 0.22f, 0.22f), "Ovale");
                g.refractiveIndex = 2.94f;
                g.density = 5.15f;
                g.hardness = 6f;
                g.lengthMm = 6.0f;
                g.widthMm = 6.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 2.0f;
                g.luster = "Métallique";
                g.color = "Gris-noir métallique";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence (minéral opaque).";
                g.fluorescenceShortWave = "Aucune fluorescence (minéral opaque).";
                g.opticalCharacter = "Non applicable (minéral opaque à éclat métallique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Opaque;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Non applicable (minéral opaque).";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Non applicable (minéral opaque).";
                g.interferenceFigure = "Non applicable (minéral opaque).";
                g.chelseaFilterReaction = "Non applicable (minéral opaque).";
                g.magnetism = "Légèrement attiré par un aimant puissant (oxyde de fer, réaction bien plus faible qu'un minéral franchement ferromagnétique).";
                g.baseValue = 10;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Angleterre", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Brésil", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Divers gisements mondiaux", rarityWeight = 35, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Pezzottaite", g =>
            {
                g.displayName = "Pezzottaite";
                g.description = "Un béryl au césium, de couleur \"framboise\", découvert seulement en 2002 à Madagascar.";
                g.icon = GetGemIconSprite("pezzottaite", new Color(0.75f, 0.20f, 0.35f), new Color(0.90f, 0.42f, 0.55f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("pezzottaite", new Color(0.75f, 0.20f, 0.35f), "Ovale");
                g.refractiveIndex = 1.615f;
                g.density = 3.13f;
                g.hardness = 8f;
                g.lengthMm = 6.5f;
                g.widthMm = 5.0f;
                g.depthMm = 3.4f;
                g.weightCarats = 1.0f;
                g.luster = "Vitreux";
                g.color = "Rose-rouge framboise";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose pâle / Rouge-violet selon l'axe (modéré).";
                g.fluorescenceLongWave = "Aucune fluorescence notable.";
                g.fluorescenceShortWave = "Aucune fluorescence notable.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Cristaux maclés en étoile à trois branches, structure de croissance particulière.";
                g.interferenceFigure = "Figure d'interférence uniaxiale.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 60;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Madagascar", rarityWeight = 70, valueMultiplier = 1.3f,
                        inclusionOverride = "Gisement d'origine, découvert récemment dans les années 2000." },
                    new GemOrigin { name = "Afghanistan", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Pyrite", g =>
            {
                g.displayName = "Pyrite";
                g.description = "Sulfure de fer opaque à éclat métallique doré, surnommé \"or des fous\".";
                g.icon = GetGemIconSprite("pyrite", new Color(0.72f, 0.62f, 0.20f), new Color(0.88f, 0.78f, 0.40f), "Rond", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("pyrite", new Color(0.72f, 0.62f, 0.20f), "Rond");
                g.refractiveIndex = 1.85f;
                g.density = 5.0f;
                g.hardness = 6.25f;
                g.lengthMm = 6.0f;
                g.widthMm = 6.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 1.8f;
                g.luster = "Métallique";
                g.color = "Jaune doré métallique";
                g.shape = "Rond";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence (minéral opaque).";
                g.fluorescenceShortWave = "Aucune fluorescence (minéral opaque).";
                g.opticalCharacter = "Non applicable (minéral opaque à éclat métallique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Opaque;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Non applicable (minéral opaque).";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Non applicable (minéral opaque).";
                g.interferenceFigure = "Non applicable (minéral opaque).";
                g.chelseaFilterReaction = "Non applicable (minéral opaque).";
                g.magnetism = "Aucune réaction notable à l'aimant, malgré sa teneur en fer.";
                g.baseValue = 8;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Pérou", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Espagne", rarityWeight = 35, valueMultiplier = 1.1f },
                    new GemOrigin { name = "États-Unis", rarityWeight = 30, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Corindon_Synthetique_Rubis", g =>
            {
                g.displayName = "Corindon Synthétique (Rubis)";
                g.description = "Corindon rouge obtenu par fusion de poudre d'alumine ; identique en apparence à un rubis, sa production remonte au début du XXe siècle.";
                g.icon = GetGemIconSprite("corindonsynthrubis", new Color(0.70f, 0.15f, 0.15f), new Color(0.88f, 0.40f, 0.35f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("corindonsynthrubis", new Color(0.70f, 0.15f, 0.15f), "Rond");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 6.8f;
                g.widthMm = 5.9f;
                g.depthMm = 4.2f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Rouge";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rouge / Orangé-rouge selon l'axe d'observation.";
                g.fluorescenceLongWave = "Fluorescence rouge vif et uniforme sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence rouge modérée sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Raies d'absorption du chrome visibles dans le rouge.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 694f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 668f, widthNm = 8f },
                    new SpectrumLine { wavelengthNm = 620f, widthNm = 12f },
                    new SpectrumLine { wavelengthNm = 560f, widthNm = 40f },
                };
                g.inclusions = "Bulles de gaz sphériques ou en forme de virgule, parfois alignées ; stries de croissance courbes et concentriques.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Réaction rouge vif sous filtre Chelsea (due au chrome).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 4;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle (fusion de poudre)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Corindon_Synthetique_Saphir", g =>
            {
                g.displayName = "Corindon Synthétique (Saphir)";
                g.description = "Corindon bleu obtenu par fusion de poudre d'alumine et d'oxydes colorants ; longtemps utilisé en horlogerie et bijouterie bon marché.";
                g.icon = GetGemIconSprite("corindonsynthsaphir", new Color(0.12f, 0.22f, 0.60f), new Color(0.35f, 0.48f, 0.82f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("corindonsynthsaphir", new Color(0.12f, 0.22f, 0.60f), "Ovale");
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
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Bleu / Bleu-violacé selon l'axe d'observation.";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Bande d'absorption large et diffuse.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Bulles de gaz sphériques, stries de croissance courbes et concentriques, absence de toute inclusion minérale naturelle.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 4;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle (fusion de poudre)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Saphir_Jaune", g =>
            {
                g.displayName = "Saphir Jaune";
                g.description = "Corindon jaune, couleur due à des centres colorés ou à des traces de fer.";
                g.icon = GetGemIconSprite("saphirjaune", new Color(0.80f, 0.68f, 0.12f), new Color(0.95f, 0.85f, 0.40f), "Rectangle à pans coupés", "Émeraude");
                g.roughIcon = GetRoughGemIconSprite("saphirjaune", new Color(0.80f, 0.68f, 0.12f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 7.5f;
                g.widthMm = 5.8f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Jaune";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Jaune plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Bande d'absorption diffuse dans le bleu-violet.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 450f, widthNm = 40f },
                };
                g.inclusions = "Zonation de couleur en bandes hexagonales, cristaux fins dispersés.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 55;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 45, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Australie", rarityWeight = 30, valueMultiplier = 0.8f },
                    new GemOrigin { name = "Thaïlande", rarityWeight = 25, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Saphir_Rose", g =>
            {
                g.displayName = "Saphir Rose";
                g.description = "Corindon rose, coloré par de faibles traces de chrome, à la frontière stylistique avec le rubis.";
                g.icon = GetGemIconSprite("saphirrose", new Color(0.80f, 0.35f, 0.50f), new Color(0.95f, 0.62f, 0.74f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("saphirrose", new Color(0.80f, 0.35f, 0.50f), "Coussin");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 6.5f;
                g.widthMm = 6.0f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Rose";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose clair / Rose plus soutenu selon l'axe (modéré).";
                g.fluorescenceLongWave = "Fluorescence rouge à rose faible à modérée sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Raies d'absorption fines dans le vert, dues à un élément chromophore en faible concentration.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 694f, widthNm = 10f },
                    new SpectrumLine { wavelengthNm = 620f, widthNm = 14f },
                };
                g.inclusions = "Soie de rutile fine, légère zonation de couleur.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Réaction rougeâtre faible à modérée possible.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 110;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 40, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 25, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Saphir_Violet", g =>
            {
                g.displayName = "Saphir Violet";
                g.description = "Corindon violet, couleur résultant de la combinaison de plusieurs éléments traces.";
                g.icon = GetGemIconSprite("saphirviolet", new Color(0.45f, 0.25f, 0.65f), new Color(0.66f, 0.46f, 0.83f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("saphirviolet", new Color(0.45f, 0.25f, 0.65f), "Ovale");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.8f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Violet";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Violet-bleu / Violet-rouge selon l'axe d'observation (modéré).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Bandes d'absorption combinées, larges et diffuses.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en secteurs, cristaux fins dispersés.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction ou réaction rougeâtre très faible.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 65;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 50, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 50, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Saphir_Orange", g =>
            {
                g.displayName = "Saphir Orange";
                g.description = "Corindon orange, teinte rare obtenue par un équilibre particulier entre plusieurs éléments traces.";
                g.icon = GetGemIconSprite("saphirorange", new Color(0.85f, 0.45f, 0.12f), new Color(0.97f, 0.66f, 0.35f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("saphirorange", new Color(0.85f, 0.45f, 0.12f), "Rond");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 6.5f;
                g.widthMm = 6.0f;
                g.depthMm = 3.9f;
                g.weightCarats = 1.2f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Orange";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Orange clair / Orangé plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Fluorescence orangée faible possible sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Bande d'absorption diffuse dans le bleu-violet.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en bandes, cristaux fins dispersés.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction ou réaction rougeâtre très faible.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 90;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 50, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 50, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Padparadscha", g =>
            {
                g.displayName = "Padparadscha";
                g.description = "Corindon rose-orangé évoquant la fleur de lotus, l'une des variétés de corindon les plus recherchées et les plus chères au monde.";
                g.icon = GetGemIconSprite("padparadscha", new Color(0.90f, 0.50f, 0.38f), new Color(0.98f, 0.72f, 0.58f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("padparadscha", new Color(0.90f, 0.50f, 0.38f), "Coussin");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 6.0f;
                g.widthMm = 5.8f;
                g.depthMm = 3.8f;
                g.weightCarats = 1.1f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Rose-orangé";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose / Orangé selon l'axe d'observation (modéré).";
                g.fluorescenceLongWave = "Fluorescence orangée à rougeâtre faible à modérée sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Raies d'absorption fines combinées à une bande diffuse.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 620f, widthNm = 12f },
                    new SpectrumLine { wavelengthNm = 450f, widthNm = 30f },
                };
                g.inclusions = "Soie de rutile fine, zonation de couleur subtile en bandes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Réaction rougeâtre faible possible.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 320;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 60, valueMultiplier = 2.2f,
                        inclusionOverride = "Couleur rose-orangé équilibrée et homogène, considérée comme la référence historique pour cette teinte." },
                    new GemOrigin { name = "Madagascar", rarityWeight = 40, valueMultiplier = 1.6f },
                };
            }),
            BuildGem("Saphir_Vert", g =>
            {
                g.displayName = "Saphir Vert";
                g.description = "Corindon vert, souvent formé de fines bandes alternées bleu/jaune donnant une teinte verte à l'œil nu.";
                g.icon = GetGemIconSprite("saphirvert", new Color(0.15f, 0.50f, 0.30f), new Color(0.35f, 0.72f, 0.48f), "Rectangle à pans coupés", "Émeraude");
                g.roughIcon = GetRoughGemIconSprite("saphirvert", new Color(0.15f, 0.50f, 0.30f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 7.5f;
                g.widthMm = 6.2f;
                g.depthMm = 4.2f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Vert";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert / Jaune-vert selon l'axe d'observation (marqué).";
                g.fluorescenceLongWave = "Inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Bandes d'absorption combinées dans le bleu et le jaune.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 460f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 480f, widthNm = 20f },
                };
                g.inclusions = "Zonation de couleur marquée en bandes fines alternées, fissures internes fréquentes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 40;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Australie", rarityWeight = 50, valueMultiplier = 0.8f },
                    new GemOrigin { name = "Thaïlande", rarityWeight = 30, valueMultiplier = 0.8f },
                    new GemOrigin { name = "Nigeria", rarityWeight = 20, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Saphir_Incolore", g =>
            {
                g.displayName = "Saphir Incolore";
                g.description = "Corindon dépourvu d'éléments traces colorants ; historiquement utilisé comme substitut bon marché du diamant.";
                g.icon = GetGemIconSprite("saphirincolore", new Color(0.88f, 0.90f, 0.93f), new Color(0.98f, 0.99f, 1.00f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("saphirincolore", new Color(0.88f, 0.90f, 0.93f), "Rond");
                g.refractiveIndex = 1.766f;
                g.density = 4.00f;
                g.hardness = 9f;
                g.lengthMm = 5.8f;
                g.widthMm = 5.8f;
                g.depthMm = 3.6f;
                g.weightCarats = 1.0f;
                g.luster = "Vitreux à sub-adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent) — dédoublement des arêtes parfois visible à fort grossissement.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, quelques cristaux fins dispersés possibles.";
                g.interferenceFigure = "Figure d'interférence uniaxiale nette.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 22;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 60, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Australie", rarityWeight = 40, valueMultiplier = 0.8f },
                };
            }),
            BuildGem("YAG", g =>
            {
                g.displayName = "YAG";
                g.description = "Grenat d'yttrium-aluminium synthétique, un des premiers simulants de diamant utilisés en joaillerie avant l'arrivée de la zircone cubique.";
                g.icon = GetGemIconSprite("yag", new Color(0.85f, 0.87f, 0.90f), new Color(0.96f, 0.97f, 0.99f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("yag", new Color(0.85f, 0.87f, 0.90f), "Rond");
                g.refractiveIndex = 1.833f;
                g.density = 4.57f;
                g.hardness = 8.25f;
                g.lengthMm = 5.8f;
                g.widthMm = 5.8f;
                g.depthMm = 3.6f;
                g.weightCarats = 1.1f;
                g.luster = "Adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence variable ou absente.";
                g.fluorescenceShortWave = "Fluorescence variable ou absente.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, bulles de gaz rondes occasionnelles.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 5;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("GGG", g =>
            {
                g.displayName = "GGG";
                g.description = "Grenat de gadolinium-gallium synthétique, simulant de diamant à très forte densité mais relativement tendre, aujourd'hui rare en joaillerie.";
                g.icon = GetGemIconSprite("ggg", new Color(0.80f, 0.82f, 0.86f), new Color(0.92f, 0.94f, 0.97f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("ggg", new Color(0.80f, 0.82f, 0.86f), "Rond");
                g.refractiveIndex = 1.97f;
                g.density = 7.05f;
                g.hardness = 6.5f;
                g.lengthMm = 5.6f;
                g.widthMm = 5.6f;
                g.depthMm = 3.5f;
                g.weightCarats = 1.4f;
                g.luster = "Adamantin";
                g.color = "Incolore";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence variable, parfois orangée faible.";
                g.fluorescenceShortWave = "Fluorescence variable ou absente.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre, rares bulles de gaz rondes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 5;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Grenat_Malaya", g =>
            {
                g.displayName = "Grenat Malaya";
                g.description = "Grenat de couleur orange à rose-orangé, intermédiaire entre le pyrope et la spessartite, longtemps considéré comme un rebut avant d'être reconnu comme variété à part entière.";
                g.icon = GetGemIconSprite("grenatmalaya", new Color(0.85f, 0.45f, 0.30f), new Color(0.95f, 0.66f, 0.50f), "Coussin", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("grenatmalaya", new Color(0.85f, 0.45f, 0.30f), "Coussin");
                g.refractiveIndex = 1.755f;
                g.density = 3.84f;
                g.hardness = 7.25f;
                g.lengthMm = 7.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux";
                g.color = "Orange à rose-orangé";
                g.shape = "Coussin";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Bandes d'absorption du fer et du manganèse combinées.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 520f, widthNm = 20f },
                    new SpectrumLine { wavelengthNm = 575f, widthNm = 20f },
                };
                g.inclusions = "Cristaux d'apatite ou de zircon entourés de halos, fissures internes occasionnelles.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Modérément attiré par un aimant puissant (teneur en fer intermédiaire).";
                g.baseValue = 45;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Tanzanie", rarityWeight = 60, valueMultiplier = 1.3f },
                    new GemOrigin { name = "Kenya", rarityWeight = 40, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Grenat_Rhodolite", g =>
            {
                g.displayName = "Grenat Rhodolite";
                g.description = "Grenat rouge-violacé (\"framboise\"), intermédiaire entre le pyrope et l'almandin.";
                g.icon = GetGemIconSprite("grenatrhodolite", new Color(0.65f, 0.20f, 0.35f), new Color(0.83f, 0.42f, 0.55f), "Poire", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("grenatrhodolite", new Color(0.65f, 0.20f, 0.35f), "Poire");
                g.refractiveIndex = 1.76f;
                g.density = 3.84f;
                g.hardness = 7.25f;
                g.lengthMm = 7.5f;
                g.widthMm = 6.2f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux";
                g.color = "Rouge-violacé";
                g.shape = "Poire";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.absorptionSpectrum = "Larges bandes d'absorption du fer.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 573f, widthNm = 16f },
                    new SpectrumLine { wavelengthNm = 526f, widthNm = 16f },
                };
                g.inclusions = "Cristaux d'apatite ou de zircon entourés de halos, fissures internes fréquentes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Attiré par un aimant puissant (teneur en fer intermédiaire).";
                g.baseValue = 38;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 35, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Inde", rarityWeight = 30, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Citrine", g =>
            {
                g.displayName = "Citrine";
                g.description = "Quartz jaune à orangé, dont la grande majorité du matériel commercial provient d'un traitement thermique de l'améthyste.";
                g.icon = GetGemIconSprite("citrine", new Color(0.85f, 0.55f, 0.12f), new Color(0.97f, 0.76f, 0.35f), "Rectangle à pans coupés", "Émeraude");
                g.roughIcon = GetRoughGemIconSprite("citrine", new Color(0.85f, 0.55f, 0.12f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 9.0f;
                g.widthMm = 6.5f;
                g.depthMm = 4.2f;
                g.weightCarats = 1.8f;
                g.luster = "Vitreux";
                g.color = "Jaune à orangé";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Jaune plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en bandes parallèles, voiles de fines fissures occasionnels.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 50, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Zambie", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 20, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ametrine", g =>
            {
                g.displayName = "Amétrine";
                g.description = "Quartz bicolore montrant à la fois les teintes violette de l'améthyste et jaune-orangé de la citrine dans un même cristal, provenant presque exclusivement d'un unique gisement bolivien.";
                g.icon = GetGemIconSprite("ametrine", new Color(0.50f, 0.35f, 0.55f), new Color(0.85f, 0.65f, 0.30f), "Rectangle à pans coupés", "Émeraude");
                g.roughIcon = GetRoughGemIconSprite("ametrine", new Color(0.50f, 0.35f, 0.55f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 8.5f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.6f;
                g.luster = "Vitreux";
                g.color = "Violet et jaune (bicolore)";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Violet / Jaune selon la zone et l'axe d'observation (variable).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur nette en secteurs, limite de transition parfois visible entre les deux teintes.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 35;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Bolivie", rarityWeight = 90, valueMultiplier = 1.6f,
                        inclusionOverride = "Zonation bicolore nette et bien délimitée, caractéristique d'un gisement unique en son genre." },
                    new GemOrigin { name = "Autre gisement", rarityWeight = 10, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Quartz_Fume", g =>
            {
                g.displayName = "Quartz Fumé";
                g.description = "Quartz brun-gris, coloré par des centres colorés liés à l'aluminium activés par une irradiation naturelle de fond.";
                g.icon = GetGemIconSprite("quartzfume", new Color(0.35f, 0.28f, 0.22f), new Color(0.53f, 0.43f, 0.33f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("quartzfume", new Color(0.35f, 0.28f, 0.22f), "Rond");
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 9.5f;
                g.widthMm = 7.0f;
                g.depthMm = 4.5f;
                g.weightCarats = 2.0f;
                g.luster = "Vitreux";
                g.color = "Brun-gris";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Brun clair / Brun plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur diffuse, voiles de fines fissures occasionnels.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 10;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Écosse", rarityWeight = 30, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Suisse", rarityWeight = 30, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Prasiolite", g =>
            {
                g.displayName = "Prasiolite";
                g.description = "Quartz vert, rare à l'état naturel, la majeure partie du matériel commercial étant obtenue par traitement thermique d'améthyste.";
                g.icon = GetGemIconSprite("prasiolite", new Color(0.35f, 0.60f, 0.35f), new Color(0.56f, 0.80f, 0.56f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("prasiolite", new Color(0.35f, 0.60f, 0.35f), "Rond");
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux";
                g.color = "Vert";
                g.shape = "Rond";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert pâle / Vert plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zonation de couleur en bandes parallèles.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 18;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 70, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Zambie", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Quartz_Rose", g =>
            {
                g.displayName = "Quartz Rose";
                g.description = "Quartz rose, souvent riche en inclusions fibreuses de rutile pouvant produire un effet d'astérisme.";
                g.icon = GetGemIconSprite("quartzrose", new Color(0.85f, 0.55f, 0.60f), new Color(0.96f, 0.76f, 0.79f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("quartzrose", new Color(0.85f, 0.55f, 0.60f), "Coussin");
                g.refractiveIndex = 1.544f;
                g.density = 2.65f;
                g.hardness = 7f;
                g.lengthMm = 8.5f;
                g.widthMm = 7.0f;
                g.depthMm = 4.5f;
                g.weightCarats = 1.9f;
                g.luster = "Vitreux à légèrement gras";
                g.color = "Rose";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose pâle / Rose plus soutenu selon l'axe (très faible).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.009f;
                g.isQuartzFamily = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Fines inclusions fibreuses abondantes, voiles denses donnant un aspect laiteux.";
                g.interferenceFigure = "Figure d'interférence uniaxiale visible en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 10;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Brésil", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis (Dakota du Sud)", rarityWeight = 30, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Zoisite", g =>
            {
                g.displayName = "Zoïsite";
                g.description = "Variété de zoïsite verte à brunâtre, chimiquement apparentée à la tanzanite mais dépourvue de son célèbre coloris bleu-violet.";
                g.icon = GetGemIconSprite("zoisite", new Color(0.25f, 0.45f, 0.30f), new Color(0.46f, 0.66f, 0.49f), "Rectangle à pans coupés", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("zoisite", new Color(0.25f, 0.45f, 0.30f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.70f;
                g.density = 3.35f;
                g.hardness = 6.5f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.5f;
                g.depthMm = 4.5f;
                g.weightCarats = 1.6f;
                g.luster = "Vitreux à résineux";
                g.color = "Vert à brunâtre";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Vert / Brun-jaune selon l'axe d'observation (marqué).";
                g.fluorescenceLongWave = "Aucune fluorescence notable sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence notable sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.009f;
                g.absorptionSpectrum = "Bandes d'absorption diffuses du fer et du chrome combinées.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Cristaux fins dispersés, parfois accompagnés d'inclusions fibreuses.";
                g.interferenceFigure = "Figure d'interférence biaxiale.";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Tanzanie", rarityWeight = 50, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Norvège", rarityWeight = 30, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Autriche", rarityWeight = 20, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Jade_Jadeite", g =>
            {
                g.displayName = "Jade Jadéite";
                g.description = "Silicate de sodium et d'aluminium du groupe des pyroxènes, la plus prisée des deux variétés de \"jade\".";
                g.icon = GetGemIconSprite("jadejadeite", new Color(0.20f, 0.55f, 0.30f), new Color(0.45f, 0.75f, 0.50f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("jadejadeite", new Color(0.20f, 0.55f, 0.30f), "Coussin");
                g.refractiveIndex = 1.665f;
                g.density = 3.34f;
                g.hardness = 6.75f;
                g.lengthMm = 9.0f;
                g.widthMm = 7.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 3.5f;
                g.luster = "Vitreux à gras";
                g.color = "Vert";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent), lecture diffuse typique d'un agrégat polycristallin.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.012f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Raie d'absorption fine dans le bleu-violet liée à un élément chromophore en trace.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 437f, widthNm = 6f },
                };
                g.inclusions = "Texture grenue à fibreuse entrelacée typique d'un agrégat cristallin compact.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat polycristallin).";
                g.chelseaFilterReaction = "Généralement aucune réaction (sauf coloration artificielle).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 120;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Myanmar", rarityWeight = 60, valueMultiplier = 1.6f,
                        inclusionOverride = "Texture fine et compacte, couleur verte homogène et intense — qualité \"impériale\" recherchée." },
                    new GemOrigin { name = "Guatemala", rarityWeight = 40, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Jade_Nephrite", g =>
            {
                g.displayName = "Jade Néphrite";
                g.description = "Silicate de calcium et de magnésium du groupe des amphiboles (série trémolite-actinote), la variété de \"jade\" la plus répandue.";
                g.icon = GetGemIconSprite("jadenephrite", new Color(0.25f, 0.45f, 0.25f), new Color(0.45f, 0.62f, 0.40f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("jadenephrite", new Color(0.25f, 0.45f, 0.25f), "Coussin");
                g.refractiveIndex = 1.615f;
                g.density = 2.95f;
                g.hardness = 6.25f;
                g.lengthMm = 9.5f;
                g.widthMm = 7.5f;
                g.depthMm = 5.0f;
                g.weightCarats = 4.0f;
                g.luster = "Vitreux à soyeux";
                g.color = "Vert sombre";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent), lecture diffuse typique d'un agrégat polycristallin.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.027f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Bande d'absorption large liée au fer.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Structure feutrée de fibres entrelacées, conférant une ténacité exceptionnelle à l'agrégat.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat polycristallin).";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 40;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Chine", rarityWeight = 35, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Nouvelle-Zélande", rarityWeight = 25, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Canada", rarityWeight = 25, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Russie", rarityWeight = 15, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Grenat_Hydrogrossulaire", g =>
            {
                g.displayName = "Grenat Hydrogrossulaire";
                g.description = "Variété massive et hydratée de grenat grossulaire, parfois surnommée \"jade du Transvaal\" en raison de sa ressemblance avec le jade.";
                g.icon = GetGemIconSprite("hydrogrossulaire", new Color(0.35f, 0.55f, 0.35f), new Color(0.55f, 0.72f, 0.50f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("hydrogrossulaire", new Color(0.35f, 0.55f, 0.35f), "Coussin");
                g.refractiveIndex = 1.720f;
                g.density = 3.47f;
                g.hardness = 7.0f;
                g.lengthMm = 8.5f;
                g.widthMm = 7.0f;
                g.depthMm = 4.5f;
                g.weightCarats = 2.8f;
                g.luster = "Vitreux à sub-vitreux";
                g.color = "Vert";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Bande d'absorption diffuse liée au chrome ou au fer selon la teinte.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Petits cristaux noirs opaques (oxydes) dispersés dans la masse, texture granuleuse compacte.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 18;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afrique du Sud", rarityWeight = 55, valueMultiplier = 1.1f,
                        inclusionOverride = "Gisement historique du Transvaal, teinte verte homogène caractéristique." },
                    new GemOrigin { name = "Canada", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Myanmar", rarityWeight = 20, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Sinhalite", g =>
            {
                g.displayName = "Sinhalite";
                g.description = "Borate de magnésium et d'aluminium, longtemps confondu avec le péridot brun avant d'être reconnu comme espèce distincte en 1952.";
                g.icon = GetGemIconSprite("sinhalite", new Color(0.45f, 0.30f, 0.15f), new Color(0.65f, 0.48f, 0.28f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("sinhalite", new Color(0.45f, 0.30f, 0.15f), "Ovale");
                g.refractiveIndex = 1.68f;
                g.density = 3.48f;
                g.hardness = 6.75f;
                g.lengthMm = 7.0f;
                g.widthMm = 5.5f;
                g.depthMm = 3.8f;
                g.weightCarats = 1.3f;
                g.luster = "Vitreux";
                g.color = "Brun";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Brun clair / Brun-rouge sombre selon les trois axes (marqué).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.038f;
                g.absorptionSpectrum = "Bandes d'absorption fines liées au fer.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement peu incluse, cristaux fins occasionnels.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 55;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 70, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement type qui a donné son nom à l'espèce." },
                    new GemOrigin { name = "Myanmar", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Scapolite", g =>
            {
                g.displayName = "Scapolite";
                g.description = "Silicate d'aluminium de la série marialite-méionite, au port cristallin allongé et prismatique caractéristique.";
                g.icon = GetGemIconSprite("scapolite", new Color(0.85f, 0.75f, 0.25f), new Color(0.96f, 0.88f, 0.50f), "Baguette", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("scapolite", new Color(0.85f, 0.75f, 0.25f), "Baguette");
                g.refractiveIndex = 1.555f;
                g.density = 2.70f;
                g.hardness = 6.25f;
                g.lengthMm = 10.0f;
                g.widthMm = 5.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.6f;
                g.luster = "Vitreux à résineux";
                g.color = "Jaune";
                g.shape = "Baguette";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Jaune plus soutenu selon l'axe (modéré).";
                g.fluorescenceLongWave = "Fluorescence orangée à rosée possible sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible possible sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.021f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Inclusions fibreuses ou tubulaires parallèles à l'axe d'allongement du cristal.";
                g.interferenceFigure = "Figure uniaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 30;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Myanmar", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Tanzanie", rarityWeight = 35, valueMultiplier = 1.1f },
                    new GemOrigin { name = "Madagascar", rarityWeight = 25, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Pierre_De_Lune", g =>
            {
                g.displayName = "Pierre de Lune";
                g.description = "Feldspar alcalin montrant un phénomène de lumière bleutée et flottante (adularescence) dû à l'intercroissance de deux minéraux.";
                g.icon = GetGemIconSprite("pierredelune", new Color(0.80f, 0.85f, 0.90f), new Color(0.55f, 0.70f, 0.90f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("pierredelune", new Color(0.80f, 0.85f, 0.90f), "Ovale");
                g.refractiveIndex = 1.525f;
                g.density = 2.57f;
                g.hardness = 6.25f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.5f;
                g.depthMm = 4.5f;
                g.weightCarats = 1.8f;
                g.luster = "Vitreux à adamantin par endroits";
                g.color = "Incolore à reflets bleutés";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.006f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Structure lamellaire fine en intercroissance, responsable du flotté lumineux bleuté caractéristique.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 25;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sri Lanka", rarityWeight = 50, valueMultiplier = 1.2f,
                        inclusionOverride = "Flotté bleuté intense et bien centré, qualité de référence." },
                    new GemOrigin { name = "Inde", rarityWeight = 30, valueMultiplier = 0.9f },
                    new GemOrigin { name = "Myanmar", rarityWeight = 20, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Orthose", g =>
            {
                g.displayName = "Orthose";
                g.description = "Feldspath potassique, dont la variété gemme jaune limpide est principalement extraite à Madagascar.";
                g.icon = GetGemIconSprite("orthose", new Color(0.85f, 0.72f, 0.20f), new Color(0.96f, 0.85f, 0.45f), "Rectangle à pans coupés", "Émeraude");
                g.roughIcon = GetRoughGemIconSprite("orthose", new Color(0.85f, 0.72f, 0.20f), "Rectangle à pans coupés");
                g.refractiveIndex = 1.522f;
                g.density = 2.56f;
                g.hardness = 6.25f;
                g.lengthMm = 8.0f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux";
                g.color = "Jaune";
                g.shape = "Rectangle à pans coupés";
                g.cutStyle = "Émeraude";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Jaune pâle / Jaune plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.008f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement propre, quelques plans de clivage fins.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 22;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Madagascar", rarityWeight = 100, valueMultiplier = 1.0f,
                        inclusionOverride = "Quasi-unique gisement mondial produisant cette variété transparente jaune." },
                };
            }),
            BuildGem("Labradorite", g =>
            {
                g.displayName = "Labradorite";
                g.description = "Feldspath plagioclase montrant un jeu de lumière irisé (labradorescence) dû à une structure lamellaire interne.";
                g.icon = GetGemIconSprite("labradorite", new Color(0.35f, 0.38f, 0.42f), new Color(0.25f, 0.55f, 0.60f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("labradorite", new Color(0.35f, 0.38f, 0.42f), "Ovale");
                g.refractiveIndex = 1.563f;
                g.density = 2.70f;
                g.hardness = 6.25f;
                g.lengthMm = 9.0f;
                g.widthMm = 7.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 2.4f;
                g.luster = "Vitreux à métallique par endroits";
                g.color = "Gris à reflets bleu-vert";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.009f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Fines lamelles internes responsables du jeu de lumière irisé caractéristique.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 20;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Madagascar", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Canada", rarityWeight = 35, valueMultiplier = 1.0f,
                        inclusionOverride = "Gisement type de la péninsule du Labrador, qui a donné son nom à l'espèce." },
                    new GemOrigin { name = "Finlande", rarityWeight = 30, valueMultiplier = 1.8f,
                        inclusionOverride = "Jeu de lumière exceptionnellement intense et multicolore, qualité rare recherchée." },
                };
            }),
            BuildGem("Serpentine_Bowenite", g =>
            {
                g.displayName = "Serpentine Bowénite";
                g.description = "Variété compacte et dure d'antigorite (groupe du serpentin), parfois commercialisée comme substitut du jade.";
                g.icon = GetGemIconSprite("bowenite", new Color(0.25f, 0.50f, 0.35f), new Color(0.45f, 0.68f, 0.50f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("bowenite", new Color(0.25f, 0.50f, 0.35f), "Coussin");
                g.refractiveIndex = 1.555f;
                g.density = 2.60f;
                g.hardness = 5.5f;
                g.lengthMm = 9.0f;
                g.widthMm = 7.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 3.0f;
                g.luster = "Vitreux à cireux";
                g.color = "Vert";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent), lecture diffuse typique d'un agrégat.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.014f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Texture compacte à grain fin, parfois veinules blanchâtres.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat polycristallin).";
                g.chelseaFilterReaction = "Aucune réaction notable.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 15;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Chine", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Afghanistan", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Nouvelle-Zélande", rarityWeight = 30, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Opale_Noble", g =>
            {
                g.displayName = "Opale Noble";
                g.description = "Silice hydratée amorphe organisée en réseau régulier de sphérules microscopiques, produisant un jeu de couleurs spectaculaire.";
                g.icon = GetGemIconSprite("opalenoble", new Color(0.85f, 0.85f, 0.88f), new Color(0.55f, 0.75f, 0.90f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("opalenoble", new Color(0.85f, 0.85f, 0.88f), "Coussin");
                g.refractiveIndex = 1.450f;
                g.density = 2.10f;
                g.hardness = 6.0f;
                g.lengthMm = 8.5f;
                g.widthMm = 6.5f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.7f;
                g.luster = "Vitreux à résineux";
                g.color = "Multicolore (jeu de couleurs)";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence blanchâtre à verdâtre possible sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible possible sous UV courte onde.";
                g.opticalCharacter = "Isotrope (matériau amorphe, non cristallin).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Réseau régulier de sphérules de silice responsable du jeu de couleurs, parfois craquelures internes.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau amorphe).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 90;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Australie", rarityWeight = 65, valueMultiplier = 1.4f,
                        inclusionOverride = "Jeu de couleurs large et vif sur fond sombre, qualité de référence mondiale." },
                    new GemOrigin { name = "Éthiopie", rarityWeight = 35, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Opale_De_Feu", g =>
            {
                g.displayName = "Opale de Feu";
                g.description = "Silice hydratée amorphe de couleur orangée à rouge vif, généralement dépourvue de jeu de couleurs.";
                g.icon = GetGemIconSprite("opaledefeu", new Color(0.90f, 0.45f, 0.15f), new Color(0.98f, 0.65f, 0.35f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("opaledefeu", new Color(0.90f, 0.45f, 0.15f), "Ovale");
                g.refractiveIndex = 1.430f;
                g.density = 2.00f;
                g.hardness = 6.0f;
                g.lengthMm = 7.5f;
                g.widthMm = 6.0f;
                g.depthMm = 4.0f;
                g.weightCarats = 1.4f;
                g.luster = "Vitreux à résineux";
                g.color = "Orange à rouge";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Isotrope (matériau amorphe, non cristallin).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement propre, parfois craquelures internes dues à la déshydratation.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau amorphe).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 35;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Mexique", rarityWeight = 70, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement historique produisant les teintes orangées les plus vives." },
                    new GemOrigin { name = "Brésil", rarityWeight = 15, valueMultiplier = 0.9f },
                    new GemOrigin { name = "États-Unis", rarityWeight = 15, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Kyanite", g =>
            {
                g.displayName = "Kyanite";
                g.description = "Silicate d'aluminium au port cristallin en lames allongées, connu pour une dureté variant fortement selon la direction testée.";
                g.icon = GetGemIconSprite("kyanite", new Color(0.15f, 0.30f, 0.60f), new Color(0.35f, 0.50f, 0.80f), "Baguette", "À degrés");
                g.roughIcon = GetRoughGemIconSprite("kyanite", new Color(0.15f, 0.30f, 0.60f), "Baguette");
                g.refractiveIndex = 1.720f;
                g.density = 3.68f;
                g.hardness = 5.5f;
                g.lengthMm = 10.0f;
                g.widthMm = 5.0f;
                g.depthMm = 3.5f;
                g.weightCarats = 1.5f;
                g.luster = "Vitreux à nacré sur les plans de clivage";
                g.color = "Bleu";
                g.shape = "Baguette";
                g.cutStyle = "À degrés";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Incolore / Bleu pâle / Bleu soutenu selon les trois axes (marqué).";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.017f;
                g.absorptionSpectrum = "Raies d'absorption fines liées au fer.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Plans de clivage marqués, zonation de couleur en bandes selon la longueur du cristal.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de fer).";
                g.baseValue = 45;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Népal", rarityWeight = 40, valueMultiplier = 1.2f },
                    new GemOrigin { name = "Brésil", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Kenya", rarityWeight = 25, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Turquoise", g =>
            {
                g.displayName = "Turquoise";
                g.description = "Phosphate hydraté de cuivre et d'aluminium, minéral opaque bleu à vert utilisé en joaillerie depuis l'Antiquité.";
                g.icon = GetGemIconSprite("turquoise", new Color(0.15f, 0.60f, 0.60f), new Color(0.40f, 0.78f, 0.75f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("turquoise", new Color(0.15f, 0.60f, 0.60f), "Coussin");
                g.refractiveIndex = 1.62f;
                g.density = 2.80f;
                g.hardness = 5.5f;
                g.lengthMm = 9.0f;
                g.widthMm = 7.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 3.2f;
                g.luster = "Vitreux à cireux";
                g.color = "Bleu";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence verdâtre à jaunâtre faible possible sous UV longue onde.";
                g.fluorescenceShortWave = "Non applicable (minéral opaque).";
                g.opticalCharacter = "Non applicable (minéral opaque).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Opaque;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Non applicable (minéral opaque).";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Veinules brunâtres ou noirâtres de matrice hôte fréquentes (\"toile d'araignée\").";
                g.interferenceFigure = "Non applicable (minéral opaque).";
                g.chelseaFilterReaction = "Non applicable (minéral opaque).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 35;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Iran", rarityWeight = 30, valueMultiplier = 1.6f,
                        inclusionOverride = "Couleur bleu ciel homogène et intense, sans veinules — qualité historique la plus recherchée." },
                    new GemOrigin { name = "États-Unis", rarityWeight = 40, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Chine", rarityWeight = 30, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Turquoise_Stabilisee", g =>
            {
                g.displayName = "Turquoise Stabilisée";
                g.description = "Turquoise naturelle poreuse imprégnée de résine pour en renforcer la structure et stabiliser sa couleur avant taille.";
                g.icon = GetGemIconSprite("turquoisestab", new Color(0.18f, 0.55f, 0.55f), new Color(0.42f, 0.72f, 0.70f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("turquoisestab", new Color(0.18f, 0.55f, 0.55f), "Coussin");
                g.refractiveIndex = 1.62f;
                g.density = 2.78f;
                g.hardness = 5.25f;
                g.lengthMm = 9.0f;
                g.widthMm = 7.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 3.2f;
                g.luster = "Vitreux à cireux";
                g.color = "Bleu";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence bleuâtre terne possible sous UV longue onde (résidus de résine).";
                g.fluorescenceShortWave = "Non applicable (minéral opaque).";
                g.opticalCharacter = "Non applicable (minéral opaque).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Opaque;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Non applicable (minéral opaque).";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Veinules de matrice hôte, structure poreuse comblée par une substance résineuse.";
                g.interferenceFigure = "Non applicable (minéral opaque).";
                g.chelseaFilterReaction = "Non applicable (minéral opaque).";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 12;
                g.forcedTreatment = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "États-Unis", rarityWeight = 55, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Chine", rarityWeight = 45, valueMultiplier = 0.9f },
                };
            }),
            BuildGem("Obsidienne", g =>
            {
                g.displayName = "Obsidienne";
                g.description = "Verre volcanique naturel formé par un refroidissement très rapide de lave riche en silice, sans structure cristalline.";
                g.icon = GetGemIconSprite("obsidienne", new Color(0.08f, 0.08f, 0.09f), new Color(0.25f, 0.25f, 0.28f), "Rond", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("obsidienne", new Color(0.08f, 0.08f, 0.09f), "Rond");
                g.refractiveIndex = 1.490f;
                g.density = 2.45f;
                g.hardness = 5.25f;
                g.lengthMm = 8.0f;
                g.widthMm = 8.0f;
                g.depthMm = 5.0f;
                g.weightCarats = 2.5f;
                g.luster = "Vitreux";
                g.color = "Noir";
                g.shape = "Rond";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide à opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Aucune fluorescence sous UV longue onde.";
                g.fluorescenceShortWave = "Aucune fluorescence sous UV courte onde.";
                g.opticalCharacter = "Isotrope (matériau amorphe, non cristallin).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Cassure conchoïdale caractéristique, parfois bulles de gaz ou inclusions minérales en flocons.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau amorphe).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 5;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Mexique", rarityWeight = 45, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis", rarityWeight = 35, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Islande", rarityWeight = 20, valueMultiplier = 1.1f },
                };
            }),
            BuildGem("Rhodocrosite", g =>
            {
                g.displayName = "Rhodocrosite";
                g.description = "Carbonate de manganèse rose à rouge, souvent présenté en bandes concentriques, surnommé \"rose des Incas\".";
                g.icon = GetGemIconSprite("rhodocrosite", new Color(0.85f, 0.40f, 0.45f), new Color(0.95f, 0.62f, 0.65f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("rhodocrosite", new Color(0.85f, 0.40f, 0.45f), "Coussin");
                g.refractiveIndex = 1.60f;
                g.density = 3.75f;
                g.hardness = 3.75f;
                g.lengthMm = 8.5f;
                g.widthMm = 7.0f;
                g.depthMm = 4.5f;
                g.weightCarats = 2.6f;
                g.luster = "Vitreux à nacré sur les plans de clivage";
                g.color = "Rose à rouge";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = true;
                g.pleochroicDescription = "Rose pâle / Rose plus soutenu selon l'axe (faible).";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, uniaxial — biréfringence exceptionnellement élevée.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.220f;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Bandes concentriques alternant teintes claires et soutenues, très caractéristiques.";
                g.interferenceFigure = "Figure uniaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction notable à l'aimant.";
                g.baseValue = 28;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Argentine", rarityWeight = 50, valueMultiplier = 1.3f,
                        inclusionOverride = "Bandes roses et blanches très marquées, qualité \"rose des Incas\" de référence." },
                    new GemOrigin { name = "Pérou", rarityWeight = 25, valueMultiplier = 1.0f },
                    new GemOrigin { name = "États-Unis", rarityWeight = 25, valueMultiplier = 1.4f,
                        inclusionOverride = "Cristaux transparents rhomboédriques bien formés, rares et recherchés des collectionneurs." },
                };
            }),
            BuildGem("Rhodonite", g =>
            {
                g.displayName = "Rhodonite";
                g.description = "Silicate de manganèse rose, généralement traversé de veines noires d'oxydes de manganèse.";
                g.icon = GetGemIconSprite("rhodonite", new Color(0.75f, 0.35f, 0.42f), new Color(0.90f, 0.55f, 0.58f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("rhodonite", new Color(0.75f, 0.35f, 0.42f), "Coussin");
                g.refractiveIndex = 1.735f;
                g.density = 3.55f;
                g.hardness = 6.0f;
                g.lengthMm = 8.5f;
                g.widthMm = 7.0f;
                g.depthMm = 4.5f;
                g.weightCarats = 2.6f;
                g.luster = "Vitreux";
                g.color = "Rose";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide à opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Généralement inerte sous UV courte onde.";
                g.opticalCharacter = "Anisotrope, biaxial (biréfringent).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Biaxial;
                g.birefringence = 0.012f;
                g.absorptionSpectrum = "Bande d'absorption liée au manganèse.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Veines noires d'oxydes de manganèse serpentant dans la masse rose, très caractéristiques.";
                g.interferenceFigure = "Figure biaxe.";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (traces de manganèse).";
                g.baseValue = 22;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Russie", rarityWeight = 40, valueMultiplier = 1.2f,
                        inclusionOverride = "Gisement historique de l'Oural, matériau massif utilisé en ornementation depuis des siècles." },
                    new GemOrigin { name = "Australie", rarityWeight = 30, valueMultiplier = 1.0f },
                    new GemOrigin { name = "Suède", rarityWeight = 30, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Diamant_Synthetique_CVD", g =>
            {
                g.displayName = "Diamant Synthétique (CVD)";
                g.description = "Diamant de synthèse obtenu par dépôt chimique en phase vapeur : des atomes de carbone se déposent couche par couche sur un germe de diamant placé dans un plasma d'hydrocarbures.";
                g.icon = GetGemIconSprite("diamantcvd", new Color(0.88f, 0.92f, 0.90f), new Color(0.97f, 0.99f, 0.97f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("diamantcvd", new Color(0.88f, 0.92f, 0.90f), "Rond");
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
                g.fluorescenceLongWave = "Fluorescence orangée faible parfois observée sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence plus faible sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.isThermallyConductiveLikeDiamond = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre ; fin grainage parallèle aux couches de croissance parfois visible à fort grossissement.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 40;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle (dépôt chimique en phase vapeur)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Diamant_Synthetique_HPHT", g =>
            {
                g.displayName = "Diamant Synthétique (HPHT)";
                g.description = "Diamant de synthèse obtenu par haute pression et haute température, reproduisant en quelques semaines les conditions de formation naturelle du diamant à l'aide d'un catalyseur métallique.";
                g.icon = GetGemIconSprite("diamanthpht", new Color(0.90f, 0.90f, 0.85f), new Color(0.99f, 0.98f, 0.92f), "Rond", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("diamanthpht", new Color(0.90f, 0.90f, 0.85f), "Rond");
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
                g.fluorescenceLongWave = "Fluorescence variable ; phosphorescence prolongée après extinction de la lampe parfois observée, un phénomène rarement rencontré naturellement.";
                g.fluorescenceShortWave = "Fluorescence plus faible sous UV courte onde.";
                g.opticalCharacter = "Isotrope (monoréfringent, réseau cubique).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Isotropic;
                g.birefringence = 0.000f;
                g.isThermallyConductiveLikeDiamond = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Généralement très propre ; rares inclusions métalliques résiduelles du catalyseur de croissance possibles.";
                g.interferenceFigure = "Aucune figure d'interférence (matériau isotrope).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Légère réaction possible avec un aimant très puissant (inclusions métalliques résiduelles du procédé de croissance).";
                g.baseValue = 45;
                g.forcedSynthetic = true;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Fabrication industrielle (haute pression, haute température)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Painite", g =>
            {
                g.displayName = "Painite";
                g.description = "Borate de zirconium et d'aluminium longtemps considéré comme le minéral gemme le plus rare au monde, ne se trouvant quasiment que dans une seule région de Birmanie.";
                g.icon = GetGemIconSprite("painite", new Color(0.55f, 0.22f, 0.12f), new Color(0.78f, 0.42f, 0.20f), "Ovale", "Mixte");
                g.roughIcon = GetRoughGemIconSprite("painite", new Color(0.55f, 0.22f, 0.12f), "Ovale");
                g.refractiveIndex = 1.80f;
                g.density = 4.01f;
                g.hardness = 8.0f;
                g.lengthMm = 5.0f;
                g.widthMm = 4.0f;
                g.depthMm = 3.0f;
                g.weightCarats = 0.6f;
                g.luster = "Vitreux";
                g.color = "Brun-rouge à orangé";
                g.shape = "Ovale";
                g.cutStyle = "Mixte";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Nuances marquées de brun-rouge, orangé et brun foncé selon l'axe d'observation.";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent, réseau hexagonal).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.022f;
                g.absorptionSpectrum = "Raies d'absorption marquées dans le rouge et le bleu, liées à des traces de fer et de chrome.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 445f, widthNm = 6f },
                    new SpectrumLine { wavelengthNm = 635f, widthNm = 6f },
                };
                g.inclusions = "Cristaux minéraux inclus rarement observés ; fines stries de croissance parfois visibles.";
                g.interferenceFigure = "Figure uniaxe centrée observable en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction particulière au filtre Chelsea.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 1500;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 10000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Birmanie (Mogok)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Beryl_Rouge", g =>
            {
                g.displayName = "Béryl Rouge (Bixbite)";
                g.description = "Variété rouge framboise du béryl, colorée par des traces de manganèse, comptant parmi les gemmes les plus rares au monde et ne dépassant presque jamais le carat une fois taillée.";
                g.icon = GetGemIconSprite("berylrouge", new Color(0.65f, 0.10f, 0.20f), new Color(0.88f, 0.30f, 0.38f), "Ovale", "Brillant");
                g.roughIcon = GetRoughGemIconSprite("berylrouge", new Color(0.65f, 0.10f, 0.20f), "Ovale");
                g.refractiveIndex = 1.570f;
                g.density = 2.68f;
                g.hardness = 7.75f;
                g.lengthMm = 4.0f;
                g.widthMm = 3.0f;
                g.depthMm = 2.0f;
                g.weightCarats = 0.25f;
                g.luster = "Vitreux";
                g.color = "Rouge framboise intense";
                g.shape = "Ovale";
                g.cutStyle = "Brillant";
                g.transparency = "Transparent";
                g.pleochroic = true;
                g.pleochroicDescription = "Dichroïsme faible à modéré entre rouge pourpré et orangé selon l'axe.";
                g.fluorescenceLongWave = "Généralement inerte sous UV longue onde.";
                g.fluorescenceShortWave = "Inerte sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent, réseau hexagonal).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.006f;
                g.absorptionSpectrum = "Bandes d'absorption liées au manganèse, notamment dans le vert-jaune.";
                g.spectrumLines = new List<SpectrumLine>
                {
                    new SpectrumLine { wavelengthNm = 550f, widthNm = 10f },
                };
                g.inclusions = "Voiles de croissance et canaux allongés occasionnels, typiques des béryls.";
                g.interferenceFigure = "Figure uniaxe centrée en lumière convergente.";
                g.chelseaFilterReaction = "Aucune réaction marquée au filtre Chelsea.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 1200;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 10000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "États-Unis (Utah, Wah Wah Mountains)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ivoire_Elephant", g =>
            {
                g.displayName = "Ivoire d'Éléphant";
                g.description = "Dentine provenant des défenses d'éléphant. Son commerce international est interdit par la Convention de Washington (CITES) depuis 1989 en raison du braconnage massif ayant décimé les populations d'éléphants.";
                g.icon = GetGemIconSprite("ivoireelephant", new Color(0.90f, 0.87f, 0.78f), new Color(0.97f, 0.95f, 0.90f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("ivoireelephant", new Color(0.90f, 0.87f, 0.78f), "Ovale");
                g.refractiveIndex = 1.54f;
                g.density = 1.85f;
                g.hardness = 2.75f;
                g.lengthMm = 30f;
                g.widthMm = 20f;
                g.depthMm = 10f;
                g.weightCarats = 15f;
                g.luster = "Cireux";
                g.color = "Blanc crème à ivoire";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence bleu-blanc caractéristique sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence plus faible et diffuse sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique au spectroscope.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "En coupe transversale, fines lignes entrecroisées (lignes de Schreger) formant des angles généralement supérieurs à 115°.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 600;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afrique (défenses d'éléphant)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ivoire_Mammouth", g =>
            {
                g.displayName = "Ivoire de Mammouth";
                g.description = "Dentine fossile extraite des défenses de mammouths laineux préservées dans le pergélisol sibérien depuis des millénaires. Contrairement à l'ivoire d'éléphant, sa vente est légale dans la plupart des pays car l'espèce est éteinte, ce qui alimente hélas un trafic parallèle d'ivoire frais faussement étiqueté.";
                g.icon = GetGemIconSprite("ivoiremammouth", new Color(0.80f, 0.68f, 0.50f), new Color(0.90f, 0.80f, 0.65f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("ivoiremammouth", new Color(0.80f, 0.68f, 0.50f), "Ovale");
                g.refractiveIndex = 1.54f;
                g.density = 1.90f;
                g.hardness = 2.75f;
                g.lengthMm = 32f;
                g.widthMm = 22f;
                g.depthMm = 11f;
                g.weightCarats = 16f;
                g.luster = "Cireux";
                g.color = "Beige à brun, parfois taché de bleu-vert";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque à translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence bleu-blanc plus faible et irrégulière sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence diffuse sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Lignes de Schreger entrecroisées formant des angles généralement inférieurs à 115°, souvent accompagnées de fissures de surface dues au vieillissement.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 500;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Sibérie (pergélisol)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ivoire_Morse", g =>
            {
                g.displayName = "Ivoire de Morse";
                g.description = "Dentine provenant des défenses de morse, reconnaissable à son cœur granuleux caractéristique en coupe transversale. Sa vente est très réglementée et interdite dans de nombreux pays pour protéger les populations arctiques.";
                g.icon = GetGemIconSprite("ivoiremorse", new Color(0.88f, 0.86f, 0.80f), new Color(0.96f, 0.95f, 0.90f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("ivoiremorse", new Color(0.88f, 0.86f, 0.80f), "Ovale");
                g.refractiveIndex = 1.54f;
                g.density = 1.95f;
                g.hardness = 2.9f;
                g.lengthMm = 25f;
                g.widthMm = 16f;
                g.depthMm = 9f;
                g.weightCarats = 10f;
                g.luster = "Cireux";
                g.color = "Blanc ivoire à crème";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque à translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence blanc-bleuté sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Cœur central granuleux et marbré, nettement distinct de la couche externe dense, visible en coupe transversale.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 480;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Arctique (Alaska, Canada)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ivoire_Hippopotame", g =>
            {
                g.displayName = "Ivoire d'Hippopotame";
                g.description = "Dentine issue des canines et incisives d'hippopotame, réputée pour sa dureté et son grain très fin, autrefois utilisée pour la fabrication de dentiers avant l'apparition des matériaux synthétiques. Comme l'ivoire d'éléphant, son commerce international est interdit par la CITES.";
                g.icon = GetGemIconSprite("ivoirehippopotame", new Color(0.93f, 0.91f, 0.85f), new Color(0.99f, 0.98f, 0.95f), "Ovale", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("ivoirehippopotame", new Color(0.93f, 0.91f, 0.85f), "Ovale");
                g.refractiveIndex = 1.54f;
                g.density = 1.90f;
                g.hardness = 3.0f;
                g.lengthMm = 22f;
                g.widthMm = 14f;
                g.depthMm = 8f;
                g.weightCarats = 8f;
                g.luster = "Cireux, prenant un poli particulièrement fin";
                g.color = "Blanc pur à crème";
                g.shape = "Ovale";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence blanc-bleuté sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Grain exceptionnellement fin et homogène, sans structure entrecroisée marquée.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 500;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afrique (zones humides)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Defense_Narval", g =>
            {
                g.displayName = "Défense de Narval";
                g.description = "Dent canine spiralée du narval, cétacé arctique parfois surnommé « licorne des mers ». Sa vente est encadrée par la CITES pour protéger l'espèce.";
                g.icon = GetGemIconSprite("defensenarval", new Color(0.90f, 0.88f, 0.82f), new Color(0.97f, 0.96f, 0.92f), "Baguette", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("defensenarval", new Color(0.90f, 0.88f, 0.82f), "Baguette");
                g.refractiveIndex = 1.54f;
                g.density = 1.90f;
                g.hardness = 2.75f;
                g.lengthMm = 40f;
                g.widthMm = 15f;
                g.depthMm = 15f;
                g.weightCarats = 20f;
                g.luster = "Cireux";
                g.color = "Ivoire crème, marqué de sillons en spirale";
                g.shape = "Baguette";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence blanc-bleuté sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence faible sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.003f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Croissance en spirale senestre caractéristique, visible en stries torsadées sur toute la longueur.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 700;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Arctique (Groenland)", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Corne_Rhinoceros", g =>
            {
                g.displayName = "Corne de Rhinocéros";
                g.description = "Kératine compactée formant la corne du rhinocéros, chimiquement proche des cheveux et des ongles humains, sans lien avec l'ivoire. Sa demande sur les marchés parallèles, alimentée par des croyances médicinales sans fondement scientifique, en fait l'une des matières les plus braconnées au monde. Sa vente est prohibée par la CITES.";
                g.icon = GetGemIconSprite("cornerhinoceros", new Color(0.30f, 0.26f, 0.22f), new Color(0.45f, 0.40f, 0.35f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("cornerhinoceros", new Color(0.30f, 0.26f, 0.22f), "Coussin");
                g.refractiveIndex = 1.55f;
                g.density = 1.30f;
                g.hardness = 2.5f;
                g.lengthMm = 28f;
                g.widthMm = 18f;
                g.depthMm = 18f;
                g.weightCarats = 12f;
                g.luster = "Mat à soyeux";
                g.color = "Brun-gris à noir";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Opaque";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Non applicable (matériau opaque).";
                g.fluorescenceShortWave = "Non applicable (matériau opaque).";
                g.opticalCharacter = "Non applicable (matériau opaque).";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Opaque;
                g.birefringence = 0.0f;
                g.absorptionSpectrum = "Non applicable (matériau opaque).";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Structure fibreuse compacte, visible sous forme de fines fibres parallèles à la base de la corne.";
                g.interferenceFigure = "Non applicable (matériau opaque).";
                g.chelseaFilterReaction = "Non applicable (matériau opaque).";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 1100;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Afrique australe", rarityWeight = 100, valueMultiplier = 1.0f },
                };
            }),
            BuildGem("Ecaille_Tortue", g =>
            {
                g.displayName = "Écaille de Tortue";
                g.description = "Kératine translucide provenant de la carapace de la tortue imbriquée, façonnée depuis l'Antiquité en bijoux, peignes et incrustations pour son motif tacheté unique. Sa vente est interdite par la CITES depuis 1977 pour protéger cette espèce aujourd'hui en danger critique d'extinction.";
                g.icon = GetGemIconSprite("ecailletortue", new Color(0.55f, 0.38f, 0.15f), new Color(0.78f, 0.58f, 0.28f), "Coussin", "Cabochon");
                g.roughIcon = GetRoughGemIconSprite("ecailletortue", new Color(0.55f, 0.38f, 0.15f), "Coussin");
                g.refractiveIndex = 1.55f;
                g.density = 1.29f;
                g.hardness = 2.5f;
                g.lengthMm = 20f;
                g.widthMm = 15f;
                g.depthMm = 4f;
                g.weightCarats = 6f;
                g.luster = "Résineux à cireux";
                g.color = "Brun ambré tacheté de noir";
                g.shape = "Coussin";
                g.cutStyle = "Cabochon";
                g.transparency = "Translucide";
                g.pleochroic = false;
                g.pleochroicDescription = "";
                g.fluorescenceLongWave = "Fluorescence faible, parfois verdâtre, sous UV longue onde.";
                g.fluorescenceShortWave = "Fluorescence quasi nulle sous UV courte onde.";
                g.opticalCharacter = "Uniaxe (biréfringent), lecture diffuse typique d'un agrégat organique.";
                g.opticalCharacterClass = GemDefinition.OpticalCharacterClass.Uniaxial;
                g.birefringence = 0.004f;
                g.isPolycrystalline = true;
                g.absorptionSpectrum = "Aucun spectre d'absorption caractéristique.";
                g.spectrumLines = new List<SpectrumLine>();
                g.inclusions = "Zones pigmentées irrégulières emprisonnées dans une matrice translucide homogène.";
                g.interferenceFigure = "Figure diffuse, non exploitable (agrégat organique).";
                g.chelseaFilterReaction = "Aucune réaction particulière.";
                g.magnetism = "Aucune réaction à l'aimant.";
                g.baseValue = 450;
                g.isTunnelExclusive = true;
                g.tunnelMinPrice = 2000;
                g.origins = new List<GemOrigin>
                {
                    new GemOrigin { name = "Caraïbes / Océan Indien", rarityWeight = 100, valueMultiplier = 1.0f },
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

    private static Tile GetOrCreateTileFromSprite(string name, Sprite sprite)
    {
        var tilePath = $"Assets/Art/Tilesets/{name}.asset";
        var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existingTile != null) return existingTile;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;

        Directory.CreateDirectory("Assets/Art/Tilesets");
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    // ----- External sprite pack (LimeZu Modern Exteriors) -----

    private const string PackThemeRoot = "Assets/Art/tileset/modernexteriors-win/Modern_Exteriors_32x32/ME_Theme_Sorter_32x32/";
    private const string PackTerrains = PackThemeRoot + "1_Terrains_and_Fences_Singles_32x32/ME_Singles_Terrains_and_Fences_32x32_";
    private const string PackGarden = PackThemeRoot + "17_Garden_Singles_32x32/ME_Singles_Garden_32x32_";
    private const string PackCityProps = PackThemeRoot + "3_City_Props_Singles_32x32/ME_Singles_City_Props_32x32_";

    // Center-pivot import, matching our procedural sprites — used for tilemap tiles.
    private static Sprite LoadPackSprite(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Sprite pack asset not found: {fullPath}");
            return null;
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        if (importer == null)
        {
            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        }
        if (importer == null)
        {
            AssetDatabase.Refresh();
            importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        }
        if (importer == null)
        {
            Debug.LogWarning($"Could not import sprite: {fullPath}");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
    }

    // Bottom-pivot import — used for standalone world decorations, so `position` is where the object stands.
    private static Sprite LoadPackSpriteGrounded(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Sprite pack asset not found: {fullPath}");
            return null;
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        if (importer == null)
        {
            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        }
        if (importer == null)
        {
            AssetDatabase.Refresh();
            importer = (TextureImporter)AssetImporter.GetAtPath(fullPath);
        }
        if (importer == null)
        {
            Debug.LogWarning($"Could not import sprite: {fullPath}");
            return null;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        if (importer.textureType != TextureImporterType.Sprite || settings.spriteAlignment != (int)SpriteAlignment.Custom)
        {
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);

            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
    }

    private static readonly string[] PlayerWalkDirections = { "Down", "Up", "Left", "Right" };

    private static Sprite GetPlayerSprite()
    {
        foreach (var direction in PlayerWalkDirections)
        {
            for (var frame = 0; frame < 6; frame++)
                LoadPackSpriteGrounded($"Assets/Resources/PlayerWalk/{direction}/{frame}.png");
            LoadPackSpriteGrounded($"Assets/Resources/PlayerIdle/{direction}.png");
        }

        return LoadPackSpriteGrounded("Assets/Art/Sprites/player_character.png");
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

    private static string[] BuildStripeFlagPattern(int[] bandHeights, bool vertical)
    {
        var keys = new[] { 'A', 'B', 'C', 'D', 'E' };
        var rows = new string[16];

        if (vertical)
        {
            var parts = new List<object>();
            for (var i = 0; i < bandHeights.Length; i++)
            {
                parts.Add(bandHeights[i]);
                parts.Add(keys[i]);
            }
            var rowStr = Row(parts.ToArray());
            for (var y = 0; y < 16; y++) rows[y] = rowStr;
        }
        else
        {
            var y = 0;
            for (var i = 0; i < bandHeights.Length; i++)
            {
                var rowStr = Row(16, keys[i]);
                for (var r = 0; r < bandHeights[i] && y < 16; r++) rows[y++] = rowStr;
            }
        }

        return rows;
    }

    private static Sprite GetOrCreateFlagSprite(MarketCountries.Country country)
    {
        var slug = MarketCountries.Slug(country.Name);
        var spritePath = $"Assets/Resources/Flags/flag_{slug}_pixel.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        var pattern = BuildStripeFlagPattern(country.FlagBandHeights, country.FlagVertical);
        var keys = new[] { 'A', 'B', 'C', 'D', 'E' };
        var palette = new Dictionary<char, Color>();
        for (var i = 0; i < country.FlagColors.Length; i++) palette[keys[i]] = country.FlagColors[i];

        var tex = BuildPixelArtTexture(pattern, palette, 2);
        Directory.CreateDirectory("Assets/Resources/Flags");
        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        return ImportSprite(spritePath);
    }

    private static void BakeAllFlags()
    {
        foreach (var country in MarketCountries.Pool)
            GetOrCreateFlagSprite(country);
    }

    private static void CreateMusicManager()
    {
        var townClip = GetMusicClip("Music_Town.mp3");
        var laboratoryClip = GetMusicClip("Music_Laboratoire.mp3");
        var mainMenuClip = GetMusicClip("Music_MainMenu.mp3");
        var tunnelClip = GetMusicClip("Music_Tunnel.mp3");
        var manoirClip = GetMusicClip("Music_Manoir.mp3");
        var marketClip = GetMusicClip("Music_Marche.mp3");

        var go = new GameObject("MusicManager");
        var manager = go.AddComponent<MusicManager>();
        var so = new SerializedObject(manager);
        var townProp = so.FindProperty("townMusic");
        var labProp = so.FindProperty("laboratoryMusic");
        var mainMenuProp = so.FindProperty("mainMenuMusic");
        var tunnelProp = so.FindProperty("tunnelMusic");
        var manoirProp = so.FindProperty("manoirMusic");
        var marketProp = so.FindProperty("marketMusic");
        if (townProp != null) townProp.objectReferenceValue = townClip;
        if (labProp != null) labProp.objectReferenceValue = laboratoryClip;
        if (mainMenuProp != null) mainMenuProp.objectReferenceValue = mainMenuClip;
        if (tunnelProp != null) tunnelProp.objectReferenceValue = tunnelClip;
        if (manoirProp != null) manoirProp.objectReferenceValue = manoirClip;
        if (marketProp != null) marketProp.objectReferenceValue = marketClip;
        so.ApplyModifiedProperties();
    }

    private static AudioClip GetMusicClip(string fileName)
    {
        var path = $"Assets/Audio/Music/{fileName}";
        if (!File.Exists(path)) return null;

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            AssetDatabase.Refresh();
            importer = (AudioImporter)AssetImporter.GetAtPath(path);
        }
        if (importer == null)
        {
            Debug.LogWarning($"Could not import audio clip: {path}");
            return null;
        }

        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
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

    private static Color LabFloorPixel(int x, int y)
    {
        var tileA = new Color(0.24f, 0.26f, 0.30f);
        var tileB = new Color(0.21f, 0.23f, 0.27f);
        var line = new Color(0.14f, 0.15f, 0.18f);

        var isLine = x % 8 == 0 || y % 8 == 0;
        if (isLine) return line;
        return (x / 8 + y / 8) % 2 == 0 ? tileA : tileB;
    }

    private static Color TunnelWallPixel(int x, int y)
    {
        var rock = new Color(0.16f, 0.15f, 0.16f);
        var rockDark = new Color(0.11f, 0.10f, 0.11f);
        var vein = new Color(0.20f, 0.17f, 0.14f);

        var noise = (x * 7 + y * 13) % 17;
        if (noise == 0) return vein;
        return (x / 3 + y / 3) % 2 == 0 ? rock : rockDark;
    }

    private static Color TunnelFloorPixel(int x, int y)
    {
        var dirtA = new Color(0.27f, 0.22f, 0.17f);
        var dirtB = new Color(0.23f, 0.19f, 0.15f);
        var pebble = new Color(0.32f, 0.30f, 0.28f);

        var noise = (x * 5 + y * 11) % 23;
        if (noise == 0) return pebble;
        return (x / 4 + y / 4) % 2 == 0 ? dirtA : dirtB;
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
            case "Aimant": return GetOrCreatePixelSprite("aimant_pixel", MagnetPattern, MagnetPalette);
            case "FTIR": return GetOrCreatePixelSprite("ftir_pixel", RefractometrePattern, FTIRPalette);
            case "Raman": return GetOrCreatePixelSprite("raman_pixel", DichroscopePattern, RamanPalette);
            case "UVVisNIR": return GetOrCreatePixelSprite("uvvisnir_pixel", BalanceCaratPattern, UVVisNIRPalette);
            case "MEB": return GetOrCreatePixelSprite("meb_pixel", MicroscopePattern, MEBPalette);
            case "DiamondView": return GetOrCreatePixelSprite("diamondview_pixel", MicroscopePattern, DiamondViewPalette);
            case "DiamondSure": return GetOrCreatePixelSprite("diamondsure_pixel", RefractometrePattern, DiamondSurePalette);
            case "TesteurThermique": return GetOrCreatePixelSprite("testeurthermique_pixel", MicrometrePattern, ThermalProbePalette);
            case "TesteurElectrique": return GetOrCreatePixelSprite("testeurelectrique_pixel", MicrometrePattern, ElectricalProbePalette);
            default: return GetOrCreatePixelSprite("loupe_pixel", LoupePattern, LoupePalette);
        }
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

    private static readonly string[] BushSprites =
    {
        PackGarden + "Bush_4.png",
        PackGarden + "Bush_3.png",
    };

    private static readonly string[] FlowerSprites =
    {
        PackGarden + "Small_Red_Flower.png",
        PackGarden + "Small_Light_Blue_Flower.png",
        PackGarden + "Small_Sunflower.png",
    };

    private static void CreateTree(Vector3 position, int variant = 0)
    {
        var sprite = LoadPackSpriteGrounded(BushSprites[variant % BushSprites.Length]);
        if (sprite == null) return;

        var go = new GameObject("Arbre");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.35f);
        col.offset = new Vector2(0f, 0.15f);
    }

    private static void CreateLamppost(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded(PackCityProps + "Street_Lamp_5.png");
        if (sprite == null) return;

        var go = new GameObject("Lampadaire");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;
    }

    private static void CreateFlowerCluster(Vector3 position, int variant)
    {
        var sprite = LoadPackSpriteGrounded(FlowerSprites[variant % FlowerSprites.Length]);
        if (sprite == null) return;

        var go = new GameObject("Fleurs");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 3;
        go.transform.position = position;
    }

    private static void CreateFence(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded(PackTerrains + "Fence_1_7.png");
        if (sprite == null) return;

        var go = new GameObject("Cloture");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.3f);
        col.offset = new Vector2(0f, 0.15f);
    }

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

    private static readonly string[] GemRoundPattern =
    {
        Row(16, '.'),
        Row(5, '.', 5, 'G', 6, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(1, '.', 7, 'G', 1, 'F', 5, 'G', 2, '.'),
        Row(1, '.', 13, 'G', 2, '.'),
        Row(1, '.', 13, 'G', 2, '.'),
        Row(1, '.', 13, 'G', 2, '.'),
        Row(1, '.', 13, 'G', 2, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(5, '.', 5, 'G', 6, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemOvalPattern =
    {
        Row(7, '.', 1, 'G', 8, '.'),
        Row(5, '.', 5, 'G', 6, '.'),
        Row(4, '.', 7, 'G', 5, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(3, '.', 5, 'G', 1, 'F', 3, 'G', 4, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(4, '.', 7, 'G', 5, '.'),
        Row(5, '.', 5, 'G', 6, '.'),
        Row(7, '.', 1, 'G', 8, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemCushionPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(2, '.', 6, 'G', 1, 'F', 5, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemPearPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(6, '.', 3, 'G', 1, 'F', 6, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemMarquisePattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(3, '.', 5, 'G', 1, 'F', 4, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(7, '.', 2, 'G', 7, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemSquarePattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 6, 'G', 1, 'F', 5, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemBaguettePattern =
    {
        Row(16, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 3, 'G', 1, 'F', 2, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(16, '.'),
    };

    private static readonly string[] GemEmeraldCutPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(2, '.', 6, 'G', 1, 'F', 5, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(2, '.', 12, 'G', 2, '.'),
        Row(3, '.', 10, 'G', 3, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static Sprite GetGemIconSprite(string key, Color gemColor, Color facetColor, string shape = "", string cutStyle = "")
    {
        var basePattern = shape switch
        {
            "Rond" => GemRoundPattern,
            "Ovale" => GemOvalPattern,
            "Coussin" => GemCushionPattern,
            "Poire" => GemPearPattern,
            "Marquise" => GemMarquisePattern,
            "Carré" => GemSquarePattern,
            "Baguette" => GemBaguettePattern,
            "Rectangle à pans coupés" => GemEmeraldCutPattern,
            _ => GemIconPattern,
        };

        var pattern = ApplyCutStyleFacets(basePattern, cutStyle);
        var palette = new Dictionary<char, Color> { { 'G', gemColor }, { 'F', facetColor } };
        return GetOrCreatePixelSprite($"gemicon_{key}_pixel", pattern, palette);
    }

    private static string[] ApplyCutStyleFacets(string[] basePattern, string cutStyle)
    {
        var isCabochon = cutStyle == "Cabochon";
        var isStepCut = cutStyle == "À degrés" || cutStyle == "Émeraude" || cutStyle == "Émeraude (à degrés)";
        if (!isCabochon && !isStepCut) return basePattern;

        var rows = new string[basePattern.Length];
        for (var y = 0; y < basePattern.Length; y++)
        {
            var chars = basePattern[y].ToCharArray();
            var hadFacet = false;
            for (var x = 0; x < chars.Length; x++)
            {
                if (chars[x] != 'F') continue;
                hadFacet = true;
                chars[x] = 'G';
            }

            if (hadFacet && isStepCut)
            {
                for (var x = 0; x < chars.Length; x++)
                {
                    if (chars[x] == 'G' && x % 2 == 0) chars[x] = 'F';
                }
            }

            rows[y] = new string(chars);
        }

        return rows;
    }

    private static readonly string[] RoughNodulePattern =
    {
        Row(16, '.'),
        Row(5, '.', 3, 'G', 8, '.'),
        Row(3, '.', 6, 'G', 7, '.'),
        Row(2, '.', 9, 'G', 5, '.'),
        Row(4, '.', 2, 'G', 1, 'F', 6, 'G', 3, '.'),
        Row(1, '.', 12, 'G', 3, '.'),
        Row(2, '.', 11, 'G', 3, '.'),
        Row(1, '.', 10, 'G', 5, '.'),
        Row(3, '.', 9, 'G', 4, '.'),
        Row(2, '.', 8, 'G', 6, '.'),
        Row(4, '.', 7, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly string[] RoughCrystalPattern =
    {
        Row(7, '.', 2, 'G', 7, '.'),
        Row(6, '.', 4, 'G', 6, '.'),
        Row(5, '.', 1, 'F', 5, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(5, '.', 6, 'G', 5, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(4, '.', 8, 'G', 4, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static Sprite GetRoughGemIconSprite(string key, Color gemColor, string shape)
    {
        var pattern = shape == "Baguette" ? RoughCrystalPattern : RoughNodulePattern;
        var muted = new Color(gemColor.r * 0.72f, gemColor.g * 0.72f, gemColor.b * 0.72f, gemColor.a);
        var palette = new Dictionary<char, Color> { { 'G', gemColor }, { 'F', muted } };
        return GetOrCreatePixelSprite($"roughicon_{key}_pixel", pattern, palette);
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

    private static readonly Dictionary<char, Color> FTIRPalette = new Dictionary<char, Color>
    {
        { 'G', new Color(0.90f, 0.45f, 0.18f) },
        { 'B', new Color(0.32f, 0.16f, 0.13f) },
        { 'K', new Color(0.08f, 0.08f, 0.09f) },
    };

    private static readonly Dictionary<char, Color> RamanPalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.30f, 0.14f, 0.14f) },
        { 'L', new Color(0.95f, 0.15f, 0.15f) },
    };

    private static readonly Dictionary<char, Color> UVVisNIRPalette = new Dictionary<char, Color>
    {
        { 'D', new Color(0.38f, 0.20f, 0.78f) },
        { 'P', new Color(0.55f, 0.60f, 0.72f) },
        { 'K', new Color(0.15f, 0.15f, 0.18f) },
    };

    private static readonly Dictionary<char, Color> MEBPalette = new Dictionary<char, Color>
    {
        { 'E', new Color(0.72f, 0.76f, 0.84f) },
        { 'B', new Color(0.20f, 0.25f, 0.35f) },
        { 'K', new Color(0.08f, 0.08f, 0.09f) },
    };

    private static readonly Dictionary<char, Color> DiamondViewPalette = new Dictionary<char, Color>
    {
        { 'E', new Color(0.45f, 0.75f, 0.95f) },
        { 'B', new Color(0.12f, 0.15f, 0.28f) },
        { 'K', new Color(0.06f, 0.06f, 0.08f) },
    };

    private static readonly Dictionary<char, Color> DiamondSurePalette = new Dictionary<char, Color>
    {
        { 'G', new Color(0.35f, 0.85f, 0.55f) },
        { 'B', new Color(0.20f, 0.22f, 0.26f) },
        { 'K', new Color(0.08f, 0.08f, 0.09f) },
    };

    private static readonly Dictionary<char, Color> ThermalProbePalette = new Dictionary<char, Color>
    {
        { 'B', new Color(0.75f, 0.30f, 0.15f) },
        { 'J', new Color(0.95f, 0.55f, 0.25f) },
    };

    private static readonly Dictionary<char, Color> ElectricalProbePalette = new Dictionary<char, Color>
    {
        { 'B', new Color(0.20f, 0.35f, 0.75f) },
        { 'J', new Color(0.90f, 0.85f, 0.20f) },
    };

    private static readonly string[] MagnetPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(3, '.', 10, 'H', 3, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(3, '.', 10, 'M', 3, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> MagnetPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.45f, 0.45f, 0.50f) },
        { 'M', new Color(0.14f, 0.14f, 0.16f) },
    };

    private static readonly string[] BedPattern =
    {
        Row(16, '.'),
        Row(1, '.', 14, 'H', 1, '.'),
        Row(1, '.', 14, 'H', 1, '.'),
        Row(1, '.', 4, 'P', 10, 'B', 1, '.'),
        Row(1, '.', 4, 'P', 10, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'B', 1, '.'),
        Row(1, '.', 14, 'F', 1, '.'),
        Row(2, '.', 2, 'F', 8, '.', 2, 'F', 2, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> BedPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.42f, 0.28f, 0.16f) },
        { 'P', new Color(0.92f, 0.92f, 0.95f) },
        { 'B', new Color(0.30f, 0.45f, 0.70f) },
        { 'F', new Color(0.25f, 0.17f, 0.10f) },
    };
}
