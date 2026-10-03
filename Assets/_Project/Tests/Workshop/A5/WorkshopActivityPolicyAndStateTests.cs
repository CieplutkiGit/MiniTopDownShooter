using Application;
using Application.Workshop;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.A5
{
    public class WorkshopActivityPolicyAndStateTests
    {
        [Test]
        public void GameActivityPolicy_EnforcesCorrectPermissions_ForAllStates()
        {
            // Menu
            Assert.IsFalse(GameActivityPolicy.CanMove(GameState.Menu));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.Menu));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.Menu));
            Assert.IsTrue(GameActivityPolicy.IsTimeFrozen(GameState.Menu));

            // Playing
            Assert.IsTrue(GameActivityPolicy.CanMove(GameState.Playing));
            Assert.IsTrue(GameActivityPolicy.CanFire(GameState.Playing));
            Assert.IsTrue(GameActivityPolicy.AdvancesRun(GameState.Playing));
            Assert.IsFalse(GameActivityPolicy.IsTimeFrozen(GameState.Playing));

            // Paused
            Assert.IsFalse(GameActivityPolicy.CanMove(GameState.Paused));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.Paused));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.Paused));
            Assert.IsTrue(GameActivityPolicy.IsTimeFrozen(GameState.Paused));

            // GameOver
            Assert.IsFalse(GameActivityPolicy.CanMove(GameState.GameOver));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.GameOver));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.GameOver));
            Assert.IsFalse(GameActivityPolicy.IsTimeFrozen(GameState.GameOver));

            // Victory
            Assert.IsFalse(GameActivityPolicy.CanMove(GameState.Victory));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.Victory));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.Victory));
            Assert.IsTrue(GameActivityPolicy.IsTimeFrozen(GameState.Victory));

            // WorkshopRoaming
            Assert.IsTrue(GameActivityPolicy.CanMove(GameState.WorkshopRoaming));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.WorkshopRoaming));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.WorkshopRoaming));
            Assert.IsFalse(GameActivityPolicy.IsTimeFrozen(GameState.WorkshopRoaming));

            // WorkshopEditing
            Assert.IsFalse(GameActivityPolicy.CanMove(GameState.WorkshopEditing));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.WorkshopEditing));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.WorkshopEditing));
            Assert.IsTrue(GameActivityPolicy.IsTimeFrozen(GameState.WorkshopEditing));

            // WorkshopFiringRange
            Assert.IsTrue(GameActivityPolicy.CanMove(GameState.WorkshopFiringRange));
            Assert.IsTrue(GameActivityPolicy.CanFire(GameState.WorkshopFiringRange));
            Assert.IsFalse(GameActivityPolicy.AdvancesRun(GameState.WorkshopFiringRange));
            Assert.IsFalse(GameActivityPolicy.IsTimeFrozen(GameState.WorkshopFiringRange));
        }

        [Test]
        [TestCase(GameState.Menu, 0f)]
        [TestCase(GameState.Playing, 1f)]
        [TestCase(GameState.Paused, 0f)]
        [TestCase(GameState.GameOver, 1f)]
        [TestCase(GameState.Victory, 0f)]
        [TestCase(GameState.WorkshopRoaming, 1f)]
        [TestCase(GameState.WorkshopEditing, 0f)]
        [TestCase(GameState.WorkshopFiringRange, 1f)]
        public void TimeController_AppliesTimeScale_BasedOnActivityPolicy(GameState state, float expectedTimeScale)
        {
            var go = new GameObject("TimeControllerTest");
            var stateController = go.AddComponent<GameStateController>();
            var timeController = go.AddComponent<TimeController>();

            try
            {
                timeController.Initialize(stateController);

                switch (state)
                {
                    case GameState.Menu:
                        stateController.ReturnToMenu();
                        break;
                    case GameState.Playing:
                        stateController.StartGame();
                        break;
                    case GameState.Paused:
                        stateController.StartGame();
                        stateController.Pause();
                        break;
                    case GameState.GameOver:
                        stateController.StartGame();
                        stateController.EndGame();
                        break;
                    case GameState.Victory:
                        stateController.StartGame();
                        stateController.TriggerVictory();
                        break;
                    case GameState.WorkshopRoaming:
                        stateController.EnterWorkshopRoaming();
                        break;
                    case GameState.WorkshopEditing:
                        stateController.EnterWorkshopRoaming();
                        stateController.EnterWorkshopEditing();
                        break;
                    case GameState.WorkshopFiringRange:
                        stateController.EnterWorkshopRoaming();
                        stateController.EnterWorkshopFiringRange();
                        break;
                }

                Assert.AreEqual(expectedTimeScale, Time.timeScale, 0.001f);
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.WorkshopRoaming)]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.WorkshopFiringRange)]
        public void PauseAndResume_PreservesExactOriginState(GameState originState)
        {
            var manager = new GameStateManager();

            switch (originState)
            {
                case GameState.Playing:
                    manager.StartGame();
                    break;
                case GameState.WorkshopRoaming:
                    manager.EnterWorkshopRoaming();
                    break;
                case GameState.WorkshopEditing:
                    manager.EnterWorkshopEditing();
                    break;
                case GameState.WorkshopFiringRange:
                    manager.EnterWorkshopFiringRange();
                    break;
            }

            Assert.AreEqual(originState, manager.CurrentState);

            manager.Pause();
            Assert.AreEqual(GameState.Paused, manager.CurrentState);
            Assert.AreEqual(originState, manager.PreviousStateBeforePause);

            manager.Resume();
            Assert.AreEqual(originState, manager.CurrentState);
        }

        [Test]
        public void GameStateController_ExposesWorkshopTransitionsAndPreviousState()
        {
            var go = new GameObject("StateTest");
            var controller = go.AddComponent<GameStateController>();

            try
            {
                controller.EnterWorkshopRoaming();
                Assert.AreEqual(GameState.WorkshopRoaming, controller.CurrentState);

                controller.EnterWorkshopEditing();
                Assert.AreEqual(GameState.WorkshopEditing, controller.CurrentState);

                controller.Pause();
                Assert.AreEqual(GameState.Paused, controller.CurrentState);
                Assert.AreEqual(GameState.WorkshopEditing, controller.PreviousStateBeforePause);

                controller.Resume();
                Assert.AreEqual(GameState.WorkshopEditing, controller.CurrentState);

                controller.EnterWorkshopRoaming();
                controller.EnterWorkshopFiringRange();
                Assert.AreEqual(GameState.WorkshopFiringRange, controller.CurrentState);

                controller.Pause();
                Assert.AreEqual(GameState.Paused, controller.CurrentState);
                Assert.AreEqual(GameState.WorkshopFiringRange, controller.PreviousStateBeforePause);

                controller.Resume();
                Assert.AreEqual(GameState.WorkshopFiringRange, controller.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
