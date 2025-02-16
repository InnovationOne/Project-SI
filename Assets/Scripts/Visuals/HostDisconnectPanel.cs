using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostDisconnectPanel : MonoBehaviour
{
    public static HostDisconnectPanel Instance { get; private set; }

    [SerializeField] private Button _mainMenuButton;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one local instance of HostDisconnectPanel in the scene!");
            return;
        }
        Instance = this;
    }

    private void Start() {
        _mainMenuButton.onClick.AddListener(() => {
            Debug.Log("Main Menu Button Pressed");
            //NetworkManager.Singleton.Shutdown();
            //LoadSceneManager.LoadScene(LoadSceneManager.Scene.TitleScreenScene);
            });

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;

        gameObject.SetActive(false);
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        if (clientId == NetworkManager.ServerClientId) {
            // Server is shutting down
            Show();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }
}
