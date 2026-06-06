using Application;
using UnityEngine;

namespace Game
{
    public class TimeController : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameStateRef;

        private IGameStateProvider _gameState;

        private void Awake()
        {
            _gameState = _gameStateRef;
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            Time.timeScale = 1f;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Paused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }
    }
}
