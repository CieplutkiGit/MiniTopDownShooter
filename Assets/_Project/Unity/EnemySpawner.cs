using System.Collections.Generic;
using Application;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace Game
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private Transform _player;
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private int _maxAlive = 100;
        [SerializeField] private float _spawnRadius = 15f;
        [SerializeField] private int _defaultPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 150;

        private ObjectPool<EnemyController> _pool;
        private IGameStateProvider _gameState;
        private List<EnemyController> _alive;
        private int _aliveCount;

        private void Awake()
        {
            _pool = new ObjectPool<EnemyController>(CreateEnemy, OnGetEnemy, OnReleaseEnemy, OnDestroyEnemy, true, _defaultPoolSize, _maxPoolSize);
            _gameState = _gameStateRef;
            _alive = new List<EnemyController>();
        }

        private void Start()
        {
            InvokeRepeating(nameof(SpawnEnemy), _spawnInterval, _spawnInterval);
        }

        private void SpawnEnemy()
        {
            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return;
            }

            if (_aliveCount >= _maxAlive)
            {
                return;
            }

            if (!TryGetSpawnPosition(out Vector3 position))
            {
                return;
            }

            EnemyController enemy = _pool.Get();
            enemy.Died += OnEnemyDied;
            enemy.Spawn(position, _player);
            _alive.Add(enemy);
            _aliveCount++;
        }

        public void ClearAllAlive()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = _alive[i];
                enemy.Died -= OnEnemyDied;
                _pool.Release(enemy);
            }

            _alive.Clear();
            _aliveCount = 0;
        }

        private bool TryGetSpawnPosition(out Vector3 position)
        {
            float angle = Random.value * Mathf.PI * 2f;
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
            enemy.Died -= OnEnemyDied;
            _alive.Remove(enemy);
            _aliveCount--;
            _pool.Release(enemy);
        }

        private EnemyController CreateEnemy()
        {
            return Instantiate(_enemyPrefab);
        }

        private void OnGetEnemy(EnemyController enemy)
        {
            enemy.gameObject.SetActive(true);
        }

        private void OnReleaseEnemy(EnemyController enemy)
        {
            enemy.gameObject.SetActive(false);
        }

        private void OnDestroyEnemy(EnemyController enemy)
        {
            Destroy(enemy.gameObject);
        }
    }
}
