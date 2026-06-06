using System;
using Application;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class Gun : MonoBehaviour, IGunEvents
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _fireRate = 0.2f;
        [SerializeField] private int _defaultPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 20;

        private ObjectPool<Projectile> _pool;
        private float _lastShootTime;

        public event Action Fired;

        private void Awake()
        {
            _pool = new ObjectPool<Projectile>(CreateProjectile, OnGetProjectile, OnReleaseProjectile, OnDestroyProjectile, true, _defaultPoolSize, _maxPoolSize);
        }

        private void Start()
        {
            PrewarmPool();
        }

        public void Shoot(Vector3 direction)
        {
            if (Time.time < _lastShootTime + _fireRate)
            {
                return;
            }

            _lastShootTime = Time.time;

            Projectile projectile = _pool.Get();
            projectile.transform.position = _spawnPoint.position;
            projectile.Initialize(direction, _pool.Release, _damage);

            Fired?.Invoke();
        }

        private void PrewarmPool()
        {
            Projectile[] projectiles = new Projectile[_defaultPoolSize];

            for (int i = 0; i < _defaultPoolSize; i++)
            {
                projectiles[i] = _pool.Get();
            }

            for (int i = 0; i < _defaultPoolSize; i++)
            {
                _pool.Release(projectiles[i]);
            }
        }

        private Projectile CreateProjectile()
        {
            return Instantiate(_prefab, _spawnPoint.position, Quaternion.identity);
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
    }
}
