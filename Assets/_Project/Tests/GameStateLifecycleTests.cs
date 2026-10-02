using System.Collections.Generic;
using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class GameStateLifecycleTests
    {
        [Test]
        public void InitialState_IsMenu()
        {
            GameStateManager manager = new GameStateManager();
            Assert.AreEqual(GameState.Menu, manager.CurrentState);
        }

        [Test]
        public void CompleteLifecycle_MenuToPlayingToPausedToVictory()
        {
            GameStateManager manager = new GameStateManager();
            List<(GameState oldState, GameState newState)> transitions = new List<(GameState, GameState)>();

            manager.OnStateChanged += (o, n) => transitions.Add((o, n));

            // Menu -> Playing
            manager.StartGame();
            Assert.AreEqual(GameState.Playing, manager.CurrentState);

            // Playing -> Paused
            manager.Pause();
            Assert.AreEqual(GameState.Paused, manager.CurrentState);

            // Paused -> Playing
            manager.Resume();
            Assert.AreEqual(GameState.Playing, manager.CurrentState);

            // Playing -> Victory
            manager.TriggerVictory();
            Assert.AreEqual(GameState.Victory, manager.CurrentState);

            Assert.AreEqual(4, transitions.Count);
            Assert.AreEqual((GameState.Menu, GameState.Playing), transitions[0]);
            Assert.AreEqual((GameState.Playing, GameState.Paused), transitions[1]);
            Assert.AreEqual((GameState.Paused, GameState.Playing), transitions[2]);
            Assert.AreEqual((GameState.Playing, GameState.Victory), transitions[3]);
        }

        [Test]
        public void RestartFromVictory_EntersPlaying()
        {
            GameStateManager manager = new GameStateManager();
            manager.StartGame();
            manager.TriggerVictory();
            Assert.AreEqual(GameState.Victory, manager.CurrentState);

            manager.StartGame();
            Assert.AreEqual(GameState.Playing, manager.CurrentState);
        }

        [Test]
        public void RestartFromGameOver_EntersPlaying()
        {
            GameStateManager manager = new GameStateManager();
            manager.StartGame();
            manager.EndGame();
            Assert.AreEqual(GameState.GameOver, manager.CurrentState);

            manager.StartGame();
            Assert.AreEqual(GameState.Playing, manager.CurrentState);
        }

        [Test]
        public void ReturnToMenu_TransitionsFromAnyActiveState()
        {
            GameStateManager manager = new GameStateManager();
            manager.StartGame();
            manager.ReturnToMenu();
            Assert.AreEqual(GameState.Menu, manager.CurrentState);

            manager.StartGame();
            manager.Pause();
            manager.ReturnToMenu();
            Assert.AreEqual(GameState.Menu, manager.CurrentState);

            manager.StartGame();
            manager.TriggerVictory();
            manager.ReturnToMenu();
            Assert.AreEqual(GameState.Menu, manager.CurrentState);
        }
    }
}
