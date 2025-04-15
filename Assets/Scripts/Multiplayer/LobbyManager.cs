using UnityEngine;
using Unity.Netcode;
using UnityEngine.Analytics;

public class LobbyManager : NetworkBehaviour {
    public static LobbyManager Instance { get; private set; }
    private NetworkList<LobbyPlayerData> _lobbyPlayers;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _lobbyPlayers = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            // Füge den Host hinzu – sofern noch nicht vorhanden.
            AddPlayer(NetworkManager.Singleton.LocalClientId, "Host");
        }
    }

    public void AddPlayer(ulong clientId, string name) {
        if (!IsServer) return;
        // Verhindere Duplikate
        foreach (var player in _lobbyPlayers) {
            if (player.ClientId == clientId)
                return;
        }
        _lobbyPlayers.Add(new LobbyPlayerData {
            ClientId = clientId,
            PlayerName = name,
            IsReady = false
        });
    }

    public void SetPlayerReady(ulong clientId, bool ready) {
        if (!IsServer) return;

        for (int i = 0; i < _lobbyPlayers.Count; i++) {
            if (_lobbyPlayers[i].ClientId == clientId) {
                var data = _lobbyPlayers[i];
                data.IsReady = ready;
                _lobbyPlayers[i] = data;
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateReadyStatusServerRpc(bool ready, ServerRpcParams rpcParams = default) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        SetPlayerReady(clientId, ready);
    }

    private void OnClientConnected(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;
        AddPlayer(clientId, $"Client {clientId}");
    }

    private void OnClientDisconnected(ulong clientId) {
        for (int i = 0; i < _lobbyPlayers.Count; i++) {
            if (_lobbyPlayers[i].ClientId == clientId) {
                _lobbyPlayers.RemoveAt(i);
                break;
            }
        }
    }

    public NetworkList<LobbyPlayerData> GetLobbyPlayers() => _lobbyPlayers;
}
