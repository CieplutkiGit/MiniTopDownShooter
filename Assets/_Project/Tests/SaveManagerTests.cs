using System.IO;
using Application;
using Game;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class SaveManagerTests
    {
        private string _testDir;
        private string _testFile;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "SaveManagerTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _testFile = Path.Combine(_testDir, "test_settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public void SaveToFile_CreatesValidJsonFile()
        {
            GameSettingsData settings = new GameSettingsData
            {
                MasterVolume = 0.75f,
                AimSensitivity = 1.8f
            };

            bool success = SaveManager.SaveToFile(_testFile, settings);
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(_testFile));

            GameSettingsData loaded = SaveManager.LoadFromFile<GameSettingsData>(_testFile);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0.75f, loaded.MasterVolume, 0.001f);
            Assert.AreEqual(1.8f, loaded.AimSensitivity, 0.001f);
        }

        [Test]
        public void SecondSave_CreatesBackupFile()
        {
            GameSettingsData first = new GameSettingsData { MasterVolume = 0.5f };
            SaveManager.SaveToFile(_testFile, first);

            string backupPath = _testFile + ".bak";
            Assert.IsFalse(File.Exists(backupPath), "Backup should not exist on initial save");

            GameSettingsData second = new GameSettingsData { MasterVolume = 0.9f };
            SaveManager.SaveToFile(_testFile, second);

            Assert.IsTrue(File.Exists(backupPath), "Backup should exist after overwrite");

            GameSettingsData loadedBackup = SaveManager.LoadFromFile<GameSettingsData>(backupPath);
            Assert.IsNotNull(loadedBackup);
            Assert.AreEqual(0.5f, loadedBackup.MasterVolume, 0.001f);
        }

        [Test]
        public void CorruptedPrimaryFile_RecoversFromBackup()
        {
            // 1. First save (volume = 0.4)
            GameSettingsData first = new GameSettingsData { MasterVolume = 0.4f };
            SaveManager.SaveToFile(_testFile, first);

            // 2. Second save (volume = 0.8) -> creates backup with volume = 0.4
            GameSettingsData second = new GameSettingsData { MasterVolume = 0.8f };
            SaveManager.SaveToFile(_testFile, second);

            // 3. Corrupt primary file with garbage text
            File.WriteAllText(_testFile, "{ THIS IS CORRUPTED JSON GARBAGE !!!");

            // 4. LoadFromFile should recover from backup
            GameSettingsData recovered = SaveManager.LoadFromFile<GameSettingsData>(_testFile);
            Assert.IsNotNull(recovered);
            Assert.AreEqual(0.4f, recovered.MasterVolume, 0.001f);
        }

        [Test]
        public void BothFilesCorrupt_ReturnsNull()
        {
            File.WriteAllText(_testFile, "GARBAGE 1");
            File.WriteAllText(_testFile + ".bak", "GARBAGE 2");

            GameSettingsData result = SaveManager.LoadFromFile<GameSettingsData>(_testFile);
            Assert.IsNull(result);
        }
    }
}
