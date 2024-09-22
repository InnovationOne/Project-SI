using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the player's inventory and provides methods for saving and loading player data.
/// </summary>
public class PlayerInventoryController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerInventoryController LocalInstance { get; private set; }

    // Immutable collection of possible inventory sizes.
    private static readonly ReadOnlyCollection<int> _inventorySizes = Array.AsReadOnly(new int[] { 10, 20, 30 });
    public ReadOnlyCollection<int> InventorySizes => _inventorySizes;

    // Current inventory size, defaulting to maximum size.
    private int _currentInventorySize = 30;
    public int CurrentInventorySize => _currentInventorySize;

    // Reference to the inventory container ScriptableObject.
    [SerializeField] private ItemContainerSO _inventoryContainer;
    public ItemContainerSO InventoryContainer => _inventoryContainer;

    // Cached reference to InventoryUI to minimize property access overhead.
    private InventoryUI _inventoryUI;


    private void Start() {
        // Cache the InventoryUI reference
        _inventoryUI = InventoryUI.Instance;

        SetInventorySize(_currentInventorySize);
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

    /// <summary>
    /// Sets the inventory size.
    /// </summary>
    /// <param name="inventorySize">The new inventory size.</param>
    public void SetInventorySize(int inventorySize) {
        if (inventorySize > _inventorySizes[^1]) {
            Debug.LogError($"Cannot set inventory size higher than maximum allowed size {_inventorySizes[^1]}.");
            return;
        }

        if (_currentInventorySize == inventorySize) {
            return; // No change needed
        }

        _currentInventorySize = inventorySize;

        if (_inventoryContainer != null) {
            _inventoryUI.InventoryOrToolbeltSizeChanged();
        }

    }

    #region Save and Load
    public void SavePlayer(PlayerData playerData) {
        if (_inventoryContainer != null) {
            playerData.Inventory = _inventoryContainer.SaveItemContainer();
        }
        playerData.InventorySize = _currentInventorySize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _currentInventorySize = playerData.InventorySize;

        if (!string.IsNullOrEmpty(playerData.Inventory)) {
            _inventoryContainer.LoadItemContainer(playerData.Inventory);
        }
    }
    #endregion
}
