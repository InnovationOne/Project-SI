using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

// Represents the data file handler responsible for loading and saving game data.
public class FileDataHandler {
    // Directory and filename for data storage
    readonly string _dataDirPath;
    readonly string _dataFileName;

    // Encryption parameters
    // TODO: Do not hardcode the key in production code
    readonly bool _useEncryption = false;
    static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("GsYSSZ7Z5Ka4B70QsP7DqtykZJub/CH7tIIjwHeotlM=");
    const string BACKUP_EXTENTION = ".bak";

    // Constructor sets up directories, filenames, and encryption settings
    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption) {
        _dataDirPath = dataDirPath;
        _dataFileName = dataFileName;
        _useEncryption = useEncryption;

        if (_useEncryption && AES_KEY.Length != 32) {
            throw new ArgumentException("AES_KEY must be 32 bytes for AES-256 encryption.");
        }
    }

    // Loads game data for a given profile; optionally attempts rollback from backup on failure
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

            if (_useEncryption) {
                dataToLoad = Decrypt(dataToLoad);
            }

            // Deserialize
            loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            // Verify checksum
            if (!ValidateChecksum(dataToLoad, loadedData.Checksum)) {
                throw new Exception("Checksum validation failed. Save file may be corrupted or tampered with.");
            }
        } catch (Exception e) {
            if (allowRestoreFromBackup) {
                Debug.LogWarning("Failed to load data file. Attempting rollback.\n" + e);
                bool rollbackSuccess = AttemptRollback(fullPath);
                if (rollbackSuccess) {
                    return Load(profileId, false);
                }
            } else {
                Debug.LogError("Could not load or restore data: " + fullPath + "\n" + e);
            }
        }

        return loadedData;
    }

    // Saves the provided GameData object to disk, creates a backup on success
    public void Save(GameData data, string profileId) {
        if (data == null || string.IsNullOrEmpty(profileId)) return;

        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string backupFilePath = fullPath + BACKUP_EXTENTION;

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            // Compute checksum
            string jsonData = JsonUtility.ToJson(data, true);
            string checksum = ComputeChecksum(jsonData);
            data.Checksum = checksum;
            // Re-serialize with checksum
            string dataToStore = JsonUtility.ToJson(data, true);

            if (_useEncryption) {
                dataToStore = Encrypt(dataToStore);
            }

            // Write data to file
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) {
                writer.Write(dataToStore);
            }

            // Verify
            GameData verifiedData = Load(profileId);
            if (verifiedData != null) {
                File.Copy(fullPath, backupFilePath, true);
            } else {
                throw new Exception("Verification failed, backup not created.");
            }
        } catch (Exception e) {
            Debug.LogError("Error saving data: " + fullPath + "\n" + e);
        }
    }

    // Loads all profiles found in the data directory and returns them as a dictionary
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
            if (profileData != null) {
                profileDictionary.Add(profileId, profileData);
            } else {
                Debug.LogError("Failed to load profile data: " + profileId);
            }
        }

        return profileDictionary;
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

    // Duplicates a profile's data file into a new profile
    public void DuplicateFile(string profileId) {
        if (string.IsNullOrEmpty(profileId)) return;

        string sourcePath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        if (!File.Exists(sourcePath)) return;

        string nextProfileId = FindNextProfileID();
        string destinationPath = Path.Combine(_dataDirPath, nextProfileId, _dataFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
        File.Copy(sourcePath, destinationPath);
    }

    // Deletes a profile directory and data
    public void DeleteFile(string profileId) {
        if (string.IsNullOrEmpty(profileId)) return;
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string directoryPath = Path.GetDirectoryName(fullPath);
        if (Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, true);
        }
    }

    // Opens the specified profile's save file location in the system file explorer
    public void OpenFileInExplorer(string profileId) {
        if (string.IsNullOrEmpty(profileId)) return;
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName).Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + fullPath);
    }

    // Finds the next available numeric profile ID by checking existing profiles
    public string FindNextProfileID() {
        var profilesGameData = DataPersistenceManager.Instance.GetAllProfilesGameData();
        int i = 0;
        while (profilesGameData.ContainsKey(i.ToString())) {
            i++;
        }
        return i.ToString();
    }

    // Encrypts the plaintext using AES and returns a Base64 string containing IV + ciphertext
    string Encrypt(string plainText) {
        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV(); // Generates a new IV for each encryption

        using MemoryStream memoryStream = new();
        // Prepend IV to the ciphertext
        memoryStream.Write(aes.IV, 0, aes.IV.Length);

        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using StreamWriter writer = new(cryptoStream);
        writer.Write(plainText);
        writer.Flush();
        cryptoStream.FlushFinalBlock();

        byte[] encryptedBytes = memoryStream.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    // Decrypts the Base64 string containing IV + ciphertext and returns the plaintext
    string Decrypt(string cipherText) {
        byte[] cipherBytesWithIV = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherBytesWithIV, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // Extract ciphertext
        int cipherTextStartIndex = iv.Length;
        int cipherTextLength = cipherBytesWithIV.Length - cipherTextStartIndex;
        byte[] cipherBytes = new byte[cipherTextLength];
        Array.Copy(cipherBytesWithIV, cipherTextStartIndex, cipherBytes, 0, cipherTextLength);

        using MemoryStream memoryStream = new(cipherBytes);
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader reader = new(cryptoStream);
        string plainText = reader.ReadToEnd();
        return plainText;
    }

    // Attempts to roll back to a backup file if loading fails
    bool AttemptRollback(string fullPath) {
        string backupFilePath = fullPath + BACKUP_EXTENTION;
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

    // Compute a checksum of the data to ensure integrity
    string ComputeChecksum(string data) {
        using var md5 = MD5.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] hashBytes = md5.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    // Validate the loaded data against its checksum
    bool ValidateChecksum(string jsonData, string expectedChecksum) {
        if (string.IsNullOrEmpty(expectedChecksum)) return false;
        string computed = ComputeChecksum(jsonData);
        return computed == expectedChecksum;
    }
}
