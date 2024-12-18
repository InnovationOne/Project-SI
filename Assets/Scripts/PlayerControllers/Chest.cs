using System;
using Unity.Collections;
using UnityEngine;

public class Chest : PlaceableObject {
    [Header("References")]
    private SpriteRenderer _visual;
    private static readonly ChestUI _chestUI = ChestUI.Instance;

    [NonSerialized] private const float MAX_DISTANCE_TO_PLAYER = 1.5f;
    public float MaxDistanceToPlayer { get => MAX_DISTANCE_TO_PLAYER; }

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
        _visual = GetComponent<SpriteRenderer>();
        _visual.sprite =ChestSO.ClosedSprite;
    }

    /// <summary>
    /// Interacts with the chest.
    /// </summary>
    /// <param name="player">The player interacting with the chest.</param>
    public void Interact(PlayerController player) {
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
    private void UpdateVisual() => _visual.sprite = _opened ? ChestSO.OpenSprite : ChestSO.ClosedSprite;
    

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
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        foreach (ItemSlot itemSlot in _itemContainer.ItemSlots) {
            int remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(itemSlot, false);
            if (remainingAmount > 0) {
                ItemSpawnManager.Instance.SpawnItemServerRpc(
                    itemSlot: itemSlot,
                    initialPosition: transform.position,
                    motionDirection: PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    /// <summary>
    /// Represents a chest scriptable object.
    /// </summary>
    private ChestSO ChestSO => ItemManager.Instance.ItemDatabase[_itemId] as ChestSO;


    #region Save & Load
    public override string SaveObject() {
        return _itemContainer.SaveItemContainer();
    }

    public override void LoadObject(FixedString4096Bytes data) {
        string jsonData = data.ToString();

        if (!string.IsNullOrEmpty(jsonData)) {
            InitializeItemContainer();
            _itemContainer.LoadItemContainer(jsonData);
        }
    }
    #endregion
}
