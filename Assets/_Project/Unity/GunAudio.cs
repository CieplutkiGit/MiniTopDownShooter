using Application;
using UnityEngine;

namespace Game
{
    public class GunAudio : AudioListenerBase
    {
        [SerializeField] private Gun _gunRef;
        [SerializeField] private AudioClip _shotClip;

        private IGunEvents _gun;

        private void Awake()
        {
            _gun = _gunRef;
        }

        private void OnEnable()
        {
            if (_gun == null)
            {
                return;
            }

            _gun.Fired += HandleFired;
        }

        private void OnDisable()
        {
            if (_gun == null)
            {
                return;
            }

            _gun.Fired -= HandleFired;
        }

        private void HandleFired()
        {
            PlayClip(_shotClip);
        }
    }
}
