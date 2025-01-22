using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolbeltUI : ItemContainerUI {
    public static ToolbeltUI Instance { get; private set; }
    public Action<int> OnToolbeltSlotLeftClick;

    [Header("Selection wheel")]
    [SerializeField] Image _toolbeltSelectionWheelSelectedImage;
    [SerializeField] TextMeshProUGUI _toolbeltSelectionWheelSelectedText;

    [Header("Inventory Button")]
    [SerializeField] Button _inventoryButton;
    [SerializeField] Sprite[] _rarityIconSprites;

    int _lastSelectedSlot;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ToolbeltPanel in the scene!");
            return;
        }
        Instance = this;

        _inventoryButton.onClick.AddListener(() => InventoryMasterUI.Instance.HandleInventoryToggle());
    }

    private void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
    }

    // Adjusts toolbelt size.
    public void SetToolbeltSize(int toolbeltSize) {
        for (int i = 0; i < ItemButtons.Length; i++) {
            ItemButtons[i].SetInteractable(i < toolbeltSize);
        }
    }

    // Visually highlights the currently selected toolbelt slot.
    public void SetToolbeltSlotHighlight(int currentlySelectedSlot) {
        ItemButtons[_lastSelectedSlot].SetButtonHighlight(false);
        _lastSelectedSlot = currentlySelectedSlot;
        ItemButtons[currentlySelectedSlot].SetButtonHighlight(true);
    }

    // Rotates the selection wheel UI and updates the displayed slot number.
    public void ToolbeltChanged(int selectedToolbelt, float rotation) {
        _toolbeltSelectionWheelSelectedImage.transform.Rotate(0f, 0f, -rotation);
        _toolbeltSelectionWheelSelectedText.text = (selectedToolbelt + 1).ToString();
    }

    // Toggles the entire toolbelt UI on or off.
    public void ToggleToolbelt() {
        gameObject.SetActive(!gameObject.activeSelf);

        if (gameObject.activeSelf) {
            ShowUIButtonContains();
        }
    }

    public override void OnPlayerLeftClick(int selectedToolbeltSlot) {
        OnToolbeltSlotLeftClick?.Invoke(selectedToolbeltSlot);
        SetToolbeltSlotHighlight(selectedToolbeltSlot);
    }
}
