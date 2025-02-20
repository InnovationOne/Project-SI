using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventoryController : MonoBehaviour, IPlayerDataPersistance {
    // Predefined inventory sizes.
    public ReadOnlyCollection<int> InventorySizes { get; private set; } = Array.AsReadOnly(new int[] { 10, 20, 30 });
    public int CurrentInventorySize { get; private set; }

    public ItemContainerSO InventoryContainer;
    InventoryUI _inventoryUI;

    void Start() {
        _inventoryUI = UIManager.Instance.InventoryUI;
        SetInventorySize(InventorySizes[^1]);
    }

    // Sets and updates inventory size if valid.
    public void SetInventorySize(int inventorySize) {
        if (inventorySize > InventorySizes[^1]) return;
        CurrentInventorySize = inventorySize;
        _inventoryUI.InventorySizeChanged();
    }

    // Saves inventory data to player persistence.
    public void SavePlayer(PlayerData playerData) {
        playerData.Inventory = InventoryContainer.SaveItemContainer();
        playerData.InventorySize = CurrentInventorySize;
    }

    // Loads inventory data from player persistence.
    public void LoadPlayer(PlayerData playerData) {
        CurrentInventorySize = playerData.InventorySize;
        InventoryContainer.LoadItemContainer(playerData.Inventory);
    }
}
