using System;

namespace Application
{
    public class GameStateManager
    {
        private GameState _currentState;

        public event Action<GameState> OnStateChanged;

        public GameState CurrentState
        {
            get { return _currentState; }
        }

        public GameStateManager()
        {
            _currentState = GameState.Menu;
        }

        public void StartGame()
        {
            if (_currentState == GameState.Menu || _currentState == GameState.GameOver)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void Pause()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void Resume()
        {
            if (_currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void EndGame()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.GameOver);
            }
        }

        public void ReturnToMenu()
        {
            if (_currentState != GameState.Menu)
            {
                ChangeState(GameState.Menu);
            }
        }

        private void ChangeState(GameState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
