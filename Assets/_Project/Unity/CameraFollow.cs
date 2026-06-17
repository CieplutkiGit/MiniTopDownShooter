using UnityEngine;

namespace Game
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 10, -5);
        [SerializeField] private ScreenShake _shake;

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _basePosition;

        private void OnEnable()
        {
            if (_target != null)
            {
                _basePosition = _target.position + _offset;
                transform.position = _basePosition;
            }
        }

        private void LateUpdate()
        {
            Vector3 targetPosition = _target.position + _offset;
            _basePosition = Vector3.SmoothDamp(_basePosition, targetPosition, ref _velocity, _smoothTime);

            Vector3 result = _basePosition;
            if (_shake != null)
            {
                result += _shake.CurrentOffset;
            }

            transform.position = result;
        }

        private void OnValidate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = _target.position + _offset;
        }
    }
}
