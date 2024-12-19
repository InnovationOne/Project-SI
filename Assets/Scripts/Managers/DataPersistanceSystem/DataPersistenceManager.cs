using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System;

// Manages data persistence, integrating with multiplayer (Netcode) and ensuring data is synced across connected players.
public class DataPersistenceManager : NetworkBehaviour {
    public static DataPersistenceManager Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] bool _initializeDataIfNull = false;

    [Header("File Storage Configuration")]
    [SerializeField] string _fileName = "data.emi";
    [SerializeField] bool _useEncryption = false;
    [SerializeField] bool _useCloudSaves = false;

    // Selected profile ID synchronized across the network
    string CurrentGameVersion => Application.version;
    string _selectedProfileId;
    GameData _gameData;
    List<IDataPersistance> _dataPersistenceObjects;
    FileDataHandler _dataHandler;
    ICloudSaveProvider _cloudSaveProvider;

    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogError("Multiple instances of DataPersistenceManager found. Destroying this one.");
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);

        if (_useCloudSaves) {
            _cloudSaveProvider = new SteamCloudSaveProvider();
        } else {
            _cloudSaveProvider = null;
        }

        InitializeSelectedProfileId();
        _dataPersistenceObjects = FindAllDataPersistanceObjects();
    }

    // Ensures data is saved when the application quits (TODO: For testing)
    private void OnApplicationQuit() {
        SaveGame();
    }

    // Finds all objects in the scene that implement IDataPersistance
    private List<IDataPersistance> FindAllDataPersistanceObjects() {
        var objs = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID).OfType<IDataPersistance>();
        return new List<IDataPersistance>(objs);
    }

    // Checks if game data is currently loaded
    public bool HasGameData() => _gameData != null;

    // Retrieves all available profiles' game data
    public Dictionary<string, GameData> GetAllProfilesGameData() => _dataHandler.LoadAllProfiles();

    // Changes the selected profile ID
    public void ChangeSelectedProfile(string newProfileId) {
        _selectedProfileId = newProfileId;
        LoadGame();
    }

    // Starts a new game with optional initial data
    public void NewGame(GameData newGameData = null) {
        _gameData = newGameData ?? new GameData();
        _gameData.GameVersion = CurrentGameVersion;
        _selectedProfileId = _dataHandler.FindNextProfileID();
    }

    // Loads game data from the selected profile
    private void LoadGame() {
        if (string.IsNullOrEmpty(_selectedProfileId)) {
            if (_initializeDataIfNull) {
                NewGame();
            } else {
                Debug.Log("No profile selected or data uninitialized.");
                return;
            }
        }

        // Attempt to load from cloud first if enabled
        if (_useCloudSaves && _cloudSaveProvider.HasCloudSave(_selectedProfileId)) {
            string cloudData = _cloudSaveProvider.Download(_selectedProfileId);
            if (!string.IsNullOrEmpty(cloudData)) {
                _gameData = JsonUtility.FromJson<GameData>(cloudData);
                if (_gameData == null && _initializeDataIfNull) {
                    NewGame();
                }
            }
        } else {
            _gameData = _dataHandler.Load(_selectedProfileId);
            if (_gameData == null && _initializeDataIfNull) {
                NewGame();
            }
        }

        if (_gameData == null) {
            Debug.Log("No existing data found. Start a new game before loading.");
            return;
        }

        // Migrate if versions differ
        MigrateGameData(_gameData);

        foreach (IDataPersistance dataPersistenceObj in _dataPersistenceObjects) {
            dataPersistenceObj.LoadData(_gameData);
        }
    }

    // Saves the current game data to disk
    public void SaveGame() {
        if (_gameData == null) {
            Debug.LogWarning("No data to save. Start a new game first.");
            return;
        }

        foreach (IDataPersistance dataPersistenceObj in _dataPersistenceObjects) {
            dataPersistenceObj.SaveData(_gameData);
        }

        _gameData.LastPlayed = DateTime.Now.ToBinary();
        _gameData.GameVersion = CurrentGameVersion;
        _dataHandler.Save(_gameData, _selectedProfileId);

        if (_useCloudSaves && _cloudSaveProvider != null) {
            string jsonData = JsonUtility.ToJson(_gameData, true);
            _cloudSaveProvider.Upload(_selectedProfileId, jsonData);
        }
    }

    // Duplicates a profile's data file to a new profile ID
    public void DuplicateFile(string profileId) => _dataHandler.DuplicateFile(profileId);

    // Deletes a profile and updates the currently selected profile
    public void DeleteFile(string profileId) {
        _dataHandler.DeleteFile(profileId);
        InitializeSelectedProfileId();
        LoadGame();
    }

    // Initializes the selected profile ID from the most recently played profile
    private void InitializeSelectedProfileId() {
        string recentId = _dataHandler.GetMostRecentlyPlayedProfileId();
        _selectedProfileId = recentId ?? _dataHandler.FindNextProfileID();
    }

    // Opens a given profile's save file in the system file explorer
    public void OpenFileInExplorer(string profileId) => _dataHandler.OpenFileInExplorer(profileId);

    private void MigrateGameData(GameData data) {
        // If the loaded data's version is different from the current Application.version,
        // run migration steps. Here we just log a message and assume compatibility.
        // Actual migration logic depends on how versions differ.
        if (data.GameVersion != CurrentGameVersion) {
            Debug.Log($"Migrating save data from version {data.GameVersion} to {CurrentGameVersion}.");
            // Implement migration logic as needed here.
            data.GameVersion = CurrentGameVersion;
        }
    }
}
