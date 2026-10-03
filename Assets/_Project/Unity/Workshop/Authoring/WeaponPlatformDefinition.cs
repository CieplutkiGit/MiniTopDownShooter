using System;
using System.Collections.Generic;
using Application;
using Application.Weapons;
using UnityEngine;

namespace Game
{
    [Serializable]
    public struct SlotPartPair
    {
        [SerializeField] private string _slotId;
        [SerializeField] private string _partId;

        public string SlotId
        {
            get => _slotId;
            set => _slotId = value;
        }

        public string PartId
        {
            get => _partId;
            set => _partId = value;
        }

        public SlotPartPair(string slotId, string partId)
        {
            _slotId = slotId;
            _partId = partId;
        }
    }

    [CreateAssetMenu(fileName = "WeaponPlatformDefinition", menuName = "Mini Top Down Shooter/Workshop/Weapon Platform Definition")]
    public class WeaponPlatformDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _weaponId = "";
        [SerializeField] private string _displayName = "";

        [Header("Configuration")]
        [SerializeField] private List<string> _supportedSlots = new List<string>();
        [SerializeField] private List<SlotPartPair> _defaultParts = new List<SlotPartPair>();

        [Header("Base Numerical Stats")]
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _baseFireInterval = 0.2f;
        [SerializeField] private int _baseMagazineCapacity = 12;
        [SerializeField] private int _baseStartingReserveAmmo = 48;
        [SerializeField] private int _baseMaxReserveAmmo = 120;
        [SerializeField] private float _baseReloadDuration = 1.2f;
        [SerializeField] private float _baseSpreadAngle = 0f;
        [SerializeField] private float _maxSpreadAngle = 20f;
        [SerializeField] private float _spreadPerShot = 0f;
        [SerializeField] private float _spreadRecoveryPerSecond = 12f;
        [SerializeField] private float _range = 50f;
        [SerializeField] private float _projectileSpeed = 20f;
        [SerializeField] private float _projectileLifetime = 3f;
        [SerializeField] private int _basePellets = 1;
        [SerializeField] private Application.WeaponFireMode _baseFireMode = Application.WeaponFireMode.Automatic;
        [SerializeField] private int _burstCount = 3;
        [SerializeField] private float _burstInterval = 0.08f;
        [SerializeField] private float _aimTurnSpeed = 180f;
        [SerializeField] private Application.WeaponDeliveryMode _deliveryMode = Application.WeaponDeliveryMode.Projectile;
        [SerializeField] private bool _infiniteAmmo = false;
        [SerializeField] private bool _autoReloadOnEmpty = true;
        [SerializeField] private bool _cancelReloadOnFire = true;
        [SerializeField] private float _damageFalloffStart = 0f;
        [SerializeField] private float _damageFalloffEnd = 0f;
        [SerializeField] private float _minDamageRatio = 1f;

        public string WeaponId
        {
            get => _weaponId;
            set => _weaponId = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public List<string> SupportedSlots
        {
            get => _supportedSlots ?? (_supportedSlots = new List<string>());
            set => _supportedSlots = value;
        }

        public List<SlotPartPair> DefaultParts
        {
            get => _defaultParts ?? (_defaultParts = new List<SlotPartPair>());
            set => _defaultParts = value;
        }

        public float BaseDamage
        {
            get => _baseDamage;
            set => _baseDamage = value;
        }

        public float BaseFireInterval
        {
            get => _baseFireInterval;
            set => _baseFireInterval = value;
        }

        public int BaseMagazineCapacity
        {
            get => _baseMagazineCapacity;
            set => _baseMagazineCapacity = value;
        }

        public int BaseStartingReserveAmmo
        {
            get => _baseStartingReserveAmmo;
            set => _baseStartingReserveAmmo = value;
        }

        public int BaseMaxReserveAmmo
        {
            get => _baseMaxReserveAmmo;
            set => _baseMaxReserveAmmo = value;
        }

        public float BaseReloadDuration
        {
            get => _baseReloadDuration;
            set => _baseReloadDuration = value;
        }

        public float BaseSpreadAngle
        {
            get => _baseSpreadAngle;
            set => _baseSpreadAngle = value;
        }

        public float MaxSpreadAngle
        {
            get => _maxSpreadAngle;
            set => _maxSpreadAngle = value;
        }

        public float SpreadPerShot
        {
            get => _spreadPerShot;
            set => _spreadPerShot = value;
        }

        public float SpreadRecoveryPerSecond
        {
            get => _spreadRecoveryPerSecond;
            set => _spreadRecoveryPerSecond = value;
        }

        public float Range
        {
            get => _range;
            set => _range = value;
        }

        public float ProjectileSpeed
        {
            get => _projectileSpeed;
            set => _projectileSpeed = value;
        }

        public float ProjectileLifetime
        {
            get => _projectileLifetime;
            set => _projectileLifetime = value;
        }

        public int BasePellets
        {
            get => _basePellets;
            set => _basePellets = value;
        }

        public Application.WeaponFireMode BaseFireMode
        {
            get => _baseFireMode;
            set => _baseFireMode = value;
        }

        public int BurstCount
        {
            get => _burstCount;
            set => _burstCount = value;
        }

        public float BurstInterval
        {
            get => _burstInterval;
            set => _burstInterval = value;
        }

        public float AimTurnSpeed
        {
            get => _aimTurnSpeed;
            set => _aimTurnSpeed = value;
        }

        public Application.WeaponDeliveryMode DeliveryMode
        {
            get => _deliveryMode;
            set => _deliveryMode = value;
        }

        public bool InfiniteAmmo
        {
            get => _infiniteAmmo;
            set => _infiniteAmmo = value;
        }

        public bool AutoReloadOnEmpty
        {
            get => _autoReloadOnEmpty;
            set => _autoReloadOnEmpty = value;
        }

        public bool CancelReloadOnFire
        {
            get => _cancelReloadOnFire;
            set => _cancelReloadOnFire = value;
        }

        public float DamageFalloffStart
        {
            get => _damageFalloffStart;
            set => _damageFalloffStart = value;
        }

        public float DamageFalloffEnd
        {
            get => _damageFalloffEnd;
            set => _damageFalloffEnd = value;
        }

        public float MinDamageRatio
        {
            get => _minDamageRatio;
            set => _minDamageRatio = value;
        }

        public void SetDefaultParts(IDictionary<string, string> defaultParts)
        {
            _defaultParts = new List<SlotPartPair>();
            if (defaultParts != null)
            {
                foreach (var kvp in defaultParts)
                {
                    _defaultParts.Add(new SlotPartPair(kvp.Key, kvp.Value));
                }
            }
        }

        public WeaponPlatformSpec ToSpec()
        {
            var defaultDict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_defaultParts != null)
            {
                foreach (var pair in _defaultParts)
                {
                    if (!string.IsNullOrEmpty(pair.SlotId))
                    {
                        defaultDict[pair.SlotId] = pair.PartId ?? "";
                    }
                }
            }

            return new WeaponPlatformSpec(
                weaponId: _weaponId ?? "",
                displayName: string.IsNullOrEmpty(_displayName) ? (_weaponId ?? "") : _displayName,
                supportedSlots: _supportedSlots ?? new List<string>(),
                defaultParts: defaultDict,
                baseDamage: _baseDamage,
                baseFireInterval: _baseFireInterval,
                baseMagazineCapacity: _baseMagazineCapacity,
                baseStartingReserveAmmo: _baseStartingReserveAmmo,
                baseMaxReserveAmmo: _baseMaxReserveAmmo,
                baseReloadDuration: _baseReloadDuration,
                baseSpreadAngle: _baseSpreadAngle,
                maxSpreadAngle: _maxSpreadAngle,
                spreadPerShot: _spreadPerShot,
                spreadRecoveryPerSecond: _spreadRecoveryPerSecond,
                range: _range,
                projectileSpeed: _projectileSpeed,
                projectileLifetime: _projectileLifetime,
                basePellets: _basePellets,
                baseFireMode: _baseFireMode,
                burstCount: _burstCount,
                burstInterval: _burstInterval,
                aimTurnSpeed: _aimTurnSpeed,
                deliveryMode: _deliveryMode,
                infiniteAmmo: _infiniteAmmo,
                autoReloadOnEmpty: _autoReloadOnEmpty,
                cancelReloadOnFire: _cancelReloadOnFire,
                damageFalloffStart: _damageFalloffStart,
                damageFalloffEnd: _damageFalloffEnd,
                minDamageRatio: _minDamageRatio
            );
        }

        public void CaptureFromWeaponDefinition(
            WeaponDefinition def,
            string weaponId,
            string displayName,
            List<string> supportedSlots,
            List<SlotPartPair> defaultParts)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            _weaponId = weaponId;
            _displayName = displayName;
            _supportedSlots = supportedSlots != null ? new List<string>(supportedSlots) : new List<string>();
            _defaultParts = defaultParts != null ? new List<SlotPartPair>(defaultParts) : new List<SlotPartPair>();

            _baseDamage = def.Damage;
            _baseFireInterval = def.FireInterval;
            _baseMagazineCapacity = def.MagazineSize;
            _baseStartingReserveAmmo = def.StartingReserveAmmo;
            _baseMaxReserveAmmo = def.MaxReserveAmmo;
            _baseReloadDuration = def.ReloadDuration;
            _baseSpreadAngle = def.SpreadAngle;
            _maxSpreadAngle = def.MaxSpreadAngle;
            _spreadPerShot = def.SpreadPerShot;
            _spreadRecoveryPerSecond = def.SpreadRecoveryPerSecond;
            _range = def.HitscanRange > 0f ? def.HitscanRange : (def.ProjectileSpeed * def.ProjectileLifetime);
            _projectileSpeed = def.ProjectileSpeed;
            _projectileLifetime = def.ProjectileLifetime;
            _basePellets = def.ProjectilesPerShot;
            _baseFireMode = (Application.WeaponFireMode)def.FireMode;
            _burstCount = def.BurstCount;
            _burstInterval = def.BurstInterval;
            _aimTurnSpeed = 180f;
            _deliveryMode = (Application.WeaponDeliveryMode)def.DeliveryMode;
            _infiniteAmmo = def.InfiniteAmmo;
            _autoReloadOnEmpty = def.AutoReloadOnEmpty;
            _cancelReloadOnFire = def.CancelReloadOnFire;
            _damageFalloffStart = 0f;
            _damageFalloffEnd = 0f;
            _minDamageRatio = 1f;
        }
    }
}
