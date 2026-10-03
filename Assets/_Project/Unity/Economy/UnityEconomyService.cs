using System;
using System.Collections.Generic;
using Application;
using Application.Economy;
using UnityEngine;

namespace Game.Economy
{
    public class UnityEconomyService : IEconomyService
    {
        private static UnityEconomyService s_instance;
        public static UnityEconomyService Instance => s_instance ?? (s_instance = new UnityEconomyService());

        private readonly EconomyService _innerService;

        public UnityEconomyService()
        {
            _innerService = new EconomyService(
                () => SaveManager.LoadProfile(),
                profile => SaveManager.SaveProfile(profile),
                Game.Workshop.WeaponBuildApplier.DefaultCatalog);

            _innerService.OnEconomyStateChanged += () => OnEconomyStateChanged?.Invoke();
            _innerService.OnWeaponUnlocked += id => OnWeaponUnlocked?.Invoke(id);
            _innerService.OnPartCrafted += id => OnPartCrafted?.Invoke(id);
            _innerService.OnComponentsSold += (id, count, coins) => OnComponentsSold?.Invoke(id, count, coins);
            _innerService.OnXpEarned += (xp, lvl) => OnXpEarned?.Invoke(xp, lvl);
        }

        public static void SetInstanceForTesting(UnityEconomyService instance)
        {
            s_instance = instance;
        }

        public static void ResetInstance()
        {
            s_instance = null;
        }

        // ── IEconomyService delegation ─────────────────────────────────────
        public int Coins => _innerService.Coins;
        public int Xp => _innerService.Xp;
        public int Level => _innerService.Level;
        public int XpInCurrentLevel => _innerService.XpInCurrentLevel;
        public int XpForNextLevel => _innerService.XpForNextLevel;
        public float LevelProgressNormalized => _innerService.LevelProgressNormalized;

        public int GetComponentCount(string componentId) => _innerService.GetComponentCount(componentId);
        public IReadOnlyDictionary<string, int> GetAllComponents() => _innerService.GetAllComponents();
        public IReadOnlyList<string> UnlockedWeaponIds => _innerService.UnlockedWeaponIds;
        public IReadOnlyList<string> UnlockedPartIds => _innerService.UnlockedPartIds;

        public bool IsWeaponUnlocked(string weaponId) => _innerService.IsWeaponUnlocked(weaponId);
        public bool CanUnlockWeapon(string weaponId, out string lockedReason) => _innerService.CanUnlockWeapon(weaponId, out lockedReason);
        public WeaponPlatformCost GetWeaponCost(string weaponId) => _innerService.GetWeaponCost(weaponId);

        public bool IsPartUnlocked(string weaponId, string slotId, string partId) => _innerService.IsPartUnlocked(weaponId, slotId, partId);
        public bool CanCraftPart(string weaponId, string slotId, string partId, out string lockedReason) => _innerService.CanCraftPart(weaponId, slotId, partId, out lockedReason);
        public CraftingRecipe GetPartRecipe(string partId) => _innerService.GetPartRecipe(partId);
        public int GetComponentSellPrice(string componentId) => _innerService.GetComponentSellPrice(componentId);

        public EconomyOperationResult TryUnlockWeapon(string weaponId) => _innerService.TryUnlockWeapon(weaponId);
        public EconomyOperationResult TryCraftPart(string weaponId, string slotId, string partId) => _innerService.TryCraftPart(weaponId, slotId, partId);
        public EconomyOperationResult TrySellComponents(string componentId, int count) => _innerService.TrySellComponents(componentId, count);

        public event Action OnEconomyStateChanged;
        public event Action<string> OnWeaponUnlocked;
        public event Action<string> OnPartCrafted;
        public event Action<string, int, int> OnComponentsSold;
        public event Action<int, int> OnXpEarned;
    }
}
