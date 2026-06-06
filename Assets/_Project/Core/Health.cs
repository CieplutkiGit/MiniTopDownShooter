using System;

namespace Core
{
    public class Health : IDamageable
    {
        private readonly int _maxHealth;
        private int _currentHealth;

        public event Action OnDead;
        public event Action<int, int> OnHealthChanged;

        public Health(int maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
        }

        public void TakeDamage(DamageData data)
        {
            _currentHealth -= data.Damage;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                OnDead?.Invoke();
            }
        }

        public void Reset()
        {
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
