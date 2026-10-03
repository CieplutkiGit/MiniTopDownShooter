namespace Application
{
    public interface IGameStateController : IGameStateProvider
    {
        void StartGame();
        void Pause();
        void Resume();
        void ReturnToMenu();
        void TriggerVictory();
        void EndGame();
        void EnterWorkshopRoaming();
        void EnterWorkshopEditing();
        void EnterWorkshopFiringRange();
        void EnterDeploymentBriefing();
        void EnterLoading();
        bool CanEnterState(GameState targetState);
        bool CanResume { get; }
    }
}
