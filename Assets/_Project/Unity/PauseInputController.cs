using Application;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Game
{
    public class PauseInputController : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private MobileInputState _mobileInput;

        private GameInput _input;
        private IGameStateController _gameState;

        public void Initialize(GameStateController gameState, MobileInputState mobileInput = null)
        {
            _gameStateRef = gameState;
            _gameState = gameState;
            if (mobileInput != null)
            {
                _mobileInput = mobileInput;
            }
        }

        private void Awake()
        {
            _input = new GameInput();
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;

            if (_mobileInput == null)
            {
                _mobileInput = FindFirstObjectByType<MobileInputState>(
                    FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            _input.Enable();
            _input.Player.Pause.performed += OnPausePerformed;
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private void OnDisable()
        {
            _input.Player.Pause.performed -= OnPausePerformed;
            InputSystem.onDeviceChange -= HandleDeviceChange;
            _input.Disable();
        }

        private static bool CanPauseFrom(GameState state)
        {
            return state == GameState.Playing ||
                   state == GameState.WorkshopRoaming ||
                   state == GameState.WorkshopEditing ||
                   state == GameState.WorkshopFiringRange;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _gameState != null && CanPauseFrom(_gameState.CurrentState))
            {
                _gameState.Pause();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _gameState != null && CanPauseFrom(_gameState.CurrentState))
            {
                _gameState.Pause();
            }
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Disconnected && _gameState != null && CanPauseFrom(_gameState.CurrentState))
            {
                _gameState.Pause();
            }
        }

        private void Update()
        {
            if (_mobileInput != null &&
                _mobileInput.ConsumePausePressed())
            {
                TogglePause();
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Dispose();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            if (_gameState == null)
            {
                if (_gameStateRef != null)
                {
                    _gameState = _gameStateRef;
                }
                else
                {
                    _gameState = FindFirstObjectByType<GameStateController>(FindObjectsInactive.Include);
                }
            }

            if (_gameState == null)
            {
                return;
            }

            GameState current = _gameState.CurrentState;

            if (CanPauseFrom(current))
            {
                _gameState.Pause();
            }
            else if (current == GameState.Paused)
            {
                _gameState.Resume();
            }
        }
    }
}
