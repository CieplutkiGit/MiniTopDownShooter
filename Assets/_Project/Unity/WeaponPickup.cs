using UnityEngine;

namespace Game
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private Gun _weaponPrefab;
        [SerializeField] private bool _equipImmediately = true;
        [SerializeField] private bool _destroyOnCollect = true;

        private void OnTriggerEnter(Collider other)
        {
            WeaponLoadout loadout = other.GetComponentInParent<WeaponLoadout>();

            if (loadout == null || _weaponPrefab == null)
            {
                return;
            }

            if (_weaponPrefab.Definition != null && loadout.ContainsDefinition(_weaponPrefab.Definition))
            {
                return;
            }

            Gun weapon = Instantiate(_weaponPrefab, loadout.WeaponMount);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            if (!loadout.AddWeapon(weapon, _equipImmediately))
            {
                Destroy(weapon.gameObject);
                return;
            }

            if (_destroyOnCollect)
            {
                Destroy(gameObject);
            }
        }
    }
}
