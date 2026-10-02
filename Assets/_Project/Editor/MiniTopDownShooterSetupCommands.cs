using Cieplutki.MiniTopDownShooter.Runtime;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Cieplutki.MiniTopDownShooter.Editor
{
public static class MiniTopDownShooterSetupCommands
{
    private const string RiflePrefabPath =
        "Assets/_Project/Weapons/Gun_Rifle.prefab";

    private const string DefaultEnemyStatsPath =
        "Assets/_Project/Data/EnemyStats_Default.asset";

    private const string ShowcaseWaveSetPath =
        "Assets/_Project/Data/Waves/WaveSet_ArenaShowcase.asset";

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
}
}
