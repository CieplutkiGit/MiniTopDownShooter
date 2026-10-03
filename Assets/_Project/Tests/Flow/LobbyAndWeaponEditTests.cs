using System;
using System.IO;
using System.Reflection;
using Application;
using Application.Flow;
using Game;
using Game.Flow;
using Game.Lobby;
using Game.Workshop;
using Game.Workshop.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Flow
{
    [TestFixture]
    public class LobbyAndWeaponEditTests
    {
        [Test]
        public void SceneFlowController_HasWeaponEditSceneConfigured()
        {
            var go = new GameObject("SceneFlow_Test");
            var flow = go.AddComponent<SceneFlowController>();

            Assert.AreEqual("BaseHub", flow.HubSceneName);
            Assert.AreEqual("ArenaShowcase", flow.ArenaSceneName);
            Assert.AreEqual("WeaponEdit", flow.WeaponEditSceneName);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EditorBuildSettings_ContainsLobbyAndWeaponEditScenes()
        {
            bool hubFound = false;
            bool weaponEditFound = false;

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path.Contains("BaseHub.unity")) hubFound = true;
                if (scene.enabled && scene.path.Contains("WeaponEdit.unity")) weaponEditFound = true;
            }

            Assert.IsTrue(hubFound, "BaseHub.unity must be present and enabled in EditorBuildSettings.");
            Assert.IsTrue(weaponEditFound, "WeaponEdit.unity must be present and enabled in EditorBuildSettings.");
        }

        [Test]
        public void LobbyHeroShowcase_InitializesFacingCamera()
        {
            var go = new GameObject("Hero_Test");
            var showcase = go.AddComponent<LobbyHeroShowcase>();
            showcase.ResetFacing();

            // Default facing direction is Vector3(0, 0, -1), which corresponds to euler angles Y = 180
            Assert.AreEqual(180f, go.transform.rotation.eulerAngles.y, 1f);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void WeaponPinchRotateController_DefaultsAndResetView()
        {
            var rig = new GameObject("WeaponRig_Test");
            var camGo = new GameObject("Cam_Test");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;

            var controller = rig.AddComponent<WeaponPinchRotateController>();
            controller.TargetTransform = rig.transform;
            controller.InspectionCamera = cam;

            rig.transform.rotation = Quaternion.Euler(45f, 90f, 0f);
            cam.fieldOfView = 25f;

            controller.ResetView();

            Assert.AreEqual(38f, cam.fieldOfView, 0.1f, "ResetView must restore the current weapon-inspection default FOV.");
            Assert.AreEqual(10f, rig.transform.rotation.eulerAngles.x, 1f);

            UnityEngine.Object.DestroyImmediate(rig);
            UnityEngine.Object.DestroyImmediate(camGo);
        }

        [Test]
        public void LobbyHeroShowcase_InputSystemUpdate_DoesNotThrow()
        {
            var go = new GameObject("Hero_Test", typeof(PlayerMovement), typeof(PlayerShoot), typeof(PlayerRotation));
            var showcase = go.AddComponent<LobbyHeroShowcase>();
            InvokeLifecycle(showcase, "Awake");
            InvokeLifecycle(showcase, "OnEnable");

            Assert.DoesNotThrow(() =>
            {
                // Invoke Update via reflection or enable
                var updateMethod = typeof(LobbyHeroShowcase).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                updateMethod?.Invoke(showcase, null);
            });

            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void InvokeLifecycle(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected Unity lifecycle method {component.GetType().Name}.{methodName} to exist.");
            method.Invoke(component, null);
        }

        [Test]
        public void WeaponPinchRotateController_InputSystemUpdate_DoesNotThrow()
        {
            var rig = new GameObject("Rig_Test");
            var camGo = new GameObject("Cam_Test");
            var cam = camGo.AddComponent<Camera>();
            var controller = rig.AddComponent<WeaponPinchRotateController>();
            controller.TargetTransform = rig.transform;
            controller.InspectionCamera = cam;

            Assert.DoesNotThrow(() =>
            {
                var updateMethod = typeof(WeaponPinchRotateController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                updateMethod?.Invoke(controller, null);
            });

            UnityEngine.Object.DestroyImmediate(rig);
            UnityEngine.Object.DestroyImmediate(camGo);
        }

        [Test]
        public void LobbyUI_RefreshWeaponDisplay_ShowsEquippedWeaponName()
        {
            var uiGo = new GameObject("LobbyUI_Test");
            var lobbyUI = uiGo.AddComponent<LobbyUI>();

            var nameTextGo = new GameObject("EquippedText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            nameTextGo.transform.SetParent(uiGo.transform, false);
            var nameTmp = nameTextGo.GetComponent<TMPro.TextMeshProUGUI>();

            var so = new SerializedObject(lobbyUI);
            so.FindProperty("_equippedWeaponNameText").objectReferenceValue = nameTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            lobbyUI.RefreshAll();

            Assert.IsTrue(nameTmp.text.Contains("EQUIPPED:"), "Equipped weapon text must display EQUIPPED prefix.");

            UnityEngine.Object.DestroyImmediate(uiGo);
        }

        [Test]
        public void LobbyHeroShowcase_FreezesRigidbodyAndDisablesGravity()
        {
            var go = new GameObject("Hero_Test", typeof(Rigidbody), typeof(PlayerMovement), typeof(PlayerRotation));
            var rb = go.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            var showcase = go.AddComponent<LobbyHeroShowcase>();
            showcase.FreezeLobbyHero();

            Assert.IsTrue(rb.isKinematic, "Hero Rigidbody must be kinematic in lobby.");
            Assert.IsFalse(rb.useGravity, "Hero Rigidbody must have gravity disabled in lobby.");
            Assert.AreEqual(RigidbodyConstraints.FreezeAll, rb.constraints, "Hero Rigidbody must have FreezeAll constraints in lobby.");
            Assert.AreEqual(Vector3.zero, go.transform.position, "Hero position must be locked at origin.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void BaseHub_SceneVerification_HasCleanLobbyStructure()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/BaseHub.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            // 1. Verify no 3D world space text signs
            var worldTexts = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(0, worldTexts.Length, "BaseHub must not have any 3D world-space text floating in the room.");

            // 2. Verify pedestal exists and has solid collider
            var pedestal = GameObject.Find("LobbyPedestal");
            Assert.IsNotNull(pedestal, "BaseHub must have LobbyPedestal.");
            var pedestalCol = pedestal.GetComponent<Collider>();
            Assert.IsNotNull(pedestalCol, "LobbyPedestal must have a solid Collider so entities never fall.");

            // 3. Verify Player has LobbyHeroShowcase and kinematic Rigidbody without gravity
            var showcase = UnityEngine.Object.FindFirstObjectByType<LobbyHeroShowcase>();
            Assert.IsNotNull(showcase, "Player in BaseHub must have LobbyHeroShowcase.");
            var playerRb = showcase.GetComponent<Rigidbody>();
            Assert.IsNotNull(playerRb, "Player in BaseHub must have Rigidbody.");
            Assert.IsTrue(playerRb.isKinematic, "Player in BaseHub must have isKinematic = true.");
            Assert.IsFalse(playerRb.useGravity, "Player in BaseHub must have useGravity = false.");

            // 4. Verify LobbyUI exists
            var lobbyUI = UnityEngine.Object.FindFirstObjectByType<LobbyUI>();
            Assert.IsNotNull(lobbyUI, "BaseHub must have LobbyUI.");
        }

        [Test]
        public void WeaponEditUI_ExplodedViewToggle()
        {
            var uiGo = new GameObject("WeaponEditUI_Test");
            var editUI = uiGo.AddComponent<WeaponEditUI>();

            var btnGo = new GameObject("ExplodeBtn", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            btnGo.transform.SetParent(uiGo.transform, false);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "EXPLODE";

            var so = new SerializedObject(editUI);
            so.FindProperty("_explodeButton").objectReferenceValue = btnGo.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("_explodeButtonText").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            editUI.ToggleExploded();
            Assert.AreEqual("ASSEMBLE", tmp.text);

            editUI.ToggleExploded();
            Assert.AreEqual("EXPLODE", tmp.text);

            UnityEngine.Object.DestroyImmediate(uiGo);
        }

        [Test]
        public void WeaponEdit_SceneVerification_HasCleanStudioAndFullVisualProfiles()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WeaponEdit.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            var sceneRoot = UnityEngine.Object.FindFirstObjectByType<WeaponEditSceneRoot>();
            Assert.IsNotNull(sceneRoot, "WeaponEdit scene must have WeaponEditSceneRoot.");

            var rotateController = UnityEngine.Object.FindFirstObjectByType<WeaponPinchRotateController>();
            Assert.IsNotNull(rotateController, "WeaponEdit scene must have WeaponPinchRotateController.");

            var editUI = UnityEngine.Object.FindFirstObjectByType<WeaponEditUI>();
            Assert.IsNotNull(editUI, "WeaponEdit scene must have WeaponEditUI.");

            var compRoot = UnityEngine.Object.FindFirstObjectByType<GameCompositionRoot>();
            Assert.IsNotNull(compRoot, "WeaponEdit scene must have GameCompositionRoot.");
            Assert.IsNotNull(compRoot.WeaponCatalog, "GameCompositionRoot must have WeaponCatalog configured.");
            Assert.GreaterOrEqual(compRoot.WeaponVisualProfiles.Length, 5, "WeaponEdit scene must register all 5 weapon visual profiles.");
        }

        [Test]
        public void ArenaShowcase_SceneVerification_MainMenuIsInactive()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ArenaShowcase.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            var mainMenu = GameObject.Find("MainMenu");
            Assert.IsTrue(mainMenu == null || !mainMenu.activeSelf, "MainMenu panel in ArenaShowcase must be inactive so it does not block combat gameplay.");
        }

        [Test]
        public void StarterWeapon_Pistol_IsEquippedOnStart_WhenOtherWeaponsLocked()
        {
            var playerGo = new GameObject("PlayerTest");
            var player = playerGo.AddComponent<PlayerController>();
            var loadout = playerGo.AddComponent<WeaponLoadout>();

            var originalProfile = SaveManager.LoadProfile();
            try
            {
                // Setup profile where only Pistol is unlocked
                var profile = new UserProfileData();
                profile.ValidateAndMigrate();
                SaveManager.SaveProfile(profile);
                var service = new Game.Economy.UnityEconomyService();
                loadout.EconomyPolicy = service;

                // Load gun prefabs
                var pistolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Gun_Pistol.prefab");
                var riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Gun_Rifle.prefab");
                Assert.IsNotNull(pistolPrefab, "Gun_Pistol prefab must exist");
                Assert.IsNotNull(riflePrefab, "Gun_Rifle prefab must exist");

                Assert.AreEqual(Application.Weapons.WeaponWorkshopIds.Pistol, pistolPrefab.GetComponent<Gun>().WeaponId);
                Assert.AreEqual(Application.Weapons.WeaponWorkshopIds.Rifle, riflePrefab.GetComponent<Gun>().WeaponId);

                // Simulate pre-existing locked rifle in loadout (as in BaseHub / ArenaShowcase)
                var rifleInstance = UnityEngine.Object.Instantiate(riflePrefab, playerGo.transform).GetComponent<Gun>();
                loadout.AddWeapon(rifleInstance, false);

                var compGo = new GameObject("CompRoot");
                var compRoot = compGo.AddComponent<GameCompositionRoot>();
                var soComp = new SerializedObject(compRoot);
                soComp.FindProperty("_player").objectReferenceValue = player;
                var prefabsProp = soComp.FindProperty("_weaponPrefabs");
                prefabsProp.arraySize = 2;
                prefabsProp.GetArrayElementAtIndex(0).objectReferenceValue = pistolPrefab.GetComponent<Gun>();
                prefabsProp.GetArrayElementAtIndex(1).objectReferenceValue = riflePrefab.GetComponent<Gun>();
                soComp.ApplyModifiedPropertiesWithoutUndo();

                compRoot.EnsureOwnedWeapons();

                Assert.IsNotNull(loadout.ActiveGun, "ActiveGun must not be null on start");
                Assert.AreEqual(Application.Weapons.WeaponWorkshopIds.Pistol, loadout.ActiveGun.WeaponId, "Starter weapon must be Pistol");

                UnityEngine.Object.DestroyImmediate(compGo);
                UnityEngine.Object.DestroyImmediate(playerGo);
            }
            finally
            {
                SaveManager.SaveProfile(originalProfile);
            }
        }
    }
}
