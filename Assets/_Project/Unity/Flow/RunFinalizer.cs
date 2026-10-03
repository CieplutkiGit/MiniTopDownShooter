using System;
using System.Collections.Generic;
using Application;
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

            if (!ContainsPending(result.RunId)) PendingRuns.Add(result);
            RetryPending();
            return !ContainsPending(result.RunId);
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

                    profile.RecordFinalizedRun(result.RunId);
                    if (!SaveManager.SaveProfile(profile))
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
