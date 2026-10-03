using System;

namespace Application.Flow
{
    public sealed class RunResult
    {
        public string RunId { get; }
        public string MissionId { get; }
        public RunOutcome Outcome { get; }
        public DeploymentLoadoutSnapshot DeploymentLoadout { get; }
        public float ElapsedActiveTime { get; }
        public int FinalScore { get; }
        public int TotalKills { get; }
        public int WavesCleared { get; }
        public DateTime TimestampUtc { get; }

        // ── Economy & Salvage Rewards ──────────────────────────────────────
        public int ScrapCollected { get; }
        public int AlloyCollected { get; }
        public int CoreCollected { get; }
        public int EarnedCoins { get; }
        public int EarnedXp { get; }

        public RunResult(
            string runId,
            string missionId,
            RunOutcome outcome,
            DeploymentLoadoutSnapshot deploymentLoadout,
            float elapsedActiveTime,
            int finalScore,
            int totalKills,
            int wavesCleared,
            DateTime? timestampUtc = null,
            int scrapCollected = 0,
            int alloyCollected = 0,
            int coreCollected = 0,
            int earnedCoins = 0,
            int earnedXp = 0)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException("RunId cannot be null or empty.", nameof(runId));
            }

            RunId = runId;
            MissionId = missionId ?? string.Empty;
            Outcome = outcome;
            DeploymentLoadout = deploymentLoadout ?? DeploymentLoadoutSnapshot.Empty;
            ElapsedActiveTime = Math.Max(0f, elapsedActiveTime);
            FinalScore = Math.Max(0, finalScore);
            TotalKills = Math.Max(0, totalKills);
            WavesCleared = Math.Max(0, wavesCleared);
            TimestampUtc = timestampUtc ?? DateTime.UtcNow;

            ScrapCollected = Math.Max(0, scrapCollected);
            AlloyCollected = Math.Max(0, alloyCollected);
            CoreCollected = Math.Max(0, coreCollected);
            EarnedCoins = Math.Max(0, earnedCoins);
            EarnedXp = Math.Max(0, earnedXp);
        }

        public bool CountsAsRun => Outcome.CountsAsRun();
        public bool IncrementsWins => Outcome.IncrementsWins();
        public bool IncrementsLosses => Outcome.IncrementsLosses();
        public bool RecordsScoreAndKills => Outcome.RecordsScoreAndKills();
    }
}
