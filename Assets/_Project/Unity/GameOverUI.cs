using Application;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameState;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _restartButton;

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClicked);
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

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            if (_panel == null)
            {
                return;
            }

            bool show = newState == GameState.GameOver;
            _panel.SetActive(show);
        }

        private void HandleRestartClicked()
        {
            if (_gameState == null)
            {
                return;
            }

            // TODO: world reset (player HP, clear enemies, respawn) - wires up in restart manager step
            _gameState.StartGame();
        }
    }
}
