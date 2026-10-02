using Core;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests
{
    public class EnemyArmorTests
    {
        [Test]
        public void DefaultArmor_ReducesDamageButKeepsMinimumDamage()
        {
            GameObject gameObject = new GameObject("EnemyArmorTests");

            try
            {
                EnemyArmor armor = gameObject.AddComponent<EnemyArmor>();
                DamageData result = armor.ModifyDamage(new DamageData(10));

                Assert.Greater(result.Damage, 0);
                Assert.Less(result.Damage, 10);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
