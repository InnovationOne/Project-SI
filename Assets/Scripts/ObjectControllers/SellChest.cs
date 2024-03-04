using UnityEngine;

public class SellChest : Interactable {
    [SerializeField] private ItemContainerSO _sellBoxContainer;
    [SerializeField] private SpriteRenderer _sellBoxHighlight;


    private void Awake() {
        _sellBoxHighlight.gameObject.SetActive(false);
    }

    public override void Interact(Player character) {

        if (character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot().Item != null 
            && character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot().Item.CanBeSold) {

            _sellBoxContainer.ItemSlots.Add(character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot());

            character.GetComponent<PlayerToolbeltController>().ClearCurrentItemSlot();
        }
    }

    public override void ShowPossibleInteraction(bool show) {
        _sellBoxHighlight.gameObject.SetActive(show);
    }
}
