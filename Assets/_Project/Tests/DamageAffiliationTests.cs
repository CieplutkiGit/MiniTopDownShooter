using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests
{
    public class DamageAffiliationTests
    {
        private GameObject _sourceObject;
        private GameObject _targetObject;
        private DamageAffiliation _source;
        private DamageAffiliation _target;

        [SetUp]
        public void SetUp()
        {
            _sourceObject = new GameObject("Source");
            _targetObject = new GameObject("Target");
            _source = _sourceObject.AddComponent<DamageAffiliation>();
            _target = _targetObject.AddComponent<DamageAffiliation>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sourceObject);
            Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void DifferentTeams_CanDamageEachOther()
        {
            _source.Configure(CombatTeam.Player);
            _target.Configure(CombatTeam.Enemy);

            Assert.IsTrue(_source.CanDamage(_target));
        }

        [Test]
        public void SameTeam_DefaultsToNoFriendlyFire()
        {
            _source.Configure(CombatTeam.Enemy);
            _target.Configure(CombatTeam.Enemy);

            Assert.IsFalse(_source.CanDamage(_target));
            Assert.IsTrue(
                DamageAffiliation.ShouldIgnoreFriendlyCollision(
                    _source,
                    CombatTeam.Enemy,
                    _target,
                    CombatTeam.Enemy));
        }

        [Test]
        public void FriendlyFire_CanBeEnabledPerSource()
        {
            _source.Configure(CombatTeam.Enemy, true);
            _target.Configure(CombatTeam.Enemy);

            Assert.IsTrue(_source.CanDamage(_target));
        }

        [Test]
        public void NeutralTargets_RemainDamageable()
        {
            _source.Configure(CombatTeam.Player);
            _target.Configure(CombatTeam.Neutral);

            Assert.IsTrue(_source.CanDamage(_target));
        }
    }
}
