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

    [Header("Backpack & Inventory")]
    [SerializeField] private Image _backpackRarityIcon;
    [SerializeField] private Image _toolbeltRarityIcon;
    [SerializeField] private Sprite[] _rarityIconSprites;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of InventoryPanel in the scene!");
            return;
        }
        Instance = this;

        _sortButton.onClick.AddListener(() => PlayerInventoryController.LocalInstance.InventoryContainer.SortItems());
        _trashButton.onClick.AddListener(() => PlayerItemDragAndDropController.LocalInstance.ClearDragItem());
    }

    private void Start() {
        base.ItemContainerPanelAwake();

        ItemContainer.OnItemsUpdated += ShowUIButtonContains;

        Init();
    }

    public void InventoryOrToolbeltSizeChanged() {
        for (int i = 0; i < ItemButtons.Length; i++) {
            // Set the toolbelt slots
            if (i < PlayerToolbeltController.LocalInstance.ToolbeltSizes[2]) {
                if (i < PlayerToolbeltController.LocalInstance.ToolbeltSize) {
                    ItemButtons[i].GetComponent<Button>().interactable = true;
                    ItemButtons[i].GetComponent<Image>().raycastTarget = true;
                    continue;
                } else {
                    ItemButtons[i].GetComponent<Button>().interactable = false;
                    ItemButtons[i].GetComponent<Image>().raycastTarget = false;
                    continue;
                }
            }

            // Set the inventory slots
            if (i < (PlayerInventoryController.LocalInstance.CurrentInventorySize + PlayerToolbeltController.LocalInstance.ToolbeltSizes[^1])) {
                ItemButtons[i].GetComponent<Button>().interactable = true;
                ItemButtons[i].GetComponent<Image>().raycastTarget = true;
                continue;
            } else {
                ItemButtons[i].GetComponent<Button>().interactable = false;
                ItemButtons[i].GetComponent<Image>().raycastTarget = false;
                continue;
            }
        }

        // Show the correct inventory rarity icon
        _backpackRarityIcon.sprite = Array.IndexOf(PlayerInventoryController.LocalInstance.InventorySizes, PlayerInventoryController.LocalInstance.CurrentInventorySize) switch {
            0 => _rarityIconSprites[0],
            1 => _rarityIconSprites[1],
            _ => _rarityIconSprites[2],
        };

        // Show the correct toolbelt rarity icon
        _toolbeltRarityIcon.sprite = Array.IndexOf(PlayerToolbeltController.LocalInstance.ToolbeltSizes, PlayerToolbeltController.LocalInstance.ToolbeltSize) switch {
            0 => _rarityIconSprites[0],
            1 => _rarityIconSprites[1],
            _ => _rarityIconSprites[2],
        };
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        if (Input.GetKey(KeyCode.LeftShift)) {
            int remainingAmount;
            if (buttonIndex < PlayerToolbeltController.LocalInstance.ToolbeltSizes[2]) {
                remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], true);
            } else {
                remainingAmount = PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(ItemContainer.ItemSlots[buttonIndex], false);
            }

            if (remainingAmount > 0) {
                ItemContainer.ItemSlots[buttonIndex].Amount = remainingAmount;
            } else {
                ItemContainer.ItemSlots[buttonIndex].Clear();
            }
        } else {
            LastSlotId = buttonIndex;

            PlayerItemDragAndDropController.LocalInstance.OnLeftClick(ItemContainer.ItemSlots[buttonIndex]);
        }

        ShowUIButtonContains();
    }

    public override void OnPlayerRightClick(int buttonIndex) {
        LastSlotId = buttonIndex;

        // If the drag item is active, call for the OnRightClick of the ItemDragAndDropManager 
        if (DragItemUI.Instance.gameObject.activeSelf) {
            PlayerItemDragAndDropController.LocalInstance.OnRightClick(ItemContainer.ItemSlots[buttonIndex]);
        }
    }
}
