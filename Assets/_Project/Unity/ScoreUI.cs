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
        private bool _isSubscribed;

        public void Initialize(ScoreController score)
        {
            UnsubscribeEvents();
            _scoreRef = score;
            _score = score;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
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
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _score == null)
            {
                return;
            }

            _score.OnScoreChanged += HandleScoreChanged;
            _isSubscribed = true;
            UpdateLabel(_score.Score);
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _score == null)
            {
                return;
            }

            _score.OnScoreChanged -= HandleScoreChanged;
            _isSubscribed = false;
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
