using System;

namespace Application.Flow
{
    public class RunSession
    {
        public string RunId { get; }
        public string MissionId { get; }
        public DeploymentLoadoutSnapshot DeploymentLoadout { get; }

        public RunOutcome Outcome { get; private set; }
        public bool IsCompleted { get; private set; }
        public RunResult FinalResult { get; private set; }

        public float ElapsedActiveTime { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int WavesCleared { get; private set; }
        public int ScrapCollected { get; private set; }
        public int AlloyCollected { get; private set; }
        public int CoreCollected { get; private set; }

        public void SetSalvage(int scrap, int alloy, int core)
        {
            if (IsCompleted) return;
            ScrapCollected = Math.Max(0, scrap);
            AlloyCollected = Math.Max(0, alloy);
            CoreCollected = Math.Max(0, core);
        }

        public RunSession(string runId, string missionId, DeploymentLoadoutSnapshot deploymentLoadout = null)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException("RunId cannot be null or empty.", nameof(runId));
            }

            RunId = runId;
            MissionId = missionId ?? string.Empty;
            DeploymentLoadout = deploymentLoadout ?? DeploymentLoadoutSnapshot.Empty;
            Outcome = RunOutcome.None;
            IsCompleted = false;
        }

        public void RecordActiveTime(float deltaSeconds)
        {
            if (IsCompleted || deltaSeconds <= 0f) return;
            ElapsedActiveTime += deltaSeconds;
        }

        public void SetScore(int score)
        {
            if (IsCompleted) return;
            Score = Math.Max(0, score);
        }

        public void AddScore(int points)
        {
            if (IsCompleted || points <= 0) return;
            Score += points;
        }

        public void SetKills(int kills)
        {
            if (IsCompleted) return;
            Kills = Math.Max(0, kills);
        }

        public void AddKill()
        {
            if (IsCompleted) return;
            Kills++;
        }

        public void SetWavesCleared(int waves)
        {
            if (IsCompleted) return;
            WavesCleared = Math.Max(0, waves);
        }

        /// <summary>
        /// Finalizes the run with a terminal outcome. Idempotent: subsequent calls return false
        /// and preserve the original terminal outcome and result.
        /// </summary>
        public bool TryComplete(RunOutcome outcome, int? finalScore = null, int? finalKills = null, int? finalWavesCleared = null)
        {
            if (IsCompleted)
            {
                return false;
            }

            if (outcome == RunOutcome.None)
            {
                throw new ArgumentException("Cannot complete run with RunOutcome.None.", nameof(outcome));
            }

            if (finalScore.HasValue) SetScore(finalScore.Value);
            if (finalKills.HasValue) SetKills(finalKills.Value);
            if (finalWavesCleared.HasValue) SetWavesCleared(finalWavesCleared.Value);

            Outcome = outcome;
            IsCompleted = true;
            FinalResult = new RunResult(
                RunId,
                MissionId,
                Outcome,
                DeploymentLoadout,
                ElapsedActiveTime,
                Score,
                Kills,
                WavesCleared,
                scrapCollected: ScrapCollected,
                alloyCollected: AlloyCollected,
                coreCollected: CoreCollected);

            return true;
        }
    }
}
