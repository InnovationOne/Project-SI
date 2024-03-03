using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerItemDragAndDropController : NetworkBehaviour {
    public static PlayerItemDragAndDropController LocalInstance { get; private set; }

    private ItemSlot _dragItemSlot;

    // References
    private PlayerMovementController _playerMovementController;
    private PlayerInventoryController _playerInventoryController;
    private ItemSpawnManager _itemSpawnManager;
    private DragItemPanel _dragItemPanel;
    private InputManager _inputManager;


    private void Awake() {
        _dragItemSlot = new ItemSlot();

        // References
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerItemDragAndDropController in the scene!");
                return;
            }
            LocalInstance = this;
        }

        // References
        _itemSpawnManager = ItemSpawnManager.Instance;
        _dragItemPanel = DragItemPanel.Instance;
        _inputManager = InputManager.Instance;
    }

    private void Update() {
        // Check if the current object is the owner and the drag item panel is active
        if (IsOwner && _dragItemPanel.gameObject.activeSelf) {
            // Get the pointer position from the input manager
            Vector3 pointerPosition = _inputManager.GetPointerPosition();

            // Round the pointer position to the nearest integer values
            var roundedPosition = new Vector3(
                Mathf.RoundToInt(pointerPosition.x),
                Mathf.RoundToInt(pointerPosition.y),
                _dragItemPanel.transform.position.z // Keep the same z-coordinate
            );

            // Update the position of the drag item panel
            _dragItemPanel.transform.position = roundedPosition;
        }
    }


    #region Left Click
    public void OnLeftClick(ItemSlot itemSlot) {
        // Place item slot based on pointer position on map or in inventory
        if (!EventSystem.current.IsPointerOverGameObject()) {
            PlaceItemSlotOnMap();
        } else {
            PickUpPlaceOrSwitchItemSlot(itemSlot);
        }

        CheckToClearDragItem();

        _playerInventoryController.InventoryContainer.ItemContainerNeedsToBeUpdated = true;

        UpdateIcon();
    }

    private void PlaceItemSlotOnMap() {
        _itemSpawnManager.SpawnItemFromInventory(_dragItemSlot.Item, _dragItemSlot.Amount, _dragItemSlot.RarityID, transform.position, _playerMovementController.LastMotionDirection);
        _dragItemSlot.Amount = 0;
    }

    private void PickUpPlaceOrSwitchItemSlot(ItemSlot itemSlot) {
        if (_dragItemSlot.Item == null) {
            // Handle picking up an item when drag item is empty.
            if (itemSlot.Item != null) {
                PickUpItemSlot(itemSlot);
            }
        } else if (itemSlot.Item == null) {
            PlaceItemSlotIntoInventory(itemSlot);
        } else if (_dragItemSlot.Item == itemSlot.Item && _dragItemSlot.RarityID.Equals(itemSlot.RarityID)) {
            // Handle switching or stacking items.
            if (itemSlot.Amount == itemSlot.Item.MaxStackableAmount || _dragItemSlot.Amount == _dragItemSlot.Item.MaxStackableAmount) {
                SwitchItemSlots(itemSlot);
            } else {
                // Calculate the available space in the target slot.
                int availableSpace = itemSlot.Item.MaxStackableAmount - itemSlot.Amount;

                // Calculate the amount to move from the drag slot.
                int amountToMove = Math.Min(availableSpace, _dragItemSlot.Amount);

                // Move items from drag slot to target slot.
                itemSlot.Amount += amountToMove;
                _dragItemSlot.Amount -= amountToMove;

            }
        }
        else {
            SwitchItemSlots(itemSlot);
        }
    }

    private void PickUpItemSlot(ItemSlot itemSlot) {
        _dragItemSlot.Copy(itemSlot.Item.ItemID, itemSlot.Amount, itemSlot.RarityID);
        itemSlot.Clear();
    }

    private void PlaceItemSlotIntoInventory(ItemSlot itemSlot) {
        itemSlot.Copy(_dragItemSlot.Item.ItemID, _dragItemSlot.Amount, _dragItemSlot.RarityID);
        _dragItemSlot.Amount = 0;
    }

    private void SwitchItemSlots(ItemSlot itemSlot) {
        // Save the item and count of the item slot in temporary variables
        ItemSO transferItem = itemSlot.Item;
        int transferCount = itemSlot.Amount;
        int transferRarityID = itemSlot.RarityID;

        // Copy the contents of the drag item slot to the item slot
        itemSlot.Copy(_dragItemSlot.Item.ItemID, _dragItemSlot.Amount, _dragItemSlot.RarityID);

        // Set the drag item slot to the saved item and count from the item slot
        _dragItemSlot.Set(transferItem.ItemID, transferCount, transferRarityID);
    }

    private void UpdateIcon() {
        if (_dragItemSlot.Item != null) {
            _dragItemPanel.gameObject.SetActive(true);

            var toolRarity = _dragItemSlot.Item.ItemType == ItemTypes.Tools ? _dragItemSlot.Item.ToolItemRarity[_dragItemSlot.RarityID - 1] : null;

            _dragItemPanel.SetItemSlot(_dragItemSlot, toolRarity);
        } else {
            _dragItemPanel.gameObject.SetActive(false);
        }

    }
    #endregion


    #region Right Click
    public void OnRightClick(ItemSlot itemSlot) {
        // Place item based on pointer position on map or in inventory
        if (!EventSystem.current.IsPointerOverGameObject()) {
            PlaceItemOnMap();
        } else {
            PlaceItemInInventory(itemSlot);
        }

        CheckToClearDragItem();

        _playerInventoryController.InventoryContainer.ItemContainerNeedsToBeUpdated = true;

        UpdateIcon();
    }

    private void PlaceItemOnMap() {
        int spawnAmount = _inputManager.GetShiftPressed() ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT) : 1;
        _itemSpawnManager.SpawnItemFromInventory(_dragItemSlot.Item, spawnAmount, _dragItemSlot.RarityID, transform.position, _playerMovementController.LastMotionDirection);
        _dragItemSlot.Amount -= spawnAmount;
    }

    private void PlaceItemInInventory(ItemSlot itemSlot) {
        // If: Item slot is empty.
        // Else: Item slot is not empty, but the itemId or rarityId doesn't match the drag item's itemId or the item slot is full.
        if (itemSlot.Item == null) {
            itemSlot.Item = _dragItemSlot.Item;
            itemSlot.RarityID = _dragItemSlot.RarityID;
        } else if (itemSlot.Item.ItemID != _dragItemSlot.Item.ItemID ||
                  itemSlot.RarityID != _dragItemSlot.RarityID ||
                  itemSlot.Amount >= itemSlot.Item.MaxStackableAmount) {
            return;
        }

        int transferAmount = _inputManager.GetShiftPressed() ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT) : 1;
        itemSlot.Amount += transferAmount;
        _dragItemSlot.Amount -= transferAmount;
    }
    #endregion


    public int TryToAddItemToDragItem(ItemSlot itemSlot) {
        if (!CanAddToDragItem(itemSlot)) {
            // Calculate the amount of items that can be added to the drag item slot.
            int missingAmount = _dragItemSlot.Item.MaxStackableAmount - _dragItemSlot.Amount;

            if (missingAmount >= itemSlot.Amount) {
                // The missing amount is greater than item slot amount, add the item slot amount to the drag item slot. Set the item slot amount to 0.
                _dragItemSlot.Amount += itemSlot.Amount;
                itemSlot.Amount = 0;
            } else {
                // Add the missing amount to the drag item slot. Remove the missing amount from the item slot.
                _dragItemSlot.Amount = _dragItemSlot.Item.MaxStackableAmount;
                itemSlot.Amount -= missingAmount;
            }
        } 

        // Return the amount of items that weren't added to the drag item slot.
        return itemSlot.Amount;
    }

    // Checks if an item can be added to the drag item slot.
    private bool CanAddToDragItem(ItemSlot itemSlot) {
        return itemSlot.Item.ItemID != _dragItemSlot.Item.ItemID ||
               itemSlot.RarityID != _dragItemSlot.RarityID ||
               itemSlot.Amount >= itemSlot.Item.MaxStackableAmount;
    }

    public void AddDragItemBackIntoBackpack(int lastSlotId) {
        _playerInventoryController.InventoryContainer.ItemSlots[lastSlotId].Set(_dragItemSlot.Item.ItemID, _dragItemSlot.Amount, _dragItemSlot.RarityID);
        ClearDragItem();
    }

    private void CheckToClearDragItem() {
        if (_dragItemSlot.Amount <= 0) {
            ClearDragItem();
        }
    }

    public void ClearDragItem() {
        _dragItemSlot.Clear();
        _dragItemPanel.gameObject.SetActive(false);
    }
}
