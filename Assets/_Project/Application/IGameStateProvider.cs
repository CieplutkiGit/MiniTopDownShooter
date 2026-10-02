using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IGameStateProvider
    {
        GameState CurrentState { get; }
        event Action<GameState, GameState> OnStateChanged;
    }
}
