using UnityEngine;
using UnityEngine.EventSystems;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class MobileActionButton :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private MobileInputState _input;
        [SerializeField] private MobileInputAction _action = MobileInputAction.Fire;

        private int _activePointerId = int.MinValue;

        public MobileInputState Input => _input;
        public MobileInputAction Action => _action;

        public void Configure(
            MobileInputState input,
            MobileInputAction action)
        {
            _input = input;
            _action = action;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_input == null || _activePointerId != int.MinValue)
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            _input.Press(_action);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
            {
                return;
            }

            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_action == MobileInputAction.Fire &&
                eventData.pointerId == _activePointerId)
            {
                Release();
            }
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (_input != null)
            {
                _input.Release(_action);
            }

            _activePointerId = int.MinValue;
        }
    }
}
