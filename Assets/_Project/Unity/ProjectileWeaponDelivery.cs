using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Game.Flow;

namespace Game
{
    public class ProjectileWeaponDelivery : IWeaponDelivery, IWeaponDeliveryAdmission
    {
        private readonly Projectile _prefab;
        private int _projectilesPerShot;
        private float _speedOverride;
        private float _lifetimeOverride;
        private readonly ObjectPool<Projectile> _pool;
        private readonly List<Projectile> _activeProjectiles = new List<Projectile>();
        private bool _isDisposed;
        private readonly Scene _scene;
        private readonly List<SceneObjectBudget> _reservedBudgets = new List<SceneObjectBudget>();
        private readonly Dictionary<Projectile, SceneObjectBudget> _activeBudgets = new Dictionary<Projectile, SceneObjectBudget>();
        private int _reservedShotPelletCount;

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
            float lifetimeOverride = -1f,
            Scene scene = default)
        {
            _prefab = prefab;
            _projectilesPerShot = Mathf.Max(1, projectilesPerShot);
            _speedOverride = speedOverride;
            _lifetimeOverride = lifetimeOverride;
            _scene = scene.IsValid() ? scene : SceneManager.GetActiveScene();

            int minSize = Mathf.Max(1, defaultPoolSize);
            int maxSize = Mathf.Max(minSize, maxPoolSize);

            if (_prefab == null)
            {
#if UNITY_EDITOR
                _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Projectile>("Assets/_Project/Projectile.prefab");
#endif
            }

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
                CancelReservedShot();
                return;
            }

            int pelletCount = _reservedShotPelletCount > 0 ? _reservedShotPelletCount : _projectilesPerShot;
            while (_reservedBudgets.Count < pelletCount) {
                SceneObjectBudget budget = SceneObjectBudget.FindForScene(spawnPoint.gameObject.scene);
                if (budget != null && !budget.TryReserveProjectile()) { CancelReservedShot(); return; }
                _reservedBudgets.Add(budget);
            }

            for (int i = 0; i < pelletCount; i++)
            {
                SceneObjectBudget budget = _reservedBudgets[0];
                _reservedBudgets.RemoveAt(0);
                Projectile projectile = _pool.Get();
                if (budget != null) _activeBudgets[projectile] = budget;
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
            _reservedShotPelletCount = 0;
        }

        public bool TryReserveShot(Transform spawnPoint)
        {
            CancelReservedShot();
            if (_pool == null || spawnPoint == null || _isDisposed) return false;
            SceneObjectBudget budget = spawnPoint != null ? SceneObjectBudget.FindForScene(spawnPoint.gameObject.scene) : null;
            _reservedShotPelletCount = _projectilesPerShot;
            for (int i = 0; i < _projectilesPerShot; i++)
            {
                if (budget != null && !budget.TryReserveProjectile()) { CancelReservedShot(); return false; }
                _reservedBudgets.Add(budget);
            }
            return true;
        }

        public void CancelReservedShot()
        {
            for (int i = 0; i < _reservedBudgets.Count; i++)
                if (_reservedBudgets[i] != null) _reservedBudgets[i].ReleaseProjectile();
            _reservedBudgets.Clear();
            _reservedShotPelletCount = 0;
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
            CancelReservedShot();
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
            Projectile instance;
            if (_prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(_prefab);
            }
            else
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Fallback_Projectile";
                go.transform.localScale = new Vector3(0.15f, 0.15f, 0.35f);
                Collider col = go.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
                instance = go.AddComponent<Projectile>();
            }

            if (_scene.IsValid() && _scene.isLoaded && instance.gameObject.scene != _scene)
                SceneManager.MoveGameObjectToScene(instance.gameObject, _scene);
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
            ReleaseBudget(projectile);
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyProjectile(Projectile projectile)
        {
            _activeProjectiles.Remove(projectile);
            ReleaseBudget(projectile);
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

        private void ReleaseBudget(Projectile projectile)
        {
            if (projectile != null && _activeBudgets.TryGetValue(projectile, out SceneObjectBudget budget))
            {
                _activeBudgets.Remove(projectile);
                if (budget != null) budget.ReleaseProjectile();
            }
        }
    }
}
