using System;
using Application;
using UnityEngine;

namespace Game
{
    public class ScoreController : MonoBehaviour, IScoreProvider
    {
        [SerializeField] private EnemySpawner _spawnerRef;
        [SerializeField] private GameStateController _gameStateRef;

        private ScoreTracker _tracker;
        private IGameStateProvider _gameState;
        private ISpawner _spawner;

        public event Action<int> OnScoreChanged;

        public int Score
        {
            get
            {
                if (_tracker == null)
                {
                    return 0;
                }
                return _tracker.Score;
            }
        }

        public void ResetScore()
        {
            _tracker?.Reset();
        }

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
            if (_tracker == null)
            {
                _tracker = new ScoreTracker();
                _tracker.OnScoreChanged += HandleScoreChanged;
            }

            if (_gameState == null)
            {
                if (_gameStateRef == null)
                {
                    _gameStateRef = FindFirstObjectByType<GameStateController>();
                }
                _gameState = _gameStateRef;
            }

            if (_spawner == null)
            {
                if (_spawnerRef == null)
                {
                    _spawnerRef = FindFirstObjectByType<EnemySpawner>();
                }
                _spawner = _spawnerRef;
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (_tracker != null)
            {
                _tracker.OnScoreChanged -= HandleScoreChanged;
            }
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _isSubscribed = false;
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_tracker == null)
            {
                _tracker = new ScoreTracker();
                _tracker.OnScoreChanged += HandleScoreChanged;
            }

            _tracker.Add(scoreValue);
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (newState != GameState.Playing)
            {
                return;
            }

            if (oldState == GameState.Paused)
            {
                return;
            }

            if (_tracker == null)
            {
                _tracker = new ScoreTracker();
                _tracker.OnScoreChanged += HandleScoreChanged;
            }

            _tracker.Reset();
        }

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }
    }
}
