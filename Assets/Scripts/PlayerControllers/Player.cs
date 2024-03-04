using System;
using Unity.Netcode;
using UnityEngine;


public class Player : NetworkBehaviour, IPlayerDataPersistance {
    public static Player LocalInstance { get; private set; }

    public bool InBed { get; private set; }


    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of Player in the scene!");
                return;
            }
            LocalInstance = this;
        }

        PlayerDataManager.Instance.AddPlayer(this);
        GameManager.Instance.AddPlayerToSleepingDict(OwnerClientId);
    }

    public override void OnNetworkDespawn() {
        PlayerDataManager.Instance.RemovePlayer(this);
        GameManager.Instance.RemovePlayerFromSleepingDict(OwnerClientId);
    }

    // Toggle player in bed
    public void SetPlayerInBed(bool inBed) {
        SetInBed(inBed);

        if (InBed) {
            GameManager.Instance.PlayerIsSleepingServerRpc();
        } else {
            GameManager.Instance.PlayerIsAwakeServerRpc();
        }
    }

    public void SetInBed(bool inBed) {
        InBed = inBed;
    }


    #region Save and Load
    public void SavePlayer(PlayerData playerData) {
    }

    public void LoadPlayer(PlayerData playerData) {
    }
    #endregion
}
