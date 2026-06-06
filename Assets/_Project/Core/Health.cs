using System;

namespace Core
{
    public class Health : IDamageable
    {
        private readonly int _maxHealth;
        private int _currentHealth;
        private bool _isDead;

        public event Action OnDead;
        public event Action<int, int> OnHealthChanged;

        public bool IsDead
        {
            get { return _isDead; }
        }

        public Health(int maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            _isDead = false;
        }

        public void TakeDamage(DamageData data)
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth -= data.Damage;

            if (_currentHealth < 0)
            {
                _currentHealth = 0;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                _isDead = true;
                OnDead?.Invoke();
            }
        }

        public void Reset()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
