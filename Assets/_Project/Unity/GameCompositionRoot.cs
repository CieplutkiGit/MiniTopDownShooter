using UnityEngine;

namespace Game
{
    /// <summary>
    /// Composition root for MiniTopDownShooter.
    /// Runs before standard MonoBehaviours (ExecutionOrder -100) to deterministically
    /// wire dependencies between game controllers, managers, and UI panels without
    /// relying on runtime FindFirstObjectByType calls.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameCompositionRoot : MonoBehaviour
    {
        [Header("Player & World")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private EffectPool _effectPool;
        [SerializeField] private MobileInputState _mobileInput;

        [Header("Game Controllers")]
        [SerializeField] private GameStateController _gameStateController;
        [SerializeField] private TimeController _timeController;
        [SerializeField] private ScoreController _scoreController;
        [SerializeField] private HighScoreController _highScoreController;
        [SerializeField] private WaveController _waveController;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private WorldResetManager _worldResetManager;

        [Header("User Interface")]
        [SerializeField] private MainMenuUI _mainMenuUI;
        [SerializeField] private PauseUI _pauseUI;
        [SerializeField] private GameOverUI _gameOverUI;
        [SerializeField] private VictoryUI _victoryUI;
        [SerializeField] private ScoreUI _scoreUI;
        [SerializeField] private WaveUI _waveUI;
        [SerializeField] private WeaponHUD _weaponHUD;
        [SerializeField] private HighScoreUI _highScoreUI;
        [SerializeField] private HealthBarUI _healthBarUI;
        [SerializeField] private BossHealthBarUI _bossHealthBarUI;
        [SerializeField] private SettingsUI _settingsUI;

        // Public accessors for testing and architectural inspection
        public PlayerController Player => _player;
        public GameStateController GameStateController => _gameStateController;
        public TimeController TimeController => _timeController;
        public ScoreController ScoreController => _scoreController;
        public HighScoreController HighScoreController => _highScoreController;
        public WaveController WaveController => _waveController;
        public EnemySpawner EnemySpawner => _enemySpawner;
        public WorldResetManager WorldResetManager => _worldResetManager;
        public EffectPool EffectPool => _effectPool;

        private void Awake()
        {
            ComposeDependencies();
        }

        public void ComposeDependencies()
        {
            ResolveMissingReferences();
            WireDependencies();
        }

        private void ResolveMissingReferences()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            if (_gameStateController == null)
            {
                _gameStateController = FindFirstObjectByType<GameStateController>();
            }

            if (_timeController == null)
            {
                _timeController = FindFirstObjectByType<TimeController>();
            }

            if (_scoreController == null)
            {
                _scoreController = FindFirstObjectByType<ScoreController>();
            }

            if (_highScoreController == null)
            {
                _highScoreController = FindFirstObjectByType<HighScoreController>();
            }

            if (_waveController == null)
            {
                _waveController = FindFirstObjectByType<WaveController>();
            }

            if (_enemySpawner == null)
            {
                _enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (_worldResetManager == null)
            {
                _worldResetManager = FindFirstObjectByType<WorldResetManager>();
            }

            if (_effectPool == null)
            {
                _effectPool = FindFirstObjectByType<EffectPool>();
            }

            if (_mobileInput == null)
            {
                _mobileInput = FindFirstObjectByType<MobileInputState>(FindObjectsInactive.Include);
            }

            // UI Elements
            if (_mainMenuUI == null)
            {
                _mainMenuUI = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            }

            if (_pauseUI == null)
            {
                _pauseUI = FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
            }

            if (_gameOverUI == null)
            {
                _gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            }

            if (_victoryUI == null)
            {
                _victoryUI = FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
            }

            if (_scoreUI == null)
            {
                _scoreUI = FindFirstObjectByType<ScoreUI>(FindObjectsInactive.Include);
            }

            if (_waveUI == null)
            {
                _waveUI = FindFirstObjectByType<WaveUI>(FindObjectsInactive.Include);
            }

            if (_weaponHUD == null)
            {
                _weaponHUD = FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
            }

            if (_highScoreUI == null)
            {
                _highScoreUI = FindFirstObjectByType<HighScoreUI>(FindObjectsInactive.Include);
            }

            if (_healthBarUI == null)
            {
                _healthBarUI = FindFirstObjectByType<HealthBarUI>(FindObjectsInactive.Include);
            }

            if (_bossHealthBarUI == null)
            {
                _bossHealthBarUI = FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
            }

            if (_settingsUI == null)
            {
                _settingsUI = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            }
        }

        private void WireDependencies()
        {
            Camera mainCam = Camera.main;

            if (_player != null && _gameStateController != null)
            {
                _player.Initialize(_gameStateController, mainCam, _mobileInput);
            }

            if (_gameStateController != null)
            {
                _gameStateController.Initialize(_player, _waveController);
            }

            if (_timeController != null && _gameStateController != null)
            {
                _timeController.Initialize(_gameStateController);
            }

            if (_enemySpawner != null)
            {
                Transform playerTransform = _player != null ? _player.transform : null;
                _enemySpawner.Initialize(playerTransform, _gameStateController, _effectPool);
            }

            if (_waveController != null)
            {
                _waveController.Initialize(_enemySpawner, _gameStateController);
            }

            if (_scoreController != null)
            {
                _scoreController.Initialize(_enemySpawner, _gameStateController);
            }

            if (_highScoreController != null && _scoreController != null && _gameStateController != null)
            {
                _highScoreController.Initialize(_scoreController, _gameStateController);
            }

            if (_worldResetManager != null)
            {
                _worldResetManager.Initialize(
                    _player,
                    _enemySpawner,
                    _gameStateController,
                    _waveController,
                    _effectPool,
                    _scoreController,
                    _spawnPoint);
            }

            // UI Wiring
            if (_mainMenuUI != null && _gameStateController != null && _worldResetManager != null)
            {
                _mainMenuUI.Initialize(_gameStateController, _worldResetManager);
            }

            if (_pauseUI != null && _gameStateController != null)
            {
                _pauseUI.Initialize(_gameStateController);
            }

            if (_gameOverUI != null && _gameStateController != null && _worldResetManager != null)
            {
                _gameOverUI.Initialize(
                    _gameStateController,
                    _worldResetManager,
                    _scoreController,
                    _highScoreController,
                    _waveController);
            }

            if (_victoryUI != null && _gameStateController != null && _worldResetManager != null)
            {
                _victoryUI.Initialize(
                    _gameStateController,
                    _worldResetManager,
                    _scoreController,
                    _highScoreController,
                    _waveController);
            }

            if (_scoreUI != null && _scoreController != null)
            {
                _scoreUI.Initialize(_scoreController);
            }

            if (_waveUI != null && _waveController != null)
            {
                _waveUI.Initialize(_waveController);
            }

            if (_weaponHUD != null && _player != null)
            {
                WeaponLoadout loadout = _player.GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    _weaponHUD.Initialize(loadout);
                }
            }

            if (_highScoreUI != null && _highScoreController != null)
            {
                _highScoreUI.Initialize(_highScoreController);
            }

            if (_healthBarUI != null && _player != null)
            {
                HealthComponent playerHealth = _player.GetComponent<HealthComponent>();
                if (playerHealth != null)
                {
                    _healthBarUI.Initialize(playerHealth);
                }
            }

            ScreenShake shake = FindFirstObjectByType<ScreenShake>();
            if (shake != null && _player != null)
            {
                HealthComponent playerHealth = _player.GetComponent<HealthComponent>();
                if (playerHealth != null)
                {
                    shake.Initialize(playerHealth);
                }
            }

            PlayerAudio playerAudio = FindFirstObjectByType<PlayerAudio>();
            if (playerAudio != null && _player != null)
            {
                playerAudio.Initialize(_player);
            }

            if (_settingsUI != null && _player != null)
            {
                _settingsUI.Initialize(_player);
            }
        }
    }
}
