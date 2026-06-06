using System;
using Application;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerRotation))]
    [RequireComponent(typeof(PlayerShoot))]
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour, IPlayerEvents
    {
        [SerializeField] private float _shootThreshold = 0.1f;

        private InputReader _input;
        private PlayerMovement _movement;
        private PlayerRotation _rotation;
        private PlayerShoot _shoot;
        private HealthComponent _health;

        private IPlayerState _currentState;
        private AlivePlayerState _aliveState;
        private DeadPlayerState _deadState;

        public event Action Died;

        private void Awake()
        {
            _input = new InputReader();
            _movement = GetComponent<PlayerMovement>();
            _rotation = GetComponent<PlayerRotation>();
            _shoot = GetComponent<PlayerShoot>();
            _health = GetComponent<HealthComponent>();

            _aliveState = new AlivePlayerState(_input, _movement, _rotation, _shoot, _shootThreshold);
            _deadState = new DeadPlayerState(_movement);
        }

        private void OnEnable()
        {
            _health.OnDead += HandleDead;
            ChangeState(_aliveState);
        }

        private void OnDisable()
        {
            _health.OnDead -= HandleDead;
        }

        private void FixedUpdate()
        {
            if (_currentState != null)
            {
                _currentState.Update();
            }
        }

        private void ChangeState(IPlayerState newState)
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
            ChangeState(_deadState);
            Died?.Invoke();
        }

        public void Respawn(Vector3 position)
        {
            _movement.Warp(position);
            _health.ResetHealth();
            ChangeState(_aliveState);
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }
    }
}
