using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Application.Weapons
{
    public sealed class WeaponBuild : IEquatable<WeaponBuild>
    {
        private readonly ReadOnlyDictionary<string, string> _selections;

        public string WeaponId { get; }
        public IReadOnlyDictionary<string, string> Selections => _selections;

        public WeaponBuild(string weaponId) : this(weaponId, null)
        {
        }

        public WeaponBuild(string weaponId, IDictionary<string, string> selections)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                throw new ArgumentException("WeaponId cannot be null or empty.", nameof(weaponId));
            }

            WeaponId = weaponId;

            // Defensive copy into a sorted dictionary for deterministic ordering
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (selections != null)
            {
                foreach (var kvp in selections)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                    {
                        sorted[kvp.Key] = kvp.Value;
                    }
                }
            }
            _selections = new ReadOnlyDictionary<string, string>(sorted);
        }

        public string GetPart(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            return _selections.TryGetValue(slotId, out string partId) ? partId : null;
        }

        public bool HasSlot(string slotId)
        {
            return !string.IsNullOrEmpty(slotId) && _selections.ContainsKey(slotId);
        }

        public WeaponBuild WithSelection(string slotId, string partId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("SlotId cannot be null or empty.", nameof(slotId));
            }

            var dict = new Dictionary<string, string>(_selections);
            if (string.IsNullOrWhiteSpace(partId))
            {
                dict.Remove(slotId);
            }
            else
            {
                dict[slotId] = partId;
            }

            return new WeaponBuild(WeaponId, dict);
        }

        public bool Equals(WeaponBuild other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!string.Equals(WeaponId, other.WeaponId, StringComparison.Ordinal)) return false;
            if (_selections.Count != other._selections.Count) return false;

            foreach (var kvp in _selections)
            {
                if (!other._selections.TryGetValue(kvp.Key, out string otherVal) ||
                    !string.Equals(kvp.Value, otherVal, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is WeaponBuild other && Equals(other));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (WeaponId != null ? StringComparer.Ordinal.GetHashCode(WeaponId) : 0);
                foreach (var kvp in _selections)
                {
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(kvp.Key);
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(kvp.Value);
                }
                return hash;
            }
        }

        public override string ToString()
        {
            string parts = string.Join(", ", _selections.Select(kv => $"{kv.Key}:{kv.Value}"));
            return $"WeaponBuild[{WeaponId}] {{{parts}}}";
        }
    }
}
