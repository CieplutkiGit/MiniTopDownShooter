using Application;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Panels & Navigation")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Selectable _firstSelected;

        [Header("Audio Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Input Sliders")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Slider _deadzoneSlider;

        [Header("Buttons")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetDefaultsButton;
        [SerializeField] private Button _closeButton;

        [Header("Runtime Binding")]
        [SerializeField] private PlayerController _player;

        private GameSettingsData _currentSettings;

        public void Initialize(PlayerController player)
        {
            _player = player;
            if (_currentSettings != null && _player != null)
            {
                _player.ApplySettings(_currentSettings);
            }
        }

        private void Awake()
        {
            _currentSettings = SaveManager.LoadSettings();
            ApplySettingsToUI(_currentSettings);
            ApplySettingsToGame(_currentSettings);
        }

        private void OnEnable()
        {
            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(SaveAndApply);
            }

            if (_resetDefaultsButton != null)
            {
                _resetDefaultsButton.onClick.AddListener(ResetDefaults);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
            }
        }

        private void OnDisable()
        {
            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(SaveAndApply);
            }

            if (_resetDefaultsButton != null)
            {
                _resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Open()
        {
            _currentSettings = SaveManager.LoadSettings();
            ApplySettingsToUI(_currentSettings);

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            if (_firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
            }
        }

        public void Close()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void SaveAndApply()
        {
            ReadSettingsFromUI();
            SaveManager.SaveSettings(_currentSettings);
            ApplySettingsToGame(_currentSettings);
            Close();
        }

        public void ResetDefaults()
        {
            _currentSettings = new GameSettingsData();
            ApplySettingsToUI(_currentSettings);
        }

        private void ApplySettingsToUI(GameSettingsData data)
        {
            if (data == null)
            {
                return;
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = data.MasterVolume;
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = data.MusicVolume;
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = data.SFXVolume;
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.value = data.AimSensitivity;
            }

            if (_deadzoneSlider != null)
            {
                _deadzoneSlider.value = data.Deadzone;
            }
        }

        private void ReadSettingsFromUI()
        {
            if (_currentSettings == null)
            {
                _currentSettings = new GameSettingsData();
            }

            if (_masterVolumeSlider != null)
            {
                _currentSettings.MasterVolume = _masterVolumeSlider.value;
            }

            if (_musicVolumeSlider != null)
            {
                _currentSettings.MusicVolume = _musicVolumeSlider.value;
            }

            if (_sfxVolumeSlider != null)
            {
                _currentSettings.SFXVolume = _sfxVolumeSlider.value;
            }

            if (_sensitivitySlider != null)
            {
                _currentSettings.AimSensitivity = _sensitivitySlider.value;
            }

            if (_deadzoneSlider != null)
            {
                _currentSettings.Deadzone = _deadzoneSlider.value;
            }
        }

        private void ApplySettingsToGame(GameSettingsData data)
        {
            if (data == null)
            {
                return;
            }

            AudioListener.volume = data.MasterVolume;

            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            if (_player != null)
            {
                _player.ApplySettings(data);
            }
        }
    }
}
