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

        private class Pool
        {
            private readonly ParticleBurst _prefab;
            private readonly ObjectPool<ParticleBurst> _pool;

            public Pool(ParticleSystem prefab, int defaultSize, int maxSize)
            {
                _prefab = prefab.GetComponent<ParticleBurst>();
                _pool = new ObjectPool<ParticleBurst>(Create, OnGet, OnRelease, OnDestroyBurst, true, defaultSize, maxSize);
            }

            public ParticleBurst Get()
            {
                return _pool.Get();
            }

            private ParticleBurst Create()
            {
                ParticleBurst burst = Object.Instantiate(_prefab);
                burst.SetReturnCallback(Release);
                return burst;
            }

            private void Release(ParticleBurst burst)
            {
                _pool.Release(burst);
            }

            private void OnGet(ParticleBurst burst)
            {
                burst.gameObject.SetActive(true);
            }

            private void OnRelease(ParticleBurst burst)
            {
                burst.gameObject.SetActive(false);
            }

            private void OnDestroyBurst(ParticleBurst burst)
            {
                Object.Destroy(burst.gameObject);
            }
        }
    }
}
