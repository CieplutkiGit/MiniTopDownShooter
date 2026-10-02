using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IScoreProvider
    {
        int Score { get; }
        event Action<int> OnScoreChanged;
    }
}
