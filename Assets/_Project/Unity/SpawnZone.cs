using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public enum SpawnZoneShape
    {
        PointGroup,
        Circle,
        Box
    }

    public class SpawnZone : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string _id = "Default";
        [Min(1)]
        [SerializeField] private int _weight = 1;

        [Header("Shape")]
        [SerializeField] private SpawnZoneShape _shape = SpawnZoneShape.Circle;
        [SerializeField] private Transform[] _points;
        [Min(0.1f)]
        [SerializeField] private float _radius = 4f;
        [SerializeField] private Vector2 _boxSize = new Vector2(8f, 8f);

        [Header("Player Distance")]
        [Min(0f)]
        [SerializeField] private float _minPlayerDistance = 8f;
        [Tooltip("Zero means unlimited.")]
        [Min(0f)]
        [SerializeField] private float _maxPlayerDistance;

        [Header("Visibility")]
        [SerializeField] private bool _requireOffscreen;
        [SerializeField] private Camera _visibilityCamera;
        [Range(0f, 0.5f)]
        [SerializeField] private float _viewportMargin = 0.05f;

        [Header("NavMesh")]
        [Min(0.1f)]
        [SerializeField] private float _navMeshSampleRadius = 2f;
        [Range(1, 64)]
        [SerializeField] private int _attempts = 12;

        public string Id => _id;
        public int Weight => Mathf.Max(1, _weight);

        public bool TryGetSpawnPosition(
            Transform player,
            Camera fallbackCamera,
            out Vector3 position)
        {
            Camera visibilityCamera = _visibilityCamera != null
                ? _visibilityCamera
                : fallbackCamera;

            for (int attempt = 0; attempt < Mathf.Max(1, _attempts); attempt++)
            {
                if (!TryCreateCandidate(out Vector3 candidate) ||
                    !TrySampleReachablePosition(candidate, player, _navMeshSampleRadius, out Vector3 samplePos))
                {
                    continue;
                }

                if (_requireOffscreen &&
                    visibilityCamera != null &&
                    !IsOffscreen(samplePos, visibilityCamera))
                {
                    continue;
                }

                position = samplePos;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public bool TryGetFallbackSpawnPosition(
            Transform player,
            Camera fallbackCamera,
            out Vector3 position)
        {
            for (int attempt = 0; attempt < Mathf.Max(1, _attempts); attempt++)
            {
                if (TryCreateCandidate(out Vector3 candidate) &&
                    TrySampleReachablePosition(candidate, player, _navMeshSampleRadius * 2f, out position))
                {
                    return true;
                }
            }

            return TrySampleReachablePosition(transform.position, player, _navMeshSampleRadius * 4f, out position);
        }

        private bool TrySampleReachablePosition(
            Vector3 candidate,
            Transform player,
            float sampleRadius,
            out Vector3 position)
        {
            position = Vector3.zero;
            if (!PassesPlayerDistance(candidate, player) ||
                !NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas) ||
                !IsPositionInZone(hit.position) ||
                !PassesPlayerDistance(hit.position, player))
            {
                return false;
            }

            if (player != null)
            {
                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, player.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    return false;
                }
            }

            position = hit.position;
            return true;
        }

        public bool IsPositionInZone(Vector3 position, float tolerance = 1.5f)
        {
            switch (_shape)
            {
                case SpawnZoneShape.PointGroup:
                    if (_points == null || _points.Length == 0)
                    {
                        return Vector3.Distance(position, transform.position) <= _radius + tolerance;
                    }
                    for (int i = 0; i < _points.Length; i++)
                    {
                        if (_points[i] != null && Vector3.Distance(position, _points[i].position) <= _radius + tolerance)
                        {
                            return true;
                        }
                    }
                    return false;

                case SpawnZoneShape.Box:
                    Vector3 local = transform.InverseTransformPoint(position);
                    float halfX = _boxSize.x * 0.5f + tolerance;
                    float halfZ = _boxSize.y * 0.5f + tolerance;
                    return Mathf.Abs(local.x) <= halfX && Mathf.Abs(local.z) <= halfZ;

                default: // Circle
                    Vector3 diff = position - transform.position;
                    diff.y = 0f;
                    return diff.magnitude <= _radius + tolerance;
            }
        }

        private bool TryCreateCandidate(out Vector3 candidate)
        {
            switch (_shape)
            {
                case SpawnZoneShape.PointGroup:
                    return TryGetPointCandidate(out candidate);

                case SpawnZoneShape.Box:
                    float halfX = Mathf.Max(0.05f, _boxSize.x * 0.5f);
                    float halfZ = Mathf.Max(0.05f, _boxSize.y * 0.5f);
                    Vector3 local = new Vector3(
                        Random.Range(-halfX, halfX),
                        0f,
                        Random.Range(-halfZ, halfZ));
                    candidate = transform.TransformPoint(local);
                    return true;

                default:
                    float angle = Random.value * Mathf.PI * 2f;
                    float distance = Mathf.Sqrt(Random.value) * Mathf.Max(0.1f, _radius);
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * distance,
                        0f,
                        Mathf.Sin(angle) * distance);
                    candidate = transform.position + offset;
                    return true;
            }
        }

        private bool TryGetPointCandidate(out Vector3 candidate)
        {
            if (_points == null || _points.Length == 0)
            {
                candidate = transform.position;
                return true;
            }

            int start = Random.Range(0, _points.Length);

            for (int i = 0; i < _points.Length; i++)
            {
                Transform point = _points[(start + i) % _points.Length];

                if (point != null)
                {
                    candidate = point.position;
                    return true;
                }
            }

            candidate = Vector3.zero;
            return false;
        }

        private bool PassesPlayerDistance(Vector3 candidate, Transform player)
        {
            if (player == null)
            {
                return true;
            }

            Vector3 delta = candidate - player.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (distance < _minPlayerDistance)
            {
                return false;
            }

            return _maxPlayerDistance <= 0f || distance <= _maxPlayerDistance;
        }

        private bool IsOffscreen(Vector3 worldPosition, Camera camera)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);

            if (viewport.z <= 0f)
            {
                return true;
            }

            return viewport.x < -_viewportMargin ||
                   viewport.x > 1f + _viewportMargin ||
                   viewport.y < -_viewportMargin ||
                   viewport.y > 1f + _viewportMargin;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = gameObject.name;
            }

            _weight = Mathf.Max(1, _weight);
            _radius = Mathf.Max(0.1f, _radius);
            _boxSize.x = Mathf.Max(0.1f, _boxSize.x);
            _boxSize.y = Mathf.Max(0.1f, _boxSize.y);
            _minPlayerDistance = Mathf.Max(0f, _minPlayerDistance);

            if (_maxPlayerDistance > 0f)
            {
                _maxPlayerDistance = Mathf.Max(_minPlayerDistance, _maxPlayerDistance);
            }

            _navMeshSampleRadius = Mathf.Max(0.1f, _navMeshSampleRadius);
            _attempts = Mathf.Clamp(_attempts, 1, 64);
        }
    }
}
