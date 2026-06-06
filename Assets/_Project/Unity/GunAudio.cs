using Application;
using UnityEngine;

namespace Game
{
    public class GunAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
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
            if (_shotClip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(_shotClip);
        }
    }
}
