using System;
using System.Collections.Generic;
using Application.Economy;
using Application.Weapons;

namespace Application
{
    [Serializable]
    public class ComponentInventoryEntry
    {
        public string ComponentId;
        public int Count;

        public ComponentInventoryEntry()
        {
        }

        public ComponentInventoryEntry(string componentId, int count)
        {
            ComponentId = componentId ?? string.Empty;
            Count = Math.Max(0, count);
        }
    }

    [Serializable]
    public class UserProfileData
    {
        public int Version = 3;
        public int HighScore = 0;
        public int TotalKills = 0;
        public int TotalRuns = 0;
        public int TotalWins = 0;
        public int TotalLosses = 0;
        public List<string> FinalizedRunIds = new List<string>();

        // ── Economy & Progression ──────────────────────────────────────────
        public int Coins = 0;
        public int Xp = 0;
        public int Level = 1;
        public List<ComponentInventoryEntry> Components = new List<ComponentInventoryEntry>();
        public List<string> UnlockedWeaponIds = new List<string>();
        public List<string> UnlockedPartIds = new List<string>();

        public bool HasFinalizedRun(string runId)
        {
            return !string.IsNullOrWhiteSpace(runId) && FinalizedRunIds != null && FinalizedRunIds.Contains(runId);
        }

        public bool RecordFinalizedRun(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false;
            if (FinalizedRunIds == null) FinalizedRunIds = new List<string>();
            if (FinalizedRunIds.Contains(runId)) return false;
            FinalizedRunIds.Add(runId);
            return true;
        }

        // ── Economy Helpers ────────────────────────────────────────────────
        public int GetComponentCount(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId) || Components == null) return 0;
            var entry = Components.Find(c => c != null && string.Equals(c.ComponentId, componentId, StringComparison.OrdinalIgnoreCase));
            return entry != null ? Math.Max(0, entry.Count) : 0;
        }

        public void SetComponentCount(string componentId, int count)
        {
            if (string.IsNullOrWhiteSpace(componentId)) return;
            if (Components == null) Components = new List<ComponentInventoryEntry>();
            count = Math.Max(0, count);

            var entry = Components.Find(c => c != null && string.Equals(c.ComponentId, componentId, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                entry.Count = count;
            }
            else
            {
                Components.Add(new ComponentInventoryEntry(componentId, count));
            }
        }

        public void AddComponents(string componentId, int amount)
        {
            if (string.IsNullOrWhiteSpace(componentId) || amount <= 0) return;
            int current = GetComponentCount(componentId);
            long total = (long)current + amount;
            int clamped = total > int.MaxValue ? int.MaxValue : (int)total;
            SetComponentCount(componentId, clamped);
        }

        public bool TryRemoveComponents(string componentId, int amount)
        {
            if (string.IsNullOrWhiteSpace(componentId) || amount <= 0) return false;
            int current = GetComponentCount(componentId);
            if (current < amount) return false;
            SetComponentCount(componentId, current - amount);
            return true;
        }

        public bool IsWeaponUnlocked(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return false;
            // Starter weapon is always unlocked
            if (string.Equals(weaponId, WeaponWorkshopIds.Pistol, StringComparison.OrdinalIgnoreCase)) return true;
            return UnlockedWeaponIds != null && UnlockedWeaponIds.Exists(id => string.Equals(id, weaponId, StringComparison.OrdinalIgnoreCase));
        }

        public void UnlockWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return;
            if (UnlockedWeaponIds == null) UnlockedWeaponIds = new List<string>();
            if (!IsWeaponUnlocked(weaponId))
            {
                UnlockedWeaponIds.Add(weaponId);
            }
        }

        public bool IsPartUnlocked(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return true;
            if (EconomyPricingPolicy.IsDefaultPart(partId)) return true;
            return UnlockedPartIds != null && UnlockedPartIds.Exists(id => string.Equals(id, partId, StringComparison.OrdinalIgnoreCase));
        }

        public void UnlockPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return;
            if (UnlockedPartIds == null) UnlockedPartIds = new List<string>();
            if (!IsPartUnlocked(partId))
            {
                UnlockedPartIds.Add(partId);
            }
        }

        public bool CanAffordCoins(int amount)
        {
            return amount >= 0 && Coins >= amount;
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            long total = (long)Coins + amount;
            Coins = total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            long total = (long)Xp + amount;
            Xp = total > int.MaxValue ? int.MaxValue : (int)total;
            Level = Math.Max(1, EconomyLevelCurve.GetLevelForXp(Xp));
        }

        // ── Validation and Migration ───────────────────────────────────────
        public void ValidateAndMigrate()
        {
            if (Version < 1)
            {
                Version = 1;
            }
            if (Version < 2) Version = 2;
            if (Version < 3)
            {
                Version = 3;
            }

            if (FinalizedRunIds == null) FinalizedRunIds = new List<string>();
            FinalizedRunIds.RemoveAll(string.IsNullOrWhiteSpace);
            var uniqueRuns = new HashSet<string>(StringComparer.Ordinal);
            var deduplicatedRuns = new List<string>();
            foreach (var runId in FinalizedRunIds)
            {
                if (uniqueRuns.Add(runId)) deduplicatedRuns.Add(runId);
            }
            FinalizedRunIds = deduplicatedRuns;

            if (HighScore < 0) HighScore = 0;
            if (TotalKills < 0) TotalKills = 0;
            if (TotalRuns < 0) TotalRuns = 0;
            if (TotalWins < 0) TotalWins = 0;
            if (TotalLosses < 0) TotalLosses = 0;

            if (Coins < 0) Coins = 0;
            if (Xp < 0) Xp = 0;
            Level = Math.Max(1, EconomyLevelCurve.GetLevelForXp(Xp));

            if (Components == null) Components = new List<ComponentInventoryEntry>();
            var consolidated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Components.Count; i++)
            {
                var entry = Components[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.ComponentId) && entry.Count > 0)
                {
                    if (consolidated.TryGetValue(entry.ComponentId, out int existing))
                    {
                        long sum = (long)existing + entry.Count;
                        consolidated[entry.ComponentId] = sum > int.MaxValue ? int.MaxValue : (int)sum;
                    }
                    else
                    {
                        consolidated[entry.ComponentId] = entry.Count;
                    }
                }
            }
            Components.Clear();
            foreach (var kvp in consolidated)
            {
                Components.Add(new ComponentInventoryEntry(kvp.Key.ToLowerInvariant(), kvp.Value));
            }

            if (UnlockedWeaponIds == null) UnlockedWeaponIds = new List<string>();
            UnlockedWeaponIds.RemoveAll(string.IsNullOrWhiteSpace);
            var uniqueWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deduplicatedWeapons = new List<string>();
            foreach (var id in UnlockedWeaponIds)
            {
                if (uniqueWeapons.Add(id)) deduplicatedWeapons.Add(id);
            }
            UnlockedWeaponIds = deduplicatedWeapons;
            if (!uniqueWeapons.Contains(WeaponWorkshopIds.Pistol))
            {
                UnlockedWeaponIds.Add(WeaponWorkshopIds.Pistol);
            }

            if (UnlockedPartIds == null) UnlockedPartIds = new List<string>();
            UnlockedPartIds.RemoveAll(string.IsNullOrWhiteSpace);
            var uniqueParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deduplicatedParts = new List<string>();
            foreach (var id in UnlockedPartIds)
            {
                if (uniqueParts.Add(id)) deduplicatedParts.Add(id);
            }
            UnlockedPartIds = deduplicatedParts;
        }
    }

    [Serializable]
    public class GameSettingsData
    {
        public int Version = 1;
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 1.0f;
        public float AimSensitivity = 1.0f;
        public float Deadzone = 0.1f;
        public bool MobileTouchControls = false;
        public float TouchControlScale = 1.0f;

        public void ValidateAndMigrate()
        {
            if (Version < 1)
            {
                Version = 1;
            }
            MasterVolume = Clamp01(MasterVolume, 1.0f);
            MusicVolume = Clamp01(MusicVolume, 0.8f);
            SFXVolume = Clamp01(SFXVolume, 1.0f);
            AimSensitivity = Clamp(AimSensitivity, 0.05f, 10.0f, 1.0f);
            Deadzone = Clamp(Deadzone, 0f, 0.9f, 0.1f);
            TouchControlScale = Clamp(TouchControlScale, 0.5f, 2.5f, 1.0f);
        }

        private static float Clamp01(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
