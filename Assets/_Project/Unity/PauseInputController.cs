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

        private void Awake()
        {
            _input = new GameInput();
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
        }

        private void OnDisable()
        {
            _input.Player.Pause.performed -= OnPausePerformed;
            _input.Disable();
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

        private void TogglePause()
        {
            if (_gameState == null)
            {
                return;
            }

            GameState current = _gameState.CurrentState;

            if (current == GameState.Playing)
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
