using Application;
using UnityEngine;

namespace Game
{
    public class WaveAudio : AudioListenerBase
    {
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private AudioClip _waveStartClip;

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
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _waves == null)
            {
                return;
            }

            _waves.WaveStarted -= HandleWaveStarted;
            _isSubscribed = false;
        }

        private void HandleWaveStarted(int waveNumber)
        {
            PlayClip(_waveStartClip);
        }
    }
}
