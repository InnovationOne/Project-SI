using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game")]
    [SerializeField] private bool _singlePlayer;

    [SerializeField] private Transform _playerPrefab;

    private Dictionary<ulong, bool> _playerSleepingDict;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of GameManager in the scene!");
            return;
        }
        Instance = this;

        _playerSleepingDict = new Dictionary<ulong, bool>();
    }

    private void Start() {
        if (_singlePlayer) {
            NetworkManager.Singleton.StartHost();
            TestNetcodeUI.Instance.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted; ;
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut) {
        foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds) {
            Transform playerTransform = Instantiate(_playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        }
    }


    #region Sleep
    // Add player to sleeping dict when player is spawned
    public void AddPlayerToSleepingDict(ulong clientId) {
        if (!_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict.Add(clientId, false);
            Debug.Log($"Add player {clientId} to sleeping dict");
        }
    }

    // Remove player from sleeping dict when player is despawned
    public void RemovePlayerFromSleepingDict(ulong clientId) {
        if (!_playerSleepingDict.ContainsKey(clientId)) {
            _playerSleepingDict.Remove(clientId);
            Debug.Log($"Remove player {clientId} from sleeping dict");
        }
    }
    
    // Set the calling player to sleeping
    [ServerRpc(RequireOwnership = false)]
    public void PlayerIsSleepingServerRpc(ServerRpcParams serverRpcParams = default) {
        var clientId = serverRpcParams.Receive.SenderClientId;
        _playerSleepingDict[clientId] = true;
        Debug.Log($"Player {clientId} is sleeping");
        CheckIfAllPlayersSleep();
    }

    // Set the calling player to awake
    [ServerRpc(RequireOwnership = false)]
    public void PlayerIsAwakeServerRpc(ServerRpcParams serverRpcParams = default) {
        var clientId = serverRpcParams.Receive.SenderClientId;
        _playerSleepingDict[clientId] = false;
        Debug.Log($"Player {clientId} is awake");
    }

    // Check if all players are sleeping after a player is set to sleeping
    private void CheckIfAllPlayersSleep() {
        Debug.Log(_playerSleepingDict.Keys.Count);
        foreach (var clientId in _playerSleepingDict.Keys) {
            if (_playerSleepingDict[clientId] == false) {
                Debug.Log("Not all players are sleeping");
                return;
            }
        }
        Debug.Log("All players are sleeping");

        // All players in bed
        var clientIds = new List<ulong>(_playerSleepingDict.Keys);
        foreach (var clientId in clientIds) {
            _playerSleepingDict[clientId] = false;
        }

        SetPlayerAwakeClientRpc();
        //TimeAndWeatherManager.Instance.StartNextDay();
        Debug.Log("Next day started");
    }

    // Set all players to awake when a new day starts
    [ClientRpc]
    private void SetPlayerAwakeClientRpc() {
        Player.LocalInstance.SetInBed(false);
    }
    #endregion
}
