using Application;
using UnityEngine;

namespace Game
{
    public class AlivePlayerState : IPlayerState
    {
        private readonly InputReader _input;
        private readonly PlayerMovement _movement;
        private readonly PlayerRotation _rotation;
        private readonly PlayerShoot _shoot;
        private readonly float _shootThreshold;

        public AlivePlayerState(
            InputReader input,
            PlayerMovement movement,
            PlayerRotation rotation,
            PlayerShoot shoot,
            float shootThreshold)
        {
            _input = input;
            _movement = movement;
            _rotation = rotation;
            _shoot = shoot;
            _shootThreshold = shootThreshold;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            Vector2 moveDirection = _input.MoveDirection;
            Vector2 lookDirection = _input.LookDirection;

            _movement.Move(moveDirection);
            _rotation.Rotate(lookDirection);

            if (_input.IsShootHeld(_shootThreshold))
            {
                _shoot.TryShoot();
            }
        }

        public void Exit()
        {
            _movement.Move(Vector2.zero);
        }
    }
}
