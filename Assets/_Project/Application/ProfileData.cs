using System;

namespace Application
{
    [Serializable]
    public class UserProfileData
    {
        public int Version = 1;
        public int HighScore = 0;
        public int TotalKills = 0;
        public int TotalRuns = 0;
        public int TotalWins = 0;
        public int TotalLosses = 0;
    }

    [Serializable]
    public class GameSettingsData
    {
        public int Version = 1;
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 1.0f;
        public float AimSensitivity = 1.0f;
        public float Deadzone = 0.1f;
        public bool MobileTouchControls = false;
        public float TouchControlScale = 1.0f;
    }
}
