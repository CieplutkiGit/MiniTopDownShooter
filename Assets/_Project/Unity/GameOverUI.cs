using Application;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class GameOverUI : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private WorldResetManager _resetManager;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private IGameStateController _gameState;

        private void Awake()
        {
            _gameState = _gameStateRef;
        }

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

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
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

            bool show = newState == GameState.GameOver;
            _panel.SetActive(show);
        }

        private void HandleRestartClicked()
        {
            if (_resetManager == null)
            {
                return;
            }

            _resetManager.Restart();
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
