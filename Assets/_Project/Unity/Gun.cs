using System;
using Application;
using Application.Weapons;
using Application.Workshop;
using Core;
using UnityEngine;

namespace Game
{
    public class Gun : MonoBehaviour, IGunEvents, IWeaponReadModel, IWeaponBuildTarget
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

        private WeaponRuntime _runtime;
        private IWeaponDelivery _delivery;
        private bool _isEquipped = true;
        private Vector3 _aimDirection = Vector3.forward;
        private DamageAffiliation _damageAffiliation;
        private Transform _damageSourceRoot;
        private CombatTeam _sourceTeam;
        private GameStateController _gameStateRef;
        private IGameStateProvider _gameState;

        public event Action Fired;
        public event Action EmptyFired;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public event Action ReloadCanceled;
        public event Action Equipped;
        public event Action Unequipped;
        public event Action<int, int> AmmoChanged;

        public WeaponDefinition Definition => _definition;
        public string WeaponId => _definition != null ? _definition.WeaponId : WeaponWorkshopIds.Rifle;
        public WeaponBuild CurrentBuild { get; private set; }
        public ResolvedWeaponStats CurrentStats { get; private set; }
        public WeaponRuntime Runtime
        {
            get
            {
                if (_runtime == null)
                {
                    InitializeRuntime();
                }
                return _runtime;
            }
        }
        public int AmmoInMagazine => Runtime != null ? Runtime.Ammo.InMagazine : 0;
        public int ReserveAmmo => Runtime != null ? Runtime.Ammo.ReserveAmmo : 0;
        public int MagazineSize => Runtime != null ? Runtime.Ammo.MagazineSize : 0;
        public bool InfiniteAmmo => Runtime == null || Runtime.Ammo.InfiniteAmmo;
        public bool IsReloading => Runtime != null && Runtime.IsReloading;
        public float ReloadProgress => Runtime != null ? Runtime.ReloadProgress(Time.time) : 0f;
        public bool IsEquipped => _isEquipped;
        public IWeaponDelivery Delivery => _delivery;
        public Transform SpawnPoint => _spawnPoint;
        public void SetSpawnPoint(Transform spawnPoint) => _spawnPoint = spawnPoint;

        public int Damage => CurrentStats != null
            ? (int)Mathf.Round(CurrentStats.Damage)
            : (_definition != null ? _definition.Damage : Mathf.Max(1, _damage));

        public Application.WeaponDeliveryMode DeliveryMode => CurrentStats != null
            ? CurrentStats.DeliveryMode
            : (_definition != null ? (Application.WeaponDeliveryMode)_definition.DeliveryMode : Application.WeaponDeliveryMode.Projectile);

        private void Awake()
        {
            _damageSourceRoot = transform.root;
            _damageAffiliation = DamageAffiliation.Find(this);
            _sourceTeam = _damageAffiliation != null && _damageAffiliation.Team != CombatTeam.Neutral
                ? _damageAffiliation.Team
                : DamageAffiliation.ResolveTeam(this);

            _gameStateRef = FindFirstObjectByType<GameStateController>();
            _gameState = _gameStateRef;

            if (_runtime == null)
            {
                InitializeRuntime();
            }
            if (_delivery == null)
            {
                InitializeDelivery();
            }
        }

        private void Start()
        {
            SetVisualState(_isEquipped);
            RaiseAmmoChanged();
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }
            CancelBurst();
            CancelReload();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.Playing)
            {
                // Explicit pause/resume behavior: cancel pending burst so no unintended rounds discharge while paused or upon unpausing
                CancelBurst();

                if (newState == GameState.GameOver || newState == GameState.Victory || newState == GameState.Menu)
                {
                    CancelReload();
                }
            }
        }

        private void Update()
        {
            if (_runtime == null || !_isEquipped)
            {
                return;
            }

            if (_gameState == null && _gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
                _gameState = _gameStateRef;
                if (_gameState != null && enabled)
                {
                    _gameState.OnStateChanged += HandleGameStateChanged;
                }
            }

            if (_gameState != null && !GameActivityPolicy.CanFire(_gameState.CurrentState))
            {
                return;
            }

            _runtime.TickReload(Time.time);

            bool reserved = TryReserveDeliveryShot();
            if (reserved && _runtime.TickBurst(Time.time))
            {
                DeliverRound(_aimDirection);
            }
            else if (reserved) CancelReservedDeliveryShot();

            _runtime.RecoverSpread(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }
            _delivery?.Dispose();
        }

        public void HandleTrigger(Vector3 direction, bool isHeld, bool wasPressed)
        {
            if (!_isEquipped || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (_gameState == null && _gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
                _gameState = _gameStateRef;
            }

            if (_gameState != null && !GameActivityPolicy.CanFire(_gameState.CurrentState))
            {
                return;
            }

            _aimDirection = direction.normalized;

            if (Runtime == null)
            {
                return;
            }

            if (_runtime.IsReloading && wasPressed && _runtime.Config.CancelReloadOnFire && _runtime.Ammo.CanFire)
            {
                _runtime.CancelReload();
            }

            switch (_runtime.Config.FireMode)
            {
                case Application.WeaponFireMode.SemiAutomatic:
                case Application.WeaponFireMode.Shotgun:
                    if (wasPressed && TryReserveDeliveryShot())
                    {
                        if (_runtime.TryFire(Time.time)) DeliverRound(_aimDirection);
                        else CancelReservedDeliveryShot();
                    }
                    break;

                case Application.WeaponFireMode.Burst:
                    if (wasPressed && _runtime.StartBurst(Time.time))
                    {
                        bool reserved = TryReserveDeliveryShot();
                        if (reserved && _runtime.TickBurst(Time.time))
                        {
                            DeliverRound(_aimDirection);
                        }
                        else if (reserved) CancelReservedDeliveryShot();
                    }
                    break;

                default: // Automatic
                    if ((isHeld || wasPressed) && TryReserveDeliveryShot())
                    {
                        if (_runtime.TryFire(Time.time)) DeliverRound(_aimDirection);
                        else CancelReservedDeliveryShot();
                    }
                    break;
            }
        }

        public void Shoot(Vector3 direction)
        {
            HandleTrigger(direction, isHeld: true, wasPressed: true);
        }

        public void CancelBurst()
        {
            _runtime?.CancelBurst();
        }

        public bool Reload()
        {
            if (_gameState == null && _gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
                _gameState = _gameStateRef;
            }

            if (_runtime == null || (_gameState != null && !GameActivityPolicy.CanFire(_gameState.CurrentState)))
            {
                return false;
            }

            return _runtime.StartReload(Time.time);
        }

        public void CancelReload()
        {
            _runtime?.CancelReload();
        }

        public int AddAmmo(int amount)
        {
            if (_runtime == null)
            {
                return 0;
            }

            return _runtime.AddReserveAmmo(amount);
        }

        public void SetEquipped(bool equipped)
        {
            if (_isEquipped == equipped)
            {
                SetVisualState(equipped);
                return;
            }

            _isEquipped = equipped;

            if (!equipped)
            {
                CancelBurst();
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

        public void ConfigureForTesting(WeaponRuntime runtime, IWeaponDelivery delivery, Transform spawnPoint)
        {
            _runtime = runtime;
            if (_runtime != null)
            {
                _runtime.Fired += () => Fired?.Invoke();
                _runtime.EmptyFired += () => EmptyFired?.Invoke();
                _runtime.ReloadStarted += () => ReloadStarted?.Invoke();
                _runtime.ReloadCompleted += () => ReloadCompleted?.Invoke();
                _runtime.ReloadCanceled += () => ReloadCanceled?.Invoke();
                _runtime.AmmoChanged += (mag, res) => AmmoChanged?.Invoke(mag, res);
            }
            _delivery = delivery;
            _spawnPoint = spawnPoint;
            _isEquipped = true;
        }

        public ApplyResult TryApply(WeaponBuild build, ResolvedWeaponStats stats)
        {
            if (build == null)
            {
                return ApplyResult.Failure("NullBuild", "Build cannot be null.");
            }

            if (stats == null)
            {
                return ApplyResult.Failure("NullStats", "Resolved weapon stats cannot be null.");
            }

            if (!string.Equals(build.WeaponId, WeaponId, StringComparison.Ordinal))
            {
                return ApplyResult.Failure("WeaponMismatch", $"Build weapon ID '{build.WeaponId}' does not match target weapon ID '{WeaponId}'.");
            }

            if (_runtime != null)
            {
                _runtime.CancelBurst();
                _runtime.CancelReload();
            }

            var newConfig = new WeaponRuntimeConfig
            {
                FireMode = stats.FireMode,
                FireInterval = stats.FireInterval,
                BurstCount = stats.BurstCount,
                BurstInterval = stats.BurstInterval,
                BaseSpreadAngle = stats.BaseSpreadAngle,
                MaxSpreadAngle = stats.MaxSpreadAngle,
                SpreadPerShot = stats.RecoilPerShot,
                SpreadRecoveryPerSecond = stats.SpreadRecoveryRate,
                ReloadDuration = stats.ReloadDuration,
                MagazineSize = stats.MagazineCapacity,
                MaxReserveAmmo = stats.MaxReserveAmmo,
                StartingReserveAmmo = stats.StartingReserveAmmo,
                InfiniteAmmo = stats.InfiniteAmmo,
                AutoReloadOnEmpty = stats.AutoReloadOnEmpty,
                CancelReloadOnFire = stats.CancelReloadOnFire
            };

            if (_runtime == null)
            {
                _runtime = new WeaponRuntime(newConfig);
                _runtime.Fired += () => Fired?.Invoke();
                _runtime.EmptyFired += () => EmptyFired?.Invoke();
                _runtime.ReloadStarted += () => ReloadStarted?.Invoke();
                _runtime.ReloadCompleted += () => ReloadCompleted?.Invoke();
                _runtime.ReloadCanceled += () => ReloadCanceled?.Invoke();
                _runtime.AmmoChanged += (mag, res) => AmmoChanged?.Invoke(mag, res);
            }
            else
            {
                _runtime.ApplyConfig(newConfig);
            }

            ApplyDeliveryParameters(stats);

            PlayerRotation playerRotation = GetComponentInParent<PlayerRotation>();
            if (playerRotation == null)
            {
                playerRotation = GetComponent<PlayerRotation>();
            }
            if (playerRotation != null && stats.AimTurnSpeed > 0f)
            {
                playerRotation.SetTurnSpeed(stats.AimTurnSpeed);
            }

            CurrentBuild = build;
            CurrentStats = stats;

            RaiseAmmoChanged();

            return ApplyResult.Success();
        }

        private void ApplyDeliveryParameters(ResolvedWeaponStats stats)
        {
            if (stats.DeliveryMode == Application.WeaponDeliveryMode.Hitscan)
            {
                if (_delivery is HitscanWeaponDelivery hitscan)
                {
                    hitscan.UpdateParameters(stats.PelletCount, stats.Range, CreateFalloffEvaluator(stats));
                }
                else if (_delivery == null || _delivery is ProjectileWeaponDelivery)
                {
                    _delivery?.Dispose();
                    LayerMask mask = _definition != null ? _definition.HitscanMask : (LayerMask)(~0);
                    int penetrations = _definition != null ? _definition.MaxPenetrations : 0;
                    _delivery = new HitscanWeaponDelivery(
                        stats.Range,
                        mask,
                        penetrations,
                        stats.PelletCount,
                        CreateFalloffEvaluator(stats),
                        null);
                }
            }
            else
            {
                if (_delivery is ProjectileWeaponDelivery projectile)
                {
                    projectile.UpdateParameters(stats.PelletCount, stats.ProjectileSpeed, stats.ProjectileLifetime);
                }
                else if (_delivery == null || _delivery is HitscanWeaponDelivery)
                {
                    _delivery?.Dispose();
                    Projectile prefab = _definition != null && _definition.ProjectilePrefab != null
                        ? _definition.ProjectilePrefab
                        : _prefab;
                    int defaultPool = _definition != null ? _definition.DefaultPoolSize : _defaultPoolSize;
                    int maxPool = _definition != null ? _definition.MaxPoolSize : _maxPoolSize;

                    _delivery = new ProjectileWeaponDelivery(
                        prefab,
                        defaultPool,
                        maxPool,
                        stats.PelletCount,
                        stats.ProjectileSpeed,
                        stats.ProjectileLifetime,
                        gameObject.scene);
                }
            }
        }

        public static Func<float, float> CreateFalloffEvaluator(ResolvedWeaponStats stats)
        {
            if (stats == null || stats.DamageFalloffEnd <= stats.DamageFalloffStart || stats.DamageFalloffEnd <= 0f)
            {
                return _ => 1f;
            }

            float start = stats.DamageFalloffStart;
            float end = stats.DamageFalloffEnd;
            float minRatio = stats.MinDamageRatio;

            return distance =>
            {
                if (distance <= start) return 1f;
                if (distance >= end) return minRatio;
                float t = Mathf.Clamp01((distance - start) / (end - start));
                return Mathf.Lerp(1f, minRatio, t);
            };
        }

        public void ResetRuntimeState()
        {
            _runtime?.CancelBurst();
            _runtime?.Reset();
            _delivery?.ClearActiveProjectiles();
            RaiseAmmoChanged();
        }

        private void InitializeRuntime()
        {
            WeaponRuntimeConfig config = _definition != null
                ? _definition.CreateRuntimeConfig()
                : new WeaponRuntimeConfig
                {
                    FireMode = Application.WeaponFireMode.Automatic,
                    FireInterval = Mathf.Max(0.01f, _fireRate),
                    BaseSpreadAngle = 0f,
                    MaxSpreadAngle = 20f,
                    SpreadPerShot = 0f,
                    SpreadRecoveryPerSecond = 12f,
                    ReloadDuration = 1.2f,
                    AutoReloadOnEmpty = true,
                    CancelReloadOnFire = true,
                    InfiniteAmmo = true,
                    MagazineSize = 1,
                    StartingReserveAmmo = 0,
                    MaxReserveAmmo = 0
                };

            _runtime = new WeaponRuntime(config);
            _runtime.Fired += () => Fired?.Invoke();
            _runtime.EmptyFired += () => EmptyFired?.Invoke();
            _runtime.ReloadStarted += () => ReloadStarted?.Invoke();
            _runtime.ReloadCompleted += () => ReloadCompleted?.Invoke();
            _runtime.ReloadCanceled += () => ReloadCanceled?.Invoke();
            _runtime.AmmoChanged += (mag, res) => AmmoChanged?.Invoke(mag, res);
        }

        private void InitializeDelivery()
        {
            _delivery?.Dispose();

            if (DeliveryMode == Application.WeaponDeliveryMode.Hitscan && _definition != null)
            {
                _delivery = new HitscanWeaponDelivery(
                    _definition.HitscanRange,
                    _definition.HitscanMask,
                    _definition.MaxPenetrations,
                    _definition.ProjectilesPerShot,
                    _definition.EvaluateDamageMultiplier,
                    null);
            }
            else
            {
                Projectile prefab = _definition != null && _definition.ProjectilePrefab != null
                    ? _definition.ProjectilePrefab
                    : _prefab;

                int defaultPool = _definition != null ? _definition.DefaultPoolSize : _defaultPoolSize;
                int maxPool = _definition != null ? _definition.MaxPoolSize : _maxPoolSize;
                int count = _definition != null ? _definition.ProjectilesPerShot : 1;
                float speed = _definition != null && _definition.OverrideProjectileMotion ? _definition.ProjectileSpeed : -1f;
                float lifetime = _definition != null && _definition.OverrideProjectileMotion ? _definition.ProjectileLifetime : -1f;

                _delivery = new ProjectileWeaponDelivery(
                    prefab,
                    defaultPool,
                    maxPool,
                    count,
                    speed,
                    lifetime,
                    gameObject.scene);
            }
        }

        private void DeliverRound(Vector3 direction)
        {
            if (_delivery == null || _spawnPoint == null)
            {
                return;
            }

            _delivery.Deliver(
                _spawnPoint,
                direction,
                Damage,
                _damageAffiliation,
                _damageSourceRoot,
                _effectPool,
                _runtime != null ? _runtime.CurrentSpreadAngle : 0f);
        }

        private bool TryReserveDeliveryShot()
        {
            return !(_delivery is IWeaponDeliveryAdmission admission) || admission.TryReserveShot(_spawnPoint);
        }

        private void CancelReservedDeliveryShot()
        {
            if (_delivery is IWeaponDeliveryAdmission admission) admission.CancelReservedShot();
        }

        private void RaiseAmmoChanged()
        {
            if (_runtime != null)
            {
                AmmoChanged?.Invoke(_runtime.Ammo.InMagazine, _runtime.Ammo.ReserveAmmo);
            }
        }

        private void SetVisualState(bool equipped)
        {
            if (_equippedVisualRoot != null)
            {
                _equippedVisualRoot.SetActive(equipped);
            }
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
