using System;
using Application;
using UnityEngine;

namespace Game
{
    public class HighScoreController : MonoBehaviour, IHighScoreProvider
    {
        [SerializeField] private ScoreController _scoreRef;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private string _prefsKey = "HighScore";

        private IScoreProvider _score;
        private IGameStateProvider _gameState;
        private int _highScore;

        public event Action<int> OnHighScoreChanged;

        public int HighScore
        {
            get { return _highScore; }
        }

        private void Awake()
        {
            _score = _scoreRef;
            _gameState = _gameStateRef;
            _highScore = PlayerPrefs.GetInt(_prefsKey, 0);
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.GameOver)
            {
                return;
            }

            if (_score == null)
            {
                return;
            }

            int finalScore = _score.Score;
            if (finalScore <= _highScore)
            {
                return;
            }

            _highScore = finalScore;
            PlayerPrefs.SetInt(_prefsKey, _highScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(_highScore);
        }
    }
}
