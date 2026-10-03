using System;
using System.Collections.Generic;
using Application;
using Application.Flow;
using Application.Weapons;
using Game.Flow;
using Game.Economy;
using Game.UI;
using Game.Workshop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Lobby
{
    /// <summary>
    /// Clean, mobile-first Lobby UI controller for BaseHub.
    /// Provides:
    /// - Top bar: Player profile (Commander), live economy level/XP, High Score, Credits, Settings button.
    /// - Center card: Current equipped weapon display and quick switch tabs.
    /// - Bottom action bar: Prominent "DEPLOY" mission launch button, "WEAPONS" workshop button.
    /// - Fully responsive non-overlapping layout across 1920x1080, 1280x720, and narrow aspects.
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
        [SerializeField] private ScrollRect _quickSwitchScrollRect;

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
        private UnityEconomyService _economy;
        private EconomyMarketPopup _marketPopup;

        public void Initialize(AppCompositionRoot app, GameStateController gameState, WeaponLoadout loadout, DeploymentTerminal terminal = null)
        {
            _app = app;
            _gameState = gameState;
            _loadout = loadout;
            BindEconomy();
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

            EnsureResponsiveLayout();
            EnsureMarketAccess();
        }

        private void EnsureResponsiveLayout()
        {
            var profileObj = transform.Find("Header/PlayerProfile") ?? transform.Find("TopBar/ProfileBox");
            var statsObj = transform.Find("Header/StatsAndSettings") ?? transform.Find("TopBar/StatsBox");
            if (profileObj != null && statsObj != null)
            {
                var profRt = profileObj.GetComponent<RectTransform>();
                var statsRt = statsObj.GetComponent<RectTransform>();
                profRt.anchorMin = new Vector2(0f, 0.5f);
                profRt.anchorMax = new Vector2(0f, 0.5f);
                profRt.pivot = new Vector2(0f, 0.5f);
                statsRt.anchorMin = new Vector2(1f, 0.5f);
                statsRt.anchorMax = new Vector2(1f, 0.5f);
                statsRt.pivot = new Vector2(1f, 0.5f);
            }
        }

        private void Start()
        {
            if (_app == null) _app = AppCompositionRoot.Instance;
            if (_loadout == null) _loadout = FindFirstObjectByType<WeaponLoadout>();
            if (_gameState == null) _gameState = FindFirstObjectByType<GameStateController>();

            EnsureMarketAccess();
            RefreshAll();
        }

        private void OnEnable()
        {
            BindEconomy();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_economy != null)
            {
                _economy.OnEconomyStateChanged -= HandleEconomyChanged;
                _economy.OnWeaponUnlocked -= HandleWeaponUnlocked;
            }
        }

        private void BindEconomy()
        {
            UnityEconomyService service = UnityEconomyService.Instance;
            if (_economy == service) return;
            if (_economy != null)
            {
                _economy.OnEconomyStateChanged -= HandleEconomyChanged;
                _economy.OnWeaponUnlocked -= HandleWeaponUnlocked;
            }
            _economy = service;
            if (_economy != null)
            {
                _economy.OnEconomyStateChanged += HandleEconomyChanged;
                _economy.OnWeaponUnlocked += HandleWeaponUnlocked;
            }
        }

        private void HandleEconomyChanged()
        {
            RefreshAll();
        }

        private void HandleWeaponUnlocked(string weaponId)
        {
            RefreshAll();
        }

        private void EnsureMarketAccess()
        {
            if (_marketPopup != null || _creditsText == null) return;

            RectTransform labelRect = _creditsText.rectTransform;
            labelRect.sizeDelta = new Vector2(labelRect.sizeDelta.x, Mathf.Max(34f, labelRect.sizeDelta.y));
            labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, -20f);
            _creditsText.alignment = TextAlignmentOptions.Right;
            _creditsText.raycastTarget = true;

            Button marketButton = _creditsText.GetComponent<Button>();
            if (marketButton == null) marketButton = _creditsText.gameObject.AddComponent<Button>();
            marketButton.targetGraphic = _creditsText;
            marketButton.transition = Selectable.Transition.None;
            _marketPopup = EconomyMarketPopup.Attach(transform, marketButton, _creditsText, _economy, WeaponBuildApplier.DefaultCatalog);
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
                int level = _economy != null ? _economy.Level : 1;
                _playerLevelText.text = $"LV. {level}  •  {_economy?.Xp ?? 0} XP";
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = $"BEST: {profile.HighScore:N0}";
            }

            if (_creditsText != null)
            {
                _creditsText.text = _economy != null ? $"MARKET  ·  {_economy.Coins:N0} CR" : "MARKET";
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
                : (_app?.PlayerSession?.EquippedWeaponId ?? WeaponWorkshopIds.Pistol);

            if (_equippedWeaponNameText != null)
            {
                string displayName = weaponId.Replace("weapon.", "").ToUpperInvariant();
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
                bool isUnlocked = _economy == null || _economy.IsWeaponUnlocked(gunId);
                bool isEquipped = _loadout.ActiveIndex == slotIndex;
                string label = gunId.Replace("weapon.", "").ToUpperInvariant();

                CreateWeaponSwitchButton(_weaponQuickSwitchContainer, label, isEquipped, isUnlocked, () =>
                {
                    if (!_loadout.EquipSlot(slotIndex)) return;
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

        private static GameObject CreateWeaponSwitchButton(RectTransform parent, string label, bool isEquipped, bool isUnlocked, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Weapon_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(105f, 38f);

            var img = btnObj.GetComponent<Image>();
            Color baseColor = isEquipped
                ? UITheme.ColorAccentCyan
                : (isUnlocked ? UITheme.ColorButtonNormal : new Color(0.12f, 0.14f, 0.18f, 0.6f));
            img.color = baseColor;

            var btn = btnObj.GetComponent<Button>();
            UITheme.ApplyButtonColors(btn, baseColor);
            btn.interactable = isUnlocked;
            if (onClick != null && isUnlocked) btn.onClick.AddListener(onClick);

            var layout = btnObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 105f;
            layout.preferredHeight = 38f;
            layout.minHeight = 36f;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            Color textColor = isEquipped
                ? UITheme.ColorTextDark
                : (isUnlocked ? UITheme.ColorTextPrimary : UITheme.ColorTextMuted);
            UITheme.ApplyTextStyle(tmp, 11f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            tmp.text = isUnlocked ? label : $"{label} [LOCKED]";

            return btnObj;
        }
    }
}
