using Application.Flow;
using Application.Weapons;
using System.Collections.Generic;
using Game.Workshop;
using Game.Workshop.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Flow
{
    /// <summary>
    /// Boot-scene singleton. Initializes the app-lifetime services once,
    /// injects them into playable scene roots via SceneFlowController.
    /// Must run before everything else (ExecutionOrder -200).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class AppCompositionRoot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SceneFlowController _sceneFlow;
        [SerializeField] private EventSystem _bootEventSystem;
        [SerializeField] private Game.WeaponCatalog _weaponCatalog;
        [SerializeField] private WeaponVisualProfile[] _weaponVisualProfiles;

        // App-lifetime singletons
        public PlayerSession PlayerSession { get; private set; }
        public GameFlowCoordinator FlowCoordinator { get; private set; }
        public SessionWeaponBuildStore BuildStore { get; private set; }
        public SceneFlowController SceneFlow => _sceneFlow;

        private static AppCompositionRoot _instance;

        public static AppCompositionRoot Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            PlayerSession = new PlayerSession();
            FlowCoordinator = new GameFlowCoordinator();
            FlowCoordinator.OnRunCompleted += HandleRunCompleted;
            BuildStore = new SessionWeaponBuildStore(PlayerSession, new SaveManagerWeaponBuildStore());
            RegisterWeaponServices(_weaponCatalog, _weaponVisualProfiles);
            RunFinalizer.ManagedFinalizationEnabled = true;
            RunFinalizer.RetryPending();

            if (_sceneFlow == null)
                _sceneFlow = GetComponent<SceneFlowController>();

            if (_sceneFlow != null)
                _sceneFlow.Initialize(this);
        }

        private void Start()
        {
            _sceneFlow?.GoToHub();
        }

        public void RegisterWeaponServices(Game.WeaponCatalog catalog, IEnumerable<WeaponVisualProfile> visualProfiles)
        {
            if (catalog != null)
            {
                _weaponCatalog = catalog;
                WeaponBuildApplier.SetCatalog(catalog);
            }
            WeaponBuildApplier.SetStore(BuildStore);
            if (visualProfiles != null)
            {
                foreach (WeaponVisualProfile profile in visualProfiles)
                    WeaponBuildApplier.RegisterVisualProfile(profile);
            }

            if (_weaponCatalog == null) return;
            var resolver = new WeaponBuildResolver();
            foreach (string weaponId in _weaponCatalog.GetAllWeaponIds())
            {
                if (PlayerSession.GetCommittedBuild(weaponId) != null) continue;
                BuildLoadResult loaded = BuildStore.Load(weaponId);
                WeaponBuild build = loaded.IsSuccess ? loaded.Build : null;
                if (build == null && _weaponCatalog.TryGetPlatform(weaponId, out WeaponPlatformSpec platform))
                    build = platform.CreateDefaultBuild();
                if (build == null) continue;

                build = resolver.Normalize(build, _weaponCatalog, out bool repaired);
                PlayerSession.SetDurableBuild(weaponId, build);
                if (loaded.IsSuccess && repaired) BuildStore.Save(build);
            }
        }

        public int RetryPendingBuildSaves()
        {
            return BuildStore != null ? BuildStore.RetryPendingSaves() : 0;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                if (FlowCoordinator != null)
                    FlowCoordinator.OnRunCompleted -= HandleRunCompleted;
                _instance = null;
                RunFinalizer.ManagedFinalizationEnabled = false;
            }
        }

        private void HandleRunCompleted(RunResult result)
        {
            RunFinalizer.TryFinalize(result);
        }

        /// <summary>
        /// Called by scene roots so they can get the app-level context.
        /// </summary>
        public void InjectIntoSceneRoot(GameCompositionRoot root)
        {
            if (root == null) return;
            root.BindAppContext(PlayerSession, BuildStore);
            RegisterWeaponServices(root.WeaponCatalog, root.WeaponVisualProfiles);
        }
    }
}
