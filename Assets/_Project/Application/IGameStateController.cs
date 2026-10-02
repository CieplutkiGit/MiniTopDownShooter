namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IGameStateController : IGameStateProvider
    {
        void StartGame();
        void Pause();
        void Resume();
        void ReturnToMenu();
    }
}
