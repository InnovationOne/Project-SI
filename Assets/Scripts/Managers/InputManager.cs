using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles custom input actions and manages input-related events.
/// Utilizes modern C# features and optimizes for performance and memory usage.
/// </summary>
public class InputManager : NetworkBehaviour {
    // Singleton Instance
    public static InputManager Instance { get; private set; }

    // Input Action Maps
    private PlayerInputActions _playerInputActions;

    // Player Events
    public event Action OnRunAction;
    public event Action OnDropItemAction;
    public event Action OnInteractAction;
    public event Action OnInventoryAction;
    public event Action OnCraftAction;
    public event Action OnRelationAction;
    public event Action OnCollectionAction;
    public event Action OnCharacterAction;
    public event Action OnMissionAction;
    public event Action OnMapAction;
    public event Action OnPauseAction;
    public event Action OnEscapeAction;

    // Toolbelt Slot Actions
    private readonly Dictionary<int, Action> _toolbeltSlotActions = new Dictionary<int, Action>();

    // Mouse Click Events
    public event Action OnLeftClickAction;
    public event Action OnLeftClickStarted;
    public event Action OnLeftClickCanceled;
    public event Action OnRightClickAction;

    // Modifier Keys
    public event Action OnLeftControlAction;

    // Debug Console Events
    public event Action DebugConsole_OnDebugConsoleAction;
    public event Action DebugConsole_OnCheatConsoleAction;
    public event Action DebugConsole_OnEnterAction;
    public event Action DebugConsole_OnArrowUpAction;
    public event Action DebugConsole_OnArrowDownAction;

    // Constants
    public const int SHIFT_KEY_AMOUNT = 10;

    // Array der ToolbeltSlot InputActions für einfachen Zugriff
    private readonly InputAction[] _toolbeltSlotInputActions = new InputAction[10];

    // Dictionary zur Speicherung der Delegates für die ToolbeltSlot Performeds
    private readonly Dictionary<int, Action<InputAction.CallbackContext>> _toolbeltSlotPerformedCallbacks = new Dictionary<int, Action<InputAction.CallbackContext>>();


    /// <summary>
    /// Initializes the singleton instance and input actions.
    /// </summary>
    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Input Manager in the scene.");
        } else {
            Instance = this;
        }

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
        _playerInputActions.Player.DropItem.performed += DropItem_performed;
        _playerInputActions.Player.Interact.performed += Interact_performed;

        _playerInputActions.Player.Inventory.performed += Inventory_performed;
        _playerInputActions.Player.Craft.performed += Craft_performed;
        _playerInputActions.Player.Relation.performed += Relation_performed;
        _playerInputActions.Player.Wiki.performed += Wiki_performed;
        _playerInputActions.Player.Character.performed += Character_performed;

        _playerInputActions.Player.Mission.performed += Mission_performed;
        _playerInputActions.Player.Map.performed += Map_performed;

        _playerInputActions.Player.Pause.performed += Pause_performed;
        _playerInputActions.Player.Escape.performed += Escape_performed;

        // Mouse Clicks
        _playerInputActions.Player.LeftClick.performed += LeftClick_performed;
        _playerInputActions.Player.LeftClick.started += LeftClick_started;
        _playerInputActions.Player.LeftClick.canceled += LeftClick_canceled;
        _playerInputActions.Player.RightClick.performed += RightClick_performed;

        // Modifier Keys
        _playerInputActions.Player.LeftControl.performed += LeftControl_performed;
        _playerInputActions.Player.LeftControl.canceled += LeftControl_canceled;

        // Debug Console
        _playerInputActions.DebugConsole.DebugConsole.performed += DebugConsole_DebugConsole_performed;
        _playerInputActions.DebugConsole.CheatConsole.performed += DebugConsole_CheatConsole_performed;
        _playerInputActions.DebugConsole.Enter.performed += DebugConsoleEnter_performed;
        _playerInputActions.DebugConsole.ArrowUp.performed += DebugConsoleArrowUp_performed;
        _playerInputActions.DebugConsole.ArrowDown.performed += DebugConsoleArrowDown_performed;
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
    private void Run_performed(InputAction.CallbackContext obj) => OnRunAction?.Invoke();

    private void DropItem_performed(InputAction.CallbackContext obj) => OnDropItemAction?.Invoke();

    private void Interact_performed(InputAction.CallbackContext obj) => OnInteractAction?.Invoke();

    private void Inventory_performed(InputAction.CallbackContext obj) => OnInventoryAction?.Invoke();

    private void Craft_performed(InputAction.CallbackContext obj) => OnCraftAction?.Invoke();

    private void Relation_performed(InputAction.CallbackContext obj) => OnRelationAction?.Invoke();

    private void Wiki_performed(InputAction.CallbackContext obj) => OnCollectionAction?.Invoke();

    private void Character_performed(InputAction.CallbackContext obj) => OnCharacterAction?.Invoke();

    private void Mission_performed(InputAction.CallbackContext obj) => OnMissionAction?.Invoke();

    private void Map_performed(InputAction.CallbackContext obj) => OnMapAction?.Invoke();

    private void Pause_performed(InputAction.CallbackContext obj) => OnPauseAction?.Invoke();

    private void Escape_performed(InputAction.CallbackContext obj) => OnEscapeAction?.Invoke();

    private void LeftClick_performed(InputAction.CallbackContext obj) => OnLeftClickAction?.Invoke();

    private void LeftClick_started(InputAction.CallbackContext obj) => OnLeftClickStarted?.Invoke();

    private void LeftClick_canceled(InputAction.CallbackContext obj) => OnLeftClickCanceled?.Invoke();

    private void RightClick_performed(InputAction.CallbackContext obj) => OnRightClickAction?.Invoke();

    private void LeftControl_performed(InputAction.CallbackContext obj) => OnLeftControlAction?.Invoke();

    private void LeftControl_canceled(InputAction.CallbackContext obj) => OnLeftControlAction?.Invoke();
    #endregion

    #region Toolbelt Slot Handling
    /// <summary>
    /// Invokes all subscribed actions for a specific toolbelt slot.
    /// </summary>
    /// <param name="slotNumber">The toolbelt slot number (1-10).</param>
    private void InvokeToolbeltSlotAction(int slotNumber) {
        if (_toolbeltSlotActions.TryGetValue(slotNumber, out var action)) {
            action?.Invoke();
        }
    }
    #endregion

    #region Debug Console Handlers
    private void DebugConsole_DebugConsole_performed(InputAction.CallbackContext obj) => DebugConsole_OnDebugConsoleAction?.Invoke();

    private void DebugConsole_CheatConsole_performed(InputAction.CallbackContext obj) => DebugConsole_OnCheatConsoleAction?.Invoke();

    private void DebugConsoleEnter_performed(InputAction.CallbackContext obj) => DebugConsole_OnEnterAction?.Invoke();

    private void DebugConsoleArrowUp_performed(InputAction.CallbackContext obj) => DebugConsole_OnArrowUpAction?.Invoke();

    private void DebugConsoleArrowDown_performed(InputAction.CallbackContext obj) => DebugConsole_OnArrowDownAction?.Invoke();
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
        return _playerInputActions.Player.MouseWheel.ReadValue<Vector2>();
    }

    /// <summary>
    /// Retrieves the current pointer position.
    /// </summary>
    /// <returns>Pointer position vector.</returns>
    public Vector2 GetPointerPosition() {
        return _playerInputActions.Player.PointerPosition.ReadValue<Vector2>();
    }

    /// <summary>
    /// Checks if the shift key is pressed.
    /// </summary>
    /// <returns>True if shift is pressed; otherwise, false.</returns>
    public bool GetShiftPressed() {
        return _playerInputActions.Player.Run.ReadValue<float>() > 0;
    }

    /// <summary>
    /// Checks if the left control key is pressed.
    /// </summary>
    /// <returns>True if left control is pressed; otherwise, false.</returns>
    public bool GetLeftControlPressed() {
        return _playerInputActions.Player.LeftControl.ReadValue<float>() > 0;
    }

    #region Action Map Management

    /// <summary>
    /// Enables the Debug Console action map and disables the Player action map.
    /// </summary>
    public void EnableDebugConsoleActionMap() {
        _playerInputActions.DebugConsole.Enable();
        _playerInputActions.Player.Disable();
    }

    /// <summary>
    /// Enables the Player action map and disables the Debug Console action map.
    /// </summary>
    public void EnablePlayerActionMap() {
        _playerInputActions.DebugConsole.Disable();
        _playerInputActions.Player.Enable();
    }

    #endregion

    /// <summary>
    /// Cleans up event subscriptions to prevent memory leaks.
    /// </summary>
    private void OnDestroy() {
        // Unsubscribe from player input actions
        _playerInputActions.Player.Run.performed -= Run_performed;
        _playerInputActions.Player.DropItem.performed -= DropItem_performed;
        _playerInputActions.Player.Interact.performed -= Interact_performed;

        _playerInputActions.Player.Inventory.performed -= Inventory_performed;
        _playerInputActions.Player.Craft.performed -= Craft_performed;
        _playerInputActions.Player.Relation.performed -= Relation_performed;
        _playerInputActions.Player.Wiki.performed -= Wiki_performed;
        _playerInputActions.Player.Character.performed -= Character_performed;

        _playerInputActions.Player.Mission.performed -= Mission_performed;
        _playerInputActions.Player.Map.performed -= Map_performed;

        _playerInputActions.Player.Pause.performed -= Pause_performed;
        _playerInputActions.Player.Escape.performed -= Escape_performed;

        _playerInputActions.Player.LeftClick.performed -= LeftClick_performed;
        _playerInputActions.Player.LeftClick.started -= LeftClick_started;
        _playerInputActions.Player.LeftClick.canceled -= LeftClick_canceled;
        _playerInputActions.Player.RightClick.performed -= RightClick_performed;

        _playerInputActions.Player.LeftControl.performed -= LeftControl_performed;
        _playerInputActions.Player.LeftControl.canceled -= LeftControl_canceled;

        // Unsubscribe from debug console actions
        _playerInputActions.DebugConsole.DebugConsole.performed -= DebugConsole_DebugConsole_performed;
        _playerInputActions.DebugConsole.CheatConsole.performed -= DebugConsole_CheatConsole_performed;
        _playerInputActions.DebugConsole.Enter.performed -= DebugConsoleEnter_performed;
        _playerInputActions.DebugConsole.ArrowUp.performed -= DebugConsoleArrowUp_performed;
        _playerInputActions.DebugConsole.ArrowDown.performed -= DebugConsoleArrowDown_performed;

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
