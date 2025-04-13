using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelController : NetworkBehaviour {
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private GameObject _newGamePanel;
    [SerializeField] private GameObject _loadGamePanel;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _loadGameButton;
    [SerializeField] private Button _leaveLobbyButton;

    private void Start() {
        if (NetworkManager.Singleton.IsHost) _optionsPanel.SetActive(true);
        else _optionsPanel.SetActive(false);

        _newGameButton.onClick.AddListener(NewGame);
        _loadGameButton.onClick.AddListener(LoadGame);
        _leaveLobbyButton.onClick.AddListener(LeaveLobby);
    }

    public void NewGame() {
        _optionsPanel.SetActive(false);
        _newGamePanel.SetActive(true);
        ShowNewGamePanelClientRpc();
    }

    [ClientRpc]
    private void ShowNewGamePanelClientRpc() {
        if (!IsHost) _newGamePanel.SetActive(true);
    }

    public void LoadGame() {
        _optionsPanel.SetActive(false);
        _loadGamePanel.SetActive(true);
        // Weitere Logik für Spiel laden
    }

    public void LeaveLobby() {
        NetworkManager.Singleton.Shutdown();
        _optionsPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
    }
}
