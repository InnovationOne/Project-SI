using Unity.Netcode;
using UnityEngine;

public class TitleScreenCleanUp : MonoBehaviour {
    private void Awake() {
        if (NetworkManager.Singleton != null) {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}
