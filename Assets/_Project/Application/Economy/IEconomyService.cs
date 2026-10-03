using System;
using System.Collections.Generic;

namespace Application.Economy
{
    public interface IEconomyService : IEconomyPolicy
    {
        // ── Wallet & Progression ───────────────────────────────────────────
        int Coins { get; }
        int Xp { get; }
        int Level { get; }
        int XpInCurrentLevel { get; }
        int XpForNextLevel { get; }
        float LevelProgressNormalized { get; }

        // ── Inventories ────────────────────────────────────────────────────
        int GetComponentCount(string componentId);
        IReadOnlyDictionary<string, int> GetAllComponents();
        IReadOnlyList<string> UnlockedWeaponIds { get; }
        IReadOnlyList<string> UnlockedPartIds { get; }

        // ── Queries & Costs ────────────────────────────────────────────────
        bool CanUnlockWeapon(string weaponId, out string lockedReason);
        WeaponPlatformCost GetWeaponCost(string weaponId);

        bool CanCraftPart(string weaponId, string slotId, string partId, out string lockedReason);
        CraftingRecipe GetPartRecipe(string partId);
        int GetComponentSellPrice(string componentId);

        // ── Transactions ───────────────────────────────────────────────────
        EconomyOperationResult TryUnlockWeapon(string weaponId);
        EconomyOperationResult TryCraftPart(string weaponId, string slotId, string partId);
        EconomyOperationResult TrySellComponents(string componentId, int count);

        // ── Event Updates ──────────────────────────────────────────────────
        event Action OnEconomyStateChanged;
        event Action<string> OnWeaponUnlocked;
        event Action<string> OnPartCrafted;
        event Action<string, int, int> OnComponentsSold;
        event Action<int, int> OnXpEarned;
    }
}
