using Application;
using TMPro;
using UnityEngine;

namespace Game
{
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private TMP_Text _label;

        private IWaveProvider _waves;
        private bool _isSubscribed;

        public void Initialize(WaveController waves)
        {
            UnsubscribeEvents();
            _waveRef = waves;
            _waves = waves;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            _waves = _waveRef;
            if (_label != null)
            {
                _label.raycastTarget = false;
                _label.enableAutoSizing = true;
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _waves == null)
            {
                return;
            }

            _waves.WaveStarted += HandleWaveStarted;
            _waves.AllWavesCompleted += HandleAllWavesCompleted;
            _isSubscribed = true;
            UpdateLabel(_waves.CurrentWaveNumber);
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _waves == null)
            {
                return;
            }

            _waves.WaveStarted -= HandleWaveStarted;
            _waves.AllWavesCompleted -= HandleAllWavesCompleted;
            _isSubscribed = false;
        }

        private void HandleWaveStarted(int waveNumber)
        {
            UpdateLabel(waveNumber);
        }

        private void HandleAllWavesCompleted()
        {
            if (_label != null)
            {
                _label.text = "Victory!";
            }
        }

        private void UpdateLabel(int waveNumber)
        {
            if (_label == null)
            {
                return;
            }

            if (waveNumber <= 0)
            {
                _label.text = "";
                return;
            }

            _label.text = $"Wave {waveNumber}";
        }
    }
}
