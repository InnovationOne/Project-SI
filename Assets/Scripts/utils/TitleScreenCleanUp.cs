using Unity.Netcode;
using UnityEngine;

// Cleans up any existing NetworkManager instance when returning to the title screen.
public class TitleScreenCleanUp : MonoBehaviour {
    private void Awake() {
        if (NetworkManager.Singleton != null) {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}
