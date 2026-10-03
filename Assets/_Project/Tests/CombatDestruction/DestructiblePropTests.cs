using NUnit.Framework;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class DestructiblePropTests
    {
        private GameObject _propGo;
        private DestructibleProp _prop;
        private BoxCollider _collider;
        private MeshRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _propGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _propGo.name = "TestCoverProp";
            _collider = _propGo.GetComponent<BoxCollider>();
            _renderer = _propGo.GetComponent<MeshRenderer>();
            _prop = _propGo.AddComponent<DestructibleProp>();
            _prop.InitializeProp();
            _prop.Configure(60, debrisStage: 3, debrisDeath: 8);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (_propGo != null)
            {
                Object.DestroyImmediate(_propGo);
            }
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [Test]
        public void Prop_TakesDamage_ReducesHealth()
        {
            _prop.TakeDamage(new DamageData(20));
            Assert.AreEqual(40, _prop.CurrentHealth);
            Assert.IsFalse(_prop.IsDestroyed);
        }

        [Test]
        public void ZeroHealth_DestroysProp_DisablesCollider()
        {
            bool destroyedEventFired = false;
            _prop.OnDestroyed += p => destroyedEventFired = true;

            _prop.TakeDamage(new DamageData(60));

            Assert.AreEqual(0, _prop.CurrentHealth);
            Assert.IsTrue(_prop.IsDestroyed);
            Assert.IsTrue(destroyedEventFired);
            Assert.IsFalse(_collider.enabled);
            Assert.IsFalse(_propGo.activeSelf);
        }

        [Test]
        public void ResetProp_RestoresToPristine()
        {
            _prop.TakeDamage(new DamageData(60));
            Assert.IsTrue(_prop.IsDestroyed);

            _prop.ResetProp();

            Assert.IsFalse(_prop.IsDestroyed);
            Assert.AreEqual(60, _prop.CurrentHealth);
            Assert.AreEqual(0, _prop.CurrentStage);
            Assert.IsTrue(_collider.enabled);
            Assert.IsTrue(_propGo.activeSelf);
        }

        [Test]
        public void StructuralSurfaces_CannotBeDestructibleProps()
        {
            GameObject floorGo = new GameObject("Floor");
            floorGo.AddComponent<BoxCollider>();

            GameObject boundsGo = new GameObject("WorldBounds_North");
            boundsGo.AddComponent<BoxCollider>();

            Assert.IsFalse(CombatDestructionInstaller.IsSuitableDestructibleProp(floorGo));
            Assert.IsFalse(CombatDestructionInstaller.IsSuitableDestructibleProp(boundsGo));

            Object.DestroyImmediate(floorGo);
            Object.DestroyImmediate(boundsGo);
        }
    }
}
