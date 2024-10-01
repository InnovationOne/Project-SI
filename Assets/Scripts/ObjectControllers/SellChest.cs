using Ink.Parsed;
using UnityEngine;

public class SellChest : MonoBehaviour, IInteractable {
    [SerializeField] private ItemContainerSO _sellBoxContainer;
    [SerializeField] private SpriteRenderer _sellBoxHighlight;

    public float MaxDistanceToPlayer => 0f;

    private void Awake() {
        _sellBoxHighlight.gameObject.SetActive(false);
    }

    public void Interact(Player character) {

        if (ItemManager.Instance.ItemDatabase[PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId] != null 
            && ItemManager.Instance.ItemDatabase[PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId].CanBeSold) {

            var itemSlot = character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot();
            _sellBoxContainer.AddItem(itemSlot, false);

            character.GetComponent<PlayerToolbeltController>().ClearCurrentItemSlot();
        }
    }

    public void PickUpItemsInPlacedObject(Player player) { }

    public void InitializePreLoad(int itemId) { }
}
