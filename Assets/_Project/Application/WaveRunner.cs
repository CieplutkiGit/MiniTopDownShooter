using System;

namespace Application
{
    public class WaveRunner
    {
        private readonly Wave[] _waves;
        private int _currentWaveIndex;
        private int _spawnedInWave;
        private int _killedInWave;
        private float _spawnTimer;
        private float _interWaveTimer;
        private float _preWaveTimer;
        private bool _isRunning;
        private bool _isBetweenWaves;

        public event Action SpawnRequested;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;

        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public bool IsRunning => _isRunning;

        public WaveRunner(Wave[] waves)
        {
            _waves = waves;
            _isRunning = false;
        }

        public void StartWaves()
        {
            if (_waves == null || _waves.Length == 0)
            {
                return;
            }

            _currentWaveIndex = 0;
            _spawnedInWave = 0;
            _killedInWave = 0;
            _spawnTimer = 0f;
            _preWaveTimer = Math.Max(0f, _waves[0].InitialDelay);
            _isBetweenWaves = false;
            _isRunning = true;
            WaveStarted?.Invoke(CurrentWaveNumber);
        }

        public void StopWaves()
        {
            _isRunning = false;
        }

        public void Resume()
        {
            if (_waves == null || _waves.Length == 0)
            {
                return;
            }

            if (_currentWaveIndex >= _waves.Length)
            {
                return;
            }

            _isRunning = true;
        }

        public void NotifyEnemySpawned()
        {
            if (_isRunning)
            {
                _spawnedInWave++;
            }
        }

        public void NotifyEnemyKilled()
        {
            if (_isRunning)
            {
                _killedInWave++;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isRunning || _currentWaveIndex >= _waves.Length)
            {
                return;
            }

            if (_isBetweenWaves)
            {
                TickBetweenWaves(deltaTime);
                return;
            }

            if (_preWaveTimer > 0f)
            {
                _preWaveTimer -= deltaTime;

                if (_preWaveTimer > 0f)
                {
                    return;
                }
            }

            Wave current = _waves[_currentWaveIndex];

            if (_spawnedInWave < current.EnemyCount)
            {
                _spawnTimer -= deltaTime;

                if (_spawnTimer <= 0f)
                {
                    SpawnRequested?.Invoke();
                    _spawnTimer = current.SpawnInterval;
                }
            }

            if (_spawnedInWave >= current.EnemyCount && _killedInWave >= current.EnemyCount)
            {
                WaveCompleted?.Invoke(CurrentWaveNumber);
                _currentWaveIndex++;

                if (_currentWaveIndex >= _waves.Length)
                {
                    AllWavesCompleted?.Invoke();
                    _isRunning = false;
                    return;
                }

                _isBetweenWaves = true;
                _interWaveTimer = Math.Max(0f, current.DelayAfter);
            }
        }

        private void TickBetweenWaves(float deltaTime)
        {
            _interWaveTimer -= deltaTime;

            if (_interWaveTimer > 0f)
            {
                return;
            }

            _isBetweenWaves = false;
            _spawnedInWave = 0;
            _killedInWave = 0;
            _spawnTimer = 0f;
            _preWaveTimer = Math.Max(0f, _waves[_currentWaveIndex].InitialDelay);
            WaveStarted?.Invoke(CurrentWaveNumber);
        }
    }
}
