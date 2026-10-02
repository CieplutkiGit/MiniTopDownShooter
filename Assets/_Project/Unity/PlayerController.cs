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
        [Header("Aiming")]
        [SerializeField] private Camera _aimCamera;
        [SerializeField] private float _shootThreshold = 0.1f;

        [Header("Mobile")]
        [SerializeField] private MobileInputState _mobileInput;

        [Header("State")]
        [SerializeField] private GameStateController _gameStateRef;

        private InputReader _input;
        private PlayerMovement _movement;
        private PlayerRotation _rotation;
        private PlayerShoot _shoot;
        private HealthComponent _health;
        private IGameStateProvider _gameState;

        private IPlayerState _currentState;
        private AlivePlayerState _aliveState;
        private DeadPlayerState _deadState;

        public event Action Died;

        public void Initialize(GameStateController gameState, Camera aimCamera = null, MobileInputState mobileInput = null)
        {
            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }

            _gameStateRef = gameState;
            _gameState = gameState;
            if (aimCamera != null)
            {
                _aimCamera = aimCamera;
            }
            if (mobileInput != null)
            {
                _mobileInput = mobileInput;
            }

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleGameStateChanged;
            }
        }

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

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            _gameState = _gameStateRef;
            _input = new InputReader(_aimCamera, transform, _mobileInput);
            _movement = GetComponent<PlayerMovement>();
            _rotation = GetComponent<PlayerRotation>();
            _shoot = GetComponent<PlayerShoot>();
            _health = GetComponent<HealthComponent>();

            _aliveState = new AlivePlayerState(_input, _movement, _rotation, _shoot, _shootThreshold);
            _deadState = new DeadPlayerState(_movement);
        }

        private void Start()
        {
            GameSettingsData settings = SaveManager.LoadSettings();
            ApplySettings(settings);
        }

        private void OnEnable()
        {
            _health.OnDead += HandleDead;
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleGameStateChanged;
            }
            ChangeState(_aliveState);
        }

        private void OnDisable()
        {
            _health.OnDead -= HandleDead;
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.Playing)
            {
                _movement?.Move(Vector2.zero);
                _input?.ClearQueuedActions();
                _shoot?.CancelActions();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _movement?.Move(Vector2.zero);
                _input?.ClearQueuedActions();
                _shoot?.CancelActions();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _movement?.Move(Vector2.zero);
                _input?.ClearQueuedActions();
                _shoot?.CancelActions();
            }
        }

        private void FixedUpdate()
        {
            if (_gameState != null && _gameState.CurrentState != GameState.Playing)
            {
                _movement?.Move(Vector2.zero);
                return;
            }

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
            _shoot?.CancelActions();
            ChangeState(_deadState);
            Died?.Invoke();
        }

        public void Respawn(Vector3 position)
        {
            ResetToSpawn(position, transform.rotation);
        }

        public void ResetToSpawn()
        {
            ResetToSpawn(transform.position, transform.rotation);
        }

        public void ResetToSpawn(Vector3 position, Quaternion rotation)
        {
            _movement.Warp(position);
            _rotation.SetRotation(rotation);
            _health.ResetHealth();

            if (_shoot != null)
            {
                _shoot.ResetWeapons();
            }
            else
            {
                WeaponLoadout loadout = GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    loadout.ResetToDefault();
                }
            }

            _input.ResetGameplayTransientState();
            _input.ClearQueuedActions();

            ChangeState(_aliveState);
        }

        public void ApplySettings(GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            if (_input != null)
            {
                _input.AimSensitivity = settings.AimSensitivity;
                _input.MoveDeadzone = settings.Deadzone;
                _input.LookDeadzone = settings.Deadzone;
            }

            if (_rotation != null)
            {
                _rotation.AimSensitivity = settings.AimSensitivity;
            }
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
