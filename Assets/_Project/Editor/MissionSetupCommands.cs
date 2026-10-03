using UnityEditor;
using UnityEngine;

namespace Game.Flow.Editor
{
    /// <summary>
    /// T08/T13: Editor utility to create the Mission_ArenaSweep asset
    /// and configure the production build scenes.
    /// Run via Mini Top Down Shooter > Setup > Create Arena Sweep Mission.
    /// </summary>
    public static class MissionSetupCommands
    {
        private const string MissionAssetPath = "Assets/_Project/Data/Missions/Mission_ArenaSweep.asset";
        private const string WaveSetPath = "Assets/_Project/Data/Waves/WaveSet_ArenaShowcase.asset";

        [MenuItem("Mini Top Down Shooter/Setup/Create Arena Sweep Mission")]
        public static void CreateArenaSweepMission()
        {
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(MissionAssetPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                AssetDatabase.CreateAsset(mission, MissionAssetPath);
            }

            // Try to wire the WaveSet
            WaveSet waveSet = AssetDatabase.LoadAssetAtPath<WaveSet>(WaveSetPath);
            if (waveSet != null)
            {
                var so = new SerializedObject(mission);
                so.FindProperty("_waveSet").objectReferenceValue = waveSet;
                so.FindProperty("_missionId").stringValue = "Mission_ArenaSweep";
                so.FindProperty("_displayName").stringValue = "Arena Sweep";
                so.FindProperty("_briefingText").stringValue = "Clear four enemy waves and defeat the arena commander. Your committed weapon build deploys with you.";
                so.FindProperty("_sceneName").stringValue = "ArenaShowcase";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = mission;
            Debug.Log($"[MissionSetup] Configured {MissionAssetPath}.");
        }

        [MenuItem("Mini Top Down Shooter/Setup/Configure Production Build Scenes")]
        public static void ConfigureProductionBuildScenes()
        {
            // Production scene order: Boot (0), BaseHub (1), ArenaShowcase (2)
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/BaseHub.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/ArenaShowcase.unity", true),
                // Legacy/dev scenes kept for CI/testing
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/MobileDemo.unity", false),
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[MissionSetup] Production build scene order configured. Boot=0, BaseHub=1, ArenaShowcase=2.");
        }
    }
}
