using UnityEngine;

namespace Game
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private Gun _weaponPrefab;
        [SerializeField] private bool _equipImmediately = true;
        [SerializeField] private bool _destroyOnCollect = true;
        [SerializeField] private bool _isRuntimeDrop;

        public bool IsRuntimeDrop
        {
            get => _isRuntimeDrop;
            set => _isRuntimeDrop = value;
        }

        public Gun WeaponPrefab
        {
            get => _weaponPrefab;
            set => _weaponPrefab = value;
        }

        public bool EquipImmediately
        {
            get => _equipImmediately;
            set => _equipImmediately = value;
        }

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

            Game.Workshop.WeaponBuildApplier.ApplySavedBuild(weapon);

            if (!loadout.AddWeapon(weapon, _equipImmediately))
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(weapon.gameObject);
                }
                else
                {
                    DestroyImmediate(weapon.gameObject);
                }
                return;
            }

            if (_destroyOnCollect)
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
    }
}
