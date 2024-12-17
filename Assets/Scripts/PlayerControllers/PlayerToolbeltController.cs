using System;
using System.Collections.ObjectModel;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls the player's toolbelt, handling tool selection, toolbelt size, and interactions.
/// </summary>
public class PlayerToolbeltController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerToolbeltController LocalInstance { get; private set; }

    // Events for toolbelt changes and toggling
    public event Action OnToolbeltChanged;
    public event Action OnToggleToolbelt;

    // Predefined toolbelt sizes
    private readonly int[] _toolbeltSizes = { 5, 7, 10 };
    public ReadOnlyCollection<int> ToolbeltSizes => Array.AsReadOnly(_toolbeltSizes);

    // Current toolbelt size and selected tool slot/belt
    private int _toolbeltSize = 10;
    public int ToolbeltSize => _toolbeltSize;

    // Constants
    private const int MAX_TOOLBELTS = 3;
    private const float ROTATION_STEP = 90f;

    // Current selections
    private int _selectedToolSlot = 0;
    private int _selectedToolbelt = 0;

    // Flags
    private bool _toolbeltSelectionBlocked = false;

    // Cached references
    private ToolbeltUI _visual;
    private InputManager _inputManager;
    private PlayerMovementController _playerMovement;
    private PlayerInventoryController _inventoryController;


    private void Start() {
        _visual = ToolbeltUI.Instance;
        _inputManager = InputManager.Instance;
        _playerMovement = GetComponent<PlayerMovementController>();
        _inventoryController = GetComponent<PlayerInventoryController>();

        // Subscribe to visual events
        _visual.OnToolbeltSlotLeftClick += OnToolbeltSlotLeftClick;

        // Subscribe to input actions using a loop to reduce redundancy
        for (int i = 1; i <= _toolbeltSizes[^1]; i++) {
            int slotIndex = i - 1; // Zero-based index
            _inputManager.SubscribeToolbeltSlotAction(i, () => SelectToolbeltSlot(slotIndex));
        }

        // Subscribe to other input actions
        _inputManager.OnDropItemAction += OnDropItemAction;

        // Subscribe to pause menu toggle
        PauseGameManager.Instance.OnShowLocalPauseGame += OnTogglePauseMenu;

        Initialize();
    }

    private new void OnDestroy() {
        base.OnDestroy();

        if (_visual != null) {
            _visual.OnToolbeltSlotLeftClick -= OnToolbeltSlotLeftClick;
        }

        if (_inputManager != null) {
            for (int i = 1; i <= _toolbeltSizes[^1]; i++) {
                _inputManager.UnsubscribeToolbeltSlotAction(i, () => SelectToolbeltSlot(i - 1));
            }

            _inputManager.OnDropItemAction -= OnDropItemAction;
        }

        if (PauseGameManager.Instance != null) {
            PauseGameManager.Instance.OnShowLocalPauseGame -= OnTogglePauseMenu;
        }
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of PlayerToolbeltController in the scene!");
                return;
            }
            LocalInstance = this;
        }
    }

    /// <summary>
    /// Initializes the player toolbelt controller.
    /// </summary>
    private void Initialize() {
        _visual.SetToolbeltSize(_toolbeltSize);
        _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    private void Update() {
        if (IsOwner && !_toolbeltSelectionBlocked) {
            ProcessToolSelection();
        }
    }

    #region Input Handling
    /// <summary>
    /// Processes the tool selection based on the mouse wheel input.
    /// </summary>
    private void ProcessToolSelection() {
        float mouseWheelDelta = _inputManager.GetMouseWheelVector().y;
        if (Mathf.Approximately(mouseWheelDelta, 0f)) {
            return;
        }

        bool isNext = mouseWheelDelta < 0f;
        bool isMaxSize = _toolbeltSize == _toolbeltSizes[^1];
        bool isCtrlPressed = _inputManager.GetLeftControlPressed();

        if (isMaxSize && isCtrlPressed) {
            ToggleToolbelt(isNext, mouseWheelDelta);
        } else {
            SelectTool(isNext);
        }

        OnToolbeltChanged?.Invoke();
    }

    /// <summary>
    /// Selects the next or previous tool in the toolbelt.
    /// </summary>
    /// <param name="isNext">True to select the next tool, false for the previous.</param>
    private void SelectTool(bool isNext) {
        _selectedToolSlot = isNext
            ? (_selectedToolSlot + 1) % _toolbeltSize
            : (_selectedToolSlot - 1 + _toolbeltSize) % _toolbeltSize;

        _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    /// <summary>
    /// Toggles the toolbelt to the next or previous set.
    /// </summary>
    /// <param name="isNext">True to select the next toolbelt, false for the previous.</param>
    /// <param name="mouseWheelDelta">The delta value of the mouse wheel input.</param>
    private void ToggleToolbelt(bool isNext, float mouseWheelDelta) {
        _selectedToolbelt = isNext
            ? (_selectedToolbelt + 1) % MAX_TOOLBELTS
            : (_selectedToolbelt - 1 + MAX_TOOLBELTS) % MAX_TOOLBELTS;

        float rotationAmount = isNext ? ROTATION_STEP : -ROTATION_STEP;
        _visual.ToolbeltChanged(_selectedToolbelt, rotationAmount);

        int shiftAmount = isNext ? _toolbeltSizes[^1] : -_toolbeltSizes[^1];
        _inventoryController.InventoryContainer.ShiftSlots(shiftAmount);

        _visual.ShowUIButtonContains();
    }

    /// <summary>
    /// Event handler for left-clicking a toolbelt slot in the UI.
    /// </summary>
    /// <param name="selectedToolbeltSlot">The index of the selected toolbelt slot.</param>
    private void OnToolbeltSlotLeftClick(int selectedToolbeltSlot) {
        if (selectedToolbeltSlot >= 0 && selectedToolbeltSlot < _toolbeltSize) {
            _selectedToolSlot = selectedToolbeltSlot;
            OnToolbeltChanged?.Invoke();
            _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
        }
    }

    /// <summary>
    /// Handles the action of dropping an item from the player's toolbelt.
    /// </summary>
    private void OnDropItemAction() {
        if (!IsOwner || DragItemUI.Instance.gameObject.activeSelf) {
            return;
        }

        ItemSlot toolbeltItem = GetCurrentlySelectedToolbeltItemSlot();
        if (toolbeltItem == null) {
            return;
        }

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(toolbeltItem.ItemId, 1, toolbeltItem.RarityId),
            initialPosition: transform.position,
            motionDirection: _playerMovement.LastMotionDirection,
            useInventoryPosition: true);

        _inventoryController.InventoryContainer.RemoveItem(new ItemSlot(toolbeltItem.ItemId, 1, toolbeltItem.RarityId));
    }

    /// <summary>
    /// Selects a toolbelt slot based on the provided index.
    /// </summary>
    /// <param name="slotIndex">Zero-based index of the toolbelt slot.</param>
    private void SelectToolbeltSlot(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < _toolbeltSize) {
            _selectedToolSlot = slotIndex;
            OnToolbeltChanged?.Invoke();
            _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
        }
    }
    #endregion

    #region Toolbelt Management
    /// <summary>
    /// Sets the toolbelt size based on the provided size ID.
    /// </summary>
    /// <param name="toolbeltSizeId">The ID representing the new toolbelt size.</param>
    public void SetToolbeltSize(int toolbeltSizeId) {
        return;

        if (toolbeltSizeId < 0 || toolbeltSizeId >= _toolbeltSizes.Length) {
            Debug.LogError("Invalid toolbelt size ID.");
            return;
        }

        _toolbeltSize = _toolbeltSizes[toolbeltSizeId];
        _visual.SetToolbeltSize(_toolbeltSize);
        _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
        InventoryUI.Instance.InventoryOrToolbeltSizeChanged();
    }

    /// <summary>
    /// Clears the currently selected item slot in the toolbelt.
    /// </summary>
    public void ClearCurrentItemSlot() {
        _inventoryController.InventoryContainer.ClearItemSlot(_selectedToolSlot);
        _visual.ShowUIButtonContains();
    }

    /// <summary>
    /// Retrieves the currently selected item slot in the toolbelt.
    /// </summary>
    /// <returns>The currently selected ItemSlot.</returns>
    public ItemSlot GetCurrentlySelectedToolbeltItemSlot() {
        if (_inventoryController.InventoryContainer.ItemSlots.Count > _selectedToolSlot) {
            return _inventoryController.InventoryContainer.ItemSlots[_selectedToolSlot];
        }

        return null;
    }

    /// <summary>
    /// Locks or unlocks the toolbelt selection.
    /// </summary>
    /// <param name="block">True to lock, false to unlock.</param>
    public void LockToolbelt(bool block) => _toolbeltSelectionBlocked = block;
    #endregion

    #region Pause Menu Handling
    /// <summary>
    /// Event handler for toggling the pause menu.
    /// </summary>
    private void OnTogglePauseMenu() => OnToggleToolbelt?.Invoke();
    #endregion


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.LastSelectedToolbeltSlot = _selectedToolSlot;
        playerData.ToolbeltSize = _toolbeltSize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _selectedToolSlot = Mathf.Clamp(playerData.LastSelectedToolbeltSlot, 0, _toolbeltSize - 1);
        _toolbeltSize = Mathf.Clamp(playerData.ToolbeltSize, _toolbeltSizes[0], _toolbeltSizes[^1]);

        _visual.SetToolbeltSize(_toolbeltSize);
        _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
    }
    #endregion
}
