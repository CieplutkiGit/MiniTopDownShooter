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
        private bool _isSubscribed;

        public void Initialize(GameStateController gameState, WorldResetManager resetManager)
        {
            UnsubscribeEvents();

            _gameStateRef = gameState;
            _resetManager = resetManager;
            _gameState = gameState;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
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
            SubscribeEvents();

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
            UnsubscribeEvents();

            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayClicked);
            }
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged -= HandleStateChanged;
            _isSubscribed = false;
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

            bool show = state == GameState.Menu;
            if (_panel == gameObject)
            {
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = show ? 1f : 0f;
                cg.interactable = show;
                cg.blocksRaycasts = show;
            }
            else
            {
                _panel.SetActive(show);
            }
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
