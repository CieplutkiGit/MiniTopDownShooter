using System;

namespace Application.Economy
{
    public static class EconomyLevelCurve
    {
        // Cumulative XP required to reach each level (index = level)
        private static readonly int[] CumulativeThresholds = new[]
        {
            0,    // Level 0 (unused)
            0,    // Level 1: 0 XP
            100,  // Level 2: 100 XP
            250,  // Level 3: 250 XP
            450,  // Level 4: 450 XP
            700,  // Level 5: 700 XP
            1000, // Level 6: 1000 XP
            1350, // Level 7: 1350 XP
            1750, // Level 8: 1750 XP
            2200, // Level 9: 2200 XP
            2700  // Level 10: 2700 XP
        };

        public static int MaxLevel => 50;

        public static int GetRequiredXpForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level < CumulativeThresholds.Length)
            {
                return CumulativeThresholds[level];
            }

            int lastKnown = CumulativeThresholds[CumulativeThresholds.Length - 1];
            int extraLevels = level - (CumulativeThresholds.Length - 1);
            return lastKnown + extraLevels * 600;
        }

        public static int GetLevelForXp(int xp)
        {
            if (xp <= 0) return 1;

            int level = 1;
            while (level < MaxLevel && xp >= GetRequiredXpForLevel(level + 1))
            {
                level++;
            }

            return level;
        }

        public static void GetLevelProgress(
            int xp,
            out int currentLevel,
            out int xpInCurrentLevel,
            out int xpForNextLevel,
            out float progressNormalized)
        {
            if (xp < 0) xp = 0;
            currentLevel = GetLevelForXp(xp);

            int currentBaseXp = GetRequiredXpForLevel(currentLevel);
            int nextLevelBaseXp = GetRequiredXpForLevel(currentLevel + 1);

            xpInCurrentLevel = xp - currentBaseXp;
            xpForNextLevel = nextLevelBaseXp - currentBaseXp;

            if (xpForNextLevel <= 0)
            {
                progressNormalized = 1f;
            }
            else
            {
                progressNormalized = Math.Max(0f, Math.Min(1f, (float)xpInCurrentLevel / xpForNextLevel));
            }
        }
    }
}
