using System;
using System.Collections.Generic;
using System.Linq;
using Application.Weapons;
using Application.Workshop;
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
    /// - Header with Back to Lobby, Weapon Title, and Explode/Inspect toggles
    /// - Horizontal slot selector tabs (Receiver, Barrel, Magazine, Stock, etc.)
    /// - Part cards with active selection badges
    /// - Live stat diff indicators (Damage, Fire Rate, Reload, Range, Mag Size)
    /// - Apply & Discard action buttons with validation feedback
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

        [Header("Part Selector")]
        [SerializeField] private RectTransform _partsContainer;
        [SerializeField] private TMP_Text _currentSlotLabel;

        [Header("Stats Panel")]
        [SerializeField] private TMP_Text _statsSummaryText;

        [Header("Action Buttons")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _discardButton;

        [Header("Error Display")]
        [SerializeField] private GameObject _errorRoot;
        [SerializeField] private TMP_Text _errorText;

        [Header("Pinch Rotate Controller Hook")]
        [SerializeField] private WeaponPinchRotateController _rotateController;

        private IWeaponWorkshopSession _session;
        private IWeaponPreviewView _previewView;
        private IWeaponCatalog _catalog;

        private string _selectedSlot;
        private bool _isExploded;
        private ResolvedWeaponStats _baselineStats;
        private readonly List<string> _availableSlots = new List<string>();
        private readonly List<WeaponPartSpec> _availableParts = new List<WeaponPartSpec>();

        public event Action OnBackClicked;

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(HandleBack);
            if (_explodeButton != null) _explodeButton.onClick.AddListener(ToggleExploded);
            if (_resetViewButton != null) _resetViewButton.onClick.AddListener(ResetView);
            if (_applyButton != null) _applyButton.onClick.AddListener(ApplyChanges);
            if (_discardButton != null) _discardButton.onClick.AddListener(DiscardChanges);

            if (_rotateController == null)
                _rotateController = FindFirstObjectByType<WeaponPinchRotateController>();
        }

        public void Bind(IWeaponWorkshopSession session, IWeaponPreviewView previewView, IWeaponCatalog catalog)
        {
            Unbind();

            _session = session;
            _previewView = previewView;
            _catalog = catalog;

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
            if (_availableSlots.Count > 0)
            {
                SelectSlot(_availableSlots[0]);
            }

            RefreshUI();
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
        }

        public void SelectPart(string partId)
        {
            if (_session == null || string.IsNullOrEmpty(_selectedSlot))
                return;

            _session.SelectPart(_selectedSlot, partId);
            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshParts();
            RefreshUI();
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
            string color = isGood ? "#2EC4B6" : "#E63946";

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
            rect.sizeDelta = new Vector2(120f, 44f);

            var img = btnObj.GetComponent<Image>();
            img.color = isSelected ? new Color(0.0f, 0.76f, 0.74f, 1f) : new Color(0.13f, 0.16f, 0.20f, 0.9f);

            var btn = btnObj.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            var layout = btnObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 120f;
            layout.preferredHeight = 44f;
            layout.minHeight = 44f;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = label.ToUpperInvariant();
            tmp.fontSize = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isSelected ? Color.black : Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }

        private static GameObject CreatePartCard(RectTransform parent, string title, bool isEquipped, UnityAction onClick)
        {
            GameObject cardObj = new GameObject($"Part_{title}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            cardObj.transform.SetParent(parent, false);

            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 70f);

            var img = cardObj.GetComponent<Image>();
            img.color = isEquipped ? new Color(0.18f, 0.28f, 0.35f, 1f) : new Color(0.11f, 0.13f, 0.17f, 0.9f);

            var btn = cardObj.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            var layout = cardObj.GetComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.preferredHeight = 70f;
            layout.minHeight = 60f;

            GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            string badge = isEquipped ? "\n<color=#00F5D4><size=11>[EQUIPPED]</size></color>" : "";
            tmp.text = $"<size=13><b>{title}</b></size>{badge}";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return cardObj;
        }
    }
}
