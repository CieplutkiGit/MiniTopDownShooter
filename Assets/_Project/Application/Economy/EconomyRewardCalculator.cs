using System;
using Application.Flow;

namespace Application.Economy
{
    public static class EconomyRewardCalculator
    {
        public static void CalculateReward(
            RunResult result,
            out int totalCoins,
            out int totalXp,
            out int scrap,
            out int alloy,
            out int core)
        {
            if (result == null)
            {
                totalCoins = 0;
                totalXp = 0;
                scrap = 0;
                alloy = 0;
                core = 0;
                return;
            }

            scrap = Math.Max(0, result.ScrapCollected);
            alloy = Math.Max(0, result.AlloyCollected);
            core = Math.Max(0, result.CoreCollected);

            if (result.EarnedCoins > 0 || result.EarnedXp > 0)
            {
                totalCoins = Math.Max(0, result.EarnedCoins);
                totalXp = Math.Max(0, result.EarnedXp);
                return;
            }

            int baseCoins;
            int killCoinMultiplier;
            int waveCoinMultiplier;

            int baseXp;
            int killXpMultiplier;
            int waveXpMultiplier;

            switch (result.Outcome)
            {
                case RunOutcome.Victory:
                    baseCoins = 50;
                    killCoinMultiplier = 2;
                    waveCoinMultiplier = 10;
                    baseXp = 150;
                    killXpMultiplier = 5;
                    waveXpMultiplier = 25;
                    break;

                case RunOutcome.Defeat:
                    // Fair failure reward!
                    baseCoins = 20;
                    killCoinMultiplier = 1;
                    waveCoinMultiplier = 5;
                    baseXp = 50;
                    killXpMultiplier = 5;
                    waveXpMultiplier = 12;
                    break;

                case RunOutcome.Abandoned:
                    baseCoins = 5;
                    killCoinMultiplier = 1;
                    waveCoinMultiplier = 2;
                    baseXp = 20;
                    killXpMultiplier = 3;
                    waveXpMultiplier = 5;
                    break;

                default: // TechnicalError or None
                    baseCoins = 0;
                    killCoinMultiplier = 0;
                    waveCoinMultiplier = 0;
                    baseXp = 0;
                    killXpMultiplier = 0;
                    waveXpMultiplier = 0;
                    break;
            }

            totalCoins = baseCoins + (result.TotalKills * killCoinMultiplier) + (result.WavesCleared * waveCoinMultiplier);

            // Salvage XP bonus
            int salvageXpBonus = (scrap * 2) + (alloy * 5) + (core * 15);
            totalXp = baseXp + (result.TotalKills * killXpMultiplier) + (result.WavesCleared * waveXpMultiplier) + salvageXpBonus;
        }

        public static void ApplyRunReward(UserProfileData profile, RunResult result)
        {
            if (profile == null || result == null) return;

            CalculateReward(result, out int coins, out int xp, out int scrap, out int alloy, out int core);

            if (scrap > 0) profile.AddComponents(EconomyComponentExtensions.ScrapId, scrap);
            if (alloy > 0) profile.AddComponents(EconomyComponentExtensions.AlloyId, alloy);
            if (core > 0) profile.AddComponents(EconomyComponentExtensions.CoreId, core);

            if (coins > 0) profile.AddCoins(coins);
            if (xp > 0) profile.AddXp(xp);
        }
    }
}
