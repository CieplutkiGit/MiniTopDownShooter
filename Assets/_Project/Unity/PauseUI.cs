using Application;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class PauseUI : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _menuButton;

        private IGameStateController _gameState;
        private bool _isSubscribed;

        public void Initialize(GameStateController gameState)
        {
            UnsubscribeEvents();

            _gameStateRef = gameState;
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

            _gameState = _gameStateRef;
        }

        private void OnEnable()
        {
            SubscribeEvents();

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(HandleMenuClicked);
            }

            SetPanelActive(false);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(HandleMenuClicked);
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

        private void SetPanelActive(bool active)
        {
            if (_panel == null)
            {
                return;
            }

            if (_panel == gameObject)
            {
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = active ? 1f : 0f;
                cg.interactable = active;
                cg.blocksRaycasts = active;
            }
            else
            {
                _panel.SetActive(active);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (_panel == null)
            {
                return;
            }

            bool show = newState == GameState.Paused;
            SetPanelActive(show);
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
