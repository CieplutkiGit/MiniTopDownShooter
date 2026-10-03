using System;

namespace Application.Flow
{
    public class GameFlowCoordinator : IGameFlowCoordinator
    {
        private GameFlowState _flowState;
        private RunSession _activeRun;
        private RunResult _lastFinalizedResult;
        private int _activeTransitionToken;
        private int _tokenCounter;

        public GameFlowState FlowState => _flowState;
        public RunSession ActiveRun => _activeRun;
        public RunResult LastFinalizedResult => _lastFinalizedResult;
        public int ActiveTransitionToken => _activeTransitionToken;

        public bool CanDeploy => (_flowState == GameFlowState.InHub ||
                                  _flowState == GameFlowState.Deploying) &&
                                 _activeTransitionToken == 0;

        public bool CanReturnToBase => (_flowState == GameFlowState.MissionResults ||
                                        _flowState == GameFlowState.InMission ||
                                        _flowState == GameFlowState.Deploying ||
                                        _flowState == GameFlowState.ReturningToBase) &&
                                       _activeTransitionToken == 0;

        public event Action<GameFlowState, GameFlowState> OnFlowStateChanged;
        public event Action<RunSession> OnRunStarted;
        public event Action<RunResult> OnRunCompleted;

        public GameFlowCoordinator(GameFlowState initialState = GameFlowState.InHub)
        {
            _flowState = initialState;
        }

        public bool TryDeploy(DeployCommand command, out int transitionToken)
        {
            transitionToken = 0;
            if (!CanDeploy || command == null)
            {
                return false;
            }

            _activeTransitionToken = ++_tokenCounter;
            transitionToken = _activeTransitionToken;

            string runId = Guid.NewGuid().ToString("N");
            _activeRun = new RunSession(runId, command.MissionId, command.Loadout);

            SetFlowState(GameFlowState.Deploying);
            OnRunStarted?.Invoke(_activeRun);
            return true;
        }

        public bool TryDeploy(DeployCommand command)
        {
            return TryDeploy(command, out _);
        }

        public bool TryDeploy(string missionId, DeploymentLoadoutSnapshot loadout = null)
        {
            if (string.IsNullOrWhiteSpace(missionId)) return false;
            return TryDeploy(new DeployCommand(missionId, loadout));
        }

        public bool NotifyMissionLoaded(int transitionToken)
        {
            if (transitionToken == 0 || transitionToken != _activeTransitionToken || _flowState != GameFlowState.Deploying)
            {
                return false;
            }

            SetFlowState(GameFlowState.InMission);
            return true;
        }

        public bool NotifyMissionStarted(int transitionToken)
        {
            if (transitionToken == 0 || transitionToken != _activeTransitionToken ||
                (_flowState != GameFlowState.InMission && _flowState != GameFlowState.MissionResults))
                return false;
            _activeTransitionToken = 0;
            return true;
        }

        public bool NotifyBaseLoaded(int transitionToken)
        {
            if (transitionToken == 0 || transitionToken != _activeTransitionToken || _flowState != GameFlowState.ReturningToBase)
            {
                return false;
            }

            _activeTransitionToken = 0;
            _activeRun = null;
            SetFlowState(GameFlowState.InHub);
            return true;
        }

        public bool NotifyTransitionFailed(int transitionToken, string error = null)
        {
            if (transitionToken == 0 || transitionToken != _activeTransitionToken)
            {
                return false;
            }

            if (_flowState == GameFlowState.Deploying)
            {
                if (_activeRun != null && !_activeRun.IsCompleted)
                {
                    _activeRun.TryComplete(RunOutcome.TechnicalError);
                    _lastFinalizedResult = _activeRun.FinalResult;
                    OnRunCompleted?.Invoke(_lastFinalizedResult);
                }

                _activeTransitionToken = 0;
                SetFlowState(GameFlowState.InHub);
                return true;
            }

            if (_flowState == GameFlowState.ReturningToBase)
            {
                _activeTransitionToken = 0;
                return true;
            }

            if (_flowState == GameFlowState.InMission)
            {
                if (_activeRun != null && !_activeRun.IsCompleted)
                {
                    _activeRun.TryComplete(RunOutcome.TechnicalError);
                    _lastFinalizedResult = _activeRun.FinalResult;
                    OnRunCompleted?.Invoke(_lastFinalizedResult);
                }
                _activeTransitionToken = 0;
                SetFlowState(GameFlowState.InHub);
                return true;
            }

            if (_flowState == GameFlowState.MissionResults)
            {
                _activeTransitionToken = 0;
                SetFlowState(GameFlowState.InHub);
                return true;
            }

            return false;
        }

        public bool TryReportRunOutcome(RunOutcome outcome, int score = 0, int kills = 0, int wavesCleared = 0)
        {
            if (_flowState != GameFlowState.InMission || _activeRun == null || outcome == RunOutcome.None)
            {
                return false;
            }

            if (_activeRun.IsCompleted)
            {
                // Terminal-result idempotence: cannot overwrite an already-completed run outcome!
                return false;
            }

            bool completed = _activeRun.TryComplete(outcome, score, kills, wavesCleared);
            if (!completed)
            {
                return false;
            }

            _lastFinalizedResult = _activeRun.FinalResult;
            SetFlowState(GameFlowState.MissionResults);
            OnRunCompleted?.Invoke(_lastFinalizedResult);
            return true;
        }

        public bool TryReturnToBase(ReturnToBaseCommand command, out int transitionToken)
        {
            transitionToken = 0;
            if (!CanReturnToBase)
            {
                return false;
            }

            // If returning directly from InMission (abandon)
            if (_flowState == GameFlowState.InMission)
            {
                if (_activeRun != null && !_activeRun.IsCompleted)
                {
                    _activeRun.TryComplete(RunOutcome.Abandoned);
                    _lastFinalizedResult = _activeRun.FinalResult;
                    OnRunCompleted?.Invoke(_lastFinalizedResult);
                }
            }

            _activeTransitionToken = ++_tokenCounter;
            transitionToken = _activeTransitionToken;
            SetFlowState(GameFlowState.ReturningToBase);
            return true;
        }

        public bool TryReturnToBase(ReturnToBaseCommand command = null)
        {
            return TryReturnToBase(command ?? ReturnToBaseCommand.Default, out _);
        }

        public void StartDirectMission(string missionId, DeploymentLoadoutSnapshot loadout = null)
        {
            string runId = Guid.NewGuid().ToString("N");
            _activeRun = new RunSession(runId, missionId ?? "direct_arena", loadout);
            _activeTransitionToken = 0;
            SetFlowState(GameFlowState.InMission);
            OnRunStarted?.Invoke(_activeRun);
        }

        private void SetFlowState(GameFlowState newState)
        {
            if (_flowState == newState) return;
            GameFlowState oldState = _flowState;
            _flowState = newState;
            OnFlowStateChanged?.Invoke(oldState, newState);
        }
    }
}
