using System;
using Application.Flow;
using Application.Weapons;

namespace Game
{
    /// <summary>
    /// Keeps committed workshop builds in the app-lifetime session while forwarding
    /// durable writes to the configured save store.
    /// </summary>
    public sealed class SessionWeaponBuildStore : IWeaponBuildStore
    {
        private readonly PlayerSession _session;
        private readonly IWeaponBuildStore _durableStore;

        public SessionWeaponBuildStore(PlayerSession session, IWeaponBuildStore durableStore)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _durableStore = durableStore ?? throw new ArgumentNullException(nameof(durableStore));
        }

        public BuildLoadResult Load(string weaponId)
        {
            WeaponBuild committed = _session.GetCommittedBuild(weaponId);
            if (committed != null) return BuildLoadResult.Success(committed);
            BuildLoadResult loaded = _durableStore.Load(weaponId);
            if (loaded.IsSuccess && loaded.Build != null)
                _session.SetDurableBuild(weaponId, loaded.Build);
            return loaded;
        }

        public SaveResult Save(WeaponBuild build)
        {
            if (build == null) return SaveResult.Failure("Weapon build cannot be null.");
            if (string.IsNullOrWhiteSpace(build.WeaponId))
                return SaveResult.Failure("Weapon build must have a valid WeaponId.");
            SaveResult saved;
            try { saved = _durableStore.Save(build); }
            catch (Exception ex) { saved = SaveResult.Failure(ex.Message); }
            if (saved != null && saved.IsSuccess)
            {
                _session.SetDurableBuild(build.WeaponId, build);
                return saved;
            }
            _session.SetPendingBuild(build.WeaponId, build);
            return saved ?? SaveResult.Failure("Build store returned no save result.");
        }

        /// <summary>Retries each pending build and leaves failures pending for a later retry.</summary>
        public int RetryPendingSaves()
        {
            int savedCount = 0;
            foreach (string weaponId in _session.GetPendingWeaponIds())
            {
                WeaponBuild build = _session.GetCommittedBuild(weaponId);
                if (build == null) continue;
                SaveResult result;
                try { result = _durableStore.Save(build); }
                catch { continue; }
                if (result != null && result.IsSuccess)
                {
                    _session.SetDurableBuild(weaponId, build);
                    savedCount++;
                }
            }
            return savedCount;
        }
    }
}
