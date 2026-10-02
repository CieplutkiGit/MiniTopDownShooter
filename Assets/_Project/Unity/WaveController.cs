using System.Collections.Generic;
using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    [System.Serializable]
    public class WaveConfig
    {
        [Min(1)]
        public int EnemyCount = 5;

        [Min(0.01f)]
        public float SpawnInterval = 1f;

        [Min(0f)]
        public float InitialDelay;

        [Min(0f)]
        public float DelayAfter = 3f;

        [Tooltip("Optional explicit enemy composition. Empty keeps the legacy global spawner weights.")]
        public List<WaveEnemyGroup> EnemyGroups = new List<WaveEnemyGroup>();

        [Tooltip("Optional SpawnZone IDs allowed for this wave. Empty allows any configured zone.")]
        public List<string> SpawnZoneIds = new List<string>();

        [Header("Boss")]
        public EnemyController BossPrefab;

        [Min(0)]
        public int BossCount;

        [Min(0f)]
        public float BossDelay;
    }

    public class WaveController : MonoBehaviour, IWaveProvider
    {
        [Header("Wave Data")]
        [Tooltip("Optional reusable Wave Set. When assigned, it overrides the inline waves below.")]
        [SerializeField] private WaveSet _waveSet;
        [SerializeField] private List<WaveConfig> _waves;

        [Header("Scene References")]
        [SerializeField] private EnemySpawner _spawnerRef;
        [SerializeField] private GameStateController _gameStateRef;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private WaveRunner _runner;
        private IGameStateProvider _gameState;
        private ISpawner _spawner;
        private List<WaveSpawnEntry>[] _spawnPlans;
        private List<WaveSpawnEntry> _activePlan;
        private int _spawnPlanIndex;
        private WaveSpawnEntry _pendingSpawnEntry;
        private float _pendingSpawnDelay;

        public event System.Action<int> WaveStarted;
        public event System.Action<int> WaveCompleted;
        public event System.Action AllWavesCompleted;

        public WaveSet WaveSet => _waveSet;

        public IReadOnlyList<WaveConfig> ConfiguredWaves =>
            _waveSet != null ? _waveSet.Waves : _waves;

        public int CurrentWaveNumber => _runner != null ? _runner.CurrentWaveNumber : 0;

        private void Awake()
        {
            Wave[] data = BuildWaveData();
            _runner = new WaveRunner(data);
            _runner.SpawnRequested += HandleSpawnRequested;
            _runner.WaveStarted += HandleWaveStarted;
            _runner.WaveCompleted += HandleWaveCompleted;
            _runner.AllWavesCompleted += HandleAllWavesCompleted;
            _gameState = _gameStateRef;
            _spawner = _spawnerRef;
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }
        }

        private void OnDestroy()
        {
            if (_runner == null)
            {
                return;
            }

            _runner.SpawnRequested -= HandleSpawnRequested;
            _runner.WaveStarted -= HandleWaveStarted;
            _runner.WaveCompleted -= HandleWaveCompleted;
            _runner.AllWavesCompleted -= HandleAllWavesCompleted;
        }

        private void Update()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            if (_pendingSpawnEntry != null)
            {
                _pendingSpawnDelay -= Time.deltaTime;

                if (_pendingSpawnDelay <= 0f)
                {
                    WaveSpawnEntry entry = _pendingSpawnEntry;
                    _pendingSpawnEntry = null;
                    _pendingSpawnDelay = 0f;
                    TrySpawnEntry(entry);
                }

                return;
            }

            _runner.Tick(Time.deltaTime);
        }

        private Wave[] BuildWaveData()
        {
            IReadOnlyList<WaveConfig> source = _waveSet != null ? _waveSet.Waves : _waves;

            if (source == null || source.Count == 0)
            {
                _spawnPlans = new List<WaveSpawnEntry>[0];
                return new Wave[0];
            }

            Wave[] result = new Wave[source.Count];
            _spawnPlans = new List<WaveSpawnEntry>[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                WaveConfig config = source[i];

                if (config == null)
                {
                    List<WaveSpawnEntry> fallbackPlan = new List<WaveSpawnEntry>
                    {
                        new WaveSpawnEntry(null, null, 0f)
                    };

                    _spawnPlans[i] = fallbackPlan;
                    result[i] = new Wave(1, 1f, 0f);
                    continue;
                }

                List<WaveSpawnEntry> plan = BuildSpawnPlan(config);
                _spawnPlans[i] = plan;

                result[i] = new Wave(
                    Mathf.Max(1, plan.Count),
                    Mathf.Max(0.01f, config.SpawnInterval),
                    Mathf.Max(0f, config.DelayAfter),
                    Mathf.Max(0f, config.InitialDelay));
            }

            return result;
        }

        private static List<WaveSpawnEntry> BuildSpawnPlan(WaveConfig config)
        {
            List<WaveSpawnEntry> plan = new List<WaveSpawnEntry>();
            List<WaveEnemyGroup> groups = GetValidGroups(config.EnemyGroups);
            HashSet<WaveEnemyGroup> delayApplied = new HashSet<WaveEnemyGroup>();
            int baseEnemyCount = Mathf.Max(1, config.EnemyCount);

            if (groups.Count == 0)
            {
                for (int i = 0; i < baseEnemyCount; i++)
                {
                    plan.Add(new WaveSpawnEntry(null, config.SpawnZoneIds, 0f));
                }
            }
            else
            {
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    WaveEnemyGroup group = groups[groupIndex];

                    for (int i = 0; i < Mathf.Max(0, group.GuaranteedCount); i++)
                    {
                        float delay = 0f;

                        if (!delayApplied.Contains(group))
                        {
                            delay = Mathf.Max(0f, group.DelayBefore);
                            delayApplied.Add(group);
                        }

                        plan.Add(new WaveSpawnEntry(
                            group.Prefab,
                            config.SpawnZoneIds,
                            delay));
                    }
                }

                while (plan.Count < baseEnemyCount)
                {
                    WaveEnemyGroup group = PickWeightedGroup(groups);
                    float delay = 0f;

                    if (!delayApplied.Contains(group))
                    {
                        delay = Mathf.Max(0f, group.DelayBefore);
                        delayApplied.Add(group);
                    }

                    plan.Add(new WaveSpawnEntry(
                        group.Prefab,
                        config.SpawnZoneIds,
                        delay));
                }
            }

            if (config.BossPrefab != null && config.BossCount > 0)
            {
                for (int i = 0; i < config.BossCount; i++)
                {
                    float delay = i == 0 ? Mathf.Max(0f, config.BossDelay) : 0f;
                    plan.Add(new WaveSpawnEntry(
                        config.BossPrefab,
                        config.SpawnZoneIds,
                        delay));
                }
            }

            return plan;
        }

        private static List<WaveEnemyGroup> GetValidGroups(
            IReadOnlyList<WaveEnemyGroup> source)
        {
            List<WaveEnemyGroup> result = new List<WaveEnemyGroup>();

            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                WaveEnemyGroup group = source[i];

                if (group != null && group.Prefab != null)
                {
                    result.Add(group);
                }
            }

            return result;
        }

        private static WaveEnemyGroup PickWeightedGroup(
            IReadOnlyList<WaveEnemyGroup> groups)
        {
            int totalWeight = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                totalWeight += Mathf.Max(1, groups[i].Weight);
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                cumulative += Mathf.Max(1, groups[i].Weight);

                if (roll < cumulative)
                {
                    return groups[i];
                }
            }

            return groups[groups.Count - 1];
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Playing)
            {
                if (oldState == GameState.Paused)
                {
                    _runner.Resume();
                }
                else
                {
                    _pendingSpawnEntry = null;
                    _pendingSpawnDelay = 0f;
                    _runner.StartWaves();
                }
            }
            else
            {
                _runner.StopWaves();
            }
        }

        private void HandleSpawnRequested()
        {
            if (_spawner == null)
            {
                return;
            }

            WaveSpawnEntry entry = GetCurrentSpawnEntry();

            if (entry == null)
            {
                return;
            }

            if (!entry.DelayConsumed && entry.DelayBefore > 0f)
            {
                entry.DelayConsumed = true;
                _pendingSpawnEntry = entry;
                _pendingSpawnDelay = entry.DelayBefore;
                return;
            }

            TrySpawnEntry(entry);
        }

        private WaveSpawnEntry GetCurrentSpawnEntry()
        {
            if (_activePlan == null ||
                _spawnPlanIndex < 0 ||
                _spawnPlanIndex >= _activePlan.Count)
            {
                return null;
            }

            return _activePlan[_spawnPlanIndex];
        }

        private void TrySpawnEntry(WaveSpawnEntry entry)
        {
            bool spawned;

            if (_spawnerRef != null)
            {
                spawned = _spawnerRef.SpawnOne(entry.Prefab, entry.ZoneIds);
            }
            else
            {
                spawned = _spawner.SpawnOne();
            }

            if (!spawned)
            {
                return;
            }

            _spawnPlanIndex++;
            _runner.NotifyEnemySpawned();
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            _runner.NotifyEnemyKilled();
        }

        private void HandleWaveStarted(int waveNumber)
        {
            int index = waveNumber - 1;
            _activePlan =
                _spawnPlans != null && index >= 0 && index < _spawnPlans.Length
                    ? _spawnPlans[index]
                    : null;
            _spawnPlanIndex = 0;
            _pendingSpawnEntry = null;
            _pendingSpawnDelay = 0f;

            if (_activePlan != null)
            {
                for (int i = 0; i < _activePlan.Count; i++)
                {
                    _activePlan[i].DelayConsumed = false;
                }
            }

            if (_debugLog)
            {
                Debug.Log($"Wave {waveNumber} started with {_activePlan?.Count ?? 0} spawn entries");
            }

            WaveStarted?.Invoke(waveNumber);
        }

        private void HandleWaveCompleted(int waveNumber)
        {
            if (_debugLog)
            {
                Debug.Log($"Wave {waveNumber} completed");
            }

            WaveCompleted?.Invoke(waveNumber);
        }

        private void HandleAllWavesCompleted()
        {
            _activePlan = null;
            _pendingSpawnEntry = null;

            if (_debugLog)
            {
                Debug.Log("All waves completed");
            }

            AllWavesCompleted?.Invoke();
        }

        private void OnValidate()
        {
            bool hasReusableSet = _waveSet != null && _waveSet.Count > 0;
            bool hasInlineWaves = _waves != null && _waves.Count > 0;

            if (!hasReusableSet && !hasInlineWaves)
            {
                Debug.LogWarning(
                    "WaveController has no configured waves. Assign a Wave Set or add inline waves.",
                    this);
            }
        }

        private sealed class WaveSpawnEntry
        {
            public WaveSpawnEntry(
                EnemyController prefab,
                IReadOnlyList<string> zoneIds,
                float delayBefore)
            {
                Prefab = prefab;
                ZoneIds = zoneIds;
                DelayBefore = delayBefore;
            }

            public EnemyController Prefab { get; }
            public IReadOnlyList<string> ZoneIds { get; }
            public float DelayBefore { get; }
            public bool DelayConsumed { get; set; }
        }
    }
}
