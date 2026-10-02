using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class MuzzleFlashFX : MonoBehaviour
    {
        [SerializeField] private Gun _gunRef;
        [SerializeField] private ParticleSystem _muzzleEffect;

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
            if (_muzzleEffect == null)
            {
                return;
            }

            _muzzleEffect.Play();
        }
    }
}
