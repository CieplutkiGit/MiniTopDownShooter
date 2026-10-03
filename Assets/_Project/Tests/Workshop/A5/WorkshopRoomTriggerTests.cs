using Application;
using Game;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.A5
{
    public class WorkshopRoomTriggerTests
    {
        private GameObject _root;
        private GameStateController _stateController;
        private GameObject _playerGo;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("WorkshopRoomTestRoot");
            _stateController = _root.AddComponent<GameStateController>();

            _playerGo = new GameObject("Player");
            _playerGo.tag = "Player";
            _playerGo.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
        }

        [Test]
        public void WorkshopBenchTrigger_ShowsPrompt_WhenPlayerEntersInRoaming()
        {
            var benchGo = new GameObject("BenchTrigger");
            benchGo.transform.SetParent(_root.transform);
            var trigger = benchGo.AddComponent<WorkshopBenchTrigger>();
            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(benchGo.transform);

            trigger.Initialize(_stateController, promptObj);
            _stateController.EnterWorkshopRoaming();

            Assert.IsFalse(trigger.IsPlayerInside);
            Assert.IsFalse(trigger.IsPromptVisible);

            // Simulate player entering zone
            var collider = _playerGo.GetComponent<BoxCollider>();
            benchGo.SendMessage("OnTriggerEnter", collider, SendMessageOptions.DontRequireReceiver);

            Assert.IsTrue(trigger.IsPlayerInside);
            Assert.IsTrue(trigger.IsPromptVisible);
            Assert.IsTrue(promptObj.activeSelf);
            Assert.AreEqual("Press E / Tap to Customize", trigger.PromptText);

            // Simulate player leaving zone
            benchGo.SendMessage("OnTriggerExit", collider, SendMessageOptions.DontRequireReceiver);

            Assert.IsFalse(trigger.IsPlayerInside);
            Assert.IsFalse(trigger.IsPromptVisible);
            Assert.IsFalse(promptObj.activeSelf);
        }

        [Test]
        public void WorkshopBenchTrigger_InteractTransitionsToEditing_AndExitReturnsToRoaming()
        {
            var benchGo = new GameObject("BenchTrigger");
            benchGo.transform.SetParent(_root.transform);
            var trigger = benchGo.AddComponent<WorkshopBenchTrigger>();
            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(benchGo.transform);

            trigger.Initialize(_stateController, promptObj);
            _stateController.EnterWorkshopRoaming();

            var collider = _playerGo.GetComponent<BoxCollider>();
            benchGo.SendMessage("OnTriggerEnter", collider, SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual(GameState.WorkshopRoaming, _stateController.CurrentState);
            Assert.IsTrue(trigger.IsPromptVisible);

            // Player interacts
            trigger.Interact();

            Assert.AreEqual(GameState.WorkshopEditing, _stateController.CurrentState);
            Assert.IsFalse(trigger.IsPromptVisible);

            // Exit workbench
            trigger.ExitBench();

            Assert.AreEqual(GameState.WorkshopRoaming, _stateController.CurrentState);
            Assert.IsTrue(trigger.IsPromptVisible); // Player still inside zone
        }

        [Test]
        public void WorkshopFiringRangeTrigger_TransitionsToFiringRange_AndExitsToRoaming()
        {
            var rangeGo = new GameObject("RangeTrigger");
            rangeGo.transform.SetParent(_root.transform);
            var trigger = rangeGo.AddComponent<WorkshopFiringRangeTrigger>();
            trigger.Initialize(_stateController);

            _stateController.EnterWorkshopRoaming();
            Assert.AreEqual(GameState.WorkshopRoaming, _stateController.CurrentState);
            Assert.IsFalse(trigger.IsPlayerInside);

            // Enter zone
            trigger.EnterRange();

            Assert.IsTrue(trigger.IsPlayerInside);
            Assert.AreEqual(GameState.WorkshopFiringRange, _stateController.CurrentState);

            // Exit zone
            trigger.ExitRange();

            Assert.IsFalse(trigger.IsPlayerInside);
            Assert.AreEqual(GameState.WorkshopRoaming, _stateController.CurrentState);
        }

        [Test]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.WorkshopRoaming)]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.WorkshopFiringRange)]
        public void PauseInputController_TogglePause_PreservesOriginState(GameState originState)
        {
            var pauseGo = new GameObject("PauseInputTest");
            pauseGo.transform.SetParent(_root.transform);
            var pauseController = pauseGo.AddComponent<PauseInputController>();

            switch (originState)
            {
                case GameState.Playing:
                    _stateController.StartGame();
                    break;
                case GameState.WorkshopRoaming:
                    _stateController.EnterWorkshopRoaming();
                    break;
                case GameState.WorkshopEditing:
                    _stateController.EnterWorkshopRoaming();
                    _stateController.EnterWorkshopEditing();
                    break;
                case GameState.WorkshopFiringRange:
                    _stateController.EnterWorkshopRoaming();
                    _stateController.EnterWorkshopFiringRange();
                    break;
            }

            Assert.AreEqual(originState, _stateController.CurrentState);

            // Toggle pause -> paused
            pauseController.TogglePause();
            Assert.AreEqual(GameState.Paused, _stateController.CurrentState);

            // Toggle pause again -> resume to origin
            pauseController.TogglePause();
            Assert.AreEqual(originState, _stateController.CurrentState);
        }
    }
}
