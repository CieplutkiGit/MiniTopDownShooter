using Application;
using Application.Workshop;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.A5
{
    public class WorkshopInputRearmTests
    {
        private GameObject _playerRoot;
        private MobileInputState _mobileInput;
        private InputReader _inputReader;

        [SetUp]
        public void SetUp()
        {
            _playerRoot = new GameObject("PlayerRoot");
            _mobileInput = _playerRoot.AddComponent<MobileInputState>();
            _inputReader = new InputReader(null, _playerRoot.transform, _mobileInput);
        }

        [TearDown]
        public void TearDown()
        {
            _inputReader?.Dispose();
            if (_playerRoot != null)
            {
                Object.DestroyImmediate(_playerRoot);
            }
        }

        [Test]
        public void InputReader_RequireNeutralToRearm_DisarmsUntilNeutral()
        {
            Assert.IsTrue(_inputReader.IsFireArmed);

            // Simulate fire held via mobile input
            _mobileInput.Press(MobileInputAction.Fire);
            Assert.IsTrue(_mobileInput.FireHeld);

            // Disarm on state transition
            _inputReader.RequireNeutralToRearm();
            Assert.IsFalse(_inputReader.IsFireArmed);

            // While fire is held, reading shoot state outputs false and remains disarmed
            _inputReader.ReadShootState(0.1f, out bool isHeld, out bool wasPressed);
            Assert.IsFalse(isHeld);
            Assert.IsFalse(wasPressed);
            Assert.IsFalse(_inputReader.IsShootHeld(0.1f));
            Assert.IsFalse(_inputReader.IsFireArmed);

            // Release fire to neutral
            _mobileInput.Release(MobileInputAction.Fire);
            Assert.IsFalse(_mobileInput.FireHeld);

            // Next read detects neutral and rearms!
            _inputReader.ReadShootState(0.1f, out isHeld, out wasPressed);
            Assert.IsTrue(_inputReader.IsFireArmed);
            Assert.IsFalse(isHeld);
            Assert.IsFalse(wasPressed);

            // Subsequent fire press works normally
            _mobileInput.Press(MobileInputAction.Fire);
            _inputReader.ReadShootState(0.1f, out isHeld, out wasPressed);
            Assert.IsTrue(isHeld);
            Assert.IsTrue(wasPressed);
        }

        [Test]
        public void InputReader_LookStickThreshold_RequiresNeutralBeforeRearm()
        {
            // Simulate look aim exceeding threshold
            _mobileInput.SetLook(Vector2.right, true, 0.5f);

            _inputReader.RequireNeutralToRearm();
            Assert.IsFalse(_inputReader.IsFireArmed);

            // Look stick still held -> cannot shoot
            _inputReader.ReadShootState(0.5f, out bool isHeld, out bool wasPressed);
            Assert.IsFalse(isHeld);
            Assert.IsFalse(_inputReader.IsFireArmed);

            // Release look stick to neutral
            _mobileInput.SetLook(Vector2.zero, false, 0.5f);

            // ReadShootState rearms
            _inputReader.ReadShootState(0.5f, out isHeld, out wasPressed);
            Assert.IsTrue(_inputReader.IsFireArmed);
        }

        [Test]
        public void PlayerController_StateTransition_TriggersNeutralRearm()
        {
            var stateGo = new GameObject("StateGo");
            var stateController = stateGo.AddComponent<GameStateController>();

            var playerGo = new GameObject("PlayerControllerTest");
            var movement = playerGo.AddComponent<PlayerMovement>();
            var rotation = playerGo.AddComponent<PlayerRotation>();
            var shoot = playerGo.AddComponent<PlayerShoot>();
            var health = playerGo.AddComponent<HealthComponent>();
            var player = playerGo.AddComponent<PlayerController>();

            try
            {
                player.Initialize(stateController, mobileInput: _mobileInput);

                // Start in Roaming
                stateController.EnterWorkshopRoaming();

                // Hold fire input
                _mobileInput.Press(MobileInputAction.Fire);

                // Transition to Firing Range
                stateController.EnterWorkshopFiringRange();

                // Input should be disarmed until released
                Assert.IsNotNull(player.Input);
                Assert.IsFalse(player.Input.IsFireArmed);

                player.Input.ReadShootState(0.1f, out bool isHeld, out bool wasPressed);
                Assert.IsFalse(isHeld);
                Assert.IsFalse(wasPressed);

                // Release to neutral
                _mobileInput.Release(MobileInputAction.Fire);
                player.Input.ReadShootState(0.1f, out isHeld, out wasPressed);
                Assert.IsTrue(player.Input.IsFireArmed);
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(stateGo);
            }
        }

        [Test]
        public void MobileInputState_InteractAction_PressedAndConsumed()
        {
            Assert.IsFalse(_mobileInput.ConsumeInteractPressed());

            _mobileInput.Press(MobileInputAction.Interact);
            Assert.IsTrue(_mobileInput.ConsumeInteractPressed());
            Assert.IsFalse(_mobileInput.ConsumeInteractPressed());
        }

        [Test]
        public void InputReader_ConsumeInteractPressed_ConsumesOnce()
        {
            Assert.IsFalse(_inputReader.ConsumeInteractPressed());

            _mobileInput.Press(MobileInputAction.Interact);
            Assert.IsTrue(_inputReader.ConsumeInteractPressed());
            Assert.IsFalse(_inputReader.ConsumeInteractPressed());
        }
    }
}
