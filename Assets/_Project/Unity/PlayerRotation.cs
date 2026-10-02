using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class PlayerRotation : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 720f;

        public void Rotate(Vector2 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            Vector3 lookDirection = new Vector3(direction.x, 0, direction.y);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
