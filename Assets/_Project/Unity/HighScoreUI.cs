using Application;
using TMPro;
using UnityEngine;

namespace Game
{
    public class HighScoreUI : MonoBehaviour
    {
        [SerializeField] private HighScoreController _highScoreRef;
        [SerializeField] private TMP_Text _label;

        private IHighScoreProvider _highScore;
        private bool _isSubscribed;

        public void Initialize(HighScoreController highScore)
        {
            UnsubscribeEvents();
            _highScoreRef = highScore;
            _highScore = highScore;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_highScoreRef == null)
            {
                _highScoreRef = FindFirstObjectByType<HighScoreController>();
            }

            _highScore = _highScoreRef;
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
            if (_isSubscribed || _highScore == null)
            {
                return;
            }

            _highScore.OnHighScoreChanged += HandleHighScoreChanged;
            _isSubscribed = true;
            UpdateLabel(_highScore.HighScore);
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _highScore == null)
            {
                return;
            }

            _highScore.OnHighScoreChanged -= HandleHighScoreChanged;
            _isSubscribed = false;
        }

        private void HandleHighScoreChanged(int newHighScore)
        {
            UpdateLabel(newHighScore);
        }

        private void UpdateLabel(int newHighScore)
        {
            if (_label == null)
            {
                return;
            }

            _label.text = $"Best {newHighScore}";
        }
    }
}
