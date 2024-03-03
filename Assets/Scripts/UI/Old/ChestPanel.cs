using UnityEngine;
using UnityEngine.UI;

public class ChestPanel : ItemContainerPanel {
    public static ChestPanel Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Button _buttonPrefab;
    private ChestBehaviour _chestBehaviour;

    [Header("Contentbox")]
    [SerializeField] private Transform _content;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ChestPanel in the scene!");
            return;
        }
        Instance = this;
    }

    public void ShowChest(ItemContainerSO itemContainer, ChestBehaviour chest) {
        _chestBehaviour = chest;
        ItemContainer = itemContainer;

        foreach (Transform child in _content) {
            Destroy(child.gameObject);
        }

        ItemButtons = new Button[ItemContainer.ItemSlots.Count];
        for (int i = 0; i < ItemContainer.ItemSlots.Count; i++) {
            Button button = Instantiate(_buttonPrefab, _content);
            ItemButtons[i] = button;
        }

        Init();
    }

    public void HideChest() {
        _chestBehaviour.CloseChest();
        _chestBehaviour = null;
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        if (Input.GetKey(KeyCode.LeftShift)) {
            int remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItemToItemContainer(
                ItemContainer.ItemSlots[buttonIndex].Item.ItemID,
                ItemContainer.ItemSlots[buttonIndex].Amount,
                ItemContainer.ItemSlots[buttonIndex].RarityID,
                true);

            if (remainingAmount > 0) {
                ItemContainer.ItemSlots[buttonIndex].Amount = remainingAmount;
            } else {
                ItemContainer.ItemSlots[buttonIndex].Clear();
            }
        } else {
            PlayerItemDragAndDropController.LocalInstance.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }
}
