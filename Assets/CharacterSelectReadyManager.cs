using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectReadyManager : NetworkBehaviour {
    public static CharacterSelectReadyManager Instance { get; private set; }

    private Dictionary<ulong, bool> playerReadyDict;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one local instance of Player in the scene!");
            return;
        }
        Instance = this;

        playerReadyDict = new Dictionary<ulong, bool>();
    }

    public void SetPlayerReady() {
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default) {
        playerReadyDict[serverRpcParams.Receive.SenderClientId] = true;

        bool allPlayersReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (!playerReadyDict.ContainsKey(clientId) || !playerReadyDict[clientId]) {
                // This player is not ready
                allPlayersReady = false;
                break;
            }
        }

        if (allPlayersReady) {
            LoadSceneManager.LoadNetwork(LoadSceneManager.Scene.GameScene);
        }
    }
}
