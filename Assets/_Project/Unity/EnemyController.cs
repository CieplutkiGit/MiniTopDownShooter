using System;
using Application;
using Core;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float _thinkInterval = 0.2f;

        private EnemyMovement _movement;
        private EnemyAttack _attack;
        private HealthComponent _health;

        private IEnemyState _currentState;
        private ChaseState _chaseState;
        private AttackState _attackState;
        private DeadState _deadState;

        private Transform _target;
        private IDamageable _targetDamageable;

        public event Action<EnemyController> Died;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _attack = GetComponent<EnemyAttack>();
            _health = GetComponent<HealthComponent>();
            _deadState = new DeadState(_movement);
        }

        private void OnEnable()
        {
            _health.OnDead += HandleDead;
        }

        private void OnDisable()
        {
            _health.OnDead -= HandleDead;
            CancelInvoke();
        }

        public void Spawn(Vector3 position, Transform target)
        {
            _movement.Warp(position);
            _health.ResetHealth();

            _target = target;
            _targetDamageable = target.GetComponent<IDamageable>();

            _chaseState = new ChaseState(_movement, _target);
            _attackState = new AttackState(_attack, _targetDamageable);

            ChangeState(_chaseState);

            float startDelay = UnityEngine.Random.Range(0f, _thinkInterval);
            InvokeRepeating(nameof(Think), startDelay, _thinkInterval);
        }

        private void Think()
        {
            if (_currentState == null || _target == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);
            bool inRange = _attack.IsInRange(distance);

            if (inRange && _currentState == _chaseState)
            {
                ChangeState(_attackState);
            }
            else if (!inRange && _currentState == _attackState)
            {
                ChangeState(_chaseState);
            }

            _currentState.Update();
        }

        private void ChangeState(IEnemyState newState)
        {
            if (_currentState != null)
            {
                _currentState.Exit();
            }

            _currentState = newState;
            _currentState.Enter();
        }

        private void HandleDead()
        {
            CancelInvoke();
            ChangeState(_deadState);
            Died?.Invoke(this);
        }
    }
}
