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

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogError($"[SaveManager] Serialized data is empty for '{filePath}'.");
                    return false;
                }

                // Write to temp file and flush completely to disk
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (new FileInfo(tempPath).Length == 0)
                {
                    Debug.LogError($"[SaveManager] Temp file is empty after write: '{tempPath}'.");
                    return false;
                }

                if (File.Exists(filePath))
                {
                    // Preserve validated backup of the existing valid file
                    try
                    {
                        File.Copy(filePath, backupPath, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveManager] Failed to create backup: {ex.Message}");
                    }

                    // Atomic replacement supported by platform
                    try
                    {
                        File.Replace(tempPath, filePath, null, true);
                    }
                    catch
                    {
                        File.Move(tempPath, filePath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, filePath, overwrite: true);
                    try
                    {
                        File.Copy(filePath, backupPath, true);
                    }
                    catch {}
                }

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

            // 1. Try primary file
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
                            ValidateLoadedObject(result);
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Primary file corrupt at '{filePath}': {ex.Message}. Attempting backup recovery.");
                }
            }

            // 2. Try backup recovery
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
                            ValidateLoadedObject(result);
                            Debug.Log($"[SaveManager] Successfully recovered from backup '{backupPath}'. Restoring primary.");
                            AtomicWrite(filePath, result);
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

        private static void ValidateLoadedObject<T>(T obj)
        {
            if (obj is UserProfileData profile)
            {
                profile.ValidateAndMigrate();
            }
            else if (obj is GameSettingsData settings)
            {
                settings.ValidateAndMigrate();
            }
        }
    }
}
