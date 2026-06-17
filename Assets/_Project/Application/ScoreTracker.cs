using System;

namespace Application
{
    public class ScoreTracker : IScoreProvider
    {
        private int _score;

        public event Action<int> OnScoreChanged;

        public int Score
        {
            get { return _score; }
        }

        public ScoreTracker()
        {
            _score = 0;
        }

        public void Add(int amount)
        {
            _score += amount;
            OnScoreChanged?.Invoke(_score);
        }

        public void Reset()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }
    }
}
