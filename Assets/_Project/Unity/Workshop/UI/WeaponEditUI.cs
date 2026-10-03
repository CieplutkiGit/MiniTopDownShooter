using System;
using System.Collections.Generic;
using Application.Economy;
using Application.Weapons;
using Application.Workshop;
using Game.Economy;
using Game.UI;
using Game.Workshop.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Workshop.UI
{
    /// <summary>
    /// Clean, mobile-first UI controller for the dedicated WeaponEdit scene.
    /// Features:
    /// - Header with Back to Lobby, Weapon Title, Explode/Inspect toggles, and Reset view
    /// - Horizontally scrollable slot selector tabs (Receiver, Barrel, Magazine, Stock, etc.)
    /// - Horizontally scrollable part cards with active selection badges
    /// - Live stat diff indicators (Damage, Fire Rate, Reload, Range, Accuracy)
    /// - Apply & Discard action buttons with validation feedback
    /// - Dedicated preview viewport with deterministic pointer routing
    /// - Reserved space for compact wallet and locked-weapon purchase controls
    /// </summary>
    public class WeaponEditUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _weaponTitleText;
        [SerializeField] private Button _explodeButton;
        [SerializeField] private TMP_Text _explodeButtonText;
        [SerializeField] private Button _resetViewButton;

        [Header("Slot Selector")]
        [SerializeField] private RectTransform _slotsContainer;
        [SerializeField] private ScrollRect _slotsScrollRect;

        [Header("Part Selector")]
        [SerializeField] private RectTransform _partsContainer;
        [SerializeField] private ScrollRect _partsScrollRect;
        [SerializeField] private TMP_Text _currentSlotLabel;

        [Header("Stats Panel")]
        [SerializeField] private TMP_Text _statsSummaryText;

        [Header("Action Buttons")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _discardButton;

        [Header("Preview Viewport")]
        [SerializeField] private RectTransform _previewViewport;

        [Header("Error Display")]
        [SerializeField] private GameObject _errorRoot;
        [SerializeField] private TMP_Text _errorText;

        [Header("Economy Placeholders (For Independent Worker)")]
        [SerializeField] private GameObject _walletPanel;
        [SerializeField] private TMP_Text _walletText;
        [SerializeField] private GameObject _lockedPurchasePrompt;

        [Header("Pinch Rotate Controller Hook")]
        [SerializeField] private WeaponPinchRotateController _rotateController;

        private IWeaponWorkshopSession _session;
        private IWeaponPreviewView _previewView;
        private IWeaponCatalog _catalog;
        private EconomyPanel _economyPanel;
        private GameObject _economyOverlay;
        private Button _economyToggle;
        private UnityEconomyService _economyService;

        private string _selectedSlot;
        private bool _isExploded;
        private ResolvedWeaponStats _baselineStats;
        private readonly List<string> _availableSlots = new List<string>();
        private readonly List<WeaponPartSpec> _availableParts = new List<WeaponPartSpec>();

        public event Action OnBackClicked;

        public RectTransform PreviewViewport
        {
            get => _previewViewport;
            set => _previewViewport = value;
        }

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(HandleBack);
            if (_explodeButton != null) _explodeButton.onClick.AddListener(ToggleExploded);
            if (_resetViewButton != null) _resetViewButton.onClick.AddListener(ResetView);
            if (_applyButton != null) _applyButton.onClick.AddListener(ApplyChanges);
            if (_discardButton != null) _discardButton.onClick.AddListener(DiscardChanges);

            if (_rotateController == null)
                _rotateController = FindFirstObjectByType<WeaponPinchRotateController>();

            if (_rotateController != null && _previewViewport != null)
            {
                _rotateController.PreviewViewport = _previewViewport;
            }

            SetupEconomyPanel();

            EnsureResponsiveLayout();
        }

        private void SetupEconomyPanel()
        {
            if (_walletPanel == null || _walletText == null) return;

            var walletImage = _walletPanel.GetComponent<Image>();
            if (walletImage == null) walletImage = _walletPanel.AddComponent<Image>();
            walletImage.color = UITheme.ColorButtonNormal;
            walletImage.raycastTarget = true;

            _economyToggle = _walletPanel.GetComponent<Button>();
            if (_economyToggle == null) _economyToggle = _walletPanel.AddComponent<Button>();
            UITheme.ApplyButtonColors(_economyToggle, UITheme.ColorButtonNormal);
            _economyToggle.onClick.AddListener(ToggleEconomyPanel);

            var labelRect = _walletText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 1f);
            labelRect.offsetMax = new Vector2(-4f, -1f);
            _walletText.alignment = TextAlignmentOptions.Center;
            _walletText.color = UITheme.ColorTextPrimary;
            _walletText.fontSize = 12f;

            CreateEconomyOverlay();
            _economyService = UnityEconomyService.Instance;
            _economyService.OnEconomyStateChanged += RefreshEconomyWallet;
            RefreshEconomyWallet();
        }

        private void OnDestroy()
        {
            if (_economyService != null)
                _economyService.OnEconomyStateChanged -= RefreshEconomyWallet;
        }

        private void CreateEconomyOverlay()
        {
            if (_economyOverlay != null) return;

            _economyOverlay = new GameObject("EconomyOverlay", typeof(RectTransform), typeof(Image));
            _economyOverlay.transform.SetParent(transform, false);
            var overlayRect = _economyOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = _economyOverlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.50f);
            overlayImage.raycastTarget = true;

            var panel = new GameObject("EconomyPanelFrame", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_economyOverlay.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.offsetMin = new Vector2(-330f, 92f);
            panelRect.offsetMax = new Vector2(-12f, -82f);
            panel.GetComponent<Image>().color = UITheme.ColorPanelSurface;

            var closeGo = new GameObject("CloseMarket", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            closeRect.sizeDelta = new Vector2(70f, 32f);
            closeGo.GetComponent<Image>().color = UITheme.ColorButtonNormal;
            var closeButton = closeGo.GetComponent<Button>();
            UITheme.ApplyButtonColors(closeButton, UITheme.ColorButtonNormal);
            closeButton.onClick.AddListener(ToggleEconomyPanel);
            var closeLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabel.transform.SetParent(closeGo.transform, false);
            var closeLabelRect = closeLabel.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            var closeText = closeLabel.GetComponent<TextMeshProUGUI>();
            closeText.text = "CLOSE";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontSize = 12f;
            closeText.color = UITheme.ColorTextPrimary;

            var scrollGo = new GameObject("EconomyScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(12f, 12f);
            scrollRt.offsetMax = new Vector2(-12f, -54f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("EconomyContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = scrollRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            _economyService = UnityEconomyService.Instance;
            _economyPanel = EconomyPanel.Create(contentGo.transform, _economyService, _catalog);
            _economyOverlay.SetActive(false);
        }

        private void ToggleEconomyPanel()
        {
            if (_economyOverlay == null) return;
            bool opening = !_economyOverlay.activeSelf;
            _economyOverlay.SetActive(opening);
            if (opening && _session != null)
            {
                _economyPanel?.SelectWeapon(_session.WeaponId);
                RefreshEconomyPartContext();
            }
        }

        private void RefreshEconomyWallet()
        {
            if (_walletText != null)
                _walletText.text = $"MARKET  ·  {_economyService?.Coins ?? 0:N0} CR";
        }

        private void RefreshEconomyPartContext()
        {
            if (_economyPanel == null || _session == null || string.IsNullOrEmpty(_selectedSlot)) return;
            string partId = null;
            _session.DraftBuild?.Selections?.TryGetValue(_selectedSlot, out partId);
            if (string.IsNullOrEmpty(partId) && _availableParts.Count > 0)
                partId = _availableParts[0]?.PartId;
            if (!string.IsNullOrEmpty(partId))
                _economyPanel.SelectPart(_session.WeaponId, _selectedSlot, partId);
        }

        private void EnsureResponsiveLayout()
        {
            if (_slotsScrollRect != null)
            {
                var rt = _slotsScrollRect.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = new Vector2(10f, -116f);
                    rt.offsetMax = new Vector2(-10f, -72f);
                }
            }

            if (_previewViewport != null)
            {
                _previewViewport.anchorMin = new Vector2(0f, 0f);
                _previewViewport.anchorMax = new Vector2(1f, 1f);
                _previewViewport.offsetMin = new Vector2(0f, 215f);
                _previewViewport.offsetMax = new Vector2(0f, -125f);
            }

            // Ensure non-overlapping layout for bottom card rows (PartsScrollView, Action buttons, SlotLabel)
            var bottomPanel = transform.Find("BottomCustomizationPanel");
            if (bottomPanel != null)
            {
                var partsRt = bottomPanel.Find("PartsScrollView") as RectTransform;
                if (partsRt != null)
                {
                    partsRt.anchorMin = new Vector2(0f, 0f);
                    partsRt.anchorMax = new Vector2(0.68f, 0f);
                    partsRt.pivot = new Vector2(0.5f, 0.5f);
                    partsRt.anchoredPosition = new Vector2(20f, 105f);
                    partsRt.sizeDelta = new Vector2(-30f, 72f);
                }

                var discardBtn = bottomPanel.Find("DiscardButton") as RectTransform;
                if (discardBtn != null)
                {
                    discardBtn.anchorMin = new Vector2(0f, 0f);
                    discardBtn.anchorMax = new Vector2(0f, 0f);
                    discardBtn.pivot = new Vector2(0f, 0f);
                    discardBtn.anchoredPosition = new Vector2(20f, 14f);
                    discardBtn.sizeDelta = new Vector2(115f, 42f);
                }

                var applyBtn = bottomPanel.Find("ApplyButton") as RectTransform;
                if (applyBtn != null)
                {
                    applyBtn.anchorMin = new Vector2(0f, 0f);
                    applyBtn.anchorMax = new Vector2(0f, 0f);
                    applyBtn.pivot = new Vector2(0f, 0f);
                    applyBtn.anchoredPosition = new Vector2(145f, 14f);
                    applyBtn.sizeDelta = new Vector2(155f, 42f);
                }

                var slotLabel = bottomPanel.Find("SlotLabel") as RectTransform;
                if (slotLabel != null)
                {
                    slotLabel.anchorMin = new Vector2(0f, 0f);
                    slotLabel.anchorMax = new Vector2(0f, 0f);
                    slotLabel.pivot = new Vector2(0f, 0.5f);
                    slotLabel.anchoredPosition = new Vector2(25f, 185f);
                    slotLabel.sizeDelta = new Vector2(200f, 26f);
                }
            }
        }

        public void Bind(IWeaponWorkshopSession session, IWeaponPreviewView previewView, IWeaponCatalog catalog)
        {
            Unbind();

            _session = session;
            _previewView = previewView;
            _catalog = catalog;
            _economyPanel?.Bind(UnityEconomyService.Instance, catalog);

            if (_rotateController != null)
            {
                if (_previewViewport != null) _rotateController.PreviewViewport = _previewViewport;
                if (previewView is Presentation.WeaponPreviewView pView) _rotateController.PreviewView = pView;
            }

            if (_session != null)
            {
                _session.SessionChanged += HandleSessionChanged;
                _baselineStats = _session.Target?.CurrentStats ?? _session.DraftStats;
            }

            if (_previewView != null)
            {
                _previewView.SlotSelected += HandleSlotSelectedFromPreview;
                if (_session != null)
                {
                    _previewView.ShowBuild(_session.DraftBuild);
                }
            }

            RefreshSlots();
            if (_session != null)
                _economyPanel?.SelectWeapon(_session.WeaponId);
            if (_availableSlots.Count > 0)
            {
                SelectSlot(_availableSlots[0]);
            }

            RefreshUI();
            RefreshEconomyPartContext();
        }

        public void Unbind()
        {
            if (_session != null)
            {
                _session.SessionChanged -= HandleSessionChanged;
                _session = null;
            }

            if (_previewView != null)
            {
                _previewView.SlotSelected -= HandleSlotSelectedFromPreview;
                _previewView = null;
            }

            _catalog = null;
            _selectedSlot = null;
            _baselineStats = null;
        }

        public void SelectSlot(string slotId)
        {
            _selectedSlot = slotId;

            if (_previewView != null && !string.IsNullOrEmpty(slotId))
            {
                _previewView.SelectSlot(slotId);
            }

            RefreshSlotButtons();
            RefreshParts();
            RefreshUI();
            RefreshEconomyPartContext();
        }

        public void SelectPart(string partId)
        {
            if (_session == null || string.IsNullOrEmpty(_selectedSlot))
                return;

            _session.SelectPart(_selectedSlot, partId);
            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshParts();
            RefreshUI();
            RefreshEconomyPartContext();
        }

        public void ToggleExploded()
        {
            _isExploded = !_isExploded;
            _previewView?.SetExploded(_isExploded);
            if (_explodeButtonText != null)
            {
                _explodeButtonText.text = _isExploded ? "ASSEMBLE" : "EXPLODE";
            }
        }

        public void ResetView()
        {
            _rotateController?.ResetView();
        }

        public void ApplyChanges()
        {
            if (_session == null || !_session.HasUnappliedChanges || !_session.IsValid)
                return;

            ApplyResult result = _session.Apply();
            if (result.IsSuccess)
            {
                _baselineStats = _session.DraftStats;
            }

            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshParts();
            RefreshUI();
        }

        public void DiscardChanges()
        {
            if (_session == null || !_session.HasUnappliedChanges)
                return;

            _session.Discard();
            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshParts();
            RefreshUI();
        }

        private void HandleBack()
        {
            OnBackClicked?.Invoke();
        }

        private void HandleSessionChanged(IWeaponWorkshopSession session)
        {
            if (_previewView != null && _session != null)
            {
                _previewView.ShowBuild(_session.DraftBuild);
            }
            RefreshParts();
            RefreshUI();
        }

        private void HandleSlotSelectedFromPreview(string slotId)
        {
            SelectSlot(slotId);
        }

        private void RefreshSlots()
        {
            _availableSlots.Clear();
            if (_session == null) return;

            if (_catalog != null && _catalog.TryGetPlatform(_session.WeaponId, out var platform) && platform.SupportedSlots != null)
            {
                _availableSlots.AddRange(platform.SupportedSlots);
            }
            else if (_session.DraftBuild?.Selections != null)
            {
                _availableSlots.AddRange(_session.DraftBuild.Selections.Keys);
            }

            if (!string.IsNullOrEmpty(_selectedSlot) && !_availableSlots.Contains(_selectedSlot))
            {
                _selectedSlot = _availableSlots.Count > 0 ? _availableSlots[0] : null;
            }

            RefreshSlotButtons();
        }

        private void RefreshSlotButtons()
        {
            if (_slotsContainer == null) return;
            ClearChildren(_slotsContainer);

            for (int i = 0; i < _availableSlots.Count; i++)
            {
                string slotId = _availableSlots[i];
                bool isSelected = string.Equals(slotId, _selectedSlot, StringComparison.OrdinalIgnoreCase);

                CreateSlotButton(_slotsContainer, slotId, isSelected, () => SelectSlot(slotId));
            }
        }

        private void RefreshParts()
        {
            _availableParts.Clear();
            if (_catalog != null && _session != null && !string.IsNullOrEmpty(_selectedSlot))
            {
                var parts = _catalog.GetPartsForSlot(_session.WeaponId, _selectedSlot);
                if (parts != null) _availableParts.AddRange(parts);
            }

            if (_currentSlotLabel != null)
            {
                _currentSlotLabel.text = !string.IsNullOrEmpty(_selectedSlot)
                    ? $"SLOT: {_selectedSlot.ToUpperInvariant()}"
                    : "SELECT A SLOT";
            }

            if (_partsContainer == null) return;
            ClearChildren(_partsContainer);

            string currentPartId = null;
            _session?.DraftBuild?.Selections?.TryGetValue(_selectedSlot, out currentPartId);

            for (int i = 0; i < _availableParts.Count; i++)
            {
                WeaponPartSpec part = _availableParts[i];
                if (part == null) continue;

                string partId = part.PartId;
                string title = !string.IsNullOrEmpty(part.DisplayName) ? part.DisplayName : partId;
                bool isEquipped = string.Equals(partId, currentPartId, StringComparison.OrdinalIgnoreCase);

                CreatePartCard(_partsContainer, title, isEquipped, () => SelectPart(partId));
            }
        }

        private void RefreshUI()
        {
            // Title
            if (_weaponTitleText != null)
            {
                string name = _session != null ? _session.WeaponId : "WEAPON";
                if (_catalog != null && _session != null && _catalog.TryGetPlatform(_session.WeaponId, out var platform))
                {
                    name = platform.DisplayName.ToUpperInvariant();
                }
                _weaponTitleText.text = name;
            }

            // Stats
            UpdateStatsSummary();

            // Error
            UpdateErrors();

            // Buttons
            bool canApply = _session != null && _session.HasUnappliedChanges && _session.IsValid;
            bool canDiscard = _session != null && _session.HasUnappliedChanges;

            if (_applyButton != null) _applyButton.interactable = canApply;
            if (_discardButton != null) _discardButton.interactable = canDiscard;
        }

        private void UpdateStatsSummary()
        {
            if (_statsSummaryText == null) return;

            if (_session == null || _session.DraftStats == null)
            {
                _statsSummaryText.text = "";
                return;
            }

            ResolvedWeaponStats draft = _session.DraftStats;
            ResolvedWeaponStats baseline = _baselineStats ?? draft;

            var lines = new List<string>
            {
                FormatStatDiff("DAMAGE", draft.Damage, baseline.Damage, " HP", true),
                FormatStatDiff("FIRE RATE", draft.FireRate, baseline.FireRate, " /s", true),
                FormatStatDiff("MAGAZINE", draft.MagazineCapacity, baseline.MagazineCapacity, " RDS", true),
                FormatStatDiff("RELOAD", draft.ReloadDuration, baseline.ReloadDuration, "s", false),
                FormatStatDiff("RANGE", draft.Range, baseline.Range, "m", true),
                FormatStatDiff("ACCURACY", 100f - draft.BaseSpreadAngle, 100f - baseline.BaseSpreadAngle, "%", true)
            };

            _statsSummaryText.text = string.Join("\n", lines);
        }

        private static string FormatStatDiff(string statName, float current, float baseline, string unit, bool higherIsBetter)
        {
            float delta = current - baseline;
            if (Math.Abs(delta) < 0.001f)
            {
                return $"<color=#8B949E>{statName}:</color> <color=#FFFFFF>{current:0.#}{unit}</color>";
            }

            bool isPositive = delta > 0f;
            bool isGood = isPositive == higherIsBetter;
            string sign = isPositive ? "+" : "";
            string color = isGood ? "#00F5D4" : "#E63946";

            return $"<color=#8B949E>{statName}:</color> <color=#FFFFFF>{current:0.#}{unit}</color> <color={color}>({sign}{delta:0.#}{unit})</color>";
        }

        private void UpdateErrors()
        {
            var errors = new List<string>();

            if (_session != null)
            {
                if (!_session.IsValid)
                {
                    if (_session.DraftResolution?.Errors != null && _session.DraftResolution.Errors.Count > 0)
                        errors.AddRange(_session.DraftResolution.Errors);
                    else
                        errors.Add("Configuration is incomplete or incompatible.");
                }

                if (_session.LastSaveFailed)
                {
                    errors.Add(!string.IsNullOrEmpty(_session.LastSaveError) ? _session.LastSaveError : "Save failed.");
                }
            }

            bool hasError = errors.Count > 0;
            if (_errorRoot != null) _errorRoot.SetActive(hasError);
            if (_errorText != null) _errorText.text = string.Join("\n", errors);
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

        private static GameObject CreateSlotButton(RectTransform parent, string label, bool isSelected, UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Slot_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(115f, 40f);

            var img = btnObj.GetComponent<Image>();
            img.color = isSelected ? UITheme.ColorAccentCyan : UITheme.ColorButtonNormal;

            var btn = btnObj.GetComponent<Button>();
            UITheme.ApplyButtonColors(btn, isSelected ? UITheme.ColorAccentCyan : UITheme.ColorButtonNormal);
            if (onClick != null) btn.onClick.AddListener(onClick);

            var layout = btnObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 115f;
            layout.preferredHeight = 40f;
            layout.minHeight = 38f;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            UITheme.ApplyTextStyle(tmp, 13f, FontStyles.Bold, TextAlignmentOptions.Center, isSelected ? UITheme.ColorTextDark : UITheme.ColorTextPrimary);

            tmp.text = label.ToUpperInvariant();
            return btnObj;
        }

        private static GameObject CreatePartCard(RectTransform parent, string title, bool isEquipped, UnityAction onClick)
        {
            GameObject cardObj = new GameObject($"Part_{title}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            cardObj.transform.SetParent(parent, false);

            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 64f);

            var img = cardObj.GetComponent<Image>();
            img.color = isEquipped ? UITheme.ColorCardSurfaceAlt : UITheme.ColorCardSurface;

            var btn = cardObj.GetComponent<Button>();
            UITheme.ApplyButtonColors(btn, isEquipped ? UITheme.ColorCardSurfaceAlt : UITheme.ColorCardSurface);
            if (onClick != null) btn.onClick.AddListener(onClick);

            var layout = cardObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 140f;
            layout.preferredHeight = 64f;
            layout.minHeight = 58f;

            GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 4f);
            textRect.offsetMax = new Vector2(-6f, -4f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            UITheme.ApplyTextStyle(tmp, 12f, FontStyles.Normal, TextAlignmentOptions.Center, UITheme.ColorTextPrimary);

            string badge = isEquipped ? "\n<color=#00F5D4><size=10>[EQUIPPED]</size></color>" : "";
            tmp.text = $"<b>{title}</b>{badge}";

            return cardObj;
        }
    }
}
