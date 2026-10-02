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
            _input.ResetGameplayTransientState();
        }

        public void Update()
        {
            Vector2 moveDirection = _input.MoveDirection;
            Vector2 lookDirection = _input.LookDirection;

            _movement.Move(moveDirection);
            _rotation.Rotate(lookDirection);

            if (_input.ConsumePreviousWeaponPressed())
            {
                _shoot.PreviousWeapon();
            }

            if (_input.ConsumeNextWeaponPressed())
            {
                _shoot.NextWeapon();
            }

            if (_input.ConsumeReloadPressed())
            {
                _shoot.Reload();
            }

            _input.ReadShootState(_shootThreshold, out bool isShootHeld, out bool wasShootPressed);
            _shoot.HandleTrigger(isShootHeld, wasShootPressed);
        }

        public void Exit()
        {
            _movement.Move(Vector2.zero);
            _input.ResetGameplayTransientState();
        }
    }
}
