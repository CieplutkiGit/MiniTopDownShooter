using Application;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameState;
        [SerializeField] private WorldResetManager _resetManager;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _playButton;

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(HandlePlayClicked);
            }

            GameState initial = GameState.Menu;
            if (_gameState != null)
            {
                initial = _gameState.CurrentState;
            }

            UpdateVisibility(initial);
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayClicked);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            UpdateVisibility(newState);
        }

        private void UpdateVisibility(GameState state)
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(state == GameState.Menu);
        }

        private void HandlePlayClicked()
        {
            if (_resetManager == null)
            {
                return;
            }

            _resetManager.Restart();
        }
    }
}
