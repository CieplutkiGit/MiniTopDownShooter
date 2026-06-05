using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerRotation))]
    [RequireComponent(typeof(PlayerShoot))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _shootThreshold = 0.1f;

        private InputReader _input;
        private PlayerMovement _movement;
        private PlayerRotation _rotation;
        private PlayerShoot _shoot;

        private void Awake()
        {
            _input = new InputReader();
            _movement = GetComponent<PlayerMovement>();
            _rotation = GetComponent<PlayerRotation>();
            _shoot = GetComponent<PlayerShoot>();
        }

        private void FixedUpdate()
        {
            _movement.Move(_input.MoveDirection);
            _rotation.Rotate(_input.LookDirection);

            if (_input.LookDirection.magnitude > _shootThreshold)
            {
                _shoot.TryShoot();
            }
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }
    }
}
