using System;
using Application.Economy;
using Application.Weapons;

namespace Application.Workshop
{
    public sealed class WorkshopSession : IWeaponWorkshopSession
    {
        private readonly IWeaponCatalog _catalog;
        private readonly IWeaponBuildResolver _resolver;
        private readonly IWeaponBuildStore _store;
        private readonly IEconomyPolicy _economyPolicy;

        public string WeaponId { get; private set; }
        public WeaponBuild CommittedBuild { get; private set; }
        public WeaponBuild DraftBuild { get; private set; }
        public ResolvedWeaponStats DraftStats { get; private set; }
        public BuildResolution DraftResolution { get; private set; }
        public bool HasUnappliedChanges { get; private set; }
        public bool IsValid { get; private set; }
        public bool LastSaveFailed { get; private set; }
        public string LastSaveError { get; private set; }
        public IWeaponBuildTarget Target { get; private set; }
        public IEconomyPolicy EconomyPolicy => _economyPolicy;

        public event Action<IWeaponWorkshopSession> SessionChanged;

        public WorkshopSession(
            string weaponId,
            WeaponBuild initialBuild,
            IWeaponCatalog catalog,
            IWeaponBuildResolver resolver,
            IWeaponBuildStore store,
            IWeaponBuildTarget target = null,
            IEconomyPolicy economyPolicy = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _economyPolicy = economyPolicy ?? EconomyPolicyProvider.DefaultPolicy;

            if (string.IsNullOrWhiteSpace(weaponId) && initialBuild != null)
            {
                weaponId = initialBuild.WeaponId;
            }

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                throw new ArgumentException("WeaponId cannot be null or empty.", nameof(weaponId));
            }

            if (initialBuild == null)
            {
                if (_catalog.TryGetPlatform(weaponId, out var platform))
                {
                    initialBuild = platform.CreateDefaultBuild();
                }
            }

            initialBuild = _resolver.Normalize(initialBuild, _catalog, out _);

            WeaponId = initialBuild?.WeaponId ?? weaponId;
            CommittedBuild = initialBuild;
            DraftBuild = initialBuild;
            Target = target;
            LastSaveFailed = false;
            LastSaveError = null;

            ReevaluateDraft();
        }

        public void SelectPart(string slotId, string partId)
        {
            if (DraftBuild == null) return;

            DraftBuild = DraftBuild.WithSelection(slotId, partId);
            ReevaluateDraft();
            SessionChanged?.Invoke(this);
        }

        public ApplyResult Apply()
        {
            if (!IsValid)
            {
                string errorMsg = DraftResolution?.Errors != null && DraftResolution.Errors.Count > 0
                    ? string.Join("; ", DraftResolution.Errors)
                    : "Draft build is invalid.";
                var affected = DraftResolution?.AffectedSlotOrPartIds;
                return ApplyResult.Failure("InvalidDraft", errorMsg, affected);
            }

            // Gating: check weapon platform ownership
            if (_economyPolicy != null && !_economyPolicy.IsWeaponUnlocked(WeaponId))
            {
                return ApplyResult.Failure("WeaponLocked", $"Weapon platform '{WeaponId}' is locked.");
            }

            // Gating: check part ownership
            if (_economyPolicy != null && DraftBuild?.Selections != null)
            {
                foreach (var kvp in DraftBuild.Selections)
                {
                    string slotId = kvp.Key;
                    string partId = kvp.Value;
                    if (!string.IsNullOrEmpty(partId) && !_economyPolicy.IsPartUnlocked(WeaponId, slotId, partId))
                    {
                        return ApplyResult.Failure("PartUnowned", $"Weapon part '{partId}' is not unlocked or owned.", partId);
                    }
                }
            }

            if (Target != null)
            {
                var targetResult = Target.TryApply(DraftBuild, DraftStats);
                if (targetResult != null && !targetResult.IsSuccess)
                {
                    return targetResult;
                }
            }

            CommittedBuild = DraftBuild;
            HasUnappliedChanges = false;

            if (_store != null)
            {
                var saveResult = _store.Save(CommittedBuild);
                if (saveResult != null && !saveResult.IsSuccess)
                {
                    LastSaveFailed = true;
                    LastSaveError = saveResult.ErrorMessage;
                }
                else
                {
                    LastSaveFailed = false;
                    LastSaveError = null;
                }
            }

            SessionChanged?.Invoke(this);
            return ApplyResult.Success();
        }

        public void Discard()
        {
            DraftBuild = CommittedBuild;
            ReevaluateDraft();
            SessionChanged?.Invoke(this);
        }

        public void BindTarget(IWeaponBuildTarget target)
        {
            Target = target;
            SessionChanged?.Invoke(this);
        }

        public void SwitchWeapon(string weaponId, IWeaponBuildTarget target)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                throw new ArgumentException("WeaponId cannot be null or empty.", nameof(weaponId));
            }

            WeaponBuild loadedBuild = null;
            if (_store != null)
            {
                var loadResult = _store.Load(weaponId);
                if (loadResult != null && loadResult.IsSuccess && loadResult.Build != null)
                {
                    loadedBuild = loadResult.Build;
                }
            }

            if (loadedBuild == null)
            {
                if (_catalog.TryGetPlatform(weaponId, out var platform))
                {
                    loadedBuild = platform.CreateDefaultBuild();
                }
            }

            loadedBuild = _resolver.Normalize(loadedBuild, _catalog, out _);

            WeaponId = loadedBuild?.WeaponId ?? weaponId;
            CommittedBuild = loadedBuild;
            DraftBuild = loadedBuild;
            Target = target;
            LastSaveFailed = false;
            LastSaveError = null;

            ReevaluateDraft();
            SessionChanged?.Invoke(this);
        }

        private void ReevaluateDraft()
        {
            DraftResolution = _resolver.Resolve(DraftBuild, _catalog);
            IsValid = DraftResolution != null && DraftResolution.IsValid;
            DraftStats = IsValid ? DraftResolution.Stats : null;
            HasUnappliedChanges = CommittedBuild == null || !CommittedBuild.Equals(DraftBuild);
        }
    }
}
