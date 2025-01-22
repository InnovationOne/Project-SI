using System;
using UnityEngine;
using UnityEngine.UI;

// This class is for the backpack panel
public class InventoryUI : ItemContainerUI {
    public static InventoryUI Instance { get; private set; }

    public int LastSlotId { get; private set; }

    [Header("Buttons")]
    [SerializeField] Button _sortButton;
    [SerializeField] Button _trashButton;

    PlayerInventoryController _playerInventoryController;
    PlayerToolbeltController _playerToolbeltController;
    PlayerItemDragAndDropController _playerItemDragAndDropController;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of InventoryPanel in the scene!");
            return;
        }
        Instance = this;

    }

    void Start() {
        ItemContainerUIAwake();
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();

        _playerInventoryController = PlayerController.LocalInstance.PlayerInventoryController;
        _playerToolbeltController = PlayerController.LocalInstance.PlayerToolbeltController;
        _playerItemDragAndDropController = PlayerController.LocalInstance.PlayerItemDragAndDropController;

        _sortButton.onClick.AddListener(() => _playerInventoryController.InventoryContainer.SortItems());
        _trashButton.onClick.AddListener(() => _playerItemDragAndDropController.ClearDragItem());
    }

    // Updates inventory and toolbelt sizes based on the player's current configuration.
    public void InventoryOrToolbeltSizeChanged() {
        int toolbeltSize = _playerToolbeltController.CurrentToolbeltSize;
        int maxToolbeltSize = _playerToolbeltController.ToolbeltSizes[^1];
        int inventorySize = _playerInventoryController.CurrentInventorySize;

        for (int i = 0; i < ItemButtons.Length; i++) {
            // Toolbelt slots
            if (i < maxToolbeltSize) {
                ItemButtons[i].SetInteractable(i < toolbeltSize);
                continue;
            }

            // Inventory slots
            ItemButtons[i].SetInteractable(i < (maxToolbeltSize + inventorySize));
        }
    }


    public override void OnPlayerLeftClick(int buttonIndex) {
        if (GameManager.Instance.InputManager.IsShiftPressed()) {
            int remainingAmount;

            // Decides if we should add items directly to the inventory or toolbelt region.
            bool isToolbeltSlot = buttonIndex < _playerToolbeltController.ToolbeltSizes[^1];
            remainingAmount = _playerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], isToolbeltSlot);

            var slot = ItemContainer.ItemSlots[buttonIndex];
            if (remainingAmount > 0) {
                slot.Set(new ItemSlot(slot.ItemId, remainingAmount, slot.RarityId));
            } else {
                slot.Clear();
            }
        } else {
            LastSlotId = buttonIndex;
            _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }

    public override void OnPlayerRightClick(int buttonIndex) {
        LastSlotId = buttonIndex;

        // If an item is on the cursor, do a drag-and-drop right-click action
        if (DragItemUI.Instance.gameObject.activeSelf) {
            _playerItemDragAndDropController.OnRightClick(ItemContainer.ItemSlots[buttonIndex]);
        }
    }
}
