using System;
using UnityEngine;

namespace Game
{
    public class InputReader : IDisposable
    {
        private readonly GameInput _input;

        public InputReader()
        {
            _input = new GameInput();
            _input.Enable();
        }

        public Vector2 MoveDirection
        {
            get { return _input.Player.Move.ReadValue<Vector2>(); }
        }

        public Vector2 LookDirection
        {
            get { return _input.Player.Look.ReadValue<Vector2>(); }
        }

        public void Dispose()
        {
            _input.Disable();
            _input.Dispose();
        }
    }
}
