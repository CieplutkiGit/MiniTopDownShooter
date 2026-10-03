using System;
using System.Collections.Generic;

namespace Application
{
    [Serializable]
    public class UserProfileData
    {
        public int Version = 2;
        public int HighScore = 0;
        public int TotalKills = 0;
        public int TotalRuns = 0;
        public int TotalWins = 0;
        public int TotalLosses = 0;
        public List<string> FinalizedRunIds = new List<string>();

        public bool HasFinalizedRun(string runId)
        {
            return !string.IsNullOrWhiteSpace(runId) && FinalizedRunIds != null && FinalizedRunIds.Contains(runId);
        }

        public bool RecordFinalizedRun(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false;
            if (FinalizedRunIds == null) FinalizedRunIds = new List<string>();
            if (FinalizedRunIds.Contains(runId)) return false;
            FinalizedRunIds.Add(runId);
            return true;
        }

        public void ValidateAndMigrate()
        {
            if (Version < 1)
            {
                Version = 1;
            }
            if (Version < 2) Version = 2;
            if (FinalizedRunIds == null) FinalizedRunIds = new List<string>();
            FinalizedRunIds.RemoveAll(string.IsNullOrWhiteSpace);
            if (HighScore < 0) HighScore = 0;
            if (TotalKills < 0) TotalKills = 0;
            if (TotalRuns < 0) TotalRuns = 0;
            if (TotalWins < 0) TotalWins = 0;
            if (TotalLosses < 0) TotalLosses = 0;
        }
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

        public void ValidateAndMigrate()
        {
            if (Version < 1)
            {
                Version = 1;
            }
            MasterVolume = Clamp01(MasterVolume, 1.0f);
            MusicVolume = Clamp01(MusicVolume, 0.8f);
            SFXVolume = Clamp01(SFXVolume, 1.0f);
            AimSensitivity = Clamp(AimSensitivity, 0.05f, 10.0f, 1.0f);
            Deadzone = Clamp(Deadzone, 0f, 0.9f, 0.1f);
            TouchControlScale = Clamp(TouchControlScale, 0.5f, 2.5f, 1.0f);
        }

        private static float Clamp01(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
