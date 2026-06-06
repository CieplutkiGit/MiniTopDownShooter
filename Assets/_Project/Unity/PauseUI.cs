using Application;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameState;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _menuButton;

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(HandleMenuClicked);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(HandleMenuClicked);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_panel == null)
            {
                return;
            }

            bool show = newState == GameState.Paused;
            _panel.SetActive(show);
        }

        private void HandleResumeClicked()
        {
            if (_gameState == null)
            {
                return;
            }

            _gameState.Resume();
        }

        private void HandleMenuClicked()
        {
            if (_gameState == null)
            {
                return;
            }

            _gameState.ReturnToMenu();
        }
    }
}
