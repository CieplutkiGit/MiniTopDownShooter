using System;
using System.Collections.Generic;
using Application;
using Application.Flow;
using Game.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Lobby
{
    /// <summary>
    /// Clean, mobile-first Lobby UI controller for BaseHub.
    /// Provides:
    /// - Top bar: Player profile (Commander), Level, High Score, Credits, Settings button.
    /// - Center card: Current equipped weapon display and quick switch tabs.
    /// - Bottom action bar: Prominent "DEPLOY" mission launch button, "WEAPONS" workshop button.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _playerLevelText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private TMP_Text _creditsText;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private SettingsUI _settingsUI;

        [Header("Center Weapon Badge")]
        [SerializeField] private TMP_Text _equippedWeaponNameText;
        [SerializeField] private RectTransform _weaponQuickSwitchContainer;

        [Header("Bottom Action Bar")]
        [SerializeField] private Button _deployButton;
        [SerializeField] private TMP_Text _deployButtonTitle;
        [SerializeField] private TMP_Text _deployMissionSubtext;
        [SerializeField] private Button _workshopButton;

        [Header("Deployment Integration")]
        [SerializeField] private DeploymentTerminal _deploymentTerminal;

        private AppCompositionRoot _app;
        private GameStateController _gameState;
        private WeaponLoadout _loadout;

        public void Initialize(AppCompositionRoot app, GameStateController gameState, WeaponLoadout loadout, DeploymentTerminal terminal = null)
        {
            _app = app;
            _gameState = gameState;
            _loadout = loadout;
            if (terminal != null) _deploymentTerminal = terminal;

            RefreshAll();
        }

        private void Awake()
        {
            if (_settingsButton != null) _settingsButton.onClick.AddListener(HandleSettingsClicked);
            if (_deployButton != null) _deployButton.onClick.AddListener(HandleDeployClicked);
            if (_workshopButton != null) _workshopButton.onClick.AddListener(HandleWorkshopClicked);

            if (_settingsUI == null)
                _settingsUI = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);

            if (_deploymentTerminal == null)
                _deploymentTerminal = FindFirstObjectByType<DeploymentTerminal>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            if (_app == null) _app = AppCompositionRoot.Instance;
            if (_loadout == null) _loadout = FindFirstObjectByType<WeaponLoadout>();
            if (_gameState == null) _gameState = FindFirstObjectByType<GameStateController>();

            RefreshAll();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshProfileHeader();
            RefreshWeaponDisplay();
            RefreshQuickSwitchButtons();
        }

        private void RefreshProfileHeader()
        {
            UserProfileData profile = SaveManager.LoadProfile();

            if (_playerNameText != null)
                _playerNameText.text = "COMMANDER";

            if (_playerLevelText != null)
            {
                int level = Mathf.Max(1, 1 + profile.TotalRuns / 3);
                _playerLevelText.text = $"LV. {level} VETERAN";
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = $"BEST: {profile.HighScore:N0}";
            }

            if (_creditsText != null)
            {
                int credits = 1000 + profile.TotalRuns * 250;
                _creditsText.text = $"SCRAP: {credits:N0}";
            }

            if (_deployMissionSubtext != null)
            {
                _deployMissionSubtext.text = "ARENA SWEEP";
            }
        }

        private void RefreshWeaponDisplay()
        {
            string weaponId = _loadout != null && _loadout.ActiveGun != null
                ? _loadout.ActiveGun.WeaponId
                : (_app?.PlayerSession?.EquippedWeaponId ?? "Rifle");

            if (_equippedWeaponNameText != null)
            {
                string displayName = weaponId.ToUpperInvariant();
                _equippedWeaponNameText.text = $"EQUIPPED: {displayName}";
            }
        }

        private void RefreshQuickSwitchButtons()
        {
            if (_weaponQuickSwitchContainer == null || _loadout == null) return;

            ClearChildren(_weaponQuickSwitchContainer);

            for (int i = 0; i < _loadout.Weapons.Count; i++)
            {
                Gun gun = _loadout.Weapons[i];
                if (gun == null) continue;

                int slotIndex = i;
                string gunId = !string.IsNullOrEmpty(gun.WeaponId) ? gun.WeaponId : gun.name;
                bool isEquipped = _loadout.ActiveIndex == slotIndex;

                CreateWeaponSwitchButton(_weaponQuickSwitchContainer, gunId, isEquipped, () =>
                {
                    _loadout.EquipSlot(slotIndex);
                    _app?.PlayerSession?.SetEquippedWeapon(gunId);
                    RefreshWeaponDisplay();
                    RefreshQuickSwitchButtons();
                });
            }
        }

        private void HandleSettingsClicked()
        {
            if (_settingsUI != null)
            {
                _settingsUI.Open();
            }
        }

        private void HandleDeployClicked()
        {
            if (_app == null) _app = AppCompositionRoot.Instance;

            if (_deploymentTerminal != null && _app != null && _app.FlowCoordinator != null && _app.SceneFlow != null)
            {
                _deploymentTerminal.HandleDeploy();
                return;
            }

            // Direct fallback
            if (_app != null && _app.FlowCoordinator != null && _app.SceneFlow != null)
            {
                DeploymentLoadoutSnapshot snapshot = _app.PlayerSession?.CreateDeploymentSnapshot() ?? DeploymentLoadoutSnapshot.Empty;
                if (snapshot.OrderedWeaponIds.Count > 0 && _app.FlowCoordinator.TryDeploy("Mission_ArenaSweep", snapshot))
                {
                    _app.SceneFlow.GoToArena();
                    return;
                }
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("ArenaShowcase");
        }

        private void HandleWorkshopClicked()
        {
            if (_app == null) _app = AppCompositionRoot.Instance;
            if (_app != null && _app.SceneFlow != null)
            {
                _app.SceneFlow.GoToWeaponEdit();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("WeaponEdit");
            }
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject go = root.GetChild(i).gameObject;
                if (UnityEngine.Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        private static GameObject CreateWeaponSwitchButton(RectTransform parent, string label, bool isEquipped, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Weapon_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 40f);

            var img = btnObj.GetComponent<Image>();
            img.color = isEquipped ? new Color(0.0f, 0.76f, 0.74f, 1f) : new Color(0.12f, 0.15f, 0.19f, 0.85f);

            var btn = btnObj.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            var layout = btnObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 110f;
            layout.preferredHeight = 40f;
            layout.minHeight = 40f;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = label.ToUpperInvariant();
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isEquipped ? Color.black : Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }
    }
}
