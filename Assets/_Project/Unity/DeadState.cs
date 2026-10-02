using Cieplutki.MiniTopDownShooter.Application;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class DeadState : IEnemyState
    {
        private readonly EnemyMovement _movement;

        public DeadState(EnemyMovement movement)
        {
            _movement = movement;
        }

        public void Enter()
        {
            _movement.Stop();
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}
