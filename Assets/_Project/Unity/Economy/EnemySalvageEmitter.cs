using System;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Scene-local salvage emitter attached to enemies.
    /// Hooks directly into EnemyController.Died at spawn time, eliminating scan latency
    /// and ensuring enemies killed before the next scan frame reliably drop salvage.
    /// Integration contract for Combat worker:
    /// In EnemyController.Spawn(Vector3 position, ...), call:
    ///     EnemySalvageEmitter.Attach(spawnedEnemy);
    /// Or attach this component directly to the enemy prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemySalvageEmitter : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;

        public EnemyController Enemy => _enemy;

        public static EnemySalvageEmitter Attach(EnemyController enemy)
        {
            if (enemy == null) return null;

            EnemySalvageEmitter emitter = enemy.GetComponent<EnemySalvageEmitter>();
            if (emitter == null)
            {
                emitter = enemy.gameObject.AddComponent<EnemySalvageEmitter>();
            }
            emitter.Initialize(enemy);
            return emitter;
        }

        public void Initialize(EnemyController enemy)
        {
            if (_enemy != null && _enemy != enemy)
            {
                _enemy.Died -= HandleEnemyDied;
            }

            _enemy = enemy;

            if (_enemy != null)
            {
                _enemy.Died -= HandleEnemyDied;
                _enemy.Died += HandleEnemyDied;
            }

            EnemySalvageDropManager.RegisterEnemyStatic(_enemy);
        }

        private void Awake()
        {
            if (_enemy == null)
            {
                _enemy = GetComponent<EnemyController>();
            }
        }

        private void OnEnable()
        {
            if (_enemy == null)
            {
                _enemy = GetComponent<EnemyController>();
            }

            if (_enemy != null)
            {
                _enemy.Died -= HandleEnemyDied;
                _enemy.Died += HandleEnemyDied;
                EnemySalvageDropManager.RegisterEnemyStatic(_enemy);
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.Died -= HandleEnemyDied;
            }
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            var dropManager = EnemySalvageDropManager.Instance;
            if (dropManager != null && dropManager.isActiveAndEnabled)
            {
                dropManager.HandleEnemyDied(enemy != null ? enemy : _enemy);
            }
        }
    }
}
