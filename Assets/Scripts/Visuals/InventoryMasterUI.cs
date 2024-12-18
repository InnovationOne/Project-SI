using System;
using System.Collections.Generic;
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
    [SerializeField] SubPanel[] _subPanelsArray;
    [SerializeField] Button _closeButton;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI _inventoryText;

    public InventorySubUIs LastOpenPanel { get; private set; } = InventorySubUIs.None;

    Dictionary<InventorySubUIs, SubPanel> _subPanels;
    InputManager _iM;
    InventoryUI _iUI;
    ToolbeltUI _tUI;
    PlayerItemDragAndDropController _pIDADC;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one InventoryMasterUI instance in the scene!");
            return;
        }
        Instance = this;

        // Convert array to dictionary for safer lookups
        _subPanels = new Dictionary<InventorySubUIs, SubPanel>();
        foreach (var panel in _subPanelsArray) {
            _subPanels[panel.PanelType] = panel;
        }

        // Setup panel button listeners
        foreach (var kvp in _subPanels) {
            var subPanel = kvp.Value;
            if (subPanel.Button != null) {
                subPanel.Button.onClick.AddListener(() => SetSubPanel(subPanel.PanelType));
            }
        }

        if (_closeButton != null) {
            _closeButton.onClick.AddListener(() => ToggleInventory());
        }
    }

    void Start() {
        _iM = InputManager.Instance;
        _iUI = InventoryUI.Instance;
        _tUI = ToolbeltUI.Instance;
        SubscribeToInputEvents();

        DeactivateAllSubPanelsExcept(InventorySubUIs.Inventory);
        gameObject.SetActive(false);
    }

    void OnDestroy() {
        UnsubscribeFromInputEvents();

        // Remove listeners
        foreach (var kvp in _subPanels) {
            var subPanel = kvp.Value;
            if (subPanel.Button != null) {
                subPanel.Button.onClick.RemoveAllListeners();
            }
        }

        if (_closeButton != null) {
            _closeButton.onClick.RemoveAllListeners();
        }
    }

    #region -------------------- Initialization --------------------
    void SubscribeToInputEvents() {
        _iM.OnInventoryAction += HandleInventoryToggle;
        _iM.OnEscapeAction += HandleEscape;
    }

    void UnsubscribeFromInputEvents() {
        _iM.OnInventoryAction -= HandleInventoryToggle;
        _iM.OnEscapeAction -= HandleEscape;
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
        // If panel is already open, close instead
        if (targetPanel == LastOpenPanel && gameObject.activeSelf) {
            ToggleInventory();
            return;
        }

        // If inventory is closed, open it first
        if (!gameObject.activeSelf) {
            ToggleInventory();
        }

        DeactivateLastOpenPanel();
        ActivateTargetPanel(targetPanel);
        UpdateInventoryText(targetPanel);
        LastOpenPanel = targetPanel;
    }

    void DeactivateLastOpenPanel() {
        if (LastOpenPanel != InventorySubUIs.None && _subPanels.TryGetValue(LastOpenPanel, out var lastSubPanel)) {
            if (lastSubPanel.UIElement != null) {
                lastSubPanel.UIElement.SetActive(false);
            }
            ToggleButtonVisual(lastSubPanel, false);
        }
    }

    void ActivateTargetPanel(InventorySubUIs targetPanel) {
        if (targetPanel != InventorySubUIs.None && _subPanels.TryGetValue(targetPanel, out var targetSubPanel)) {
            if (targetSubPanel.UIElement != null) {
                targetSubPanel.UIElement.SetActive(true);
            }
            ToggleButtonVisual(targetSubPanel, true);
        }
    }

    void ToggleButtonVisual(SubPanel subPanel, bool isActive) {
        if (subPanel.Button.TryGetComponent<Inven_Main_A>(out var invenMainA)) {
            invenMainA.ToggleButtonVisual(isActive);
        }
    }

    void UpdateInventoryText(InventorySubUIs currentPanel) {
        if (_inventoryText != null) {
            _inventoryText.text = currentPanel.ToString();
        }
    }

    void DeactivateAllSubPanelsExcept(InventorySubUIs exception) {
        foreach (var kvp in _subPanels) {
            if (kvp.Key != exception && kvp.Value.UIElement != null) {
                kvp.Value.UIElement.SetActive(false);
            }
        }
    }

    void ToggleInventory(bool forceActive = false) {
        var newActiveState = forceActive || !gameObject.activeSelf;
        gameObject.SetActive(newActiveState);
        _tUI.ToggleToolbelt();

        // If dragging an item and closing the inventory, return the item
        if (!newActiveState && DragItemUI.Instance != null && DragItemUI.Instance.gameObject.activeSelf) {
            _pIDADC.AddDragItemBackIntoBackpack(
                _iUI != null ? _iUI.LastSlotId : -1
            );
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
