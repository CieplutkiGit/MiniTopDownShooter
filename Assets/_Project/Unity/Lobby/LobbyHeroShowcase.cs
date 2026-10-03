using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Lobby
{
    /// <summary>
    /// Showcase controller for the player character in the Lobby (BaseHub) scene.
    /// Freezes gameplay movement and shooting so the character stands proudly facing front to the camera,
    /// and allows interactive horizontal turntable dragging to rotate and inspect the hero and weapon in 3D.
    /// </summary>
    [ExecuteAlways]
    public class LobbyHeroShowcase : MonoBehaviour
    {
        [Header("Orientation")]
        [Tooltip("Direction the hero faces by default (towards the camera).")]
        [SerializeField] private Vector3 _defaultFacingDirection = new Vector3(0, 0, -1);
        [SerializeField] private float _turnSensitivity = 0.4f;
        [SerializeField] private float _inertiaDamping = 6f;

        private PlayerMovement _movement;
        private PlayerShoot _shoot;
        private PlayerRotation _rotation;
        private Rigidbody _rigidbody;

        private float _currentYaw = 180f; // Facing -Z
        private float _yawVelocity = 0f;
        private bool _isDragging = false;
        private bool _pressOriginatedOnUI;
        private int _activePointerId = int.MinValue;
        private Vector2 _lastPointerPos;
        private static readonly System.Collections.Generic.List<RaycastResult> s_RaycastResults =
            new System.Collections.Generic.List<RaycastResult>();

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _shoot = GetComponent<PlayerShoot>();
            _rotation = GetComponent<PlayerRotation>();
            _rigidbody = GetComponent<Rigidbody>();

            FreezeLobbyHero();

            if (_defaultFacingDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(_defaultFacingDirection);
                _currentYaw = targetRot.eulerAngles.y;
                transform.rotation = targetRot;
            }
        }

        private void OnEnable()
        {
            FreezeLobbyHero();
            if (UnityEngine.Application.isPlaying)
            {
                try { EnhancedTouchSupport.Enable(); } catch { }
            }
        }

        private void OnDisable()
        {
            _isDragging = false;
            _pressOriginatedOnUI = false;
            _activePointerId = int.MinValue;
        }

        private void Start()
        {
            FreezeLobbyHero();
        }

        public void FreezeLobbyHero()
        {
            transform.position = Vector3.zero;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_movement == null)
                _movement = GetComponent<PlayerMovement>();

            if (_rotation == null)
                _rotation = GetComponent<PlayerRotation>();

            if (_shoot == null)
                _shoot = GetComponent<PlayerShoot>();

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                _rigidbody.position = Vector3.zero;
            }

            if (_movement != null)
                _movement.enabled = false;

            if (_rotation != null)
                _rotation.enabled = false;
        }

        private void FixedUpdate()
        {
            if (!UnityEngine.Application.isPlaying) return;

            // Lock position firmly at origin in physics loop
            transform.position = Vector3.zero;
            if (_rigidbody != null)
            {
                _rigidbody.position = Vector3.zero;
            }
        }

        private void Update()
        {
            if (!UnityEngine.Application.isPlaying) return;

            transform.position = Vector3.zero;
            if (_rigidbody != null)
            {
                _rigidbody.position = Vector3.zero;
            }

            if (_shoot != null)
                _shoot.CancelActions();

            HandleDragInput();

            // Inertia rotation
            if (!_isDragging && Mathf.Abs(_yawVelocity) > 0.01f)
            {
                _currentYaw += _yawVelocity * Time.unscaledDeltaTime;
                _yawVelocity = Mathf.Lerp(_yawVelocity, 0f, Time.unscaledDeltaTime * _inertiaDamping);
                ApplyYaw();
            }
        }

        private void HandleDragInput()
        {
            // Touch input
            if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
            {
                if (Touch.activeTouches.Count > 1)
                {
                    _isDragging = false;
                    _activePointerId = int.MinValue;
                    return;
                }

                var touch = Touch.activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _activePointerId = touch.touchId;
                    _pressOriginatedOnUI = IsPointerOverUI(touch.screenPosition);
                    _isDragging = !_pressOriginatedOnUI;
                    _lastPointerPos = touch.screenPosition;
                    _yawVelocity = 0f;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _isDragging &&
                    !_pressOriginatedOnUI && touch.touchId == _activePointerId)
                {
                    float deltaX = touch.delta.x;
                    _lastPointerPos = touch.screenPosition;
                    ApplyDeltaYaw(deltaX);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _isDragging = false;
                    _pressOriginatedOnUI = false;
                    _activePointerId = int.MinValue;
                }
                return;
            }

            // Mouse input
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    _lastPointerPos = mouse.position.ReadValue();
                    _pressOriginatedOnUI = IsPointerOverUI(_lastPointerPos);
                    _isDragging = !_pressOriginatedOnUI;
                    _activePointerId = -1;
                    _yawVelocity = 0f;
                }
                else if (mouse.leftButton.isPressed && _isDragging && !_pressOriginatedOnUI && _activePointerId == -1)
                {
                    Vector2 currentPos = mouse.position.ReadValue();
                    float deltaX = currentPos.x - _lastPointerPos.x;
                    _lastPointerPos = currentPos;
                    ApplyDeltaYaw(deltaX);
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    _isDragging = false;
                    _pressOriginatedOnUI = false;
                    _activePointerId = int.MinValue;
                }
            }
        }

        private void ApplyDeltaYaw(float deltaX)
        {
            float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float yawDelta = -deltaX * _turnSensitivity;
            _currentYaw += yawDelta;
            _yawVelocity = (yawDelta / dt) * 0.15f;
            ApplyYaw();
        }

        private void ApplyYaw()
        {
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        }

        public void ResetFacing()
        {
            if (_defaultFacingDirection != Vector3.zero)
            {
                _currentYaw = Quaternion.LookRotation(_defaultFacingDirection).eulerAngles.y;
                _yawVelocity = 0f;
                ApplyYaw();
            }
        }

        private static bool IsPointerOverUI(Vector2 screenPosition)
        {
            return InputReader.IsPointerOverUI(screenPosition);
        }
    }
}
