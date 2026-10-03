using System;
using System.Collections.Generic;
using Application.Economy;
using Application.Weapons;
using Game.Workshop;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Economy
{
    /// <summary>
    /// Compact Economy View & Controller for Hub/Workshop/Lobby integration.
    /// Exposes explicit public binding API for the UI / final integration worker.
    /// Uses standard uGUI and TextMeshPro components.
    /// </summary>
    public class EconomyPanel : MonoBehaviour
    {
        [Header("Wallet & Progression UI")]
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private Image _xpFillImage;

        [Header("Salvage Inventory UI")]
        [SerializeField] private TMP_Text _scrapText;
        [SerializeField] private TMP_Text _alloyText;
        [SerializeField] private TMP_Text _coreText;
        [SerializeField] private Button _sellScrapButton;
        [SerializeField] private Button _sellAlloyButton;
        [SerializeField] private Button _sellCoreButton;

        [Header("Weapon Platform Unlocks UI")]
        [SerializeField] private TMP_Text _selectedWeaponText;
        [SerializeField] private TMP_Text _weaponCostText;
        [SerializeField] private Button _unlockWeaponButton;

        [Header("Weapon Part Crafting UI")]
        [SerializeField] private TMP_Text _selectedPartText;
        [SerializeField] private TMP_Text _craftingRecipeText;
        [SerializeField] private Button _craftPartButton;

        [Header("Status & Feedback")]
        [SerializeField] private TMP_Text _feedbackText;

        // Runtime dependencies
        private IEconomyService _economyService;
        private IWeaponCatalog _catalog;

        private string _selectedWeaponId = WeaponWorkshopIds.Pistol;
        private string _selectedSlotId = "pistol.slot.barrel";
        private string _selectedPartId;
        private bool _buttonsHooked;
        private readonly Dictionary<string, Button> _platformButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TMP_Text> _platformButtonLabels = new Dictionary<string, TMP_Text>(StringComparer.OrdinalIgnoreCase);

        public IEconomyService EconomyService => _economyService;
        public string SelectedWeaponId => _selectedWeaponId;
        public string SelectedPartId => _selectedPartId;
        public WeaponPlatformCost SelectedWeaponCost => _economyService != null
            ? _economyService.GetWeaponCost(_selectedWeaponId)
            : EconomyPricingPolicy.GetPlatformCost(_selectedWeaponId);
        public bool SelectedWeaponIsUnlocked => _economyService != null && _economyService.IsWeaponUnlocked(_selectedWeaponId);

        public bool CanUnlockSelectedWeapon(out string reason)
        {
            if (_economyService == null)
            {
                reason = "Economy service is unavailable.";
                return false;
            }
            return _economyService.CanUnlockWeapon(_selectedWeaponId, out reason);
        }

        public event Action<string> OnFeedbackMessage;

        /// <summary>Creates a compact, self-contained economy panel under a runtime UI root.</summary>
        public static EconomyPanel Create(Transform parent, IEconomyService service, IWeaponCatalog catalog = null)
        {
            var root = new GameObject("EconomyPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var panel = root.AddComponent<EconomyPanel>();
            panel._coinsText = CreateText(root.transform, "Wallet", UITheme.FontSizeHeaderMedium, UITheme.ColorAccentAmber, FontStyles.Bold);
            panel._levelText = CreateText(root.transform, "Level", UITheme.FontSizeBody, UITheme.ColorAccentCyan, FontStyles.Bold);
            panel._xpText = CreateText(root.transform, "XP", UITheme.FontSizeCaption, UITheme.ColorTextSecondary);
            panel._xpSlider = CreateSlider(root.transform);
            panel._scrapText = CreateText(root.transform, "Scrap", UITheme.FontSizeBody, UITheme.ColorTextPrimary);
            panel._sellScrapButton = CreateButton(root.transform, "Sell 1 Scrap");
            panel._alloyText = CreateText(root.transform, "Alloy", UITheme.FontSizeBody, UITheme.ColorTextPrimary);
            panel._sellAlloyButton = CreateButton(root.transform, "Sell 1 Alloy");
            panel._coreText = CreateText(root.transform, "Cores", UITheme.FontSizeBody, UITheme.ColorTextPrimary);
            panel._sellCoreButton = CreateButton(root.transform, "Sell 1 Core");
            panel._selectedWeaponText = CreateText(root.transform, "Weapon", UITheme.FontSizeHeaderMedium, UITheme.ColorTextPrimary, FontStyles.Bold);
            panel._weaponCostText = CreateText(root.transform, "Weapon status", UITheme.FontSizeCaption, UITheme.ColorTextSecondary);
            panel._catalog = catalog ?? WeaponBuildApplier.DefaultCatalog;
            panel.CreatePlatformSelector(root.transform);
            panel._unlockWeaponButton = CreateButton(root.transform, "Unlock weapon");
            panel._selectedPartText = CreateText(root.transform, "Upgrade", UITheme.FontSizeBody, UITheme.ColorTextPrimary);
            panel._craftingRecipeText = CreateText(root.transform, "Recipe", UITheme.FontSizeCaption, UITheme.ColorTextSecondary);
            panel._craftPartButton = CreateButton(root.transform, "Craft upgrade");
            panel._feedbackText = CreateText(root.transform, "", UITheme.FontSizeCaption, UITheme.ColorAccentAmber);
            panel.Bind(service, catalog);
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string initialText, float size, Color color, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = initialText;
            UITheme.ApplyTextStyle(text, size, style, TextAlignmentOptions.Left, color);
            text.enableWordWrapping = true;
            return text;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UITheme.ColorButtonNormal;
            var button = go.GetComponent<Button>();
            UITheme.ApplyButtonColors(button, UITheme.ColorButtonNormal);
            go.GetComponent<LayoutElement>().preferredHeight = 30f;
            var text = CreateText(go.transform, label, UITheme.FontSizeCaption, UITheme.ColorTextPrimary, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static Slider CreateSlider(Transform parent)
        {
            var go = new GameObject("XP Progress", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 12f;
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
            var background = go.AddComponent<Image>();
            background.color = UITheme.ColorPanelSurface;
            slider.targetGraphic = background;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(go.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = UITheme.ColorAccentGreen;
            slider.fillRect = fillRect;
            return slider;
        }

        private void CreatePlatformSelector(Transform parent)
        {
            if (_catalog == null) return;
            var weaponIds = _catalog.GetAllWeaponIds();
            if (weaponIds == null || weaponIds.Count == 0) return;

            var selector = new GameObject("Weapon Platforms", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            selector.transform.SetParent(parent, false);
            var rows = selector.GetComponent<VerticalLayoutGroup>();
            rows.spacing = 4f;
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            selector.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < weaponIds.Count; i += 2)
            {
                var row = new GameObject("Platform Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(selector.transform, false);
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 4f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;
                row.GetComponent<LayoutElement>().preferredHeight = 32f;

                AddPlatformButton(row.transform, weaponIds[i]);
                if (i + 1 < weaponIds.Count) AddPlatformButton(row.transform, weaponIds[i + 1]);
            }
        }

        private void AddPlatformButton(Transform parent, string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return;
            var cost = EconomyPricingPolicy.GetPlatformCost(weaponId);
            var button = CreateButton(parent, cost.DisplayName);
            var buttonText = button.GetComponentInChildren<TMP_Text>();
            _platformButtons[weaponId] = button;
            _platformButtonLabels[weaponId] = buttonText;
            button.onClick.AddListener(() => SelectWeapon(weaponId));
        }

        public void Bind(IEconomyService service, IWeaponCatalog catalog = null)
        {
            Unbind();

            _economyService = service ?? UnityEconomyService.Instance;
            _catalog = catalog ?? WeaponBuildApplier.DefaultCatalog;

            HookButtons();

            if (_economyService != null)
            {
                _economyService.OnEconomyStateChanged += Refresh;
            }

            Refresh();
        }

        public void Unbind()
        {
            if (_economyService != null)
            {
                _economyService.OnEconomyStateChanged -= Refresh;
                _economyService = null;
            }
            _catalog = null;
        }

        public void SelectWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return;
            if (!string.Equals(_selectedWeaponId, weaponId, StringComparison.OrdinalIgnoreCase))
            {
                _selectedSlotId = null;
                _selectedPartId = null;
            }
            _selectedWeaponId = weaponId;
            Refresh();
        }

        public void SelectPart(string weaponId, string slotId, string partId)
        {
            _selectedWeaponId = weaponId ?? _selectedWeaponId;
            _selectedSlotId = slotId;
            _selectedPartId = partId;
            Refresh();
        }

        public void Refresh()
        {
            if (_economyService == null) return;

            RefreshWalletAndLevel();
            RefreshSalvageInventory();
            RefreshWeaponSection();
            RefreshCraftingSection();
        }

        private void RefreshWalletAndLevel()
        {
            if (_coinsText != null)
            {
                _coinsText.text = $"{_economyService.Coins} Coins";
            }

            if (_levelText != null)
            {
                _levelText.text = $"Level {_economyService.Level}";
            }

            if (_xpText != null)
            {
                _xpText.text = $"XP: {_economyService.XpInCurrentLevel} / {_economyService.XpForNextLevel}";
            }

            float progress = _economyService.LevelProgressNormalized;
            if (_xpSlider != null)
            {
                _xpSlider.value = progress;
            }
            if (_xpFillImage != null)
            {
                _xpFillImage.fillAmount = progress;
            }
        }

        private void RefreshSalvageInventory()
        {
            int scrap = _economyService.GetComponentCount(EconomyComponentExtensions.ScrapId);
            int alloy = _economyService.GetComponentCount(EconomyComponentExtensions.AlloyId);
            int core = _economyService.GetComponentCount(EconomyComponentExtensions.CoreId);

            if (_scrapText != null) _scrapText.text = $"Scrap: {scrap} (+{EconomyPricingPolicy.ScrapSellPrice}c)";
            if (_alloyText != null) _alloyText.text = $"Alloy: {alloy} (+{EconomyPricingPolicy.AlloySellPrice}c)";
            if (_coreText != null) _coreText.text = $"Cores: {core} (+{EconomyPricingPolicy.CoreSellPrice}c)";

            if (_sellScrapButton != null) _sellScrapButton.interactable = scrap > 0;
            if (_sellAlloyButton != null) _sellAlloyButton.interactable = alloy > 0;
            if (_sellCoreButton != null) _sellCoreButton.interactable = core > 0;
        }

        private void RefreshWeaponSection()
        {
            if (string.IsNullOrEmpty(_selectedWeaponId)) return;

            bool isUnlocked = _economyService.IsWeaponUnlocked(_selectedWeaponId);
            var cost = _economyService.GetWeaponCost(_selectedWeaponId);

            if (_selectedWeaponText != null)
            {
                _selectedWeaponText.text = cost.DisplayName;
            }

            if (_weaponCostText != null)
            {
                if (isUnlocked)
                {
                    _weaponCostText.text = "Status: UNLOCKED";
                }
                else
                {
                    _weaponCostText.text = $"Unlock Cost: {cost.CoinCost} Coins (Requires Level {cost.RequiredLevel})";
                }
            }

            if (_unlockWeaponButton != null)
            {
                bool canUnlock = _economyService.CanUnlockWeapon(_selectedWeaponId, out _);
                _unlockWeaponButton.gameObject.SetActive(!isUnlocked);
                _unlockWeaponButton.interactable = canUnlock;
            }

            foreach (var entry in _platformButtons)
            {
                bool selected = string.Equals(entry.Key, _selectedWeaponId, StringComparison.OrdinalIgnoreCase);
                bool unlocked = _economyService.IsWeaponUnlocked(entry.Key);
                WeaponPlatformCost platformCost = _economyService.GetWeaponCost(entry.Key);
                if (_platformButtonLabels.TryGetValue(entry.Key, out var label) && label != null)
                {
                    label.text = unlocked
                        ? $"{platformCost.DisplayName} · OWNED"
                        : $"{platformCost.DisplayName} · {platformCost.CoinCost} CR · LV {platformCost.RequiredLevel}";
                    label.color = selected ? UITheme.ColorTextDark : UITheme.ColorTextPrimary;
                }
                var image = entry.Value != null ? entry.Value.GetComponent<Image>() : null;
                if (image != null) image.color = selected ? UITheme.ColorAccentCyan : UITheme.ColorButtonNormal;
                UITheme.ApplyButtonColors(entry.Value, selected ? UITheme.ColorAccentCyan : UITheme.ColorButtonNormal);
            }
        }

        private void RefreshCraftingSection()
        {
            if (string.IsNullOrEmpty(_selectedPartId))
            {
                if (_selectedPartText != null) _selectedPartText.text = "No Part Selected";
                if (_craftingRecipeText != null) _craftingRecipeText.text = string.Empty;
                if (_craftPartButton != null) _craftPartButton.interactable = false;
                return;
            }

            bool isOwned = _economyService.IsPartUnlocked(_selectedWeaponId, _selectedSlotId, _selectedPartId);
            var recipe = _economyService.GetPartRecipe(_selectedPartId);

            if (_selectedPartText != null)
            {
                _selectedPartText.text = _catalog != null && _catalog.TryGetPart(_selectedPartId, out var part) && part != null
                    ? part.DisplayName
                    : _selectedPartId;
            }

            if (_craftingRecipeText != null)
            {
                if (isOwned)
                {
                    _craftingRecipeText.text = "Status: OWNED / READY";
                }
                else
                {
                    var reqs = new List<string>();
                    if (recipe.ScrapCost > 0) reqs.Add($"{recipe.ScrapCost} Scrap");
                    if (recipe.AlloyCost > 0) reqs.Add($"{recipe.AlloyCost} Alloy");
                    if (recipe.CoreCost > 0) reqs.Add($"{recipe.CoreCost} Core");
                    _craftingRecipeText.text = $"Cost: {string.Join(", ", reqs)}";
                }
            }

            if (_craftPartButton != null)
            {
                bool canCraft = _economyService.CanCraftPart(_selectedWeaponId, _selectedSlotId, _selectedPartId, out _);
                _craftPartButton.gameObject.SetActive(!isOwned);
                _craftPartButton.interactable = canCraft;
            }
        }

        // ── User Action Handlers ───────────────────────────────────────────

        public void RequestUnlockSelectedWeapon()
        {
            if (_economyService == null || string.IsNullOrEmpty(_selectedWeaponId)) return;

            var result = _economyService.TryUnlockWeapon(_selectedWeaponId);
            if (result.IsSuccess)
            {
                ShowFeedback($"Unlocked {_selectedWeaponId} successfully!");
            }
            else
            {
                ShowFeedback($"Cannot unlock: {result.ErrorMessage}");
            }
        }

        public void RequestCraftSelectedPart()
        {
            if (_economyService == null || string.IsNullOrEmpty(_selectedPartId)) return;

            var result = _economyService.TryCraftPart(_selectedWeaponId, _selectedSlotId, _selectedPartId);
            if (result.IsSuccess)
            {
                ShowFeedback($"Crafted upgrade {_selectedPartId}!");
            }
            else
            {
                ShowFeedback($"Cannot craft: {result.ErrorMessage}");
            }
        }

        public void RequestSellComponent(string componentId, int count = 1)
        {
            if (_economyService == null || string.IsNullOrEmpty(componentId)) return;

            var result = _economyService.TrySellComponents(componentId, count);
            if (result.IsSuccess)
            {
                int earned = count * _economyService.GetComponentSellPrice(componentId);
                ShowFeedback($"Sold {count}x {componentId} for {earned} coins.");
            }
            else
            {
                ShowFeedback($"Sale failed: {result.ErrorMessage}");
            }
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
            }
            OnFeedbackMessage?.Invoke(message);
        }

        private void HookButtons()
        {
            if (_buttonsHooked) return;
            _buttonsHooked = true;
            if (_sellScrapButton != null)
                _sellScrapButton.onClick.AddListener(() => RequestSellComponent(EconomyComponentExtensions.ScrapId, 1));
            if (_sellAlloyButton != null)
                _sellAlloyButton.onClick.AddListener(() => RequestSellComponent(EconomyComponentExtensions.AlloyId, 1));
            if (_sellCoreButton != null)
                _sellCoreButton.onClick.AddListener(() => RequestSellComponent(EconomyComponentExtensions.CoreId, 1));

            if (_unlockWeaponButton != null)
                _unlockWeaponButton.onClick.AddListener(RequestUnlockSelectedWeapon);

            if (_craftPartButton != null)
                _craftPartButton.onClick.AddListener(RequestCraftSelectedPart);
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
