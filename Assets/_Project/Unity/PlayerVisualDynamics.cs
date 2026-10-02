using System;
using UnityEngine;

namespace Game
{
    public class PlayerVisualDynamics : MonoBehaviour
    {
        [Header("Movement Tilt")]
        [SerializeField] private float _maxPitchAngle = 7f;
        [SerializeField] private float _maxRollAngle = 9f;
        [SerializeField] private float _tiltSmoothSpeed = 10f;
        [SerializeField] private float _speedReference = 5f;

        [Header("Recoil Impulse")]
        [SerializeField] private float _kickbackDistance = 0.07f;
        [SerializeField] private float _recoilPitchAngle = 3.5f;
        [SerializeField] private float _recoilRecoverySpeed = 18f;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _lastWorldPosition;
        private Vector2 _currentTilt;
        private float _currentRecoilOffset;
        private float _currentRecoilPitch;

        private Gun _activeGun;
        private WeaponLoadout _loadout;
        private PlayerShoot _shoot;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _lastWorldPosition = transform.parent != null ? transform.parent.position : transform.position;

            _loadout = GetComponentInParent<WeaponLoadout>();
            _shoot = GetComponentInParent<PlayerShoot>();
        }

        private void OnEnable()
        {
            _lastWorldPosition = transform.parent != null ? transform.parent.position : transform.position;
            _currentTilt = Vector2.zero;
            _currentRecoilOffset = 0f;
            _currentRecoilPitch = 0f;

            if (_loadout != null)
            {
                _loadout.WeaponEquipped += HandleWeaponEquipped;
            }

            HookActiveGun();
        }

        private void OnDisable()
        {
            if (_loadout != null)
            {
                _loadout.WeaponEquipped -= HandleWeaponEquipped;
            }

            UnhookActiveGun();
        }

        private void Update()
        {
            Transform parent = transform.parent;
            Vector3 worldPos = parent != null ? parent.position : transform.position;
            float dt = Time.deltaTime;

            if (dt > 0.0001f)
            {
                Vector3 worldVelocity = (worldPos - _lastWorldPosition) / dt;
                Vector3 localVelocity = parent != null
                    ? parent.InverseTransformDirection(worldVelocity)
                    : worldVelocity;

                float speedNorm = Mathf.Max(0.1f, _speedReference);
                float targetPitch = Mathf.Clamp(-localVelocity.z / speedNorm * _maxPitchAngle, -_maxPitchAngle, _maxPitchAngle);
                float targetRoll = Mathf.Clamp(-localVelocity.x / speedNorm * _maxRollAngle, -_maxRollAngle, _maxRollAngle);

                _currentTilt.x = Mathf.Lerp(_currentTilt.x, targetPitch, dt * _tiltSmoothSpeed);
                _currentTilt.y = Mathf.Lerp(_currentTilt.y, targetRoll, dt * _tiltSmoothSpeed);
            }

            _lastWorldPosition = worldPos;

            _currentRecoilOffset = Mathf.Lerp(_currentRecoilOffset, 0f, dt * _recoilRecoverySpeed);
            _currentRecoilPitch = Mathf.Lerp(_currentRecoilPitch, 0f, dt * _recoilRecoverySpeed);

            transform.localPosition = _initialLocalPosition + new Vector3(0f, 0f, _currentRecoilOffset);
            Quaternion tiltRot = Quaternion.Euler(_currentTilt.x + _currentRecoilPitch, 0f, _currentTilt.y);
            transform.localRotation = _initialLocalRotation * tiltRot;

            if (_activeGun == null)
            {
                HookActiveGun();
            }
        }

        public void TriggerRecoil()
        {
            _currentRecoilOffset = Mathf.Clamp(_currentRecoilOffset - _kickbackDistance, -_kickbackDistance * 1.5f, 0f);
            _currentRecoilPitch = Mathf.Clamp(_currentRecoilPitch - _recoilPitchAngle, -_recoilPitchAngle * 1.5f, 0f);
        }

        private void HandleWeaponEquipped(Gun gun, int slot)
        {
            UnhookActiveGun();
            _activeGun = gun;
            if (_activeGun != null)
            {
                _activeGun.Fired += HandleFired;
            }
        }

        private void HookActiveGun()
        {
            UnhookActiveGun();

            if (_loadout != null && _loadout.ActiveGun != null)
            {
                _activeGun = _loadout.ActiveGun;
            }
            else if (_shoot != null && _shoot.ActiveGun != null)
            {
                _activeGun = _shoot.ActiveGun;
            }
            else
            {
                _activeGun = GetComponentInParent<Gun>();
            }

            if (_activeGun != null)
            {
                _activeGun.Fired += HandleFired;
            }
        }

        private void UnhookActiveGun()
        {
            if (_activeGun != null)
            {
                _activeGun.Fired -= HandleFired;
                _activeGun = null;
            }
        }

        private void HandleFired()
        {
            TriggerRecoil();
        }
    }
}
