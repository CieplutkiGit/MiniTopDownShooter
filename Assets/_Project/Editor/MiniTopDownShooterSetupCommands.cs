using System;
using System.Collections.Generic;
using System.Reflection;
using Game;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MiniTopDownShooterSetupCommands
{
    private const string RiflePrefabPath =
        "Assets/_Project/Weapons/Gun_Rifle.prefab";

    private const string DefaultEnemyStatsPath =
        "Assets/_Project/Data/EnemyStats_Default.asset";

    private const string ShowcaseWaveSetPath =
        "Assets/_Project/Data/Waves/WaveSet_ArenaShowcase.asset";

    public const string AudioMixerPath =
        "Assets/_Project/Audio/GameAudioMixer.mixer";

    public const string CombatMusicPath =
        "Assets/_Project/Audio/combat_music.wav";

    [MenuItem("Tools/Mini Top Down Shooter/Build/Build StandaloneWindows64")]
    public static void BuildStandaloneWindows64()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/ArenaShowcase.unity",
            "Assets/Scenes/MobileDemo.unity"
        };

        string buildPath = "Builds/StandaloneWindows64/MiniTopDownShooter.exe";
        string buildDir = System.IO.Path.GetDirectoryName(buildPath);
        if (!System.IO.Directory.Exists(buildDir))
        {
            System.IO.Directory.CreateDirectory(buildDir);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Build StandaloneWindows64 SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F2}s");
        }
        else
        {
            Debug.LogError($"[Build] Build StandaloneWindows64 FAILED: {summary.result}, errors: {summary.totalErrors}");
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Player")]
    public static void CreatePlayer()
    {
        GameObject playerObject =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);

        playerObject.name = "Player";
        Undo.RegisterCreatedObjectUndo(
            playerObject,
            "Create Mini Top Down Shooter Player");

        Rigidbody body = playerObject.AddComponent<Rigidbody>();
        body.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        playerObject.AddComponent<PlayerMovement>();
        playerObject.AddComponent<PlayerRotation>();
        PlayerShoot shoot = playerObject.AddComponent<PlayerShoot>();
        playerObject.AddComponent<HealthComponent>();

        DamageAffiliation affiliation =
            playerObject.AddComponent<DamageAffiliation>();

        affiliation.Configure(CombatTeam.Player);

        PlayerController controller =
            playerObject.AddComponent<PlayerController>();

        GameObject riflePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RiflePrefabPath);

        if (riflePrefab != null)
        {
            GameObject rifle =
                PrefabUtility.InstantiatePrefab(riflePrefab)
                    as GameObject;

            if (rifle != null)
            {
                Undo.RegisterCreatedObjectUndo(
                    rifle,
                    "Add Starter Rifle");

                rifle.transform.SetParent(
                    playerObject.transform,
                    false);

                Gun gun = rifle.GetComponent<Gun>();

                SerializedObject serializedShoot =
                    new SerializedObject(shoot);

                serializedShoot.FindProperty("_gun")
                    .objectReferenceValue = gun;

                serializedShoot.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (Camera.main == null)
        {
            CreateMainCamera(playerObject.transform);
        }

        Selection.activeGameObject = playerObject;
        MarkSceneDirty();
        Debug.Log(
            "Created a player with movement, health, affiliation, controller, and starter rifle.");
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Enemy")]
    public static void CreateEnemy()
    {
        GameObject enemyObject =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);

        enemyObject.name = "Enemy";
        Undo.RegisterCreatedObjectUndo(
            enemyObject,
            "Create Mini Top Down Shooter Enemy");

        Collider collider = enemyObject.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger = true;
        }

        enemyObject.AddComponent<NavMeshAgent>();
        enemyObject.AddComponent<EnemyMovement>();
        enemyObject.AddComponent<EnemyAttack>();
        enemyObject.AddComponent<HealthComponent>();

        DamageAffiliation affiliation =
            enemyObject.AddComponent<DamageAffiliation>();

        affiliation.Configure(CombatTeam.Enemy);

        EnemyController controller =
            enemyObject.AddComponent<EnemyController>();

        EnemyStats defaultStats =
            AssetDatabase.LoadAssetAtPath<EnemyStats>(
                DefaultEnemyStatsPath);

        if (defaultStats != null)
        {
            SerializedObject serializedEnemy =
                new SerializedObject(controller);

            serializedEnemy.FindProperty("_statsRef")
                .objectReferenceValue = defaultStats;

            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.activeGameObject = enemyObject;
        MarkSceneDirty();
        Debug.Log(
            "Created a NavMesh melee enemy. Duplicate it or add an EnemyBehaviorBase module for another archetype.");
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Gun")]
    public static void CreateGun()
    {
        GameObject riflePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RiflePrefabPath);

        if (riflePrefab == null)
        {
            Debug.LogError(
                $"Example rifle prefab not found at {RiflePrefabPath}.");
            return;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(riflePrefab)
                as GameObject;

        if (instance == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Create Mini Top Down Shooter Gun");

        if (Selection.activeTransform != null)
        {
            instance.transform.SetParent(
                Selection.activeTransform,
                false);
        }

        Selection.activeGameObject = instance;
        MarkSceneDirty();
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Spawn Zone")]
    public static void CreateSpawnZone()
    {
        GameObject zoneObject = new GameObject("SpawnZone");
        Undo.RegisterCreatedObjectUndo(
            zoneObject,
            "Create Mini Top Down Shooter Spawn Zone");

        zoneObject.AddComponent<SpawnZone>();
        Selection.activeGameObject = zoneObject;
        MarkSceneDirty();
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Arena")]
    public static void CreateArena()
    {
        GameObject arena =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        arena.name = "Arena";
        arena.transform.localScale =
            new Vector3(10f, 1f, 10f);

        Undo.RegisterCreatedObjectUndo(
            arena,
            "Create Mini Top Down Shooter Arena");

        NavMeshSurface surface =
            arena.AddComponent<NavMeshSurface>();

        CreateConfiguredZone(
            "SpawnZone_North",
            new Vector3(0f, 0f, 18f),
            SpawnZoneShape.Circle);

        CreateConfiguredZone(
            "SpawnZone_West",
            new Vector3(-18f, 0f, 0f),
            SpawnZoneShape.Box);

        CreateConfiguredZone(
            "SpawnZone_East",
            new Vector3(18f, 0f, 0f),
            SpawnZoneShape.Circle);

        Selection.activeGameObject = arena;

        try
        {
            surface.BuildNavMesh();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                $"Arena created, but NavMesh baking did not complete automatically: {exception.Message}");
        }

        MarkSceneDirty();
    }

    [MenuItem("Tools/Mini Top Down Shooter/Create/Wave Controller")]
    public static void CreateWaveController()
    {
        PlayerController player =
            Object.FindFirstObjectByType<PlayerController>(
                FindObjectsInactive.Include);

        GameStateController gameState =
            Object.FindFirstObjectByType<GameStateController>(
                FindObjectsInactive.Include);

        if (gameState == null)
        {
            GameObject stateObject =
                new GameObject("GameStateController");

            Undo.RegisterCreatedObjectUndo(
                stateObject,
                "Create Game State Controller");

            gameState =
                stateObject.AddComponent<GameStateController>();

            AssignObjectReference(
                gameState,
                "_playerRef",
                player);
        }

        EffectPool effectPool =
            Object.FindFirstObjectByType<EffectPool>(
                FindObjectsInactive.Include);

        if (effectPool == null)
        {
            GameObject effectObject =
                new GameObject("EffectPool");

            Undo.RegisterCreatedObjectUndo(
                effectObject,
                "Create Effect Pool");

            effectPool =
                effectObject.AddComponent<EffectPool>();
        }

        EnemySpawner spawner =
            Object.FindFirstObjectByType<EnemySpawner>(
                FindObjectsInactive.Include);

        if (spawner == null)
        {
            GameObject spawnerObject =
                new GameObject("EnemySpawner");

            Undo.RegisterCreatedObjectUndo(
                spawnerObject,
                "Create Enemy Spawner");

            spawner =
                spawnerObject.AddComponent<EnemySpawner>();
        }

        AssignObjectReference(
            spawner,
            "_player",
            player != null ? player.transform : null);

        AssignObjectReference(
            spawner,
            "_gameStateRef",
            gameState);

        AssignObjectReference(
            spawner,
            "_effectPool",
            effectPool);

        AssignSpawnZones(spawner);

        WaveController waveController =
            Object.FindFirstObjectByType<WaveController>(
                FindObjectsInactive.Include);

        if (waveController == null)
        {
            GameObject waveObject =
                new GameObject("WaveController");

            Undo.RegisterCreatedObjectUndo(
                waveObject,
                "Create Wave Controller");

            waveController =
                waveObject.AddComponent<WaveController>();
        }

        AssignObjectReference(
            waveController,
            "_spawnerRef",
            spawner);

        AssignObjectReference(
            waveController,
            "_gameStateRef",
            gameState);

        WaveSet showcase =
            AssetDatabase.LoadAssetAtPath<WaveSet>(
                ShowcaseWaveSetPath);

        AssignObjectReference(
            waveController,
            "_waveSet",
            showcase);

        Selection.activeGameObject =
            waveController.gameObject;

        MarkSceneDirty();
    }

    [MenuItem("Tools/Mini Top Down Shooter/Fix Common Setup Issues")]
    public static void FixCommonSetupIssues()
    {
        PlayerController player =
            Object.FindFirstObjectByType<PlayerController>(
                FindObjectsInactive.Include);

        GameStateController gameState =
            Object.FindFirstObjectByType<GameStateController>(
                FindObjectsInactive.Include);

        EnemySpawner spawner =
            Object.FindFirstObjectByType<EnemySpawner>(
                FindObjectsInactive.Include);

        WaveController wave =
            Object.FindFirstObjectByType<WaveController>(
                FindObjectsInactive.Include);

        EffectPool effectPool =
            Object.FindFirstObjectByType<EffectPool>(
                FindObjectsInactive.Include);

        if (gameState != null && player != null)
        {
            AssignIfMissing(
                gameState,
                "_playerRef",
                player);
        }

        if (spawner != null)
        {
            if (player != null)
            {
                AssignIfMissing(
                    spawner,
                    "_player",
                    player.transform);
            }

            if (gameState != null)
            {
                AssignIfMissing(
                    spawner,
                    "_gameStateRef",
                    gameState);
            }

            if (effectPool != null)
            {
                AssignIfMissing(
                    spawner,
                    "_effectPool",
                    effectPool);
            }

            AssignSpawnZones(spawner, true);
        }

        if (wave != null)
        {
            if (spawner != null)
            {
                AssignIfMissing(
                    wave,
                    "_spawnerRef",
                    spawner);
            }

            if (gameState != null)
            {
                AssignIfMissing(
                    wave,
                    "_gameStateRef",
                    gameState);
            }

            SerializedObject serializedWave =
                new SerializedObject(wave);

            SerializedProperty setProperty =
                serializedWave.FindProperty("_waveSet");

            if (setProperty != null &&
                setProperty.objectReferenceValue == null)
            {
                WaveSet showcase =
                    AssetDatabase.LoadAssetAtPath<WaveSet>(
                        ShowcaseWaveSetPath);

                setProperty.objectReferenceValue = showcase;
                serializedWave.ApplyModifiedProperties();
            }
        }

        WireAudioInScene();

        MarkSceneDirty();
        Debug.Log(
            "Filled common missing references where safe. Run Validate Open Scene for remaining issues.");
    }

    private static void CreateMainCamera(
        Transform player)
    {
        GameObject cameraObject =
            new GameObject("Main Camera");

        Undo.RegisterCreatedObjectUndo(
            cameraObject,
            "Create Main Camera");

        cameraObject.tag = "MainCamera";

        Camera camera =
            cameraObject.AddComponent<Camera>();

        cameraObject.transform.position =
            player.position +
            new Vector3(0f, 12f, -8f);

        cameraObject.transform.rotation =
            Quaternion.Euler(55f, 0f, 0f);

        camera.clearFlags = CameraClearFlags.Skybox;
    }

    private static void CreateConfiguredZone(
        string id,
        Vector3 position,
        SpawnZoneShape shape)
    {
        GameObject zoneObject =
            new GameObject(id);

        Undo.RegisterCreatedObjectUndo(
            zoneObject,
            "Create Spawn Zone");

        zoneObject.transform.position = position;

        SpawnZone zone =
            zoneObject.AddComponent<SpawnZone>();

        SerializedObject serialized =
            new SerializedObject(zone);

        serialized.FindProperty("_id").stringValue = id;
        serialized.FindProperty("_shape").enumValueIndex =
            (int)shape;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignSpawnZones(
        EnemySpawner spawner,
        bool onlyIfEmpty = false)
    {
        SpawnZone[] zones =
            Object.FindObjectsByType<SpawnZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        SerializedObject serialized =
            new SerializedObject(spawner);

        SerializedProperty property =
            serialized.FindProperty("_spawnZones");

        if (property == null)
        {
            return;
        }

        if (onlyIfEmpty && property.arraySize > 0)
        {
            return;
        }

        property.arraySize = zones.Length;

        for (int i = 0; i < zones.Length; i++)
        {
            property.GetArrayElementAtIndex(i)
                .objectReferenceValue = zones[i];
        }

        serialized.ApplyModifiedProperties();
    }

    private static void AssignIfMissing(
        Object target,
        string propertyName,
        Object value)
    {
        if (target == null || value == null)
        {
            return;
        }

        SerializedObject serialized =
            new SerializedObject(target);

        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null ||
            property.objectReferenceValue != null)
        {
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void AssignObjectReference(
        Object target,
        string propertyName,
        Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serialized =
            new SerializedObject(target);

        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void MarkSceneDirty()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    [MenuItem("Tools/Mini Top Down Shooter/Wire All Scenes")]
    public static void WireAllScenes()
    {
        string[] scenes = new[]
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/ArenaShowcase.unity",
            "Assets/Scenes/MobileDemo.unity"
        };

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        for (int s = 0; s < scenes.Length; s++)
        {
            string scenePath = scenes[s];
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene not found at {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"Wiring scene: {scene.name}");

            // 1. Player, starter weapons, and WeaponLoadout
            PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            WeaponLoadout loadout = null;
            if (player != null)
            {
                loadout = WirePlayerLoadout(player);
            }

            // 2. Canvas & UI elements
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            VictoryUI victoryUI = Object.FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
            if (victoryUI == null && canvas != null)
            {
                victoryUI = WireVictoryUI(canvas.transform, font);
            }

            SettingsUI settingsUI = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            if (settingsUI == null && canvas != null)
            {
                settingsUI = WireSettingsUI(canvas.transform, player, font);
            }

            WeaponHUD weaponHUD = Object.FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
            if (weaponHUD == null && canvas != null)
            {
                weaponHUD = WireWeaponHUD(canvas.transform, loadout, font);
            }

            BossHealthBarUI bossUI = Object.FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
            if (bossUI == null && canvas != null)
            {
                bossUI = WireBossHealthBarUI(canvas.transform, font);
            }

            // Connect settings buttons to MainMenu and Pause if present
            MainMenuUI mainMenu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (mainMenu != null && settingsUI != null)
            {
                SerializedObject serializedMenu = new SerializedObject(mainMenu);
                SerializedProperty settingsProp = serializedMenu.FindProperty("_settingsUIRef");
                if (settingsProp != null)
                {
                    settingsProp.objectReferenceValue = settingsUI;
                }
                SerializedProperty buttonProp = serializedMenu.FindProperty("_settingsButton");
                if (buttonProp != null && buttonProp.objectReferenceValue == null)
                {
                    Transform parent = mainMenu.transform;
                    SerializedProperty panelProp = serializedMenu.FindProperty("_panel");
                    if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
                    {
                        parent = pObj.transform;
                    }
                    Button existingBtn = parent.Find("SettingsButton")?.GetComponent<Button>();
                    if (existingBtn == null)
                    {
                        existingBtn = CreateButton("SettingsButton", parent, font, "Settings", new Vector2(0f, -60f), new Vector2(160f, 40f));
                    }
                    buttonProp.objectReferenceValue = existingBtn;
                }
                serializedMenu.ApplyModifiedProperties();
            }

            PauseUI pauseUI = Object.FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
            if (pauseUI != null && settingsUI != null)
            {
                SerializedObject serializedPause = new SerializedObject(pauseUI);
                SerializedProperty settingsProp = serializedPause.FindProperty("_settingsUIRef");
                if (settingsProp != null)
                {
                    settingsProp.objectReferenceValue = settingsUI;
                }
                SerializedProperty buttonProp = serializedPause.FindProperty("_settingsButton");
                if (buttonProp != null && buttonProp.objectReferenceValue == null)
                {
                    Transform parent = pauseUI.transform;
                    SerializedProperty panelProp = serializedPause.FindProperty("_panel");
                    if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
                    {
                        parent = pObj.transform;
                    }
                    Button existingBtn = parent.Find("SettingsButton")?.GetComponent<Button>();
                    if (existingBtn == null)
                    {
                        existingBtn = CreateButton("SettingsButton", parent, font, "Settings", new Vector2(0f, -20f), new Vector2(160f, 40f));
                    }
                    buttonProp.objectReferenceValue = existingBtn;
                }
                serializedPause.ApplyModifiedProperties();
            }

            // 3. Fix common missing references
            FixCommonSetupIssues();

            // 4. Ensure GameCompositionRoot
            GameCompositionRoot compRoot = Object.FindFirstObjectByType<GameCompositionRoot>(FindObjectsInactive.Include);
            if (compRoot == null)
            {
                GameObject compObj = new GameObject("GameCompositionRoot");
                compRoot = compObj.AddComponent<GameCompositionRoot>();
            }

            compRoot.ComposeDependencies();
            EditorUtility.SetDirty(compRoot);

            // 5. Mark and Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Successfully wired and saved scene: {scene.name}");
        }

        EditorSceneManager.SaveOpenScenes();
    }

    private static WeaponLoadout WirePlayerLoadout(PlayerController player)
    {
        WeaponLoadout loadout = player.GetComponent<WeaponLoadout>();
        if (loadout == null)
        {
            loadout = player.gameObject.AddComponent<WeaponLoadout>();
        }

        List<Gun> guns = new List<Gun>(player.GetComponentsInChildren<Gun>(true));

        if (guns.Count == 0)
        {
            GameObject riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            if (riflePrefab != null)
            {
                GameObject rifleObj = PrefabUtility.InstantiatePrefab(riflePrefab) as GameObject;
                if (rifleObj != null)
                {
                    rifleObj.transform.SetParent(player.transform, false);
                    Gun rifleGun = rifleObj.GetComponent<Gun>();
                    if (rifleGun != null)
                    {
                        guns.Add(rifleGun);
                    }
                }
            }
        }

        SerializedObject serializedLoadout = new SerializedObject(loadout);
        SerializedProperty weaponsProp = serializedLoadout.FindProperty("_weapons");
        weaponsProp.arraySize = guns.Count;
        for (int i = 0; i < guns.Count; i++)
        {
            weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue = guns[i];
        }

        SerializedProperty mountProp = serializedLoadout.FindProperty("_weaponMount");
        if (mountProp != null && mountProp.objectReferenceValue == null)
        {
            mountProp.objectReferenceValue = player.transform;
        }

        serializedLoadout.ApplyModifiedProperties();

        PlayerShoot shoot = player.GetComponent<PlayerShoot>();
        if (shoot != null && guns.Count > 0)
        {
            SerializedObject serializedShoot = new SerializedObject(shoot);
            SerializedProperty gunProp = serializedShoot.FindProperty("_gun");
            if (gunProp != null && gunProp.objectReferenceValue == null)
            {
                gunProp.objectReferenceValue = guns[0];
                serializedShoot.ApplyModifiedProperties();
            }
        }

        return loadout;
    }

    private static VictoryUI WireVictoryUI(Transform canvasTransform, TMP_FontAsset font)
    {
        GameObject victoryObj = CreateUIObject("VictoryUI", canvasTransform);
        VictoryUI victoryUI = victoryObj.AddComponent<VictoryUI>();

        GameObject panelObj = CreateUIObject("VictoryPanel", victoryObj.transform);
        StretchFull(panelObj.GetComponent<RectTransform>());
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        TextMeshProUGUI title = CreateTMPText("TitleText", panelObj.transform, font, "VICTORY!", 44, new Vector2(0f, 120f), new Vector2(500f, 60f));
        TextMeshProUGUI score = CreateTMPText("ScoreText", panelObj.transform, font, "Final Score: 0", 26, new Vector2(0f, 40f), new Vector2(400f, 40f));
        TextMeshProUGUI highScore = CreateTMPText("HighScoreText", panelObj.transform, font, "High Score: 0", 22, new Vector2(0f, -10f), new Vector2(400f, 40f));
        TextMeshProUGUI waves = CreateTMPText("WavesText", panelObj.transform, font, "Waves Cleared: 0", 22, new Vector2(0f, -50f), new Vector2(400f, 40f));

        Button restartBtn = CreateButton("RestartButton", panelObj.transform, font, "Play Again", new Vector2(0f, -120f), new Vector2(200f, 50f));
        Button menuBtn = CreateButton("MenuButton", panelObj.transform, font, "Main Menu", new Vector2(0f, -180f), new Vector2(200f, 50f));

        SerializedObject serialized = new SerializedObject(victoryUI);
        serialized.FindProperty("_panel").objectReferenceValue = panelObj;
        serialized.FindProperty("_titleLabel").objectReferenceValue = title;
        serialized.FindProperty("_scoreLabel").objectReferenceValue = score;
        serialized.FindProperty("_highScoreLabel").objectReferenceValue = highScore;
        serialized.FindProperty("_wavesLabel").objectReferenceValue = waves;
        serialized.FindProperty("_restartButton").objectReferenceValue = restartBtn;
        serialized.FindProperty("_menuButton").objectReferenceValue = menuBtn;
        serialized.ApplyModifiedProperties();

        panelObj.SetActive(false);
        return victoryUI;
    }

    private static SettingsUI WireSettingsUI(Transform canvasTransform, PlayerController player, TMP_FontAsset font)
    {
        GameObject settingsObj = CreateUIObject("SettingsUI", canvasTransform);
        SettingsUI settingsUI = settingsObj.AddComponent<SettingsUI>();

        GameObject panelObj = CreateUIObject("SettingsPanel", settingsObj.transform);
        StretchFull(panelObj.GetComponent<RectTransform>());
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        CreateTMPText("Title", panelObj.transform, font, "SETTINGS", 36, new Vector2(0f, 180f), new Vector2(400f, 50f));

        Slider masterSlider = CreateSimpleSlider("MasterSlider", panelObj.transform, new Vector2(0f, 110f), 1f);
        Slider musicSlider = CreateSimpleSlider("MusicSlider", panelObj.transform, new Vector2(0f, 60f), 0.8f);
        Slider sfxSlider = CreateSimpleSlider("SFXSlider", panelObj.transform, new Vector2(0f, 10f), 1f);
        Slider sensSlider = CreateSimpleSlider("SensSlider", panelObj.transform, new Vector2(0f, -40f), 1f);
        Slider deadzoneSlider = CreateSimpleSlider("DeadzoneSlider", panelObj.transform, new Vector2(0f, -90f), 0.1f);
        Slider touchScaleSlider = CreateSimpleSlider("TouchScaleSlider", panelObj.transform, new Vector2(0f, -140f), 1f);

        Button saveBtn = CreateButton("SaveButton", panelObj.transform, font, "Save", new Vector2(-110f, -210f), new Vector2(100f, 40f));
        Button defaultsBtn = CreateButton("DefaultsButton", panelObj.transform, font, "Reset", new Vector2(0f, -210f), new Vector2(100f, 40f));
        Button closeBtn = CreateButton("CloseButton", panelObj.transform, font, "Close", new Vector2(110f, -210f), new Vector2(100f, 40f));

        SerializedObject serialized = new SerializedObject(settingsUI);
        serialized.FindProperty("_panel").objectReferenceValue = panelObj;
        serialized.FindProperty("_firstSelected").objectReferenceValue = closeBtn;
        serialized.FindProperty("_masterVolumeSlider").objectReferenceValue = masterSlider;
        serialized.FindProperty("_musicVolumeSlider").objectReferenceValue = musicSlider;
        serialized.FindProperty("_sfxVolumeSlider").objectReferenceValue = sfxSlider;
        serialized.FindProperty("_sensitivitySlider").objectReferenceValue = sensSlider;
        serialized.FindProperty("_deadzoneSlider").objectReferenceValue = deadzoneSlider;
        serialized.FindProperty("_touchScaleSlider").objectReferenceValue = touchScaleSlider;
        serialized.FindProperty("_saveButton").objectReferenceValue = saveBtn;
        serialized.FindProperty("_resetDefaultsButton").objectReferenceValue = defaultsBtn;
        serialized.FindProperty("_closeButton").objectReferenceValue = closeBtn;
        AudioMixer mixer = EnsureAudioMixer();
        if (mixer != null)
        {
            serialized.FindProperty("_audioMixer").objectReferenceValue = mixer;
        }

        if (player != null)
        {
            serialized.FindProperty("_player").objectReferenceValue = player;
        }
        serialized.ApplyModifiedProperties();

        panelObj.SetActive(false);
        return settingsUI;
    }

    private static WeaponHUD WireWeaponHUD(Transform canvasTransform, WeaponLoadout loadout, TMP_FontAsset font)
    {
        GameObject hudObj = CreateUIObject("WeaponHUD", canvasTransform);
        RectTransform rt = hudObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-30f, 30f);
        rt.sizeDelta = new Vector2(250f, 100f);

        WeaponHUD hud = hudObj.AddComponent<WeaponHUD>();

        TextMeshProUGUI nameLabel = CreateTMPText("WeaponName", hudObj.transform, font, "Rifle", 20, new Vector2(0f, 35f), new Vector2(240f, 30f), TextAlignmentOptions.Right);
        TextMeshProUGUI ammoLabel = CreateTMPText("AmmoLabel", hudObj.transform, font, "30 / 90", 26, new Vector2(0f, 5f), new Vector2(240f, 35f), TextAlignmentOptions.Right);
        TextMeshProUGUI reloadLabel = CreateTMPText("ReloadStatus", hudObj.transform, font, "", 16, new Vector2(0f, -25f), new Vector2(240f, 25f), TextAlignmentOptions.Right);
        TextMeshProUGUI loadoutLabel = CreateTMPText("LoadoutLabel", hudObj.transform, font, "1 / 1", 16, new Vector2(0f, -45f), new Vector2(240f, 25f), TextAlignmentOptions.Right);

        Slider reloadSlider = CreateSimpleSlider("ReloadProgress", hudObj.transform, new Vector2(0f, -15f), 0f);
        reloadSlider.gameObject.SetActive(false);

        SerializedObject serialized = new SerializedObject(hud);
        if (loadout != null)
        {
            serialized.FindProperty("_loadoutRef").objectReferenceValue = loadout;
        }
        serialized.FindProperty("_weaponNameLabel").objectReferenceValue = nameLabel;
        serialized.FindProperty("_ammoLabel").objectReferenceValue = ammoLabel;
        serialized.FindProperty("_reloadStatusLabel").objectReferenceValue = reloadLabel;
        serialized.FindProperty("_reloadProgressSlider").objectReferenceValue = reloadSlider;
        serialized.FindProperty("_loadoutLabel").objectReferenceValue = loadoutLabel;
        serialized.ApplyModifiedProperties();

        return hud;
    }

    private static BossHealthBarUI WireBossHealthBarUI(Transform canvasTransform, TMP_FontAsset font)
    {
        GameObject bossBarObj = CreateUIObject("BossHealthBarUI", canvasTransform);
        RectTransform rt = bossBarObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(500f, 70f);

        BossHealthBarUI bossUI = bossBarObj.AddComponent<BossHealthBarUI>();

        GameObject panelObj = CreateUIObject("Panel", bossBarObj.transform);
        StretchFull(panelObj.GetComponent<RectTransform>());

        TextMeshProUGUI nameLabel = CreateTMPText("BossName", panelObj.transform, font, "BOSS", 22, new Vector2(0f, 15f), new Vector2(400f, 30f));
        TextMeshProUGUI phaseLabel = CreateTMPText("PhaseLabel", panelObj.transform, font, "Phase 1", 16, new Vector2(0f, -5f), new Vector2(400f, 25f));
        Slider healthSlider = CreateSimpleSlider("HealthSlider", panelObj.transform, new Vector2(0f, -25f), 1f);

        SerializedObject serialized = new SerializedObject(bossUI);
        serialized.FindProperty("_panel").objectReferenceValue = panelObj;
        serialized.FindProperty("_bossNameLabel").objectReferenceValue = nameLabel;
        serialized.FindProperty("_phaseLabel").objectReferenceValue = phaseLabel;
        serialized.FindProperty("_healthSlider").objectReferenceValue = healthSlider;
        serialized.ApplyModifiedProperties();

        panelObj.SetActive(false);
        return bossUI;
    }

    public static void WireAudioInScene()
    {
        AudioMixer mixer = EnsureAudioMixer();
        AudioMixerGroup musicGroup = null;
        AudioMixerGroup sfxGroup = null;

        if (mixer != null)
        {
            AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");
            if (musicGroups != null && musicGroups.Length > 0)
            {
                for (int i = 0; i < musicGroups.Length; i++)
                {
                    if (musicGroups[i].name == "Music")
                    {
                        musicGroup = musicGroups[i];
                        break;
                    }
                }
            }

            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            if (sfxGroups != null && sfxGroups.Length > 0)
            {
                for (int i = 0; i < sfxGroups.Length; i++)
                {
                    if (sfxGroups[i].name == "SFX")
                    {
                        sfxGroup = sfxGroups[i];
                        break;
                    }
                }
            }
        }

        AudioClip combatMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatMusicPath);
        GameStateAudio gameStateAudio = Object.FindFirstObjectByType<GameStateAudio>(FindObjectsInactive.Include);
        AudioSource musicSource = null;

        if (gameStateAudio != null)
        {
            SerializedObject serializedGSA = new SerializedObject(gameStateAudio);
            if (combatMusic != null)
            {
                serializedGSA.FindProperty("_combatMusicClip").objectReferenceValue = combatMusic;
            }

            SerializedProperty musicSourceProp = serializedGSA.FindProperty("_musicSource");
            musicSource = musicSourceProp.objectReferenceValue as AudioSource;
            if (musicSource == null)
            {
                Transform musicChild = gameStateAudio.transform.Find("MusicSource");
                if (musicChild != null)
                {
                    musicSource = musicChild.GetComponent<AudioSource>();
                }
                if (musicSource == null)
                {
                    GameObject musicObj = new GameObject("MusicSource");
                    musicObj.transform.SetParent(gameStateAudio.transform, false);
                    musicSource = musicObj.AddComponent<AudioSource>();
                }
                musicSourceProp.objectReferenceValue = musicSource;
            }

            if (musicSource != null && musicGroup != null)
            {
                musicSource.outputAudioMixerGroup = musicGroup;
                EditorUtility.SetDirty(musicSource);
            }

            SerializedProperty sourceProp = serializedGSA.FindProperty("_source");
            AudioSource gsaSource = sourceProp.objectReferenceValue as AudioSource;
            if (gsaSource == null)
            {
                gsaSource = gameStateAudio.GetComponent<AudioSource>();
                if (gsaSource == null)
                {
                    gsaSource = gameStateAudio.gameObject.AddComponent<AudioSource>();
                }
                sourceProp.objectReferenceValue = gsaSource;
            }

            if (gsaSource != null && sfxGroup != null)
            {
                gsaSource.outputAudioMixerGroup = sfxGroup;
                EditorUtility.SetDirty(gsaSource);
            }

            serializedGSA.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameStateAudio);
        }

        SettingsUI settingsUI = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settingsUI != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settingsUI);
            if (mixer != null)
            {
                serializedSettings.FindProperty("_audioMixer").objectReferenceValue = mixer;
            }
            if (musicSource != null)
            {
                serializedSettings.FindProperty("_musicSource").objectReferenceValue = musicSource;
            }
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settingsUI);
        }

        AudioSource[] allAudioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allAudioSources.Length; i++)
        {
            AudioSource asrc = allAudioSources[i];
            if (musicSource != null && asrc == musicSource)
            {
                continue;
            }
            if (sfxGroup != null)
            {
                asrc.outputAudioMixerGroup = sfxGroup;
                EditorUtility.SetDirty(asrc);
            }
        }

        // Clean up stale standalone scene-level GunAudio objects (fixed-gun legacy playback)
        GunAudio[] sceneGunAudios = Object.FindObjectsByType<GunAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneGunAudios.Length; i++)
        {
            GunAudio ga = sceneGunAudios[i];
            if (ga != null && ga.GetComponent<Gun>() == null && ga.GetComponentInParent<Gun>() == null)
            {
                Object.DestroyImmediate(ga.gameObject);
            }
        }

        // Ensure every Gun in the scene has authored audio bindings
        Gun[] sceneGuns = Object.FindObjectsByType<Gun>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneGuns.Length; i++)
        {
            Gun gun = sceneGuns[i];
            if (gun == null) continue;

            AudioSource gunSource = gun.GetComponent<AudioSource>();
            if (gunSource == null)
            {
                gunSource = gun.gameObject.AddComponent<AudioSource>();
                gunSource.playOnAwake = false;
                gunSource.loop = false;
                gunSource.spatialBlend = 0f;
            }
            if (sfxGroup != null)
            {
                gunSource.outputAudioMixerGroup = sfxGroup;
            }
            EditorUtility.SetDirty(gunSource);

            GunAudio gunAudio = gun.GetComponent<GunAudio>();
            if (gunAudio == null)
            {
                gunAudio = gun.gameObject.AddComponent<GunAudio>();
            }

            SerializedObject serializedAudio = new SerializedObject(gunAudio);
            SerializedProperty gunProp = serializedAudio.FindProperty("_gunRef");
            if (gunProp != null && gunProp.objectReferenceValue == null)
            {
                gunProp.objectReferenceValue = gun;
            }
            SerializedProperty srcProp = serializedAudio.FindProperty("_source");
            if (srcProp != null && srcProp.objectReferenceValue == null)
            {
                srcProp.objectReferenceValue = gunSource;
            }
            SerializedProperty clipProp = serializedAudio.FindProperty("_shotClip");
            if (clipProp != null && clipProp.objectReferenceValue == null)
            {
                clipProp.objectReferenceValue = ResolveShotClipForGun(gun);
            }
            serializedAudio.ApplyModifiedProperties();
            EditorUtility.SetDirty(gunAudio);
        }
    }

    public static AudioClip ResolveShotClipForGun(Gun gun)
    {
        string id = gun != null && gun.Definition != null ? gun.Definition.WeaponId : (gun != null ? gun.name : "");
        id = id.ToLowerInvariant();

        if (id.Contains("pistol"))
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_pistol.wav");
        }
        if (id.Contains("shotgun"))
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_shotgun.wav");
        }
        if (id.Contains("launcher"))
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_shotgun.wav");
        }
        if (id.Contains("smg"))
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_pistol.wav");
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_rifle.wav");
    }

    public static AudioMixer EnsureAudioMixer()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);
        if (mixer != null)
        {
            return mixer;
        }

        return CreateGameAudioMixerAsset();
    }

    public static AudioMixer CreateGameAudioMixerAsset()
    {
        string path = AudioMixerPath;
        string dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        Type controllerType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
        Type groupType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
        Type audioGroupParamPathType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
        Type exposedParamType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.ExposedAudioParameter");

        var createMethod = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var controller = createMethod.Invoke(null, new object[] { path });

        var masterGroupProp = controllerType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var masterGroup = masterGroupProp.GetValue(controller);

        var createNewGroupMethod = controllerType.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var musicGroup = createNewGroupMethod.Invoke(controller, new object[] { "Music", false });
        var sfxGroup = createNewGroupMethod.Invoke(controller, new object[] { "SFX", false });

        var childrenProp = groupType.GetProperty("children", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var childrenArray = Array.CreateInstance(groupType, 2);
        childrenArray.SetValue(musicGroup, 0);
        childrenArray.SetValue(sfxGroup, 1);
        childrenProp.SetValue(masterGroup, childrenArray);

        var getGUIDForVolume = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var addExposedParamMethod = controllerType.GetMethod("AddExposedParameter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        object p1 = Activator.CreateInstance(audioGroupParamPathType, new object[] { masterGroup, getGUIDForVolume.Invoke(masterGroup, null) });
        addExposedParamMethod.Invoke(controller, new object[] { p1 });

        object p2 = Activator.CreateInstance(audioGroupParamPathType, new object[] { musicGroup, getGUIDForVolume.Invoke(musicGroup, null) });
        addExposedParamMethod.Invoke(controller, new object[] { p2 });

        object p3 = Activator.CreateInstance(audioGroupParamPathType, new object[] { sfxGroup, getGUIDForVolume.Invoke(sfxGroup, null) });
        addExposedParamMethod.Invoke(controller, new object[] { p3 });

        var exposedParamsProp = controllerType.GetProperty("exposedParameters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Array exposed = (Array)exposedParamsProp.GetValue(controller);

        var nameField = exposedParamType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        object e0 = exposed.GetValue(0);
        nameField.SetValue(e0, "MasterVolume");
        exposed.SetValue(e0, 0);

        object e1 = exposed.GetValue(1);
        nameField.SetValue(e1, "MusicVolume");
        exposed.SetValue(e1, 1);

        object e2 = exposed.GetValue(2);
        nameField.SetValue(e2, "SFXVolume");
        exposed.SetValue(e2, 2);

        exposedParamsProp.SetValue(controller, exposed);

        EditorUtility.SetDirty((UnityEngine.Object)controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent, TMP_FontAsset font, string text, float fontSize, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        CreateTMPText("Text", obj.transform, font, label, 18, Vector2.zero, size);
        return btn;
    }

    private static Slider CreateSimpleSlider(string name, Transform parent, Vector2 anchoredPos, float defaultValue)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300f, 20f);

        Slider slider = obj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        GameObject bg = CreateUIObject("Background", obj.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        GameObject fillArea = CreateUIObject("Fill Area", obj.transform);
        StretchFull(fillArea.GetComponent<RectTransform>());

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f, 0.9f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        return slider;
    }
}
