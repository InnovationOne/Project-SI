using Ink.Parsed;
using UnityEngine;

public class SellChest : MonoBehaviour, IInteractable {
    [SerializeField] private ItemContainerSO _sellBoxContainer;
    [SerializeField] private SpriteRenderer _sellBoxHighlight;

    public float MaxDistanceToPlayer => 0f;

    private void Awake() {
        _sellBoxHighlight.gameObject.SetActive(false);
    }

    public void Interact(PlayerController character) {

        if (GameManager.Instance.ItemManager.ItemDatabase[PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId] != null 
            && GameManager.Instance.ItemManager.ItemDatabase[PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId].CanBeSold) {

            var itemSlot = character.GetComponent<PlayerToolbeltController>().GetCurrentlySelectedToolbeltItemSlot();
            _sellBoxContainer.AddItem(itemSlot, false);

            character.GetComponent<PlayerToolbeltController>().ClearCurrentItemSlot();
        }
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void InitializePreLoad(int itemId) { }
}
