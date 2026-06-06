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

        private void Awake()
        {
            _highScore = _highScoreRef;
        }

        private void OnEnable()
        {
            if (_highScore == null)
            {
                return;
            }

            _highScore.OnHighScoreChanged += HandleHighScoreChanged;
            UpdateLabel(_highScore.HighScore);
        }

        private void OnDisable()
        {
            if (_highScore == null)
            {
                return;
            }

            _highScore.OnHighScoreChanged -= HandleHighScoreChanged;
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
