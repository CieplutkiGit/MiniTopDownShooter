using Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class PauseInputController : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameState;

        private GameInput _input;

        private void Awake()
        {
            _input = new GameInput();
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

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Dispose();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
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
