using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class DeadPlayerState : IPlayerState
    {
        private readonly PlayerMovement _movement;

        public DeadPlayerState(PlayerMovement movement)
        {
            _movement = movement;
        }

        public void Enter()
        {
            _movement.Move(Vector2.zero);
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}
