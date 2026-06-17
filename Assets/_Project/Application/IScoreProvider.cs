using System;

namespace Application
{
    public interface IScoreProvider
    {
        int Score { get; }
        event Action<int> OnScoreChanged;
    }
}
