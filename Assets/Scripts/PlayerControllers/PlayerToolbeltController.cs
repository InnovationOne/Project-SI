using System;
using Unity.Netcode;
using UnityEngine;

// This script controlls the players toolbelt
public class PlayerToolbeltController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerToolbeltController LocalInstance { get; private set; }

    public event Action OnToolbeltChanged;
    public event Action OnToggleToolbelt;

    private readonly int[] _toolbeltSizes = { 5, 7, 10 };
    public int[] ToolbeltSizes => _toolbeltSizes;

    private int _toolbeltSize = 10;
    public int ToolbeltSize => _toolbeltSize;


    private const int MAX_TOOLBELTS = 3;
    private int _selectedToolSlot = 0;
    private int _selectedToolbelt = 0;
    private const float _rotationStep = 90f;
    private bool _toolbeltSelectionBlocked = false;

    private ToolbeltUI _visual;
    private InputManager _inputManager;


    private void Start() {
        _visual = ToolbeltUI.Instance;
        _inputManager = InputManager.Instance;

        _visual.OnToolbeltSlotLeftClick += ToolbeltVisual_OnToolbeltSlotLeftClick;
        _inputManager.OnDropItemAction += InputManager_OnDropItemAction;
        _inputManager.OnToolbeltSlot1Action += InputManager_OnToolbeltSlot1Action;
        _inputManager.OnToolbeltSlot2Action += InputManager_OnToolbeltSlot2Action;
        _inputManager.OnToolbeltSlot3Action += InputManager_OnToolbeltSlot3Action;
        _inputManager.OnToolbeltSlot4Action += InputManager_OnToolbeltSlot4Action;
        _inputManager.OnToolbeltSlot5Action += InputManager_OnToolbeltSlot5Action;
        _inputManager.OnToolbeltSlot6Action += InputManager_OnToolbeltSlot6Action;
        _inputManager.OnToolbeltSlot7Action += InputManager_OnToolbeltSlot7Action;
        _inputManager.OnToolbeltSlot8Action += InputManager_OnToolbeltSlot8Action;
        _inputManager.OnToolbeltSlot9Action += InputManager_OnToolbeltSlot9Action;
        _inputManager.OnToolbeltSlot10Action += InputManager_OnToolbeltSlot10Action;

        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuController_OnTogglePauseMenu;

        Initialize();
    }

    private new void OnDestroy() {
        _visual.OnToolbeltSlotLeftClick -= ToolbeltVisual_OnToolbeltSlotLeftClick;

        _inputManager.OnDropItemAction -= InputManager_OnDropItemAction;
        _inputManager.OnToolbeltSlot1Action -= InputManager_OnToolbeltSlot1Action;
        _inputManager.OnToolbeltSlot2Action -= InputManager_OnToolbeltSlot2Action;
        _inputManager.OnToolbeltSlot3Action -= InputManager_OnToolbeltSlot3Action;
        _inputManager.OnToolbeltSlot4Action -= InputManager_OnToolbeltSlot4Action;
        _inputManager.OnToolbeltSlot5Action -= InputManager_OnToolbeltSlot5Action;
        _inputManager.OnToolbeltSlot6Action -= InputManager_OnToolbeltSlot6Action;
        _inputManager.OnToolbeltSlot7Action -= InputManager_OnToolbeltSlot7Action;
        _inputManager.OnToolbeltSlot8Action -= InputManager_OnToolbeltSlot8Action;
        _inputManager.OnToolbeltSlot9Action -= InputManager_OnToolbeltSlot9Action;
        _inputManager.OnToolbeltSlot10Action -= InputManager_OnToolbeltSlot10Action;

        PauseGameManager.Instance.OnShowLocalPauseGame -= PauseMenuController_OnTogglePauseMenu;
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

    #region Input
    /// <summary>
    /// Processes the tool selection based on the mouse wheel input.
    /// </summary>
    private void ProcessToolSelection() {
        float mouseWheelDelta = _inputManager.GetMouseWheelVector().y;
        if (mouseWheelDelta == 0f) {
            return;
        }

        bool isNext = mouseWheelDelta < 0f;
        if (_toolbeltSize == _toolbeltSizes[^1] && _inputManager.GetLeftControlPressed()) {
            SelectToolbeltFromMouseWheele(isNext, mouseWheelDelta);
        } else {
            SelectToolFromMouseWheel(isNext);
        }

        OnToolbeltChanged?.Invoke();
    }

    /// <summary>
    /// Selects the next or previous tool in the toolbelt based on the mouse wheel input.
    /// </summary>
    /// <param name="isNext">A boolean value indicating whether to select the next tool (true) or the previous tool (false).</param>
    private void SelectToolFromMouseWheel(bool isNext) {
        if (isNext) {
            SetToolRight();
        } else {            
            SetToolLeft();
        }

        _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
    }

    /// <summary>
    /// Selects the next or previous toolbelt based on the mouse wheel input.
    /// </summary>
    /// <param name="isNext">True if selecting the next toolbelt, false if selecting the previous toolbelt.</param>
    /// <param name="mouseWheelDelta">The delta value of the mouse wheel input.</param>
    private void SelectToolbeltFromMouseWheele(bool isNext, float mouseWheelDelta) {
        int shiftAmount = mouseWheelDelta < 0f ? _toolbeltSizes[^1] : -_toolbeltSizes[^1];
        float rotationAmount = mouseWheelDelta < 0f ? _rotationStep : -_rotationStep;

        if (isNext) {
            SetNextToolbelt();
        } else {
            SetPreviousToolbelt();
        }

        _visual.ToolbeltChanged(_selectedToolbelt, rotationAmount);
        PlayerInventoryController.LocalInstance.InventoryContainer.ShiftSlots(shiftAmount);
        _visual.ShowUIButtonContains();
    }

    /// <summary>
    /// Sets the selected tool slot to the next slot in the toolbelt.
    /// </summary>
    private void SetToolRight() => _selectedToolSlot = (_selectedToolSlot + 1) % _toolbeltSize;
    /// <summary>
    /// Sets the selected tool slot to the left of the current slot.
    /// </summary>
    private void SetToolLeft() => _selectedToolSlot = (_selectedToolSlot - 1 + _toolbeltSize) % _toolbeltSize;
    /// <summary>
    /// Sets the next toolbelt in the rotation.
    /// </summary>
    private void SetNextToolbelt() => _selectedToolbelt = (_selectedToolbelt + 1) % MAX_TOOLBELTS;
    /// <summary>
    /// Sets the previous toolbelt in the player's inventory.
    /// </summary>
    private void SetPreviousToolbelt() => _selectedToolbelt = (_selectedToolbelt - 1 + MAX_TOOLBELTS) % MAX_TOOLBELTS;

    /// <summary>
    /// Event handler for left-clicking on a toolbelt slot in the toolbelt visual.
    /// </summary>
    /// <param name="selectedToolbeltSlot">The index of the selected toolbelt slot.</param>
    private void ToolbeltVisual_OnToolbeltSlotLeftClick(int selectedToolbeltSlot) => _selectedToolSlot = selectedToolbeltSlot;

    /// <summary>
    /// Handles the action of dropping an item from the player's toolbelt.
    /// </summary>
    private void InputManager_OnDropItemAction() {
        if (!IsOwner || DragItemUI.Instance.gameObject.activeSelf) {
            return;
        }

        var toolbeltItem = GetCurrentlySelectedToolbeltItemSlot();
        var playerMovement = GetComponent<PlayerMovementController>();
        var inventoryController = GetComponent<PlayerInventoryController>();

        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(toolbeltItem.ItemId, 1, toolbeltItem.RarityId),
            initialPosition: transform.position,
            motionDirection: playerMovement.LastMotionDirection,
            useInventoryPosition: true);

        inventoryController.InventoryContainer.RemoveItem(new ItemSlot(toolbeltItem.ItemId, 1, toolbeltItem.RarityId));
    }

    /// <summary>
    /// Selects the toolbelt slot with the specified slot ID.
    /// </summary>
    /// <param name="slotId">The ID of the toolbelt slot to select.</param>
    private void SelectToolbeltSlot(int slotId) {
        if (_toolbeltSize > slotId) {
            _selectedToolSlot = slotId;
            OnToolbeltChanged?.Invoke();
            _visual.SetToolbeltSlotHighlight(_selectedToolSlot);
        }
    }

    private void InputManager_OnToolbeltSlot1Action() => SelectToolbeltSlot(0);
    private void InputManager_OnToolbeltSlot2Action() => SelectToolbeltSlot(1);
    private void InputManager_OnToolbeltSlot3Action() => SelectToolbeltSlot(2);
    private void InputManager_OnToolbeltSlot4Action() => SelectToolbeltSlot(3);
    private void InputManager_OnToolbeltSlot5Action() => SelectToolbeltSlot(4);
    private void InputManager_OnToolbeltSlot6Action() => SelectToolbeltSlot(5);
    private void InputManager_OnToolbeltSlot7Action() => SelectToolbeltSlot(6);
    private void InputManager_OnToolbeltSlot8Action() => SelectToolbeltSlot(7);
    private void InputManager_OnToolbeltSlot9Action() => SelectToolbeltSlot(8);
    private void InputManager_OnToolbeltSlot10Action() => SelectToolbeltSlot(9);
    #endregion

    // ????
    private void SetToolbeltSize() {
        // TODO: Change when UI is implemented
        //InventoryPanel.Instance.InventoryOrToolbeltSizeChanged();
    }

    /// <summary>
    /// Clears the current item slot in the player's toolbelt.
    /// </summary>
    public void ClearCurrentItemSlot() {
        PlayerInventoryController.LocalInstance.InventoryContainer.ClearItemSlot(_selectedToolSlot);
        _visual.ShowUIButtonContains();
    }

    /// <summary>
    /// Represents a slot in the player's toolbelt that can hold an item.
    /// </summary>
    public ItemSlot GetCurrentlySelectedToolbeltItemSlot() => PlayerInventoryController.LocalInstance.InventoryContainer.ItemSlots[_selectedToolSlot];

    /// <summary>
    /// Locks or unlocks the toolbelt selection.
    /// </summary>
    /// <param name="block">True to lock the toolbelt selection, false to unlock it.</param>
    public void LockToolbelt(bool block) => _toolbeltSelectionBlocked = block;
    
    /// <summary>
    /// Event handler for toggling the pause menu.
    /// </summary>
    private void PauseMenuController_OnTogglePauseMenu() => OnToggleToolbelt?.Invoke();


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.LastSelectedToolbeltSlot = _selectedToolSlot;
        playerData.ToolbeltSize = _toolbeltSize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _selectedToolSlot = playerData.LastSelectedToolbeltSlot;
        _toolbeltSize = playerData.ToolbeltSize;
    }
    #endregion
}
