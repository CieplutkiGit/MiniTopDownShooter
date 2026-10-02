using Cieplutki.MiniTopDownShooter.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Tests
{
    public class MobileInputStateTests
    {
        private GameObject _gameObject;
        private MobileInputState _input;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("MobileInputStateTests");
            _input = _gameObject.AddComponent<MobileInputState>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void MoveDirection_IsClampedToUnitLength()
        {
            _input.SetMove(new Vector2(2f, 0f));

            Assert.AreEqual(Vector2.right, _input.MoveDirection);
        }

        [Test]
        public void AimToFire_QueuesSinglePressWhenThresholdIsCrossed()
        {
            _input.SetLook(Vector2.zero, true, 0.5f);
            _input.SetLook(Vector2.right * 0.75f, true, 0.5f);

            Assert.IsTrue(_input.FireHeld);
            Assert.IsTrue(_input.ConsumeFirePressed());
            Assert.IsFalse(_input.ConsumeFirePressed());
        }

        [Test]
        public void FireButton_HoldsAndReleasesFire()
        {
            _input.Press(MobileInputAction.Fire);

            Assert.IsTrue(_input.FireHeld);
            Assert.IsTrue(_input.ConsumeFirePressed());

            _input.Release(MobileInputAction.Fire);

            Assert.IsFalse(_input.FireHeld);
        }

        [Test]
        public void DiscreteActions_AreConsumedOnce()
        {
            _input.Press(MobileInputAction.Reload);
            _input.Press(MobileInputAction.NextWeapon);
            _input.Press(MobileInputAction.Pause);

            Assert.IsTrue(_input.ConsumeReloadPressed());
            Assert.IsFalse(_input.ConsumeReloadPressed());

            Assert.IsTrue(_input.ConsumeNextWeaponPressed());
            Assert.IsFalse(_input.ConsumeNextWeaponPressed());

            Assert.IsTrue(_input.ConsumePausePressed());
            Assert.IsFalse(_input.ConsumePausePressed());
        }
    }
}
