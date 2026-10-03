using Application;
using Application.Workshop;
using UnityEngine;

namespace Game
{
    public class TimeController : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameStateRef;

        private IGameStateProvider _gameState;
        private bool _isSubscribed;

        public void Initialize(GameStateController gameState)
        {
            UnsubscribeEvents();

            _gameStateRef = gameState;
            _gameState = gameState;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
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
            SubscribeEvents();
            if (_gameState != null)
            {
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
            UnsubscribeEvents();
            Time.timeScale = 1f;
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            ApplyTimeScale(newState);
        }

        private void ApplyTimeScale(GameState state)
        {
            if (GameActivityPolicy.IsTimeFrozen(state))
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
