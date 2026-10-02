using UnityEngine;
using UnityEngine.AI;

namespace Cieplutki.MiniTopDownShooter.Runtime
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

        private void ApplySpeed()
        {
            if (_agent != null)
            {
                _agent.speed = _baseSpeed * _speedMultiplier;
            }
        }
    }
}
