using System;
using System.Reflection;
using Game;
using Game.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniTopDownShooter.PlayModeTests
{
    public class ProjectileBudgetIntegrationTests
    {
        [Test]
        public void PrewarmAndRejectedPelletsDoNotLeakSlots_AndReturnsReleaseAllSlots()
        {
            Scene scene = SceneManager.CreateScene("ProjectileBudget_" + Guid.NewGuid().ToString("N"));
            var owner = new GameObject("BudgetOwner");
            SceneManager.MoveGameObjectToScene(owner, scene);
            var budget = owner.AddComponent<SceneObjectBudget>();
            var prefabObject = new GameObject("ProjectilePrefab");
            SceneManager.MoveGameObjectToScene(prefabObject, scene);
            prefabObject.SetActive(false);
            var prefab = prefabObject.AddComponent<Projectile>();
            ProjectileWeaponDelivery delivery = null;
            try
            {
                var cap = typeof(SceneObjectBudget).GetField("_maxActiveProjectiles", BindingFlags.Instance | BindingFlags.NonPublic);
                cap.SetValue(budget, 1);
                delivery = new ProjectileWeaponDelivery(prefab, 2, 4, 2, scene: scene);
                Assert.IsTrue(budget.HasProjectileCapacity, "Prewarm must not reserve active slots.");
                Assert.IsFalse(delivery.TryReserveShot(owner.transform));
                Assert.IsTrue(budget.HasProjectileCapacity, "A rejected pellet group must release partial reservations.");

                cap.SetValue(budget, 2);
                Assert.IsTrue(delivery.TryReserveShot(owner.transform));
                Assert.IsFalse(budget.HasProjectileCapacity);
                delivery.CancelReservedShot();
                Assert.IsTrue(budget.HasProjectileCapacity);

                Assert.IsTrue(delivery.TryReserveShot(owner.transform));
                delivery.Deliver(owner.transform, Vector3.forward, 10, null, null, null);
                Assert.IsFalse(budget.HasProjectileCapacity);
                delivery.ClearActiveProjectiles();
                Assert.IsTrue(budget.HasProjectileCapacity, "Returned projectiles must release their scene slots.");
            }
            finally
            {
                delivery?.Dispose();
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(prefabObject);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
