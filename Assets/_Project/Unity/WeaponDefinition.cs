using Application;
using UnityEngine;

namespace Game
{
    public enum WeaponFireMode
    {
        SemiAutomatic = 0,
        Automatic = 1,
        Burst = 2,
        Shotgun = 3
    }

    public enum WeaponDeliveryMode
    {
        Projectile = 0,
        Hitscan = 1
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Mini Top Down Shooter/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Firing")]
        [SerializeField] private WeaponFireMode _fireMode = WeaponFireMode.Automatic;
        [SerializeField] private WeaponDeliveryMode _deliveryMode = WeaponDeliveryMode.Projectile;

        [Min(1)]
        [SerializeField] private int _damage = 10;

        [Min(0.01f)]
        [SerializeField] private float _fireInterval = 0.2f;

        [Min(1)]
        [SerializeField] private int _projectilesPerShot = 1;

        [Range(0f, 45f)]
        [SerializeField] private float _spreadAngle;

        [Min(0f)]
        [SerializeField] private float _spreadPerShot;

        [Range(0f, 60f)]
        [SerializeField] private float _maxSpreadAngle = 20f;

        [Min(0f)]
        [SerializeField] private float _spreadRecoveryPerSecond = 12f;

        [Header("Burst")]
        [Min(2)]
        [SerializeField] private int _burstCount = 3;

        [Min(0.01f)]
        [SerializeField] private float _burstInterval = 0.08f;

        [Header("Ammo")]
        [Tooltip("Enabled by default so existing WeaponDefinition assets remain backwards compatible.")]
        [SerializeField] private bool _infiniteAmmo = true;

        [Min(1)]
        [SerializeField] private int _magazineSize = 12;

        [Min(0)]
        [SerializeField] private int _startingReserveAmmo = 48;

        [Min(0)]
        [SerializeField] private int _maxReserveAmmo = 120;

        [Min(0f)]
        [SerializeField] private float _reloadDuration = 1.2f;

        [SerializeField] private bool _autoReloadOnEmpty = true;
        [SerializeField] private bool _cancelReloadOnFire = true;

        [Header("Projectile")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private bool _overrideProjectileMotion;

        [Min(0.01f)]
        [SerializeField] private float _projectileSpeed = 20f;

        [Min(0.01f)]
        [SerializeField] private float _projectileLifetime = 3f;

        [Header("Hitscan")]
        [Min(0.1f)]
        [SerializeField] private float _hitscanRange = 50f;

        [SerializeField] private LayerMask _hitscanMask = ~0;

        [Min(0)]
        [SerializeField] private int _maxPenetrations;

        [SerializeField] private bool _useDamageFalloff;

        [SerializeField] private AnimationCurve _damageFalloff =
            AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Pooling")]
        [Min(1)]
        [SerializeField] private int _defaultPoolSize = 10;

        [Min(1)]
        [SerializeField] private int _maxPoolSize = 20;

        public WeaponFireMode FireMode => _fireMode;
        public WeaponDeliveryMode DeliveryMode => _deliveryMode;
        public Projectile ProjectilePrefab => _projectilePrefab;
        public int Damage => _damage;
        public float FireInterval => _fireInterval;
        public int ProjectilesPerShot => _projectilesPerShot;
        public float SpreadAngle => _spreadAngle;
        public float SpreadPerShot => _spreadPerShot;
        public float MaxSpreadAngle => Mathf.Max(_spreadAngle, _maxSpreadAngle);
        public float SpreadRecoveryPerSecond => _spreadRecoveryPerSecond;
        public int BurstCount => _burstCount;
        public float BurstInterval => _burstInterval;
        public bool InfiniteAmmo => _infiniteAmmo;
        public int MagazineSize => _magazineSize;
        public int StartingReserveAmmo => _startingReserveAmmo;
        public int MaxReserveAmmo => _maxReserveAmmo;
        public float ReloadDuration => _reloadDuration;
        public bool AutoReloadOnEmpty => _autoReloadOnEmpty;
        public bool CancelReloadOnFire => _cancelReloadOnFire;
        public bool OverrideProjectileMotion => _overrideProjectileMotion;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float HitscanRange => _hitscanRange;
        public LayerMask HitscanMask => _hitscanMask;
        public int MaxPenetrations => _maxPenetrations;
        public int DefaultPoolSize => _defaultPoolSize;
        public int MaxPoolSize => _maxPoolSize;

        public float EvaluateDamageMultiplier(float distance)
        {
            if (!_useDamageFalloff || _damageFalloff == null || _hitscanRange <= 0f)
            {
                return 1f;
            }

            float normalizedDistance = Mathf.Clamp01(distance / _hitscanRange);
            return Mathf.Max(0f, _damageFalloff.Evaluate(normalizedDistance));
        }

        public WeaponRuntimeConfig CreateRuntimeConfig()
        {
            return new WeaponRuntimeConfig
            {
                FireMode = (Application.WeaponFireMode)_fireMode,
                FireInterval = _fireInterval,
                BurstCount = _burstCount,
                BurstInterval = _burstInterval,
                BaseSpreadAngle = _spreadAngle,
                MaxSpreadAngle = _maxSpreadAngle,
                SpreadPerShot = _spreadPerShot,
                SpreadRecoveryPerSecond = _spreadRecoveryPerSecond,
                ReloadDuration = _reloadDuration,
                AutoReloadOnEmpty = _autoReloadOnEmpty,
                CancelReloadOnFire = _cancelReloadOnFire,
                InfiniteAmmo = _infiniteAmmo,
                MagazineSize = _magazineSize,
                StartingReserveAmmo = _startingReserveAmmo,
                MaxReserveAmmo = _maxReserveAmmo
            };
        }

        private void OnValidate()
        {
            _damage = Mathf.Max(1, _damage);
            _fireInterval = Mathf.Max(0.01f, _fireInterval);
            _projectilesPerShot = Mathf.Max(1, _projectilesPerShot);
            _spreadAngle = Mathf.Clamp(_spreadAngle, 0f, 45f);
            _spreadPerShot = Mathf.Max(0f, _spreadPerShot);
            _maxSpreadAngle = Mathf.Max(_spreadAngle, _maxSpreadAngle);
            _spreadRecoveryPerSecond = Mathf.Max(0f, _spreadRecoveryPerSecond);
            _burstCount = Mathf.Max(2, _burstCount);
            _burstInterval = Mathf.Max(0.01f, _burstInterval);
            _magazineSize = Mathf.Max(1, _magazineSize);
            _startingReserveAmmo = Mathf.Max(0, _startingReserveAmmo);
            _maxReserveAmmo = Mathf.Max(_startingReserveAmmo, _maxReserveAmmo);
            _reloadDuration = Mathf.Max(0f, _reloadDuration);
            _projectileSpeed = Mathf.Max(0.01f, _projectileSpeed);
            _projectileLifetime = Mathf.Max(0.01f, _projectileLifetime);
            _hitscanRange = Mathf.Max(0.1f, _hitscanRange);
            _maxPenetrations = Mathf.Max(0, _maxPenetrations);
            _defaultPoolSize = Mathf.Max(1, _defaultPoolSize);
            _maxPoolSize = Mathf.Max(_defaultPoolSize, _maxPoolSize);
        }
    }
}
