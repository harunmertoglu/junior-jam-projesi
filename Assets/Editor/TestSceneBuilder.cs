using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sadece Editor'de çalışır, oyunun build'ine girmez.
// Menü: Junyorjem > Build Test Scene
// Tüm mekanikleri sırayla deneyebileceğin bir test sahnesi kurar. Tekrar çalıştırınca sahneyi baştan kurar.
public static class TestSceneBuilder
{
    const string rootFolder = "Assets/TestScene";
    const string materialFolder = "Assets/TestScene/Materials";
    const string scenePath = "Assets/TestScene/TestScene.unity";

    [MenuItem("Junyorjem/Build Test Scene")]
    public static void BuildTestScene()
    {
        CreateFolders();
        int playerLayer = CreatePlayerLayer();
        // Player layer'ı hariç her şey zemin sayılır
        int groundLayers = ~(1 << playerLayer);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject inputManagerObject = new GameObject("InputManager");
        inputManagerObject.AddComponent<InputManager>();

        GameObject player = CreatePlayer(playerLayer, groundLayers);
        Transform playerVisual = player.transform.Find("Visual");

        CreateCamera(playerVisual);
        CreateGameSystems(player);
        CreatePauseMenu();
        CreateLevel();

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("TestSceneBuilder: sahne kuruldu -> " + scenePath);
    }

    #region Player
    static GameObject CreatePlayer(int playerLayer, int groundLayers)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";

        Rigidbody playerRigidbody = player.AddComponent<Rigidbody>();
        playerRigidbody.mass = 1f;

        CapsuleCollider playerCollider = player.AddComponent<CapsuleCollider>();
        playerCollider.height = 1.1f;
        playerCollider.radius = 0.3f;
        playerCollider.center = new Vector3(0f, 0.55f, 0f);
        playerCollider.sharedMaterial = CreateNoFrictionMaterial();

        // Placeholder karakter: kırmızı kapsül + nereye baktığını gösteren sarı burun
        GameObject visual = CreatePrimitive(PrimitiveType.Capsule, "Visual", player.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 0.55f, 0.6f), CreateMaterial("Player", new Color(0.85f, 0.1f, 0.1f)), false);
        CreatePrimitive(PrimitiveType.Cube, "Nose", visual.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(0.4f, 0.3f, 0.4f), CreateMaterial("PlayerNose", new Color(1f, 0.85f, 0.1f)), false);

        GameObject shadow = CreatePrimitive(PrimitiveType.Cylinder, "GroundShadow", player.transform, Vector3.zero, new Vector3(0.5f, 0.01f, 0.5f), CreateMaterial("Shadow", new Color(0.05f, 0.05f, 0.05f)), false);

        SetLayerRecursively(player, playerLayer);

        PlayerMovement playerMovement = player.AddComponent<PlayerMovement>();
        SetReference(playerMovement, "playerVisualTransform", visual.transform);
        SetInt(playerMovement, "groundLayers", groundLayers);

        PlayerDash playerDash = player.AddComponent<PlayerDash>();
        SetReference(playerDash, "recallMarkerPrefab", CreateRecallMarkerPrefab());

        PlayerGroundShadow playerGroundShadow = player.AddComponent<PlayerGroundShadow>();
        SetReference(playerGroundShadow, "shadowTransform", shadow.transform);
        SetInt(playerGroundShadow, "groundLayers", groundLayers);

        return player;
    }

    static PhysicsMaterial CreateNoFrictionMaterial()
    {
        string path = rootFolder + "/PlayerNoFriction.physicMaterial";
        PhysicsMaterial noFrictionMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);

        if (noFrictionMaterial == null)
        {
            noFrictionMaterial = new PhysicsMaterial("PlayerNoFriction");
            AssetDatabase.CreateAsset(noFrictionMaterial, path);
        }

        // Sürtünme 0: karakter duvarlara yapışmasın
        noFrictionMaterial.dynamicFriction = 0f;
        noFrictionMaterial.staticFriction = 0f;
        noFrictionMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(noFrictionMaterial);
        return noFrictionMaterial;
    }

    static GameObject CreateRecallMarkerPrefab()
    {
        GameObject marker = CreatePrimitive(PrimitiveType.Cylinder, "RecallMarker", null, Vector3.zero, new Vector3(0.9f, 0.02f, 0.9f), CreateMaterial("RecallMarker", new Color(0.55f, 0.3f, 0.1f)), false);
        GameObject markerPrefab = PrefabUtility.SaveAsPrefabAsset(marker, rootFolder + "/RecallMarker.prefab");
        Object.DestroyImmediate(marker);
        return markerPrefab;
    }
    #endregion

    #region Camera
    static void CreateCamera(Transform playerVisual)
    {
        Camera mainCamera = Object.FindAnyObjectByType<Camera>();
        mainCamera.gameObject.AddComponent<CinemachineBrain>();

        GameObject cameraObject = new GameObject("PlayerCamera");
        CinemachineCamera cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
        cinemachineCamera.Target.TrackingTarget = playerVisual;

        CinemachineOrbitalFollow orbitalFollow = cameraObject.AddComponent<CinemachineOrbitalFollow>();
        orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbitalFollow.Radius = 6f;
        orbitalFollow.TargetOffset = new Vector3(0f, 0.8f, 0f);
        orbitalFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        // Otomatik arkaya dönme kapalı, kamerayı sadece oyuncu çevirir
        orbitalFollow.HorizontalAxis.Recentering = new InputAxis.RecenteringSettings { Enabled = false, Wait = 2f, Time = 1f };

        cameraObject.AddComponent<CinemachineRotationComposer>();

        // Kamera duvarın içine girmesin
        CinemachineDeoccluder deoccluder = cameraObject.AddComponent<CinemachineDeoccluder>();
        deoccluder.IgnoreTag = "Player";
        deoccluder.AvoidObstacles.Enabled = true;

        CameraLook cameraLook = cameraObject.AddComponent<CameraLook>();
        SetReference(cameraLook, "orbitalFollow", orbitalFollow);
    }
    #endregion

    #region Systems
    static void CreateGameSystems(GameObject player)
    {
        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.position = Vector3.zero;

        GameObject gameSystems = new GameObject("GameSystems");

        SeasonManager seasonManager = gameSystems.AddComponent<SeasonManager>();
        SetFloat(seasonManager, "seasonValue", 0f);
        SetFloat(seasonManager, "recallDuration", 10f);

        RespawnManager respawnManager = gameSystems.AddComponent<RespawnManager>();
        SetReference(respawnManager, "playerMovement", player.GetComponent<PlayerMovement>());
        SetReference(respawnManager, "startPoint", startPoint.transform);
    }

    static void CreatePauseMenu()
    {
        GameObject canvasObject = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        GameObject pausePanel = DefaultControls.CreatePanel(uiResources);
        pausePanel.name = "PausePanel";
        pausePanel.transform.SetParent(canvasObject.transform, false);
        pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        PauseMenu pauseMenu = canvasObject.AddComponent<PauseMenu>();

        Button resumeButton = CreateButton(uiResources, pausePanel.transform, "Devam", 120f);
        Button restartButton = CreateButton(uiResources, pausePanel.transform, "Yeniden başla", 0f);
        Button mainMenuButton = CreateButton(uiResources, pausePanel.transform, "Ana menü", -120f);

        UnityEventTools.AddPersistentListener(resumeButton.onClick, pauseMenu.ResumeGame);
        UnityEventTools.AddPersistentListener(restartButton.onClick, pauseMenu.RestartIsland);
        UnityEventTools.AddPersistentListener(mainMenuButton.onClick, pauseMenu.ReturnToMainMenu);

        SetReference(pauseMenu, "pausePanel", pausePanel);
        SetReference(pauseMenu, "firstSelectedButton", resumeButton.gameObject);
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

    #region Level
    static void CreateLevel()
    {
        Material groundMaterial = CreateMaterial("Ground", new Color(0.45f, 0.4f, 0.3f));
        Material testMaterial = CreateMaterial("TestBlock", new Color(0.6f, 0.6f, 0.65f));
        Material dashStripMaterial = CreateMaterial("DashStrip", new Color(1f, 0.85f, 0.1f));
        Material wetMaterial = CreateMaterial("WetGround", new Color(0.2f, 0.35f, 0.55f));
        Material leafMaterial = CreateMaterial("Leaf", new Color(0.9f, 0.45f, 0.1f));
        Material iceMaterial = CreateMaterial("Ice", new Color(0.7f, 0.9f, 1f));
        Material flameMaterial = CreateMaterial("Flame", new Color(1f, 0.25f, 0f));
        Material doorMaterial = CreateMaterial("Door", new Color(0.35f, 0.2f, 0.1f));
        Material switchMaterial = CreateMaterial("Switch", new Color(0.2f, 0.8f, 0.3f));
        Material seedMaterial = CreateMaterial("Seed", new Color(0.6f, 1f, 0.4f));
        Material checkpointMaterial = CreateMaterial("Checkpoint", new Color(0.3f, 0.9f, 1f));

        Transform level = new GameObject("Level").transform;

        // Adanın altına düşen oyuncu son checkpoint'e döner
        GameObject fallZone = new GameObject("FallZone");
        fallZone.transform.SetParent(level);
        fallZone.transform.position = new Vector3(0f, -15f, 30f);
        AddTriggerBox(fallZone, Vector3.zero, new Vector3(300f, 2f, 300f));
        fallZone.AddComponent<HazardZone>();

        // 1) Başlangıç alanı: z -15 .. 15
        CreatePrimitive(PrimitiveType.Cube, "GroundA", level, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), groundMaterial, true);
        CreatePrimitive(PrimitiveType.Cube, "Cube_2m", level, new Vector3(-6f, 1f, 8f), new Vector3(2f, 2f, 2f), testMaterial, true);
        CreatePrimitive(PrimitiveType.Cube, "Cube_2.5m", level, new Vector3(-10f, 1.25f, 8f), new Vector3(2f, 2.5f, 2f), testMaterial, true);
        // Dash 6 m gitmeli: şerit z 0 .. 6
        CreatePrimitive(PrimitiveType.Cube, "DashStrip_6m", level, new Vector3(6f, 0.01f, 3f), new Vector3(0.5f, 0.02f, 6f), dashStripMaterial, false);

        GameObject wetGround = CreatePrimitive(PrimitiveType.Cube, "WetGround", level, new Vector3(8f, 0.01f, -7f), new Vector3(8f, 0.02f, 8f), wetMaterial, true);
        wetGround.AddComponent<GroundSurface>();

        // 2) Solan yapraklar: aralar 1-1.5 m
        CreateFadingLeaf(level, new Vector3(0f, -0.15f, 17.5f), leafMaterial);
        CreateFadingLeaf(level, new Vector3(0f, -0.15f, 20.5f), leafMaterial);
        CreateFadingLeaf(level, new Vector3(0f, -0.15f, 23.5f), leafMaterial);

        // 3) Checkpoint + yükselip alçalan alev: z 25.5 .. 35.5
        CreatePrimitive(PrimitiveType.Cube, "GroundB", level, new Vector3(0f, -0.5f, 30.5f), new Vector3(10f, 1f, 10f), groundMaterial, true);
        CreateCheckpoint(level, new Vector3(0f, 0f, 27f), 10f, checkpointMaterial);
        CreateRisingFlame(level, new Vector3(0f, 0f, 31.5f), flameMaterial);

        // 4) Eriyen platformlar, yandakinin üstünde tohum (al ve geri dön)
        CreateMeltingPlatform(level, new Vector3(0f, -0.25f, 38f), iceMaterial);
        CreateMeltingPlatform(level, new Vector3(0f, -0.25f, 42f), iceMaterial);
        CreateMeltingPlatform(level, new Vector3(5f, -0.25f, 40f), iceMaterial);
        SeasonSeed seed = CreateSeed(level, new Vector3(5f, 0.8f, 40f), seedMaterial);

        // 5) Şalter ve sunak, her biri bir kapı açar: z 45 .. 59
        CreatePrimitive(PrimitiveType.Cube, "GroundC", level, new Vector3(0f, -0.5f, 52f), new Vector3(14f, 1f, 14f), groundMaterial, true);
        CreateCheckpoint(level, new Vector3(0f, 0f, 46.5f), 14f, checkpointMaterial);

        ProgressionDoor switchDoor = CreateDoor(level, "SwitchDoor", new Vector3(0f, 2f, 56f), doorMaterial);
        ProgressionDoor altarDoor = CreateDoor(level, "AltarDoor", new Vector3(0f, 2f, 57.5f), doorMaterial);
        CreateSwitch(level, new Vector3(-4f, 0f, 50f), switchDoor, switchMaterial);
        CreateAltar(level, new Vector3(4f, 0f, 50f), seed, altarDoor, testMaterial);

        // 6) Alev duvarı: girince ortadaki yol kapanır, kenarlardan geçilir. z 60 .. 70
        CreatePrimitive(PrimitiveType.Cube, "GroundD", level, new Vector3(0f, -0.5f, 65f), new Vector3(10f, 1f, 10f), groundMaterial, true);
        CreateFlameWall(level, new Vector3(0f, 0f, 61f), new Vector3(0f, 0f, 65f), flameMaterial);
    }

    static void CreateFadingLeaf(Transform parent, Vector3 position, Material material)
    {
        GameObject leaf = CreatePlatformWithVisual("FadingLeaf", parent, position, new Vector3(2f, 0.3f, 2f), material);
        FadingLeaf fadingLeaf = leaf.AddComponent<FadingLeaf>();
        SetReference(fadingLeaf, "leafCollider", leaf.GetComponent<Collider>());
        SetReference(fadingLeaf, "leafVisual", leaf.transform.Find("Visual"));
    }

    static void CreateMeltingPlatform(Transform parent, Vector3 position, Material material)
    {
        GameObject platform = CreatePlatformWithVisual("MeltingPlatform", parent, position, new Vector3(3f, 0.5f, 3f), material);
        MeltingPlatform meltingPlatform = platform.AddComponent<MeltingPlatform>();
        SetReference(meltingPlatform, "platformCollider", platform.GetComponent<Collider>());
        SetReference(meltingPlatform, "platformVisual", platform.transform.Find("Visual"));
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

    static void CreateRisingFlame(Transform parent, Vector3 position, Material material)
    {
        GameObject flameRoot = new GameObject("RisingFlame");
        flameRoot.transform.SetParent(parent);
        flameRoot.transform.position = position;

        // Alçalınca zeminin altına girer (üst yüzeyi -0.5'te kalır)
        GameObject flame = CreatePrimitive(PrimitiveType.Cube, "Flame", flameRoot.transform, new Vector3(0f, -1.5f, 0f), new Vector3(10f, 2f, 0.6f), material, true);
        flame.GetComponent<Collider>().isTrigger = true;
        flame.AddComponent<HazardZone>();

        RisingFlame risingFlame = flameRoot.AddComponent<RisingFlame>();
        SetReference(risingFlame, "flameTransform", flame.transform);
        SetFloat(risingFlame, "loweredHeight", -1.5f);
        SetFloat(risingFlame, "raisedHeight", 1f);
    }

    static SeasonSeed CreateSeed(Transform parent, Vector3 position, Material material)
    {
        GameObject seed = CreatePrimitive(PrimitiveType.Sphere, "SeasonSeed", parent, position, new Vector3(0.5f, 0.5f, 0.5f), material, true);
        seed.GetComponent<Collider>().isTrigger = true;
        return seed.AddComponent<SeasonSeed>();
    }

    static ProgressionDoor CreateDoor(Transform parent, string name, Vector3 position, Material material)
    {
        GameObject door = CreatePrimitive(PrimitiveType.Cube, name, parent, position, new Vector3(14f, 4f, 0.5f), material, true);
        ProgressionDoor progressionDoor = door.AddComponent<ProgressionDoor>();
        SetVector3(progressionDoor, "openOffset", new Vector3(0f, -4.5f, 0f));
        return progressionDoor;
    }

    static void CreateSwitch(Transform parent, Vector3 position, ProgressionDoor targetDoor, Material material)
    {
        GameObject switchRoot = new GameObject("ProgressionSwitch");
        switchRoot.transform.SetParent(parent);
        switchRoot.transform.position = position;
        AddTriggerBox(switchRoot, new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 1f, 1.5f));

        GameObject switchVisual = CreatePrimitive(PrimitiveType.Cube, "Visual", switchRoot.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.3f, 1.2f), material, false);

        ProgressionSwitch progressionSwitch = switchRoot.AddComponent<ProgressionSwitch>();
        SetReference(progressionSwitch, "targetDoor", targetDoor);
        SetReference(progressionSwitch, "switchVisual", switchVisual.transform);
    }

    static void CreateAltar(Transform parent, Vector3 position, SeasonSeed requiredSeed, ProgressionDoor targetDoor, Material material)
    {
        GameObject altar = new GameObject("SeedAltar");
        altar.transform.SetParent(parent);
        altar.transform.position = position;
        AddTriggerBox(altar, new Vector3(0f, 1f, 0f), new Vector3(2.5f, 2f, 2.5f));

        CreatePrimitive(PrimitiveType.Cube, "Visual", altar.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.8f, 1f), material, false);

        GameObject placePoint = new GameObject("SeedPlacePoint");
        placePoint.transform.SetParent(altar.transform);
        placePoint.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        SeedAltar seedAltar = altar.AddComponent<SeedAltar>();
        SetReference(seedAltar, "requiredSeed", requiredSeed);
        SetReference(seedAltar, "targetDoor", targetDoor);
        SetReference(seedAltar, "seedPlacePoint", placePoint.transform);
    }

    static void CreateFlameWall(Transform parent, Vector3 triggerPosition, Vector3 wallPosition, Material material)
    {
        GameObject wallTrigger = new GameObject("FlameWallTrigger");
        wallTrigger.transform.SetParent(parent);
        wallTrigger.transform.position = triggerPosition;
        AddTriggerBox(wallTrigger, new Vector3(0f, 1f, 0f), new Vector3(10f, 2f, 1f));

        // Ortadaki 6 m'yi kapatır, kenarlarda 2'şer m yol kalır
        GameObject flameWall = CreatePrimitive(PrimitiveType.Cube, "FlameWall", parent, wallPosition + new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 0.6f), material, true);
        flameWall.GetComponent<Collider>().isTrigger = true;
        flameWall.AddComponent<HazardZone>();

        FlameWallTrigger flameWallTrigger = wallTrigger.AddComponent<FlameWallTrigger>();
        SetReference(flameWallTrigger, "flameWall", flameWall);
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

    // Root'ta collider + script, mesh Visual child'ında (FadingLeaf ve MeltingPlatform için)
    static GameObject CreatePlatformWithVisual(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject platform = new GameObject(name);
        platform.transform.SetParent(parent);
        platform.transform.position = position;

        BoxCollider platformCollider = platform.AddComponent<BoxCollider>();
        platformCollider.size = size;

        CreatePrimitive(PrimitiveType.Cube, "Visual", platform.transform, Vector3.zero, size, material, false);
        return platform;
    }

    static void AddTriggerBox(GameObject target, Vector3 center, Vector3 size)
    {
        BoxCollider triggerBox = target.AddComponent<BoxCollider>();
        triggerBox.isTrigger = true;
        triggerBox.center = center;
        triggerBox.size = size;
    }

    static Material CreateMaterial(string name, Color color)
    {
        string path = materialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            AssetDatabase.CreateFolder("Assets", "TestScene");
        }

        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder(rootFolder, "Materials");
        }
    }

    // Project Settings > Tags and Layers'a "Player" layer'ını ekler
    static int CreatePlayerLayer()
    {
        int existingLayer = LayerMask.NameToLayer("Player");
        if (existingLayer != -1)
        {
            return existingLayer;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // 0-7 Unity'nin kendi layer'ları
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = "Player";
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        Debug.LogError("TestSceneBuilder: boş layer bulunamadı");
        return 0;
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // [SerializeField] private alanlara Inspector'daki gibi değer yazar
    static void SetReference(Object target, string fieldName, Object value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).objectReferenceValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
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

    static void SetVector3(Object target, string fieldName, Vector3 value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).vector3Value = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion
}
