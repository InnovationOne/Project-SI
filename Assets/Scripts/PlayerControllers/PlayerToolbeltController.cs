using System;
using Unity.Netcode;
using UnityEngine;

// This script controlls the players toolbelt
public class PlayerToolbeltController : NetworkBehaviour, IPlayerDataPersistance {
    public static PlayerToolbeltController LocalInstance { get; private set; }

    public event Action OnToolbeltChanged;

    public int[] ToolbeltSizes { get { return new int[] { 5, 7, 10 }; } }
    public int CurrentToolbeltSize = 10;

    private const int MAX_TOOLBELTS = 4;

    private int _currentlySelectedToolbeltSlot = 0;
    private int _currentlySelectedToolbelt = 0;
    private bool _toolbeltToolSelectionBlocked = false;


    private void Start() {
        SetToolbeltSize(CurrentToolbeltSize);
        ToolbeltPanel.Instance.SetToolbeltSlotHighlight(_currentlySelectedToolbeltSlot);

        ToolbeltPanel.Instance.OnToolbeltSlotLeftClick += ToolbeltVisual_OnToolbeltSlotLeftClick;

        InputManager.Instance.OnDropItemAction += InputManager_OnDropItemAction;
        InputManager.Instance.OnToolbeltSlot1Action += InputManager_OnToolbeltSlot1Action;
        InputManager.Instance.OnToolbeltSlot2Action += InputManager_OnToolbeltSlot2Action;
        InputManager.Instance.OnToolbeltSlot3Action += InputManager_OnToolbeltSlot3Action;
        InputManager.Instance.OnToolbeltSlot4Action += InputManager_OnToolbeltSlot4Action;
        InputManager.Instance.OnToolbeltSlot5Action += InputManager_OnToolbeltSlot5Action;
        InputManager.Instance.OnToolbeltSlot6Action += InputManager_OnToolbeltSlot6Action;
        InputManager.Instance.OnToolbeltSlot7Action += InputManager_OnToolbeltSlot7Action;
        InputManager.Instance.OnToolbeltSlot8Action += InputManager_OnToolbeltSlot8Action;
        InputManager.Instance.OnToolbeltSlot9Action += InputManager_OnToolbeltSlot9Action;
        InputManager.Instance.OnToolbeltSlot10Action += InputManager_OnToolbeltSlot10Action;

        PauseGameManager.Instance.OnShowLocalPauseGame += PauseMenuController_OnTogglePauseMenu;
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

    private void Update() {
        if (IsOwner && !_toolbeltToolSelectionBlocked) {
            if (Input.GetKey(KeyCode.LeftControl)) {
                // Select the toolbelt
                SelectToolbeltFromMouseWheele(InputManager.Instance.GetMouseWheelVector().y);
            } else {
                // Select the item slot
                SelectToolFromMouseWheel(InputManager.Instance.GetMouseWheelVector().y);
            }
        }
    }

    #region InputManager
    private void ToolbeltVisual_OnToolbeltSlotLeftClick(int selectedToolbeltSlot) {
        _currentlySelectedToolbeltSlot = selectedToolbeltSlot;
    }

    private void InputManager_OnDropItemAction() {
        if (!IsOwner) return;

        if (!DragItemPanel.Instance.gameObject.activeSelf) {
            ItemSpawnManager.Instance.SpawnItemFromInventory(GetCurrentlySelectedToolbeltItemSlot().Item, 1, GetCurrentlySelectedToolbeltItemSlot().RarityId, transform.position, GetComponent<PlayerMovementController>().LastMotionDirection);
            GetComponent<PlayerInventoryController>().InventoryContainer.ShootItem(_currentlySelectedToolbeltSlot);
        }
    }

    private void InputManager_OnToolbeltSlot1Action() {
        _currentlySelectedToolbeltSlot = 0;
        OnToolbeltChanged?.Invoke();
    }

    private void InputManager_OnToolbeltSlot2Action() {
        _currentlySelectedToolbeltSlot = 1;
        OnToolbeltChanged?.Invoke();
    }

    private void InputManager_OnToolbeltSlot3Action() {
        _currentlySelectedToolbeltSlot = 2;
        OnToolbeltChanged?.Invoke();
    }

    private void InputManager_OnToolbeltSlot4Action() {
        _currentlySelectedToolbeltSlot = 3;
        OnToolbeltChanged?.Invoke();
    }

    private void InputManager_OnToolbeltSlot5Action() {
        _currentlySelectedToolbeltSlot = 4;
        OnToolbeltChanged?.Invoke();
    }

    private void InputManager_OnToolbeltSlot6Action() {
        if (CurrentToolbeltSize > 5) {
            _currentlySelectedToolbeltSlot = 5;
            OnToolbeltChanged?.Invoke();
        }
    }

    private void InputManager_OnToolbeltSlot7Action() {
        if (CurrentToolbeltSize > 6) {
            _currentlySelectedToolbeltSlot = 6;
            OnToolbeltChanged?.Invoke();
        }
    }

    private void InputManager_OnToolbeltSlot8Action() {
        if (CurrentToolbeltSize > 7) {
            _currentlySelectedToolbeltSlot = 7;
            OnToolbeltChanged?.Invoke();
        }
    }

    private void InputManager_OnToolbeltSlot9Action() {
        if (CurrentToolbeltSize > 8) {
            _currentlySelectedToolbeltSlot = 8;
            OnToolbeltChanged?.Invoke();
        }
    }

    private void InputManager_OnToolbeltSlot10Action() {
        if (CurrentToolbeltSize > 9) {
            _currentlySelectedToolbeltSlot = 9;
            OnToolbeltChanged?.Invoke();
        }
    }

    private void SelectToolFromMouseWheel(float mouseWheelDelta) {
        if (mouseWheelDelta < 0f) {
            SetToolLeft();
            OnToolbeltChanged?.Invoke();
        } else if (mouseWheelDelta > 0f) {
            SetToolRight();
            OnToolbeltChanged?.Invoke();
        }
        ToolbeltPanel.Instance.SetToolbeltSlotHighlight(_currentlySelectedToolbeltSlot);
    }

    private void SelectToolbeltFromMouseWheele(float mouseWheelDelta) {
        if (mouseWheelDelta < 0f) {
            SetNextToolbelt();
            ToolbeltPanel.Instance.ToolbeltChanged(_currentlySelectedToolbelt, 90f);
            GetComponent<PlayerInventoryController>().InventoryContainer.ShiftSlots(10);
            ToolbeltPanel.Instance.ShowUIButtonContains();
            OnToolbeltChanged?.Invoke();
        } else if (mouseWheelDelta > 0f) {
            SetPreviousToolbelt();
            ToolbeltPanel.Instance.ToolbeltChanged(_currentlySelectedToolbelt, -90f);

            GetComponent<PlayerInventoryController>().InventoryContainer.ShiftSlots(-10);
            ToolbeltPanel.Instance.ShowUIButtonContains();
            OnToolbeltChanged?.Invoke();
        }
    }
    #endregion


    // This functions selects the tool left to the current tool. If there is no left tool set it to the most right
    private void SetToolLeft() {
        _currentlySelectedToolbeltSlot++;
        _currentlySelectedToolbeltSlot = _currentlySelectedToolbeltSlot >= CurrentToolbeltSize ? 0 : _currentlySelectedToolbeltSlot;
    }

    // This functions selects the tool right to the current tool. If there is no right tool set it to the most left
    private void SetToolRight() {
        _currentlySelectedToolbeltSlot--;
        _currentlySelectedToolbeltSlot = _currentlySelectedToolbeltSlot < 0 ? CurrentToolbeltSize - 1 : _currentlySelectedToolbeltSlot;
    }

    private void SetNextToolbelt() {
        _currentlySelectedToolbelt++;
        _currentlySelectedToolbelt = _currentlySelectedToolbelt >= MAX_TOOLBELTS ? 0 : _currentlySelectedToolbelt;
    }

    private void SetPreviousToolbelt() {
        _currentlySelectedToolbelt--;
        _currentlySelectedToolbelt = _currentlySelectedToolbelt < 0 ? MAX_TOOLBELTS - 1 : _currentlySelectedToolbelt;
    }

    public void SetToolbeltSize(int toolbeltSize) {
        if (toolbeltSize > ToolbeltSizes[^1]) {
            Debug.LogError("Can't set toolbeltSize higher than MAX_TOOLBELT_SIZE");
            return;
        }

        CurrentToolbeltSize = toolbeltSize;
        ToolbeltPanel.Instance.SetToolbeltSize(toolbeltSize);
        InventoryPanel.Instance.InventoryOrToolbeltSizeChanged();
    }

    public void ClearCurrentItemSlot() {
        gameObject.GetComponent<PlayerInventoryController>().InventoryContainer.ItemSlots[_currentlySelectedToolbeltSlot].Clear();
        ToolbeltPanel.Instance.ShowUIButtonContains();
    }

    public ItemSlot GetCurrentlySelectedToolbeltItemSlot() {
        return gameObject.GetComponent<PlayerInventoryController>().InventoryContainer.ItemSlots[_currentlySelectedToolbeltSlot];
    }

    // Blocks the toolselection on the toolbelt - block = true => to block.
    public void LockToolbeltSlotSelection() {
        _toolbeltToolSelectionBlocked = true;
    }

    public void UnlockToolbeltSlotSelection() {
        _toolbeltToolSelectionBlocked = false;
    }

    public void ToggleToolbelt() {
        ToolbeltPanel.Instance.ToggleToolbelt();
    }

    private void PauseMenuController_OnTogglePauseMenu() {
        ToolbeltPanel.Instance.ToggleToolbelt();
    }


    #region Save & Load
    public void SavePlayer(PlayerData playerData) {
        playerData.LastSelectedToolbeltSlot = _currentlySelectedToolbeltSlot;
        playerData.ToolbeltSize = CurrentToolbeltSize;
    }

    public void LoadPlayer(PlayerData playerData) {
        _currentlySelectedToolbeltSlot = playerData.LastSelectedToolbeltSlot;
        CurrentToolbeltSize = playerData.ToolbeltSize;
    }
    #endregion
}
