using UnityEngine;
using UnityEngine.UI;

public class ClothingUI : ItemContainerUI {
    public static ClothingUI Instance { get; private set; }

    [Header("Character Parts Visuals")]
    [SerializeField] private Image[] _playerClothingUiImages;

    public InventorySlot[] PlayerClothingUIItemButtons => ItemButtons;

    PlayerItemDragAndDropController _playerItemDragAndDropController;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlayerClothingUI in the scene!");
            return;
        }
        Instance = this;
    }

    private void Start() {
        _playerItemDragAndDropController = PlayerController.LocalInstance.PlayerItemDragAndDropController;
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        _playerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex], ItemButtons[buttonIndex]);
        ShowUIButtonContains();
        UpdatePlayerVisual();
    }

    private void UpdatePlayerVisual() {
        for (int i = 0; i < _playerClothingUiImages.Length; i++) {
            _playerClothingUiImages[i].sprite = ItemButtons[i].GetPlayerClothingUiSprite();
            _playerClothingUiImages[i].enabled = _playerClothingUiImages[i].sprite != null;
        }
    }
}
