using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static ItemSlot;

public class PlayerItemDragAndDropController : MonoBehaviour {
    public ItemSlot DragItemSlot;
    PlayerInventoryController _playerInventoryController;
    PlayerMovementController _playerMovementController;
    ItemManager _itemManager;
    ItemSpawnManager _itemSpawnManager;
    InputManager _inputManager;
    DragItemUI _dragItemUI;

    // Reused for position calculations
    Vector3 _roundedPosition;

    void Awake() {
        DragItemSlot = new ItemSlot();
        _playerInventoryController = GetComponent<PlayerInventoryController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
    }

    void Start() {
        _itemManager = GameManager.Instance.ItemManager;
        _itemSpawnManager = GameManager.Instance.ItemSpawnManager;
        _inputManager = GameManager.Instance.InputManager;
        _dragItemUI = UIManager.Instance.DragItemUI;
    }

    void Update() {
        if (!_dragItemUI.gameObject.activeSelf) return;
        UpdateVisualPosition();
    }

    // Smooths item icon position to mouse pointer.
    void UpdateVisualPosition() {
        var pointerPosition = _inputManager.GetPointerPosition();
        _roundedPosition.x = Mathf.RoundToInt(pointerPosition.x);
        _roundedPosition.y = Mathf.RoundToInt(pointerPosition.y);
        _dragItemUI.transform.position = _roundedPosition;
    }

    #region -------------------- Left Click --------------------

    // Primary left-click entry point.
    public void OnLeftClick(ItemSlot itemSlot, InventorySlot inventorySlot = null) {
        if (!EventSystem.current.IsPointerOverGameObject()) PlaceItemOnMap(DragItemSlot.Amount);
        else HandleLeftClickOnInventory(itemSlot, inventorySlot);
        FinalizeDragAction();
    }

    // Processes item logic for left-click inside the inventory.
    void HandleLeftClickOnInventory(ItemSlot clickedSlot, InventorySlot clickedInventorySlot) {
        if (DragItemSlot.IsEmpty && clickedSlot.ItemId != EmptyItemId) PickUpItemSlot(clickedSlot);
        else if (clickedSlot.IsEmpty) {
            clickedSlot.Set(DragItemSlot);
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
            DragItemSlot.Clear();
        } else if (DragItemSlot.CanStackWith(clickedSlot)) HandleItemStacking(clickedSlot);
        else SwitchItemSlots(clickedSlot);

    }

    // Stacks items if there's space, else swap them.
    void HandleItemStacking(ItemSlot targetItemSlot) {
        int maxStackTarget = _itemManager.GetMaxStackableAmount(targetItemSlot.ItemId);
        int maxStackDrag = _itemManager.GetMaxStackableAmount(DragItemSlot.ItemId);
        if (targetItemSlot.Amount >= maxStackTarget || DragItemSlot.Amount >= maxStackDrag) {
            SwitchItemSlots(targetItemSlot);
            return;
        }

        int availableSpace = maxStackTarget - targetItemSlot.Amount;
        int amountToMove = Math.Min(availableSpace, DragItemSlot.Amount);
        int moved = targetItemSlot.AddAmount(amountToMove, maxStackTarget);
        if (moved > 0) GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);

        DragItemSlot.RemoveAmount(moved);
    }

    // Picks up an item from a slot into the drag slot.
    void PickUpItemSlot(ItemSlot sourceSlot) {
        DragItemSlot.Set(sourceSlot);
        sourceSlot.Clear();
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemPickup, transform.position);
    }

    // Swaps items between the dragged slot and a target slot.
    void SwitchItemSlots(ItemSlot targetSlot) {
        DragItemSlot.SwapWith(targetSlot);
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
    }

    // Places items in the world if cursor is not over UI.
    void PlaceItemOnMap(int spawnAmount) {
        if (DragItemSlot.IsEmpty || DragItemSlot.Amount <= 0) return;
        _itemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(DragItemSlot.ItemId, spawnAmount, DragItemSlot.RarityId),
            transform.position,
            _playerMovementController.LastMotionDirection,
            true);
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        DragItemSlot.RemoveAmount(spawnAmount);
    }

    #endregion -------------------- Left Click --------------------

    #region -------------------- Right Click --------------------

    // Adds items to an inventory slot on right-click. 
    public void OnRightClick(ItemSlot clickedItemSlot) {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            int spawnAmount = _inputManager.GetShiftPressed()
                ? Mathf.Min(DragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            PlaceItemOnMap(spawnAmount);
        } else PlaceItemInInventory(clickedItemSlot);
        FinalizeDragAction();
    }

    // Places an item into a target slot (right-click scenario).
    void PlaceItemInInventory(ItemSlot targetSlot) {
        if (DragItemSlot.IsEmpty || DragItemSlot.Amount <= 0) return;
        if (targetSlot.IsEmpty) {
            int transferAmount = _inputManager.GetShiftPressed()
                ? Mathf.Min(DragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            targetSlot.Set(new ItemSlot(DragItemSlot.ItemId, transferAmount, DragItemSlot.RarityId));
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
            DragItemSlot.RemoveAmount(transferAmount);
        } else if (DragItemSlot.CanStackWith(targetSlot)) {
            int transferAmount = _inputManager.GetShiftPressed()
                ? Mathf.Min(DragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            int actualMoved = targetSlot.AddAmount(transferAmount, _itemManager.GetMaxStackableAmount(targetSlot.ItemId));
            if (actualMoved > 0) GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
            DragItemSlot.RemoveAmount(actualMoved);
        }
    }

    #endregion -------------------- Right Click --------------------

    #region -------------------- Helper Methodes --------------------

    // Attempts to merge items into the dragged slot from another slot.
    public int TryToAddItemToDragItem(ItemSlot sourceSlot) {
        if (DragItemSlot.IsEmpty || DragItemSlot.CanStackWith(sourceSlot)) {
            int maxStack = _itemManager.GetMaxStackableAmount(DragItemSlot.ItemId);
            int missingAmount = maxStack - DragItemSlot.Amount;
            if (missingAmount >= sourceSlot.Amount) {
                int added = DragItemSlot.AddAmount(sourceSlot.Amount, maxStack);
                sourceSlot.RemoveAmount(added);
            } else {
                DragItemSlot.AddAmount(missingAmount, maxStack);
                sourceSlot.RemoveAmount(missingAmount);
            }
        }
        UpdateIcon();
        return sourceSlot.Amount;
    }

    // Places the drag item back into the player’s backpack slot.
    public void AddDragItemBackIntoBackpack(int lastSlotId) {
        if (lastSlotId < 0 || lastSlotId >= _playerInventoryController.InventoryContainer.ItemSlots.Count || DragItemSlot.IsEmpty) return;
        var backpackSlot = _playerInventoryController.InventoryContainer.ItemSlots[lastSlotId];
        backpackSlot.Set(DragItemSlot);
        ClearDragItem();
        UpdateIcon();
    }

    // Concludes drag operations and updates UI.
    private void FinalizeDragAction() {
        if (DragItemSlot.IsEmpty || DragItemSlot.Amount <= 0) ClearDragItem();
        _playerInventoryController.InventoryContainer.UpdateUI();
        UpdateIcon();
    }

    // Clears the drag slot and hides the drag icon.
    public void ClearDragItem() {
        DragItemSlot.Clear();
        UpdateIcon();
    }

    // Updates drag icon visuals.
    private void UpdateIcon() => _dragItemUI.SetItemSlot(DragItemSlot);
    

    #endregion -------------------- Helper Methodes --------------------
}
