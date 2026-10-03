using System;
using System.Collections.Generic;
using Application.Economy;
using UnityEngine;

namespace Game.Economy
{
    public class EnemySalvageDropManager : MonoBehaviour
    {
        private static EnemySalvageDropManager s_instance;
        public static EnemySalvageDropManager Instance => s_instance;

        public static void RegisterEnemyStatic(EnemyController enemy)
        {
            if (s_instance != null && s_instance.isActiveAndEnabled && enemy != null)
            {
                s_instance.RegisterEnemy(enemy);
            }
        }

        [SerializeField] private SalvagePickupPool _pickupPool;
        [SerializeField] private Transform _player;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private float _scanInterval = 0.5f;

        private readonly HashSet<EnemyController> _subscribedEnemies = new HashSet<EnemyController>();
        private readonly Dictionary<EnemyController, int> _lastDropFrame = new Dictionary<EnemyController, int>();
        private float _lastScanTime;
        private int _lastAliveCount = -1;

        public SalvagePickupPool PickupPool => _pickupPool;
        public int TrackedEnemiesCount => _subscribedEnemies.Count;

        public event Action<EnemyController, EconomyComponentType, int> OnSalvageDropped;

        public void Initialize(SalvagePickupPool pool, Transform player = null)
        {
            s_instance = this;
            enabled = true;
            _pickupPool = pool;
            if (_pickupPool != null)
            {
                _pickupPool.Initialize();
            }
            _player = player;
            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<EnemySpawner>();
                if (_spawner != null)
                {
                    _spawner.EnemyKilled -= HandleSpawnerEnemyKilled;
                    _spawner.EnemyKilled += HandleSpawnerEnemyKilled;
                }
            }
            ScanAndRegisterEnemies();
        }

        private void Awake()
        {
            s_instance = this;
            if (_pickupPool == null)
            {
                _pickupPool = GetComponent<SalvagePickupPool>();
                if (_pickupPool == null)
                {
                    _pickupPool = gameObject.AddComponent<SalvagePickupPool>();
                }
            }
        }

        private void Start()
        {
            if (_player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<EnemySpawner>();
                if (_spawner != null)
                {
                    _spawner.EnemyKilled -= HandleSpawnerEnemyKilled;
                    _spawner.EnemyKilled += HandleSpawnerEnemyKilled;
                }
            }

            ScanAndRegisterEnemies();
        }

        private void Update()
        {
            if (!isActiveAndEnabled) return;

            PruneDestroyedEnemies();

            if (_spawner != null)
            {
                if (_spawner.AliveCount > _subscribedEnemies.Count)
                {
                    _lastAliveCount = _spawner.AliveCount;
                    _lastScanTime = Time.time;
                    ScanAndRegisterEnemies();
                }
                return;
            }

            if (Time.time - _lastScanTime >= _scanInterval)
            {
                _lastScanTime = Time.time;
                ScanAndRegisterEnemies();
            }
        }

        public void PruneDestroyedEnemies()
        {
            _subscribedEnemies.RemoveWhere(e => e == null);
        }

        public void ScanAndRegisterEnemies()
        {
            var scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    if (roots[r] == null) continue;
                    var enemies = roots[r].GetComponentsInChildren<EnemyController>(true);
                    for (int i = 0; i < enemies.Length; i++)
                    {
                        RegisterEnemy(enemies[i]);
                    }
                }
            }
            else
            {
                EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < enemies.Length; i++)
                {
                    RegisterEnemy(enemies[i]);
                }
            }
        }

        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null) return;

            if (_subscribedEnemies.Add(enemy))
            {
                enemy.Died += HandleEnemyDied;
            }
        }

        public void UnregisterEnemy(EnemyController enemy)
        {
            if (enemy == null) return;

            if (_subscribedEnemies.Remove(enemy))
            {
                enemy.Died -= HandleEnemyDied;
            }
            _lastDropFrame.Remove(enemy);
        }

        private void HandleSpawnerEnemyKilled(int scoreValue)
        {
            // If an enemy was killed before registration, scan and prune
            PruneDestroyedEnemies();
        }

        public void HandleEnemyDied(EnemyController enemy)
        {
            if (enemy == null) return;

            // Guard against duplicate pooled enemy drops
            if (_lastDropFrame.TryGetValue(enemy, out int frame) && frame == Time.frameCount)
            {
                return;
            }
            _lastDropFrame[enemy] = Time.frameCount;

            if (_pickupPool == null) return;

            Vector3 deathPos = enemy.transform.position + Vector3.up * 0.4f;
            int score = enemy.ScoreValue;

            if (_player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            // Always drop at least 1-2 Scrap
            int scrapCount = score >= 30 ? 2 : 1;
            for (int i = 0; i < scrapCount; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0f, UnityEngine.Random.Range(-0.4f, 0.4f));
                _pickupPool.Spawn(deathPos + offset, EconomyComponentType.Scrap, 1, _player);
                OnSalvageDropped?.Invoke(enemy, EconomyComponentType.Scrap, 1);
            }

            // Medium & Elite enemies drop Alloy
            bool dropAlloy = (score >= 50) || (score >= 20 && UnityEngine.Random.value < 0.5f) || (UnityEngine.Random.value < 0.25f);
            if (dropAlloy)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                _pickupPool.Spawn(deathPos + offset, EconomyComponentType.Alloy, 1, _player);
                OnSalvageDropped?.Invoke(enemy, EconomyComponentType.Alloy, 1);
            }

            // Bosses & Elite enemies drop Energy Core
            bool dropCore = (score >= 50) || (score >= 30 && UnityEngine.Random.value < 0.25f) || (UnityEngine.Random.value < 0.08f);
            if (dropCore)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.2f, UnityEngine.Random.Range(-0.3f, 0.3f));
                _pickupPool.Spawn(deathPos + offset, EconomyComponentType.Core, 1, _player);
                OnSalvageDropped?.Invoke(enemy, EconomyComponentType.Core, 1);
            }
        }

        public void ClearAndReset()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            if (_spawner != null)
            {
                _spawner.EnemyKilled -= HandleSpawnerEnemyKilled;
                _spawner = null;
            }

            foreach (var enemy in _subscribedEnemies)
            {
                if (enemy != null)
                {
                    enemy.Died -= HandleEnemyDied;
                }
            }
            _subscribedEnemies.Clear();
            _lastDropFrame.Clear();
            _lastAliveCount = -1;
            enabled = false;
        }

        private void OnDestroy()
        {
            ClearAndReset();
        }
    }
}
