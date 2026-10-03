using System;
using System.Collections.Generic;
using Application.Flow;
using Application.Weapons;
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
        private List<Gun> _deploymentWeapons;
        private DeploymentLoadoutSnapshot _deploymentSnapshot;

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

            if (_deploymentSnapshot == null)
                ApplySavedBuildsToAll();

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].SetEquipped(false);
                }
            }

            if (_weapons.Count > 0)
            {
                if (_deploymentSnapshot != null)
                    EquipDeploymentWeapon();
                else
                    EquipSlot(Mathf.Clamp(_startingSlot, 0, _weapons.Count - 1));
            }
        }

        public void ResetToDefault()
        {
            if (_defaultWeapons == null)
            {
                _defaultWeapons = new List<Gun>(_weapons);
            }

            List<Gun> restoreWeapons = _deploymentSnapshot != null && _deploymentWeapons != null
                ? _deploymentWeapons
                : _defaultWeapons;
            for (int i = _weapons.Count - 1; i >= 0; i--)
            {
                Gun gun = _weapons[i];
                if (gun != null && !restoreWeapons.Contains(gun))
                {
                    gun.SetEquipped(false);
                    Destroy(gun.gameObject);
                }
            }

            _weapons.Clear();
            _weapons.AddRange(restoreWeapons);

            if (_deploymentSnapshot != null)
                ApplyDeploymentBuilds(_deploymentSnapshot, Game.Workshop.WeaponBuildApplier.DefaultCatalog);
            else
                ApplySavedBuildsToAll();

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
                if (_deploymentSnapshot != null)
                    EquipDeploymentWeapon();
                else
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

            ApplySavedBuildToGun(gun);

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

        public void ApplySavedBuildsToAll()
        {
            if (_deploymentSnapshot != null) return;

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null)
                {
                    ApplySavedBuildToGun(_weapons[i]);
                }
            }
        }

        public bool ApplyDeploymentSnapshot(DeploymentLoadoutSnapshot snapshot, IWeaponCatalog catalog)
        {
            if (snapshot == null || catalog == null) return false;

            var availableById = new Dictionary<string, Gun>(StringComparer.Ordinal);
            for (int i = 0; i < _weapons.Count; i++)
            {
                Gun gun = _weapons[i];
                if (gun != null && !string.IsNullOrEmpty(gun.WeaponId) && !availableById.ContainsKey(gun.WeaponId))
                    availableById.Add(gun.WeaponId, gun);
            }

            var ordered = new List<Gun>();
            var deployedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string weaponId in snapshot.OrderedWeaponIds)
            {
                if (string.IsNullOrWhiteSpace(weaponId) || !deployedIds.Add(weaponId) ||
                    !availableById.TryGetValue(weaponId, out Gun gun) ||
                    !snapshot.CommittedBuilds.TryGetValue(weaponId, out WeaponBuild build) ||
                    build == null || build.WeaponId != weaponId)
                    return false;
                ordered.Add(gun);
            }

            if (ordered.Count == 0 || !deployedIds.Contains(snapshot.EquippedWeaponId)) return false;

            foreach (Gun gun in _weapons)
            {
                if (gun != null) gun.SetEquipped(false);
            }

            _deploymentSnapshot = snapshot;
            _deploymentWeapons = new List<Gun>(ordered);
            _weapons.Clear();
            _weapons.AddRange(ordered);

            bool buildsApplied = ApplyDeploymentBuilds(snapshot, catalog);
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].ResetRuntimeState();
                    _weapons[i].SetEquipped(false);
                }
            }
            EquipDeploymentWeapon();
            return buildsApplied;
        }

        private bool ApplyDeploymentBuilds(DeploymentLoadoutSnapshot snapshot, IWeaponCatalog catalog)
        {
            if (snapshot == null || catalog == null) return false;
            bool allApplied = true;
            IWeaponBuildResolver resolver = Game.Workshop.WeaponBuildApplier.DefaultResolver;
            foreach (Gun gun in _weapons)
            {
                if (gun == null || !snapshot.CommittedBuilds.TryGetValue(gun.WeaponId, out WeaponBuild build) || build == null)
                {
                    allApplied = false;
                    continue;
                }
                WeaponBuild normalized = resolver.Normalize(build, catalog, out _);
                if (normalized == null || !Game.Workshop.WeaponBuildApplier.ApplyBuild(gun, normalized, catalog, resolver))
                {
                    allApplied = false;
                    Debug.LogWarning($"Could not apply deployed build for weapon '{gun.WeaponId}'.", gun);
                }
            }
            return allApplied;
        }

        private void EquipDeploymentWeapon()
        {
            if (_deploymentSnapshot == null) return;
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] != null && _weapons[i].WeaponId == _deploymentSnapshot.EquippedWeaponId)
                {
                    EquipSlot(i);
                    return;
                }
            }
            if (_weapons.Count > 0) EquipSlot(0);
        }

        private void ApplySavedBuildToGun(Gun gun)
        {
            if (gun != null && gun.Definition != null)
            {
                Game.Workshop.WeaponBuildApplier.ApplySavedBuild(gun);
            }
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
