using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Workshop
{
    /// <summary>
    /// Interactive 3D weapon inspection controller for the WeaponEdit scene.
    /// Provides smooth single-touch / mouse drag orbit rotation (pitch and yaw with inertia),
    /// two-finger pinch-to-zoom (and mouse scrollwheel zoom), and view reset.
    /// Uses Unity InputSystem without legacy UnityEngine.Input calls.
    /// </summary>
    public class WeaponPinchRotateController : MonoBehaviour
    {
        [Header("Target & Camera")]
        [Tooltip("Transform that rotates when dragged (turntable / weapon mount).")]
        [SerializeField] private Transform _targetTransform;
        [Tooltip("Camera for zooming, or null to adjust target position.")]
        [SerializeField] private Camera _camera;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 0.35f;
        [SerializeField] private float _inertiaDamping = 8.0f;
        [SerializeField] private float _minPitch = -60f;
        [SerializeField] private float _maxPitch = 60f;

        [Header("Zoom Settings")]
        [SerializeField] private float _pinchZoomSpeed = 0.008f;
        [SerializeField] private float _scrollZoomSpeed = 0.5f;
        [SerializeField] private float _minFov = 20f;
        [SerializeField] private float _maxFov = 65f;
        [SerializeField] private float _defaultFov = 40f;

        [Header("Default Orientation")]
        [SerializeField] private Vector3 _defaultEulerAngles = new Vector3(10f, -30f, 0f);

        private float _currentYaw;
        private float _currentPitch;
        private Vector2 _rotationVelocity;
        private bool _isDragging;
        private Vector2 _lastPointerPos;

        private float _targetFov;

        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }

        public Camera InspectionCamera
        {
            get => _camera;
            set => _camera = value;
        }

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;

            if (_camera == null)
                _camera = Camera.main;

            _currentPitch = _defaultEulerAngles.x;
            _currentYaw = _defaultEulerAngles.y;

            if (_camera != null)
            {
                _targetFov = _camera.fieldOfView > 0 ? _camera.fieldOfView : _defaultFov;
            }
            else
            {
                _targetFov = _defaultFov;
            }

            ApplyRotationImmediate();
        }

        private void OnEnable()
        {
            try { EnhancedTouchSupport.Enable(); } catch { }
        }

        private void OnDisable()
        {
            _isDragging = false;
        }

        public void ResetView()
        {
            _currentPitch = _defaultEulerAngles.x;
            _currentYaw = _defaultEulerAngles.y;
            _rotationVelocity = Vector2.zero;
            _targetFov = _defaultFov;

            if (_camera != null)
                _camera.fieldOfView = _defaultFov;

            ApplyRotationImmediate();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            HandleTouchInput();
            HandleMouseInput();

            // Apply rotation inertia
            if (!_isDragging && _rotationVelocity.sqrMagnitude > 0.0001f)
            {
                _currentYaw += _rotationVelocity.x * dt;
                _currentPitch -= _rotationVelocity.y * dt;
                _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);

                _rotationVelocity = Vector2.Lerp(_rotationVelocity, Vector2.zero, dt * _inertiaDamping);
                ApplyRotationImmediate();
            }

            // Smooth FOV zoom
            if (_camera != null && !Mathf.Approximately(_camera.fieldOfView, _targetFov))
            {
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFov, dt * 12f);
            }
        }

        private void HandleTouchInput()
        {
            if (!EnhancedTouchSupport.enabled) return;

            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
            {
                _isDragging = false;
                var touch0 = touches[0];
                var touch1 = touches[1];

                // Check if either touch is over UI
                if (IsPointerOverUI(touch0.touchId) || IsPointerOverUI(touch1.touchId))
                    return;

                Vector2 prevPos0 = touch0.screenPosition - touch0.delta;
                Vector2 prevPos1 = touch1.screenPosition - touch1.delta;

                float prevDeltaMag = (prevPos0 - prevPos1).magnitude;
                float currentDeltaMag = (touch0.screenPosition - touch1.screenPosition).magnitude;

                float deltaMagnitudeDiff = prevDeltaMag - currentDeltaMag;
                ApplyZoom(deltaMagnitudeDiff * _pinchZoomSpeed * 10f);
            }
            else if (touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (IsPointerOverUI(touch.touchId))
                    {
                        _isDragging = false;
                        return;
                    }
                    _isDragging = true;
                    _lastPointerPos = touch.screenPosition;
                    _rotationVelocity = Vector2.zero;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _isDragging)
                {
                    Vector2 delta = touch.delta;
                    _lastPointerPos = touch.screenPosition;
                    ApplyDrag(delta);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _isDragging = false;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private void HandleMouseInput()
        {
            if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
                return; // Mobile touch already handled

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Mouse scrollwheel zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI(-1))
            {
                float normalizedScroll = Mathf.Sign(scroll) * Mathf.Min(Mathf.Abs(scroll) * 0.01f, 3f);
                ApplyZoom(-normalizedScroll * _scrollZoomSpeed * 4f);
            }

            // Mouse drag rotate
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI(-1))
                {
                    _isDragging = false;
                    return;
                }

                _isDragging = true;
                _lastPointerPos = mouse.position.ReadValue();
                _rotationVelocity = Vector2.zero;
            }
            else if (mouse.leftButton.isPressed && _isDragging)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                Vector2 delta = currentPos - _lastPointerPos;
                _lastPointerPos = currentPos;
                ApplyDrag(delta);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }
        }

        private void ApplyDrag(Vector2 delta)
        {
            float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float yawDelta = delta.x * _rotationSpeed;
            float pitchDelta = delta.y * _rotationSpeed;

            _currentYaw += yawDelta;
            _currentPitch -= pitchDelta;
            _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);

            _rotationVelocity = new Vector2(yawDelta / dt, pitchDelta / dt) * 0.2f;
            ApplyRotationImmediate();
        }

        private void ApplyZoom(float delta)
        {
            _targetFov = Mathf.Clamp(_targetFov + delta, _minFov, _maxFov);
            if (_camera != null && Mathf.Abs(_camera.fieldOfView - _targetFov) > 0.01f)
            {
                _camera.fieldOfView = Mathf.Clamp(_camera.fieldOfView + delta, _minFov, _maxFov);
            }
        }

        private void ApplyRotationImmediate()
        {
            if (_targetTransform != null)
            {
                _targetTransform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            }
        }

        private bool IsPointerOverUI(int pointerId)
        {
            try
            {
                if (EventSystem.current == null)
                    return false;

                if (pointerId >= 0)
                    return EventSystem.current.IsPointerOverGameObject(pointerId);

                return EventSystem.current.IsPointerOverGameObject();
            }
            catch
            {
                return false;
            }
        }
    }
}
