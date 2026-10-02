using System;

namespace Application
{
    public class WeaponRuntimeConfig
    {
        public WeaponFireMode FireMode = WeaponFireMode.Automatic;
        public float FireInterval = 0.2f;
        public int BurstCount = 3;
        public float BurstInterval = 0.08f;
        public float BaseSpreadAngle = 0f;
        public float MaxSpreadAngle = 20f;
        public float SpreadPerShot = 0f;
        public float SpreadRecoveryPerSecond = 12f;
        public float ReloadDuration = 1.2f;
        public bool AutoReloadOnEmpty = true;
        public bool CancelReloadOnFire = true;
        public bool InfiniteAmmo = true;
        public int MagazineSize = 12;
        public int StartingReserveAmmo = 48;
        public int MaxReserveAmmo = 120;
    }

    public class WeaponRuntime
    {
        private readonly WeaponRuntimeConfig _config;
        private readonly WeaponAmmoState _ammo;

        private float _lastShootTime = float.NegativeInfinity;
        private float _currentDynamicSpread;
        private bool _isReloading;
        private float _reloadCompleteTime;
        private int _burstShotsRemaining;
        private float _nextBurstShotTime;

        public event Action Fired;
        public event Action EmptyFired;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public event Action ReloadCanceled;
        public event Action<int, int> AmmoChanged;

        public WeaponRuntime(WeaponRuntimeConfig config, WeaponAmmoState ammo = null)
        {
            _config = config ?? new WeaponRuntimeConfig();
            _ammo = ammo ?? new WeaponAmmoState(
                _config.MagazineSize,
                _config.StartingReserveAmmo,
                _config.MaxReserveAmmo,
                _config.InfiniteAmmo);
        }

        public WeaponRuntimeConfig Config => _config;
        public WeaponAmmoState Ammo => _ammo;
        public bool IsReloading => _isReloading;
        public int BurstShotsRemaining => _burstShotsRemaining;
        public float LastShootTime => _lastShootTime;
        public float CurrentSpreadAngle => _config.BaseSpreadAngle + _currentDynamicSpread;
        public float DynamicSpread => _currentDynamicSpread;

        public float ReloadProgress(float currentTime)
        {
            if (!_isReloading || _config.ReloadDuration <= 0f)
            {
                return 0f;
            }

            float remaining = _reloadCompleteTime - currentTime;
            return Math.Max(0f, Math.Min(1f, 1f - (remaining / _config.ReloadDuration)));
        }

        public bool TryFire(float currentTime, bool ignoreFireInterval = false)
        {
            if (_isReloading)
            {
                return false;
            }

            if (!ignoreFireInterval && currentTime < _lastShootTime + _config.FireInterval)
            {
                return false;
            }

            if (!_ammo.TryConsumeRound())
            {
                EmptyFired?.Invoke();

                if (_config.AutoReloadOnEmpty && _ammo.CanReload)
                {
                    StartReload(currentTime);
                }

                return false;
            }

            _lastShootTime = currentTime;
            AddSpread();
            Fired?.Invoke();
            AmmoChanged?.Invoke(_ammo.InMagazine, _ammo.ReserveAmmo);

            if (!_ammo.InfiniteAmmo && _ammo.InMagazine <= 0 && _config.AutoReloadOnEmpty && _ammo.CanReload)
            {
                StartReload(currentTime);
            }

            return true;
        }

        public bool StartBurst(float currentTime)
        {
            if (_burstShotsRemaining > 0 || _isReloading || _ammo.InMagazine <= 0)
            {
                return false;
            }

            if (currentTime < _lastShootTime + _config.FireInterval)
            {
                return false;
            }

            _burstShotsRemaining = _config.BurstCount;
            _nextBurstShotTime = currentTime;

            return true;
        }

        public bool TickBurst(float currentTime)
        {
            if (_burstShotsRemaining <= 0 || _config.FireMode != WeaponFireMode.Burst)
            {
                return false;
            }

            if (currentTime < _nextBurstShotTime)
            {
                return false;
            }

            if (!TryFire(currentTime, true))
            {
                _burstShotsRemaining = 0;
                return false;
            }

            _burstShotsRemaining--;

            if (_burstShotsRemaining > 0)
            {
                _nextBurstShotTime = currentTime + _config.BurstInterval;
            }

            return true;
        }

        public void CancelBurst()
        {
            _burstShotsRemaining = 0;
        }

        public bool StartReload(float currentTime)
        {
            if (_isReloading || !_ammo.CanReload)
            {
                return false;
            }

            _burstShotsRemaining = 0;
            _isReloading = true;
            _reloadCompleteTime = currentTime + _config.ReloadDuration;
            ReloadStarted?.Invoke();

            if (_config.ReloadDuration <= 0f)
            {
                CompleteReload();
            }

            return true;
        }

        public void CancelReload()
        {
            if (!_isReloading)
            {
                return;
            }

            _isReloading = false;
            ReloadCanceled?.Invoke();
        }

        public bool TickReload(float currentTime)
        {
            if (_isReloading && currentTime >= _reloadCompleteTime)
            {
                CompleteReload();
                return true;
            }

            return false;
        }

        public void RecoverSpread(float deltaTime)
        {
            if (_currentDynamicSpread <= 0f || _config.SpreadRecoveryPerSecond <= 0f)
            {
                return;
            }

            _currentDynamicSpread = Math.Max(
                0f,
                _currentDynamicSpread - _config.SpreadRecoveryPerSecond * deltaTime);
        }

        public int AddReserveAmmo(int amount)
        {
            int added = _ammo.AddReserve(amount);
            if (added > 0)
            {
                AmmoChanged?.Invoke(_ammo.InMagazine, _ammo.ReserveAmmo);
            }
            return added;
        }

        public void Reset()
        {
            _burstShotsRemaining = 0;
            _isReloading = false;
            _currentDynamicSpread = 0f;
            _lastShootTime = float.NegativeInfinity;
            _ammo.ResetToInitial(_config.StartingReserveAmmo);
            AmmoChanged?.Invoke(_ammo.InMagazine, _ammo.ReserveAmmo);
        }

        private void CompleteReload()
        {
            if (!_isReloading)
            {
                return;
            }

            _isReloading = false;
            _ammo.Reload();
            ReloadCompleted?.Invoke();
            AmmoChanged?.Invoke(_ammo.InMagazine, _ammo.ReserveAmmo);
        }

        private void AddSpread()
        {
            float maxDynamic = Math.Max(0f, _config.MaxSpreadAngle - _config.BaseSpreadAngle);
            _currentDynamicSpread = Math.Min(maxDynamic, _currentDynamicSpread + _config.SpreadPerShot);
        }
    }
}
