using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls the player's toolbelt, handling tool selection, toolbelt size, and interactions.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerToolbeltController : NetworkBehaviour, IPlayerDataPersistance {
    // Notifies changes in toolbelt setup and toggle actions
    public event Action OnToolbeltChanged;
    public event Action OnToolbeltSlotChanged;
    public event Action OnToggleToolbelt;

    // Predefined toolbelt sizes (read-only)
    static readonly ReadOnlyCollection<int> TOOLBELT_SIZES = Array.AsReadOnly(new int[] { 5, 7, 10 });
    public ReadOnlyCollection<int> ToolbeltSizes => TOOLBELT_SIZES;

    // Current toolbelt size and selections
    int _toolbeltSize = TOOLBELT_SIZES[^1];
    public int CurrentToolbeltSize => _toolbeltSize;

    // Max sets of toolbelts and rotation step for switching sets
    const int MAX_TOOLBELTS = 3;
    const float ROTATION_STEP = 90f;

    // Currently selected slot and toolbelt set
    int _selectedToolSlot = 0;
    int _selectedToolbelt = 0;

    // If true, prevents changing toolbelt selection
    bool _toolbeltSelectionBlocked = false;

    // Cached references for quick access
    ToolbeltUI _toolbeltUI;
    InventoryUI _inventoryUI;
    InputManager _inputManager;
    PlayerMovementController _playerMovement;
    PlayerInventoryController _inventoryController;
    PauseGameManager _pauseGameManager;

    // Store delegates for slot actions to ensure proper unsubscribe
    Action[] _toolbeltSlotActions;

    void Start() {
        _toolbeltUI = ToolbeltUI.Instance;
        _inventoryUI = InventoryUI.Instance;
        _inputManager = GameManager.Instance.InputManager;
        _pauseGameManager = GameManager.Instance.PauseGameManager;
        _playerMovement = GetComponent<PlayerMovementController>();
        _inventoryController = GetComponent<PlayerInventoryController>();

        // Subscribe to UI slot clicks
        _toolbeltUI.OnToolbeltSlotLeftClick += OnToolbeltSlotLeftClick;

        // Prepare and subscribe slot selection actions
        int maxSlots = TOOLBELT_SIZES[^1];
        _toolbeltSlotActions = new Action[maxSlots];
        for (int i = 1; i < maxSlots; i++) {
            int slotIndex = i - 1;
            _toolbeltSlotActions[slotIndex] = () => SelectToolbeltSlot(slotIndex);
            _inputManager.SubscribeToolbeltSlotAction(i, _toolbeltSlotActions[slotIndex]);
        }

        // Subscribe to drop item action
        _inputManager.OnDropItemAction += OnDropItemAction;

        // Subscribe to pause menu toggling
        _pauseGameManager.OnShowLocalPauseGame += OnTogglePauseMenu;

        Initialize();

        // Invoke for first automatic selection
        OnToolbeltSlotChanged?.Invoke();
    }

    new void OnDestroy() {
        // Unsubscribe UI events
        if (_toolbeltUI != null) {
            _toolbeltUI.OnToolbeltSlotLeftClick -= OnToolbeltSlotLeftClick;
        }

        // Unsubscribe input events
        if (_inputManager != null && _toolbeltSlotActions != null) {
            int maxSlots = TOOLBELT_SIZES[^1];
            for (int i = 1; i <= maxSlots; i++) {
                int slotIndex = i - 1;
                _inputManager.UnsubscribeToolbeltSlotAction(i, _toolbeltSlotActions[slotIndex]);
            }
            _inputManager.OnDropItemAction -= OnDropItemAction;
        }

        // Unsubscribe pause menu events
        _pauseGameManager.OnShowLocalPauseGame -= OnTogglePauseMenu;

        base.OnDestroy();
    }

    // Sets initial visual state for toolbelt
    void Initialize() {
        _toolbeltUI.SetToolbeltSize(_toolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    void Update() {
        // Handle tool selection only if local owner and not blocked
        if (IsOwner && !_toolbeltSelectionBlocked) {
            ProcessToolSelection();
        }
    }

    #region -------------------- Input Handling --------------------
    // Handles mouse wheel input for switching tools or toolbelts
    private void ProcessToolSelection() {
        float mouseWheelDelta = _inputManager.GetMouseWheelVector().y;
        if (Mathf.Approximately(mouseWheelDelta, 0f)) return;

        bool isNext = mouseWheelDelta < 0f;
        bool isMaxSize = _toolbeltSize == TOOLBELT_SIZES[^1];
        bool isCtrlPressed = _inputManager.GetLeftControlPressed();

        if (isMaxSize && isCtrlPressed) {
            ToggleToolbelt(isNext);
        } else {
            SelectTool(isNext);
        }

        OnToolbeltChanged?.Invoke();
    }

    // Selects the next or previous tool slot
    private void SelectTool(bool isNext) {
        _selectedToolSlot = isNext
            ? (_selectedToolSlot + 1) % _toolbeltSize
            : (_selectedToolSlot - 1 + _toolbeltSize) % _toolbeltSize;

        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        OnToolbeltSlotChanged?.Invoke();
    }

    // Rotates through available toolbelts and shifts inventory slots
    private void ToggleToolbelt(bool isNext) {
        _selectedToolbelt = isNext
            ? (_selectedToolbelt + 1) % MAX_TOOLBELTS
            : (_selectedToolbelt - 1 + MAX_TOOLBELTS) % MAX_TOOLBELTS;

        float rotationAmount = isNext ? ROTATION_STEP : -ROTATION_STEP;
        _toolbeltUI.ToolbeltChanged(_selectedToolbelt, rotationAmount);

        int shiftAmount = isNext ? TOOLBELT_SIZES[^1] : -TOOLBELT_SIZES[^1];
        _inventoryController.InventoryContainer.ShiftSlots(shiftAmount);

        _toolbeltUI.ShowUIButtonContains();
        OnToolbeltSlotChanged?.Invoke();
    }

    // Handles slot selection from UI clicks
    private void OnToolbeltSlotLeftClick(int selectedToolbeltSlot) {
        if (selectedToolbeltSlot >= 0 && selectedToolbeltSlot < _toolbeltSize) {
            _selectedToolSlot = selectedToolbeltSlot;
            OnToolbeltChanged?.Invoke();
            _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
            OnToolbeltSlotChanged?.Invoke();
        }
    }

    // Handles dropping an item from the currently selected tool slot
    private void OnDropItemAction() {
        if (!IsOwner || DragItemUI.Instance.gameObject.activeSelf) return;

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

    // Allows external calls to directly set selected slot
    private void SelectToolbeltSlot(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < _toolbeltSize) {
            _selectedToolSlot = slotIndex;
            OnToolbeltChanged?.Invoke();
            _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
            OnToolbeltSlotChanged?.Invoke();
        }
    }
    #endregion -------------------- Input Handling --------------------

    #region -------------------- Toolbelt --------------------
    // Sets a new toolbelt size based on a predefined size ID
    public void SetToolbeltSize(int toolbeltSizeId) {
        if (toolbeltSizeId < 0 || toolbeltSizeId >= TOOLBELT_SIZES.Count) {
            Debug.LogError("Invalid toolbelt size ID.");
            return;
        }

        _toolbeltSize = TOOLBELT_SIZES[toolbeltSizeId];
        _toolbeltUI.SetToolbeltSize(_toolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
        _inventoryUI.InventoryOrToolbeltSizeChanged();
    }

    // Clears the currently selected slot
    public void ClearCurrentItemSlot() {
        _inventoryController.InventoryContainer.ClearItemSlot(_selectedToolSlot);
        _toolbeltUI.ShowUIButtonContains();
    }

    // Retrieves the currently selected item slot, if any
    public ItemSlot GetCurrentlySelectedToolbeltItemSlot() {
        var slots = _inventoryController.InventoryContainer.ItemSlots;
        return (slots != null && slots.Count > _selectedToolSlot) ? slots[_selectedToolSlot] : null;
    }

    // Locks or unlocks slot selection
    public void LockToolbelt(bool block) => _toolbeltSelectionBlocked = block;
    
    // Toggles toolbelt display when pause menu shows
    private void OnTogglePauseMenu() => OnToggleToolbelt?.Invoke();
    #endregion -------------------- Toolbelt --------------------

    #region -------------------- Save & Load --------------------
    public void SavePlayer(PlayerData playerData) {
        playerData.LastSelectedToolbeltSlot = _selectedToolSlot;
        playerData.ToolbeltSize = _toolbeltSize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _selectedToolSlot = Mathf.Clamp(playerData.LastSelectedToolbeltSlot, 0, _toolbeltSize - 1);
        _toolbeltSize = Mathf.Clamp(playerData.ToolbeltSize, TOOLBELT_SIZES[0], TOOLBELT_SIZES[^1]);

        _toolbeltUI.SetToolbeltSize(_toolbeltSize);
        _toolbeltUI.SetToolbeltSlotHighlight(_selectedToolSlot);
    }
    #endregion -------------------- Save & Load --------------------
}
