using Unity.Netcode;
using UnityEngine;


public class Player : NetworkBehaviour {
    public static Player LocalInstance { get; private set; }

    // Cached references to managers to optimize performance
    private GameManager _gameManager;

    public bool InBed { get; private set; }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of Player in the scene!");
                return;
            }
            LocalInstance = this;

            _gameManager = GameManager.Instance;
        }

        PlayerDataManager.Instance.AddPlayer(this);
        GameManager.Instance.AddPlayerToSleepingDict(OwnerClientId);
    }

    public override void OnNetworkDespawn() {
        PlayerDataManager.Instance.RemovePlayer(this);
        GameManager.Instance.RemovePlayerFromSleepingDict(OwnerClientId);
    }

    /// <summary>
    /// Toggles the player's bed state. Sends RPCs only if the state changes.
    /// </summary>
    /// <param name="inBed">True if the player is going to bed, false otherwise.</param>
    public void SetPlayerInBed(bool inBed) {
        if (InBed == inBed) {
            // No state change; no action required.
            return;
        }

        InBed = inBed;
        _gameManager.SetPlayerSleepingStateServerRpc(inBed);
    }
}
