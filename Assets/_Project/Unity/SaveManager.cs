using System;
using System.IO;
using Application;
using UnityEngine;

namespace Game
{
    public static class SaveManager
    {
        private const string ProfileFileName = "user_profile.json";
        private const string SettingsFileName = "game_settings.json";

        public static string SaveDirectory => UnityEngine.Application.persistentDataPath;

        public static bool SaveProfile(UserProfileData profile)
        {
            string path = Path.Combine(SaveDirectory, ProfileFileName);
            return AtomicWrite(path, profile);
        }

        public static UserProfileData LoadProfile()
        {
            string path = Path.Combine(SaveDirectory, ProfileFileName);
            UserProfileData loaded = SafeRead<UserProfileData>(path);
            if (loaded == null)
            {
                loaded = new UserProfileData();
                SaveProfile(loaded);
            }
            return loaded;
        }

        public static bool SaveSettings(GameSettingsData settings)
        {
            string path = Path.Combine(SaveDirectory, SettingsFileName);
            return AtomicWrite(path, settings);
        }

        public static GameSettingsData LoadSettings()
        {
            string path = Path.Combine(SaveDirectory, SettingsFileName);
            GameSettingsData loaded = SafeRead<GameSettingsData>(path);
            if (loaded == null)
            {
                loaded = new GameSettingsData();
                SaveSettings(loaded);
            }
            return loaded;
        }

        public static bool SaveToFile<T>(string filePath, T data)
        {
            return AtomicWrite(filePath, data);
        }

        public static T LoadFromFile<T>(string filePath) where T : class, new()
        {
            return SafeRead<T>(filePath);
        }

        private static bool AtomicWrite<T>(string filePath, T data)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = filePath + ".tmp";
                string backupPath = filePath + ".bak";
                string json = JsonUtility.ToJson(data, true);

                File.WriteAllText(tempPath, json);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Copy(filePath, backupPath, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveManager] Failed to create backup: {ex.Message}");
                    }
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Move(tempPath, filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error saving to '{filePath}': {ex.Message}");
                return false;
            }
        }

        private static T SafeRead<T>(string filePath) where T : class, new()
        {
            string backupPath = filePath + ".bak";

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        T result = JsonUtility.FromJson<T>(json);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Primary file corrupt at '{filePath}': {ex.Message}. Attempting backup recovery.");
                }
            }

            if (File.Exists(backupPath))
            {
                try
                {
                    string backupJson = File.ReadAllText(backupPath);
                    if (!string.IsNullOrWhiteSpace(backupJson))
                    {
                        T result = JsonUtility.FromJson<T>(backupJson);
                        if (result != null)
                        {
                            Debug.Log($"[SaveManager] Successfully recovered from backup '{backupPath}'.");
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Backup file also corrupt at '{backupPath}': {ex.Message}. Falling back to default.");
                }
            }

            return null;
        }
    }
}
