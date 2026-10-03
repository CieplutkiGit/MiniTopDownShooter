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

        private bool _isManagerSubscribed;
        private bool _isGameplaySubscribed;

        public GameStateController()
        {
            SubscribeManager();
        }

        private void SubscribeManager()
        {
            if (_isManagerSubscribed) return;
            _manager.OnStateChanged += HandleStateChanged;
            _isManagerSubscribed = true;
        }

        public bool IsPlaying => _manager.CurrentState == GameState.Playing;

        public void Initialize(PlayerController player, WaveController waves)
        {
            UnsubscribeGameplayEvents();

            _playerRef = player;
            _waveRef = waves;
            _player = player;
            _waves = waves;

            if (isActiveAndEnabled)
            {
                SubscribeGameplayEvents();
            }
        }

        private void Awake()
        {
            SubscribeManager();
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
            SubscribeGameplayEvents();
        }

        private void OnDisable()
        {
            UnsubscribeGameplayEvents();
        }

        private void SubscribeGameplayEvents()
        {
            if (_isGameplaySubscribed || (_player == null && _waves == null)) return;

            if (_player != null)
            {
                _player.Died += HandlePlayerDied;
            }

            if (_waves != null)
            {
                _waves.AllWavesCompleted += HandleAllWavesCompleted;
            }

            _isGameplaySubscribed = true;
        }

        private void UnsubscribeGameplayEvents()
        {
            if (!_isGameplaySubscribed) return;

            if (_player != null)
            {
                _player.Died -= HandlePlayerDied;
            }

            if (_waves != null)
            {
                _waves.AllWavesCompleted -= HandleAllWavesCompleted;
            }

            _isGameplaySubscribed = false;
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
            UnsubscribeGameplayEvents();

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

        public GameState PreviousStateBeforePause => _manager.PreviousStateBeforePause;

        public void EnterWorkshopRoaming()
        {
            _manager.EnterWorkshopRoaming();
        }

        public void EnterWorkshopEditing()
        {
            _manager.EnterWorkshopEditing();
        }

        public void EnterWorkshopFiringRange()
        {
            _manager.EnterWorkshopFiringRange();
        }

        public void ReturnToMenu()
        {
            _manager.ReturnToMenu();
        }

        public void TriggerVictory()
        {
            _manager.TriggerVictory();
        }

        public void EndGame()
        {
            _manager.EndGame();
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

            if (OnStateChanged == null) return;

            foreach (Action<GameState, GameState> listener in OnStateChanged.GetInvocationList())
            {
                // A listener may terminate an invalid wave while handling Playing.
                if (_manager.CurrentState != newState) break;
                listener(oldState, newState);
            }
        }
    }
}
