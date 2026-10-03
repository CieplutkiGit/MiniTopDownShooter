using System;
using Application.Economy;
using Game.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Economy
{
    /// <summary>
    /// Bootstraps production economy hooks, drops, and policy wiring at runtime.
    /// Runs automatically via RuntimeInitializeOnLoadMethod.
    /// </summary>
    [DefaultExecutionOrder(-190)]
    public class EconomyProductionBootstrapper : MonoBehaviour
    {
        private const int NoActiveArenaSceneHandle = -1;
        private static EconomyProductionBootstrapper s_instance;
        public static EconomyProductionBootstrapper Instance => s_instance;

        private SalvagePickupPool _pickupPool;
        private EnemySalvageDropManager _dropManager;
        private RunSalvageTracker _salvageTracker;
        private int _activeArenaSceneHandle = NoActiveArenaSceneHandle;

        public int ActiveArenaSceneHandle => _activeArenaSceneHandle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            s_instance = null;
            WeaponLoadout.ActivePolicy = null;
            SaveManagerWeaponBuildStore.ActivePolicy = null;
            EconomyPolicyProvider.ResetForTesting();
            EconomyPricingPolicy.ResetForTesting();
            UnityEconomyService.ResetInstance();
            RunSalvageTracker.ResetForTesting();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeProduction()
        {
            if (s_instance != null) return;

            GameObject root = new GameObject("EconomyProductionBootstrapper");
            DontDestroyOnLoad(root);
            s_instance = root.AddComponent<EconomyProductionBootstrapper>();
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);

            // Wire real economy policy into production systems
            WireProductionPolicy();

            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;

            // Initialize for current active scene
            SetupForScene(SceneManager.GetActiveScene());
        }

        public static void WireProductionPolicy()
        {
            EconomyPricingPolicy.SetCatalogProvider(() => Game.Workshop.WeaponBuildApplier.DefaultCatalog);
            var productionPolicy = UnityEconomyService.Instance;
            WeaponLoadout.ActivePolicy = productionPolicy;
            SaveManagerWeaponBuildStore.ActivePolicy = productionPolicy;
            EconomyPolicyProvider.SetDefaultPolicyProvider(() => UnityEconomyService.Instance);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetupForScene(scene);
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (ShouldTeardownArenaForSceneUnload(_activeArenaSceneHandle, scene.handle))
            {
                TeardownArenaEconomyComponents();
            }
        }

        public static bool ShouldTeardownArenaForSceneUnload(int activeArenaSceneHandle, int unloadedSceneHandle)
        {
            return activeArenaSceneHandle != NoActiveArenaSceneHandle &&
                   activeArenaSceneHandle == unloadedSceneHandle;
        }

        public static bool IsArenaScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            string name = scene.name;
            if (string.IsNullOrEmpty(name)) return false;

            if (name.IndexOf("Hub", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Lobby", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("WeaponEdit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Workshop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            EnemySpawner spawner = SceneComponents.Find<EnemySpawner>(scene);
            if (spawner != null) return true;

            EnemyController enemy = SceneComponents.Find<EnemyController>(scene);
            return enemy != null;
        }

        public void SetupForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            // Ensure production policy is active
            WireProductionPolicy();

            if (IsArenaScene(scene))
            {
                PlayerController player = SceneComponents.Find<PlayerController>(scene);
                EnsureArenaEconomyComponents(scene, player != null ? player.transform : null);
            }
        }

        private void EnsureArenaEconomyComponents(Scene scene, Transform player)
        {
            if (_salvageTracker == null)
            {
                _salvageTracker = gameObject.GetComponent<RunSalvageTracker>();
                if (_salvageTracker == null)
                {
                    _salvageTracker = gameObject.AddComponent<RunSalvageTracker>();
                }
            }
            bool isNewArenaScene = _activeArenaSceneHandle != scene.handle;
            if (isNewArenaScene) _salvageTracker.ResetTracker();

            if (_pickupPool == null)
            {
                _pickupPool = gameObject.GetComponent<SalvagePickupPool>();
                if (_pickupPool == null)
                {
                    _pickupPool = gameObject.AddComponent<SalvagePickupPool>();
                }
            }

            if (_dropManager == null)
            {
                _dropManager = gameObject.GetComponent<EnemySalvageDropManager>();
                if (_dropManager == null)
                {
                    _dropManager = gameObject.AddComponent<EnemySalvageDropManager>();
                }
            }

            _dropManager.enabled = true;
            _dropManager.Initialize(_pickupPool, player);
            _salvageTracker.BindPool(_pickupPool);
            _activeArenaSceneHandle = scene.handle;
        }

        public void TeardownArenaEconomyComponents()
        {
            _activeArenaSceneHandle = NoActiveArenaSceneHandle;
            if (_dropManager != null)
            {
                _dropManager.ClearAndReset();
                _dropManager.enabled = false;
            }
            if (_pickupPool != null)
            {
                _pickupPool.ClearAll();
            }
            if (_salvageTracker != null)
            {
                _salvageTracker.UnbindPool();
                _salvageTracker.EndRun();
            }
        }

        public static void ResetForTesting()
        {
            if (s_instance != null)
            {
                s_instance.TeardownArenaEconomyComponents();
                if (s_instance.gameObject != null)
                {
#if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                    {
                        DestroyImmediate(s_instance.gameObject);
                    }
                    else
#endif
                    {
                        Destroy(s_instance.gameObject);
                    }
                }
                s_instance = null;
            }

            WeaponLoadout.ActivePolicy = null;
            SaveManagerWeaponBuildStore.ActivePolicy = null;
            EconomyPolicyProvider.ResetForTesting();
            EconomyPricingPolicy.ResetForTesting();
            UnityEconomyService.ResetInstance();
            RunSalvageTracker.ResetForTesting();
            RunFinalizer.ResetForTesting();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            TeardownArenaEconomyComponents();
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
