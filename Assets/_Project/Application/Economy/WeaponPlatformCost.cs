using System;

namespace Application.Economy
{
    public sealed class WeaponPlatformCost
    {
        public string WeaponId { get; }
        public string DisplayName { get; }
        public int RequiredLevel { get; }
        public int CoinCost { get; }
        public bool IsStarter => RequiredLevel <= 1 && CoinCost == 0;

        public WeaponPlatformCost(string weaponId, string displayName, int requiredLevel, int coinCost)
        {
            WeaponId = weaponId ?? throw new ArgumentNullException(nameof(weaponId));
            DisplayName = displayName ?? weaponId;
            RequiredLevel = Math.Max(1, requiredLevel);
            CoinCost = Math.Max(0, coinCost);
        }

        public bool CanUnlock(int playerLevel, int playerCoins, out string reason)
        {
            if (playerLevel < RequiredLevel)
            {
                reason = $"Requires Player Level {RequiredLevel} (Current: {playerLevel}).";
                return false;
            }
            if (playerCoins < CoinCost)
            {
                reason = $"Requires {CoinCost} Coins (Current: {playerCoins}).";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
