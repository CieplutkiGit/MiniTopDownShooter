using Application.Economy;
using Game;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyPickupLifecycleTests
    {
        private GameObject _rootGo;
        private SalvagePickupPool _pool;
        private EnemySalvageDropManager _dropManager;
        private RunSalvageTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _rootGo = new GameObject("PickupTestsRoot");
            _pool = _rootGo.AddComponent<SalvagePickupPool>();
            _pool.Initialize();
            _dropManager = _rootGo.AddComponent<EnemySalvageDropManager>();
            _tracker = _rootGo.AddComponent<RunSalvageTracker>();
            _tracker.Initialize();

            _dropManager.Initialize(_pool);
            _tracker.BindPool(_pool);
        }

        [TearDown]
        public void TearDown()
        {
            RunSalvageTracker.ResetForTesting();
            if (_rootGo != null)
            {
                Object.DestroyImmediate(_rootGo);
            }
        }

        [Test]
        public void PickupPool_SpawnAndCollect_RecyclesPickupWithoutLeaking()
        {
            int initialCreated = _pool.TotalCreatedCount;
            Assert.GreaterOrEqual(initialCreated, 1);

            // Spawn pickup
            SalvagePickup pickup = _pool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 1);
            Assert.IsNotNull(pickup);
            Assert.IsTrue(pickup.gameObject.activeSelf);
            Assert.AreEqual(1, _pool.ActiveCount);

            // Collect pickup
            pickup.Collect();

            Assert.IsFalse(pickup.gameObject.activeSelf);
            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(1, _tracker.ScrapCollected);
        }

        [Test]
        public void EnemyDropManager_SpawnsPhysicalSalvage_OnEnemyDeath()
        {
            GameObject enemyGo = new GameObject("EnemyInstance");
            enemyGo.AddComponent<EnemyMovement>();
            enemyGo.AddComponent<EnemyAttack>();
            enemyGo.AddComponent<HealthComponent>();
            EnemyController enemy = enemyGo.AddComponent<EnemyController>();

            _dropManager.RegisterEnemy(enemy);

            int startActive = _pool.ActiveCount;
            _dropManager.HandleEnemyDied(enemy);

            Assert.Greater(_pool.ActiveCount, startActive, "Salvage pickups must be spawned on enemy death");

            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void EnemyDropManager_PreventsDuplicateDrops_OnSamePooledEnemyDeathFrame()
        {
            GameObject enemyGo = new GameObject("PooledEnemy");
            enemyGo.AddComponent<EnemyMovement>();
            enemyGo.AddComponent<EnemyAttack>();
            enemyGo.AddComponent<HealthComponent>();
            EnemyController enemy = enemyGo.AddComponent<EnemyController>();

            _dropManager.RegisterEnemy(enemy);

            _dropManager.HandleEnemyDied(enemy);
            int activeAfterFirst = _pool.ActiveCount;

            // Trigger death again in same frame/cycle (e.g. from pooled duplicate invoke)
            _dropManager.HandleEnemyDied(enemy);
            int activeAfterSecond = _pool.ActiveCount;

            Assert.AreEqual(activeAfterFirst, activeAfterSecond, "Pooled enemy must not produce duplicate drops for the same death event");

            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void RunSalvageTracker_TracksVariousComponentTypes()
        {
            _tracker.ResetTracker();

            SalvagePickup scrapPickup = _pool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 2);
            SalvagePickup alloyPickup = _pool.Spawn(Vector3.zero, EconomyComponentType.Alloy, 1);
            SalvagePickup corePickup = _pool.Spawn(Vector3.zero, EconomyComponentType.Core, 1);

            scrapPickup.Collect();
            alloyPickup.Collect();
            corePickup.Collect();

            Assert.AreEqual(2, _tracker.ScrapCollected);
            Assert.AreEqual(1, _tracker.AlloyCollected);
            Assert.AreEqual(1, _tracker.CoreCollected);
            Assert.AreEqual(4, _tracker.TotalComponentsCollected);
        }

        [Test]
        public void PickupPool_UnderCapacityPressure_MergesAmountsInsteadOfDiscarding()
        {
            var smallPoolGo = new GameObject("SmallPool");
            var smallPool = smallPoolGo.AddComponent<SalvagePickupPool>();
            typeof(SalvagePickupPool).GetField("_maxPoolSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(smallPool, 2);

            SalvagePickup p1 = smallPool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 1);
            SalvagePickup p2 = smallPool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 2);

            Assert.AreEqual(2, smallPool.ActiveCount);

            SalvagePickup p3 = smallPool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 5);

            Assert.IsNotNull(p3);
            Assert.AreEqual(2, smallPool.ActiveCount);
            Assert.IsTrue(p1.Amount == 6 || p2.Amount == 7);

            Object.DestroyImmediate(smallPoolGo);
        }

        [Test]
        public void PickupPool_ReleaseTwice_DoesNotQueuePickupTwice()
        {
            SalvagePickup pickup = _pool.Spawn(Vector3.zero, EconomyComponentType.Scrap, 1);
            int availableBefore = _pool.AvailableCount;

            _pool.Release(pickup);
            int availableAfterFirst = _pool.AvailableCount;
            Assert.AreEqual(availableBefore + 1, availableAfterFirst);

            _pool.Release(pickup);
            int availableAfterSecond = _pool.AvailableCount;
            Assert.AreEqual(availableAfterFirst, availableAfterSecond);
        }

        [Test]
        public void Teardown_Bootstrapper_ResetForTesting_CleansAllStateWithoutError()
        {
            EconomyProductionBootstrapper.WireProductionPolicy();
            Assert.IsNotNull(WeaponLoadout.ActivePolicy);

            EconomyProductionBootstrapper.ResetForTesting();
            Assert.IsNull(WeaponLoadout.ActivePolicy);
        }
    }
}
