using UnityEngine;
using UnityEngine.EventSystems;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public enum MobileJoystickChannel
    {
        Move,
        Look
    }

    public class MobileJoystick :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        [SerializeField] private MobileInputState _input;
        [SerializeField] private MobileJoystickChannel _channel;
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;

        [Range(0f, 0.95f)]
        [SerializeField] private float _deadZone = 0.15f;

        [Range(0.25f, 1f)]
        [SerializeField] private float _handleTravel = 0.7f;

        [Header("Aim / Fire")]
        [SerializeField] private bool _fireWhileAiming = true;

        [Range(0f, 1f)]
        [SerializeField] private float _fireThreshold = 0.2f;

        private int _activePointerId = int.MinValue;

        public MobileInputState Input => _input;
        public MobileJoystickChannel Channel => _channel;
        public bool FireWhileAiming => _fireWhileAiming;

        public void Configure(
            MobileInputState input,
            MobileJoystickChannel channel,
            RectTransform background,
            RectTransform handle,
            float deadZone = 0.15f,
            float handleTravel = 0.7f,
            bool fireWhileAiming = true,
            float fireThreshold = 0.2f)
        {
            _input = input;
            _channel = channel;
            _background = background;
            _handle = handle;
            _deadZone = Mathf.Clamp(deadZone, 0f, 0.95f);
            _handleTravel = Mathf.Clamp(handleTravel, 0.25f, 1f);
            _fireWhileAiming = fireWhileAiming;
            _fireThreshold = Mathf.Clamp01(fireThreshold);
        }

        private void Awake()
        {
            if (_background == null)
            {
                _background = transform as RectTransform;
            }
        }

        private void OnDisable()
        {
            ResetStick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue)
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId)
            {
                UpdateStick(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
            {
                return;
            }

            ResetStick();
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (_input == null || _background == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Rect rect = _background.rect;
            float radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Vector2 centeredPoint = localPoint - rect.center;
            Vector2 normalized = Vector2.ClampMagnitude(centeredPoint / radius, 1f);
            normalized = ApplyDeadZone(normalized);

            if (_handle != null)
            {
                _handle.anchoredPosition =
                    normalized * radius * Mathf.Clamp01(_handleTravel);
            }

            if (_channel == MobileJoystickChannel.Move)
            {
                _input.SetMove(normalized);
            }
            else
            {
                _input.SetLook(normalized, _fireWhileAiming, _fireThreshold);
            }
        }

        private Vector2 ApplyDeadZone(Vector2 value)
        {
            float magnitude = value.magnitude;

            if (magnitude <= _deadZone)
            {
                return Vector2.zero;
            }

            float remappedMagnitude = Mathf.InverseLerp(_deadZone, 1f, magnitude);
            return value.normalized * remappedMagnitude;
        }

        private void ResetStick()
        {
            _activePointerId = int.MinValue;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_input == null)
            {
                return;
            }

            if (_channel == MobileJoystickChannel.Move)
            {
                _input.SetMove(Vector2.zero);
            }
            else
            {
                _input.SetLook(Vector2.zero, _fireWhileAiming, _fireThreshold);
            }
        }

        private void OnValidate()
        {
            _deadZone = Mathf.Clamp(_deadZone, 0f, 0.95f);
            _handleTravel = Mathf.Clamp(_handleTravel, 0.25f, 1f);
            _fireThreshold = Mathf.Clamp01(_fireThreshold);
        }
    }
}
