using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public class BulletTrail : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Color _startColor = new Color(1f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color _endColor = new Color(1f, 0.5f, 0.1f, 0f);
        [SerializeField] private float _time = 0.15f;
        [SerializeField] private float _width = 0.12f;

        private TrailRenderer _trail;

        private void Awake()
        {
            _trail = gameObject.AddComponent<TrailRenderer>();
            Configure();
        }

        private void OnEnable()
        {
            if (_trail == null)
            {
                return;
            }

            _trail.Clear();
            _trail.emitting = false;
        }

        private void LateUpdate()
        {
            if (_trail.emitting)
            {
                return;
            }

            _trail.Clear();
            _trail.emitting = true;
        }

        private void Configure()
        {
            _trail.time = _time;
            _trail.startWidth = _width;
            _trail.endWidth = 0f;
            _trail.startColor = _startColor;
            _trail.endColor = _endColor;
            _trail.numCapVertices = 4;
            _trail.minVertexDistance = 0.05f;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.sharedMaterial = _material;
        }
    }
}
