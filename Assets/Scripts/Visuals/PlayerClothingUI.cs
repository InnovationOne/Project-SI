using UnityEngine;
using UnityEngine.UI;

public class PlayerClothingUI : ItemContainerUI {
    public static PlayerClothingUI Instance { get; private set; }

    [Header("Character Parts Visuals")]
    [SerializeField] private Image[] _playerClothingUiImages;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of PlayerClothingUI in the scene!");
            return;
        }
        Instance = this;
    }

    public override void OnPlayerLeftClick(int buttonIndex) {
        PlayerController.LocalInstance.PlayerItemDragAndDropController.OnLeftClick(ItemContainer.ItemSlots[buttonIndex], ItemButtons[buttonIndex].GetComponent<InventorySlot>());
        ShowUIButtonContains();
        UpdatePlayerVisual();
    }

    private void UpdatePlayerVisual() {
        Debug.Log("UpdatePlayerVisual");
        for (int i = 0; i < _playerClothingUiImages.Length; i++) {
            _playerClothingUiImages[i].sprite = ItemButtons[i].GetComponent<InventorySlot>().GetPlayerClothingUiSprite();
            if (_playerClothingUiImages[i].sprite != null) {
                _playerClothingUiImages[i].enabled = true;
            } else {
                _playerClothingUiImages[i].enabled = false;
            }
        }
    }
}
