using System.Collections.Generic;
using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class MockRandomProvider : IRandomProvider
    {
        private readonly Queue<int> _rangeIntResults = new Queue<int>();
        private readonly Queue<float> _rangeFloatResults = new Queue<float>();

        public void EnqueueInt(int value) => _rangeIntResults.Enqueue(value);
        public void EnqueueFloat(float value) => _rangeFloatResults.Enqueue(value);

        public int Range(int minInclusive, int maxExclusive)
        {
            if (_rangeIntResults.Count > 0)
            {
                return _rangeIntResults.Dequeue();
            }
            return minInclusive;
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (_rangeFloatResults.Count > 0)
            {
                return _rangeFloatResults.Dequeue();
            }
            return minInclusive;
        }

        public float Value => 0.5f;
    }

    public class WaveSpawnPlannerTests
    {
        [Test]
        public void BuildSpawnPlan_EmptyGroups_GeneratesDefaultItems()
        {
            WavePlanConfig<string> config = new WavePlanConfig<string>
            {
                EnemyCount = 4,
                SpawnZoneIds = new List<string> { "ZoneA", "ZoneB" }
            };

            List<SpawnPlanItem<string>> plan = WaveSpawnPlanner.BuildSpawnPlan(config, new MockRandomProvider());

            Assert.AreEqual(4, plan.Count);
            for (int i = 0; i < plan.Count; i++)
            {
                Assert.IsNull(plan[i].Prefab);
                Assert.AreEqual(2, plan[i].ZoneIds.Count);
                Assert.AreEqual("ZoneA", plan[i].ZoneIds[0]);
                Assert.AreEqual(0f, plan[i].DelayBefore);
            }
        }

        [Test]
        public void BuildSpawnPlan_GuaranteedCounts_FulfilledFirst()
        {
            WavePlanConfig<string> config = new WavePlanConfig<string>
            {
                EnemyCount = 5,
                EnemyGroups = new List<SpawnGroupData<string>>
                {
                    new SpawnGroupData<string> { Prefab = "FastEnemy", GuaranteedCount = 2, Weight = 1 },
                    new SpawnGroupData<string> { Prefab = "TankEnemy", GuaranteedCount = 1, Weight = 1 }
                }
            };

            MockRandomProvider mockRng = new MockRandomProvider();
            // Remaining 2 enemies to reach count 5:
            mockRng.EnqueueInt(0); // Pick FastEnemy
            mockRng.EnqueueInt(1); // Pick TankEnemy

            List<SpawnPlanItem<string>> plan = WaveSpawnPlanner.BuildSpawnPlan(config, mockRng);

            Assert.AreEqual(5, plan.Count);
            // First 2 are FastEnemy (guaranteed)
            Assert.AreEqual("FastEnemy", plan[0].Prefab);
            Assert.AreEqual("FastEnemy", plan[1].Prefab);
            // 3rd is TankEnemy (guaranteed)
            Assert.AreEqual("TankEnemy", plan[2].Prefab);
            // 4th and 5th from weighted random
            Assert.AreEqual("FastEnemy", plan[3].Prefab);
            Assert.AreEqual("TankEnemy", plan[4].Prefab);
        }

        [Test]
        public void BuildSpawnPlan_BossAdded_WithConfiguredDelay()
        {
            WavePlanConfig<string> config = new WavePlanConfig<string>
            {
                EnemyCount = 2,
                BossPrefab = "BossEnemy",
                BossCount = 1,
                BossDelay = 4.5f
            };

            List<SpawnPlanItem<string>> plan = WaveSpawnPlanner.BuildSpawnPlan(config, new MockRandomProvider());

            Assert.AreEqual(3, plan.Count);
            // Last is the boss
            SpawnPlanItem<string> bossItem = plan[2];
            Assert.AreEqual("BossEnemy", bossItem.Prefab);
            Assert.AreEqual(4.5f, bossItem.DelayBefore);
        }
    }
}
