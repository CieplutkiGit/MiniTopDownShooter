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

        public void Initialize(EnemySpawner spawner, GameStateController gameState)
        {
            if (_spawner != null && enabled)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _spawnerRef = spawner;
            _gameStateRef = gameState;
            _spawner = spawner;
            _gameState = gameState;

            if (_spawner != null && enabled)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void Awake()
        {
            if (_tracker == null)
            {
                _tracker = new ScoreTracker();
                _tracker.OnScoreChanged += HandleScoreChanged;
            }

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
            if (_spawner != null)
            {
                _spawner.EnemyKilled += HandleEnemyKilled;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleEnemyKilled;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (_tracker != null)
            {
                _tracker.OnScoreChanged -= HandleScoreChanged;
            }
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            _tracker.Add(scoreValue);
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.Playing)
            {
                return;
            }

            if (oldState == GameState.Paused)
            {
                return;
            }

            _tracker.Reset();
        }

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }
    }
}
