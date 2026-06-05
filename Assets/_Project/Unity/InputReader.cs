using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class InputReader
    {
        private readonly GameInput _input;

        public InputReader()
        {
            _input = new GameInput();
            _input.Enable();

        }

        public Vector2 MoveDirection => _input.Player.Move.ReadValue<Vector2>();
    }
}
