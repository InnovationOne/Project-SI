using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using static ItemSlot;

public class PlayerItemDragAndDropController : NetworkBehaviour {
    ItemSlot _dragItemSlot;
    PlayerInventoryController _playerInventoryController;
    PlayerMovementController _playerMovementController;
    ItemManager _itemManager;
    ItemSpawnManager _itemSpawnManager;
    InputManager _inputManager;
    DragItemUI _dragItemUI;

    // Reused for position calculations
    Vector3 _roundedPosition;

    void Awake() {
        _dragItemSlot = new ItemSlot();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
    }

    void Start() {
        _itemManager = ItemManager.Instance;
        _itemSpawnManager = ItemSpawnManager.Instance;
        _inputManager = InputManager.Instance;
        _dragItemUI = DragItemUI.Instance;
    }

    void Update() {
        // Update dragged item visual only if active and owned by this client
        if (IsOwner && _dragItemUI.gameObject.activeSelf) {
            UpdateVisualPosition();
        }
    }

    // Positions the dragged item icon to the pointer's rounded position
    void UpdateVisualPosition() {
        var pointerPosition = _inputManager.GetPointerPosition();
        _roundedPosition.x = Mathf.RoundToInt(pointerPosition.x);
        _roundedPosition.y = Mathf.RoundToInt(pointerPosition.y);
        _dragItemUI.transform.position = _roundedPosition;
    }

    #region -------------------- Left Click --------------------
    // Handles a left-click input on a specified item slot (inventory or outside)
    public void OnLeftClick(ItemSlot itemSlot) {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            PlaceItemOnMap(_dragItemSlot.Amount);
        } else {
            HandleLeftClickOnInventory(itemSlot);
        }

        FinalizeDragAction();
    }

    // Manages inventory logic when left-clicking inside the inventory area
    void HandleLeftClickOnInventory(ItemSlot clickedSlot) {
        if (_dragItemSlot.IsEmpty && clickedSlot.ItemId != EmptyItemId) {
            PickUpItemSlot(clickedSlot);
        } else if (clickedSlot.IsEmpty) {
            clickedSlot.Set(_dragItemSlot);
            _dragItemSlot.Clear();
        } else if (_dragItemSlot.CanStackWith(clickedSlot)) {
            HandleItemStacking(clickedSlot);
        } else {
            SwitchItemSlots(clickedSlot);
        }
    }

    // Stacks items if possible, or switches slots if already at max stack
    void HandleItemStacking(ItemSlot targetItemSlot) {
        int maxStackTarget = _itemManager.GetMaxStackableAmount(targetItemSlot.ItemId);
        int maxStackDrag = _itemManager.GetMaxStackableAmount(_dragItemSlot.ItemId);

        if (targetItemSlot.Amount >= maxStackTarget || _dragItemSlot.Amount >= maxStackDrag) {
            SwitchItemSlots(targetItemSlot);
            return;
        }

        int availableSpace = maxStackTarget - targetItemSlot.Amount;
        int amountToMove = Math.Min(availableSpace, _dragItemSlot.Amount);
        int moved = targetItemSlot.AddAmount(amountToMove, maxStackTarget);
        _dragItemSlot.RemoveAmount(moved);
    }
    #endregion -------------------- Left Click --------------------



    #region -------------------- Right Click --------------------
    // Handles a right-click input on a specified item slot (inventory or outside)
    public void OnRightClick(ItemSlot clickedItemSlot) {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            int spawnAmount = _inputManager.IsShiftPressed()
                ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            PlaceItemOnMap(spawnAmount);
        } else {
            PlaceItemInInventory(clickedItemSlot);
        }

        FinalizeDragAction();
    }
    #endregion -------------------- Right Click --------------------

    #region -------------------- Drag Item --------------------
    // Attempts to add items from a source slot into the currently dragged slot
    public int TryToAddItemToDragItem(ItemSlot sourceSlot) {
        if (!CanAddToDragItem(sourceSlot)) {
            int maxStack = _itemManager.GetMaxStackableAmount(_dragItemSlot.ItemId);
            int missingAmount = maxStack - _dragItemSlot.Amount;

            if (missingAmount >= sourceSlot.Amount) {
                int added = _dragItemSlot.AddAmount(sourceSlot.Amount, maxStack);
                sourceSlot.RemoveAmount(added);
            } else {
                _dragItemSlot.AddAmount(missingAmount, maxStack);
                sourceSlot.RemoveAmount(missingAmount);
            }
        }

        return sourceSlot.Amount;
    }

    // Checks if the dragged slot can accept items from another slot
    bool CanAddToDragItem(ItemSlot sourceSlot) => _dragItemSlot.IsEmpty || _dragItemSlot.CanStackWith(sourceSlot);

    // Places the currently dragged item slot back into a specific backpack slot
    public void AddDragItemBackIntoBackpack(int lastSlotId) {
        var backpackSlot = _playerInventoryController.InventoryContainer.ItemSlots[lastSlotId];
        backpackSlot.Set(_dragItemSlot);
        ClearDragItem();
    }

    // Wrap-up logic after drag actions, updating UI and icon
    private void FinalizeDragAction() {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) {
            ClearDragItem();
        }

        _playerInventoryController.InventoryContainer.UpdateUI();
        UpdateIcon();
    }

    // Clears the dragged item and hides the visual
    public void ClearDragItem() {
        _dragItemSlot.Clear();
        _dragItemUI.gameObject.SetActive(false);
    }

    // Updates the drag icon display based on the currently dragged item
    private void UpdateIcon() {
        if (_dragItemSlot.ItemId != EmptyItemId) {
            _dragItemUI.gameObject.SetActive(true);
            _dragItemUI.SetItemSlot(_dragItemSlot);
        } else {
            _dragItemUI.gameObject.SetActive(false);
        }
    }

    // Moves an item's entire slot into the drag slot
    private void PickUpItemSlot(ItemSlot sourceSlot) {
        _dragItemSlot.Set(sourceSlot);
        sourceSlot.Clear();
    }

    // Swaps the contents of the dragged slot with the target slot
    private void SwitchItemSlots(ItemSlot targetSlot) => _dragItemSlot.SwapWith(targetSlot);

    // Spawns items on the map when placing them outside the inventory
    private void PlaceItemOnMap(int spawnAmount) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) return;

        var itemToSpawn = new ItemSlot(_dragItemSlot.ItemId, spawnAmount, _dragItemSlot.RarityId);
        Vector3 spawnPosition = transform.position;
        Vector2 motionDirection = _playerMovementController.LastMotionDirection;
        _itemSpawnManager.SpawnItemServerRpc(itemToSpawn, spawnPosition, motionDirection, true);
        _dragItemSlot.RemoveAmount(spawnAmount);
    }

    // Places items into the inventory slot with possible stacking, triggered by right-click
    private void PlaceItemInInventory(ItemSlot targetSlot) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) return;

        if (targetSlot.IsEmpty) {
            targetSlot.Set(_dragItemSlot);
            _dragItemSlot.Clear();
        } else if (_dragItemSlot.CanStackWith(targetSlot)) {
            int transferAmount = _inputManager.IsShiftPressed()
                ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;

            int actualMoved = targetSlot.AddAmount(transferAmount, _itemManager.GetMaxStackableAmount(targetSlot.ItemId));
            _dragItemSlot.RemoveAmount(actualMoved);
        }
    }
    #endregion -------------------- Drag Item --------------------
}
