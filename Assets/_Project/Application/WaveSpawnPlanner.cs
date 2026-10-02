using System;
using System.Collections.Generic;

namespace Application
{
    public interface IRandomProvider
    {
        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
        float Value { get; }
    }

    public class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                return minInclusive;
            }
            return _random.Next(minInclusive, maxExclusive);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return (float)(minInclusive + _random.NextDouble() * (maxInclusive - minInclusive));
        }

        public float Value => (float)_random.NextDouble();
    }

    public class SpawnGroupData<T>
    {
        public T Prefab { get; set; }
        public int GuaranteedCount { get; set; }
        public int Weight { get; set; } = 1;
        public float DelayBefore { get; set; }
    }

    public class WavePlanConfig<T>
    {
        public int EnemyCount { get; set; } = 5;
        public float SpawnInterval { get; set; } = 1f;
        public float InitialDelay { get; set; }
        public float DelayAfter { get; set; } = 3f;
        public List<SpawnGroupData<T>> EnemyGroups { get; set; } = new List<SpawnGroupData<T>>();
        public List<string> SpawnZoneIds { get; set; } = new List<string>();
        public T BossPrefab { get; set; }
        public int BossCount { get; set; }
        public float BossDelay { get; set; }
    }

    public class SpawnPlanItem<T>
    {
        public T Prefab { get; }
        public IReadOnlyList<string> ZoneIds { get; }
        public float DelayBefore { get; }

        public SpawnPlanItem(T prefab, IReadOnlyList<string> zoneIds, float delayBefore)
        {
            Prefab = prefab;
            ZoneIds = zoneIds;
            DelayBefore = delayBefore;
        }
    }

    public static class WaveSpawnPlanner
    {
        public static List<SpawnPlanItem<T>> BuildSpawnPlan<T>(
            WavePlanConfig<T> config,
            IRandomProvider random)
        {
            List<SpawnPlanItem<T>> plan = new List<SpawnPlanItem<T>>();
            if (config == null)
            {
                return plan;
            }

            if (random == null)
            {
                random = new SystemRandomProvider();
            }

            List<SpawnGroupData<T>> validGroups = new List<SpawnGroupData<T>>();
            if (config.EnemyGroups != null)
            {
                for (int i = 0; i < config.EnemyGroups.Count; i++)
                {
                    SpawnGroupData<T> g = config.EnemyGroups[i];
                    if (g != null && g.Prefab != null)
                    {
                        validGroups.Add(g);
                    }
                }
            }

            HashSet<SpawnGroupData<T>> delayApplied = new HashSet<SpawnGroupData<T>>();
            int baseEnemyCount = Math.Max(1, config.EnemyCount);

            if (validGroups.Count == 0)
            {
                for (int i = 0; i < baseEnemyCount; i++)
                {
                    plan.Add(new SpawnPlanItem<T>(default, config.SpawnZoneIds, 0f));
                }
            }
            else
            {
                for (int groupIndex = 0; groupIndex < validGroups.Count; groupIndex++)
                {
                    SpawnGroupData<T> group = validGroups[groupIndex];
                    int guaranteed = Math.Max(0, group.GuaranteedCount);

                    for (int i = 0; i < guaranteed; i++)
                    {
                        float delay = 0f;
                        if (!delayApplied.Contains(group))
                        {
                            delay = Math.Max(0f, group.DelayBefore);
                            delayApplied.Add(group);
                        }

                        plan.Add(new SpawnPlanItem<T>(group.Prefab, config.SpawnZoneIds, delay));
                    }
                }

                while (plan.Count < baseEnemyCount)
                {
                    SpawnGroupData<T> group = PickWeightedGroup(validGroups, random);
                    float delay = 0f;
                    if (!delayApplied.Contains(group))
                    {
                        delay = Math.Max(0f, group.DelayBefore);
                        delayApplied.Add(group);
                    }

                    plan.Add(new SpawnPlanItem<T>(group.Prefab, config.SpawnZoneIds, delay));
                }
            }

            if (config.BossPrefab != null && config.BossCount > 0)
            {
                for (int i = 0; i < config.BossCount; i++)
                {
                    float delay = i == 0 ? Math.Max(0f, config.BossDelay) : 0f;
                    plan.Add(new SpawnPlanItem<T>(config.BossPrefab, config.SpawnZoneIds, delay));
                }
            }

            return plan;
        }

        private static SpawnGroupData<T> PickWeightedGroup<T>(
            List<SpawnGroupData<T>> groups,
            IRandomProvider random)
        {
            int totalWeight = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                totalWeight += Math.Max(1, groups[i].Weight);
            }

            int roll = random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                cumulative += Math.Max(1, groups[i].Weight);
                if (roll < cumulative)
                {
                    return groups[i];
                }
            }

            return groups[groups.Count - 1];
        }
    }
}
