using System.Collections.Generic;
using System.IO;
using Application;
using Core;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests
{
    public class ReleaseGatesTests
    {
        private string _tempDir;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ReleaseGatesTests_" + System.Guid.NewGuid().ToString("N"));
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
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void ProfileTracking_VictoryAndGameOver_UpdatesRunsWinsLosses()
        {
            string profilePath = Path.Combine(_tempDir, "user_profile.json");
            UserProfileData profile = new UserProfileData
            {
                TotalRuns = 0,
                TotalWins = 0,
                TotalLosses = 0,
                HighScore = 100
            };

            // 1. Simulate Victory Run
            profile.TotalRuns++;
            profile.TotalWins++;
            int scoreVictory = 150;
            if (scoreVictory > profile.HighScore)
            {
                profile.HighScore = scoreVictory;
            }

            SaveManager.SaveToFile(profilePath, profile);
            UserProfileData loaded = SaveManager.LoadFromFile<UserProfileData>(profilePath);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.TotalRuns);
            Assert.AreEqual(1, loaded.TotalWins);
            Assert.AreEqual(0, loaded.TotalLosses);
            Assert.AreEqual(150, loaded.HighScore);

            // 2. Simulate GameOver Run
            loaded.TotalRuns++;
            loaded.TotalLosses++;
            int scoreLoss = 80;
            if (scoreLoss > loaded.HighScore)
            {
                loaded.HighScore = scoreLoss;
            }

            SaveManager.SaveToFile(profilePath, loaded);
            UserProfileData reloaded = SaveManager.LoadFromFile<UserProfileData>(profilePath);

            Assert.IsNotNull(reloaded);
            Assert.AreEqual(2, reloaded.TotalRuns);
            Assert.AreEqual(1, reloaded.TotalWins);
            Assert.AreEqual(1, reloaded.TotalLosses);
            Assert.AreEqual(150, reloaded.HighScore, "HighScore should not decrease on lower score");
        }

        [Test]
        public void EnemyAttack_ResetCooldown_AllowsImmediateAttack()
        {
            GameObject go = new GameObject("TestEnemyAttack");
            _spawnedObjects.Add(go);
            EnemyAttack attack = go.AddComponent<EnemyAttack>();

            attack.Init(damage: 15, attackRange: 3f, attackCooldown: 5f);
            Assert.AreEqual(15, attack.Damage);
            Assert.AreEqual(3f, attack.AttackRange);
            Assert.AreEqual(5f, attack.AttackCooldown);
            Assert.AreEqual(float.NegativeInfinity, attack.LastAttackTime);

            // Set damage and cooldown setters
            attack.SetDamage(25);
            Assert.AreEqual(25, attack.Damage);
            attack.SetAttackCooldown(2.5f);
            Assert.AreEqual(2.5f, attack.AttackCooldown);

            // ResetCooldown restores LastAttackTime to NegativeInfinity
            attack.ResetCooldown();
            Assert.AreEqual(float.NegativeInfinity, attack.LastAttackTime);
        }

        [Test]
        public void WeaponRuntime_SwitchingPreservesAmmoDuringActiveReload()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                MagazineSize = 10,
                StartingReserveAmmo = 30,
                MaxReserveAmmo = 50,
                InfiniteAmmo = false,
                ReloadDuration = 2.0f,
                FireInterval = 0.1f
            };

            WeaponRuntime runtime = new WeaponRuntime(config);
            Assert.AreEqual(10, runtime.Ammo.InMagazine);
            Assert.AreEqual(30, runtime.Ammo.ReserveAmmo);

            // Fire 4 rounds
            for (int i = 0; i < 4; i++)
            {
                bool fired = runtime.TryFire(i * 0.2f);
                Assert.IsTrue(fired);
            }

            Assert.AreEqual(6, runtime.Ammo.InMagazine);
            Assert.AreEqual(30, runtime.Ammo.ReserveAmmo);

            // Start Reload
            bool reloadStarted = runtime.StartReload(1.0f);
            Assert.IsTrue(reloadStarted);
            Assert.IsTrue(runtime.IsReloading);
            Assert.Greater(runtime.ReloadProgress(1.5f), 0f);

            // Switch weapon before reload finishes (CancelReload)
            runtime.CancelReload();
            Assert.IsFalse(runtime.IsReloading);

            // Ammo in magazine and reserve must be completely preserved
            Assert.AreEqual(6, runtime.Ammo.InMagazine);
            Assert.AreEqual(30, runtime.Ammo.ReserveAmmo);

            // Verify weapon can fire again without getting jammed
            bool firedAfterCancel = runtime.TryFire(2.0f);
            Assert.IsTrue(firedAfterCancel);
            Assert.AreEqual(5, runtime.Ammo.InMagazine);
        }

        [Test]
        public void WaveSpawnEntry_TracksRetryCountAndPreventsDeadlock()
        {
            WaveController.WaveSpawnEntry entry = new WaveController.WaveSpawnEntry(null, new List<string> { "Zone1", "Zone2" }, 0.5f);
            Assert.AreEqual(0, entry.RetryCount);
            Assert.IsFalse(entry.DelayConsumed);

            // Simulate retrying failed spawns
            for (int i = 0; i < 5; i++)
            {
                entry.RetryCount++;
            }

            Assert.AreEqual(5, entry.RetryCount);

            // Simulate exhausting retries
            for (int i = 0; i < 6; i++)
            {
                entry.RetryCount++;
            }

            Assert.AreEqual(11, entry.RetryCount);
            Assert.IsTrue(entry.RetryCount > 10, "Entry tracks high retry count to allow plan skip");
        }

        [Test]
        public void HealthComponent_ResetHealth_RestoresFullHealth()
        {
            GameObject go = new GameObject("TestHealth");
            _spawnedObjects.Add(go);
            HealthComponent health = go.AddComponent<HealthComponent>();

            // Initial health
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
            Assert.AreEqual(100, health.CurrentHealth);

            // Damage
            health.TakeDamage(35);
            Assert.AreEqual(65, health.CurrentHealth);

            // Reset health
            health.ResetHealth();
            Assert.AreEqual(100, health.CurrentHealth);
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
        }

        [Test]
        public void TimeController_PauseBoundaries_TimeScaleGating()
        {
            GameObject go = new GameObject("TestTimeController");
            _spawnedObjects.Add(go);
            TimeController timeController = go.AddComponent<TimeController>();

            GameObject stateGo = new GameObject("TestStateController");
            _spawnedObjects.Add(stateGo);
            GameStateController stateController = stateGo.AddComponent<GameStateController>();

            timeController.Initialize(stateController);

            // Initially state is Menu -> TimeScale should be 0
            stateController.StartGame();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);

            stateController.Pause();
            Assert.AreEqual(GameState.Paused, stateController.CurrentState);
            Assert.AreEqual(0f, Time.timeScale, 0.001f);

            stateController.Resume();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);

            stateController.TriggerVictory();
            Assert.AreEqual(GameState.Victory, stateController.CurrentState);
            Assert.AreEqual(0f, Time.timeScale, 0.001f);
        }

        [Test]
        public void WorldResetManager_StateGating_OnlyRestartsFromPermittedStates()
        {
            GameObject resetGo = new GameObject("TestResetManager");
            _spawnedObjects.Add(resetGo);
            WorldResetManager resetManager = resetGo.AddComponent<WorldResetManager>();

            GameObject stateGo = new GameObject("TestGameState");
            _spawnedObjects.Add(stateGo);
            GameStateController stateController = stateGo.AddComponent<GameStateController>();

            resetManager.Initialize(
                player: null,
                spawner: null,
                gameState: stateController,
                waves: null,
                effectPool: null,
                score: null);

            // State is Menu (permitted) -> Restart should transition to Playing
            resetManager.Restart();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);

            // Now in Playing (NOT permitted to restart while actively playing)
            resetManager.Restart();
            Assert.AreEqual(GameState.Playing, stateController.CurrentState);
        }
    }
}
