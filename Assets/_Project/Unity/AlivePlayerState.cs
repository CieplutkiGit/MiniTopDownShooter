using Application;
using Application.Workshop;
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
        private readonly IGameStateProvider _gameState;

        public AlivePlayerState(
            InputReader input,
            PlayerMovement movement,
            PlayerRotation rotation,
            PlayerShoot shoot,
            float shootThreshold,
            IGameStateProvider gameState = null)
        {
            _input = input;
            _movement = movement;
            _rotation = rotation;
            _shoot = shoot;
            _shootThreshold = shootThreshold;
            _gameState = gameState;
        }

        public void Enter()
        {
            _input?.ResetGameplayTransientState();
            _input?.RequireNeutralToRearm();
        }

        public void Update()
        {
            if (_input == null)
            {
                return;
            }

            GameState state = _gameState != null ? _gameState.CurrentState : GameState.Playing;

            bool canMove = GameActivityPolicy.CanMove(state);
            bool canFire = GameActivityPolicy.CanFire(state);
            bool canSwitchWeapon = GameActivityPolicy.CanSwitchWeapon(state);

            Vector2 moveDirection = canMove ? _input.MoveDirection : Vector2.zero;
            Vector2 lookDirection = _input.LookDirection;

            _movement.Move(moveDirection);
            _rotation.Rotate(lookDirection);

            if (_input.ConsumePreviousWeaponPressed())
            {
                if (canSwitchWeapon)
                {
                    _shoot.PreviousWeapon();
                }
            }

            if (_input.ConsumeNextWeaponPressed())
            {
                if (canSwitchWeapon)
                {
                    _shoot.NextWeapon();
                }
            }

            if (_input.ConsumeReloadPressed())
            {
                if (canFire)
                {
                    _shoot.Reload();
                }
            }

            if (canFire)
            {
                _input.ReadShootState(_shootThreshold, out bool isShootHeld, out bool wasShootPressed);
                _shoot.HandleTrigger(isShootHeld, wasShootPressed);
            }
            else
            {
                _shoot.HandleTrigger(false, false);
                _shoot.CancelActions();
            }
        }

        public void Exit()
        {
            _movement.Move(Vector2.zero);
            _input?.ResetGameplayTransientState();
            _input?.RequireNeutralToRearm();
            _shoot?.CancelActions();
        }
    }
}
