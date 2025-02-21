using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles custom input actions and manages input-related events.
/// </summary>
public class InputManager : MonoBehaviour {
    // Input Action Maps
    private PlayerInputActions _playerInputActions;

    // Player Events
    public event Action OnRunAction;
    public event Action OnDashAction;
    public event Action OnDropItemAction;
    public event Action OnInteractAction;

    public event Action OnInventoryAction;
    public event Action OnEscapeAction;

    public event Action OnRotateAction;
    public event Action OnVMirrorAction;
    public event Action OnHMirrorAction;

    // Mouse Click Events
    public event Action OnLeftClickAction;
    public event Action OnLeftClickStarted;
    public event Action OnLeftClickCanceled;
    public event Action OnRightClickAction;
    public event Action OnRightClickStarted;
    public event Action OnRightClickCanceled;

    // Modifier Keys
    public event Action OnLeftControlAction;

    // Debug Console Events
    public event Action DebugConsole_OnDebugConsoleAction;
    public event Action Player_OnDebugConsoleAction;
    public event Action DebugConsole_OnCheatConsoleAction;
    public event Action DebugConsole_OnEnterAction;
    public event Action DebugConsole_OnArrowUpAction;
    public event Action DebugConsole_OnArrowDownAction;

    public event Action Dialogue_OnContinueAction;
    public event Action Dialogue_OnContinueStarted;
    public event Action Dialogue_OnContinueCanceled;
    public event Action Dialogue_OnResponseDown;
    public event Action Dialogue_OnResponseUp;


    // Constants
    public const int SHIFT_KEY_AMOUNT = 10;

    // Array der ToolbeltSlot InputActions für einfachen Zugriff
    private readonly InputAction[] _toolbeltSlotInputActions = new InputAction[10];

    // Dictionary zur Speicherung der Delegates für die ToolbeltSlot Performeds
    private readonly Dictionary<int, Action<InputAction.CallbackContext>> _toolbeltSlotPerformedCallbacks = new();

    // Toolbelt Slot Actions
    private readonly Dictionary<int, Action> _toolbeltSlotActions = new();

    private bool _blockPlayerActions = false;

    /// <summary>
    /// Initializes the singleton instance and input actions.
    /// </summary>
    private void Awake() {
        _playerInputActions = new PlayerInputActions();
    }

    /// <summary>
    /// Subscribes an action to a specific toolbelt slot.
    /// </summary>
    /// <param name="slotNumber">The toolbelt slot number (1-10).</param>
    /// <param name="action">The action to invoke when the slot is activated.</param>
    public void SubscribeToolbeltSlotAction(int slotNumber, Action action) {
        if (slotNumber < 1 || slotNumber > 10) {
            Debug.LogError($"Invalid toolbelt slot number: {slotNumber}. Must be between 1 and 10.");
            return;
        }

        if (_toolbeltSlotActions.ContainsKey(slotNumber)) {
            _toolbeltSlotActions[slotNumber] += action;
        } else {
            _toolbeltSlotActions[slotNumber] = action;
        }
    }

    /// <summary>
    /// Unsubscribes an action from a specific toolbelt slot.
    /// </summary>
    /// <param name="slotNumber">The toolbelt slot number (1-10).</param>
    /// <param name="action">The action to remove.</param>
    public void UnsubscribeToolbeltSlotAction(int slotNumber, Action action) {
        if (_toolbeltSlotActions.ContainsKey(slotNumber)) {
            _toolbeltSlotActions[slotNumber] -= action;
            if (_toolbeltSlotActions[slotNumber] == null) {
                _toolbeltSlotActions.Remove(slotNumber);
            }
        }
    }

    /// <summary>
    /// Enables the appropriate action maps on start.
    /// </summary>
    private void Start() {
        _playerInputActions.Player.Enable();
        _playerInputActions.DebugConsole.Disable(); // Assuming DebugConsole is disabled by default

        // Initialisieren der ToolbeltSlot InputActions
        InitializeToolbeltSlotActions();

        // Subscribe to player input actions
        _playerInputActions.Player.Run.performed += Run_performed;
        _playerInputActions.Player.Dash.performed += Dash_performed;
        _playerInputActions.Player.DropItem.performed += DropItem_performed;
        _playerInputActions.Player.Interact.performed += Interact_performed;

        _playerInputActions.Player.Inventory.performed += Inventory_performed;
        _playerInputActions.Player.Escape.performed += Escape_performed;

        _playerInputActions.Player.RotateCWObj.performed += RotateObj_Performed;
        _playerInputActions.Player.VMirrorObj.performed += VMirrorObj_Performed;
        _playerInputActions.Player.HMirrorObj.performed += HMirrorObj_Performed;

        // Mouse Clicks
        _playerInputActions.Player.LeftClick.performed += LeftClick_performed;
        _playerInputActions.Player.LeftClick.started += LeftClick_started;
        _playerInputActions.Player.LeftClick.canceled += LeftClick_canceled;
        _playerInputActions.Player.RightClick.performed += RightClick_performed;
        _playerInputActions.Player.RightClick.started += RightClick_started;
        _playerInputActions.Player.RightClick.canceled += RightClick_canceled;

        // Modifier Keys
        _playerInputActions.Player.LeftControl.performed += LeftControl_performed;
        _playerInputActions.Player.LeftControl.canceled += LeftControl_canceled;

        // Debug Console
        _playerInputActions.DebugConsole.DebugConsole.performed += DebugConsole_DebugConsole_performed;
        _playerInputActions.Player.DebugConsole.performed += Player_DebugConsole_performed;
        _playerInputActions.DebugConsole.CheatConsole.performed += DebugConsole_CheatConsole_performed;
        _playerInputActions.DebugConsole.Enter.performed += DebugConsoleEnter_performed;
        _playerInputActions.DebugConsole.ArrowUp.performed += DebugConsoleArrowUp_performed;
        _playerInputActions.DebugConsole.ArrowDown.performed += DebugConsoleArrowDown_performed;

        // Dialogue
        _playerInputActions.Dialogue.Continue.performed += Dialogue_Continue_performed;
        _playerInputActions.Dialogue.Continue.started += Dialogue_Continue_started;
        _playerInputActions.Dialogue.Continue.canceled += Dialogue_Continue_canceled;
        _playerInputActions.Dialogue.ResponseDown.performed += Dialogue_ResponseDown_performed;
        _playerInputActions.Dialogue.ResponseUp.performed += Dialogue_ResponseUp_performed;
    }

    /// <summary>
    /// Initialisiert die ToolbeltSlot InputActions und abonniert deren performed Events.
    /// </summary>
    private void InitializeToolbeltSlotActions() {
        // Manuelles Zuordnen der ToolbeltSlot InputActions
        _toolbeltSlotInputActions[0] = _playerInputActions.Player.ToolbeltSlot1;
        _toolbeltSlotInputActions[1] = _playerInputActions.Player.ToolbeltSlot2;
        _toolbeltSlotInputActions[2] = _playerInputActions.Player.ToolbeltSlot3;
        _toolbeltSlotInputActions[3] = _playerInputActions.Player.ToolbeltSlot4;
        _toolbeltSlotInputActions[4] = _playerInputActions.Player.ToolbeltSlot5;
        _toolbeltSlotInputActions[5] = _playerInputActions.Player.ToolbeltSlot6;
        _toolbeltSlotInputActions[6] = _playerInputActions.Player.ToolbeltSlot7;
        _toolbeltSlotInputActions[7] = _playerInputActions.Player.ToolbeltSlot8;
        _toolbeltSlotInputActions[8] = _playerInputActions.Player.ToolbeltSlot9;
        _toolbeltSlotInputActions[9] = _playerInputActions.Player.ToolbeltSlot10;

        for (int i = 0; i < _toolbeltSlotInputActions.Length; i++) {
            int slotNumber = i + 1; // 1-basiert
            Action<InputAction.CallbackContext> callback = (ctx) => InvokeToolbeltSlotAction(slotNumber);
            _toolbeltSlotInputActions[i].performed += callback;
            _toolbeltSlotPerformedCallbacks.Add(slotNumber, callback);
        }
    }

    #region Player Input Handlers
    private void Run_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnRunAction?.Invoke();
    }

    private void Dash_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnDashAction?.Invoke();
    }

    private void DropItem_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnDropItemAction?.Invoke();
    }

    private void Interact_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnInteractAction?.Invoke();
    }

    private void Inventory_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnInventoryAction?.Invoke();
    }

    private void Escape_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnEscapeAction?.Invoke();
    }

    private void RotateObj_Performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnRotateAction?.Invoke();
    }

    private void VMirrorObj_Performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnVMirrorAction?.Invoke();
    }

    private void HMirrorObj_Performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnHMirrorAction?.Invoke();
    }


    private void LeftClick_performed(InputAction.CallbackContext obj) => OnLeftClickAction?.Invoke();

    private void LeftClick_started(InputAction.CallbackContext obj) => OnLeftClickStarted?.Invoke();

    private void LeftClick_canceled(InputAction.CallbackContext obj) => OnLeftClickCanceled?.Invoke();


    private void RightClick_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return; 
        OnRightClickAction?.Invoke();
    }

    private void RightClick_started(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return; 
        OnRightClickStarted?.Invoke(); 
    }

    private void RightClick_canceled(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return; 
        OnRightClickCanceled?.Invoke(); 
    }

    private void LeftControl_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnLeftControlAction?.Invoke();
    }

    private void LeftControl_canceled(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return;
        OnLeftControlAction?.Invoke();
    }
    #endregion

    #region Toolbelt Slot Handling
    /// <summary>
    /// Invokes all subscribed actions for a specific toolbelt slot.
    /// </summary>
    /// <param name="slotNumber">The toolbelt slot number (1-10).</param>
    private void InvokeToolbeltSlotAction(int slotNumber) {
        if (_blockPlayerActions) return;
        if (_toolbeltSlotActions.TryGetValue(slotNumber, out var action)) {
            action?.Invoke();
        }
    }
    #endregion

    #region Debug Console Handlers
    private void DebugConsole_DebugConsole_performed(InputAction.CallbackContext obj) => DebugConsole_OnDebugConsoleAction?.Invoke();

    private void Player_DebugConsole_performed(InputAction.CallbackContext obj) {
        if (_blockPlayerActions) return; 
        Player_OnDebugConsoleAction?.Invoke();
    }

    private void DebugConsole_CheatConsole_performed(InputAction.CallbackContext obj) => DebugConsole_OnCheatConsoleAction?.Invoke();

    private void DebugConsoleEnter_performed(InputAction.CallbackContext obj) => DebugConsole_OnEnterAction?.Invoke();

    private void DebugConsoleArrowUp_performed(InputAction.CallbackContext obj) => DebugConsole_OnArrowUpAction?.Invoke();

    private void DebugConsoleArrowDown_performed(InputAction.CallbackContext obj) => DebugConsole_OnArrowDownAction?.Invoke();

    private void Dialogue_Continue_performed(InputAction.CallbackContext obj) => Dialogue_OnContinueAction?.Invoke();

    private void Dialogue_Continue_started(InputAction.CallbackContext obj) => Dialogue_OnContinueStarted?.Invoke();

    private void Dialogue_Continue_canceled(InputAction.CallbackContext obj) => Dialogue_OnContinueCanceled?.Invoke();

    private void Dialogue_ResponseDown_performed(InputAction.CallbackContext obj) => Dialogue_OnResponseDown?.Invoke();

    private void Dialogue_ResponseUp_performed(InputAction.CallbackContext obj) => Dialogue_OnResponseUp?.Invoke();
    #endregion

    /// <summary>
    /// Retrieves the normalized movement vector.
    /// </summary>
    /// <returns>Normalized movement vector.</returns>
    public Vector2 GetMovementVectorNormalized() {
        return _playerInputActions.Player.Movement.ReadValue<Vector2>().normalized;
    }

    /// <summary>
    /// Retrieves the mouse wheel input vector.
    /// </summary>
    /// <returns>Mouse wheel vector.</returns>
    public Vector2 GetMouseWheelVector() {
        if (_blockPlayerActions) return Vector2.zero;
        return _playerInputActions.Player.MouseWheel.ReadValue<Vector2>();
    }

    /// <summary>
    /// Retrieves the current pointer position.
    /// </summary>
    /// <returns>Pointer position vector.</returns>
    public Vector2 GetPointerPosition() {
        if (_blockPlayerActions) return Vector2.zero;
        return _playerInputActions.Player.PointerPosition.ReadValue<Vector2>();
    }

    /// <summary>
    /// Checks if the shift key is pressed.
    /// </summary>
    /// <returns>True if shift is pressed; otherwise, false.</returns>
    public bool GetShiftPressed() {
        if (_blockPlayerActions) return false;
        return _playerInputActions.Player.Run.ReadValue<float>() > 0;
    }

    /// <summary>
    /// Checks if the left control key is pressed.
    /// </summary>
    /// <returns>True if left control is pressed; otherwise, false.</returns>
    public bool GetLeftControlPressed() {
        if (_blockPlayerActions) return false;
        return _playerInputActions.Player.LeftControl.ReadValue<float>() > 0;
    }

    #region Action Map Management

    public void EnableDebugConsoleActionMap() {
        _playerInputActions.Player.Disable();
        _playerInputActions.Dialogue.Disable();

        _playerInputActions.DebugConsole.Enable();
    }

    public void EnablePlayerActionMap() {
        _playerInputActions.DebugConsole.Disable();
        _playerInputActions.Dialogue.Disable();

        _playerInputActions.Player.Enable();
    }

    public void EnableDialogueActionMap() {
        _playerInputActions.DebugConsole.Disable();
        _playerInputActions.Player.Disable();

        _playerInputActions.Dialogue.Enable();
    }

    public void DisableAll() {
        _playerInputActions.DebugConsole.Disable();
        _playerInputActions.Player.Disable();
        _playerInputActions.Dialogue.Disable();
    }

    public void EnableAll() {
        _playerInputActions.DebugConsole.Enable();
        _playerInputActions.Player.Enable();
        _playerInputActions.Dialogue.Enable();
    }

    public void BlockPlayerActions(bool block) {
        _blockPlayerActions = block;
    }

    #endregion

    /// <summary>
    /// Cleans up event subscriptions to prevent memory leaks.
    /// </summary>
    private void OnDestroy() {
        // Unsubscribe from player input actions
        _playerInputActions.Player.Run.performed -= Run_performed;
        _playerInputActions.Player.Dash.performed -= Dash_performed;
        _playerInputActions.Player.DropItem.performed -= DropItem_performed;
        _playerInputActions.Player.Interact.performed -= Interact_performed;

        _playerInputActions.Player.Inventory.performed -= Inventory_performed;
        _playerInputActions.Player.Escape.performed -= Escape_performed;

        _playerInputActions.Player.RotateCWObj.performed -= RotateObj_Performed;
        _playerInputActions.Player.VMirrorObj.performed -= VMirrorObj_Performed;
        _playerInputActions.Player.HMirrorObj.performed -= HMirrorObj_Performed;

        _playerInputActions.Player.LeftClick.performed -= LeftClick_performed;
        _playerInputActions.Player.LeftClick.started -= LeftClick_started;
        _playerInputActions.Player.LeftClick.canceled -= LeftClick_canceled;
        _playerInputActions.Player.RightClick.performed -= RightClick_performed;
        _playerInputActions.Player.RightClick.started -= RightClick_started;
        _playerInputActions.Player.RightClick.canceled -= RightClick_canceled;

        _playerInputActions.Player.LeftControl.performed -= LeftControl_performed;
        _playerInputActions.Player.LeftControl.canceled -= LeftControl_canceled;

        // Unsubscribe from debug console actions
        _playerInputActions.DebugConsole.DebugConsole.performed -= DebugConsole_DebugConsole_performed;
        _playerInputActions.Player.DebugConsole.performed -= Player_DebugConsole_performed;
        _playerInputActions.DebugConsole.CheatConsole.performed -= DebugConsole_CheatConsole_performed;
        _playerInputActions.DebugConsole.Enter.performed -= DebugConsoleEnter_performed;
        _playerInputActions.DebugConsole.ArrowUp.performed -= DebugConsoleArrowUp_performed;
        _playerInputActions.DebugConsole.ArrowDown.performed -= DebugConsoleArrowDown_performed;

        _playerInputActions.Dialogue.Continue.performed -= Dialogue_Continue_performed;
        _playerInputActions.Dialogue.Continue.started -= Dialogue_Continue_started;
        _playerInputActions.Dialogue.Continue.canceled -= Dialogue_Continue_canceled;
        _playerInputActions.Dialogue.ResponseDown.performed -= Dialogue_ResponseDown_performed;
        _playerInputActions.Dialogue.ResponseUp.performed -= Dialogue_ResponseUp_performed;

        // Unsubscribe from toolbelt slot performed events
        foreach (var kvp in _toolbeltSlotPerformedCallbacks) {
            int slotNumber = kvp.Key;
            var callback = kvp.Value;
            if (slotNumber >= 1 && slotNumber <= 10) {
                _toolbeltSlotInputActions[slotNumber - 1].performed -= callback;
            }
        }
        _toolbeltSlotPerformedCallbacks.Clear();
        _toolbeltSlotActions.Clear();

        // Dispose input actions
        _playerInputActions.Dispose();
    }
}
