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

        [Header("Audio Mixer & Music Hooks")]
        [SerializeField] private UnityEngine.Audio.AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";
        [SerializeField] private AudioSource _musicSource;

        [Header("Input Sliders")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Slider _deadzoneSlider;

        [Header("Mobile Touch Controls")]
        [SerializeField] private Toggle _mobileTouchControlsToggle;
        [SerializeField] private Slider _touchScaleSlider;

        [Header("Buttons")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetDefaultsButton;
        [SerializeField] private Button _closeButton;

        [Header("Runtime Binding")]
        [SerializeField] private PlayerController _player;

        private GameSettingsData _currentSettings;
        private GameStateController _modalGameState;
        private bool _pausedForSettings;
        public bool IsOpen { get; private set; }

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
            SetPanelActive(false);
            _currentSettings = SaveManager.LoadSettings();
            ApplySettingsToUI(_currentSettings);
        }

        private void Start()
        {
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
            IsOpen = false;
            _pausedForSettings = false;
            ClearGameplayInput();
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

        private void SetPanelActive(bool active)
        {
            if (_panel == null)
            {
                return;
            }

            if (_panel == gameObject)
            {
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = active ? 1f : 0f;
                cg.interactable = active;
                cg.blocksRaycasts = active;
            }
            else
            {
                _panel.SetActive(active);
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _modalGameState = Game.Flow.SceneComponents.Find<GameStateController>(gameObject.scene);
            _pausedForSettings = _modalGameState != null && _modalGameState.CanEnterState(GameState.Paused);
            if (_pausedForSettings) _modalGameState.Pause();
            ClearGameplayInput();
            _currentSettings = SaveManager.LoadSettings();
            ApplySettingsToUI(_currentSettings);

            SetPanelActive(true);
            transform.SetAsLastSibling();

            if (_firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
            }
        }

        public void Close()
        {
            IsOpen = false;
            SetPanelActive(false);
            ClearGameplayInput();
            if (_pausedForSettings && _modalGameState != null && _modalGameState.CurrentState == GameState.Paused)
                _modalGameState.Resume();
            _pausedForSettings = false;
        }

        private void ClearGameplayInput()
        {
            var player = _player != null ? _player : Game.Flow.SceneComponents.Find<PlayerController>(gameObject.scene);
            player?.Input?.ResetGameplayTransientState();
            player?.Input?.RequireNeutralToRearm();
            Game.Flow.SceneComponents.Find<MobileInputState>(gameObject.scene)?.ResetAll();
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

            if (_mobileTouchControlsToggle != null)
            {
                _mobileTouchControlsToggle.isOn = data.MobileTouchControls;
            }

            if (_touchScaleSlider != null)
            {
                _touchScaleSlider.value = data.TouchControlScale;
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

            if (_mobileTouchControlsToggle != null)
            {
                _currentSettings.MobileTouchControls = _mobileTouchControlsToggle.isOn;
            }

            if (_touchScaleSlider != null)
            {
                _currentSettings.TouchControlScale = _touchScaleSlider.value;
            }
        }

        public UnityEngine.Audio.AudioMixer AudioMixer
        {
            get => _audioMixer;
            set => _audioMixer = value;
        }

        public AudioSource MusicSource
        {
            get => _musicSource;
            set => _musicSource = value;
        }

        public string MasterVolumeParam
        {
            get => !string.IsNullOrEmpty(_masterVolumeParam) ? _masterVolumeParam : "MasterVolume";
            set => _masterVolumeParam = value;
        }

        public string MusicVolumeParam
        {
            get => !string.IsNullOrEmpty(_musicVolumeParam) ? _musicVolumeParam : "MusicVolume";
            set => _musicVolumeParam = value;
        }

        public string SFXVolumeParam
        {
            get => !string.IsNullOrEmpty(_sfxVolumeParam) ? _sfxVolumeParam : "SFXVolume";
            set => _sfxVolumeParam = value;
        }

        private void ApplySettingsToGame(GameSettingsData data)
        {
            if (data == null)
            {
                return;
            }

            if (_audioMixer != null)
            {
                // Use mixer as the sole volume authority: remove duplicate attenuation
                AudioListener.volume = 1f;

                if (_musicSource != null)
                {
                    _musicSource.volume = 1f;
                }

                string masterParam = !string.IsNullOrEmpty(_masterVolumeParam) ? _masterVolumeParam : "MasterVolume";
                string musicParam = !string.IsNullOrEmpty(_musicVolumeParam) ? _musicVolumeParam : "MusicVolume";
                string sfxParam = !string.IsNullOrEmpty(_sfxVolumeParam) ? _sfxVolumeParam : "SFXVolume";

                SetMixerVolume(masterParam, data.MasterVolume);
                SetMixerVolume(musicParam, data.MusicVolume);
                SetMixerVolume(sfxParam, data.SFXVolume);
            }
            else
            {
                // Fallback when no AudioMixer is assigned
                AudioListener.volume = data.MasterVolume;

                if (_musicSource != null)
                {
                    _musicSource.volume = data.MusicVolume;
                }
            }

            MobileDemoControlsBootstrap mobileControls = FindFirstObjectByType<MobileDemoControlsBootstrap>();
            if (mobileControls != null)
            {
                mobileControls.ApplySettings(data);
            }

            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            if (_player != null)
            {
                _player.ApplySettings(data);
            }
        }

        private void SetMixerVolume(string parameter, float volume)
        {
            if (!_audioMixer.SetFloat(parameter, VolumeToDecibels(volume)))
            {
                Debug.LogWarning($"[SettingsUI] Could not apply mixer parameter '{parameter}'.");
            }
        }

        private static float VolumeToDecibels(float volume)
        {
            if (volume <= 0.0001f)
            {
                return -80f;
            }
            return Mathf.Log10(volume) * 20f;
        }
    }
}
