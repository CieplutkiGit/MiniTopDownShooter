using System;
using Application;
using UnityEngine;

namespace Game
{
    public class GameStateController : MonoBehaviour, IGameStateProvider, IGameStateController
    {
        [SerializeField] private PlayerController _playerRef;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private bool _autoStart = false;
        [SerializeField] private bool _debugLog = false;

        private readonly GameStateManager _manager = new GameStateManager();
        private IPlayerEvents _player;
        private IWaveProvider _waves;

        public event Action<GameState, GameState> OnStateChanged;

        public GameState CurrentState
        {
            get { return _manager.CurrentState; }
        }

        public bool IsPlaying => _manager.CurrentState == GameState.Playing;

        public void Initialize(PlayerController player, WaveController waves)
        {
            _playerRef = player;
            _waveRef = waves;
            _player = player;
            _waves = waves;
        }

        private void Awake()
        {
            _manager.OnStateChanged += HandleStateChanged;
            if (_playerRef == null)
            {
                _playerRef = FindFirstObjectByType<PlayerController>();
            }
            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            _player = _playerRef;
            _waves = _waveRef;
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.Died += HandlePlayerDied;
            }

            if (_waves != null)
            {
                _waves.AllWavesCompleted += HandleAllWavesCompleted;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.Died -= HandlePlayerDied;
            }

            if (_waves != null)
            {
                _waves.AllWavesCompleted -= HandleAllWavesCompleted;
            }
        }

        private void Start()
        {
            if (_autoStart)
            {
                _manager.StartGame();
            }
            else
            {
                // Apply initial state immediately to all listeners
                OnStateChanged?.Invoke(GameState.Menu, _manager.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnStateChanged -= HandleStateChanged;
            }
        }

        public void StartGame()
        {
            _manager.StartGame();
        }

        public void Pause()
        {
            _manager.Pause();
        }

        public void Resume()
        {
            _manager.Resume();
        }

        public void ReturnToMenu()
        {
            _manager.ReturnToMenu();
        }

        public void TriggerVictory()
        {
            _manager.TriggerVictory();
        }

        private void HandlePlayerDied()
        {
            _manager.EndGame();
        }

        private void HandleAllWavesCompleted()
        {
            if (_manager.CurrentState == GameState.Playing)
            {
                _manager.TriggerVictory();
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_debugLog)
            {
                Debug.Log($"GameState {oldState} -> {newState}");
            }

            OnStateChanged?.Invoke(oldState, newState);
        }
    }
}
