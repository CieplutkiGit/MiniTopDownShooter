using Application;
using Application.Economy;
using Application.Weapons;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyMigrationTests
    {
        [Test]
        public void MigrateV1Profile_SetsVersion3_AndUnlocksStarterWeapon()
        {
            var profile = new UserProfileData
            {
                Version = 1,
                HighScore = 50,
                TotalKills = 10,
                Coins = -10,
                Xp = -5,
                Level = 0,
                UnlockedWeaponIds = null
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(3, profile.Version);
            Assert.AreEqual(0, profile.Coins);
            Assert.AreEqual(0, profile.Xp);
            Assert.AreEqual(1, profile.Level);
            Assert.IsNotNull(profile.UnlockedWeaponIds);
            Assert.IsTrue(profile.IsWeaponUnlocked(WeaponWorkshopIds.Pistol));
        }

        [Test]
        public void MigrateV2Profile_SetsVersion3_PreservesExistingStats_AndEnsuresStarterPistol()
        {
            var profile = new UserProfileData
            {
                Version = 2,
                HighScore = 250,
                TotalKills = 42,
                TotalRuns = 5,
                TotalWins = 3,
                TotalLosses = 2,
                Coins = 0,
                Xp = 0
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(3, profile.Version);
            Assert.AreEqual(250, profile.HighScore);
            Assert.AreEqual(42, profile.TotalKills);
            Assert.AreEqual(5, profile.TotalRuns);
            Assert.IsTrue(profile.IsWeaponUnlocked(WeaponWorkshopIds.Pistol));
        }

        [Test]
        public void MigrateProfile_RecalculatesLevelFromXp()
        {
            var profile = new UserProfileData
            {
                Version = 2,
                Xp = 500, // Level 4 requires 450 XP, Level 5 requires 700 XP
                Level = 1
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(4, profile.Level);
        }

        [Test]
        public void MigrateProfile_CleansNullAndNegativeComponents()
        {
            var profile = new UserProfileData
            {
                Version = 2,
                Components = new System.Collections.Generic.List<ComponentInventoryEntry>
                {
                    new ComponentInventoryEntry("scrap", 15),
                    null,
                    new ComponentInventoryEntry("", 5),
                    new ComponentInventoryEntry("alloy", -3)
                }
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(1, profile.Components.Count);
            Assert.AreEqual("scrap", profile.Components[0].ComponentId);
            Assert.AreEqual(15, profile.Components[0].Count);
        }

        [Test]
        public void MigrateProfile_ConsolidatesDuplicateComponents_BySummingCounts()
        {
            var profile = new UserProfileData
            {
                Version = 2,
                Components = new System.Collections.Generic.List<ComponentInventoryEntry>
                {
                    new ComponentInventoryEntry("scrap", 5),
                    new ComponentInventoryEntry("SCRAP", 10),
                    new ComponentInventoryEntry("alloy", 2),
                    new ComponentInventoryEntry("alloy", 3)
                }
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(2, profile.Components.Count);
            Assert.AreEqual(15, profile.GetComponentCount("scrap"));
            Assert.AreEqual(5, profile.GetComponentCount("alloy"));
        }

        [Test]
        public void MigrateProfile_DeduplicatesWeaponAndPartLists()
        {
            var profile = new UserProfileData
            {
                Version = 2,
                UnlockedWeaponIds = new System.Collections.Generic.List<string> { "weapon.rifle", "weapon.rifle", "weapon.pistol" },
                UnlockedPartIds = new System.Collections.Generic.List<string> { "part.a", "part.a", "part.b" }
            };

            profile.ValidateAndMigrate();

            Assert.AreEqual(2, profile.UnlockedWeaponIds.Count);
            Assert.AreEqual(2, profile.UnlockedPartIds.Count);
        }

        [Test]
        public void Profile_AddCoins_And_AddComponents_ProtectAgainstOverflow()
        {
            var profile = new UserProfileData
            {
                Coins = int.MaxValue - 10,
                Xp = int.MaxValue - 5
            };
            profile.SetComponentCount("scrap", int.MaxValue - 20);

            profile.AddCoins(100);
            profile.AddXp(50);
            profile.AddComponents("scrap", 100);

            Assert.AreEqual(int.MaxValue, profile.Coins);
            Assert.AreEqual(int.MaxValue, profile.Xp);
            Assert.AreEqual(int.MaxValue, profile.GetComponentCount("scrap"));
        }

        [Test]
        public void StarterWeaponPistol_IsAlwaysConsideredUnlocked_EvenIfListCleared()
        {
            var profile = new UserProfileData();
            profile.UnlockedWeaponIds.Clear();

            Assert.IsTrue(profile.IsWeaponUnlocked(WeaponWorkshopIds.Pistol));
        }
    }
}
