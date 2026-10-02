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
        [Header("Enemy Pool")]
        [SerializeField] private WeightedPrefab[] _prefabs;
        [SerializeField] private int _defaultPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 150;
        [SerializeField] private int _maxAlive = 100;

        [Header("Scene References")]
        [SerializeField] private Transform _player;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private EffectPool _effectPool;

        [Header("Spawn Position")]
        [Tooltip("Optional reusable zones. If none can produce a valid position, the legacy radius is used.")]
        [SerializeField] private SpawnZone[] _spawnZones;
        [SerializeField] private Camera _spawnCamera;
        [Min(0.1f)]
        [SerializeField] private float _spawnRadius = 15f;

        [Range(1, 10)]
        [SerializeField] private int _maxSpawnRetries = 5;

        private Dictionary<EnemyController, VariantPool> _pools;
        private Dictionary<EnemyController, VariantPool> _instanceToPool;
        private IGameStateProvider _gameState;
        private List<EnemyController> _alive;
        private int _aliveCount;
        private int _totalWeight;

        public event Action<int> EnemyKilled;

        public IReadOnlyList<SpawnZone> SpawnZones => _spawnZones;

        public bool HasFallbackPrefabs
        {
            get
            {
                if (_prefabs == null)
                {
                    return false;
                }

                for (int i = 0; i < _prefabs.Length; i++)
                {
                    if (_prefabs[i].Prefab != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Initialize(Transform player, GameStateController gameState, EffectPool effectPool)
        {
            _player = player;
            _gameStateRef = gameState;
            _gameState = gameState;
            _effectPool = effectPool;
        }

        private void Awake()
        {
            if (_player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    _player = pc.transform;
                }
            }

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_effectPool == null)
            {
                _effectPool = FindFirstObjectByType<EffectPool>();
            }

            _gameState = _gameStateRef;
            _alive = new List<EnemyController>();
            _pools = new Dictionary<EnemyController, VariantPool>();
            _instanceToPool = new Dictionary<EnemyController, VariantPool>();

            if (_spawnCamera == null)
            {
                _spawnCamera = Camera.main;
            }

            if (_prefabs == null)
            {
                return;
            }

            for (int i = 0; i < _prefabs.Length; i++)
            {
                EnemyController prefab = _prefabs[i].Prefab;

                if (prefab == null)
                {
                    continue;
                }

                _totalWeight += Mathf.Max(1, _prefabs[i].Weight);
                EnsurePool(prefab);
            }
        }

        public bool SpawnOne()
        {
            return SpawnOne(null, null);
        }

        public bool SpawnOne(
            EnemyController requestedPrefab,
            IReadOnlyList<string> allowedZoneIds)
        {
            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return false;
            }

            if (_aliveCount >= _maxAlive)
            {
                return false;
            }

            bool gotPosition = false;
            Vector3 position = Vector3.zero;

            for (int attempt = 0; attempt < Mathf.Max(1, _maxSpawnRetries); attempt++)
            {
                if (TryGetSpawnPosition(allowedZoneIds, out position))
                {
                    gotPosition = true;
                    break;
                }
            }

            if (!gotPosition)
            {
                return false;
            }

            EnemyController prefab = requestedPrefab != null
                ? requestedPrefab
                : PickPrefab();

            if (prefab == null)
            {
                return false;
            }

            VariantPool pool = EnsurePool(prefab);
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

                if (enemy == null)
                {
                    continue;
                }

                enemy.Died -= OnEnemyDied;

                EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
                if (attack != null)
                {
                    attack.ResetCooldown();
                }

                if (_instanceToPool.TryGetValue(enemy, out VariantPool pool))
                {
                    pool.Release(enemy);
                    _instanceToPool.Remove(enemy);
                }
            }

            _alive.Clear();
            _aliveCount = 0;
        }

        private VariantPool EnsurePool(EnemyController prefab)
        {
            if (!_pools.TryGetValue(prefab, out VariantPool pool))
            {
                pool = new VariantPool(
                    prefab,
                    Mathf.Max(1, _defaultPoolSize),
                    Mathf.Max(_defaultPoolSize, _maxPoolSize),
                    _effectPool);

                _pools[prefab] = pool;
            }

            return pool;
        }

        private EnemyController PickPrefab()
        {
            if (_prefabs == null || _prefabs.Length == 0 || _totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, _totalWeight);
            int cumulative = 0;
            EnemyController lastValid = null;

            for (int i = 0; i < _prefabs.Length; i++)
            {
                if (_prefabs[i].Prefab == null)
                {
                    continue;
                }

                lastValid = _prefabs[i].Prefab;
                cumulative += Mathf.Max(1, _prefabs[i].Weight);

                if (roll < cumulative)
                {
                    return _prefabs[i].Prefab;
                }
            }

            return lastValid;
        }

        private bool TryGetSpawnPosition(
            IReadOnlyList<string> allowedZoneIds,
            out Vector3 position)
        {
            if (_spawnZones != null && _spawnZones.Length > 0)
            {
                SpawnZone selected = PickSpawnZone(allowedZoneIds);

                if (selected != null &&
                    selected.TryGetSpawnPosition(_player, _spawnCamera, out position))
                {
                    return true;
                }

                for (int i = 0; i < _spawnZones.Length; i++)
                {
                    SpawnZone zone = _spawnZones[i];

                    if (zone == null || zone == selected || !IsZoneAllowed(zone, allowedZoneIds))
                    {
                        continue;
                    }

                    if (zone.TryGetSpawnPosition(_player, _spawnCamera, out position))
                    {
                        return true;
                    }
                }
            }

            if (allowedZoneIds != null && allowedZoneIds.Count > 0)
            {
                // Preserve zone restrictions: never sample legacy arbitrary position outside allowed zones
                position = Vector3.zero;
                return false;
            }

            return TryGetLegacySpawnPosition(out position);
        }

        private SpawnZone PickSpawnZone(IReadOnlyList<string> allowedZoneIds)
        {
            int totalWeight = 0;

            for (int i = 0; i < _spawnZones.Length; i++)
            {
                SpawnZone zone = _spawnZones[i];

                if (zone != null && IsZoneAllowed(zone, allowedZoneIds))
                {
                    totalWeight += zone.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < _spawnZones.Length; i++)
            {
                SpawnZone zone = _spawnZones[i];

                if (zone == null || !IsZoneAllowed(zone, allowedZoneIds))
                {
                    continue;
                }

                cumulative += zone.Weight;

                if (roll < cumulative)
                {
                    return zone;
                }
            }

            return null;
        }

        private static bool IsZoneAllowed(
            SpawnZone zone,
            IReadOnlyList<string> allowedZoneIds)
        {
            if (allowedZoneIds == null || allowedZoneIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < allowedZoneIds.Count; i++)
            {
                if (string.Equals(
                        zone.Id,
                        allowedZoneIds[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetLegacySpawnPosition(out Vector3 position)
        {
            if (_player == null)
            {
                position = Vector3.zero;
                return false;
            }

            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
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
            _aliveCount = Mathf.Max(0, _aliveCount - 1);

            if (_instanceToPool.TryGetValue(enemy, out VariantPool pool))
            {
                pool.Release(enemy);
                _instanceToPool.Remove(enemy);
            }

            EnemyKilled?.Invoke(scoreValue);
        }

        private void OnValidate()
        {
            _maxAlive = Mathf.Max(1, _maxAlive);
            _spawnRadius = Mathf.Max(0.1f, _spawnRadius);
            _defaultPoolSize = Mathf.Max(1, _defaultPoolSize);
            _maxPoolSize = Mathf.Max(_defaultPoolSize, _maxPoolSize);
        }

        private class VariantPool
        {
            private readonly EnemyController _prefab;
            private readonly EffectPool _effectPool;
            private readonly ObjectPool<EnemyController> _pool;

            public VariantPool(
                EnemyController prefab,
                int defaultSize,
                int maxSize,
                EffectPool effectPool)
            {
                _prefab = prefab;
                _effectPool = effectPool;
                _pool = new ObjectPool<EnemyController>(
                    Create,
                    OnGet,
                    OnRelease,
                    OnDestroyEnemy,
                    true,
                    defaultSize,
                    maxSize);
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
                enemy.GetComponent<EnemyAttack>()?.ResetCooldown();
                enemy.gameObject.SetActive(true);
            }

            private void OnRelease(EnemyController enemy)
            {
                enemy.GetComponent<EnemyAttack>()?.ResetCooldown();
                enemy.gameObject.SetActive(false);
            }

            private void OnDestroyEnemy(EnemyController enemy)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }
    }
}
