using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// T08: Authored ScriptableObject asset that defines a single mission.
    /// Stable mission ID, display/briefing data, arena scene reference, and WaveSet.
    /// Adding a new mission = authoring a new scene + MissionDefinition asset.
    /// </summary>
    [CreateAssetMenu(fileName = "MissionDefinition", menuName = "Mini Top Down Shooter/Flow/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        [Tooltip("Stable, unique identifier used for analytics and save data.")]
        [SerializeField] private string _missionId = "Mission_ArenaSweep";

        [Tooltip("Human-readable mission name shown in briefing UI.")]
        [SerializeField] private string _displayName = "Arena Sweep";

        [TextArea(2, 5)]
        [Tooltip("Short briefing shown before deployment.")]
        [SerializeField] private string _briefingText = "Clear all waves and defeat the boss.";

        [Tooltip("Exact name of the Unity scene to load for this mission.")]
        [SerializeField] private string _sceneName = "ArenaShowcase";

        [Tooltip("WaveSet asset used by WaveController in the mission scene.")]
        [SerializeField] private WaveSet _waveSet;

        public string MissionId => _missionId;
        public string DisplayName => _displayName;
        public string BriefingText => _briefingText;
        public string SceneName => _sceneName;
        public WaveSet WaveSet => _waveSet;
    }
}
