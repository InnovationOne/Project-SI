using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class MultiplayerMenuController : MonoBehaviour {
    [Header("UI-Elemente")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private TMP_Text _statusText;

    private void Start() {
        _hostButton.onClick.AddListener(HostGame);
        _joinButton.onClick.AddListener(JoinGame);
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;
    }

    private void HostGame() {
        _statusText.text = "Starte Host...";
        NetworkManager.Singleton.StartHost();
        _statusText.text = "Host gestartet.";
    }

    private void JoinGame() {
        string ipAddress = _ipInputField.text;
        if (string.IsNullOrEmpty(ipAddress)) {
            _statusText.text = "Bitte IP-Adresse eingeben.";
            return;
        }

        _statusText.text = $"Verbinde zu {ipAddress}...";
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = ipAddress;
        NetworkManager.Singleton.StartClient();
        _statusText.text = "Verbindung hergestellt.";
    }
}
