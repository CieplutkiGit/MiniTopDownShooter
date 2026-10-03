using System.Collections;
using Application;
using Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniTopDownShooter.PlayModeTests
{
    public class GameLifecycleIntegrationTests
    {
        [UnityTest]
        public IEnumerator ArenaShowcase_Lifecycle_PauseFreezesTime_AndVictoryFreezesTime()
        {
            yield return LoadScene("ArenaShowcase");
            yield return null;

            GameStateController gameState = Object.FindFirstObjectByType<GameStateController>();
            Assert.IsNotNull(gameState, "GameStateController should exist in ArenaShowcase.");

            TimeController timeController = Object.FindFirstObjectByType<TimeController>();
            Assert.IsNotNull(timeController, "TimeController should exist in ArenaShowcase.");

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "PlayerController should exist in ArenaShowcase.");

            // Start the game
            gameState.StartGame();
            yield return null;

            Assert.AreEqual(GameState.Playing, gameState.CurrentState);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);

            // Pause freezes time
            gameState.Pause();
            yield return null;

            Assert.AreEqual(GameState.Paused, gameState.CurrentState);
            Assert.AreEqual(0f, Time.timeScale, 0.001f);

            // Resume restores time
            gameState.Resume();
            yield return null;

            Assert.AreEqual(GameState.Playing, gameState.CurrentState);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);

            // Trigger Victory freezes time
            gameState.TriggerVictory();
            yield return null;

            Assert.AreEqual(GameState.Victory, gameState.CurrentState);
            Assert.AreEqual(0f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator ArenaShowcase_WorldResetManager_RestoresPlayerHealthAndState()
        {
            yield return LoadScene("ArenaShowcase");
            yield return null;

            GameStateController gameState = Object.FindFirstObjectByType<GameStateController>();
            WorldResetManager resetManager = Object.FindFirstObjectByType<WorldResetManager>();
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            HealthComponent playerHealth = player.GetComponent<HealthComponent>();

            Assert.IsNotNull(gameState);
            Assert.IsNotNull(resetManager);
            Assert.IsNotNull(player);
            Assert.IsNotNull(playerHealth);

            // Start game
            gameState.StartGame();
            yield return null;

            // Damage player
            playerHealth.TakeDamage(25);
            Assert.Less(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            // Simulate Game Over
            gameState.Pause(); // or end game
            player.ResetToSpawn();
            gameState.ReturnToMenu();
            yield return null;

            // Trigger Restart via WorldResetManager
            resetManager.Restart();
            yield return null;

            // Player health should be restored to max and game state back to Playing
            Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"Scene '{sceneName}' could not be loaded.");
            float timeout = Time.realtimeSinceStartup + 15f;
            while (!op.isDone)
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    Assert.Fail($"Timed out waiting for scene '{sceneName}' to load.");
                }
                yield return null;
            }
            yield return null;
        }
    }
}
