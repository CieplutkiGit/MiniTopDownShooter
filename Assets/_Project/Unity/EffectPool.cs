using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Game.Flow;

namespace Game
{
    public class EffectPool : MonoBehaviour
    {
        [SerializeField] private int _defaultSize = 4;
        [SerializeField] private int _maxSize = 32;

        private readonly Dictionary<ParticleSystem, Pool> _pools = new Dictionary<ParticleSystem, Pool>();

        public void Play(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }

            Pool pool = GetPool(prefab);
            SceneObjectBudget budget = SceneObjectBudget.FindForScene(gameObject.scene);
            if (budget != null && !budget.TryReserveEffect()) return;
            ParticleBurst burst = pool.Get(budget);
            burst.PlayAt(position);
        }

        private Pool GetPool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool existing))
            {
                return existing;
            }

            Pool created = new Pool(prefab, _defaultSize, _maxSize, gameObject.scene);
            _pools[prefab] = created;
            return created;
        }

        public void Prewarm(ParticleSystem prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Pool pool = GetPool(prefab);
            pool.Prewarm(count);
        }

        public void ClearAllActive()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.ClearActive();
            }
        }

        private void OnDestroy()
        {
            ClearAllActive();
            foreach (Pool pool in _pools.Values) pool.Dispose();
            _pools.Clear();
        }

        private class Pool
        {
            private readonly ParticleBurst _prefab;
            private readonly Scene _scene;
            private readonly ObjectPool<ParticleBurst> _pool;
            private readonly List<ParticleBurst> _active = new List<ParticleBurst>();
            private readonly Dictionary<ParticleBurst, SceneObjectBudget> _budgets = new Dictionary<ParticleBurst, SceneObjectBudget>();
            private readonly int _maxSize;

            public Pool(ParticleSystem prefab, int defaultSize, int maxSize, Scene scene)
            {
                _prefab = prefab.GetComponent<ParticleBurst>();
                _scene = scene;
                _maxSize = maxSize;
                _pool = new ObjectPool<ParticleBurst>(Create, OnGet, OnRelease, OnDestroyBurst, true, defaultSize, maxSize);
            }

            public void Prewarm(int count)
            {
                int target = Mathf.Clamp(count, 0, _maxSize);
                if (target <= 0) return;
                List<ParticleBurst> spawned = new List<ParticleBurst>(target);
                for (int i = 0; i < target; i++)
                {
                    spawned.Add(_pool.Get());
                }
                for (int i = 0; i < spawned.Count; i++)
                {
                    if (spawned[i] != null)
                    {
                        _pool.Release(spawned[i]);
                    }
                }
            }

            public ParticleBurst Get(SceneObjectBudget budget)
            {
                ParticleBurst burst = _pool.Get();
                if (budget != null && !ReferenceEquals(burst, null)) _budgets[burst] = budget;
                return burst;
            }

            public void ClearActive()
            {
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    ParticleBurst b = _active[i];
                    if (b != null)
                    {
                        b.Stop();
                        _pool.Release(b);
                    }
                    else
                    {
                        ReleaseBudget(b);
                    }
                }
                _active.Clear();
            }

            public void Dispose() => _pool.Dispose();

            private ParticleBurst Create()
            {
                ParticleBurst burst = Object.Instantiate(_prefab);
                if (burst != null)
                {
                    if (_scene.IsValid() && _scene.isLoaded && burst.gameObject.scene != _scene)
                        SceneManager.MoveGameObjectToScene(burst.gameObject, _scene);
                    burst.SetReturnCallback(Release);
                }
                return burst;
            }

            private void Release(ParticleBurst burst)
            {
                _active.Remove(burst);
                if (burst != null)
                {
                    _pool.Release(burst);
                }
                else
                {
                    ReleaseBudget(burst);
                }
            }

            private void OnGet(ParticleBurst burst)
            {
                if (burst == null) return;
                _active.Add(burst);
                if (burst.gameObject != null)
                {
                    burst.gameObject.SetActive(true);
                }
            }

            private void OnRelease(ParticleBurst burst)
            {
                _active.Remove(burst);
                ReleaseBudget(burst);
                if (burst != null && burst.gameObject != null)
                {
                    burst.gameObject.SetActive(false);
                }
            }

            private void OnDestroyBurst(ParticleBurst burst)
            {
                _active.Remove(burst);
                ReleaseBudget(burst);
                if (burst != null && burst.gameObject != null)
                {
                    Object.Destroy(burst.gameObject);
                }
            }

            private void ReleaseBudget(ParticleBurst burst)
            {
                if (!ReferenceEquals(burst, null) && _budgets.TryGetValue(burst, out SceneObjectBudget budget))
                {
                    _budgets.Remove(burst);
                    if (budget != null) budget.ReleaseEffect();
                }
            }
        }
    }
}
