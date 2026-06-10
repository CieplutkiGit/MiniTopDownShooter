using Core;
using UnityEngine;

namespace Game
{
    public class EnemyHitFX : MonoBehaviour
    {
        [SerializeField] private HealthComponent _healthRef;
        [SerializeField] private ParticleSystem _hitEffect;

        private IHealthReadable _health;
        private int _lastHealth;

        private void Awake()
        {
            _health = _healthRef;
        }

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged += HandleHealthChanged;
            _lastHealth = _health.Current;
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (current >= _lastHealth)
            {
                _lastHealth = current;
                return;
            }

            _lastHealth = current;

            if (current <= 0)
            {
                return;
            }

            if (_hitEffect == null)
            {
                return;
            }

            Instantiate(_hitEffect, transform.position, Quaternion.identity);
        }
    }
}
