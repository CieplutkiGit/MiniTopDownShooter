using System;
using Application;
using UnityEngine;

namespace Game
{
    public class ScoreController : MonoBehaviour, IScoreProvider
    {
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private int _pointsPerKill = 1;

        private ScoreTracker _tracker;
        private IGameStateProvider _gameState;

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

        private void Awake()
        {
            _tracker = new ScoreTracker();
            _tracker.OnScoreChanged += HandleScoreChanged;
            _gameState = _gameStateRef;
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

        private void HandleEnemyKilled()
        {
            _tracker.Add(_pointsPerKill);
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                _tracker.Reset();
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }
    }
}
