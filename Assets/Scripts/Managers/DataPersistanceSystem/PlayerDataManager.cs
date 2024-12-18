using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerDataManager : NetworkBehaviour, IDataPersistance {
    public static PlayerDataManager Instance { get; private set; }

    public List<PlayerController> CurrentlyConnectedPlayers { get; private set; }

    private PlayerDataContainer _playerDataContainer;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlayerDataManager in the scene!");
            Destroy(this);
            return;
        }
        Instance = this;

        CurrentlyConnectedPlayers = new List<PlayerController>();
        _playerDataContainer = new PlayerDataContainer();

        DontDestroyOnLoad(this);
    }

    public void AddPlayer(PlayerController player) {
        //Debug.Log("Add player to connected players list");
        CurrentlyConnectedPlayers.Add(player);
    }

    public void LoadPlayerData(int playerID) {
        Debug.Log("Load Player Data");
    }

    public void RemovePlayer(PlayerController player) {
        //Debug.Log("Remove player from connected players list");
        CurrentlyConnectedPlayers.Remove(player);
    }

    #region Save & Load
    public void SaveData(GameData data) {
    }

    public void LoadData(GameData data) {
    }
    #endregion
}