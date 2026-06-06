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

        private void LateUpdate()
        {
            Vector3 targetPosition = _target.position + _offset;
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);

            if (_shake != null)
            {
                smoothed += _shake.CurrentOffset;
            }

            transform.position = smoothed;
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
