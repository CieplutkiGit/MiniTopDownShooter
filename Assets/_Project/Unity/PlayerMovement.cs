using UnityEngine;

namespace Game
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Move(Vector2 direction)
        {
            Vector3 move = new Vector3(direction.x, 0, direction.y);
            _rigidbody.MovePosition(_rigidbody.position + move * _speed * Time.fixedDeltaTime);
        }
    }
}
