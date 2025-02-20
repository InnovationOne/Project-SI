using UnityEngine;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("Inventory Container")]
    public GameObject InventoryRoot;
    public InventoryUI InventoryUI;
    public ClothingUI ClothingUI;
    public ChestUI ChestUI;

    [Header("UI auﬂerhalb des Inventarbereichs")]
    public ToolbeltUI ToolbeltUI;
    public DragItemUI DragItemUI;
    public DialogueUI DialogueUI;
    public PauseGameUI PauseGameUI;
    public HostDisconnectedUI HostDisconnectedUI;

    Canvas _mainCanvas;
    InputManager _inputManager;
    bool _inventoryIsOpen;
    bool _toolbeltIsOpen;
    GameObject _lastActiveInventorySubUI;


    void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of UIManager in the scene!");
            Destroy(this);
            return;
        }
        Instance = this;
        _mainCanvas = GetComponent<Canvas>();
    }

    void Start() {
        CloseInventory();
        OpenToolbelt();
        _inputManager = GameManager.Instance.InputManager;
        _inputManager.OnInventoryAction += ToggleInventory;
        _inputManager.OnEscapeAction += CloseInventory;
    }

    private void OnDestroy() {
        if (_inputManager != null) {
            _inputManager.OnInventoryAction -= ToggleInventory;
            _inputManager.OnEscapeAction -= CloseInventory;
        }
    }

    #region -------------------- Main methods for opening/closing --------------------

    // Toggles the entire inventory container.
    public void ToggleInventory() {
        if (!_inventoryIsOpen) OpenInventory(_lastActiveInventorySubUI != null ? _lastActiveInventorySubUI : InventoryUI.gameObject);
        else OpenToolbelt();
    }

    // Opens the inventory container and displays a specific sub-panel.
    public void OpenInventory(GameObject subUI) {
        if (ChestUI.gameObject.activeSelf) PlayerController.LocalInstance.PlayerInteractionController.StopInteract();

        CloseToolbelt();
        InventoryRoot.SetActive(true);
        _inventoryIsOpen = true;
        DeactivateAllInventorySubUIs();

        // If the chosen subUI is the main Inventory, also show Clothing.
        if (subUI == InventoryUI.gameObject) {
            InventoryUI.gameObject.SetActive(true);
            ClothingUI.gameObject.SetActive(true);
            _lastActiveInventorySubUI = InventoryUI.gameObject;
        } else if (subUI == ChestUI.gameObject) {
            InventoryUI.gameObject.SetActive(true);
            ChestUI.gameObject.SetActive(true);
            _lastActiveInventorySubUI = InventoryUI.gameObject;
        } else {
            if (subUI != null) subUI.SetActive(true);
            _lastActiveInventorySubUI = subUI;
        }
    }

    // Closes the entire inventory container.
    public void CloseInventory() {
        InventoryRoot.SetActive(false);
        _inventoryIsOpen = false;
        if (DragItemUI.gameObject.activeSelf && PlayerController.LocalInstance != null) {
            var c = PlayerController.LocalInstance.PlayerItemDragAndDropController;
            c.AddDragItemBackIntoBackpack(InventoryUI.LastSlotId);
        }
    }

    // Toggles the toolbelt UI while closing the inventory.
    public void ToggleToolbelt() {
        if (!_toolbeltIsOpen) OpenToolbelt();
        else CloseToolbelt();
    }

    // Opens the toolbelt
    public void OpenToolbelt() {
        // Schlieﬂe das Inventar, wenn Toolbelt geˆffnet wird.
        CloseInventory();

        ToolbeltUI.gameObject.SetActive(true);
        _toolbeltIsOpen = true;
    }

    // Closes the toolbelt
    public void CloseToolbelt() {
        ToolbeltUI.gameObject.SetActive(false);
        _toolbeltIsOpen = false;
    }

    #endregion -------------------- Main methods for opening/closing --------------------

    #region -------------------- Methods for specific scenarios --------------------

    // Opens the chest UI and forces the inventory open.
    public void OpenChestUI(ItemContainerSO itemContainer) {
        OpenInventory(ChestUI.gameObject);
        ChestUI.InitChestUI(itemContainer);
    }

    // Closes the chest UI and the inventory.
    public void CloseChestUI() {
        ChestUI.gameObject.SetActive(false);
        OpenToolbelt();
    }

    // Closes all sub-panels inside the inventory container.
    private void DeactivateAllInventorySubUIs() {
        InventoryUI.gameObject.SetActive(false);
        ClothingUI.gameObject.SetActive(false);
        ChestUI.gameObject.SetActive(false);
    }

    #endregion -------------------- Methods for specific scenarios --------------------

    #region -------------------- External UIs --------------------

    // Toggles additional UI panels.
    public void TogglePauseMenu() => PauseGameUI.gameObject.SetActive(!PauseGameUI.gameObject.activeSelf);
    public void ToggleDialogueUI() => DialogueUI.gameObject.SetActive(!DialogueUI.gameObject.activeSelf);
    public void ToggleHostDisconnectedUI() => HostDisconnectedUI.gameObject.SetActive(!HostDisconnectedUI.gameObject.activeSelf);

    public bool IsAnyBlockingUIOpen() {
        return InventoryRoot.activeSelf
            || PauseGameUI.gameObject.activeSelf
            || DialogueUI.gameObject.activeSelf
            || HostDisconnectedUI.gameObject.activeSelf;
    }

    // Check for whether certain sub-panels are open.
    public bool IsChestUIOpen() => ChestUI.gameObject.activeSelf;
    public bool IsClothingUIOpen() => ClothingUI.gameObject.activeSelf;
    public bool IsInventoryUIOpen() => InventoryUI.gameObject.activeSelf;

    #endregion -------------------- External UIs --------------------
}
