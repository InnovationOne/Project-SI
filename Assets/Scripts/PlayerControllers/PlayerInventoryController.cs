using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the player's inventory and provides methods for saving and loading player data.
/// </summary>
public class PlayerInventoryController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerInventoryController LocalInstance { get; private set; }

    private readonly int[] _inventorySizes = { 10, 20, 30 };
    public int[] InventorySizes => _inventorySizes;

    private int _currentInventorySize = 30;
    public int CurrentInventorySize => _currentInventorySize;

    [SerializeField] private ItemContainerSO _inventoryContainer;
    public ItemContainerSO InventoryContainer => _inventoryContainer;


    private void Start() {
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
    /// Initializes the item container.
    /// </summary>
    private void InitializeItemContainer() {
        if (_inventoryContainer == null) {
            _inventoryContainer = (ItemContainerSO)ScriptableObject.CreateInstance(typeof(ItemContainerSO));
            _inventoryContainer.Initialize(_inventorySizes[^1]);
        }
    }

    /// <summary>
    /// Sets the inventory size.
    /// </summary>
    /// <param name="inventorySize">The new inventory size.</param>
    public void SetInventorySize(int inventorySize) {
        if (inventorySize > InventorySizes[^1]) {
            Debug.LogError("Can't set inventorySize higher than MAX_INVENTORY_SIZE");
            return;
        }

        _currentInventorySize = inventorySize;
        InventoryUI.Instance.InventoryOrToolbeltSizeChanged();
    }

    #region Save and Load
    public void SavePlayer(PlayerData playerData) {
        playerData.Inventory = _inventoryContainer.SaveItemContainer();
        playerData.InventorySize = _currentInventorySize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _currentInventorySize = playerData.InventorySize;
        InitializeItemContainer();
        if (!string.IsNullOrEmpty(playerData.Inventory)) {
            _inventoryContainer.LoadItemContainer(playerData.Inventory);
        }
    }

    #endregion
}
