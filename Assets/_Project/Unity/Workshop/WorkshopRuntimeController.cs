using Application.Weapons;
using Application.Workshop;
using Application;
using Game;
using Game.Flow;
using System.Linq;
using UnityEngine;

namespace Game.Workshop
{
    /// <summary>
    /// T05: Constructs and binds a WorkshopSession for the selected gun and
    /// preview, connects the WorkshopUIController, and cleans up on exit.
    /// Lives in the hub scene beside existing workshop adapters.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class WorkshopRuntimeController : MonoBehaviour
    {
        [Header("Catalog & Build")]
        [SerializeField] private Game.WeaponCatalog _catalogAsset;

        [Header("UI & Presentation")]
        [SerializeField] private WorkshopUIController _uiController;
        [SerializeField] private Presentation.WeaponPreviewView _previewView;

        [Header("Combat Integration")]
        [SerializeField] private WeaponCombatAdapter _combatAdapter;

        // Runtime
        private WorkshopSession _activeSession;
        private IWeaponBuildStore _buildStore;
        private WeaponLoadout _loadout;
        private GameStateController _gameState;

        // ── Public session accessor ────────────────────────────────────────
        public IWeaponWorkshopSession ActiveSession => _activeSession;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            ResolveReferences();
            _buildStore = new SaveManagerWeaponBuildStore();
        }

        public void Initialize(GameStateController gameState, WeaponLoadout loadout, IWeaponBuildStore buildStore = null)
        {
            if (_gameState != null) _gameState.OnStateChanged -= HandleGameStateChanged;
            if (_loadout != null)
            {
                _loadout.WeaponEquipped -= HandleWeaponEquipped;
                _loadout.WeaponAdded -= HandleWeaponAdded;
            }
            _gameState = gameState;
            _loadout = loadout;
            if (_catalogAsset == null)
                _catalogAsset = WeaponBuildApplier.DefaultCatalog as Game.WeaponCatalog;
            if (buildStore != null) _buildStore = buildStore;
            if (_gameState != null) _gameState.OnStateChanged += HandleGameStateChanged;
            if (_loadout != null)
            {
                _loadout.WeaponEquipped += HandleWeaponEquipped;
                _loadout.WeaponAdded += HandleWeaponAdded;
            }
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopEditing)
                OpenSessionForActiveGun(_loadout);
        }

        private void ResolveReferences()
        {
            if (_catalogAsset == null)
                _catalogAsset = WeaponBuildApplier.DefaultCatalog as Game.WeaponCatalog;

            if (_uiController == null)
                _uiController = SceneComponents.Find<WorkshopUIController>(gameObject.scene);

            if (_previewView == null)
                _previewView = SceneComponents.Find<Presentation.WeaponPreviewView>(gameObject.scene);

            if (_combatAdapter == null)
                _combatAdapter = SceneComponents.Find<WeaponCombatAdapter>(gameObject.scene);
            if (_combatAdapter == null)
                _combatAdapter = gameObject.AddComponent<WeaponCombatAdapter>();
        }

        // ── Session management ─────────────────────────────────────────────

        /// <summary>
        /// Open a workshop session for the given weapon.
        /// Unbinds any previous session first.
        /// </summary>
        public void OpenSession(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                Debug.LogWarning("[WorkshopRuntimeController] OpenSession called with empty weaponId.");
                return;
            }

            if (_catalogAsset == null)
            {
                Debug.LogError("[WorkshopRuntimeController] No catalog asset — cannot open workshop session.");
                return;
            }

            CloseSession();

            // Load or create the initial build
            WeaponBuild initialBuild = null;
            var loadResult = _buildStore?.Load(weaponId);
            if (loadResult != null && loadResult.IsSuccess && loadResult.Build != null)
                initialBuild = loadResult.Build;

            var resolver = new WeaponBuildResolver();

            _activeSession = new WorkshopSession(
                weaponId,
                initialBuild,
                _catalogAsset,
                resolver,
                _buildStore,
                _combatAdapter);

            // Bind UI
            if (_uiController != null)
                _uiController.Bind(_activeSession, _previewView, _catalogAsset);
        }

        /// <summary>
        /// Open a session for the gun currently active on the supplied loadout.
        /// </summary>
        public void OpenSessionForActiveGun(WeaponLoadout loadout)
        {
            if (loadout == null || loadout.ActiveGun == null)
            {
                Debug.LogWarning("[WorkshopRuntimeController] No active gun in loadout.");
                return;
            }

            CloseSession();
            Gun activeGun = loadout.ActiveGun;
            string weaponId = activeGun.WeaponId;
            if (_combatAdapter != null)
                _combatAdapter.Bind(activeGun, activeGun.GetComponentInParent<PlayerController>()?.GetComponent<PlayerRotation>());
            if (_previewView != null)
                _previewView.VisualProfile = WeaponBuildApplier.GetVisualProfile(weaponId);
            OpenSession(weaponId);
            if (_previewView != null && _activeSession != null)
            {
                _previewView.gameObject.SetActive(true);
                _previewView.ShowBuild(_activeSession.DraftBuild);
            }
        }

        /// <summary>
        /// Discard unapplied draft edits and unbind session.
        /// Committed builds are preserved; only uncommitted draft is discarded.
        /// </summary>
        public void CloseSession()
        {
            if (_activeSession == null) return;

            // Discard unapplied draft changes only
            if (_activeSession.HasUnappliedChanges)
                _activeSession.Discard();

            if (_uiController != null)
                _uiController.Unbind();

            _activeSession = null;
            RetryPendingCommittedSaves();

            // Disable preview rendering
            if (_previewView != null)
                _previewView.gameObject.SetActive(false);
        }

        public int RetryPendingCommittedSaves()
        {
            return AppCompositionRoot.Instance != null
                ? AppCompositionRoot.Instance.RetryPendingBuildSaves()
                : 0;
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.WorkshopEditing)
                OpenSessionForActiveGun(_loadout);
            else if (oldState == GameState.WorkshopEditing)
                CloseSession();
        }

        private void HandleWeaponEquipped(Gun gun, int index)
        {
            if (gun != null)
                AppCompositionRoot.Instance?.PlayerSession?.SetEquippedWeapon(gun.WeaponId);

            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopEditing)
                OpenSessionForActiveGun(_loadout);
        }

        private void HandleWeaponAdded(Gun gun, int index)
        {
            AppCompositionRoot.Instance?.PlayerSession?.SetLoadout(
                _loadout != null ? _loadout.Weapons.Where(weapon => weapon != null).Select(weapon => weapon.WeaponId) : null,
                _loadout != null && _loadout.ActiveGun != null ? _loadout.ActiveGun.WeaponId : null);
        }

        private void OnDestroy()
        {
            if (_gameState != null) _gameState.OnStateChanged -= HandleGameStateChanged;
            if (_loadout != null)
            {
                _loadout.WeaponEquipped -= HandleWeaponEquipped;
                _loadout.WeaponAdded -= HandleWeaponAdded;
            }
            CloseSession();
        }
    }
}
