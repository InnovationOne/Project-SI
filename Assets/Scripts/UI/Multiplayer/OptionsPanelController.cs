using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelController : MonoBehaviour {
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _newGamePanel;
    [SerializeField] private GameObject _loadGamePanel;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _loadGameButton;
    [SerializeField] private Button _leaveLobbyButton;

    private void Start() {
        _newGameButton.onClick.AddListener(NewGame);
        _loadGameButton.onClick.AddListener(LoadGame);
        _leaveLobbyButton.onClick.AddListener(LeaveLobby);
    }

    public void Init() {
        _newGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        _loadGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);

    }

    public void NewGame() {
        _newGamePanel.SetActive(true);
        ShowNewGamePanelClientRpc();
    }

    [ClientRpc]
    private void ShowNewGamePanelClientRpc() {
        if (!NetworkManager.Singleton.IsHost) {
            _newGamePanel.SetActive(true); 
        }
    }

    public void LoadGame() {
        _loadGamePanel.SetActive(true);
        // Weitere Logik für Spiel laden
    }

    public void LeaveLobby() {
        NetworkManager.Singleton.Shutdown();
        gameObject.SetActive(false);
        _lobbyPanel.SetActive(true);
    }
}
