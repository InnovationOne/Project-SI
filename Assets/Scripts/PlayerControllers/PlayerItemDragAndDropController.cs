using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static ClothingSO;
using static ItemSlot;

public class PlayerItemDragAndDropController : MonoBehaviour {
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
        _itemManager = GameManager.Instance.ItemManager;
        _itemSpawnManager = GameManager.Instance.ItemSpawnManager;
        _inputManager = GameManager.Instance.InputManager;
        _dragItemUI = DragItemUI.Instance;
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
        if (!EventSystem.current.IsPointerOverGameObject()) {
            PlaceItemOnMap(_dragItemSlot.Amount);
        } else {
            HandleLeftClickOnInventory(itemSlot, inventorySlot);
        }
        FinalizeDragAction();
    }

    // Processes item logic for left-click inside the inventory.
    void HandleLeftClickOnInventory(ItemSlot clickedSlot, InventorySlot clickedInventorySlot) {
        // Check if we're dragging a clothing item
        if (!_dragItemSlot.IsEmpty) {
            var clothingItem = _itemManager.ItemDatabase[_dragItemSlot.ItemId] as ClothingSO;
            if (clothingItem != null) {
                int torsoSlotIndex = (int)ClothingType.Torso;
                int legsSlotIndex = (int)ClothingType.Legs;

                var slots = ClothingUI.Instance.PlayerClothingUIItemContainer.ItemSlots;
                var torsoSlot = slots[torsoSlotIndex];
                var legsSlot = slots[legsSlotIndex];
                var torsoNotEmpty = !torsoSlot.IsEmpty;
                var legsNotEmpty = !legsSlot.IsEmpty;

                // -----------------------------------------------
                // SCENARIO 1:
                // Dress is drag item + torso slot occupied + legs slot occupied
                // => place the dress in torso, swap with torso item, 
                //    then move the legs slot item to inventory or spawn leftover.
                // -----------------------------------------------
                if (clothingItem.Type == ClothingType.Dress && torsoNotEmpty && legsNotEmpty) {
                    // Swap the dragged dress with whatever is in torso
                    var oldTorso = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(_dragItemSlot);
                    _dragItemSlot.Set(oldTorso);

                    // Move the legs slot clothing to inventory, leftover to world
                    MoveItemToInventoryOrSpawn(legsSlot);
                    legsSlot.Clear();

                    // Done. Return so we skip normal inventory logic below.
                    _playerInventoryController.InventoryContainer.UpdateUI();
                    GameManager.Instance.AudioManager.PlayOneShot(
                        GameManager.Instance.FMODEvents.ItemShift,
                        transform.position
                    );
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 2:
                // Dress is drag item + only legs slot occupied
                // => place the dress in torso; the legs item becomes the new drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingType.Dress && legsNotEmpty && !torsoNotEmpty) {
                    // Put dress in torso
                    torsoSlot.Set(_dragItemSlot);

                    // The item in legs becomes the drag item
                    var oldLegs = new ItemSlot(legsSlot.ItemId, legsSlot.Amount, legsSlot.RarityId);
                    legsSlot.Clear();
                    _dragItemSlot.Set(oldLegs);

                    _playerInventoryController.InventoryContainer.UpdateUI();
                    GameManager.Instance.AudioManager.PlayOneShot(
                        GameManager.Instance.FMODEvents.ItemShift,
                        transform.position
                    );
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 3:
                // Dress is drag item + only torso slot occupied
                // => swap them directly (dress <-> torso)
                // -----------------------------------------------
                if (clothingItem.Type == ClothingType.Dress && torsoNotEmpty && !legsNotEmpty) {
                    var oldTorso = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(_dragItemSlot);
                    _dragItemSlot.Set(oldTorso);

                    _playerInventoryController.InventoryContainer.UpdateUI();
                    GameManager.Instance.AudioManager.PlayOneShot(
                        GameManager.Instance.FMODEvents.ItemShift,
                        transform.position
                    );
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 4:
                // Pants as DragItem + Dress in torso
                // => place pants in legs, the dress goes to drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingType.Legs && IsDressInSlot(torsoSlot)) {
                    // The user is dragging pants. We put them into legs.
                    // The old dress in torso becomes the drag item.
                    var oldDress = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Clear();

                    legsSlot.Set(_dragItemSlot);
                    _dragItemSlot.Set(oldDress);

                    _playerInventoryController.InventoryContainer.UpdateUI();
                    GameManager.Instance.AudioManager.PlayOneShot(
                        GameManager.Instance.FMODEvents.ItemShift,
                        transform.position
                    );
                    return;
                }

                // -----------------------------------------------
                // SCENARIO 5:
                // Top as DragItem + Dress in torso
                // => top is placed in torso, dress is swapped into drag item
                // -----------------------------------------------
                if (clothingItem.Type == ClothingType.Torso && IsDressInSlot(torsoSlot)) {
                    var oldDress = new ItemSlot(torsoSlot.ItemId, torsoSlot.Amount, torsoSlot.RarityId);
                    torsoSlot.Set(_dragItemSlot);
                    _dragItemSlot.Set(oldDress);

                    _playerInventoryController.InventoryContainer.UpdateUI();
                    GameManager.Instance.AudioManager.PlayOneShot(
                        GameManager.Instance.FMODEvents.ItemShift,
                        transform.position
                    );
                    return;
                }
            }
        }

        // If no special clothing scenario was triggered, do regular inventory logic: pick up, stack, swap, etc.
        if (_dragItemSlot.IsEmpty && clickedSlot.ItemId != EmptyItemId) {
            PickUpItemSlot(clickedSlot);
        } else if (clickedSlot.IsEmpty) {
            clickedSlot.Set(_dragItemSlot);
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
            _dragItemSlot.Clear();
        } else if (_dragItemSlot.CanStackWith(clickedSlot)) {
            HandleItemStacking(clickedSlot);
        } else {
            SwitchItemSlots(clickedSlot);
        }
    }

    // Utility method to see if there's a dress in a given slot.
    bool IsDressInSlot(ItemSlot slot) {
        if (slot.IsEmpty) return false;
        var cso = _itemManager.ItemDatabase[slot.ItemId] as ClothingSO;
        return cso != null && cso.Type == ClothingType.Dress;
    }

    // Moves a slot item to player inventory, spawns leftover if inventory is full.
    void MoveItemToInventoryOrSpawn(ItemSlot slotToMove) {
        int leftover = _playerInventoryController.InventoryContainer.AddItem(slotToMove, true);
        if (leftover > 0) {
            _itemSpawnManager.SpawnItemServerRpc(
                slotToMove,
                transform.position,
                _playerMovementController.LastMotionDirection,
                true);
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        }
    }

    // Stacks items if there's space, else swap them.
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
        if (moved > 0) GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);

        _dragItemSlot.RemoveAmount(moved);
    }

    // Picks up an item from a slot into the drag slot.
    void PickUpItemSlot(ItemSlot sourceSlot) {
        _dragItemSlot.Set(sourceSlot);
        sourceSlot.Clear();
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemPickup, transform.position);
    }

    // Swaps items between the dragged slot and a target slot.
    void SwitchItemSlots(ItemSlot targetSlot) {
        _dragItemSlot.SwapWith(targetSlot);
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
    }

    // Places items in the world if cursor is not over UI.
    void PlaceItemOnMap(int spawnAmount) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) return;
        _itemSpawnManager.SpawnItemServerRpc(
            new ItemSlot(_dragItemSlot.ItemId, spawnAmount, _dragItemSlot.RarityId),
            transform.position,
            _playerMovementController.LastMotionDirection,
            true);
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        _dragItemSlot.RemoveAmount(spawnAmount);
    }

    #endregion -------------------- Left Click --------------------

    #region -------------------- Right Click --------------------

    // Adds items to an inventory slot on right-click. 
    public void OnRightClick(ItemSlot clickedItemSlot) {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            int spawnAmount = _inputManager.GetShiftPressed()
                ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            PlaceItemOnMap(spawnAmount);
        } else {
            PlaceItemInInventory(clickedItemSlot);
        }
        FinalizeDragAction();
    }

    // Places an item into a target slot (right-click scenario).
    void PlaceItemInInventory(ItemSlot targetSlot) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) return;
        if (targetSlot.IsEmpty) {
            targetSlot.Set(_dragItemSlot);
            GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
            _dragItemSlot.Clear();
        } else if (_dragItemSlot.CanStackWith(targetSlot)) {
            int transferAmount = _inputManager.GetShiftPressed()
                ? Mathf.Min(_dragItemSlot.Amount, InputManager.SHIFT_KEY_AMOUNT)
                : 1;
            int actualMoved = targetSlot.AddAmount(transferAmount, _itemManager.GetMaxStackableAmount(targetSlot.ItemId));
            if (actualMoved > 0) GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
            _dragItemSlot.RemoveAmount(actualMoved);
        }
    }

    #endregion -------------------- Right Click --------------------

    #region -------------------- Helper Methodes --------------------

    // Attempts to merge items into the dragged slot from another slot.
    public int TryToAddItemToDragItem(ItemSlot sourceSlot) {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.CanStackWith(sourceSlot)) {
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

    // Places the drag item back into the player’s backpack slot.
    public void AddDragItemBackIntoBackpack(int lastSlotId) {
        var backpackSlot = _playerInventoryController.InventoryContainer.ItemSlots[lastSlotId];
        backpackSlot.Set(_dragItemSlot);
        ClearDragItem();
    }

    // Concludes drag operations and updates UI.
    private void FinalizeDragAction() {
        if (_dragItemSlot.IsEmpty || _dragItemSlot.Amount <= 0) {
            ClearDragItem();
        }
        _playerInventoryController.InventoryContainer.UpdateUI();
        UpdateIcon();
    }

    // Clears the drag slot and hides the drag icon.
    public void ClearDragItem() {
        _dragItemSlot.Clear();
        _dragItemUI.gameObject.SetActive(false);
    }

    // Updates drag icon visuals.
    private void UpdateIcon() {
        if (_dragItemSlot.ItemId != EmptyItemId) {
            _dragItemUI.SetItemSlot(_dragItemSlot);
            _dragItemUI.gameObject.SetActive(true);
        } else _dragItemUI.gameObject.SetActive(false);
    }

    #endregion -------------------- Helper Methodes --------------------
}
