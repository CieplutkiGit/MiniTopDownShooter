using System;
using System.Collections.Generic;
using Application.Weapons;

namespace Application.Flow
{
    /// <summary>
    /// Application-layer data bag that owns the player's committed builds,
    /// ordered loadout weapon IDs, and the currently equipped weapon ID.
    /// Lives for the entire app lifetime; survives scene changes.
    /// Unity types never appear here.
    /// </summary>
    public class PlayerSession
    {
        // Ordered list of weapon IDs in the loadout (slot 0, 1, 2 …)
        private readonly List<string> _loadoutIds = new List<string>();

        // Committed builds keyed by weapon ID
        private readonly Dictionary<string, WeaponBuild> _committedBuilds
            = new Dictionary<string, WeaponBuild>(StringComparer.Ordinal);

        // Pending committed builds that could not yet be durably saved
        private readonly Dictionary<string, WeaponBuild> _pendingBuilds
            = new Dictionary<string, WeaponBuild>(StringComparer.Ordinal);

        public string EquippedWeaponId { get; private set; }

        public IReadOnlyList<string> LoadoutIds => _loadoutIds;

        public bool HasPendingSaves => _pendingBuilds.Count > 0;

        // ── Loadout management ─────────────────────────────────────────────

        public void SetLoadout(IEnumerable<string> orderedWeaponIds, string equippedWeaponId)
        {
            _loadoutIds.Clear();
            if (orderedWeaponIds != null)
            {
                foreach (string id in orderedWeaponIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        _loadoutIds.Add(id);
                }
            }

            EquippedWeaponId = equippedWeaponId ?? (_loadoutIds.Count > 0 ? _loadoutIds[0] : null);
        }

        public void SetEquippedWeapon(string weaponId)
        {
            EquippedWeaponId = weaponId;
        }

        // ── Committed builds ───────────────────────────────────────────────

        /// <summary>
        /// Returns the committed build for <paramref name="weaponId"/>, or null if none.
        /// Pending (unsaved) builds are authoritative over durable builds.
        /// </summary>
        public WeaponBuild GetCommittedBuild(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return null;

            if (_pendingBuilds.TryGetValue(weaponId, out WeaponBuild pending))
                return pending;

            _committedBuilds.TryGetValue(weaponId, out WeaponBuild build);
            return build;
        }

        /// <summary>
        /// Records a durably saved committed build. Removes it from the pending set.
        /// </summary>
        public void SetDurableBuild(string weaponId, WeaponBuild build)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || build == null) return;
            _committedBuilds[weaponId] = build;
            _pendingBuilds.Remove(weaponId);
        }

        /// <summary>
        /// Records a committed build that could not yet be durably saved.
        /// The build is authoritative and will be retried later.
        /// </summary>
        public void SetPendingBuild(string weaponId, WeaponBuild build)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || build == null) return;
            _pendingBuilds[weaponId] = build;
        }

        /// <summary>
        /// Returns all weapon IDs that have pending (unsaved) builds.
        /// </summary>
        public IReadOnlyList<string> GetPendingWeaponIds()
        {
            var list = new List<string>(_pendingBuilds.Keys);
            return list;
        }

        /// <summary>
        /// Creates an immutable snapshot of the current loadout for deployment.
        /// Pending builds are included so the run uses the latest committed state.
        /// </summary>
        public DeploymentLoadoutSnapshot CreateDeploymentSnapshot()
        {
            var buildsByWeapon = new Dictionary<string, WeaponBuild>(StringComparer.Ordinal);
            // First copy durable builds, then overwrite with pending (authoritative)
            foreach (var kv in _committedBuilds)
                buildsByWeapon[kv.Key] = kv.Value;
            foreach (var kv in _pendingBuilds)
                buildsByWeapon[kv.Key] = kv.Value;

            return new DeploymentLoadoutSnapshot(
                new List<string>(_loadoutIds),
                EquippedWeaponId,
                buildsByWeapon);
        }
    }
}
