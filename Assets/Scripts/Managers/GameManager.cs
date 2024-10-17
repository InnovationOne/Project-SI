using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game-wide functionalities, including player states and scene management.
/// Implements a singleton pattern to ensure only one instance exists.
/// </summary>
public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool _singlePlayer;

    [Header("Prefabs")]
    [SerializeField] private Transform _playerPrefab;

    // Dictionary to track player sleeping states; only accessed on the server
    private Dictionary<ulong, bool> _playerSleepingDict;

    // Cached references for performance optimization
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
            Debug.Log($"Added player {clientId} to sleeping dictionary.");
        }
    }

    /// <summary>
    /// Removes a player from the sleeping dictionary.
    /// </summary>
    /// <param name="clientId">The client ID of the player.</param>
    public void RemovePlayerFromSleepingDict(ulong clientId) {
        if (_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict.Remove(clientId);
            Debug.Log($"Removed player {clientId} from sleeping dictionary.");
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
        List<ulong> clientIds = new List<ulong>(_playerSleepingDict.Keys);
        foreach (ulong clientId in clientIds) {
            _playerSleepingDict[clientId] = false;
        }

        // Notify all clients to wake up
        SetAllPlayersAwakeClientRpc();

        TimeManager.Instance.StartNextDay();
        Debug.Log("Next day started.");
    }


    /// <summary>
    /// RPC to set all players to awake on their respective clients.
    /// </summary>
    [ClientRpc]
    private void SetAllPlayersAwakeClientRpc() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.SetPlayerInBed(false);
        } else {
            Debug.LogWarning("LocalInstance of Player is not set.");
        }
    }

    #endregion
}

