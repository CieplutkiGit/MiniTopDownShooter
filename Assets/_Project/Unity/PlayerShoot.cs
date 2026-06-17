using UnityEngine;

namespace Game
{
    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private Gun _gun;

        public void TryShoot()
        {
            _gun.Shoot(transform.forward);
        }
    }
}
