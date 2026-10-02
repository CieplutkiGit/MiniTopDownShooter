using System;
using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerRotation))]
    [RequireComponent(typeof(PlayerShoot))]
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour, IPlayerEvents
    {
        [Header("Aiming")]
        [SerializeField] private Camera _aimCamera;
        [SerializeField] private float _shootThreshold = 0.1f;

        [Header("Mobile")]
        [SerializeField] private MobileInputState _mobileInput;

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
            if (_aimCamera == null)
            {
                _aimCamera = Camera.main;
            }

            if (_mobileInput == null)
            {
                _mobileInput = FindFirstObjectByType<MobileInputState>(
                    FindObjectsInactive.Include);
            }

            _input = new InputReader(_aimCamera, transform, _mobileInput);
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

        public void BeginInputRebind(string actionName, int bindingIndex)
        {
            _input.BeginInteractiveRebind(actionName, bindingIndex);
        }

        public string GetBindingDisplayString(string actionName, int bindingIndex)
        {
            return _input.GetBindingDisplayString(actionName, bindingIndex);
        }

        public void ResetInputBindings()
        {
            _input.ResetBindingsToDefault();
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }

        private void OnValidate()
        {
            _shootThreshold = Mathf.Max(0f, _shootThreshold);
        }
    }
}
