using Application;
using Application.Flow;
using Game.Workshop;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// T06: Composition root for the BaseHub scene.
    /// Receives the app context from AppCompositionRoot, configures local
    /// state to WorkshopRoaming after the title overlay is dismissed, and
    /// connects the workshop, bench trigger, and deployment terminal.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class HubSceneRoot : MonoBehaviour
    {
        [Header("Local Scene References")]
        [SerializeField] private GameStateController _gameStateController;
        [SerializeField] private GameCompositionRoot _gameCompositionRoot;
        [SerializeField] private WorkshopRuntimeController _workshopRuntimeController;
        [SerializeField] private DeploymentTerminal _deploymentTerminal;

        [Header("Entry")]
        [Tooltip("If true, show the title menu overlay on entry. Otherwise go directly to roaming.")]
        [SerializeField] private bool _showTitleOnEntry = false;

        private AppCompositionRoot _app;
        public bool IsInitialized { get; private set; }

        private IEnumerator Start()
        {
            _app = AppCompositionRoot.Instance;
            string savedEquippedId = _app?.PlayerSession?.EquippedWeaponId;
            bool returningFromRun = _app?.FlowCoordinator?.LastFinalizedResult != null;

            // WeaponLoadout initializes its starting slot in Start.
            yield return null;

            if (_gameStateController == null)
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);

            if (_gameCompositionRoot == null)
                _gameCompositionRoot = SceneComponents.Find<GameCompositionRoot>(gameObject.scene);

            if (_workshopRuntimeController == null)
                _workshopRuntimeController = SceneComponents.Find<WorkshopRuntimeController>(gameObject.scene);

            WeaponLoadout loadout = _gameCompositionRoot != null && _gameCompositionRoot.Player != null
                ? _gameCompositionRoot.Player.GetComponent<WeaponLoadout>()
                : SceneComponents.Find<WeaponLoadout>(gameObject.scene);

            if (_app != null && loadout != null)
            {
                if (!string.IsNullOrEmpty(savedEquippedId))
                {
                    for (int i = 0; i < loadout.Weapons.Count; i++)
                    {
                        if (loadout.Weapons[i] != null && loadout.Weapons[i].WeaponId == savedEquippedId)
                        {
                            loadout.EquipSlot(i);
                            break;
                        }
                    }
                }

                _app.PlayerSession?.SetLoadout(
                    loadout.Weapons.Where(gun => gun != null).Select(gun => gun.WeaponId),
                    loadout.ActiveGun != null ? loadout.ActiveGun.WeaponId : null);
            }

            if (_workshopRuntimeController != null)
                _workshopRuntimeController.Initialize(_gameStateController, loadout, _app?.BuildStore);

            _workshopRuntimeController?.RetryPendingCommittedSaves();

            // Wire deployment terminal to flow coordinator
            if (_deploymentTerminal != null && _app != null)
                _deploymentTerminal.Initialize(_app.FlowCoordinator, _app.PlayerSession);

            // If returning to hub from a run, skip title and go straight to roaming
            if (_app != null)
            {
                if (!_showTitleOnEntry || returningFromRun)
                    EnterRoaming();
                // else: GameStateManager starts in Menu — title overlay shows automatically
            }
            IsInitialized = true;
        }

        /// <summary>Called by MainMenuUI when Play / Enter is pressed.</summary>
        public void EnterRoaming()
        {
            if (_gameStateController != null)
                _gameStateController.EnterWorkshopRoaming();
        }
    }
}
