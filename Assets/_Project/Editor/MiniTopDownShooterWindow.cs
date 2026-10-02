using System.Collections.Generic;
using System.Text;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class MiniTopDownShooterWindow : EditorWindow
{
    private const string DocumentationPath = "Assets/_Project/Documentation/README.md";
    private Vector2 _scroll;
    private string _lastReport = "Run validation to inspect the currently open scene.";

    [MenuItem("Tools/Mini Top Down Shooter/Setup & Validation")]
    public static void ShowWindow()
    {
        MiniTopDownShooterWindow window = GetWindow<MiniTopDownShooterWindow>();
        window.titleContent = new GUIContent("Mini Top Down Shooter");
        window.minSize = new Vector2(540f, 440f);
        window.Show();
    }

    [MenuItem("Tools/Mini Top Down Shooter/Validate Open Scene")]
    public static void ValidateFromMenu()
    {
        string report = BuildValidationReport(out int errors, out int warnings);
        LogReport(report, errors, warnings);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Mini Top Down Shooter", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Setup, validation, and reusable gameplay data authoring.",
            EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Open scene: {SceneManager.GetActiveScene().name}");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate Open Scene", GUILayout.Height(30)))
            {
                _lastReport = BuildValidationReport(out int errors, out int warnings);
                LogReport(_lastReport, errors, warnings);
            }

            if (GUILayout.Button("Create Weapon Definition", GUILayout.Height(30)))
            {
                CreateWeaponDefinition();
            }

            if (GUILayout.Button("Create Wave Set", GUILayout.Height(30)))
            {
                CreateWaveSet();
            }
        }

        if (GUILayout.Button("Open Documentation"))
        {
            Object documentation = AssetDatabase.LoadAssetAtPath<Object>(DocumentationPath);
            if (documentation != null)
            {
                AssetDatabase.OpenAsset(documentation);
            }
            else
            {
                Debug.LogWarning($"Documentation not found at {DocumentationPath}.");
            }
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Validation report", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_lastReport, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private static string BuildValidationReport(out int errors, out int warnings)
    {
        errors = 0;
        warnings = 0;

        StringBuilder report = new StringBuilder();
        report.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
        report.AppendLine();

        ValidateSingleton<PlayerController>("PlayerController", report, ref errors, ref warnings);
        ValidateSingleton<GameStateController>("GameStateController", report, ref errors, ref warnings);
        ValidateSingleton<EnemySpawner>("EnemySpawner", report, ref errors, ref warnings);
        ValidateSingleton<WaveController>("WaveController", report, ref errors, ref warnings);
        ValidateSingleton<EffectPool>("EffectPool", report, ref errors, ref warnings);

        PlayerController[] players = Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            SerializedObject serializedPlayer = new SerializedObject(player);
            SerializedProperty aimCamera = serializedPlayer.FindProperty("_aimCamera");

            if ((aimCamera == null || aimCamera.objectReferenceValue == null) && Camera.main == null)
            {
                AddWarning(
                    report,
                    $"PlayerController '{player.name}' has no aim camera and no Main Camera is available. Mouse aiming will be disabled.",
                    ref warnings);
            }
        }

        Gun[] guns = Object.FindObjectsByType<Gun>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (guns.Length == 0)
        {
            AddError(report, "No Gun component found.", ref errors);
        }
        else
        {
            report.AppendLine($"[OK] Gun components: {guns.Length}");
            foreach (Gun gun in guns)
            {
                ValidateGun(gun, report, ref errors, ref warnings);
            }
        }

        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        bool hasGlobalEnemyFallback = false;

        foreach (EnemySpawner spawner in spawners)
        {
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            SerializedProperty player = serializedSpawner.FindProperty("_player");
            hasGlobalEnemyFallback |= spawner.HasFallbackPrefabs;

            if (!spawner.HasFallbackPrefabs)
            {
                AddWarning(
                    report,
                    $"EnemySpawner '{spawner.name}' has no global fallback prefabs. Every wave must use explicit enemy groups.",
                    ref warnings);
            }

            if (player == null || player.objectReferenceValue == null)
            {
                AddError(report, $"EnemySpawner '{spawner.name}' has no player reference.", ref errors);
            }
        }

        HashSet<string> knownSpawnZoneIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        SpawnZone[] spawnZones = Object.FindObjectsByType<SpawnZone>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SpawnZone zone in spawnZones)
        {
            if (string.IsNullOrWhiteSpace(zone.Id))
            {
                AddError(report, $"SpawnZone '{zone.name}' has an empty ID.", ref errors);
                continue;
            }

            if (!knownSpawnZoneIds.Add(zone.Id))
            {
                AddWarning(
                    report,
                    $"Duplicate SpawnZone ID '{zone.Id}'. Per-wave zone selection may be ambiguous.",
                    ref warnings);
            }
        }

        if (spawnZones.Length > 0)
        {
            report.AppendLine($"[OK] Spawn zones: {spawnZones.Length}");
        }

        WaveController[] waveControllers = Object.FindObjectsByType<WaveController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (WaveController waveController in waveControllers)
        {
            SerializedObject serializedWaveController = new SerializedObject(waveController);
            SerializedProperty waves = serializedWaveController.FindProperty("_waves");

            bool hasReusableSet = waveController.WaveSet != null && waveController.WaveSet.Count > 0;
            bool hasInlineWaves = waves != null && waves.arraySize > 0;

            if (!hasReusableSet && !hasInlineWaves)
            {
                AddWarning(
                    report,
                    $"WaveController '{waveController.name}' has no Wave Set or inline waves.",
                    ref warnings);
            }
            else if (waveController.WaveSet == null && hasInlineWaves)
            {
                AddWarning(
                    report,
                    $"WaveController '{waveController.name}' uses inline waves. Assign a Wave Set for reusable configuration.",
                    ref warnings);
            }

            IReadOnlyList<WaveConfig> configuredWaves = waveController.ConfiguredWaves;

            if (configuredWaves != null)
            {
                for (int waveIndex = 0; waveIndex < configuredWaves.Count; waveIndex++)
                {
                    WaveConfig wave = configuredWaves[waveIndex];

                    if (wave == null)
                    {
                        continue;
                    }

                    if (wave.SpawnZoneIds != null)
                    {
                        for (int zoneIndex = 0; zoneIndex < wave.SpawnZoneIds.Count; zoneIndex++)
                        {
                            string zoneId = wave.SpawnZoneIds[zoneIndex];

                            if (!string.IsNullOrWhiteSpace(zoneId) &&
                                !knownSpawnZoneIds.Contains(zoneId))
                            {
                                AddWarning(
                                    report,
                                    $"Wave {waveIndex + 1} references missing SpawnZone ID '{zoneId}'.",
                                    ref warnings);
                            }
                        }
                    }

                    if (wave.EnemyGroups != null)
                    {
                        for (int groupIndex = 0; groupIndex < wave.EnemyGroups.Count; groupIndex++)
                        {
                            WaveEnemyGroup group = wave.EnemyGroups[groupIndex];

                            if (group != null && group.Prefab == null)
                            {
                                AddWarning(
                                    report,
                                    $"Wave {waveIndex + 1} enemy group {groupIndex + 1} has no prefab and will be ignored.",
                                    ref warnings);
                            }
                        }
                    }

                    bool hasExplicitEnemyGroup = false;

                    if (wave.EnemyGroups != null)
                    {
                        for (int groupIndex = 0; groupIndex < wave.EnemyGroups.Count; groupIndex++)
                        {
                            WaveEnemyGroup group = wave.EnemyGroups[groupIndex];

                            if (group != null && group.Prefab != null)
                            {
                                hasExplicitEnemyGroup = true;
                                break;
                            }
                        }
                    }

                    if (!hasExplicitEnemyGroup && !hasGlobalEnemyFallback)
                    {
                        AddError(
                            report,
                            $"Wave {waveIndex + 1} relies on global enemy weights, but EnemySpawner has no fallback prefabs.",
                            ref errors);
                    }

                    if (wave.BossCount > 0 && wave.BossPrefab == null)
                    {
                        AddWarning(
                            report,
                            $"Wave {waveIndex + 1} has Boss Count {wave.BossCount} but no Boss Prefab.",
                            ref warnings);
                    }
                }
            }
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            AddWarning(report, "No baked NavMesh detected.", ref warnings);
        }
        else
        {
            report.AppendLine("[OK] Baked NavMesh detected.");
        }

        report.AppendLine();
        report.AppendLine($"Result: {errors} error(s), {warnings} warning(s).");
        return report.ToString();
    }

    private static void ValidateSingleton<T>(
        string displayName,
        StringBuilder report,
        ref int errors,
        ref int warnings)
        where T : Component
    {
        T[] objects = Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (objects.Length == 0)
        {
            AddError(report, $"{displayName} is missing.", ref errors);
        }
        else if (objects.Length > 1)
        {
            AddWarning(report, $"Found {objects.Length} {displayName} components.", ref warnings);
        }
        else
        {
            report.AppendLine($"[OK] {displayName}");
        }
    }

    private static void ValidateGun(
        Gun gun,
        StringBuilder report,
        ref int errors,
        ref int warnings)
    {
        SerializedObject serializedGun = new SerializedObject(gun);
        SerializedProperty spawnPoint = serializedGun.FindProperty("_spawnPoint");
        SerializedProperty legacyPrefab = serializedGun.FindProperty("_prefab");

        if (spawnPoint == null || spawnPoint.objectReferenceValue == null)
        {
            AddError(report, $"Gun '{gun.name}' has no spawn point.", ref errors);
        }

        if (gun.Definition == null)
        {
            if (legacyPrefab == null || legacyPrefab.objectReferenceValue == null)
            {
                AddError(report, $"Gun '{gun.name}' has no projectile configuration.", ref errors);
            }
            else
            {
                AddWarning(
                    report,
                    $"Gun '{gun.name}' uses legacy inline values. Assign a Weapon Definition for reusable configuration.",
                    ref warnings);
            }

            return;
        }

        if (gun.Definition.DeliveryMode == WeaponDeliveryMode.Projectile &&
            gun.Definition.ProjectilePrefab == null)
        {
            if (legacyPrefab == null || legacyPrefab.objectReferenceValue == null)
            {
                AddError(
                    report,
                    $"Projectile Weapon Definition '{gun.Definition.name}' has no projectile prefab.",
                    ref errors);
            }
            else
            {
                AddWarning(
                    report,
                    $"Weapon Definition '{gun.Definition.name}' falls back to Gun '{gun.name}' legacy projectile prefab.",
                    ref warnings);
            }
        }
    }

    private static void CreateWeaponDefinition()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Weapon Definition",
            "WeaponDefinition",
            "asset",
            "Choose where to save the reusable weapon configuration.",
            "Assets/_Project/Data");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        WeaponDefinition definition = CreateInstance<WeaponDefinition>();
        AssetDatabase.CreateAsset(definition, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = definition;
        EditorGUIUtility.PingObject(definition);
    }

    private static void CreateWaveSet()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Wave Set",
            "WaveSet",
            "asset",
            "Choose where to save the reusable wave configuration.",
            "Assets/_Project/Data");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        WaveSet waveSet = CreateInstance<WaveSet>();
        AssetDatabase.CreateAsset(waveSet, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = waveSet;
        EditorGUIUtility.PingObject(waveSet);
    }

    private static void LogReport(string report, int errors, int warnings)
    {
        if (errors > 0)
        {
            Debug.LogError(report);
        }
        else if (warnings > 0)
        {
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }
    }

    private static void AddError(StringBuilder report, string message, ref int errors)
    {
        errors++;
        report.AppendLine($"[ERROR] {message}");
    }

    private static void AddWarning(StringBuilder report, string message, ref int warnings)
    {
        warnings++;
        report.AppendLine($"[WARN] {message}");
    }
}
