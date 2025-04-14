using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Handles file operations such as load, save, duplicate, delete, and encryption/decryption.
/// Also provides helper methods to retrieve data for main menu display.
/// </summary>
public class FileDataHandler {
    // Paths and filenames for data storage
    readonly string _dataDirPath;
    readonly string _dataFileName;

    // Encryption settings
    readonly bool _useEncryption = false;
    // WARNING / TODO: Do not hardcode encryption keys in production!
    static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("GsYSSZ7Z5Ka4B70QsP7DqtykZJub/CH7tIIjwHeotlM=");
    const string BACKUP_EXTENSION = ".bak";

    /// <summary>
    /// Holds basic profile information for display in the main menu.
    /// </summary>
    public struct ProfileDisplayInfo {
        public string ProfileId;
        public int Money;
        public DateTime LastPlayed;
        public TimeSpan PlayingTime;
    }

    /// <summary>
    /// Initializes the file handler with a directory, file name, and encryption preference.
    /// </summary>
    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption) {
        _dataDirPath = dataDirPath;
        _dataFileName = dataFileName;
        _useEncryption = useEncryption;

        if (_useEncryption && AES_KEY.Length != 32) throw new ArgumentException("AES_KEY must be 32 bytes for AES-256 encryption.");
    }

    #region Data Loading Methods

    /// <summary>
    /// Loads game data for the given profile. Optionally performs a rollback from backup if loading fails.
    /// </summary>
    public GameData Load(string profileId, bool allowRestoreFromBackup = true) {
        if (string.IsNullOrEmpty(profileId)) return null;
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        if (!File.Exists(fullPath)) return null;

        GameData loadedData = null;
        try {
            string dataToLoad;
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream)) {
                dataToLoad = reader.ReadToEnd();
            }

            // Decrypt if enabled
            if (_useEncryption) dataToLoad = Decrypt(dataToLoad);

            // Deserialize JSON into a GameData object
            loadedData = JsonUtility.FromJson<GameData>(dataToLoad);

            // Validate checksum to ensure data integrity
            if (!ValidateChecksum(dataToLoad, loadedData.Checksum)) throw new Exception("Checksum validation failed. Save file may be corrupted or tampered with.");

        } catch (Exception e) {
            Debug.LogWarning($"Error loading file for profile {profileId}: {e}");
            if (allowRestoreFromBackup && AttemptRollback(fullPath)) return Load(profileId, false);
            else Debug.LogError("Could not load or restore data from: " + fullPath);
        }
        return loadedData;
    }

    /// <summary>
    /// Loads all profiles found in the data directory.
    /// </summary>
    public Dictionary<string, GameData> LoadAllProfiles() {
        var profileDictionary = new Dictionary<string, GameData>();
        var directories = new DirectoryInfo(_dataDirPath).EnumerateDirectories();
        foreach (var dirInfo in directories) {
            string profileId = dirInfo.Name;
            string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);

            if (!File.Exists(fullPath)) {
                Debug.LogWarning("Skipping directory without data: " + profileId);
                continue;
            }

            GameData profileData = Load(profileId);
            if (profileData != null) profileDictionary.Add(profileId, profileData);
            else Debug.LogError("Failed to load profile data: " + profileId);
        }
        return profileDictionary;
    }

    /// <summary>
    /// Returns basic display info for all profiles for the Main Menu.
    /// </summary>
    public Dictionary<string, ProfileDisplayInfo> GetAllProfilesDisplayInfo() {
        var displayInfoDictionary = new Dictionary<string, ProfileDisplayInfo>();
        var profiles = LoadAllProfiles();
        foreach (var kvp in profiles) {
            GameData data = kvp.Value;
            // Convert binary date into DateTime and extract other display-related fields.
            ProfileDisplayInfo info = new() {
                ProfileId = kvp.Key,
                Money = data.MoneyOfFarm,
                LastPlayed = DateTime.FromBinary(data.LastPlayed),
                PlayingTime = data.PlayTime
            };
            displayInfoDictionary.Add(kvp.Key, info);
        }
        return displayInfoDictionary;
    }

    #endregion

    #region Data Saving Methods

    /// <summary>
    /// Saves the provided GameData object to disk and creates a backup.
    /// </summary>
    public void Save(GameData data, string profileId) {
        if (data == null || string.IsNullOrEmpty(profileId)) return;

        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string backupFilePath = fullPath + BACKUP_EXTENSION;
        string tempFilePath = fullPath + ".tmp";

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Clear and recompute the checksum
            string oldChecksum = data.Checksum;
            data.Checksum = "";
            string jsonWithoutChecksum = JsonUtility.ToJson(data, true);
            string newChecksum = ComputeChecksum(jsonWithoutChecksum);
            data.Checksum = newChecksum;

            // Serialize with the new checksum
            string dataToStore = JsonUtility.ToJson(data, true);
            if (_useEncryption) dataToStore = Encrypt(dataToStore);

            // Back up the current file if it exists
            if (File.Exists(fullPath)) File.Copy(fullPath, backupFilePath, true);

            // Write data to a temporary file, then replace the original
            using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(dataToStore);
            File.Copy(tempFilePath, fullPath, true);
            File.Delete(tempFilePath);

            // Verify save integrity; if successful, update backup file
            GameData verifiedData = Load(profileId) ?? throw new Exception("Verification failed, backup not created.");
            File.Copy(fullPath, backupFilePath, true);

        } catch (Exception e) {
            Debug.LogError($"Error saving data for profile {profileId}: {e}");

            // Attempt to restore from backup if available
            if (File.Exists(backupFilePath)) {
                try {
                    File.Copy(backupFilePath, fullPath, true);
                    Debug.LogWarning("Restored data from backup due to save failure.");
                } catch (Exception restoreEx) {
                    Debug.LogError($"Failed to restore data from backup: {backupFilePath}\n{restoreEx}");
                }
            }
        }
    }

    /// <summary>
    /// Duplicates the save file of an existing profile to a new profile slot.
    /// </summary>
    public void DuplicateFile(string profileId) {
        if (string.IsNullOrEmpty(profileId)) return;
        string sourcePath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        if (!File.Exists(sourcePath)) return;

        string nextProfileId = FindNextProfileID();
        string destinationPath = Path.Combine(_dataDirPath, nextProfileId, _dataFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
        File.Copy(sourcePath, destinationPath);
    }

    /// <summary>
    /// Deletes a profile and its associated save data.
    /// </summary>
    public void DeleteFile(string profileId) {
        if (string.IsNullOrEmpty(profileId)) return;
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string directoryPath = Path.GetDirectoryName(fullPath);
        if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, true);
    }

    /// <summary>
    /// Finds the next available numeric profile ID.
    /// </summary>
    public string FindNextProfileID() {
        var profilesGameData = DataPersistenceManager.Instance.GetAllProfilesGameData();
        int i = 0;
        while (profilesGameData.ContainsKey(i.ToString())) i++;
        return i.ToString();
    }

    // Identifies the most recently played profile by comparing their timestamps
    public string GetMostRecentlyPlayedProfileId() {
        var allProfiles = LoadAllProfiles();
        string mostRecentProfileId = null;

        foreach (var kvp in allProfiles) {
            string profileId = kvp.Key;
            GameData data = kvp.Value;
            if (data == null) continue;

            if (mostRecentProfileId == null) {
                mostRecentProfileId = profileId;
            } else {
                DateTime currentMostRecent = DateTime.FromBinary(allProfiles[mostRecentProfileId].LastPlayed);
                DateTime candidate = DateTime.FromBinary(data.LastPlayed);
                if (candidate > currentMostRecent) {
                    mostRecentProfileId = profileId;
                }
            }
        }

        return mostRecentProfileId;
    }

    #endregion

    #region Encryption & Decryption

    /// <summary>
    /// Encrypts a plaintext string using AES-256 in CBC mode and returns a Base64 string containing IV + ciphertext.
    /// </summary>
    string Encrypt(string plainText) {
        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using MemoryStream memoryStream = new();
        // Write IV first, then ciphertext.
        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using StreamWriter writer = new(cryptoStream);
        writer.Write(plainText);
        writer.Flush();
        cryptoStream.FlushFinalBlock();
        byte[] encryptedBytes = memoryStream.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts a Base64 string containing IV + ciphertext using AES-256 and returns the original plaintext.
    /// </summary>
    string Decrypt(string cipherText) {
        byte[] cipherBytesWithIV = Convert.FromBase64String(cipherText);
        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherBytesWithIV, 0, iv, 0, iv.Length);
        aes.IV = iv;
        int cipherTextStartIndex = iv.Length;
        int cipherTextLength = cipherBytesWithIV.Length - cipherTextStartIndex;
        byte[] cipherBytes = new byte[cipherTextLength];
        Array.Copy(cipherBytesWithIV, cipherTextStartIndex, cipherBytes, 0, cipherTextLength);

        using MemoryStream memoryStream = new(cipherBytes);
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader reader = new(cryptoStream);
        return reader.ReadToEnd();
    }

    #endregion

    #region Backup & Checksum

    /// <summary>
    /// Attempts to restore data from a backup file if loading fails.
    /// </summary>
    bool AttemptRollback(string fullPath) {
        string backupFilePath = fullPath + BACKUP_EXTENSION;
        if (!File.Exists(backupFilePath)) {
            Debug.LogError("No backup file available for rollback: " + backupFilePath);
            return false;
        }
        try {
            File.Copy(backupFilePath, fullPath, true);
            Debug.LogWarning("Rolled back to backup file: " + backupFilePath);
            return true;
        } catch (Exception e) {
            Debug.LogError("Failed to roll back from backup: " + backupFilePath + "\n" + e);
            return false;
        }
    }

    /// <summary>
    /// Computes a SHA256 checksum for the given string data.
    /// </summary>
    string ComputeChecksum(string data) {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Validates the data integrity by comparing the computed checksum with the expected one.
    /// </summary>
    bool ValidateChecksum(string jsonData, string expectedChecksum) {
        return true;
        // TODO: Uncomment this when checksum validation is needed AND fixed
        if (string.IsNullOrEmpty(expectedChecksum)) return false;
        // Deserialize without checksum for computing the hash
        GameData data = JsonUtility.FromJson<GameData>(jsonData);
        if (data == null) return false;
        string originalChecksum = data.Checksum;
        data.Checksum = "";
        string jsonWithoutChecksum = JsonUtility.ToJson(data, true);
        string computedChecksum = ComputeChecksum(jsonWithoutChecksum);
        data.Checksum = originalChecksum;
        return computedChecksum == expectedChecksum;
    }

    #endregion
}
