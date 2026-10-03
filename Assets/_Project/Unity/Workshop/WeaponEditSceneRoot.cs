using System.Collections;
using Application.Flow;
using Application.Weapons;
using Application.Workshop;
using Game.Flow;
using Game.Workshop.Presentation;
using Game.Workshop.UI;
using UnityEngine;

namespace Game.Workshop
{
    /// <summary>
    /// Composition root for the dedicated WeaponEdit scene.
    /// Manages the workshop session, weapon preview, pinch/rotate controller,
    /// and mobile weapon customization UI.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class WeaponEditSceneRoot : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameCompositionRoot _gameCompositionRoot;
        [SerializeField] private WeaponPreviewView _previewView;
        [SerializeField] private WeaponPinchRotateController _rotateController;
        [SerializeField] private WeaponEditUI _editUI;
        [SerializeField] private Game.WeaponCatalog _catalogAsset;

        private AppCompositionRoot _app;
        private WorkshopSession _activeSession;
        private IWeaponBuildStore _buildStore;
        private string _activeWeaponId;

        public bool IsInitialized { get; private set; }
        public IWeaponWorkshopSession ActiveSession => _activeSession;
        public WeaponPreviewView PreviewView => _previewView;

        private IEnumerator Start()
        {
            _app = AppCompositionRoot.Instance;
            yield return null;

            if (_gameCompositionRoot == null)
                _gameCompositionRoot = SceneComponents.Find<GameCompositionRoot>(gameObject.scene);

            if (_previewView == null)
                _previewView = SceneComponents.Find<WeaponPreviewView>(gameObject.scene);

            if (_rotateController == null)
                _rotateController = SceneComponents.Find<WeaponPinchRotateController>(gameObject.scene);

            if (_editUI == null)
                _editUI = SceneComponents.Find<WeaponEditUI>(gameObject.scene);

            if (_catalogAsset == null)
                _catalogAsset = (_gameCompositionRoot?.WeaponCatalog) ?? (WeaponBuildApplier.DefaultCatalog as Game.WeaponCatalog);

            if (_catalogAsset != null)
                WeaponBuildApplier.SetCatalog(_catalogAsset);

            if (_gameCompositionRoot != null && _gameCompositionRoot.WeaponVisualProfiles != null)
            {
                foreach (var prof in _gameCompositionRoot.WeaponVisualProfiles)
                {
                    if (prof != null) WeaponBuildApplier.RegisterVisualProfile(prof);
                }
            }

            _buildStore = (_app?.BuildStore != null)
                ? (IWeaponBuildStore)_app.BuildStore
                : new SaveManagerWeaponBuildStore();

            // Determine active weapon to edit
            _activeWeaponId = _app?.PlayerSession?.EquippedWeaponId;
            if (string.IsNullOrEmpty(_activeWeaponId))
                _activeWeaponId = "Rifle";

            OpenSession(_activeWeaponId);

            if (_editUI != null)
            {
                _editUI.OnBackClicked += HandleBackRequested;
            }

            IsInitialized = true;
        }

        public void OpenSession(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
                return;

            _activeWeaponId = weaponId;
            CloseSession();

            if (_catalogAsset == null)
            {
                Debug.LogError("[WeaponEditSceneRoot] No catalog asset available.");
                return;
            }

            // Load initial build
            WeaponBuild initialBuild = null;
            var loadResult = _buildStore?.Load(weaponId);
            if (loadResult != null && loadResult.IsSuccess && loadResult.Build != null)
            {
                initialBuild = loadResult.Build;
            }

            var resolver = new WeaponBuildResolver();
            _activeSession = new WorkshopSession(
                weaponId,
                initialBuild,
                _catalogAsset,
                resolver,
                _buildStore);

            // Setup preview
            if (_previewView != null)
            {
                var profile = WeaponBuildApplier.GetVisualProfile(weaponId);
                if (profile == null && _gameCompositionRoot?.WeaponVisualProfiles != null)
                {
                    foreach (var p in _gameCompositionRoot.WeaponVisualProfiles)
                    {
                        if (p != null && string.Equals(p.WeaponId, weaponId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            profile = p;
                            WeaponBuildApplier.RegisterVisualProfile(p);
                            break;
                        }
                    }
                }
                _previewView.VisualProfile = profile;
                _previewView.gameObject.SetActive(true);
                _previewView.ShowBuild(_activeSession.DraftBuild);
            }

            // Setup UI
            if (_editUI != null)
            {
                _editUI.Bind(_activeSession, _previewView, _catalogAsset);
            }
        }

        public void CloseSession()
        {
            if (_activeSession == null) return;

            if (_activeSession.HasUnappliedChanges)
                _activeSession.Discard();

            if (_editUI != null)
                _editUI.Unbind();

            _activeSession = null;
        }

        public void HandleBackRequested()
        {
            CloseSession();
            if (_app != null && _app.SceneFlow != null)
            {
                _app.SceneFlow.GoToHub();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("BaseHub");
            }
        }

        private void OnDestroy()
        {
            if (_editUI != null)
            {
                _editUI.OnBackClicked -= HandleBackRequested;
            }
            CloseSession();
        }
    }
}
