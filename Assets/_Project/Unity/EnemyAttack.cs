using Core;
using UnityEngine;

namespace Game
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackRange = 2f;

        private float _lastAttackTime = float.NegativeInfinity;

        public int Damage => _damage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;
        public float LastAttackTime => _lastAttackTime;

        public void Init(int damage, float attackRange, float attackCooldown)
        {
            _damage = damage;
            _attackRange = attackRange;
            _attackCooldown = attackCooldown;
            ResetCooldown();
        }

        public void SetDamage(int damage)
        {
            _damage = Mathf.Max(0, damage);
        }

        public void SetAttackCooldown(float cooldown)
        {
            _attackCooldown = Mathf.Max(0.01f, cooldown);
        }

        public void SetAttackRange(float range)
        {
            _attackRange = Mathf.Max(0.1f, range);
        }

        public void ResetCooldown()
        {
            _lastAttackTime = float.NegativeInfinity;
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
