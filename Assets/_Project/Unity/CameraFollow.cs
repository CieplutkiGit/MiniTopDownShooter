using UnityEngine;

namespace Game
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 10, -5);

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            Vector3 targetPosition = _target.position + _offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
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
