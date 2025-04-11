using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages game data persistence with support for multiplayer (Netcode for GameObjects), cloud saves, 
/// and modding-friendly architecture. Also responsible for in-game save/quit functionality.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DataPersistenceManager : NetworkBehaviour {
    public static DataPersistenceManager Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] bool _initializeDataIfNull = false;

    [Header("File Storage Configuration")]
    [SerializeField] string _fileName = "data.emi";
    [SerializeField] bool _useEncryption = false;
    [SerializeField] bool _useCloudSaves = false;

    string CurrentGameVersion => Application.version;
    string _selectedProfileId;
    GameData _gameData;
    private DateTime _sessionStartTime;
    List<IDataPersistance> _dataPersistenceObjects;
    FileDataHandler _dataHandler;
    ICloudSaveProvider _cloudSaveProvider; // TODO Implement cloud save provider (e.g., SteamCloudSaveProvider)

    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogError("Multiple instances of DataPersistenceManager found. Destroying this one.");
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);

        if (_useCloudSaves) _cloudSaveProvider = new SteamCloudSaveProvider();
        else _cloudSaveProvider = null;

        InitializeSelectedProfileId();
        _dataPersistenceObjects = FindAllDataPersistanceObjects();
    }

    void OnApplicationQuit() {
        // Ensure data is saved when quitting the application. (TODO: Remove this in production)
        SaveGame();
    }

    #region Network Methods

    public override void OnNetworkSpawn() {
        // Ensure that the game loads data when the GameScene is active. (TODO: Remove this in production)
        if (SceneManager.GetActiveScene().name == "GameScene") {
            StartCoroutine(LoadGameNextFrame());
        } else {
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    IEnumerator LoadGameNextFrame() {
        yield return null;
        LoadGame();
    }

    void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsThatFinished, List<ulong> clientsThatTimedOut) {
        if (sceneName == "GameScene") LoadGame();
    }

    public override void OnNetworkDespawn() {
        if (IsServer) NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        base.OnNetworkDespawn();
    }

    #endregion

    #region Game Data Management

    /// <summary>
    /// Finds all objects in the scene implementing IDataPersistance for data syncing.
    /// </summary>
    List<IDataPersistance> FindAllDataPersistanceObjects() {
        var objs = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID).OfType<IDataPersistance>();
        return new List<IDataPersistance>(objs);
    }

    /// <summary>
    /// Checks whether there is game data loaded.
    /// </summary>
    public bool HasGameData() => _gameData != null;

    /// <summary>
    /// Retrieves all profiles' game data.
    /// </summary>
    public Dictionary<string, GameData> GetAllProfilesGameData() => _dataHandler.LoadAllProfiles();

    /// <summary>
    /// Changes the active profile and loads the associated game data.
    /// </summary>
    public void ChangeSelectedProfile(string newProfileId) {
        _selectedProfileId = newProfileId;
        LoadGame();
    }

    /// <summary>
    /// Starts a new game with optional initial game data.
    /// </summary>
    public void NewGame(GameData newGameData = null) {
        _gameData = newGameData ?? new GameData();
        _gameData.GameVersion = CurrentGameVersion;
        _selectedProfileId = _dataHandler.FindNextProfileID();
    }

    /// <summary>
    /// Loads game data from storage (cloud or local) and applies it to all data persistence objects.
    /// </summary>
    public void LoadGame() {
        if (string.IsNullOrEmpty(_selectedProfileId)) {
            if (_initializeDataIfNull) NewGame();
            else {
                Debug.Log("No profile selected or data uninitialized.");
                return;
            }
        }

        // Try cloud saves first if enabled
        if (_useCloudSaves && _cloudSaveProvider != null && _cloudSaveProvider.HasCloudSave(_selectedProfileId)) {
            string cloudData = _cloudSaveProvider.Download(_selectedProfileId);
            if (!string.IsNullOrEmpty(cloudData)) {
                _gameData = JsonUtility.FromJson<GameData>(cloudData);
                if (_gameData == null && _initializeDataIfNull) NewGame();
            }
        } else {
            _gameData = _dataHandler.Load(_selectedProfileId);
            if (_gameData == null && _initializeDataIfNull) NewGame();
        }

        if (_gameData == null) {
            Debug.Log("No existing data found. Start a new game before loading.");
            return;
        }

        // Migrate saved data if the game version has changed.
        MigrateGameData(_gameData);

        // Let all interested objects load their data.
        foreach (var dataPersistenceObj in _dataPersistenceObjects) dataPersistenceObj.LoadData(_gameData);
        _sessionStartTime = DateTime.Now;
    }

    /// <summary>
    /// Saves the current game state to storage (both locally and in the cloud if enabled).
    /// </summary>
    public void SaveGame() {
        if (_gameData == null) {
            Debug.LogWarning("No data to save. Start a new game first.");
            return;
        }

        // Allow all data persistence objects to update the game data.
        foreach (IDataPersistance dataPersistenceObj in _dataPersistenceObjects) {
            dataPersistenceObj.SaveData(_gameData);
        }

        TimeSpan sessionDuration = DateTime.Now - _sessionStartTime;
        _gameData.PlayTime = _gameData.PlayTime.Add(sessionDuration);
        _sessionStartTime = DateTime.Now;

        _gameData.LastPlayed = DateTime.Now.ToBinary();
        _gameData.GameVersion = CurrentGameVersion;
        _dataHandler.Save(_gameData, _selectedProfileId);

        if (_useCloudSaves && _cloudSaveProvider != null) {
            string jsonData = JsonUtility.ToJson(_gameData, true);
            _cloudSaveProvider.Upload(_selectedProfileId, jsonData);
        }
    }

    /// <summary>
    /// Duplicates a profile's save file to a new profile.
    /// </summary>
    public void DuplicateFile(string profileId) => _dataHandler.DuplicateFile(profileId);

    /// <summary>
    /// Deletes a profile's save file and reloads the current game data.
    /// </summary>
    public void DeleteFile(string profileId) {
        _dataHandler.DeleteFile(profileId);
        InitializeSelectedProfileId();
        LoadGame();
    }

    /// <summary>
    /// Updates the selected profile ID using the most recently played profile.
    /// </summary>
    void InitializeSelectedProfileId() {
        string recentId = _dataHandler.GetMostRecentlyPlayedProfileId();
        _selectedProfileId = recentId ?? _dataHandler.FindNextProfileID();
    }

    /// <summary>
    /// Migrates game data if the save version does not match the current application version.
    /// </summary>
    void MigrateGameData(GameData data) {
        if (data.GameVersion != CurrentGameVersion) {
            Debug.Log($"Migrating save data from version {data.GameVersion} to {CurrentGameVersion}.");
            // Insert migration logic here if needed.
            data.GameVersion = CurrentGameVersion;
        }
    }

    /// <summary>
    /// Provides a method to quit the application from in-game or the main menu.
    /// </summary>
    public void QuitGame() => Application.Quit();
    

    #endregion
}
