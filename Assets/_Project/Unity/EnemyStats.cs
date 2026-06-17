using Application;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Game/Enemy Stats")]
    public class EnemyStats : ScriptableObject, IEnemyStats
    {
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private int _scoreValue = 10;

        public float MoveSpeed
        {
            get { return _moveSpeed; }
        }

        public int MaxHealth
        {
            get { return _maxHealth; }
        }

        public int Damage
        {
            get { return _damage; }
        }

        public float AttackRange
        {
            get { return _attackRange; }
        }

        public float AttackCooldown
        {
            get { return _attackCooldown; }
        }

        public int ScoreValue
        {
            get { return _scoreValue; }
        }
    }
}
