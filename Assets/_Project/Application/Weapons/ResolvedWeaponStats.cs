using System;

namespace Application.Weapons
{
    /// <summary>
    /// Immutable resolved weapon statistics produced deterministically by IWeaponBuildResolver.
    /// Units:
    /// - Damage: Raw HP points (clamped >= 1, rounded to nearest 0.1)
    /// - FireInterval: Seconds per round (clamped >= 0.02, rounded to 4 decimals)
    /// - FireRate: Rounds per second (1.0 / FireInterval)
    /// - MagazineCapacity: Integer rounds (clamped >= 1)
    /// - MaxReserveAmmo: Integer rounds (clamped >= MagazineCapacity)
    /// - StartingReserveAmmo: Integer rounds (clamped >= 0)
    /// - ReloadDuration: Seconds (clamped >= 0.1, rounded to 2 decimals)
    /// - BaseSpreadAngle: Degrees half-angle at rest (clamped >= 0, rounded to 2 decimals)
    /// - MaxSpreadAngle: Cap in degrees (clamped >= BaseSpreadAngle, rounded to 2 decimals)
    /// - RecoilPerShot: Spread increase per shot in degrees (clamped >= 0, rounded to 2 decimals)
    /// - SpreadRecoveryRate: Degrees per second recovered (clamped >= 0.1, rounded to 2 decimals)
    /// - Range: Meters (clamped >= 1.0, rounded to 1 decimal)
    /// - ProjectileSpeed: Meters per second (clamped >= 0, rounded to 1 decimal)
    /// - ProjectileLifetime: Seconds (clamped >= 0.1, rounded to 2 decimals)
    /// - PelletCount: Integer pellets or rays per shot (clamped >= 1)
    /// - AimTurnSpeed: Degrees per second (clamped >= 10.0, rounded to 1 decimal)
    /// </summary>
    public sealed class ResolvedWeaponStats
    {
        public float Damage { get; }
        public float FireInterval { get; }
        public float FireRate => FireInterval > 0f ? (float)Math.Round(1f / FireInterval, 2) : 0f;
        public int MagazineCapacity { get; }
        public int MaxReserveAmmo { get; }
        public int StartingReserveAmmo { get; }
        public float ReloadDuration { get; }
        public float BaseSpreadAngle { get; }
        public float MaxSpreadAngle { get; }
        public float RecoilPerShot { get; }
        public float SpreadRecoveryRate { get; }
        public float Range { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileLifetime { get; }
        public int PelletCount { get; }
        public WeaponFireMode FireMode { get; }
        public int BurstCount { get; }
        public float BurstInterval { get; }
        public float AimTurnSpeed { get; }
        public WeaponDeliveryMode DeliveryMode { get; }
        public bool InfiniteAmmo { get; }
        public bool AutoReloadOnEmpty { get; }
        public bool CancelReloadOnFire { get; }
        public float DamageFalloffStart { get; }
        public float DamageFalloffEnd { get; }
        public float MinDamageRatio { get; }

        public ResolvedWeaponStats(
            float damage,
            float fireInterval,
            int magazineCapacity,
            int maxReserveAmmo,
            int startingReserveAmmo,
            float reloadDuration,
            float baseSpreadAngle,
            float maxSpreadAngle,
            float recoilPerShot,
            float spreadRecoveryRate,
            float range,
            float projectileSpeed,
            float projectileLifetime,
            int pelletCount,
            WeaponFireMode fireMode,
            int burstCount,
            float burstInterval,
            float aimTurnSpeed,
            WeaponDeliveryMode deliveryMode,
            bool infiniteAmmo,
            bool autoReloadOnEmpty,
            bool cancelReloadOnFire,
            float damageFalloffStart = 0f,
            float damageFalloffEnd = 0f,
            float minDamageRatio = 1f)
        {
            Damage = (float)Math.Round(Math.Max(1f, damage), 1);
            FireInterval = (float)Math.Round(Math.Max(0.02f, fireInterval), 4);
            MagazineCapacity = Math.Max(1, magazineCapacity);
            MaxReserveAmmo = Math.Max(MagazineCapacity, maxReserveAmmo);
            StartingReserveAmmo = Math.Max(0, Math.Min(startingReserveAmmo, MaxReserveAmmo));
            ReloadDuration = (float)Math.Round(Math.Max(0.1f, reloadDuration), 2);
            BaseSpreadAngle = (float)Math.Round(Math.Max(0f, baseSpreadAngle), 2);
            MaxSpreadAngle = (float)Math.Round(Math.Max(BaseSpreadAngle, maxSpreadAngle), 2);
            RecoilPerShot = (float)Math.Round(Math.Max(0f, recoilPerShot), 2);
            SpreadRecoveryRate = (float)Math.Round(Math.Max(0.1f, spreadRecoveryRate), 2);
            Range = (float)Math.Round(Math.Max(1f, range), 1);
            ProjectileSpeed = (float)Math.Round(Math.Max(0f, projectileSpeed), 1);
            ProjectileLifetime = (float)Math.Round(Math.Max(0.1f, projectileLifetime), 2);
            PelletCount = Math.Max(1, pelletCount);
            FireMode = fireMode;
            BurstCount = Math.Max(1, burstCount);
            BurstInterval = (float)Math.Round(Math.Max(0.01f, burstInterval), 4);
            AimTurnSpeed = (float)Math.Round(Math.Max(10f, aimTurnSpeed), 1);
            DeliveryMode = deliveryMode;
            InfiniteAmmo = infiniteAmmo;
            AutoReloadOnEmpty = autoReloadOnEmpty;
            CancelReloadOnFire = cancelReloadOnFire;
            DamageFalloffStart = (float)Math.Round(Math.Max(0f, damageFalloffStart), 1);
            DamageFalloffEnd = (float)Math.Round(Math.Max(DamageFalloffStart, damageFalloffEnd), 1);
            MinDamageRatio = (float)Math.Round(Math.Max(0f, Math.Min(1f, minDamageRatio)), 2);
        }
    }
}
