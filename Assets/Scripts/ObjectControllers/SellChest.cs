using UnityEngine;

public class SellChest : Interactable {
    [SerializeField] private ItemContainerSO _sellBoxContainer;
    [SerializeField] private SpriteRenderer _sellBoxHighlight;


    private void Awake() {
        _sellBoxHighlight.gameObject.SetActive(false);
    }

    public override void Interact(Player character) {

        if (ItemManager.Instance.ItemDatabase[PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId] != null 
            && ItemManager.Instance.ItemDatabase[PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId].CanBeSold) {

            var itemSlot = character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot();
            _sellBoxContainer.AddItem(itemSlot, false);

            character.GetComponent<PlayerToolbeltController>().ClearCurrentItemSlot();
        }
    }

    public override void ShowPossibleInteraction(bool show) {
        _sellBoxHighlight.gameObject.SetActive(show);
    }
}
