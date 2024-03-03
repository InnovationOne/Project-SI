using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour {
    [SerializeField] private Button _createGameButton;
    [SerializeField] private Button _joinGameButton;

    private void Awake() {
        _createGameButton.onClick.AddListener(() => {
            Debug.Log("Starting host");
            NetworkManager.Singleton.StartHost();
            LoadSceneManager.LoadNetwork(LoadSceneManager.Scene.CharacterSelectScene);
        });
        _joinGameButton.onClick.AddListener(() => {
            Debug.Log("Starting client");
            NetworkManager.Singleton.StartClient();
        });
    }
}
