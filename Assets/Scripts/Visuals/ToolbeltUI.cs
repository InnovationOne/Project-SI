using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolbeltUI : ItemContainerUI {
    public static ToolbeltUI Instance { get; private set; }

    public Action<int> OnToolbeltSlotLeftClick;

    [Header("Selection wheel")]
    [SerializeField] private Image _toolbeltSelectionWheelSelectedImage;
    [SerializeField] private TextMeshProUGUI _toolbeltSelectionWheelSelectedText;

    [Header("Inventory Button")]
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private Sprite[] _rarityIconSprites;

    private int _lastSelectedTool;


    private void Awake() {
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

    public void SetToolbeltSize(int toolbeltSize) {

        // TODO: Get the right image for unlocked and locked and set it based on the size of the toolbelt
        /*
        for (int i = 0; i < ItemButtons.Length; i++) {
            if (i < toolbeltSize) {
                ItemButtons[i].GetComponent<Button>().interactable = true;
                ItemButtons[i].GetComponent<Image>().raycastTarget = true;
            } else {
                ItemButtons[i].GetComponent<Button>().interactable = false;
                ItemButtons[i].GetComponent<Image>().raycastTarget = false;
            }
        }
        */
    }

    public void SetToolbeltSlotHighlight(int currentlySelectedTool) {
        ItemButtons[_lastSelectedTool].GetComponent<InventorySlot>().SetButtonHighlight(false);

        _lastSelectedTool = currentlySelectedTool;

        ItemButtons[currentlySelectedTool].GetComponent<InventorySlot>().SetButtonHighlight(true);
    }

    public void ToolbeltChanged(int selectedToolbelt, float rotation) {
        _toolbeltSelectionWheelSelectedImage.transform.Rotate(0f, 0f, -rotation);

        _toolbeltSelectionWheelSelectedText.text = (selectedToolbelt + 1).ToString();
    }

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
