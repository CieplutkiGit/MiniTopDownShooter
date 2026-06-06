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
        private bool _isRunning;
        private bool _isBetweenWaves;

        public event Action SpawnRequested;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;

        public int CurrentWaveNumber
        {
            get { return _currentWaveIndex + 1; }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

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
            if (!_isRunning)
            {
                return;
            }

            _spawnedInWave++;
        }

        public void NotifyEnemyKilled()
        {
            if (!_isRunning)
            {
                return;
            }

            _killedInWave++;
        }

        public void Tick(float deltaTime)
        {
            if (!_isRunning)
            {
                return;
            }

            if (_currentWaveIndex >= _waves.Length)
            {
                return;
            }

            if (_isBetweenWaves)
            {
                TickBetweenWaves(deltaTime);
                return;
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
                _interWaveTimer = current.DelayAfter;
            }
        }

        private void TickBetweenWaves(float deltaTime)
        {
            _interWaveTimer -= deltaTime;
            if (_interWaveTimer <= 0f)
            {
                _isBetweenWaves = false;
                _spawnedInWave = 0;
                _killedInWave = 0;
                _spawnTimer = 0f;
                WaveStarted?.Invoke(CurrentWaveNumber);
            }
        }
    }
}
