using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerWikiController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerWikiController LocalInstance { get; private set; }


    [SerializeField] private WikiContainerSO _wikiContainerSO;

    private void Start() {
        WikiPanel.Instance.WikiContainer = _wikiContainerSO;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerWikiController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    public void AddItemToWikiContainer(ItemSO itemSO) {
        _wikiContainerSO.AddItem(itemSO);
        _wikiContainerSO.SortItems();
    }

    public void ShowItemInWiki(int itemID) {
        if (InventoryMasterVisual.Instance.LastOpenPanel != InventorySubPanels.Wiki) {
            InventoryMasterVisual.Instance.SetSubPanel(InventorySubPanels.Wiki);
        }
        
        WikiPanel.Instance.ShowItemInWiki(itemID);
    }

    #region Save & Load
    [Serializable]
    public class SaveItemData {
        public int itemId;

        public SaveItemData(int itemId) {
            this.itemId = itemId;
        }
    }

    [Serializable]
    public class ToSave {
        public List<SaveItemData> itemData;

        public ToSave() {
            itemData = new List<SaveItemData>();
        }
    }

    public void SavePlayer(PlayerData playerData) {
        // Save the found item container to a string
        ToSave toSaveData = new();

        foreach (var item in _wikiContainerSO.Items) {
            toSaveData.itemData.Add(new SaveItemData(item.ItemID));
        }

        playerData.Wiki = JsonUtility.ToJson(toSaveData);
    }

    public void LoadPlayer(PlayerData playerData) {
        if (!string.IsNullOrEmpty(playerData.Wiki)) {
            // Clear the current items in the container
            _wikiContainerSO.Items.Clear();
            var toLoadFoundItem = JsonUtility.FromJson<ToSave>(playerData.Wiki);

            // Add the items from the ToSave object to the container
            foreach (var itemData in toLoadFoundItem.itemData) {
                _wikiContainerSO.Items.Add(ItemManager.Instance.ItemDatabase.Items[itemData.itemId]);
            }
        }
    }
    #endregion
}
