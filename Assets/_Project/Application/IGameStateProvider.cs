using System;

namespace Application
{
    public interface IGameStateProvider
    {
        GameState CurrentState { get; }
        event Action<GameState, GameState> OnStateChanged;
    }
}
