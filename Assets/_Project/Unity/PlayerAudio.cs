using Application;
using Core;
using UnityEngine;

namespace Game
{
    public class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private HealthComponent _playerHealthRef;
        [SerializeField] private PlayerController _playerRef;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _deathClip;

        private IHealthReadable _playerHealth;
        private IPlayerEvents _player;
        private int _lastHealth;

        private void Awake()
        {
            _playerHealth = _playerHealthRef;
            _player = _playerRef;
        }

        private void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += HandleHealthChanged;
                _lastHealth = _playerHealth.Current;
            }

            if (_player != null)
            {
                _player.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
            }

            if (_player != null)
            {
                _player.Died -= HandleDied;
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (current < _lastHealth)
            {
                PlayClip(_hitClip);
            }

            _lastHealth = current;
        }

        private void HandleDied()
        {
            PlayClip(_deathClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(clip);
        }
    }
}
