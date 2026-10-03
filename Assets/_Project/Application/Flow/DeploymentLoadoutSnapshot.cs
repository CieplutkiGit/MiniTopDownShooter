using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Application.Weapons;

namespace Application.Flow
{
    public sealed class DeploymentLoadoutSnapshot
    {
        private readonly ReadOnlyCollection<string> _orderedWeaponIds;
        private readonly ReadOnlyDictionary<string, WeaponBuild> _committedBuilds;

        public IReadOnlyList<string> OrderedWeaponIds => _orderedWeaponIds;
        public string EquippedWeaponId { get; }
        public IReadOnlyDictionary<string, WeaponBuild> CommittedBuilds => _committedBuilds;

        public DeploymentLoadoutSnapshot(
            IEnumerable<string> orderedWeaponIds,
            string equippedWeaponId,
            IDictionary<string, WeaponBuild> committedBuilds)
        {
            var weaponList = orderedWeaponIds != null
                ? orderedWeaponIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList()
                : new List<string>();
            _orderedWeaponIds = new ReadOnlyCollection<string>(weaponList);

            if (!string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                EquippedWeaponId = equippedWeaponId;
            }
            else if (weaponList.Count > 0)
            {
                EquippedWeaponId = weaponList[0];
            }
            else
            {
                EquippedWeaponId = string.Empty;
            }

            var buildDict = new Dictionary<string, WeaponBuild>(StringComparer.Ordinal);
            if (committedBuilds != null)
            {
                foreach (var kvp in committedBuilds)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                    {
                        buildDict[kvp.Key] = kvp.Value;
                    }
                }
            }
            _committedBuilds = new ReadOnlyDictionary<string, WeaponBuild>(buildDict);
        }

        public static DeploymentLoadoutSnapshot Empty { get; } = new DeploymentLoadoutSnapshot(
            Array.Empty<string>(),
            string.Empty,
            new Dictionary<string, WeaponBuild>());
    }
}
