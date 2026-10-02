using System;

namespace Application
{
    public sealed class WeaponAmmoState
    {
        private readonly int _magazineSize;
        private readonly int _maxReserveAmmo;
        private readonly bool _infiniteAmmo;

        public WeaponAmmoState(
            int magazineSize,
            int startingReserveAmmo,
            int maxReserveAmmo,
            bool infiniteAmmo)
        {
            _magazineSize = Math.Max(1, magazineSize);
            _maxReserveAmmo = Math.Max(0, maxReserveAmmo);
            _infiniteAmmo = infiniteAmmo;

            InMagazine = _magazineSize;
            ReserveAmmo = _infiniteAmmo
                ? 0
                : Math.Min(Math.Max(0, startingReserveAmmo), _maxReserveAmmo);
        }

        public int InMagazine { get; private set; }
        public int ReserveAmmo { get; private set; }
        public int MagazineSize => _magazineSize;
        public bool InfiniteAmmo => _infiniteAmmo;

        public bool CanFire => _infiniteAmmo || InMagazine > 0;

        public bool CanReload =>
            !_infiniteAmmo &&
            InMagazine < _magazineSize &&
            ReserveAmmo > 0;

        public bool TryConsumeRound()
        {
            if (_infiniteAmmo)
            {
                return true;
            }

            if (InMagazine <= 0)
            {
                return false;
            }

            InMagazine--;
            return true;
        }

        public int Reload()
        {
            if (!CanReload)
            {
                return 0;
            }

            int needed = _magazineSize - InMagazine;
            int loaded = Math.Min(needed, ReserveAmmo);
            InMagazine += loaded;
            ReserveAmmo -= loaded;
            return loaded;
        }

        public int AddReserve(int amount)
        {
            if (_infiniteAmmo || amount <= 0)
            {
                return 0;
            }

            int before = ReserveAmmo;
            ReserveAmmo = Math.Min(_maxReserveAmmo, ReserveAmmo + amount);
            return ReserveAmmo - before;
        }

        public void Refill()
        {
            InMagazine = _magazineSize;

            if (!_infiniteAmmo)
            {
                ReserveAmmo = _maxReserveAmmo;
            }
        }
    }
}
