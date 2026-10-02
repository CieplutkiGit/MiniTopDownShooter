using System.Collections.Generic;
using Cieplutki.MiniTopDownShooter.Runtime;
using UnityEditor;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Editor
{
[CustomEditor(typeof(WeaponDefinition))]
public class WeaponDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        WeaponDefinition definition =
            (WeaponDefinition)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Framework Summary",
            EditorStyles.boldLabel);

        float shotsPerSecond =
            definition.FireInterval > 0f
                ? 1f / definition.FireInterval
                : 0f;

        float nominalDps =
            definition.Damage *
            Mathf.Max(1, definition.ProjectilesPerShot) *
            shotsPerSecond;

        EditorGUILayout.LabelField(
            "Nominal DPS",
            nominalDps.ToString("0.0"));

        EditorGUILayout.LabelField(
            "Delivery",
            definition.DeliveryMode.ToString());

        EditorGUILayout.LabelField(
            "Ammo",
            definition.InfiniteAmmo
                ? "Infinite"
                : $"{definition.MagazineSize} magazine / {definition.StartingReserveAmmo} reserve");

        if (definition.DeliveryMode ==
                WeaponDeliveryMode.Projectile &&
            definition.ProjectilePrefab == null)
        {
            EditorGUILayout.HelpBox(
                "Projectile delivery requires a Projectile Prefab.",
                MessageType.Error);
        }

        if (definition.DeliveryMode ==
                WeaponDeliveryMode.Hitscan &&
            definition.HitscanMask.value == 0)
        {
            EditorGUILayout.HelpBox(
                "Hitscan Mask is empty, so the weapon cannot hit anything.",
                MessageType.Error);
        }

        if (definition.FireMode ==
                WeaponFireMode.Shotgun &&
            definition.ProjectilesPerShot <= 1)
        {
            EditorGUILayout.HelpBox(
                "Shotgun mode normally uses more than one projectile/ray per shot.",
                MessageType.Warning);
        }

        if (definition.FireMode ==
                WeaponFireMode.Burst &&
            definition.BurstInterval >=
                definition.FireInterval)
        {
            EditorGUILayout.HelpBox(
                "Burst Interval is greater than or equal to Fire Interval. Verify that the intended burst cadence feels correct.",
                MessageType.Info);
        }

        if (!definition.InfiniteAmmo &&
            definition.StartingReserveAmmo == 0)
        {
            EditorGUILayout.HelpBox(
                "Finite-ammo weapon starts with no reserve ammunition.",
                MessageType.Info);
        }
    }
}

[CustomEditor(typeof(WaveSet))]
public class WaveSetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        WaveSet waveSet = (WaveSet)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Framework Summary",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "Wave Count",
            waveSet.Count.ToString());

        int authoredEnemies = 0;
        int bosses = 0;
        int explicitGroups = 0;

        IReadOnlyList<WaveConfig> waves =
            waveSet.Waves;

        if (waves != null)
        {
            for (int i = 0; i < waves.Count; i++)
            {
                WaveConfig wave = waves[i];

                if (wave == null)
                {
                    EditorGUILayout.HelpBox(
                        $"Wave {i + 1} is null.",
                        MessageType.Warning);
                    continue;
                }

                authoredEnemies +=
                    Mathf.Max(1, wave.EnemyCount);

                bosses +=
                    wave.BossPrefab != null
                        ? Mathf.Max(0, wave.BossCount)
                        : 0;

                if (wave.EnemyGroups != null)
                {
                    for (int groupIndex = 0;
                         groupIndex < wave.EnemyGroups.Count;
                         groupIndex++)
                    {
                        WaveEnemyGroup group =
                            wave.EnemyGroups[groupIndex];

                        if (group == null ||
                            group.Prefab == null)
                        {
                            EditorGUILayout.HelpBox(
                                $"Wave {i + 1} has an enemy group with no prefab.",
                                MessageType.Warning);
                            continue;
                        }

                        explicitGroups++;
                    }
                }

                if (wave.BossCount > 0 &&
                    wave.BossPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        $"Wave {i + 1} has Boss Count {wave.BossCount} but no Boss Prefab.",
                        MessageType.Warning);
                }
            }
        }

        EditorGUILayout.LabelField(
            "Base Enemy Slots",
            authoredEnemies.ToString());

        EditorGUILayout.LabelField(
            "Explicit Enemy Groups",
            explicitGroups.ToString());

        EditorGUILayout.LabelField(
            "Boss Entries",
            bosses.ToString());

        if (waveSet.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Add at least one wave before assigning this asset to a WaveController.",
                MessageType.Warning);
        }
    }
}

[CustomEditor(typeof(EnemyStats))]
public class EnemyStatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EnemyStats stats = (EnemyStats)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Archetype Summary",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "Move Speed",
            stats.MoveSpeed.ToString("0.00"));

        EditorGUILayout.LabelField(
            "Health",
            stats.MaxHealth.ToString());

        EditorGUILayout.LabelField(
            "Melee DPS",
            stats.AttackCooldown > 0f
                ? (stats.Damage / stats.AttackCooldown)
                    .ToString("0.0")
                : "N/A");

        EditorGUILayout.LabelField(
            "Score",
            stats.ScoreValue.ToString());

        if (stats.MoveSpeed <= 0f)
        {
            EditorGUILayout.HelpBox(
                "Move Speed is zero or negative.",
                MessageType.Warning);
        }

        if (stats.MaxHealth <= 0)
        {
            EditorGUILayout.HelpBox(
                "Max Health must be greater than zero.",
                MessageType.Error);
        }

        if (stats.AttackRange <= 0f)
        {
            EditorGUILayout.HelpBox(
                "Attack Range is zero or negative.",
                MessageType.Warning);
        }
    }
}

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        SerializedProperty player =
            serializedObject.FindProperty("_player");

        SerializedProperty prefabs =
            serializedObject.FindProperty("_prefabs");

        SerializedProperty zones =
            serializedObject.FindProperty("_spawnZones");

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Setup Health",
            EditorStyles.boldLabel);

        if (player == null ||
            player.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Player reference is required for legacy radius spawning and distance-constrained SpawnZones.",
                MessageType.Error);
        }

        int validPrefabs = 0;

        if (prefabs != null)
        {
            for (int i = 0; i < prefabs.arraySize; i++)
            {
                SerializedProperty entry =
                    prefabs.GetArrayElementAtIndex(i);

                SerializedProperty prefab =
                    entry.FindPropertyRelative("Prefab");

                if (prefab != null &&
                    prefab.objectReferenceValue != null)
                {
                    validPrefabs++;
                }
            }
        }

        int validZones = 0;
        HashSet<string> zoneIds =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);

        if (zones != null)
        {
            for (int i = 0; i < zones.arraySize; i++)
            {
                SpawnZone zone =
                    zones.GetArrayElementAtIndex(i)
                        .objectReferenceValue
                    as SpawnZone;

                if (zone == null)
                {
                    continue;
                }

                validZones++;

                if (!zoneIds.Add(zone.Id))
                {
                    EditorGUILayout.HelpBox(
                        $"Duplicate SpawnZone ID '{zone.Id}'.",
                        MessageType.Warning);
                }
            }
        }

        EditorGUILayout.LabelField(
            "Global Enemy Prefabs",
            validPrefabs.ToString());

        EditorGUILayout.LabelField(
            "Spawn Zones",
            validZones.ToString());

        if (validPrefabs == 0)
        {
            EditorGUILayout.HelpBox(
                "No global enemy fallback is configured. This is valid only when every authored wave uses explicit enemy groups.",
                MessageType.Info);
        }

        if (validZones == 0)
        {
            EditorGUILayout.HelpBox(
                "No SpawnZones are assigned. The spawner will use legacy radius spawning.",
                MessageType.Info);
        }
    }
}
}
