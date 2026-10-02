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

        public void Initialize(WaveController waves)
        {
            _waveRef = waves;
            _waves = waves;
        }

        private void Awake()
        {
            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            _waves = _waveRef;
        }

        private void OnEnable()
        {
            if (_waves == null)
            {
                return;
            }

            _waves.WaveStarted += HandleWaveStarted;
            _waves.AllWavesCompleted += HandleAllWavesCompleted;
            UpdateLabel(_waves.CurrentWaveNumber);
        }

        private void OnDisable()
        {
            if (_waves == null)
            {
                return;
            }

            _waves.WaveStarted -= HandleWaveStarted;
            _waves.AllWavesCompleted -= HandleAllWavesCompleted;
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
