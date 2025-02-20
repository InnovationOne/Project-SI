using UnityEngine;
using UnityEngine.UI;

// Backpack panel UI for displaying and interacting with the player’s inventory.
public class InventoryUI : ItemContainerUI {
    public int LastSlotId { get; private set; } = -1;

    [Header("Buttons")]
    [SerializeField] Button _sortButton;
    [SerializeField] Button _trashButton;
    [SerializeField] Button _upBtn;
    [SerializeField] Button _downBtn;

    ItemSpawnManager _itemSpawnManager;
    PlayerInventoryController _playerInventoryController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerItemDragAndDropController _playerItemDragAndDropController;
    PlayerMovementController _playerMovementController;
    ClothingUI _clothingUI;
    ChestUI _chestUI;
    DragItemUI _dragItemUI;

    void Awake() {
        PlayerController.OnLocalPlayerSpawned += CatchReferences;
    }

    void Start() {
        _itemSpawnManager = GameManager.Instance.ItemSpawnManager;
        _clothingUI = UIManager.Instance.ClothingUI;
        _chestUI = UIManager.Instance.ChestUI;
        _dragItemUI = UIManager.Instance.DragItemUI;
        ItemContainerUIAwake();
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
    }

    private void OnDestroy() {
        if (_sortButton != null) _sortButton.onClick.RemoveAllListeners();
        if (_trashButton != null) _trashButton.onClick.RemoveAllListeners();
        ItemContainer.OnItemsUpdated -= ShowUIButtonContains;
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
    }

    // Grabs references to key controller components.
    void CatchReferences(PlayerController playerController) {
        _playerInventoryController = playerController.PlayerInventoryController;
        _playerToolbeltController = playerController.PlayerToolbeltController;
        _playerItemDragAndDropController = playerController.PlayerItemDragAndDropController;
        _playerMovementController = playerController.PlayerMovementController;

        _sortButton.onClick.AddListener(() => _playerInventoryController.InventoryContainer.SortItems(true));
        _trashButton.onClick.AddListener(() => _playerItemDragAndDropController.ClearDragItem());
        _upBtn.onClick.AddListener(() => playerController.PlayerToolbeltController.ToggleToolbelt(false));
        _downBtn.onClick.AddListener(() => playerController.PlayerToolbeltController.ToggleToolbelt(true));
    }

    // Refreshes UI after changes in inventory or toolbelt capacity.
    public void InventorySizeChanged() {
        if (_playerInventoryController == null || _playerToolbeltController == null || _playerItemDragAndDropController == null) CatchReferences(PlayerController.LocalInstance);

        int maxToolbeltSize = _playerToolbeltController.ToolbeltSizes[^1];
        int inventorySize = _playerInventoryController.CurrentInventorySize;

        for (int i = 0; i < ItemButtons.Length; i++) {
            if (i >= maxToolbeltSize) ItemButtons[i].SetInteractable(i < (maxToolbeltSize + inventorySize));
        }
    }

    public void ToolbeltSizeChanged() {
        int toolbeltSize = _playerToolbeltController.CurrentToolbeltSize;
        int maxToolbeltSize = _playerToolbeltController.ToolbeltSizes[^1];

        for (int i = 0; i < maxToolbeltSize; i++) {
            ItemButtons[i].SetInteractable(i < toolbeltSize);
        }
    }

    // Handles left-click events on slots; shift-click merges items, otherwise picks up or drags the item.
    public override void OnPlayerLeftClick(int buttonIndex) {
        var slot = ItemContainer.ItemSlots[buttonIndex];
        int toolbeltSize = _playerToolbeltController.CurrentToolbeltSize;

        // ----- SHIFT-CLICK logic -----
        if (GameManager.Instance.InputManager.GetShiftPressed()) {
            // TOOLBELT -> CLOTHING (if clothing UI is open and the item is clothing)
            if (buttonIndex < toolbeltSize && !slot.IsEmpty) {
                var itemSO = GameManager.Instance.ItemManager.ItemDatabase[slot.ItemId];
                if (_clothingUI != null && _clothingUI.gameObject.activeSelf && itemSO is ClothingSO clothingItem) {
                    TryShiftClothingItemToClothingUI(clothingItem, slot);
                    ShowUIButtonContains();
                    ItemContainer.UpdateUI();
                    _clothingUI.ShowUIButtonContains();
                    _clothingUI.UpdatePlayerVisual();
                    return;
                }
            }

            // TOOLBELT -> INVENTORY (if clicked slot is within toolbelt range)
            if (buttonIndex < toolbeltSize) {
                if (!slot.IsEmpty) {
                    int leftover = _playerInventoryController.InventoryContainer.AddItem(slot, true);
                    if (leftover > 0) slot.Set(new ItemSlot(slot.ItemId, leftover, slot.RarityId));
                    else slot.Clear();
                    GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                    ShowUIButtonContains();
                    ItemContainer.UpdateUI();
                }
                return;
            }

            // INVENTORY -> CLOTHING (if clothing UI is open)
            if (_clothingUI != null && _clothingUI.gameObject.activeSelf) {
                if (!slot.IsEmpty) {
                    var itemSO = GameManager.Instance.ItemManager.ItemDatabase[slot.ItemId];
                    if (itemSO is ClothingSO clothingItem) {
                        TryShiftClothingItemToClothingUI(clothingItem, slot);
                        ShowUIButtonContains();
                        ItemContainer.UpdateUI();
                        _clothingUI.ShowUIButtonContains();
                        _clothingUI.UpdatePlayerVisual();
                        return;
                    }
                }
            }

            // INVENTORY -> CHEST (if chest UI is open)
            if (_chestUI != null && _chestUI.gameObject.activeSelf) {
                if (!slot.IsEmpty) {
                    int leftover = _chestUI.PublicItemContainer.AddItem(slot, true);
                    if (leftover > 0)
                        slot.Set(new ItemSlot(slot.ItemId, leftover, slot.RarityId));
                    else
                        slot.Clear();
                    GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                    ShowUIButtonContains();
                    _chestUI.ShowUIButtonContains();
                    _chestUI.PublicItemContainer.UpdateUI();
                }
                return;
            }

            // Default SHIFT behavior (Inventory -> Toolbelt)
            if (!slot.IsEmpty) {
                int leftover = _playerInventoryController.InventoryContainer.AddItem(slot, false);
                if (leftover > 0)
                    slot.Set(new ItemSlot(slot.ItemId, leftover, slot.RarityId));
                else
                    slot.Clear();
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                ShowUIButtonContains();
            }
            return;
        }
        LastSlotId = buttonIndex;
        _playerItemDragAndDropController.OnLeftClick(slot);
        ShowUIButtonContains();
    }

    // Attempts to place a clothing item from an inventory slot into the correct clothing slot(s).
    void TryShiftClothingItemToClothingUI(ClothingSO clothingItem, ItemSlot inventorySlot) {
        // Copy item data from inventory, then clear that slot.
        int newItemId = inventorySlot.ItemId;
        int newItemAmt = inventorySlot.Amount;
        int newItemRarity = inventorySlot.RarityId;
        inventorySlot.Clear();

        var clothingSlots = _clothingUI.PlayerClothingUIItemContainer.ItemSlots;
        var torsoSlot = clothingSlots[(int)ClothingSO.ClothingType.Torso];
        var legsSlot = clothingSlots[(int)ClothingSO.ClothingType.Legs];

        // Handle one-to-one clothing types (e.g., belt, feet, hands, helmet).
        if (clothingItem.Type == ClothingSO.ClothingType.Belt ||
            clothingItem.Type == ClothingSO.ClothingType.Feet ||
            clothingItem.Type == ClothingSO.ClothingType.Hands ||
            clothingItem.Type == ClothingSO.ClothingType.Helmet) {

            var targetSlot = clothingSlots[(int)clothingItem.Type];
            MoveOccupantToInventory(targetSlot);
            targetSlot.Set(new ItemSlot(newItemId, newItemAmt, newItemRarity));
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
            return;
        }

        // Advanced clothing scenarios: dresses, legs, torso.
        bool torsoOccupied = !torsoSlot.IsEmpty;
        bool legsOccupied = !legsSlot.IsEmpty;

        switch (clothingItem.Type) {
            case ClothingSO.ClothingType.Dress:
                // Move occupant(s) from torso/legs to free up space, then place dress.
                if (torsoOccupied) MoveOccupantToInventory(torsoSlot);
                torsoSlot.Set(new ItemSlot(newItemId, newItemAmt, newItemRarity));
                if (legsOccupied) MoveOccupantToInventory(legsSlot);
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                break;

            case ClothingSO.ClothingType.Legs:
                // If torso has a dress, remove it first; then place pants in leg slot.
                if (IsDress(torsoSlot.ItemId)) MoveOccupantToInventory(torsoSlot);
                MoveOccupantToInventory(legsSlot);
                legsSlot.Set(new ItemSlot(newItemId, newItemAmt, newItemRarity));
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                break;

            case ClothingSO.ClothingType.Torso:
                // If torso has a dress, remove it first; then place top in torso slot.
                if (IsDress(torsoSlot.ItemId)) MoveOccupantToInventory(torsoSlot);
                MoveOccupantToInventory(torsoSlot);
                torsoSlot.Set(new ItemSlot(newItemId, newItemAmt, newItemRarity));
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                break;
        }
    }

    void MoveOccupantToInventory(ItemSlot occupantSlot) {
        if (occupantSlot.IsEmpty) return;

        int occupantId = occupantSlot.ItemId;
        int occupantAmount = occupantSlot.Amount;
        int occupantRarity = occupantSlot.RarityId;

        occupantSlot.Clear();
        int leftover = _playerInventoryController.InventoryContainer.AddItem(new ItemSlot(occupantId, occupantAmount, occupantRarity), false);

        if (leftover > 0) {
            var leftoverSlot = new ItemSlot(occupantId, leftover, occupantRarity);
            _itemSpawnManager.SpawnItemServerRpc(
                leftoverSlot,
                PlayerController.LocalInstance.transform.position,
                _playerMovementController.LastMotionDirection,
                true
            );
        }
    }

    private bool IsDress(int itemId) {
        if (GameManager.Instance.ItemManager.ItemDatabase[itemId] != null) {
            return GameManager.Instance.ItemManager.ItemDatabase[itemId] is ClothingSO clothing && clothing.Type == ClothingSO.ClothingType.Dress;
        }
        return false;
    }

    // Handles right-click actions for drag-and-drop.
    public override void OnPlayerRightClick(int buttonIndex) {
        LastSlotId = buttonIndex;
        if (_dragItemUI.gameObject.activeSelf) _playerItemDragAndDropController.OnRightClick(ItemContainer.ItemSlots[buttonIndex]);
    }
}
