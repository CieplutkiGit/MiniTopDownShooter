using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Workshop
{
    /// <summary>
    /// Interactive 3D weapon inspection and pointer routing controller for the WeaponEdit scene.
    /// Provides deterministic pointer ownership:
    /// - Presses on UI never rotate or select world objects (press-origin ownership).
    /// - Rotate and zoom occur strictly within the designated preview viewport.
    /// - Rotation does not begin until movement passes the drag threshold.
    /// - Part selection occurs only on a valid clean release inside viewport (drags and pinches never become clicks).
    /// - Drags remain owned until pointer release.
    /// - Dynamically fits assembled and exploded model bounds into the unobstructed viewport across aspect ratios.
    /// </summary>
    public class WeaponPinchRotateController : MonoBehaviour
    {
        [Header("Target & Camera")]
        [Tooltip("Transform that rotates when dragged (turntable / weapon mount).")]
        [SerializeField] private Transform _targetTransform;
        [Tooltip("Camera for zooming and inspection.")]
        [SerializeField] private Camera _camera;

        [Header("Preview Viewport & View")]
        [Tooltip("UI RectTransform defining the interactive 3D inspection area.")]
        [SerializeField] private RectTransform _previewViewport;
        [Tooltip("Weapon preview view for raycast part selection.")]
        [SerializeField] private Presentation.WeaponPreviewView _previewView;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 0.35f;
        [SerializeField] private float _inertiaDamping = 8.0f;
        [SerializeField] private float _minPitch = -60f;
        [SerializeField] private float _maxPitch = 60f;

        [Header("Zoom Settings")]
        [SerializeField] private float _pinchZoomSpeed = 0.008f;
        [SerializeField] private float _scrollZoomSpeed = 0.5f;
        [SerializeField] private float _minFov = 18f;
        [SerializeField] private float _maxFov = 60f;
        [SerializeField] private float _defaultFov = 38f;

        [Header("Framing & Orientation")]
        [SerializeField] private Vector3 _defaultEulerAngles = new Vector3(10f, -30f, 0f);
        [SerializeField] private Vector3 _cameraFramingOffset = new Vector3(0f, 0.20f, 0f);
        [SerializeField] private float _framedDistance = 1.90f;
        [SerializeField] private float _topReservedPixels = 120f;
        [SerializeField] private float _bottomReservedPixels = 215f;

        [Header("Input Thresholds")]
        [SerializeField] private float _dragThreshold = 8f; // pixels

        private float _currentYaw;
        private float _currentPitch;
        private Vector2 _rotationVelocity;
        private bool _isDragging;
        private bool _hasDragged;
        private bool _isPinching;
        private bool _pressOriginatedOnUI;
        private int _activePointerId = -999;
        private Vector2 _pointerDownPos;
        private Vector2 _lastPointerPos;
        private float _targetFov;

        // Reusable raycast list to prevent per-frame GC allocations
        private static readonly List<RaycastResult> s_RaycastResults = new List<RaycastResult>();

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

        public RectTransform PreviewViewport
        {
            get => _previewViewport;
            set => _previewViewport = value;
        }

        public Presentation.WeaponPreviewView PreviewView
        {
            get => _previewView;
            set
            {
                _previewView = value;
                if (_previewView != null)
                {
                    _previewView.SetOwnerController(this);
                }
            }
        }

        public float DragThresholdPixels
        {
            get => _dragThreshold;
            set => _dragThreshold = value;
        }

        public bool IsDragging => _isDragging;
        public bool HasDragged => _hasDragged;
        public bool IsPinching => _isPinching;
        public float TargetFov => _targetFov;
        public Vector3 CameraFramingOffset => _cameraFramingOffset;
        public float FramedDistance => _framedDistance;

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;

            if (_camera == null)
                _camera = Camera.main;

            if (_previewView == null)
                _previewView = FindFirstObjectByType<Presentation.WeaponPreviewView>();

            if (_previewView != null)
                _previewView.SetOwnerController(this);

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

            FrameModelBounds(false);
            ApplyRotationImmediate();
        }

        private void OnEnable()
        {
            try { EnhancedTouchSupport.Enable(); } catch { }
            if (_previewView != null)
                _previewView.SetOwnerController(this);
        }

        private void OnDisable()
        {
            _isDragging = false;
            _hasDragged = false;
            _isPinching = false;
            _pressOriginatedOnUI = false;
            _activePointerId = -999;
        }

        /// <summary>
        /// Measures the rendered bounds of all active weapon parts and adjusts camera distance and offset
        /// so the model is centered cleanly in the unobstructed viewport region across aspect ratios.
        /// </summary>
        public void FrameModelBounds(bool exploded = false)
        {
            Bounds bounds = CalculateModelBounds();

            float screenH = UnityEngine.Screen.height > 0 ? (float)UnityEngine.Screen.height : 720f;
            float topFrac = Mathf.Clamp01(_topReservedPixels / screenH);
            float btmFrac = Mathf.Clamp01(_bottomReservedPixels / screenH);

            float vMinY = btmFrac;
            float vMaxY = 1.0f - topFrac;
            float availV = Mathf.Max(0.2f, vMaxY - vMinY);
            float vCenterY = (vMinY + vMaxY) * 0.5f;

            // Measure composite extent with safety padding
            float padding = exploded ? 1.25f : 1.15f;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * padding;
            if (maxDimension < 0.1f) maxDimension = 0.8f;

            float fovRad = _defaultFov * Mathf.Deg2Rad;
            float effFov = 2f * Mathf.Atan(Mathf.Tan(fovRad * 0.5f) * availV);
            float requiredDist = (maxDimension * 0.5f) / Mathf.Tan(effFov * 0.5f);
            requiredDist = Mathf.Clamp(requiredDist, 1.2f, 3.5f);

            // Calculate vertical framing offset so bounds center projects onto vCenterY
            float yShift = (vCenterY - 0.5f) * 2f * requiredDist * Mathf.Tan(fovRad * 0.5f);
            _cameraFramingOffset = new Vector3(bounds.center.x, bounds.center.y - yShift, 0f);
            _framedDistance = requiredDist;

            ApplyFraming();
        }

        public Bounds CalculateModelBounds()
        {
            Transform root = _targetTransform != null ? _targetTransform : transform;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one * 0.5f);
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        public void ApplyFraming()
        {
            if (_camera != null)
            {
                _camera.transform.position = _cameraFramingOffset + new Vector3(0f, 0f, -_framedDistance);
                _camera.transform.rotation = Quaternion.identity;
                _camera.fieldOfView = _targetFov;
            }
        }

        public void ResetView()
        {
            _currentPitch = _defaultEulerAngles.x;
            _currentYaw = _defaultEulerAngles.y;
            _rotationVelocity = Vector2.zero;
            _targetFov = _defaultFov;

            FrameModelBounds(false);
            ApplyRotationImmediate();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            HandleTouchInput();
            HandleMouseInput();

            // Apply rotation inertia when released after dragging
            if (!_isDragging && !_isPinching && _rotationVelocity.sqrMagnitude > 0.0001f)
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
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFov, dt * 14f);
            }
        }

        // =========================================================================
        // PUBLIC DETERMINISTIC POINTER ROUTING METHODS (Used by tests & runtime)
        // =========================================================================

        public void OnPointerDown(int pointerId, Vector2 screenPos)
        {
            bool overUI = IsPointerOverInteractiveUI(pointerId, screenPos);
            bool insideViewport = IsInsideViewport(screenPos);

            if (overUI || !insideViewport)
            {
                _pressOriginatedOnUI = true;
                _isDragging = false;
                _hasDragged = false;
                _activePointerId = pointerId;
                return;
            }

            _pressOriginatedOnUI = false;
            _isDragging = true;
            _hasDragged = false;
            _isPinching = false;
            _activePointerId = pointerId;
            _pointerDownPos = screenPos;
            _lastPointerPos = screenPos;
            _rotationVelocity = Vector2.zero;
        }

        public void OnPointerMove(int pointerId, Vector2 screenPos)
        {
            if (_pressOriginatedOnUI || !_isDragging || _isPinching)
                return;

            if (_activePointerId != pointerId && _activePointerId != -999)
                return;

            // Only begin rotating once threshold is crossed
            if (!_hasDragged)
            {
                if ((screenPos - _pointerDownPos).sqrMagnitude > _dragThreshold * _dragThreshold)
                {
                    _hasDragged = true;
                }
            }

            if (_hasDragged)
            {
                Vector2 delta = screenPos - _lastPointerPos;
                _lastPointerPos = screenPos;
                ApplyDrag(delta);
            }
        }

        public void OnPointerUp(int pointerId, Vector2 screenPos)
        {
            if (_activePointerId != pointerId && _activePointerId != -999)
                return;

            if (_isDragging && !_hasDragged && !_isPinching && !_pressOriginatedOnUI)
            {
                // Clean tap release: must release inside viewport and NOT over any blocking UI graphic
                if (IsInsideViewport(screenPos) && !IsPointerOverInteractiveUI(pointerId, screenPos))
                {
                    if (_previewView != null)
                    {
                        _previewView.HandlePointerClick(screenPos);
                    }
                }
            }

            _isDragging = false;
            _hasDragged = false;
            _pressOriginatedOnUI = false;
            _activePointerId = -999;
        }

        public void OnPinch(Vector2 pos0, Vector2 pos1, Vector2 delta0, Vector2 delta1)
        {
            // Pinch owns input: cannot become a tap or single-finger drag
            _isPinching = true;
            _isDragging = false;
            _hasDragged = true;

            if (!IsInsideViewport(pos0) || !IsInsideViewport(pos1))
                return;

            if (IsPointerOverInteractiveUI(-1, pos0) || IsPointerOverInteractiveUI(-1, pos1))
                return;

            Vector2 prevPos0 = pos0 - delta0;
            Vector2 prevPos1 = pos1 - delta1;

            float prevDist = (prevPos0 - prevPos1).magnitude;
            float currDist = (pos0 - pos1).magnitude;
            float diff = prevDist - currDist;

            ApplyZoom(diff * _pinchZoomSpeed * 10f);
        }

        // =========================================================================
        // INPUT SUBSYSTEM POINTER HANDLING
        // =========================================================================

        private void HandleTouchInput()
        {
            if (!EnhancedTouchSupport.enabled) return;

            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
            {
                var touch0 = touches[0];
                var touch1 = touches[1];
                OnPinch(touch0.screenPosition, touch1.screenPosition, touch0.delta, touch1.delta);
            }
            else if (touches.Count == 1)
            {
                if (_isPinching)
                {
                    // Finger remaining after pinch cannot become a tap
                    return;
                }

                var touch = touches[0];
                Vector2 pos = touch.screenPosition;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    OnPointerDown(touch.touchId, pos);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    OnPointerMove(touch.touchId, pos);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    OnPointerUp(touch.touchId, pos);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _isDragging = false;
                    _hasDragged = false;
                    _pressOriginatedOnUI = false;
                    _activePointerId = -999;
                }
            }
            else
            {
                _isPinching = false;
                if (_activePointerId >= 0)
                {
                    _isDragging = false;
                    _hasDragged = false;
                    _pressOriginatedOnUI = false;
                    _activePointerId = -999;
                }
            }
        }

        private void HandleMouseInput()
        {
            if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
                return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // Scroll zoom: only inside preview viewport and not over UI
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && IsInsideViewport(mousePos) && !IsPointerOverInteractiveUI(-1, mousePos))
            {
                float normalizedScroll = Mathf.Sign(scroll) * Mathf.Min(Mathf.Abs(scroll) * 0.01f, 3f);
                ApplyZoom(-normalizedScroll * _scrollZoomSpeed * 4f);
            }

            // Mouse button down / move / up
            if (mouse.leftButton.wasPressedThisFrame)
            {
                OnPointerDown(-1, mousePos);
            }
            else if (mouse.leftButton.isPressed && _isDragging)
            {
                OnPointerMove(-1, mousePos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                OnPointerUp(-1, mousePos);
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

        public bool IsInsideViewport(Vector2 screenPos)
        {
            if (_previewViewport == null)
                return true;

            Camera eventCam = null;
            Canvas canvas = _previewViewport.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCam = canvas.worldCamera != null ? canvas.worldCamera : _camera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_previewViewport, screenPos, eventCam);
        }

        public bool IsPointerOverInteractiveUI(int pointerId, Vector2 screenPos)
        {
            try
            {
                if (EventSystem.current == null) return false;

                var pointerEventData = new PointerEventData(EventSystem.current)
                {
                    position = screenPos
                };

                s_RaycastResults.Clear();
                EventSystem.current.RaycastAll(pointerEventData, s_RaycastResults);

                bool blocking = false;
                for (int i = 0; i < s_RaycastResults.Count; i++)
                {
                    var result = s_RaycastResults[i];
                    if (result.gameObject == null) continue;

                    // If the raycast hit is the preview viewport itself (the touch catcher), ignore it
                    if (_previewViewport != null && (result.gameObject == _previewViewport.gameObject ||
                        result.gameObject.transform == _previewViewport.transform))
                    {
                        continue;
                    }

                    // Any active UI Graphic with raycastTarget=true above/outside viewport blocks 3D interaction
                    // (This includes buttons, sliders, scroll rects, dialog panels, modals, backdrop images)
                    var graphic = result.gameObject.GetComponent<Graphic>();
                    if (graphic != null && graphic.raycastTarget)
                    {
                        blocking = true;
                        break;
                    }
                }

                s_RaycastResults.Clear();
                return blocking;
            }
            catch
            {
                return false;
            }
        }
    }
}
