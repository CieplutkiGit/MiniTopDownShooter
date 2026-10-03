using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Application.Weapons
{
    public sealed class WeaponPlatformSpec
    {
        public string WeaponId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> SupportedSlots { get; }
        public IReadOnlyDictionary<string, string> DefaultParts { get; }

        public float BaseDamage { get; }
        public float BaseFireInterval { get; }
        public int BaseMagazineCapacity { get; }
        public int BaseStartingReserveAmmo { get; }
        public int BaseMaxReserveAmmo { get; }
        public float BaseReloadDuration { get; }
        public float BaseSpreadAngle { get; }
        public float MaxSpreadAngle { get; }
        public float SpreadPerShot { get; }
        public float SpreadRecoveryPerSecond { get; }
        public float Range { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileLifetime { get; }
        public int BasePellets { get; }
        public WeaponFireMode BaseFireMode { get; }
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

        public WeaponPlatformSpec(
            string weaponId,
            string displayName,
            IEnumerable<string> supportedSlots,
            IDictionary<string, string> defaultParts,
            float baseDamage,
            float baseFireInterval,
            int baseMagazineCapacity,
            int baseStartingReserveAmmo,
            int baseMaxReserveAmmo,
            float baseReloadDuration,
            float baseSpreadAngle,
            float maxSpreadAngle,
            float spreadPerShot,
            float spreadRecoveryPerSecond,
            float range,
            float projectileSpeed,
            float projectileLifetime,
            int basePellets,
            WeaponFireMode baseFireMode,
            int burstCount,
            float burstInterval,
            float aimTurnSpeed,
            WeaponDeliveryMode deliveryMode,
            bool infiniteAmmo = false,
            bool autoReloadOnEmpty = true,
            bool cancelReloadOnFire = true,
            float damageFalloffStart = 0f,
            float damageFalloffEnd = 0f,
            float minDamageRatio = 1f)
        {
            WeaponId = weaponId ?? throw new ArgumentNullException(nameof(weaponId));
            DisplayName = displayName ?? weaponId;
            SupportedSlots = (supportedSlots != null ? supportedSlots.ToList() : new List<string>()).AsReadOnly();

            var defaults = new Dictionary<string, string>(StringComparer.Ordinal);
            if (defaultParts != null)
            {
                foreach (var kvp in defaultParts)
                {
                    defaults[kvp.Key] = kvp.Value;
                }
            }
            DefaultParts = new ReadOnlyDictionary<string, string>(defaults);

            BaseDamage = Math.Max(1f, baseDamage);
            BaseFireInterval = Math.Max(0.01f, baseFireInterval);
            BaseMagazineCapacity = Math.Max(1, baseMagazineCapacity);
            BaseStartingReserveAmmo = Math.Max(0, baseStartingReserveAmmo);
            BaseMaxReserveAmmo = Math.Max(BaseMagazineCapacity, baseMaxReserveAmmo);
            BaseReloadDuration = Math.Max(0.05f, baseReloadDuration);
            BaseSpreadAngle = Math.Max(0f, baseSpreadAngle);
            MaxSpreadAngle = Math.Max(BaseSpreadAngle, maxSpreadAngle);
            SpreadPerShot = Math.Max(0f, spreadPerShot);
            SpreadRecoveryPerSecond = Math.Max(0.1f, spreadRecoveryPerSecond);
            Range = Math.Max(1f, range);
            ProjectileSpeed = Math.Max(0f, projectileSpeed);
            ProjectileLifetime = Math.Max(0.1f, projectileLifetime);
            BasePellets = Math.Max(1, basePellets);
            BaseFireMode = baseFireMode;
            BurstCount = Math.Max(1, burstCount);
            BurstInterval = Math.Max(0.01f, burstInterval);
            AimTurnSpeed = Math.Max(10f, aimTurnSpeed);
            DeliveryMode = deliveryMode;
            InfiniteAmmo = infiniteAmmo;
            AutoReloadOnEmpty = autoReloadOnEmpty;
            CancelReloadOnFire = cancelReloadOnFire;
            DamageFalloffStart = Math.Max(0f, damageFalloffStart);
            DamageFalloffEnd = Math.Max(DamageFalloffStart, damageFalloffEnd);
            MinDamageRatio = Math.Max(0f, Math.Min(1f, minDamageRatio));
        }

        public WeaponBuild CreateDefaultBuild()
        {
            return new WeaponBuild(WeaponId, new Dictionary<string, string>(DefaultParts));
        }
    }
}
