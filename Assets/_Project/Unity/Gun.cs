using System;
using Application;
using Core;
using UnityEngine;

namespace Game
{
    public class Gun : MonoBehaviour, IGunEvents, IWeaponReadModel
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
        public int AmmoInMagazine => _runtime != null ? _runtime.Ammo.InMagazine : 0;
        public int ReserveAmmo => _runtime != null ? _runtime.Ammo.ReserveAmmo : 0;
        public int MagazineSize => _runtime != null ? _runtime.Ammo.MagazineSize : 0;
        public bool InfiniteAmmo => _runtime == null || _runtime.Ammo.InfiniteAmmo;
        public bool IsReloading => _runtime != null && _runtime.IsReloading;
        public float ReloadProgress => _runtime != null ? _runtime.ReloadProgress(Time.time) : 0f;
        public bool IsEquipped => _isEquipped;
        public WeaponRuntime Runtime => _runtime;

        private int Damage => _definition != null ? _definition.Damage : Mathf.Max(1, _damage);
        private WeaponDeliveryMode DeliveryMode => _definition != null ? _definition.DeliveryMode : WeaponDeliveryMode.Projectile;

        private void Awake()
        {
            _damageSourceRoot = transform.root;
            _damageAffiliation = DamageAffiliation.Find(this);
            _sourceTeam = _damageAffiliation != null && _damageAffiliation.Team != CombatTeam.Neutral
                ? _damageAffiliation.Team
                : DamageAffiliation.ResolveTeam(this);

            _gameStateRef = FindFirstObjectByType<GameStateController>();
            _gameState = _gameStateRef;

            InitializeRuntime();
            InitializeDelivery();
        }

        private void Start()
        {
            SetVisualState(_isEquipped);
            RaiseAmmoChanged();
        }

        private void Update()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.TickReload(Time.time);

            if (_runtime.TickBurst(Time.time))
            {
                DeliverRound(_aimDirection);
            }

            _runtime.RecoverSpread(Time.deltaTime);
        }

        private void OnDestroy()
        {
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

            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return;
            }

            _aimDirection = direction.normalized;

            if (_runtime == null)
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
                    if (wasPressed && _runtime.TryFire(Time.time))
                    {
                        DeliverRound(_aimDirection);
                    }
                    break;

                case Application.WeaponFireMode.Burst:
                    if (wasPressed && _runtime.StartBurst(Time.time))
                    {
                        if (_runtime.TickBurst(Time.time))
                        {
                            DeliverRound(_aimDirection);
                        }
                    }
                    break;

                default: // Automatic
                    if ((isHeld || wasPressed) && _runtime.TryFire(Time.time))
                    {
                        DeliverRound(_aimDirection);
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
            if (_runtime == null)
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
            _delivery = delivery;
            _spawnPoint = spawnPoint;
            _isEquipped = true;
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

            if (DeliveryMode == WeaponDeliveryMode.Hitscan && _definition != null)
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
                    lifetime);
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
