using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

public class PlayerToolbeltController : MonoBehaviour, IPlayerDataPersistance {
    // Events for external systems to subscribe.
    public event Action OnToolbeltChanged;
    public event Action OnToolbeltSlotChanged;
    public event Action OnToggleToolbelt;

    public ReadOnlyCollection<int> ToolbeltSizes { get; private set; } = Array.AsReadOnly(new int[] { 5, 7, 10 });
    public int CurrentToolbeltSize { get; private set; }

    const int MAX_TOOLBELTS = 3;
    int _selectedToolSlot = 0;
    int _selectedToolbelt = 0;
    bool _toolbeltSelectionBlocked = false;

    ToolbeltUI _toolbeltUI;
    InventoryUI _inventoryUI;
    InputManager _inputManager;
    PlayerMovementController _playerMovement;
    PlayerInventoryController _inventoryController;
    PauseGameManager _pauseGameManager;
    Action[] _toolbeltSlotActions;

    #region -------------------- Unity Lifecycle --------------------

    void Start() {
        _toolbeltUI = ToolbeltUI.Instance;
        _inventoryUI = InventoryUI.Instance;
        _inputManager = GameManager.Instance.InputManager;
        _pauseGameManager = GameManager.Instance.PauseGameManager;
        _playerMovement = GetComponent<PlayerMovementController>();
        _inventoryController = GetComponent<PlayerInventoryController>();

        _toolbeltUI.OnToolbeltSlotLeftClick += OnToolbeltSlotLeftClick;

        int maxSlots = ToolbeltSizes[^1];
        _toolbeltSlotActions = new Action[maxSlots];
        for (int i = 1; i < maxSlots; i++) {
            int slotIndex = i - 1;
            _toolbeltSlotActions[slotIndex] = () => SelectToolbeltSlot(slotIndex);
            _inputManager.SubscribeToolbeltSlotAction(i, _toolbeltSlotActions[slotIndex]);
        }

        _inputManager.OnDropItemAction += OnDropItemAction;
        _pauseGameManager.OnShowLocalPauseGame += OnTogglePauseMenu;

        CurrentToolbeltSize = ToolbeltSizes[^1];
        Init();
        OnToolbeltSlotChanged?.Invoke();
    }

    void OnDestroy() {
        if (_toolbeltUI != null) _toolbeltUI.OnToolbeltSlotLeftClick -= OnToolbeltSlotLeftClick;
        if (_inputManager != null && _toolbeltSlotActions != null) {
            int maxSlots = ToolbeltSizes[^1];
            for (int i = 1; i <= maxSlots; i++) {
                int slotIndex = i - 1;
                _inputManager.UnsubscribeToolbeltSlotAction(i, _toolbeltSlotActions[slotIndex]);
            }
            _inputManager.OnDropItemAction -= OnDropItemAction;
        }
        _pauseGameManager.OnShowLocalPauseGame -= OnTogglePauseMenu;
    }

    // Sets the initial UI state.
    void Init() {
        _toolbeltUI.SetToolbeltSize(CurrentToolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    void Update() {
        if (_toolbeltSelectionBlocked) return;
        ProcessToolSelection();
    }

    #endregion -------------------- Unity Lifecycle --------------------

    #region -------------------- Input Handling --------------------

    // Checks mouse wheel for next/prev tool or toolbelt.
    void ProcessToolSelection() {
        float mouseWheelDelta = _inputManager.GetMouseWheelVector().y;
        if (Mathf.Approximately(mouseWheelDelta, 0f)) return;

        bool isNext = mouseWheelDelta < 0f;
        bool isMaxSize = CurrentToolbeltSize == ToolbeltSizes[^1];
        bool isCtrlPressed = _inputManager.GetLeftControlPressed();

        if (isMaxSize && isCtrlPressed) ToggleToolbelt(isNext);
        else SelectTool(isNext);
        OnToolbeltChanged?.Invoke();
    }

    // Moves selection in the active toolbelt.
    void SelectTool(bool isNext) {
        _selectedToolSlot = isNext
            ? (_selectedToolSlot + 1) % CurrentToolbeltSize
            : (_selectedToolSlot - 1 + CurrentToolbeltSize) % CurrentToolbeltSize;

        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        OnToolbeltSlotChanged?.Invoke();
    }

    // Cycles through multiple toolbelt configurations.
    public void ToggleToolbelt(bool isNext) {
        _selectedToolbelt = isNext
            ? (_selectedToolbelt + 1) % MAX_TOOLBELTS
            : (_selectedToolbelt - 1 + MAX_TOOLBELTS) % MAX_TOOLBELTS;

        _toolbeltUI.ToolbeltChanged(_selectedToolbelt);
        int shiftAmount = isNext ? ToolbeltSizes[^1] : -ToolbeltSizes[^1];
        _inventoryController.InventoryContainer.ShiftSlots(shiftAmount);

        _toolbeltUI.ShowUIButtonContains();
        OnToolbeltSlotChanged?.Invoke();
    }

    // Called when a toolbelt slot is clicked via UI.
    void OnToolbeltSlotLeftClick(int selectedToolbeltSlot) {
        if (selectedToolbeltSlot < 0 || selectedToolbeltSlot >= CurrentToolbeltSize) return;
        _selectedToolSlot = selectedToolbeltSlot;
        OnToolbeltChanged?.Invoke();
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        OnToolbeltSlotChanged?.Invoke();

    }

    // Drops an item from the selected slot if drag UI is inactive.
    void OnDropItemAction() {
        if (DragItemUI.Instance.gameObject.activeSelf) return;
        var toolbeltItem = GetCurrentlySelectedToolbeltItemSlot();
        if (toolbeltItem == null) return;

        var itemSlot = new ItemSlot(toolbeltItem.ItemId, 1, toolbeltItem.RarityId);
        GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
            itemSlot,
            transform.position,
            _playerMovement.LastMotionDirection,
            true);

        _inventoryController.InventoryContainer.RemoveItem(itemSlot);
    }

    // Allows external systems or keys to select a toolbelt slot directly.
    void SelectToolbeltSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= CurrentToolbeltSize) return;
        _selectedToolSlot = slotIndex;
        OnToolbeltChanged?.Invoke();
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        OnToolbeltSlotChanged?.Invoke();

    }
    #endregion -------------------- Input Handling --------------------

    #region -------------------- Toolbelt --------------------

    // Sets a new toolbelt size and updates the UI.
    public void SetToolbeltSize(int toolbeltSizeId) {
        if (toolbeltSizeId < 0 || toolbeltSizeId >= ToolbeltSizes.Count) return;
        CurrentToolbeltSize = ToolbeltSizes[toolbeltSizeId];
        _toolbeltUI.SetToolbeltSize(CurrentToolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        _inventoryUI.InventoryOrToolbeltSizeChanged();
    }

    // Clears the item in the current toolbelt slot.
    public void ClearCurrentItemSlot() {
        _inventoryController.InventoryContainer.ClearItemSlot(_selectedToolSlot);
        _toolbeltUI.ShowUIButtonContains();
    }

    // Retrieves the currently selected item slot.
    public ItemSlot GetCurrentlySelectedToolbeltItemSlot() {
        var slots = _inventoryController.InventoryContainer.ItemSlots;
        return (slots != null && slots.Count > _selectedToolSlot) 
            ? slots[_selectedToolSlot] 
            : null;
    }

    // Enables or disables toolbelt selection.
    public void LockToolbelt(bool block) => _toolbeltSelectionBlocked = block;

    // Toggles toolbelt display when the pause menu is activated.
    void OnTogglePauseMenu() => OnToggleToolbelt?.Invoke();

    #endregion -------------------- Toolbelt --------------------

    #region -------------------- Save & Load --------------------

    public void SavePlayer(PlayerData playerData) {
        playerData.LastSelectedToolbeltSlot = _selectedToolSlot;
        playerData.ToolbeltSize = CurrentToolbeltSize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _selectedToolSlot = Mathf.Clamp(playerData.LastSelectedToolbeltSlot, 0, CurrentToolbeltSize - 1);
        CurrentToolbeltSize = Mathf.Clamp(playerData.ToolbeltSize, ToolbeltSizes[0], ToolbeltSizes[^1]);
        _toolbeltUI.SetToolbeltSize(CurrentToolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    #endregion -------------------- Save & Load --------------------
}
