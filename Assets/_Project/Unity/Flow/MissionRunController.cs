using Application;
using Application.Flow;
using System.Collections;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// T08: Prepares the arena scene for a mission run.
    /// Applies deployment snapshot to the loadout, resets health/ammo,
    /// starts the WaveController, and reports a terminal outcome to GameFlowCoordinator.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class MissionRunController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameCompositionRoot _sceneRoot;
        [SerializeField] private WaveController _waveController;
        [SerializeField] private GameStateController _gameStateController;
        [SerializeField] private MissionDefinition _missionDefinition;
        [SerializeField] private ParticleSystem[] _missionEffects;

        [Header("UI")]
        [SerializeField] private MissionResultsUI _resultsUI;

        private AppCompositionRoot _app;
        private Application.Flow.RunSession _activeRun;
        private EnemySpawner _enemySpawner;
        private bool _missionStarted;
        private bool _missionPrepared;
        private bool _outcomeReported;
        private bool _combatEventsSubscribed;
        private bool _abandonRequested;
        private RunOutcome? _pendingOutcome;
        private int _kills;
        private int _wavesCleared;

        public bool IsMissionPrepared => _missionPrepared;
        public bool IsMissionStarted => _missionStarted;

        private void Start()
        {
            _app = AppCompositionRoot.Instance;

            if (_sceneRoot == null)
                _sceneRoot = SceneComponents.Find<GameCompositionRoot>(gameObject.scene);

            if (_waveController == null)
                _waveController = SceneComponents.Find<WaveController>(gameObject.scene);

            if (_gameStateController == null)
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);

            _enemySpawner = _sceneRoot != null && _sceneRoot.EnemySpawner != null
                ? _sceneRoot.EnemySpawner
                : SceneComponents.Find<EnemySpawner>(gameObject.scene);

            if (_app != null)
            {
                _activeRun = _app.FlowCoordinator.ActiveRun;
                _app.FlowCoordinator.OnRunCompleted += HandleRunCompleted;
            }

            SubscribeCombatEvents();

            StartCoroutine(BeginMissionWhenReady());
        }

        private void OnDestroy()
        {
            if (_app != null)
                _app.FlowCoordinator.OnRunCompleted -= HandleRunCompleted;
            UnsubscribeCombatEvents();
        }

        // ── Mission setup ─────────────────────────────────────────────────

        private IEnumerator BeginMissionWhenReady()
        {
            float timeout = Time.realtimeSinceStartup + 0.5f;
            while (_app != null &&
                   (_app.FlowCoordinator.ActiveRun == null ||
                    _app.FlowCoordinator.FlowState != GameFlowState.InMission))
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    break;
                }
                yield return null;
            }

            // Let scene roots and loadouts finish their Start initialization before applying the run snapshot.
            yield return null;
            if (!PrepareMission())
            {
                Debug.LogError("Mission setup failed; combat was not started.", this);
                _app?.FlowCoordinator.TryReportRunOutcome(RunOutcome.TechnicalError);
                yield break;
            }
            StartMission();
        }

        /// <summary>
        /// Prepares the run after SceneFlowController has notified the coordinator that the mission scene loaded.
        /// </summary>
        public bool PrepareMission()
        {
            if (_missionPrepared) return true;
            if (_app != null && _app.FlowCoordinator.ActiveRun != null &&
                _app.FlowCoordinator.FlowState != GameFlowState.InMission) return false;

            _activeRun = _app?.FlowCoordinator.ActiveRun;

            if (_sceneRoot != null && _sceneRoot.Player != null)
            {
                PlayerController player = _sceneRoot.Player;
                player.ResetToSpawn(player.transform.position, player.transform.rotation);
            }

            DeploymentLoadoutSnapshot snapshot = _app?.FlowCoordinator.ActiveRun?.DeploymentLoadout;
            _sceneRoot?.EnsureOwnedWeapons();

            if (snapshot != null && snapshot.OrderedWeaponIds.Count > 0)
            {
                PlayerController player = _sceneRoot != null ? _sceneRoot.Player : null;
                WeaponLoadout loadout = player != null ? player.GetComponent<WeaponLoadout>() : null;
                var catalog = _sceneRoot != null
                    ? _sceneRoot.WeaponCatalog ?? Game.Workshop.WeaponBuildApplier.DefaultCatalog
                    : Game.Workshop.WeaponBuildApplier.DefaultCatalog;
                if (loadout == null || catalog == null || !loadout.ApplyDeploymentSnapshot(snapshot, catalog))
                    return false;
            }
            else
            {
                PlayerController player = _sceneRoot != null ? _sceneRoot.Player : null;
                WeaponLoadout loadout = player != null ? player.GetComponent<WeaponLoadout>() : null;
                if (loadout != null && loadout.ActiveGun == null)
                {
                    loadout.EquipDefaultSlot();
                }
            }

            HealthComponent health = _sceneRoot?.Player != null
                ? _sceneRoot.Player.GetComponent<HealthComponent>()
                : null;
            health?.ResetHealth();
            PrewarmMissionPools();
            if (_activeRun != null)
                global::Game.Economy.RunSalvageTracker.Instance?.BeginRun(_activeRun.RunId);
            _missionPrepared = true;
            return true;
        }

        private void PrewarmMissionPools()
        {
            WaveSet waves = _missionDefinition != null ? _missionDefinition.WaveSet : _waveController?.WaveSet;
            if (_enemySpawner != null && waves != null)
            {
                var warmed = new System.Collections.Generic.HashSet<EnemyController>();
                foreach (WaveConfig wave in waves.Waves)
                {
                    foreach (WaveEnemyGroup group in wave.EnemyGroups)
                        if (group.Prefab != null && warmed.Add(group.Prefab))
                            _enemySpawner.PrewarmVariant(group.Prefab, 4);
                    if (wave.BossPrefab != null && warmed.Add(wave.BossPrefab))
                        _enemySpawner.PrewarmVariant(wave.BossPrefab, 1);
                }
            }
            EffectPool effects = SceneComponents.Find<EffectPool>(gameObject.scene);
            if (effects != null && _missionEffects != null)
                foreach (ParticleSystem effect in _missionEffects) effects.Prewarm(effect, 4);
        }

        public void StartMission()
        {
            if (_missionStarted) return;
            if (!_missionPrepared && !PrepareMission()) return;
            _missionStarted = true;

            // Subscribe before StartGame can synchronously start the first wave or end the run.
            SubscribeCombatEvents();

            // Start combat
            if (_gameStateController != null)
                _gameStateController.StartGame();
        }

        private void SubscribeCombatEvents()
        {
            if (_combatEventsSubscribed) return;
            _combatEventsSubscribed = true;
            if (_gameStateController != null)
                _gameStateController.OnStateChanged += HandleStateChanged;
            if (_enemySpawner != null)
                _enemySpawner.EnemyKilled += HandleEnemyKilled;
            if (_waveController != null)
            {
                _waveController.WaveCompleted += HandleWaveCompleted;
                _waveController.TechnicalFailure += HandleTechnicalFailure;
            }
        }

        private void UnsubscribeCombatEvents()
        {
            if (!_combatEventsSubscribed) return;
            _combatEventsSubscribed = false;
            if (_gameStateController != null)
                _gameStateController.OnStateChanged -= HandleStateChanged;
            if (_enemySpawner != null)
                _enemySpawner.EnemyKilled -= HandleEnemyKilled;
            if (_waveController != null)
            {
                _waveController.WaveCompleted -= HandleWaveCompleted;
                _waveController.TechnicalFailure -= HandleTechnicalFailure;
            }
        }

        // ── Run tracking ──────────────────────────────────────────────────

        private void Update()
        {
            if (_pendingOutcome.HasValue && !_outcomeReported)
                ReportOutcome(_pendingOutcome.Value);
            if (_abandonRequested && _outcomeReported)
                TryFinishAbandon();

            // Track active time while Playing
            if (_app?.FlowCoordinator.ActiveRun != null &&
                _gameStateController != null &&
                _gameStateController.CurrentState == GameState.Playing)
            {
                _app.FlowCoordinator.ActiveRun.RecordActiveTime(Time.deltaTime);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_outcomeReported) return;

            if (newState == GameState.Victory)
            {
                ReportOutcome(RunOutcome.Victory);
            }
            else if (newState == GameState.GameOver)
            {
                ReportOutcome(_waveController != null && !string.IsNullOrEmpty(_waveController.SpawnErrorReason)
                    ? RunOutcome.TechnicalError
                    : RunOutcome.Defeat);
            }
        }

        private void HandleTechnicalFailure(string reason)
        {
            ReportOutcome(RunOutcome.TechnicalError);
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            _kills++;
            _activeRun?.AddKill();
            SyncRunStats();
        }

        private void HandleWaveCompleted(int waveNumber)
        {
            _wavesCleared++;
            _activeRun?.SetWavesCleared(_wavesCleared);
        }

        private void SyncRunStats()
        {
            if (_activeRun == null) return;
            _activeRun.SetScore(_sceneRoot?.ScoreController != null ? _sceneRoot.ScoreController.Score : 0);
            _activeRun.SetKills(_kills);
            _activeRun.SetWavesCleared(_wavesCleared);
            var salvage = global::Game.Economy.RunSalvageTracker.Instance;
            if (salvage != null && salvage.RunId == _activeRun.RunId)
                _activeRun.SetSalvage(salvage.ScrapCollected, salvage.AlloyCollected, salvage.CoreCollected);
        }

        private void ReportOutcome(RunOutcome outcome)
        {
            if (_outcomeReported || _app == null) return;
            _pendingOutcome = outcome;
            SyncRunStats();
            int score = _activeRun?.Score ?? (_sceneRoot?.ScoreController != null ? _sceneRoot.ScoreController.Score : 0);
            int kills = _activeRun?.Kills ?? _kills;
            int waves = _activeRun?.WavesCleared ?? _wavesCleared;
            if (_app.FlowCoordinator.TryReportRunOutcome(outcome, score, kills, waves))
            {
                _outcomeReported = true;
                _pendingOutcome = null;
            }
        }

        private void HandleRunCompleted(Application.Flow.RunResult result)
        {
            // Show results UI
            if (_resultsUI != null)
                _resultsUI.Show(result);
        }

        // ── Abandon (from pause menu) ─────────────────────────────────────
        public void AbandonMission()
        {
            if (_app == null || _outcomeReported) return;
            _abandonRequested = true;
            SyncRunStats();
            ReportOutcome(RunOutcome.Abandoned);
            if (_outcomeReported) TryFinishAbandon();
        }

        private void TryFinishAbandon()
        {
            if (_app == null || !_abandonRequested || _app.SceneFlow == null ||
                _app.SceneFlow.IsTransitioning || !_app.FlowCoordinator.TryReturnToBase()) return;
            _abandonRequested = false;
            _app.SceneFlow.GoToHub();
        }
    }
}
