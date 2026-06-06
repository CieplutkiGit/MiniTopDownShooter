using System.Collections.Generic;
using Application;
using UnityEngine;

namespace Game
{
    [System.Serializable]
    public class WaveConfig
    {
        public int EnemyCount = 5;
        public float SpawnInterval = 1f;
        public float DelayAfter = 3f;
    }

    public class WaveController : MonoBehaviour, IWaveProvider
    {
        [SerializeField] private List<WaveConfig> _waves;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private bool _debugLog = false;

        private WaveRunner _runner;
        private IGameStateProvider _gameState;

        public event System.Action<int> WaveStarted;
        public event System.Action<int> WaveCompleted;
        public event System.Action AllWavesCompleted;

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
            if (_waves == null || _waves.Count == 0)
            {
                return new Wave[0];
            }

            Wave[] result = new Wave[_waves.Count];
            for (int i = 0; i < _waves.Count; i++)
            {
                WaveConfig config = _waves[i];
                result[i] = new Wave(config.EnemyCount, config.SpawnInterval, config.DelayAfter);
            }

            return result;
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                _runner.StartWaves();
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

            _spawner.SpawnOne();
        }

        private void HandleEnemyKilled()
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
    }
}
