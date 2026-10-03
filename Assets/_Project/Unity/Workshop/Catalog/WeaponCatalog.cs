using System;
using System.Collections.Generic;
using System.Linq;
using Application.Weapons;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Mini Top Down Shooter/Workshop/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject, IWeaponCatalog
    {
        [SerializeField] private List<WeaponPlatformDefinition> _platforms = new List<WeaponPlatformDefinition>();
        [SerializeField] private List<WeaponPartDefinition> _parts = new List<WeaponPartDefinition>();

        private readonly object _lock = new object();
        private Dictionary<string, WeaponPlatformSpec> _platformSpecs;
        private Dictionary<string, WeaponPartSpec> _partSpecs;
        private Dictionary<string, List<WeaponPartSpec>> _partsBySlot;
        private List<string> _weaponIds;

        public IReadOnlyList<WeaponPlatformDefinition> PlatformDefinitions =>
            _platforms != null ? _platforms.AsReadOnly() : (IReadOnlyList<WeaponPlatformDefinition>)Array.Empty<WeaponPlatformDefinition>();

        public IReadOnlyList<WeaponPartDefinition> PartDefinitions =>
            _parts != null ? _parts.AsReadOnly() : (IReadOnlyList<WeaponPartDefinition>)Array.Empty<WeaponPartDefinition>();

        private void OnEnable()
        {
            RebuildCache();
        }

        public void Initialize(IEnumerable<WeaponPlatformDefinition> platforms, IEnumerable<WeaponPartDefinition> parts)
        {
            _platforms = platforms != null ? platforms.ToList() : new List<WeaponPlatformDefinition>();
            _parts = parts != null ? parts.ToList() : new List<WeaponPartDefinition>();
            RebuildCache();
        }

        public void SetPlatforms(IEnumerable<WeaponPlatformDefinition> platforms)
        {
            _platforms = platforms != null ? platforms.ToList() : new List<WeaponPlatformDefinition>();
            RebuildCache();
        }

        public void SetParts(IEnumerable<WeaponPartDefinition> parts)
        {
            _parts = parts != null ? parts.ToList() : new List<WeaponPartDefinition>();
            RebuildCache();
        }

        public void RebuildCache()
        {
            lock (_lock)
            {
                var platformDict = new Dictionary<string, WeaponPlatformSpec>(StringComparer.Ordinal);
                var partDict = new Dictionary<string, WeaponPartSpec>(StringComparer.Ordinal);
                var slotDict = new Dictionary<string, List<WeaponPartSpec>>(StringComparer.Ordinal);
                var ids = new List<string>();

                if (_platforms != null)
                {
                    foreach (var platformDef in _platforms)
                    {
                        if (platformDef == null) continue;
                        var spec = platformDef.ToSpec();
                        if (string.IsNullOrEmpty(spec.WeaponId))
                        {
                            throw new ArgumentException("Weapon platform ID cannot be null or empty.");
                        }

                        if (platformDict.ContainsKey(spec.WeaponId))
                        {
                            throw new ArgumentException($"Duplicate weapon platform ID detected: '{spec.WeaponId}'.");
                        }

                        platformDict.Add(spec.WeaponId, spec);
                        ids.Add(spec.WeaponId);
                    }
                }

                if (_parts != null)
                {
                    foreach (var partDef in _parts)
                    {
                        if (partDef == null) continue;
                        var spec = partDef.ToSpec();
                        if (string.IsNullOrEmpty(spec.PartId))
                        {
                            throw new ArgumentException("Weapon part ID cannot be null or empty.");
                        }

                        if (partDict.ContainsKey(spec.PartId))
                        {
                            throw new ArgumentException($"Duplicate weapon part ID detected: '{spec.PartId}'.");
                        }

                        partDict.Add(spec.PartId, spec);

                        if (!string.IsNullOrEmpty(spec.SlotId))
                        {
                            if (!slotDict.TryGetValue(spec.SlotId, out var list))
                            {
                                list = new List<WeaponPartSpec>();
                                slotDict[spec.SlotId] = list;
                            }
                            list.Add(spec);
                        }
                    }
                }

                _platformSpecs = platformDict;
                _partSpecs = partDict;
                _partsBySlot = slotDict;
                _weaponIds = ids;
            }
        }

        private void EnsureCache()
        {
            if (_platformSpecs == null || _partSpecs == null)
            {
                RebuildCache();
            }
        }

        public WeaponPlatformSpec GetPlatform(string weaponId)
        {
            if (weaponId == null) throw new ArgumentNullException(nameof(weaponId));
            EnsureCache();

            if (_platformSpecs.TryGetValue(weaponId, out var platform))
            {
                return platform;
            }

            throw new KeyNotFoundException($"Platform '{weaponId}' was not found in catalog.");
        }

        public bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform)
        {
            if (weaponId == null)
            {
                platform = null;
                return false;
            }

            EnsureCache();
            return _platformSpecs.TryGetValue(weaponId, out platform);
        }

        public WeaponPartSpec GetPart(string partId)
        {
            if (partId == null) throw new ArgumentNullException(nameof(partId));
            EnsureCache();

            if (_partSpecs.TryGetValue(partId, out var part))
            {
                return part;
            }

            throw new KeyNotFoundException($"Part '{partId}' was not found in catalog.");
        }

        public bool TryGetPart(string partId, out WeaponPartSpec part)
        {
            if (partId == null)
            {
                part = null;
                return false;
            }

            EnsureCache();
            return _partSpecs.TryGetValue(partId, out part);
        }

        public IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return Array.Empty<WeaponPartSpec>();
            }

            EnsureCache();

            if (!string.IsNullOrEmpty(weaponId) && _platformSpecs.TryGetValue(weaponId, out var platform))
            {
                if (platform.SupportedSlots != null && !platform.SupportedSlots.Contains(slotId))
                {
                    return Array.Empty<WeaponPartSpec>();
                }
            }

            if (_partsBySlot.TryGetValue(slotId, out var list))
            {
                return list.AsReadOnly();
            }

            return Array.Empty<WeaponPartSpec>();
        }

        public IReadOnlyList<string> GetAllWeaponIds()
        {
            EnsureCache();
            return _weaponIds.AsReadOnly();
        }

        public CatalogValidationReport Validate()
        {
            return WeaponCatalogValidation.Validate(this);
        }
    }
}
