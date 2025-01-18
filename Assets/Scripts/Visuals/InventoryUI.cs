using System;
using UnityEngine;
using UnityEngine.UI;

// This class is for the backpack panel
public class InventoryUI : ItemContainerUI {
    public static InventoryUI Instance { get; private set; }

    public int LastSlotId { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button _sortButton;
    [SerializeField] private Button _trashButton;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of InventoryPanel in the scene!");
            return;
        }
        Instance = this;

        _sortButton.onClick.AddListener(() => PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.SortItems());
        _trashButton.onClick.AddListener(() => PlayerController.LocalInstance.PlayerItemDragAndDropController.ClearDragItem());
    }

    private void Start() {
        base.ItemContainerPanelAwake();

        ItemContainer.OnItemsUpdated += ShowUIButtonContains;

        Init();
    }

    public void InventoryOrToolbeltSizeChanged() {
        int toolbeltSize = PlayerController.LocalInstance.PlayerToolbeltController.CurrentToolbeltSize;
        int maxToolbeltSize = PlayerController.LocalInstance.PlayerToolbeltController.ToolbeltSizes[^1];
        int inventorySize = PlayerController.LocalInstance.PlayerInventoryController.CurrentInventorySize;



        for (int i = 0; i < ItemButtons.Length; i++) {
            // Set the toolbelt slots
            if (i < maxToolbeltSize) {
                if (i < toolbeltSize) {
                    ItemButtons[i].interactable = true;
                    ItemButtons[i].GetComponent<InventorySlot>().SetActive();
                    continue;
                } else {
                    ItemButtons[i].interactable = false;
                    ItemButtons[i].GetComponent<InventorySlot>().SetLocked();
                    continue;
                }
            }

            // Set the inventory slots
            if (i < (maxToolbeltSize + inventorySize)) {
                ItemButtons[i].GetComponent<Button>().interactable = true;
                ItemButtons[i].GetComponent<InventorySlot>().SetActive();
                continue;
            } else {
                ItemButtons[i].GetComponent<Button>().interactable = false;
                ItemButtons[i].GetComponent<InventorySlot>().SetLocked();
                continue;
            }
        }
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        Debug.Log("Left click on button " + buttonIndex);

        if (Input.GetKey(KeyCode.LeftShift)) {
            int remainingAmount;
            if (buttonIndex < PlayerController.LocalInstance.PlayerToolbeltController.ToolbeltSizes[^1]) {
                remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], true);
            } else {
                remainingAmount = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], false);
            }

            if (remainingAmount > 0) {
                var slot = ItemContainer.ItemSlots[buttonIndex];
                slot.Set(new ItemSlot(slot.ItemId, remainingAmount, slot.RarityId));
            } else {
                ItemContainer.ItemSlots[buttonIndex].Clear();
            }
        } else {
            LastSlotId = buttonIndex;

            PlayerController.LocalInstance.PlayerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }

    public override void OnPlayerRightClick(int buttonIndex) {
        LastSlotId = buttonIndex;

        // If the drag item is active, call for the OnRightClick of the ItemDragAndDropManager 
        if (DragItemUI.Instance.gameObject.activeSelf) {
            PlayerController.LocalInstance.PlayerItemDragAndDropController.OnRightClick(ItemContainer.ItemSlots[buttonIndex]);
        }
    }
}
