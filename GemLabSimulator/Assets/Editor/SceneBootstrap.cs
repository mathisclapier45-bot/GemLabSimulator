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

    // Interior camera framing: a larger reference resolution shows more of the room (480x270 = 15x8.4 world units).
    private const int InteriorRefResolutionX = 480;
    private const int InteriorRefResolutionY = 270;

    // The player's own house: one small room.
    private const int HouseMinX = -5;
    private const int HouseMaxX = 5;
    private const int HouseMinY = -4;
    private const int HouseMaxY = 3;

    // The GemStore interior.
    private const int ShopMinX = -8;
    private const int ShopMaxX = 8;
    private const int ShopMinY = -5;
    private const int ShopMaxY = 4;

    private const int DividerX = 6;
    private const int DoorGapY1 = -1;
    private const int DoorGapY2 = 0;

    private const int TownMinX = -70;
    private const int TownMaxX = 70;
    private const int TownMinY = -60;
    private const int TownMaxY = 45;

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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("home", Color.white, "floor_pixel", FloorPixel);
        var wallTile = GetInteriorSideWallTile("home", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, RoomMinX, RoomMaxX, RoomMaxY, "home", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, RoomMinY + 1, 0f), "SpawnPoint_Home");

        // The entrance room is the storage room: a row of wardrobes (the bed and the personal chest now live in the player's house).
        // The two blue ones are reserved for client orders; the wooden ones keep the donations.
        foreach (var armoireX in new[] { -4f, -2f })
            CreateArmoire(new Vector3(armoireX, 3.0f, 0f), true);
        foreach (var armoireX in new[] { 0f, 2f, 4f })
            CreateArmoire(new Vector3(armoireX, 3.0f, 0f));

        CreateMailbox(new Vector3(DividerX - 2, RoomMinY + 1.1f, 0f));

        var gemPool = GetOrCreateGemPool();
        UpdateGemDatabase(gemPool);

        // Tools stand on small desks along the walls of the lab half of the house.
        const float topDeskY = 3.0f;
        const float bottomDeskY = -2.9f;
        CreateDeskStation("Loupe", ExaminationTool.MeasurementType.VisualExamination, 8.6f, topDeskY, false);
        CreateDeskStation("Refractometre", ExaminationTool.MeasurementType.RefractiveIndex, 11.2f, topDeskY, false);
        CreateDeskStation("BalanceHydrostatique", ExaminationTool.MeasurementType.Density, 13.8f, topDeskY, false);
        CreateDeskStation("Dichroscope", ExaminationTool.MeasurementType.Pleochroism, 16.4f, topDeskY, false);
        CreateDeskStation("Polariscope", ExaminationTool.MeasurementType.OpticalCharacter, 19.0f, topDeskY, false);
        CreateDeskStation("Spectroscope", ExaminationTool.MeasurementType.AbsorptionSpectrum, 21.6f, topDeskY, false);
        CreateDeskStation("Microscope", ExaminationTool.MeasurementType.Inclusions, 24.2f, topDeskY, false);
        // The computer that receives the microphotographs, right next to the microscope.
        CreateComputerDesk(26.8f, topDeskY);

        // The conoscope is built into the polariscope; one UV cabinet with both lamps replaces the two UV stations.
        CreateDeskStation("CabineUV", ExaminationTool.MeasurementType.FluorescenceLongWave, 9.3f, bottomDeskY, true);
        CreateDeskStation("FiltreChelsea", ExaminationTool.MeasurementType.ChelseaFilter, 12.0f, bottomDeskY, true);
        CreateDeskStation("Micrometre", ExaminationTool.MeasurementType.Dimensions, 14.7f, bottomDeskY, true);
        CreateDeskStation("BalanceCarat", ExaminationTool.MeasurementType.Weight, 17.4f, bottomDeskY, true);
        CreateDeskStation("Aimant", ExaminationTool.MeasurementType.Magnetism, 20.1f, bottomDeskY, true);
        // Bought at the GemStore: these two desks only appear once the tools are owned.
        CreateDeskStation("TesteurThermique", ExaminationTool.MeasurementType.ThermalConductivity, 22.8f, bottomDeskY, true, true);
        CreateDeskStation("TesteurElectrique", ExaminationTool.MeasurementType.ElectricalConductivity, 25.5f, bottomDeskY, true, true);

        CreateDirector(new Vector3(26, 0, 0f));
        DecorateHome();

        CreateUI(true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Home.unity");
        AddSceneToBuildSettings("Assets/Scenes/Home.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Home scene created at Assets/Scenes/Home.unity");
    }

    // A free-standing red post box (grounded sprite: feet at its position), with a wider trigger in front of it.
    private static void CreateMailbox(Vector3 position, bool personal = false)
    {
        var mailboxSprite = LoadPackSpriteGrounded("Assets/Art/Sprites/furniture_mailbox.png");
        var mailbox = new GameObject(personal ? "Boite aux lettres perso" : "Mailbox");
        var mailboxSr = mailbox.AddComponent<SpriteRenderer>();
        var mailboxCol = mailbox.AddComponent<BoxCollider2D>();
        mailboxCol.isTrigger = true;
        var mailboxComponent = mailbox.AddComponent<Mailbox>();
        if (personal)
        {
            var mailboxSo = new SerializedObject(mailboxComponent);
            mailboxSo.FindProperty("kind").enumValueIndex = (int)Mailbox.MailboxKind.Personal;
            mailboxSo.ApplyModifiedProperties();
        }
        if (mailboxSprite != null)
        {
            mailboxSr.sprite = mailboxSprite;
            mailboxCol.size = new Vector2(1.6f, 1.6f);
            mailboxCol.offset = new Vector2(0f, 0.5f);
            mailbox.transform.position = position;
        }
        else
        {
            mailboxSr.sprite = GetOrCreatePixelSprite("mailbox_pixel", MailboxPattern, MailboxPalette);
            mailboxCol.size = new Vector2(1.2f, 1.2f);
            mailbox.transform.position = position - new Vector3(0f, 0.1f, 0f);
        }
        mailboxSr.sortingOrder = 4;
    }

    [MenuItem("GemLab/Setup Player House Scene")]
    public static void CreatePlayerHouseScene()
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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("home", Color.white, "floor_pixel", FloorPixel);
        var wallTile = GetInteriorSideWallTile("home", InteriorSideWallTint, "wall_pixel", WallPixel);

        for (var x = HouseMinX; x <= HouseMaxX; x++)
        {
            for (var y = HouseMinY; y <= HouseMaxY; y++)
            {
                var isPerimeter = x == HouseMinX || x == HouseMaxX || y == HouseMinY || y == HouseMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        PaintInteriorTopWall(wallsTilemap, HouseMinX, HouseMaxX, HouseMaxY, "home", Color.white);

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0f, -1.2f, 0f);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, HouseMinY + 1, 0f), "SpawnPoint_MaisonJoueur");

        // Against the back wall: bed, wardrobe, desk, personal chest. Post box by the door.
        CreateBed(new Vector3(-3.5f, 1.4f, 0f));
        PlaceFurniture("wardrobe_a", new Vector3(-1.3f, HouseMaxY - 1f, 0f));
        PlaceFurniture("desk_a", new Vector3(1.3f, HouseMaxY - 1f, 0f));
        CreateChest(new Vector3(3.5f, 1.5f, 0f));
        CreateMailbox(new Vector3(3.5f, HouseMinY + 1.1f, 0f), true);

        PlaceWallDecor("window_white", -3, HouseMaxY);
        PlaceWallDecor("window_white", 3, HouseMaxY);
        PlaceRug("rug_red", new Vector3(0f, -0.4f, 0f));
        PlaceFurniture("plant_small", new Vector3(-4.0f, HouseMinY + 1.1f, 0f));

        CreateUI(true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MaisonJoueur.unity");
        AddSceneToBuildSettings("Assets/Scenes/MaisonJoueur.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Player house scene created at Assets/Scenes/MaisonJoueur.unity");
    }

    [MenuItem("GemLab/Setup GemStore Scene")]
    public static void CreateGemStoreScene()
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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("lab", Color.white, "floor_pixel", FloorPixel);
        var wallTile = GetInteriorSideWallTile("lab", InteriorSideWallTint, "wall_pixel", WallPixel);

        for (var x = ShopMinX; x <= ShopMaxX; x++)
        {
            for (var y = ShopMinY; y <= ShopMaxY; y++)
            {
                var isPerimeter = x == ShopMinX || x == ShopMaxX || y == ShopMinY || y == ShopMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        PaintInteriorTopWall(wallsTilemap, ShopMinX, ShopMaxX, ShopMaxY, "lab", Color.white);

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0f, ShopMinY + 2f, 0f);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, ShopMinY + 1, 0f), "SpawnPoint_GemStore");

        // Shelves against the back wall and the left wall: the items for sale will be placed on them later.
        var backWallY = ShopMaxY - 1f;
        var backShelves = new List<GameObject>();
        foreach (var shelfX in new[] { -5.5f, -2.5f, 0.5f, 5.5f })
            backShelves.Add(CreateShopShelf("gemstore_shelf", new Vector3(shelfX, backWallY, 0f), new[] { -1.0f, 0f, 1.0f }));
        var sideShelves = new List<GameObject>();
        foreach (var shelfY in new[] { -3.0f, -1.0f, 1.0f })
            sideShelves.Add(CreateShopShelf("gemstore_shelf_side", new Vector3(ShopMinX + 1.5f, shelfY, 0f), new[] { 0f }));

        // First items for sale: the two diamond testers, bought once and then installed in the home lab.
        CreateShopItem(backShelves[0], "Slot_0", "TesteurThermique", "Diamond Selector II (testeur thermique)", 2000,
            "Sonde de conductivité thermique. Le diamant évacue la chaleur bien mieux que la plupart de ses imitations : l'appareil les distingue en quelques secondes.\n\nAttention : la moissanite conduit aussi la chaleur, un second test sera nécessaire.");
        CreateShopItem(backShelves[0], "Slot_1", "TesteurElectrique", "Testeur Moissanite / Diamant (conductivité électrique)", 2000,
            "Sonde de conductivité électrique. Le diamant est un isolant, la moissanite synthétique conduit l'électricité : c'est le test qui les départage.");

        // Microscope upgrades: bought once, they appear on the microscope of the home lab.
        // Field equipment: bought items go straight into the player's bag (H).
        CreateShopItem(backShelves[0], "Slot_2", "Maglite", "Maglite", 159,
            "Lampe torche compacte et puissante. Indispensable pour éclairer une gemme (diaphanéité, inclusions, éclat) en dehors du laboratoire.",
            "", true);
        CreateShopItem(backShelves[1], "Slot_2", "SpectroscopePoche", "Spectroscope de poche", 129,
            "Spectroscope à réseau de diffraction : décompose la lumière traversant la gemme pour montrer ses bandes d'absorption. Se glisse dans le sac et s'utilise partout.",
            "", true);
        CreateShopItem(backShelves[2], "Slot_0", "Loupe10X", "Loupe 10× (Hastings)", 59,
            "Loupe de poche à triplet, grossissement ×10 : l'outil de base du gemmologue. Se glisse dans le sac et s'utilise partout.",
            "", true);
        CreateShopItem(backShelves[2], "Slot_1", "Loupe20X", "Loupe 20× (triplet)", 69,
            "Loupe de poche à triplet, grossissement ×20 : pour distinguer les fines inclusions. Se glisse dans le sac et s'utilise partout.",
            "", true);
        CreateShopItem(backShelves[2], "Slot_2", "FiltreChelseaPortable", "Filtre Chelsea", 79,
            "Filtre dichromatique ne laissant passer que le rouge profond et le vert-jaune, pour repérer certaines gemmes colorées par le chrome. Se glisse dans le sac.",
            "", true);
        CreateShopItem(sideShelves[0], "Slot_0", "BalanceCaratPoche", "Balance à carat de poche", 199,
            "Petite balance électronique de précision, donnant le poids d'une gemme en carats. Se glisse dans le sac et s'utilise partout.",
            "", true);
        CreateShopItem(sideShelves[0], "Slot_1", "LampeUVLongs", "Lampe UV longs (365 nm)", 249,
            "Torche à LED ultraviolettes de 365 nm : révèle la fluorescence de certaines gemmes dans la pénombre. Se glisse dans le sac et s'utilise partout.",
            "", true);
        CreateShopItem(sideShelves[1], "Slot_0", "Chamoisine", "Chamoisine (gem cloth)", 15,
            "Carré de feutre doux pour essuyer une gemme sans la rayer : poussières, traces de doigts et résidus gras disparaissent avant l'examen. Se glisse dans le sac.",
            "", true);
        CreateShopItem(sideShelves[1], "Slot_1", "AimantGemmologie", "Aimant de gemmologie", 5,
            "Aimant au néodyme très puissant : il attire les gemmes riches en fer (grenats, par exemple) et sert à un premier tri rapide sur le terrain. Se glisse dans le sac.",
            "", true);
        CreateShopItem(sideShelves[2], "Slot_0", "DichroscopeCalcite", "Dichroscope à calcite", 99,
            "Deux fenêtres côte à côte à travers un cristal de calcite : on compare les deux couleurs d'un pléochroïsme en tournant la gemme. Se glisse dans le sac.",
            "", true);
        CreateShopItem(sideShelves[2], "Slot_1", "DichroscopeLondres", "Dichroscope de Londres", 90,
            "Dichroscope tubulaire classique (modèle londonien) : il sépare la lumière en deux faisceaux polarisés pour révéler le pléochroïsme. Se glisse dans le sac.",
            "", true);

        CreateShopItem(backShelves[1], "Slot_0", "Objectif60X", "Objectif ×60 pour microscope", 6499,
            "Objectif à forte ouverture numérique (60x / 0,85). S'ajoute au microscope du laboratoire à côté des objectifs ×10, ×20 et ×40.\n\nLes plus fines inclusions deviennent visibles, mais la mise au point demande plus de précision.",
            "Objectif ×60 acheté ! Il est monté sur le microscope du laboratoire.");
        CreateShopItem(backShelves[1], "Slot_1", "CameraMicroscope", "Caméra pour microscope", 8999,
            "Caméra numérique à fixer sur le microscope. Elle permet de prendre en photo ce que l'on observe : les images sont envoyées directement à l'ordinateur du laboratoire.",
            "Caméra achetée ! Elle est fixée sur le microscope : un bouton « Photographier » apparaît.");

        // Counter along the right side, the clerk standing behind it.
        CreateShopCounter(new Vector3(4.5f, -1.5f, 0f), 4);
        CreateShopkeeper(new Vector3(6.1f, -1.0f, 0f));

        PlaceWallDecor("window_white", 3, ShopMaxY);
        PlaceRug("rug_gold", new Vector3(-1.0f, -1.2f, 0f));
        PlaceFurniture("plant_small", new Vector3(-6.3f, ShopMinY + 1.1f, 0f));
        PlaceFurniture("plant_small", new Vector3(6.3f, ShopMinY + 1.1f, 0f));

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GemStore.unity");
        AddSceneToBuildSettings("Assets/Scenes/GemStore.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("GemStore scene created at Assets/Scenes/GemStore.unity");
    }

    // Wooden shelf unit. Each compartment gets an empty "Slot_*" child (on the shelf boards) where a sale item will be put.
    private static GameObject CreateShopShelf(string spriteName, Vector3 feet, float[] slotXs)
    {
        var shelf = PlaceFurniture(spriteName, feet);
        if (shelf == null) return null;
        shelf.name = "Etagere";

        var slotIndex = 0;
        foreach (var boardY in new[] { 0.4375f, 1.125f })
        {
            foreach (var slotX in slotXs)
            {
                var slot = new GameObject($"Slot_{slotIndex++}");
                slot.transform.SetParent(shelf.transform, false);
                slot.transform.localPosition = new Vector3(slotX, boardY, 0f);
            }
        }
        return shelf;
    }

    // A purchasable instrument standing on a shelf slot. `slotName` is one of the shelf's "Slot_*" children.
    // `addsToBag`: the item goes into the player's bag (field equipment) instead of the lab. The item is clicked with the mouse.
    private static void CreateShopItem(GameObject shelf, string slotName, string toolId, string displayName, int price, string description,
        string purchaseMessage = "", bool addsToBag = false)
    {
        if (shelf == null) return;
        var slot = shelf.transform.Find(slotName);
        var spritePath = addsToBag ? $"Assets/Resources/BagIcons/{toolId}.png" : $"Assets/Art/Sprites/shopitem_{toolId}.png";
        var sprite = LoadPackSpriteGrounded(spritePath);
        if (slot == null || sprite == null) return;

        var item = new GameObject($"Article_{toolId}");
        item.transform.position = slot.position;
        var sr = item.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        // Click area around the sprite (a trigger: it never blocks the player).
        var clickArea = item.AddComponent<BoxCollider2D>();
        clickArea.isTrigger = true;
        clickArea.size = sprite.bounds.size + new Vector3(0.15f, 0.15f, 0f);
        clickArea.offset = new Vector2(0f, sprite.bounds.size.y / 2f);

        var shopItem = item.AddComponent<ShopItem>();
        var so = new SerializedObject(shopItem);
        so.FindProperty("toolId").stringValue = toolId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("price").intValue = price;
        so.FindProperty("purchaseMessage").stringValue = purchaseMessage;
        so.FindProperty("addsToBag").boolValue = addsToBag;
        so.ApplyModifiedProperties();
    }

    // A counter made of `length` 1x1 tiles running north-south, `bottom` being the centre of the lowest tile row.
    private static void CreateShopCounter(Vector3 bottomCenter, int length)
    {
        for (var i = 0; i < length; i++)
        {
            var kind = i == 0 ? "bottom" : (i == length - 1 ? "top" : "mid");
            var sprite = LoadPackSprite($"{InteriorSpriteRoot}prop_gemstore_counter_{kind}.png");
            if (sprite == null) continue;

            var piece = new GameObject("Comptoir");
            var sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;
            piece.transform.position = bottomCenter + new Vector3(0f, i, 0f);

            var solid = piece.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, 1f);
        }

        var registerSprite = LoadPackSprite($"{InteriorSpriteRoot}prop_gemstore_register.png");
        if (registerSprite != null)
        {
            var register = new GameObject("Caisse");
            var registerSr = register.AddComponent<SpriteRenderer>();
            registerSr.sprite = registerSprite;
            registerSr.sortingOrder = 5;
            register.transform.position = bottomCenter + new Vector3(0.15f, length - 1.6f, 0f);
        }
    }

    private static void CreateShopkeeper(Vector3 feet)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/npc_merchant1.png");
        var clerk = new GameObject("Vendeur GemStore");
        var sr = clerk.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : GetOrCreatePixelSprite("directeur_pixel", DirectorPattern, DirectorPalette);
        sr.sortingOrder = 4;
        clerk.transform.position = feet;

        // Wide trigger reaching over the counter, so the player can talk from the customer side.
        var trigger = clerk.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(5f, 3.5f);
        trigger.offset = new Vector2(-2.5f, 0.8f);

        var solid = clerk.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(0.7f, 0.4f);
        solid.offset = new Vector2(0f, 0.2f);

        clerk.AddComponent<ShopkeeperNPC>();
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
        pixelPerfect.refResolutionX = 896;
        pixelPerfect.refResolutionY = 504;
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
        var pathTileAlt = GetOrCreateTileFromSprite("path_pack_alt", LoadPackSprite(PackTerrains + "Grass_3_9.png"));
        var sidewalkTile = GetOrCreateTileFromSprite("sidewalk_pack", LoadPackSprite(PackCityTerrains + "Sidewalk_1_9.png"));

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

        void PaintStreetRect(int minX, int maxX, int minY, int maxY, bool vertical)
        {
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var isSidewalk = vertical ? (x == minX || x == maxX) : (y == minY || y == maxY);
                    var tile = isSidewalk ? sidewalkTile : ((x + y) % 2 == 0 ? pathTile : pathTileAlt);
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        // Rural dirt paths first (long path to the character's house, path to Vincent Pardieu's cabin).
        PaintStreetRect(-53, -51, -49, -20, true);
        PaintStreetRect(-58, -40, 3, 6, false);
        PaintStreetRect(-57, -55, 6, 9, true);

        // Asphalt roads: sidewalk on both edges, dashed line along the middle. Crossings stay plain asphalt.
        var asphaltTiles = new[]
        {
            GetOrCreateTileFromSprite("asphalt_a", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_16.png")),
            GetOrCreateTileFromSprite("asphalt_b", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_17.png")),
            GetOrCreateTileFromSprite("asphalt_c", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_18.png")),
        };
        var dashHTiles = new[]
        {
            GetOrCreateTileFromSprite("asphalt_dash_h1", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_2.png")),
            GetOrCreateTileFromSprite("asphalt_dash_h2", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_6.png")),
        };
        var dashVTiles = new[]
        {
            GetOrCreateTileFromSprite("asphalt_dash_v1", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_4.png")),
            GetOrCreateTileFromSprite("asphalt_dash_v2", LoadPackSprite(PackCityTerrains + "Asphalt_1_Variation_8.png")),
        };
        var asphaltSet = new HashSet<TileBase>(asphaltTiles);
        foreach (var t in dashHTiles) asphaltSet.Add(t);
        foreach (var t in dashVTiles) asphaltSet.Add(t);

        void PaintRoad(int minX, int maxX, int minY, int maxY, bool vertical)
        {
            var mid = vertical ? (minX + maxX) / 2 : (minY + maxY) / 2;
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var existing = groundTilemap.GetTile(cell);
                    var isEdge = vertical ? (x == minX || x == maxX) : (y == minY || y == maxY);
                    if (isEdge)
                    {
                        if (!asphaltSet.Contains(existing)) groundTilemap.SetTile(cell, sidewalkTile);
                        continue;
                    }

                    var isCenter = vertical ? x == mid : y == mid;
                    if (isCenter && !asphaltSet.Contains(existing))
                        groundTilemap.SetTile(cell, vertical ? dashVTiles[Mathf.Abs(y) % 2] : dashHTiles[Mathf.Abs(x) % 2]);
                    else
                        groundTilemap.SetTile(cell, asphaltTiles[Mathf.Abs(x * 7 + y * 13) % asphaltTiles.Length]);
                }
            }
        }

        PaintRoad(-65, 65, 23, 27, false);
        PaintRoad(-55, 55, -20, -16, false);
        PaintRoad(-40, 40, 2, 6, false);
        PaintRoad(-45, 45, -40, -36, false);
        // Airport on the right edge: street in front of the terminal, then the runway.
        PaintRoad(40, 63, 2, 6, false);
        PaintRoad(-3, 3, -48, 27, true);
        PaintRunway(groundTilemap, 64, 68, -13, 21);

        // River crossing the whole town from west to east, with a stone bridge on the main road
        // and a wooden footbridge on the path to the character's house.
        const int RiverMinY = -29;
        const int RiverMaxY = -24;
        var waterGO = new GameObject("Water", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        waterGO.transform.SetParent(gridGO.transform);
        var waterTilemap = waterGO.GetComponent<Tilemap>();
        // Above the ground layer: the grass tiles underneath would otherwise be drawn over the water (same sorting order).
        waterGO.GetComponent<TilemapRenderer>().sortingOrder = 1;
        var shallowTile = GetOrCreateTileFromSprite("water_shallow", LoadPackSprite(PackTerrains + "Deep_Water_1_22.png"));
        var deepTiles = new[]
        {
            GetOrCreateTileFromSprite("water_deep_a", LoadPackSprite(PackTerrains + "Deep_Water_1_9.png")),
            GetOrCreateTileFromSprite("water_deep_b", LoadPackSprite(PackTerrains + "Deep_Water_1_10.png")),
        };
        var topEdgeTiles = new[]
        {
            GetOrCreateTileFromSprite("water_edge_top_1", LoadPackSprite(PackTerrains + "Deep_Water_1_1.png")),
            GetOrCreateTileFromSprite("water_edge_top_2", LoadPackSprite(PackTerrains + "Deep_Water_1_2.png")),
            GetOrCreateTileFromSprite("water_edge_top_3", LoadPackSprite(PackTerrains + "Deep_Water_1_3.png")),
        };
        var bottomEdgeTile = GetOrCreateTileFromSprite("water_edge_bottom", LoadPackSprite(PackTerrains + "Deep_Water_1_6.png"));
        var plankTile = GetOrCreateTileFromSprite("bridge_plank", LoadPackSprite("Assets/Art/Sprites/bridge_plank.png"));

        for (var x = TownMinX + 1; x <= TownMaxX - 1; x++)
        {
            var onMainBridge = x >= -3 && x <= 3;
            var onFootbridge = x >= -53 && x <= -51;
            for (var y = RiverMinY; y <= RiverMaxY; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (onFootbridge)
                {
                    groundTilemap.SetTile(cell, plankTile);
                    continue;
                }
                if (onMainBridge) continue;

                TileBase tile;
                if (y == RiverMaxY || y == RiverMinY) tile = shallowTile;
                else if (y == RiverMaxY - 1) tile = topEdgeTiles[Mathf.Abs(x) % topEdgeTiles.Length];
                else if (y == RiverMinY + 1) tile = bottomEdgeTile;
                else tile = deepTiles[(Mathf.Abs(x * 5 + y) % 9 == 0) ? 1 : 0];
                waterTilemap.SetTile(cell, tile);
            }
        }

        // Bridge parapets: thin strips along both outer edges (the water colliders keep the player on the deck).
        var stoneRail = LoadPackSpriteGrounded("Assets/Art/Sprites/bridge_rail_stone.png");
        var woodRail = LoadPackSpriteGrounded("Assets/Art/Sprites/bridge_rail_wood.png");
        for (var y = RiverMinY; y <= RiverMaxY; y++)
        {
            PlaceRail(stoneRail, new Vector3(-3f + 0.094f, y, 0f));
            PlaceRail(stoneRail, new Vector3(4f - 0.094f, y, 0f));
            PlaceRail(woodRail, new Vector3(-53f + 0.094f, y, 0f));
            PlaceRail(woodRail, new Vector3(-50f - 0.094f, y, 0f));
        }

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(-55, 25, 0f);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateRealBuilding("Maison & Bureau de Gemmologie", new Vector3(-55, 28, 0f), HouseStyle.Chalet, "Home", true);
        CreateRealBuilding("Laboratoire de Gemmologie", new Vector3(-30, 28, 0f), HouseStyle.Modern, "Laboratoire", true);
        CreateRealBuilding("Atelier Lapidaire", new Vector3(-5, 28, 0f), HouseStyle.Chalet, "AtelierLapidaire", true);
        CreateRealBuilding("Musee", new Vector3(20, 28, 0f), HouseStyle.Modern, "Musee", true);
        CreateRealBuilding("Quartier des Diamantaires", new Vector3(45, 28, 0f), HouseStyle.Chalet, "", false);

        CreateRealBuilding("Marche aux Pierres", new Vector3(-45, -15, 0f), HouseStyle.Modern, "Marche", true);
        CreateRealBuilding("Ecole", new Vector3(-15, -15, 0f), HouseStyle.Chalet, "", false);
        CreateRealBuilding("Marche aux Brutes", new Vector3(15, -15, 0f), HouseStyle.Modern, "MarcheBrutes", true);
        CreateRealBuilding("Le Manoir", new Vector3(45, -15, 0f), HouseStyle.Chalet, "Manoir", true);

        // Decorative for now: no interior yet, the door shows the usual "not accessible" message.
        CreateRealBuilding("Bijouterie", new Vector3(-22, 7, 0f), HouseStyle.Chalet, "", false);
        CreateRealBuilding("Banque et Encheres", new Vector3(22, 7, 0f), HouseStyle.Modern, "", false);

        CreateDealership(new Vector3(28, -35, 0f));
        CreateVincentPardieuHouse(new Vector3(-58, 10, 0f));
        CreateLibrary(new Vector3(-41, 7, 0f));
        CreateGemStore(new Vector3(35, 7, 0f));
        CreatePersonalHouse(new Vector3(-52, -50, 0f));
        CreateAirport(new Vector3(58, 8, 0f), new Vector3(46, 8, 0f), new Vector3(59, -5, 0f));

        CreateTunnelEntrance(new Vector3(0, -50, 0f));

        CreateNamedSpawnPoint("SpawnPoint_Home", new Vector3(-55, 26, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Laboratoire", new Vector3(-30, 26, 0f));
        CreateNamedSpawnPoint("SpawnPoint_AtelierLapidaire", new Vector3(-5, 26, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Musee", new Vector3(20, 26, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Marche", new Vector3(-45, -17, 0f));
        CreateNamedSpawnPoint("SpawnPoint_MarcheBrutes", new Vector3(15, -17, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Manoir", new Vector3(45, -17, 0f));
        CreateNamedSpawnPoint("SpawnPoint_Tunnel", new Vector3(0, -47, 0f));
        CreateNamedSpawnPoint("SpawnPoint_GemStore", new Vector3(34f, 5f, 0f));
        CreateNamedSpawnPoint("SpawnPoint_MaisonJoueur", new Vector3(-51.2f, -52f, 0f));

        for (var i = 0; i < 5; i++)
            CreateFence(new Vector3(-59 + i, 22, 0f));

        // Footprints of everything placed so far (buildings, cars, runway...): scenery must stay clear of them.
        var occupied = new List<Rect>();
        foreach (var existingRenderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            var b = existingRenderer.bounds;
            occupied.Add(new Rect(b.min.x - 1.5f, b.min.y - 1.5f, b.size.x + 3f, b.size.y + 3f));
        }

        bool GroundIsGrass(float px, float py, int radius)
        {
            var cx = Mathf.FloorToInt(px);
            var cy = Mathf.FloorToInt(py);
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var cell = new Vector3Int(cx + dx, cy + dy, 0);
                    if (groundTilemap.GetTile(cell) != grassTile || waterTilemap.HasTile(cell)) return false;
                }
            }
            return true;
        }

        bool AreaIsFree(Rect area)
        {
            foreach (var r in occupied)
                if (r.Overlaps(area)) return false;
            return true;
        }

        bool AreaIsGrass(Rect area)
        {
            for (var cx = Mathf.FloorToInt(area.xMin); cx <= Mathf.FloorToInt(area.xMax); cx++)
                for (var cy = Mathf.FloorToInt(area.yMin); cy <= Mathf.FloorToInt(area.yMax); cy++)
                {
                    var cell = new Vector3Int(cx, cy, 0);
                    if (groundTilemap.GetTile(cell) != grassTile || waterTilemap.HasTile(cell)) return false;
                }
            return true;
        }

        // Places a ground-anchored prop if its footprint (width x depth, in tiles) is clear. Returns whether it was placed.
        bool TryPlace(string spritePath, float fx, float fy, float footW, float footD, string name,
                      float colW = 0f, float colH = 0f, bool needGrass = true)
        {
            var foot = new Rect(fx - footW / 2f, fy - 0.2f, footW, footD);
            if (!AreaIsFree(foot)) return false;
            if (needGrass && !AreaIsGrass(foot)) return false;
            var go = PlaceTownProp(spritePath, new Vector3(fx, fy, 0f), name, colW, colH);
            if (go == null) return false;
            occupied.Add(foot);
            return true;
        }

        var rng = new System.Random(20260);
        float Rf(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        // --- East park: paved plaza with a fountain, benches, flower beds and oaks ---
        // (the buildings on both sides are large: the free corridor is x 4..14 east and x -11..-4 west of the main road)
        var eastPark = new Rect(4f, 8f, 10f, 14f);
        if (AreaIsFree(eastPark) && AreaIsGrass(eastPark))
        {
            for (var x = 7; x <= 11; x++)
                for (var y = 10; y <= 15; y++)
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), sidewalkTile);
            for (var x = 4; x <= 6; x++)
                for (var y = 12; y <= 13; y++)
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), sidewalkTile);

            TryPlace(PackCityProps + "Fountain_1.png", 9f, 11.2f, 2f, 1.4f, "Fontaine", 1.5f, 0.8f, false);
            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", 9f, 16.1f, 2f, 0.8f, "Banc", 1.7f, 0.4f, false);
            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", 9f, 9.2f, 2f, 0.8f, "Banc", 1.7f, 0.4f, false);
            TryPlace(PackCityProps + "Flower_Bush_4.png", 6.6f, 18.6f, 3f, 0.8f, "Massif");
            TryPlace(PackCityProps + "Flower_Bush_5.png", 12f, 18.6f, 3f, 0.8f, "Massif");
            TryPlace(PackCityProps + "Flower_Bush_6.png", 12.2f, 8.6f, 3f, 0.8f, "Massif");
            TryPlace(PackCityProps + "Flower_Bush_2.png", 5.8f, 9.0f, 2f, 0.8f, "Massif");
            TryPlace(PackCityProps + "Tree_11.png", 13.0f, 20.8f, 2.2f, 1f, "Arbre", 0.9f, 0.5f);
            TryPlace(PackCityProps + "Tree_12.png", 5.6f, 21.4f, 2.2f, 1f, "Arbre", 0.9f, 0.5f);
            TryPlace(PackCityProps + "Tree_1.png", 13.2f, 12.8f, 1.4f, 1f, "Arbre", 0.7f, 0.4f);
            CreateLamppost(new Vector3(6.3f, 14.2f, 0f));
            CreateLamppost(new Vector3(11.8f, 14.2f, 0f));
        }

        // --- West park: a little pond with benches and trees ---
        var westPark = new Rect(-11f, 8f, 7f, 14f);
        if (AreaIsFree(westPark) && AreaIsGrass(westPark))
        {
            for (var x = -10; x <= -6; x++)
            {
                for (var y = 11; y <= 15; y++)
                {
                    var rim = x == -10 || x == -6 || y == 11 || y == 15;
                    waterTilemap.SetTile(new Vector3Int(x, y, 0), rim ? shallowTile : deepTiles[0]);
                }
            }
            for (var x = -5; x <= -4; x++)
                for (var y = 12; y <= 13; y++)
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), sidewalkTile);

            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", -8f, 17.2f, 2f, 0.8f, "Banc", 1.7f, 0.4f);
            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", -8f, 9.4f, 2f, 0.8f, "Banc", 1.7f, 0.4f);
            TryPlace(PackCityProps + "Tree_11.png", -9.2f, 20.8f, 2.2f, 1f, "Arbre", 0.9f, 0.5f);
            TryPlace(PackCityProps + "Tree_12.png", -5.6f, 21.4f, 2.2f, 1f, "Arbre", 0.9f, 0.5f);
            TryPlace(PackCityProps + "Tree_2.png", -10.3f, 9.0f, 1.5f, 1f, "Arbre", 0.7f, 0.4f);
            TryPlace(PackCityProps + "Tree_5.png", -10.3f, 18.6f, 1.5f, 1f, "Cyprès", 0.7f, 0.4f);
            TryPlace(PackCityProps + "Tree_6.png", -5.6f, 17.6f, 1.5f, 1f, "Cyprès", 0.7f, 0.4f);
            TryPlace(PackCityProps + "Flower_Bush_1.png", -5.8f, 9.3f, 2f, 0.8f, "Massif");
        }

        // Planter trees along the main road.
        var planterTrees = new[] { PackCityProps + "Tree_9.png", PackCityProps + "Tree_10.png" };
        var planterIndex = 0;
        for (var y = -46; y <= 20; y += 6)
        {
            if ((y >= 0 && y <= 8) || (y >= -22 && y <= -14) || (y >= -42 && y <= -34) || (y >= -31 && y <= -22)) continue;
            if (y >= 10 && y <= 15) continue;
            TryPlace(planterTrees[planterIndex++ % 2], 4.9f, y, 1.8f, 1f, "Arbre en bac", 0.9f, 0.5f);
            TryPlace(planterTrees[planterIndex++ % 2], -4.9f, y + 1, 1.8f, 1f, "Arbre en bac", 0.9f, 0.5f);
        }

        // Garden along the path to the character's house: flower beds, bushes and a few lamps.
        for (var y = -46; y <= -31; y += 3)
        {
            TryPlace(PackCityProps + $"Flower_Bush_{1 + (y + 100) % 3}.png", -55.6f, y, 2f, 0.8f, "Massif");
            TryPlace(PackGarden + $"Bush_{18 + (y + 100) % 4}.png", -48.2f, y, 2f, 1f, "Buisson", 1.2f, 0.5f);
        }
        foreach (var lampY in new[] { -45f, -38f, -32f })
            CreateLamppost(new Vector3(-49.4f, lampY, 0f));

        // Flowers along Vincent Pardieu's path, and bushes beside the cabin.
        for (var x = -54; x <= -46; x += 3)
            TryPlace(FlowerSprites[Mathf.Abs(x) % FlowerSprites.Length], x, 7.3f, 1f, 0.6f, "Fleurs");

        // River banks: bushes, flowers and benches facing the water.
        foreach (var benchX in new[] { -12f, 12f, -36f, 36f })
        {
            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", benchX, -22.8f, 2f, 0.8f, "Banc", 1.7f, 0.4f);
            TryPlace(PackGarden + "Medium_Bench_Horizontal.png", benchX, -31.4f, 2f, 0.8f, "Banc", 1.7f, 0.4f);
        }
        var bankBushes = new[] { "Bush_1.png", "Bush_2.png", "Bush_3.png", "Bush_5.png", "Bush_18.png", "Bush_19.png" };
        for (var x = TownMinX + 4; x <= TownMaxX - 4; x += 3)
        {
            if (Rf(0f, 1f) < 0.35f) continue;
            var bankY = Rf(0f, 1f) < 0.5f ? Rf(-22.9f, -21.4f) : Rf(-31.8f, -30.2f);
            if (Rf(0f, 1f) < 0.6f)
                TryPlace(PackGarden + bankBushes[rng.Next(bankBushes.Length)], x + Rf(-1f, 1f), bankY, 1.4f, 0.8f, "Buisson", 0.8f, 0.4f);
            else
                TryPlace(FlowerSprites[rng.Next(FlowerSprites.Length)], x + Rf(-1f, 1f), bankY, 1f, 0.6f, "Fleurs");
        }

        // Groves and forest edges: denser near the town border and where the noise field is high.
        var forest = new[]
        {
            new { path = PackCityProps + "Tree_11.png", w = 2.2f, cw = 1.0f },
            new { path = PackCityProps + "Tree_12.png", w = 2.2f, cw = 1.0f },
            new { path = PackCityProps + "Tree_1.png", w = 1.5f, cw = 0.7f },
            new { path = PackCityProps + "Tree_2.png", w = 1.5f, cw = 0.7f },
            new { path = PackCityProps + "Tree_5.png", w = 1.5f, cw = 0.7f },
            new { path = PackCityProps + "Tree_6.png", w = 1.5f, cw = 0.7f },
        };
        var treesPlaced = 0;
        for (var attempt = 0; attempt < 6000 && treesPlaced < 260; attempt++)
        {
            var tx = Rf(TownMinX + 3f, TownMaxX - 3f);
            var ty = Rf(TownMinY + 3f, TownMaxY - 3f);
            var noise = Mathf.PerlinNoise(tx * 0.07f + 11.3f, ty * 0.07f + 5.7f);
            var edgeDistance = Mathf.Min(Mathf.Min(tx - TownMinX, TownMaxX - tx), Mathf.Min(ty - TownMinY, TownMaxY - ty));
            var chance = (noise > 0.52f ? 0.9f : 0.06f) + (edgeDistance < 10f ? 0.6f : 0f);
            if (Rf(0f, 1f) > chance) continue;
            if (!GroundIsGrass(tx, ty, 1)) continue;

            var pick = forest[rng.Next(forest.Length)];
            if (TryPlace(pick.path, tx, ty, pick.w, 1.1f, "Arbre", pick.cw, 0.45f)) treesPlaced++;
        }

        // Small bushes and flower tufts scattered on open grass.
        var scatter = 0;
        for (var attempt = 0; attempt < 3000 && scatter < 160; attempt++)
        {
            var sx = Rf(TownMinX + 3f, TownMaxX - 3f);
            var sy = Rf(TownMinY + 3f, TownMaxY - 3f);
            if (!GroundIsGrass(sx, sy, 1)) continue;
            var pickFlower = Rf(0f, 1f) < 0.65f;
            var ok = pickFlower
                ? TryPlace(FlowerSprites[rng.Next(FlowerSprites.Length)], sx, sy, 1f, 0.6f, "Fleurs")
                : TryPlace(PackGarden + bankBushes[rng.Next(bankBushes.Length)], sx, sy, 1.4f, 0.8f, "Buisson", 0.8f, 0.4f);
            if (ok) scatter++;
        }

        // Lamps and flower clusters in front of the buildings. Asphalt streets are one tile wider now:
        // the lamps stand on the sidewalk next to the buildings.
        var asphaltSpots = new[]
        {
            new Vector3(-55, 26, 0f), new Vector3(-30, 26, 0f), new Vector3(-5, 26, 0f),
            new Vector3(20, 26, 0f), new Vector3(45, 26, 0f),
            new Vector3(-45, -17, 0f), new Vector3(-15, -17, 0f), new Vector3(15, -17, 0f), new Vector3(45, -17, 0f),
            new Vector3(-22, 5, 0f), new Vector3(22, 5, 0f), new Vector3(28, -37, 0f),
            new Vector3(-41, 5, 0f), new Vector3(35, 5, 0f),
        };
        var otherSpots = new[]
        {
            new Vector3(-56, 8, 0f), new Vector3(46, 6, 0f), new Vector3(58, 6, 0f), new Vector3(-52, -47, 0f),
        };

        foreach (var pos in asphaltSpots)
            CreateLamppost(pos + new Vector3(4.5f, 1f, 0f));
        foreach (var pos in otherSpots)
            CreateLamppost(pos + new Vector3(4.5f, 0f, 0f));

        var flowerVariant = 0;
        foreach (var pos in asphaltSpots)
        {
            CreateFlowerCluster(pos + new Vector3(-4.5f, 1.1f, 0f), flowerVariant % 3);
            flowerVariant++;
        }
        foreach (var pos in otherSpots)
        {
            CreateFlowerCluster(pos + new Vector3(-4.5f, 0.3f, 0f), flowerVariant % 3);
            flowerVariant++;
        }

        // Townspeople strolling along the streets and the main road.
        var strollZones = new[]
        {
            new Rect(-60f, 23.4f, 120f, 3f), new Rect(-60f, 23.4f, 120f, 3f),
            new Rect(-38f, 2.4f, 76f, 3f), new Rect(-38f, 2.4f, 76f, 3f),
            new Rect(-50f, -19.6f, 100f, 3f), new Rect(-50f, -19.6f, 100f, 3f),
            new Rect(-44f, -39.6f, 88f, 3f),
            new Rect(-2f, -46f, 4f, 68f), new Rect(-2f, -46f, 4f, 68f), new Rect(-2f, -46f, 4f, 68f),
            new Rect(42f, 2.4f, 20f, 3f),
        };
        for (var i = 0; i < strollZones.Length; i++)
        {
            var zone = strollZones[i];
            var walker = new GameObject($"Passant {i + 1}", typeof(SpriteRenderer), typeof(TownWanderer));
            var wander = walker.GetComponent<TownWanderer>();
            wander.variant = i % 8;
            wander.zone = zone;
            wander.speed = Rf(1.1f, 1.9f);
            walker.transform.position = new Vector3(Rf(zone.xMin, zone.xMax), Rf(zone.yMin, zone.yMax), 0f);
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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("lab", Color.white, "labfloor_pixel", LabFloorPixel);
        var wallTile = GetInteriorSideWallTile("lab", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, LabMinX, LabMaxX, LabMaxY, "lab", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, LabMinY + 1, 0f), "SpawnPoint_Laboratoire");

        CreateExaminationStation("FTIR", ExaminationTool.MeasurementType.Treatment, new Vector3(-5, 2, 0f));
        CreateExaminationStation("Raman", ExaminationTool.MeasurementType.Authenticity, new Vector3(0, 2, 0f));
        CreateExaminationStation("Spectrophotometre", ExaminationTool.MeasurementType.ConfirmedOrigin, new Vector3(5, 2, 0f));
        CreateExaminationStation("DiamondView", ExaminationTool.MeasurementType.DiamondGrowthImaging, new Vector3(-5, -2, 0f));
        CreateExaminationStation("MEB", ExaminationTool.MeasurementType.GrowthStructure, new Vector3(0, -2, 0f));
        CreateExaminationStation("DiamondSure", ExaminationTool.MeasurementType.DiamondScreening, new Vector3(5, -2, 0f));

        DecorateLaboratory();

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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("market", Color.white, "labfloor_pixel", LabFloorPixel);
        var wallTile = GetInteriorSideWallTile("market", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, MarketMinX, MarketMaxX, MarketMaxY, "market", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
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

        DecorateMarket();

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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("rough", Color.white, "labfloor_pixel", LabFloorPixel);
        var wallTile = GetInteriorSideWallTile("rough", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, RoughMarketMinX, RoughMarketMaxX, RoughMarketMaxY, "rough", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
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

        DecorateRoughMarket();

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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("museum", Color.white, "labfloor_pixel", LabFloorPixel);
        var wallTile = GetInteriorSideWallTile("museum", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, MuseumMinX, MuseumMaxX, MuseumMaxY, "museum", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
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

        DecorateMuseum();

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

        var floorTile = GetInteriorFloorTile("tunnel", new Color(0.55f, 0.55f, 0.58f, 1f), "tunnelfloor_pixel", TunnelFloorPixel);
        var wallTile = GetInteriorSideWallTile("tunnel", new Color(0.40f, 0.40f, 0.44f, 1f), "tunnelwall_pixel", TunnelWallPixel);

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

        PaintInteriorTopWall(wallsTilemap, TunnelMinX, TunnelMaxX, TunnelMaxY, "tunnel", new Color(0.55f, 0.55f, 0.58f, 1f));

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("manoir", Color.white, "floor_pixel", FloorPixel);
        var wallTile = GetInteriorSideWallTile("manoir", InteriorSideWallTint, "wall_pixel", WallPixel);

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

        PaintInteriorTopWall(wallsTilemap, ManoirMinX, ManoirMaxX, ManoirMaxY, "manoir", Color.white);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, ManoirMinY + 1, 0f), "SpawnPoint_Manoir");

        CreateOldMan(new Vector3(-3, 1, 0f));
        CreateOldLady(new Vector3(3, 1, 0f));

        DecorateManoir();

        CreateUI(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Manoir.unity");
        AddSceneToBuildSettings("Assets/Scenes/Manoir.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Manoir scene created at Assets/Scenes/Manoir.unity");
    }

    private const int AtelierMinX = -10;
    private const int AtelierMaxX = 10;
    private const int AtelierMinY = -6;
    private const int AtelierMaxY = 6;

    [MenuItem("GemLab/Setup Atelier Lapidaire Scene")]
    public static void CreateAtelierLapidaireScene()
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
        pixelPerfect.refResolutionX = InteriorRefResolutionX;
        pixelPerfect.refResolutionY = InteriorRefResolutionY;
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

        var floorTile = GetInteriorFloorTile("atelier", Color.white, "floor_pixel", FloorPixel);
        var wallTile = GetInteriorSideWallTile("atelier", InteriorSideWallTint, "wall_pixel", WallPixel);

        for (var x = AtelierMinX; x <= AtelierMaxX; x++)
        {
            for (var y = AtelierMinY; y <= AtelierMaxY; y++)
            {
                var isPerimeter = x == AtelierMinX || x == AtelierMaxX || y == AtelierMinY || y == AtelierMaxY;
                var cell = new Vector3Int(x, y, 0);
                if (isPerimeter)
                    wallsTilemap.SetTile(cell, wallTile);
                else
                    groundTilemap.SetTile(cell, floorTile);
            }
        }

        PaintInteriorTopWall(wallsTilemap, AtelierMinX, AtelierMaxX, AtelierMaxY, "atelier", Color.white);

        var spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.position = new Vector3(0, AtelierMinY + 2, 0f);

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
        player.AddComponent<ReputationState>();
        player.AddComponent<LapidaryState>();
        player.AddComponent<PlayerCabinet>();
        player.AddComponent<PlayerOrderCabinet>();
        player.AddComponent<PlayerOwnedTools>();
        player.transform.position = spawnGO.transform.position;

        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);

        CreateMusicManager();

        CreateExitDoor(new Vector3(0, AtelierMinY + 1, 0f), "SpawnPoint_AtelierLapidaire");

        CreateLapidaryDesk(new Vector3(0, 1, 0f));
        CreateWindowPolisher(new Vector3(6, 3, 0f));

        DecorateAtelier();

        CreateUI(false);

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            try
            {
                canvas.AddComponent<LapidaryUI>();
                canvas.AddComponent<WindowPolisherUI>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Lapidary UI setup failed: {e.Message}");
            }
        }

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/AtelierLapidaire.unity");
        AddSceneToBuildSettings("Assets/Scenes/AtelierLapidaire.unity");

        AssetDatabase.SaveAssets();
        Debug.Log("Atelier Lapidaire scene created at Assets/Scenes/AtelierLapidaire.unity");
    }

    private static readonly Dictionary<char, Color> LapidaryPalette = new Dictionary<char, Color>
    {
        { 'H', new Color(0.55f, 0.55f, 0.58f) },
        { 'S', new Color(0.85f, 0.68f, 0.55f) },
        { 'E', new Color(0.10f, 0.10f, 0.10f) },
        { 'C', new Color(0.50f, 0.34f, 0.18f) },
        { 'D', new Color(0.36f, 0.24f, 0.12f) },
        { 'B', new Color(0.18f, 0.14f, 0.12f) },
    };

    private static readonly string[] WindowPolisherPattern =
    {
        Row(16, '.'),
        Row(16, '.'),
        Row(16, '.'),
        Row(6, '.', 4, 'W', 6, '.'),
        Row(4, '.', 8, 'W', 4, '.'),
        Row(3, '.', 4, 'W', 2, 'H', 4, 'W', 3, '.'),
        Row(3, '.', 3, 'W', 4, 'H', 3, 'W', 3, '.'),
        Row(3, '.', 4, 'W', 2, 'H', 4, 'W', 3, '.'),
        Row(4, '.', 8, 'W', 4, '.'),
        Row(6, '.', 4, 'W', 6, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 2, 'M', 8, 'D', 2, 'M', 2, '.'),
        Row(2, '.', 12, 'M', 2, '.'),
        Row(2, '.', 12, 'K', 2, '.'),
        Row(16, '.'),
    };

    private static readonly Dictionary<char, Color> WindowPolisherPalette = new Dictionary<char, Color>
    {
        { 'W', new Color(0.80f, 0.82f, 0.86f) },
        { 'H', new Color(0.40f, 0.40f, 0.45f) },
        { 'M', new Color(0.45f, 0.48f, 0.52f) },
        { 'D', new Color(0.20f, 0.22f, 0.25f) },
        { 'K', new Color(0.25f, 0.22f, 0.20f) },
    };

    private static void CreateWindowPolisher(Vector3 position)
    {
        var sprite = GetOrCreatePixelSprite("windowpolisher_pixel", WindowPolisherPattern, WindowPolisherPalette);

        var go = new GameObject("Polisseuse a fenetre");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var solid = go.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(1.2f, 0.7f);
        solid.offset = new Vector2(0f, -0.2f);

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.6f, 2.4f);
        trigger.offset = new Vector2(0f, -0.2f);

        go.AddComponent<WindowPolisher>();
        CreateBuildingLabel("Polisseuse (fenêtre)", position + new Vector3(0f, 1.0f, 0f));
    }

    private static void CreateLapidaryDesk(Vector3 position)
    {
        var tableSprite = GetOrCreatePixelSprite("markettable_pixel", MarketTablePattern, MarketTablePalette);

        var deskGO = new GameObject("Bureau du Lapidaire");
        deskGO.transform.position = position;
        for (var i = -1; i <= 1; i++)
        {
            var tableGO = new GameObject($"Table_{i + 1}");
            tableGO.transform.SetParent(deskGO.transform, false);
            tableGO.transform.localPosition = new Vector3(i, 0f, 0f);
            var tableSr = tableGO.AddComponent<SpriteRenderer>();
            tableSr.sprite = tableSprite;
            tableSr.sortingOrder = 4;
        }

        var solid = deskGO.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(3f, 0.6f);
        solid.offset = new Vector2(0f, 0.1f);

        var npcSprite = GetOrCreatePixelSprite("lapidary_pixel", PlayerPattern, LapidaryPalette);
        var npcGO = new GameObject("Lapidaire");
        var npcSr = npcGO.AddComponent<SpriteRenderer>();
        npcSr.sprite = npcSprite;
        npcSr.sortingOrder = 4;
        npcGO.transform.position = position + new Vector3(0f, 1.2f, 0f);

        var trigger = npcGO.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4f, 3.2f);
        trigger.offset = new Vector2(0f, -1.1f);

        npcGO.AddComponent<LapidaryNPC>();
        if (TryUseNpcSprite(npcGO, npcSr, trigger, "lapidary", position + new Vector3(0f, 1.2f, 0f)))
        {
            // Behind the desk, so its tables hide the lower body; keep the wide trigger in front of the desk.
            npcSr.sortingOrder = 3;
            trigger.offset = new Vector2(0f, -0.6f);
        }
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

        TryUseNpcSprite(go, sr, col, "oldman", position);
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

        TryUseNpcSprite(go, sr, col, "oldlady", position);
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
        var sprite = LoadPackSprite("Assets/Art/Sprites/furniture_chest.png")
                     ?? GetOrCreatePixelSprite("chest_pixel", ChestPattern, ChestPalette);

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

    // Wardrobe standing against the wall. Wooden ones keep donated stones; the blue ones are reserved for client orders.
    // `feet` is where it stands.
    private static void CreateArmoire(Vector3 feet, bool clientOrders = false)
    {
        var sprite = LoadPackSpriteGrounded(clientOrders
            ? "Assets/Art/Sprites/furniture_armoire_orders.png"
            : "Assets/Art/Sprites/furniture_armoire.png");
        if (sprite == null) return;

        var armoire = new GameObject(clientOrders ? "Armoire des commandes" : "Armoire des dons");
        var sr = armoire.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        armoire.transform.position = feet;

        var solid = armoire.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(1.1f, 0.6f);
        solid.offset = new Vector2(0f, 0.4f);

        var trigger = armoire.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 1.6f);
        trigger.offset = new Vector2(0f, -0.5f);

        var interactable = armoire.AddComponent<CabinetInteractable>();
        if (clientOrders)
        {
            var so = new SerializedObject(interactable);
            so.FindProperty("kind").enumValueIndex = (int)CabinetKind.ClientOrders;
            so.ApplyModifiedProperties();
        }
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

    private enum HouseStyle { Chalet, Modern }

    private static void CreateRealBuilding(string label, Vector3 position, HouseStyle style, string targetScene, bool implemented)
    {
        var isChalet = style == HouseStyle.Chalet;
        var spriteFile = isChalet ? "house_chalet.png" : "house_modern.png";
        var doorOffset = isChalet ? new Vector2(-6.5f, 6.5f) : new Vector2(-1.6f, 1.8f);
        var doorSize = isChalet ? new Vector2(3.5f, 3.5f) : new Vector2(3.5f, 3f);
        var labelHeight = isChalet ? 16.7f : 15.7f;

        var sprite = LoadPackSpriteGrounded($"Assets/Art/Sprites/{spriteFile}");
        if (sprite == null) return;

        var go = new GameObject(label);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = doorSize;
        col.offset = doorOffset;

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        var lockedProp = so.FindProperty("lockedMessage");
        if (sceneNameProp != null) sceneNameProp.stringValue = targetScene;
        if (implementedProp != null) implementedProp.boolValue = implemented;
        if (lockedProp != null) lockedProp.stringValue = $"{label} : ce lieu n'est pas encore accessible.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel(label, position + new Vector3(0, labelHeight, 0));
    }

    // Painted on the ground tilemap: asphalt with white edge lines and a dashed center line.
    private static void PaintRunway(Tilemap ground, int minX, int maxX, int minY, int maxY)
    {
        Color Asphalt(int x, int y) => ((x * 7 + y * 13) % 17 == 0) ? new Color(0.27f, 0.28f, 0.31f) : new Color(0.22f, 0.23f, 0.26f);

        var plain = GetOrCreateTile("runway_plain", (x, y) => Asphalt(x, y));
        var edgeLeft = GetOrCreateTile("runway_edge_left", (x, y) => x >= 3 && x <= 4 ? new Color(0.9f, 0.9f, 0.9f) : Asphalt(x, y));
        var edgeRight = GetOrCreateTile("runway_edge_right", (x, y) => x >= 27 && x <= 28 ? new Color(0.9f, 0.9f, 0.9f) : Asphalt(x, y));
        var dash = GetOrCreateTile("runway_dash", (x, y) => x >= 15 && x <= 16 && y >= 4 && y <= 27 ? new Color(0.9f, 0.9f, 0.9f) : Asphalt(x, y));

        var centerX = (minX + maxX) / 2;
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                Tile tile;
                if (x == minX) tile = edgeLeft;
                else if (x == maxX) tile = edgeRight;
                else if (x == centerX && y % 2 == 0) tile = dash;
                else tile = plain;
                ground.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    // Decorative airport: terminal hangar (locked for now, later the way to travel to the mines), control tower
    // and a parked helicopter. The runway itself is painted in the town scene.
    private static void CreateAirport(Vector3 terminalPos, Vector3 towerPos, Vector3 helicopterPos)
    {
        var terminal = LoadPackSpriteGrounded("Assets/Art/Sprites/airport_hangar.png");
        if (terminal != null)
        {
            var go = new GameObject("Aeroport");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = terminal;
            sr.sortingOrder = 4;
            go.transform.position = terminalPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(4f, 3f);
            col.offset = new Vector2(-0.3f, 1.2f);

            var door = go.AddComponent<DoorTrigger>();
            var so = new SerializedObject(door);
            var sceneNameProp = so.FindProperty("sceneName");
            var implementedProp = so.FindProperty("implemented");
            var lockedProp = so.FindProperty("lockedMessage");
            if (sceneNameProp != null) sceneNameProp.stringValue = "";
            if (implementedProp != null) implementedProp.boolValue = false;
            if (lockedProp != null) lockedProp.stringValue = "Aéroport : les vols vers les mines ne sont pas encore ouverts.";
            so.ApplyModifiedProperties();

            CreateBuildingLabel("Aeroport", terminalPos + new Vector3(0f, terminal.bounds.size.y + 0.5f, 0f));
        }

        var tower = LoadPackSpriteGrounded("Assets/Art/Sprites/airport_tower.png");
        if (tower != null)
        {
            var go = new GameObject("Tour de controle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tower;
            sr.sortingOrder = 4;
            go.transform.position = towerPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(tower.bounds.size.x * 0.8f, 1.2f);
            col.offset = new Vector2(0f, 0.6f);
        }

        var helicopter = LoadPackSpriteGrounded("Assets/Art/Sprites/airport_helicopter.png");
        if (helicopter != null)
        {
            var go = new GameObject("Helicoptere");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = helicopter;
            sr.sortingOrder = 4;
            go.transform.position = helicopterPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(helicopter.bounds.size.x * 0.6f, 1.6f);
            col.offset = new Vector2(0f, 1.2f);
        }
    }

    // The character's own little house, far from the town (decorative for now, door locked).
    private static void CreatePersonalHouse(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/house_personal.png");
        if (sprite == null) return;

        var go = new GameObject("Maison du personnage");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        // Collider values are in local units: the sprite is scaled by 2.
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 1.6f);
        col.offset = new Vector2(0.55f, 0.8f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        if (sceneNameProp != null) sceneNameProp.stringValue = "MaisonJoueur";
        if (implementedProp != null) implementedProp.boolValue = true;
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Maison du personnage", position + new Vector3(0f, sprite.bounds.size.y * 2f + 0.5f, 0f));
    }

    // GemStore: the gemology tool shop (sells tools and tool upgrades).
    private static void CreateGemStore(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/building_gemstore.png");
        if (sprite == null) return;

        var go = new GameObject("GemStore");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.4f, 2.4f);
        col.offset = new Vector2(-1.0f, 1.0f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        if (sceneNameProp != null) sceneNameProp.stringValue = "GemStore";
        if (implementedProp != null) implementedProp.boolValue = true;
        so.ApplyModifiedProperties();

        CreateBuildingLabel("GemStore", position + new Vector3(0f, sprite.bounds.size.y + 0.5f, 0f));
    }

    // Decorative library (brick building with a clock-less tower); locked until its interior exists.
    private static void CreateLibrary(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/building_library.png");
        if (sprite == null) return;

        var go = new GameObject("Bibliotheque");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3f, 3f);
        col.offset = new Vector2(-6f, 1.5f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        var lockedProp = so.FindProperty("lockedMessage");
        if (sceneNameProp != null) sceneNameProp.stringValue = "";
        if (implementedProp != null) implementedProp.boolValue = false;
        if (lockedProp != null) lockedProp.stringValue = "Bibliothèque : ce lieu n'est pas encore accessible.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Bibliotheque", position + new Vector3(0f, sprite.bounds.size.y + 0.5f, 0f));
    }

    // Small cabin away from the town center: home of Vincent Pardieu (NPC, not accessible yet).
    private static void CreateVincentPardieuHouse(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/house_cabin.png");
        if (sprite == null) return;

        var go = new GameObject("Maison de Vincent Pardieu");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3f, 3f);
        col.offset = new Vector2(1.6f, 1.5f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        var lockedProp = so.FindProperty("lockedMessage");
        if (sceneNameProp != null) sceneNameProp.stringValue = "";
        if (implementedProp != null) implementedProp.boolValue = false;
        if (lockedProp != null) lockedProp.stringValue = "Maison de Vincent Pardieu : personne ne répond pour l'instant.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Maison de Vincent Pardieu", position + new Vector3(0f, sprite.bounds.size.y + 0.5f, 0f));
    }

    // Decorative car dealership: showroom building (locked door) with a few cars parked beside it.
    private static void CreateDealership(Vector3 position)
    {
        var sprite = LoadPackSpriteGrounded("Assets/Art/Sprites/dealership_building.png");
        if (sprite == null) return;

        var go = new GameObject("Concessionnaire Automobile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = position;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4f, 3f);
        col.offset = new Vector2(-0.3f, 1.5f);

        var door = go.AddComponent<DoorTrigger>();
        var so = new SerializedObject(door);
        var sceneNameProp = so.FindProperty("sceneName");
        var implementedProp = so.FindProperty("implemented");
        var lockedProp = so.FindProperty("lockedMessage");
        if (sceneNameProp != null) sceneNameProp.stringValue = "";
        if (implementedProp != null) implementedProp.boolValue = false;
        if (lockedProp != null) lockedProp.stringValue = "Concessionnaire Automobile : ce lieu n'est pas encore accessible.";
        so.ApplyModifiedProperties();

        CreateBuildingLabel("Concessionnaire Auto", position + new Vector3(0f, sprite.bounds.size.y + 0.5f, 0f));

        var cars = new (int model, float dx)[] { (1, -20f), (4, -17f), (11, -14f), (13, 14f), (5, 17f) };
        foreach (var (model, dx) in cars)
        {
            var carSprite = LoadPackSpriteGrounded($"Assets/Art/Sprites/car_down_{model}.png");
            if (carSprite == null) continue;

            var car = new GameObject($"Voiture_{model}");
            var carSr = car.AddComponent<SpriteRenderer>();
            carSr.sprite = carSprite;
            carSr.sortingOrder = 4;
            car.transform.position = position + new Vector3(dx, 0.2f, 0f);

            var carCol = car.AddComponent<BoxCollider2D>();
            carCol.size = new Vector2(1.7f, 3.2f);
            carCol.offset = new Vector2(0f, 1.8f);
        }
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
        TryUseNpcSprite(go, sr, col, "tunnel", position);

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

    private static GameObject CreateExaminationStation(string toolName, ExaminationTool.MeasurementType measurementType, Vector3 position,
        Sprite spriteOverride = null)
    {
        var sprite = spriteOverride != null ? spriteOverride : GetToolSprite(toolName);

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
        return station;
    }

    // A tool standing on its own small desk, against a wall. `accessFromNorth` is for desks on the bottom wall,
    // where the player stands on the room side (north of the desk).
    // Desk with the lab computer. Pressing E in front of it tells how many microphotographs it holds (interface to come).
    private static void CreateComputerDesk(float x, float feetY)
    {
        var deskSprite = LoadPackSpriteGrounded("Assets/Art/Sprites/interior_prop_desk_a.png");
        if (deskSprite != null)
        {
            var desk = new GameObject("Bureau_Ordinateur");
            var deskSr = desk.AddComponent<SpriteRenderer>();
            deskSr.sprite = deskSprite;
            deskSr.sortingOrder = 4;
            desk.transform.position = new Vector3(x, feetY, 0f);

            var solid = desk.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1.5f, 1.2f);
            solid.offset = new Vector2(0f, 0.65f);
        }

        var pcSprite = LoadPackSprite("Assets/Art/Sprites/toolhd_Ordinateur.png");
        if (pcSprite == null) return;

        var pc = new GameObject("Ordinateur");
        var pcSr = pc.AddComponent<SpriteRenderer>();
        pcSr.sprite = pcSprite;
        pcSr.sortingOrder = 5;
        pc.transform.position = new Vector3(x, feetY + 0.8f + pcSprite.bounds.size.y / 2f, 0f);

        var trigger = pc.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.7f, 1.7f);
        trigger.offset = new Vector2(0f, -1.1f);

        pc.AddComponent<ComputerInteractable>();
    }

    // `requiresPurchase`: the desk and its tool only appear once the player has bought the tool at the GemStore.
    private static void CreateDeskStation(string toolName, ExaminationTool.MeasurementType measurementType, float x, float feetY, bool accessFromNorth,
        bool requiresPurchase = false)
    {
        var gated = new List<GameObject>();
        var deskSprite = LoadPackSpriteGrounded("Assets/Art/Sprites/interior_prop_desk_a.png");
        if (deskSprite != null)
        {
            var desk = new GameObject($"Bureau_{toolName}");
            gated.Add(desk);
            var deskSr = desk.AddComponent<SpriteRenderer>();
            deskSr.sprite = deskSprite;
            deskSr.sortingOrder = 4;
            desk.transform.position = new Vector3(x, feetY, 0f);

            var solid = desk.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1.5f, 1.2f);
            solid.offset = new Vector2(0f, 0.65f);
        }

        // The tool sits on the tabletop, drawn over the desk (detailed 48x40 sprite when available).
        var toolSprite = LoadPackSprite($"Assets/Art/Sprites/toolhd_{toolName}.png");
        var centerY = toolSprite != null ? feetY + 0.8f + toolSprite.bounds.size.y / 2f : feetY + 1.25f;
        var station = CreateExaminationStation(toolName, measurementType, new Vector3(x, centerY, 0f), toolSprite);
        station.GetComponent<SpriteRenderer>().sortingOrder = 5;

        // Wider trigger reaching the side the player stands on.
        var trigger = station.GetComponent<BoxCollider2D>();
        trigger.size = new Vector2(1.7f, 1.7f);
        trigger.offset = accessFromNorth ? new Vector2(0f, 0.9f) : new Vector2(0f, -1.1f);

        if (!requiresPurchase) return;
        gated.Add(station);
        var gate = new GameObject($"Acces_{toolName}").AddComponent<OwnedToolGate>();
        var gateSo = new SerializedObject(gate);
        gateSo.FindProperty("toolId").stringValue = toolName;
        var targets = gateSo.FindProperty("targets");
        targets.arraySize = gated.Count;
        for (var i = 0; i < gated.Count; i++)
            targets.GetArrayElementAtIndex(i).objectReferenceValue = gated[i];
        gateSo.ApplyModifiedProperties();
    }

    // NPC art: the free LimeZu characters recolored and accessorized into Assets/Art/Sprites/npc_<name>.png.
    // The sprites have their feet at the bottom pivot, whereas the old placeholders were centered: `centerPosition` is
    // where the old sprite's middle was, so the feet land half a unit below it.
    private static bool TryUseNpcSprite(GameObject go, SpriteRenderer sr, BoxCollider2D col, string npcName, Vector3 centerPosition)
    {
        var sprite = LoadPackSpriteGrounded($"Assets/Art/Sprites/npc_{npcName}.png");
        if (sprite == null) return false;

        sr.sprite = sprite;
        go.transform.position = centerPosition + new Vector3(0f, -0.5f, 0f);
        if (col != null) col.offset = new Vector2(0f, 0.5f);
        return true;
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
        if (TryUseNpcSprite(director, sr, col, "directeur", position))
            director.transform.localScale = Vector3.one;
        else
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
        var usesNpcArt = TryUseNpcSprite(merchantGO, merchantSr, null, $"merchant{vendorIndex % 4}", position + new Vector3(0f, 0.6f, 0f));
        // Behind the table, so the table hides the lower body.
        if (usesNpcArt) merchantSr.sortingOrder = 3;

        var flagGO = new GameObject("Flag", typeof(SpriteRenderer));
        flagGO.transform.SetParent(merchantGO.transform, false);
        flagGO.transform.localPosition = new Vector3(0f, usesNpcArt ? 2.0f : 0.9f, 0f);
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
        var usesNpcArt = TryUseNpcSprite(merchantGO, merchantSr, null, $"rough{vendorIndex % 4}", position + new Vector3(0f, 0.6f, 0f));
        // Behind the table, so the table hides the lower body.
        if (usesNpcArt) merchantSr.sortingOrder = 3;

        var flagGO = new GameObject("Flag", typeof(SpriteRenderer));
        flagGO.transform.SetParent(merchantGO.transform, false);
        flagGO.transform.localPosition = new Vector3(0f, usesNpcArt ? 2.0f : 0.9f, 0f);
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
        CreateUIText("MapHint", canvasGO.transform, "C : Carte",
            new Vector2(20, -164), new Vector2(300, 24), 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));

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
        CreateReputationUI(canvasGO.transform);

        try
        {
            CreateTownMapUI(canvasGO.transform);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CreateTownMapUI failed: {e}");
        }

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

    private static void CreateReputationUI(Transform canvasTransform)
    {
        var containerGO = new GameObject("ReputationGauge", typeof(RectTransform));
        containerGO.transform.SetParent(canvasTransform, false);
        var containerRect = containerGO.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(1f, 1f);
        containerRect.anchoredPosition = new Vector2(-20, -20);
        containerRect.sizeDelta = new Vector2(160, 36);

        var levelGO = CreateUIText("LevelText", containerGO.transform, "Niveau 1",
            Vector2.zero, new Vector2(160, 20), 14, TextAnchor.UpperRight, Color.white);
        var levelRect = levelGO.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1f, 1f);
        levelRect.anchorMax = new Vector2(1f, 1f);
        levelRect.pivot = new Vector2(1f, 1f);
        levelRect.anchoredPosition = Vector2.zero;

        var barBgGO = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        barBgGO.transform.SetParent(containerGO.transform, false);
        var barBgRect = barBgGO.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(1f, 1f);
        barBgRect.anchorMax = new Vector2(1f, 1f);
        barBgRect.pivot = new Vector2(1f, 1f);
        barBgRect.anchoredPosition = new Vector2(0, -22);
        barBgRect.sizeDelta = new Vector2(160, 14);
        barBgGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

        var barFillGO = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barBgGO.transform, false);
        var barFillRect = barFillGO.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.sizeDelta = Vector2.zero;
        barFillRect.anchoredPosition = Vector2.zero;
        var barFillImage = barFillGO.GetComponent<Image>();
        barFillImage.color = new Color(0.55f, 0.35f, 0.85f, 1f);
        barFillImage.type = Image.Type.Filled;
        barFillImage.fillMethod = Image.FillMethod.Horizontal;
        barFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFillImage.fillAmount = 0f;

        var reputationUI = containerGO.AddComponent<ReputationUI>();
        var reputationSo = new SerializedObject(reputationUI);
        var levelProp = reputationSo.FindProperty("levelText");
        var fillProp = reputationSo.FindProperty("fillImage");
        if (levelProp != null) levelProp.objectReferenceValue = levelGO.GetComponent<Text>();
        if (fillProp != null) fillProp.objectReferenceValue = barFillImage;
        reputationSo.ApplyModifiedProperties();
    }

    private static void CreateTownMapUI(Transform canvasTransform)
    {
        var panelGO = new GameObject("TownMapPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasTransform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 420);
        panelRect.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.07f, 0.97f);
        panelGO.SetActive(false);

        CreateUIText("Title", panelGO.transform, "Carte de la Ville",
            new Vector2(0, -14), new Vector2(460, 26), 18, TextAnchor.UpperCenter, Color.white);

        var mapAreaGO = new GameObject("MapArea", typeof(RectTransform), typeof(Image));
        mapAreaGO.transform.SetParent(panelGO.transform, false);
        var mapAreaRect = mapAreaGO.GetComponent<RectTransform>();
        mapAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapAreaRect.pivot = new Vector2(0.5f, 0.5f);
        mapAreaRect.sizeDelta = new Vector2(460, 330);
        mapAreaRect.anchoredPosition = new Vector2(0, -20);
        mapAreaGO.GetComponent<Image>().color = new Color(0.20f, 0.32f, 0.18f, 1f);

        var buildings = new (string name, float x, float y)[]
        {
            ("Maison", -55, 28), ("Laboratoire", -30, 28), ("Atelier Lapidaire", -5, 28),
            ("Musée", 20, 28), ("Quartier Diamantaires", 45, 28),
            ("Marché Pierres", -45, -15), ("École", -15, -15),
            ("Marché Brutes", 15, -15), ("Manoir", 45, -15),
            ("Bijouterie", -22, 7), ("Banque", 22, 7), ("Concessionnaire", 28, -35), ("Vincent Pardieu", -58, 10), ("Aéroport", 58, 8), ("Bibliothèque", -41, 7), ("GemStore", 35, 7), ("Maison perso", -52, -50),
            ("Tunnel", 0, -50),
        };

        var mapWidth = mapAreaRect.sizeDelta.x;
        var mapHeight = mapAreaRect.sizeDelta.y;

        float MapX(float worldX) => Mathf.InverseLerp(TownMinX, TownMaxX, worldX) * mapWidth - mapWidth / 2f;
        float MapY(float worldY) => Mathf.InverseLerp(TownMinY, TownMaxY, worldY) * mapHeight - mapHeight / 2f;

        foreach (var b in buildings)
        {
            var markerGO = new GameObject($"Marker_{b.name}", typeof(RectTransform), typeof(Image));
            markerGO.transform.SetParent(mapAreaGO.transform, false);
            var markerRect = markerGO.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(14, 14);
            markerRect.anchoredPosition = new Vector2(MapX(b.x), MapY(b.y));
            markerGO.GetComponent<Image>().color = new Color(0.75f, 0.55f, 0.25f, 1f);

            var labelGO = CreateUIText($"Label_{b.name}", mapAreaGO.transform, b.name,
                new Vector2(MapX(b.x), MapY(b.y) - 10), new Vector2(90, 16), 9, TextAnchor.UpperCenter, Color.white);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(MapX(b.x), MapY(b.y) - 10);
        }

        var playerMarkerGO = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        playerMarkerGO.transform.SetParent(mapAreaGO.transform, false);
        var playerMarkerRect = playerMarkerGO.GetComponent<RectTransform>();
        playerMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        playerMarkerRect.sizeDelta = new Vector2(10, 10);
        playerMarkerGO.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.2f, 1f);

        var closeButtonGO = CreateUIButton("CloseButton", panelGO.transform, "Fermer",
            new Vector2(0, -395), new Vector2(160, 30));
        closeButtonGO.AddComponent<CloseTownMapButton>();

        var mapUI = canvasTransform.gameObject.AddComponent<TownMapUI>();
        var mapSo = new SerializedObject(mapUI);
        var panelProp = mapSo.FindProperty("panel");
        var mapAreaProp = mapSo.FindProperty("mapAreaRect");
        var markerProp = mapSo.FindProperty("playerMarker");
        if (panelProp != null) panelProp.objectReferenceValue = panelGO;
        if (mapAreaProp != null) mapAreaProp.objectReferenceValue = mapAreaRect;
        if (markerProp != null) markerProp.objectReferenceValue = playerMarkerRect;
        mapSo.ApplyModifiedProperties();
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

        lastGemIconColor = default;
        lastGemIconFacetColor = default;
        configure(gem);

        // Remember the icon colours so custom-cut icons can be drawn at runtime (lapidary).
        if (lastGemIconColor.a > 0f)
        {
            gem.iconColor = lastGemIconColor;
            gem.iconFacetColor = lastGemIconFacetColor;
        }

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
    private const string PackCityTerrains = PackThemeRoot + "2_City_Terrains_Singles_32x32/ME_Singles_City_Terrains_32x32_";

    // ----- Per-room decoration -----

    private static void DecorateHome()
    {
        foreach (var x in new[] { 9, 14, 19, 24 }) PlaceWallDecor("window_white", x, RoomMaxY);

        PlaceRug("rug_red", new Vector3(-1.5f, -0.5f, 0f));
        PlaceFurniture("plant_small", new Vector3(7.6f, -3.0f, 0f));
    }

    private static void DecorateLaboratory()
    {
        foreach (var x in new[] { -7, -2, 3, 8 }) PlaceWallDecor("window_white", x, LabMaxY);

        PlaceFurniture("shelf_a", new Vector3(-7f, LabMaxY - 1f, 0f));
        PlaceFurniture("shelf_b", new Vector3(7f, LabMaxY - 1f, 0f));
        PlaceFurniture("palm", new Vector3(-8.8f, LabMaxY - 1.2f, 0f));
        PlaceFurniture("palm", new Vector3(9.0f, LabMaxY - 1.2f, 0f));
    }

    private static void DecorateMarket()
    {
        var shelves = new[] { "shop_a", "shop_b", "shop_c", "shop_d", "shop_e", "shop_f", "shop_a" };
        for (var i = 0; i < shelves.Length; i++)
            PlaceFurniture(shelves[i], new Vector3(-12f + i * 4f, MarketMaxY - 1f, 0f));

        PlaceFurniture("plant_small", new Vector3(-12.8f, MarketMinY + 1.2f, 0f));
        PlaceFurniture("plant_small", new Vector3(12.8f, MarketMinY + 1.2f, 0f));
    }

    private static void DecorateRoughMarket()
    {
        var shelves = new[] { "shelf_a", "shelf_b", "shelf_a", "shelf_b" };
        var xs = new[] { -12f, -4f, 4f, 12f };
        for (var i = 0; i < shelves.Length; i++)
            PlaceFurniture(shelves[i], new Vector3(xs[i], RoughMarketMaxY - 1f, 0f));

        foreach (var x in new[] { -8, 0, 8 }) PlaceWallDecor("window_red", x, RoughMarketMaxY);
    }

    private static void DecorateMuseum()
    {
        foreach (var x in new[] { -12, -8, -4, 0, 4, 8, 12 }) PlaceWallDecor("window_white", x, MuseumMaxY);

        PlaceRug("rug_gold", new Vector3(0f, 0.2f, 0f));
        PlaceFurniture("palm", new Vector3(-13.0f, MuseumMaxY - 1.2f, 0f));
        PlaceFurniture("palm", new Vector3(13.0f, MuseumMaxY - 1.2f, 0f));
        PlaceFurniture("globe", new Vector3(-6.5f, MuseumMaxY - 1.3f, 0f));
        PlaceFurniture("globe", new Vector3(6.5f, MuseumMaxY - 1.3f, 0f));
    }

    private static void DecorateManoir()
    {
        PlaceFurniture("fireplace", new Vector3(0f, ManoirMaxY - 1.5f, 0f));
        PlaceFurniture("wardrobe_a", new Vector3(-7f, ManoirMaxY - 1.5f, 0f));
        PlaceFurniture("mirror", new Vector3(7f, ManoirMaxY - 1.5f, 0f));
        PlaceWallDecor("window_red", -4, ManoirMaxY);
        PlaceWallDecor("window_red", 3, ManoirMaxY);

        PlaceRug("rug_red", new Vector3(0f, -0.5f, 0f));
        PlaceFurniture("palm", new Vector3(9.0f, ManoirMaxY - 1.5f, 0f));
        PlaceFurniture("plant_small", new Vector3(-9.0f, ManoirMaxY - 1.5f, 0f));
    }

    private static void DecorateAtelier()
    {
        PlaceWallDecor("window_white", -8, AtelierMaxY);
        PlaceWallDecor("window_white", 8, AtelierMaxY);

        PlaceFurniture("shelf_a", new Vector3(-3f, AtelierMaxY - 1f, 0f));
        PlaceFurniture("shelf_b", new Vector3(3f, AtelierMaxY - 1f, 0f));
        PlaceFurniture("palm", new Vector3(-8.8f, AtelierMaxY - 1.2f, 0f));
        PlaceFurniture("plant_small", new Vector3(9.0f, AtelierMinY + 1.2f, 0f));
        PlaceRug("rug_red", new Vector3(0f, -2.0f, 0f));
    }

    // ----- Interior dressing (LimeZu Modern Interiors / Office, pre-cut into Assets/Art/Sprites/interior_*.png) -----

    private const string InteriorSpriteRoot = "Assets/Art/Sprites/interior_";
    private static readonly Color InteriorSideWallTint = new Color(0.62f, 0.62f, 0.68f, 1f);

    private static Tile GetOrCreateTintedTile(string name, Sprite sprite, Color tint)
    {
        var tilePath = $"Assets/Art/Tilesets/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existing != null) return existing;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = tint;

        Directory.CreateDirectory("Assets/Art/Tilesets");
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    private static Tile GetInteriorFloorTile(string theme, Color tint, string fallbackName, Func<int, int, Color> fallback)
    {
        var sprite = LoadPackSprite($"{InteriorSpriteRoot}floor_{theme}.png");
        return sprite != null
            ? GetOrCreateTintedTile($"interior_floor_{theme}", sprite, tint)
            : GetOrCreateTile(fallbackName, fallback);
    }

    // Side and bottom walls are the wallpaper tile darkened, so the top wall reads as the lit wall face.
    private static Tile GetInteriorSideWallTile(string theme, Color tint, string fallbackName, Func<int, int, Color> fallback)
    {
        var sprite = LoadPackSprite($"{InteriorSpriteRoot}wall_{theme}.png");
        return sprite != null
            ? GetOrCreateTintedTile($"interior_sidewall_{theme}", sprite, tint)
            : GetOrCreateTile(fallbackName, fallback);
    }

    private static void PaintInteriorTopWall(Tilemap walls, int minX, int maxX, int maxY, string theme, Color tint)
    {
        var sprite = LoadPackSprite($"{InteriorSpriteRoot}wall_{theme}.png");
        if (sprite == null) return;

        var tile = GetOrCreateTintedTile($"interior_topwall_{theme}", sprite, tint);
        for (var x = minX; x <= maxX; x++)
            walls.SetTile(new Vector3Int(x, maxY, 0), tile);
    }

    // Decorative prop. `grounded` props stand with their feet at `position`; the others are centered on it.
    private static GameObject PlaceInteriorProp(string spriteName, Vector3 position, bool grounded, bool solid, int sortingOrder)
    {
        var path = $"{InteriorSpriteRoot}prop_{spriteName}.png";
        var sprite = grounded ? LoadPackSpriteGrounded(path) : LoadPackSprite(path);
        if (sprite == null) return null;

        var go = new GameObject($"Deco_{spriteName}");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        go.transform.position = position;

        if (solid)
        {
            var col = go.AddComponent<BoxCollider2D>();
            var width = sprite.bounds.size.x * 0.8f;
            col.size = new Vector2(width, 0.5f);
            col.offset = grounded ? new Vector2(0f, 0.25f) : Vector2.zero;
        }
        return go;
    }

    private static void PlaceWallDecor(string spriteName, int tileX, int wallY)
    {
        PlaceInteriorProp(spriteName, new Vector3(tileX + 0.5f, wallY + 0.5f, 0f), false, false, 1);
    }

    private static void PlaceRug(string spriteName, Vector3 center)
    {
        PlaceInteriorProp(spriteName, center, false, false, 1);
    }

    private static GameObject PlaceFurniture(string spriteName, Vector3 feet)
    {
        return PlaceInteriorProp(spriteName, feet, true, true, 4);
    }

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
            case "CabineUV": return GetOrCreatePixelSprite("uvlongues_pixel", LampeUVPattern, LampeUVPalette);
            case "FiltreChelsea": return GetOrCreatePixelSprite("chelsea_pixel", LoupePattern, ChelseaPalette);
            case "Micrometre": return GetOrCreatePixelSprite("micrometre_pixel", MicrometrePattern, MicrometrePalette);
            case "BalanceCarat": return GetOrCreatePixelSprite("balancecarat_pixel", BalanceCaratPattern, BalanceCaratPalette);
            case "Aimant": return GetOrCreatePixelSprite("aimant_pixel", MagnetPattern, MagnetPalette);
            case "FTIR": return GetOrCreatePixelSprite("ftir_pixel", RefractometrePattern, FTIRPalette);
            case "Raman": return GetOrCreatePixelSprite("raman_pixel", DichroscopePattern, RamanPalette);
            case "Spectrophotometre":
            case "LAICPMS": return GetOrCreatePixelSprite("laicpms_pixel", BalanceCaratPattern, LAICPMSPalette);
            case "MEB": return GetOrCreatePixelSprite("meb_pixel", MicroscopePattern, MEBPalette);
            case "DiamondView": return GetOrCreatePixelSprite("diamondview_pixel", MicroscopePattern, DiamondViewPalette);
            case "DiamondSure": return GetOrCreatePixelSprite("diamondsure_pixel", RefractometrePattern, DiamondSurePalette);
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

    // Ground-anchored scenery prop (sprite pivot at its feet) with an optional small trunk/base collider.
    private static GameObject PlaceTownProp(string spritePath, Vector3 feet, string name, float colliderWidth = 0f, float colliderHeight = 0f)
    {
        var sprite = LoadPackSpriteGrounded(spritePath);
        if (sprite == null) return null;

        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        go.transform.position = feet;

        if (colliderWidth > 0f)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(colliderWidth, colliderHeight);
            col.offset = new Vector2(0f, colliderHeight / 2f);
        }
        return go;
    }

    private static void PlaceRail(Sprite sprite, Vector3 position)
    {
        if (sprite == null) return;
        var go = new GameObject("Parapet");
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

    private static Color lastGemIconColor;
    private static Color lastGemIconFacetColor;

    private static Sprite GetGemIconSprite(string key, Color gemColor, Color facetColor, string shape = "", string cutStyle = "")
    {
        lastGemIconColor = gemColor;
        lastGemIconFacetColor = facetColor;
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

    private static readonly Dictionary<char, Color> LAICPMSPalette = new Dictionary<char, Color>
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
