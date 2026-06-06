using System;
using Core;
using UnityEngine;

namespace Game
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private bool _debugLog = false;

        private Health _health;

        public event Action OnDead;
        public event Action<int, int> OnHealthChanged;

        private void Awake()
        {
            _health = new Health(_maxHealth);
            _health.OnDead += HandleDead;
            _health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDead -= HandleDead;
                _health.OnHealthChanged -= HandleHealthChanged;
            }
        }

        public void TakeDamage(DamageData data)
        {
            _health.TakeDamage(data);
        }

        public void ResetHealth()
        {
            _health.Reset();
        }

        private void HandleDead()
        {
            OnDead?.Invoke();
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (_debugLog)
            {
                Debug.Log($"{name} health: {current}/{max}");
            }

            OnHealthChanged?.Invoke(current, max);
        }
    }
}
