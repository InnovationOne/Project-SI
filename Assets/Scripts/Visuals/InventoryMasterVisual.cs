using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventorySubPanels {
    Inventory,
    Crafting,
    Relationships,
    Wiki,
    Wardrobe,
    none,
}

// The script manages the player's inventory
public class InventoryMasterVisual : MonoBehaviour {
    public static InventoryMasterVisual Instance { get; private set; }

    //public event Action<bool> OnChestPanelToggled;


    [Header("Standard Panels")]
    [SerializeField] private GameObject[] _subPanels;
    [SerializeField] private Button[] _inventoryButtons;
    [SerializeField] private Image[] _inventoryButtonsSelected;
    [SerializeField] private TextMeshProUGUI _inventoryText;

    [Header("Close Button")]
    [SerializeField] private Button _closeButton;

    public InventorySubPanels LastOpenPanel { get; private set; } = InventorySubPanels.none;

    [Header("Chest Panel")]
    [SerializeField] private ChestUI _chestPanel;

    [Header("Store Panel")]
    [SerializeField] private StoreVisual _storePanel;

    private bool _keyPressed = false;


    private void Awake() {
        Instance = this;

        _inventoryButtons[0].onClick.AddListener(() => SetSubPanel(InventorySubPanels.Crafting));
        _inventoryButtons[1].onClick.AddListener(() => SetSubPanel(InventorySubPanels.Relationships));
        _inventoryButtons[2].onClick.AddListener(() => SetSubPanel(InventorySubPanels.Wiki));
        _inventoryButtons[3].onClick.AddListener(() => SetSubPanel(InventorySubPanels.Wardrobe));

        _closeButton.onClick.AddListener(() => {
            if (_chestPanel.gameObject.activeSelf) {
                // Chest
                CloseChestWithButton();
            } else if (_storePanel.gameObject.activeSelf) {
                // Store
                //PlayerInteractController.LocalInstance.InteractAction();
            } else {
                // Other
                ToggleMasterPanel();
                LastOpenPanel = InventorySubPanels.none;
            }
        });
    }

    private void Start() {
        InputManager.Instance.OnInventoryAction += InputManager_OnInventoryAction;
        InputManager.Instance.OnCraftAction += InputManager_OnCraftAction;
        InputManager.Instance.OnRelationAction += InputManager_OnRelationAction;
        InputManager.Instance.OnCollectionAction += InputManager_OnCollectionAction;
        InputManager.Instance.OnCharacterAction += InputManager_OnCharacterAction;
        InputManager.Instance.OnEscapeAction += InputManager_OnReturnAction;

        //_subPanels[3].GetComponent<WikiPanel>().StartForWikiVisual();
        for (int i = 1; i < _subPanels.Length; i++) {
            _subPanels[i].SetActive(false);
        }
        //_chestPanel.gameObject.SetActive(false);
        _storePanel.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }


    #region PlayerInput
    private void InputManager_OnInventoryAction() {
        _keyPressed = true;
        if (LastOpenPanel == InventorySubPanels.none || LastOpenPanel == InventorySubPanels.Inventory) {
            SetSubPanel(InventorySubPanels.Crafting);
        } else {
            SetSubPanel(LastOpenPanel);
        }
    }

    private void InputManager_OnCraftAction() {
        _keyPressed = true;
        SetSubPanel(InventorySubPanels.Crafting);
    }

    private void InputManager_OnRelationAction() {
        _keyPressed = true;
        SetSubPanel(InventorySubPanels.Relationships);
    }

    private void InputManager_OnCollectionAction() {
        _keyPressed = true;
        SetSubPanel(InventorySubPanels.Wiki);
    }

    private void InputManager_OnCharacterAction() {
        _keyPressed = true;
        SetSubPanel(InventorySubPanels.Wardrobe);
    }

    private void InputManager_OnReturnAction() {
        if (gameObject.activeSelf) {
            ToggleMasterPanel();
        }
    }
    #endregion


    public void SetSubPanel(InventorySubPanels subPanel) {
        if (_chestPanel.gameObject.activeSelf || _storePanel.gameObject.activeSelf) {
            return;
        }

        // Set inventory
        if (subPanel == LastOpenPanel && _keyPressed) {
            ToggleMasterPanel();
            LastOpenPanel = InventorySubPanels.none;
            _keyPressed = false;
        } else if (!gameObject.activeSelf) {
            ToggleMasterPanel();
        }

        // Set sub panel
        if (LastOpenPanel != InventorySubPanels.none && LastOpenPanel != InventorySubPanels.Inventory) {
            _inventoryButtonsSelected[(int)LastOpenPanel - 1].gameObject.SetActive(false);
            _subPanels[(int)LastOpenPanel].SetActive(false);
        }
        _inventoryButtonsSelected[(int)subPanel - 1].gameObject.SetActive(true);
        _subPanels[(int)subPanel].SetActive(true);

        // Update last open panel
        LastOpenPanel = subPanel;

        _inventoryText.text = subPanel.ToString() + " & Inventory";
    }

    private void ToggleMasterPanel() {
        gameObject.SetActive(!gameObject.activeSelf);
        ToolbeltUI.Instance.ToggleToolbelt();

        if (DragItemUI.Instance.gameObject.activeSelf) {
            PlayerItemDragAndDropController.LocalInstance.AddDragItemBackIntoBackpack(_subPanels[(int)InventorySubPanels.Inventory].GetComponent<InventoryUI>().LastSlotId);
        }
    }


    #region Chest
    public void ShowChestPanel() {
        //OnChestPanelToggled?.Invoke(!_chestPanel.gameObject.activeSelf);

        ToggleMasterPanel();

        for (int i = 1; i < _subPanels.Length; i++) {
            _subPanels[i].SetActive(false);
        }
        LastOpenPanel = InventorySubPanels.Inventory;

        foreach (Button button in _inventoryButtons) {
            button.gameObject.SetActive(false);
        }

        _chestPanel.gameObject.SetActive(true);

        _inventoryText.text = "Chest & Inventory";
    }

    private void CloseChestWithButton() {
        HideChestPanel();

        _chestPanel.HideChest();
    }

    public void HideChestPanel() {
        //OnChestPanelToggled?.Invoke(_chestPanel.gameObject.activeSelf);
        ToggleMasterPanel();

        foreach (Button button in _inventoryButtons) {
            button.gameObject.SetActive(true);
        }

        _chestPanel.gameObject.SetActive(false);
    }
    #endregion


    #region Store
    public void ToggleStorePanel() {
        if (_storePanel.gameObject.activeSelf) {
            HideStorePanel();
        } else {
            ShowStorePanel();
        }
    }

    private void ShowStorePanel() {
        ToggleMasterPanel();

        for (int i = 1; i < _subPanels.Length; i++) {
            _subPanels[i].SetActive(false);
        }
        LastOpenPanel = InventorySubPanels.Inventory;

        foreach (Button button in _inventoryButtons) {
            button.gameObject.SetActive(false);
        }

        _storePanel.gameObject.SetActive(true);

        _inventoryText.text = "Store & Inventory";
    }

    private void HideStorePanel() {
        ToggleMasterPanel();

        foreach (Button button in _inventoryButtons) {
            button.gameObject.SetActive(true);
        }

        _storePanel.gameObject.SetActive(false);
    }
    #endregion
}
