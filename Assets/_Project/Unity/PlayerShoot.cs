using UnityEngine;

namespace Game
{
    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private Gun _gun;

        private InputReader _input;

        public void Initialize(InputReader input)
        {
            _input = input;
            _input.OnShoot += HandleShoot;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnShoot -= HandleShoot;
            }
        }

        private void HandleShoot()
        {
            _gun.Shoot(transform.forward);
        }
    }
}
