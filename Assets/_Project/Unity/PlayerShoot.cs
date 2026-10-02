using UnityEngine;

namespace Game
{
    public class PlayerShoot : MonoBehaviour
    {
        [Tooltip("Legacy single-gun reference. Used when no WeaponLoadout is assigned.")]
        [SerializeField] private Gun _gun;
        [SerializeField] private WeaponLoadout _loadout;

        public Gun ActiveGun
        {
            get
            {
                if (_loadout != null && _loadout.ActiveGun != null)
                {
                    return _loadout.ActiveGun;
                }

                return _gun;
            }
        }

        private void Awake()
        {
            if (_loadout == null)
            {
                _loadout = GetComponent<WeaponLoadout>();
            }
        }

        public void HandleTrigger(bool isHeld, bool wasPressed)
        {
            Gun gun = ActiveGun;

            if (gun != null)
            {
                gun.HandleTrigger(transform.forward, isHeld, wasPressed);
            }
        }

        public void TryShoot()
        {
            Gun gun = ActiveGun;

            if (gun != null)
            {
                gun.Shoot(transform.forward);
            }
        }

        public bool Reload()
        {
            Gun gun = ActiveGun;
            return gun != null && gun.Reload();
        }

        public bool NextWeapon()
        {
            return _loadout != null && _loadout.EquipNext();
        }

        public bool PreviousWeapon()
        {
            return _loadout != null && _loadout.EquipPrevious();
        }

        public int AddAmmo(int amount)
        {
            Gun gun = ActiveGun;
            return gun != null ? gun.AddAmmo(amount) : 0;
        }

        public void CancelActions()
        {
            Gun gun = ActiveGun;
            if (gun != null)
            {
                gun.CancelBurst();
                gun.CancelReload();
            }
        }

        public void ResetWeapons()
        {
            CancelActions();

            if (_loadout != null)
            {
                _loadout.ResetToDefault();
            }

            if (_gun != null)
            {
                _gun.ResetRuntimeState();
                _gun.SetEquipped(_loadout == null || _loadout.Count == 0);
            }
        }
    }
}
