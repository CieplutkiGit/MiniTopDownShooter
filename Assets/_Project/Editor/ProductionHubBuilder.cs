using System;
using System.IO;
using System.Reflection;
using Game;
using Game.Flow;
using Game.Workshop;
using Game.Workshop.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Flow.Editor
{
    public static class ProductionHubBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/ArenaShowcase.unity";
        private const string HubScenePath = "Assets/Scenes/BaseHub.unity";
        private const string RenderTexturePath = "Assets/_Project/Data/Hub/WeaponPreview.renderTexture";
        private const string CatalogAssetPath = "Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset";
        private const string RifleProfilePath = "Assets/_Project/Data/WeaponCustomization/Rifle/VisualProfile_Rifle.asset";
        private const string MatWallPath = "Assets/_Project/Materials/Mat_Arena_Wall.mat";
        private const string MatSpawnPadPath = "Assets/_Project/Materials/Mat_Arena_SpawnPad.mat";
        private const string MatAccentPath = "Assets/_Project/Materials/Mat_Weapon_Accent.mat";

        [MenuItem("Mini Top Down Shooter/Build Production Hub")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

            var matWall = AssetDatabase.LoadAssetAtPath<Material>(MatWallPath);
            var matSpawnPad = AssetDatabase.LoadAssetAtPath<Material>(MatSpawnPadPath);
            var matAccent = AssetDatabase.LoadAssetAtPath<Material>(MatAccentPath);

            var compRoot = UnityEngine.Object.FindFirstObjectByType<GameCompositionRoot>();
            var gameState = UnityEngine.Object.FindFirstObjectByType<GameStateController>();

            // 1. Remove combat elements safely
            RemoveCombatComponents(compRoot);

            // 2. Disable Menu / Victory / GameOver panels
            DisableCombatPanels();

            // 3. Ensure player starts at (0, 1, 0)
            ConfigurePlayer(compRoot);

            // 4. Ensure MainCanvas with 1280x720 scaler
            Canvas mainCanvas = EnsureMainCanvas();

            // 5. Ensure RenderTexture asset
            RenderTexture previewRt = EnsureRenderTexture();

            // 6. Preview rig at y = 1000
            (WeaponPreviewView previewView, WeaponModelAssembler assembler) = CreatePreviewRig(previewRt);

            // 7. Workshop UI on always-active root with separate inactive panel
            (WorkshopUIController uiController, WorkshopRuntimeController runtimeController) =
                CreateWorkshopControllers(mainCanvas.transform, previewRt, previewView, gameState);

            // 8. Add colored bench at (-8, 0.5, 4)
            var benchTrigger = CreateBench(matAccent != null ? matAccent : matSpawnPad, mainCanvas.transform, gameState);

            // 9. Add firing range at (8, 0, 4)
            var rangeTrigger = CreateFiringRange(matWall, matSpawnPad, matAccent, gameState);

            // 10. Add deployment terminal at (0, 0, 12)
            var terminal = CreateDeploymentTerminal(matSpawnPad, mainCanvas.transform, gameState);

            // 11. Configure HubSceneRoot
            ConfigureHubSceneRoot(compRoot, gameState, runtimeController, terminal);

            // 12. Update GameCompositionRoot references
            if (compRoot != null)
            {
                var so = new SerializedObject(compRoot);
                SetRef(so, "_workshopUIController", uiController);
                SetRef(so, "_workshopBenchTrigger", benchTrigger);
                SetRef(so, "_workshopFiringRangeTrigger", rangeTrigger);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 13. Save scene (preserve GUID on rerun, NO DeleteAsset)
            Directory.CreateDirectory(Path.GetDirectoryName(HubScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, HubScenePath);
            EnsureInBuildSettings(HubScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProductionHubBuilder] Successfully built BaseHub scene.");
        }

        private static void RemoveCombatComponents(GameCompositionRoot compRoot)
        {
            var wave = UnityEngine.Object.FindFirstObjectByType<WaveController>();
            if (wave != null) UnityEngine.Object.DestroyImmediate(wave);

            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) UnityEngine.Object.DestroyImmediate(spawner);

            foreach (var zone in UnityEngine.Object.FindObjectsByType<SpawnZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(zone.gameObject);

            var missionRun = UnityEngine.Object.FindFirstObjectByType<MissionRunController>();
            if (missionRun != null)
            {
                if (compRoot != null && missionRun.gameObject == compRoot.gameObject)
                    UnityEngine.Object.DestroyImmediate(missionRun);
                else
                    UnityEngine.Object.DestroyImmediate(missionRun.gameObject);
            }

            var missionResults = UnityEngine.Object.FindFirstObjectByType<MissionResultsUI>();
            if (missionResults != null)
            {
                if (compRoot != null && missionResults.gameObject == compRoot.gameObject)
                    UnityEngine.Object.DestroyImmediate(missionResults);
                else
                    UnityEngine.Object.DestroyImmediate(missionResults.gameObject);
            }

            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        private static void DisableCombatPanels()
        {
            void DisableComponentPanel(Component comp, string fieldName)
            {
                if (comp == null) return;
                var field = comp.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                var panel = field?.GetValue(comp) as GameObject;
                if (panel != null) panel.SetActive(false);
                comp.gameObject.SetActive(false);
            }

            DisableComponentPanel(UnityEngine.Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include), "_panel");
            DisableComponentPanel(UnityEngine.Object.FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include), "_panel");
            DisableComponentPanel(UnityEngine.Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include), "_panel");
        }

        private static void ConfigurePlayer(GameCompositionRoot compRoot)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = new Vector3(0f, 1f, 0f);
                player.transform.rotation = Quaternion.identity;
            }

            if (compRoot != null)
            {
                var spawnField = typeof(GameCompositionRoot).GetField("_spawnPoint", BindingFlags.NonPublic | BindingFlags.Instance);
                var spawnTransform = spawnField?.GetValue(compRoot) as Transform;
                if (spawnTransform != null) spawnTransform.position = new Vector3(0f, 1f, 0f);
            }
        }

        private static Canvas EnsureMainCanvas()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RenderTexture EnsureRenderTexture()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RenderTexturePath));
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (rt == null)
            {
                rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
                AssetDatabase.CreateAsset(rt, RenderTexturePath);
            }
            return rt;
        }

        private static (WeaponPreviewView, WeaponModelAssembler) CreatePreviewRig(RenderTexture rt)
        {
            var existingRig = GameObject.Find("WeaponPreviewRig");
            if (existingRig != null) UnityEngine.Object.DestroyImmediate(existingRig);

            var rigRoot = new GameObject("WeaponPreviewRig");
            rigRoot.transform.position = new Vector3(0f, 1000f, 0f);

            var turntable = new GameObject("InspectionTurntable");
            turntable.transform.SetParent(rigRoot.transform, false);
            turntable.transform.localPosition = Vector3.zero;

            var assembler = turntable.AddComponent<WeaponModelAssembler>();
            var previewView = turntable.AddComponent<WeaponPreviewView>();

            var camObj = new GameObject("PreviewCamera");
            camObj.transform.SetParent(rigRoot.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 0f, -1.8f);
            camObj.transform.localRotation = Quaternion.identity;

            var cam = camObj.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);

            var listener = camObj.GetComponent<AudioListener>();
            if (listener != null) UnityEngine.Object.DestroyImmediate(listener);

            var rifleProfile = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(RifleProfilePath);
            previewView.VisualProfile = rifleProfile;
            previewView.PreviewCamera = cam;
            previewView.InspectionRigRoot = turntable.transform;
            previewView.Assembler = assembler;

            var so = new SerializedObject(previewView);
            SetRef(so, "_assembler", assembler);
            SetRef(so, "_visualProfile", rifleProfile);
            SetRef(so, "_previewCamera", cam);
            SetRef(so, "_inspectionRigRoot", turntable.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return (previewView, assembler);
        }

        private static (WorkshopUIController, WorkshopRuntimeController) CreateWorkshopControllers(
            Transform canvasTransform, RenderTexture rt, WeaponPreviewView previewView, GameStateController gameState)
        {
            var uiRoot = new GameObject("WorkshopRoot", typeof(RectTransform));
            uiRoot.transform.SetParent(canvasTransform, false);
            var uiRootRt = uiRoot.GetComponent<RectTransform>();
            uiRootRt.anchorMin = Vector2.zero;
            uiRootRt.anchorMax = Vector2.one;
            uiRootRt.sizeDelta = Vector2.zero;
            uiRoot.SetActive(true);

            var uiController = uiRoot.AddComponent<WorkshopUIController>();
            var runtimeController = uiRoot.AddComponent<WorkshopRuntimeController>();
            var combatAdapter = uiRoot.AddComponent<WeaponCombatAdapter>();

            var panel = new GameObject("WorkshopPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(uiRoot.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            panel.SetActive(false);

            var weaponName = CreateText(panel.transform, "WeaponNameText", "Rifle", 32, new Vector2(0, 310), new Vector2(400, 50));
            var statsSummary = CreateText(panel.transform, "StatsSummaryText", "Stats", 16, new Vector2(365, 50), new Vector2(300, 400));
            statsSummary.alignment = TextAlignmentOptions.TopLeft;

            var errorRoot = new GameObject("ErrorRoot", typeof(RectTransform));
            errorRoot.transform.SetParent(panel.transform, false);
            var errorMsg = CreateText(errorRoot.transform, "ErrorMessageText", "", 18, Vector2.zero, new Vector2(400, 40));
            errorMsg.color = Color.red;
            errorRoot.SetActive(false);

            var btnExplode = CreateButton(panel.transform, "ExplodeButton", "Exploded View", new Vector2(160, 60), new Vector2(-250, -300));
            var btnApply = CreateButton(panel.transform, "ApplyButton", "Apply", new Vector2(120, 60), new Vector2(-80, -300));
            var btnDiscard = CreateButton(panel.transform, "DiscardButton", "Discard", new Vector2(120, 60), new Vector2(60, -300));
            var btnExit = CreateButton(panel.transform, "ExitButton", "Exit", new Vector2(120, 60), new Vector2(200, -300));

            var slots = new GameObject("Slots", typeof(RectTransform), typeof(VerticalLayoutGroup));
            slots.transform.SetParent(panel.transform, false);
            var slotsRt = slots.GetComponent<RectTransform>();
            slotsRt.anchoredPosition = new Vector2(-420, 50); slotsRt.sizeDelta = new Vector2(220, 400);

            var parts = new GameObject("Parts", typeof(RectTransform), typeof(VerticalLayoutGroup));
            parts.transform.SetParent(panel.transform, false);
            var partsRt = parts.GetComponent<RectTransform>();
            partsRt.anchoredPosition = new Vector2(-180, 50); partsRt.sizeDelta = new Vector2(220, 400);

            var rawImgObj = new GameObject("RawImage", typeof(RectTransform), typeof(RawImage));
            rawImgObj.transform.SetParent(panel.transform, false);
            var rawImgRt = rawImgObj.GetComponent<RectTransform>();
            rawImgRt.anchoredPosition = new Vector2(75, 60); rawImgRt.sizeDelta = new Vector2(256, 256);
            rawImgObj.GetComponent<RawImage>().texture = rt;
            ProductionLoopSetup.Set(previewView, "_previewSurface", rawImgRt);

            var soUi = new SerializedObject(uiController);
            SetRef(soUi, "_panel", panel);
            SetRef(soUi, "_errorRoot", errorRoot);
            SetRef(soUi, "_weaponNameText", weaponName);
            SetRef(soUi, "_errorMessageText", errorMsg);
            SetRef(soUi, "_statsSummaryText", statsSummary);
            SetRef(soUi, "_explodeButton", btnExplode);
            SetRef(soUi, "_applyButton", btnApply);
            SetRef(soUi, "_discardButton", btnDiscard);
            SetRef(soUi, "_exitButton", btnExit);
            SetRef(soUi, "_gameStateRef", gameState);
            soUi.ApplyModifiedPropertiesWithoutUndo();

            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogAssetPath);
            var soRuntime = new SerializedObject(runtimeController);
            SetRef(soRuntime, "_catalogAsset", catalog);
            SetRef(soRuntime, "_uiController", uiController);
            SetRef(soRuntime, "_previewView", previewView);
            SetRef(soRuntime, "_combatAdapter", combatAdapter);
            soRuntime.ApplyModifiedPropertiesWithoutUndo();

            return (uiController, runtimeController);
        }

        private static WorkshopBenchTrigger CreateBench(Material mat, Transform canvasTransform, GameStateController gameState)
        {
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "WorkshopBench";
            bench.transform.position = new Vector3(-8f, 0.5f, 4f);
            bench.transform.localScale = new Vector3(2.5f, 1f, 1.5f);
            if (mat != null) bench.GetComponent<Renderer>().sharedMaterial = mat;

            var col = bench.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.5f, 2.5f, 3.5f);

            var benchTrigger = bench.AddComponent<WorkshopBenchTrigger>();
            var hubTrigger = bench.AddComponent<HubInteractionTrigger>();

            var mobileInput = UnityEngine.Object.FindFirstObjectByType<MobileInputState>(FindObjectsInactive.Include);

            var promptGo = new GameObject("BenchPrompt", typeof(RectTransform), typeof(Image));
            promptGo.transform.SetParent(canvasTransform, false);
            var promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchoredPosition = new Vector2(-300, -220); promptRt.sizeDelta = new Vector2(280, 80);
            promptGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.85f);
            CreateText(promptGo.transform, "Label", "Workshop Bench", 18, new Vector2(0, 15), new Vector2(260, 30));
            var btn = CreateButton(promptGo.transform, "Btn", "Customize [E]", new Vector2(220, 35), new Vector2(0, -18));
            promptGo.SetActive(false);

            btn.onClick.AddListener(() => hubTrigger.TryInteract());

            var soBench = new SerializedObject(benchTrigger);
            SetRef(soBench, "_gameStateRef", gameState);
            SetRef(soBench, "_promptRoot", promptGo);
            SetRef(soBench, "_mobileInput", mobileInput);
            soBench.ApplyModifiedPropertiesWithoutUndo();

            var soHub = new SerializedObject(hubTrigger);
            SetRef(soHub, "_promptRoot", promptGo);
            SetRef(soHub, "_promptButton", btn);
            SetRef(soHub, "_gameState", gameState);
            SetRef(soHub, "_workshopBench", benchTrigger);
            soHub.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldSign("Sign_Bench", new Vector3(-8f, 2.2f, 4f), "WORKSHOP BENCH\nCustomize Weapons");
            return benchTrigger;
        }

        private static WorkshopFiringRangeTrigger CreateFiringRange(Material matWall, Material matPad, Material matAccent, GameStateController gameState)
        {
            var rangeObj = new GameObject("FiringRange");
            rangeObj.transform.position = new Vector3(8f, 0f, 4f);

            var col = rangeObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(8f, 3.5f, 8f);
            col.center = new Vector3(0f, 1.5f, 2f);

            var trigger = rangeObj.AddComponent<WorkshopFiringRangeTrigger>();
            trigger.Initialize(gameState);

            var soTrig = new SerializedObject(trigger);
            SetRef(soTrig, "_gameStateRef", gameState);
            soTrig.ApplyModifiedPropertiesWithoutUndo();

            void CreateTarget(string name, Vector3 pos, Material mat)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = name;
                target.transform.SetParent(rangeObj.transform, false);
                target.transform.localPosition = pos;
                target.transform.localScale = new Vector3(0.8f, 1.5f, 0.8f);
                var rend = target.GetComponent<Renderer>();
                if (mat != null) rend.sharedMaterial = mat;

                var health = target.AddComponent<HealthComponent>();
                var rangeTarget = target.AddComponent<RangeTarget>();

                var soHealth = new SerializedObject(health);
                soHealth.FindProperty("_maxHealth").intValue = 100;
                soHealth.ApplyModifiedPropertiesWithoutUndo();

                var soTarget = new SerializedObject(rangeTarget);
                SetRef(soTarget, "_renderer", rend);
                soTarget.FindProperty("_maxHealth").intValue = 100;
                soTarget.ApplyModifiedPropertiesWithoutUndo();
            }

            CreateTarget("Target_1", new Vector3(0f, 1.5f, 0f), matAccent);
            CreateTarget("Target_2", new Vector3(-2f, 1.5f, 3f), matPad);
            CreateTarget("Target_3", new Vector3(2f, 1.5f, 3f), matWall);

            CreateWorldSign("Sign_Range", new Vector3(8f, 2.2f, 4f), "FIRING RANGE\nTest Weapon Systems");
            return trigger;
        }

        private static DeploymentTerminal CreateDeploymentTerminal(Material mat, Transform canvasTransform, GameStateController gameState)
        {
            var termObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            termObj.name = "DeploymentTerminal";
            termObj.transform.position = new Vector3(0f, 0f, 12f);
            termObj.transform.localScale = new Vector3(1.6f, 2f, 0.8f);
            if (mat != null) termObj.GetComponent<Renderer>().sharedMaterial = mat;

            var col = termObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.5f, 2.5f, 3.5f);
            col.center = new Vector3(0f, 1f, 0f);

            var terminal = termObj.AddComponent<DeploymentTerminal>();
            var trigger = termObj.AddComponent<HubInteractionTrigger>();

            var promptGo = new GameObject("TerminalPrompt", typeof(RectTransform), typeof(Image));
            promptGo.transform.SetParent(canvasTransform, false);
            var promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchoredPosition = new Vector2(0, -220); promptRt.sizeDelta = new Vector2(280, 80);
            promptGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.85f);
            CreateText(promptGo.transform, "Label", "Deployment Terminal", 18, new Vector2(0, 15), new Vector2(260, 30));
            var promptBtn = CreateButton(promptGo.transform, "Btn", "Deploy [E]", new Vector2(220, 35), new Vector2(0, -18));
            promptGo.SetActive(false);

            promptBtn.onClick.AddListener(() => trigger.TryInteract());

            var soTrig = new SerializedObject(trigger);
            SetRef(soTrig, "_promptRoot", promptGo);
            SetRef(soTrig, "_promptButton", promptBtn);
            SetRef(soTrig, "_gameState", gameState);
            SetRef(soTrig, "_deploymentTerminal", terminal);
            soTrig.ApplyModifiedPropertiesWithoutUndo();

            var briefingPanel = new GameObject("DeploymentBriefingPanel", typeof(RectTransform), typeof(Image));
            briefingPanel.transform.SetParent(canvasTransform, false);
            var panelRt = briefingPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(600, 420);
            briefingPanel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

            var titleText = CreateText(briefingPanel.transform, "MissionNameText", "Arena Sweep", 36, new Vector2(0, 150), new Vector2(500, 50));
            var loadoutText = CreateText(briefingPanel.transform, "LoadoutSummaryText", "Loadout: Rifle", 22, new Vector2(0, 70), new Vector2(500, 40));
            var warningText = CreateText(briefingPanel.transform, "PendingSaveWarningText", "", 18, new Vector2(0, 10), new Vector2(500, 30));
            warningText.color = Color.yellow;

            var deployBtn = CreateButton(briefingPanel.transform, "DeployButton", "DEPLOY", new Vector2(220, 60), new Vector2(-120, -140));
            var cancelBtn = CreateButton(briefingPanel.transform, "CancelButton", "CANCEL", new Vector2(220, 60), new Vector2(120, -140));
            briefingPanel.SetActive(false);

            var missionDef = AssetDatabase.LoadAssetAtPath<MissionDefinition>(ProductionLoopSetup.MissionPath);
            if (missionDef == null)
            {
                string[] missionGuids = AssetDatabase.FindAssets("t:MissionDefinition");
                if (missionGuids.Length > 0)
                    missionDef = AssetDatabase.LoadAssetAtPath<MissionDefinition>(AssetDatabase.GUIDToAssetPath(missionGuids[0]));
            }

            var soTerm = new SerializedObject(terminal);
            SetRef(soTerm, "_briefingPanel", briefingPanel);
            SetRef(soTerm, "_missionNameText", titleText);
            SetRef(soTerm, "_loadoutSummaryText", loadoutText);
            SetRef(soTerm, "_pendingSaveWarningText", warningText);
            SetRef(soTerm, "_deployButton", deployBtn);
            SetRef(soTerm, "_cancelButton", cancelBtn);
            SetRef(soTerm, "_missionDefinition", missionDef);
            soTerm.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldSign("Sign_Terminal", new Vector3(0f, 2.2f, 12f), "DEPLOYMENT TERMINAL\nEnter Mission");
            return terminal;
        }

        private static void ConfigureHubSceneRoot(GameCompositionRoot compRoot, GameStateController gameState,
            WorkshopRuntimeController workshopRuntime, DeploymentTerminal terminal)
        {
            var hub = UnityEngine.Object.FindFirstObjectByType<HubSceneRoot>();
            if (hub == null)
            {
                var hubGo = new GameObject("HubSceneRoot");
                hub = hubGo.AddComponent<HubSceneRoot>();
            }

            var so = new SerializedObject(hub);
            SetRef(so, "_gameStateController", gameState);
            SetRef(so, "_gameCompositionRoot", compRoot);
            SetRef(so, "_workshopRuntimeController", workshopRuntime);
            SetRef(so, "_deploymentTerminal", terminal);
            var titleProp = so.FindProperty("_showTitleOnEntry");
            if (titleProp == null)
            {
                throw new InvalidOperationException("HubSceneRoot has no serialized property '_showTitleOnEntry'");
            }
            titleProp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWorldSign(string name, Vector3 pos, string text)
        {
            var signObj = new GameObject(name);
            signObj.transform.position = pos;
            var tmp = signObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.2f, 0.28f, 0.42f, 0.95f);
            var btn = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.sizeDelta = Vector2.zero;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = Mathf.Min(size.y * 0.45f, 22f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object target)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                throw new InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized property '{propertyName}'");
            }
            prop.objectReferenceValue = target;
        }

        private static void EnsureInBuildSettings(string scenePath)
        {
            var currentScenes = EditorBuildSettings.scenes;
            foreach (var s in currentScenes)
            {
                if (string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            Array.Copy(currentScenes, newScenes, currentScenes.Length);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
