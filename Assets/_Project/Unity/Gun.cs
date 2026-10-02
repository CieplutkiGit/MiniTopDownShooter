using System;
using System.Collections.Generic;
using Application;
using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class Gun : MonoBehaviour, IGunEvents
    {
        [Header("Configuration")]
        [Tooltip("Optional reusable weapon data. When assigned, it overrides the legacy values below.")]
        [SerializeField] private WeaponDefinition _definition;

        [Header("Scene References")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private EffectPool _effectPool;
        [Tooltip("Optional child root hidden while this weapon is unequipped.")]
        [SerializeField] private GameObject _equippedVisualRoot;

        [Header("Legacy Defaults")]
        [Tooltip("Used when no Weapon Definition is assigned.")]
        [SerializeField] private Projectile _prefab;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _fireRate = 0.2f;
        [SerializeField] private int _defaultPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 20;

        private ObjectPool<Projectile> _pool;
        private WeaponAmmoState _ammo;
        private float _lastShootTime = float.NegativeInfinity;
        private float _currentSpread;
        private bool _isReloading;
        private float _reloadCompleteTime;
        private bool _isEquipped = true;
        private int _burstShotsRemaining;
        private float _nextBurstShotTime;
        private Vector3 _burstDirection;
        private DamageAffiliation _damageAffiliation;
        private Transform _damageSourceRoot;
        private CombatTeam _sourceTeam;

        public event Action Fired;
        public event Action EmptyFired;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public event Action ReloadCanceled;
        public event Action Equipped;
        public event Action Unequipped;
        public event Action<int, int> AmmoChanged;

        public WeaponDefinition Definition => _definition;
        public int AmmoInMagazine => _ammo != null ? _ammo.InMagazine : 0;
        public int ReserveAmmo => _ammo != null ? _ammo.ReserveAmmo : 0;
        public int MagazineSize => _ammo != null ? _ammo.MagazineSize : 0;
        public bool InfiniteAmmo => _ammo == null || _ammo.InfiniteAmmo;
        public bool IsReloading => _isReloading;
        public bool IsEquipped => _isEquipped;

        private WeaponFireMode FireMode =>
            _definition != null ? _definition.FireMode : WeaponFireMode.Automatic;

        private WeaponDeliveryMode DeliveryMode =>
            _definition != null ? _definition.DeliveryMode : WeaponDeliveryMode.Projectile;

        private Projectile ProjectilePrefab =>
            _definition != null && _definition.ProjectilePrefab != null
                ? _definition.ProjectilePrefab
                : _prefab;

        private int Damage => _definition != null ? _definition.Damage : Mathf.Max(1, _damage);
        private float FireInterval => _definition != null ? _definition.FireInterval : Mathf.Max(0.01f, _fireRate);
        private int ProjectilesPerShot => _definition != null ? _definition.ProjectilesPerShot : 1;
        private float BaseSpreadAngle => _definition != null ? _definition.SpreadAngle : 0f;
        private int DefaultPoolSize => _definition != null ? _definition.DefaultPoolSize : Mathf.Max(1, _defaultPoolSize);
        private int MaxPoolSize => _definition != null ? _definition.MaxPoolSize : Mathf.Max(DefaultPoolSize, _maxPoolSize);

        private void Awake()
        {
            InitializeAmmoState();
            _damageSourceRoot = transform.root;
            _damageAffiliation = DamageAffiliation.Find(this);
            _sourceTeam =
                _damageAffiliation != null &&
                _damageAffiliation.Team != CombatTeam.Neutral
                    ? _damageAffiliation.Team
                    : DamageAffiliation.ResolveTeam(this);

            if (_spawnPoint == null)
            {
                Debug.LogError($"{nameof(Gun)} on '{name}' has no spawn point.", this);
                return;
            }

            if (DeliveryMode == WeaponDeliveryMode.Projectile)
            {
                if (ProjectilePrefab == null)
                {
                    Debug.LogError(
                        $"{nameof(Gun)} on '{name}' has no projectile prefab. Assign a Weapon Definition or legacy projectile prefab.",
                        this);
                    return;
                }

                _pool = new ObjectPool<Projectile>(
                    CreateProjectile,
                    OnGetProjectile,
                    OnReleaseProjectile,
                    OnDestroyProjectile,
                    true,
                    DefaultPoolSize,
                    MaxPoolSize);
            }
        }

        private void Start()
        {
            PrewarmPool();
            SetVisualState(_isEquipped);
            RaiseAmmoChanged();
        }

        private void Update()
        {
            TickReload();
            TickBurst();
            RecoverSpread();
        }

        public void HandleTrigger(Vector3 direction, bool isHeld, bool wasPressed)
        {
            if (!_isEquipped || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (_isReloading && wasPressed && CancelReloadOnFire && (_ammo == null || _ammo.CanFire))
            {
                CancelReload();
            }

            switch (FireMode)
            {
                case WeaponFireMode.SemiAutomatic:
                case WeaponFireMode.Shotgun:
                    if (wasPressed)
                    {
                        TryFire(direction);
                    }
                    break;

                case WeaponFireMode.Burst:
                    if (wasPressed)
                    {
                        StartBurst(direction);
                    }
                    break;

                default:
                    if (isHeld)
                    {
                        TryFire(direction);
                    }
                    break;
            }
        }

        public void Shoot(Vector3 direction)
        {
            TryFire(direction);
        }

        public bool Reload()
        {
            if (_definition == null || _ammo == null || _isReloading || !_ammo.CanReload)
            {
                return false;
            }

            _burstShotsRemaining = 0;
            _isReloading = true;
            _reloadCompleteTime = Time.time + _definition.ReloadDuration;
            ReloadStarted?.Invoke();

            if (_definition.ReloadDuration <= 0f)
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

        public int AddAmmo(int amount)
        {
            if (_ammo == null)
            {
                return 0;
            }

            int added = _ammo.AddReserve(amount);

            if (added > 0)
            {
                RaiseAmmoChanged();
            }

            return added;
        }

        public void SetEquipped(bool equipped)
        {
            if (_isEquipped == equipped)
            {
                SetVisualState(equipped);
                return;
            }

            _isEquipped = equipped;
            _burstShotsRemaining = 0;

            if (!equipped)
            {
                CancelReload();
            }

            SetVisualState(equipped);

            if (equipped)
            {
                Equipped?.Invoke();
                RaiseAmmoChanged();
            }
            else
            {
                Unequipped?.Invoke();
            }
        }

        public void ResetRuntimeState()
        {
            _burstShotsRemaining = 0;
            _isReloading = false;
            _currentSpread = 0f;
            _lastShootTime = float.NegativeInfinity;
            InitializeAmmoState();
            RaiseAmmoChanged();
        }

        private bool TryFire(Vector3 direction)
        {
            if (!_isEquipped || _spawnPoint == null || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            if (_isReloading)
            {
                return false;
            }

            if (Time.time < _lastShootTime + FireInterval)
            {
                return false;
            }

            return FireRound(direction.normalized, false);
        }

        private void StartBurst(Vector3 direction)
        {
            if (_definition == null || _burstShotsRemaining > 0 || _isReloading)
            {
                return;
            }

            if (Time.time < _lastShootTime + FireInterval)
            {
                return;
            }

            _burstDirection = direction.normalized;
            _burstShotsRemaining = _definition.BurstCount;
            _nextBurstShotTime = Time.time;
            TickBurst();
        }

        private void TickBurst()
        {
            if (_burstShotsRemaining <= 0 || _definition == null || FireMode != WeaponFireMode.Burst)
            {
                return;
            }

            if (Time.time < _nextBurstShotTime)
            {
                return;
            }

            if (!FireRound(_burstDirection, true))
            {
                _burstShotsRemaining = 0;
                return;
            }

            if (_burstShotsRemaining > 0)
            {
                _burstShotsRemaining--;
            }

            if (_burstShotsRemaining > 0)
            {
                _nextBurstShotTime = Time.time + _definition.BurstInterval;
            }
        }

        private bool FireRound(Vector3 direction, bool ignoreFireInterval)
        {
            if (_isReloading)
            {
                return false;
            }

            if (DeliveryMode == WeaponDeliveryMode.Projectile && _pool == null)
            {
                return false;
            }

            if (DeliveryMode == WeaponDeliveryMode.Hitscan && _definition == null)
            {
                return false;
            }

            if (!ignoreFireInterval && Time.time < _lastShootTime + FireInterval)
            {
                return false;
            }

            if (_ammo != null && !_ammo.TryConsumeRound())
            {
                EmptyFired?.Invoke();

                if (AutoReloadOnEmpty)
                {
                    Reload();
                }

                return false;
            }

            _lastShootTime = Time.time;

            if (DeliveryMode == WeaponDeliveryMode.Hitscan)
            {
                FireHitscan(direction);
            }
            else
            {
                FireProjectiles(direction);
            }

            if (_definition != null)
            {
                _currentSpread = Mathf.Min(
                    _definition.MaxSpreadAngle - BaseSpreadAngle,
                    _currentSpread + _definition.SpreadPerShot);
            }

            Fired?.Invoke();
            RaiseAmmoChanged();

            if (_ammo != null && !_ammo.InfiniteAmmo && _ammo.InMagazine <= 0 && AutoReloadOnEmpty)
            {
                Reload();
            }

            return true;
        }

        private void FireProjectiles(Vector3 direction)
        {
            if (_pool == null)
            {
                return;
            }

            int count = FireMode == WeaponFireMode.Shotgun
                ? Mathf.Max(1, ProjectilesPerShot)
                : Mathf.Max(1, ProjectilesPerShot);

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = _pool.Get();
                projectile.transform.position = _spawnPoint.position;
                projectile.transform.rotation = _spawnPoint.rotation;

                float speedOverride = -1f;
                float lifetimeOverride = -1f;

                if (_definition != null && _definition.OverrideProjectileMotion)
                {
                    speedOverride = _definition.ProjectileSpeed;
                    lifetimeOverride = _definition.ProjectileLifetime;
                }

                projectile.Initialize(
                    ApplySpread(direction),
                    _pool.Release,
                    Damage,
                    _effectPool,
                    speedOverride,
                    lifetimeOverride,
                    _damageAffiliation,
                    _damageSourceRoot);
            }
        }

        private void FireHitscan(Vector3 direction)
        {
            if (_definition == null)
            {
                return;
            }

            int rayCount = Mathf.Max(1, ProjectilesPerShot);

            for (int rayIndex = 0; rayIndex < rayCount; rayIndex++)
            {
                Vector3 shotDirection = ApplySpread(direction);
                RaycastHit[] hits = Physics.RaycastAll(
                    _spawnPoint.position,
                    shotDirection,
                    _definition.HitscanRange,
                    _definition.HitscanMask,
                    QueryTriggerInteraction.Ignore);

                Array.Sort(hits, CompareHitDistance);

                int remainingPenetrations = _definition.MaxPenetrations;
                HashSet<int> damagedTargets = new HashSet<int>();

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider collider = hits[i].collider;

                    if (collider == null ||
                        (_damageSourceRoot != null &&
                         collider.transform.root == _damageSourceRoot))
                    {
                        continue;
                    }

                    DamageAffiliation targetAffiliation =
                        DamageAffiliation.Find(collider);

                    CombatTeam targetTeam =
                        targetAffiliation != null &&
                        targetAffiliation.Team != CombatTeam.Neutral
                            ? targetAffiliation.Team
                            : DamageAffiliation.ResolveTeam(collider);

                    if (DamageAffiliation.ShouldIgnoreFriendlyCollision(
                            _damageAffiliation,
                            _sourceTeam,
                            targetAffiliation,
                            targetTeam))
                    {
                        continue;
                    }

                    if (!DamageAffiliation.TryGetDamageable(
                            collider,
                            out IDamageable damageable,
                            out Component owner))
                    {
                        break;
                    }

                    targetTeam =
                        targetAffiliation != null &&
                        targetAffiliation.Team != CombatTeam.Neutral
                            ? targetAffiliation.Team
                            : DamageAffiliation.ResolveTeam(owner);

                    if (!DamageAffiliation.CanDamage(
                            _damageAffiliation,
                            _sourceTeam,
                            targetAffiliation,
                            targetTeam))
                    {
                        break;
                    }

                    int targetId =
                        owner != null
                            ? owner.GetInstanceID()
                            : collider.GetInstanceID();

                    if (!damagedTargets.Add(targetId))
                    {
                        continue;
                    }

                    float multiplier =
                        _definition.EvaluateDamageMultiplier(hits[i].distance);

                    int resolvedDamage =
                        Mathf.RoundToInt(Damage * multiplier);

                    if (resolvedDamage > 0)
                    {
                        damageable.TakeDamage(
                            new DamageData(resolvedDamage));
                    }

                    if (remainingPenetrations <= 0)
                    {
                        break;
                    }

                    remainingPenetrations--;
                }
            }
        }

        private Vector3 ApplySpread(Vector3 direction)
        {
            float spread = BaseSpreadAngle + _currentSpread;

            if (spread <= 0f)
            {
                return direction;
            }

            float angle = UnityEngine.Random.Range(-spread, spread);
            return Quaternion.AngleAxis(angle, Vector3.up) * direction;
        }

        private void RecoverSpread()
        {
            if (_definition == null || _currentSpread <= 0f)
            {
                return;
            }

            _currentSpread = Mathf.MoveTowards(
                _currentSpread,
                0f,
                _definition.SpreadRecoveryPerSecond * Time.deltaTime);
        }

        private void TickReload()
        {
            if (_isReloading && Time.time >= _reloadCompleteTime)
            {
                CompleteReload();
            }
        }

        private void CompleteReload()
        {
            if (!_isReloading || _ammo == null)
            {
                return;
            }

            _isReloading = false;
            _ammo.Reload();
            ReloadCompleted?.Invoke();
            RaiseAmmoChanged();
        }

        private void InitializeAmmoState()
        {
            if (_definition == null)
            {
                _ammo = new WeaponAmmoState(1, 0, 0, true);
                return;
            }

            _ammo = new WeaponAmmoState(
                _definition.MagazineSize,
                _definition.StartingReserveAmmo,
                _definition.MaxReserveAmmo,
                _definition.InfiniteAmmo);
        }

        private bool AutoReloadOnEmpty => _definition != null && _definition.AutoReloadOnEmpty;
        private bool CancelReloadOnFire => _definition != null && _definition.CancelReloadOnFire;

        private void RaiseAmmoChanged()
        {
            if (_ammo != null)
            {
                AmmoChanged?.Invoke(_ammo.InMagazine, _ammo.ReserveAmmo);
            }
        }

        private void SetVisualState(bool equipped)
        {
            if (_equippedVisualRoot != null)
            {
                _equippedVisualRoot.SetActive(equipped);
            }
        }

        private void PrewarmPool()
        {
            if (_pool == null)
            {
                return;
            }

            Projectile[] projectiles = new Projectile[DefaultPoolSize];

            for (int i = 0; i < projectiles.Length; i++)
            {
                projectiles[i] = _pool.Get();
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                _pool.Release(projectiles[i]);
            }
        }

        private Projectile CreateProjectile()
        {
            return Instantiate(ProjectilePrefab, _spawnPoint.position, _spawnPoint.rotation);
        }

        private void OnGetProjectile(Projectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private void OnReleaseProjectile(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyProjectile(Projectile projectile)
        {
            Destroy(projectile.gameObject);
        }

        private static int CompareHitDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }

        private void OnValidate()
        {
            _damage = Mathf.Max(1, _damage);
            _fireRate = Mathf.Max(0.01f, _fireRate);
            _defaultPoolSize = Mathf.Max(1, _defaultPoolSize);
            _maxPoolSize = Mathf.Max(_defaultPoolSize, _maxPoolSize);
        }
    }
}
