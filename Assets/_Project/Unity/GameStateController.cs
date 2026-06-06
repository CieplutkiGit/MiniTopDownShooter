using System;
using Application;
using UnityEngine;

namespace Game
{
    public class GameStateController : MonoBehaviour, IGameStateProvider
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private bool _debugLog = false;

        private GameStateManager _manager;

        public event Action<GameState> OnStateChanged;

        public GameState CurrentState
        {
            get { return _manager.CurrentState; }
        }

        private void Awake()
        {
            _manager = new GameStateManager();
            _manager.OnStateChanged += HandleStateChanged;
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.Died += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.Died -= HandlePlayerDied;
            }
        }

        private void Start()
        {
            if (_autoStart)
            {
                _manager.StartGame();
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

        private void HandlePlayerDied()
        {
            _manager.EndGame();
        }

        private void HandleStateChanged(GameState newState)
        {
            if (_debugLog)
            {
                Debug.Log($"GameState -> {newState}");
            }

            OnStateChanged?.Invoke(newState);
        }
    }
}
