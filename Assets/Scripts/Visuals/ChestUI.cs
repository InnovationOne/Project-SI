using UnityEngine;
using UnityEngine.UI;

public class ChestUI : ItemContainerUI {
    public static ChestUI Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] InventorySlot _buttonPrefab;

    [Header("Contentbox")]
    [SerializeField] Transform _content;

    PlayerInventoryController _playerInventoryController;
    PlayerItemDragAndDropController _playerItemDragAndDropController;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ChestPanel in the scene!");
            return;
        }
        Instance = this;
    }

    private void Start() {
        _playerInventoryController = PlayerController.LocalInstance.PlayerInventoryController;
        _playerItemDragAndDropController = PlayerController.LocalInstance.PlayerItemDragAndDropController;
    }

    // Dynamically shows a chest's item container by creating slot UI.
    public void ShowChestUI(ItemContainerSO itemContainer) {
        gameObject.SetActive(true);
        ItemContainer = itemContainer;

        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }

        ItemButtons = new InventorySlot[ItemContainer.ItemSlots.Count];
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            var button = Instantiate(_buttonPrefab, _content);
            ItemButtons[i] = button;
        }

        Init();
    }

    // Hides the entire chest UI.
    public void HideChestUI() {
        gameObject.SetActive(false);
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        if (GameManager.Instance.InputManager.IsShiftPressed()) {
            int remainingAmount = _playerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], true);

            if (remainingAmount > 0) {
                var slot = ItemContainer.ItemSlots[buttonIndex];
                slot.Set(new ItemSlot(slot.ItemId, remainingAmount, slot.RarityId));
            } else {
                ItemContainer.ItemSlots[buttonIndex].Clear();
            }
        } else {
            _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }
}
