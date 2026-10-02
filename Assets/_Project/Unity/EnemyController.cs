using System;
using Application;
using Core;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyController : MonoBehaviour, IEnemyEvents
    {
        [SerializeField] private float _thinkInterval = 0.2f;
        [SerializeField] private EnemyStats _statsRef;
        [SerializeField] private int _fallbackScore = 10;
        [Tooltip("Optional behavior module. Leave empty to use the original chase/melee state machine.")]
        [SerializeField] private EnemyBehaviorBase _behavior;

        [SerializeField] private GameStateController _gameStateRef;

        private EnemyMovement _movement;
        private EnemyAttack _attack;
        private HealthComponent _health;
        private IEnemyStats _stats;
        private IGameStateProvider _gameState;

        private IEnemyState _currentState;
        private ChaseState _chaseState;
        private AttackState _attackState;
        private DeadState _deadState;

        private Transform _target;
        private IDamageable _targetDamageable;

        public event Action<EnemyController> Died;

        public int ScoreValue => _stats != null ? _stats.ScoreValue : _fallbackScore;
        public EnemyBehaviorBase Behavior =>
            _behavior != null ? _behavior : GetComponent<EnemyBehaviorBase>();
        public EnemyStats Stats => _statsRef;

        private Action _diedObservers;

        event Action IEnemyEvents.Died
        {
            add { _diedObservers += value; }
            remove { _diedObservers -= value; }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;

            _movement = GetComponent<EnemyMovement>();
            _attack = GetComponent<EnemyAttack>();
            _health = GetComponent<HealthComponent>();
            _deadState = new DeadState(_movement);
            _stats = _statsRef;

            if (_behavior == null)
            {
                _behavior = GetComponent<EnemyBehaviorBase>();
            }
        }

        private void OnEnable()
        {
            _health.OnDead += HandleDead;
        }

        private void OnDisable()
        {
            _health.OnDead -= HandleDead;
            CancelInvoke();

            if (_behavior != null)
            {
                _behavior.OnDespawn();
            }
        }

        public void Spawn(Vector3 position, Transform target)
        {
            CancelInvoke();
            transform.position = position;

            if (_movement != null)
            {
                _movement.Warp(position);
                _movement.SetSpeedMultiplier(1f);
            }

            if (_stats != null)
            {
                if (_movement != null)
                {
                    _movement.Init(_stats.MoveSpeed);
                }
                if (_attack != null)
                {
                    _attack.Init(_stats.Damage, _stats.AttackRange, _stats.AttackCooldown);
                }
                if (_health != null)
                {
                    _health.SetMaxHealth(_stats.MaxHealth);
                }
            }
            else if (_health != null)
            {
                _health.ResetHealth();
            }

            _target = target;
            _targetDamageable = target != null ? target.GetComponent<IDamageable>() : null;

            if (_behavior != null)
            {
                _currentState = null;
                _behavior.Initialize(
                    this,
                    _movement,
                    _attack,
                    _health,
                    _target,
                    _targetDamageable);
            }
            else
            {
                _chaseState = new ChaseState(_movement, _target);
                _attackState = new AttackState(_attack, _targetDamageable);
                ChangeState(_chaseState);
            }

            float startDelay = UnityEngine.Random.Range(0f, Mathf.Max(0.01f, _thinkInterval));
            InvokeRepeating(nameof(Think), startDelay, Mathf.Max(0.01f, _thinkInterval));
        }

        private void Think()
        {
            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                return;
            }

            if (_target == null)
            {
                return;
            }

            if (_behavior != null)
            {
                _behavior.Tick();
                return;
            }

            if (_currentState == null || _targetDamageable == null)
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

            if (_currentState != null)
            {
                _currentState.Enter();
            }
        }

        private void HandleDead()
        {
            CancelInvoke();

            if (_behavior != null)
            {
                _behavior.OnDeath();
            }

            ChangeState(_deadState);
            _diedObservers?.Invoke();
            Died?.Invoke(this);
        }

        private void OnValidate()
        {
            _thinkInterval = Mathf.Max(0.01f, _thinkInterval);
            _fallbackScore = Mathf.Max(0, _fallbackScore);
        }
    }
}
