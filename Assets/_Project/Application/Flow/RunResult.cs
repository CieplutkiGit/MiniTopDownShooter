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

        public RunResult(
            string runId,
            string missionId,
            RunOutcome outcome,
            DeploymentLoadoutSnapshot deploymentLoadout,
            float elapsedActiveTime,
            int finalScore,
            int totalKills,
            int wavesCleared,
            DateTime? timestampUtc = null)
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
        }

        public bool CountsAsRun => Outcome.CountsAsRun();
        public bool IncrementsWins => Outcome.IncrementsWins();
        public bool IncrementsLosses => Outcome.IncrementsLosses();
        public bool RecordsScoreAndKills => Outcome.RecordsScoreAndKills();
    }
}
