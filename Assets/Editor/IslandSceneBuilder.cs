using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sadece Editor'de çalışır, oyunun build'ine girmez.
// Menü: Junyorjem > Build Islands And Menus
// Ana menü, Ada 1 ve bitiş sahnesini koddan kurar, Build Settings sırasını ayarlar.
// Oyuncu, kamera ve sistemler TestScene'den prefab olarak alınır (arkadaşının ayarları korunsun diye).
// TestScene sadece okunur, asla kaydedilmez.
public static class IslandSceneBuilder
{
    const string testScenePath = "Assets/TestScene/TestScene.unity";

    const string prefabFolder = "Assets/Prefabs";
    const string islandRigPrefabPath = "Assets/Prefabs/IslandRig.prefab";
    const string inputManagerPrefabPath = "Assets/Prefabs/InputManager.prefab";

    const string scenesFolder = "Assets/Scenes";
    const string islandFolder = "Assets/Scenes/Islands";
    const string materialFolder = "Assets/Scenes/Islands/Materials";
    const string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string island1ScenePath = "Assets/Scenes/Islands/Island1.unity";
    const string endScenePath = "Assets/Scenes/End.unity";

    // Platformların kalınlığı, üst yüzey yüksekliği ayrıca verilir
    const float groundThickness = 2f;

    [MenuItem("Junyorjem/Build Islands And Menus")]
    public static void BuildIslandsAndMenus()
    {
        // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor (batchmode'da soru sorulamaz)
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        CreateFolders();

        // Prefab'lar sadece yoksa oluşturulur, elle yapılan prefab düzenlemeleri silinmesin
        if (!CreateGameplayPrefabs(false))
        {
            return;
        }

        BuildIsland1();
        BuildMainMenu();
        BuildEndScene();
        SetBuildSettings();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(island1ScenePath, OpenSceneMode.Single);
        Debug.Log("IslandSceneBuilder: MainMenu, Island1 ve End kuruldu, Build Settings ayarlandı");
    }

    [MenuItem("Junyorjem/Refresh Gameplay Prefabs From TestScene")]
    public static void RefreshGameplayPrefabs()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        CreateFolders();

        if (CreateGameplayPrefabs(true))
        {
            Debug.Log("IslandSceneBuilder: prefab'lar TestScene'den yenilendi. Adaları güncellemek için Build Islands And Menus'ü tekrar çalıştır.");
        }
    }

    #region Gameplay Prefabs
    // TestScene'deki oyuncu, kamera ve sistemleri prefab olarak kaydeder.
    // Birbirine referans veren objeler TEK prefab'a (IslandRig) girer, böylece referanslar kopmaz.
    static bool CreateGameplayPrefabs(bool forceRefresh)
    {
        bool prefabsExist = AssetDatabase.LoadAssetAtPath<GameObject>(islandRigPrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(inputManagerPrefabPath) != null;

        if (prefabsExist && !forceRefresh)
        {
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(testScenePath) == null)
        {
            Debug.LogError("IslandSceneBuilder: TestScene bulunamadı -> " + testScenePath);
            return false;
        }

        // Önce boş bir sahne aç, sonra TestScene'i yanına ekle (diskten temiz yüklensin)
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene testScene = EditorSceneManager.OpenScene(testScenePath, OpenSceneMode.Additive);

        // İsimle değil component ile arıyoruz, obje adı değişse de bulunsun
        GameObject playerRoot = FindRootWith<PlayerMovement>(testScene);
        GameObject playerCameraRoot = FindRootWith<CinemachineCamera>(testScene);
        GameObject mainCameraRoot = FindRootWith<CinemachineBrain>(testScene);
        GameObject gameSystemsRoot = FindRootWith<RespawnManager>(testScene);
        GameObject pauseCanvasRoot = FindRootWith<PauseMenu>(testScene);
        GameObject eventSystemRoot = FindRootWith<EventSystem>(testScene);
        GameObject inputManagerRoot = FindRootWith<InputManager>(testScene);

        if (playerRoot == null || playerCameraRoot == null || mainCameraRoot == null || gameSystemsRoot == null
            || pauseCanvasRoot == null || eventSystemRoot == null || inputManagerRoot == null)
        {
            Debug.LogError("IslandSceneBuilder: TestScene'de Player / PlayerCamera / Main Camera / GameSystems / PauseCanvas / EventSystem / InputManager'dan biri eksik");
            EditorSceneManager.CloseScene(testScene, true);
            return false;
        }

        // IslandRig objesini TestScene'in içinde oluşturuyoruz, taşıma aynı sahnede olsun
        GameObject islandRig = new GameObject("IslandRig");
        SceneManager.MoveGameObjectToScene(islandRig, testScene);

        List<GameObject> rigRoots = new List<GameObject>();
        AddUniqueRoot(rigRoots, playerRoot);
        AddUniqueRoot(rigRoots, playerCameraRoot);
        AddUniqueRoot(rigRoots, mainCameraRoot);
        AddUniqueRoot(rigRoots, gameSystemsRoot);
        AddUniqueRoot(rigRoots, pauseCanvasRoot);
        AddUniqueRoot(rigRoots, eventSystemRoot);

        foreach (GameObject rigRoot in rigRoots)
        {
            rigRoot.transform.SetParent(islandRig.transform, true);
        }

        // Başlangıç noktası her adada farklı, prefab'a girmez. Her adada yeniden bağlanır.
        SetReference(gameSystemsRoot.GetComponentInChildren<RespawnManager>(true), "startPoint", null);

        AddSquashStretch(playerRoot.GetComponentInChildren<PlayerMovement>(true).gameObject);

        PrefabUtility.SaveAsPrefabAsset(islandRig, islandRigPrefabPath);
        // InputManager DontDestroyOnLoad kullanıyor, sahnede root obje olmalı. O yüzden ayrı prefab.
        PrefabUtility.SaveAsPrefabAsset(inputManagerRoot, inputManagerPrefabPath);

        // true = sahneyi kaldır. Kaydetmeden kapatır, TestScene diskte hiç değişmez.
        EditorSceneManager.CloseScene(testScene, true);
        AssetDatabase.SaveAssets();
        return true;
    }

    // Squash & stretch ayak hizasındaki wobble pivotunu ölçekler, yoksa ayaklar yerden kalkar
    static void AddSquashStretch(GameObject player)
    {
        if (player.GetComponent<PlayerSquashStretch>() != null)
        {
            return;
        }

        PlayerVisualWobble playerVisualWobble = player.GetComponent<PlayerVisualWobble>();
        Transform wobblePivot = playerVisualWobble != null ? GetReference(playerVisualWobble, "wobblePivotTransform") as Transform : null;

        if (wobblePivot == null)
        {
            Debug.LogWarning("IslandSceneBuilder: oyuncuda ayak hizasında pivot (PlayerVisualWobble) yok, PlayerSquashStretch eklenmedi");
            return;
        }

        PlayerSquashStretch playerSquashStretch = player.AddComponent<PlayerSquashStretch>();
        SetReference(playerSquashStretch, "squashPivotTransform", wobblePivot);
    }

    // T component'i (kendisinde ya da child'larında) olan ilk root objeyi döndürür
    static GameObject FindRootWith<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<T>(true) != null)
            {
                return root;
            }
        }

        return null;
    }

    // Aynı root iki kere eklenmesin (ör. EventSystem PauseCanvas'ın child'ıysa)
    static void AddUniqueRoot(List<GameObject> rigRoots, GameObject root)
    {
        if (!rigRoots.Contains(root))
        {
            rigRoots.Add(root);
        }
    }

    // Ada sahnesine InputManager + IslandRig koyar, oyuncuyu başlangıca taşır, sistemleri ayarlar
    static GameObject CreateIslandRig(Scene scene, Transform startPoint, float seasonValue, float recallDuration)
    {
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(inputManagerPrefabPath), scene);
        GameObject islandRig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(islandRigPrefabPath), scene);

        RespawnManager respawnManager = islandRig.GetComponentInChildren<RespawnManager>(true);
        SetReference(respawnManager, "startPoint", startPoint);

        SeasonManager seasonManager = islandRig.GetComponentInChildren<SeasonManager>(true);
        SetFloat(seasonManager, "seasonValue", seasonValue);
        SetFloat(seasonManager, "recallDuration", recallDuration);

        PlacePlayerAtStart(islandRig.GetComponentInChildren<PlayerMovement>(true).gameObject, startPoint);
        return islandRig;
    }

    static void PlacePlayerAtStart(GameObject player, Transform startPoint)
    {
        // RespawnManager gibi: ayaklar başlangıç noktasına gelsin, pivot nerede olursa olsun
        Physics.SyncTransforms();
        Collider playerCollider = player.GetComponent<Collider>();
        float feetHeight = 0f;

        if (playerCollider != null && playerCollider.bounds.size.y > 0f)
        {
            feetHeight = player.transform.position.y - playerCollider.bounds.min.y;
        }

        player.transform.position = startPoint.position + Vector3.up * (feetHeight + 0.05f);
        player.transform.rotation = startPoint.rotation;
    }
    #endregion

    #region Island 1
    // Ada 1 - Sonbahar başı: yürüme, zıplama, ıslak zemin. Yaz ruhu tehlikesi yok.
    // Parkur +Z yönünde ilerler, x = 0 yolun ortası.
    public static void BuildIsland1()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Varsayılan kamerayı sil, kamera IslandRig'den geliyor
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        Light sunLight = CreateSunLight(scene);
        Transform level = CreateIsland1Level();
        Transform startPoint = level.Find("StartPoint");

        CreateIslandRig(scene, startPoint, 0f, 10f);

        // PlayerSquashStretch prefab'da (AddSquashStretch), mevsim görselleri her adanın kendi sahnesinde
        AddSeasonVisuals(sunLight);

        EditorSceneManager.SaveScene(scene, island1ScenePath);
    }

    static void AddSeasonVisuals(Light sunLight)
    {
        GameObject seasonVisualsObject = new GameObject("SeasonVisuals");
        SeasonVisuals seasonVisuals = seasonVisualsObject.AddComponent<SeasonVisuals>();
        SetReference(seasonVisuals, "sunLight", sunLight);

        // Sis sahnede açık olmalı: build'e giren hiçbir sahnede sis yoksa URP sis shader'larını build'den çıkarır
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
    }

    static Light CreateSunLight(Scene scene)
    {
        Light sunLight = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Light rootLight = root.GetComponent<Light>();
            if (rootLight != null && rootLight.type == LightType.Directional)
            {
                sunLight = rootLight;
            }
        }

        if (sunLight == null)
        {
            sunLight = new GameObject("Sun").AddComponent<Light>();
            sunLight.type = LightType.Directional;
        }

        // Sıcak, alçak sonbahar güneşi
        sunLight.gameObject.name = "Sun";
        sunLight.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        sunLight.color = new Color(1f, 0.87f, 0.68f);
        sunLight.intensity = 1.1f;
        sunLight.shadows = LightShadows.Soft;
        RenderSettings.sun = sunLight;
        return sunLight;
    }

    // Koşu 6 m/sn, zıplama 2.2 m: düz zeminde ~4.1 m, 1 m yukarı ~3.6 m uzağa gidilir.
    // Bu yüzden boşluklar en fazla 3 m, yukarı çıkılan yerlerde 2 m.
    static Transform CreateIsland1Level()
    {
        Material groundMaterial = CreateMaterial("AutumnGround", new Color(0.55f, 0.4f, 0.25f), 0.1f);
        Material grassMaterial = CreateMaterial("AutumnGrass", new Color(0.7f, 0.55f, 0.25f), 0.1f);
        Material wetMaterial = CreateMaterial("WetStone", new Color(0.25f, 0.32f, 0.4f), 0.85f);
        Material checkpointMaterial = CreateMaterial("Checkpoint", new Color(0.3f, 0.9f, 1f), 0.3f);
        Material trunkMaterial = CreateMaterial("TreeTrunk", new Color(0.35f, 0.22f, 0.12f), 0.1f);
        Material orangeLeafMaterial = CreateMaterial("LeavesOrange", new Color(0.9f, 0.45f, 0.1f), 0.1f);
        Material redLeafMaterial = CreateMaterial("LeavesRed", new Color(0.75f, 0.2f, 0.08f), 0.1f);
        Material dashHintMaterial = CreateMaterial("DashHint", new Color(1f, 0.85f, 0.1f), 0.3f);
        Material exitMaterial = CreateMaterial("ExitArch", new Color(0.95f, 0.75f, 0.35f), 0.3f);

        Transform level = new GameObject("Level").transform;

        // Adanın altına düşen oyuncu son checkpoint'e döner
        GameObject fallZone = new GameObject("FallZone");
        fallZone.transform.SetParent(level);
        fallZone.transform.position = new Vector3(0f, -12f, 70f);
        AddTriggerBox(fallZone, Vector3.zero, new Vector3(400f, 4f, 400f));
        fallZone.AddComponent<HazardZone>();

        // 1) Başlangıç: yürümeyi öğren. z -6 .. 12
        CreateGround("StartGround", level, 0f, 0f, -6f, 12f, 12f, grassMaterial);
        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.SetParent(level);
        startPoint.transform.position = new Vector3(0f, 0f, -2f);
        CreateTree(level, new Vector3(-4.5f, 0f, -3f), orangeLeafMaterial, trunkMaterial);
        CreateTree(level, new Vector3(4.5f, 0f, 2f), redLeafMaterial, trunkMaterial);
        CreateTree(level, new Vector3(-4.5f, 0f, 8f), orangeLeafMaterial, trunkMaterial);

        // 2) İlk zıplamalar: boşluklar 1.5, 2, 3 m (düz)
        CreateGround("Jump1", level, 0f, 0f, 13.5f, 18.5f, 6f, groundMaterial);
        CreateGround("Jump2", level, 0f, 0f, 20.5f, 25.5f, 6f, groundMaterial);
        CreateGround("Jump3", level, 0f, 0f, 28.5f, 33.5f, 6f, groundMaterial);

        // 3) Checkpoint 1: 2 m boşluk, 1 m yukarı. z 35.5 .. 45.5
        CreateGround("Checkpoint1Ground", level, 0f, 1f, 35.5f, 45.5f, 10f, grassMaterial);
        CreateCheckpoint(level, new Vector3(0f, 1f, 37f), 10f, checkpointMaterial);
        CreateTree(level, new Vector3(4f, 1f, 40f), redLeafMaterial, trunkMaterial);

        // 4) Islak zemin, geniş ve güvenli: kaymayı dene. z 45.5 .. 57.5
        CreateWetGround("WetPatch", level, 0f, 1f, 45.5f, 57.5f, 10f, wetMaterial);
        // 5) Islak dar köprü (4 m): kayarak yana düşebilirsin. z 57.5 .. 69.5
        CreateWetGround("WetBridge", level, 0f, 1f, 57.5f, 69.5f, 4f, wetMaterial);

        // 6) Checkpoint 2: ıslak zeminden 2 m zıplama. z 71.5 .. 81.5
        CreateGround("Checkpoint2Ground", level, 0f, 1f, 71.5f, 81.5f, 10f, grassMaterial);
        CreateCheckpoint(level, new Vector3(0f, 1f, 73f), 10f, checkpointMaterial);
        CreateTree(level, new Vector3(-4f, 1f, 79f), orangeLeafMaterial, trunkMaterial);

        // 7) Basamaklar: her biri 2 m boşluk ve 1 m yukarı, hafif sağa sola
        CreateGround("Step1", level, 0f, 2f, 83.5f, 86.5f, 4f, groundMaterial);
        CreateGround("Step2", level, 1.5f, 3f, 88.5f, 91.5f, 4f, groundMaterial);
        CreateGround("Step3", level, 0f, 4f, 93.5f, 96.5f, 4f, groundMaterial);

        // 8) Tepe + checkpoint 3. z 98.5 .. 108.5, x -5 .. 5
        CreateGround("HighGround", level, 0f, 4f, 98.5f, 108.5f, 10f, grassMaterial);
        CreateCheckpoint(level, new Vector3(0f, 4f, 100f), 10f, checkpointMaterial);
        CreateTree(level, new Vector3(-4f, 4f, 106f), redLeafMaterial, trunkMaterial);

        // Opsiyonel dash boşluğu: sağa 8 m (zıplama ~4 m + dash 6 m ≈ 10 m). Dash Ada 2'de öğretiliyor.
        CreatePrimitive(PrimitiveType.Cube, "DashHintStrip", level, new Vector3(4f, 4.01f, 103.5f), new Vector3(2f, 0.02f, 0.5f), dashHintMaterial, false);
        CreateGround("DashBonusPlatform", level, 15f, 4f, 101.5f, 105.5f, 4f, grassMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "GoldenLeafPile", level, new Vector3(15f, 4.3f, 103.5f), new Vector3(2f, 0.6f, 2f), dashHintMaterial, false);

        // 9) İniş: ıslak basamak (2 m boşluk, 1 m aşağı), sonra normal basamak (2.5 m, 1 m aşağı)
        CreateWetGround("WetStepDown", level, 0f, 3f, 110.5f, 116.5f, 5f, wetMaterial);
        CreateGround("StepDown", level, 0f, 2f, 119f, 124f, 5f, groundMaterial);

        // 10) Bitiş: 2.5 m boşluk. Kemerin altından geçince sonraki sahne yüklenir. z 126.5 .. 142.5
        CreateGround("EndGround", level, 0f, 2f, 126.5f, 142.5f, 14f, grassMaterial);
        CreateTree(level, new Vector3(-5.5f, 2f, 130f), orangeLeafMaterial, trunkMaterial);
        CreateTree(level, new Vector3(5.5f, 2f, 131f), redLeafMaterial, trunkMaterial);
        CreateTree(level, new Vector3(-5.5f, 2f, 141f), redLeafMaterial, trunkMaterial);
        CreateTree(level, new Vector3(5.5f, 2f, 140f), orangeLeafMaterial, trunkMaterial);
        CreateIslandExit(level, new Vector3(0f, 2f, 136f), 14f, exitMaterial);

        return level;
    }

    // Üst yüzeyi topHeight'ta, z startZ'den endZ'ye uzanan zemin
    static GameObject CreateGround(string name, Transform parent, float centerX, float topHeight, float startZ, float endZ, float width, Material material)
    {
        float length = endZ - startZ;
        Vector3 position = new Vector3(centerX, topHeight - groundThickness / 2f, startZ + length / 2f);
        return CreatePrimitive(PrimitiveType.Cube, name, parent, position, new Vector3(width, groundThickness, length), material, true);
    }

    static GameObject CreateWetGround(string name, Transform parent, float centerX, float topHeight, float startZ, float endZ, float width, Material material)
    {
        GameObject wetGround = CreateGround(name, parent, centerX, topHeight, startZ, endZ, width, material);
        wetGround.AddComponent<GroundSurface>();
        return wetGround;
    }

    static void CreateCheckpoint(Transform parent, Vector3 position, float width, Material material)
    {
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.SetParent(parent);
        checkpoint.transform.position = position;
        AddTriggerBox(checkpoint, new Vector3(0f, 1f, 0f), new Vector3(width, 2f, 1f));
        checkpoint.AddComponent<Checkpoint>();

        CreatePrimitive(PrimitiveType.Cube, "Visual", checkpoint.transform, new Vector3(0f, 0.01f, 0f), new Vector3(width, 0.02f, 1f), material, false);
    }

    // Gövde (collider'lı) + yaprak küresi (collider'sız)
    static void CreateTree(Transform parent, Vector3 groundPosition, Material leafMaterial, Material trunkMaterial)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.position = groundPosition;

        CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 1.2f, 0.4f), trunkMaterial, true);
        CreatePrimitive(PrimitiveType.Sphere, "Leaves", tree.transform, new Vector3(0f, 3f, 0f), new Vector3(2.4f, 2.2f, 2.4f), leafMaterial, false);
    }

    static void CreateIslandExit(Transform parent, Vector3 groundPosition, float width, Material material)
    {
        GameObject islandExit = new GameObject("IslandExit");
        islandExit.transform.SetParent(parent);
        islandExit.transform.position = groundPosition;
        AddTriggerBox(islandExit, new Vector3(0f, 2f, 0f), new Vector3(width, 4f, 2f));
        islandExit.AddComponent<IslandExit>();

        // Kemer: iki direk + üst kiriş
        CreatePrimitive(PrimitiveType.Cube, "PillarLeft", islandExit.transform, new Vector3(-4f, 2f, 0f), new Vector3(0.8f, 4f, 0.8f), material, true);
        CreatePrimitive(PrimitiveType.Cube, "PillarRight", islandExit.transform, new Vector3(4f, 2f, 0f), new Vector3(0.8f, 4f, 0.8f), material, true);
        CreatePrimitive(PrimitiveType.Cube, "Beam", islandExit.transform, new Vector3(0f, 4.4f, 0f), new Vector3(9f, 0.8f, 0.8f), material, false);
    }
    #endregion

    #region Main Menu
    public static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMenuCamera(new Color(0.45f, 0.25f, 0.12f));
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(inputManagerPrefabPath), scene);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject canvasObject = CreateCanvas("MainMenuCanvas");
        DefaultControls.Resources uiResources = CreateUIResources();
        Color textColor = new Color(1f, 0.93f, 0.8f);

        // Ana panel: başlık + Başla + Çık
        GameObject mainPanel = CreatePanel(uiResources, canvasObject.transform, "MainPanel");
        CreateText(uiResources, mainPanel.transform, "TitleText", "Sonbahardan Kışa", 110, new Vector2(1400f, 200f), 250f, textColor);
        Button startButton = CreateButton(uiResources, mainPanel.transform, "Başla", -40f);
        Button quitButton = CreateButton(uiResources, mainPanel.transform, "Çık", -160f);

        // Giriş paneli: kısa hikâye + Devam
        GameObject introPanel = CreatePanel(uiResources, canvasObject.transform, "IntroPanel");
        string introText = "Yağmurluğunu giymiş küçük bir kız, sonbaharın son günlerinde yola çıkıyor.\n\n"
            + "Kışa ulaşmak için adadan adaya geçmesi gerek. Islak taşlarda kayabilir, solan yaprakların üstünde fazla oyalanmamalı.\n\n"
            + "Haydi, kış onu bekliyor!";
        CreateText(uiResources, introPanel.transform, "IntroText", introText, 44, new Vector2(1300f, 500f), 120f, textColor);
        Button continueButton = CreateButton(uiResources, introPanel.transform, "Devam", -300f);

        MainMenu mainMenu = canvasObject.AddComponent<MainMenu>();
        UnityEventTools.AddPersistentListener(startButton.onClick, mainMenu.StartGame);
        UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenu.QuitGame);
        UnityEventTools.AddPersistentListener(continueButton.onClick, mainMenu.ContinueFromIntro);

        SetReference(mainMenu, "mainPanel", mainPanel);
        SetReference(mainMenu, "introPanel", introPanel);
        SetReference(mainMenu, "mainFirstButton", startButton.gameObject);
        SetReference(mainMenu, "introFirstButton", continueButton.gameObject);
        SetInt(mainMenu, "firstIslandSceneIndex", 1);

        // MainMenu.Start panelleri zaten ayarlıyor, editörde de doğru görünsün
        introPanel.SetActive(false);

        EditorSceneManager.SaveScene(scene, mainMenuScenePath);
    }
    #endregion

    #region End
    public static void BuildEndScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMenuCamera(new Color(0.78f, 0.85f, 0.93f));
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject canvasObject = CreateCanvas("EndCanvas");
        DefaultControls.Resources uiResources = CreateUIResources();
        Color textColor = new Color(0.15f, 0.22f, 0.35f);

        CreateText(uiResources, canvasObject.transform, "TitleText", "Kış geldi.", 110, new Vector2(1400f, 160f), 330f, textColor);
        string finalText = "Küçük kız sonunda karların arasına ulaştı.\n"
            + "Ama mevsimler hiç durmaz: kış biter, bahar gelir, yaz geçer ve yapraklar yeniden dökülür.\n"
            + "Döngü devam ediyor.";
        CreateText(uiResources, canvasObject.transform, "FinalText", finalText, 40, new Vector2(1400f, 260f), 120f, textColor);
        string creditsText = "Yapımcılar\n[İsimler buraya]\n\nJunior Jam - 48 saat";
        CreateText(uiResources, canvasObject.transform, "CreditsText", creditsText, 30, new Vector2(1000f, 220f), -140f, textColor);
        Button mainMenuButton = CreateButton(uiResources, canvasObject.transform, "Ana menü", -370f);

        EndScreen endScreen = canvasObject.AddComponent<EndScreen>();
        UnityEventTools.AddPersistentListener(mainMenuButton.onClick, endScreen.ReturnToMainMenu);
        SetReference(endScreen, "firstSelectedButton", mainMenuButton.gameObject);
        SetInt(endScreen, "mainMenuSceneIndex", 0);

        EditorSceneManager.SaveScene(scene, endScenePath);
    }
    #endregion

    #region Build Settings
    // Sıra: 0 MainMenu, 1..N Island1..IslandN (diskte olanlar), son End. Diğer sahneler sonda, kapalı.
    static void SetBuildSettings()
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        buildScenes.Add(new EditorBuildSettingsScene(mainMenuScenePath, true));

        for (int islandNumber = 1; islandNumber <= 9; islandNumber++)
        {
            string islandScenePath = islandFolder + "/Island" + islandNumber + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(islandScenePath) != null)
            {
                buildScenes.Add(new EditorBuildSettingsScene(islandScenePath, true));
            }
        }

        buildScenes.Add(new EditorBuildSettingsScene(endScenePath, true));

        foreach (EditorBuildSettingsScene oldScene in EditorBuildSettings.scenes)
        {
            if (!ContainsScenePath(buildScenes, oldScene.path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(oldScene.path, false));
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    static bool ContainsScenePath(List<EditorBuildSettingsScene> buildScenes, string scenePath)
    {
        foreach (EditorBuildSettingsScene buildScene in buildScenes)
        {
            if (buildScene.path == scenePath)
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region UI Helpers
    static void CreateMenuCamera(Color backgroundColor)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera menuCamera = cameraObject.GetComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = backgroundColor;
    }

    static GameObject CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvasObject;
    }

    // Unity'nin kendi UI sprite'ları: köşeleri yuvarlak buton
    static DefaultControls.Resources CreateUIResources()
    {
        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        return uiResources;
    }

    // Ekranı kaplayan şeffaf panel, sadece butonları gruplamak için
    static GameObject CreatePanel(DefaultControls.Resources uiResources, Transform parent, string name)
    {
        GameObject panel = DefaultControls.CreatePanel(uiResources);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        return panel;
    }

    static Text CreateText(DefaultControls.Resources uiResources, Transform parent, string name, string content, int fontSize, Vector2 size, float positionY, Color color)
    {
        GameObject textObject = DefaultControls.CreateText(uiResources);
        textObject.name = name;
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = size;
        textRect.anchoredPosition = new Vector2(0f, positionY);

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(DefaultControls.Resources uiResources, Transform parent, string label, float positionY)
    {
        GameObject buttonObject = DefaultControls.CreateButton(uiResources);
        buttonObject.name = label + "Button";
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(400f, 90f);
        buttonRect.anchoredPosition = new Vector2(0f, positionY);

        Text buttonText = buttonObject.GetComponentInChildren<Text>();
        buttonText.text = label;
        buttonText.fontSize = 36;

        return buttonObject.GetComponent<Button>();
    }
    #endregion

    #region Helpers
    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;

        if (parent != null)
        {
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
        }
        else
        {
            primitive.transform.position = position;
        }

        primitive.transform.localScale = scale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;

        if (!keepCollider)
        {
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
        }

        return primitive;
    }

    static void AddTriggerBox(GameObject target, Vector3 center, Vector3 size)
    {
        BoxCollider triggerBox = target.AddComponent<BoxCollider>();
        triggerBox.isTrigger = true;
        triggerBox.center = center;
        triggerBox.size = size;
    }

    // smoothness: 0 = mat, 1 = parlak (ıslak taş parlak görünsün)
    static Material CreateMaterial(string name, Color color, float smoothness)
    {
        string path = materialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void CreateFolders()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets", "Scenes");
        CreateFolder(scenesFolder, "Islands");
        CreateFolder(islandFolder, "Materials");
    }

    static void CreateFolder(string parentFolder, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + folderName))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    // [SerializeField] private alanlara Inspector'daki gibi değer yazar
    static void SetReference(Object target, string fieldName, Object value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).objectReferenceValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    static Object GetReference(Object target, string fieldName)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        return serializedTarget.FindProperty(fieldName).objectReferenceValue;
    }

    static void SetFloat(Object target, string fieldName, float value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).floatValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetInt(Object target, string fieldName, int value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).intValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion
}
