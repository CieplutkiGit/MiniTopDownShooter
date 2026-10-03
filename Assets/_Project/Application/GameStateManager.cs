using System;

namespace Application
{
    public class GameStateManager
    {
        private GameState _currentState;
        private GameState _previousStateBeforePause = GameState.Playing;

        public event Action<GameState, GameState> OnStateChanged;

        public GameState CurrentState
        {
            get { return _currentState; }
        }

        public GameState PreviousStateBeforePause => _previousStateBeforePause;

        public GameStateManager()
        {
            _currentState = GameState.Menu;
        }

        public void StartGame()
        {
            if (_currentState == GameState.Menu || _currentState == GameState.GameOver || _currentState == GameState.Victory)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void EnterWorkshopRoaming()
        {
            if (_currentState != GameState.Paused)
            {
                ChangeState(GameState.WorkshopRoaming);
            }
        }

        public void EnterWorkshopEditing()
        {
            if (_currentState != GameState.Paused)
            {
                ChangeState(GameState.WorkshopEditing);
            }
        }

        public void EnterWorkshopFiringRange()
        {
            if (_currentState != GameState.Paused)
            {
                ChangeState(GameState.WorkshopFiringRange);
            }
        }

        public void Pause()
        {
            if (_currentState == GameState.Playing ||
                _currentState == GameState.WorkshopRoaming ||
                _currentState == GameState.WorkshopEditing ||
                _currentState == GameState.WorkshopFiringRange)
            {
                _previousStateBeforePause = _currentState;
                ChangeState(GameState.Paused);
            }
        }

        public void Resume()
        {
            if (_currentState == GameState.Paused)
            {
                ChangeState(_previousStateBeforePause);
            }
        }

        public void EndGame()
        {
            if (_currentState == GameState.Playing ||
                _currentState == GameState.Paused ||
                _currentState == GameState.WorkshopRoaming ||
                _currentState == GameState.WorkshopEditing ||
                _currentState == GameState.WorkshopFiringRange)
            {
                ChangeState(GameState.GameOver);
            }
        }

        public void TriggerVictory()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Victory);
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

            GameState oldState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }
    }
}
