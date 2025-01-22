using Unity.Collections;
using UnityEngine;

public class Chest : PlaceableObject {
    [Header("References")]
    SpriteRenderer _visual;

    public float MaxDistanceToPlayer => 1.5f;

    ItemContainerSO _itemContainer;
    ChestUI _chestUI;
    bool _opened;
    int _itemId;


    private void Awake() {
        // Ensure the chest has a valid item container on creation.
        InitializeItemContainer();
    }

    private void Start() {
        _chestUI = ChestUI.Instance;
    }

    // Creates or reinitializes the item container if it does not already exist.
    private void InitializeItemContainer() {
        if (_itemContainer == null) {
            _itemContainer = ScriptableObject.CreateInstance<ItemContainerSO>();
            _itemContainer.Initialize(ChestSO.ItemSlots);
        }
    }

    // Sets up chest data and visual state before the chest is fully loaded into the scene.
    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;
        InitializeItemContainer();
        _visual = GetComponent<SpriteRenderer>();
        _visual.sprite = ChestSO.ClosedSprite;
    }

    // Allows the player to interact with the chest, toggling its open/closed state.
    public void Interact(PlayerController player) {
        ToggleChest();
        UpdateVisual();
        UpdateUI();
    }

    // Changes the chest between opened and closed.
    private void ToggleChest() => _opened = !_opened;

    // Updates the chest's sprite based on whether it is opened or closed.
    private void UpdateVisual() => _visual.sprite = _opened ? ChestSO.OpenSprite : ChestSO.ClosedSprite;


    // Shows or hides the chest UI depending on the chest's current state.
    private void UpdateUI() {
        if (_opened) {
            _chestUI.ShowChestUI(_itemContainer);
        } else {
            _chestUI.HideChestUI();
        }
    }

    // Lets the player pick up any remaining items from inside the chest.
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        foreach (var slot in _itemContainer.ItemSlots) {
            // Tries to add items to the player's inventory.
            int remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(slot, false);

            // If items remain, spawn them in the world around the chest.
            if (remainingAmount > 0) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    slot,
                    transform.position,
                    PlayerController.LocalInstance.PlayerMovementController.LastMotionDirection,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }

    // Retrieves the ChestSO data from the global item database using this chest's item ID.
    private ChestSO ChestSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as ChestSO;

    #region -------------------- Save & Load --------------------
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
    #endregion -------------------- Save & Load --------------------
}
