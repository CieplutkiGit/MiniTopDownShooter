using Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Workshop
{
    public class WorkshopBenchTrigger : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private string _promptText = "Press E / Tap to Customize";
        [SerializeField] private MobileInputState _mobileInput;

        private IGameStateController _gameState;
        private bool _isPlayerInside;
        private bool _isPromptVisible;

        public bool IsPlayerInside => _isPlayerInside;
        public bool IsPromptVisible => _promptRoot != null ? _promptRoot.activeSelf : _isPromptVisible;
        public string PromptText => _promptText;
        public GameObject PromptRoot => _promptRoot;

        public void Initialize(GameStateController gameState, GameObject promptRoot = null, MobileInputState mobileInput = null)
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }

            _gameStateRef = gameState;
            _gameState = gameState;
            if (promptRoot != null)
            {
                _promptRoot = promptRoot;
            }
            if (mobileInput != null)
            {
                _mobileInput = mobileInput;
            }

            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }

            UpdatePromptVisibility();
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;

            if (_mobileInput == null)
            {
                _mobileInput = FindFirstObjectByType<MobileInputState>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged += HandleStateChanged;
            }
            UpdatePromptVisibility();
        }

        private void OnDisable()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= HandleStateChanged;
            }
            SetPromptActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                _isPlayerInside = true;
                UpdatePromptVisibility();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                _isPlayerInside = false;
                UpdatePromptVisibility();

                if (_gameState != null && _gameState.CurrentState == GameState.WorkshopEditing)
                {
                    _gameState.EnterWorkshopRoaming();
                }
            }
        }

        public void Interact()
        {
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopRoaming)
            {
                _gameState.EnterWorkshopEditing();
                SetPromptActive(false);
            }
        }

        public void ExitBench()
        {
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopEditing)
            {
                _gameState.EnterWorkshopRoaming();
                UpdatePromptVisibility();
            }
        }

        private void Update()
        {
            if (!_isPlayerInside || _gameState == null || _gameState.CurrentState != GameState.WorkshopRoaming)
            {
                return;
            }

            bool interactPressed = false;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            else if (_mobileInput != null && _mobileInput.ConsumeInteractPressed())
            {
                interactPressed = true;
            }

            if (interactPressed)
            {
                Interact();
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            UpdatePromptVisibility();
        }

        private void UpdatePromptVisibility()
        {
            bool shouldShow = _isPlayerInside && _gameState != null && _gameState.CurrentState == GameState.WorkshopRoaming;
            SetPromptActive(shouldShow);
        }

        private void SetPromptActive(bool active)
        {
            _isPromptVisible = active;
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(active);
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.GetComponent<PlayerController>() != null ||
                   other.GetComponentInParent<PlayerController>() != null ||
                   other.CompareTag("Player");
        }
    }
}
