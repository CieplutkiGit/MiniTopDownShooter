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
            if (_currentState == GameState.Menu ||
                _currentState == GameState.GameOver ||
                _currentState == GameState.Victory ||
                _currentState == GameState.DeploymentBriefing ||
                _currentState == GameState.Loading ||
                _currentState == GameState.WorkshopRoaming)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void EnterWorkshopRoaming()
        {
            if (_currentState != GameState.Paused &&
                _currentState != GameState.WorkshopRoaming &&
                (_currentState == GameState.Menu ||
                 _currentState == GameState.WorkshopEditing ||
                 _currentState == GameState.WorkshopFiringRange ||
                 _currentState == GameState.DeploymentBriefing ||
                 _currentState == GameState.Loading ||
                 _currentState == GameState.GameOver ||
                 _currentState == GameState.Victory ||
                 _currentState == GameState.Playing))
            {
                ChangeState(GameState.WorkshopRoaming);
            }
        }

        public void EnterWorkshopEditing()
        {
            if (_currentState == GameState.WorkshopRoaming)
            {
                ChangeState(GameState.WorkshopEditing);
            }
        }

        public void EnterWorkshopFiringRange()
        {
            if (_currentState == GameState.WorkshopRoaming)
            {
                ChangeState(GameState.WorkshopFiringRange);
            }
        }

        public void EnterDeploymentBriefing()
        {
            if (_currentState == GameState.WorkshopRoaming)
            {
                ChangeState(GameState.DeploymentBriefing);
            }
        }

        public void EnterLoading()
        {
            if (_currentState != GameState.WorkshopEditing &&
                _currentState != GameState.WorkshopFiringRange &&
                _currentState != GameState.Loading)
            {
                ChangeState(GameState.Loading);
            }
        }

        public void Pause()
        {
            if (_currentState == GameState.Playing ||
                _currentState == GameState.WorkshopRoaming ||
                _currentState == GameState.WorkshopEditing ||
                _currentState == GameState.WorkshopFiringRange ||
                _currentState == GameState.DeploymentBriefing)
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

        public bool CanResume => _currentState == GameState.Paused;

        public void EndGame()
        {
            if (_currentState == GameState.Playing ||
                (_currentState == GameState.Paused && _previousStateBeforePause == GameState.Playing))
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

        public bool CanEnterState(GameState targetState)
        {
            if (_currentState == targetState) return false;

            if (_currentState == GameState.Paused)
            {
                if (targetState == _previousStateBeforePause) return true;
                if (targetState == GameState.Menu) return true;
                if (targetState == GameState.Loading) return true;
                if (targetState == GameState.GameOver && _previousStateBeforePause == GameState.Playing) return true;
                return false;
            }

            return IsTransitionAllowed(_currentState, targetState);
        }

        public static bool IsTransitionAllowed(GameState from, GameState to)
        {
            if (from == to) return false;

            switch (to)
            {
                case GameState.Playing:
                    return from == GameState.Menu ||
                           from == GameState.GameOver ||
                           from == GameState.Victory ||
                           from == GameState.DeploymentBriefing ||
                           from == GameState.Loading ||
                           from == GameState.WorkshopRoaming;

                case GameState.WorkshopRoaming:
                    return from != GameState.Paused &&
                           from != GameState.WorkshopRoaming;

                case GameState.WorkshopEditing:
                case GameState.WorkshopFiringRange:
                case GameState.DeploymentBriefing:
                    return from == GameState.WorkshopRoaming;

                case GameState.Loading:
                    return from != GameState.WorkshopEditing &&
                           from != GameState.WorkshopFiringRange &&
                           from != GameState.Loading;

                case GameState.Paused:
                    return from == GameState.Playing ||
                           from == GameState.WorkshopRoaming ||
                           from == GameState.WorkshopEditing ||
                           from == GameState.WorkshopFiringRange ||
                           from == GameState.DeploymentBriefing;

                case GameState.GameOver:
                    return from == GameState.Playing ||
                           from == GameState.Paused;

                case GameState.Victory:
                    return from == GameState.Playing;

                case GameState.Menu:
                    return from != GameState.Menu;

                default:
                    return false;
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
