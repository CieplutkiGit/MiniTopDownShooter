using Application;
using TMPro;
using UnityEngine;

namespace Game
{
    public class ScoreUI : MonoBehaviour
    {
        [SerializeField] private ScoreController _scoreRef;
        [SerializeField] private TMP_Text _label;

        private IScoreProvider _score;

        public void Initialize(ScoreController score)
        {
            _scoreRef = score;
            _score = score;
        }

        private void Awake()
        {
            if (_scoreRef == null)
            {
                _scoreRef = FindFirstObjectByType<ScoreController>();
            }

            _score = _scoreRef;
        }

        private void OnEnable()
        {
            if (_score == null)
            {
                return;
            }

            _score.OnScoreChanged += HandleScoreChanged;
            UpdateLabel(_score.Score);
        }

        private void OnDisable()
        {
            if (_score == null)
            {
                return;
            }

            _score.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int newScore)
        {
            UpdateLabel(newScore);
        }

        private void UpdateLabel(int newScore)
        {
            if (_label == null)
            {
                return;
            }

            _label.text = $"Score {newScore}";
        }
    }
}
