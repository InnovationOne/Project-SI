using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerDataManager : NetworkBehaviour, IDataPersistance {
    public static PlayerDataManager Instance { get; private set; }

    public List<Player> CurrentlyConnectedPlayers { get; private set; }

    private PlayerDataContainer _playerDataContainer;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlayerDataManager in the scene!");
            return;
        }
        Instance = this;

        CurrentlyConnectedPlayers = new List<Player>();
        _playerDataContainer = new PlayerDataContainer();

        DontDestroyOnLoad(this);
    }

    public void AddPlayer(Player player) {
        Debug.Log("Add player to connected players list");
        CurrentlyConnectedPlayers.Add(player);
    }

    public void LoadPlayerData(int playerID) {
        Debug.Log("Load Player Data");
    }

    public void RemovePlayer(Player player) {
        Debug.Log("Remove player from connected players list");
        CurrentlyConnectedPlayers.Remove(player);
    }

    #region Save & Load
    public void SaveData(GameData data) {
    }

    public void LoadData(GameData data) {
    }
    #endregion
}