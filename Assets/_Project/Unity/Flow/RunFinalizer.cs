using System;
using System.Collections.Generic;
using Application;
using Application.Economy;
using Application.Flow;
using UnityEngine;

namespace Game.Flow
{
    public enum RunSaveStatus
    {
        Saved,
        AlreadySaved,
        PendingRetry,
        NothingPending
    }

    /// <summary>Applies each completed run once and persists its ledger with its statistics.</summary>
    public static class RunFinalizer
    {
        private static readonly List<RunResult> PendingRuns = new List<RunResult>();

        public static bool ManagedFinalizationEnabled { get; set; }
        public static int PendingCount => PendingRuns.Count;
        public static RunSaveStatus CurrentSaveStatus { get; private set; } = RunSaveStatus.NothingPending;
        public static event Action<RunSaveStatus> SaveStatusChanged;

        public static bool TryFinalize(RunResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.RunId)) return false;

            UserProfileData saved = SaveManager.LoadProfile() ?? new UserProfileData();
            saved.ValidateAndMigrate();
            if (saved.HasFinalizedRun(result.RunId))
            {
                PendingRuns.RemoveAll(pending => pending.RunId == result.RunId);
                SetStatus(PendingRuns.Count > 0 ? RunSaveStatus.PendingRetry : RunSaveStatus.AlreadySaved);
                return true;
            }

            if (ContainsPending(result.RunId))
            {
                RetryPending();
                return !ContainsPending(result.RunId);
            }

            RunResult frozenResult = FreezeSalvageIntoResult(result);
            PendingRuns.Add(frozenResult);
            RetryPending();
            return !ContainsPending(frozenResult.RunId);
        }

        /// <summary>Retries queued completions in arrival order against the latest saved profile.</summary>
        public static bool RetryPending()
        {
            if (PendingRuns.Count == 0)
            {
                SetStatus(RunSaveStatus.NothingPending);
                return true;
            }

            while (PendingRuns.Count > 0)
            {
                RunResult result = PendingRuns[0];
                UserProfileData profile = SaveManager.LoadProfile() ?? new UserProfileData();
                profile.ValidateAndMigrate();
                if (!profile.HasFinalizedRun(result.RunId))
                {
                    if (result.CountsAsRun)
                    {
                        profile.TotalRuns++;
                        if (result.IncrementsWins) profile.TotalWins++;
                        if (result.IncrementsLosses) profile.TotalLosses++;
                        if (result.RecordsScoreAndKills)
                        {
                            profile.TotalKills += result.TotalKills;
                            if (result.FinalScore > profile.HighScore) profile.HighScore = result.FinalScore;
                        }
                    }

                    // Apply economy payout exactly once across retry and reload
                    EconomyRewardCalculator.ApplyRunReward(profile, result);
                    profile.RecordFinalizedRun(result.RunId);

                    bool saveSuccess = false;
                    try
                    {
                        saveSuccess = SaveManager.SaveProfile(profile);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RunFinalizer] Exception persisting run {result.RunId}: {ex.Message}");
                        saveSuccess = false;
                    }

                    if (!saveSuccess)
                    {
                        Debug.LogError($"[RunFinalizer] Failed to persist run {result.RunId}; it remains queued for retry.");
                        SetStatus(RunSaveStatus.PendingRetry);
                        return false;
                    }
                }

                PendingRuns.RemoveAt(0);
            }

            SetStatus(RunSaveStatus.Saved);
            return true;
        }

        private static RunResult FreezeSalvageIntoResult(RunResult result)
        {
            if (result == null) return null;

            int scrap = result.ScrapCollected;
            int alloy = result.AlloyCollected;
            int core = result.CoreCollected;

            var tracker = Game.Economy.RunSalvageTracker.Instance;
            bool trackerBelongsToRun = tracker != null &&
                (string.IsNullOrEmpty(tracker.RunId) || string.Equals(tracker.RunId, result.RunId, StringComparison.Ordinal));
            bool resultHasSalvage = scrap > 0 || alloy > 0 || core > 0;

            if (trackerBelongsToRun && !resultHasSalvage && tracker.TotalComponentsCollected > 0)
            {
                scrap = tracker.ScrapCollected;
                alloy = tracker.AlloyCollected;
                core = tracker.CoreCollected;
                tracker.ResetTracker();
            }
            else if (trackerBelongsToRun && resultHasSalvage)
            {
                // Preserve explicit result values while consuming any duplicate live tracker copy.
                tracker.ResetTracker();
            }

            return new RunResult(
                result.RunId,
                result.MissionId,
                result.Outcome,
                result.DeploymentLoadout,
                result.ElapsedActiveTime,
                result.FinalScore,
                result.TotalKills,
                result.WavesCleared,
                result.TimestampUtc,
                scrap,
                alloy,
                core,
                result.EarnedCoins,
                result.EarnedXp);
        }

        public static void ResetForTesting()
        {
            PendingRuns.Clear();
            ManagedFinalizationEnabled = false;
            SetStatus(RunSaveStatus.NothingPending);
        }

        private static bool ContainsPending(string runId)
        {
            return PendingRuns.Exists(pending => pending.RunId == runId);
        }

        private static void SetStatus(RunSaveStatus status)
        {
            if (CurrentSaveStatus == status) return;
            CurrentSaveStatus = status;
            SaveStatusChanged?.Invoke(status);
        }
    }
}
