using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void MoveToward(Vector3 targetPosition)
        {
            _agent.SetDestination(targetPosition);
        }

        public void Stop()
        {
            _agent.ResetPath();
        }

        public void Warp(Vector3 position)
        {
            _agent.Warp(position);
        }
    }
}
