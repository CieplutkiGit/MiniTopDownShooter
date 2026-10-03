using System;

namespace Application.Flow
{
    public enum RunOutcome
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Abandoned = 3,
        TechnicalError = 4
    }

    public static class RunOutcomeExtensions
    {
        public static bool IsTerminal(this RunOutcome outcome)
        {
            return outcome != RunOutcome.None;
        }

        public static bool CountsAsRun(this RunOutcome outcome)
        {
            return outcome == RunOutcome.Victory ||
                   outcome == RunOutcome.Defeat ||
                   outcome == RunOutcome.Abandoned;
        }

        public static bool IncrementsWins(this RunOutcome outcome)
        {
            return outcome == RunOutcome.Victory;
        }

        public static bool IncrementsLosses(this RunOutcome outcome)
        {
            return outcome == RunOutcome.Defeat;
        }

        public static bool RecordsScoreAndKills(this RunOutcome outcome)
        {
            return outcome == RunOutcome.Victory || outcome == RunOutcome.Defeat;
        }
    }
}
