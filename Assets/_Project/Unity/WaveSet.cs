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
                wave.DelayAfter = Mathf.Max(0f, wave.DelayAfter);
            }
        }
    }
}
