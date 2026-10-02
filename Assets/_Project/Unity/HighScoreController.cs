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

        public void Initialize(ScoreController score, GameStateController gameState)
        {
            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _scoreRef = score;
            _gameStateRef = gameState;
            _score = score;
            _gameState = gameState;

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void Awake()
        {
            if (_scoreRef == null)
            {
                _scoreRef = FindFirstObjectByType<ScoreController>();
            }

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            _score = _scoreRef;
            _gameState = _gameStateRef;

            UserProfileData profile = SaveManager.LoadProfile();
            int prefsScore = PlayerPrefs.GetInt(_prefsKey, 0);
            int profileScore = profile != null ? profile.HighScore : 0;
            _highScore = Math.Max(prefsScore, profileScore);

            if (profile != null && _highScore > profile.HighScore)
            {
                profile.HighScore = _highScore;
                SaveManager.SaveProfile(profile);
            }
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
            if (oldState != GameState.Playing || (newState != GameState.GameOver && newState != GameState.Victory))
            {
                return;
            }

            UserProfileData profile = SaveManager.LoadProfile() ?? new UserProfileData();
            profile.TotalRuns++;

            if (newState == GameState.Victory)
            {
                profile.TotalWins++;
            }
            else if (newState == GameState.GameOver)
            {
                profile.TotalLosses++;
            }

            if (_score != null)
            {
                int finalScore = _score.Score;
                if (finalScore > _highScore)
                {
                    _highScore = finalScore;
                    profile.HighScore = _highScore;
                    PlayerPrefs.SetInt(_prefsKey, _highScore);
                    PlayerPrefs.Save();
                    OnHighScoreChanged?.Invoke(_highScore);
                }
            }

            SaveManager.SaveProfile(profile);
        }
    }
}
