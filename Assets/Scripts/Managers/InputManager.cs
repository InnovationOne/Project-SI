using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// This class handels the custom input system and saves / loads custom hotkeys
public class InputManager : NetworkBehaviour {
    public static InputManager Instance { get; private set; }

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

    public event Action OnDebugConsoleAction;
    public event Action OnEnterAction;


    private PlayerInputActions playerInputActions;

    public const int SHIFT_KEY_AMOUNT = 10;


    // This functions is called when the script is loaded
    private void Awake() {
        if (Instance != null) {
            throw new Exception("Found more than one Input Manager in the scene.");
        } else {
            Instance = this;
        }

        playerInputActions = new();
        playerInputActions.Player.Enable();

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

        playerInputActions.Player.DebugConsole.performed += DebugConsole_performed;
        playerInputActions.Player.Enter.performed += Enter_performed;
        playerInputActions.Player.Escape.performed += Escape_performed;
    }

    private void Run_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnRunAction?.Invoke();
    }

    private void DropItem_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnDropItemAction?.Invoke();
    }

    private void Interact_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnInteractAction?.Invoke();
    }

    private void Inventory_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnInventoryAction?.Invoke();
    }

    private void Craft_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnCraftAction?.Invoke();
    }

    private void Relation_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnRelationAction?.Invoke();
    }

    private void Wiki_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnCollectionAction?.Invoke();
    }

    private void Character_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnCharacterAction?.Invoke();
    }

    private void Mission_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnMissionAction?.Invoke();
    }

    private void Map_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnMapAction?.Invoke();
    }

    private void Escape_performed(InputAction.CallbackContext obj) {
        OnEscapeAction?.Invoke();
    }

    private void Pause_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnPauseAction?.Invoke();
    }

    private void ToolbeltSlot1_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot1Action?.Invoke();
    }

    private void ToolbeltSlot2_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot2Action?.Invoke();
    }

    private void ToolbeltSlot3_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot3Action?.Invoke();
    }

    private void ToolbeltSlot4_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot4Action?.Invoke();
    }

    private void ToolbeltSlot5_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot5Action?.Invoke();
    }

    private void ToolbeltSlot6_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot6Action?.Invoke();
    }

    private void ToolbeltSlot7_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot7Action?.Invoke();
    }

    private void ToolbeltSlot8_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot8Action?.Invoke();
    }

    private void ToolbeltSlot9_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot9Action?.Invoke();
    }

    private void ToolbeltSlot10_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnToolbeltSlot10Action?.Invoke();
    }

    private void LeftClick_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnLeftClickAction?.Invoke();
    }

    private void RightClick_performed(InputAction.CallbackContext obj) {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return; }
        OnRightClickAction?.Invoke();
    }

    public void DebugConsole_performed(InputAction.CallbackContext obj) {
        OnDebugConsoleAction?.Invoke();
    }

    public void Enter_performed(InputAction.CallbackContext obj) {
        OnEnterAction?.Invoke();
    }

    public Vector2 GetMovementVectorNormalized() {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return Vector2.zero; }
        return playerInputActions.Player.Movement.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetMouseWheelVector() {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return Vector2.zero; }
        return playerInputActions.Player.ToolbeltSlotSelect.ReadValue<Vector2>();
    }

    public Vector2 GetPointerPosition() {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return Vector2.zero; }
        return playerInputActions.Player.PointerPosition.ReadValue<Vector2>();
    }

    public bool GetShiftPressed() {
        if (PlayerDebugController.Instance.ShowDebugConsole) { return false; }
        return playerInputActions.Player.Run.ReadValue<float>() > 0;
    }


    private void OnDestroy() {
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
        playerInputActions.Player.DebugConsole.performed -= DebugConsole_performed;
        playerInputActions.Player.Enter.performed -= Enter_performed;

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


    }
}
