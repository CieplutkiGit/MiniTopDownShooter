using System;
using System.IO;
using Application;
using Application.Economy;
using Application.Flow;
using Game;
using Game.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyIdempotentRewardsTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "EconomyRewardsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            SaveManager.CustomSaveDirectory = _directory;
            RunFinalizer.ResetForTesting();
            Game.Economy.RunSalvageTracker.ResetForTesting();
            SaveManager.SaveProfile(new UserProfileData());
        }

        [TearDown]
        public void TearDown()
        {
            RunFinalizer.ResetForTesting();
            Game.Economy.RunSalvageTracker.ResetForTesting();
            SaveManager.CustomSaveDirectory = null;
            SaveManager.FileOps = new SaveManager.DefaultFileOperations();
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void FinalizeRun_CreditsCoinsXpAndSalvage_ExactlyOnce()
        {
            var result = new RunResult(
                runId: "run-id-1",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 45f,
                finalScore: 500,
                totalKills: 12,
                wavesCleared: 3,
                timestampUtc: DateTime.UtcNow,
                scrapCollected: 6,
                alloyCollected: 2,
                coreCollected: 1);

            Assert.IsTrue(RunFinalizer.TryFinalize(result));

            UserProfileData profile = SaveManager.LoadProfile();
            int firstCoins = profile.Coins;
            int firstXp = profile.Xp;
            int firstScrap = profile.GetComponentCount("scrap");
            int firstAlloy = profile.GetComponentCount("alloy");
            int firstCore = profile.GetComponentCount("core");

            Assert.Greater(firstCoins, 0);
            Assert.Greater(firstXp, 0);
            Assert.AreEqual(6, firstScrap);
            Assert.AreEqual(2, firstAlloy);
            Assert.AreEqual(1, firstCore);
            Assert.IsTrue(profile.HasFinalizedRun("run-id-1"));

            // Re-attempt finalization for the same run ID
            Assert.IsTrue(RunFinalizer.TryFinalize(result));

            UserProfileData secondLoad = SaveManager.LoadProfile();
            Assert.AreEqual(firstCoins, secondLoad.Coins, "Coins must not increase on duplicate run finalization");
            Assert.AreEqual(firstXp, secondLoad.Xp, "XP must not increase on duplicate run finalization");
            Assert.AreEqual(6, secondLoad.GetComponentCount("scrap"));
            Assert.AreEqual(2, secondLoad.GetComponentCount("alloy"));
            Assert.AreEqual(1, secondLoad.GetComponentCount("core"));
        }

        [Test]
        public void Defeat_StillYieldsFairReward_NeverZero()
        {
            var result = new RunResult(
                runId: "defeat-run",
                missionId: "arena",
                outcome: RunOutcome.Defeat,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 30f,
                finalScore: 100,
                totalKills: 5,
                wavesCleared: 1,
                timestampUtc: DateTime.UtcNow,
                scrapCollected: 3);

            Assert.IsTrue(RunFinalizer.TryFinalize(result));

            UserProfileData profile = SaveManager.LoadProfile();
            Assert.Greater(profile.Coins, 0, "Defeat must yield non-zero fair coin reward");
            Assert.Greater(profile.Xp, 0, "Defeat must yield non-zero fair XP reward");
            Assert.AreEqual(3, profile.GetComponentCount("scrap"));
        }

        [Test]
        public void FinalizationSaveFailure_KeepsRunPending_AndPaysOutOnceOnRetry()
        {
            var toggleOps = new ToggleOps();
            SaveManager.FileOps = toggleOps;

            toggleOps.FailWrites = true; // Initial save fails
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*SaveManager.*Disk error.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*RunFinalizer.*Failed to persist.*"));
            var result = new RunResult(
                runId: "retry-run",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 40f,
                finalScore: 300,
                totalKills: 8,
                wavesCleared: 2,
                timestampUtc: DateTime.UtcNow,
                scrapCollected: 4);

            bool firstTry = RunFinalizer.TryFinalize(result);
            Assert.IsFalse(firstTry);
            Assert.AreEqual(1, RunFinalizer.PendingCount);

            // Turn off failure; retry
            toggleOps.FailWrites = false;
            bool retrySuccess = RunFinalizer.RetryPending();
            Assert.IsTrue(retrySuccess);
            Assert.AreEqual(0, RunFinalizer.PendingCount);

            UserProfileData profile = SaveManager.LoadProfile();
            Assert.IsTrue(profile.HasFinalizedRun("retry-run"));
            Assert.Greater(profile.Coins, 0);
            Assert.AreEqual(4, profile.GetComponentCount("scrap"));

            // Further retry does not payout again
            RunFinalizer.RetryPending();
            UserProfileData rechecked = SaveManager.LoadProfile();
            Assert.AreEqual(profile.Coins, rechecked.Coins);
            Assert.AreEqual(4, rechecked.GetComponentCount("scrap"));
        }

        [Test]
        public void CollectParts_SaveFails_TrackerResetsOrNewRunStarts_RetryPersistsOriginalAmountOnce_RepeatedFinalizeDoesNotDouble()
        {
            var trackerGo = new UnityEngine.GameObject("TrackerForTest");
            var tracker = trackerGo.AddComponent<Game.Economy.RunSalvageTracker>();
            tracker.Initialize();
            tracker.RecordPickup(EconomyComponentType.Scrap, 5);
            tracker.RecordPickup(EconomyComponentType.Alloy, 2);

            var toggleOps = new ToggleOps();
            SaveManager.FileOps = toggleOps;
            toggleOps.FailWrites = true; // First save fails
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*SaveManager.*Disk error.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*RunFinalizer.*Failed to persist.*"));

            var originalRun = new RunResult(
                runId: "frozen-run-id",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 30f,
                finalScore: 200,
                totalKills: 6,
                wavesCleared: 2,
                timestampUtc: DateTime.UtcNow);

            // Finalize attempts save, fails, but freezes the 5 scrap and 2 alloy into PendingRuns
            bool firstAttempt = RunFinalizer.TryFinalize(originalRun);
            Assert.IsFalse(firstAttempt);
            Assert.AreEqual(1, RunFinalizer.PendingCount);

            // Tracker is reset and a new run begins collecting different salvage
            tracker.ResetTracker();
            tracker.RecordPickup(EconomyComponentType.Scrap, 100);
            tracker.RecordPickup(EconomyComponentType.Core, 10);

            // Save is repaired
            toggleOps.FailWrites = false;

            // RetryPending persists the frozen original amounts (5 scrap, 2 alloy), NOT the new tracker values
            bool retrySuccess = RunFinalizer.RetryPending();
            Assert.IsTrue(retrySuccess);
            Assert.AreEqual(0, RunFinalizer.PendingCount);

            UserProfileData saved = SaveManager.LoadProfile();
            Assert.IsTrue(saved.HasFinalizedRun("frozen-run-id"));
            Assert.AreEqual(5, saved.GetComponentCount("scrap"), "Should have saved original 5 scrap, not borrowed new tracker amounts");
            Assert.AreEqual(2, saved.GetComponentCount("alloy"), "Should have saved original 2 alloy");
            Assert.AreEqual(0, saved.GetComponentCount("core"), "Should have 0 cores since original run had 0");

            // Repeated finalize doesn't add twice
            Assert.IsTrue(RunFinalizer.TryFinalize(originalRun));
            UserProfileData reloaded = SaveManager.LoadProfile();
            Assert.AreEqual(5, reloaded.GetComponentCount("scrap"));
            Assert.AreEqual(2, reloaded.GetComponentCount("alloy"));

            UnityEngine.Object.DestroyImmediate(trackerGo);
            Game.Economy.RunSalvageTracker.ResetForTesting();
        }

        [Test]
        public void FinalizeOlderRun_DoesNotCaptureNewerRunSalvage()
        {
            var trackerGo = new GameObject("RunIdentityTracker");
            var tracker = trackerGo.AddComponent<Game.Economy.RunSalvageTracker>();
            tracker.Initialize();
            tracker.BeginRun("newer-run");
            tracker.RecordPickup(EconomyComponentType.Scrap, 9);

            var olderRun = new RunResult(
                runId: "older-run",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 20f,
                finalScore: 100,
                totalKills: 2,
                wavesCleared: 1,
                timestampUtc: DateTime.UtcNow);

            Assert.IsTrue(RunFinalizer.TryFinalize(olderRun));
            Assert.AreEqual(0, SaveManager.LoadProfile().GetComponentCount("scrap"));
            Assert.AreEqual("newer-run", tracker.RunId);
            Assert.AreEqual(9, tracker.ScrapCollected);

            var currentRun = new RunResult(
                runId: "newer-run",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 30f,
                finalScore: 200,
                totalKills: 4,
                wavesCleared: 2,
                timestampUtc: DateTime.UtcNow);

            Assert.IsTrue(RunFinalizer.TryFinalize(currentRun));
            Assert.AreEqual(9, SaveManager.LoadProfile().GetComponentCount("scrap"));
            Assert.AreEqual(0, tracker.TotalComponentsCollected);
            Assert.AreEqual("newer-run", tracker.RunId);

            tracker.RecordPickup(EconomyComponentType.Alloy, 2);
            tracker.BeginRun("newer-run");
            Assert.AreEqual(2, tracker.AlloyCollected, "Beginning the same run is idempotent");
            tracker.BeginRun("next-run");
            Assert.AreEqual(0, tracker.TotalComponentsCollected, "A different run starts with empty salvage counts");
            Assert.AreEqual("next-run", tracker.RunId);
            tracker.EndRun();
            Assert.IsNull(tracker.RunId);

            UnityEngine.Object.DestroyImmediate(trackerGo);
            Game.Economy.RunSalvageTracker.ResetForTesting();
        }

        [Test]
        public void FinalizeExplicitSalvage_ConsumesMatchingLiveTrackerWithoutReplacingResult()
        {
            var trackerGo = new GameObject("ExplicitRunTracker");
            var tracker = trackerGo.AddComponent<Game.Economy.RunSalvageTracker>();
            tracker.Initialize();
            tracker.BeginRun("explicit-run");
            tracker.RecordPickup(EconomyComponentType.Scrap, 9);

            var explicitRun = new RunResult(
                runId: "explicit-run",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 30f,
                finalScore: 200,
                totalKills: 4,
                wavesCleared: 2,
                timestampUtc: DateTime.UtcNow,
                scrapCollected: 3);

            Assert.IsTrue(RunFinalizer.TryFinalize(explicitRun));
            Assert.AreEqual(3, SaveManager.LoadProfile().GetComponentCount("scrap"));
            Assert.AreEqual(0, tracker.TotalComponentsCollected);
            Assert.AreEqual("explicit-run", tracker.RunId);

            UnityEngine.Object.DestroyImmediate(trackerGo);
            Game.Economy.RunSalvageTracker.ResetForTesting();
        }

        [Test]
        public void ArenaBootstrapper_UnloadingHubOrPreviousArenaDoesNotTeardownCurrentArena()
        {
            const int activeArenaHandle = 42;
            Assert.IsTrue(Game.Economy.EconomyProductionBootstrapper.ShouldTeardownArenaForSceneUnload(activeArenaHandle, activeArenaHandle));
            Assert.IsFalse(Game.Economy.EconomyProductionBootstrapper.ShouldTeardownArenaForSceneUnload(activeArenaHandle, 7),
                "Unloading a Hub or previous arena scene must leave the currently tracked arena running.");
            Assert.IsFalse(Game.Economy.EconomyProductionBootstrapper.ShouldTeardownArenaForSceneUnload(-1, activeArenaHandle),
                "A scene unload must not tear down arena systems when no arena is tracked.");
        }

        [Test]
        public void ReFinalization_BeforeAndAfterFailedSave_FreshRunSalvageUnaffected()
        {
            var trackerGo = new UnityEngine.GameObject("TrackerForTest2");
            var tracker = trackerGo.AddComponent<Game.Economy.RunSalvageTracker>();
            tracker.Initialize();

            var toggleOps = new ToggleOps();
            SaveManager.FileOps = toggleOps;

            // Run 1 collects 3 scrap
            tracker.RecordPickup(EconomyComponentType.Scrap, 3);
            var run1 = new RunResult(
                runId: "run-failed-save",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 25f,
                finalScore: 150,
                totalKills: 4,
                wavesCleared: 1,
                timestampUtc: DateTime.UtcNow);

            // Fail save for run 1
            toggleOps.FailWrites = true;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*SaveManager.*Disk error.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*RunFinalizer.*Failed to persist.*"));
            bool run1FirstAttempt = RunFinalizer.TryFinalize(run1);
            Assert.IsFalse(run1FirstAttempt);
            Assert.AreEqual(1, RunFinalizer.PendingCount);

            // New run begins: tracker resets and records fresh salvage for run 2
            tracker.ResetTracker();
            tracker.RecordPickup(EconomyComponentType.Scrap, 25);
            tracker.RecordPickup(EconomyComponentType.Core, 2);

            // Re-attempting finalization of Run 1 while it's already pending and save still fails:
            // Must NOT capture Run 2's tracker data!
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*SaveManager.*Disk error.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*RunFinalizer.*Failed to persist.*"));
            bool run1RepeatPending = RunFinalizer.TryFinalize(run1);
            Assert.IsFalse(run1RepeatPending);
            Assert.AreEqual(1, RunFinalizer.PendingCount);

            // Save works again
            toggleOps.FailWrites = false;

            // Fresh run 2 finishes and finalizes
            var run2 = new RunResult(
                runId: "run-fresh",
                missionId: "arena",
                outcome: RunOutcome.Victory,
                deploymentLoadout: DeploymentLoadoutSnapshot.Empty,
                elapsedActiveTime: 35f,
                finalScore: 350,
                totalKills: 10,
                wavesCleared: 2,
                timestampUtc: DateTime.UtcNow);

            bool run2Success = RunFinalizer.TryFinalize(run2);
            Assert.IsTrue(run2Success);

            // Finalization retries all queued runs in order, so both Run 1's frozen 3
            // scrap and Run 2's fresh 25 scrap are now persisted.
            UserProfileData profileAfterRun2 = SaveManager.LoadProfile();
            Assert.IsTrue(profileAfterRun2.HasFinalizedRun("run-fresh"));
            Assert.IsTrue(profileAfterRun2.HasFinalizedRun("run-failed-save"));
            Assert.AreEqual(28, profileAfterRun2.GetComponentCount("scrap"));
            Assert.AreEqual(2, profileAfterRun2.GetComponentCount("core"));
            Assert.AreEqual(0, RunFinalizer.PendingCount);

            // Repeating either retry path must not duplicate either run's frozen payout
            // or consume any salvage that belongs to a later run.
            tracker.RecordPickup(EconomyComponentType.Scrap, 7);
            bool retryRun1Success = RunFinalizer.RetryPending();
            Assert.IsTrue(retryRun1Success);
            Assert.AreEqual(0, RunFinalizer.PendingCount);
            Assert.AreEqual(7, tracker.ScrapCollected);

            UserProfileData profileAfterRetry = SaveManager.LoadProfile();
            Assert.IsTrue(profileAfterRetry.HasFinalizedRun("run-failed-save"));
            Assert.IsTrue(profileAfterRetry.HasFinalizedRun("run-fresh"));
            Assert.AreEqual(28, profileAfterRetry.GetComponentCount("scrap"));
            Assert.AreEqual(2, profileAfterRetry.GetComponentCount("core"));

            // Re-finalizing Run 1 after successful save has no effect (idempotent)
            bool run1AfterSuccess = RunFinalizer.TryFinalize(run1);
            Assert.IsTrue(run1AfterSuccess);
            UserProfileData profileFinal = SaveManager.LoadProfile();
            Assert.AreEqual(28, profileFinal.GetComponentCount("scrap"));
            Assert.AreEqual(2, profileFinal.GetComponentCount("core"));
            Assert.AreEqual(7, tracker.ScrapCollected);

            UnityEngine.Object.DestroyImmediate(trackerGo);
            Game.Economy.RunSalvageTracker.ResetForTesting();
        }

        private class ToggleOps : SaveManager.IFileOperations
        {
            private readonly SaveManager.DefaultFileOperations _inner = new SaveManager.DefaultFileOperations();
            public bool FailWrites;
            public bool Exists(string path) => _inner.Exists(path);
            public string ReadAllText(string path) => _inner.ReadAllText(path);
            public void WriteAllText(string path, string contents) => _inner.WriteAllText(path, contents);
            public void Copy(string s, string d, bool o) => _inner.Copy(s, d, o);
            public void Replace(string s, string d, string b, bool i) => _inner.Replace(s, d, b, i);
            public void Delete(string path) => _inner.Delete(path);
            public void Move(string s, string d) => _inner.Move(s, d);
            public Stream Create(string path) => FailWrites ? throw new IOException("Disk error") : _inner.Create(path);
            public long GetLength(string path) => _inner.GetLength(path);
        }
    }
}
