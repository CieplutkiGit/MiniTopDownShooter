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

        private GameInput _input;
        private IGameStateController _gameState;

        private void Awake()
        {
            _input = new GameInput();
            _gameState = _gameStateRef;
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
