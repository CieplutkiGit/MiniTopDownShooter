using Application;
using UnityEngine;

namespace Game
{
    public class GunAudio : AudioListenerBase
    {
        [SerializeField] private Gun _gunRef;
        [SerializeField] private AudioClip _shotClip;

        private IGunEvents _gun;
        private bool _isSubscribed;

        public void Initialize(Gun gun)
        {
            UnsubscribeEvents();
            _gunRef = gun;
            _gun = gun;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_gunRef == null)
            {
                _gunRef = GetComponent<Gun>();
            }

            _gun = _gunRef;
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
            if (_isSubscribed || _gun == null)
            {
                return;
            }

            _gun.Fired += HandleFired;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gun == null)
            {
                return;
            }

            _gun.Fired -= HandleFired;
            _isSubscribed = false;
        }

        private void HandleFired()
        {
            PlayClip(_shotClip);
        }
    }
}
