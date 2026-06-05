using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerShoot))]
    public class PlayerController : MonoBehaviour
    {
        private InputReader _input;
        private PlayerMovement _movement;
        private PlayerShoot _shoot;

        private void Awake()
        {
            _input = new InputReader();
            _movement = GetComponent<PlayerMovement>();
            _shoot = GetComponent<PlayerShoot>();
            _shoot.Initialize(_input);
        }

        private void FixedUpdate()
        {
            _movement.Move(_input.MoveDirection);
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }
    }
}
