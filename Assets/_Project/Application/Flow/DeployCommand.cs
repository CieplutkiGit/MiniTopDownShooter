using System;

namespace Application.Flow
{
    public sealed class DeployCommand
    {
        public string MissionId { get; }
        public DeploymentLoadoutSnapshot Loadout { get; }

        public DeployCommand(string missionId, DeploymentLoadoutSnapshot loadout = null)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                throw new ArgumentException("MissionId cannot be null or empty.", nameof(missionId));
            }

            MissionId = missionId;
            Loadout = loadout ?? DeploymentLoadoutSnapshot.Empty;
        }
    }
}
