using System.Collections.Generic;
using Application;
using UnityEngine;
using UnityEngine.AI;

namespace Game
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
        public string SpawnErrorReason { get; private set; }

        public IReadOnlyList<WaveConfig> ConfiguredWaves =>
            _waveSet != null ? _waveSet.Waves : _waves;

        public int CurrentWaveNumber => _runner != null ? _runner.CurrentWaveNumber : 0;

        private bool _isSubscribed;

        public void Initialize(EnemySpawner spawner, GameStateController gameState)
        {
            UnsubscribeEvents();

            _spawnerRef = spawner;
            _gameStateRef = gameState;
            _spawner = spawner;
            _gameState = gameState;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            Wave[] data = BuildWaveData();
            _runner = new WaveRunner(data);
            _runner.SpawnRequested += HandleSpawnRequested;
            _runner.WaveStarted += HandleWaveStarted;
            _runner.WaveCompleted += HandleWaveCompleted;
            _runner.AllWavesCompleted += HandleAllWavesCompleted;

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_spawnerRef == null)
            {
                _spawnerRef = FindFirstObjectByType<EnemySpawner>();
            }

            _gameState = _gameStateRef;
            _spawner = _spawnerRef;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }

            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }

            _isSubscribed = false;
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

        public void SetWaves(IEnumerable<WaveConfig> waves)
        {
            _waveSet = null;
            _waves = waves != null ? new List<WaveConfig>(waves) : new List<WaveConfig>();
        }

        public void SetWaveSet(WaveSet waveSet)
        {
            _waveSet = waveSet;
        }

        public void Tick(float deltaTime)
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            if (_pendingSpawnEntry != null)
            {
                _pendingSpawnDelay -= deltaTime;

                if (_pendingSpawnDelay <= 0f)
                {
                    WaveSpawnEntry entry = _pendingSpawnEntry;
                    _pendingSpawnEntry = null;
                    _pendingSpawnDelay = 0f;
                    TrySpawnEntry(entry);
                }

                return;
            }

            _runner.Tick(deltaTime);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
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
            WavePlanConfig<EnemyController> planConfig = new WavePlanConfig<EnemyController>
            {
                EnemyCount = config.EnemyCount,
                SpawnInterval = config.SpawnInterval,
                InitialDelay = config.InitialDelay,
                DelayAfter = config.DelayAfter,
                SpawnZoneIds = config.SpawnZoneIds,
                BossPrefab = config.BossPrefab,
                BossCount = config.BossCount,
                BossDelay = config.BossDelay
            };

            if (config.EnemyGroups != null)
            {
                for (int i = 0; i < config.EnemyGroups.Count; i++)
                {
                    WaveEnemyGroup g = config.EnemyGroups[i];
                    if (g != null)
                    {
                        planConfig.EnemyGroups.Add(new SpawnGroupData<EnemyController>
                        {
                            Prefab = g.Prefab,
                            GuaranteedCount = g.GuaranteedCount,
                            Weight = g.Weight,
                            DelayBefore = g.DelayBefore
                        });
                    }
                }
            }

            List<SpawnPlanItem<EnemyController>> items =
                WaveSpawnPlanner.BuildSpawnPlan(planConfig, new UnityRandomProvider());

            List<WaveSpawnEntry> result = new List<WaveSpawnEntry>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                result.Add(new WaveSpawnEntry(
                    items[i].Prefab,
                    items[i].ZoneIds,
                    items[i].DelayBefore));
            }

            return result;
        }

        public void ResetWaves()
        {
            SpawnErrorReason = null;
            if (_runner != null)
            {
                _runner.SpawnRequested -= HandleSpawnRequested;
                _runner.WaveStarted -= HandleWaveStarted;
                _runner.WaveCompleted -= HandleWaveCompleted;
                _runner.AllWavesCompleted -= HandleAllWavesCompleted;
            }

            _activePlan = null;
            _pendingSpawnEntry = null;
            _pendingSpawnDelay = 0f;
            _spawnPlanIndex = 0;

            Wave[] data = BuildWaveData();
            _runner = new WaveRunner(data);
            _runner.SpawnRequested += HandleSpawnRequested;
            _runner.WaveStarted += HandleWaveStarted;
            _runner.WaveCompleted += HandleWaveCompleted;
            _runner.AllWavesCompleted += HandleAllWavesCompleted;
        }

        private sealed class UnityRandomProvider : IRandomProvider
        {
            public int Range(int minInclusive, int maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);
            public float Range(float minInclusive, float maxInclusive) => UnityEngine.Random.Range(minInclusive, maxInclusive);
            public float Value => UnityEngine.Random.value;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Playing)
            {
                if (_runner == null)
                {
                    ResetWaves();
                }

                if (oldState == GameState.Paused)
                {
                    _runner?.Resume();
                }
                else
                {
                    SpawnErrorReason = null;
                    _pendingSpawnEntry = null;
                    _pendingSpawnDelay = 0f;
                    _runner?.StartWaves();
                }
            }
            else
            {
                _runner?.StopWaves();
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

        private const int MaxSpawnEntryRetries = 5;
        private const int MaxPositionAttempts = 15;

        private void TrySpawnEntry(WaveSpawnEntry entry)
        {
            SpawnResult result = SpawnResult.CapacityReached;
            bool spawned = false;

            if (_spawnerRef != null)
            {
                result = _spawnerRef.TrySpawn(entry.Prefab, entry.ZoneIds, out _);
                spawned = result == SpawnResult.Success;
            }
            else if (_spawner != null)
            {
                spawned = _spawner.SpawnOne();
                result = spawned ? SpawnResult.Success : SpawnResult.CapacityReached;
            }

            if (spawned)
            {
                _spawnPlanIndex++;
                _runner.NotifyEnemySpawned();
                return;
            }

            // Distinguish temporary capacity limit from position/configuration failure
            if (result == SpawnResult.CapacityReached || result == SpawnResult.GameStateNotPlaying)
            {
                // Temporary capacity limit: arena is full. Do NOT burn retries or discard the spawn.
                return;
            }

            string prefabName = entry.Prefab != null ? entry.Prefab.name : "null";
            string zoneStr = entry.ZoneIds != null && entry.ZoneIds.Count > 0 ? string.Join(", ", entry.ZoneIds) : "none";

            // If spawner failed due to invalid configuration:
            if (result == SpawnResult.InvalidConfiguration)
            {
                Debug.LogError($"[WaveController] Fatal spawn error: Invalid configuration for enemy '{prefabName}' (requested zones: [{zoneStr}]). Terminating wave.");
                TerminateWithSpawnError("Enemy setup is incomplete. Please return to the menu.");
                return;
            }

            entry.RetryCount++;

            // Preserve authored spawn restrictions after retries: attempt safe stuck recovery within the authored zone
            if (result == SpawnResult.NoAvailablePosition && entry.RetryCount >= MaxSpawnEntryRetries)
            {
                if (_spawnerRef != null && _spawnerRef.TryRecoverSpawnInZone(entry.Prefab, entry.ZoneIds, out _))
                {
                    _spawnPlanIndex++;
                    _runner.NotifyEnemySpawned();
                    return;
                }
            }

            // If recovery still fails, stop after 15 failed position attempts for that entry:
            if (entry.RetryCount >= MaxPositionAttempts)
            {
                Debug.LogError($"[WaveController] Fatal spawn error: Unable to find valid spawn position for enemy '{prefabName}' (requested zones: [{zoneStr}]) after {entry.RetryCount} attempts. Terminating wave.");
                TerminateWithSpawnError("No safe enemy spawn position was found. Please retry.");
                return;
            }
        }

        private void TerminateWithSpawnError(string reason)
        {
            SpawnErrorReason = reason;
            _activePlan = null;
            _pendingSpawnEntry = null;
            _pendingSpawnDelay = 0f;

            if (_runner != null && _runner.IsRunning)
            {
                _runner.StopWaves();
            }

            if (_gameStateRef != null)
            {
                _gameStateRef.EndGame();
            }
            else if (_gameState is IGameStateController stateCtrl)
            {
                stateCtrl.EndGame();
            }
        }

        private bool ValidateActiveWave(int waveNumber, out string errorReason)
        {
            errorReason = null;

            if (_spawnerRef != null && _spawnerRef.MaxAlive <= 0)
            {
                errorReason = "Spawner has zero or negative spawn capacity";
                return false;
            }

            if (_activePlan == null || _activePlan.Count == 0)
            {
                return true;
            }

            if (_spawnerRef != null && UnityEngine.Application.isPlaying && NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                errorReason = "Arena navigation data is missing";
                return false;
            }

            for (int i = 0; i < _activePlan.Count; i++)
            {
                WaveSpawnEntry entry = _activePlan[i];
                if (entry == null) continue;

                if (entry.Prefab == null)
                {
                    bool hasFallback = _spawnerRef != null ? _spawnerRef.HasFallbackPrefabs : (_spawner != null);
                    if (!hasFallback)
                    {
                        string zoneIds = entry.ZoneIds != null && entry.ZoneIds.Count > 0 ? string.Join(", ", entry.ZoneIds) : "none";
                        errorReason = $"Missing required enemy prefab for spawn entry {i} (zones: [{zoneIds}])";
                        return false;
                    }
                }

                if (entry.ZoneIds != null && entry.ZoneIds.Count > 0 && _spawnerRef != null)
                {
                    if (!_spawnerRef.ValidateZones(entry.ZoneIds))
                    {
                        string prefabName = entry.Prefab != null ? entry.Prefab.name : "Fallback";
                        string zoneIds = string.Join(", ", entry.ZoneIds);
                        errorReason = $"Required zone configuration missing for enemy '{prefabName}' (requested zones: [{zoneIds}])";
                        return false;
                    }
                }
            }

            return true;
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

            if (!ValidateActiveWave(waveNumber, out string errorReason))
            {
                Debug.LogError($"[WaveController] Wave {waveNumber} configuration invalid: {errorReason}. Terminating wave.");
                TerminateWithSpawnError($"This wave could not start: {errorReason}.");
                return;
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

        public sealed class WaveSpawnEntry
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
            public int RetryCount { get; set; }
        }
    }
}
