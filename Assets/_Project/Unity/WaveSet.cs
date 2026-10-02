using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "WaveSet", menuName = "Mini Top Down Shooter/Wave Set")]
    public class WaveSet : ScriptableObject
    {
        [SerializeField] private List<WaveConfig> _waves = new List<WaveConfig>();

        public IReadOnlyList<WaveConfig> Waves => _waves;
        public int Count => _waves != null ? _waves.Count : 0;

        private void OnValidate()
        {
            if (_waves == null)
            {
                return;
            }

            for (int i = 0; i < _waves.Count; i++)
            {
                WaveConfig wave = _waves[i];

                if (wave == null)
                {
                    continue;
                }

                wave.EnemyCount = Mathf.Max(1, wave.EnemyCount);
                wave.SpawnInterval = Mathf.Max(0.01f, wave.SpawnInterval);
                wave.InitialDelay = Mathf.Max(0f, wave.InitialDelay);
                wave.DelayAfter = Mathf.Max(0f, wave.DelayAfter);
                wave.BossCount = Mathf.Max(0, wave.BossCount);
                wave.BossDelay = Mathf.Max(0f, wave.BossDelay);

                if (wave.EnemyGroups != null)
                {
                    for (int groupIndex = 0; groupIndex < wave.EnemyGroups.Count; groupIndex++)
                    {
                        WaveEnemyGroup group = wave.EnemyGroups[groupIndex];

                        if (group == null)
                        {
                            continue;
                        }

                        group.GuaranteedCount = Mathf.Max(0, group.GuaranteedCount);
                        group.Weight = Mathf.Max(1, group.Weight);
                        group.DelayBefore = Mathf.Max(0f, group.DelayBefore);
                    }
                }

                if (wave.SpawnZoneIds != null)
                {
                    for (int zoneIndex = wave.SpawnZoneIds.Count - 1; zoneIndex >= 0; zoneIndex--)
                    {
                        if (string.IsNullOrWhiteSpace(wave.SpawnZoneIds[zoneIndex]))
                        {
                            wave.SpawnZoneIds.RemoveAt(zoneIndex);
                        }
                    }
                }
            }
        }
    }
}
