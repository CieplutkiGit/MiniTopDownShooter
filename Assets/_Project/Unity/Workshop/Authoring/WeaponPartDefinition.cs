using System;
using System.Collections.Generic;
using Application;
using Application.Weapons;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "WeaponPartDefinition", menuName = "Mini Top Down Shooter/Workshop/Weapon Part Definition")]
    public class WeaponPartDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _partId = "";
        [SerializeField] private string _slotId = "";
        [SerializeField] private string _displayName = "";
        [SerializeField, TextArea(2, 4)] private string _description = "";

        [Header("Additive Deltas")]
        [SerializeField] private float _damageDelta;
        [SerializeField] private float _fireIntervalDelta;
        [SerializeField] private int _magazineCapacityDelta;
        [SerializeField] private int _maxReserveAmmoDelta;
        [SerializeField] private float _reloadDurationDelta;
        [SerializeField] private float _baseSpreadAngleDelta;
        [SerializeField] private float _maxSpreadAngleDelta;
        [SerializeField] private float _spreadPerShotDelta;
        [SerializeField] private float _spreadRecoveryDelta;
        [SerializeField] private float _rangeDelta;
        [SerializeField] private float _projectileSpeedDelta;
        [SerializeField] private float _projectileLifetimeDelta;
        [SerializeField] private int _pelletsDelta;
        [SerializeField] private float _aimTurnSpeedDelta;

        [Header("Multipliers (1.0 = Neutral)")]
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private float _fireIntervalMultiplier = 1f;
        [SerializeField] private float _reloadDurationMultiplier = 1f;
        [SerializeField] private float _spreadMultiplier = 1f;
        [SerializeField] private float _rangeMultiplier = 1f;
        [SerializeField] private float _projectileSpeedMultiplier = 1f;
        [SerializeField] private float _aimTurnSpeedMultiplier = 1f;

        [Header("Overrides")]
        [SerializeField] private bool _overrideFireMode;
        [SerializeField] private Application.WeaponFireMode _fireModeOverride = Application.WeaponFireMode.SemiAutomatic;

        [SerializeField] private bool _overrideBurstCount;
        [SerializeField] private int _burstCountOverride = 3;

        [SerializeField] private bool _overrideBurstInterval;
        [SerializeField] private float _burstIntervalOverride = 0.08f;

        [SerializeField] private bool _overrideDeliveryMode;
        [SerializeField] private Application.WeaponDeliveryMode _deliveryModeOverride = Application.WeaponDeliveryMode.Projectile;

        [Header("Compatibility")]
        [SerializeField] private List<string> _incompatiblePartIds = new List<string>();
        [SerializeField] private List<string> _requiredMountTags = new List<string>();
        [SerializeField] private List<string> _providedMountTags = new List<string>();

        public string PartId
        {
            get => _partId;
            set => _partId = value;
        }

        public string SlotId
        {
            get => _slotId;
            set => _slotId = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public float DamageDelta
        {
            get => _damageDelta;
            set => _damageDelta = value;
        }

        public float FireIntervalDelta
        {
            get => _fireIntervalDelta;
            set => _fireIntervalDelta = value;
        }

        public int MagazineCapacityDelta
        {
            get => _magazineCapacityDelta;
            set => _magazineCapacityDelta = value;
        }

        public int MaxReserveAmmoDelta
        {
            get => _maxReserveAmmoDelta;
            set => _maxReserveAmmoDelta = value;
        }

        public float ReloadDurationDelta
        {
            get => _reloadDurationDelta;
            set => _reloadDurationDelta = value;
        }

        public float BaseSpreadAngleDelta
        {
            get => _baseSpreadAngleDelta;
            set => _baseSpreadAngleDelta = value;
        }

        public float MaxSpreadAngleDelta
        {
            get => _maxSpreadAngleDelta;
            set => _maxSpreadAngleDelta = value;
        }

        public float SpreadPerShotDelta
        {
            get => _spreadPerShotDelta;
            set => _spreadPerShotDelta = value;
        }

        public float SpreadRecoveryDelta
        {
            get => _spreadRecoveryDelta;
            set => _spreadRecoveryDelta = value;
        }

        public float RangeDelta
        {
            get => _rangeDelta;
            set => _rangeDelta = value;
        }

        public float ProjectileSpeedDelta
        {
            get => _projectileSpeedDelta;
            set => _projectileSpeedDelta = value;
        }

        public float ProjectileLifetimeDelta
        {
            get => _projectileLifetimeDelta;
            set => _projectileLifetimeDelta = value;
        }

        public int PelletsDelta
        {
            get => _pelletsDelta;
            set => _pelletsDelta = value;
        }

        public float AimTurnSpeedDelta
        {
            get => _aimTurnSpeedDelta;
            set => _aimTurnSpeedDelta = value;
        }

        public float DamageMultiplier
        {
            get => _damageMultiplier;
            set => _damageMultiplier = value;
        }

        public float FireIntervalMultiplier
        {
            get => _fireIntervalMultiplier;
            set => _fireIntervalMultiplier = value;
        }

        public float ReloadDurationMultiplier
        {
            get => _reloadDurationMultiplier;
            set => _reloadDurationMultiplier = value;
        }

        public float SpreadMultiplier
        {
            get => _spreadMultiplier;
            set => _spreadMultiplier = value;
        }

        public float RangeMultiplier
        {
            get => _rangeMultiplier;
            set => _rangeMultiplier = value;
        }

        public float ProjectileSpeedMultiplier
        {
            get => _projectileSpeedMultiplier;
            set => _projectileSpeedMultiplier = value;
        }

        public float AimTurnSpeedMultiplier
        {
            get => _aimTurnSpeedMultiplier;
            set => _aimTurnSpeedMultiplier = value;
        }

        public bool OverrideFireMode
        {
            get => _overrideFireMode;
            set => _overrideFireMode = value;
        }

        public Application.WeaponFireMode FireModeOverride
        {
            get => _fireModeOverride;
            set => _fireModeOverride = value;
        }

        public bool OverrideBurstCount
        {
            get => _overrideBurstCount;
            set => _overrideBurstCount = value;
        }

        public int BurstCountOverride
        {
            get => _burstCountOverride;
            set => _burstCountOverride = value;
        }

        public bool OverrideBurstInterval
        {
            get => _overrideBurstInterval;
            set => _overrideBurstInterval = value;
        }

        public float BurstIntervalOverride
        {
            get => _burstIntervalOverride;
            set => _burstIntervalOverride = value;
        }

        public bool OverrideDeliveryMode
        {
            get => _overrideDeliveryMode;
            set => _overrideDeliveryMode = value;
        }

        public Application.WeaponDeliveryMode DeliveryModeOverride
        {
            get => _deliveryModeOverride;
            set => _deliveryModeOverride = value;
        }

        public List<string> IncompatiblePartIds
        {
            get => _incompatiblePartIds ?? (_incompatiblePartIds = new List<string>());
            set => _incompatiblePartIds = value;
        }

        public List<string> RequiredMountTags
        {
            get => _requiredMountTags ?? (_requiredMountTags = new List<string>());
            set => _requiredMountTags = value;
        }

        public List<string> ProvidedMountTags
        {
            get => _providedMountTags ?? (_providedMountTags = new List<string>());
            set => _providedMountTags = value;
        }

        public void SetFireModeOverride(Application.WeaponFireMode? fireMode)
        {
            _overrideFireMode = fireMode.HasValue;
            if (fireMode.HasValue)
            {
                _fireModeOverride = fireMode.Value;
            }
        }

        public void SetBurstCountOverride(int? burstCount)
        {
            _overrideBurstCount = burstCount.HasValue;
            if (burstCount.HasValue)
            {
                _burstCountOverride = burstCount.Value;
            }
        }

        public void SetBurstIntervalOverride(float? burstInterval)
        {
            _overrideBurstInterval = burstInterval.HasValue;
            if (burstInterval.HasValue)
            {
                _burstIntervalOverride = burstInterval.Value;
            }
        }

        public void SetDeliveryModeOverride(Application.WeaponDeliveryMode? deliveryMode)
        {
            _overrideDeliveryMode = deliveryMode.HasValue;
            if (deliveryMode.HasValue)
            {
                _deliveryModeOverride = deliveryMode.Value;
            }
        }

        public WeaponPartSpec ToSpec()
        {
            return new WeaponPartSpec(
                partId: _partId ?? "",
                slotId: _slotId ?? "",
                displayName: string.IsNullOrEmpty(_displayName) ? (_partId ?? "") : _displayName,
                description: _description ?? "",
                damageDelta: _damageDelta,
                fireIntervalDelta: _fireIntervalDelta,
                magazineCapacityDelta: _magazineCapacityDelta,
                maxReserveAmmoDelta: _maxReserveAmmoDelta,
                reloadDurationDelta: _reloadDurationDelta,
                baseSpreadAngleDelta: _baseSpreadAngleDelta,
                maxSpreadAngleDelta: _maxSpreadAngleDelta,
                spreadPerShotDelta: _spreadPerShotDelta,
                spreadRecoveryDelta: _spreadRecoveryDelta,
                rangeDelta: _rangeDelta,
                projectileSpeedDelta: _projectileSpeedDelta,
                projectileLifetimeDelta: _projectileLifetimeDelta,
                pelletsDelta: _pelletsDelta,
                aimTurnSpeedDelta: _aimTurnSpeedDelta,
                damageMultiplier: _damageMultiplier,
                fireIntervalMultiplier: _fireIntervalMultiplier,
                reloadDurationMultiplier: _reloadDurationMultiplier,
                spreadMultiplier: _spreadMultiplier,
                rangeMultiplier: _rangeMultiplier,
                projectileSpeedMultiplier: _projectileSpeedMultiplier,
                aimTurnSpeedMultiplier: _aimTurnSpeedMultiplier,
                fireModeOverride: _overrideFireMode ? (Application.WeaponFireMode?)_fireModeOverride : null,
                burstCountOverride: _overrideBurstCount ? (int?)_burstCountOverride : null,
                burstIntervalOverride: _overrideBurstInterval ? (float?)_burstIntervalOverride : null,
                deliveryModeOverride: _overrideDeliveryMode ? (Application.WeaponDeliveryMode?)_deliveryModeOverride : null,
                incompatiblePartIds: _incompatiblePartIds != null ? new List<string>(_incompatiblePartIds) : null,
                requiredMountTags: _requiredMountTags != null ? new List<string>(_requiredMountTags) : null,
                providedMountTags: _providedMountTags != null ? new List<string>(_providedMountTags) : null
            );
        }
    }
}
