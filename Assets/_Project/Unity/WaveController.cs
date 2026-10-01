using System.Collections.Generic;
using Application;
using UnityEngine;

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
        public float DelayAfter = 3f;
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

        public event System.Action<int> WaveStarted;
        public event System.Action<int> WaveCompleted;
        public event System.Action AllWavesCompleted;

        public WaveSet WaveSet => _waveSet;

        public int CurrentWaveNumber
        {
            get
            {
                if (_runner == null)
                {
                    return 0;
                }

                return _runner.CurrentWaveNumber;
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
            if (_runner != null)
            {
                _runner.SpawnRequested -= HandleSpawnRequested;
                _runner.WaveStarted -= HandleWaveStarted;
                _runner.WaveCompleted -= HandleWaveCompleted;
                _runner.AllWavesCompleted -= HandleAllWavesCompleted;
            }
        }

        private void Update()
        {
            if (_runner == null)
            {
                return;
            }

            _runner.Tick(Time.deltaTime);
        }

        private Wave[] BuildWaveData()
        {
            IReadOnlyList<WaveConfig> source = _waveSet != null ? _waveSet.Waves : _waves;

            if (source == null || source.Count == 0)
            {
                return new Wave[0];
            }

            Wave[] result = new Wave[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                WaveConfig config = source[i];

                if (config == null)
                {
                    result[i] = new Wave(1, 1f, 0f);
                    continue;
                }

                result[i] = new Wave(
                    Mathf.Max(1, config.EnemyCount),
                    Mathf.Max(0.01f, config.SpawnInterval),
                    Mathf.Max(0f, config.DelayAfter));
            }

            return result;
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

            if (_spawner.SpawnOne())
            {
                _runner.NotifyEnemySpawned();
            }
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            _runner.NotifyEnemyKilled();
        }

        private void HandleWaveStarted(int waveNumber)
        {
            if (_debugLog)
            {
                Debug.Log($"Wave {waveNumber} started");
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
    }
}
