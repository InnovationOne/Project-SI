using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolbeltUI : ItemContainerUI {
    public static ToolbeltUI Instance { get; private set; }

    public Action<int> OnToolbeltSlotLeftClick;

    [Header("Toolbelt Selection")]
    [SerializeField] Button _upBtn;
    [SerializeField] Button _downBtn;
    [SerializeField] TextMeshProUGUI _currentToolbeltIdx;
    [SerializeField] Image[] SelectionHighlights;
    [SerializeField] Sprite ToolbarCircleOn;
    [SerializeField] Sprite ToolbarCircleOff;

    [Header("Inventory Button")]
    [SerializeField] Button _inventoryButton;

    int _lastSelectedSlot;
    int _lastSelectedToolbelt;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("Multiple instances of ToolbeltUI found.");
            return;
        }
        Instance = this;
        PlayerController.OnLocalPlayerSpawned += CatchReferences;

        // Assign button events.
        _inventoryButton.onClick.AddListener(() => InventoryMasterUI.Instance.HandleInventoryToggle());
    }

    void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
    }

    private void OnDestroy() {
        ItemContainer.OnItemsUpdated -= ShowUIButtonContains;
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
    }

    void CatchReferences(PlayerController playerController) {
        _upBtn.onClick.AddListener(() => playerController.PlayerToolbeltController.ToggleToolbelt(true));
        _downBtn.onClick.AddListener(() => playerController.PlayerToolbeltController.ToggleToolbelt(false));
    }

    // Adjusts how many slots are interactable.
    public void SetToolbeltSize(int toolbeltSize) {
        for (int i = 0; i < ItemButtons.Length; i++) {
            ItemButtons[i].SetInteractable(i < toolbeltSize);
        }
    }

    // Updates highlight to show the active toolbelt slot.
    public void SetToolbeltSlotHighlight(int currentlySelectedSlot) {
        ItemButtons[_lastSelectedSlot].SetButtonHighlight(false);
        ItemButtons[currentlySelectedSlot].SetButtonHighlight(true);
        _lastSelectedSlot = currentlySelectedSlot;
    }

    // Changes the active toolbelt index and refreshes the circular UI.
    public void ToolbeltChanged(int selectedToolbelt) {
        SelectionHighlights[_lastSelectedToolbelt].sprite = ToolbarCircleOff;
        SelectionHighlights[selectedToolbelt].sprite = ToolbarCircleOn;
        _lastSelectedToolbelt = selectedToolbelt;
        _currentToolbeltIdx.text = (selectedToolbelt + 1).ToString();
    }

    // Toggles visibility of the toolbelt UI.
    public void ToggleToolbelt() {
        gameObject.SetActive(!gameObject.activeSelf);
        // Optionally call ShowUIButtonContains() if you want to refresh on toggle.
    }

    // Notifies listeners about a left-click on a toolbelt slot.
    public override void OnPlayerLeftClick(int selectedToolbeltSlot) {
        OnToolbeltSlotLeftClick?.Invoke(selectedToolbeltSlot);
        SetToolbeltSlotHighlight(selectedToolbeltSlot);
    }
}
