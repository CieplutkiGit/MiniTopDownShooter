using System;
using System.IO;
using Game;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class WeaponAudioPipeline
    {
        public const string MixerPath = "Assets/_Project/Audio/GameAudioMixer.mixer";

        private static readonly (string PrefabPath, string ClipPath)[] WeaponConfigs = new[]
        {
            ("Assets/_Project/Weapons/Gun_Rifle.prefab", "Assets/_Project/Audio/shot_rifle.wav"),
            ("Assets/_Project/Weapons/Gun_Pistol.prefab", "Assets/_Project/Audio/shot_pistol.wav"),
            ("Assets/_Project/Weapons/Gun_Shotgun.prefab", "Assets/_Project/Audio/shot_shotgun.wav"),
            ("Assets/_Project/Weapons/Gun_SMG.prefab", "Assets/_Project/Audio/shot_pistol.wav"),
            ("Assets/_Project/Weapons/Gun_Launcher.prefab", "Assets/_Project/Audio/shot_shotgun.wav")
        };

        private static readonly string[] ScenePaths = new[]
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/MobileDemo.unity"
        };

        [MenuItem("Tools/Mini Top Down Shooter/Audio/Setup Weapon Audio Prefabs and Scenes")]
        public static void ExecuteAll()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            ConfigureAllWeaponPrefabs();
            ResaveAndCleanScenes();
            ConvertArenaToText();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WeaponAudioPipeline] Completed weapon audio setup and scene resave successfully.");
        }

        [MenuItem("Tools/Mini Top Down Shooter/Audio/Force Reserialize Arena")]
        public static void ForceReserializeArena()
        {
            ConvertArenaToText();
        }

        public static void ConvertArenaToText()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();

            string scenePath = "Assets/Scenes/ArenaShowcase.unity";
            Scene originalScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Create a brand new additive scene so Unity creates a fresh scene definition
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject[] rootObjs = originalScene.GetRootGameObjects();
            for (int i = 0; i < rootObjs.Length; i++)
            {
                SceneManager.MoveGameObjectToScene(rootObjs[i], newScene);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.14f, 0.16f, 0.20f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.09f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.04f);

            EditorSceneManager.CloseScene(originalScene, true);

            // Clean up any stale standalone scene GunAudio objects
            GunAudio[] sceneGunAudios = Object.FindObjectsByType<GunAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneGunAudios.Length; i++)
            {
                GunAudio ga = sceneGunAudios[i];
                if (ga != null && ga.GetComponent<Gun>() == null && ga.GetComponentInParent<Gun>() == null)
                {
                    Debug.Log($"[WeaponAudioPipeline] Removing stale standalone GunAudio object '{ga.gameObject.name}' from ArenaShowcase");
                    Object.DestroyImmediate(ga.gameObject);
                }
            }

            // Re-bake NavMesh and persist NavMeshData to external asset
            NavMeshSurface navMesh = Object.FindFirstObjectByType<NavMeshSurface>();
            if (navMesh != null)
            {
                navMesh.BuildNavMesh();
                if (navMesh.navMeshData != null)
                {
                    string navMeshFolder = "Assets/Scenes/ArenaShowcase";
                    if (!Directory.Exists(navMeshFolder))
                    {
                        Directory.CreateDirectory(navMeshFolder);
                    }
                    string navMeshAssetPath = "Assets/Scenes/ArenaShowcase/NavMesh-Plane.asset";
                    if (File.Exists(navMeshAssetPath))
                    {
                        AssetDatabase.DeleteAsset(navMeshAssetPath);
                    }
                    AssetDatabase.CreateAsset(navMesh.navMeshData, navMeshAssetPath);
                    EditorUtility.SetDirty(navMesh);
                }
            }

            // Wire audio and dependencies
            MiniTopDownShooterSetupCommands.WireAudioInScene();
            MiniTopDownShooterSetupCommands.FixCommonSetupIssues();
            GameCompositionRoot compRoot = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (compRoot != null)
            {
                compRoot.ComposeDependencies();
                EditorUtility.SetDirty(compRoot);
            }

            string fullPath = Path.GetFullPath(scenePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            EditorSceneManager.MarkSceneDirty(newScene);
            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[WeaponAudioPipeline] Saved new text scene to {scenePath}: {saved}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ConfigureAllWeaponPrefabs()
        {
            AudioMixer mixer = MiniTopDownShooterSetupCommands.EnsureAudioMixer();
            if (mixer == null)
            {
                Debug.LogError("[WeaponAudioPipeline] AudioMixer could not be loaded or created.");
                return;
            }

            AudioMixerGroup sfxGroup = null;
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

            if (sfxGroup == null)
            {
                Debug.LogError("[WeaponAudioPipeline] SFX group not found in GameAudioMixer.");
                return;
            }

            for (int i = 0; i < WeaponConfigs.Length; i++)
            {
                string prefabPath = WeaponConfigs[i].PrefabPath;
                string clipPath = WeaponConfigs[i].ClipPath;

                if (!File.Exists(prefabPath))
                {
                    Debug.LogWarning($"[WeaponAudioPipeline] Prefab not found at {prefabPath}");
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                {
                    Debug.LogError($"[WeaponAudioPipeline] Clip not found at {clipPath}");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    Gun gun = root.GetComponent<Gun>();
                    if (gun == null)
                    {
                        Debug.LogError($"[WeaponAudioPipeline] No Gun component on root of {prefabPath}");
                        continue;
                    }

                    AudioSource source = root.GetComponent<AudioSource>();
                    if (source == null)
                    {
                        source = root.AddComponent<AudioSource>();
                    }

                    source.playOnAwake = false;
                    source.loop = false;
                    source.spatialBlend = 0f;
                    source.outputAudioMixerGroup = sfxGroup;

                    GunAudio gunAudio = root.GetComponent<GunAudio>();
                    if (gunAudio == null)
                    {
                        gunAudio = root.AddComponent<GunAudio>();
                    }

                    SerializedObject serializedAudio = new SerializedObject(gunAudio);
                    SerializedProperty gunProp = serializedAudio.FindProperty("_gunRef");
                    SerializedProperty srcProp = serializedAudio.FindProperty("_source");
                    SerializedProperty clipProp = serializedAudio.FindProperty("_shotClip");

                    gunProp.objectReferenceValue = gun;
                    srcProp.objectReferenceValue = source;
                    clipProp.objectReferenceValue = clip;

                    serializedAudio.ApplyModifiedPropertiesWithoutUndo();

                    bool valid = gunAudio.ValidateReferences();
                    if (!valid)
                    {
                        Debug.LogWarning($"[WeaponAudioPipeline] Reference validation warning on {prefabPath}");
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log($"[WeaponAudioPipeline] Configured audio for prefab {prefabPath} with clip {clip.name} and SFX mixer.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        public static void ResaveAndCleanScenes()
        {
            for (int s = 0; s < ScenePaths.Length; s++)
            {
                string scenePath = ScenePaths[s];
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[WeaponAudioPipeline] Scene not found: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log($"[WeaponAudioPipeline] Processing scene: {scene.name}");

                // 1. Clean up stale standalone scene-level GunAudio objects (legacy fixed-gun audio)
                GunAudio[] sceneGunAudios = Object.FindObjectsByType<GunAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < sceneGunAudios.Length; i++)
                {
                    GunAudio ga = sceneGunAudios[i];
                    if (ga != null && ga.GetComponent<Gun>() == null && ga.GetComponentInParent<Gun>() == null)
                    {
                        Debug.Log($"[WeaponAudioPipeline] Removing stale standalone GunAudio object '{ga.gameObject.name}' from scene '{scene.name}'");
                        Object.DestroyImmediate(ga.gameObject);
                    }
                }

                // 2. Wire scene audio (including scene guns and SFX mixer routing)
                MiniTopDownShooterSetupCommands.WireAudioInScene();

                // 3. Compose dependencies
                GameCompositionRoot compRoot = Object.FindFirstObjectByType<GameCompositionRoot>(FindObjectsInactive.Include);
                if (compRoot != null)
                {
                    compRoot.ComposeDependencies();
                    EditorUtility.SetDirty(compRoot);
                }

                EditorSettings.serializationMode = SerializationMode.ForceText;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log($"[WeaponAudioPipeline] Saved scene '{scene.name}'.");
            }

            EditorSceneManager.SaveOpenScenes();
        }
    }
}
