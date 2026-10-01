using Core;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class HealthTests
    {
        [Test]
        public void TakeDamage_ReducesCurrentHealthAndRaisesChanged()
        {
            Health health = new Health(100);
            int observedCurrent = -1;
            int observedMax = -1;

            health.OnHealthChanged += (current, max) =>
            {
                observedCurrent = current;
                observedMax = max;
            };

            health.TakeDamage(new DamageData(25));

            Assert.AreEqual(75, health.Current);
            Assert.AreEqual(75, observedCurrent);
            Assert.AreEqual(100, observedMax);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void LethalDamage_ClampsAtZeroAndRaisesDeadOnce()
        {
            Health health = new Health(10);
            int deathCount = 0;
            health.OnDead += () => deathCount++;

            health.TakeDamage(new DamageData(50));
            health.TakeDamage(new DamageData(50));

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deathCount);
        }

        [Test]
        public void Reset_RestoresMaxHealthAndAliveState()
        {
            Health health = new Health(20);
            health.TakeDamage(new DamageData(20));

            health.Reset();

            Assert.AreEqual(20, health.Current);
            Assert.AreEqual(20, health.Max);
            Assert.IsFalse(health.IsDead);
        }
    }
}
