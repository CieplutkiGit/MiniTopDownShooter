using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class ProjectileWeaponDelivery : IWeaponDelivery
    {
        private readonly Projectile _prefab;
        private readonly int _projectilesPerShot;
        private readonly float _speedOverride;
        private readonly float _lifetimeOverride;
        private readonly ObjectPool<Projectile> _pool;
        private readonly List<Projectile> _activeProjectiles = new List<Projectile>();
        private bool _isDisposed;

        public ProjectileWeaponDelivery(
            Projectile prefab,
            int defaultPoolSize,
            int maxPoolSize,
            int projectilesPerShot = 1,
            float speedOverride = -1f,
            float lifetimeOverride = -1f)
        {
            _prefab = prefab;
            _projectilesPerShot = Mathf.Max(1, projectilesPerShot);
            _speedOverride = speedOverride;
            _lifetimeOverride = lifetimeOverride;

            int minSize = Mathf.Max(1, defaultPoolSize);
            int maxSize = Mathf.Max(minSize, maxPoolSize);

            if (_prefab != null)
            {
                _pool = new ObjectPool<Projectile>(
                    CreateProjectile,
                    OnGetProjectile,
                    OnReleaseProjectile,
                    OnDestroyProjectile,
                    true,
                    minSize,
                    maxSize);

                PrewarmPool(minSize);
            }
        }

        public void Deliver(
            Transform spawnPoint,
            Vector3 direction,
            int damage,
            DamageAffiliation sourceAffiliation,
            Transform sourceRoot,
            EffectPool effectPool)
        {
            if (_pool == null || spawnPoint == null || _isDisposed)
            {
                return;
            }

            for (int i = 0; i < _projectilesPerShot; i++)
            {
                Projectile projectile = _pool.Get();
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = spawnPoint.rotation;

                projectile.Initialize(
                    direction,
                    ReleaseProjectile,
                    damage,
                    effectPool,
                    _speedOverride,
                    _lifetimeOverride,
                    sourceAffiliation,
                    sourceRoot);
            }
        }

        public void ClearActiveProjectiles()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _activeProjectiles[i];
                if (p != null)
                {
                    p.ForceReturnToPool();
                }
            }

            _activeProjectiles.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            ClearActiveProjectiles();

            if (_pool != null)
            {
                _pool.Clear();
                _pool.Dispose();
            }
        }

        private void PrewarmPool(int count)
        {
            if (_pool == null)
            {
                return;
            }

            Projectile[] temp = new Projectile[count];
            for (int i = 0; i < count; i++)
            {
                temp[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(temp[i]);
            }
        }

        private Projectile CreateProjectile()
        {
            Projectile instance = UnityEngine.Object.Instantiate(_prefab);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGetProjectile(Projectile projectile)
        {
            _activeProjectiles.Add(projectile);
            projectile.gameObject.SetActive(true);
        }

        private void OnReleaseProjectile(Projectile projectile)
        {
            _activeProjectiles.Remove(projectile);
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyProjectile(Projectile projectile)
        {
            _activeProjectiles.Remove(projectile);
            if (projectile != null)
            {
                UnityEngine.Object.Destroy(projectile.gameObject);
            }
        }

        private void ReleaseProjectile(Projectile projectile)
        {
            if (_pool != null && !_isDisposed && projectile != null)
            {
                _pool.Release(projectile);
            }
        }
    }
}
