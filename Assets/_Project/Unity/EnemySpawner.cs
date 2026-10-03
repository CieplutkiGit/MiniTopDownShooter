using System;
using System.Collections.Generic;
using Application;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Game.Flow;

namespace Game
{
    public enum SpawnResult
    {
        Success,
        CapacityReached,
        NoAvailablePosition,
        InvalidConfiguration,
        GameStateNotPlaying
    }

    public class EnemySpawner : MonoBehaviour, ISpawner
    {
        // (Fields defined above)
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

        private Dictionary<EnemyController, VariantPool> _pools = new Dictionary<EnemyController, VariantPool>();
        private Dictionary<EnemyController, VariantPool> _instanceToPool = new Dictionary<EnemyController, VariantPool>();
        private IGameStateProvider _gameState;
        private List<EnemyController> _alive = new List<EnemyController>();
        private int _aliveCount;
        private int _totalWeight;
        private SceneObjectBudget _objectBudget;
        private readonly Dictionary<EnemyController, SceneObjectBudget> _enemyBudgets = new Dictionary<EnemyController, SceneObjectBudget>();

        public event Action<int> EnemyKilled;

        public int MaxAlive => _maxAlive;
        public int AliveCount => _aliveCount;

        private void EnsureCollections()
        {
            if (_alive == null)
            {
                _alive = new List<EnemyController>();
            }

            if (_pools == null)
            {
                _pools = new Dictionary<EnemyController, VariantPool>();
            }

            if (_instanceToPool == null)
            {
                _instanceToPool = new Dictionary<EnemyController, VariantPool>();
            }
        }

        public bool HasZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || _spawnZones == null)
            {
                return false;
            }

            for (int i = 0; i < _spawnZones.Length; i++)
            {
                if (_spawnZones[i] != null && string.Equals(_spawnZones[i].Id, zoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ValidateZones(IReadOnlyList<string> zoneIds)
        {
            if (zoneIds == null || zoneIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < zoneIds.Count; i++)
            {
                if (!HasZone(zoneIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

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
            EnsureCollections();
            _player = player;
            _gameStateRef = gameState;
            _gameState = gameState;
            _effectPool = effectPool;
        }

        private void Awake()
        {
            _objectBudget = SceneObjectBudget.FindForScene(gameObject.scene);
            EnsureCollections();
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
            return TrySpawn(requestedPrefab, allowedZoneIds, out _) == SpawnResult.Success;
        }

        public SpawnResult TrySpawn(
            EnemyController requestedPrefab,
            IReadOnlyList<string> allowedZoneIds,
            out EnemyController spawnedEnemy)
        {
            spawnedEnemy = null;

            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return SpawnResult.GameStateNotPlaying;
            }

            if (_aliveCount >= _maxAlive)
            {
                return SpawnResult.CapacityReached;
            }

            EnemyController prefab = requestedPrefab != null
                ? requestedPrefab
                : PickPrefab();

            if (prefab == null)
            {
                return SpawnResult.InvalidConfiguration;
            }

            // Validate requested zone IDs against configured zones. Missing required zones are InvalidConfiguration.
            if (allowedZoneIds != null && allowedZoneIds.Count > 0)
            {
                if (!ValidateZones(allowedZoneIds))
                {
                    return SpawnResult.InvalidConfiguration;
                }
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
                return SpawnResult.NoAvailablePosition;
            }

            if (_objectBudget != null && !_objectBudget.TryReserveEnemy()) return SpawnResult.CapacityReached;

            VariantPool pool = EnsurePool(prefab);
            EnemyController enemy = pool.Get();
            _instanceToPool[enemy] = pool;
            enemy.Died += OnEnemyDied;
            enemy.Spawn(position, _player);
            _alive.Add(enemy);
            _aliveCount++;
            if (_objectBudget != null) _enemyBudgets[enemy] = _objectBudget;
            spawnedEnemy = enemy;
            return SpawnResult.Success;
        }

        public bool TryRecoverSpawnInZone(
            EnemyController requestedPrefab,
            IReadOnlyList<string> allowedZoneIds,
            out EnemyController spawnedEnemy)
        {
            spawnedEnemy = null;

            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return false;
            }

            if (_aliveCount >= _maxAlive)
            {
                return false;
            }

            EnemyController prefab = requestedPrefab != null ? requestedPrefab : PickPrefab();
            if (prefab == null)
            {
                return false;
            }

            Vector3 position = Vector3.zero;
            bool gotPosition = false;

            if (_spawnZones != null && _spawnZones.Length > 0)
            {
                for (int i = 0; i < _spawnZones.Length; i++)
                {
                    SpawnZone zone = _spawnZones[i];
                    if (zone != null && IsZoneAllowed(zone, allowedZoneIds))
                    {
                        if (zone.TryGetFallbackSpawnPosition(_player, _spawnCamera, out position))
                        {
                            gotPosition = true;
                            break;
                        }
                    }
                }
            }

            if (!gotPosition)
            {
                return false;
            }

            if (_objectBudget != null && !_objectBudget.TryReserveEnemy()) return false;

            VariantPool pool = EnsurePool(prefab);
            EnemyController enemy = pool.Get();
            _instanceToPool[enemy] = pool;
            enemy.Died += OnEnemyDied;
            enemy.Spawn(position, _player);
            _alive.Add(enemy);
            _aliveCount++;
            if (_objectBudget != null) _enemyBudgets[enemy] = _objectBudget;
            spawnedEnemy = enemy;
            return true;
        }

        public void ClearAllAlive()
        {
            EnsureCollections();
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = _alive[i];

                if (enemy == null)
                {
                    ReleaseEnemyBudget(enemy);
                    _instanceToPool.Remove(enemy);
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
                ReleaseEnemyBudget(enemy);
            }

            _alive.Clear();
            _aliveCount = 0;
        }

        public void Prewarm(int count)
        {
            if (_prefabs == null || count <= 0) return;
            for (int i = 0; i < _prefabs.Length; i++)
            {
                EnemyController prefab = _prefabs[i].Prefab;
                if (prefab != null)
                {
                    PrewarmVariant(prefab, count);
                }
            }
        }

        public void PrewarmVariant(EnemyController prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            VariantPool pool = EnsurePool(prefab);
            pool.Prewarm(count);
        }

        private VariantPool EnsurePool(EnemyController prefab)
        {
            EnsureCollections();
            if (!_pools.TryGetValue(prefab, out VariantPool pool))
            {
                pool = new VariantPool(
                    prefab,
                    Mathf.Max(1, _defaultPoolSize),
                    Mathf.Max(_defaultPoolSize, _maxPoolSize),
                    _effectPool,
                    gameObject.scene);

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
            Vector3 origin = _player != null ? _player.position : transform.position;

            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = origin + direction * _spawnRadius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            if (NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                position = candidate;
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
            ReleaseEnemyBudget(enemy);

            EnemyKilled?.Invoke(scoreValue);
        }

        private void ReleaseEnemyBudget(EnemyController enemy)
        {
            if (!ReferenceEquals(enemy, null) && _enemyBudgets.TryGetValue(enemy, out SceneObjectBudget budget))
            {
                _enemyBudgets.Remove(enemy);
                if (budget != null) budget.ReleaseEnemy();
            }
        }

        private void OnDestroy()
        {
            ClearAllAlive();
            foreach (VariantPool pool in _pools.Values) pool.Dispose();
            _pools.Clear();
        }

        public void NotifyEnemyKilled(int scoreValue)
        {
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
            private readonly Scene _scene;
            private readonly ObjectPool<EnemyController> _pool;
            private readonly int _maxSize;

            public VariantPool(
                EnemyController prefab,
                int defaultSize,
                int maxSize,
                EffectPool effectPool,
                Scene scene)
            {
                _prefab = prefab;
                _effectPool = effectPool;
                _scene = scene;
                _maxSize = maxSize;
                _pool = new ObjectPool<EnemyController>(
                    Create,
                    OnGet,
                    OnRelease,
                    OnDestroyEnemy,
                    true,
                    defaultSize,
                    maxSize);
            }

            public void Prewarm(int count)
            {
                int target = Mathf.Clamp(count, 0, _maxSize);
                if (target <= 0) return;
                List<EnemyController> spawned = new List<EnemyController>(target);
                for (int i = 0; i < target; i++)
                {
                    spawned.Add(Get());
                }
                for (int i = 0; i < spawned.Count; i++)
                {
                    if (spawned[i] != null)
                    {
                        Release(spawned[i]);
                    }
                }
            }

            public EnemyController Get()
            {
                return _pool.Get();
            }

            public void Release(EnemyController enemy)
            {
                if (enemy == null) return;
                _pool.Release(enemy);
            }

            public void Dispose() => _pool.Dispose();

            private EnemyController Create()
            {
                EnemyController enemy = UnityEngine.Object.Instantiate(_prefab);
                if (enemy != null)
                {
                    if (_scene.IsValid() && _scene.isLoaded && enemy.gameObject.scene != _scene)
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemy.gameObject, _scene);
                    InjectEffectPool(enemy);
                }
                return enemy;
            }

            private void InjectEffectPool(EnemyController enemy)
            {
                if (_effectPool == null || enemy == null)
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
                if (enemy == null) return;
                enemy.GetComponent<EnemyAttack>()?.ResetCooldown();
                if (enemy.gameObject != null)
                {
                    enemy.gameObject.SetActive(true);
                }
            }

            private void OnRelease(EnemyController enemy)
            {
                if (enemy == null) return;
                enemy.GetComponent<EnemyAttack>()?.ResetCooldown();
                if (enemy.gameObject != null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            private void OnDestroyEnemy(EnemyController enemy)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    UnityEngine.Object.Destroy(enemy.gameObject);
                }
            }
        }
    }
}
