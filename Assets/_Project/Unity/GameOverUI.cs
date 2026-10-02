using Application;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class GameOverUI : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private WorldResetManager _resetManager;
        [SerializeField] private ScoreController _scoreRef;
        [SerializeField] private HighScoreController _highScoreRef;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private TMP_Text _highScoreLabel;
        [SerializeField] private TMP_Text _wavesLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private IGameStateController _gameState;
        private IScoreProvider _score;
        private IHighScoreProvider _highScore;
        private IWaveProvider _waves;

        public void Initialize(GameStateController gameState, WorldResetManager resetManager, ScoreController score = null, HighScoreController highScore = null, WaveController waves = null)
        {
            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _gameStateRef = gameState;
            _resetManager = resetManager;
            _scoreRef = score;
            _highScoreRef = highScore;
            _waveRef = waves;
            _gameState = gameState;
            _score = score;
            _highScore = highScore;
            _waves = waves;

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_resetManager == null)
            {
                _resetManager = FindFirstObjectByType<WorldResetManager>();
            }

            if (_scoreRef == null)
            {
                _scoreRef = FindFirstObjectByType<ScoreController>();
            }

            if (_highScoreRef == null)
            {
                _highScoreRef = FindFirstObjectByType<HighScoreController>();
            }

            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            _gameState = _gameStateRef;
            _score = _scoreRef;
            _highScore = _highScoreRef;
            _waves = _waveRef;
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(HandleMenuClicked);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(HandleMenuClicked);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_panel == null)
            {
                return;
            }

            bool show = newState == GameState.GameOver;
            _panel.SetActive(show);

            if (show)
            {
                UpdateResultsDisplay();
            }
        }

        private void UpdateResultsDisplay()
        {
            if (_scoreLabel != null && _score != null)
            {
                _scoreLabel.text = $"Final Score: {_score.Score}";
            }

            if (_highScoreLabel != null && _highScore != null)
            {
                _highScoreLabel.text = $"High Score: {_highScore.HighScore}";
            }

            if (_wavesLabel != null && _waves != null)
            {
                _wavesLabel.text = $"Waves Cleared: {_waves.CurrentWaveNumber}";
            }
        }

        private void HandleRestartClicked()
        {
            if (_resetManager == null)
            {
                return;
            }

            _resetManager.Restart();
        }

        private void HandleMenuClicked()
        {
            if (_gameState == null)
            {
                return;
            }

            _gameState.ReturnToMenu();
        }
    }
}
