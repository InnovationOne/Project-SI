using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventoryController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerInventoryController LocalInstance { get; private set; }

    public int[] InventorySizes { get { return new int[] { 10, 20, 30 }; } }
    public int CurrentInventorySize = 30;


    public ItemContainerSO InventoryContainer;


    private void Start() {
        SetInventorySize(CurrentInventorySize);
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerInventoryController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    public void SetInventorySize(int inventorySize) {
        if (inventorySize > InventorySizes[^1]) {
            Debug.LogError("Can't set inventorySize higher than MAX_INVENTORY_SIZE");
            return;
        }

        CurrentInventorySize = inventorySize;
        InventoryPanel.Instance.InventoryOrToolbeltSizeChanged();
    }


    #region Save and Load
    [Serializable]
    public class ItemSlotData {
        public int ItemID;
        public int Amount;
        public int RarityID;

        public ItemSlotData(int ItemID, int Amount, int RarityID) {
            this.ItemID = ItemID;
            this.Amount = Amount;
            this.RarityID = RarityID;
        }
    }

    [Serializable]
    public class ItemSlotsData {
        public List<ItemSlotData> ItemSlotDataList;

        public ItemSlotsData() {
            ItemSlotDataList = new List<ItemSlotData>();
        }
    }

    public void SavePlayer(PlayerData playerData) {
        // Save the inventory container to a string
        ItemSlotsData toSaveData = new();

        foreach (var item in InventoryContainer.ItemSlots) {
            // Check if the slot has an item
            if (item.ItemId == -1) {
                toSaveData.ItemSlotDataList.Add(new ItemSlotData(
                    -1,
                    -1,
                    -1));
            } else {
                toSaveData.ItemSlotDataList.Add(new ItemSlotData(
                    item.ItemId,
                    item.Amount,
                    item.RarityId));
            }
        }

        playerData.Inventory = JsonUtility.ToJson(toSaveData);
        playerData.InventorySize = CurrentInventorySize;
    }

    public void LoadPlayer(PlayerData playerData) {
        return;

        // Load the inventory container if it exists
        if (!string.IsNullOrEmpty(playerData.Inventory)) {
            ItemSlotsData toLoadInventory = JsonUtility.FromJson<ItemSlotsData>(playerData.Inventory);

            for (int i = 0; i < toLoadInventory.ItemSlotDataList.Count; i++) {
                if (toLoadInventory.ItemSlotDataList[i].ItemID == -1) {
                    InventoryContainer.ItemSlots[i].Clear();
                } else {
                    InventoryContainer.ItemSlots[i].ItemId = toLoadInventory.ItemSlotDataList[i].ItemID;
                    InventoryContainer.ItemSlots[i].Amount = toLoadInventory.ItemSlotDataList[i].Amount;
                    InventoryContainer.ItemSlots[i].RarityId = toLoadInventory.ItemSlotDataList[i].RarityID;
                }
            }
        }

        CurrentInventorySize = playerData.InventorySize;
    }
    #endregion
}
