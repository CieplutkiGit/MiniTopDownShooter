using Application;
using UnityEngine;

namespace Game
{
    public class WaveAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private AudioClip _waveStartClip;

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
            if (_waveStartClip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(_waveStartClip);
        }
    }
}
