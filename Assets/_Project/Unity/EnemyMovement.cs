using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private float _baseSpeed = 3.5f;
        private float _speedMultiplier = 1f;

        public float BaseSpeed => _baseSpeed;
        public float SpeedMultiplier => _speedMultiplier;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Init(float speed)
        {
            _baseSpeed = Mathf.Max(0f, speed);
            ApplySpeed();
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
            ApplySpeed();
        }

        public void MoveToward(Vector3 targetPosition)
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                return;
            }

            _agent.SetDestination(targetPosition);
        }

        public void MoveAwayFrom(Vector3 threatPosition, float distance)
        {
            Vector3 away = transform.position - threatPosition;
            away.y = 0f;

            if (away.sqrMagnitude <= Mathf.Epsilon)
            {
                away = -transform.forward;
            }

            MoveToward(transform.position + away.normalized * Mathf.Max(0.1f, distance));
        }

        public void Stop()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }

        public void Warp(Vector3 position)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }

        [Header("Stuck Recovery")]
        [SerializeField] private float _stuckTimeThreshold = 2.0f;
        [SerializeField] private float _stuckVelocityThreshold = 0.05f;

        private float _stuckTimer;

        private void Update()
        {
            CheckAndRecoverStuck();
        }

        private void CheckAndRecoverStuck()
        {
            if (_agent == null || !_agent.isOnNavMesh || !_agent.hasPath)
            {
                _stuckTimer = 0f;
                return;
            }

            if (_agent.remainingDistance > _agent.stoppingDistance &&
                _agent.velocity.sqrMagnitude < _stuckVelocityThreshold * _stuckVelocityThreshold)
            {
                _stuckTimer += Time.deltaTime;

                if (_stuckTimer >= _stuckTimeThreshold)
                {
                    _stuckTimer = 0f;
                    RecoverStuck();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        private void RecoverStuck()
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                return;
            }

            Vector3 destination = _agent.destination;
            Vector3 dir = (destination - transform.position).normalized;
            Vector3 candidate = transform.position + dir * 1.5f;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                _agent.SetDestination(destination);
            }
            else
            {
                _agent.ResetPath();
                _agent.SetDestination(destination);
            }
        }

        private void ApplySpeed()
        {
            if (_agent != null)
            {
                _agent.speed = _baseSpeed * _speedMultiplier;
            }
        }
    }
}
