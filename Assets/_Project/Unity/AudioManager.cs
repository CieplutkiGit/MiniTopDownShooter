using Application;
using Core;
using UnityEngine;

namespace Game
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;

        [SerializeField] private Gun _gun;
        [SerializeField] private HealthComponent _playerHealthRef;
        [SerializeField] private PlayerController _player;
        [SerializeField] private EnemySpawner _spawnerRef;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private GameStateController _gameStateRef;

        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private AudioClip _playerHitClip;
        [SerializeField] private AudioClip _enemyDeathClip;
        [SerializeField] private AudioClip _playerDeathClip;
        [SerializeField] private AudioClip _waveStartClip;
        [SerializeField] private AudioClip _gameOverClip;

        private ISpawner _spawner;
        private IWaveProvider _waves;
        private IGameStateProvider _gameState;
        private IHealthReadable _playerHealth;
        private int _lastPlayerHealth;

        private void Awake()
        {
            _spawner = _spawnerRef;
            _waves = _waveRef;
            _gameState = _gameStateRef;
            _playerHealth = _playerHealthRef;
        }

        private void OnEnable()
        {
            if (_gun != null)
            {
                _gun.Fired += HandleGunFired;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }

            if (_waves != null)
            {
                _waves.WaveStarted += HandleWaveStarted;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
                _lastPlayerHealth = _playerHealth.Current;
            }

            if (_player != null)
            {
                _player.Died += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (_gun != null)
            {
                _gun.Fired -= HandleGunFired;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }

            if (_waves != null)
            {
                _waves.WaveStarted -= HandleWaveStarted;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            if (_player != null)
            {
                _player.Died -= HandlePlayerDied;
            }
        }

        private void HandleGunFired()
        {
            PlayClip(_shotClip);
        }

        private void HandleEnemyKilled()
        {
            PlayClip(_enemyDeathClip);
        }

        private void HandleWaveStarted(int waveNumber)
        {
            PlayClip(_waveStartClip);
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                PlayClip(_gameOverClip);
            }
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            if (current < _lastPlayerHealth)
            {
                PlayClip(_playerHitClip);
            }

            _lastPlayerHealth = current;
        }

        private void HandlePlayerDied()
        {
            PlayClip(_playerDeathClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (_source == null)
            {
                return;
            }

            _source.PlayOneShot(clip);
        }
    }
}
