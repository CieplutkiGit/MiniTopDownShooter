using UnityEngine;

namespace Game
{

    [RequireComponent(typeof(PlayerMovement))]

    public class PlayerController : MonoBehaviour
    {
        private InputReader _input;
        private PlayerMovement _movement;

        private void Awake()
        {
            _input = new InputReader();
            _movement = GetComponent<PlayerMovement>();
        }

        private void FixedUpdate()
        {
            _movement.Move(_input.MoveDirection);
        }
    }
}
