using Application;
using UnityEngine;

namespace Game
{
    public class TimeController : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameStateRef;

        private IGameStateProvider _gameState;

        public void Initialize(GameStateController gameState)
        {
            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _gameStateRef = gameState;
            _gameState = gameState;

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleStateChanged;
                ApplyTimeScale(_gameState.CurrentState);
            }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
                ApplyTimeScale(_gameState.CurrentState);
            }
        }

        private void Start()
        {
            if (_gameState != null)
            {
                ApplyTimeScale(_gameState.CurrentState);
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
            ApplyTimeScale(newState);
        }

        private void ApplyTimeScale(GameState state)
        {
            if (state == GameState.Paused || state == GameState.Menu || state == GameState.Victory)
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
