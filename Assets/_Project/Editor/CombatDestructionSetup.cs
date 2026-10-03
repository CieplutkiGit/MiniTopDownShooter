using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game;
using Game.Combat;

namespace Game.Editor
{
    /// <summary>
    /// Narrow scene setup helper for combat destruction.
    /// Configures cover props and scene targets without modifying shared builders or UI.
    /// </summary>
    public static class CombatDestructionSetup
    {
        private const string ArenaShowcasePath = "Assets/Scenes/ArenaShowcase.unity";

        [MenuItem("Tools/Mini Top Down Shooter/Setup Arena Combat Destruction")]
        public static void SetupArenaShowcase()
        {
            Scene scene = SceneManager.GetActiveScene();
            bool opened = false;

            if (scene.path != ArenaShowcasePath)
            {
                scene = EditorSceneManager.OpenScene(ArenaShowcasePath, OpenSceneMode.Single);
                opened = true;
            }

            SetupScene(scene);

            if (opened)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        public static void SetupScene(Scene scene)
        {
            if (!scene.IsValid()) return;

            GameObject[] roots = scene.GetRootGameObjects();
            int configuredProps = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                MeshRenderer[] renderers = roots[i].GetComponentsInChildren<MeshRenderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    GameObject go = renderers[r].gameObject;
                    if (CombatDestructionInstaller.IsSuitableDestructibleProp(go))
                    {
                        if (go.GetComponent<DestructibleProp>() == null)
                        {
                            DestructibleProp prop = go.AddComponent<DestructibleProp>();
                            int hp = ResolvePropHealth(go.name);
                            prop.Configure(hp);
                            configuredProps++;
                        }
                    }
                }

                EnemyController[] enemies = roots[i].GetComponentsInChildren<EnemyController>(true);
                for (int e = 0; e < enemies.Length; e++)
                {
                    if (enemies[e] != null && enemies[e].GetComponent<EnemyDamageVisuals>() == null)
                    {
                        enemies[e].gameObject.AddComponent<EnemyDamageVisuals>();
                    }
                }
            }

            Debug.Log($"[CombatDestructionSetup] Configured {configuredProps} destructible props in '{scene.name}'.");
        }

        public static DestructibleProp ConfigureProp(GameObject go, int health = 80, int stageDebris = 3, int deathDebris = 10)
        {
            if (go == null) return null;

            DestructibleProp prop = go.GetComponent<DestructibleProp>();
            if (prop == null)
            {
                prop = go.AddComponent<DestructibleProp>();
            }
            prop.Configure(health, stageDebris, deathDebris);
            return prop;
        }

        private static int ResolvePropHealth(string name)
        {
            if (name.IndexOf("Pylon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pillar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 140;
            }
            if (name.IndexOf("Conduit", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 40;
            }
            return 80;
        }
    }
}
