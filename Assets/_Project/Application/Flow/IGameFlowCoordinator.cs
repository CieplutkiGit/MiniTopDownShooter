using System;

namespace Application.Flow
{
    public interface IGameFlowCoordinator
    {
        GameFlowState FlowState { get; }
        RunSession ActiveRun { get; }
        RunResult LastFinalizedResult { get; }
        int ActiveTransitionToken { get; }

        bool CanDeploy { get; }
        bool CanReturnToBase { get; }

        event Action<GameFlowState, GameFlowState> OnFlowStateChanged;
        event Action<RunSession> OnRunStarted;
        event Action<RunResult> OnRunCompleted;

        bool TryDeploy(DeployCommand command, out int transitionToken);
        bool TryDeploy(DeployCommand command);
        bool TryDeploy(string missionId, DeploymentLoadoutSnapshot loadout = null);

        bool NotifyMissionLoaded(int transitionToken);
        bool NotifyMissionStarted(int transitionToken);
        bool NotifyBaseLoaded(int transitionToken);
        bool NotifyTransitionFailed(int transitionToken, string error = null);

        bool TryReportRunOutcome(RunOutcome outcome, int score = 0, int kills = 0, int wavesCleared = 0);

        bool TryReturnToBase(ReturnToBaseCommand command, out int transitionToken);
        bool TryReturnToBase(ReturnToBaseCommand command = null);

        void StartDirectMission(string missionId, DeploymentLoadoutSnapshot loadout = null);
    }
}
