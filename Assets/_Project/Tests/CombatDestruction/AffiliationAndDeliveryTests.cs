using NUnit.Framework;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class AffiliationAndDeliveryTests
    {
        private GameObject _playerGo;
        private GameObject _allyGo;
        private GameObject _enemyGo;
        private HealthComponent _allyHealth;
        private HealthComponent _enemyHealth;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("PlayerSource");
            DamageAffiliation playerAff = _playerGo.AddComponent<DamageAffiliation>();
            playerAff.Configure(CombatTeam.Player, allowFriendlyFire: false, ignoreFriendlyCollisions: true);

            _allyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _allyGo.name = "AllyTarget";
            _allyGo.transform.position = new Vector3(2f, 0f, 0f);
            DamageAffiliation allyAff = _allyGo.AddComponent<DamageAffiliation>();
            allyAff.Configure(CombatTeam.Player, allowFriendlyFire: false, ignoreFriendlyCollisions: true);
            _allyHealth = _allyGo.AddComponent<HealthComponent>();
            _allyHealth.SetMaxHealth(100);

            _enemyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _enemyGo.name = "EnemyTarget";
            _enemyGo.transform.position = new Vector3(0f, 0f, 3f);
            DamageAffiliation enemyAff = _enemyGo.AddComponent<DamageAffiliation>();
            enemyAff.Configure(CombatTeam.Enemy);
            _enemyHealth = _enemyGo.AddComponent<HealthComponent>();
            _enemyHealth.SetMaxHealth(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            if (_allyGo != null) Object.DestroyImmediate(_allyGo);
            if (_enemyGo != null) Object.DestroyImmediate(_enemyGo);
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [Test]
        public void ExplosionDamage_DamagesEnemy_DoesNotDamageAllyWithoutFriendlyFire()
        {
            DamageAffiliation playerAff = _playerGo.GetComponent<DamageAffiliation>();

            ExplosionDamage.Detonate(
                Vector3.zero,
                radius: 5f,
                maxDamage: 50,
                sourceAffiliation: playerAff,
                sourceRoot: _playerGo.transform);

            // Enemy takes damage
            Assert.Less(_enemyHealth.CurrentHealth, 100);

            // Ally on same team does not take damage because friendly fire is false
            Assert.AreEqual(100, _allyHealth.CurrentHealth);
        }

        [Test]
        public void ExplosionDamage_RespectsMinimumDamageRatio()
        {
            DamageAffiliation playerAff = _playerGo.GetComponent<DamageAffiliation>();

            // Detonate at center, target is at distance 3, radius is 3.2 (near edge)
            ExplosionDamage.Detonate(
                Vector3.zero,
                radius: 3.2f,
                maxDamage: 100,
                sourceAffiliation: playerAff,
                sourceRoot: _playerGo.transform,
                minDamageRatio: 0.3f);

            int damageTaken = 100 - _enemyHealth.CurrentHealth;
            // Near edge of explosion, damage should still respect minimum ratio (~30)
            Assert.GreaterOrEqual(damageTaken, 25);
            Assert.LessOrEqual(damageTaken, 45);
        }
    }
}
