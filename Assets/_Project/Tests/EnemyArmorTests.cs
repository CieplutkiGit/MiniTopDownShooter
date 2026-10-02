using Cieplutki.MiniTopDownShooter.Core;
using Cieplutki.MiniTopDownShooter.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Tests
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
