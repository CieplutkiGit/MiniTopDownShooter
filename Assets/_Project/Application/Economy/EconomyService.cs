using System;
using System.Collections.Generic;
using Application.Weapons;

namespace Application.Economy
{
    public class EconomyService : IEconomyService
    {
        private readonly Func<UserProfileData> _profileLoader;
        private readonly Func<UserProfileData, bool> _profileSaver;
        private readonly IWeaponCatalog _catalog;

        public event Action OnEconomyStateChanged;
        public event Action<string> OnWeaponUnlocked;
        public event Action<string> OnPartCrafted;
        public event Action<string, int, int> OnComponentsSold;
        public event Action<int, int> OnXpEarned;

        public EconomyService(Func<UserProfileData> profileLoader, Func<UserProfileData, bool> profileSaver, IWeaponCatalog catalog = null)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _profileSaver = profileSaver ?? throw new ArgumentNullException(nameof(profileSaver));
            _catalog = catalog;
        }

        private UserProfileData GetProfile()
        {
            UserProfileData profile = _profileLoader();
            if (profile == null)
            {
                profile = new UserProfileData();
            }
            profile.ValidateAndMigrate();
            return profile;
        }

        // ── Wallet & Progression ───────────────────────────────────────────
        public int Coins => GetProfile().Coins;
        public int Xp => GetProfile().Xp;
        public int Level => GetProfile().Level;

        public int XpInCurrentLevel
        {
            get
            {
                EconomyLevelCurve.GetLevelProgress(Xp, out _, out int inLevel, out _, out _);
                return inLevel;
            }
        }

        public int XpForNextLevel
        {
            get
            {
                EconomyLevelCurve.GetLevelProgress(Xp, out _, out _, out int forNext, out _);
                return forNext;
            }
        }

        public float LevelProgressNormalized
        {
            get
            {
                EconomyLevelCurve.GetLevelProgress(Xp, out _, out _, out _, out float normalized);
                return normalized;
            }
        }

        // ── Inventories ────────────────────────────────────────────────────
        public int GetComponentCount(string componentId)
        {
            return GetProfile().GetComponentCount(componentId);
        }

        public IReadOnlyDictionary<string, int> GetAllComponents()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            UserProfileData profile = GetProfile();
            if (profile.Components != null)
            {
                foreach (var entry in profile.Components)
                {
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.ComponentId))
                    {
                        dict[entry.ComponentId] = entry.Count;
                    }
                }
            }
            return dict;
        }

        public IReadOnlyList<string> UnlockedWeaponIds => GetProfile().UnlockedWeaponIds;
        public IReadOnlyList<string> UnlockedPartIds => GetProfile().UnlockedPartIds;

        // ── Queries & Costs ────────────────────────────────────────────────
        public bool IsWeaponUnlocked(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return false;
            return GetProfile().IsWeaponUnlocked(weaponId);
        }

        public bool CanUnlockWeapon(string weaponId, out string lockedReason)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                lockedReason = "Invalid weapon ID.";
                return false;
            }

            if (IsWeaponUnlocked(weaponId))
            {
                lockedReason = "Weapon already unlocked.";
                return false;
            }

            UserProfileData profile = GetProfile();
            WeaponPlatformCost cost = EconomyPricingPolicy.GetPlatformCost(weaponId);
            return cost.CanUnlock(profile.Level, profile.Coins, out lockedReason);
        }

        public WeaponPlatformCost GetWeaponCost(string weaponId)
        {
            return EconomyPricingPolicy.GetPlatformCost(weaponId);
        }

        public bool IsPartUnlocked(string weaponId, string slotId, string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return true;

            // Default parts are inherently owned
            if (EconomyPricingPolicy.IsDefaultPart(partId, weaponId, slotId, _catalog)) return true;

            return GetProfile().IsPartUnlocked(partId);
        }

        public bool CanCraftPart(string weaponId, string slotId, string partId, out string lockedReason)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                lockedReason = "Invalid part ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                lockedReason = "Invalid weapon ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(slotId))
            {
                lockedReason = "Invalid slot ID.";
                return false;
            }

            if (!EconomyPricingPolicy.IsKnownPlatform(weaponId))
            {
                lockedReason = $"Weapon platform '{weaponId}' is not recognized.";
                return false;
            }

            if (!EconomyPricingPolicy.IsValidPart(weaponId, slotId, partId, _catalog))
            {
                lockedReason = $"Part '{partId}' is not valid for slot '{slotId}' on platform '{weaponId}'.";
                return false;
            }

            if (EconomyPricingPolicy.IsDefaultPart(partId, weaponId, slotId, _catalog))
            {
                lockedReason = "Starter/default parts cannot be crafted (already owned).";
                return false;
            }

            if (!IsWeaponUnlocked(weaponId))
            {
                lockedReason = "Weapon platform is locked.";
                return false;
            }

            if (IsPartUnlocked(weaponId, slotId, partId))
            {
                lockedReason = "Part is already owned.";
                return false;
            }

            CraftingRecipe recipe = EconomyPricingPolicy.GetPartRecipe(partId);
            UserProfileData profile = GetProfile();

            int scrap = profile.GetComponentCount(EconomyComponentExtensions.ScrapId);
            int alloy = profile.GetComponentCount(EconomyComponentExtensions.AlloyId);
            int core = profile.GetComponentCount(EconomyComponentExtensions.CoreId);

            if (!recipe.CanAfford(scrap, alloy, core))
            {
                var missing = new List<string>();
                if (scrap < recipe.ScrapCost) missing.Add($"{recipe.ScrapCost - scrap} Scrap");
                if (alloy < recipe.AlloyCost) missing.Add($"{recipe.AlloyCost - alloy} Alloy");
                if (core < recipe.CoreCost) missing.Add($"{recipe.CoreCost - core} Energy Core");
                lockedReason = $"Need: {string.Join(", ", missing)}.";
                return false;
            }

            lockedReason = string.Empty;
            return true;
        }

        public CraftingRecipe GetPartRecipe(string partId)
        {
            return EconomyPricingPolicy.GetPartRecipe(partId);
        }

        public int GetComponentSellPrice(string componentId)
        {
            return EconomyPricingPolicy.GetSellPrice(componentId);
        }

        // ── Transactions with Save Failure Rollback ────────────────────────
        public EconomyOperationResult TryUnlockWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return EconomyOperationResult.Failure("InvalidId", "Weapon ID cannot be null or empty.");
            }

            if (!EconomyPricingPolicy.IsKnownPlatform(weaponId))
            {
                return EconomyOperationResult.Failure("UnknownWeapon", $"Weapon '{weaponId}' is not a valid weapon platform.");
            }

            UserProfileData profile = GetProfile();
            if (profile.IsWeaponUnlocked(weaponId))
            {
                return EconomyOperationResult.Failure("AlreadyUnlocked", $"Weapon '{weaponId}' is already unlocked.");
            }

            WeaponPlatformCost cost = EconomyPricingPolicy.GetPlatformCost(weaponId);
            if (!cost.CanUnlock(profile.Level, profile.Coins, out string reason))
            {
                return EconomyOperationResult.Failure("RequirementsNotMet", reason);
            }

            // Deduct & unlock tentatively
            int previousCoins = profile.Coins;
            profile.Coins -= cost.CoinCost;
            profile.UnlockWeapon(weaponId);

            // Attempt save with exception rollback
            bool saved = false;
            try
            {
                saved = _profileSaver(profile);
            }
            catch (Exception)
            {
                saved = false;
            }

            if (!saved)
            {
                profile.Coins = previousCoins;
                profile.UnlockedWeaponIds.Remove(weaponId);
                return EconomyOperationResult.Failure("SaveFailed", "Failed to persist transaction. Resources preserved.");
            }

            OnWeaponUnlocked?.Invoke(weaponId);
            OnEconomyStateChanged?.Invoke();
            return EconomyOperationResult.Success();
        }

        public EconomyOperationResult TryCraftPart(string weaponId, string slotId, string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                return EconomyOperationResult.Failure("InvalidId", "Part ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return EconomyOperationResult.Failure("InvalidId", "Weapon platform ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(slotId))
            {
                return EconomyOperationResult.Failure("InvalidId", "Slot ID cannot be null or empty.");
            }

            if (!EconomyPricingPolicy.IsKnownPlatform(weaponId))
            {
                return EconomyOperationResult.Failure("UnknownWeapon", $"Weapon platform '{weaponId}' is not a valid weapon platform.");
            }

            if (!EconomyPricingPolicy.IsValidPart(weaponId, slotId, partId, _catalog))
            {
                return EconomyOperationResult.Failure("InvalidPart", $"Part '{partId}' is not valid for slot '{slotId}' on platform '{weaponId}'.");
            }

            if (EconomyPricingPolicy.IsDefaultPart(partId, weaponId, slotId, _catalog))
            {
                return EconomyOperationResult.Failure("DefaultPart", $"Part '{partId}' is a default starter part and cannot be crafted.");
            }

            UserProfileData profile = GetProfile();
            if (!profile.IsWeaponUnlocked(weaponId))
            {
                return EconomyOperationResult.Failure("WeaponLocked", $"Weapon platform '{weaponId}' is locked.");
            }

            if (profile.IsPartUnlocked(partId))
            {
                return EconomyOperationResult.Failure("AlreadyOwned", $"Part '{partId}' is already owned.");
            }

            CraftingRecipe recipe = EconomyPricingPolicy.GetPartRecipe(partId);
            int scrap = profile.GetComponentCount(EconomyComponentExtensions.ScrapId);
            int alloy = profile.GetComponentCount(EconomyComponentExtensions.AlloyId);
            int core = profile.GetComponentCount(EconomyComponentExtensions.CoreId);

            if (!recipe.CanAfford(scrap, alloy, core))
            {
                return EconomyOperationResult.Failure("InsufficientComponents", "Player does not have enough components for this upgrade.");
            }

            // Tentatively spend components and grant part
            int prevScrap = scrap;
            int prevAlloy = alloy;
            int prevCore = core;
            profile.TryRemoveComponents(EconomyComponentExtensions.ScrapId, recipe.ScrapCost);
            profile.TryRemoveComponents(EconomyComponentExtensions.AlloyId, recipe.AlloyCost);
            profile.TryRemoveComponents(EconomyComponentExtensions.CoreId, recipe.CoreCost);
            profile.UnlockPart(partId);

            // Attempt save with exception rollback
            bool saved = false;
            try
            {
                saved = _profileSaver(profile);
            }
            catch (Exception)
            {
                saved = false;
            }

            if (!saved)
            {
                profile.SetComponentCount(EconomyComponentExtensions.ScrapId, prevScrap);
                profile.SetComponentCount(EconomyComponentExtensions.AlloyId, prevAlloy);
                profile.SetComponentCount(EconomyComponentExtensions.CoreId, prevCore);
                profile.UnlockedPartIds.Remove(partId);
                return EconomyOperationResult.Failure("SaveFailed", "Failed to persist crafted part. Resources preserved.");
            }

            OnPartCrafted?.Invoke(partId);
            OnEconomyStateChanged?.Invoke();
            return EconomyOperationResult.Success();
        }

        public EconomyOperationResult TrySellComponents(string componentId, int count)
        {
            if (string.IsNullOrWhiteSpace(componentId) || count <= 0)
            {
                return EconomyOperationResult.Failure("InvalidParameters", "Count must be greater than zero.");
            }

            if (!EconomyPricingPolicy.IsKnownComponent(componentId))
            {
                return EconomyOperationResult.Failure("UnknownComponent", $"Component '{componentId}' is not a valid tradeable component.");
            }

            UserProfileData profile = GetProfile();
            int currentCount = profile.GetComponentCount(componentId);
            if (currentCount < count)
            {
                return EconomyOperationResult.Failure("InsufficientComponents", $"Only have {currentCount} of {componentId}. Cannot sell {count}.");
            }

            int sellPrice = EconomyPricingPolicy.GetSellPrice(componentId);
            long totalCoinsLong = (long)count * sellPrice;
            int totalCoins = totalCoinsLong > int.MaxValue ? int.MaxValue : (int)totalCoinsLong;

            int prevCount = currentCount;
            int prevCoins = profile.Coins;

            // Tentatively deduct components and grant coins
            profile.TryRemoveComponents(componentId, count);
            profile.AddCoins(totalCoins);

            // Attempt save with exception rollback
            bool saved = false;
            try
            {
                saved = _profileSaver(profile);
            }
            catch (Exception)
            {
                saved = false;
            }

            if (!saved)
            {
                profile.SetComponentCount(componentId, prevCount);
                profile.Coins = prevCoins;
                return EconomyOperationResult.Failure("SaveFailed", "Failed to persist sale. Inventory preserved.");
            }

            OnComponentsSold?.Invoke(componentId, count, totalCoins);
            OnEconomyStateChanged?.Invoke();
            return EconomyOperationResult.Success();
        }
    }
}
