using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Weapons
{
    public sealed class WeaponPartSpec
    {
        public string PartId { get; }
        public string SlotId { get; }
        public string DisplayName { get; }
        public string Description { get; }

        // Additive modifiers
        public float DamageDelta { get; }
        public float FireIntervalDelta { get; }
        public int MagazineCapacityDelta { get; }
        public int MaxReserveAmmoDelta { get; }
        public float ReloadDurationDelta { get; }
        public float BaseSpreadAngleDelta { get; }
        public float MaxSpreadAngleDelta { get; }
        public float SpreadPerShotDelta { get; }
        public float SpreadRecoveryDelta { get; }
        public float RangeDelta { get; }
        public float ProjectileSpeedDelta { get; }
        public float ProjectileLifetimeDelta { get; }
        public int PelletsDelta { get; }
        public float AimTurnSpeedDelta { get; }

        // Multipliers (1.0 = neutral)
        public float DamageMultiplier { get; }
        public float FireIntervalMultiplier { get; }
        public float ReloadDurationMultiplier { get; }
        public float SpreadMultiplier { get; }
        public float RangeMultiplier { get; }
        public float ProjectileSpeedMultiplier { get; }
        public float AimTurnSpeedMultiplier { get; }

        // Overrides
        public WeaponFireMode? FireModeOverride { get; }
        public int? BurstCountOverride { get; }
        public float? BurstIntervalOverride { get; }
        public WeaponDeliveryMode? DeliveryModeOverride { get; }

        // Compatibility
        public IReadOnlyList<string> IncompatiblePartIds { get; }
        public IReadOnlyList<string> RequiredMountTags { get; }
        public IReadOnlyList<string> ProvidedMountTags { get; }

        public WeaponPartSpec(
            string partId,
            string slotId,
            string displayName,
            string description = "",
            float damageDelta = 0f,
            float fireIntervalDelta = 0f,
            int magazineCapacityDelta = 0,
            int maxReserveAmmoDelta = 0,
            float reloadDurationDelta = 0f,
            float baseSpreadAngleDelta = 0f,
            float maxSpreadAngleDelta = 0f,
            float spreadPerShotDelta = 0f,
            float spreadRecoveryDelta = 0f,
            float rangeDelta = 0f,
            float projectileSpeedDelta = 0f,
            float projectileLifetimeDelta = 0f,
            int pelletsDelta = 0,
            float aimTurnSpeedDelta = 0f,
            float damageMultiplier = 1f,
            float fireIntervalMultiplier = 1f,
            float reloadDurationMultiplier = 1f,
            float spreadMultiplier = 1f,
            float rangeMultiplier = 1f,
            float projectileSpeedMultiplier = 1f,
            float aimTurnSpeedMultiplier = 1f,
            WeaponFireMode? fireModeOverride = null,
            int? burstCountOverride = null,
            float? burstIntervalOverride = null,
            WeaponDeliveryMode? deliveryModeOverride = null,
            IEnumerable<string> incompatiblePartIds = null,
            IEnumerable<string> requiredMountTags = null,
            IEnumerable<string> providedMountTags = null)
        {
            PartId = partId ?? throw new ArgumentNullException(nameof(partId));
            SlotId = slotId ?? throw new ArgumentNullException(nameof(slotId));
            DisplayName = displayName ?? partId;
            Description = description ?? "";

            DamageDelta = damageDelta;
            FireIntervalDelta = fireIntervalDelta;
            MagazineCapacityDelta = magazineCapacityDelta;
            MaxReserveAmmoDelta = maxReserveAmmoDelta;
            ReloadDurationDelta = reloadDurationDelta;
            BaseSpreadAngleDelta = baseSpreadAngleDelta;
            MaxSpreadAngleDelta = maxSpreadAngleDelta;
            SpreadPerShotDelta = spreadPerShotDelta;
            SpreadRecoveryDelta = spreadRecoveryDelta;
            RangeDelta = rangeDelta;
            ProjectileSpeedDelta = projectileSpeedDelta;
            ProjectileLifetimeDelta = projectileLifetimeDelta;
            PelletsDelta = pelletsDelta;
            AimTurnSpeedDelta = aimTurnSpeedDelta;

            DamageMultiplier = Math.Max(0.01f, damageMultiplier);
            FireIntervalMultiplier = Math.Max(0.01f, fireIntervalMultiplier);
            ReloadDurationMultiplier = Math.Max(0.01f, reloadDurationMultiplier);
            SpreadMultiplier = Math.Max(0.01f, spreadMultiplier);
            RangeMultiplier = Math.Max(0.01f, rangeMultiplier);
            ProjectileSpeedMultiplier = Math.Max(0.01f, projectileSpeedMultiplier);
            AimTurnSpeedMultiplier = Math.Max(0.01f, aimTurnSpeedMultiplier);

            FireModeOverride = fireModeOverride;
            BurstCountOverride = burstCountOverride;
            BurstIntervalOverride = burstIntervalOverride;
            DeliveryModeOverride = deliveryModeOverride;

            IncompatiblePartIds = (incompatiblePartIds != null ? incompatiblePartIds.ToList() : new List<string>()).AsReadOnly();
            RequiredMountTags = (requiredMountTags != null ? requiredMountTags.ToList() : new List<string>()).AsReadOnly();
            ProvidedMountTags = (providedMountTags != null ? providedMountTags.ToList() : new List<string>()).AsReadOnly();
        }
    }
}
