using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour {
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject lobbyPlayerEntryPrefab;

    private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();

    private void Start() {
        if (LobbyManager.Instance != null) {
            LobbyManager.Instance.GetLobbyPlayers().OnListChanged += OnLobbyPlayersChanged;
            RefreshPlayerList();
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent) {
        RefreshPlayerList();
    }

    private void RefreshPlayerList() {
        // Entferne alte UI-Einträge
        foreach (var entry in playerEntries.Values) {
            Destroy(entry);
        }
        playerEntries.Clear();

        // Erstelle UI-Einträge für jeden Spieler in der Lobby
        foreach (var playerData in LobbyManager.Instance.GetLobbyPlayers()) {
            var entry = Instantiate(lobbyPlayerEntryPrefab, playerListContainer);
            var nameText = entry.transform.Find("PlayerNameText").GetComponent<TMP_Text>();
            var readyToggle = entry.transform.Find("ReadyToggle").GetComponent<Toggle>();

            nameText.text = playerData.PlayerName.ToString();
            readyToggle.isOn = playerData.IsReady;

            // Nur der eigene Client kann den Toggle steuern
            if (playerData.ClientId == NetworkManager.Singleton.LocalClientId) {
                readyToggle.interactable = true;
                readyToggle.onValueChanged.RemoveAllListeners();
                readyToggle.onValueChanged.AddListener(isReady => {
                    // Client meldet via ServerRpc seine Ready-Änderung
                    LobbyManager.Instance.UpdateReadyStatusServerRpc(isReady);
                });
            } else {
                readyToggle.interactable = false;
            }

            playerEntries[playerData.ClientId] = entry;
        }
    }
}
