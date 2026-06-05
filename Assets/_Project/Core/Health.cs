using System;
namespace Core
{
    public class Health : IDamageable
    {
        private int _maxHealth;
        private int _currentHealth;

        public event Action OnDead;

        public Health(int maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
        }

        public void TakeDamage(DamageData data)
        {
            _currentHealth -= data.Damage;

            if(_currentHealth <= 0)
            OnDead?.Invoke();

        }
    }
}
