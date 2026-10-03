using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Workshop
{
    public sealed class StatDiff
    {
        public string StatName { get; }
        public float CurrentValue { get; }
        public float BaselineValue { get; }
        public float Delta => CurrentValue - BaselineValue;
        public string Formatted { get; }

        public StatDiff(string statName, float currentValue, float baselineValue, string unit = "", bool higherIsBetter = true)
        {
            StatName = statName;
            CurrentValue = currentValue;
            BaselineValue = baselineValue;
            float delta = currentValue - baselineValue;
            string sign = delta > 0f ? "+" : "";

            if (Math.Abs(delta) < 0.001f)
            {
                Formatted = $"{statName}: {currentValue:0.##}{unit}";
            }
            else
            {
                Formatted = $"{statName}: {currentValue:0.##}{unit} ({sign}{delta:0.##}{unit})";
            }
        }
    }

    public class WorkshopUIController : MonoBehaviour
    {
        [Header("State Controller")]
        [SerializeField] private GameStateController _gameStateRef;

        [Header("Panels & Roots")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _errorRoot;

        [Header("Text Displays")]
        [SerializeField] private TMP_Text _weaponNameText;
        [SerializeField] private TMP_Text _errorMessageText;
        [SerializeField] private TMP_Text _statsSummaryText;

        [Header("Action Buttons")]
        [SerializeField] private Button _explodeButton;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _discardButton;
        [SerializeField] private Button _exitButton;

        private IGameStateController _gameState;
        private IWeaponWorkshopSession _session;
        private IWeaponPreviewView _previewView;
        private IWeaponCatalog _catalog;

        private string _selectedSlot;
        private bool _isExploded;
        private ResolvedWeaponStats _baselineStats;
        private string _currentWeaponName = string.Empty;
        private string _errorMessage = string.Empty;
        private IReadOnlyList<string> _availableSlots = Array.Empty<string>();
        private IReadOnlyList<WeaponPartSpec> _availableParts = Array.Empty<WeaponPartSpec>();
        private List<StatDiff> _currentDiffs = new List<StatDiff>();

        public event Action<string> OnSlotSelected;
        public event Action OnExitRequested;

        public IWeaponWorkshopSession Session => _session;
        public IWeaponPreviewView PreviewView => _previewView;
        public IWeaponCatalog Catalog => _catalog;
        public string SelectedSlot => _selectedSlot;
        public bool IsExploded => _isExploded;
        public string CurrentWeaponName => _weaponNameText != null ? _weaponNameText.text : _currentWeaponName;
        public string ErrorMessage => _errorMessage;
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);
        public IReadOnlyList<string> AvailableSlots => _availableSlots;
        public IReadOnlyList<WeaponPartSpec> AvailableParts => _availableParts;
        public IReadOnlyList<StatDiff> CurrentDiffs => _currentDiffs;
        public GameObject Panel => _panel;
        public Button ApplyButton => _applyButton;
        public Button DiscardButton => _discardButton;
        public Button ExplodeButton => _explodeButton;
        public Button ExitButton => _exitButton;

        public bool IsApplyInteractable =>
            _applyButton != null ? _applyButton.interactable : (_session != null && _session.HasUnappliedChanges && _session.IsValid);

        public bool IsDiscardInteractable =>
            _discardButton != null ? _discardButton.interactable : (_session != null && _session.HasUnappliedChanges);

        public void Initialize(GameStateController gameState)
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }

            _gameStateRef = gameState;
            _gameState = gameState;

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameState.Menu, _gameState.CurrentState);
            }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;

            HookButtons();
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameState.Menu, _gameState.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            Unbind();
            UnhookButtons();
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }
        }

        public void Bind(IWeaponWorkshopSession session, IWeaponPreviewView previewView, IWeaponCatalog catalog = null)
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
                _previewView.SlotSelected += HandlePreviewSlotSelected;
                if (_session != null)
                {
                    _previewView.ShowBuild(_session.DraftBuild);
                }
            }

            RefreshAvailableSlots();
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
                _previewView.SlotSelected -= HandlePreviewSlotSelected;
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

            RefreshAvailableParts();
            OnSlotSelected?.Invoke(slotId);
        }

        public void SelectPart(string partId)
        {
            if (_session == null || string.IsNullOrEmpty(_selectedSlot))
            {
                return;
            }

            _session.SelectPart(_selectedSlot, partId);
            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshUI();
        }

        public void ToggleExploded()
        {
            _isExploded = !_isExploded;
            _previewView?.SetExploded(_isExploded);
        }

        public void Apply()
        {
            if (_session == null || !_session.HasUnappliedChanges || !_session.IsValid)
            {
                return;
            }

            ApplyResult result = _session.Apply();
            if (result.IsSuccess)
            {
                _baselineStats = _session.DraftStats;
            }

            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshUI();
        }

        public void Discard()
        {
            if (_session == null || !_session.HasUnappliedChanges)
            {
                return;
            }

            _session.Discard();
            _previewView?.ShowBuild(_session.DraftBuild);
            RefreshUI();
        }

        public void Exit()
        {
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopEditing)
            {
                _gameState.EnterWorkshopRoaming();
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            OnExitRequested?.Invoke();
        }

        public void RefreshUI()
        {
            UpdateWeaponName();
            RefreshAvailableSlots();
            RefreshAvailableParts();
            UpdateStatsDiffs();
            UpdateErrorMessages();
            UpdateButtonStates();
        }

        private void HookButtons()
        {
            if (_explodeButton != null) _explodeButton.onClick.AddListener(ToggleExploded);
            if (_applyButton != null) _applyButton.onClick.AddListener(Apply);
            if (_discardButton != null) _discardButton.onClick.AddListener(Discard);
            if (_exitButton != null) _exitButton.onClick.AddListener(Exit);
        }

        private void UnhookButtons()
        {
            if (_explodeButton != null) _explodeButton.onClick.RemoveListener(ToggleExploded);
            if (_applyButton != null) _applyButton.onClick.RemoveListener(Apply);
            if (_discardButton != null) _discardButton.onClick.RemoveListener(Discard);
            if (_exitButton != null) _exitButton.onClick.RemoveListener(Exit);
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            bool isEditing = newState == GameState.WorkshopEditing;
            if (_panel != null)
            {
                _panel.SetActive(isEditing);
            }
        }

        private void HandleSessionChanged(IWeaponWorkshopSession session)
        {
            if (_previewView != null && _session != null)
            {
                _previewView.ShowBuild(_session.DraftBuild);
            }

            RefreshUI();
        }

        private void HandlePreviewSlotSelected(string slotId)
        {
            SelectSlot(slotId);
        }

        private void UpdateWeaponName()
        {
            if (_session == null)
            {
                _currentWeaponName = string.Empty;
            }
            else if (_catalog != null && _catalog.TryGetPlatform(_session.WeaponId, out var platform))
            {
                _currentWeaponName = platform.DisplayName;
            }
            else
            {
                _currentWeaponName = _session.WeaponId;
            }

            if (_weaponNameText != null)
            {
                _weaponNameText.text = _currentWeaponName;
            }
        }

        private void RefreshAvailableSlots()
        {
            if (_session == null)
            {
                _availableSlots = Array.Empty<string>();
                return;
            }

            if (_catalog != null && _catalog.TryGetPlatform(_session.WeaponId, out var platform) && platform.SupportedSlots != null)
            {
                _availableSlots = platform.SupportedSlots;
            }
            else if (_session.DraftBuild != null && _session.DraftBuild.Selections != null)
            {
                _availableSlots = _session.DraftBuild.Selections.Keys.ToList().AsReadOnly();
            }
            else
            {
                _availableSlots = Array.Empty<string>();
            }

            if (!string.IsNullOrEmpty(_selectedSlot) && !_availableSlots.Contains(_selectedSlot))
            {
                _selectedSlot = _availableSlots.Count > 0 ? _availableSlots[0] : null;
            }
        }

        private void RefreshAvailableParts()
        {
            if (_catalog != null && _session != null && !string.IsNullOrEmpty(_selectedSlot))
            {
                _availableParts = _catalog.GetPartsForSlot(_session.WeaponId, _selectedSlot) ?? Array.Empty<WeaponPartSpec>();
            }
            else
            {
                _availableParts = Array.Empty<WeaponPartSpec>();
            }
        }

        private void UpdateStatsDiffs()
        {
            _currentDiffs.Clear();

            if (_session == null || _session.DraftStats == null)
            {
                if (_statsSummaryText != null) _statsSummaryText.text = string.Empty;
                return;
            }

            ResolvedWeaponStats draft = _session.DraftStats;
            ResolvedWeaponStats baseline = _baselineStats ?? draft;

            _currentDiffs.Add(new StatDiff("Damage", draft.Damage, baseline.Damage, " HP"));
            _currentDiffs.Add(new StatDiff("Fire Rate", draft.FireRate, baseline.FireRate, " /s"));
            _currentDiffs.Add(new StatDiff("Fire Interval", draft.FireInterval, baseline.FireInterval, "s", higherIsBetter: false));
            _currentDiffs.Add(new StatDiff("Mag Capacity", draft.MagazineCapacity, baseline.MagazineCapacity, " rds"));
            _currentDiffs.Add(new StatDiff("Reload Time", draft.ReloadDuration, baseline.ReloadDuration, "s", higherIsBetter: false));
            _currentDiffs.Add(new StatDiff("Range", draft.Range, baseline.Range, "m"));
            _currentDiffs.Add(new StatDiff("Velocity", draft.ProjectileSpeed, baseline.ProjectileSpeed, "m/s"));
            _currentDiffs.Add(new StatDiff("Aim Speed", draft.AimTurnSpeed, baseline.AimTurnSpeed, "°/s"));
            _currentDiffs.Add(new StatDiff("Base Spread", draft.BaseSpreadAngle, baseline.BaseSpreadAngle, "°", higherIsBetter: false));
            _currentDiffs.Add(new StatDiff("Recoil", draft.RecoilPerShot, baseline.RecoilPerShot, "°", higherIsBetter: false));

            if (_statsSummaryText != null)
            {
                _statsSummaryText.text = string.Join("\n", _currentDiffs.Select(d => d.Formatted));
            }
        }

        private void UpdateErrorMessages()
        {
            var errors = new List<string>();

            if (_session != null)
            {
                if (!_session.IsValid)
                {
                    if (_session.DraftResolution?.Errors != null && _session.DraftResolution.Errors.Count > 0)
                    {
                        errors.AddRange(_session.DraftResolution.Errors);
                    }
                    else
                    {
                        errors.Add("Draft build is invalid.");
                    }
                }

                if (_session.LastSaveFailed)
                {
                    string saveErr = string.IsNullOrEmpty(_session.LastSaveError)
                        ? "Save failed."
                        : _session.LastSaveError;
                    errors.Add(saveErr);
                }
            }

            _errorMessage = string.Join("\n", errors);

            if (_errorMessageText != null)
            {
                _errorMessageText.text = _errorMessage;
            }

            if (_errorRoot != null)
            {
                _errorRoot.SetActive(!string.IsNullOrEmpty(_errorMessage));
            }
        }

        private void UpdateButtonStates()
        {
            bool canApply = _session != null && _session.HasUnappliedChanges && _session.IsValid;
            bool canDiscard = _session != null && _session.HasUnappliedChanges;

            if (_applyButton != null)
            {
                _applyButton.interactable = canApply;
            }

            if (_discardButton != null)
            {
                _discardButton.interactable = canDiscard;
            }
        }
    }
}
