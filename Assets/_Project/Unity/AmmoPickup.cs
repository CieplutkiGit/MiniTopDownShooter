using UnityEngine;

namespace Game
{
    public class AmmoPickup : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int _amount = 24;
        [SerializeField] private bool _destroyOnCollect = true;
        [SerializeField] private bool _isRuntimeDrop;

        public bool IsRuntimeDrop
        {
            get => _isRuntimeDrop;
            set => _isRuntimeDrop = value;
        }

        public int Amount
        {
            get => _amount;
            set => _amount = Mathf.Max(1, value);
        }

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
                Collect();
            }
        }

        public void Collect()
        {
            if (_isRuntimeDrop)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
        }
    }
}
