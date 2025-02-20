using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChestUI : ItemContainerUI {

    [Header("Buttons")]
    [SerializeField] Button _invenToChestBtn;
    [SerializeField] Button _chestToInvenBtn;
    [SerializeField] Button _sortBtn;
    [SerializeField] Button _trashBtn;
    [SerializeField] Button _closeBtn;

    [Header("Prefabs")]
    [SerializeField] InventorySlot _inventoryButtonPrefab;

    [Header("Contentbox")]
    [SerializeField] Transform _content;

    public ItemContainerSO PublicItemContainer => ItemContainer;

    PlayerInventoryController _playerInventoryController;
    PlayerItemDragAndDropController _playerItemDragAndDropController;
    PlayerInteractController _playerInteractController;

    private void Awake() {
        PlayerController.OnLocalPlayerSpawned += CatchReferences;
    }

    private void Start() {
        ItemContainerUIAwake();
        if (_invenToChestBtn != null) _invenToChestBtn.onClick.AddListener(StoreAllValidItemsInChest);
        if (_chestToInvenBtn != null) _chestToInvenBtn.onClick.AddListener(TakeAllItemsFromChest);
    }

    private void OnDestroy() {
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
        if (_sortBtn != null) _sortBtn.onClick.RemoveAllListeners();
        if (_trashBtn != null) _trashBtn.onClick.RemoveAllListeners();
        if (_closeBtn != null) _closeBtn.onClick.RemoveAllListeners();
    }

    void CatchReferences(PlayerController playerController) {
        _playerInventoryController = playerController.PlayerInventoryController;
        _playerItemDragAndDropController = playerController.PlayerItemDragAndDropController;
        _playerInteractController = playerController.PlayerInteractionController;

        if (_sortBtn != null) _sortBtn.onClick.AddListener(() => ItemContainer.SortItems(false));
        if (_trashBtn != null) _trashBtn.onClick.AddListener(() => _playerItemDragAndDropController.ClearDragItem());
        if (_closeBtn != null) _closeBtn.onClick.AddListener(() => _playerInteractController.StopInteract());
    }

    // Displays dynamic chest slots.
    public void InitChestUI(ItemContainerSO itemContainer) {
        ItemContainer = itemContainer;
        foreach (Transform child in _content) Destroy(child.gameObject);

        ItemButtons = new InventorySlot[ItemContainer.ItemSlots.Count];
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            var button = Instantiate(_inventoryButtonPrefab, _content);
            ItemButtons[i] = button;
        }
        Init();
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        // SHIFT => from chest to inventory
        if (GameManager.Instance.InputManager.GetShiftPressed()) {
            var slot = ItemContainer.ItemSlots[buttonIndex];
            if (!slot.IsEmpty) {
                int leftover = _playerInventoryController.InventoryContainer.AddItem(slot, true);
                if (leftover > 0) slot.Set(new ItemSlot(slot.ItemId, leftover, slot.RarityId)); else slot.Clear();
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
            }
            ShowUIButtonContains();
            _playerInventoryController.InventoryContainer.UpdateUI();
            return;
        }
        // Normal left-click: pick up / swap / stack with drag
        _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        ShowUIButtonContains();
    }

    public override void OnPlayerRightClick(int buttonIndex) {
        // Right-click => deposit from DragItem to chest slot
        if (UIManager.Instance.DragItemUI.gameObject.activeSelf) {
            _playerItemDragAndDropController.OnRightClick(ItemContainer.ItemSlots[buttonIndex]);
            ShowUIButtonContains();
            return;
        }
        // If no drag item, do nothing or show right-click menu
        ShowRightClickMenu(buttonIndex, Input.mousePosition);
    }


    // Moves all items from chest to player inventory.
    void TakeAllItemsFromChest() {
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            var slot = ItemContainer.ItemSlots[i];
            if (!slot.IsEmpty) {
                int leftover = _playerInventoryController.InventoryContainer.AddItem(slot, true);
                if (leftover <= 0) slot.Clear();
                else slot.Set(new ItemSlot(slot.ItemId, leftover, slot.RarityId));
            }
        }
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        ShowUIButtonContains();
        _playerInventoryController.InventoryContainer.UpdateUI();
    }

    // Moves valid items from player inventory to chest.
    void StoreAllValidItemsInChest() {
        var itemIdsInChest = new HashSet<int>();
        foreach (var chestSlot in ItemContainer.ItemSlots) {
            if (!chestSlot.IsEmpty) itemIdsInChest.Add(chestSlot.ItemId);
        }

        var playerSlots = _playerInventoryController.InventoryContainer.ItemSlots;
        for (int i = 0; i < playerSlots.Count; i++) {
            var playerSlot = playerSlots[i];
            if (!playerSlot.IsEmpty && itemIdsInChest.Contains(playerSlot.ItemId)) {
                int leftover = ItemContainer.AddItem(playerSlot, true);
                if (leftover <= 0) playerSlot.Clear();
                else playerSlot.Set(new ItemSlot(playerSlot.ItemId, leftover, playerSlot.RarityId));
            }
        }
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);

        // Update chest and the player's inventory
        ShowUIButtonContains();
        _playerInventoryController.InventoryContainer.UpdateUI();
    }
}
