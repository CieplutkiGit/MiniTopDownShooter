using Application;
using UnityEngine;

namespace Game
{
    public class EnemyDeathFX : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemyRef;
        [SerializeField] private ParticleSystem _deathEffect;

        private IEnemyEvents _enemy;

        private void Awake()
        {
            _enemy = _enemyRef;
        }

        private void OnEnable()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (_deathEffect == null)
            {
                return;
            }

            Instantiate(_deathEffect, transform.position, Quaternion.identity);
        }
    }
}
