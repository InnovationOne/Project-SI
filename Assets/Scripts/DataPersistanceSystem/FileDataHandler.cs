using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class FileDataHandler
{
    private string _dataDirPath = ""; //Path to save data
    private string _dataFileName = ""; //File to save to
    private bool _useEncryption = false;
    private readonly string _encryptionCodeWord = "UHeotgrzkDwfxuNvJtbrURNGLMyhRF";
    private readonly string _backupExtention = ".bak";

    public FileDataHandler (string dataDirPath, string dataFileName, bool useEncryption)
    {
        _dataDirPath = dataDirPath;
        _dataFileName = dataFileName;
        _useEncryption = useEncryption;
    }

    // Loads a Game Data object
    public GameData Load(string profileId, bool allowRestoreFromBackup = true)
    {
        //If profileId is null
        if (profileId == null)
            return null;

        //Combines the pathes due to divernt OS's having different path seperators
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        GameData loadedData = null;
        if (File.Exists(fullPath))
        {
            try
            {
                //Load the serialized data from the file
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                    using (StreamReader reader = new StreamReader(stream))
                        dataToLoad = reader.ReadToEnd();

                //Optional decrypt the data
                if (_useEncryption)
                    dataToLoad = EncryptDecrypt(dataToLoad);

                //Deserialize data from JSON back into the C# object
                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e)
            {
                //Since we're calling Load(..) recursively, we need to account for the case where the rollback succeeds,
                //but data is still failinf to load for some other reason, which without this check may cause an infinit recursion loop
                if (allowRestoreFromBackup)
                {
                    Debug.LogWarning("Failed to load data file. Atte,pting to roll back.\n" + e);
                    bool rollbackSuccess = AttemptRollback(fullPath);
                    if (rollbackSuccess)
                        loadedData = Load(profileId, false);
                }
                //If we hit this else block, one possibility is that the backup file is also currupt
                else
                    Debug.LogError("Error occured when trying to load file at path: " + fullPath + " and backup did not work.\n" + e);
            }
        }
        return loadedData;
    }

    // Saves a Game Data object
    public void Save(GameData data, string profileId)
    {
        //If profileId is null
        if (profileId == null)
            return;

        //Combines the pathes due to divernt OS's having different path seperators
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string backupFilePath = fullPath + _backupExtention;
        try
        {
            //Create the directory the file will be written to if it doesn't already exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            //Serialize the C# game data object into JSON
            string dataToStore = JsonUtility.ToJson(data, true);

            //Optional encryption the data
            if (_useEncryption)
                dataToStore = EncryptDecrypt(dataToStore);

            //Write the seriaized data to the file
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                using (StreamWriter writer = new StreamWriter(stream))
                    writer.Write(dataToStore);

            //Verify the newly saved file can be loaded successfully
            GameData verifiedGameDate = Load(profileId);
            //If the data can be verified, back it up
            if (verifiedGameDate != null)
                File.Copy(fullPath, backupFilePath, true);
            //Otherwise something went wrong
            else
                throw new Exception("Save file could not be verified and backup could not be created.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error occured when trying to save data to file: " + fullPath + "\n" + e);
        }
    }

    // Maps all savefiles to a string id
    public Dictionary<string, GameData> LoadAllProfiles()
    {
        Dictionary<string, GameData> profileDictionary = new Dictionary<string, GameData>();

        //Loop over all directory names in the data directory path
        IEnumerable<DirectoryInfo> dirInfos = new DirectoryInfo(_dataDirPath).EnumerateDirectories();
        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            string profileId = dirInfo.Name;

            //Check if the data file exists, if not then this folder isn't a profile and should be skipped
            string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Skipping directory when loading all profiles because it doesn't contain data: " 
                    + profileId);
                continue;
            }

            //Load the game data for this profile and put it in the dictionary
            GameData profileData = Load(profileId);

            //Check if data isn't null, because if it is then something went wrong
            if (profileData != null)
                profileDictionary.Add(profileId, profileData);
            else
                Debug.LogError("Tried to load profile but somethink went wrong. ProfileId: " + profileId);
        }

        return profileDictionary;
    }

    // Returns the most recently played profile
    public string GetMostRecentlyPlayedProfileId()
    {
        string mostRecentProfileId = null;

        Dictionary<string, GameData> profilesGameData = LoadAllProfiles();
        foreach (KeyValuePair<string, GameData> pair in profilesGameData)
        {
            string profileId = pair.Key;
            GameData gameData = pair.Value;

            //Skip entry when if game data is null
            if (gameData == null)
                continue;

            //If this is the first data, it's the most recent so far
            if (mostRecentProfileId == null)
                mostRecentProfileId = profileId;
            //Otherwise, compare to see which date is the most recent
            else
            {
                DateTime mostRecentDateTime = DateTime.FromBinary(profilesGameData[mostRecentProfileId].LastPlayed);
                DateTime newDateTime = DateTime.FromBinary(gameData.LastPlayed);

                //The greater DateTime value is the most recent
                if (newDateTime > mostRecentDateTime)
                    mostRecentProfileId = profileId;
            }
        }

        return mostRecentProfileId;
    }

    // Duplicates a file
    public void DuplicateFile(string profileId)
    {
        string sourcePath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        string destinationPath = Path.Combine(_dataDirPath, FindNextProfileID(), _dataFileName);

        //Create the new directory
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

        //Copy file to new path
        File.Copy(sourcePath, destinationPath);        
    }

    // Deletes a file
    public void DeleteFile(string profileId)
    {
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        Directory.Delete(Path.GetDirectoryName(fullPath), true);
    }

    // Opens the save folder for the profileId
    public void OpenFileInExplorer(string profileId)
    {
        string fullPath = Path.Combine(_dataDirPath, profileId, _dataFileName);
        fullPath = fullPath.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select," + fullPath);
    }

    // Finds the next unused profile id in the saves folder
    public string FindNextProfileID()
    {
        GameData gameData = null;
        Dictionary<string, GameData> profilesGameData = DataPersistenceManager.Instance.GetAllProfilesGameData();

        int i = 0;
        bool notFound = false;
        while (!notFound)
        {
            if (profilesGameData.TryGetValue(i.ToString(), out gameData))
                i++;
            else
                notFound = true;
        }

        return i.ToString();
    }

    // XOR Encrypts / Decrypts data
    private string EncryptDecrypt(string data)
    {
        string modifiedData = "";
        for (int i = 0; i < data.Length; i++)
            modifiedData += (char)(data[i] ^ _encryptionCodeWord[i % _encryptionCodeWord.Length]);
        return modifiedData;
    }

    // Try loading the backup data
    private bool AttemptRollback(string fullPath)
    {
        bool success = false;
        string backupFilePath = fullPath + _backupExtention;
        try
        {
            //If the file exists, attempt to roll back to it by overwriting the original file
            if (File.Exists(backupFilePath))
            {
                File.Copy(backupFilePath, fullPath, true);
                success = true;
                Debug.LogWarning("Had to roll back to backup file at: " + backupFilePath);
            }
            //Otherwise, we don't have a backup file yet
            else
                throw new Exception("Tried to roll back, but no backup file exists to roll back to.");

        }
        catch (Exception e)
        {
            Debug.LogError("Error occured when tying to roll back to backup file at: " + backupFilePath + "\n" + e);
        }

        return success;
    }
}
