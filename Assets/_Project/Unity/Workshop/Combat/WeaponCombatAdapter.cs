using System;
using Application.Weapons;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Component helper that bridges weapon combat runtime, live Gun instance,
    /// attachment anchor positioning, and player rotation aim sensitivity.
    /// </summary>
    public class WeaponCombatAdapter : MonoBehaviour, IWeaponBuildTarget
    {
        [SerializeField] private Gun _gun;
        [SerializeField] private PlayerRotation _playerRotation;
        [SerializeField] private Transform _muzzleAnchor;

        public Gun Gun => _gun;
        public PlayerRotation PlayerRotation => _playerRotation;
        public Transform MuzzleAnchor => _muzzleAnchor;

        public string WeaponId => _gun != null ? _gun.WeaponId : WeaponWorkshopIds.Rifle;
        public WeaponBuild CurrentBuild => _gun != null ? _gun.CurrentBuild : null;
        public ResolvedWeaponStats CurrentStats => _gun != null ? _gun.CurrentStats : null;

        private void Awake()
        {
            if (_gun == null)
            {
                _gun = GetComponent<Gun>() ?? GetComponentInChildren<Gun>();
            }

            if (_playerRotation == null)
            {
                _playerRotation = GetComponentInParent<PlayerRotation>() ?? GetComponent<PlayerRotation>();
            }

            if (_muzzleAnchor != null && _gun != null)
            {
                _gun.SetSpawnPoint(_muzzleAnchor);
            }
        }

        public void Bind(Gun gun, PlayerRotation playerRotation = null)
        {
            _gun = gun;
            if (playerRotation != null)
            {
                _playerRotation = playerRotation;
            }

            if (_muzzleAnchor != null && _gun != null)
            {
                _gun.SetSpawnPoint(_muzzleAnchor);
            }
        }

        public void SetMuzzleAnchor(Transform muzzleAnchor)
        {
            _muzzleAnchor = muzzleAnchor;
            if (_gun != null && muzzleAnchor != null)
            {
                _gun.SetSpawnPoint(muzzleAnchor);
            }
        }

        public ApplyResult TryApply(WeaponBuild build, ResolvedWeaponStats stats)
        {
            if (_gun == null)
            {
                return ApplyResult.Failure("MissingGun", "No target Gun component assigned to WeaponCombatAdapter.");
            }

            ApplyResult result = _gun.TryApply(build, stats);
            if (result.IsSuccess && stats != null && _playerRotation != null && stats.AimTurnSpeed > 0f)
            {
                _playerRotation.SetTurnSpeed(stats.AimTurnSpeed);
            }

            return result;
        }
    }
}
