using System.Collections.Generic;

public class PlayerDataContainer {
    private List<PlayerData> _playerDatas;

    public PlayerDataContainer() {
        _playerDatas = new List<PlayerData>();
    }

    public void AddNewPlayer(PlayerData playerData) {
        _playerDatas.Add(playerData);
    }

    public void RemovePlayerPermanently(PlayerData playerData) {
        _playerDatas.Remove(playerData);
    }
}
