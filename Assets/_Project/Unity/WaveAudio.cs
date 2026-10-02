using Application;
using UnityEngine;

namespace Game
{
    public class WaveAudio : AudioListenerBase
    {
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private AudioClip _waveStartClip;

        private IWaveProvider _waves;

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
        }

        private void OnDisable()
        {
            if (_waves == null)
            {
                return;
            }

            _waves.WaveStarted -= HandleWaveStarted;
        }

        private void HandleWaveStarted(int waveNumber)
        {
            PlayClip(_waveStartClip);
        }
    }
}
