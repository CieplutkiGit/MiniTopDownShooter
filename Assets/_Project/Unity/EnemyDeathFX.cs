using Application;
using UnityEngine;

namespace Game
{
    public class EnemyDeathFX : MonoBehaviour, IEffectPoolUser
    {
        [SerializeField] private EnemyController _enemyRef;
        [SerializeField] private ParticleSystem _deathEffect;

        private IEnemyEvents _enemy;
        private EffectPool _pool;

        private void Awake()
        {
            _enemy = _enemyRef;
        }

        public void SetEffectPool(EffectPool pool)
        {
            _pool = pool;
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

            if (_pool != null)
            {
                _pool.Play(_deathEffect, transform.position);
                return;
            }

            Instantiate(_deathEffect, transform.position, Quaternion.identity);
        }
    }
}
