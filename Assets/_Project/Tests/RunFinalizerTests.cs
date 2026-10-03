using System;
using System.IO;
using System.Text.RegularExpressions;
using Application;
using Application.Flow;
using Game;
using Game.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniTopDownShooter.Tests
{
    public class RunFinalizerTests
    {
        private string _directory;
        private ToggleFileOperations _fileOperations;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "RunFinalizerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            SaveManager.CustomSaveDirectory = _directory;
            _fileOperations = new ToggleFileOperations();
            SaveManager.FileOps = _fileOperations;
            RunFinalizer.ResetForTesting();
            SaveManager.SaveProfile(new UserProfileData());
        }

        [TearDown]
        public void TearDown()
        {
            RunFinalizer.ResetForTesting();
            SaveManager.CustomSaveDirectory = null;
            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void RepeatedRunId_IsAppliedOnlyOnceAcrossFinalizerReset()
        {
            RunResult result = CreateResult("same", 30, RunOutcome.Victory);

            Assert.IsTrue(RunFinalizer.TryFinalize(result));
            RunFinalizer.ResetForTesting();
            Assert.IsTrue(RunFinalizer.TryFinalize(result));

            UserProfileData profile = SaveManager.LoadProfile();
            Assert.AreEqual(1, profile.TotalRuns);
            Assert.AreEqual(1, profile.TotalWins);
            Assert.AreEqual(30, profile.TotalKills);
            Assert.IsTrue(profile.HasFinalizedRun("same"));
        }

        [Test]
        public void OutOfOrderCompletions_AreAppliedOnceAndRetainInterveningProfileChanges()
        {
            _fileOperations.FailWrites = true;
            ExpectFailedRunSave("first");
            Assert.IsFalse(RunFinalizer.TryFinalize(CreateResult("first", 5, RunOutcome.Defeat)));
            ExpectFailedRunSave("first");
            Assert.IsFalse(RunFinalizer.TryFinalize(CreateResult("second", 8, RunOutcome.Victory)));
            Assert.AreEqual(2, RunFinalizer.PendingCount);

            _fileOperations.FailWrites = false;
            UserProfileData interveningSave = SaveManager.LoadProfile();
            interveningSave.HighScore = 100;
            Assert.IsTrue(SaveManager.SaveProfile(interveningSave));

            Assert.IsTrue(RunFinalizer.RetryPending());
            UserProfileData profile = SaveManager.LoadProfile();
            Assert.AreEqual(2, profile.TotalRuns);
            Assert.AreEqual(1, profile.TotalWins);
            Assert.AreEqual(1, profile.TotalLosses);
            Assert.AreEqual(13, profile.TotalKills);
            Assert.AreEqual(100, profile.HighScore);
            Assert.AreEqual(0, RunFinalizer.PendingCount);
        }

        [Test]
        public void FailedSave_RemainsQueuedAndRetryPersistsLedgerWithStats()
        {
            _fileOperations.FailWrites = true;
            ExpectFailedRunSave("retry");
            Assert.IsFalse(RunFinalizer.TryFinalize(CreateResult("retry", 11, RunOutcome.Victory)));
            Assert.AreEqual(1, RunFinalizer.PendingCount);
            Assert.AreEqual(RunSaveStatus.PendingRetry, RunFinalizer.CurrentSaveStatus);

            _fileOperations.FailWrites = false;
            Assert.IsTrue(RunFinalizer.RetryPending());
            UserProfileData profile = SaveManager.LoadProfile();
            Assert.AreEqual(1, profile.TotalRuns);
            Assert.AreEqual(1, profile.TotalWins);
            Assert.AreEqual(11, profile.TotalKills);
            Assert.IsTrue(profile.HasFinalizedRun("retry"));
            Assert.AreEqual(0, RunFinalizer.PendingCount);
        }

        private static void ExpectFailedRunSave(string runId)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] Error saving"));
            LogAssert.Expect(LogType.Error, new Regex(@"\[RunFinalizer\] Failed to persist run " + Regex.Escape(runId)));
        }

        private static RunResult CreateResult(string id, int kills, RunOutcome outcome)
        {
            return new RunResult(id, "mission", outcome, DeploymentLoadoutSnapshot.Empty, 10f, kills * 2, kills, 1);
        }

        private sealed class ToggleFileOperations : SaveManager.IFileOperations
        {
            private readonly SaveManager.DefaultFileOperations _inner = new SaveManager.DefaultFileOperations();
            public bool FailWrites;
            public bool Exists(string path) => _inner.Exists(path);
            public string ReadAllText(string path) => _inner.ReadAllText(path);
            public void WriteAllText(string path, string contents) => _inner.WriteAllText(path, contents);
            public void Copy(string sourceFileName, string destFileName, bool overwrite) => _inner.Copy(sourceFileName, destFileName, overwrite);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
                => _inner.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            public void Delete(string path) => _inner.Delete(path);
            public void Move(string sourceFileName, string destFileName) => _inner.Move(sourceFileName, destFileName);
            public Stream Create(string path) => FailWrites ? throw new IOException("Simulated save failure.") : _inner.Create(path);
            public long GetLength(string path) => _inner.GetLength(path);
        }
    }
}
