using Application;
using UnityEngine;

namespace Game
{
    public class GameStateAudio : AudioListenerBase
    {
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private AudioClip _gameOverClip;

        private IGameStateProvider _gameState;

        private void Awake()
        {
            _gameState = _gameStateRef;
        }

        private void OnEnable()
        {
            if (_gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (_gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.GameOver)
            {
                return;
            }

            PlayClip(_gameOverClip);
        }
    }
}
