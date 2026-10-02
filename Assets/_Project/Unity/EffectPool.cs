using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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
            ParticleBurst burst = pool.Get();
            burst.PlayAt(position);
        }

        private Pool GetPool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out Pool existing))
            {
                return existing;
            }

            Pool created = new Pool(prefab, _defaultSize, _maxSize);
            _pools[prefab] = created;
            return created;
        }

        public void ClearAllActive()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.ClearActive();
            }
        }

        private class Pool
        {
            private readonly ParticleBurst _prefab;
            private readonly ObjectPool<ParticleBurst> _pool;
            private readonly List<ParticleBurst> _active = new List<ParticleBurst>();

            public Pool(ParticleSystem prefab, int defaultSize, int maxSize)
            {
                _prefab = prefab.GetComponent<ParticleBurst>();
                _pool = new ObjectPool<ParticleBurst>(Create, OnGet, OnRelease, OnDestroyBurst, true, defaultSize, maxSize);
            }

            public ParticleBurst Get()
            {
                return _pool.Get();
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
                }
                _active.Clear();
            }

            private ParticleBurst Create()
            {
                ParticleBurst burst = Object.Instantiate(_prefab);
                burst.SetReturnCallback(Release);
                return burst;
            }

            private void Release(ParticleBurst burst)
            {
                _active.Remove(burst);
                _pool.Release(burst);
            }

            private void OnGet(ParticleBurst burst)
            {
                _active.Add(burst);
                burst.gameObject.SetActive(true);
            }

            private void OnRelease(ParticleBurst burst)
            {
                _active.Remove(burst);
                burst.gameObject.SetActive(false);
            }

            private void OnDestroyBurst(ParticleBurst burst)
            {
                _active.Remove(burst);
                Object.Destroy(burst.gameObject);
            }
        }
    }
}
