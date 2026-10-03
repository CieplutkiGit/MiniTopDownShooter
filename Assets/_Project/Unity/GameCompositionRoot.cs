using UnityEngine;
using Application.Flow;
using Game.Flow;
using Game.Workshop.Presentation;

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

        [Header("Weapon Workshop")]
        [SerializeField] private WeaponCatalog _weaponCatalog;
        [SerializeField] private WeaponVisualProfile[] _weaponVisualProfiles;
        [SerializeField] private Game.Workshop.WorkshopUIController _workshopUIController;
        [SerializeField] private Game.Workshop.WorkshopBenchTrigger _workshopBenchTrigger;
        [SerializeField] private Game.Workshop.WorkshopFiringRangeTrigger _workshopFiringRangeTrigger;

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
        public WeaponCatalog WeaponCatalog => _weaponCatalog;
        public WeaponVisualProfile[] WeaponVisualProfiles => _weaponVisualProfiles;
        public PlayerSession PlayerSession { get; private set; }
        public SessionWeaponBuildStore BuildStore { get; private set; }
        public Game.Workshop.WorkshopUIController WorkshopUIController => _workshopUIController;
        public Game.Workshop.WorkshopBenchTrigger WorkshopBenchTrigger => _workshopBenchTrigger;
        public Game.Workshop.WorkshopFiringRangeTrigger WorkshopFiringRangeTrigger => _workshopFiringRangeTrigger;

        private void Awake()
        {
            SceneObjectBudget.EnsureForScene(gameObject.scene, gameObject);
            ComposeDependencies();
        }

        public void ComposeDependencies()
        {
            ResolveMissingReferences();
            WireDependencies();
        }

        public void BindAppContext(PlayerSession playerSession, SessionWeaponBuildStore buildStore)
        {
            PlayerSession = playerSession;
            BuildStore = buildStore;
        }

        private void ResolveMissingReferences()
        {
            if (_player == null)
            {
                _player = SceneComponents.Find<PlayerController>(gameObject.scene);
            }

            if (_gameStateController == null)
            {
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);
            }

            if (_timeController == null)
            {
                _timeController = SceneComponents.Find<TimeController>(gameObject.scene);
            }

            if (_scoreController == null)
            {
                _scoreController = SceneComponents.Find<ScoreController>(gameObject.scene);
            }

            if (_highScoreController == null)
            {
                _highScoreController = SceneComponents.Find<HighScoreController>(gameObject.scene);
            }

            if (_waveController == null)
            {
                _waveController = SceneComponents.Find<WaveController>(gameObject.scene);
            }

            if (_enemySpawner == null)
            {
                _enemySpawner = SceneComponents.Find<EnemySpawner>(gameObject.scene);
            }

            if (_worldResetManager == null)
            {
                _worldResetManager = SceneComponents.Find<WorldResetManager>(gameObject.scene);
            }

            if (_effectPool == null)
            {
                _effectPool = SceneComponents.Find<EffectPool>(gameObject.scene);
            }

            if (_mobileInput == null)
            {
                _mobileInput = SceneComponents.Find<MobileInputState>(gameObject.scene);
            }

            // UI Elements
            if (_mainMenuUI == null)
            {
                _mainMenuUI = SceneComponents.Find<MainMenuUI>(gameObject.scene);
            }

            if (_pauseUI == null)
            {
                _pauseUI = SceneComponents.Find<PauseUI>(gameObject.scene);
            }

            if (_gameOverUI == null)
            {
                _gameOverUI = SceneComponents.Find<GameOverUI>(gameObject.scene);
            }

            if (_victoryUI == null)
            {
                _victoryUI = SceneComponents.Find<VictoryUI>(gameObject.scene);
            }

            if (_scoreUI == null)
            {
                _scoreUI = SceneComponents.Find<ScoreUI>(gameObject.scene);
            }

            if (_waveUI == null)
            {
                _waveUI = SceneComponents.Find<WaveUI>(gameObject.scene);
            }

            if (_weaponHUD == null)
            {
                _weaponHUD = SceneComponents.Find<WeaponHUD>(gameObject.scene);
            }

            if (_highScoreUI == null)
            {
                _highScoreUI = SceneComponents.Find<HighScoreUI>(gameObject.scene);
            }

            if (_healthBarUI == null)
            {
                _healthBarUI = SceneComponents.Find<HealthBarUI>(gameObject.scene);
            }

            if (_bossHealthBarUI == null)
            {
                _bossHealthBarUI = SceneComponents.Find<BossHealthBarUI>(gameObject.scene);
            }

            if (_settingsUI == null)
            {
                _settingsUI = SceneComponents.Find<SettingsUI>(gameObject.scene);
            }

            if (_weaponCatalog == null)
            {
#if UNITY_EDITOR
                _weaponCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset");
#endif
                if (_weaponCatalog == null)
                {
                    var found = Resources.FindObjectsOfTypeAll<WeaponCatalog>();
                    if (found != null && found.Length > 0)
                    {
                        _weaponCatalog = found[0];
                    }
                }
            }

            if (_workshopUIController == null)
            {
                _workshopUIController = SceneComponents.Find<Game.Workshop.WorkshopUIController>(gameObject.scene);
            }

            if (_workshopBenchTrigger == null)
            {
                _workshopBenchTrigger = SceneComponents.Find<Game.Workshop.WorkshopBenchTrigger>(gameObject.scene);
            }

            if (_workshopFiringRangeTrigger == null)
            {
                _workshopFiringRangeTrigger = SceneComponents.Find<Game.Workshop.WorkshopFiringRangeTrigger>(gameObject.scene);
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
                _mainMenuUI.Initialize(_gameStateController, _worldResetManager, _settingsUI);
            }

            if (_pauseUI != null && _gameStateController != null)
            {
                _pauseUI.Initialize(_gameStateController, _settingsUI);
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

            ScreenShake shake = SceneComponents.Find<ScreenShake>(gameObject.scene);
            if (shake != null && _player != null)
            {
                HealthComponent playerHealth = _player.GetComponent<HealthComponent>();
                if (playerHealth != null)
                {
                    shake.Initialize(playerHealth);
                }
            }

            PlayerAudio playerAudio = SceneComponents.Find<PlayerAudio>(gameObject.scene);
            if (playerAudio != null && _player != null)
            {
                playerAudio.Initialize(_player);
            }

            GameStateAudio gameStateAudio = SceneComponents.Find<GameStateAudio>(gameObject.scene);
            if (gameStateAudio != null && _gameStateController != null)
            {
                gameStateAudio.Initialize(_gameStateController);
            }

            WaveAudio waveAudio = SceneComponents.Find<WaveAudio>(gameObject.scene);
            if (waveAudio != null && _waveController != null)
            {
                waveAudio.Initialize(_waveController);
            }

            EnemyAudio enemyAudio = SceneComponents.Find<EnemyAudio>(gameObject.scene);
            if (enemyAudio != null && _enemySpawner != null)
            {
                enemyAudio.Initialize(_enemySpawner);
            }

            if (_settingsUI != null && _player != null)
            {
                _settingsUI.Initialize(_player);
            }

            // Gun Audio Wiring & Stale Guard
            if (_player != null)
            {
                WeaponLoadout loadout = _player.GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    for (int i = 0; i < loadout.Count; i++)
                    {
                        Gun gun = loadout.Weapons[i];
                        if (gun != null)
                        {
                            GunAudio gunAudio = gun.GetComponent<GunAudio>();
                            if (gunAudio != null)
                            {
                                gunAudio.Initialize(gun);
                            }
                        }
                    }

                    loadout.WeaponAdded -= HandleWeaponAddedToLoadout;
                    loadout.WeaponAdded += HandleWeaponAddedToLoadout;
                }
                else
                {
                    PlayerShoot shoot = _player.GetComponent<PlayerShoot>();
                    if (shoot != null && shoot.ActiveGun != null)
                    {
                        GunAudio gunAudio = shoot.ActiveGun.GetComponent<GunAudio>();
                        if (gunAudio != null)
                        {
                            gunAudio.Initialize(shoot.ActiveGun);
                        }
                    }
                }
            }

            GunAudio[] allGunAudios = FindObjectsByType<GunAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allGunAudios.Length; i++)
            {
                if (allGunAudios[i] != null && allGunAudios[i].GetComponent<Gun>() == null && allGunAudios[i].GetComponentInParent<Gun>() == null)
                {
                    // Stale/duplicate fixed-gun playback object in scene
                    allGunAudios[i].gameObject.SetActive(false);
                    if (UnityEngine.Application.isPlaying)
                    {
                        Destroy(allGunAudios[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(allGunAudios[i].gameObject);
                    }
                }
            }

            // Weapon Workshop Wiring
            if (_weaponCatalog != null)
            {
                Game.Workshop.WeaponBuildApplier.DefaultCatalog = _weaponCatalog;
            }

            if (_workshopUIController != null && _gameStateController != null)
            {
                _workshopUIController.Initialize(_gameStateController);
            }

            if (_workshopBenchTrigger != null && _gameStateController != null)
            {
                _workshopBenchTrigger.Initialize(_gameStateController, mobileInput: _mobileInput);
            }

            if (_workshopFiringRangeTrigger != null && _gameStateController != null)
            {
                _workshopFiringRangeTrigger.Initialize(_gameStateController);
            }
        }

        private void HandleWeaponAddedToLoadout(Gun gun, int index)
        {
            if (gun != null)
            {
                GunAudio gunAudio = gun.GetComponent<GunAudio>();
                if (gunAudio != null)
                {
                    gunAudio.Initialize(gun);
                }
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                WeaponLoadout loadout = _player.GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    loadout.WeaponAdded -= HandleWeaponAddedToLoadout;
                }
            }
        }
    }
}
