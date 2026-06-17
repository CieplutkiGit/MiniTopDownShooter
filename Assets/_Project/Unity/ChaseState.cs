using Application;
using UnityEngine;

namespace Game
{
    public class ChaseState : IEnemyState
    {
        private readonly EnemyMovement _movement;
        private readonly Transform _target;

        public ChaseState(EnemyMovement movement, Transform target)
        {
            _movement = movement;
            _target = target;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            _movement.MoveToward(_target.position);
        }

        public void Exit()
        {
            _movement.Stop();
        }
    }
}
