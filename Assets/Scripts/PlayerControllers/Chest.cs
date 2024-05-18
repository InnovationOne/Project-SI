using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Interactable, IObjectDataPersistence {
    [Header("References")]
    private ObjectVisual _visual;
    private static readonly ChestUI _chestUI = ChestUI.Instance;

    [NonSerialized] private const float MAX_DISTANCE_TO_PLAYER = 1.5f;
    public override float MaxDistanceToPlayer { get => MAX_DISTANCE_TO_PLAYER; }

    private ItemContainerSO _itemContainer;
    private bool _opened;
    private int _itemId;

    private void Awake() {
        InitializeItemContainer();
    }

    /// <summary>
    /// Initializes the item container for the chest.
    /// </summary>
    private void InitializeItemContainer() {
        if (_itemContainer == null) {
            _itemContainer = (ItemContainerSO)ScriptableObject.CreateInstance(typeof(ItemContainerSO));
            _itemContainer.Initialize(ChestSO.ItemSlots);
        }
    }

    /// <summary>
    /// Initializes the chest with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the item to initialize the chest with.</param>
    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        InitializeItemContainer();
        _visual = GetComponentInChildren<ObjectVisual>();
        _visual.SetSprite(ChestSO.InactiveSprite);
    }

    /// <summary>
    /// Interacts with the chest.
    /// </summary>
    /// <param name="player">The player interacting with the chest.</param>
    public override void Interact(Player player) {
        ToggleChest();
        UpdateVisual();
        UpdateUI();
    }

    /// <summary>
    /// Toggles the state of the chest (opened or closed).
    /// </summary>
    private void ToggleChest() => _opened = !_opened;

    /// <summary>
    /// Updates the visual appearance of the chest based on its current state.
    /// </summary>
    private void UpdateVisual() => _visual.SetSprite(_opened ? ChestSO.ActiveSprite : ChestSO.InactiveSprite);
    

    /// <summary>
    /// Updates the UI based on the state of the chest.
    /// If the chest is opened, it shows the item container in the chest UI.
    /// If the chest is closed, it hides the chest UI.
    /// </summary>
    private void UpdateUI() {
        if (_opened) {
            _chestUI.ShowChest(_itemContainer);
        } else {
            _chestUI.HideChest();
        }
    }

    /// <summary>
    /// Picks up items in the placed object and adds them to the player's inventory.
    /// </summary>
    /// <param name="player">The player who is picking up the items.</param>
    public override void PickUpItemsInPlacedObject(Player player) {
        foreach (ItemSlot itemSlot in _itemContainer.ItemSlots) {
            int remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(itemSlot, false);
            if (remainingAmount > 0) {
                ItemSpawnManager.Instance.SpawnItemServerRpc(
                    itemSlot: itemSlot,
                    initialPosition: transform.position,
                    motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    /// <summary>
    /// Represents a chest scriptable object.
    /// </summary>
    private ChestSO ChestSO => ItemManager.Instance.ItemDatabase[_itemId] as ChestSO;


    #region Save & Load
    public string SaveObject() {
        return _itemContainer.SaveItemContainer();
    }

    public void LoadObject(string data) {
        if (!string.IsNullOrEmpty(data)) {
            InitializeItemContainer();
            _itemContainer.LoadItemContainer(data);
        }
    }
    #endregion
}
