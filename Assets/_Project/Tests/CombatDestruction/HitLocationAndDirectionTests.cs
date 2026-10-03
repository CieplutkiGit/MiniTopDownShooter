using NUnit.Framework;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class HitLocationAndDirectionTests
    {
        private GameObject _targetGo;
        private HealthComponent _health;

        [SetUp]
        public void SetUp()
        {
            _targetGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetGo.name = "TestTarget";
            _targetGo.transform.position = new Vector3(0f, 1f, 5f);
            _health = _targetGo.AddComponent<HealthComponent>();
            _health.SetMaxHealth(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (_targetGo != null)
            {
                Object.DestroyImmediate(_targetGo);
            }
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [Test]
        public void HealthComponent_RecordsLastHitContext()
        {
            Vector3 expectedPoint = new Vector3(0f, 1f, 4.5f);
            Vector3 expectedNormal = Vector3.back;
            Vector3 expectedDir = Vector3.forward;

            HitContext context = new HitContext(expectedPoint, expectedNormal, expectedDir);
            _health.TakeDamage(new DamageData(20), context);

            Assert.AreEqual(expectedPoint, _health.LastHit.Point);
            Assert.AreEqual(expectedNormal, _health.LastHit.Normal);
            Assert.AreEqual(expectedDir, _health.LastHit.Direction);
        }

        [Test]
        public void HitscanDelivery_FeedsActualHitLocation()
        {
            HitscanWeaponDelivery delivery = new HitscanWeaponDelivery(
                range: 20f,
                mask: ~0,
                maxPenetrations: 0,
                projectilesPerShot: 1);

            GameObject shooterGo = new GameObject("Shooter");
            shooterGo.transform.position = new Vector3(0f, 1f, 0f);
            Physics.SyncTransforms();

            delivery.Deliver(
                shooterGo.transform,
                Vector3.forward,
                25,
                null,
                shooterGo.transform,
                null);

            Assert.AreEqual(75, _health.CurrentHealth);
            Assert.AreEqual(Vector3.forward, _health.LastHit.Direction);
            // Front face of cube at Z=5 is at Z=4.5
            Assert.AreEqual(4.5f, _health.LastHit.Point.z, 0.05f);

            Object.DestroyImmediate(shooterGo);
        }

        [Test]
        public void ImpactMark_AlignsWithHitNormalAndDirection()
        {
            Vector3 hitPoint = new Vector3(1f, 2f, 3f);
            Vector3 hitNormal = Vector3.up;
            Vector3 hitDir = new Vector3(0f, -1f, 1f).normalized;

            CombatImpactMark mark = CombatImpactPool.SpawnMark(hitPoint, hitNormal, hitDir, _targetGo.transform);

            Assert.IsNotNull(mark);
            Assert.AreEqual(_targetGo.transform, mark.TargetTransform);
            // Mark position sits on surface
            Assert.AreEqual(hitPoint.x, mark.transform.position.x, 0.02f);
            Assert.AreEqual(hitPoint.z, mark.transform.position.z, 0.02f);
        }
    }
}
