using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class AmmoPickup : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int _amount = 24;
        [SerializeField] private bool _destroyOnCollect = true;

        private void OnTriggerEnter(Collider other)
        {
            PlayerShoot shoot = other.GetComponentInParent<PlayerShoot>();

            if (shoot == null)
            {
                return;
            }

            int added = shoot.AddAmmo(_amount);

            if (added > 0 && _destroyOnCollect)
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
        }
    }
}
