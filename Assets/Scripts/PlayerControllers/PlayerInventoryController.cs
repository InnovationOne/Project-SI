using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

/// Manages the player's inventory.
[RequireComponent(typeof(NetworkObject))]
public class PlayerInventoryController : NetworkBehaviour, IPlayerDataPersistance {
    // Predefined inventory sizes (read-only)
    static readonly ReadOnlyCollection<int> INVENTORY_SIZES = Array.AsReadOnly(new int[] { 10, 20, 30 });
    public ReadOnlyCollection<int> InventorySizes => INVENTORY_SIZES;

    // Current inventory size (default: largest)
    int _currentInventorySize = INVENTORY_SIZES[^1];
    public int CurrentInventorySize => _currentInventorySize;

    // Inventory container (ScriptableObject reference)
    [SerializeField] ItemContainerSO _inventoryContainer;
    public ItemContainerSO InventoryContainer => _inventoryContainer;

    // Cached UI reference for faster access
    private InventoryUI _inventoryUI;


    private void Start() {
        _inventoryUI = InventoryUI.Instance;
        SetInventorySize(_currentInventorySize);
    }

    // Updates inventory size if valid and refreshes UI
    public void SetInventorySize(int inventorySize) {
        if (inventorySize > INVENTORY_SIZES[^1]) {
            Debug.LogError($"Inventory size can't exceed {INVENTORY_SIZES[^1]}.");
            return;
        }

        _currentInventorySize = inventorySize;
        _inventoryUI.InventoryOrToolbeltSizeChanged();
    }

    // Saves inventory contents and size
    public void SavePlayer(PlayerData playerData) {
        playerData.Inventory = _inventoryContainer.SaveItemContainer();
        playerData.InventorySize = _currentInventorySize;
    }

    // Loads inventory contents and size
    public void LoadPlayer(PlayerData playerData) {
        _currentInventorySize = playerData.InventorySize;
        _inventoryContainer.LoadItemContainer(playerData.Inventory);
    }
}
