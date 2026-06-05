using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class InputReader : IDisposable
    {
        private readonly GameInput _input;

        public event Action OnShoot;

        public InputReader()
        {
            _input = new GameInput();
            _input.Player.Shoot.performed += OnShootPerformed;
            _input.Enable();
        }

        private void OnShootPerformed(InputAction.CallbackContext context)
        {
            OnShoot?.Invoke();
        }

        public Vector2 MoveDirection
        {
            get { return _input.Player.Move.ReadValue<Vector2>(); }
        }

        public void Dispose()
        {
            _input.Player.Shoot.performed -= OnShootPerformed;
            _input.Disable();
            _input.Dispose();
        }
    }
}
