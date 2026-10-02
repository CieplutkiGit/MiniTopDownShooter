using UnityEngine;

namespace Game
{
    public class PlayerRotation : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 720f;

        public float AimSensitivity { get; set; } = 1.0f;

        public void Rotate(Vector2 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            Vector3 lookDirection = new Vector3(direction.x, 0, direction.y);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            float effectiveSpeed = _rotationSpeed * Mathf.Max(0.1f, AimSensitivity);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, effectiveSpeed * Time.fixedDeltaTime);
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}
