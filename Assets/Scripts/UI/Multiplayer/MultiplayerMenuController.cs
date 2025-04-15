using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class MultiplayerMenuController : MonoBehaviour {
    [Header("Panels")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _optionsPanel;

    [Header("Buttons")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _returnButton;

    [Header("Input Field")]
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private TMP_Text _statusText;

    private void Start() {
        _hostButton.onClick.AddListener(HostGame);
        _joinButton.onClick.AddListener(JoinGame);
        _returnButton.onClick.AddListener(Return);
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

        _lobbyPanel.SetActive(true);
        _optionsPanel.SetActive(false);
    }

    private void HostGame() {
        _statusText.text = "Starte Host...";
        NetworkManager.Singleton.StartHost();
        _statusText.text = "Host gestartet.";

        _lobbyPanel.SetActive(false);
        _optionsPanel.SetActive(true);
        _optionsPanel.GetComponent<OptionsPanelController>().Init();
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

        _lobbyPanel.SetActive(false);
        _optionsPanel.SetActive(true);
        _optionsPanel.GetComponent<OptionsPanelController>().Init();
    }

    private void Return() {
        gameObject.SetActive(false);
        _lobbyPanel.SetActive(true);
        _optionsPanel.SetActive(false);
    }
}
