using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private List<Gun> _weapons = new List<Gun>();
        [SerializeField] private int _startingSlot;
        [SerializeField] private Transform _weaponMount;

        private int _activeIndex = -1;

        public event Action<Gun, int> WeaponEquipped;
        public event Action<Gun, int> WeaponAdded;

        public Gun ActiveGun =>
            _activeIndex >= 0 && _activeIndex < _weapons.Count
                ? _weapons[_activeIndex]
                : null;

        public int ActiveIndex => _activeIndex;
        public int Count => _weapons.Count;
        public Transform WeaponMount => _weaponMount != null ? _weaponMount : transform;
        public IReadOnlyList<Gun> Weapons => _weapons;

        private List<Gun> _defaultWeapons;

        private void Awake()
        {
            _defaultWeapons = new List<Gun>(_weapons);
        }

        private void Start()
        {
            if (_defaultWeapons == null)
            {
                _defaultWeapons = new List<Gun>(_weapons);
            }

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].SetEquipped(false);
                }
            }

            if (_weapons.Count > 0)
            {
                EquipSlot(Mathf.Clamp(_startingSlot, 0, _weapons.Count - 1));
            }
        }

        public void ResetToDefault()
        {
            if (_defaultWeapons == null)
            {
                _defaultWeapons = new List<Gun>(_weapons);
            }

            for (int i = _weapons.Count - 1; i >= 0; i--)
            {
                Gun gun = _weapons[i];
                if (gun != null && !_defaultWeapons.Contains(gun))
                {
                    gun.SetEquipped(false);
                    Destroy(gun.gameObject);
                }
            }

            _weapons.Clear();
            _weapons.AddRange(_defaultWeapons);

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].ResetRuntimeState();
                    _weapons[i].SetEquipped(false);
                }
            }

            _activeIndex = -1;
            if (_weapons.Count > 0)
            {
                EquipSlot(Mathf.Clamp(_startingSlot, 0, _weapons.Count - 1));
            }
        }

        public bool EquipSlot(int index)
        {
            if (index < 0 || index >= _weapons.Count || _weapons[index] == null)
            {
                return false;
            }

            if (_activeIndex >= 0 && _activeIndex < _weapons.Count)
            {
                Gun previous = _weapons[_activeIndex];

                if (previous != null && previous != _weapons[index])
                {
                    previous.SetEquipped(false);
                }
            }

            _activeIndex = index;
            Gun active = _weapons[_activeIndex];
            active.SetEquipped(true);
            WeaponEquipped?.Invoke(active, _activeIndex);
            return true;
        }

        public bool EquipNext()
        {
            return EquipRelative(1);
        }

        public bool EquipPrevious()
        {
            return EquipRelative(-1);
        }

        public bool AddWeapon(Gun gun, bool equipImmediately = true)
        {
            if (gun == null || _weapons.Contains(gun))
            {
                return false;
            }

            _weapons.Add(gun);
            int index = _weapons.Count - 1;
            gun.SetEquipped(false);
            WeaponAdded?.Invoke(gun, index);

            if (equipImmediately || _activeIndex < 0)
            {
                EquipSlot(index);
            }

            return true;
        }

        public bool ContainsDefinition(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null && _weapons[i].Definition == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private bool EquipRelative(int direction)
        {
            if (_weapons.Count == 0)
            {
                return false;
            }

            int start = _activeIndex >= 0 ? _activeIndex : 0;

            for (int step = 1; step <= _weapons.Count; step++)
            {
                int index = (start + direction * step) % _weapons.Count;

                if (index < 0)
                {
                    index += _weapons.Count;
                }

                if (_weapons[index] != null)
                {
                    return EquipSlot(index);
                }
            }

            return false;
        }

        private void OnValidate()
        {
            _startingSlot = Mathf.Max(0, _startingSlot);
        }
    }
}
