using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Application;
using Application.Weapons;
using Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class WeaponWorkshopPersistenceTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "WeaponWorkshopPersistenceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            SaveManager.CustomSaveDirectory = _testDir;
            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.CustomSaveDirectory = null;
            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
            if (Directory.Exists(_testDir))
            {
                try
                {
                    Directory.Delete(_testDir, true);
                }
                catch
                {
                }
            }
        }

        private WeaponBuild CreateSampleRifleBuild(string barrelVariant = "rifle.barrel.standard")
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, barrelVariant },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard" }
            };
            return new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
        }

        private WeaponBuild CreateSamplePistolBuild()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.standard" },
                { WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.standard" },
                { WeaponWorkshopIds.PistolSlots.Grip, "pistol.grip.standard" },
                { WeaponWorkshopIds.PistolSlots.Slide, "pistol.slide.standard" }
            };
            return new WeaponBuild(WeaponWorkshopIds.Pistol, selections);
        }

        [Test]
        public void WeaponBuildDto_FromAndToDomain_PreservesSelections()
        {
            var original = CreateSampleRifleBuild("rifle.barrel.long");
            var dto = WeaponBuildDto.FromDomain(original);

            Assert.AreEqual(original.WeaponId, dto.WeaponId);
            Assert.AreEqual(4, dto.Slots.Count);

            var domain = dto.ToDomain();
            Assert.AreEqual(original.WeaponId, domain.WeaponId);
            Assert.AreEqual(original, domain);
            Assert.AreEqual("rifle.barrel.long", domain.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void RoundTrip_SaveAndLoad_PreservesWeaponBuild()
        {
            var store = new SaveManagerWeaponBuildStore();
            var build = CreateSampleRifleBuild("rifle.barrel.long");

            var saveResult = store.Save(build);
            Assert.IsTrue(saveResult.IsSuccess, $"Save failed: {saveResult.ErrorMessage}");

            var loadResult = store.Load(build.WeaponId);
            Assert.IsTrue(loadResult.IsSuccess, $"Load failed: {loadResult.ErrorMessage}");
            Assert.IsNotNull(loadResult.Build);
            Assert.AreEqual(build.WeaponId, loadResult.Build.WeaponId);
            Assert.AreEqual(build, loadResult.Build);
            Assert.AreEqual("rifle.barrel.long", loadResult.Build.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.IsFalse(loadResult.WasMigratedOrRepaired);
        }

        [Test]
        public void RoundTrip_MultipleWeapons_AreStoredIndependently()
        {
            var store = new SaveManagerWeaponBuildStore();
            var rifle = CreateSampleRifleBuild("rifle.barrel.long");
            var pistol = CreateSamplePistolBuild();

            Assert.IsTrue(store.Save(rifle).IsSuccess);
            Assert.IsTrue(store.Save(pistol).IsSuccess);

            var loadedRifle = store.Load(rifle.WeaponId);
            var loadedPistol = store.Load(pistol.WeaponId);

            Assert.IsTrue(loadedRifle.IsSuccess);
            Assert.IsTrue(loadedPistol.IsSuccess);
            Assert.AreEqual(rifle, loadedRifle.Build);
            Assert.AreEqual(pistol, loadedPistol.Build);

            // Updating rifle does not change pistol
            var updatedRifle = CreateSampleRifleBuild("rifle.barrel.short");
            Assert.IsTrue(store.Save(updatedRifle).IsSuccess);

            var reloadedRifle = store.Load(rifle.WeaponId);
            var reloadedPistol = store.Load(pistol.WeaponId);

            Assert.AreEqual("rifle.barrel.short", reloadedRifle.Build.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(pistol, reloadedPistol.Build);
        }

        [Test]
        public void Load_NonExistentWeapon_ReturnsCleanFailure()
        {
            var store = new SaveManagerWeaponBuildStore();
            var result = store.Load("weapon.nonexistent");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Build);
            StringAssert.Contains("No saved build for weapon", result.ErrorMessage);
        }

        [Test]
        public void Load_EmptyOrNullWeaponId_ReturnsCleanFailure()
        {
            var store = new SaveManagerWeaponBuildStore();

            var nullResult = store.Load(null);
            Assert.IsFalse(nullResult.IsSuccess);
            StringAssert.Contains("No saved build for weapon", nullResult.ErrorMessage);

            var emptyResult = store.Load("");
            Assert.IsFalse(emptyResult.IsSuccess);
            StringAssert.Contains("No saved build for weapon", emptyResult.ErrorMessage);
        }

        [Test]
        public void Save_NullBuild_ReturnsCleanFailure()
        {
            var store = new SaveManagerWeaponBuildStore();
            var result = store.Save(null);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Test]
        public void BackupRecovery_CorruptedPrimaryFile_RecoversFromBackup()
        {
            var store = new SaveManagerWeaponBuildStore();
            string primaryPath = Path.Combine(_testDir, "weapon_workshop.json");
            string backupPath = primaryPath + ".bak";

            // 1. Initial save directly to file without prior read (v1: standard barrel)
            var buildV1 = CreateSampleRifleBuild("rifle.barrel.standard");
            var initialData = new WeaponWorkshopSaveData
            {
                Builds = new List<WeaponBuildDto> { WeaponBuildDto.FromDomain(buildV1) }
            };
            Assert.IsTrue(SaveManager.SaveWorkshopData(initialData));
            Assert.IsFalse(File.Exists(backupPath), "Backup should not exist on first save");

            // 2. Second save (v2: long barrel) -> backup is created with v1
            var buildV2 = CreateSampleRifleBuild("rifle.barrel.long");
            Assert.IsTrue(store.Save(buildV2).IsSuccess);
            Assert.IsTrue(File.Exists(backupPath), "Backup should exist after overwrite");

            // 3. Corrupt primary file
            File.WriteAllText(primaryPath, "{ THIS IS COMPLETELY CORRUPTED GARBAGE JSON }}}");

            // 4. Load should recover from backup (v1 data)
            var loadResult = store.Load(buildV1.WeaponId);
            Assert.IsTrue(loadResult.IsSuccess, $"Backup recovery failed: {loadResult.ErrorMessage}");
            Assert.IsNotNull(loadResult.Build);
            Assert.AreEqual("rifle.barrel.standard", loadResult.Build.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));

            // 5. Verify primary file was restored
            Assert.IsTrue(File.Exists(primaryPath));
            string restoredJson = File.ReadAllText(primaryPath);
            Assert.IsTrue(restoredJson.Contains("rifle.barrel.standard"));
        }

        [Test]
        public void SaveFailure_SimulatedFileOperationsError_ReturnsCleanFailure()
        {
            var store = new SaveManagerWeaponBuildStore();
            var build = CreateSampleRifleBuild();

            // Ensure initial file exists so LoadWorkshopData doesn't error during initialization
            Assert.IsTrue(store.Save(build).IsSuccess);

            // Inject failing file operations
            SaveManager.FileOps = new FailingFileOperations();

            // SaveManager logs an error when AtomicWrite catches an exception
            LogAssert.Expect(LogType.Error, new Regex(".*Simulated disk I/O failure.*"));

            var result = store.Save(build);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Test]
        public void ValidateAndMigrate_PrunesCorruptEntriesAndUpgradesVersion()
        {
            string primaryPath = Path.Combine(_testDir, "weapon_workshop.json");

            // Raw JSON simulating older/corrupted workshop save:
            // Version = 0, one build with empty weapon ID, one build with invalid slot, one valid build
            string rawJson = @"{
                ""Version"": 0,
                ""Builds"": [
                    {
                        ""WeaponId"": """",
                        ""Slots"": []
                    },
                    {
                        ""WeaponId"": ""weapon.rifle"",
                        ""Slots"": [
                            { ""SlotId"": """", ""PartId"": ""rifle.barrel.standard"" },
                            { ""SlotId"": ""rifle.slot.barrel"", ""PartId"": ""rifle.barrel.standard"" }
                        ]
                    }
                ]
            }";

            File.WriteAllText(primaryPath, rawJson);

            var store = new SaveManagerWeaponBuildStore();
            var loadResult = store.Load("weapon.rifle");

            Assert.IsTrue(loadResult.IsSuccess, $"Load failed: {loadResult.ErrorMessage}");
            Assert.IsTrue(loadResult.WasMigratedOrRepaired);
            Assert.AreEqual("rifle.barrel.standard", loadResult.Build.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));

            // Load workshop data directly to verify version and corruption pruning
            var data = SaveManager.LoadWorkshopData();
            Assert.AreEqual(1, data.Version);
            Assert.AreEqual(1, data.Builds.Count);
            Assert.AreEqual("weapon.rifle", data.Builds[0].WeaponId);
            Assert.AreEqual(1, data.Builds[0].Slots.Count);
            Assert.AreEqual("rifle.slot.barrel", data.Builds[0].Slots[0].SlotId);
        }

        [Test]
        public void PreserveExistingPersistence_UserProfileAndGameSettings_Unmodified()
        {
            // 1. Save profile
            var profile = new UserProfileData
            {
                HighScore = 2500,
                TotalKills = 150,
                TotalRuns = 10,
                TotalWins = 3
            };
            Assert.IsTrue(SaveManager.SaveProfile(profile));

            // 2. Save settings
            var settings = new GameSettingsData
            {
                MasterVolume = 0.65f,
                SFXVolume = 0.85f,
                AimSensitivity = 2.2f
            };
            Assert.IsTrue(SaveManager.SaveSettings(settings));

            // 3. Save workshop build
            var store = new SaveManagerWeaponBuildStore();
            var rifle = CreateSampleRifleBuild("rifle.barrel.long");
            Assert.IsTrue(store.Save(rifle).IsSuccess);

            // 4. Verify all files exist independently
            Assert.IsTrue(File.Exists(Path.Combine(_testDir, "user_profile.json")));
            Assert.IsTrue(File.Exists(Path.Combine(_testDir, "game_settings.json")));
            Assert.IsTrue(File.Exists(Path.Combine(_testDir, "weapon_workshop.json")));

            // 5. Load and verify profile
            var loadedProfile = SaveManager.LoadProfile();
            Assert.IsNotNull(loadedProfile);
            Assert.AreEqual(2500, loadedProfile.HighScore);
            Assert.AreEqual(150, loadedProfile.TotalKills);
            Assert.AreEqual(10, loadedProfile.TotalRuns);
            Assert.AreEqual(3, loadedProfile.TotalWins);

            // 6. Load and verify settings
            var loadedSettings = SaveManager.LoadSettings();
            Assert.IsNotNull(loadedSettings);
            Assert.AreEqual(0.65f, loadedSettings.MasterVolume, 0.001f);
            Assert.AreEqual(0.85f, loadedSettings.SFXVolume, 0.001f);
            Assert.AreEqual(2.2f, loadedSettings.AimSensitivity, 0.001f);

            // 7. Load and verify workshop build
            var loadedWorkshop = store.Load(rifle.WeaponId);
            Assert.IsTrue(loadedWorkshop.IsSuccess);
            Assert.AreEqual(rifle, loadedWorkshop.Build);
        }

        private class FailingFileOperations : SaveManager.IFileOperations
        {
            private readonly SaveManager.DefaultFileOperations _inner = new SaveManager.DefaultFileOperations();

            public bool Exists(string path) => _inner.Exists(path);
            public string ReadAllText(string path) => _inner.ReadAllText(path);
            public void WriteAllText(string path, string contents) => _inner.WriteAllText(path, contents);
            public void Copy(string sourceFileName, string destFileName, bool overwrite) => _inner.Copy(sourceFileName, destFileName, overwrite);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
                => _inner.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            public void Delete(string path) => _inner.Delete(path);
            public void Move(string sourceFileName, string destFileName) => _inner.Move(sourceFileName, destFileName);
            public Stream Create(string path) => throw new IOException("Simulated disk I/O failure on Create.");
            public long GetLength(string path) => _inner.GetLength(path);
        }
    }
}
