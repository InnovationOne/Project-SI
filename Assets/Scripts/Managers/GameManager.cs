using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CropsManager))]
[RequireComponent(typeof(DialogueManager))]
[RequireComponent(typeof(FinanceManager))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(ItemManager))]
[RequireComponent(typeof(ItemSpawnManager))]
[RequireComponent(typeof(SI_LoadSceneManager))]
[RequireComponent(typeof(QuestManager))]
[RequireComponent(typeof(TimeManager))]
[RequireComponent(typeof(RecipeManager))]
[RequireComponent(typeof(PauseGameManager))]
[RequireComponent(typeof(PlaceableObjectsManager))]
[RequireComponent(typeof(EventsManager))]
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(FMODEvents))]
[RequireComponent(typeof(WeatherManager))]
public class GameManager : NetworkBehaviour, IDataPersistance {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool _singlePlayer;

    [Header("Prefabs")]
    [SerializeField] private Transform _playerPrefab;

    // Dictionary to track player sleeping states; only accessed on the server
    private Dictionary<ulong, bool> _playerSleepingDict;
    public List<PlayerController> PlayerControllers { get; private set; } = new();

    // Cached references
    public CropsManager CropsManager;
    public DialogueManager DialogueManager;
    public FinanceManager FinanceManager;
    public InputManager InputManager;
    public ItemManager ItemManager;
    public ItemSpawnManager ItemSpawnManager;
    public SI_LoadSceneManager LoadSceneManager;
    public QuestManager QuestManager;
    public TimeManager TimeManager;
    public RecipeManager RecipeManager;
    public PauseGameManager PauseGameManager;
    public PlaceableObjectsManager PlaceableObjectsManager;
    public EventsManager EventsManager;
    public AudioManager AudioManager;
    public FMODEvents FMODEvents;
    public WeatherManager WeatherManager;


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
        LoadSceneManager = GetComponent<SI_LoadSceneManager>();
        QuestManager = GetComponent<QuestManager>();
        TimeManager = GetComponent<TimeManager>();
        RecipeManager = GetComponent<RecipeManager>();
        PauseGameManager = GetComponent<PauseGameManager>();
        PlaceableObjectsManager = GetComponent<PlaceableObjectsManager>();
        EventsManager = GetComponent<EventsManager>();
        AudioManager = GetComponent<AudioManager>();
        FMODEvents = GetComponent<FMODEvents>();
        WeatherManager = GetComponent<WeatherManager>();
    }

    private void Start() {
        // Cache commonly accessed singletons
        _networkManager = NetworkManager.Singleton;
        _testNetcodeUI = TestNetcodeUI.Instance;

        if (_singlePlayer) {
            _networkManager.StartHost();
            if (_testNetcodeUI != null) {
                _testNetcodeUI.gameObject.SetActive(false);
            } else {
                Debug.LogWarning("TestNetcodeUI instance is not assigned.");
            }
        }
    }

    /// <summary>
    /// Subscribes to network events upon network spawn.
    /// </summary>
    public override void OnNetworkSpawn() {
        if (IsServer) {
            _networkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    /// <summary>
    /// Unsubscribes from network events upon network despawn.
    /// </summary>
    public override void OnNetworkDespawn() {
        if (IsServer) {
            _networkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    /// <summary>
    /// Handles player spawning once the scene has fully loaded.
    /// </summary>
    /// <param name="sceneName">Name of the loaded scene.</param>
    /// <param name="loadSceneMode">Mode in which the scene was loaded.</param>
    /// <param name="clientsCompleted">List of clients that have completed loading.</param>
    /// <param name="clientsTimedOut">List of clients that timed out during loading.</param>
    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        foreach (ulong clientId in _networkManager.ConnectedClientsIds) {
            if (!_playerSleepingDict.ContainsKey(clientId)) {
                SpawnPlayerForClient(clientId);
            }
        }
    }

    /// <summary>
    /// Spawns a player object for the specified client.
    /// </summary>
    /// <param name="clientId">The client ID for which to spawn the player.</param>
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

    /// <summary>
    /// Removes a player from the sleeping dictionary.
    /// </summary>
    /// <param name="clientId">The client ID of the player.</param>
    public void RemovePlayerFromSleepingDict(ulong clientId) {
        if (_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict.Remove(clientId);
            //Debug.Log($"Removed player {clientId} from sleeping dictionary.");
        }
    }

    /// <summary>
    /// Sets the sleeping state of a player. Invokes RPCs to update the state across the network.
    /// </summary>
    /// <param name="isSleeping">True if the player is going to sleep; false otherwise.</param>
    /// <param name="serverRpcParams">Parameters for the Server RPC.</param>
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

    /// <summary>
    /// Checks if all players are currently sleeping. If so, initiates the transition to the next day.
    /// </summary>
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


    /// <summary>
    /// RPC to set all players to awake on their respective clients.
    /// </summary>
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
            var dataPersistanceObjects = FindAllDataPersistanceObjects(pc);
            var playerData = new PlayerData();

            foreach (var persistence in dataPersistanceObjects) {
                persistence.SavePlayer(playerData);
            }

            playerData.OwnerClientId = pc.OwnerClientId;
            allPlayerData.Add(playerData);
        }

        var playerDataList = new PlayerDataList {
            _players = allPlayerData
        };

        string json = JsonUtility.ToJson(playerDataList, true);
        data.PlayerData = json;
    }

    public void LoadData(GameData data) {
        // TODO: Implement loading player data maybe with UI to select player
    }

    private List<IPlayerDataPersistance> FindAllDataPersistanceObjects(PlayerController pc) {
        // Annahme: PlayerController ist ein MonoBehaviour und hat ein GameObject
        IEnumerable<IPlayerDataPersistance> dataPersistanceObjects = pc.GetComponents<MonoBehaviour>().OfType<IPlayerDataPersistance>();
        return new List<IPlayerDataPersistance>(dataPersistanceObjects);
    }
    #endregion
}
