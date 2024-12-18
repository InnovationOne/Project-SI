using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventorySubUIs {
    Inventory,
    Tool,
    Heart,
    Food,
    Fish,
    Insect,
    Plant,
    Resource,
    None
}

[Serializable]
public struct SubPanel {
    public InventorySubUIs PanelType;
    public GameObject UIElement;
    public Button Button;
}

public class InventoryMasterUI : MonoBehaviour {
    public static InventoryMasterUI Instance { get; private set; }

    [Header("Standard Panels")]
    [SerializeField] SubPanel[] _subPanels;
    [SerializeField] Button _closeButton;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI _inventoryText;

    public InventorySubUIs LastOpenPanel { get; private set; } = InventorySubUIs.None;

    InputManager _iPM;


    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one InventoryMasterUI instance in the scene!");
            return;
        }
        Instance = this;

        InitializeSubPanelListeners();
        // TODO: //_closeButton.onClick.AddListener(ToggleInventory);
    }

    void Start() {
        _iPM = InputManager.Instance;
        SubscribeToInputEvents();

        DeactivateAllSubPanelsExcept(InventorySubUIs.Inventory);
        gameObject.SetActive(false);
    }

    void OnDestroy() {
        UnsubscribeFromInputEvents();
        RemoveSubPanelListeners();
    }

    #region -------------------- Initialization --------------------
    void InitializeSubPanelListeners() {
        foreach (var subPanel in _subPanels) {
            subPanel.Button.onClick.AddListener(() => SetSubPanel(subPanel.PanelType));
        }
    }

    void RemoveSubPanelListeners() {
        foreach (var subPanel in _subPanels) {
            subPanel.Button.onClick.RemoveAllListeners();
        }
        // TODO: //_closeButton.onClick.RemoveAllListeners();
    }

    void SubscribeToInputEvents() {
        _iPM.OnInventoryAction += HandleInventoryToggle;
        _iPM.OnEscapeAction += HandleEscape;
    }

    void UnsubscribeFromInputEvents() {
        _iPM.OnInventoryAction -= HandleInventoryToggle;
        _iPM.OnEscapeAction -= HandleEscape;
    }
    #endregion -------------------- Initialization --------------------

    #region -------------------- Input Handlers --------------------
    public void HandleInventoryToggle() {
        if (LastOpenPanel == InventorySubUIs.None) {
            SetSubPanel(InventorySubUIs.Inventory);
        } else {
            ToggleInventory();
        }
    }

    void HandleEscape() {
        if (gameObject.activeSelf) {
            ToggleInventory();
        }
    }
    #endregion -------------------- Input Handlers --------------------

    #region -------------------- Panel Management --------------------
    public void SetSubPanel(InventorySubUIs targetPanel) {
        // Close when same panel is clicked
        if (targetPanel == LastOpenPanel && gameObject.activeSelf) {
            ToggleInventory();
            return;
        }

        // Open panel when closed
        if (!gameObject.activeSelf) {
            ToggleInventory();
        }

        DeactivateLastOpenPanel();
        ActivateTargetPanel(targetPanel);
        UpdateInventoryText(targetPanel);
        LastOpenPanel = targetPanel;
    }

    void DeactivateLastOpenPanel() {
        if (LastOpenPanel != InventorySubUIs.None) {
            var lastSubPanel = _subPanels[(int)LastOpenPanel];
            lastSubPanel.UIElement.SetActive(false);
            ToggleButtonVisual(lastSubPanel, false);
        }
    }

    void ActivateTargetPanel(InventorySubUIs targetPanel) {
        if (targetPanel != InventorySubUIs.None) {
            var targetSubPanel = _subPanels[(int)targetPanel];
            targetSubPanel.UIElement.SetActive(true);
            ToggleButtonVisual(targetSubPanel, true);
        }
    }

    void ToggleButtonVisual(SubPanel subPanel, bool isActive) {
        Transform bT = subPanel.Button.transform;
        bT.GetChild(0).gameObject.SetActive(!isActive);
        bT.GetChild(1).gameObject.SetActive(!isActive);
        bT.GetChild(2).gameObject.SetActive(isActive);
        bT.GetChild(3).gameObject.SetActive(isActive);
    }

    void UpdateInventoryText(InventorySubUIs currentPanel) => _inventoryText.text = currentPanel.ToString();

    void DeactivateAllSubPanelsExcept(InventorySubUIs exception) {
        foreach (var subPanel in _subPanels) {
            if (subPanel.PanelType != exception) {
                subPanel.UIElement.SetActive(false);
            }
        }
    }

    void ToggleInventory() {
        gameObject.SetActive(!gameObject.activeSelf);
        ToolbeltUI.Instance.ToggleToolbelt();

        if (DragItemUI.Instance.gameObject.activeSelf) {
            var inventoryUI = _subPanels[(int)InventorySubUIs.Inventory].UIElement.GetComponent<InventoryUI>();
            PlayerItemDragAndDropController.LocalInstance.AddDragItemBackIntoBackpack(inventoryUI.LastSlotId);
        }
    }
    #endregion -------------------- Panel Management --------------------

    /*
    #region Chest
    public void ShowChestPanel() {
        //OnChestPanelToggled?.Invoke(!_chestPanel.gameObject.activeSelf);

        ToggleMasterPanel();

        for (int i = 1; i < _subPanels.Length; i++) {
            _subPanels[i].SetActive(false);
        }
        LastOpenPanel = InventorySubUIs.Inventory;

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
        LastOpenPanel = InventorySubUIs.Inventory;

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
    */
}
