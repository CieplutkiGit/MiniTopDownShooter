using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IHighScoreProvider
    {
        int HighScore { get; }
        event Action<int> OnHighScoreChanged;
    }
}
