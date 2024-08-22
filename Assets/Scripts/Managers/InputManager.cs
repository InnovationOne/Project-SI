using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// This class handels the custom input system and saves / loads custom hotkeys
public class InputManager : NetworkBehaviour {
    public static InputManager Instance { get; private set; }

    private PlayerInputActions playerInputActions;

    // Player
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
    public event Action OnEscapeAction;
    public event Action OnPauseAction;

    public event Action OnToolbeltSlot1Action;
    public event Action OnToolbeltSlot2Action;
    public event Action OnToolbeltSlot3Action;
    public event Action OnToolbeltSlot4Action;
    public event Action OnToolbeltSlot5Action;
    public event Action OnToolbeltSlot6Action;
    public event Action OnToolbeltSlot7Action;
    public event Action OnToolbeltSlot8Action;
    public event Action OnToolbeltSlot9Action;
    public event Action OnToolbeltSlot10Action;

    public event Action OnLeftClickAction;
    public event Action OnRightClickAction;

    public event Action OnLeftControlAction;

    public const int SHIFT_KEY_AMOUNT = 10;


    // Debug Console
    public event Action DebugConsole_OnDebugConsoleAction;
    public event Action DebugConsole_OnCheatConsoleAction;
    public event Action DebugConsole_OnEnterAction;
    public event Action DebugConsole_OnArrowUpAction;
    public event Action DebugConsole_OnArrowDownAction;


    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Input Manager in the scene.");
        } else {
            Instance = this;
        }

        playerInputActions = new();
    }

    private void Start() {
        playerInputActions.Player.Enable();
        playerInputActions.Fishing.Disable();

        playerInputActions.Player.Run.performed += Run_performed;
        playerInputActions.Player.DropItem.performed += DropItem_performed;
        playerInputActions.Player.Interact.performed += Interact_performed;

        playerInputActions.Player.Inventory.performed += Inventory_performed;
        playerInputActions.Player.Craft.performed += Craft_performed;
        playerInputActions.Player.Relation.performed += Relation_performed;
        playerInputActions.Player.Wiki.performed += Wiki_performed;
        playerInputActions.Player.Character.performed += Character_performed;

        playerInputActions.Player.Mission.performed += Mission_performed;
        playerInputActions.Player.Map.performed += Map_performed;

        playerInputActions.Player.Pause.performed += Pause_performed;

        playerInputActions.Player.ToolbeltSlot1.performed += ToolbeltSlot1_performed;
        playerInputActions.Player.ToolbeltSlot2.performed += ToolbeltSlot2_performed;
        playerInputActions.Player.ToolbeltSlot3.performed += ToolbeltSlot3_performed;
        playerInputActions.Player.ToolbeltSlot4.performed += ToolbeltSlot4_performed;
        playerInputActions.Player.ToolbeltSlot5.performed += ToolbeltSlot5_performed;
        playerInputActions.Player.ToolbeltSlot6.performed += ToolbeltSlot6_performed;
        playerInputActions.Player.ToolbeltSlot7.performed += ToolbeltSlot7_performed;
        playerInputActions.Player.ToolbeltSlot8.performed += ToolbeltSlot8_performed;
        playerInputActions.Player.ToolbeltSlot9.performed += ToolbeltSlot9_performed;
        playerInputActions.Player.ToolbeltSlot10.performed += ToolbeltSlot10_performed;

        playerInputActions.Player.LeftClick.performed += LeftClick_performed;
        playerInputActions.Player.RightClick.performed += RightClick_performed;

        playerInputActions.Player.Escape.performed += Escape_performed;
        playerInputActions.Player.DebugConsole.performed += DebugConsole_DebugConsole_performed;


        // Debug Console
        playerInputActions.DebugConsole.DebugConsole.performed += DebugConsole_DebugConsole_performed;
        playerInputActions.DebugConsole.CheatConsole.performed += DebugConsole_CheatConsole_performed;
        playerInputActions.DebugConsole.Enter.performed += DebugConsoleEnter_performed;
        playerInputActions.DebugConsole.ArrowUp.performed += DebugConsoleArrowUp_performed;
        playerInputActions.DebugConsole.ArrowDown.performed += DebugConsoleArrowDown_performed;
    }

    #region Player
    private void Run_performed(InputAction.CallbackContext obj) {
        OnRunAction?.Invoke();
    }

    private void DropItem_performed(InputAction.CallbackContext obj) {
        OnDropItemAction?.Invoke();
    }

    private void Interact_performed(InputAction.CallbackContext obj) {
        OnInteractAction?.Invoke();
    }

    private void Inventory_performed(InputAction.CallbackContext obj) {
        OnInventoryAction?.Invoke();
    }

    private void Craft_performed(InputAction.CallbackContext obj) {
        OnCraftAction?.Invoke();
    }

    private void Relation_performed(InputAction.CallbackContext obj) {
        OnRelationAction?.Invoke();
    }

    private void Wiki_performed(InputAction.CallbackContext obj) {
        OnCollectionAction?.Invoke();
    }

    private void Character_performed(InputAction.CallbackContext obj) {
        OnCharacterAction?.Invoke();
    }

    private void Mission_performed(InputAction.CallbackContext obj) {
        OnMissionAction?.Invoke();
    }

    private void Map_performed(InputAction.CallbackContext obj) {
        OnMapAction?.Invoke();
    }

    private void Escape_performed(InputAction.CallbackContext obj) {
        OnEscapeAction?.Invoke();
    }

    private void Pause_performed(InputAction.CallbackContext obj) {
        OnPauseAction?.Invoke();
    }

    private void ToolbeltSlot1_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot1Action?.Invoke();
    }

    private void ToolbeltSlot2_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot2Action?.Invoke();
    }

    private void ToolbeltSlot3_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot3Action?.Invoke();
    }

    private void ToolbeltSlot4_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot4Action?.Invoke();
    }

    private void ToolbeltSlot5_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot5Action?.Invoke();
    }

    private void ToolbeltSlot6_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot6Action?.Invoke();
    }

    private void ToolbeltSlot7_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot7Action?.Invoke();
    }

    private void ToolbeltSlot8_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot8Action?.Invoke();
    }

    private void ToolbeltSlot9_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot9Action?.Invoke();
    }

    private void ToolbeltSlot10_performed(InputAction.CallbackContext obj) {
        OnToolbeltSlot10Action?.Invoke();
    }

    private void LeftClick_performed(InputAction.CallbackContext obj) {
        OnLeftClickAction?.Invoke();
    }

    private void RightClick_performed(InputAction.CallbackContext obj) {
        OnRightClickAction?.Invoke();
    }

    public Vector2 GetMovementVectorNormalized() {
        return playerInputActions.Player.Movement.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetMouseWheelVector() {
        return playerInputActions.Player.ToolbeltSlotSelect.ReadValue<Vector2>();
    }

    public Vector2 GetPointerPosition() {
        return playerInputActions.Player.PointerPosition.ReadValue<Vector2>();
    }

    public bool GetShiftPressed() {
        return playerInputActions.Player.Run.ReadValue<float>() > 0;
    }

    public bool GetLeftControlPressed() {
        return playerInputActions.Player.LeftControl.ReadValue<float>() > 0;
    }
    #endregion


    #region Fishing
    public bool GetButton1Pressed() => playerInputActions.Fishing.Button1.ReadValue<float>() > 0;
    public bool GetButton2Pressed() => playerInputActions.Fishing.Button2.ReadValue<float>() > 0;
    public bool GetButton3Pressed() => playerInputActions.Fishing.Button3.ReadValue<float>() > 0;
    public bool GetButton4Pressed() => playerInputActions.Fishing.Button4.ReadValue<float>() > 0;
    public bool GetButton5Pressed() => playerInputActions.Fishing.Button5.ReadValue<float>() > 0;
    public bool GetButton6Pressed() => playerInputActions.Fishing.Button6.ReadValue<float>() > 0;
    #endregion


    #region Debug Console
    public void DebugConsole_DebugConsole_performed(InputAction.CallbackContext obj) {
        DebugConsole_OnDebugConsoleAction?.Invoke();
    }

    public void DebugConsole_CheatConsole_performed(InputAction.CallbackContext obj) {
        DebugConsole_OnCheatConsoleAction?.Invoke();
    }

    public void DebugConsoleEnter_performed(InputAction.CallbackContext obj) {
        DebugConsole_OnEnterAction?.Invoke();
    }

    public void DebugConsoleArrowUp_performed(InputAction.CallbackContext obj) {
        DebugConsole_OnArrowUpAction?.Invoke();
    }

    public void DebugConsoleArrowDown_performed(InputAction.CallbackContext obj) {
        DebugConsole_OnArrowDownAction?.Invoke();
    }
    #endregion

    
    public void EnableDebugConsoleActionMap() {
        playerInputActions.DebugConsole.Enable();
        playerInputActions.Player.Disable();
        playerInputActions.Fishing.Disable();
    }

    public void EnablePlayerActionMap() {
        playerInputActions.DebugConsole.Disable();
        playerInputActions.Player.Enable();
        playerInputActions.Fishing.Disable();
    }


    private new void OnDestroy() {
        playerInputActions.Player.Run.performed -= Run_performed;
        playerInputActions.Player.DropItem.performed -= DropItem_performed;
        playerInputActions.Player.Interact.performed -= Interact_performed;

        playerInputActions.Player.Inventory.performed -= Inventory_performed;
        playerInputActions.Player.Relation.performed -= Relation_performed;
        playerInputActions.Player.Wiki.performed -= Wiki_performed;
        playerInputActions.Player.Character.performed -= Character_performed;
        playerInputActions.Player.Mission.performed -= Mission_performed;
        playerInputActions.Player.Map.performed -= Map_performed;
        playerInputActions.Player.Escape.performed -= Escape_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;

        playerInputActions.Player.ToolbeltSlot1.performed -= ToolbeltSlot1_performed;
        playerInputActions.Player.ToolbeltSlot2.performed -= ToolbeltSlot2_performed;
        playerInputActions.Player.ToolbeltSlot3.performed -= ToolbeltSlot3_performed;
        playerInputActions.Player.ToolbeltSlot4.performed -= ToolbeltSlot4_performed;
        playerInputActions.Player.ToolbeltSlot5.performed -= ToolbeltSlot5_performed;
        playerInputActions.Player.ToolbeltSlot6.performed -= ToolbeltSlot6_performed;
        playerInputActions.Player.ToolbeltSlot7.performed -= ToolbeltSlot7_performed;
        playerInputActions.Player.ToolbeltSlot8.performed -= ToolbeltSlot8_performed;
        playerInputActions.Player.ToolbeltSlot9.performed -= ToolbeltSlot9_performed;
        playerInputActions.Player.ToolbeltSlot10.performed -= ToolbeltSlot10_performed;

        playerInputActions.Player.LeftClick.performed -= LeftClick_performed;
        playerInputActions.Player.RightClick.performed -= RightClick_performed;

        playerInputActions.Player.Escape.performed -= Escape_performed;
        playerInputActions.Player.DebugConsole.performed -= DebugConsole_DebugConsole_performed;

        // Debug Console
        playerInputActions.DebugConsole.DebugConsole.performed -= DebugConsole_DebugConsole_performed;
        playerInputActions.DebugConsole.CheatConsole.performed -= DebugConsole_CheatConsole_performed;
        playerInputActions.DebugConsole.Enter.performed -= DebugConsoleEnter_performed;
        playerInputActions.DebugConsole.ArrowUp.performed -= DebugConsoleArrowUp_performed;
        playerInputActions.DebugConsole.ArrowDown.performed -= DebugConsoleArrowDown_performed;
    }
}
