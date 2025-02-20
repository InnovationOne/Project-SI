using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostDisconnectedUI : MonoBehaviour {
    [SerializeField] Button _mainMenuBtn;

    void Start() {
        _mainMenuBtn.onClick.AddListener(() => {
            Debug.Log("Returning to main menu.");
            // Example for Netcode session shutdown:
            // NetworkManager.Singleton.Shutdown();
            // LoadSceneManager.LoadScene(LoadSceneManager.Scene.TitleScreenScene);
        });

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        gameObject.SetActive(false);
    }

    void OnDestroy() {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId) {
        if (clientId == NetworkManager.ServerClientId) Show();
    }

    private void Show() {
        gameObject.SetActive(true);
    }
}
