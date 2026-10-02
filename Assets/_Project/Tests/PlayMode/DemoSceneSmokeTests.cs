using System.Collections;
using Application;
using Core;
using Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniTopDownShooter.PlayModeTests
{
    public class DemoSceneSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator SampleScene_LoadsLegacyLearningPath()
        {
            yield return LoadScene("SampleScene");

            Assert.IsNotNull(
                Object.FindFirstObjectByType<PlayerController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<GameStateController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<EnemySpawner>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<WaveController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<Gun>());
        }

        [UnityTest]
        public IEnumerator ArenaShowcase_UsesAuthoredFrameworkData()
        {
            yield return LoadScene("ArenaShowcase");

            WaveController wave =
                Object.FindFirstObjectByType<WaveController>();

            Assert.IsNotNull(wave);
            Assert.IsNotNull(
                wave.WaveSet,
                "Arena showcase should use a reusable WaveSet.");

            SpawnZone[] zones =
                Object.FindObjectsByType<SpawnZone>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.GreaterOrEqual(
                zones.Length,
                3,
                "Arena showcase should demonstrate spawn zones.");

            Gun[] guns =
                Object.FindObjectsByType<Gun>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            bool hasDefinition = false;

            for (int i = 0; i < guns.Length; i++)
            {
                hasDefinition |= guns[i].Definition != null;
            }

            Assert.IsTrue(
                hasDefinition,
                "Arena showcase should demonstrate WeaponDefinition.");
        }

        [UnityTest]
        public IEnumerator MobileDemo_BuildsTouchInputPath()
        {
            yield return LoadScene("MobileDemo");
            yield return null;

            Assert.IsNotNull(
                Object.FindFirstObjectByType<MobileDemoControlsBootstrap>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<MobileInputState>());

            Assert.IsNotNull(
                GameObject.Find("MobileDemoControls"));
        }

        [UnityTest]
        public IEnumerator RealScene_FullLifecycle_PlayPauseResumeDeathVictoryRetry()
        {
            yield return LoadScene("ArenaShowcase");
            yield return null;

            GameStateController gameState = Object.FindFirstObjectByType<GameStateController>();
            Assert.IsNotNull(gameState, "GameStateController should exist in ArenaShowcase");

            WorldResetManager resetManager = Object.FindFirstObjectByType<WorldResetManager>();
            Assert.IsNotNull(resetManager, "WorldResetManager should exist in ArenaShowcase");

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "PlayerController should exist in ArenaShowcase");

            HealthComponent playerHealth = player.GetComponent<HealthComponent>();
            Assert.IsNotNull(playerHealth, "Player HealthComponent should exist");

            // 1. Play
            gameState.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gameState.CurrentState, "State should be Playing");
            Assert.AreEqual(1f, Time.timeScale, 0.01f);

            // 2. Pause & Resume
            gameState.Pause();
            yield return null;
            Assert.AreEqual(GameState.Paused, gameState.CurrentState, "State should be Paused");
            Assert.AreEqual(0f, Time.timeScale, 0.01f);

            gameState.Resume();
            yield return null;
            Assert.AreEqual(GameState.Playing, gameState.CurrentState, "State should be Playing after resume");
            Assert.AreEqual(1f, Time.timeScale, 0.01f);

            // 3. Death & Retry
            playerHealth.TakeDamage(new DamageData(playerHealth.Current));
            yield return null;
            Assert.AreEqual(GameState.GameOver, gameState.CurrentState, "State should be GameOver on player death");

            resetManager.Restart();
            gameState.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gameState.CurrentState, "State should be Playing after death retry");
            Assert.AreEqual(playerHealth.Max, playerHealth.Current, "Player health should be fully restored");

            // 4. Victory & Retry
            gameState.TriggerVictory();
            yield return null;
            Assert.AreEqual(GameState.Victory, gameState.CurrentState, "State should be Victory");

            resetManager.Restart();
            gameState.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gameState.CurrentState, "State should be Playing after victory retry");

            gameState.EndGame();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss_DeathDuringShockwaveWindup_PlayerHealthRemainsUnchanged()
        {
            GameObject playerGo = new GameObject("TestPlayer");
            playerGo.transform.position = Vector3.zero;
            HealthComponent playerHealth = playerGo.AddComponent<HealthComponent>();
            DamageAffiliation playerAffil = playerGo.AddComponent<DamageAffiliation>();
            playerAffil.Configure(CombatTeam.Player);
            playerHealth.SetMaxHealth(100);

            GameObject bossGo = new GameObject("TestBoss");
            bossGo.transform.position = new Vector3(2f, 0f, 0f);
            HealthComponent bossHealth = bossGo.AddComponent<HealthComponent>();
            DamageAffiliation bossAffil = bossGo.AddComponent<DamageAffiliation>();
            bossAffil.Configure(CombatTeam.Enemy);
            bossHealth.SetMaxHealth(100);

            BossPhaseController bossPhases = bossGo.AddComponent<BossPhaseController>();
            GameObject telegraphGo = new GameObject("Telegraph");
            telegraphGo.transform.SetParent(bossGo.transform, false);

            BossPhase phase = new BossPhase
            {
                PhaseName = "Phase 2 Enrage",
                EnterAtHealthFraction = 0.5f,
                TriggerShockwaveOnEnter = true,
                ShockwaveRadius = 8f,
                ShockwaveDamage = 50,
                ShockwaveWindup = 0.35f,
                ShockwaveRecovery = 0.1f,
                ShockwaveTelegraph = telegraphGo
            };

            var bpSer = new UnityEditor.SerializedObject(bossPhases);
            var phasesProp = bpSer.FindProperty("_phases");
            phasesProp.arraySize = 1;
            var p0 = phasesProp.GetArrayElementAtIndex(0);
            p0.FindPropertyRelative("PhaseName").stringValue = phase.PhaseName;
            p0.FindPropertyRelative("EnterAtHealthFraction").floatValue = phase.EnterAtHealthFraction;
            p0.FindPropertyRelative("TriggerShockwaveOnEnter").boolValue = phase.TriggerShockwaveOnEnter;
            p0.FindPropertyRelative("ShockwaveRadius").floatValue = phase.ShockwaveRadius;
            p0.FindPropertyRelative("ShockwaveDamage").intValue = phase.ShockwaveDamage;
            p0.FindPropertyRelative("ShockwaveWindup").floatValue = phase.ShockwaveWindup;
            p0.FindPropertyRelative("ShockwaveRecovery").floatValue = phase.ShockwaveRecovery;
            p0.FindPropertyRelative("ShockwaveTelegraph").objectReferenceValue = telegraphGo;
            bpSer.ApplyModifiedPropertiesWithoutUndo();

            bossPhases.Initialize(bossHealth);

            yield return null;

            bossHealth.TakeDamage(new DamageData(50));
            yield return null;

            Assert.IsTrue(telegraphGo.activeSelf, "Shockwave telegraph should activate on entering phase");

            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(100, playerHealth.Current, "Player should be full health during windup");

            bossHealth.TakeDamage(new DamageData(50));
            yield return null;

            Assert.AreEqual(0, bossHealth.Current, "Boss should be dead");
            Assert.IsFalse(telegraphGo.activeSelf, "Telegraph should deactivate immediately upon boss death");

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(100, playerHealth.Current, "Player health must remain unchanged when boss dies during windup");

            Object.Destroy(playerGo);
            Object.Destroy(bossGo);
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Single);

            Assert.IsNotNull(
                operation,
                $"Scene '{sceneName}' is not in build settings.");

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}
