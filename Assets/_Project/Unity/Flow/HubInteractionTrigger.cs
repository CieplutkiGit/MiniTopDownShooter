using System;
using Application;
using Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Flow
{
    /// <summary>
    /// Interaction trigger for Hub objects (Workshop Bench, Deployment Terminal, etc.).
    /// Shows prompt only while the player is in range during WorkshopRoaming.
    /// Resets transient state on disable. Does not auto-open briefings/panels on trigger overlap.
    /// Supports interaction via keyboard (E), player interact input, and UI prompt buttons.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HubInteractionTrigger : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GameStateController _gameState;
        [SerializeField] private GameState _activeState = GameState.WorkshopRoaming;

        [Header("UI Prompt")]
        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private Button _promptButton;

        [Header("Terminal Target (Optional)")]
        [SerializeField] private DeploymentTerminal _deploymentTerminal;

        [Header("Workshop Bench (Optional)")]
        [SerializeField] private Game.Workshop.WorkshopBenchTrigger _workshopBench;

        [Header("Events")]
        [SerializeField] private UnityEvent _onInteract;

        private bool _isPlayerInRange;
        private PlayerController _player;

        public bool IsPlayerInRange => _isPlayerInRange;
        public GameObject PromptRoot => _promptRoot;
        public Button PromptButton => _promptButton;
        public DeploymentTerminal DeploymentTerminal => _deploymentTerminal;
        public Game.Workshop.WorkshopBenchTrigger WorkshopBench => _workshopBench;
        public UnityEvent OnInteract => _onInteract;

        public void Initialize(GameStateController gameState)
        {
            if (_gameState != null)
                _gameState.OnStateChanged -= HandleStateChanged;

            _gameState = gameState;

            if (_gameState != null)
                _gameState.OnStateChanged += HandleStateChanged;

            UpdatePromptVisibility();
        }

        protected virtual void Awake()
        {
            ResolveReferences();

            if (_promptButton != null)
                _promptButton.onClick.AddListener(HandleButtonClicked);
        }

        protected virtual void OnEnable()
        {
            ResolveReferences();

            if (_gameState != null)
                _gameState.OnStateChanged += HandleStateChanged;

            UpdatePromptVisibility();
        }

        protected virtual void OnDisable()
        {
            if (_gameState != null)
                _gameState.OnStateChanged -= HandleStateChanged;

            _isPlayerInRange = false;
            _player = null;

            if (_promptRoot != null)
                _promptRoot.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (_promptButton != null)
                _promptButton.onClick.RemoveListener(HandleButtonClicked);

            if (_gameState != null)
                _gameState.OnStateChanged -= HandleStateChanged;
        }

        private void ResolveReferences()
        {
            if (_gameState == null)
                _gameState = SceneComponents.Find<GameStateController>(gameObject.scene);

            if (_gameState == null)
                _gameState = FindFirstObjectByType<GameStateController>();

            if (_deploymentTerminal == null)
                _deploymentTerminal = GetComponent<DeploymentTerminal>();

            if (_workshopBench == null)
                _workshopBench = GetComponent<Game.Workshop.WorkshopBenchTrigger>();
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                _player = player;
                _isPlayerInRange = true;
                UpdatePromptVisibility();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                player = other.GetComponent<PlayerController>();

            if (player != null && (_player == null || player == _player))
            {
                _isPlayerInRange = false;
                _player = null;
                UpdatePromptVisibility();
            }
        }

        private void Update()
        {
            if (!_isPlayerInRange)
                return;

            if (_gameState != null && _gameState.CurrentState != _activeState)
            {
                if (_promptRoot != null && _promptRoot.activeSelf)
                    _promptRoot.SetActive(false);
                return;
            }

            bool interactPressed = false;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            if (!interactPressed && _player != null && _player.Input != null)
            {
                interactPressed = _player.Input.ConsumeInteractPressed();
            }

            if (interactPressed)
            {
                TryInteract();
            }
        }

        public virtual bool TryInteract()
        {
            if (!_isPlayerInRange)
                return false;

            if (_gameState != null && (_gameState.CurrentState != GameState.WorkshopRoaming || _gameState.CurrentState != _activeState))
                return false;

            if (_deploymentTerminal != null)
            {
                _deploymentTerminal.OpenBriefing();
            }

            if (_workshopBench != null)
            {
                _workshopBench.Interact();
            }

            _onInteract?.Invoke();

            UpdatePromptVisibility();
            return true;
        }

        private void HandleButtonClicked()
        {
            TryInteract();
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            UpdatePromptVisibility();
        }

        private void UpdatePromptVisibility()
        {
            bool shouldShow = _isPlayerInRange && (_gameState == null || _gameState.CurrentState == _activeState);
            if (_promptRoot != null)
                _promptRoot.SetActive(shouldShow);
        }
    }

}
