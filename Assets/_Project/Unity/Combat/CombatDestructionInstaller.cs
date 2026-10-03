using System;
using System.Collections.Generic;
using Core;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    /// <summary>
    /// Runtime installer that safely hooks up destructible props, mark pools,
    /// and enemy progressive damage visuals in production Arena scenes without editing shared scenes or existing builders.
    /// Resets subscriptions across disabled domain reloads and scopes pools to the active scene.
    /// </summary>
    public static class CombatDestructionInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticRegistration()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded)
            {
                Install(active);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Install(scene);
        }

        public static void Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            // Limit environment alterations strictly to Arena mission scenes
            if (string.IsNullOrEmpty(scene.name) || scene.name.IndexOf("Arena", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            // Scope pools to actual scene if created
            if (CombatImpactPool.Instance != null && CombatImpactPool.Instance.gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(CombatImpactPool.Instance.gameObject, scene);
            }
            if (CombatDebrisPool.Instance != null && CombatDebrisPool.Instance.gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(CombatDebrisPool.Instance.gameObject, scene);
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                if (root == null) continue;

                ProcessHierarchy(root);
            }
        }

        public static void ProcessHierarchy(GameObject root)
        {
            // Process props in hierarchy
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                if (mr == null) continue;

                GameObject go = mr.gameObject;
                if (IsSuitableDestructibleProp(go))
                {
                    if (go.GetComponent<DestructibleProp>() == null)
                    {
                        int hp = ResolvePropHealth(go.name);
                        DestructibleProp prop = go.AddComponent<DestructibleProp>();
                        prop.Configure(hp);
                    }
                }
            }

            // Ensure all enemies have EnemyDamageVisuals
            EnemyController[] enemies = root.GetComponentsInChildren<EnemyController>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].GetComponent<EnemyDamageVisuals>() == null)
                {
                    enemies[i].gameObject.AddComponent<EnemyDamageVisuals>();
                }
            }
        }

        public static bool IsSuitableDestructibleProp(GameObject go)
        {
            if (go == null) return false;

            // Must have a non-trigger collider
            Collider col = go.GetComponent<Collider>();
            if (col == null || col.isTrigger) return false;

            // Must not already be damageable
            if (go.GetComponent<IDamageable>() != null) return false;

            // Ancestor component exclusions (Player, Gun, Enemy, Canvas, Camera, Light)
            if (go.GetComponentInParent<PlayerController>() != null) return false;
            if (go.GetComponentInParent<Gun>() != null) return false;
            if (go.GetComponentInParent<EnemyController>() != null) return false;
            if (go.GetComponentInParent<Canvas>() != null) return false;
            if (go.GetComponentInParent<Camera>() != null) return false;
            if (go.GetComponentInParent<Light>() != null) return false;

            // Ancestor name inspection to strictly protect boundaries and essential gameplay triggers
            Transform cur = go.transform;
            while (cur != null)
            {
                string n = cur.name;
                if (n.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("NavMesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("World", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Bound", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Perimeter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("SpawnZone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Canvas", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
                cur = cur.parent;
            }

            // Must match suitable cover / prop names
            string name = go.name;
            bool isCover =
                name.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Cover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pylon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Conduit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Barrier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Crate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Barrel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Prop", StringComparison.OrdinalIgnoreCase) >= 0;

            return isCover;
        }

        private static int ResolvePropHealth(string name)
        {
            if (name.IndexOf("Pylon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 140; // Heavy pillar / pylon
            }
            if (name.IndexOf("Conduit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 40;  // Light conduit pipe
            }
            return 80;      // Medium cover block / obstacle
        }
    }
}
