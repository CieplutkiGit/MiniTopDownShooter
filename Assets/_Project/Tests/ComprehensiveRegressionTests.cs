using System.Collections.Generic;
using System.IO;
using System;
using Application;
using Core;
using Game;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Audio;

namespace MiniTopDownShooter.Tests
{
    public class ComprehensiveRegressionTests
    {
        private string _tempDir;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RegressionTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _spawnedObjects = new List<GameObject>();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            if (_spawnedObjects != null)
            {
                for (int i = 0; i < _spawnedObjects.Count; i++)
                {
                    if (_spawnedObjects[i] != null)
                    {
                        Object.DestroyImmediate(_spawnedObjects[i]);
                    }
                }
                _spawnedObjects.Clear();
            }

            SaveManager.CustomSaveDirectory = null;
            SaveManager.FileOps = new SaveManager.DefaultFileOperations();

            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch {}
            }
        }

        [Test]
        public void BurstFiring_ExactCountAndAmmoConsumption_CancelsSafely()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Burst,
                FireInterval = 0.5f,
                BurstCount = 3,
                BurstInterval = 0.1f,
                MagazineSize = 10,
                StartingReserveAmmo = 30,
                MaxReserveAmmo = 50,
                InfiniteAmmo = false
            };

            WeaponRuntime weapon = new WeaponRuntime(config);
            Assert.AreEqual(10, weapon.Ammo.InMagazine);
            Assert.AreEqual(0, weapon.BurstShotsRemaining);

            // 1. Start Burst
            bool started = weapon.StartBurst(1.0f);
            Assert.IsTrue(started);
            Assert.AreEqual(3, weapon.BurstShotsRemaining);

            // 2. Shot 1 fires immediately at t=1.0
            bool shot1 = weapon.TickBurst(1.0f);
            Assert.IsTrue(shot1);
            Assert.AreEqual(2, weapon.BurstShotsRemaining);
            Assert.AreEqual(9, weapon.Ammo.InMagazine);

            // 3. Tick too early at t=1.05
            bool early = weapon.TickBurst(1.05f);
            Assert.IsFalse(early);
            Assert.AreEqual(2, weapon.BurstShotsRemaining);
            Assert.AreEqual(9, weapon.Ammo.InMagazine);

            // 4. Shot 2 fires at t=1.1
            bool shot2 = weapon.TickBurst(1.1f);
            Assert.IsTrue(shot2);
            Assert.AreEqual(1, weapon.BurstShotsRemaining);
            Assert.AreEqual(8, weapon.Ammo.InMagazine);

            // 5. Cancel burst mid-sequence
            weapon.CancelBurst();
            Assert.AreEqual(0, weapon.BurstShotsRemaining);

            // 6. Tick at t=1.2 does not fire remaining shot
            bool shot3 = weapon.TickBurst(1.2f);
            Assert.IsFalse(shot3);
            Assert.AreEqual(8, weapon.Ammo.InMagazine, "Cancelled burst must not consume ammo");

            // 7. Cannot start new burst before FireInterval has elapsed (1.0 + 0.5 = 1.5)
            Assert.IsFalse(weapon.StartBurst(1.3f));

            // 8. Can start burst after FireInterval
            Assert.IsTrue(weapon.StartBurst(1.6f));
            Assert.AreEqual(3, weapon.BurstShotsRemaining);
        }

        [Test]
        public void BurstFiring_RapidWeaponSwitching_CancelsBurstAndAmmoMatchesRoundsFired()
        {
            GameObject playerGo = new GameObject("PlayerWithLoadout");
            _spawnedObjects.Add(playerGo);

            GameObject gunGo1 = new GameObject("BurstGun");
            gunGo1.transform.SetParent(playerGo.transform);
            _spawnedObjects.Add(gunGo1);
            Gun burstGun = gunGo1.AddComponent<Gun>();

            GameObject spawnGo1 = new GameObject("Spawn1");
            spawnGo1.transform.SetParent(gunGo1.transform);
            _spawnedObjects.Add(spawnGo1);

            WeaponRuntimeConfig burstConfig = new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Burst,
                FireInterval = 0.5f,
                BurstCount = 3,
                BurstInterval = 0.1f,
                MagazineSize = 12,
                StartingReserveAmmo = 36,
                MaxReserveAmmo = 60,
                InfiniteAmmo = false
            };
            burstGun.ConfigureForTesting(new WeaponRuntime(burstConfig), null, spawnGo1.transform);

            GameObject gunGo2 = new GameObject("PistolGun");
            gunGo2.transform.SetParent(playerGo.transform);
            _spawnedObjects.Add(gunGo2);
            Gun pistolGun = gunGo2.AddComponent<Gun>();

            GameObject spawnGo2 = new GameObject("Spawn2");
            spawnGo2.transform.SetParent(gunGo2.transform);
            _spawnedObjects.Add(spawnGo2);

            WeaponRuntimeConfig pistolConfig = new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.SemiAutomatic,
                FireInterval = 0.2f,
                MagazineSize = 10,
                StartingReserveAmmo = 30,
                MaxReserveAmmo = 50,
                InfiniteAmmo = false
            };
            pistolGun.ConfigureForTesting(new WeaponRuntime(pistolConfig), null, spawnGo2.transform);

            WeaponLoadout loadout = playerGo.AddComponent<WeaponLoadout>();
            loadout.AddWeapon(burstGun, equipImmediately: true);
            loadout.AddWeapon(pistolGun, equipImmediately: false);

            Assert.AreSame(burstGun, loadout.ActiveGun);
            Assert.AreEqual(12, burstGun.AmmoInMagazine);

            // Fire burst round 1 programmatically
            burstGun.Shoot(Vector3.forward);
            Assert.AreEqual(11, burstGun.AmmoInMagazine, "Shot 1 consumes 1 round");
            Assert.AreEqual(2, burstGun.Runtime.BurstShotsRemaining);

            // Rapidly switch weapon before remaining burst rounds fire
            bool switched = loadout.EquipNext();
            Assert.IsTrue(switched);
            Assert.AreSame(pistolGun, loadout.ActiveGun);

            // Verify burst on previous weapon was cancelled
            Assert.AreEqual(0, burstGun.Runtime.BurstShotsRemaining, "Unequipping must cancel pending burst shots");
            Assert.IsFalse(burstGun.IsEquipped);
            Assert.AreEqual(11, burstGun.AmmoInMagazine, "Cancelled shots must not consume magazine ammo");

            // Switch back to burst gun
            loadout.EquipPrevious();
            Assert.AreSame(burstGun, loadout.ActiveGun);
            Assert.AreEqual(0, burstGun.Runtime.BurstShotsRemaining);
            Assert.AreEqual(11, burstGun.AmmoInMagazine);
        }

        [Test]
        public void Gun_AutomaticWeapon_FiresViaShootMethod()
        {
            GameObject gunGo = new GameObject("TestAutoGun");
            _spawnedObjects.Add(gunGo);

            GameObject spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetParent(gunGo.transform);
            _spawnedObjects.Add(spawnGo);

            Gun gun = gunGo.AddComponent<Gun>();

            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Automatic,
                FireInterval = 0.1f,
                MagazineSize = 30,
                StartingReserveAmmo = 90,
                MaxReserveAmmo = 120,
                InfiniteAmmo = false
            };

            WeaponRuntime runtime = new WeaponRuntime(config);
            gun.ConfigureForTesting(runtime, null, spawnGo.transform);

            Assert.AreEqual(30, gun.AmmoInMagazine);

            // Gun.Shoot programmatic firing
            gun.Shoot(Vector3.forward);

            Assert.AreEqual(29, gun.AmmoInMagazine, "Calling gun.Shoot on automatic weapon must consume 1 round");
        }

        [Test]
        public void Gun_SaturatedDelivery_DoesNotConsumeAmmoOrRaiseFired()
        {
            GameObject gunGo = new GameObject("SaturatedGun");
            _spawnedObjects.Add(gunGo);
            Gun gun = gunGo.AddComponent<Gun>();
            WeaponRuntime runtime = new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.SemiAutomatic,
                FireInterval = 0.1f,
                MagazineSize = 3,
                StartingReserveAmmo = 3,
                MaxReserveAmmo = 3,
                InfiniteAmmo = false
            });
            var delivery = new SaturatedWeaponDelivery();
            gun.ConfigureForTesting(runtime, delivery, gunGo.transform);
            int fired = 0;
            gun.Fired += () => fired++;

            gun.Shoot(Vector3.forward);

            Assert.AreEqual(3, gun.AmmoInMagazine);
            Assert.AreEqual(0, fired);
            Assert.AreEqual(1, delivery.AdmissionAttempts);
        }

        [Test]
        public void Gun_CanShootAndReloadInWorkshopFiringRange()
        {
            GameObject stateGo = new GameObject("StateController");
            _spawnedObjects.Add(stateGo);
            GameStateController state = stateGo.AddComponent<GameStateController>();
            state.EnterWorkshopRoaming();
            state.EnterWorkshopFiringRange();

            GameObject gunGo = new GameObject("RangeGun");
            _spawnedObjects.Add(gunGo);
            Gun gun = gunGo.AddComponent<Gun>();
            gun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.SemiAutomatic,
                FireInterval = 0.1f,
                ReloadDuration = 1f,
                MagazineSize = 2,
                StartingReserveAmmo = 2,
                MaxReserveAmmo = 2,
                InfiniteAmmo = false
            }), null, gunGo.transform);

            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, gun.AmmoInMagazine);
            Assert.IsTrue(gun.Reload());
        }

        private sealed class SaturatedWeaponDelivery : IWeaponDelivery, IWeaponDeliveryAdmission
        {
            public int AdmissionAttempts { get; private set; }
            public bool TryReserveShot(Transform spawnPoint) { AdmissionAttempts++; return false; }
            public void CancelReservedShot() { }
            public void Deliver(Transform spawnPoint, Vector3 direction, int damage, DamageAffiliation sourceAffiliation, Transform sourceRoot, EffectPool effectPool, float spreadAngle = 0f) { }
            public void ClearActiveProjectiles() { }
            public void Dispose() { }
        }

        [Test]
        public void Shotgun_ProjectileWeaponDelivery_SamplesIndependentPelletSpreadAngles()
        {
            GameObject prefabGo = new GameObject("PelletPrefab");
            _spawnedObjects.Add(prefabGo);
            Projectile prefab = prefabGo.AddComponent<Projectile>();

            GameObject spawnPoint = new GameObject("SpawnPoint");
            _spawnedObjects.Add(spawnPoint);

            int pelletsPerShot = 8;
            float spreadAngle = 20f;

            ProjectileWeaponDelivery delivery = new ProjectileWeaponDelivery(
                prefab,
                defaultPoolSize: pelletsPerShot * 2,
                maxPoolSize: pelletsPerShot * 4,
                projectilesPerShot: pelletsPerShot);

            delivery.Deliver(
                spawnPoint.transform,
                Vector3.forward,
                10,
                null,
                null,
                null,
                spreadAngle);

            Projectile[] spawned = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            HashSet<int> distinctAngles = new HashSet<int>();

            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == prefab) continue;
                Vector3 dir = spawned[i].transform.forward;
                int hash = Mathf.RoundToInt(dir.x * 1000f);
                distinctAngles.Add(hash);
            }

            Assert.Greater(distinctAngles.Count, 1, "Shotgun pellets delivered via ProjectileWeaponDelivery must sample independent spread directions");

            delivery.Dispose();
        }

        [Test]
        public void ProjectileWeaponDelivery_Dispose_DestroysActiveProjectilesWithoutOrphans()
        {
            GameObject prefabGo = new GameObject("TestProjectilePrefab");
            _spawnedObjects.Add(prefabGo);
            Projectile prefab = prefabGo.AddComponent<Projectile>();

            GameObject spawnPoint = new GameObject("SpawnPoint");
            _spawnedObjects.Add(spawnPoint);

            ProjectileWeaponDelivery delivery = new ProjectileWeaponDelivery(
                prefab,
                defaultPoolSize: 2,
                maxPoolSize: 10,
                projectilesPerShot: 1);

            // Deliver a projectile
            delivery.Deliver(
                spawnPoint.transform,
                Vector3.forward,
                10,
                null,
                null,
                null,
                0f);

            // Verify a projectile was activated
            Projectile[] activeBefore = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.AreEqual(2, activeBefore.Length, "Should have source prefab and 1 active spawned projectile");

            // Dispose delivery
            delivery.Dispose();

            // Verify zero active projectiles spawned by delivery remain (only source prefab remains)
            Projectile[] activeAfter = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.AreEqual(1, activeAfter.Length, "Dispose must leave zero orphan active projectiles");
            Assert.AreSame(prefab, activeAfter[0], "The only projectile left should be the source prefab");
        }

        [Test]
        public void WorldResetManager_RepeatedRestarts_FullRestoration()
        {
            // 1. Setup GameStateController
            GameObject stateGo = new GameObject("StateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            // 2. Setup Player
            GameObject playerGo = new GameObject("Player");
            _spawnedObjects.Add(playerGo);
            PlayerController player = playerGo.AddComponent<PlayerController>();
            HealthComponent playerHealth = playerGo.GetComponent<HealthComponent>();

            // 3. Setup ResetManager
            GameObject resetGo = new GameObject("ResetManager");
            _spawnedObjects.Add(resetGo);
            WorldResetManager resetManager = resetGo.AddComponent<WorldResetManager>();
            resetManager.Initialize(player, null, gameState, null, null, null);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Play and take damage
                gameState.StartGame();
                Assert.AreEqual(GameState.Playing, gameState.CurrentState);

                playerHealth.TakeDamage(40);
                Assert.AreEqual(60, playerHealth.CurrentHealth);

                // Add pickup and debris
                GameObject debrisGo = new GameObject($"DebrisChunk_{cycle}");
                _spawnedObjects.Add(debrisGo);
                DebrisChunk debris = debrisGo.AddComponent<DebrisChunk>();
                debris.Initialize(Vector3.up, Vector3.zero, 5f, 9.8f, 0.3f, 0f);

                GameObject pickupGo = new GameObject($"WeaponPickup_{cycle}");
                _spawnedObjects.Add(pickupGo);
                pickupGo.AddComponent<WeaponPickup>();

                // End run via GameOver or Victory
                if (cycle % 2 == 0)
                {
                    gameState.EndGame();
                    Assert.AreEqual(GameState.GameOver, gameState.CurrentState);
                }
                else
                {
                    gameState.TriggerVictory();
                    Assert.AreEqual(GameState.Victory, gameState.CurrentState);
                }

                // Restart
                resetManager.Restart();

                // Assert state restored
                Assert.AreEqual(GameState.Playing, gameState.CurrentState);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
                Assert.IsFalse(debrisGo.activeInHierarchy, "Debris must be cleared on restart");
                Assert.IsTrue(pickupGo == null || !pickupGo.activeInHierarchy, "Pickups must be cleared on restart");
            }
        }

        [Test]
        public void UIControllers_SingleBindSubscription_AcrossRepeatedLifecycleCycles()
        {
            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController stateController = stateGo.AddComponent<GameStateController>();

            GameObject victoryGo = new GameObject("VictoryUI");
            _spawnedObjects.Add(victoryGo);
            VictoryUI victoryUI = victoryGo.AddComponent<VictoryUI>();

            GameObject playerGo = new GameObject("PlayerController");
            _spawnedObjects.Add(playerGo);
            PlayerController player = playerGo.AddComponent<PlayerController>();

            GameObject settingsGo = new GameObject("SettingsUI");
            _spawnedObjects.Add(settingsGo);
            SettingsUI settingsUI = settingsGo.AddComponent<SettingsUI>();

            GameObject highScoreGo = new GameObject("HighScoreController");
            _spawnedObjects.Add(highScoreGo);
            HighScoreController highScoreController = highScoreGo.AddComponent<HighScoreController>();

            // Initialize UI components
            victoryUI.Initialize(stateController);
            settingsUI.Initialize(player);
            highScoreController.Initialize(null, stateController);

            // Cycle active state multiple times
            for (int i = 0; i < 3; i++)
            {
                victoryGo.SetActive(false);
                victoryGo.SetActive(true);

                settingsGo.SetActive(false);
                settingsGo.SetActive(true);

                highScoreGo.SetActive(false);
                highScoreGo.SetActive(true);
            }

            // Re-initialize (idempotency check)
            victoryUI.Initialize(stateController);
            settingsUI.Initialize(player);
            highScoreController.Initialize(null, stateController);

            // Trigger state transitions without throwing exceptions or duplicate handlers
            stateController.StartGame();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);

            stateController.Pause();
            Assert.AreEqual(GameState.Paused, stateController.CurrentState);

            stateController.Resume();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);

            stateController.TriggerVictory();
            Assert.AreEqual(GameState.Victory, stateController.CurrentState);
        }

        [Test]
        public void BossPhase_DeathDuringShockwaveWindup_CancelsAttackAndPreventsDamage()
        {
            GameObject bossGo = new GameObject("TestBoss");
            _spawnedObjects.Add(bossGo);

            HealthComponent bossHealth = bossGo.AddComponent<HealthComponent>();
            bossHealth.SetMaxHealth(100);

            BossPhaseController phaseController = bossGo.AddComponent<BossPhaseController>();
            GameObject telegraphGo = new GameObject("TelegraphObj");
            telegraphGo.transform.SetParent(bossGo.transform);
            _spawnedObjects.Add(telegraphGo);
            telegraphGo.SetActive(false);

            BossPhase enragePhase = new BossPhase
            {
                PhaseName = "Enrage",
                EnterAtHealthFraction = 0.5f,
                TriggerShockwaveOnEnter = true,
                ShockwaveRadius = 6f,
                ShockwaveDamage = 50,
                ShockwaveWindup = 2.0f,
                ShockwaveRecovery = 0.5f,
                ShockwaveTelegraph = telegraphGo
            };

            // Inject phase into serialized array via reflection
            var phasesField = typeof(BossPhaseController).GetField("_phases", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            phasesField.SetValue(phaseController, new BossPhase[] { enragePhase });

            phaseController.Initialize(bossHealth);

            // Damage boss to trigger phase 2 windup
            bossHealth.TakeDamage(60); // Health: 40/100 (<= 0.5)
            Assert.AreEqual(0, phaseController.ActivePhaseIndex);
            Assert.IsTrue(telegraphGo.activeSelf, "Telegraph should be active during shockwave windup");

            // Kill boss during windup
            bossHealth.TakeDamage(40); // Health: 0 (dead)
            Assert.AreEqual(0, bossHealth.CurrentHealth);

            // Telegraph must be immediately deactivated and attack cancelled on boss death
            Assert.IsFalse(telegraphGo.activeSelf, "Telegraph must be cancelled immediately when boss dies");
        }

        [Test]
        public void ScoreController_CompositionRoot_LifecycleAndNoDuplicateKills()
        {
            GameObject compGo = new GameObject("GameCompositionRoot");
            _spawnedObjects.Add(compGo);
            GameCompositionRoot compRoot = compGo.AddComponent<GameCompositionRoot>();

            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            GameObject scoreGo = new GameObject("ScoreController");
            _spawnedObjects.Add(scoreGo);
            ScoreController scoreController = scoreGo.AddComponent<ScoreController>();

            compRoot.ComposeDependencies();

            Assert.AreEqual(0, scoreController.Score);

            int notifications = 0;
            int lastNotifiedScore = 0;
            scoreController.OnScoreChanged += s =>
            {
                notifications++;
                lastNotifiedScore = s;
            };

            // 1. One kill worth 10 points produces exactly 10 points and one score-change notification
            spawner.NotifyEnemyKilled(10);
            Assert.AreEqual(10, scoreController.Score);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(10, lastNotifiedScore);

            // 2. Repeat after three disable/enable cycles
            for (int i = 0; i < 3; i++)
            {
                scoreController.enabled = false;
                scoreController.enabled = true;
            }

            notifications = 0;
            spawner.NotifyEnemyKilled(10);
            Assert.AreEqual(20, scoreController.Score);
            Assert.AreEqual(1, notifications, "After 3 disable/enable cycles, exactly one notification per kill");
            Assert.AreEqual(20, lastNotifiedScore);

            // 3. Repeated initialization must be harmless
            scoreController.Initialize(spawner, gameState);
            scoreController.Initialize(spawner, gameState);

            notifications = 0;
            spawner.NotifyEnemyKilled(10);
            Assert.AreEqual(30, scoreController.Score);
            Assert.AreEqual(1, notifications, "After repeated initialization, exactly one notification per kill");
            Assert.AreEqual(30, lastNotifiedScore);

            // 4. Disabled controllers must receive no kills
            scoreController.enabled = false;
            notifications = 0;
            spawner.NotifyEnemyKilled(10);
            Assert.AreEqual(30, scoreController.Score, "Disabled controller must not increment score");
            Assert.AreEqual(0, notifications, "Disabled controller must not fire score notifications");

            scoreController.enabled = true;
            notifications = 0;
            spawner.NotifyEnemyKilled(10);
            Assert.AreEqual(40, scoreController.Score);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void SaveManager_CorruptPrimary_RecoversFromBackupAndPreservesBackup_ProfileAndSettings()
        {
            // Test Profile
            string profileFile = Path.Combine(_tempDir, "user_profile.json");
            string profileBackup = profileFile + ".bak";

            UserProfileData initialProfile = new UserProfileData
            {
                HighScore = 100,
                TotalRuns = 2,
                TotalWins = 1,
                TotalLosses = 1
            };
            Assert.IsTrue(SaveManager.SaveToFile(profileFile, initialProfile));

            UserProfileData updatedProfile = new UserProfileData
            {
                HighScore = 250,
                TotalRuns = 5,
                TotalWins = 3,
                TotalLosses = 2
            };
            Assert.IsTrue(SaveManager.SaveToFile(profileFile, updatedProfile));
            Assert.IsTrue(File.Exists(profileBackup), "Backup must exist after second save");

            // Corrupt primary
            File.WriteAllText(profileFile, "{ broken primary json @@ ##");

            UserProfileData recoveredProfile = SaveManager.LoadFromFile<UserProfileData>(profileFile);
            Assert.IsNotNull(recoveredProfile, "Profile must recover from backup");
            Assert.AreEqual(100, recoveredProfile.HighScore, "Profile data must match backup content");
            Assert.IsTrue(File.Exists(profileBackup), "Backup must remain intact after recovery");
            Assert.IsFalse(File.ReadAllText(profileFile).Contains("broken primary"), "Primary must be restored to valid json");

            // Test Settings
            string settingsFile = Path.Combine(_tempDir, "game_settings.json");
            string settingsBackup = settingsFile + ".bak";

            GameSettingsData initialSettings = new GameSettingsData
            {
                MasterVolume = 0.5f,
                MusicVolume = 0.4f,
                SFXVolume = 0.6f
            };
            Assert.IsTrue(SaveManager.SaveToFile(settingsFile, initialSettings));

            GameSettingsData updatedSettings = new GameSettingsData
            {
                MasterVolume = 0.8f,
                MusicVolume = 0.7f,
                SFXVolume = 0.9f
            };
            Assert.IsTrue(SaveManager.SaveToFile(settingsFile, updatedSettings));
            Assert.IsTrue(File.Exists(settingsBackup));

            // Corrupt primary
            File.WriteAllText(settingsFile, "{ corrupted settings data !!!");

            GameSettingsData recoveredSettings = SaveManager.LoadFromFile<GameSettingsData>(settingsFile);
            Assert.IsNotNull(recoveredSettings, "Settings must recover from backup");
            Assert.AreEqual(0.5f, recoveredSettings.MasterVolume, 0.001f);
            Assert.IsTrue(File.Exists(settingsBackup), "Settings backup must remain intact");
            Assert.IsFalse(File.ReadAllText(settingsFile).Contains("corrupted settings"), "Settings primary must be restored");
        }

        [Test]
        public void SaveManager_BothCorrupt_ReturnsDefaults_ProfileAndSettings()
        {
            SaveManager.CustomSaveDirectory = _tempDir;

            string profileFile = Path.Combine(_tempDir, "user_profile.json");
            string profileBackup = profileFile + ".bak";
            File.WriteAllText(profileFile, "bad1");
            File.WriteAllText(profileBackup, "bad2");

            UserProfileData defaultProfile = SaveManager.LoadProfile();
            Assert.IsNotNull(defaultProfile, "Must return default profile when both files are corrupt");
            Assert.AreEqual(0, defaultProfile.HighScore);
            Assert.AreEqual(0, defaultProfile.TotalRuns);

            string settingsFile = Path.Combine(_tempDir, "game_settings.json");
            string settingsBackup = settingsFile + ".bak";
            File.WriteAllText(settingsFile, "bad1");
            File.WriteAllText(settingsBackup, "bad2");

            GameSettingsData defaultSettings = SaveManager.LoadSettings();
            Assert.IsNotNull(defaultSettings, "Must return default settings when both files are corrupt");
            Assert.AreEqual(1f, defaultSettings.MasterVolume, 0.01f);
            Assert.AreEqual(0.8f, defaultSettings.MusicVolume, 0.01f);
            Assert.AreEqual(1f, defaultSettings.SFXVolume, 0.01f);
        }

        private class TestFileOperations : SaveManager.IFileOperations
        {
            public bool FailOnCreate { get; set; }
            public bool FailOnReplace { get; set; }

            public bool Exists(string path) => File.Exists(path);
            public string ReadAllText(string path) => File.ReadAllText(path);
            public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
            public void Copy(string sourceFileName, string destFileName, bool overwrite) => File.Copy(sourceFileName, destFileName, overwrite);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
            {
                if (FailOnReplace)
                {
                    throw new IOException("Simulated replacement failure");
                }
                File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            }
            public void Delete(string path) => File.Delete(path);
            public void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);
            public Stream Create(string path)
            {
                if (FailOnCreate)
                {
                    throw new IOException("Simulated create failure");
                }
                return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            public long GetLength(string path) => new FileInfo(path).Length;
        }

        [Test]
        public void SaveManager_FailureBeforeReplacement_PreservesPreviousSave()
        {
            string saveFile = Path.Combine(_tempDir, "preserve_test.json");
            UserProfileData initial = new UserProfileData { HighScore = 999 };
            Assert.IsTrue(SaveManager.SaveToFile(saveFile, initial));

            TestFileOperations testOps = new TestFileOperations { FailOnCreate = true };
            SaveManager.FileOps = testOps;

            UserProfileData next = new UserProfileData { HighScore = 1 };
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SaveManager\] Error saving to .*"));
            bool result = SaveManager.SaveToFile(saveFile, next);
            Assert.IsFalse(result, "Save must return false when writing temp file fails");

            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
            UserProfileData reloaded = SaveManager.LoadFromFile<UserProfileData>(saveFile);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(999, reloaded.HighScore, "Previous save must be preserved when failure occurs before replacement");
        }

        [Test]
        public void SaveManager_ReplacementFailure_PreservesRecoverableData()
        {
            string saveFile = Path.Combine(_tempDir, "replace_fail_test.json");
            UserProfileData initial = new UserProfileData { HighScore = 555 };
            Assert.IsTrue(SaveManager.SaveToFile(saveFile, initial));

            TestFileOperations testOps = new TestFileOperations { FailOnReplace = true };
            SaveManager.FileOps = testOps;

            UserProfileData next = new UserProfileData { HighScore = 777 };
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SaveManager\] Atomic replacement failed for .*"));
            bool result = SaveManager.SaveToFile(saveFile, next);
            Assert.IsFalse(result, "Save must return false when atomic replacement fails");

            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
            UserProfileData reloaded = SaveManager.LoadFromFile<UserProfileData>(saveFile);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(555, reloaded.HighScore, "Recoverable save data must be preserved on replacement failure");
        }

        [Test]
        public void WaveController_MissingBossZone_TerminatesRecoverably()
        {
            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            GameObject waveGo = new GameObject("WaveController");
            _spawnedObjects.Add(waveGo);
            WaveController waveController = waveGo.AddComponent<WaveController>();

            GameObject bossPrefabGo = new GameObject("BossPrefab");
            _spawnedObjects.Add(bossPrefabGo);
            EnemyController bossPrefab = bossPrefabGo.AddComponent<EnemyController>();

            WaveConfig bossWave = new WaveConfig
            {
                EnemyCount = 0,
                SpawnInterval = 0.05f,
                InitialDelay = 0f,
                DelayAfter = 0f,
                BossPrefab = bossPrefab,
                BossCount = 1,
                BossDelay = 0f,
                SpawnZoneIds = new List<string> { "NonExistentZone" }
            };

            waveController.SetWaves(new[] { bossWave });
            waveController.Initialize(spawner, gameState);
            gameState.Initialize(null, waveController);

            bool victoryTriggered = false;
            bool technicalFailureReported = false;
            waveController.AllWavesCompleted += () => victoryTriggered = true;
            waveController.TechnicalFailure += _ => technicalFailureReported = true;

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[WaveController\] Wave 1 configuration invalid: .*"));
            gameState.StartGame();

            // Advance controlled ticks
            for (int i = 0; i < 20; i++)
            {
                waveController.Tick(0.1f);
            }

            Assert.IsFalse(victoryTriggered, "Unspawned boss must NEVER grant victory");
            Assert.IsTrue(technicalFailureReported, "Invalid wave configuration must report a technical failure");
            Assert.AreNotEqual(GameState.Victory, gameState.CurrentState, "State must not be Victory");
            Assert.AreEqual(GameState.GameOver, gameState.CurrentState, "Missing boss zone must terminate into GameOver/retry flow");
        }

        [Test]
        public void WaveController_PersistentPositionFailure_TerminatesAfter15Attempts()
        {
            GameObject playerGo = new GameObject("Player");
            _spawnedObjects.Add(playerGo);
            playerGo.transform.position = Vector3.zero;

            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            // Setup a zone that fails position checks by requiring impossible player distance
            GameObject zoneGo = new GameObject("StrictZone");
            _spawnedObjects.Add(zoneGo);
            zoneGo.transform.position = Vector3.zero;
            SpawnZone zone = zoneGo.AddComponent<SpawnZone>();

            var zoneSer = new UnityEditor.SerializedObject(zone);
            zoneSer.FindProperty("_id").stringValue = "StrictZone";
            zoneSer.FindProperty("_minPlayerDistance").floatValue = 5000f; // Candidate points within radius 4 cannot pass min 5000
            zoneSer.ApplyModifiedPropertiesWithoutUndo();

            var spawnerSer = new UnityEditor.SerializedObject(spawner);
            var zonesProp = spawnerSer.FindProperty("_spawnZones");
            zonesProp.arraySize = 1;
            zonesProp.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            spawnerSer.ApplyModifiedPropertiesWithoutUndo();

            GameObject enemyPrefabGo = new GameObject("EnemyPrefab");
            _spawnedObjects.Add(enemyPrefabGo);
            EnemyController enemyPrefab = enemyPrefabGo.AddComponent<EnemyController>();

            spawner.Initialize(playerGo.transform, gameState, null);

            GameObject waveGo = new GameObject("WaveController");
            _spawnedObjects.Add(waveGo);
            WaveController waveController = waveGo.AddComponent<WaveController>();

            WaveConfig waveConfig = new WaveConfig
            {
                EnemyCount = 1,
                SpawnInterval = 0.05f,
                InitialDelay = 0f,
                DelayAfter = 0f,
                BossCount = 0,
                SpawnZoneIds = new List<string> { "StrictZone" },
                EnemyGroups = new List<WaveEnemyGroup>
                {
                    new WaveEnemyGroup { Prefab = enemyPrefab, GuaranteedCount = 1, Weight = 1 }
                }
            };

            waveController.SetWaves(new[] { waveConfig });
            waveController.Initialize(spawner, gameState);
            gameState.Initialize(null, waveController);

            gameState.StartGame();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[WaveController\] Fatal spawn error: .*"));

            // Advance controlled ticks: position failure attempts accumulate up to 15
            for (int i = 0; i < 30; i++)
            {
                waveController.Tick(0.1f);
            }

            Assert.AreEqual(GameState.GameOver, gameState.CurrentState, "Persistent position failure must terminate into GameOver");
        }

        [Test]
        public void WaveController_CapacityExhaustion_ResumesWhenCapacityAvailable()
        {
            GameObject playerGo = new GameObject("Player");
            _spawnedObjects.Add(playerGo);

            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            var spawnerSer = new UnityEditor.SerializedObject(spawner);
            spawnerSer.FindProperty("_maxAlive").intValue = 1; // Capacity = 1
            spawnerSer.ApplyModifiedPropertiesWithoutUndo();

            GameObject enemyPrefabGo = new GameObject("EnemyPrefab");
            _spawnedObjects.Add(enemyPrefabGo);
            EnemyController enemyPrefab = enemyPrefabGo.AddComponent<EnemyController>();
            enemyPrefabGo.AddComponent<HealthComponent>();

            spawner.Initialize(playerGo.transform, gameState, null);

            GameObject waveGo = new GameObject("WaveController");
            _spawnedObjects.Add(waveGo);
            WaveController waveController = waveGo.AddComponent<WaveController>();

            WaveConfig waveConfig = new WaveConfig
            {
                EnemyCount = 2,
                SpawnInterval = 0.01f,
                InitialDelay = 0f,
                DelayAfter = 0f,
                BossCount = 0,
                EnemyGroups = new List<WaveEnemyGroup>
                {
                    new WaveEnemyGroup { Prefab = enemyPrefab, GuaranteedCount = 2, Weight = 1 }
                }
            };

            waveController.SetWaves(new[] { waveConfig });
            waveController.Initialize(spawner, gameState);
            gameState.Initialize(null, waveController);

            gameState.StartGame();

            // First tick: enemy 1 spawns
            waveController.Tick(0.1f);
            Assert.AreEqual(1, spawner.AliveCount, "First enemy should spawn");

            // Advance several ticks: capacity limit is reached, must NOT terminate or fail
            for (int i = 0; i < 10; i++)
            {
                waveController.Tick(0.1f);
            }
            Assert.AreEqual(1, spawner.AliveCount);
            Assert.AreEqual(GameState.Playing, gameState.CurrentState, "Game must remain Playing when waiting for capacity");

            // Free capacity
            spawner.ClearAllAlive();
            Assert.AreEqual(0, spawner.AliveCount);

            // Tick again: enemy 2 now spawns
            waveController.Tick(0.1f);
            Assert.AreEqual(1, spawner.AliveCount, "Second enemy should spawn after capacity frees");
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);
        }

        [Test]
        public void WaveController_SuccessfulSpawning_ProducesNormalVictory()
        {
            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            GameObject enemyPrefabGo = new GameObject("EnemyPrefab");
            _spawnedObjects.Add(enemyPrefabGo);
            EnemyController enemyPrefab = enemyPrefabGo.AddComponent<EnemyController>();
            enemyPrefabGo.AddComponent<HealthComponent>();

            spawner.Initialize(null, gameState, null);

            GameObject waveGo = new GameObject("WaveController");
            _spawnedObjects.Add(waveGo);
            WaveController waveController = waveGo.AddComponent<WaveController>();

            WaveConfig waveConfig = new WaveConfig
            {
                EnemyCount = 1,
                SpawnInterval = 0.01f,
                InitialDelay = 0f,
                DelayAfter = 0f,
                BossCount = 0,
                EnemyGroups = new List<WaveEnemyGroup>
                {
                    new WaveEnemyGroup { Prefab = enemyPrefab, GuaranteedCount = 1, Weight = 1 }
                }
            };

            waveController.SetWaves(new[] { waveConfig });
            waveController.Initialize(spawner, gameState);
            gameState.Initialize(null, waveController);

            gameState.StartGame();

            waveController.Tick(0.1f);
            Assert.AreEqual(1, spawner.AliveCount);

            // Kill enemy
            spawner.NotifyEnemyKilled(10);
            spawner.ClearAllAlive();

            // Tick to complete wave
            waveController.Tick(0.1f);

            Assert.AreEqual(GameState.Victory, gameState.CurrentState, "Clearing wave must produce normal victory");
        }

        [Test]
        public void WorldResetManager_AuthoredAndRuntimePickups_RestoresCorrectlyAcrossConsecutiveRestarts()
        {
            GameObject playerGo = new GameObject("Player");
            _spawnedObjects.Add(playerGo);
            playerGo.transform.position = Vector3.zero;
            PlayerController player = playerGo.AddComponent<PlayerController>();

            GameObject spawnPointGo = new GameObject("SpawnPoint");
            _spawnedObjects.Add(spawnPointGo);
            spawnPointGo.transform.position = new Vector3(5f, 0f, 5f);

            GameObject camGo = new GameObject("MainCamera");
            _spawnedObjects.Add(camGo);
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            ScreenShake shake = camGo.AddComponent<ScreenShake>();

            // 1. Scene-authored pickups
            GameObject pickup1Go = new GameObject("AuthoredAmmoPickup");
            _spawnedObjects.Add(pickup1Go);
            pickup1Go.transform.position = new Vector3(10f, 0f, 0f);
            AmmoPickup ammoPickup = pickup1Go.AddComponent<AmmoPickup>();
            ammoPickup.Amount = 30;

            GameObject pickup2Go = new GameObject("AuthoredWeaponPickup");
            _spawnedObjects.Add(pickup2Go);
            pickup2Go.transform.position = new Vector3(20f, 0f, 0f);
            WeaponPickup weaponPickup = pickup2Go.AddComponent<WeaponPickup>();

            GameObject resetGo = new GameObject("WorldResetManager");
            _spawnedObjects.Add(resetGo);
            WorldResetManager resetManager = resetGo.AddComponent<WorldResetManager>();

            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("EnemySpawner");
            _spawnedObjects.Add(spawnerGo);
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            GameObject waveGo = new GameObject("WaveController");
            _spawnedObjects.Add(waveGo);
            WaveController wave = waveGo.AddComponent<WaveController>();

            resetManager.Initialize(player, spawner, gameState, wave, null, null, spawnPointGo.transform);

            // Verify both authored pickups are registered and active
            Assert.IsTrue(ammoPickup.gameObject.activeSelf);
            Assert.IsTrue(weaponPickup.gameObject.activeSelf);

            // 2. Gameplay: collect authored ammo pickup (should deactivate, not destroy)
            ammoPickup.Collect();
            Assert.IsFalse(ammoPickup.gameObject.activeSelf, "Authored pickup should deactivate when collected");

            // Leave weaponPickup untouched (active)

            // Spawn a runtime drop
            GameObject runtimeDropGo = new GameObject("RuntimeDrop");
            _spawnedObjects.Add(runtimeDropGo);
            runtimeDropGo.transform.position = new Vector3(15f, 0f, 0f);
            AmmoPickup runtimeDrop = runtimeDropGo.AddComponent<AmmoPickup>();
            runtimeDrop.IsRuntimeDrop = true;

            // Player moves away
            playerGo.transform.position = new Vector3(50f, 0f, 50f);

            // 3. First restart
            resetManager.ResetWorld();

            // Assertions after 1st restart
            Assert.IsTrue(runtimeDrop == null || runtimeDrop.gameObject == null, "Runtime drop must be destroyed on restart");
            Assert.IsTrue(ammoPickup.gameObject.activeSelf, "Authored ammo pickup must be reactivated");
            Assert.AreEqual(new Vector3(10f, 0f, 0f), ammoPickup.transform.position, "Authored ammo pickup must return to initial position");
            Assert.AreEqual(30, ammoPickup.Amount, "Authored pickup contents must be preserved");

            Assert.IsTrue(weaponPickup.gameObject.activeSelf, "Authored weapon pickup must remain active");
            Assert.AreEqual(new Vector3(20f, 0f, 0f), weaponPickup.transform.position);

            Assert.AreEqual(new Vector3(5f, 0f, 5f), playerGo.transform.position, "Player must reset to spawn point");

            // 4. Second consecutive restart
            resetManager.ResetWorld();

            // Verify no duplicates
            AmmoPickup[] allAmmo = Object.FindObjectsByType<AmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            WeaponPickup[] allWeapons = Object.FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, allAmmo.Length, "Must be exactly one AmmoPickup after two consecutive restarts");
            Assert.AreEqual(1, allWeapons.Length, "Must be exactly one WeaponPickup after two consecutive restarts");
            Assert.IsTrue(allAmmo[0].gameObject.activeSelf);
            Assert.IsTrue(allWeapons[0].gameObject.activeSelf);
        }

        [Test]
        public void Audio_SettingsUI_VolumeAuthority_And_GameStateAudioTransitions()
        {
            AudioMixer mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Audio/GameAudioMixer.mixer");
            Assert.IsNotNull(mixer, "GameAudioMixer asset must exist");

            GameObject settingsGo = new GameObject("SettingsUI");
            _spawnedObjects.Add(settingsGo);
            SettingsUI settingsUI = settingsGo.AddComponent<SettingsUI>();
            settingsUI.AudioMixer = mixer;

            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject audioGo = new GameObject("GameStateAudio");
            _spawnedObjects.Add(audioGo);
            GameStateAudio gameStateAudio = audioGo.AddComponent<GameStateAudio>();

            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/combat_music.wav");
            gameStateAudio.CombatMusicClip = clip;

            AudioSource musicSource = audioGo.AddComponent<AudioSource>();
            var gsaSer = new UnityEditor.SerializedObject(gameStateAudio);
            gsaSer.FindProperty("_musicSource").objectReferenceValue = musicSource;
            gsaSer.FindProperty("_combatMusicClip").objectReferenceValue = clip;
            gsaSer.ApplyModifiedPropertiesWithoutUndo();

            settingsUI.MusicSource = musicSource;
            gameStateAudio.Initialize(gameState);

            // 1. Settings Data save and volume authority
            GameSettingsData settings = new GameSettingsData
            {
                MasterVolume = 0.5f,
                MusicVolume = 0.25f,
                SFXVolume = 0.75f
            };

            // Using reflection or UI methods to apply settings to game
            var applyMethod = typeof(SettingsUI).GetMethod("ApplySettingsToGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            applyMethod.Invoke(settingsUI, new object[] { settings });

            // Mixer is volume authority: AudioListener and MusicSource volume are 1f (no double attenuation)
            Assert.AreEqual(1f, AudioListener.volume, 0.001f);
            Assert.AreEqual(1f, musicSource.volume, 0.001f);

            // Mixer parameters updated if audio engine is running
            if (UnityEngine.Application.isPlaying && mixer.GetFloat("MasterVolume", out float masterDb))
            {
                Assert.IsTrue(masterDb < 0f && masterDb > -40f, $"MasterVolume decibels should reflect 0.5 linear ({masterDb}dB)");
            }
            if (UnityEngine.Application.isPlaying && mixer.GetFloat("MusicVolume", out float musicDb) && mixer.GetFloat("SFXVolume", out float sfxDb))
            {
                Assert.IsTrue(musicDb < sfxDb, "Music volume (0.25) should be lower than SFX volume (0.75)");
            }

            // 2. GameStateAudio music playback lifecycle
            gameState.StartGame(); // -> Playing
            Assert.AreEqual(clip, musicSource.clip);
            if (UnityEngine.Application.isPlaying)
            {
                Assert.IsTrue(musicSource.isPlaying, "Music should play during GameState.Playing");
            }

            gameState.Pause(); // -> Paused
            Assert.IsFalse(musicSource.isPlaying, "Music should pause during GameState.Paused");

            gameState.Resume(); // -> Playing
            if (UnityEngine.Application.isPlaying)
            {
                Assert.IsTrue(musicSource.isPlaying, "Music should resume when unpaused");
            }

            gameState.EndGame(); // -> GameOver
            Assert.IsFalse(musicSource.isPlaying, "Music should stop on GameOver");
        }
    }
}

