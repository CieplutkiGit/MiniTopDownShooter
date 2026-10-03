using System;

namespace Application
{
    public sealed class WeaponAmmoState
    {
        private int _magazineSize;
        private int _maxReserveAmmo;
        private bool _infiniteAmmo;

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
        public int MaxReserveAmmo => _maxReserveAmmo;
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

        public void ResetToInitial(int startingReserveAmmo)
        {
            InMagazine = _magazineSize;
            ReserveAmmo = _infiniteAmmo
                ? 0
                : Math.Min(Math.Max(0, startingReserveAmmo), _maxReserveAmmo);
        }

        /// <summary>
        /// Adapts magazine capacity and reserve ammo limits according to weapon workshop ammo conservation rules:
        /// - Customizing grants no free ammo.
        /// - If new magazine capacity >= InMagazine: InMagazine is preserved.
        /// - If new magazine capacity < InMagazine: excess rounds (InMagazine - newMagazineCapacity) are returned to ReserveAmmo.
        /// - If ReserveAmmo exceeds newMaxReserveAmmo: clamp to newMaxReserveAmmo.
        /// - Updates internal capacity limits.
        /// </summary>
        public void AdaptCapacity(int newMagazineCapacity, int newMaxReserveAmmo)
        {
            _magazineSize = Math.Max(1, newMagazineCapacity);
            _maxReserveAmmo = Math.Max(0, newMaxReserveAmmo);

            if (_infiniteAmmo)
            {
                InMagazine = Math.Min(InMagazine, _magazineSize);
                ReserveAmmo = 0;
                return;
            }

            if (InMagazine > _magazineSize)
            {
                int excess = InMagazine - _magazineSize;
                InMagazine = _magazineSize;
                ReserveAmmo += excess;
            }

            if (ReserveAmmo > _maxReserveAmmo)
            {
                ReserveAmmo = _maxReserveAmmo;
            }
        }

        public void AdaptCapacity(int newMagazineCapacity, int newMaxReserveAmmo, bool infiniteAmmo)
        {
            _infiniteAmmo = infiniteAmmo;
            AdaptCapacity(newMagazineCapacity, newMaxReserveAmmo);
        }
    }
}
