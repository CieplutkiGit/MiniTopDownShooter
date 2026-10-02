using Application;
using UnityEngine;

namespace Game
{
    public class MuzzleFlashFX : MonoBehaviour
    {
        [SerializeField] private Gun _gunRef;
        [SerializeField] private ParticleSystem _muzzleEffect;

        private IGunEvents _gun;

        private void Awake()
        {
            if (_gunRef == null)
            {
                _gunRef = GetComponent<Gun>();
                if (_gunRef == null)
                {
                    _gunRef = GetComponentInParent<Gun>();
                }
            }

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
            if (_muzzleEffect == null)
            {
                return;
            }

            _muzzleEffect.Play();
        }
    }
}
