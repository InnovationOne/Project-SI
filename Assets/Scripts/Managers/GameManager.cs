using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour, IDataPersistance {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool _singlePlayer = true;

    [Header("Prefabs")]
    [SerializeField] private Transform _playerPrefab;

    // Dictionary to track player sleeping states; only accessed on the server
    private Dictionary<ulong, bool> _playerSleepingDict;
    public List<PlayerController> PlayerControllers { get; private set; } = new();

    // Cached references
    [HideInInspector] public CropsManager CropsManager;
    [HideInInspector] public DialogueManager DialogueManager;
    [HideInInspector] public FinanceManager FinanceManager;
    [HideInInspector] public InputManager InputManager;
    [HideInInspector] public ItemManager ItemManager;
    [HideInInspector] public ItemSpawnManager ItemSpawnManager;
    [HideInInspector] public LoadSceneManager LoadSceneManager;
    [HideInInspector] public QuestManager QuestManager;
    [HideInInspector] public TimeManager TimeManager;
    [HideInInspector] public RecipeManager RecipeManager;
    [HideInInspector] public PauseGameManager PauseGameManager;
    [HideInInspector] public PlaceableObjectsManager PlaceableObjectsManager;
    [HideInInspector] public EventsManager EventsManager;
    [HideInInspector] public AudioManager AudioManager;
    [HideInInspector] public FMODEvents FMODEvents;
    [HideInInspector] public WeatherManager WeatherManager;
    [HideInInspector] public CutsceneManager CutsceneManager;

    private NetworkManager _networkManager;
    private TestNetcodeUI _testNetcodeUI;

    /// <summary>
    /// Initializes the singleton instance and the player sleeping dictionary.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of GameManager in the scene!");
            return;
        }
        Instance = this;

        // Initialize the network-synchronized dictionary
        _playerSleepingDict = new Dictionary<ulong, bool>();

        CropsManager = GetComponent<CropsManager>();
        DialogueManager = GetComponent<DialogueManager>();
        FinanceManager = GetComponent<FinanceManager>();
        InputManager = GetComponent<InputManager>();
        ItemManager = GetComponent<ItemManager>();
        ItemSpawnManager = GetComponent<ItemSpawnManager>();
        LoadSceneManager = GetComponent<LoadSceneManager>();
        QuestManager = GetComponent<QuestManager>();
        TimeManager = GetComponent<TimeManager>();
        RecipeManager = GetComponent<RecipeManager>();
        PauseGameManager = GetComponent<PauseGameManager>();
        PlaceableObjectsManager = GetComponent<PlaceableObjectsManager>();
        EventsManager = GetComponent<EventsManager>();
        AudioManager = AudioManager.Instance;
        FMODEvents = FMODEvents.Instance;
        WeatherManager = GetComponent<WeatherManager>();
        CutsceneManager = GetComponent<CutsceneManager>();
    }

    private void Start() {
        _networkManager = NetworkManager.Singleton;
        _testNetcodeUI = TestNetcodeUI.Instance;

        if (_singlePlayer) {
            _networkManager.StartHost();
            if (_testNetcodeUI != null) _testNetcodeUI.gameObject.SetActive(false);
            else Debug.LogWarning("TestNetcodeUI instance is not assigned.");
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            _networkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    public override void OnNetworkDespawn() {
        if (IsServer) {
            _networkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        foreach (ulong clientId in _networkManager.ConnectedClientsIds) {
            if (!_playerSleepingDict.ContainsKey(clientId)) {
                SpawnPlayerForClient(clientId);
            }
        }
    }

    private void SpawnPlayerForClient(ulong clientId) {
        if (_playerPrefab == null) {
            Debug.LogError("Player prefab is not assigned in the GameManager.");
            return;
        }

        Transform playerTransform = Instantiate(_playerPrefab);

        if (!playerTransform.TryGetComponent<NetworkObject>(out var networkObject)) {
            Debug.LogError("The player prefab does not contain a NetworkObject component.");
            Destroy(playerTransform.gameObject);
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId, true);
    }


    #region Sleep Management

    /// <summary>
    /// Adds a player to the sleeping dictionary, initializing their state to awake.
    /// </summary>
    /// <param name="clientId">The client ID of the player.</param>
    public void AddPlayerToSleepingDict(ulong clientId) {
        if (!_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict[clientId] = false;
            //Debug.Log($"Added player {clientId} to sleeping dictionary.");
        }
    }


    public void RemovePlayerFromSleepingDict(ulong clientId) {
        if (_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict.Remove(clientId);
            //Debug.Log($"Removed player {clientId} from sleeping dictionary.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerSleepingStateServerRpc(bool isSleeping, ServerRpcParams serverRpcParams = default) {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        if (_playerSleepingDict.ContainsKey(clientId)) {
            if (_playerSleepingDict[clientId] == isSleeping) {
                // No state change; no action required.
                return;
            }

            _playerSleepingDict[clientId] = isSleeping;
            Debug.Log($"Player {clientId} is {(isSleeping ? "sleeping" : "awake")}.");

            if (isSleeping) {
                CheckIfAllPlayersAreSleeping();
            }
        } else {
            Debug.LogWarning($"Attempted to set sleeping state for unknown player {clientId}.");
        }
    }

    private void CheckIfAllPlayersAreSleeping() {
        foreach (var playerState in _playerSleepingDict.Values) {
            if (!playerState) {
                Debug.Log("Not all players are sleeping.");
                return;
            }
        }

        Debug.Log("All players are sleeping. Transitioning to the next day.");

        // Reset all players' sleeping states
        List<ulong> clientIds = new(_playerSleepingDict.Keys);
        foreach (ulong clientId in clientIds) {
            _playerSleepingDict[clientId] = false;
        }

        // Notify all clients to wake up
        SetAllPlayersAwakeClientRpc();

        TimeManager.StartNextDay();
        Debug.Log("Next day started.");
    }

    [ClientRpc]
    private void SetAllPlayersAwakeClientRpc() {
        if (PlayerController.LocalInstance != null) {
            PlayerController.LocalInstance.TogglePlayerInBed();
        } else {
            Debug.LogWarning("LocalInstance of Player is not set.");
        }
    }

    public void AddPlayer(PlayerController playerController) {
        PlayerControllers.Add(playerController);
    }

    public void RemovePlayer(PlayerController playerController) {
        PlayerControllers.Remove(playerController);
    }
    #endregion


    #region Save & Load
    [Serializable]
    public class PlayerDataList {
        public List<PlayerData> _players = new();
    }

    public void SaveData(GameData data) {
        var allPlayerData = new List<PlayerData>();
        foreach (var pc in PlayerControllers) {
            var playerData = new PlayerData(pc.OwnerClientId);
            var persisters = pc.GetComponents<IPlayerDataPersistance>();
            foreach (var persister in persisters) {
                persister.SavePlayer(playerData);
            }

            allPlayerData.Add(playerData);
        }

        var playerDataList = new PlayerDataList {
            _players = allPlayerData
        };

        string json = JsonUtility.ToJson(playerDataList, true);
        data.PlayerData = json;
    }

    public void LoadData(GameData data) {
        if (!IsServer || string.IsNullOrEmpty(data.PlayerData)) return;
        var playerDataList = JsonUtility.FromJson<PlayerDataList>(data.PlayerData);

        foreach (var pd in playerDataList._players) {
            var pc = PlayerControllers.FirstOrDefault(p => p.OwnerClientId == pd.UniqueId);
            if (pc == null) {
                Debug.LogWarning($"No matching PlayerController found for OwnerClientId {pd.UniqueId}.");
                continue;
            }

            var persisters = pc.GetComponents<IPlayerDataPersistance>();
            foreach (var persister in persisters) {
                persister.LoadPlayer(pd);
            }
        }
    }
    #endregion
}
