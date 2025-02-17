using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChestUI : ItemContainerUI {
    public static ChestUI Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] Button _invenToChestBtn;
    [SerializeField] Button _chestToInvenBtn;
    [SerializeField] Button _sortBtn;
    [SerializeField] Button _trashBtn;
    [SerializeField] Button _closeButton;

    [Header("Prefabs")]
    [SerializeField] InventorySlot _inventoryButtonPrefab;

    [Header("Contentbox")]
    [SerializeField] Transform _content;

    public ItemContainerSO PublicItemContainer => ItemContainer;

    PlayerInventoryController _playerInventoryController;
    PlayerItemDragAndDropController _playerItemDragAndDropController;
    PlayerInteractController _playerInteractController;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ChestPanel in the scene!");
            return;
        }
        Instance = this;
        PlayerController.OnLocalPlayerSpawned += CatchReferences;
    }

    private void Start() {
        if (_invenToChestBtn != null) _invenToChestBtn.onClick.AddListener(StoreAllValidItemsInChest);
        if (_chestToInvenBtn != null) _chestToInvenBtn.onClick.AddListener(TakeAllItemsFromChest);
    }

    private void OnDestroy() {
        if (_sortBtn != null) _sortBtn.onClick.RemoveAllListeners();
        if (_trashBtn != null) _trashBtn.onClick.RemoveAllListeners();
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
    }

    void CatchReferences(PlayerController playerController) {
        _playerInventoryController = playerController.PlayerInventoryController;
        _playerItemDragAndDropController = playerController.PlayerItemDragAndDropController;
        _playerInteractController = playerController.PlayerInteractionController;

        if (_sortBtn != null) _sortBtn.onClick.AddListener(() => _playerInventoryController.InventoryContainer.SortItems());
        if (_trashBtn != null) _trashBtn.onClick.AddListener(() => _playerItemDragAndDropController.ClearDragItem());
        if (_closeButton != null) _closeButton.onClick.AddListener(() => _playerInteractController.StopInteract());
    }

    // Displays dynamic chest slots.
    public void ShowChestUI(ItemContainerSO itemContainer) {
        Debug.Log("Showing chest UI");
        gameObject.SetActive(true);
        ItemContainer = itemContainer;

        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }

        ItemButtons = new InventorySlot[ItemContainer.ItemSlots.Count];
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            var button = Instantiate(_inventoryButtonPrefab, _content);
            ItemButtons[i] = button;
        }
        Init();
    }

    // Hides the chest UI entirely.
    public void HideChestUI() {
        gameObject.SetActive(false);
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        // SHIFT => Move item from chest to inventory; otherwise do normal drag
        if (GameManager.Instance.InputManager.GetShiftPressed()) {
            var slot = ItemContainer.ItemSlots[buttonIndex];
            if (!slot.IsEmpty) {
                int remainingAmount = _playerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], true);
                if (remainingAmount > 0) slot.Set(new ItemSlot(slot.ItemId, remainingAmount, slot.RarityId));
                else ItemContainer.ItemSlots[buttonIndex].Clear();
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
            }
        } else _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        ShowUIButtonContains();
    }

    // Moves all items from chest to player inventory.
    void TakeAllItemsFromChest() {
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            if (!ItemContainer.ItemSlots[i].IsEmpty) {
                int remaining = _playerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[i], true);
                if (remaining <= 0) ItemContainer.ItemSlots[i].Clear();
                else ItemContainer.ItemSlots[i].Set(new ItemSlot(ItemContainer.ItemSlots[i].ItemId, remaining, ItemContainer.ItemSlots[i].RarityId));
            }
        }
        GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemDrop, transform.position);
        ShowUIButtonContains();
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
        ShowUIButtonContains();
    }
}
