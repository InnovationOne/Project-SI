using UnityEngine;
using UnityEngine.UI;

public class ClothingUI : ItemContainerUI {
    public static ClothingUI Instance { get; private set; }

    [Header("Character Parts Visuals")]
    [SerializeField] Image[] _playerClothingUiImages;

    public InventorySlot[] PlayerClothingUIItemButtons => ItemButtons;
    public ItemContainerSO PlayerClothingUIItemContainer => ItemContainer;

    PlayerItemDragAndDropController _playerItemDragAndDropController;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlayerClothingUI in the scene!");
            return;
        }
        Instance = this;
        PlayerController.OnLocalPlayerSpawned += CatchReferences;
    }

    void Start() {
        ItemContainer.OnItemsUpdated += ShowUIButtonContains;
        Init();
        UpdatePlayerVisual();
    }

    void CatchReferences(PlayerController playerController) {
        _playerItemDragAndDropController = playerController.PlayerItemDragAndDropController;
    }

    private void OnDestroy() {
        ItemContainer.OnItemsUpdated -= ShowUIButtonContains;
        PlayerController.OnLocalPlayerSpawned -= CatchReferences;
    }    

    public override void OnPlayerLeftClick(int buttonIndex) {
        // SHIFT-CLICK => Move from clothing slot back to Inventory
        if (GameManager.Instance.InputManager.GetShiftPressed()) {
            var clothingSlot = ItemContainer.ItemSlots[buttonIndex];
            if (!clothingSlot.IsEmpty) {
                var inventoryContainer = PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer;
                int leftover = inventoryContainer.AddItem(clothingSlot, false);
                if (leftover > 0) clothingSlot.Set(new ItemSlot(clothingSlot.ItemId, leftover, clothingSlot.RarityId));
                else clothingSlot.Clear();
                GameManager.Instance.AudioManager.PlayOneShot(GameManager.Instance.FMODEvents.ItemShift, transform.position);
                ShowUIButtonContains();
                UpdatePlayerVisual();
                return;
            }
        }

        _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex], ItemButtons[buttonIndex]);
        ShowUIButtonContains();
        UpdatePlayerVisual();
    }

    void UpdatePlayerVisual() {
        for (int i = 0; i < _playerClothingUiImages.Length; i++) {
            _playerClothingUiImages[i].sprite = ItemButtons[i].GetPlayerClothingUiSprite();
            _playerClothingUiImages[i].enabled = _playerClothingUiImages[i].sprite != null;
        }
    }
}
