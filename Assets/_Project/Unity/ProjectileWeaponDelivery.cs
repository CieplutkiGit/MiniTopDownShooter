using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class ProjectileWeaponDelivery : IWeaponDelivery
    {
        private readonly Projectile _prefab;
        private int _projectilesPerShot;
        private float _speedOverride;
        private float _lifetimeOverride;
        private readonly ObjectPool<Projectile> _pool;
        private readonly List<Projectile> _activeProjectiles = new List<Projectile>();
        private bool _isDisposed;

        public int ProjectilesPerShot => _projectilesPerShot;
        public float SpeedOverride => _speedOverride;
        public float LifetimeOverride => _lifetimeOverride;

        public void UpdateParameters(int pelletCount, float speedOverride, float lifetimeOverride)
        {
            _projectilesPerShot = Mathf.Max(1, pelletCount);
            _speedOverride = speedOverride;
            _lifetimeOverride = lifetimeOverride;
        }

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
            EffectPool effectPool,
            float spreadAngle = 0f)
        {
            if (_pool == null || spawnPoint == null || _isDisposed)
            {
                return;
            }

            for (int i = 0; i < _projectilesPerShot; i++)
            {
                Projectile projectile = _pool.Get();
                projectile.transform.position = spawnPoint.position;

                Vector3 pelletDir = spreadAngle > 0.001f
                    ? Quaternion.AngleAxis(UnityEngine.Random.Range(-spreadAngle, spreadAngle), Vector3.up) * direction
                    : direction;

                projectile.transform.rotation = Quaternion.LookRotation(pelletDir);

                projectile.Initialize(
                    pelletDir,
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

            // Return all active projectiles before setting _isDisposed
            ClearActiveProjectiles();

            // Destroy any lingering active projectiles to strictly guarantee zero orphans
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _activeProjectiles[i];
                if (p != null && p.gameObject != null)
                {
                    SafeDestroy(p.gameObject);
                }
            }
            _activeProjectiles.Clear();

            _isDisposed = true;

            if (_pool != null)
            {
                _pool.Clear();
                _pool.Dispose();
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
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
                SafeDestroy(projectile.gameObject);
            }
        }

        private void ReleaseProjectile(Projectile projectile)
        {
            if (_pool != null && !_isDisposed && projectile != null)
            {
                _pool.Release(projectile);
            }
            else if (projectile != null)
            {
                _activeProjectiles.Remove(projectile);
                if (projectile.gameObject != null)
                {
                    SafeDestroy(projectile.gameObject);
                }
            }
        }
    }
}
