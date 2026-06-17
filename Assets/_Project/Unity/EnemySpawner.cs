using System;
using System.Collections.Generic;
using Application;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace Game
{
    public class EnemySpawner : MonoBehaviour, ISpawner
    {
        [SerializeField] private WeightedPrefab[] _prefabs;
        [SerializeField] private Transform _player;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private int _maxAlive = 100;
        [SerializeField] private float _spawnRadius = 15f;
        [SerializeField] private int _defaultPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 150;
        [SerializeField] private EffectPool _effectPool;

        private Dictionary<EnemyController, VariantPool> _pools;
        private Dictionary<EnemyController, VariantPool> _instanceToPool;
        private IGameStateProvider _gameState;
        private List<EnemyController> _alive;
        private int _aliveCount;
        private int _totalWeight;

        public event Action<int> EnemyKilled;

        private void Awake()
        {
            _gameState = _gameStateRef;
            _alive = new List<EnemyController>();
            _pools = new Dictionary<EnemyController, VariantPool>();
            _instanceToPool = new Dictionary<EnemyController, VariantPool>();

            for (int i = 0; i < _prefabs.Length; i++)
            {
                EnemyController prefab = _prefabs[i].Prefab;

                if (prefab == null)
                {
                    continue;
                }

                _totalWeight += _prefabs[i].Weight;

                if (!_pools.ContainsKey(prefab))
                {
                    _pools[prefab] = new VariantPool(prefab, _defaultPoolSize, _maxPoolSize, _effectPool);
                }
            }
        }

        public bool SpawnOne()
        {
            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return false;
            }

            if (_aliveCount >= _maxAlive)
            {
                return false;
            }

            if (!TryGetSpawnPosition(out Vector3 position))
            {
                return false;
            }

            EnemyController prefab = PickPrefab();

            if (prefab == null)
            {
                return false;
            }

            VariantPool pool = _pools[prefab];
            EnemyController enemy = pool.Get();
            _instanceToPool[enemy] = pool;
            enemy.Died += OnEnemyDied;
            enemy.Spawn(position, _player);
            _alive.Add(enemy);
            _aliveCount++;
            return true;
        }

        public void ClearAllAlive()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = _alive[i];
                enemy.Died -= OnEnemyDied;

                if (_instanceToPool.TryGetValue(enemy, out VariantPool pool))
                {
                    pool.Release(enemy);
                    _instanceToPool.Remove(enemy);
                }
            }

            _alive.Clear();
            _aliveCount = 0;
        }

        private EnemyController PickPrefab()
        {
            if (_prefabs == null || _prefabs.Length == 0 || _totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, _totalWeight);
            int cumulative = 0;

            for (int i = 0; i < _prefabs.Length; i++)
            {
                if (_prefabs[i].Prefab == null)
                {
                    continue;
                }

                cumulative += _prefabs[i].Weight;

                if (roll < cumulative)
                {
                    return _prefabs[i].Prefab;
                }
            }

            return _prefabs[_prefabs.Length - 1].Prefab;
        }

        private bool TryGetSpawnPosition(out Vector3 position)
        {
            if (_player == null)
            {
                position = Vector3.zero;
                return false;
            }

            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 candidate = _player.position + direction * _spawnRadius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            position = candidate;
            return false;
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            int scoreValue = enemy.ScoreValue;

            enemy.Died -= OnEnemyDied;
            _alive.Remove(enemy);
            _aliveCount--;

            if (_instanceToPool.TryGetValue(enemy, out VariantPool pool))
            {
                pool.Release(enemy);
                _instanceToPool.Remove(enemy);
            }

            EnemyKilled?.Invoke(scoreValue);
        }

        private class VariantPool
        {
            private readonly EnemyController _prefab;
            private readonly EffectPool _effectPool;
            private readonly ObjectPool<EnemyController> _pool;

            public VariantPool(EnemyController prefab, int defaultSize, int maxSize, EffectPool effectPool)
            {
                _prefab = prefab;
                _effectPool = effectPool;
                _pool = new ObjectPool<EnemyController>(
                    Create, OnGet, OnRelease, OnDestroyEnemy, true, defaultSize, maxSize);
            }

            public EnemyController Get()
            {
                return _pool.Get();
            }

            public void Release(EnemyController enemy)
            {
                _pool.Release(enemy);
            }

            private EnemyController Create()
            {
                EnemyController enemy = UnityEngine.Object.Instantiate(_prefab);
                InjectEffectPool(enemy);
                return enemy;
            }

            private void InjectEffectPool(EnemyController enemy)
            {
                if (_effectPool == null)
                {
                    return;
                }

                IEffectPoolUser[] users = enemy.GetComponentsInChildren<IEffectPoolUser>(true);

                for (int i = 0; i < users.Length; i++)
                {
                    users[i].SetEffectPool(_effectPool);
                }
            }

            private void OnGet(EnemyController enemy)
            {
                enemy.gameObject.SetActive(true);
            }

            private void OnRelease(EnemyController enemy)
            {
                enemy.gameObject.SetActive(false);
            }

            private void OnDestroyEnemy(EnemyController enemy)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }
    }
}
