using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using static ItemSlot;

public class PlayerItemDragAndDropController : NetworkBehaviour {
    public static PlayerItemDragAndDropController LocalInstance { get; private set; }

    // Cached references
    private ItemSlot _dragItemSlot;
    private PlayerInventoryController _playerInventoryController;
    private DragItemUI _visual;
    private InputManager _inputManager;
    private ItemManager _itemManager;
    private PlayerMovementController _playerMovementController;

    // Reusable Vector3 to minimize allocations
    private Vector3 _roundedPosition;

    private void Awake() {
        _dragItemSlot = new ItemSlot();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _itemManager = ItemManager.Instance;
        _playerMovementController = PlayerMovementController.LocalInstance;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerItemDragAndDropController in the scene!");
                return;
            }
            LocalInstance = this;
        }

        _visual = DragItemUI.Instance;
        _inputManager = InputManager.Instance;
    }

    private void Update() {
        if (_visual.gameObject.activeSelf && IsOwner) {
            UpdateVisualPosition();
        }
    }

    /// <summary>
    /// Updates the position of the drag visual based on the pointer position.
    /// </summary>
    private void UpdateVisualPosition() {
        Vector3 pointerPosition = _inputManager.GetPointerPosition();

        // Round the pointer position to the nearest integer values
        _roundedPosition.x = Mathf.RoundToInt(pointerPosition.x);
        _roundedPosition.y = Mathf.RoundToInt(pointerPosition.y);

        _visual.transform.position = _roundedPosition;
    }

    #region Left Click
    public void OnLeftClick(ItemSlot itemSlot) {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            PlaceItemOnMap(_dragItemSlot.Amount);
        } else {
            HandleLeftClickOnInventory(itemSlot);
        }

        FinalizeDragAction();
    }

    private void HandleLeftClickOnInventory(ItemSlot clickedItemSlot) {
        if (_dragItemSlot.IsEmpty && clickedItemSlot.ItemId != EmptyItemId) {
            PickUpItemSlot(clickedItemSlot);
        } else if (clickedItemSlot.IsEmpty) {
            PlaceItemSlotIntoInventory(clickedItemSlot);
        } else if (_dragItemSlot.CanStackWith(clickedItemSlot)) {
            HandleItemStacking(clickedItemSlot);
        } else {
            SwitchItemSlots(clickedItemSlot);
        }
    }

    /// <summary>
    /// Handles stacking logic when items can be stacked.
    /// </summary>
    /// <param name="itemSlot">The target item slot.</param>
    private void HandleItemStacking(ItemSlot targetItemSlot) {
        int maxStackTarget = _itemManager.GetMaxStackableAmount(targetItemSlot.ItemId);
        int maxStackDrag = _itemManager.GetMaxStackableAmount(_dragItemSlot.ItemId);

        if (targetItemSlot.Amount >= maxStackTarget || _dragItemSlot.Amount >= maxStackDrag) {
            SwitchItemSlots(targetItemSlot);
        } else {
            int availableSpace = maxStackTarget - targetItemSlot.Amount;
            int amountToMove = Math.Min(availableSpace, _dragItemSlot.Amount);

            int actualMoved = targetItemSlot.AddAmount(amountToMove, maxStackTarget);
            _dragItemSlot.RemoveAmount(actualMoved);
        }
    }
    #endregion



    #region Right Click Handlers
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
    #endregion

    public int TryToAddItemToDragItem(ItemSlot sourceItemSlot) {
        if (!CanAddToDragItem(sourceItemSlot)) {
            int maxStack = _itemManager.GetMaxStackableAmount(_dragItemSlot.ItemId);
            int missingAmount = maxStack - _dragItemSlot.Amount;

            if (missingAmount >= sourceItemSlot.Amount) {
                int addedAmount = _dragItemSlot.AddAmount(sourceItemSlot.Amount, maxStack);
                sourceItemSlot.RemoveAmount(addedAmount);
            } else {
                _dragItemSlot.AddAmount(missingAmount, maxStack);
                sourceItemSlot.RemoveAmount(missingAmount);
            }
        }

        // Return the amount of items that weren't added to the drag item slot.
        return sourceItemSlot.Amount;
    }

    /// <summary>
    /// Checks if an item can be added to the drag slot.
    /// </summary>
    /// <param name="sourceItemSlot">The item slot to check.</param>
    /// <returns>True if the item can be added; otherwise, false.</returns>
    private bool CanAddToDragItem(ItemSlot sourceItemSlot) {
        if (_dragItemSlot.IsEmpty) {
            return true;
        }

        return _dragItemSlot.CanStackWith(sourceItemSlot);
    }

    /// <summary>
    /// Adds the drag item back into the backpack.
    /// </summary>
    /// <param name="lastSlotId">The ID of the last slot.</param>
    public void AddDragItemBackIntoBackpack(int lastSlotId) {
        var backpackSlot = _playerInventoryController.InventoryContainer.ItemSlots[lastSlotId];
        backpackSlot.Set(_dragItemSlot);
        ClearDragItem();
    }

    /// <summary>
    /// Clears the drag item if its amount is zero or less.
    /// </summary>
    private void CheckToClearDragItem() {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) {
            ClearDragItem();
        }
    }

    /// <summary>
    /// Clears the drag item and hides the visual.
    /// </summary>
    public void ClearDragItem() {
        _dragItemSlot.Clear();
        _visual.gameObject.SetActive(false);
    }

    /// <summary>
    /// Finalizes the drag action by checking and updating the UI and icon.
    /// </summary>
    private void FinalizeDragAction() {
        CheckToClearDragItem();
        _playerInventoryController.InventoryContainer.UpdateUI();
        UpdateIcon();
    }

    /// <summary>
    /// Updates the drag item's visual icon based on the current drag item slot.
    /// </summary>
    private void UpdateIcon() {
        if (_dragItemSlot.ItemId != EmptyItemId) {
            _visual.gameObject.SetActive(true);
            _visual.SetItemSlot(_dragItemSlot);
        } else {
            _visual.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Picks up an item slot into the drag slot.
    /// </summary>
    /// <param name="sourceItemSlot">The item slot to pick up.</param>
    private void PickUpItemSlot(ItemSlot sourceItemSlot) {
        _dragItemSlot.Set(sourceItemSlot);
        sourceItemSlot.Clear();
    }

    /// <summary>
    /// Places the drag item slot into the inventory slot.
    /// </summary>
    /// <param name="targetItemSlot">The inventory slot to place into.</param>
    private void PlaceItemSlotIntoInventory(ItemSlot targetItemSlot) {
        targetItemSlot.Set(_dragItemSlot);
        _dragItemSlot.Clear();
    }

    /// <summary>
    /// Switches the drag item slot with the target item slot.
    /// </summary>
    /// <param name="targetItemSlot">The target item slot.</param>
    private void SwitchItemSlots(ItemSlot targetItemSlot) {
        _dragItemSlot.SwapWith(targetItemSlot);
    }

    /// <summary>
    /// Places items on the map by spawning them.
    /// </summary>
    /// <param name="spawnAmount">The amount of items to spawn.</param>
    private void PlaceItemOnMap(int spawnAmount) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) {
            return;
        }

        var itemToSpawn = new ItemSlot(_dragItemSlot.ItemId, spawnAmount, _dragItemSlot.RarityId);
        Vector3 spawnPosition = transform.position;
        Vector2 motionDirection = _playerMovementController.LastMotionDirection;

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: itemToSpawn,
            initialPosition: spawnPosition,
            motionDirection: motionDirection,
            useInventoryPosition: true
        );

        _dragItemSlot.RemoveAmount(spawnAmount);
    }

    /// <summary>
    /// Places items into the inventory slot.
    /// </summary>
    /// <param name="targetItemSlot">The inventory slot to place into.</param>
    private void PlaceItemInInventory(ItemSlot targetItemSlot) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) { 
            return; 
        }

        if (targetItemSlot.IsEmpty) {
            targetItemSlot.Set(_dragItemSlot);
            _dragItemSlot.Clear();
        } else if (_dragItemSlot.CanStackWith(targetItemSlot)) {
            int transferAmount = _inputManager.IsShiftPressed()
                ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;

            int actualMoved = targetItemSlot.AddAmount(transferAmount, _itemManager.GetMaxStackableAmount(targetItemSlot.ItemId));
            _dragItemSlot.RemoveAmount(actualMoved);
        }
    }
}
