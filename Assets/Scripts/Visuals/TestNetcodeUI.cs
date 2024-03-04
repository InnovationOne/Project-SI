using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestNetcodeUI : MonoBehaviour {
    public static TestNetcodeUI Instance { get; private set; }

    [SerializeField] private Button _createGameButton;
    [SerializeField] private Button _joinGameButton;

    private void Awake() {
        Instance = this;

        _createGameButton.onClick.AddListener(() => {
            Debug.Log("Starting host");
            NetworkManager.Singleton.StartHost();
            gameObject.SetActive(false);
        });
        _joinGameButton.onClick.AddListener(() => {
            Debug.Log("Starting client");
            NetworkManager.Singleton.StartClient();
            gameObject.SetActive(false);
        });
    }
}
