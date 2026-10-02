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
        public void WaveController_BossSpawnFailure_NeverCountsAsKilled()
        {
            GameObject stateGo = new GameObject("GameStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController gameState = stateGo.AddComponent<GameStateController>();

            GameObject spawnerGo = new GameObject("MockEnemySpawner");
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
                EnemyCount = 1,
                SpawnInterval = 0.1f,
                InitialDelay = 0f,
                DelayAfter = 0f,
                BossPrefab = bossPrefab,
                BossCount = 1,
                BossDelay = 0f,
                SpawnZoneIds = new List<string> { "NonExistentZone" }
            };

            // Set inline waves via reflection
            var wavesField = typeof(WaveController).GetField("_waves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            wavesField.SetValue(waveController, new List<WaveConfig> { bossWave });

            waveController.Initialize(spawner, gameState);

            bool victoryTriggered = false;
            waveController.AllWavesCompleted += () => victoryTriggered = true;

            // Start wave
            gameState.StartGame();

            // Simulate frames where boss cannot spawn because zone does not exist
            var updateMethod = typeof(WaveController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            for (int i = 0; i < 20; i++)
            {
                updateMethod.Invoke(waveController, null);
            }

            // Unspawned boss must NEVER be counted as killed!
            Assert.IsFalse(victoryTriggered, "Unspawned boss must NEVER trigger victory or be counted as killed");
            Assert.AreNotEqual(GameState.Victory, gameState.CurrentState, "Game state must not transition to Victory on unspawned boss");
        }

        [Test]
        public void SaveManager_SaveRecovery_RecoversCorruptedFileFromBackupAndMigrates()
        {
            string saveFile = Path.Combine(_tempDir, "test_profile.json");
            string backupFile = saveFile + ".bak";

            UserProfileData initialProfile = new UserProfileData
            {
                HighScore = 250,
                TotalRuns = 5,
                TotalWins = 3,
                TotalLosses = 2
            };

            // 1. Initial valid save
            Assert.IsTrue(SaveManager.SaveToFile(saveFile, initialProfile));
            Assert.IsTrue(File.Exists(saveFile));

            // 2. Second valid save (creates .bak of initial profile)
            UserProfileData updatedProfile = new UserProfileData
            {
                HighScore = 500,
                TotalRuns = 10,
                TotalWins = 6,
                TotalLosses = 4
            };
            Assert.IsTrue(SaveManager.SaveToFile(saveFile, updatedProfile));
            Assert.IsTrue(File.Exists(backupFile), "Backup file must exist after subsequent save");

            // 3. Corrupt primary file with broken JSON garbage
            File.WriteAllText(saveFile, "{ corrupted JSON text ### <<< invalid !!");

            // 4. Load from file: must detect corruption, recover from backup, and restore primary
            UserProfileData recovered = SaveManager.LoadFromFile<UserProfileData>(saveFile);
            Assert.IsNotNull(recovered, "Must successfully recover from backup when primary file is corrupt");
            Assert.AreEqual(250, recovered.HighScore, "Recovered data must match backup content");
            Assert.AreEqual(5, recovered.TotalRuns);

            // 5. Verify primary file was restored with valid JSON
            string restoredContent = File.ReadAllText(saveFile);
            Assert.IsFalse(restoredContent.Contains("corrupted JSON"), "Primary corrupt file must be replaced with recovered data");

            // 6. Test both files corrupt returns null safely without throwing exception
            File.WriteAllText(saveFile, "garbage1");
            File.WriteAllText(backupFile, "garbage2");
            UserProfileData fallback = SaveManager.LoadFromFile<UserProfileData>(saveFile);
            Assert.IsNull(fallback, "When both primary and backup are corrupt, return null safely");
        }
    }
}
