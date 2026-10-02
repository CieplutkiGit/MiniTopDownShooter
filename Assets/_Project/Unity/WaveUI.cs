using Cieplutki.MiniTopDownShooter.Application;
using TMPro;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private TMP_Text _label;

        private IWaveProvider _waves;

        private void Awake()
        {
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
                _label.text = "Cleared";
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
