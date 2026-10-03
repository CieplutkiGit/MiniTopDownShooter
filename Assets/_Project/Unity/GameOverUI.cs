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
        private bool _isSubscribed;
        private TMP_Text _spawnErrorLabel;

        public void Initialize(GameStateController gameState, WorldResetManager resetManager, ScoreController score = null, HighScoreController highScore = null, WaveController waves = null)
        {
            UnsubscribeEvents();

            _gameStateRef = gameState;
            _resetManager = resetManager;
            _scoreRef = score;
            _highScoreRef = highScore;
            _waveRef = waves;
            _gameState = gameState;
            _score = score;
            _highScore = highScore;
            _waves = waves;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
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

            TMP_Text[] texts = { _scoreLabel, _highScoreLabel, _wavesLabel };
            foreach (var t in texts)
            {
                if (t != null)
                {
                    t.raycastTarget = false;
                    t.enableAutoSizing = true;
                }
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(HandleMenuClicked);
            }

            SetPanelActive(false);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(HandleMenuClicked);
            }
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        private void SetPanelActive(bool active)
        {
            if (_panel == null)
            {
                return;
            }

            if (_panel == gameObject)
            {
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = active ? 1f : 0f;
                cg.interactable = active;
                cg.blocksRaycasts = active;
            }
            else
            {
                _panel.SetActive(active);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_panel == null)
            {
                return;
            }

            bool show = newState == GameState.GameOver;
            SetPanelActive(show);

            if (show)
            {
                UpdateResultsDisplay();
            }
        }

        private void UpdateResultsDisplay()
        {
            string spawnError = _waveRef != null ? _waveRef.SpawnErrorReason : null;
            bool hasSpawnError = !string.IsNullOrEmpty(spawnError);
            if (hasSpawnError && _spawnErrorLabel == null)
            {
                TMP_Text template = _panel.GetComponentInChildren<TMP_Text>(true);
                if (template != null)
                {
                    _spawnErrorLabel = Instantiate(template, _panel.transform);
                    _spawnErrorLabel.name = "SpawnErrorText";
                    _spawnErrorLabel.raycastTarget = false;
                    _spawnErrorLabel.richText = false;
                    _spawnErrorLabel.alignment = TextAlignmentOptions.Center;
                    _spawnErrorLabel.enableAutoSizing = true;
                    _spawnErrorLabel.fontSizeMin = 16f;
                    _spawnErrorLabel.fontSizeMax = 22f;
                    RectTransform rect = _spawnErrorLabel.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 130f);
                    rect.sizeDelta = new Vector2(600f, 80f);
                }
            }

            if (_spawnErrorLabel != null)
            {
                _spawnErrorLabel.text = spawnError ?? string.Empty;
                _spawnErrorLabel.gameObject.SetActive(hasSpawnError);
            }

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
