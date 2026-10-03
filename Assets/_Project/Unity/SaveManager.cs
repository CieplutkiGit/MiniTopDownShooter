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
        private const string WorkshopFileName = "weapon_workshop.json";

        public static string CustomSaveDirectory { get; set; }
        public static string SaveDirectory => !string.IsNullOrEmpty(CustomSaveDirectory) ? CustomSaveDirectory : UnityEngine.Application.persistentDataPath;

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

        public static bool SaveWorkshopData(WeaponWorkshopSaveData data)
        {
            string path = Path.Combine(SaveDirectory, WorkshopFileName);
            return AtomicWrite(path, data);
        }

        public static WeaponWorkshopSaveData LoadWorkshopData()
        {
            string path = Path.Combine(SaveDirectory, WorkshopFileName);
            WeaponWorkshopSaveData loaded = SafeRead<WeaponWorkshopSaveData>(path);
            if (loaded == null)
            {
                loaded = new WeaponWorkshopSaveData();
                SaveWorkshopData(loaded);
            }
            return loaded;
        }

        public static bool SaveToFile<T>(string filePath, T data) where T : class, new()
        {
            return AtomicWrite(filePath, data);
        }

        public static T LoadFromFile<T>(string filePath) where T : class, new()
        {
            return SafeRead<T>(filePath);
        }

        public interface IFileOperations
        {
            bool Exists(string path);
            string ReadAllText(string path);
            void WriteAllText(string path, string contents);
            void Copy(string sourceFileName, string destFileName, bool overwrite);
            void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
            void Delete(string path);
            void Move(string sourceFileName, string destFileName);
            Stream Create(string path);
            long GetLength(string path);
        }

        public class DefaultFileOperations : IFileOperations
        {
            public bool Exists(string path) => File.Exists(path);
            public string ReadAllText(string path) => File.ReadAllText(path);
            public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
            public void Copy(string sourceFileName, string destFileName, bool overwrite) => File.Copy(sourceFileName, destFileName, overwrite);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
                => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            public void Delete(string path) => File.Delete(path);
            public void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);
            public Stream Create(string path) => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            public long GetLength(string path) => new FileInfo(path).Length;
        }

        public static IFileOperations FileOps { get; set; } = new DefaultFileOperations();

        private static bool AtomicWrite<T>(string filePath, T data) where T : class, new()
        {
            string tempPath = filePath + ".tmp";
            string backupPath = filePath + ".bak";

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogError($"[SaveManager] Serialized data is empty for '{filePath}'.");
                    return false;
                }

                // Write to temp file in the same directory and flush completely to disk
                using (var stream = FileOps.Create(tempPath))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush();
                }

                if (FileOps.GetLength(tempPath) == 0)
                {
                    Debug.LogError($"[SaveManager] Temp file is empty after write: '{tempPath}'.");
                    try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                    return false;
                }

                // Validate written temp file contents before replacing destination
                string writtenJson = FileOps.ReadAllText(tempPath);
                T validatedTemp = JsonUtility.FromJson<T>(writtenJson);
                if (validatedTemp == null)
                {
                    Debug.LogError($"[SaveManager] Temp file contents failed validation for '{tempPath}'.");
                    try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                    return false;
                }
                ValidateLoadedObject(validatedTemp);

                if (FileOps.Exists(filePath))
                {
                    // During ordinary saves, replace the backup ONLY with a successfully parsed and validated previous primary.
                    // Never copy corrupt primary bytes into .bak.
                    bool existingPrimaryValid = false;
                    try
                    {
                        string existingJson = FileOps.ReadAllText(filePath);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            T existingObj = JsonUtility.FromJson<T>(existingJson);
                            if (existingObj != null)
                            {
                                ValidateLoadedObject(existingObj);
                                existingPrimaryValid = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveManager] Existing primary at '{filePath}' is invalid ({ex.Message}); skipping backup replacement to preserve existing backup.");
                    }

                    if (existingPrimaryValid)
                    {
                        try
                        {
                            FileOps.Copy(filePath, backupPath, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[SaveManager] Failed to create backup from valid primary: {ex.Message}");
                        }
                    }

                    // Use supported atomic replacement; when unavailable or failing, preserve existing files and return an explicit failure.
                    try
                    {
                        FileOps.Replace(tempPath, filePath, null, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] Atomic replacement failed for '{filePath}': {ex.Message}. Preserving existing files.");
                        try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        FileOps.Move(tempPath, filePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] Move failed for '{filePath}': {ex.Message}");
                        try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error saving to '{filePath}': {ex.Message}");
                try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                return false;
            }
        }

        private static bool RestorePrimaryFromBackup<T>(string filePath, T validatedData) where T : class, new()
        {
            string tempPath = filePath + ".tmp";

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(validatedData, true);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                using (var stream = FileOps.Create(tempPath))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush();
                }

                if (FileOps.GetLength(tempPath) == 0)
                {
                    try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                    return false;
                }

                string writtenJson = FileOps.ReadAllText(tempPath);
                T validatedTemp = JsonUtility.FromJson<T>(writtenJson);
                if (validatedTemp == null)
                {
                    try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                    return false;
                }
                ValidateLoadedObject(validatedTemp);

                if (FileOps.Exists(filePath))
                {
                    try
                    {
                        FileOps.Replace(tempPath, filePath, null, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] Primary restore replacement failed for '{filePath}': {ex.Message}. Preserving existing backup and primary.");
                        try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        FileOps.Move(tempPath, filePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] Primary restore move failed for '{filePath}': {ex.Message}. Preserving backup.");
                        try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to restore primary from backup for '{filePath}': {ex.Message}");
                try { if (FileOps.Exists(tempPath)) FileOps.Delete(tempPath); } catch {}
                return false;
            }
        }

        private static T SafeRead<T>(string filePath) where T : class, new()
        {
            string backupPath = filePath + ".bak";

            // 1. Try primary file
            if (FileOps.Exists(filePath))
            {
                try
                {
                    string json = FileOps.ReadAllText(filePath);
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

            // 2. Try backup recovery (separate from ordinary saving; preserves validated backup while restoring primary)
            if (FileOps.Exists(backupPath))
            {
                try
                {
                    string backupJson = FileOps.ReadAllText(backupPath);
                    if (!string.IsNullOrWhiteSpace(backupJson))
                    {
                        T result = JsonUtility.FromJson<T>(backupJson);
                        if (result != null)
                        {
                            ValidateLoadedObject(result);
                            Debug.Log($"[SaveManager] Successfully recovered from backup '{backupPath}'. Restoring primary without touching backup.");
                            RestorePrimaryFromBackup(filePath, result);
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Backup file also corrupt at '{backupPath}': {ex.Message}. Falling back to default.");
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
            else if (obj is WeaponWorkshopSaveData workshopData)
            {
                workshopData.ValidateAndMigrate();
            }
        }
    }
}
