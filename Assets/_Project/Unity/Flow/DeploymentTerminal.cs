using Application.Flow;
using Application;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Flow
{
    /// <summary>
    /// T08: Deployment terminal in the hub scene.
    /// Shows briefing/loadout info and initiates a deploy via GameFlowCoordinator.
    /// Blocks deployment when there are pending unsaved builds.
    /// </summary>
    public class DeploymentTerminal : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _briefingPanel;
        [SerializeField] private TMP_Text _missionNameText;
        [SerializeField] private TMP_Text _loadoutSummaryText;
        [SerializeField] private TMP_Text _pendingSaveWarningText;
        [SerializeField] private Button _deployButton;
        [SerializeField] private Button _cancelButton;

        [Header("Mission")]
        [SerializeField] private MissionDefinition _missionDefinition;

        private GameFlowCoordinator _flowCoordinator;
        private PlayerSession _playerSession;
        private GameStateController _gameStateController;
        private bool _isOpen;

        public void Initialize(GameFlowCoordinator coordinator, PlayerSession playerSession)
        {
            _flowCoordinator = coordinator;
            _playerSession = playerSession;
            _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);

            if (_deployButton != null)
            {
                _deployButton.onClick.RemoveAllListeners();
                _deployButton.onClick.AddListener(HandleDeploy);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(CloseBriefing);
            }

            if (_briefingPanel != null)
                _briefingPanel.SetActive(false);
        }

        /// <summary>Called when player interacts with the deployment terminal.</summary>
        public void OpenBriefing()
        {
            AppCompositionRoot.Instance?.RetryPendingBuildSaves();
            if (_gameStateController == null)
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);
            _gameStateController?.EnterDeploymentBriefing();
            _isOpen = true;
            RefreshUI();
            if (_briefingPanel != null)
                _briefingPanel.SetActive(true);
        }

        public void CloseBriefing()
        {
            _isOpen = false;
            if (_gameStateController == null)
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);
            if (_gameStateController != null && _gameStateController.CurrentState == GameState.DeploymentBriefing)
                _gameStateController.EnterWorkshopRoaming();
            if (_briefingPanel != null)
                _briefingPanel.SetActive(false);
        }

        public void HandleDeploy()
        {
            if (_flowCoordinator == null) return;
            SceneFlowController sceneFlow = AppCompositionRoot.Instance?.SceneFlow;
            if (sceneFlow == null || sceneFlow.IsTransitioning) return;
            if (_playerSession != null && _playerSession.HasPendingSaves)
            {
                Debug.LogWarning("[DeploymentTerminal] Cannot deploy: there are pending unsaved builds.");
                return;
            }

            string missionId = _missionDefinition != null ? _missionDefinition.MissionId : "Mission_ArenaSweep";
            DeploymentLoadoutSnapshot snapshot = _playerSession?.CreateDeploymentSnapshot(Game.Workshop.WeaponBuildApplier.DefaultCatalog)
                                                 ?? DeploymentLoadoutSnapshot.Empty;
            if (snapshot.OrderedWeaponIds.Count == 0)
            {
                Debug.LogWarning("[DeploymentTerminal] Cannot deploy: the loadout snapshot is empty.");
                RefreshUI();
                return;
            }

            if (_flowCoordinator.TryDeploy(missionId, snapshot))
            {
                CloseBriefing();
                string sceneName = _missionDefinition != null ? _missionDefinition.SceneName : null;
                sceneFlow.GoToArena(sceneName);
            }
        }

        private void RefreshUI()
        {
            if (_missionNameText != null && _missionDefinition != null)
                _missionNameText.text = _missionDefinition.DisplayName;

            bool hasPending = _playerSession != null && _playerSession.HasPendingSaves;

            if (_pendingSaveWarningText != null)
            {
                _pendingSaveWarningText.gameObject.SetActive(hasPending);
                _pendingSaveWarningText.text = hasPending ? "Warning: unsaved builds. Save before deploying." : "";
            }

            DeploymentLoadoutSnapshot snapshot = _playerSession?.CreateDeploymentSnapshot()
                                                 ?? DeploymentLoadoutSnapshot.Empty;
            if (_loadoutSummaryText != null)
                _loadoutSummaryText.text = snapshot.OrderedWeaponIds.Count == 0
                    ? "No weapons equipped"
                    : "Loadout: " + string.Join(", ", snapshot.OrderedWeaponIds);

            if (_deployButton != null)
                _deployButton.interactable = !hasPending && snapshot.OrderedWeaponIds.Count > 0;
        }
    }
}
