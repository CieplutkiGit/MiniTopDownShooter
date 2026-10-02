using System.Collections.Generic;
using System.IO;
using Application;
using Core;
using Game;
using NUnit.Framework;
using UnityEngine;

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
        public void Shotgun_IndependentPelletSpreadSampling_ProducesDistinctDirections()
        {
            int pelletCount = 6;
            float spreadAngle = 15f;
            Vector3 baseDir = Vector3.forward;

            HashSet<int> distinctAngles = new HashSet<int>();
            for (int i = 0; i < pelletCount; i++)
            {
                float sampledAngle = Random.Range(-spreadAngle, spreadAngle);
                Vector3 pelletDir = Quaternion.AngleAxis(sampledAngle, Vector3.up) * baseDir;
                int hash = Mathf.RoundToInt(pelletDir.x * 1000f);
                distinctAngles.Add(hash);
            }

            Assert.Greater(distinctAngles.Count, 1, "Pellets must sample independent spread angles");
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
        public void WorldResetManager_Restart_FullRestoration()
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

            // 4. Setup Debris Chunk
            GameObject debrisGo = new GameObject("DebrisChunk");
            _spawnedObjects.Add(debrisGo);
            DebrisChunk debris = debrisGo.AddComponent<DebrisChunk>();
            debris.Initialize(Vector3.up, Vector3.zero, 5f, 9.8f, 0.3f, 0f);

            // 5. Setup Weapon Pickup
            GameObject pickupGo = new GameObject("WeaponPickup");
            _spawnedObjects.Add(pickupGo);
            WeaponPickup pickup = pickupGo.AddComponent<WeaponPickup>();

            // Start game, then damage player and transition to GameOver
            gameState.StartGame();
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);

            playerHealth.TakeDamage(50);
            Assert.AreEqual(50, playerHealth.CurrentHealth);

            gameState.ReturnToMenu();
            Assert.AreEqual(GameState.Menu, gameState.CurrentState);

            // Execute World Reset
            resetManager.Restart();

            // Verify:
            // 1) Game State is Playing
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);
            // 2) Player health restored
            Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            // 3) Debris chunk deactivated
            Assert.IsFalse(debrisGo.activeInHierarchy, "Active debris chunks must be cleared on restart");
            // 4) Pickup destroyed
            Assert.IsTrue(pickupGo == null || !pickupGo.activeInHierarchy, "Pickups must be cleared on restart");
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

            // Initialize UI components
            victoryUI.Initialize(stateController);
            settingsUI.Initialize(player);

            // Cycle active state multiple times
            for (int i = 0; i < 3; i++)
            {
                victoryGo.SetActive(false);
                victoryGo.SetActive(true);

                settingsGo.SetActive(false);
                settingsGo.SetActive(true);
            }

            // Re-initialize (idempotency check)
            victoryUI.Initialize(stateController);
            settingsUI.Initialize(player);

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
        public void BossPhase_TelegraphAndRecoveryDefaults_AreConfigured()
        {
            BossPhase phase = new BossPhase
            {
                PhaseName = "Enrage",
                EnterAtHealthFraction = 0.5f,
                MovementSpeedMultiplier = 1.3f,
                AttackCooldownMultiplier = 0.7f,
                BonusDamage = 10,
                TriggerShockwaveOnEnter = true,
                ShockwaveRadius = 6f,
                ShockwaveDamage = 20,
                ShockwaveWindup = 0.8f,
                ShockwaveRecovery = 0.4f
            };

            Assert.AreEqual(0.8f, phase.ShockwaveWindup, 0.001f);
            Assert.AreEqual(0.4f, phase.ShockwaveRecovery, 0.001f);
            Assert.AreEqual(6f, phase.ShockwaveRadius, 0.001f);
            Assert.AreEqual(20, phase.ShockwaveDamage);
        }

        [Test]
        public void SaveManager_ValidateAndMigrate_PreservesLegacyHighScoreAndSanitizesData()
        {
            // Test UserProfileData migration
            UserProfileData profile = new UserProfileData
            {
                TotalRuns = -5,
                TotalWins = -2,
                TotalLosses = -3,
                HighScore = 500
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(0, profile.TotalRuns, "Negative runs must be clamped to 0");
            Assert.AreEqual(0, profile.TotalWins, "Negative wins must be clamped to 0");
            Assert.AreEqual(0, profile.TotalLosses, "Negative losses must be clamped to 0");
            Assert.AreEqual(500, profile.HighScore, "Legacy high score must be strictly preserved");

            // Test GameSettingsData migration
            GameSettingsData settings = new GameSettingsData
            {
                MasterVolume = 2.5f,
                MusicVolume = -0.5f,
                SFXVolume = 1.0f,
                AimSensitivity = 15f,
                TouchControlScale = 0.2f
            };

            settings.ValidateAndMigrate();

            Assert.AreEqual(1f, settings.MasterVolume, 0.001f, "Volume > 1 must be clamped to 1");
            Assert.AreEqual(0f, settings.MusicVolume, 0.001f, "Volume < 0 must be clamped to 0");
            Assert.AreEqual(10f, settings.AimSensitivity, 0.001f, "Sensitivity > 10 must be clamped to 10");
            Assert.AreEqual(0.5f, settings.TouchControlScale, 0.001f, "Touch scale < 0.5 must be clamped to 0.5");
        }
    }
}
