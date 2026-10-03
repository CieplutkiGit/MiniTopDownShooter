using System;
using System.IO;
using System.Reflection;
using Game;
using Game.Flow;
using Game.Lobby;
using Game.Workshop;
using Game.Workshop.Presentation;
using Game.Workshop.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class LobbyAndWeaponEditBuilder
    {
        private const string HubScenePath = "Assets/Scenes/BaseHub.unity";
        private const string WeaponEditScenePath = "Assets/Scenes/WeaponEdit.unity";
        private const string CatalogAssetPath = "Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset";
        private const string RifleProfilePath = "Assets/_Project/Data/WeaponCustomization/Rifle/VisualProfile_Rifle.asset";
        private const string MissionPath = "Assets/_Project/Data/Missions/Mission_ArenaSweep.asset";

        private const string MatFloorPath = "Assets/_Project/Materials/Mat_Arena_Floor.mat";
        private const string MatFloorAccentPath = "Assets/_Project/Materials/Mat_Arena_FloorAccent.mat";
        private const string MatSpawnPadPath = "Assets/_Project/Materials/Mat_Arena_SpawnPad.mat";
        private const string MatAccentPath = "Assets/_Project/Materials/Mat_Weapon_Accent.mat";
        private const string MatMetalPath = "Assets/_Project/Materials/Mat_Weapon_Metal.mat";

        [MenuItem("Mini Top Down Shooter/Build Lobby and Weapon Edit Scenes")]
        public static void BuildAll()
        {
            BuildLobbyScene();
            BuildWeaponEditScene();
            EnsureScenesInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[LobbyAndWeaponEditBuilder] Both Lobby and WeaponEdit scenes built and verified successfully!");
        }

        // =========================================================================
        // 1. BUILD LOBBY SCENE (BaseHub.unity)
        // =========================================================================
        public static void BuildLobbyScene()
        {
            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);

            // 1. Remove all arena clutter, bastions, conduits, targets, barricades, world signs
            RemoveArenaClutter();

            var compRoot = UnityEngine.Object.FindFirstObjectByType<GameCompositionRoot>();
            var gameState = UnityEngine.Object.FindFirstObjectByType<GameStateController>();

            // 2. Setup Player in center facing camera
            PlayerController player = ConfigureLobbyPlayer();

            // 3. Setup sleek lobby pedestal under player
            CreateLobbyPedestal();

            // 4. Setup Camera facing player
            ConfigureLobbyCamera(player);

            // 5. Setup Lobby Studio Lighting
            ConfigureLobbyLighting();

            // 6. Setup Canvas and clean Mobile Lobby UI
            Canvas mainCanvas = EnsureMainCanvas();
            DeploymentTerminal terminal = EnsureDeploymentTerminal(mainCanvas.transform, gameState);
            LobbyUI lobbyUI = CreateLobbyUI(mainCanvas.transform, terminal, gameState, player);

            // 7. Setup headless FiringRangeTrigger for test compatibility
            var firingRange = EnsureFiringRangeTrigger(gameState);

            // 8. Configure HubSceneRoot
            ConfigureHubSceneRoot(compRoot, gameState, terminal, lobbyUI, firingRange);

            // 9. Save BaseHub
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, HubScenePath);
            Debug.Log("[LobbyAndWeaponEditBuilder] BaseHub (Lobby) built successfully.");
        }

        private static void RemoveArenaClutter()
        {
            string[] objectNamesToDestroy = new string[]
            {
                "ArenaEnvironment", "Bastion_NE", "Bastion_NW", "Bastion_SE", "Bastion_SW",
                "Conduit_N", "Conduit_E", "Conduit_S", "Conduit_W",
                "SpawnPad_East", "SpawnPad_West", "SpawnPad_South",
                "Wall_North", "Wall_East", "Wall_South", "Wall_West",
                "Wall_North_Trim", "Wall_East_Trim", "Wall_South_Trim", "Wall_West_Trim",
                "Cover_NE", "Cover_NW", "Cover_SE", "Cover_SW",
                "Pylon_East", "Pylon_West", "LowBarricade_1",
                "FiringRange", "Target_1", "Target_2", "Target_3",
                "Sign_Bench", "Sign_Terminal", "Sign_Range",
                "BenchPrompt", "TerminalPrompt", "WorkshopBench"
            };

            foreach (string name in objectNamesToDestroy)
            {
                var go = GameObject.Find(name);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            // Remove any 3D TextMeshPro world signs
            foreach (var tmp in UnityEngine.Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(tmp.gameObject);
            }

            // Disable old combat / overlay panels
            DisablePanelByName("HUD");
            DisablePanelByName("MainMenu");
            DisablePanelByName("WorkshopPanel");
        }

        private static void DisablePanelByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var canvasGroup = go.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
                go.SetActive(false);
            }
        }

        private static PlayerController ConfigureLobbyPlayer()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = Vector3.zero;
                // Face towards camera (direction 0, 0, -1)
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                // Add or ensure LobbyHeroShowcase
                var showcase = player.GetComponent<LobbyHeroShowcase>();
                if (showcase == null) showcase = player.gameObject.AddComponent<LobbyHeroShowcase>();

                // Freeze Rigidbody physics in lobby so player never falls
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var soRb = new SerializedObject(rb);
                    var useGravProp = soRb.FindProperty("m_UseGravity");
                    if (useGravProp != null) useGravProp.boolValue = false;
                    var isKinProp = soRb.FindProperty("m_IsKinematic");
                    if (isKinProp != null) isKinProp.boolValue = true;
                    var constrProp = soRb.FindProperty("m_Constraints");
                    if (constrProp != null) constrProp.intValue = (int)RigidbodyConstraints.FreezeAll;
                    soRb.ApplyModifiedPropertiesWithoutUndo();
                }

                // Configure loadout with all 5 weapons
                var loadout = player.GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    Transform mount = loadout.WeaponMount;
                    string[] weaponPrefabPaths = new string[]
                    {
                        "Assets/_Project/Weapons/Gun_Rifle.prefab",
                        "Assets/_Project/Weapons/Gun_Shotgun.prefab",
                        "Assets/_Project/Weapons/Gun_SMG.prefab",
                        "Assets/_Project/Weapons/Gun_Pistol.prefab",
                        "Assets/_Project/Weapons/Gun_Launcher.prefab"
                    };

                    var soLoadout = new SerializedObject(loadout);
                    var weaponsProp = soLoadout.FindProperty("_weapons");
                    var existingGuns = new System.Collections.Generic.List<Gun>();
                    for (int i = 0; i < weaponsProp.arraySize; i++)
                    {
                        var gun = weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue as Gun;
                        if (gun != null) existingGuns.Add(gun);
                    }

                    foreach (var path in weaponPrefabPaths)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;
                        var prefabGun = prefab.GetComponent<Gun>();
                        if (prefabGun == null) continue;

                        bool alreadyPresent = false;
                        foreach (var eg in existingGuns)
                        {
                            if (eg != null && eg.WeaponId == prefabGun.WeaponId)
                            {
                                alreadyPresent = true;
                                break;
                            }
                        }

                        if (!alreadyPresent)
                        {
                            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mount);
                            instance.name = prefab.name;
                            instance.transform.localPosition = Vector3.zero;
                            instance.transform.localRotation = Quaternion.identity;
                            var gunComp = instance.GetComponent<Gun>();
                            existingGuns.Add(gunComp);
                            instance.SetActive(false);
                        }
                    }

                    weaponsProp.arraySize = existingGuns.Count;
                    for (int i = 0; i < existingGuns.Count; i++)
                    {
                        weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue = existingGuns[i];
                    }
                    soLoadout.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            return player;
        }

        private static void CreateLobbyPedestal()
        {
            var existingPedestal = GameObject.Find("LobbyPedestal");
            if (existingPedestal != null) UnityEngine.Object.DestroyImmediate(existingPedestal);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "LobbyPedestal";
            pedestal.transform.position = new Vector3(0f, -0.05f, 0f);
            pedestal.transform.localScale = new Vector3(3.2f, 0.1f, 3.2f);

            var matFloor = AssetDatabase.LoadAssetAtPath<Material>(MatFloorPath);
            var matAccent = AssetDatabase.LoadAssetAtPath<Material>(MatAccentPath);

            if (matFloor != null) pedestal.GetComponent<Renderer>().sharedMaterial = matFloor;

            // Outer glowing ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AccentRing";
            ring.transform.SetParent(pedestal.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(1.05f, 0.05f, 1.05f);
            if (matAccent != null) ring.GetComponent<Renderer>().sharedMaterial = matAccent;

            // Ensure solid floor collider under pedestal so nothing ever falls
            var col1 = pedestal.GetComponent<Collider>();
            if (col1 == null)
            {
                var box = pedestal.AddComponent<BoxCollider>();
                box.size = new Vector3(3.2f, 0.1f, 3.2f);
                box.center = Vector3.zero;
            }
            var col2 = ring.GetComponent<Collider>();
            if (col2 != null) UnityEngine.Object.DestroyImmediate(col2);
        }

        private static void ConfigureLobbyCamera(PlayerController player)
        {
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.25f, -2.6f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                cam.fieldOfView = 44f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);

                // Disable or configure CameraFollow so it stays at hero view
                var follow = cam.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    var so = new SerializedObject(follow);
                    var offProp = so.FindProperty("_offset");
                    if (offProp != null) offProp.vector3Value = new Vector3(0f, 1.25f, -2.6f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    follow.enabled = false; // Hero is fixed at (0,0,0) in lobby
                }
            }
        }

        private static void ConfigureLobbyLighting()
        {
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                var light = lightGo.GetComponent<Light>();
                if (light != null)
                {
                    light.type = LightType.Directional;
                    light.transform.rotation = Quaternion.Euler(30f, -30f, 0f);
                    light.color = new Color(1.0f, 0.98f, 0.94f, 1f);
                    light.intensity = 1.3f;
                }
            }

            // Fill light
            var fillLightGo = GameObject.Find("LobbyFillLight");
            if (fillLightGo == null)
            {
                fillLightGo = new GameObject("LobbyFillLight");
                var fill = fillLightGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
                fill.color = new Color(0.45f, 0.65f, 0.9f, 1f);
                fill.intensity = 0.6f;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.15f, 0.20f, 1f);
        }

        private static DeploymentTerminal EnsureDeploymentTerminal(Transform canvasTransform, GameStateController gameState)
        {
            var termObj = GameObject.Find("DeploymentTerminal");
            DeploymentTerminal terminal = null;
            if (termObj != null)
            {
                terminal = termObj.GetComponent<DeploymentTerminal>();
            }

            if (terminal == null)
            {
                termObj = new GameObject("DeploymentTerminal");
                terminal = termObj.AddComponent<DeploymentTerminal>();
            }

            // Keep terminal headless without blocking colliders
            var col = termObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var briefingPanel = GameObject.Find("DeploymentBriefingPanel");
            if (briefingPanel != null) briefingPanel.SetActive(false);

            var missionDef = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionPath);
            if (missionDef == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:MissionDefinition");
                if (guids.Length > 0)
                    missionDef = AssetDatabase.LoadAssetAtPath<MissionDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            var soTerm = new SerializedObject(terminal);
            SetRef(soTerm, "_missionDefinition", missionDef);
            soTerm.ApplyModifiedPropertiesWithoutUndo();

            return terminal;
        }

        private static LobbyUI CreateLobbyUI(Transform canvasTransform, DeploymentTerminal terminal, GameStateController gameState, PlayerController player)
        {
            var existingLobby = canvasTransform.Find("LobbyUI");
            if (existingLobby != null) UnityEngine.Object.DestroyImmediate(existingLobby.gameObject);

            var lobbyObj = new GameObject("LobbyUI", typeof(RectTransform), typeof(SafeAreaFitter));
            lobbyObj.transform.SetParent(canvasTransform, false);
            var lobbyRt = lobbyObj.GetComponent<RectTransform>();
            lobbyRt.anchorMin = Vector2.zero;
            lobbyRt.anchorMax = Vector2.one;
            lobbyRt.sizeDelta = Vector2.zero;

            var lobbyUI = lobbyObj.AddComponent<LobbyUI>();

            // --- Top Bar ---
            var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(lobbyObj.transform, false);
            var topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.sizeDelta = new Vector2(0f, 90f);
            topBar.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.85f);

            // Profile info (Left)
            var profileBox = new GameObject("ProfileBox", typeof(RectTransform));
            profileBox.transform.SetParent(topBar.transform, false);
            var profileRt = profileBox.GetComponent<RectTransform>();
            profileRt.anchorMin = new Vector2(0f, 0.5f);
            profileRt.anchorMax = new Vector2(0f, 0.5f);
            profileRt.pivot = new Vector2(0f, 0.5f);
            profileRt.anchoredPosition = new Vector2(30f, 0f);
            profileRt.sizeDelta = new Vector2(300f, 70f);

            var nameText = CreateTMPText(profileBox.transform, "PlayerName", "COMMANDER", 22, FontStyles.Bold, new Vector2(0f, 14f), new Vector2(280f, 30f), Color.white, TextAlignmentOptions.Left);
            var levelText = CreateTMPText(profileBox.transform, "PlayerLevel", "LV. 5 VETERAN", 14, FontStyles.Normal, new Vector2(0f, -14f), new Vector2(280f, 25f), new Color(0.0f, 0.85f, 0.8f, 1f), TextAlignmentOptions.Left);

            // Currencies & Best Score (Center-Right)
            var statsBox = new GameObject("StatsBox", typeof(RectTransform));
            statsBox.transform.SetParent(topBar.transform, false);
            var statsRt = statsBox.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(1f, 0.5f);
            statsRt.anchorMax = new Vector2(1f, 0.5f);
            statsRt.pivot = new Vector2(1f, 0.5f);
            statsRt.anchoredPosition = new Vector2(-120f, 0f);
            statsRt.sizeDelta = new Vector2(380f, 70f);

            var highScoreText = CreateTMPText(statsBox.transform, "HighScore", "BEST: 14,500", 16, FontStyles.Bold, new Vector2(-100f, 14f), new Vector2(220f, 28f), new Color(1f, 0.8f, 0.2f, 1f), TextAlignmentOptions.Right);
            var creditsText = CreateTMPText(statsBox.transform, "Credits", "SCRAP: 2,500", 14, FontStyles.Normal, new Vector2(-100f, -14f), new Vector2(220f, 25f), new Color(0.8f, 0.85f, 0.9f, 1f), TextAlignmentOptions.Right);

            // Settings button (Far Right)
            var settingsBtnObj = CreateStyledButton(topBar.transform, "SettingsBtn", "⚙", new Vector2(60f, 60f), new Vector2(-30f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Color(0.18f, 0.22f, 0.28f, 1f));

            // --- Center Equipped Weapon Badge ---
            var weaponBadge = new GameObject("WeaponBadge", typeof(RectTransform), typeof(Image));
            weaponBadge.transform.SetParent(lobbyObj.transform, false);
            var badgeRt = weaponBadge.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0f);
            badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0f);
            badgeRt.anchoredPosition = new Vector2(0f, 160f);
            badgeRt.sizeDelta = new Vector2(360f, 50f);
            weaponBadge.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.15f, 0.85f);

            var weaponNameText = CreateTMPText(weaponBadge.transform, "EquippedWeaponText", "EQUIPPED: ASSAULT RIFLE", 16, FontStyles.Bold, Vector2.zero, new Vector2(340f, 40f), Color.white, TextAlignmentOptions.Center);

            // Weapon quick switch bar
            var switchBar = new GameObject("QuickSwitchContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            switchBar.transform.SetParent(lobbyObj.transform, false);
            var switchRt = switchBar.GetComponent<RectTransform>();
            switchRt.anchorMin = new Vector2(0.5f, 0f);
            switchRt.anchorMax = new Vector2(0.5f, 0f);
            switchRt.pivot = new Vector2(0.5f, 0f);
            switchRt.anchoredPosition = new Vector2(0f, 110f);
            switchRt.sizeDelta = new Vector2(360f, 45f);

            var switchLayout = switchBar.GetComponent<HorizontalLayoutGroup>();
            switchLayout.childAlignment = TextAnchor.MiddleCenter;
            switchLayout.spacing = 10f;
            switchLayout.childControlWidth = false;
            switchLayout.childControlHeight = false;

            // --- Bottom Action Bar ---
            var bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(lobbyObj.transform, false);
            var bottomRt = bottomBar.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0f, 0f);
            bottomRt.anchorMax = new Vector2(1f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(0f, 110f);

            // WORKSHOP BUTTON (Left-Center)
            var workshopBtnObj = CreateStyledButton(bottomBar.transform, "WorkshopButton", "WEAPONS\n<size=11><color=#8B949E>CUSTOMIZE</color></size>", new Vector2(180f, 68f), new Vector2(-150f, 25f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.16f, 0.20f, 0.27f, 1f));

            // DEPLOY BUTTON (Right-Center) - Vibrant Primary CTA!
            var deployBtnObj = CreateStyledButton(bottomBar.transform, "LobbyDeployButton", "<size=22><b>DEPLOY</b></size>\n<size=11><color=#001A18>ARENA SWEEP</color></size>", new Vector2(250f, 76f), new Vector2(90f, 25f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.0f, 0.85f, 0.75f, 1f));
            var deployTmp = deployBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (deployTmp != null) deployTmp.color = new Color(0.04f, 0.1f, 0.09f, 1f);

            // Wire SerializedObject properties
            var soLobby = new SerializedObject(lobbyUI);
            SetRef(soLobby, "_playerNameText", nameText);
            SetRef(soLobby, "_playerLevelText", levelText);
            SetRef(soLobby, "_highScoreText", highScoreText);
            SetRef(soLobby, "_creditsText", creditsText);
            SetRef(soLobby, "_settingsButton", settingsBtnObj.GetComponent<Button>());
            SetRef(soLobby, "_equippedWeaponNameText", weaponNameText);
            SetRef(soLobby, "_weaponQuickSwitchContainer", switchRt);
            SetRef(soLobby, "_deployButton", deployBtnObj.GetComponent<Button>());
            SetRef(soLobby, "_workshopButton", workshopBtnObj.GetComponent<Button>());
            SetRef(soLobby, "_deploymentTerminal", terminal);

            var settingsUI = UnityEngine.Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            SetRef(soLobby, "_settingsUI", settingsUI);
            soLobby.ApplyModifiedPropertiesWithoutUndo();

            return lobbyUI;
        }

        private static void ConfigureHubSceneRoot(GameCompositionRoot compRoot, GameStateController gameState,
            DeploymentTerminal terminal, LobbyUI lobbyUI, WorkshopFiringRangeTrigger firingRange)
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
            SetRef(so, "_deploymentTerminal", terminal);
            SetRef(so, "_lobbyUI", lobbyUI);
            var titleProp = so.FindProperty("_showTitleOnEntry");
            if (titleProp != null) titleProp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (compRoot != null)
            {
                var soComp = new SerializedObject(compRoot);
                SetRef(soComp, "_workshopFiringRangeTrigger", firingRange);
                soComp.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static WorkshopFiringRangeTrigger EnsureFiringRangeTrigger(GameStateController gameState)
        {
            var rangeObj = GameObject.Find("WorkshopFiringRangeTrigger");
            WorkshopFiringRangeTrigger trigger = null;
            if (rangeObj != null)
            {
                trigger = rangeObj.GetComponent<WorkshopFiringRangeTrigger>();
            }

            if (trigger == null)
            {
                rangeObj = new GameObject("WorkshopFiringRangeTrigger");
                trigger = rangeObj.AddComponent<WorkshopFiringRangeTrigger>();
            }

            trigger.Initialize(gameState);
            return trigger;
        }

        // =========================================================================
        // 2. BUILD WEAPON EDIT SCENE (WeaponEdit.unity)
        // =========================================================================
        public static void BuildWeaponEditScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & Lighting
            Camera cam = CreateWeaponEditCamera();
            CreateWeaponEditLighting();

            // 2. Canvas & EventSystem
            Canvas canvas = EnsureMainCanvas();
            EnsureEventSystem();

            // 3. 3D Weapon Preview Rig with Assembler & PinchRotateController
            (WeaponPreviewView previewView, WeaponPinchRotateController rotateController) = CreateWeaponEditRig(cam);

            // 4. Clean Mobile WeaponEdit UI
            WeaponEditUI editUI = CreateWeaponEditUI(canvas.transform, rotateController);

            // 5. GameCompositionRoot & WeaponEditSceneRoot
            CreateWeaponEditRoots(previewView, rotateController, editUI);

            // 6. Save Scene
            Directory.CreateDirectory(Path.GetDirectoryName(WeaponEditScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, WeaponEditScenePath);
            Debug.Log("[LobbyAndWeaponEditBuilder] WeaponEdit scene built successfully.");
        }

        private static Camera CreateWeaponEditCamera()
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0.1f, -1.8f);
            cam.transform.rotation = Quaternion.identity;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            return cam;
        }

        private static void CreateWeaponEditLighting()
        {
            var keyLight = new GameObject("KeyLight", typeof(Light));
            var light1 = keyLight.GetComponent<Light>();
            light1.type = LightType.Directional;
            light1.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            light1.color = new Color(1.0f, 0.98f, 0.95f, 1f);
            light1.intensity = 1.3f;

            var rimLight = new GameObject("RimLight", typeof(Light));
            var light2 = rimLight.GetComponent<Light>();
            light2.type = LightType.Directional;
            light2.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
            light2.color = new Color(0.0f, 0.8f, 0.85f, 1f);
            light2.intensity = 0.8f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f, 1f);
        }

        private static (WeaponPreviewView, WeaponPinchRotateController) CreateWeaponEditRig(Camera cam)
        {
            var rigRoot = new GameObject("WeaponPreviewRig");
            rigRoot.transform.position = Vector3.zero;

            var turntable = new GameObject("InspectionTurntable");
            turntable.transform.SetParent(rigRoot.transform, false);

            var mount = new GameObject("WeaponMount");
            mount.transform.SetParent(turntable.transform, false);

            var previewView = mount.AddComponent<WeaponPreviewView>();
            var assembler = mount.AddComponent<WeaponModelAssembler>();
            previewView.Assembler = assembler;
            previewView.PreviewCamera = cam;
            previewView.InspectionRigRoot = turntable.transform;
            previewView.AutoRotateTurntable = false;

            var rifleProfile = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(RifleProfilePath);
            if (rifleProfile != null) previewView.VisualProfile = rifleProfile;

            // Interactive Pinch & Rotate controller
            var rotateController = turntable.AddComponent<WeaponPinchRotateController>();
            rotateController.TargetTransform = turntable.transform;
            rotateController.InspectionCamera = cam;

            return (previewView, rotateController);
        }

        private static WeaponEditUI CreateWeaponEditUI(Transform canvasTransform, WeaponPinchRotateController rotateController)
        {
            var uiRoot = new GameObject("WeaponEditUI", typeof(RectTransform), typeof(SafeAreaFitter));
            uiRoot.transform.SetParent(canvasTransform, false);
            var rootRt = uiRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            var editUI = uiRoot.AddComponent<WeaponEditUI>();

            // --- Top Header ---
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(uiRoot.transform, false);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 80f);
            header.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.9f);

            var backBtnObj = CreateStyledButton(header.transform, "BackButton", "◀ LOBBY", new Vector2(120f, 50f), new Vector2(20f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Color(0.16f, 0.20f, 0.26f, 1f));
            var titleText = CreateTMPText(header.transform, "WeaponTitle", "ASSAULT RIFLE", 22, FontStyles.Bold, Vector2.zero, new Vector2(400f, 50f), Color.white, TextAlignmentOptions.Center);
            var explodeBtnObj = CreateStyledButton(header.transform, "ExplodeButton", "EXPLODE", new Vector2(110f, 48f), new Vector2(-140f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Color(0.16f, 0.20f, 0.26f, 1f));
            var resetBtnObj = CreateStyledButton(header.transform, "ResetButton", "⟲ RESET", new Vector2(100f, 48f), new Vector2(-20f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Color(0.16f, 0.20f, 0.26f, 1f));

            // --- Slot Selector (Horizontal Tabs under header) ---
            var slotsBar = new GameObject("SlotSelector", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
            slotsBar.transform.SetParent(uiRoot.transform, false);
            var slotsRt = slotsBar.GetComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0f, 1f);
            slotsRt.anchorMax = new Vector2(1f, 1f);
            slotsRt.pivot = new Vector2(0.5f, 1f);
            slotsRt.anchoredPosition = new Vector2(0f, -85f);
            slotsRt.sizeDelta = new Vector2(0f, 52f);
            slotsBar.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.15f, 0.8f);

            var slotsLayout = slotsBar.GetComponent<HorizontalLayoutGroup>();
            slotsLayout.childAlignment = TextAnchor.MiddleCenter;
            slotsLayout.spacing = 8f;
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = false;

            // --- Part Selector (Bottom Panel) ---
            var bottomCard = new GameObject("BottomCustomizationPanel", typeof(RectTransform), typeof(Image));
            bottomCard.transform.SetParent(uiRoot.transform, false);
            var bottomRt = bottomCard.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0f, 0f);
            bottomRt.anchorMax = new Vector2(1f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(0f, 210f);
            bottomCard.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.92f);

            var slotLabel = CreateTMPText(bottomCard.transform, "SlotLabel", "SLOT: BARREL", 14, FontStyles.Bold, new Vector2(25f, 85f), new Vector2(250f, 30f), new Color(0.0f, 0.85f, 0.8f, 1f), TextAlignmentOptions.Left);
            slotLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            slotLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);

            var partsContainer = new GameObject("PartsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            partsContainer.transform.SetParent(bottomCard.transform, false);
            var partsRt = partsContainer.GetComponent<RectTransform>();
            partsRt.anchorMin = new Vector2(0f, 0f);
            partsRt.anchorMax = new Vector2(0.68f, 1f);
            partsRt.offsetMin = new Vector2(20f, 65f);
            partsRt.offsetMax = new Vector2(-10f, -40f);

            var partsLayout = partsContainer.GetComponent<HorizontalLayoutGroup>();
            partsLayout.childAlignment = TextAnchor.MiddleLeft;
            partsLayout.spacing = 10f;
            partsLayout.childControlWidth = false;
            partsLayout.childControlHeight = false;

            // --- Stats Card (Right docked in bottom panel) ---
            var statsCard = new GameObject("StatsPanel", typeof(RectTransform), typeof(Image));
            statsCard.transform.SetParent(bottomCard.transform, false);
            var statsRt = statsCard.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.70f, 0f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.offsetMin = new Vector2(10f, 15f);
            statsRt.offsetMax = new Vector2(-20f, -15f);
            statsCard.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f, 0.9f);

            var statsSummaryText = CreateTMPText(statsCard.transform, "StatsSummary", "DAMAGE: 20\nFIRE RATE: 10/s\nMAGAZINE: 30\nRELOAD: 1.8s", 13, FontStyles.Normal, Vector2.zero, Vector2.zero, Color.white, TextAlignmentOptions.TopLeft);
            statsSummaryText.rectTransform.anchorMin = Vector2.zero;
            statsSummaryText.rectTransform.anchorMax = Vector2.one;
            statsSummaryText.rectTransform.offsetMin = new Vector2(12f, 10f);
            statsSummaryText.rectTransform.offsetMax = new Vector2(-12f, -10f);

            // --- Action Buttons (Bottom Left) ---
            var discardBtnObj = CreateStyledButton(bottomCard.transform, "DiscardButton", "DISCARD", new Vector2(120f, 44f), new Vector2(20f, 15f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.18f, 0.22f, 0.28f, 1f));
            var applyBtnObj = CreateStyledButton(bottomCard.transform, "ApplyButton", "APPLY CHANGES", new Vector2(160f, 44f), new Vector2(150f, 15f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.0f, 0.85f, 0.75f, 1f));
            var applyTmp = applyBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (applyTmp != null) applyTmp.color = Color.black;

            // --- Error banner ---
            var errorRoot = new GameObject("ErrorRoot", typeof(RectTransform), typeof(Image));
            errorRoot.transform.SetParent(uiRoot.transform, false);
            var errorRt = errorRoot.GetComponent<RectTransform>();
            errorRt.anchorMin = new Vector2(0.5f, 0.5f);
            errorRt.anchorMax = new Vector2(0.5f, 0.5f);
            errorRt.sizeDelta = new Vector2(400f, 50f);
            errorRoot.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.9f);

            var errorText = CreateTMPText(errorRoot.transform, "ErrorText", "Invalid Build Configuration", 15, FontStyles.Bold, Vector2.zero, new Vector2(380f, 40f), Color.white, TextAlignmentOptions.Center);
            errorRoot.SetActive(false);

            // Wire SerializedObject properties
            var soEdit = new SerializedObject(editUI);
            SetRef(soEdit, "_backButton", backBtnObj.GetComponent<Button>());
            SetRef(soEdit, "_weaponTitleText", titleText);
            SetRef(soEdit, "_explodeButton", explodeBtnObj.GetComponent<Button>());
            SetRef(soEdit, "_explodeButtonText", explodeBtnObj.GetComponentInChildren<TMP_Text>());
            SetRef(soEdit, "_resetViewButton", resetBtnObj.GetComponent<Button>());
            SetRef(soEdit, "_slotsContainer", slotsRt);
            SetRef(soEdit, "_partsContainer", partsRt);
            SetRef(soEdit, "_currentSlotLabel", slotLabel);
            SetRef(soEdit, "_statsSummaryText", statsSummaryText);
            SetRef(soEdit, "_applyButton", applyBtnObj.GetComponent<Button>());
            SetRef(soEdit, "_discardButton", discardBtnObj.GetComponent<Button>());
            SetRef(soEdit, "_errorRoot", errorRoot);
            SetRef(soEdit, "_errorText", errorText);
            SetRef(soEdit, "_rotateController", rotateController);
            soEdit.ApplyModifiedPropertiesWithoutUndo();

            return editUI;
        }

        private static void CreateWeaponEditRoots(WeaponPreviewView previewView, WeaponPinchRotateController rotateController, WeaponEditUI editUI)
        {
            var compRootGo = new GameObject("GameCompositionRoot");
            var compRoot = compRootGo.AddComponent<GameCompositionRoot>();

            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogAssetPath);

            var soComp = new SerializedObject(compRoot);
            SetRef(soComp, "_weaponCatalog", catalog);

            string[] guids = AssetDatabase.FindAssets("t:WeaponVisualProfile");
            var profilesProp = soComp.FindProperty("_weaponVisualProfiles");
            if (profilesProp != null)
            {
                profilesProp.arraySize = guids.Length;
                for (int i = 0; i < guids.Length; i++)
                {
                    string pPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var prof = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(pPath);
                    profilesProp.GetArrayElementAtIndex(i).objectReferenceValue = prof;
                }
            }
            soComp.ApplyModifiedPropertiesWithoutUndo();

            var sceneRootGo = new GameObject("WeaponEditSceneRoot");
            var sceneRoot = sceneRootGo.AddComponent<WeaponEditSceneRoot>();

            var soScene = new SerializedObject(sceneRoot);
            SetRef(soScene, "_gameCompositionRoot", compRoot);
            SetRef(soScene, "_previewView", previewView);
            SetRef(soScene, "_rotateController", rotateController);
            SetRef(soScene, "_editUI", editUI);
            SetRef(soScene, "_catalogAsset", catalog);
            soScene.ApplyModifiedPropertiesWithoutUndo();
        }

        // =========================================================================
        // Helpers
        // =========================================================================
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

        private static void EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }

        private static void EnsureScenesInBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes;
            bool hubFound = false;
            bool editFound = false;

            foreach (var scene in currentScenes)
            {
                if (scene.path == HubScenePath) hubFound = true;
                if (scene.path == WeaponEditScenePath) editFound = true;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);
            if (!hubFound) list.Add(new EditorBuildSettingsScene(HubScenePath, true));
            if (!editFound) list.Add(new EditorBuildSettingsScene(WeaponEditScenePath, true));

            EditorBuildSettings.scenes = list.ToArray();
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float fontSize,
            FontStyles style, Vector2 pos, Vector2 size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            if (size != Vector2.zero) rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static GameObject CreateStyledButton(Transform parent, string name, string text, Vector2 size,
            Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = bgColor;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
            btn.colors = colors;

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(go.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 2f);
            textRt.offsetMax = new Vector2(-4f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return go;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object target)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = target;
        }
    }
}
