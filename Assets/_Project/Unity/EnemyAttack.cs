using Core;
using UnityEngine;

namespace Game
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackRange = 2f;

        private float _lastAttackTime;

        public void Init(int damage, float attackRange, float attackCooldown)
        {
            _damage = damage;
            _attackRange = attackRange;
            _attackCooldown = attackCooldown;
        }

        public bool IsInRange(float distance)
        {
            return distance <= _attackRange;
        }

        public void TryAttack(IDamageable target)
        {
            if (Time.time < _lastAttackTime + _attackCooldown)
            {
                return;
            }

            _lastAttackTime = Time.time;

            DamageData damage = new DamageData(_damage);
            target.TakeDamage(damage);
        }
    }
}
