using UnityEngine;

namespace Game
{
    public class EnemyDeathFX : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemyRef;
        [SerializeField] private ParticleSystem _deathEffect;

        private void OnEnable()
        {
            if (_enemyRef == null)
            {
                return;
            }

            _enemyRef.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_enemyRef == null)
            {
                return;
            }

            _enemyRef.Died -= HandleDied;
        }

        private void HandleDied(EnemyController enemy)
        {
            if (_deathEffect == null)
            {
                return;
            }

            Instantiate(_deathEffect, enemy.transform.position, Quaternion.identity);
        }
    }
}
