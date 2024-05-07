using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Unity.Netcode;


//Manager for saving and loading game data
public class DataPersistenceManager : NetworkBehaviour {
    public static DataPersistenceManager Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] private bool _initializeDataIfNull = false;

    [Header("File Storage Configuration")]
    [SerializeField] private string _fileName;
    [SerializeField] private bool _useEncryption;

    private GameData _gameData;
    private List<IDataPersistance> _dataPersistancesObjects;
    private FileDataHandler _dataHandler;

    // Id of the selected save
    private string _selectedProfileId = "";


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Data Persistence Manager in the scene.");
        } else {
            Instance = this;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);

        InitializeSelectedProfileId();

        _dataPersistancesObjects = FindAllDataPersistanceObjects();
        //LoadGame();
    }

    public override void OnNetworkSpawn() {
        _dataPersistancesObjects = FindAllDataPersistanceObjects();
        LoadGame();
    }

    public override void OnNetworkDespawn() {
        SaveGame();
    }

    /*
    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // When the scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        _dataPersistancesObjects = FindAllDataPersistanceObjects();
        LoadGame();
    }*/

    public void ChangeSelectedProfile(string newProfileId) {
        //Update the profile to use for saving and loading
        _selectedProfileId = newProfileId;

        //Load the game, which use that profile, updating our game data accordingly
        LoadGame();
    }

    // Starts a new game
    public void NewGame(GameData newGameData) {
        if (newGameData == null)
            _gameData = new GameData();
        else
            _gameData = newGameData;


        _selectedProfileId = _dataHandler.FindNextProfileID();
    }

    // Load data from gameDataObject
    private void LoadGame() {
        //Load and save data from a file using data handler
        _gameData = _dataHandler.Load(_selectedProfileId);

        //Start a new game if the data is null and we're configured to initialize data for debugging purposes
        if (_gameData == null && _initializeDataIfNull)
            NewGame(null);

        if (_gameData == null) {
            Debug.Log("No data was found. A new Game needs to be started befor data can be loaded.");
            return;
        }

        //Push loaded data to all scripts that need it
        foreach (IDataPersistance dataPersistanceObj in _dataPersistancesObjects)
            dataPersistanceObj.LoadData(_gameData);
    }

    // Save data to gameDataObject
    public void SaveGame() {
        //When there is no data to save
        if (_gameData == null) {
            Debug.LogWarning("No data was found. A new Game needs to be started befor we can save data.");
            return;
        }

        //Pass data to other scripts to be updated
        foreach (IDataPersistance dataPersistanceObj in _dataPersistancesObjects)
            dataPersistanceObj.SaveData(_gameData);

        //Timestamp the data so we know when it was last saved
        _gameData.LastPlayed = DateTime.Now.ToBinary();

        //Save data to the file
        _dataHandler.Save(_gameData, _selectedProfileId);
    }

    // Find all scripts that save or load
    private List<IDataPersistance> FindAllDataPersistanceObjects() {
        IEnumerable<IDataPersistance> dataPersistanceObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID).OfType<IDataPersistance>();

        return new List<IDataPersistance>(dataPersistanceObjects);
    }

    // For testing
    private void OnApplicationQuit() {
        SaveGame();
    }

    // Checks if there is data to load
    public bool HasGameData() {
        return _gameData != null;
    }

    // Returns a dictionary with all saved profiles
    public Dictionary<string, GameData> GetAllProfilesGameData() {
        return _dataHandler.LoadAllProfiles();
    }

    // Duplicates a file
    public void DuplicateFile(string profileId) {
        _dataHandler.DuplicateFile(profileId);
    }

    // Deletes a file
    public void DeleteFile(string profileId) {
        _dataHandler.DeleteFile(profileId);

        InitializeSelectedProfileId();

        LoadGame();
    }

    // Sets the most recently selected profle
    private void InitializeSelectedProfileId() {
        _selectedProfileId = _dataHandler.GetMostRecentlyPlayedProfileId();
    }

    // Opens the save folder for the profileId
    public void OpenFileInExplorer(string profileId) {
        _dataHandler.OpenFileInExplorer(profileId);
    }
}
