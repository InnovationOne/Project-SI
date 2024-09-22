using UnityEngine;
using UnityEngine.UI;

public class ChestUI : ItemContainerUI {
    public static ChestUI Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Button _buttonPrefab;

    [Header("Contentbox")]
    [SerializeField] private Transform _content;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ChestPanel in the scene!");
            return;
        }
        Instance = this;
    }

    public void ShowChest(ItemContainerSO itemContainer) {
        gameObject.SetActive(true);

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
        gameObject.SetActive(false);
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        if (Input.GetKey(KeyCode.LeftShift)) {
            int remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], true);

            if (remainingAmount > 0) {
                var slot = ItemContainer.ItemSlots[buttonIndex];
                slot.Set(new ItemSlot(slot.ItemId, remainingAmount, slot.RarityId));
            } else {
                ItemContainer.ItemSlots[buttonIndex].Clear();
            }
        } else {
            PlayerItemDragAndDropController.LocalInstance.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }
}
