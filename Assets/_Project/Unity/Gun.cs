using System;
using Application;
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

        [Header("Legacy Defaults")]
        [Tooltip("Used when no Weapon Definition is assigned.")]
        [SerializeField] private Projectile _prefab;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _fireRate = 0.2f;
        [SerializeField] private int _defaultPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 20;

        private ObjectPool<Projectile> _pool;
        private float _lastShootTime;

        public event Action Fired;
        public WeaponDefinition Definition => _definition;

        private Projectile ProjectilePrefab =>
            _definition != null && _definition.ProjectilePrefab != null
                ? _definition.ProjectilePrefab
                : _prefab;

        private int Damage => _definition != null ? _definition.Damage : Mathf.Max(1, _damage);
        private float FireInterval => _definition != null ? _definition.FireInterval : Mathf.Max(0.01f, _fireRate);
        private int ProjectilesPerShot => _definition != null ? _definition.ProjectilesPerShot : 1;
        private float SpreadAngle => _definition != null ? _definition.SpreadAngle : 0f;
        private int DefaultPoolSize => _definition != null ? _definition.DefaultPoolSize : Mathf.Max(1, _defaultPoolSize);
        private int MaxPoolSize => _definition != null ? _definition.MaxPoolSize : Mathf.Max(DefaultPoolSize, _maxPoolSize);

        private void Awake()
        {
            if (ProjectilePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(Gun)} on '{name}' has no projectile prefab. Assign a Weapon Definition or legacy projectile prefab.",
                    this);
                return;
            }

            if (_spawnPoint == null)
            {
                Debug.LogError($"{nameof(Gun)} on '{name}' has no spawn point.", this);
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

        private void Start()
        {
            PrewarmPool();
        }

        public void Shoot(Vector3 direction)
        {
            if (_pool == null || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (Time.time < _lastShootTime + FireInterval)
            {
                return;
            }

            _lastShootTime = Time.time;
            Vector3 normalizedDirection = direction.normalized;

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                Projectile projectile = _pool.Get();
                projectile.transform.position = _spawnPoint.position;
                projectile.transform.rotation = _spawnPoint.rotation;
                projectile.Initialize(ApplySpread(normalizedDirection), _pool.Release, Damage, _effectPool);
            }

            Fired?.Invoke();
        }

        private Vector3 ApplySpread(Vector3 direction)
        {
            if (SpreadAngle <= 0f)
            {
                return direction;
            }

            float angle = UnityEngine.Random.Range(-SpreadAngle, SpreadAngle);
            return Quaternion.AngleAxis(angle, Vector3.up) * direction;
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

        private void OnValidate()
        {
            _damage = Mathf.Max(1, _damage);
            _fireRate = Mathf.Max(0.01f, _fireRate);
            _defaultPoolSize = Mathf.Max(1, _defaultPoolSize);
            _maxPoolSize = Mathf.Max(_defaultPoolSize, _maxPoolSize);
        }
    }
}
