using Application;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class MainMenuUI : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private WorldResetManager _resetManager;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _playButton;

        private IGameStateProvider _gameState;

        public void Initialize(GameStateController gameState, WorldResetManager resetManager)
        {
            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _gameStateRef = gameState;
            _resetManager = resetManager;
            _gameState = gameState;

            if (_gameState != null && enabled)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_resetManager == null)
            {
                _resetManager = FindFirstObjectByType<WorldResetManager>();
            }

            _gameState = _gameStateRef;
        }

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
