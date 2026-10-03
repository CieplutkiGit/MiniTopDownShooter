using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application;
using Application.Flow;
using Application.Weapons;
using Game;
using Game.Flow;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniTopDownShooter.PlayModeTests
{
    public class ProductionGameLoopTests
    {
        private string _saveDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveDirectory = Path.Combine(UnityEngine.Application.temporaryCachePath, "ProductionLoop_" + Guid.NewGuid().ToString("N"));
            SaveManager.CustomSaveDirectory = _saveDirectory;
            RunFinalizer.ResetForTesting();
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (AppCompositionRoot.Instance != null) Object.Destroy(AppCompositionRoot.Instance.gameObject);
            yield return null;
            Time.timeScale = 1f;
            SaveManager.CustomSaveDirectory = null;
            RunFinalizer.ResetForTesting();
            if (Directory.Exists(_saveDirectory)) Directory.Delete(_saveDirectory, true);
        }

        [UnityTest]
        public IEnumerator SavedUnownedUpgrades_AreRepairedBeforeCaching_AndMissionStarts()
        {
            // Reproduce an older workshop save loaded before production policy wiring.
            Game.Economy.UnityEconomyService.ResetInstance();
            WeaponLoadout.ActivePolicy = null;
            SaveManagerWeaponBuildStore.ActivePolicy = null;
            Application.Economy.EconomyPolicyProvider.ResetForTesting();
            var profile = new UserProfileData();
            profile.ValidateAndMigrate();
            profile.UnlockWeapon(WeaponWorkshopIds.Rifle);
            Assert.IsTrue(SaveManager.SaveProfile(profile));
            var legacyBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.drum" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.heavy" }
            });
            var workshop = new WeaponWorkshopSaveData();
            workshop.Builds.Add(new WeaponBuildDto(legacyBuild));
            Assert.IsTrue(SaveManager.SaveWorkshopData(workshop));

            // Boot replaces the initial test scene; keep the coroutine host alive.
            var runner = GameObject.Find("Code-based tests runner");
            if (runner != null) Object.DontDestroyOnLoad(runner);
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return WaitForHub();
            var app = AppCompositionRoot.Instance;
            var repaired = app.PlayerSession.GetCommittedBuild(WeaponWorkshopIds.Rifle);
            Assert.IsNotNull(repaired);
            foreach (var selection in repaired.Selections)
                Assert.IsTrue(Game.Economy.UnityEconomyService.Instance.IsPartUnlocked(
                    repaired.WeaponId, selection.Key, selection.Value), "Cached unowned part: " + selection.Value);
            Assert.IsFalse(Game.Economy.UnityEconomyService.Instance.IsWeaponUnlocked(WeaponWorkshopIds.Launcher));
            app.PlayerSession.SetEquippedWeapon(WeaponWorkshopIds.Rifle);
            Assert.IsTrue(app.FlowCoordinator.TryDeploy("Mission_ArenaSweep", app.PlayerSession.CreateDeploymentSnapshot()));
            app.SceneFlow.GoToArena();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ArenaShowcase" && !app.SceneFlow.IsTransitioning, 20);
            Assert.IsTrue(Find<MissionRunController>().IsMissionStarted);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, Find<PlayerController>().GetComponent<WeaponLoadout>().ActiveGun.WeaponId);
        }

        [UnityTest]
        public IEnumerator ThreeFullCycles_PreserveCustomizationAndFinalizeFourWavesOnce()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return WaitForHub();
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var state = Find<GameStateController>();
                var ui = Find<WorkshopUIController>();
                Assert.AreEqual(GameState.WorkshopRoaming, state.CurrentState);
                Assert.IsNotNull(Find<PlayerController>());
                Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
                state.EnterWorkshopEditing();
                yield return null;
                Assert.IsNotNull(ui.Session, "Workshop must bind a real session.");
                Assert.IsTrue(ui.Panel.activeInHierarchy);
                Assert.IsTrue(ui.AvailableSlots.Count > 0);
                string slot = ui.AvailableSlots.First();
                ui.SelectSlot(slot);
                Assert.IsTrue(ui.AvailableParts.Count > 0);
                var part = ui.AvailableParts.Last();
                ui.SelectPart(part.PartId);
                ui.ApplyButton.onClick.Invoke();
                Assert.AreEqual(part.PartId, ui.Session.CommittedBuild.GetPart(slot));
                var committed = ui.Session.CommittedBuild;
                ui.ExitButton.onClick.Invoke();
                Assert.AreEqual(GameState.WorkshopRoaming, state.CurrentState);
                var range = Find<WorkshopFiringRangeTrigger>();
                range.EnterRange();
                Assert.AreEqual(GameState.WorkshopFiringRange, state.CurrentState);
                var gun = Find<PlayerController>().GetComponent<WeaponLoadout>().ActiveGun;
                bool fired = false;
                gun.Fired += () => fired = true;
                gun.HandleTrigger(Vector3.forward, true, true);
                yield return null;
                Assert.IsTrue(fired, "Practice range must fire the committed weapon.");
                Assert.AreEqual(cycle, SaveManager.LoadProfile().TotalRuns, "Range practice must not finalize a mission.");
                range.ExitRange();
                var terminal = Find<DeploymentTerminal>();
                terminal.OpenBriefing();
                var deployButton = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Button>(true)).Single(button => button.name == "DeployButton");
                Assert.IsTrue(deployButton.interactable);
                deployButton.onClick.Invoke();
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ArenaShowcase" && !AppCompositionRoot.Instance.SceneFlow.IsTransitioning, 20);
                Assert.IsTrue(Find<MissionRunController>().IsMissionStarted);
                var missionPlayer = Find<PlayerController>();
                Assert.IsTrue(NavMesh.SamplePosition(missionPlayer.transform.position, out var playerNav, 3f, NavMesh.AllAreas), "Arena player has no reachable NavMesh: " + missionPlayer.transform.position);
                foreach (var zone in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<SpawnZone>(true)))
                {
                    Assert.IsTrue(NavMesh.SamplePosition(zone.transform.position, out var zoneNav, 4f, NavMesh.AllAreas), "Spawn zone has no NavMesh: " + zone.Id + " at " + zone.transform.position);
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(zoneNav.position, playerNav.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Spawn zone is disconnected: " + zone.Id);
                }
                var missionGun = Find<PlayerController>().GetComponent<WeaponLoadout>().ActiveGun;
                Assert.AreEqual(committed, missionGun.CurrentBuild);
                var app = AppCompositionRoot.Instance;
                Time.timeScale = 8f;
                float deadline = Time.realtimeSinceStartup + 35f;
                while (app.FlowCoordinator.FlowState != GameFlowState.MissionResults && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                        if (enemy.gameObject.scene == SceneManager.GetActiveScene()) enemy.GetComponent<HealthComponent>().TakeDamage(100000);
                    Find<PlayerController>().GetComponent<HealthComponent>().ResetHealth();
                    yield return null;
                }
                Assert.AreEqual(GameFlowState.MissionResults, app.FlowCoordinator.FlowState);
                Assert.AreEqual(RunOutcome.Victory, app.FlowCoordinator.LastFinalizedResult.Outcome);
                Assert.AreEqual(4, app.FlowCoordinator.LastFinalizedResult.WavesCleared);
                Assert.GreaterOrEqual(app.FlowCoordinator.LastFinalizedResult.TotalKills, 45);
                Assert.AreEqual(cycle + 1, SaveManager.LoadProfile().TotalRuns);
                Assert.IsTrue(RunFinalizer.CurrentSaveStatus == RunSaveStatus.Saved || RunFinalizer.CurrentSaveStatus == RunSaveStatus.AlreadySaved);
                var returnButton = Find<MissionResultsUI>().GetComponentsInChildren<Button>(true).Single(button => button.name == "Return");
                returnButton.onClick.Invoke();
                yield return WaitForHub();
                Assert.AreEqual(committed, Find<PlayerController>().GetComponent<WeaponLoadout>().ActiveGun.CurrentBuild);
            }
        }

        [UnityTest]
        public IEnumerator SettingsAndBackground_ClearInput_AndPausedAbandonReturnsToHub()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return WaitForHub();
            var state = Find<GameStateController>();
            var settings = Find<SettingsUI>();
            var mobile = Find<MobileInputState>();
            mobile.SetMove(Vector2.one);
            mobile.SetFireButton(true);
            mobile.Press(MobileInputAction.Reload);
            settings.Open();
            Assert.AreEqual(GameState.Paused, state.CurrentState);
            Assert.AreEqual(Vector2.zero, mobile.MoveDirection);
            Assert.IsFalse(mobile.FireHeld);
            Assert.IsFalse(mobile.ConsumeReloadPressed());
            settings.Close();
            Assert.AreEqual(GameState.WorkshopRoaming, state.CurrentState);
            var app = AppCompositionRoot.Instance;
            Assert.IsTrue(app.FlowCoordinator.TryDeploy("Mission_ArenaSweep", app.PlayerSession.CreateDeploymentSnapshot()));
            app.SceneFlow.GoToArena();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ArenaShowcase" && !app.SceneFlow.IsTransitioning, 20);
            state = Find<GameStateController>();
            mobile = Find<MobileInputState>();
            mobile.SetMove(Vector2.one);
            mobile.SetFireButton(true);
            Find<InputRearmController>().SendMessage("OnApplicationPause", true);
            Assert.AreEqual(GameState.Paused, state.CurrentState);
            Assert.AreEqual(Vector2.zero, mobile.MoveDirection);
            Assert.IsFalse(mobile.FireHeld);
            Find<InputRearmController>().SendMessage("OnApplicationPause", false);
            Assert.AreEqual(GameState.Paused, state.CurrentState);
            var pause = Find<PauseUI>();
            pause.SendMessage("HandleMenuClicked");
            yield return WaitForHub();
            Assert.AreEqual(RunOutcome.Abandoned, app.FlowCoordinator.LastFinalizedResult.Outcome);
            Assert.AreEqual(1, SaveManager.LoadProfile().TotalRuns);
        }

        private static T Find<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).First();

        private static IEnumerator WaitForHub() => WaitUntil(() => AppCompositionRoot.Instance != null && SceneManager.GetActiveScene().name == "BaseHub" && !AppCompositionRoot.Instance.SceneFlow.IsTransitioning && Find<HubSceneRoot>().IsInitialized, 20);

        private static IEnumerator WaitUntil(Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), "Production flow did not reach its destination.");
        }
    }
}
