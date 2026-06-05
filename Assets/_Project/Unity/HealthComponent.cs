using System;
using Core;
using UnityEngine;

namespace Game
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 100;

        private Health _health;

        public event Action OnDead;

        private void Awake()
        {
            _health = new Health(_maxHealth);
            _health.OnDead += HandleDead;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDead -= HandleDead;
            }
        }

        public void TakeDamage(DamageData data)
        {
            _health.TakeDamage(data);
        }

        private void HandleDead()
        {
            OnDead?.Invoke();
        }
    }
}
