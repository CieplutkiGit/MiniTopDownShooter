using System;
using System.Linq;
using Game.Workshop.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Flow.Editor
{
    [InitializeOnLoad]
    public static class ProductionLoopSetup
    {
        public const string MissionPath = "Assets/_Project/Data/Missions/Mission_ArenaSweep.asset";

        static ProductionLoopSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var entry = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
                if (entry != null && EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled)?.path == "Assets/Scenes/Boot.unity")
                    EditorSceneManager.playModeStartScene = entry;
            };
        }

        [MenuItem("Tools/Mini Top Down Shooter/Production/Build Full Game Loop")]
        public static void Build()
        {
            if (!UnityEngine.Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            MissionSetupCommands.CreateArenaSweepMission();
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionPath);
            if (mission == null || mission.WaveSet == null || mission.WaveSet.Count != 4 || mission.WaveSet.Waves[3].BossPrefab == null)
                throw new InvalidOperationException("Arena Sweep requires four authored waves and a final boss.");
            ProductionHubBuilder.Build();
            var hub = SceneManager.GetActiveScene();
            ConfigureShared(hub);
            ConfigureWorkshopChoices(hub);
            ProductionSettingsPresentation.Apply(hub);
            EditorSceneManager.SaveScene(hub, "Assets/Scenes/BaseHub.unity");
            ConfigureArena(mission);
            CreateBoot();
            MissionSetupCommands.ConfigureProductionBuildScenes();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[ProductionLoop] Boot, BaseHub, mission, arena and production entry configured.");
        }

        private static void ConfigureWorkshopChoices(Scene scene)
        {
            var controller = FindAll<Game.Workshop.WorkshopUIController>(scene).Single();
            var choices = controller.GetComponent<Game.Workshop.WorkshopPartsUI>();
            if (choices == null) choices = controller.gameObject.AddComponent<Game.Workshop.WorkshopPartsUI>();
            Set(choices, "_controller", controller);
            Set(choices, "_slotsRoot", FindAll<RectTransform>(scene).Single(rect => rect.name == "Slots"));
            Set(choices, "_partsRoot", FindAll<RectTransform>(scene).Single(rect => rect.name == "Parts"));
            Set(choices, "_loadout", FindAll<PlayerController>(scene).Single().GetComponent<WeaponLoadout>());
            var weapons = UI("Weapons", controller.Panel.transform, new Vector2(700, 60), new Vector2(0, 250));
            var layout = weapons.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Set(choices, "_weaponsRoot", weapons.GetComponent<RectTransform>());
        }

        private static void ConfigureShared(Scene scene)
        {
            foreach (var system in FindAll<EventSystem>(scene)) Object.DestroyImmediate(system.gameObject);
            var mobile = FindAll<MobileInputState>(scene).FirstOrDefault();
            if (mobile == null) mobile = new GameObject("Touch Input").AddComponent<MobileInputState>();
            var touch = mobile.GetComponent<MobileDemoControlsBootstrap>() ?? mobile.gameObject.AddComponent<MobileDemoControlsBootstrap>();
            var touchSO = new SerializedObject(touch);
            touchSO.FindProperty("_showOnDesktop").boolValue = false;
            touchSO.ApplyModifiedPropertiesWithoutUndo();
            foreach (var player in FindAll<PlayerController>(scene)) Set(player, "_mobileInput", mobile);
            foreach (var bench in FindAll<Game.Workshop.WorkshopBenchTrigger>(scene)) Set(bench, "_mobileInput", mobile);
            foreach (var root in FindAll<GameCompositionRoot>(scene))
            {
                Set(root, "_weaponCatalog", AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset"));
                var so = new SerializedObject(root);
                var profiles = AssetDatabase.FindAssets("t:WeaponVisualProfile").Select(id => AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
                var field = so.FindProperty("_weaponVisualProfiles");
                field.arraySize = profiles.Length;
                for (int i = 0; i < profiles.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var state = FindAll<GameStateController>(scene).FirstOrDefault();
            if (state != null)
            {
                var so = new SerializedObject(state);
                so.FindProperty("_autoStart").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (state.GetComponent<InputRearmController>() == null) state.gameObject.AddComponent<InputRearmController>();
                Set(state.GetComponent<InputRearmController>(), "_mobileInput", mobile);
                Set(touch, "_gameStateRef", state);
            }
            var pause = FindAll<PauseUI>(scene).FirstOrDefault();
            if (pause != null)
            {
                var button = new SerializedObject(pause).FindProperty("_menuButton").objectReferenceValue as Button;
                if (button != null)
                {
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.text = scene.name == "BaseHub" ? "Back to hub" : "Abandon mission";
                }
            }
        }

        private static void ConfigureArena(MissionDefinition mission)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaShowcase.unity", OpenSceneMode.Single);
            ConfigureShared(scene);
            var root = FindAll<GameCompositionRoot>(scene).Single();
            var state = FindAll<GameStateController>(scene).Single();
            var wave = FindAll<WaveController>(scene).Single();
            var spawner = FindAll<EnemySpawner>(scene).Single();
            Set(wave, "_waveSet", mission.WaveSet);
            var zones = FindAll<SpawnZone>(scene);
            foreach (var config in mission.WaveSet.Waves)
                foreach (var id in config.SpawnZoneIds)
                    if (!zones.Any(zone => zone.Id == id)) throw new InvalidOperationException("Missing arena spawn zone: " + id);
            var spawnSO = new SerializedObject(spawner);
            var zoneField = spawnSO.FindProperty("_spawnZones");
            zoneField.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++) zoneField.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
            spawnSO.FindProperty("_maxAlive").intValue = 20;
            spawnSO.FindProperty("_defaultPoolSize").intValue = 4;
            spawnSO.FindProperty("_maxPoolSize").intValue = 20;
            spawnSO.ApplyModifiedPropertiesWithoutUndo();
            foreach (var old in FindAll<MainMenuUI>(scene)) DisablePanel(old);
            foreach (var old in FindAll<VictoryUI>(scene)) DisablePanel(old);
            foreach (var old in FindAll<GameOverUI>(scene)) DisablePanel(old);
            foreach (var old in FindAll<MissionResultsUI>(scene))
            {
                if (old.gameObject == root.gameObject) Object.DestroyImmediate(old);
                else Object.DestroyImmediate(old.gameObject);
            }
            var canvas = new GameObject("Mission Results", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var results = canvas.AddComponent<MissionResultsUI>();
            var panel = UI("ResultsPanel", canvas.transform, new Vector2(640, 570), Vector2.zero);
            panel.AddComponent<Image>().color = new Color(.025f, .045f, .065f, .98f);
            Set(results, "_panel", panel);
            Set(results, "_outcomeText", Label("Outcome", panel.transform, "MISSION COMPLETE", new Vector2(0, 230), 34));
            string[] fields = { "_scoreText", "_timeText", "_killsText", "_wavesText", "_saveStatusText" };
            for (int i = 0; i < fields.Length; i++) Set(results, fields[i], Label(fields[i], panel.transform, "", new Vector2(0, 155 - i * 48), 24));
            Set(results, "_retrySaveButton", Button("RetrySave", panel.transform, "Retry save", new Vector2(-155, -185)));
            Set(results, "_returnButton", Button("Return", panel.transform, "Return to hub", new Vector2(155, -185)));
            var run = root.GetComponent<MissionRunController>() ?? root.gameObject.AddComponent<MissionRunController>();
            Set(run, "_sceneRoot", root);
            Set(run, "_waveController", wave);
            Set(run, "_gameStateController", state);
            Set(run, "_resultsUI", results);
            Set(run, "_missionDefinition", mission);
            var runSO = new SerializedObject(run);
            var effectField = runSO.FindProperty("_missionEffects");
            string[] effectPaths = { "Assets/_Project/EnemyDeathEffect.prefab", "Assets/_Project/EnemyHitEffect.prefab", "Assets/_Project/BulletImpact.prefab" };
            effectField.arraySize = effectPaths.Length;
            for (int i = 0; i < effectPaths.Length; i++)
                effectField.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(effectPaths[i]).GetComponent<ParticleSystem>();
            runSO.ApplyModifiedPropertiesWithoutUndo();
            ProductionSettingsPresentation.Apply(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void DisablePanel(MonoBehaviour component)
        {
            var field = new SerializedObject(component).FindProperty("_panel");
            var panel = field?.objectReferenceValue as GameObject;
            if (panel != null && panel != component.gameObject) panel.SetActive(false);
            component.enabled = false;
        }

        private static void CreateBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var services = new GameObject("App Services");
            var flow = services.AddComponent<SceneFlowController>();
            var app = services.AddComponent<AppCompositionRoot>();
            services.AddComponent<LoadingFeedbackUI>();
            var eventGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventGO.transform.SetParent(services.transform);
            eventGO.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Set(app, "_sceneFlow", flow);
            Set(app, "_bootEventSystem", eventGO.GetComponent<EventSystem>());
            Set(app, "_weaponCatalog", AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset"));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
        }

        public static T[] FindAll<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();

        public static void Set(Object owner, string name, Object value)
        {
            var so = new SerializedObject(owner);
            var field = so.FindProperty(name);
            if (field == null) throw new InvalidOperationException(owner.GetType().Name + " has no field " + name);
            field.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject UI(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return go;
        }

        private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, float size)
        {
            var go = UI(name, parent, new Vector2(580, 45), position);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.75f, .95f, 1f);
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(string name, Transform parent, string text, Vector2 position)
        {
            var go = UI(name, parent, new Vector2(280, 64), position);
            go.AddComponent<Image>().color = new Color(.08f, .3f, .4f);
            var button = go.AddComponent<Button>();
            Label("Label", go.transform, text, Vector2.zero, 23).rectTransform.sizeDelta = new Vector2(270, 60);
            return button;
        }
    }
}
