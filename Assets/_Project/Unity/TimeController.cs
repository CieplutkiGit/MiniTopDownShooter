using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
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
            if (newState == GameState.Paused || newState == GameState.Menu)
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
