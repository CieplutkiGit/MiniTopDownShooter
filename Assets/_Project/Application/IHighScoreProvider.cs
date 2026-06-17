using System;

namespace Application
{
    public interface IHighScoreProvider
    {
        int HighScore { get; }
        event Action<int> OnHighScoreChanged;
    }
}
